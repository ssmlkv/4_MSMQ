using System;
using System.Threading;
using System.Windows.Forms;
using MSMQChat;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private ChatClient client;
        private bool connecting, closing;

        public frmMain()
        {
            InitializeComponent();
            tbReplyHost.Text = Environment.MachineName;
        }

        private void Post(Action action)
        {
            if (closing || IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke((MethodInvoker)delegate { if (!closing) action(); }); }
            catch (InvalidOperationException) { }
        }

        private void SetConnected(bool value)
        {
            connecting = false;
            btnConnect.Enabled = true;
            btnConnect.Text = value ? "Отключиться" : "Подключиться";
            btnSend.Enabled = value;
            tbMessage.Enabled = value;
            tbPath.Enabled = tbNick.Enabled = tbReplyHost.Enabled = !value;
            lblStatus.Text = value ? "В чате как " + tbNick.Text.Trim() : "Нет подключения";
            if (!value) lblUsers.Text = "Участники: —";
        }

        private void Disconnect()
        {
            ChatClient old = client;
            client = null;
            if (old != null) old.Dispose();
            if (!closing) SetConnected(false);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (connecting) return;
            if (client != null) { Disconnect(); return; }
            ChatClient pending;
            try { pending = new ChatClient(tbPath.Text, tbNick.Text.Trim(), tbReplyHost.Text.Trim()); }
            catch (Exception ex)
            { MessageBox.Show(this, ex.Message, "Вход в чат", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            client = pending;
            connecting = true;
            btnConnect.Enabled = false;
            tbPath.Enabled = tbNick.Enabled = tbReplyHost.Enabled = false;
            lblStatus.Text = "Ожидается подтверждение сервера…";
            pending.Received += delegate(ChatPacket packet)
            {
                Post(delegate
                {
                    if (client != pending) return;
                    if (packet.Kind == "Users") lblUsers.Text = "Участники: " + packet.Text;
                    else
                    {
                        string text = packet.Kind == "Chat" ? packet.Nick + ": " + packet.Text
                            : "[Система] " + packet.Text;
                        rtbMessages.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
                        rtbMessages.ScrollToCaret();
                    }
                });
            };
            pending.ConnectionLost += delegate(string reason)
            {
                Post(delegate
                {
                    if (client != pending || connecting) return;
                    rtbMessages.AppendText("[Система] " + reason + Environment.NewLine);
                    Disconnect();
                });
            };
            Thread worker = new Thread(delegate()
            {
                string error;
                bool success = pending.Connect(out error);
                Post(delegate
                {
                    if (client != pending) return;
                    if (success && pending.IsConnected) { SetConnected(true); tbMessage.Focus(); }
                    else
                    {
                        Disconnect();
                        MessageBox.Show(this, error ?? "Сеанс завершён сервером.", "Вход не выполнен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                });
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (client == null) return;
                client.Send(tbMessage.Text);
                tbMessage.Clear();
                tbMessage.Focus();
            }
            catch (Exception ex)
            { MessageBox.Show(this, ex.Message, "Отправка сообщения", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            closing = true;
            Disconnect();
        }
    }
}
