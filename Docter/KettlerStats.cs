using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client;


namespace Docter
{
    public partial class KettlerStats : UserControl
    {
        public KettlerStats()
        {
            InitializeComponent();
        }

        public void UpdateTextFields(TrainingItem item)
        {
            lblTraining.Text = item.ThisType.ToString();

            dynamic status = item.Status;
            lblBpm.Text = status.Heartbeat.ToString();
            lblRpm.Text = status.Rpm.ToString();
            lblSpeed.Text = status.Speed.ToString();
            lblDistance.Text = status.Distance.ToString();
            lblReqPower.Text = status.RequestedPower.ToString();
            lblEnergy.Text = status.Energy.ToString();
            lblTime.Text = ToTime(status.Time);
            lblActPower.Text = status.ActualPower.ToString();
        }

        private string ToTime(int seconds)
        {
            int minutes = seconds / 60;
            seconds = seconds % 60;

            var strMinutes = minutes < 10 ? "0" + minutes : minutes.ToString();
            var strSeconds = seconds < 10 ? "0" + seconds : seconds.ToString();

            return strMinutes + ":" + strSeconds;
        }

        public double GetValue(string value)
        {
            //Console.WriteLine(value);
            if (value.Equals("Heartbeat"))
            {
                return Convert.ToDouble(lblBpm.Text);
            }
            if (value.Equals("Rpm"))
            {
                return Convert.ToDouble(lblRpm.Text);
            }
            if (value.Equals("Speed"))
            {
                return Convert.ToDouble(lblSpeed.Text);
            }
            if (value.Equals("Distance"))
            {
                return Convert.ToDouble(lblDistance.Text);
            }
            if (value.Equals("Energy"))
            {
                return Convert.ToDouble(lblEnergy.Text);
            }
            else
            {
                //Console.WriteLine("this is not supposed to happen");
                return 0;
            }

        }
    }
}
