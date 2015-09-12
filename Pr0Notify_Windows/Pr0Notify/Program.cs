using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Net;
using System.Text;
using System.Web;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenPr0gramm;


namespace Pr0Notify
{
    static class Program
    {
        const int bloonTimeout = 3 * 1000;
        static externIcon form_externIcon;
        static NotifyIcon notifyIcon;
        static ContextMenu contextMenu;
        static MenuItem mi_exitApplication;
        static MenuItem mi_setCheckRate;
        static MenuItem mi_openSettings;
        static MenuItem mi_refresh;
        static MenuItem mi_autoRefresh;
        static MenuItem mi_enableMessageboxNotifications;
        static MenuItem mi_enableBloonTippNotification;
        static MenuItem mi_Special;
        static long inboxCount = 0;
        static BackgroundWorker autoRefresh;

        static Pr0grammClient client;

        public static Pr0grammClient Client { get { return client; } }
        public static NotifyIcon NotifyIcon { get { return notifyIcon; } }


        [STAThread]
        static void Main()
        {
            client = null;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            autoRefresh = new BackgroundWorker();
            autoRefresh.DoWork += autoRefresh_DoWork;
            
            notifyIcon = new NotifyIcon();
            contextMenu = new ContextMenu();

            mi_exitApplication = new MenuItem();
            mi_exitApplication.Text = "Beenden";
            mi_exitApplication.Click += new EventHandler(mi_exitApplication_Click);

            mi_setCheckRate = new MenuItem();
            mi_setCheckRate.Text = "Update rate";
            mi_setCheckRate.MenuItems.Add(new MenuItem("1m", mi_setCheckRate_Click));
            mi_setCheckRate.MenuItems.Add(new MenuItem("5m", mi_setCheckRate_Click));
            mi_setCheckRate.MenuItems.Add(new MenuItem("10m", mi_setCheckRate_Click));
            mi_setCheckRate.MenuItems.Add(new MenuItem("15m", mi_setCheckRate_Click));
            mi_setCheckRate.MenuItems.Add(new MenuItem("30m", mi_setCheckRate_Click));
            mi_setCheckRate.Enabled = false;
            mi_setCheckRate.MenuItems[Pr0Notify.Properties.Settings.Default.RefreshRate_MenuItemIndex].Checked = true;

            mi_autoRefresh = new MenuItem("Automatisches Neu Laden", mi_autoRefresh_Click);
            mi_autoRefresh.Enabled = false;

            mi_enableMessageboxNotifications = new MenuItem("MessageBox Benachrichtigungen", mi_openSettings_enableMessageboxNotifications_Click);
            mi_enableMessageboxNotifications.Checked = Pr0Notify.Properties.Settings.Default.messageBoxNotification;

            mi_enableBloonTippNotification = new MenuItem("Bloon Benachrichtigungen", mi_openSettings_enableBloonTippNotification_Click);
            mi_enableBloonTippNotification.Checked = Pr0Notify.Properties.Settings.Default.bloonTippNotification;

            mi_openSettings = new MenuItem();
            mi_openSettings.Text = "Einstellungen";
            mi_openSettings.MenuItems.Add(new MenuItem("Logindaten", mi_openSettings_userCredentials_Click));
            mi_openSettings.MenuItems.Add(new MenuItem("Update Suchen", mi_openSettings_checkForUpdates_Click));
            mi_openSettings.MenuItems.Add(mi_enableMessageboxNotifications);
            mi_openSettings.MenuItems.Add(mi_enableBloonTippNotification);
            mi_openSettings.MenuItems.Add(mi_autoRefresh);
            mi_openSettings.MenuItems.Add(mi_setCheckRate);

            mi_refresh = new MenuItem("Neu Laden", mi_refresh_Click);
            mi_refresh.Enabled = false;

            mi_Special = new MenuItem("Speziell", mi_refresh_Click);
            mi_Special.Enabled = false;
            mi_openSettings.MenuItems.Add(new MenuItem("PN Versenden", mi_special_sendPn));


            contextMenu.MenuItems.Add(new MenuItem("ExternIcon", mi_showExternIcon_Click));
            contextMenu.MenuItems.Add(mi_refresh);
            contextMenu.MenuItems.Add(mi_openSettings);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(mi_exitApplication);

            notifyIcon_NotLoggedIn();
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            if (Pr0Notify.Properties.Settings.Default.Username.Length != 0 && Pr0Notify.Properties.Settings.Default.Password.Length != 0)
            {
                pr0_login();
            }
            if (Pr0Notify.Properties.Settings.Default.autoRefresh)
            {
                autoRefresh.RunWorkerAsync();
                mi_autoRefresh.Checked = Pr0Notify.Properties.Settings.Default.autoRefresh;
            }
            
            Application.Run();
            notifyIcon.Visible = false;
        }
        public static void doDoubleClick()
        {
            System.Diagnostics.Process.Start(@"http://pr0gramm.com/inbox/unread");
            notifyIcon_NoNotifications();
        }
        static void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            doDoubleClick();
        }

