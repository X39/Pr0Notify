using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.IO;
using asapJson;

namespace Pr0Notify2
{
    public class ExecutableUpdateProvider
    {
        private BackgroundWorker worker;


        public class ResultAvailableEventArgs : EventArgs
        {
            public bool HasUpdate { get; internal set; }
            public bool WasSuccess { get; internal set; }
            public string Content { get; internal set; }
            public ResultAvailableEventArgs(bool HasUpdate, bool WasSuccess, string Content)
            {
                this.HasUpdate = HasUpdate;
                this.WasSuccess = WasSuccess;
                this.Content = Content;
            }
        }
        public event EventHandler<ResultAvailableEventArgs> ResultAvailable;
        public ExecutableUpdateProvider()
        {
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var eh = this.ResultAvailable;
            if (eh != null)
                eh(this, (ResultAvailableEventArgs)e.Result);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                WebRequest request = WebRequest.Create(@"http://x39.io/api.php?action=projects&project=pr0notify&get[]=version&get[]=download");
                request.Method = "GET";
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var node = new JsonNode(new StreamReader(response.GetResponseStream()).ReadToEnd(), true);
                if(node.getValue_Object()["success"].getValue_Boolean())
                {
                    if (App.ProductVersion != node.getValue_Object()["content"].getValue_Object()["version"].getValue_String())
                    {
                        e.Result = new ResultAvailableEventArgs(true, true, node.getValue_Object()["content"].getValue_Object()["download"].getValue_Array().First().getValue_Object()["link"].getValue_String());
                    }
                    else
                    {
                        e.Result = new ResultAvailableEventArgs(false, true, "");
                    }
                }
            }
            catch (Exception ex)
            {
                e.Result = new ResultAvailableEventArgs(false, false, ex.Message);
            }
        }

        public void searchForUpdate()
        {
            if (worker.IsBusy)
                return;
            worker.RunWorkerAsync();
        }
    }
}
