using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using asapJson;
using System.Net;
using System.ComponentModel;

namespace Pr0Notify2.Pr0API
{
    public class User
    {

        public string Username { get; internal set; }
        public string FullUserId { get; internal set; }
        public string _nonce { get { return this.FullUserId.Substring(0, 16); } }
        public CookieContainer Cookie { get; internal set; }
        public bool IsValid { get; internal set; }
        private System.Windows.Threading.DispatcherTimer Timer { get; set; }
        public long LastSyncId { get; internal set; }
        private int lastInboxCount;
        System.ComponentModel.BackgroundWorker backWorker;

        #region Events
        #region MessageRaised
        public class MessageRaisedEventArgs : EventArgs
        {
            public string Title { get; internal set; }
            public string Content { get; internal set; }
            public MessageRaisedType Type { get; internal set; }
            public MessageRaisedEventArgs(string title, string content, MessageRaisedType type = MessageRaisedType.Info)
            {
                this.Title = title;
                this.Content = content;
                this.Type = type;
            }
        }
        public enum MessageRaisedType
        {
            Info,
            Warning,
            Error
        }
        public event EventHandler<MessageRaisedEventArgs> MessageRaised;
        #endregion
        #region InboxCountChangedEvent
        public class InboxCountChangedEventArgs : EventArgs
        {
            public int TotalCount { get; internal set; }
            public int Change { get; internal set; }
            public InboxCountChangedEventArgs(int totalCount, int change)
            {
                this.TotalCount = totalCount;
                this.Change = change;
            }
        }
        public event EventHandler<InboxCountChangedEventArgs> InboxCountChanged;
        #endregion
        #endregion

        public User(string username)
        {
            this.Username = username;
            this.IsValid = false;
            this.Timer = new System.Windows.Threading.DispatcherTimer();
            this.Timer.Interval = new TimeSpan(0, 1, 0);
            this.Timer.Tick += Timer_Tick;
            this.LastSyncId = 0;
            this.lastInboxCount = 0;
            backWorker = null;
        }
        public User(string username, CookieContainer cookie, long lastSyncId = 0)
        {
            this.Username = username;
            this.Cookie = cookie;
            this.decodeCookie();
            this.IsValid = true;
            this.Timer = new System.Windows.Threading.DispatcherTimer();
            this.Timer.Interval = new TimeSpan(0, 1, 0);
            this.Timer.Tick += Timer_Tick;
            this.LastSyncId = lastSyncId;
            this.lastInboxCount = 0;
        }
        private void decodeCookie()
        {
            string cookieString = this.Cookie.GetCookieHeader(new Uri(@"http://pr0gramm.com"));
            cookieString = cookieString.Substring(cookieString.IndexOf("me=") + 3);
            if(cookieString.Contains(';'))
                cookieString = cookieString.Substring(0, cookieString.IndexOf(';') + 1);
            bool flag = false;
            string tmp = "";
            string output = "";
            foreach (var c in cookieString)
            {
                if (flag)
                {
                    if (tmp.Length == 2)
                    {
                        flag = false;
                        output += ((char)Convert.ToInt32(tmp, 16));
                        tmp = "";
                        if (c == '%')
                        {
                            flag = true;
                        }
                        else
                        {
                            output += (c);
                        }
                    }
                    else
                    {
                        tmp += (c);
                    }
                }
                else
                {
                    if (c == '%')
                    {
                        flag = true;
                    }
                    else
                    {
                        output += (c);
                    }
                }
            }
            JsonNode node = new JsonNode(output, true);
            this.Username = node.getValue_Object()["n"].getValue_String();
            this.FullUserId = node.getValue_Object()["id"].getValue_String();
        }

