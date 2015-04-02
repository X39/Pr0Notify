using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pr0Notify
{
    public partial class externIcon : Form
    {
        public bool dragged;
        public bool skip = true;
        public Point lastPos;
        public bool allowSizeChange;
        public bool allowSizeChange_skip;
        public externIcon()
        {
            allowSizeChange = false;
            allowSizeChange_skip = false;
            dragged = false;
            skip = false;
            InitializeComponent();
            this.MouseWheel += externIcon_MouseWheel;
        }

        void externIcon_MouseWheel(object sender, MouseEventArgs e)
        {
           if(dragged)
           {
               allowSizeChange = true;
               int newWidth = (int)(this.Size.Width + ((e.Delta > 0 ? 1 : -1) * 7));
               int newHeight = (int)(this.Size.Height + ((e.Delta > 0 ? 1 : -1) * 4));

               if (newWidth < 70)
                   newWidth = 70;

               if (newHeight < 40)
                   newHeight = 40;

               if (newWidth > 70 * 3)
                   newWidth = 70 * 3;

               if (newHeight > 40 * 3)
                   newHeight = 40 * 3;


               this.Size = new Size(newWidth, newHeight);
               Pr0Notify.Properties.Settings.Default.externIcon_width = newWidth;
               Pr0Notify.Properties.Settings.Default.externIcon_height = newHeight;
               Pr0Notify.Properties.Settings.Default.Save();

           }
        }

        private void externIcon_MouseDown(object sender, MouseEventArgs e)
        {
            dragged = true;
        }

        private void externIcon_MouseUp(object sender, MouseEventArgs e)
        {
            dragged = false;
        }

        private void externIcon_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragged && !skip)
            {
                if (lastPos == null)
                    lastPos = e.Location;
                this.Location = new Point(this.Location.X + e.X - lastPos.X, this.Location.Y + e.Y - lastPos.Y);
                skip = true;
                lastPos = e.Location;
                return;
            }
            lastPos = e.Location;
            skip = false;
        }

        private void externIcon_Load(object sender, EventArgs e)
        {
            this.Size = new Size(Pr0Notify.Properties.Settings.Default.externIcon_width, Pr0Notify.Properties.Settings.Default.externIcon_height);
        }

        private void externIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Program.doDoubleClick();
        }
    }
}
