using Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Docter
{
    public partial class FormAStrand : Form
    {
        public FormAStrand(ClientInfo client, double vo2, double avgHeartBeat)
        {
            InitializeComponent();
            label1.Text = client.UserName;
            label4.Text = client.Age.ToString();
            label7.Text = client.Weight.ToString();
            label10.Text = client.sex.ToString();
            label5.Text = avgHeartBeat.ToString();
            label6.Text = vo2.ToString();
        }
    }
}
