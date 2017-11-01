using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client;

namespace IPR
{
    public partial class ServerForm : Form
    {
        public static List<Session> sessions { get; set; }
        static int port = 1234;
        private Thread Server = null;

        public ServerForm()
        {
            InitializeComponent();

        }


        public void updateTextBox(string s)
        {
            MessageBox.Invoke(new Action(() =>
            {
                MessageBox.AppendText(s);
            }));
        }

       

        private void button1_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() => { ServerProgram.startServer(this); });
            thread.Start();
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            Application.Exit();
        }
    }
}

