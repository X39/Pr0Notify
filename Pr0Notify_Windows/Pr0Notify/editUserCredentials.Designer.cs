namespace Pr0Notify
{
    partial class EditUserCredentials
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
            this.label_username = new System.Windows.Forms.Label();
            this.tb_username = new System.Windows.Forms.TextBox();
            this.tb_password = new System.Windows.Forms.TextBox();
            this.label_password = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_username
            // 
            this.label_username.AutoSize = true;
            this.label_username.Location = new System.Drawing.Point(12, 15);
            this.label_username.Name = "label_username";
            this.label_username.Size = new System.Drawing.Size(55, 13);
            this.label_username.TabIndex = 0;
            this.label_username.Text = "Username";
            // 
            // tb_username
            // 
            this.tb_username.Location = new System.Drawing.Point(73, 12);
            this.tb_username.Name = "tb_username";
            this.tb_username.Size = new System.Drawing.Size(411, 20);
            this.tb_username.TabIndex = 1;
            // 
            // tb_password
            // 
            this.tb_password.Location = new System.Drawing.Point(73, 38);
            this.tb_password.Name = "tb_password";
            this.tb_password.PasswordChar = '#';
            this.tb_password.Size = new System.Drawing.Size(411, 20);
            this.tb_password.TabIndex = 3;
            // 
            // label_password
            // 
            this.label_password.AutoSize = true;
            this.label_password.Location = new System.Drawing.Point(12, 41);
            this.label_password.Name = "label_password";
            this.label_password.Size = new System.Drawing.Size(50, 13);
            this.label_password.TabIndex = 2;
            this.label_password.Text = "Passwort";
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(12, 64);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(472, 23);
            this.btn_OK.TabIndex = 4;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // EditUserCredentials
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 96);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.tb_password);
            this.Controls.Add(this.label_password);
            this.Controls.Add(this.tb_username);
            this.Controls.Add(this.label_username);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(512, 134);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(512, 134);
            this.Name = "EditUserCredentials";
            this.Text = "Logindaten bearbeiten";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_username;
        private System.Windows.Forms.TextBox tb_username;
        private System.Windows.Forms.TextBox tb_password;
        private System.Windows.Forms.Label label_password;
        private System.Windows.Forms.Button btn_OK;
    }
}