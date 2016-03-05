using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Pr0Notify2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += MainWindow_SourceInitialized;
        }
        #region login
        public void showLoginTemplate()
        {
            var template = (ControlTemplate)this.FindResource("GridLoginView");
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            if (this.ViewPort.Template == template)
                return;
            this.ViewPort.Template = template;
            this.ViewPort.ApplyTemplate();
            this.Height = 162;
            this.MaxHeight = 168;
            this.MinHeight = 162;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
            var btnLogin = (Button)this.ViewPort.Template.FindName("btnLogin", this.ViewPort);
            btnLogin.Click += BtnLogin_Click;
            var tbUsername = (TextBox)this.ViewPort.Template.FindName("tbUsername", this.ViewPort);
            tbUsername.KeyDown += TbUsername_KeyDown;
            var tbPassword = (PasswordBox)this.ViewPort.Template.FindName("tbPassword", this.ViewPort);
            tbPassword.KeyDown += TbPassword_KeyDown;
        }
        private void TbUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                var tbPassword = (PasswordBox)this.ViewPort.Template.FindName("tbPassword", this.ViewPort);
                tbPassword.Focus();
            }
        }
        private void TbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var btnLogin = (Button)this.ViewPort.Template.FindName("btnLogin", this.ViewPort);
                BtnLogin_Click(btnLogin, new RoutedEventArgs());
            }
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var eh = this.UserLogin;
            if (eh == null)
                return;
            var tbUsername = (TextBox)this.ViewPort.Template.FindName("tbUsername", this.ViewPort);
            var tbPassword = (PasswordBox)this.ViewPort.Template.FindName("tbPassword", this.ViewPort);
            
            if (string.IsNullOrWhiteSpace(tbUsername.Text))
            {
                MessageBox.Show("Bitte gib einen Username an", "Kein Username gegeben");
                return;
            }
            if (string.IsNullOrWhiteSpace(tbPassword.Password))
            {
                MessageBox.Show("Bitte gib ein Password an", "Kein Passwort gegeben");
                return;
            }
            eh(this, new UserLoginEventArgs(new Pr0API.User(tbUsername.Text.Trim()), tbPassword.Password));
        }
        public class UserLoginEventArgs : EventArgs
        {
            public Pr0API.User User { get; internal set; }
            public string Password { get; internal set; }
            public UserLoginEventArgs(Pr0API.User user, string password)
            {
                this.User = user;
                this.Password = password;
            }
        }
        public event EventHandler<UserLoginEventArgs> UserLogin;
        #endregion
        #region Sync Interval
        public class SyncIntervalConfirmedEventArgs : EventArgs
        {
            public int Value { get; internal set; }
            public SyncIntervalConfirmedEventArgs(int value)
            {
                this.Value = value;
            }
        }
        public event EventHandler<SyncIntervalConfirmedEventArgs> SyncIntervalConfirmed;


        public void showSyncIntervalTemplate(int curValue)
        {
            var template = (ControlTemplate)this.FindResource("SyncRateView");
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            if (this.ViewPort.Template == template)
                return;
            this.ViewPort.Template = template;
            this.ViewPort.ApplyTemplate();
            this.Height = 128;
            this.MaxHeight = 128;
            this.MinHeight = 128;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);

            var btnSyncIntervalConfirm = (Button)this.ViewPort.Template.FindName("btnSyncIntervalConfirm", this.ViewPort);
            btnSyncIntervalConfirm.Click += BtnSyncIntervalConfirm_Click;

            var tbValue = (TextBox)this.ViewPort.Template.FindName("tbValue", this.ViewPort);
            tbValue.PreviewTextInput += TbValue_PreviewTextInput;
            tbValue.KeyDown += TbValue_KeyDown;
            tbValue.Text = curValue.ToString();

            var sliderValue = (Slider)this.ViewPort.Template.FindName("sliderValue", this.ViewPort);
            sliderValue.Value = curValue;
            sliderValue.ValueChanged += SliderValue_ValueChanged;
        }


        private void TbValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                var tbValue = (TextBox)sender;
                var sliderValue = (Slider)this.ViewPort.Template.FindName("sliderValue", this.ViewPort);
                if(string.IsNullOrWhiteSpace(tbValue.Text))
                {
                    tbValue.Text = ((int)sliderValue.Value).ToString();
                    return;
                }
                sliderValue.Value = int.Parse(tbValue.Text);
            }
        }
        private void SliderValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var tbValue = (TextBox)this.ViewPort.Template.FindName("tbValue", this.ViewPort);
            tbValue.Text = ((int)e.NewValue).ToString();
        }
        private void TbValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tbValue = (TextBox)sender;
            if(string.IsNullOrWhiteSpace(tbValue.Text))
            {
                e.Handled = true;
                return;
            }
            int res;
            e.Handled = !int.TryParse(e.Text, out res);
        }
        private void BtnSyncIntervalConfirm_Click(object sender, RoutedEventArgs e)
        {
            var eh = this.SyncIntervalConfirmed;
            if (eh == null)
                return;
            var tbValue = (TextBox)this.ViewPort.Template.FindName("tbValue", this.ViewPort);
            eh(sender, new SyncIntervalConfirmedEventArgs(int.Parse(tbValue.Text)));
        }
        #endregion
        #region show PN-Manager
        public class PNM_StartNewConversationEventArgs : EventArgs
        {
            public string Value { get; internal set; }
            public PNM_StartNewConversationEventArgs(string value)
            {
                this.Value = value;
            }
        }
        public event EventHandler PNM_Initialized;
        public event EventHandler PNM_PollMoreMessages;
        public event EventHandler PNM_SyncRequested;
        public event EventHandler<PNM_StartNewConversationEventArgs> PNM_StartNewConversation;
        private MainWindow Dialog;
        private object DialogValue;
        public void showPNManager()
        {
            var template = (ControlTemplate)this.FindResource("GridPrivateMessages");
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            if (this.ViewPort.Template == template)
                return;
            this.ViewPort.Template = template;
            this.ViewPort.ApplyTemplate();
            this.Width = 512 + 256;
            this.Height = 512;
            this.MinHeight = 256;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);

            var btnStartNewConversation = (Button)this.ViewPort.Template.FindName("btnStartNewConversation", this.ViewPort);
            btnStartNewConversation.Click += BtnStartNewConversation_Click;
            var btnRefreshUi = (Button)this.ViewPort.Template.FindName("btnRefreshUi", this.ViewPort);
            btnRefreshUi.Click += BtnRefreshUi_Click;

            var eh = this.PNM_Initialized;
            if (eh != null)
                eh(this, new EventArgs());
        }

        private void BtnRefreshUi_Click(object sender, RoutedEventArgs e)
        {
            var eh = this.PNM_SyncRequested;
            if (eh != null)
                eh(this, new EventArgs());
        }

        private void BtnStartNewConversation_Click(object sender, RoutedEventArgs e)
        {
            Dialog = new MainWindow();
            Dialog.showNewConversationUi();
            var result = Dialog.ShowDialog();
            if(result.HasValue && result.Value)
            {
                string user = (string)Dialog.DialogValue;
                var eh = this.PNM_StartNewConversation;
                if (eh != null)
                    eh(this, new PNM_StartNewConversationEventArgs(user));
            }
        }
        #endregion
        #region show Confirm UI
        public class ConfirmProcessedEventArgs : EventArgs
        {
            public bool Result { get; internal set; }
            public ConfirmProcessedEventArgs(bool result)
            {
                this.Result = result;
            }
        }
        public event EventHandler<ConfirmProcessedEventArgs> ConfirmProcessed;


        public void showConfirmUi(string content)
        {
            var template = (ControlTemplate)this.FindResource("ConfirmUI");
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            if (this.ViewPort.Template == template)
                return;
            this.ViewPort.Template = template;
            this.ViewPort.ApplyTemplate();
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);

            this.MaxHeight = this.Height;
            this.MinHeight = this.Height;
            this.MaxWidth = this.Width;
            this.MinWidth = this.Width;


            var tblockContent = (TextBlock)this.ViewPort.Template.FindName("tblockContent", this.ViewPort);
            tblockContent.Text = content;
            var btnConfirm = (Button)this.ViewPort.Template.FindName("btnConfirm", this.ViewPort);
            btnConfirm.Click += BtnConfirm_Click;
            var btnReject = (Button)this.ViewPort.Template.FindName("btnReject", this.ViewPort);
            btnReject.Click += BtnReject_Click;

        }


        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            var eh = this.ConfirmProcessed;
            if (eh == null)
                return;
            eh(sender, new ConfirmProcessedEventArgs(false));
        }
        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var eh = this.ConfirmProcessed;
            if (eh == null)
                return;
            eh(sender, new ConfirmProcessedEventArgs(true));
        }
        #endregion
        #region show NewConversation UI
        public void showNewConversationUi()
        {
            var template = (ControlTemplate)this.FindResource("NewConversationUI");
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            if (this.ViewPort.Template == template)
                return;
            this.ViewPort.Template = template;
            this.ViewPort.ApplyTemplate();

            this.Height = 140;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);

            this.MaxHeight = this.Height;
            this.MinHeight = this.Height;
            this.MaxWidth = this.Width;
            this.MinWidth = this.Width;


            var tbContact = (TextBox)this.ViewPort.Template.FindName("tbContact", this.ViewPort);
            tbContact.Focus();
            var btnSearch = (Button)this.ViewPort.Template.FindName("btnSearch", this.ViewPort);
            btnSearch.Click += BtnSearch_Click;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var tbContact = (TextBox)this.ViewPort.Template.FindName("tbContact", this.ViewPort);
            if (!string.IsNullOrWhiteSpace(tbContact.Text))
            {
                this.DialogResult = true;
                this.DialogValue = tbContact.Text;
            }
            this.Close();
        }
        #endregion

        #region UI LookNFeel
        #region WINDOW native-resize stuff
        private enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }

        private HwndSource hwnd;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            hwnd = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            hwnd.AddHook(new HwndSourceHook(WndProc));
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Maximize overlap fix:
            //http://blogs.msdn.com/b/llobo/archive/2006/08/01/maximizing-window-_2800_with-windowstyle_3d00_none_2900_-considering-taskbar.aspx
            return IntPtr.Zero;
        }
        private void Resize(ResizeDirection dir)
        {
            SendMessage(hwnd.Handle, 0x112, (IntPtr)(61440 + dir), IntPtr.Zero);
        }
        #endregion

        bool WINDOW_MouseMove_Flag_wasModified = false;
        readonly int WINDOW_INVISBORDER_MARGIN = 5;


        private void WINDOW_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void WINDOW_MAXIMIZE_RESTORE_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void WINDOW_MINIMIZE_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WINDOW_TITLEBAR_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!WINDOW_MouseMove_Flag_wasModified)
            {
                DragMove();
                e.Handled = true;
            }
        }

        private void WINDOW_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                if (WINDOW_MouseMove_Flag_wasModified)
                {
                    WINDOW_MouseMove_Flag_wasModified = false;
                    this.Cursor = Cursors.Arrow;
                }
                return;
            }
            var pos = e.GetPosition(this);
            bool left = pos.X <= WINDOW_INVISBORDER_MARGIN;
            bool right = pos.X >= this.Width - WINDOW_INVISBORDER_MARGIN;
            bool top = pos.Y <= WINDOW_INVISBORDER_MARGIN;
            bool bot = pos.Y >= this.Height - WINDOW_INVISBORDER_MARGIN;
            if (left)
            {
                this.Cursor = top ? Cursors.SizeNWSE : bot ? Cursors.SizeNESW : Cursors.SizeWE;
                WINDOW_MouseMove_Flag_wasModified = true;
            }
            else if (right)
            {
                this.Cursor = top ? Cursors.SizeNESW : bot ? Cursors.SizeNWSE : Cursors.SizeWE;
                WINDOW_MouseMove_Flag_wasModified = true;
            }
            else if (top || bot)
            {
                this.Cursor = Cursors.SizeNS;
                WINDOW_MouseMove_Flag_wasModified = true;
            }
            else if (WINDOW_MouseMove_Flag_wasModified)
            {
                WINDOW_MouseMove_Flag_wasModified = false;
                this.Cursor = Cursors.Arrow;
            }
        }


        private void WINDOW_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                return;
            }
            var pos = e.GetPosition(this);
            bool left = pos.X <= WINDOW_INVISBORDER_MARGIN;
            bool right = pos.X >= this.Width - WINDOW_INVISBORDER_MARGIN;
            bool top = pos.Y <= WINDOW_INVISBORDER_MARGIN;
            bool bot = pos.Y >= this.Height - WINDOW_INVISBORDER_MARGIN;
            if (left)
            {
                this.Resize(top ? ResizeDirection.TopLeft : bot ? ResizeDirection.BottomLeft : ResizeDirection.Left);
                e.Handled = true;
            }
            else if (right)
            {
                this.Resize(top ? ResizeDirection.TopRight : bot ? ResizeDirection.BottomRight : ResizeDirection.Right);
                e.Handled = true;
            }
            else if (top)
            {
                this.Resize(ResizeDirection.Top);
                e.Handled = true;
            }
            else if (bot)
            {
                this.Resize(ResizeDirection.Bottom);
                e.Handled = true;
            }
        }
        #endregion

        private void chatCommitButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0)
                return;
            if(e.VerticalOffset == 0)
            {
                double curHeight = ((ScrollViewer)sender).ScrollableHeight;
                var eh = this.PNM_PollMoreMessages;
                if (eh != null)
                {
                    eh(this, new EventArgs());
                    Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => {
                        ((ScrollViewer)sender).ScrollToVerticalOffset(((ScrollViewer)sender).ScrollableHeight - curHeight);
                    }));
                    
                }
            }
        }

        private void MessageDisplay_Copy_Click(object sender, RoutedEventArgs e)
        {
            ListView MessageDisplay = (ListView)this.ViewPort.Template.FindName("MessageDisplay", this.ViewPort);
            ListBox userBox = (ListBox)this.ViewPort.Template.FindName("listBox", this.ViewPort);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<MyLabel> labelList = new List<MyLabel>();
            foreach (var it in MessageDisplay.SelectedItems)
            {
                labelList.Add((MyLabel)it);
            }

            labelList.Sort((x, y) =>
            {
                return MessageDisplay.Items.IndexOf(x) > MessageDisplay.Items.IndexOf(y) ? 1 : -1;
            });

            foreach(var it in labelList)
            {
                sb.Append(it.msg.Created);
                sb.Append(" <" + it.msg.SenderName + ">: ");
                sb.Append(it.msg.Text);
                sb.Append("\n\n");
            }
            Clipboard.SetText(sb.ToString());
        }

        private void MessageDisplay_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                MessageDisplay_Copy_Click(sender, new RoutedEventArgs());
            }
        }
    }
}
