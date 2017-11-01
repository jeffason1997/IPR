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
        public ClientForm(ClientConnection con)
        {
            InitializeComponent();
            _con = con;
        }

        private void bikeConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FormBikeControl(_con).Show();
            this.Hide();
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _con.close();
            Application.Exit();
        }
    }
}
