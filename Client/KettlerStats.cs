using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client;


namespace Client
{
    public partial class KettlerStats : UserControl
    {
        private TypeOfTraining type;
        private int counter = 0;
        public ClientForm CForm;
        private bool SteadyState = false;
        private bool justWent = true;

        public KettlerStats()
        {
            InitializeComponent();
        }

        public void setForm(ClientForm form)
        {
            CForm = form;
        }

        public void Messagesing(int time)
        {
            if (time == 0 && justWent)
            {
                sendMessage("First you will have 2 minutes of Warming up.");
                sendMessage("Then you will have 4 minutes of Real Training.");
                sendMessage("If you didn't reacht steady state you wil have 2 minutes of Extended Training until you reacht it.");
                sendMessage("And finnaly you will have 1 minute Cooldown.");
                sendMessage("The warming up will start now.");
                sendMessage("The wattage of the bike will be set on 50");
                updatePower(50);
                justWent = false;
            }
            else if (time == 121 && justWent)
            {
                sendMessage("The warming up is now finished, the real test starts now.");
                sendMessage("Try to get between 50 and 60 rpm");
                justWent = false;
            }
            else if (time >= 361 && type == TypeOfTraining.ExtendedTraining && time % 120 == 1 && justWent)
            {
                sendMessage("You didn't make the steady state so you get 2 minutes of extended training");
                sendMessage("Try to get between 50 and 60 rpm");
                justWent = false;
            }
            else if (time >= 361 && type == TypeOfTraining.CoolingDown && time % 120 == 1 && justWent)
            {
                sendMessage("You've completed the training, the cooling down will begin now");
                sendMessage("The wattage of the bike will be set on 50");
                updatePower(50);
                justWent = false;
            }
            else if (!justWent&&time>5)
            {
                justWent = true;
            }

            if (justWent && SteadyState)
            {
                sendMessage("Steady state reached.");
                justWent = false;
            }
        }

        public void checkstuff(HealthData item)
        {
            if (item.Heartbeat > 130)
            {
                counter++;
                if (counter >= 120 && !SteadyState)
                {
                    SteadyState = true;
                    justWent = true;
                    sendMessage("Steady state reached.");
                }
            }
            else
            {
                counter = 0;
            }

            if (type == TypeOfTraining.ExtendedTraining || type == TypeOfTraining.RealTraining)
            {

                if (item.Time % 3 == 0 && item.Heartbeat < 130 && justWent)
                {
                    if (item.Rpm >= 50 && item.Rpm <= 6)
                    {
                        int newPower = item.ActualPower + 5;
                        updatePower(newPower);
                        justWent = false;
                    }
                    else if (item.Rpm < 50)
                    {
                        sendMessage("Go Faster Please");
                    }
                    else if (item.Rpm > 60)
                    {
                        sendMessage("Slow Down Please");
                    }
                }
            }

        }

        private void sendMessage(string message)
        {
            CForm.updateTextBox(message);
        }

        private void updatePower(int power)
        {
            CForm.getConn().setPower(power);
            //sendMessage($"Power has been set to {power}");
        }

        public TypeOfTraining getType(int time, short actualpower)
        {

            if (time <= 120)
            {
                return TypeOfTraining.WarmingUp;
            }
            else if (time > 120 && time <= 360)
            {

                return TypeOfTraining.RealTraining;
            }
            else if (actualpower > 50)
            {

                return TypeOfTraining.ExtendedTraining;
            }
            else
            {
                return TypeOfTraining.CoolingDown;
            }
        }

        public void UpdateTextFields(HealthData status)
        {
            type = getType(status.Time, status.ActualPower);
            lblTraining.Text = type.ToString();
            lblBpm.Text = status.Heartbeat.ToString();
            lblRpm.Text = status.Rpm.ToString();
            lblSpeed.Text = status.Speed.ToString();
            lblDistance.Text = status.Distance.ToString();
            lblReqPower.Text = status.RequestedPower.ToString();
            lblEnergy.Text = status.Energy.ToString();
            lblTime.Text = ToTime(status.Time);
            lblActPower.Text = status.ActualPower.ToString();

            checkstuff(status);
            Messagesing(status.Time);
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
