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
        private ClientForm CForm;

        public FormBikeControl(ClientForm form)
        {
            InitializeComponent();
            CForm = form;
            comPortComboBox.Items.Add("SIM");
            string[] coms = SerialPort.GetPortNames();
            foreach (string port in coms)
            {
                comPortComboBox.Items.Add(port);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CForm.SetComPort(_com);
            //Console.WriteLine("The com port is:" + _com);
            if (CForm == null)
            {

            } else
            {
                CForm.Show();
            }
            
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

        private void comPortComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _com = comPortComboBox.SelectedItem.ToString();
        }
    }
}

