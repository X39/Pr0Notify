using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace Pr0Notify2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static App instance;


        Pr0Notify2.Pr0API.User User;
        MessageManager messageManager;
        private bool MessageManager_SyncWasRequested;
        private List<MessageManager.Message> PNM_CurrentDisplayedList;
        System.Windows.Forms.NotifyIcon TrayIcon;
        System.ComponentModel.BackgroundWorker bWorker;
        MainWindow Window;

        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            
            instance = new App();
            instance.MessageManager_SyncWasRequested = false;
            instance.InitializeComponent();
            instance.PNM_CurrentDisplayedList = null;


            instance.User = null;
            instance.TrayIcon = new System.Windows.Forms.NotifyIcon();
            instance.TrayIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            ApplyMenuItemsToContextMenu(instance.TrayIcon.ContextMenu);
            instance.Exit += Instance_Exit;
            if(string.IsNullOrWhiteSpace(Pr0Notify2.Properties.Settings.Default.Cookie))
            {
                instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.Message_LoginNotPresentIco;
                instance.TrayIcon.Text = "Nicht Eingelogt";
            }
            else
            {
                instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.NoMessageIco;
                instance.TrayIcon.Text = "Keine ungelesenen Benachrichtigungen";
                var cookie = new System.Net.CookieContainer();
                cookie.SetCookies(new Uri(@"http://pr0gramm.com"), Pr0Notify2.Properties.Settings.Default.Cookie);

                instance.User = new Pr0API.User(Pr0Notify2.Properties.Settings.Default.Username, cookie, Pr0Notify2.Properties.Settings.Default.LastSyncId);
                instance.User.MessageRaised += User_MessageRaised;
                instance.User.InboxCountChanged += User_InboxCountChanged;
                instance.User.setInterval(new TimeSpan(Pr0Notify2.Properties.Settings.Default.SyncInterval / 60, Pr0Notify2.Properties.Settings.Default.SyncInterval % 60, 0));
                instance.User.startSync();
                instance.messageManager = new MessageManager(instance.User);
                instance.messageManager.LoadMessages();
                if (Pr0Notify2.Properties.Settings.Default.PNM_AllowUsage)
                {
                    instance.messageManager.SyncStateChanged += MessageManager_SyncStateChanged;
#if !DEBUG
                    instance.messageManager.SyncMessages();
#endif
                }
                instance.setLogInState(true);
            }
            instance.TrayIcon.Visible = true;
            instance.TrayIcon.DoubleClick += TrayIcon_DoubleClick;
            instance.Run();
        }

        #region TrayIcon ContextMenu EventHandlers
        public void setLogInState(bool flag)
        {
            var cm = this.TrayIcon.ContextMenu;
            foreach(var it in cm.MenuItems)
            {
                if(it is System.Windows.Forms.MenuItem)
                {
                    var mi = (System.Windows.Forms.MenuItem)it;
                    switch(mi.Name)
                    {
                        case "Show_PNManager":
                            {
                                mi.Enabled = flag;
                            }
                            break;
                        case "Config":
                            {
                                mi.Enabled = flag;
                            }
                            break;
                    }
                }
            }
        }
        private static void ApplyMenuItemsToContextMenu(System.Windows.Forms.ContextMenu cm)
        {
            System.Windows.Forms.MenuItem mi;
            {
                mi = new System.Windows.Forms.MenuItem("Konfiguration");
                mi.Name = "Config";
                mi.Enabled = false;
                System.Windows.Forms.MenuItem mi2;
                {
                    mi2 = new System.Windows.Forms.MenuItem("Sync Interval setzen",
                        (object sender, EventArgs e) => { instance.showWindow(); instance.Window.showSyncIntervalTemplate(Pr0Notify2.Properties.Settings.Default.SyncInterval); });
                    mi2.Name = "Config_SetSyncInterval";
                    mi.MenuItems.Add(mi2);
                }
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("PN Manager", TICM_Show_PNManager_onClick);
                mi.Name = "Show_PNManager";
                mi.Enabled = false;
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("-");
                mi.Name = "Split";
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("Beenden",
                    (object sender, EventArgs e) => { Application.Current.Shutdown(); });
                mi.Name = "QuitApplication";
                cm.MenuItems.Add(mi);
            }
#if DEBUG
            {
                mi = new System.Windows.Forms.MenuItem("-");
                mi.Name = "Split";
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("DEBUG-AUserSync",
                    (object sender, EventArgs e) => { if (instance.User != null) instance.User.sync(); });
                mi.Name = "D_AUserSync";
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("DEBUG-AInboxMessages",
                    (object sender, EventArgs e) => { if (instance.messageManager != null) instance.messageManager.SyncMessages(); });
                mi.Name = "D_AInboxMessages";
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("DEBUG-ESaveMessages",
                    (object sender, EventArgs e) => { if (instance.messageManager != null) instance.messageManager.SaveMessages(); });
                mi.Name = "D_ESaveMessages";
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("DEBUG-ELoadMessages",
                    (object sender, EventArgs e) => { if (instance.messageManager != null) instance.messageManager.LoadMessages(); });
                mi.Name = "D_ELoadMessages";
                cm.MenuItems.Add(mi);
            }
            {
                mi = new System.Windows.Forms.MenuItem("DEBUG-WipeSettings",
                    (object sender, EventArgs e) => { Pr0Notify2.Properties.Settings.Default.Reset(); Pr0Notify2.Properties.Settings.Default.Save(); });
                mi.Name = "D_WipeSettings";
                cm.MenuItems.Add(mi);
            }
#endif
        }
        private static void TICM_Show_PNManager_onClick(object sender, EventArgs e)
        {
            if (Pr0Notify2.Properties.Settings.Default.PNM_AllowUsage)
            {
                if (instance.messageManager.SyncState != MessageManager.ESyncState.Synchronized)
                {
                    instance.TrayIcon.ShowBalloonTip(3000, "Synch in progress", "Momentan wird der PNM noch synchronisiert.\nBitte gedulde dich noch ein paar momente :)", System.Windows.Forms.ToolTipIcon.Info);
                    instance.MessageManager_SyncWasRequested = true;
                    instance.messageManager.SyncMessages();
                }
                else
                {
                    instance.showWindow();
                    instance.Window.showPNManager();
                }
            }
            else
            {
                instance.showWindow();
                instance.Window.ConfirmProcessed += Window_PNM_ConfirmProcessed;
                instance.Window.Height = 240;
                instance.Window.showConfirmUi("Damit Pr0Notify funktioniert, müssen die Privaten Nachrichten Lokal auf deiner Festplatte gespeichert werden. " + 
                                                "Die Nachrichten werden dort unverschlüsselt gespeichert und sind somit für jeden einsehbar der den Computer verwendet.\n\n" +
                                                "Bitte bestätige, dass du damit einverstanden bist.\n" +
                                                "Solltest du dich dagegen entscheiden, ist die Nutzung dieses Features nicht möglich.\n" +
                                                "Vor der Bestätigung werden KEINE PNs auf deinem Computer gespeichert!");
            }
        }

        private static void Window_PNM_ConfirmProcessed(object sender, MainWindow.ConfirmProcessedEventArgs e)
        {
            instance.closeWindow();
            if(e.Result)
            {
                Pr0Notify2.Properties.Settings.Default.PNM_AllowUsage = true;
                Pr0Notify2.Properties.Settings.Default.Save();
                if (instance.messageManager.SyncState != MessageManager.ESyncState.Synchronized)
                {
                    instance.TrayIcon.ShowBalloonTip(3000, "Synch in progress", "Momentan wird der PN-Manager synchronisiert.\nBitte gedulde dich noch ein paar momente :)", System.Windows.Forms.ToolTipIcon.Info);
                    instance.MessageManager_SyncWasRequested = true;
                    instance.messageManager.SyncMessages();
                }
                else
                {
                    instance.showWindow();
                    instance.Window.showPNManager();
                }
            }
            else
            {
                instance.TrayIcon.ShowBalloonTip(3000, "Dann halt nicht ...", "Ohne deine zustimmung ist es technisch nicht möglich den PN-Manager zu nutzen.\n Tut mir leid ¯\\_(ツ)_/¯", System.Windows.Forms.ToolTipIcon.Info);
            }
        }
        #endregion
        #region Event Callbacks
        private static void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            if (instance.User == null || !instance.User.IsValid)
            {
                if (instance.bWorker == null)
                {
                    instance.showWindow();
                    instance.Window.showLoginTemplate();
                }
                else
                {
                    instance.TrayIcon.ShowBalloonTip(3000, "Geduld du haben musst", "Der Login läuft noch fagg0t.", System.Windows.Forms.ToolTipIcon.Info);
                }
            }
            else
            {
                System.Diagnostics.Process.Start(@"http://pr0gramm.com/inbox/unread");
                instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.NoMessageIco;
                instance.TrayIcon.Text = "";
            }
        }
        private static void App_UserLogin(object sender, MainWindow.UserLoginEventArgs e)
        {
            instance.closeWindow();
            instance.User = e.User;
            instance.User.MessageRaised += User_MessageRaised;
            instance.User.InboxCountChanged += User_InboxCountChanged;
            instance.bWorker = new System.ComponentModel.BackgroundWorker();
            instance.bWorker.DoWork += BWorker_DoWork;
            instance.bWorker.RunWorkerAsync(e);
        }
        private static void BWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            instance.User.login(((MainWindow.UserLoginEventArgs)e.Argument).Password);
            if (!instance.User.IsValid)
            {
                instance.User = null;
                instance.bWorker = null;
                return;
            }
            Pr0Notify2.Properties.Settings.Default.Username = instance.User.Username;
            Pr0Notify2.Properties.Settings.Default.Cookie = instance.User.Cookie.GetCookieHeader(new Uri(@"http://pr0gramm.com"));
            Pr0Notify2.Properties.Settings.Default.Save();
            instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.NoMessageIco;
            instance.TrayIcon.Text = "Keine ungelesenen Benachrichtigungen";
            instance.User.startSync();
            instance.messageManager = new MessageManager(instance.User);
            instance.messageManager.SyncStateChanged += MessageManager_SyncStateChanged;
            instance.setLogInState(true);
            instance.bWorker = null;
        }
        private static void User_InboxCountChanged(object sender, Pr0API.User.InboxCountChangedEventArgs e)
        {
            if(e.TotalCount > 0)
            {
                instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.NewMessageIco;
                instance.TrayIcon.Text = e.TotalCount > 1 ? e.TotalCount + " neue Benachrichtigungenen" : "1 neue Benachrichtigung";
                instance.TrayIcon.ShowBalloonTip(6000, "Jemand hat an dich gedacht", e.TotalCount > 1 ? e.TotalCount > 2 ? e.TotalCount > 3 ? e.TotalCount > 4 ? e.TotalCount > 5 ? e.TotalCount + " neue Benachrichtigungenen" : "Fünf neue Benachrichtigungen" : "Vier neue Benachrichtigungen" : "Drei neue Benachrichtigungen" : "Zwei neue Benachrichtigungen" : "Eine neue Benachrichtigung", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.NoMessageIco;
                instance.TrayIcon.Text = "Keine ungelesenen Benachrichtigungen";
            }
        }
        private static void User_MessageRaised(object sender, Pr0API.User.MessageRaisedEventArgs e)
        {
            instance.TrayIcon.ShowBalloonTip(3000, e.Title, e.Content, e.Type == Pr0API.User.MessageRaisedType.Info ? System.Windows.Forms.ToolTipIcon.Info : e.Type == Pr0API.User.MessageRaisedType.Error ? System.Windows.Forms.ToolTipIcon.Error : System.Windows.Forms.ToolTipIcon.Warning);
        }
        private static void Window_SyncIntervalConfirmed(object sender, MainWindow.SyncIntervalConfirmedEventArgs e)
        {
            if (Pr0Notify2.Properties.Settings.Default.SyncInterval != e.Value)
            {
                Pr0Notify2.Properties.Settings.Default.SyncInterval = e.Value;
                Pr0Notify2.Properties.Settings.Default.Save();
                if(instance.User != null)
                {
                    instance.User.setInterval(new TimeSpan(e.Value / 60, e.Value % 60, 0));
                }
            }
            instance.closeWindow();
        }
        private void Window_PNM_Initialized(object sender, EventArgs e)
        {
            TextBox tbSearchUser = (TextBox)instance.Window.ViewPort.Template.FindName("tbSearchUser", instance.Window.ViewPort);
            ListBox userBox = (ListBox)instance.Window.ViewPort.Template.FindName("listBox", instance.Window.ViewPort);
            tbSearchUser.TextChanged += (object se, TextChangedEventArgs ea) =>
            {
                userBox.Items.Filter = (object obj) =>
                {
                    if (string.IsNullOrWhiteSpace(tbSearchUser.Text))
                        return true;
                    return ((Tuple<string, List<MessageManager.Message>>)obj).Item1.Contains(tbSearchUser.Text.Trim());
                };
                userBox.Items.Refresh();
            };
            userBox.DisplayMemberPath = "Item1";
            userBox.SelectedValuePath = "Item2";
            userBox.SelectionChanged += listBox_SelectionChanged;
            ListView MessageDisplay = (ListView)instance.Window.ViewPort.Template.FindName("MessageDisplay", instance.Window.ViewPort);
            PNM_RefreshUserlist();

        }
        private void listBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ListBox userBox = (ListBox)sender;
            PNM_DisplayConversation((List<MessageManager.Message>)userBox.SelectedValue);
        }
        private static void MessageManager_SyncStateChanged(object sender, MessageManager.SyncStateChangedEventArgs e)
        {
            if (instance.Window != null)
            {
                if (instance.Window.ViewPort.Template == (ControlTemplate)instance.Window.FindResource("GridPrivateMessages"))
                {
                    instance.PNM_RefreshUserlist();
                }
            }
            else if (instance.MessageManager_SyncWasRequested && e.StateNew == MessageManager.ESyncState.Synchronized)
            {
                instance.TrayIcon.ShowBalloonTip(3000, "Synch fertig", "Wir sind nun 100% Synchron, Sir!", System.Windows.Forms.ToolTipIcon.Info);
                instance.MessageManager_SyncWasRequested = false;
            }
        }
        #endregion

        private void PNM_RefreshUserlist()
        {
            ListBox userBox = (ListBox)this.Window.ViewPort.Template.FindName("listBox", this.Window.ViewPort);
            foreach (var it in this.messageManager.Contacts.OrderByDescending((it) => it.Value.Item2.Last().Created))
            {
                userBox.Items.Add(it.Value);
            }
        }

        private void PNM_DisplayConversation(List<MessageManager.Message> msgList)
        {
            ListView MessageDisplay = (ListView)this.Window.ViewPort.Template.FindName("MessageDisplay", this.Window.ViewPort);
            if (msgList == null)
            {
                MessageDisplay.Items.Clear();
                return;
            }
            ScrollViewer MessageDisplayScroll = (ScrollViewer)this.Window.ViewPort.Template.FindName("MessageDisplayScroll", this.Window.ViewPort);
            PNM_CurrentDisplayedList = msgList;
            MessageDisplay.Items.Clear();
            var tmpList = msgList;
            if(tmpList.Count > 15)
            {
                tmpList = tmpList.GetRange(msgList.Count - 15, 15);
            }
            foreach (var it in tmpList)
            {
                var label = new Label();
                label.Content = it.Text;
                label.Style = it.SenderName == this.User.Username ? (Style)this.Window.FindResource("MessageStyle_Sender") : (Style)this.Window.FindResource("MessageStyle_Receiver");
                label.ApplyTemplate();
                
                MessageDisplay.Items.Add(label);
            }
            MessageDisplayScroll.ScrollToEnd();
            if(MessageDisplayScroll.ScrollableHeight == 0)
            {
                int curCount = MessageDisplay.Items.Count;
                while (curCount != msgList.Count && MessageDisplayScroll.ScrollableHeight == 0)
                {
                    PNM_PollMoreMessages();
                    curCount = MessageDisplay.Items.Count;
                    MessageDisplayScroll.UpdateLayout();
                }
            }
        }
        private void Window_PNM_PollMoreMessages(object sender, EventArgs e)
        {
            PNM_PollMoreMessages();
        }
        private void PNM_PollMoreMessages()
        {
            if (PNM_CurrentDisplayedList == null)
                return;
            ListView MessageDisplay = (ListView)this.Window.ViewPort.Template.FindName("MessageDisplay", this.Window.ViewPort);
            ScrollViewer MessageDisplayScroll = (ScrollViewer)this.Window.ViewPort.Template.FindName("MessageDisplayScroll", this.Window.ViewPort);
            var tmpList = PNM_CurrentDisplayedList;
            int restItems = tmpList.Count - MessageDisplay.Items.Count;
            if (restItems == 0)
                return;
            if(restItems > 15)
                tmpList = tmpList.GetRange(restItems - 15, 15);
            else
                tmpList = tmpList.GetRange(0, restItems);
            tmpList.Reverse();
            foreach (var it in tmpList)
            {
                var label = new Label();
                label.Content = it.Text;
                label.Style = it.SenderName == this.User.Username ? (Style)this.Window.FindResource("MessageStyle_Sender") : (Style)this.Window.FindResource("MessageStyle_Receiver");
                label.ApplyTemplate();

                MessageDisplay.Items.Insert(0, label);
            }
        }

        private void showWindow()
        {
            if (instance.Window != null)
                return;
            instance.Window = new Pr0Notify2.MainWindow();
            instance.Window.Show();
            instance.Window.SyncIntervalConfirmed += Window_SyncIntervalConfirmed;
            instance.Window.UserLogin += App_UserLogin;
            instance.Window.Closed += closeWindow;
            instance.Window.PNM_Initialized += Window_PNM_Initialized;
            instance.Window.PNM_PollMoreMessages += Window_PNM_PollMoreMessages; ;
        }


        private void closeWindow(object sender = null, EventArgs e = null)
        {
            if (instance.Window == null)
                return;
            if (sender == null)
                instance.Window.Close();
            instance.Window = null;
        }

        private static void Instance_Exit(object sender, ExitEventArgs e)
        {
            instance.TrayIcon.Visible = false;
            instance.TrayIcon.Dispose();
            if (instance.User == null || !instance.User.IsValid)
                return;
            if (instance.User.LastSyncId != Pr0Notify2.Properties.Settings.Default.LastSyncId)
            {
                Pr0Notify2.Properties.Settings.Default.LastSyncId = instance.User.LastSyncId;
                Pr0Notify2.Properties.Settings.Default.Save();
            }
        }
    }
}
