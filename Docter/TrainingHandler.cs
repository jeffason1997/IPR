using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client;
using Newtonsoft.Json;

namespace Docter
{
    public class TrainingHandler
    {
        public DocterForm Cform;
        public ClientInfo CInfo;
        private HealthData healthData;
        private DocterApplication_Connection _con;
        private List<HealthData> measurements;
        private List<byte> heartBeats;
        private bool firstTime = true;
        private bool MaxHeartbeat, SteadyHeartBeats;
        private int time;
        private bool SteadyState = false;
        private int counter = 0;
        private int endTime = 0;



        public TrainingHandler(DocterForm form, DocterApplication_Connection con)
        {
            Cform = form;
            _con = con;
            measurements = new List<HealthData>();
            heartBeats = new List<byte>();
        }

        public TrainingItem MakeTrainingsItem(HealthData data)
        {
            time = data.Time;
            healthData = data;
            measurements.Add(data);

            TypeOfTraining tempType;
            if (time <= 120)
            {
                if (time == 0)
                {
                    firstTime = true;
                }
                tempType = TypeOfTraining.WarmingUp;
            }
            else if (time > 120 && time <= 360)
            {
                if (time == 121)
                {
                    firstTime = true;
                }
                tempType = TypeOfTraining.RealTraining;
            }
            else if (SteadyState == false)
            {
                if (time % 120 == 1)
                {
                    firstTime = true;
                }
                tempType = TypeOfTraining.ExtendedTraining;
            }
            else if (time == endTime)
            {
                _con.stopTraining(CInfo.UserName);
                sendMessage("The training is over, you can get of the bike now.");
                if (SteadyHeartBeats)
                {
                    if (SteadyState)
                    {
                        StopAstrandSucces();
                        new FormAStrand(CInfo, CalculateVO2(), AverageHeartBeat());
                    }
                    else
                    {
                        ErrorEndAstrand("Een hartslag van 130 is niet behaald. De test is dus niet geldig en zal niet worden opgeslagen.");
                    }
                }
                else
                {
                    ErrorEndAstrand("Uw hartslag is niet geleidelijk verhoorgd. De test is dus niet geldig en zal niet worden opgeslagen.");
                }
               
                return null;
            }
            else
            {
                if (time % 120 == 1)
                {
                    endTime = time + 60;
                    firstTime = true;
                }
                tempType = TypeOfTraining.CoolingDown;
            }



            return TrainingsCommunicator(tempType);
        }

        public TrainingItem TrainingsCommunicator(TypeOfTraining type)
        {
            int i = (int)type;

            switch (i)
            {
                case 1:
                    {
                        return WarmingUpTraining();
                    }

                case 2:
                    {
                        return RealTraining();
                    }

                case 3:
                    {
                        return CoolingDownTraining();
                    }

                case 4:
                    {
                        return ExtendedTraing();
                    }
            }
            return new TrainingItem(TypeOfTraining.CoolingDown, time, healthData, DateTime.Now);
        }

        public TrainingItem WarmingUpTraining()
        {
            return new TrainingItem(TypeOfTraining.WarmingUp, time, healthData, DateTime.Now);
        }

        public TrainingItem RealTraining()
        {
            return new TrainingItem(TypeOfTraining.RealTraining, time, healthData, DateTime.Now);
        }

        public TrainingItem CoolingDownTraining()
        {
            return new TrainingItem(TypeOfTraining.CoolingDown, time, healthData, DateTime.Now);
        }

        public TrainingItem ExtendedTraing()
        {
            return new TrainingItem(TypeOfTraining.ExtendedTraining, time, healthData, DateTime.Now);
        }




        public void handleTraining(TrainingItem item)
        {
            int i = (int)item.ThisType;
            Cform.updateKettlerStats(item);

            /*if (HFAboveMaximum(item.Status.Heartbeat))
            {
                MaxHeartbeat = true;
                _con.stopTraining(CInfo.UserName);
            }*/


            switch (i)
            {
                case 1:
                    {
                        WarmingUpTraining(item);
                        return;
                    }

                case 2:
                    {
                        RealTraining(item);
                        return;
                    }

                case 3:
                    {
                        CoolingDownTraining(item);
                        return;
                    }

                case 4:
                    {
                        ExtendedTraing(item);
                        return;
                    }
            }
        }

        private void WarmingUpTraining(TrainingItem item)
        {
            if (firstTime)
            {
                Cform.updateTextBox("The warming up will start now.");
                Cform.updateTextBox("The wattage of the bike will be set on 50");
                Cform.updateTextBox($"Power has been set to{50}");

                firstTime = false;
            }

            if (item.Status.Time % 15 == 0)
            {
                averageHeartBeat(item.Status.Heartbeat);
            }
        }

        private void RealTraining(TrainingItem item)
        {
            if (firstTime)
            {
                Cform.updateTextBox("The warming up is now finished, the real test starts now.");
                Cform.updateTextBox("Try to get between 50 and 60 rpm");
                
                firstTime = false;
            }

            if (time % 3 == 0 && item.Status.Heartbeat < 130 && item.Status.Rpm >= 50 && item.Status.Rpm <= 60)
            {
                int newPower = item.Status.ActualPower + 5;
                Cform.updateTextBox($"Power has been set to{newPower}");
            }

            if (time <= 180)
            {
                if (time % 60 == 0)
                {
                    averageHeartBeat(item.Status.Heartbeat);
                }
            }
            else
            {
                if (time % 15 == 0)
                {
                    averageHeartBeat(item.Status.Heartbeat);
                }
            }

            if (item.Status.Heartbeat > 130)
            {
                counter++;
                if (counter >= 120)
                {
                    SteadyState = true;
                    sendMessage("Steady state reached.");
                }
            }
            else
            {
                counter = 0;
            }
        }

