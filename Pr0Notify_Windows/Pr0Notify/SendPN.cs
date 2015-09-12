using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Web;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Web;


namespace Pr0Notify
{
    public partial class SendPN : Form
    {
        public SendPN()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tb_sendTime.Text.Equals("now", StringComparison.CurrentCultureIgnoreCase))
            {
                long sendTime;
                try
                {
                    sendTime = Convert.ToInt64(tb_sendTime);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not convert sendTime to number, did not sent the PN\n" + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += worker_DoWork;
                worker.RunWorkerAsync(sendTime);
            }
            else
            {
                sendPN(tb_recipent.Text, tb_message.Text);
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string message = tb_message.Text;
            string recipent = tb_recipent.Text;
            var sendTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); sendTime.AddSeconds((long)e.Argument);
            while(sendTime > DateTime.Now)
                Thread.Sleep(100);
            sendPN(recipent, message);
        }
        
        static async void sendPN(string recipent, string message)
        {
            var recipentID = getRecipentID(recipent);
            if (recipentID == -1)
            {
                Program.NotifyIcon.ShowBalloonTip(5 * 1000, "Whhoooops : /", "Die PN konnte nicht verschickt werden :(\nDer User '" + recipent + "' konnte nicht gefunden werden", ToolTipIcon.Error);
                return;
            }
            await Program.Client.Inbox.SendMessage(recipentID, message);
        }
        static int getRecipentID(string recipent)
        {
            WebRequest request = WebRequest.Create(@"http://pr0gramm.com/api/profile/info?name=" + recipent);
            request.Method = "GET";
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
            ((HttpWebRequest)request).CookieContainer = Program.Client.GetCookies();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var recipentIdIndex = responseString.IndexOf("{\"user\":");
            if (recipentIdIndex == -1)
                return -1;
            recipentIdIndex = responseString.IndexOf("\"id\":", recipentIdIndex);
            if (recipentIdIndex == -1)
                return -1;
            var subResponse = responseString.Substring(recipentIdIndex);
            try
            {
                return Convert.ToInt32(subResponse.Substring(0, subResponse.IndexOf(',')));
            }
            catch(Exception ex)
            {
                return -1;
            }
        }

    }
}
