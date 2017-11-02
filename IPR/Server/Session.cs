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
        private readonly NetworkStream _stream;
        public Boolean IsDoctor;
        public List<Session> DoctorsToSendDataTo;
        public string Username;

        public Session(TcpClient client)
        {
            _stream = client.GetStream();
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
            _stream.Write(buffer, 0, buffer.Length);
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
            //Console.WriteLine($"regel 98 antwoord: {answer}");
            dynamic jsonObject = JsonConvert.DeserializeObject(answer);
            try
            {
                if (jsonObject.id == "log in")
                {
                    CheckCredentials((string)jsonObject.data.username, (string)jsonObject.data.password);
                }
                else if (jsonObject.id == "session/start")
                {
                    NoPermission("session/start");
                }
                else if (jsonObject.id == "data")
                {
                    //Console.WriteLine("Data recieved");
                    DataRecieved(jsonObject);
                }
                else if (jsonObject.id == "start")
                {

                }
                else if (jsonObject.id == "session/write")
                {
                    string user = jsonObject.name;
                    foreach(var t in jsonObject.data.items)
                    {
                        HealthData tempData = new HealthData((byte)t.Status.Heartbeat,
                                        (int)t.Status.RPM,
                                        (int)t.Status.Speed, (float)t.Status.Distance,
                                        (short)t.Status.RequestedPower,
                                        (float)t.Status.Energy, (int)t.Status.Time,
                                        (short)t.Status.ActualPower,
                                        (string)t.Status.SessionId);

                        TrainingItem tempItem = new TrainingItem((TypeOfTraining)t.ThisType,
                            (int)t.Seconds,
                            tempData, t.SessionTime);


                        Boolean added = FileWriteClass.AddDataToSession(user, tempItem);
                        while (!added)
                        {
                            
                        }
                    }

                    
                }
                else if (jsonObject.id == "pauze")
                {

                }
                else if (jsonObject.id == "session/getClientInfo")
                {
                    getClientInfo((string)jsonObject.data.username);
                }
                else if (jsonObject.id == "doctor/message/toClient")
                {
                    SendMessage(jsonObject.data);
                }
                else if (jsonObject.id == "session/end")
                {
                    NoPermission("session/end");
                }
                else if (jsonObject.id == "StopAstrand")
                {
                    string username = (string)jsonObject.data.username;
                    if (CloseSession(username))
                    {
                        FileWriteClass.Close();
                    }
                }
                else if (jsonObject.id == "doctor/login")
                {
                    CheckDoctorCredentials((string)jsonObject.data.username, (string)jsonObject.data.password);
                }
                else if (jsonObject.id == "doctor/sessions")
                {
                    SessionList();
                }
                else if (jsonObject.id == "doctor/training/start")
                {
                    string username = (string)jsonObject.data.patientId;
                    if (CreateNewSession(username, true))
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
                    string username = (string)jsonObject.data.username;
                    if (CloseSession(username))
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
                        string username = (string)jsonObject.data.username;
                        FolowAPatientSession(username);
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
                        string username = (string)jsonObject.data.username;
                        UnFollowAPatientSession(username);
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
                        string username = (string)jsonObject.data.username;
                        CreateNewSession(username, true);
                    }
                    else
                    {
                        NoPermission("doctor/StartAstrand");
                    }
                }
                else if (jsonObject.id == "StopAstrand")
                {
                    if (jsonObject.data.status == "ok")
                    {
                        string patient = (string)jsonObject.data.patientId;

                        double vo2 = (double)jsonObject.data.data.vo2Max;
                        double avgPulse = (double)jsonObject.data.data.avgPulse;

                        string user = (string)jsonObject.data.data.clientInfo.Username;
                        int age = (int)jsonObject.data.data.clientInfo.age;
                        Sex sex = jsonObject.data.data.clientInfo.sex;
                        int weight = (int)jsonObject.data.data.clientInfo.weight;
                        ClientInfo tempInfo = new ClientInfo(user, age, sex, weight);
                        if (CloseSession(tempInfo, vo2, avgPulse))
                        {
                            FileWriteClass.Close();
                        }
                    }
                    else if (jsonObject.data.status == "error")
                    {
                        foreach (Session doctor in DoctorsToSendDataTo)
                        {
                            doctor.Send(JsonConvert.SerializeObject(jsonObject));
                        }
                        Session s = ServerProgram.GetSessionWithUsername((string)jsonObject.data.patientId);
                        if (s != null)
                        {
                            ServerProgram.ErrorWithSession(s);
                        }
                    }
                    else
                    {
                        Session s = ServerProgram.GetSessionWithUsername((string)jsonObject.data.patientId);
                        if (s != null)
                        {
                            ServerProgram.ErrorWithSession(s);
                        }
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

        public Boolean CloseSession(ClientInfo info, double vo2, double avgPulse)
        {
            Session client = ServerProgram.GetSessionWithUsername(info.UserName);
            if (client == null)
            {
                Send(JsonConvert.SerializeObject(Commands.DoctorTrianingStopError("No client active with given username.")));
                return false;
            }
            try
            {

                FileWriteClass.CloseActiveSession(info.UserName, vo2, avgPulse, info);
                dynamic answer = new
                {
                    id = "session/end",
                    data = new
                    {
                        status = "OK"
                    }
                };
                client.Send(JsonConvert.SerializeObject(answer));
                ServerProgram.sessions.Remove(client);
                return true;
            }
            catch (Exception e)
            {
                Send(JsonConvert.SerializeObject(Commands.DoctorTrianingStopError(e.Message)));
                return false;
            }
        }

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

        //get ClientInfo
        #region
        public void getClientInfo(string username)
        {
            ClientInfo client = FileWriteClass.GetClientInfo(username);
            dynamic response = new
            {
                id = "doctor/Client",
                data = new
                {
                    username = client.UserName,
                    age = client.Age,
                    sex = client.sex.ToString(),
                    weight = client.Weight
                }
            };
            //Console.WriteLine("Session 308 " + JsonConvert.SerializeObject(response));
            Send(JsonConvert.SerializeObject(response));
        }
        #endregion

        //Return list with active sessions to doctor
        #region
        public void SessionList()
        {
            if (!IsDoctor)
            {
                //Console.WriteLine("No permission");
                NoPermission("doctor/sessions");
            }
            else
            {
                List<Session> sessions = ServerProgram.GetAllPatients();
                List<string> sessionNames = new List<string>();
                foreach (Session s in sessions)
                {
                    sessionNames.Add(s.Username);
                }
                dynamic response = new
                {
                    id = "doctor/sessions",
                    data = new
                    {
                        sessions = sessionNames.ToArray()
                    }
                };
                //Console.WriteLine(JsonConvert.SerializeObject(response));
                Send(JsonConvert.SerializeObject(response));
            }
        }
        #endregion

        //Create new session.
        #region
        public Boolean CreateNewSession(string user, Boolean responseStartSession)
        {
            if (!IsDoctor)
            {
                NoPermission("doctor/training/start");
                return false;
            }
            else
            {
                Session ThisClient = ServerProgram.GetSessionWithUsername(user);
                if (ThisClient == null)
                {
                    Send(JsonConvert.SerializeObject(Commands.DoctorTrainingStartError("No client active with given username.")));
                    return false;
                }
                try
                {

                    if (FileWriteClass.AddActiveSession(user))
                    {
                        dynamic answer = new
                        {
                            id = "session/start",
                            data = new
                            {
                                status = "OK",
                                sessionID = user
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

            //Console.WriteLine(jsonObject);
            try
            {
                string session = (string)jsonObject.session;

                HealthData tempData = new HealthData((byte)jsonObject.data.status.Heartbeat, (int)jsonObject.data.status.Rpm,
                    (int)jsonObject.data.status.Speed, (float)jsonObject.data.status.Distance,
                    (short)jsonObject.data.status.RequestedPower, (float)jsonObject.data.status.Energy, (int)jsonObject.data.status.Time,
                    (short)jsonObject.data.status.ActualPower, (string)jsonObject.data.status.SessionId);

                dynamic answerToDoctor = new
                {
                    id = "data",
                    sessionId = session,
                    data = new
                    {
                        data = tempData
                    }
                };
                foreach (Session s in DoctorsToSendDataTo)
                {
                    s.Send(JsonConvert.SerializeObject(answerToDoctor));
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
        public Boolean CloseSession(string username)
        {
            Session Sclient = ServerProgram.GetSessionWithUsername(username);
            if (Sclient == null)
            {
                Send(JsonConvert.SerializeObject(Commands.DoctorTrianingStopError("No client active with given username.")));
                return false;
            }
            try
            {
                FileWriteClass.CloseActiveSession(username, 0.0, 0.0, null);
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
        public bool SetPowerFromClient(dynamic jsonObject)
        {
            if (!IsDoctor)
            {
                NoPermission("doctor/setPower");
                return false;
            }
            else
            {
                string username = (string)jsonObject.data.patientID;
                Session Sclient = ServerProgram.GetSessionWithUsername(username);
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

        public void SendMessage(dynamic jsonObject)
        {
            if (!IsDoctor)
            {
                NoPermission("doctor/setPower");
            }
            else
            {
                string username = (string)jsonObject.patientiD;
                Session Sclient = ServerProgram.GetSessionWithUsername(username);
                if (Sclient == null)
                {
                    Send(JsonConvert.SerializeObject(Commands.SetPowerError("No client active with given username.")));
                }
                else
                {
                    dynamic answer = new
                    {
                        id = "client/message",
                        data = new
                        {
                            message = jsonObject.messageId
                        }
                    };
                    Sclient.Send(JsonConvert.SerializeObject(answer));
                }
            }
        }

        //Follow and unfollow a patient session
        #region
        public void FolowAPatientSession(string username)
        {
            try
            {
                Session clientToListenTo = ServerProgram.GetSessionWithUsername(username);
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

        public void UnFollowAPatientSession(string username)
        {
            try
            {
                Session clientToListenTo = ServerProgram.GetSessionWithUsername(username);
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
        public void StartAstrandFromPatient(string username)
        {
            try
            {

                Session clientToStart = ServerProgram.GetSessionWithUsername(username);
                if (clientToStart != null)
                {
                    dynamic answer = new
                    {
                        id = "StartAstrand",
                        data = new
                        {
                            sessionId = username
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
                    FileWriteClass.AddActiveSession(username);
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

