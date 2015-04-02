namespace Pr0Notify
{
    partial class externIcon
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(externIcon));
            this.SuspendLayout();
            // 
            // externIcon
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(70, 40);
            this.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "externIcon";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "externIcon";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.externIcon_Load);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.externIcon_MouseDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.externIcon_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.externIcon_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.externIcon_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}