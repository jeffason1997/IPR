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
        private ClientConnection _con;

        public FormBikeControl(ClientConnection con)
        {
            InitializeComponent();
            _con = con;
            comPortComboBox.Items.Add("SIM");
            string[] coms = SerialPort.GetPortNames();
            foreach (string port in coms)
            {
                comPortComboBox.Items.Add(port);
            }
            
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            _com = comPortComboBox.SelectedItem.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Console.WriteLine("The com port is:" + _com);
            new ClientForm(_con).Show();
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

        private void ageTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }
    }
}

