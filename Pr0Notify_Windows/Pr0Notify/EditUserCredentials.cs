using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pr0Notify
{
    public partial class EditUserCredentials : Form
    {
        public EditUserCredentials()
        {
            InitializeComponent();
            this.tb_username.Text = Pr0Notify.Properties.Settings.Default.Username;
            this.tb_password.Text = Pr0Notify.Properties.Settings.Default.Password;
        }
        private void btn_OK_Click(object sender, EventArgs e)
        {
            Pr0Notify.Properties.Settings.Default.Username = this.tb_username.Text;
            Pr0Notify.Properties.Settings.Default.Password = this.tb_password.Text;
            Pr0Notify.Properties.Settings.Default.Save();
            DialogResult = DialogResult.OK;
        }
    }
}
