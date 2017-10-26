﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client;
using Newtonsoft.Json;

namespace IPR
{
    class FileWriteClass
    {
        static Dictionary<string, string> CredentialsDoctor = new Dictionary<string, string>();
        static Dictionary<string, string> CredentialsClient = new Dictionary<string, string>();
        static Dictionary<ClientInfo, List<OneTraining>> AllTrainings = new Dictionary<ClientInfo, List<OneTraining>>();
        static Dictionary<ClientInfo, OneTraining> LiveTraining = new Dictionary<ClientInfo, OneTraining>();

        public static void GetSavedData()
        {
            try
            {
                string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, @"IPR\IPR\Server\Database.txt");
                if (File.ReadAllText(path).Count() > 0)
                {
                    string AllText = File.ReadAllText(path);
                    List<string> lines = AllText.Split('&').ToList();
                    Dictionary<ClientInfo, List<OneTraining>> dictionary = new Dictionary<ClientInfo, List<OneTraining>>();
                    foreach (string s in lines)
                    {
                        dynamic jsonObject = JsonConvert.DeserializeObject(s);
                        if (jsonObject != null)
                        {
                            // This makes the client
                            ClientInfo client = new ClientInfo((string) jsonObject.Name, (int) jsonObject.Age,
                                (Sex) jsonObject.sex);

                            // This makes the trainings list
                            List<OneTraining> sessions = new List<OneTraining>();
                            foreach (dynamic training in jsonObject.data.Trainingsession)
                            {
                                OneTraining oneTrainingList = new OneTraining();
                                foreach (dynamic trainingsItem in training.data)
                                {
                                    HealthData tempData = new HealthData((byte) trainingsItem.Status.Heartbeat,
                                        (int) trainingsItem.Status.RPM,
                                        (int) trainingsItem.Status.Speed, (float) trainingsItem.Status.Distance,
                                        (short) trainingsItem.Status.RequestedPower,
                                        (float) trainingsItem.Status.Energy, (int) trainingsItem.Status.Time,
                                        (short) trainingsItem.Status.ActualPower,
                                        (string) trainingsItem.Status.SessionId);

                                    TrainingItem tempItem = new TrainingItem((TypeOfTraining) trainingsItem.ThisType,
                                        (int) trainingsItem.Seconds,
                                        tempData, trainingsItem.SessionTime);

                                    oneTrainingList.Add(tempItem);
                                }
                                sessions.Add(oneTrainingList);
                            }
                            dictionary.Add(client, sessions);
                        }
                    }
                    AllTrainings = dictionary;
                }
                
                
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error while reading:(\r\n" + e.Message);
            }
        }

        public static void GetSavedCredentials()
        {
            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, @"IPR\IPR\Server\LoginClients.txt");
            string AllText = File.ReadAllText(path);
            dynamic jsonObject = JsonConvert.DeserializeObject(AllText);
            foreach (dynamic combination in jsonObject.combinations)
            {
                CredentialsClient.Add((string)combination.username, (string)combination.password);
            }

            string path2 = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, @"IPR\IPR\Server\LoginDoctor.txt");
            string AllText2 = File.ReadAllText(path2);
            dynamic jsonObject2 = JsonConvert.DeserializeObject(AllText2);
            foreach (dynamic combination2 in jsonObject2.combinations)
            {
                CredentialsDoctor.Add((string)combination2.username, (string)combination2.password);
            }
        }

        public static Boolean CheckCredentials(string username, string password)
        {
            if (CredentialsClient.ContainsKey(username))
            {
                return CredentialsClient[username] == password;
            }
            return false;
        }

        public static Boolean CheckDoctorCredentials(string username, string password)
        {
            if (CredentialsDoctor.ContainsKey(username))
            {
                return CredentialsDoctor[username] == password;
            }
            return false;
        }

        public static void Close()
        {
            try
            {
                string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, @"IPR\IPR\Server\Database.txt");
                string toWrite = "";
                foreach (KeyValuePair<ClientInfo, List<OneTraining>> entry in AllTrainings)
                {

                    dynamic keyValuePair = new
                    {
                        username = entry.Key,
                        data = new
                        {
                            Trainingsession = entry.Value
                        }
                    };
                    toWrite += JsonConvert.SerializeObject(keyValuePair) + "&";
                }
                File.WriteAllText(path, toWrite);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error while saving:(\r\n" + e.Message);
            }
        }

        public static Boolean AddActiveSession(ClientInfo client)
        {
            if (!LiveTraining.ContainsKey(client))
            {
                LiveTraining.Add(client, new OneTraining());
                return true;
            }
            return false;
        }

        public static Boolean AddDataToSession(ClientInfo client, TrainingItem data)
        {
            if (LiveTraining.ContainsKey(client))
            {
                ((OneTraining)LiveTraining[client]).Add(data);
                return true;
            }
            return false;
        }

        public static void CloseActiveSession(ClientInfo client)
        {
            OneTraining session = LiveTraining[client];
            LiveTraining.Remove(client);
            if (AllTrainings.ContainsKey(client))
            {
                ((List<OneTraining>)AllTrainings[client]).Add(session);
            }
            else
            {
                List<OneTraining> trainList = new List<OneTraining>();
                trainList.Add(session);
                AllTrainings.Add(client, trainList);
            }
        }
    }
}
