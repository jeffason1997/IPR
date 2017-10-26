using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Client;
using Newtonsoft.Json;

namespace IPR
{
    public class Session
    {
        private bool _SSL = false;
        private readonly NetworkStream _stream;
        readonly SslStream _sslStream;
        public Boolean IsDoctor;
        public ClientInfo client;
        public List<Session> DoctorsToSendDataTo;
        private string Username;

        public Session(TcpClient client)
        {
            _stream = client.GetStream();
            if (_SSL)
            {
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2 certificate = store.Certificates.Find(X509FindType.FindByThumbprint, "52F29B382FD556BE6B75B9F026470A1609186C64", false)[0];
                store.Close();
                _sslStream = new SslStream(_stream, false);
                _sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls12, true);
            }
            DoctorsToSendDataTo = new List<Session>();
        }

        //Send to networkstream
        #region
        public void Send(string message)
        {
            System.Diagnostics.Debug.WriteLine("Send from server: \r\n" + message);
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
        #endregion

        //Read from networkstream
        #region
        public void Read()
        {
            while (true)
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
                    System.Diagnostics.Debug.WriteLine("Received at server: \r\n" + toReturn);
                    ProcesAnswer(toReturn);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }
        #endregion

        //Process answer
        #region
        public void ProcesAnswer(dynamic answer)
        {
            Console.WriteLine(answer);
            dynamic jsonObject = JsonConvert.DeserializeObject(answer);
            try
            {
                if (jsonObject.id == "log in")
                {
                    CheckCredentials((string)jsonObject.data.username, (string)jsonObject.data.password);
                }
                if (jsonObject.id == "session/start")
                {
                    NoPermission("session/start");
                }
                else if (jsonObject.id == "data")
                {
                    Console.WriteLine("Data recieved");
                    DataRecieved(jsonObject);
                }
                else if (jsonObject.id == "start")
                {

                }
                else if (jsonObject.id == "pauze")
                {

                }
                else if (jsonObject.id == "session/end")
                {
                    NoPermission("session/end");
                }
                else if (jsonObject.id == "StopAstrand")
                {
                    ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                    if (CloseSession(client))
                    {
                        FileWriteClass.Close();
                    }
                }
                else if (jsonObject.id == "doctor/sessions")
                {
                    //SessionList();
                }
                else if (jsonObject.id == "doctor/training/start")
                {
                    ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                    if (CreateNewSession(client, true))
                    {
                        dynamic response = new
                        {
                            id = "doctor/training/start",
                            data = new
                            {
                                status = "ok"
                            }
                        };
                        Send(JsonConvert.SerializeObject(response));
                    }
                }
                else if (jsonObject.id == "doctor/training/stop")
                {
                    ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                    if (CloseSession(client))
                    {
                        dynamic response = new
                        {
                            id = "doctor/training/stop",
                            data = new
                            {
                                status = "ok"
                            }
                        };
                        Send(JsonConvert.SerializeObject(response));
                        FileWriteClass.Close();
                    }
                }
                else if (jsonObject.id == "doctor/setPower")
                {
                    if (SetPowerFromClient(jsonObject))
                    {
                        dynamic response = new
                        {
                            id = "doctor/setPower",
                            data = new
                            {
                                status = "ok",
                            }
                        };
                        Send(response);
                    }
                }
                else if (jsonObject.id == "doctor/FollowPatient")
                {
                    if (IsDoctor)
                    {
                        ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                        FolowAPatientSession(client);
                    }
                    else
                    {
                        NoPermission("doctor/FollowPatient");
                    }
                }
                else if (jsonObject.id == "doctor/UnfollowPatient")
                {
                    if (IsDoctor)
                    {
                        ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                        UnFollowAPatientSession(client);
                    }
                    else
                    {
                        NoPermission("doctor/FollowPatient");
                    }
                }
                else if (jsonObject.id == "doctor/StartAstrand")
                {
                    if (IsDoctor)
                    {
                        ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                        CreateNewSession(client, false);
                        StartAstrandFromPatient(client);
                    }
                    else
                    {
                        NoPermission("doctor/StartAstrand");
                    }
                }
            }
            catch (Exception e)
            {
                dynamic error = new
                {
                    id = "command",
                    status = "Error",
                    error = e.Message
                };
                Send(JsonConvert.SerializeObject(error));
            }
        }
        #endregion

        //Login normal and doctor.
        #region
        public void CheckCredentials(string username, string password)
        {
            if (FileWriteClass.CheckCredentials(username, password))
            {
                Username = username;
                Send(JsonConvert.SerializeObject(Commands.LoginResponse("ok")));
            }
            else
            {
                Send(JsonConvert.SerializeObject(Commands.LoginResponse("error")));
            }
        }

        public void CheckDoctorCredentials(string username, string password)
        {
            if (FileWriteClass.CheckDoctorCredentials(username, password))
            {
                IsDoctor = true;
                Username = username;
                Send(JsonConvert.SerializeObject(Commands.DoctorLoginResponse("ok")));
            }
            else
            {
                Send(JsonConvert.SerializeObject(Commands.DoctorLoginResponse("error")));
            }
        }
        #endregion

        //Create new session.
        #region
        public Boolean CreateNewSession(ClientInfo client, Boolean responseStartSession)
        {
            if (!IsDoctor)
            {
                NoPermission("doctor/training/start");
                return false;
            }
            else
            {
                Session ThisClient = ServerProgram.GetSessionWithUsername(client);
                if (ThisClient == null)
                {
                    Send(JsonConvert.SerializeObject(Commands.DoctorTrainingStartError("No client active with given username.")));
                    return false;
                }
                try
                {

                    if (FileWriteClass.AddActiveSession(client))
                    {
                        dynamic answer = new
                        {
                            id = "session/start",
                            data = new
                            {
                                status = "OK",
                                sessionID = client
                            }
                        };
                        if (responseStartSession)
                        {
                            ThisClient.Send(JsonConvert.SerializeObject(answer));
                        }
                        return true;
                    }
                    else
                    {
                        Send(JsonConvert.SerializeObject(Commands.DoctorTrainingStartError("Username already active. Other session has to be stopped first before starting a new one.")));
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Send(JsonConvert.SerializeObject(Commands.DoctorTrainingStartError(e.Message)));
                    return false;
                }
            }
        }
        #endregion



        //Recieved data from patient
        #region
        public void DataRecieved(dynamic jsonObject)
        {
            try
            {
                ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);

                HealthData tempData = new HealthData((byte)jsonObject.Status.Heartbeat, (int)jsonObject.Status.RPM,
                    (int)jsonObject.Status.Speed, (float)jsonObject.Status.Distance,
                    (short)jsonObject.Status.RequestedPower, (float)jsonObject.Status.Energy, (int)jsonObject.Status.Time,
                    (short)jsonObject.Status.ActualPower, (string)jsonObject.Status.SessionId);

                TrainingItem tempItem = new TrainingItem((TypeOfTraining)jsonObject.ThisType, (int)jsonObject.Seconds,
                    tempData, jsonObject.SessionTime);

                Boolean added = FileWriteClass.AddDataToSession(client, tempItem);
                if (added)
                {
                    dynamic answer = new
                    {
                        id = "data",
                        data = new
                        {
                            status = "OK"
                        }
                    };
                    Send(JsonConvert.SerializeObject(answer));
                    dynamic answerToDoctor = new
                    {
                        id = "data",
                        sessionId = client,
                        data = new
                        {
                            data = tempItem
                        }
                    };
                    foreach (Session s in DoctorsToSendDataTo)
                    {
                        s.Send(JsonConvert.SerializeObject(answerToDoctor));
                    }
                }
                else
                {
                    dynamic answer = new
                    {
                        id = "data",
                        data = new
                        {
                            status = "ERROR",
                            error = "Geen sessie voor username bekend"
                        }
                    };
                    Send(JsonConvert.SerializeObject(answer));
                }

            }
            catch (Exception e)
            {
                dynamic answer = new
                {
                    id = "data",
                    status = "Error",
                    error = e.Message
                };
                Send(JsonConvert.SerializeObject(answer));
            }
        }
        #endregion

        //Close session
        #region
        public Boolean CloseSession(ClientInfo client)
        {
            Session Sclient = ServerProgram.GetSessionWithUsername(client);
            if (Sclient == null)
            {
                Send(JsonConvert.SerializeObject(Commands.DoctorTrianingStopError("No client active with given username.")));
                return false;
            }
            try
            {
                FileWriteClass.CloseActiveSession(client);
                dynamic answer = new
                {
                    id = "session/end",
                    data = new
                    {
                        status = "OK"
                    }
                };
                Sclient.Send(JsonConvert.SerializeObject(answer));
                ServerForm.sessions.Remove(Sclient);
                return true;
            }
            catch (Exception e)
            {
                Send(JsonConvert.SerializeObject(Commands.DoctorTrianingStopError(e.Message)));
                return false;
            }
        }
        #endregion

        //Set power from client
        #region
        public Boolean SetPowerFromClient(dynamic jsonObject)
        {
            if (!IsDoctor)
            {
                NoPermission("doctor/setPower");
                return false;
            }
            else
            {
                ClientInfo client = new ClientInfo((string)jsonObject.Name, (int)jsonObject.Age, (Sex)jsonObject.sex);
                Session Sclient = ServerProgram.GetSessionWithUsername(client);
                if (Sclient == null)
                {
                    Send(JsonConvert.SerializeObject(Commands.SetPowerError("No client active with given username.")));
                    return false;
                }
                else
                {
                    dynamic power = new
                    {
                        id = "client/SetPower",
                        data = new
                        {
                            power = jsonObject.data.power
                        }
                    };
                    Sclient.Send(JsonConvert.SerializeObject(power));
                    return true;
                }
            }
        }
        #endregion

        //Follow and unfollow a patient session
        #region
        public void FolowAPatientSession(ClientInfo client)
        {
            try
            {
                Session clientToListenTo = ServerProgram.GetSessionWithUsername(client);
                if (clientToListenTo != null)
                {
                    clientToListenTo.DoctorsToSendDataTo.Add(this);
                    dynamic answer = new
                    {
                        id = "doctor/FollowPatient",
                        data = new
                        {
                            status = "ok"
                        }
                    };
                    Send(JsonConvert.SerializeObject(answer));
                }
                else
                {
                    Send(JsonConvert.SerializeObject(Commands.FollowPatientError("Patient not active")));
                }
            }
            catch (Exception e)
            {
                Send(JsonConvert.SerializeObject(Commands.FollowPatientError(e.Message)));
            }
        }

        public void UnFollowAPatientSession(ClientInfo client)
        {
            try
            {
                Session clientToListenTo = ServerProgram.GetSessionWithUsername(client);
                if (clientToListenTo != null)
                {
                    if (clientToListenTo.DoctorsToSendDataTo.Contains(this))
                    {
                        clientToListenTo.DoctorsToSendDataTo.Remove(this);
                        dynamic answer = new
                        {
                            id = "doctor/UnfollowPatient",
                            data = new
                            {
                                status = "ok"
                            }
                        };
                        Send(JsonConvert.SerializeObject(answer));
                    }
                }
                else
                {
                    Send(JsonConvert.SerializeObject(Commands.UnFollowPatientError("Patient not found")));
                }
            }
            catch (Exception e)
            {
                Send(JsonConvert.SerializeObject(Commands.FollowPatientError(e.Message)));
            }
        }
        #endregion

        //StartAstrand from patient
        #region
        public void StartAstrandFromPatient(ClientInfo client)
        {
            try
            {

                Session clientToStart = ServerProgram.GetSessionWithUsername(client);
                if (clientToStart != null)
                {
                    dynamic answer = new
                    {
                        id = "StartAstrand",
                        data = new
                        {
                            sessionId = client
                        }
                    };
                    clientToStart.Send(JsonConvert.SerializeObject(answer));
                    dynamic answerToDocter = new
                    {
                        id = "Doctor/StartAstrand",
                        data = new
                        {
                            status = "ok"
                        }
                    };
                    Send(JsonConvert.SerializeObject(answerToDocter));
                    FileWriteClass.AddActiveSession(client);
                }
                else
                {
                    Send(JsonConvert.SerializeObject(Commands.StartAstrandError("Patient not found")));
                }
            }
            catch (Exception e)
            {
                Send(JsonConvert.SerializeObject(Commands.StartAstrandError(e.Message)));
            }
        }
        #endregion

        //NoPermission
        #region
        public void NoPermission(string idAnswer)
        {
            dynamic answer = new
            {
                id = idAnswer,
                status = "Error",
                error = "You do not have permission for this action"
            };
            Send(JsonConvert.SerializeObject(answer));
        }
        #endregion
    }
}

