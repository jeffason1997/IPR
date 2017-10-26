using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace Client
{
    public class MakeTraining
    {
        private Timer _sessionTimer;
        private int counter = 0;
        private ClientInfo UsingClient;
        private OneTraining _tempOneTraining;
        private bool NonSteadyState;

        public MakeTraining(ClientInfo client)
        {
            UsingClient = client;
            _tempOneTraining = new OneTraining();
        }

        public TrainingItem MakeTrainingsItem()
        {
            
            counter++;
            TypeOfTraining tempType;
            if (counter <= 120)
            {
                tempType = TypeOfTraining.WarmingUp;
            }
            else if (counter > 120 && counter <= 360)
            {
                tempType = TypeOfTraining.RealTraining;
            }
            else if (NonSteadyState == false)
            {
                tempType = TypeOfTraining.CoolingDown;
            }
            else
            {
                tempType = TypeOfTraining.ExtendedTraining;
            }



            return TrainingsCommunicator(tempType);


            //_tempOneTraining.TrainingsItems.Add(tempTrainingItem);
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
            return new TrainingItem(TypeOfTraining.CoolingDown, 1,null,DateTime.Now);
        }

        public TrainingItem WarmingUpTraining()
        {
            return new TrainingItem(TypeOfTraining.CoolingDown, 1, null, DateTime.Now);
        }

        public TrainingItem RealTraining()
        {
            return new TrainingItem(TypeOfTraining.CoolingDown, 1, null, DateTime.Now);
        }

        public TrainingItem CoolingDownTraining()
        {
            return new TrainingItem(TypeOfTraining.CoolingDown, 1, null, DateTime.Now);
        }

        public TrainingItem ExtendedTraing()
        {
            return new TrainingItem(TypeOfTraining.ExtendedTraining, 1, null, DateTime.Now);
        }

    }
}
