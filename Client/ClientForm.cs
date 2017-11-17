using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class ClientForm : Form
    {
        private ClientConnection _con;
        public FormBikeControl bikeControl { get; }
        public ClientInfo CInfo;

        public ClientForm(ClientConnection con)
        {
            InitializeComponent();
            _con = con;
            _con.setBikeControl(bikeControl);
            _con.setClientForm(this);
            _con.getClientInfo();
            kettlerStats1.setForm(this);
            bikeControl = new FormBikeControl(this);
        }

        public ClientConnection getConn()
        {
            return _con;
        }

        public void SetComPort(string com)
        {
            _con.setCom(com);
        }

        private void bikeConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bikeControl.Show();
            this.Hide();
        }

        public void updateKettlerStats(HealthData item)
        {
            kettlerStats1.Invoke(new Action(() =>
            {
                kettlerStats1.UpdateTextFields(item);
            }));
        }

        public void updateTextBox(string message)
        {
            textBox1.Invoke(new Action(() =>
            {
                textBox1.AppendText("- " + message + "\n");
            }));
        }

        public void updateClientInfo(ClientInfo info)
        {
            CInfo = info;
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _con.close();
            Application.Exit();
        }
    }
}
