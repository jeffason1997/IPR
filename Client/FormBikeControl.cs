using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class FormBikeControl : Form
    {
        private string _com = "SIM";
        public FormBikeControl()
        {
            InitializeComponent();
            comboBox1.Items.Add("SIM");
            string[] coms = SerialPort.GetPortNames();
            foreach (string port in coms)
            {
                comboBox1.Items.Add(port);
            }
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            _com = comboBox1.SelectedItem.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("The com port is:" + _com);
            new ClientForm().Show();
            this.Hide();
        }

        public string GetCom()
        {
            return _com;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            e.Cancel = true;
            this.Hide();
        }
    }
}

