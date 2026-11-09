using System;
using System.Windows.Forms;
using MSMQChat;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private readonly ChatServer server;
        private bool closing;

        public frmMain(string queueName)
        {
            InitializeComponent();
            server = new ChatServer(queueName);
            tbAddress.Text = server.Address;
            Text = "Сервер MSMQ — " + queueName;
            server.Log += delegate(string text) { Post(delegate
            {
                rtbMessages.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
                rtbMessages.ScrollToCaret();
            }); };
            server.MembersChanged += delegate(ChatMember[] users) { Post(delegate
            {
                ChatMember selected = lbUsers.SelectedItem as ChatMember;
                lbUsers.Items.Clear();
                lbUsers.Items.AddRange(users);
                if (selected != null)
                    foreach (ChatMember member in users)
                        if (member.SessionId == selected.SessionId) lbUsers.SelectedItem = member;
                lblUsers.Text = "Участники: " + users.Length;
            }); };
            server.Failed += delegate(string error) { Post(delegate
            {
                btnPause.Enabled = false;
                btnRemove.Enabled = false;
                rtbMessages.AppendText("[Ошибка MSMQ] " + error + Environment.NewLine);
                MessageBox.Show(this, error, "Сервер остановлен", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }); };
        }

        private void lbUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemove.Enabled = lbUsers.SelectedItem is ChatMember;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            ChatMember selected = lbUsers.SelectedItem as ChatMember;
            if (selected == null) return;
            try
            {
                server.RemoveUser(selected.SessionId);
                btnRemove.Enabled = false;
                rtbMessages.AppendText("Удаление участника " + selected.Nick
                    + " и его персональной очереди…" + Environment.NewLine);
            }
            catch (Exception ex)
            { MessageBox.Show(this, ex.Message, "Удаление участника", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void Post(Action action)
        {
            if (closing || IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke((MethodInvoker)delegate { if (!closing) action(); }); }
            catch (InvalidOperationException) { }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            rtbMessages.AppendText("Скопируйте адрес очереди в клиент. Сервер готов." + Environment.NewLine);
            server.Start();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            server.Paused = !server.Paused;
            btnPause.Text = server.Paused ? "Продолжить приём" : "Пауза приёма";
            rtbMessages.AppendText(server.Paused
                ? "Приём текста приостановлен. Сообщения накапливаются в MSMQ, проверка связи продолжается." + Environment.NewLine
                : "Приём возобновлён." + Environment.NewLine);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            closing = true;
            server.Dispose();
        }
    }
}
