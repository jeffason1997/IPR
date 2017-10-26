using System;
using System.Globalization;

namespace Client
{
    public class KettlerStatus : HealthData
    {
        public KettlerStatus(string msg, string guid)
        {
            var data = msg.TrimEnd('\r').Split('\t');
            Heartbeat = byte.Parse(data[0]);
            Rpm = int.Parse(data[1]);
            Speed = int.Parse(data[2]);
            Distance = float.Parse(data[3]);
            RequestedPower = short.Parse(data[4]);
            Energy = float.Parse(data[5]);
            var time = data[6].Split(':');
            Time = int.Parse(time[0]) * 60 + int.Parse(time[1]);
            ActualPower = short.Parse(data[7]);
            SessionId = guid;
        }
    }
}
