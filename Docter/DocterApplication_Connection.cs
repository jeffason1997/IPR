using System;
using System.Collections.Generic;
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
using Client;


namespace Docter
{
    public class DocterApplication_Connection
    {
        readonly NetworkStream _stream;
        int port = 1234;
        TcpClient client;
        IPAddress localhost;
        Boolean isConnected;
        private DocterForm DForm;
        private TrainingHandler trainingHandler;
        private List<TrainingItem> listTraining = new List<TrainingItem>();


        public DocterApplication_Connection(string user, string password)
        {

            bool ipIsOk = IPAddress.TryParse("127.0.0.1", out localhost);
            if (!ipIsOk) { Console.WriteLine("ip adres kan niet geparsed worden."); Environment.Exit(1); }

            client = new TcpClient(localhost.ToString(), port);
            _stream = client.GetStream();
            isConnected = true;
            Thread read = new Thread(Read);
            read.Start();
            sendLogin(user, password);
        }

        public void setDForm(DocterForm form)
        {
            DForm = form;
            trainingHandler = new TrainingHandler(DForm, this);
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
                    System.Diagnostics.Debug.WriteLine("Received: \r\n" + toReturn);
                    ProcessAnswer(toReturn);

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }

        public void ProcessAnswer(string information)
        {
            dynamic jsonData = JsonConvert.DeserializeObject(information);

            if (jsonData.id == "doctor/sessions")
            {

                List<String> connected_Sessions = new List<string>();
                foreach (dynamic d in jsonData.data.sessions)
                {
                    connected_Sessions.Add((string)d);
                }
                DForm.UpdateComboBox(connected_Sessions);
            }
            else if (jsonData.id == "doctor/Client")
            {
                //Console.WriteLine(jsonData);
                string user = (string)jsonData.data.username;
                int age = (int)jsonData.data.age;
                Sex sex = jsonData.data.sex;
                int weight = (int) jsonData.data.weight;
                ClientInfo tempInfo = new ClientInfo(user, age, sex,weight);
                trainingHandler.CInfo = tempInfo;
                DForm.updateClientInfo(tempInfo);
            }
            else if (jsonData.id == "data")
            {
                string user = jsonData.sessionId;
                dynamic jsonObjectHealth = jsonData.data.data;

                HealthData tempHealth = new HealthData((byte)jsonObjectHealth.Heartbeat, (int)jsonObjectHealth.Rpm, (int)jsonObjectHealth.Speed, (float)jsonObjectHealth.Distance, (short)jsonObjectHealth.RequestedPower, (float)jsonObjectHealth.Energy, (int)jsonObjectHealth.Time, (short)jsonObjectHealth.ActualPower, (string)jsonObjectHealth.SessionId);
                TrainingItem tempItem = trainingHandler.MakeTrainingsItem(tempHealth);

                
                trainingHandler.handleTraining(tempItem);
                listTraining.Add(tempItem);

            }
            else if (jsonData.id == "doctor/UnfollowPatient")
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
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void sendLogin(string username, string password)
        {
            dynamic sendLogin = new
            {
                id = "doctor/login",
                data = new
                {
                    username = username,
                    password = password
                }

            };
            Send(JsonConvert.SerializeObject(sendLogin));
        }

        public void getClientInfo(string username)
        {
            dynamic getInfo = new
            {
                id = "session/getClientInfo",
                data = new
                {
                    username = username
                }
            };
            Send(JsonConvert.SerializeObject(getInfo));
        }

        public void startTraining(string patientID)
        {
            dynamic startTraining = new
            {
                id = "doctor/training/start",
                data = new
                {
                    patientId = patientID
                }
            };

            Send(JsonConvert.SerializeObject(startTraining));
        }

        public void stopTraining(String patientID)
        {
            dynamic stopTraining = new
            {
                id = "doctor/training/stop",
                data = new
                {
                    patientId = patientID

                }


            };
            Send(JsonConvert.SerializeObject(stopTraining));
        }

        public void sendAstrandInfo(dynamic jsonObject,string user)
        {
            dynamic listTrain = new
            {
                id = "session/write",
                name = user,
                data = new
                {
                    list = listTraining.ToArray()
                }
            };
            Send(JsonConvert.SerializeObject(jsonObject));
        }

        public void sendMessageToClient(string message, string patientID)
        {
            dynamic sendMessageToClient = new
            {
                id = "doctor/message/toClient",
                data = new
                {
                    messageId = message,
                    patientiD = patientID
                }

            };
            Send(JsonConvert.SerializeObject(sendMessageToClient));
        }

        public void setPower(string power, string username)
        {
            dynamic setPower = new
            {
                id = "doctor/setPower",
                data = new
                {
                    power = power,
                    patientID = username
                }

            };
            Send(JsonConvert.SerializeObject(setPower));
        }

        public void getSessions()
        {
            dynamic getSessions = new
            {
                id = "doctor/sessions"

            };
            Send(JsonConvert.SerializeObject(getSessions));
        }

        public void FollowPatient(string SessionId)
        {
            dynamic followPatient = new
            {
                id = "doctor/FollowPatient",
                data = new
                {
                    username = SessionId
                }
            };
            Send(JsonConvert.SerializeObject(followPatient));
        }

        public void UnFollowPatient(string SessionId)
        {
            dynamic unFollowPatient = new
            {
                id = "doctor/UnfollowPatient",
                data = new
                {
                    username = SessionId
                }

            };
            Send(JsonConvert.SerializeObject(unFollowPatient));
        }

        public void close()
        {
            _stream.Close();
            client.Close();
        }
    }
}

