using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client;
using IPR;


namespace Docter
{
    public partial class DocterForm : Form
    {
        private Session UsingClient;
        public ClientList UsingClientList { get; set; }
        private IConnector _conn;
        private FormBikeControl BikeControl = new FormBikeControl(null);
        private string currentSessionID;
        private bool _keepSessionGoing;
        private DocterApplication_Connection connection;
        




        public DocterForm()
        {
            InitializeComponent();
            UsingClientList = new ClientList();

            //makeTestCode();
  
        }

        private void button1_Click(object sender, EventArgs e)
        {
            connection.getClientInfo(comboBox1.SelectedItem.ToString());

            TrainingListBox.Invoke(new Action(() =>
            {
                TrainingListBox.Items.Clear();

                /* if (UsingClient.Trainings.Count != 0)
                 {
                     foreach (OneTraining training in UsingClient.Trainings)
                     {
                         TrainingListBox.Items.Add(training);
                     }

                 }*/
            }));
        }

        public void updateClientInfo(ClientInfo client)
        {
            ClientInfoBox.Invoke(new Action(() =>
            {
                ClientInfoBox.Clear();
                ClientInfoBox.AppendText($"Client name: {client.UserName}\n");
                ClientInfoBox.AppendText($"Client Age: {client.Age}\n");
                ClientInfoBox.AppendText($"Client Age: {client.sex.ToString()}\n");
                //ClientInfoBox.AppendText($"Number of trainings: {UsingClient.Trainings.Count}\n");
            }));
        }


        public void makeTestCode()
        {
            UsingClientList.Add(new ClientInfo("Jeffrey", 20, Sex.Male));
            //UsingClientList[0].AddTraining(new OneTraining());
            //UsingClientList[0].AddTraining(new OneTraining());
            //UsingClientList[0].AddTraining(new OneTraining());
            UsingClientList.Add(new ClientInfo("Piet", 15, Sex.Male));
            UsingClientList.Add(new ClientInfo("Henk", 24, Sex.Female));
        }

        private void bikeSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BikeControl.Show();
        }

        private void StartSession()
        {
            currentSessionID = Guid.NewGuid().ToString();
            _keepSessionGoing = true;
            Console.WriteLine(currentSessionID);
            Task.Run(() =>
            {
                _conn.Open();
                Thread.Sleep(1000);
                _conn.Reset();
                Thread.Sleep(1000);
                /* _conn.GetId((msg) =>
                     Invoke(new Action(() =>
                     {
                         lblConnection.Text = msg;
                     })));*/
                StartStatusThread();
            });
        }

        private void StartStatusThread()
        {
            //new MakeTraining(UsingClient.client);
            Task.Run(() =>
            {
                while (_keepSessionGoing)
                {
                    Thread.Sleep(1000);
                    _conn.GetStats(msg =>
                    {
                        var status = new KettlerStatus(msg, currentSessionID);
                    });
                }
                Console.WriteLine("Session is done");
            });
        }

        public void UpdateComboBox(List<String> new_Connected_Sessions)
        {
            comboBox1.Invoke(new Action(() =>
            {
                comboBox1.Items.Clear();
                foreach (string c in new_Connected_Sessions)
                {
                    if (c != null)
                    {
                        comboBox1.Items.Add(c);
                    }
                }
            }));
            



        }

        private void StartButton_Click(object sender, EventArgs e)
        {

            string com = BikeControl.GetCom();
            if (com == "SIM")
                _conn = new FakeConnector();
            else
                _conn = new KettlerConnector(com);

            StartSession();

        }

        private void DocterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            connection.close();
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            connection.getSessions();
        }
    }
}