        private void ExtendedTraing(TrainingItem item)
        {
            if (firstTime)
            {
                Cform.updateTextBox("You didn't make the steady state so you get 2 minutes of extended training");
                Cform.updateTextBox("Try to get between 50 and 60 rpm");
                firstTime = false;
            }

            if (time % 3 == 0 && item.Status.Heartbeat < 130 && item.Status.Rpm >= 50 && item.Status.Rpm <= 60)
            {
                int newPower = item.Status.ActualPower + 5;
                Cform.updateTextBox($"Power has been set to{newPower}");
            }

            if (time % 15 == 0)
            {
                averageHeartBeat(item.Status.Heartbeat);
            }

            if (item.Status.Heartbeat > 130)
            {
                counter++;
                if (counter >= 120)
                {
                    SteadyState = true;
                    sendMessage("Steady state reached.");
                }
            }
            else
            {
                counter = 0;
            }
        }

        private void CoolingDownTraining(TrainingItem item)
        {
            if (firstTime)
            {
                Cform.updateTextBox("You've completed the training, the cooling down will begin now");
                Cform.updateTextBox($"Power has been set to{50}");
                firstTime = false;
            }
        }



        public void StopAstrandSucces()
        {
            dynamic request = new
            {
                id = "StopAstrand",
                data = new
                {
                    patientId = CInfo.UserName,
                    status = "ok",
                    data = new
                    {
                        clientInfo = CInfo,
                        vo2Max = CalculateVO2(),
                        avgPulse = AverageHeartBeat()
                    }
                }
            };
            _con.sendAstrandInfo(request,CInfo.UserName);
        }

        public void ErrorEndAstrand(string error)
        {
            StopAstrandError(error);
            Task.Delay(1000).Wait();
            MessageBox.Show("Er is een error opgetreden:\r\n" + error);
        }


        public void StopAstrandError(string status)
        {
            dynamic request = new
            {
                id = "StopAstrand",
                data = new
                {
                    patientId = CInfo.UserName,
                    status = "error",
                    data = new
                    {
                        status = status
                    }
                }
            };
            _con.sendAstrandInfo(request, CInfo.UserName);
        }

        public double CalculateVO2()
        {
            double maxWatage = 0;
            double VO2max = 0;
            measurements.ForEach(measurement =>
                {
                    if (measurement.ActualPower > maxWatage)
                        maxWatage = measurement.ActualPower;
                }
            );
            maxWatage *= 6.11;
            if (CInfo.sex == Sex.Male)
            {
                VO2max = (((0.00193 * maxWatage) + 0.326) / (0.769 * AverageHeartBeat() - 56.1) * 100) * 1000 / CInfo.Weight;
            }
            else
            {
                VO2max = (((0.00212 * maxWatage) + 0.299) / (0.769 * AverageHeartBeat() - 48.5) * 100) * 1000 / CInfo.Weight;
            }

            if (CInfo.Age >= 30)
                VO2max *= GetCorrectionFactor();
            return VO2max;
        }

        public int AverageHeartBeat()
        {
            int TotalPulse = 0;
            foreach (byte h in heartBeats)
            {
                TotalPulse += h;
            }
            if (heartBeats.Count > 0)
            {
                return TotalPulse / heartBeats.Count;
            }
            return 0;
        }

        private void sendMessage(string Message)
        {
            Cform.updateTextBox(Message);
            _con.sendMessageToClient(Message, CInfo.UserName);
        }

        public Boolean HFAboveMaximum(int pulse)
        {
            int age = CInfo.Age;
            if (age < 15)
            {
                return false;
            }
            int maxPulse = 0;
            if (age >= 15 && age < 25)
            {
                maxPulse = 200;
            }
            if (age >= 25 && age < 35)
            {
                maxPulse = 200;
            }
            else if (age >= 35 && age < 40)
            {
                maxPulse = 190;
            }
            else if (age >= 40 && age < 45)
            {
                maxPulse = 180;
            }
            else if (age >= 45 && age < 50)
            {
                maxPulse = 170;
            }
            else if (age >= 50 && age < 55)
            {
                maxPulse = 160;
            }
            else if (age >= 55 && age < 60)
            {
                maxPulse = 150;
            }
            return pulse > maxPulse;
        }

        public double GetCorrectionFactor()
        {
            int age = CInfo.Age;
            if (age >= 15 && age < 25)
            {
                return 1.10;
            }
            else if (age >= 25 && age < 35)
            {
                return 1.00;
            }
            else if (age >= 35 && age < 40)
            {
                return 0.93;
            }
            else if (age >= 40 && age < 45)
            {
                return 0.83;
            }
            else if (age >= 45 && age < 50)
            {
                return 0.78;
            }
            else if (age >= 50 && age < 55)
            {
                return 0.75;
            }
            else if (age >= 55 && age < 60)
            {
                return 0.71;
            }
            else
            {
                return 0.65;
            }
        }

        public void averageHeartBeat(byte heartBeat)
        {
            if (heartBeats.Any())
            {
                if (Math.Abs(heartBeats[heartBeats.Count - 1] - heartBeat) > 5)
                {
                    SteadyHeartBeats = false;
                    System.Diagnostics.Debug.WriteLine("error bij steadyHF: " + heartBeats[heartBeats.Count - 1] + "   " + heartBeat);
                }
            }
            heartBeats.Add(heartBeat);
        }
    }
}
