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
using System.Windows.Forms.DataVisualization.Charting;
using Client;
using IPR;


namespace Docter
{
    public partial class DocterForm : Form
    {
        private DocterApplication_Connection connection;
        public string user;
        private List<Tab> tabs = new List<Tab>();





        public DocterForm(DocterApplication_Connection con)
        {
            InitializeComponent();
            connection = con;
            connection.setDForm(this);

            makeGraph("Heartbeat");
            makeGraph("Rpm");
            makeGraph("Speed");
            makeGraph("Distance");
            makeGraph("Energy");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            user = comboBox1.SelectedItem.ToString();
            connection.getClientInfo(user);

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
                ClientInfoBox.AppendText($"Client weight: {client.Weight}\n");
            }));
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

        public void updateTextBox(string message)
        {
            textBox1.Invoke(new Action(() =>
            {
                textBox1.AppendText("- "+message+"\n");
            }));
        }

        public void updateKettlerStats(TrainingItem item)
        {
            kettlerStats1.Invoke(new Action(() =>
            {
                kettlerStats1.UpdateTextFields(item);
            }));
       

            foreach (Tab tab in tabs)
            {
                Chart chart = tab.chart;
                chart.Invoke(new Action(() =>
                {
                    string name = chart.Series[0].Name;
                    chart.Series[name].Points.AddY((double)item.Status.getValue(name));

                }));
            }

        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            connection.FollowPatient(user);
            connection.startTraining(user);

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

        private void makeGraph(String type)
        {
            Tab tab = new Tab(type, tabControl1);
            tabs.Add(tab);
        }
    }

    public class Tab
    {
        public Chart chart { get; }

        public Tab(string type, TabControl control)
        {
            string title = $"{type}";
            TabPage myTabPage = new TabPage(title);

            chart = new Chart();
            Series series = new Series(type);
            series.ChartType = SeriesChartType.Line;
            chart.Series.Add(series);
            ChartArea chartArea = new ChartArea();
            Axis yAxis = new Axis(chartArea, AxisName.Y);
            Axis xAxis = new Axis(chartArea, AxisName.X);
            xAxis.IsMarginVisible = false;
            chart.ChartAreas.Add(chartArea);


            chart.Dock = DockStyle.Fill;
            myTabPage.Controls.Add(chart);
            control.TabPages.Add(myTabPage);
        }
    }
}
