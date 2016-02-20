using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace Pr0Notify2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App instance;


        Pr0Notify2.Pr0API.User User;
        System.Windows.Forms.NotifyIcon TrayIcon;

        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            instance = new App();
            instance.InitializeComponent();


            instance.User = null;
            instance.TrayIcon = new System.Windows.Forms.NotifyIcon();
            instance.TrayIcon.Icon = Pr0Notify2.Properties.Resources.Message_LoginNotPresent;
            instance.TrayIcon.Text = "Nicht Eingelogt";
            instance.TrayIcon.Visible = true;
            instance.TrayIcon.DoubleClick += TrayIcon_DoubleClick;

            instance.Run();
        }

        private static void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            if(instance.User == null || !instance.User.IsValid)
            {
                ((MainWindow)instance.MainWindow).showLoginTemplate();
            }
            else
            {

            }
        }
    }
}
