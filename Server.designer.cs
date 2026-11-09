namespace MSMQ
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rtbMessages = new System.Windows.Forms.RichTextBox();
            this.tbAddress = new System.Windows.Forms.TextBox();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.lblUsers = new System.Windows.Forms.Label();
            this.lbUsers = new System.Windows.Forms.ListBox();
            System.Windows.Forms.Label addressLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            addressLabel.Text = "Адрес очереди для клиента";
            addressLabel.SetBounds(10, 8, 300, 20);
            this.tbAddress.SetBounds(10, 30, 565, 22);
            this.tbAddress.ReadOnly = true;
            this.tbAddress.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnPause.SetBounds(585, 28, 145, 26);
            this.btnPause.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnPause.Text = "Пауза приёма";
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            this.rtbMessages.SetBounds(10, 65, 550, 357);
            this.rtbMessages.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.rtbMessages.ReadOnly = true;
            this.rtbMessages.DetectUrls = false;
            this.lblUsers.SetBounds(574, 65, 156, 20);
            this.lblUsers.Text = "Участники: 0";
            this.lblUsers.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lbUsers.SetBounds(574, 88, 156, 296);
            this.lbUsers.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.lbUsers.IntegralHeight = false;
            this.lbUsers.SelectedIndexChanged += new System.EventHandler(this.lbUsers_SelectedIndexChanged);
            this.btnRemove.SetBounds(574, 394, 156, 28);
            this.btnRemove.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnRemove.Text = "Удалить участника";
            this.btnRemove.Enabled = false;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                addressLabel, this.tbAddress, this.btnPause, this.btnRemove, this.rtbMessages, this.lblUsers, this.lbUsers });
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 432);
            this.MinimumSize = new System.Drawing.Size(756, 470);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Сервер MSMQ";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.RichTextBox rtbMessages;
        private System.Windows.Forms.TextBox tbAddress;
        private System.Windows.Forms.Button btnPause, btnRemove;
        private System.Windows.Forms.Label lblUsers;
        private System.Windows.Forms.ListBox lbUsers;
    }
}
