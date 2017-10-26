using System;
using System.Globalization;

namespace Client
{
    public class HealthData
    {
        public byte Heartbeat { get; internal set; }
        public int Rpm { get; internal set; }
        public int Speed { get; internal set; }
        public float Distance { get; internal set; }
        public short RequestedPower { get; internal set; }
        public float Energy { get; internal set; }
        public int Time { get; internal set; }
        public short ActualPower { get; internal set; }
        public string SessionId { get; internal set; }

        public HealthData(byte heartbeat, int rpm, int speed, float distance, short requestedPower, float energy, int time, short actualPower, string sessionId)
        {
            Heartbeat = heartbeat;
            Rpm = rpm;
            Speed = speed;
            Distance = distance;
            RequestedPower = requestedPower;
            Energy = energy;
            Time = time;
            ActualPower = actualPower;
            SessionId = sessionId;


        }

        public double getValue(String value)
        {
            Console.WriteLine(value);
            if (value.Equals("Heartbeat"))
            {
                return (double)Heartbeat;
            }
            if (value.Equals("Rpm"))
            {
                return (double)Rpm;
            }
            if (value.Equals("Speed"))
            {
                return (double)Speed;
            }
            if (value.Equals("Distance"))
            {
                return (double)Distance;
            }
            if (value.Equals("Energy"))
            {
                return (double)Energy;
            }
            return 0.0;
        }



        internal HealthData() { }
    }
}
