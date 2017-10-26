using System;
using System.IO.Ports;

namespace Client
{
    public class KettlerConnector : IConnector
    {
        private const string CMD_RESET = "RS";
        private const string CMD_RUN = "CD";
        private const string CMD_GET_ID = "ID";
        private const string CMD_GET_STATUS = "ST ";
        private const string CMD_SET_POWER = "PW ";
        private const string CMD_SET_DISTANCE = "PD ";
        private const string CMD_SET_TIME = "PT ";

        SerialPort _conn;

        public KettlerConnector(string com)
        {
            _conn = new SerialPort(com, 9600, Parity.None);
            _conn.DataReceived += (ob, ev) => { MyAction.Action?.Invoke(_conn?.ReadLine()); };            
        }

        public void Close()
        {
            _conn.Close();
        }

        private void SetAction(Action<string> a)
        {
            MyAction.Action = a;
        }

        public void Open()
        {
            _conn.Open();
        }

        public void Reset()
        {
            SetAction(null);
            _conn.WriteLine(CMD_RESET);
        }

        public void GetId(Action<string> onDone)
        {
            SetAction(onDone);
            _conn.WriteLine(CMD_GET_ID);            
        }

        public void GetStats(Action<string> onDone)
        {
            SetAction(onDone);
            _conn.WriteLine(CMD_GET_STATUS);            
        }

        public void SetPower(int amount)
        {
            SetAction(null);
            _conn.WriteLine(CMD_SET_POWER + amount);
            _conn.WriteLine(CMD_RUN);
        }

        public void SetDistance(int distance)
        {
            SetAction(null);
            _conn.WriteLine(CMD_RUN);
            _conn.WriteLine(CMD_SET_DISTANCE + distance);
        }

        public void SetTime(int time)
        {
            SetAction(null);
            _conn.WriteLine(CMD_SET_TIME + time);
            _conn.WriteLine(CMD_RUN);
        }
    }
}
