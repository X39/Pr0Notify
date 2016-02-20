using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using asapJson;
using System.Net;

namespace Pr0Notify2.Pr0API
{
    public class User
    {

        public string Username { get; internal set; }
        public CookieContainer Cookie { get; internal set; }
        public bool IsValid { get; internal set; }
        private System.Windows.Threading.DispatcherTimer Timer { get; set; }
        private int lastSyncId;
        private int lastInboxCount;

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
        private void raise_Message(string title, string content, MessageRaisedType type = MessageRaisedType.Info)
        {
            var eh = this.MessageRaised;
            if (eh == null)
                return;
            eh(this, new MessageRaisedEventArgs(title, content, type));
        }
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
        private void raise_InboxCountChanged(int totalCount, int change)
        {
            var eh = this.InboxCountChanged;
            if (eh == null)
                return;
            eh(this, new InboxCountChangedEventArgs(totalCount, change));
        }
        #endregion
        #endregion

        public User(string username, string password, int lastSyncId = 0)
        {
            this.Username = username;
            this.IsValid = false;
            this.Timer = new System.Windows.Threading.DispatcherTimer();
            this.Timer.Interval = new TimeSpan(0, 1, 0);
            this.Timer.Tick += Timer_Tick;
            this.lastSyncId = lastSyncId;
            this.lastInboxCount = 0;
            this.login(password);
        }
        public User(string username, CookieContainer cookie, int lastSyncId = 0)
        {
            this.Username = username;
            this.Cookie = cookie;
            this.IsValid = true;
            this.Timer = new System.Windows.Threading.DispatcherTimer();
            this.Timer.Interval = new TimeSpan(0, 1, 0);
            this.Timer.Tick += Timer_Tick;
            this.lastSyncId = lastSyncId;
            this.lastInboxCount = 0;
            this.raise_Message("Cookie geladen", "Der Cookie wurde erfolgreich konsumiert!");
        }

        private void login(string password)
        {
            try
            {
                this.Cookie = new CookieContainer();
                StringBuilder postDataBuilder = new StringBuilder();
                postDataBuilder.Append("name=" + System.Security.SecurityElement.Escape(this.Username));
                postDataBuilder.Append("&password=" + System.Security.SecurityElement.Escape(this.Username));
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

                JsonNode responseNode = new JsonNode(new System.IO.StreamReader(response.GetResponseStream()));
                response.Close();
                if (responseNode.Type == JsonNode.TypeEnum.Object)
                {
                    Dictionary<string, JsonNode> dict;
                    responseNode.getValue(out dict);
                    if (dict != null)
                    {
                        var node = dict["success"];
                        if (node.Type == JsonNode.TypeEnum.Boolean)
                        {
                            bool flag;
                            node.getValue(out flag);
                            if (flag)
                            {
                                //Login Successful
                                this.raise_Message("Login Erfolgreich", "Login war erfolgreich :)", MessageRaisedType.Info);
                                this.IsValid = true;
                            }
                            else
                            {
                                //Login Failed
                                node = dict["ban"];
                                if (node.Type == JsonNode.TypeEnum.Object)
                                {
                                    Dictionary<string, JsonNode> dict2;
                                    node.getValue(out dict2);
                                    this.raise_Message(
                                        dict2 == null ?
                                            "Login Fehlgeschlagen" :
                                            "Login Fehlgeschlagen ¯\\_(ツ)_/¯",
                                        dict2 == null ?
                                            "Der Login war leider nicht erfolgreich ...\nIst das Password/der Username korrekt?" :
                                            "Der Login war nicht erfolgreich\nDu fagg0t bist gebannt",
                                        MessageRaisedType.Warning);
                                }
                                else
                                {
                                    this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER05", MessageRaisedType.Error);
                                }
                            }
                        }
                        else
                        {
                            this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER04", MessageRaisedType.Error);
                        }
                    }
                    else
                    {
                        this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER03", MessageRaisedType.Error);
                    }
                }
                else
                {
                    this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER02", MessageRaisedType.Error);
                }
            }
            catch (Exception ex)
            {
                this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER01\n" + ex.Message, MessageRaisedType.Error);
            }
        }
        private void sync()
        {
            if (!this.IsValid)
                throw new Exception("non-valid User, only valid users can operate sync.");
            try
            {
                WebRequest request = WebRequest.Create(@"http://pr0gramm.com/api/user/sync?lastId=" + this.lastSyncId);
                request.Method = "GET";
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
                ((HttpWebRequest)request).CookieContainer = this.Cookie;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();



                JsonNode responseNode = new JsonNode(new System.IO.StreamReader(response.GetResponseStream()));
                response.Close();
                if (responseNode.Type == JsonNode.TypeEnum.Object)
                {
                    Dictionary<string, JsonNode> dict;
                    responseNode.getValue(out dict);
                    if (dict != null)
                    {
                        int inboxCount = 0;
                        int lastId = 0;
                        bool flag = true;
                        var node = dict["inboxCount"];
                        if (node.Type == JsonNode.TypeEnum.Number)
                        {
                            double tmp;
                            node.getValue(out tmp);
                            inboxCount = (int)tmp;
                        }
                        else
                        {
                            flag = false;
                            this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER10", MessageRaisedType.Error);
                        }
                        node = dict["lastId"];
                        if (node.Type == JsonNode.TypeEnum.Number)
                        {
                            double tmp;
                            node.getValue(out tmp);
                            lastId = (int)tmp;
                        }
                        else
                        {
                            flag = false;
                            this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER09", MessageRaisedType.Error);
                        }
                        if (flag)
                        {
                            if (lastId > 0)
                                this.lastSyncId = lastId;
                            if (inboxCount > 0)
                            {
                                if (this.lastInboxCount != inboxCount)
                                {
                                    this.raise_Message("Du hast " + inboxCount + " neue " + (inboxCount > 1 ? "Benachrichtigungen" : "Benachrichtigung"), "Neue " + (inboxCount > 1 ? "Benachrichtigungen" : "Benachrichtigung"), MessageRaisedType.Info);
                                    raise_InboxCountChanged(inboxCount, inboxCount - this.lastInboxCount);
                                    this.lastInboxCount = inboxCount;
                                }
                            }
                        }
                    }
                    else
                    {
                        this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER08", MessageRaisedType.Error);
                    }
                }
                else
                {
                    this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRUSER07", MessageRaisedType.Error);
                }
            }
            catch (Exception ex)
            {
                this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nSync fehlgeschlagen :(\nError Code: ERRUSER06\n" + ex.Message, MessageRaisedType.Error);
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
            throw new NotImplementedException();
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
