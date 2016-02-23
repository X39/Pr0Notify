using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pr0Notify2.Pr0API;
using asapJson;
using System.Net;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace Pr0Notify2
{
    public class MessageManager
    {
        public class Message : IXmlSerializable
        {
            public bool Sent { get; internal set; }
            public long ID { get; internal set; }
            public string SenderName { get; internal set; }
            public long SenderId { get; internal set; }
            public string RecipientName { get; internal set; }
            public long RecipientId { get; internal set; }
            public DateTime Created { get; internal set; }
            public string Text { get; internal set; }
            
            public Message(User sender, string recipent, string message)
            {
                //ToDo: add informations needed for sending
                this.Sent = false;
                this.SenderName = sender.Username;
                this.RecipientName = recipent;
                this.Text = message;
            }
            public Message(asapJson.JsonNode node)
            {
                if (node.Type != asapJson.JsonNode.EJType.Object)
                    throw new ArgumentException("JSON Node not of type EJType.Object");
                Dictionary<string, asapJson.JsonNode> dict;
                node.getValue(out dict);
                if(dict == null)
                    throw new ArgumentException("JSON Node has no value");
                this.Sent = true;

                this.ID = (long)dict["id"].getValue_Number();
                this.SenderName = dict["senderName"].getValue_String();
                this.SenderId = (long)dict["senderId"].getValue_Number();
                this.RecipientName = dict["recipientName"].getValue_String();
                this.RecipientId = (long)dict["recipientId"].getValue_Number();
                this.Created = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dict["created"].getValue_Number());
                this.Text = dict["message"].getValue_String();
            }
            private Message() { }

            XmlSchema IXmlSerializable.GetSchema()
            {
                return null;
            }
            void IXmlSerializable.ReadXml(XmlReader reader)
            {
                this.Sent = true;
                do
                {
                    reader.Read();
                    switch (reader.Name)
                    {
                        case "ID":
                            reader.Read();
                            this.ID = long.Parse(reader.Value);
                            break;
                        case "SenderId":
                            reader.Read();
                            this.SenderId = long.Parse(reader.Value);
                            break;
                        case "RecipientId":
                            reader.Read();
                            this.RecipientId = long.Parse(reader.Value);
                            break;
                        case "RecipientName":
                            reader.Read();
                            this.RecipientName = reader.Value;
                            break;
                        case "SenderName":
                            reader.Read();
                            this.SenderName = reader.Value;
                            break;
                        case "Text":
                            reader.Read();
                            this.Text = reader.Value;
                            break;
                        case "Created":
                            reader.Read();
                            this.Created = DateTime.Parse(reader.Value);
                            break;
                    }
                    reader.Read();
                } while (reader.Name != "Message" && reader.Name != "");
            }
            void IXmlSerializable.WriteXml(XmlWriter writer)
            {
                //public bool Sent { get; internal set; }
                //public long ID { get; internal set; }
                //public string SenderName { get; internal set; }
                //public long SenderId { get; internal set; }
                //public string RecipientName { get; internal set; }
                //public long RecipientId { get; internal set; }
                //public DateTime Created { get; internal set; }
                //public string Text { get; internal set; }
                writer.WriteStartElement("ID");
                writer.WriteString(this.ID.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("SenderName");
                writer.WriteString(this.SenderName);
                writer.WriteEndElement();
                writer.WriteStartElement("SenderId");
                writer.WriteString(this.SenderId.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("RecipientName");
                writer.WriteString(this.RecipientName);
                writer.WriteEndElement();
                writer.WriteStartElement("RecipientId");
                writer.WriteString(this.RecipientId.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("Created");
                writer.WriteString(this.Created.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("Text");
                writer.WriteString(this.Text);
                writer.WriteEndElement();
            }
            public override string ToString()
            {
                return this.RecipientName;
            }
        }
        public enum ESyncState
        {
            NonSynced,
            Synchronizing,
            Synchronized
        }
        private ESyncState _getSyncState;
        public ESyncState SyncState
        {
            get { return _getSyncState; }
            internal set
            {
                _getSyncState = value;
                var eh = SyncStateChanged;
                if (eh != null)
                    eh(this, new SyncStateChangedEventArgs(_getSyncState, value));
            }
        }
        private User user;
        public EventedList<Message> Messages { get; internal set; }
        public Dictionary<long, Tuple<string, List<Message>>> Contacts { get; internal set; }
        private BackgroundWorker worker;
        public bool HasUnsavedChanges { get; internal set; }

        #region Events
        #region SyncState Changed
        public class SyncStateChangedEventArgs : EventArgs
        {
            public ESyncState StateNew { get; internal set; }
            public ESyncState StateOld { get; internal set; }
            public SyncStateChangedEventArgs(ESyncState stateOld, ESyncState stateNew)
            {
                this.StateNew = stateNew;
                this.StateOld = stateOld;
            }
        }
        public event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        #endregion
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
        #region NewPrivateMessage
        public class NewPrivateMessageEventArgs : EventArgs
        {
            public Message Msg { get; internal set; }
            public NewPrivateMessageEventArgs(Message msg)
            {
                this.Msg = msg;
            }
        }
        public event EventHandler<NewPrivateMessageEventArgs> NewPrivateMessage;
        #endregion
        #endregion

        public MessageManager(User user)
        {
            this._getSyncState = ESyncState.NonSynced;
            this.user = user;
            this.Messages = new EventedList<Message>();
            this.HasUnsavedChanges = false;
            this.Messages.ItemAdded += Messages_ItemAdded;
            this.Contacts = new Dictionary<long, Tuple<string, List<Message>>>();
        }

        private void Messages_ItemAdded(object sender, EventedList<Message>.ItemEventArgs e)
        {
            Tuple<string, List<Message>> tupel;
            if (e.Value.SenderName == this.user.Username)
            {
                if (!this.Contacts.TryGetValue(e.Value.RecipientId, out tupel))
                {
                    tupel = new Tuple<string, List<Message>>(e.Value.RecipientName, new List<Message>());
                    this.Contacts[e.Value.RecipientId] = tupel;
                }
            }
            else
            {
                if (!this.Contacts.TryGetValue(e.Value.SenderId, out tupel))
                {
                    tupel = new Tuple<string, List<Message>>(e.Value.SenderName, new List<Message>());
                    this.Contacts[e.Value.SenderId] = tupel;
                }
            }
            tupel.Item2.Add(e.Value);
            HasUnsavedChanges = true;
        }

        public void LoadMessages()
        {
            var x = new XmlSerializer(typeof(List<Message>));
            var reader = new System.IO.StreamReader("messages.xml");
            this.Messages.AddRange((List<Message>)x.Deserialize(reader));
            this.SyncState = ESyncState.Synchronized;
            this.HasUnsavedChanges = false;
        }
        public void SaveMessages()
        {
            var x = new XmlSerializer(typeof(List<Message>));
            var writer = new System.IO.StreamWriter("messages.xml");
            x.Serialize(writer, this.Messages);
            writer.Close();
            HasUnsavedChanges = false;
        }
        public void SyncMessages()
        {
            if (this.SyncState == ESyncState.Synchronizing)
                return;
            this.SyncState = ESyncState.Synchronizing;
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork_Sync;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.SyncState = ESyncState.Synchronized;
        }

        private void Worker_DoWork_Sync(object sender, DoWorkEventArgs e)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime curTime = origin;

            DateTime latest = origin;
            if(this.Messages.Count > 0)
            {
                latest = this.Messages.Last().Created;
            }
            var newMessages = new List<Message>();
            try
            {
                while (true)
                {
                    TimeSpan ts = curTime - origin;
                    WebRequest request = WebRequest.Create(@"http://pr0gramm.com/api/inbox/messages?before=" + Math.Floor(ts.TotalSeconds));
                    request.Method = "GET";
                    request.Credentials = CredentialCache.DefaultCredentials;
                    ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";
                    ((HttpWebRequest)request).CookieContainer = this.user.Cookie;

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();



                    JsonNode responseNode = new JsonNode(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd(), true);
                    response.Close();
                    if (responseNode.Type == JsonNode.EJType.Object)
                    {
                        Dictionary<string, JsonNode> dict;
                        responseNode.getValue(out dict);
                        if (dict != null)
                        {
                            var node = dict["messages"];
                            if (node.Type == JsonNode.EJType.Array)
                            {
                                var arr = node.getValue_Array();
                                if (arr.Count == 0)
                                    break;
                                bool flag = false;
                                List<Message> messageList = new List<Message>();
                                foreach (var it in arr)
                                {
                                    var msg = new Message(it);
                                    if (msg.Created >= latest)
                                        messageList.Add(msg);
                                    else
                                        flag = true;
                                }
                                messageList.Reverse();
                                newMessages.InsertRange(0, messageList);
                                if(flag)
                                {
                                    break;
                                }
                                curTime = messageList.First().Created;
                            }
                            else
                            {
                                this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRMESSAGEMANAGER04", MessageRaisedType.Error);
                            }
                        }
                        else
                        {
                            this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRMESSAGEMANAGER03", MessageRaisedType.Error);
                        }
                    }
                    else
                    {
                        this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nLogin nicht erfolgreich\nError Code: ERRMESSAGEMANAGER02", MessageRaisedType.Error);
                    }
                }
                this.Messages.AddRange(newMessages);
            }
            catch (Exception ex)
            {
                this.raise_Message("Whooops", "Schaut so aus als wäre irgend etwas schiefgelaufen ...\nSync fehlgeschlagen :(\nError Code: ERRMESSAGEMANAGER01\n" + ex.Message, MessageRaisedType.Error);
            }
        }

        public void SendMessage(Message msg)
        {
            if (msg.Sent)
                throw new Exception("Message already sent");
            //ToDo: Implement message sending
            throw new NotImplementedException();
        }

    }
}
