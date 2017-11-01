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
        private readonly bool _SSL = false;
        private readonly SslStream _sslStream;
        private readonly NetworkStream _stream;
        int port = 1234;
        TcpClient TcpClient;
        string CurrentSessionId;
        Thread read, getData;
        public Boolean isConnected;
        int measurement = 0;
        private IConnector _conn;
        private FormBikeControl _bikeControl;



        public ClientConnection(string username, string password)
        {
            Console.WriteLine($"{username} + {password}");
            IPAddress localhost;
            bool ipIsOk = IPAddress.TryParse("127.0.0.1", out localhost);
            if (!ipIsOk)
            {
                Console.WriteLine("ip adres kan niet geparsed worden."); Environment.Exit(1);
            }

            TcpClient = new TcpClient(localhost.ToString(), port);
            _stream = TcpClient.GetStream();
            if (_SSL)
            {
                _sslStream = new SslStream(_stream, false, new RemoteCertificateValidationCallback(ValidateCert));
                _sslStream.AuthenticateAsClient("Healthcare", null, System.Security.Authentication.SslProtocols.Tls12, false);
            }
            isConnected = true;
            read = new Thread(Read);
            read.Start();
            sendlogin(username, password);

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
                        int numberOfBytesRead = _SSL ? _sslStream.Read(receiveBuffer, 0, receiveBuffer.Length) : _stream.Read(receiveBuffer, 0, receiveBuffer.Length);
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
                    if (_SSL)
                    {
                        _sslStream.Flush();
                    }
                    else
                    {
                        _stream.Flush();
                    }
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
                    GetData());
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
                    new Thread(() => { MessageBox.Show("Username and password are correct"); }).Start();
                    close();
                }
            }
            if (jsonData.id == "client/SetPower")
            {
                if (_conn != null)
                {
                    _conn.SetPower(jsonData.data.power);
                }
            }
            if (jsonData.id == "StartAstrand")
            {

            }
        }

        public void Send(string message)
        {
            System.Diagnostics.Debug.WriteLine("Send: \r\n" + message);
            byte[] prefixArray = BitConverter.GetBytes(message.Length);
            byte[] requestArray = Encoding.Default.GetBytes(message);
            byte[] buffer = new Byte[prefixArray.Length + message.Length];
            prefixArray.CopyTo(buffer, 0);
            requestArray.CopyTo(buffer, prefixArray.Length);
            if (_SSL)
            {
                _sslStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                _stream.Write(buffer, 0, buffer.Length);
            }


        }

        public void GetData()
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
                    if (_conn.GetType() == typeof(KettlerConnector))
                    {
                        System.Diagnostics.Debug.WriteLine(isConnected);
                        _conn.GetStats(msg =>
                        {
                            var status = new KettlerStatus(msg, CurrentSessionId);

                            dynamic KettlerData = new
                            {
                                id = "data",
                                session = CurrentSessionId,
                                data = new
                                {
                                    power = status.ActualPower,
                                    speed = status.Speed,
                                    time = status.Time,
                                    RPM = status.Rpm,
                                    distance = status.Distance,
                                    pulse = status.Heartbeat
                                }
                            };
                            Send(JsonConvert.SerializeObject(KettlerData));
                        });
                    }

                }
            });
        }

        public void close()
        {
            if (_conn != null)
            {
                _conn.Close();
            }

            try
            {
                read.Abort();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error: " + e.Message);
            }
            try
            {
                getData.Abort();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error: " + e.Message);
            }
            try
            {
                if (_SSL)
                {
                    _sslStream.Close();
                }
                else
                {
                    _stream.Close();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error: " + e.Message);
            }
            try
            {
                TcpClient.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error: " + e.Message);
            }
        }

        public void SetPower(int power)
        {
            _conn.SetPower(power);
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

        public static bool ValidateCert(object sender, X509Certificate certificate,
              X509Chain chain, SslPolicyErrors sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None;
    }
}

