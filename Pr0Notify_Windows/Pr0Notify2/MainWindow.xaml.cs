using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Controls;

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
            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
        }

        public void showLoginTemplate()
        {
            this.ViewPort.Template = (ControlTemplate)this.FindResource("GridLoginView");
            this.Height = 168;
            this.MaxHeight = 168;
            this.MinHeight = 168;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
            this.Visibility = Visibility.Visible;
        }

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
            this.Visibility = Visibility.Hidden;
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

        private void listBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void chatCommitButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {

        }
    }
}
