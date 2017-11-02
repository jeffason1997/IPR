using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    [Serializable]
    public class OneTraining : List<TrainingItem>
    {
        public double VO2Max { get; set; }
        public double averageHeartBeat { get; set; }
        public ClientInfo client { get; set; }

        public override string ToString()
        {
            return this[0].SessionTime.ToString();
        }
    }

    [Serializable]
    public class TrainingItem
    {
        public TypeOfTraining ThisType { get; set; }
        public int Seconds { get; set; }
        public HealthData Status { get; set; }
        public DateTime SessionTime { get; set; }

        public TrainingItem(TypeOfTraining type, int time, HealthData stats,DateTime sessionTime)
        {
            ThisType = type;
            Seconds = time;
            Status = stats;
            SessionTime = sessionTime;
        }
    }

    public enum TypeOfTraining
    {
        WarmingUp = 1,
        RealTraining = 2,
        CoolingDown = 3,
        ExtendedTraining = 4
    }
}