        static void autoRefresh_DoWork(object sender, DoWorkEventArgs e)
        {
            while(true)
            {
                pr0_sync();
                for (int i = 0; i <= Pr0Notify.Properties.Settings.Default.RefreshRate; i++)
                    Thread.Sleep(1000);
            }
        }
        #region MenuItem Functions
        private static void mi_special_sendPn(object sender, EventArgs e)
        {
            SendPN sendPnDialog = new SendPN();
            sendPnDialog.ShowDialog();
        }
        private static void mi_openSettings_userCredentials_Click(object sender, EventArgs e)
        {
            EditUserCredentials form = new EditUserCredentials();
            DialogResult res = form.ShowDialog();
            if (res == DialogResult.OK)
                client = new Pr0grammClient();
            pr0_login(true);
        }
        private static void mi_openSettings_checkForUpdates_Click(object sender, EventArgs e)
        {
            try
            {
                WebRequest request = WebRequest.Create(@"http://x39.unitedtacticalforces.de/api.php?action=projects&project=pr0notify&get[]=version&get[]=download");
                request.Method = "GET";
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest)request).UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.76 Safari/537.36";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                string successResult = responseString.Remove(0, responseString.IndexOf("\"success\":") + "\"success\":".Length);
                successResult = successResult.Remove(successResult.IndexOf(','));
                if (successResult.Equals("false"))
                {
                    string errorResult = responseString.Remove(0, responseString.IndexOf("\"error\":\"") + "\"error\":\"".Length);
                    errorResult = errorResult.Remove(errorResult.IndexOf(',') - 1);
                    MessageBox.Show("Das updaten ist leider fehlgeschlagen ...\r\nDie Fehlermeldung:\r\n" + errorResult, "Shit happens", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string contentResult = responseString.Remove(0, responseString.IndexOf("\"content\":") + "\"content\":".Length);
                contentResult = contentResult.Remove(contentResult.LastIndexOf('}'));

                string versionResult = contentResult.Remove(0, contentResult.IndexOf("\"version\":\"") + "\"version\":\"".Length);
                versionResult = versionResult.Remove(versionResult.IndexOf(',') - 1);
                if (Application.ProductVersion != versionResult)
                {
                    DialogResult res = MessageBox.Show("Frohlocket und jauchzet!\r\nMit freude präsentiere ich euch die frohe Kunde!\r\n\r\nVersion " + versionResult + " steht zum download bereit!\r\n\r\nSoll das update herunter geladen werden?", "YAY! UPDATE IST DA!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (res == DialogResult.Yes)
                    {
                        string downloadLink = contentResult.Remove(0, contentResult.IndexOf("\"download\":\"") + "\"download\":\"".Length);
                        downloadLink = downloadLink.Remove(downloadLink.LastIndexOf('\"'));
                        WebClient webclient = new WebClient();
                        webclient.DownloadFile(downloadLink, Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf('\\')) + downloadLink.Substring(downloadLink.LastIndexOf('/')));
                        System.Diagnostics.Process.Start(Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf('\\')) + downloadLink.Substring(downloadLink.LastIndexOf('/')));
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Das updaten ist leider fehlgeschlagen ...\r\nDie Fehlermeldung:\r\n" + ex.Message, "Shit happens", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private static void mi_showExternIcon_Click(object sender, EventArgs e)
        {

            try
            {
                if (((MenuItem)sender).Checked)
                {
                    ((MenuItem)sender).Checked = false;
                    if(form_externIcon != null)
                    {
                        form_externIcon.Close();
                        form_externIcon = null;
                    }
                }
                else
                {
                    ((MenuItem)sender).Checked = true;
                    if (form_externIcon == null)
                    {
                        form_externIcon = new externIcon();
                        form_externIcon.ContextMenu = contextMenu;
                        form_externIcon.Show();
                        if (notifyIcon.Icon == Pr0Notify.Properties.Resources.NewMessage)
                            form_externIcon.BackgroundImage = Pr0Notify.Properties.Resources.NewMessage_png;
                    }
                }
            }
            catch(Exception ex)
            {
                notifyIcon.ShowBalloonTip(5 * 1000, "Kritischer Fehler", ex.Message, ToolTipIcon.Error);
            }
        }
        private static void mi_openSettings_enableMessageboxNotifications_Click(object sender, EventArgs e)
        {

            try
            {
                if (Pr0Notify.Properties.Settings.Default.messageBoxNotification)
                {
                    mi_enableMessageboxNotifications.Checked = false;
                    Pr0Notify.Properties.Settings.Default.messageBoxNotification = false;
                    Pr0Notify.Properties.Settings.Default.Save();
                }
                else
                {
                    mi_enableMessageboxNotifications.Checked = true;
                    Pr0Notify.Properties.Settings.Default.messageBoxNotification = true;
                    Pr0Notify.Properties.Settings.Default.Save();
                }
            }
            catch(Exception ex)
            {
                notifyIcon.ShowBalloonTip(5 * 1000, "Kritischer Fehler", ex.Message, ToolTipIcon.Error);
            }
        }
        private static void mi_openSettings_enableBloonTippNotification_Click(object sender, EventArgs e)
        {

            try
            {
                if (Pr0Notify.Properties.Settings.Default.bloonTippNotification)
                {
                    mi_enableBloonTippNotification.Checked = false;
                    Pr0Notify.Properties.Settings.Default.bloonTippNotification = false;
                    Pr0Notify.Properties.Settings.Default.Save();
                }
                else
                {
                    mi_enableBloonTippNotification.Checked = true;
                    Pr0Notify.Properties.Settings.Default.bloonTippNotification = true;
                    Pr0Notify.Properties.Settings.Default.Save();
                }
            }
            catch(Exception ex)
            {
                notifyIcon.ShowBalloonTip(5 * 1000, "Kritischer Fehler", ex.Message, ToolTipIcon.Error);
            }
        }
        private static void mi_setCheckRate_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            Int32 num = Convert.ToInt32(new string(item.Text.Where(c => char.IsDigit(c)).ToArray())); //get only numbers out of this string and remove the end s/m/h
            if (item.Text.EndsWith("m"))
                num *= 60;
            foreach (MenuItem obj in mi_setCheckRate.MenuItems)
            {
                obj.Checked = false;
            }
            Pr0Notify.Properties.Settings.Default.RefreshRate = num;
            Pr0Notify.Properties.Settings.Default.RefreshRate_MenuItemIndex = mi_setCheckRate.MenuItems.IndexOf(item);
            Pr0Notify.Properties.Settings.Default.Save();
            item.Checked = true;
        }
        private static void mi_exitApplication_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private static void mi_autoRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                if (Pr0Notify.Properties.Settings.Default.autoRefresh)
                {
                    mi_autoRefresh.Checked = false;
                    Pr0Notify.Properties.Settings.Default.autoRefresh = false;
                    Pr0Notify.Properties.Settings.Default.Save();
                    autoRefresh.CancelAsync();
                }
                else
                {
                    mi_autoRefresh.Checked = true;
                    Pr0Notify.Properties.Settings.Default.autoRefresh = true;
                    Pr0Notify.Properties.Settings.Default.Save();
                    autoRefresh.RunWorkerAsync();
                }
            }
            catch(Exception ex)
            {
                notifyIcon.ShowBalloonTip(5 * 1000, "Kritischer Fehler", ex.Message, ToolTipIcon.Error);
            }
        }
        private static void mi_refresh_Click(object sender, EventArgs e)
        {
            pr0_sync(true);
        }
        #endregion
        public static void serializeCookie()
        {
            string header = client.GetCookies().GetCookieHeader(new Uri(@"http://pr0gramm.com"));
            Pr0Notify.Properties.Settings.Default.cookie = header;
            Pr0Notify.Properties.Settings.Default.Save();
        }
        public static void unserializeCookie()
        {
            CookieContainer cookieContainer = new CookieContainer();
            cookieContainer.SetCookies(new Uri(@"http://pr0gramm.com"), Pr0Notify.Properties.Settings.Default.cookie);
            client = new Pr0grammClient(new Pr0grammApiClient(cookieContainer));
        }
        public static async void pr0_login(bool forceLogin = false)
        {
            if (client != null && !forceLogin)
                return;
            if (forceLogin || string.IsNullOrEmpty(Pr0Notify.Properties.Settings.Default.cookie))
                client = new Pr0grammClient();
            else
            {
                unserializeCookie();
                notifyIcon.ShowBalloonTip(bloonTimeout, "Login Erfolgreich", "Cookie wurde erfolgreich geladen", ToolTipIcon.Info);
                notifyIcon_NoNotifications();

                mi_setCheckRate.Enabled = true;
                mi_refresh.Enabled = true;
                mi_autoRefresh.Enabled = true;
                mi_Special.Enabled = true;
                return;
            }
            try
            {
                var loginResponse = await client.User.Login(Pr0Notify.Properties.Settings.Default.Username, Pr0Notify.Properties.Settings.Default.Password);
                if(!loginResponse.Success)
                {
                    notifyIcon.ShowBalloonTip(5 * 1000, "Login Fehlgeschlagen", "Etwas scheint mit den daten nicht korrekt zu sein", ToolTipIcon.Error);
                    notifyIcon_NotLoggedIn();
                }

                mi_setCheckRate.Enabled = true;
                mi_refresh.Enabled = true;
                mi_autoRefresh.Enabled = true;
                mi_Special.Enabled = true;
            }
            catch(Exception ex)
            {
                notifyIcon.ShowBalloonTip(5 * 1000, "Whhoooops : /", "Irgendwo ist ein fehler aufgetreten :(\n" + ex.Message, ToolTipIcon.Error);
                notifyIcon_NotLoggedIn();
            }
        }
        public static async void pr0_sync(bool showNoNewMessagesHint = false)
        {
            try
            {
                var syncResponse = await client.User.Sync(Pr0Notify.Properties.Settings.Default.last_sync_id);
                Pr0Notify.Properties.Settings.Default.last_sync_id = syncResponse.LastId;
                if(syncResponse.InboxCount > 0)
                {
                    notifyIcon_NewNotifications(syncResponse.InboxCount);
                    if (syncResponse.InboxCount != inboxCount)
                    {
                        if (Pr0Notify.Properties.Settings.Default.messageBoxNotification)
                            MessageBox.Show("Du hast " + syncResponse.InboxCount + " neue " + (syncResponse.InboxCount > 1 ? "Benachrichtigungen" : "Benachrichtigung"), "Neue " + (syncResponse.InboxCount > 1 ? "Benachrichtigungen" : "Benachrichtigung"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (Pr0Notify.Properties.Settings.Default.bloonTippNotification)
                            notifyIcon.ShowBalloonTip(bloonTimeout, "Neue " + (syncResponse.InboxCount > 1 ? "Benachrichtigungen" : "Benachrichtigung"), "Du hast " + syncResponse.InboxCount + " neue " + (syncResponse.InboxCount > 1 ? "Benachrichtigungen" : "Benachrichtigung"), ToolTipIcon.Info);
                    }
                }
                else
                {
                    notifyIcon_NoNotifications();
                    if (showNoNewMessagesHint)
                    {
                        if (Pr0Notify.Properties.Settings.Default.messageBoxNotification)
                            MessageBox.Show(@"Leider hat dich niemand lieb ¯\_(ツ)_/¯", "Keine neuen Benachrichtigungen vorhanden", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (Pr0Notify.Properties.Settings.Default.bloonTippNotification)
                            notifyIcon.ShowBalloonTip(bloonTimeout, @"Leider hat dich niemand lieb ¯\_(ツ)_/¯", "Keine neuen Benachrichtigungen vorhanden", ToolTipIcon.Info);
                    }
                }
                inboxCount = syncResponse.InboxCount;
            }
            catch
            {
                notifyIcon.ShowBalloonTip(bloonTimeout, "Sync Fehlgeschlagen", "Konnte keinen Sync ausführen", ToolTipIcon.Error);
            }
        }
        private static void notifyIcon_NewNotifications(long i)
        {
            notifyIcon.Icon = Pr0Notify.Properties.Resources.NewMessage;
            notifyIcon.Text = i + (i > 1 ? " Benachrichtigungen" : " Benachrichtigung");
            if (form_externIcon != null)
                form_externIcon.BackgroundImage = Pr0Notify.Properties.Resources.NewMessage_png;
        }
        private static void notifyIcon_NoNotifications()
        {
            notifyIcon.Icon = Pr0Notify.Properties.Resources.NoMessage;
            notifyIcon.Text = "Keine Benachrichtigung";
            if (form_externIcon != null)
                form_externIcon.BackgroundImage = Pr0Notify.Properties.Resources.NoMessage_png;
        }
        private static void notifyIcon_NotLoggedIn()
        {
            notifyIcon.Icon = Pr0Notify.Properties.Resources.Message_LoginNotPresent;
            notifyIcon.Text = "Nicht Eingelogt";
        }
    }
}