        public bool login(string password)
        {
            try
            {
                this.Cookie = new CookieContainer();
                StringBuilder postDataBuilder = new StringBuilder();
                postDataBuilder.Append("name=" + System.Web.HttpUtility.UrlEncode(this.Username));
                postDataBuilder.Append("&password=" + System.Web.HttpUtility.UrlEncode(password));
                byte[] postData = Encoding.ASCII.GetBytes(postDataBuilder.ToString());


                WebRequest request = WebRequest.Create(@"http://pr0gramm.com/api/user/login");
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
                ((HttpWebRequest)request).CookieContainer = this.Cookie;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postData.Length;
                request.GetRequestStream().Write(postData, 0, postData.Length);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                JsonNode responseNode = new JsonNode(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd(), true);
                response.Close();

                Dictionary<string, JsonNode> dict;
                if (!responseNode.getValue(out dict))
                    throw new Exception("Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER07");

                var node = dict["success"];
                bool flag;
                if (!node.getValue(out flag))
                    throw new Exception("Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER06");
                if (flag)
                {
                    //Login Successful
                    this.IsValid = true;
                    this.decodeCookie();
                    return true;
                }
                else
                {
                    //Login Failed
                    node = dict["ban"];
                    if(!node.getValue(out dict))
                        throw new Exception("Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER05");
                    throw new Exception(dict == null ?
                            "Der Login war leider nicht erfolgreich ...\nIst das Password/der Username korrekt?" :
                            "Der Login war nicht erfolgreich\nDu fagg0t bist gebannt");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER04\n" + ex.Message);
            }
        }
        private void resetInbox()
        {
            try
            {
                WebRequest request = WebRequest.Create(@"http://pr0gramm.com/api/inbox/unread");
                request.Method = "GET";
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
                ((HttpWebRequest)request).CookieContainer = this.Cookie;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();
            }
            catch(Exception ex)
            {
                throw new Exception("Reset failed: " + ex.Message);
            }
        }

        #region Syncing Stuff
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (backWorker != null)
                return;
            backWorker = new BackgroundWorker();
            backWorker.DoWork += BackWorker_doSync;
            backWorker.RunWorkerCompleted += BackWorker_RunWorkerCompleted;
            backWorker.RunWorkerAsync();
        }
        private void BackWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            backWorker = null;
            if (e.Result != null)
            {
                if (e.Result is MessageRaisedEventArgs)
                {
                    var eh = this.MessageRaised;
                    if(eh != null)
                        eh(this, (MessageRaisedEventArgs)e.Result);
                }
                else if (e.Result is InboxCountChangedEventArgs)
                {
                    var eh = this.InboxCountChanged;
                    if (eh != null)
                        eh(this, (InboxCountChangedEventArgs)e.Result);
                }
            }
        }
        private void BackWorker_doSync(object sender, DoWorkEventArgs e)
        {
            if (!this.IsValid)
            {
                e.Result = new MessageRaisedEventArgs("Whooops", "Nicht-Valider user! Nur Valide User können einen SYNC durchführen\nSync fehlgeschlagen :(", MessageRaisedType.Error);
                return;
            }
            e.Result = null;
            try
            {
                WebRequest request = WebRequest.Create(@"http://pr0gramm.com/api/user/sync?lastId=" + this.LastSyncId);
                request.Method = "GET";
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
                ((HttpWebRequest)request).CookieContainer = this.Cookie;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                JsonNode responseNode = new JsonNode(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd(), true);
                response.Close();


                double tmp;
                Dictionary<string, JsonNode> dict;
                if (!responseNode.getValue(out dict))
                    throw new Exception("ERRUSER03");


                int inboxCount = 0;
                var node = dict["inboxCount"];
                if (!node.getValue(out tmp))
                    throw new Exception("ERRUSER02");
                inboxCount = (int)tmp;


                int lastId = 0;
                node = dict["lastId"];
                if (!node.getValue(out tmp))
                    throw new Exception("ERRUSER01");
                lastId = (int)tmp;


                if (lastId > 0)
                    this.LastSyncId = lastId;
                if (inboxCount > 0)
                {
                    if (this.lastInboxCount != inboxCount)
                    {
                        e.Result = new InboxCountChangedEventArgs(inboxCount, inboxCount - this.lastInboxCount);
                        this.lastInboxCount = inboxCount;
                    }
                }
            }
            catch (Exception ex)
            {
                e.Result = new MessageRaisedEventArgs("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nSync fehlgeschlagen :(\n\n" + ex.Message, MessageRaisedType.Error);
            }
        }


        public void startSync()
        {
            this.Timer.Start();
        }
        public void stopSync()
        {
            this.Timer.Stop();
        }
        public void setInterval(TimeSpan span)
        {
            this.Timer.Interval = span;
        }
        #endregion
    }
}
