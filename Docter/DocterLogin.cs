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
    public partial class DocterLogin : Form
    {
        public DocterLogin()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {

            var username = txtUsername.Text;
            var password = txtPassword.Text;
            DocterApplication_Connection con = new DocterApplication_Connection(username, password);
            this.Hide();

        }
    }
}
