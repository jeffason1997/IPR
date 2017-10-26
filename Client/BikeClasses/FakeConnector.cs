using System;
using System.Linq;

namespace Client
{
    public class FakeConnector : IConnector
    {
        private int _power = 30;
        private float _distance;
        private float _energy;
        private int _time;


        public FakeConnector() { }

        public void GetId(Action<string> onDone)
        {
            onDone("Kettler Simulator");
        }

        public void Close()
        {
            
        }

        public void GetStats(Action<string> onDone)
        {
            var r = new Random();
            string[] data =
            {
                r.Next(42, 69).ToString(),
                r.Next(100, 200).ToString(),
                r.Next(171, 252).ToString(),
                _distance.ToString(),
                _power.ToString(),
                _energy.ToString(),
                ToTime(_time),
                _power.ToString()
            };

            _distance += 0.5f;
            _energy += 1.5f;
            _time += 1;
                        

            onDone(data.Aggregate((i1, i2) => i1 + "\t" + i2)+"\r");
        }

        private string ToTime(int seconds)
        {
            int minutes = seconds / 60;
            seconds = seconds % 60;

            var strMinutes = minutes < 10 ? "0" + minutes : minutes.ToString();
            var strSeconds = seconds < 10 ? "0" + seconds : seconds.ToString();

            return strMinutes + ":" + strSeconds;
        }

        public void Open()
        {
            
        }

        public void Reset()
        {
            _power = 30;
            _distance = 0;
            _energy = 0;
            _time = 0;
        }

        public void SetPower(int amount)
        {
            _power = amount;
        }

        public void SetDistance(int distance)
        {
            _distance = distance;
        }

        public void SetTime(int time)
        {
            _time = time;
        }
    }
}

