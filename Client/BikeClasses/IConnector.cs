using System;

namespace Client
{
    public interface IConnector
    {
        void Open();
        void Reset();
        void Close();
        void GetId(Action<string> onDone);
        void GetStats(Action<string> onDone);
        void SetPower(int amount);
        void SetDistance(int distance);
        void SetTime(int time);
    }
}
