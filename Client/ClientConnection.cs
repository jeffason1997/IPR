using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;



namespace Client
{
    public class ClientConnection
    {
        private readonly NetworkStream _stream;
        int port = 1234;
        TcpClient TcpClient;
        string CurrentSessionId;
        Thread read;
        public Boolean isConnected;
        private IConnector _conn;
        private FormBikeControl _bikeControl;
        private ClientForm CForm;
        private string Username;



        public ClientConnection(string username, string password)
        {
            //Console.WriteLine($"{username} + {password}");
            IPAddress localhost;
            bool ipIsOk = IPAddress.TryParse("127.0.0.1", out localhost);
            if (!ipIsOk)
            {
                Console.WriteLine("ip adres kan niet geparsed worden."); Environment.Exit(1);
            }

            TcpClient = new TcpClient(localhost.ToString(), port);
            _stream = TcpClient.GetStream();
            isConnected = true;
            read = new Thread(Read);
            read.Start();
            Username = username;
            sendlogin(username, password);

        }

        public void setClientForm(ClientForm form)
        {
            CForm = form;
        }

        public void setBikeControl(FormBikeControl form)
        {
            _bikeControl = form;
        }

        public void Read()
        {
            while (isConnected)
            {
                try
                {
                    StringBuilder response = new StringBuilder();
                    int totalBytesreceived = 0;
                    int lengthMessage = -1;
                    byte[] receiveBuffer = new byte[1024];
                    bool messagereceived = false;

                    do
                    {
                        int numberOfBytesRead = _stream.Read(receiveBuffer, 0, receiveBuffer.Length);
                        totalBytesreceived += numberOfBytesRead;
                        string received = Encoding.ASCII.GetString(receiveBuffer, 0, numberOfBytesRead);
                        response.AppendFormat("{0}", received);
                        if (lengthMessage == -1)
                        {
                            if (receiveBuffer.Length >= 4)
                            {
                                Byte[] lengthMessageArray = new Byte[4];
                                Array.Copy(receiveBuffer, 0, lengthMessageArray, 0, 3);
                                lengthMessage = BitConverter.ToInt32(lengthMessageArray, 0);
                                if ((totalBytesreceived - 4) == lengthMessage)
                                {
                                    messagereceived = true;
                                }
                            }
                        }
                        else if ((totalBytesreceived - 4) == lengthMessage)
                        {
                            messagereceived = true;
                        }
                    }
                    while (!messagereceived);
                    _stream.Flush();
                    string toReturn = response.ToString().Substring(4);
                    System.Diagnostics.Debug.WriteLine("Received client: \r\n" + toReturn);
                    ProcessAnswer(toReturn);

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }

        public void sendlogin(String username, String password)
        {
            dynamic sendlogin = new
            {
                id = "log in",
                data = new
                {
                    username = username,
                    password = password
                }
            };
            Send(JsonConvert.SerializeObject(sendlogin));
        }

        public void ProcessAnswer(string information)
        {
            dynamic jsonData = JsonConvert.DeserializeObject(information);

            if (jsonData.id == "session/start")
            {
                string user = jsonData.data.sessionID;
                CurrentSessionId = Guid.NewGuid().ToString();
                string com = _bikeControl.GetCom();
                if (com == "SIM")
                    _conn = new FakeConnector();
                else
                    _conn = new KettlerConnector(com);

                Task.Run(() =>
                {
                    _conn.Open();
                    Thread.Sleep(1000);
                    _conn.Reset();
                    Thread.Sleep(1000);
                    _conn.GetId((msg) =>
                    GetData(user));
                });
            }
            if (jsonData.id == "session/end")
            {
                System.Diagnostics.Debug.WriteLine("Closing...");
                isConnected = false;
                close();
            }
            if (jsonData.id == "log in")
            {
                if (jsonData.data.status != "ok")
                {
                    new Thread(() => { MessageBox.Show("Username or password is incorrect"); }).Start();
                    close();
                }
                else
                {
                    close();
                }
            }
            if (jsonData.id == "doctor/Client")
            {
                //Console.WriteLine(jsonData);
                string user = (string)jsonData.data.username;
                int age = (int)jsonData.data.age;
                Sex sex = jsonData.data.sex;
                int weight = jsonData.data.weight;
                ClientInfo tempInfo = new ClientInfo(user, age, sex, weight);
                CForm.updateClientInfo(tempInfo);
            }
            if (jsonData.id == "client/SetPower")
            {
                if (_conn != null)
                {
                    setPower(jsonData.data.power);
                }
            }
            if (jsonData.id == "StartAstrand")
            {

            }
            if (jsonData.id == "client/message")
            {
                CForm.updateTextBox((string)jsonData.data.message);
            }
        }

        public void setPower(int power)
        {
            _conn.SetPower(power);
        }

        public void Send(string message)
        {
            System.Diagnostics.Debug.WriteLine("Send: \r\n" + message);
            byte[] prefixArray = BitConverter.GetBytes(message.Length);
            byte[] requestArray = Encoding.Default.GetBytes(message);
            byte[] buffer = new Byte[prefixArray.Length + message.Length];
            prefixArray.CopyTo(buffer, 0);
            requestArray.CopyTo(buffer, prefixArray.Length);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void GetData(string user)
        {
            while (CurrentSessionId == null)
            {
                Thread.Sleep(100);
            }
            Task.Run(() =>
            {
                while (isConnected)
                {
                    Thread.Sleep(1000);
                    System.Diagnostics.Debug.WriteLine(isConnected);
                    _conn.GetStats(msg =>
                    {
                        var status = new KettlerStatus(msg, CurrentSessionId);
                        CForm.updateKettlerStats(status);
                        dynamic KettlerData = new
                        {
                            id = "data",
                            session = user,
                            data = new
                            {
                                status = status
                            }
                        };
                        Send(JsonConvert.SerializeObject(KettlerData));
                    });
                }
            });
        }

        public void close()
        {
            if (_conn != null)
            {
                _conn.Close();
                read.Abort();
                _stream.Close();
                TcpClient.Close();
            }
        }


        public void getClientInfo()
        {
            dynamic getInfo = new
            {
                id = "session/getClientInfo",
                data = new
                {
                    username = Username
                }
            };
            Send(JsonConvert.SerializeObject(getInfo));
        }

        public void StartAstrand()
        {
            try
            {
                //astrand.Start();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("client regel 348: " + e.StackTrace);
            }
        }
    }
}

