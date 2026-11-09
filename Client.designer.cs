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
            this.tbPath = new System.Windows.Forms.TextBox();
            this.tbNick = new System.Windows.Forms.TextBox();
            this.tbReplyHost = new System.Windows.Forms.TextBox();
            this.tbMessage = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.rtbMessages = new System.Windows.Forms.RichTextBox();
            this.lblUsers = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            System.Windows.Forms.Label pathLabel = new System.Windows.Forms.Label();
            System.Windows.Forms.Label nickLabel = new System.Windows.Forms.Label();
            System.Windows.Forms.Label replyLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            pathLabel.Text = "Очередь сервера";
            pathLabel.SetBounds(10, 13, 106, 20);
            this.tbPath.SetBounds(118, 10, 492, 22);
            this.tbPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.tbPath.Text = ".\\private$\\ServerQueue";
            this.tbPath.TabIndex = 0;
            this.btnConnect.SetBounds(620, 8, 110, 26);
            this.btnConnect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnConnect.Text = "Подключиться";
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            nickLabel.Text = "Ник";
            nickLabel.SetBounds(10, 47, 40, 20);
            this.tbNick.SetBounds(50, 44, 220, 22);
            this.tbNick.MaxLength = MSMQChat.ChatProtocol.MaxNickLength;
            this.tbNick.TabIndex = 1;
            replyLabel.Text = "Мой узел для ответов";
            replyLabel.SetBounds(290, 47, 140, 20);
            this.tbReplyHost.SetBounds(434, 44, 296, 22);
            this.tbReplyHost.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.tbReplyHost.TabIndex = 2;
            this.lblStatus.SetBounds(10, 72, 720, 20);
            this.lblStatus.Text = "Нет подключения";
            this.rtbMessages.SetBounds(10, 96, 720, 250);
            this.rtbMessages.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.rtbMessages.ReadOnly = true;
            this.rtbMessages.DetectUrls = false;
            this.rtbMessages.TabIndex = 6;
            this.lblUsers.SetBounds(10, 356, 720, 35);
            this.lblUsers.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblUsers.Text = "Участники: —";
            this.lblUsers.AutoEllipsis = true;
            this.tbMessage.SetBounds(10, 397, 600, 22);
            this.tbMessage.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.tbMessage.MaxLength = MSMQChat.ChatProtocol.MaxTextLength;
            this.tbMessage.Enabled = false;
            this.tbMessage.TabIndex = 4;
            this.btnSend.SetBounds(620, 395, 110, 26);
            this.btnSend.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnSend.Text = "Отправить";
            this.btnSend.Enabled = false;
            this.btnSend.TabIndex = 5;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            this.Controls.AddRange(new System.Windows.Forms.Control[] { pathLabel, nickLabel, replyLabel,
                this.tbPath, this.tbNick, this.tbReplyHost, this.tbMessage, this.btnConnect, this.btnSend,
                this.rtbMessages, this.lblUsers, this.lblStatus });
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 432);
            this.MinimumSize = new System.Drawing.Size(756, 470);
            this.AcceptButton = this.btnSend;
            this.Name = "frmMain";
            this.Text = "Клиент MSMQ";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox tbPath, tbNick, tbReplyHost, tbMessage;
        private System.Windows.Forms.Button btnConnect, btnSend;
        private System.Windows.Forms.RichTextBox rtbMessages;
        private System.Windows.Forms.Label lblUsers, lblStatus;
    }
}
