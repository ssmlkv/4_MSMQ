using System;
using System.Messaging;
using System.Threading;

namespace MSMQChat
{
    public sealed class ChatClient : IDisposable
    {
        private readonly string address, nick, session, localPath;
        private readonly MessageQueue inbox;
        private readonly AutoResetEvent registration = new AutoResetEvent(false);
        private readonly Thread receiver;
        private Timer heartbeat;
        private volatile bool running = true, connected;
        private string registrationError;
        private long lastReplyTicks;
        private int disposed;
        private int inboxCleaned;
        private bool removedByServer;
        private readonly object lifecycle = new object();
        public event Action<ChatPacket> Received;
        public event Action<string> ConnectionLost;

        public ChatClient(string serverAddress, string nickname, string replyHost)
        {
            string error = ChatProtocol.ValidateNick(nickname);
            if (error != null) throw new ArgumentException(error);
            address = ChatProtocol.NormalizeAddress(serverAddress);
            nick = nickname;
            session = Guid.NewGuid().ToString("N");
            localPath = ".\\private$\\MSMQChatClient_" + session;
            ReplyAddress = ChatProtocol.NormalizeAddress(replyHost + "\\private$\\MSMQChatClient_" + session);
            inbox = QueueTransport.OpenLocal("MSMQChatClient_" + session);
            receiver = new Thread(ReceiveLoop);
            receiver.IsBackground = true;
            receiver.Name = "MSMQ chat client";
        }

        public string ReplyAddress { get; private set; }
        public bool IsConnected { get { return connected && running; } }

        private void SendPacket(string kind, string text)
        {
            QueueTransport.Send(address, new ChatPacket(kind, session, nick, text));
        }

        public bool Connect(out string error)
        {
            try
            {
                lock (lifecycle)
                {
                    if (!running || disposed != 0)
                    { error = "Вход отменён."; return false; }
                    receiver.Start();
                    ChatPacket request = new ChatPacket("Register", session, nick, null);
                    request.ReplyQueue = ReplyAddress;
                    request.ExpiresUtc = DateTime.UtcNow.AddSeconds(
                        ChatProtocol.JoinTimeoutSeconds);
                    QueueTransport.Send(address, request);
                }
                if (!registration.WaitOne(ChatProtocol.JoinTimeoutSeconds * 1000, false))
                    error = "Сервер не подтвердил вход. Проверьте адрес, MSMQ, права и обратную доставку.";
                else if (!connected) error = registrationError ?? "Вход отменён.";
                else
                {
                    lock (lifecycle)
                    {
                        if (!running || disposed != 0) { error = "Вход отменён."; return false; }
                        error = null;
                        heartbeat = new Timer(Heartbeat, null, ChatProtocol.HeartbeatSeconds * 1000,
                            ChatProtocol.HeartbeatSeconds * 1000);
                        return true;
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }
            return false;
        }

        public void Send(string text)
        {
            if (!connected || !running) throw new InvalidOperationException("Сначала войдите в чат.");
            if (String.IsNullOrEmpty(text) || text.Trim().Length == 0 || text.Length > ChatProtocol.MaxTextLength)
                throw new ArgumentException("Сообщение должно содержать от 1 до 4000 символов.");
            SendPacket("Message", text);
            // Сообщение показывается только после серверной рассылки.
        }

        private void Lost(string reason)
        {
            if (!running) return;
            running = false;
            connected = false;
            if (heartbeat != null) heartbeat.Dispose();
            registrationError = reason;
            registration.Set();
            if (ConnectionLost != null) ConnectionLost(reason);
        }

        private void Heartbeat(object state)
        {
            if (!connected || !running) return;
            if ((DateTime.UtcNow - new DateTime(Interlocked.Read(ref lastReplyTicks), DateTimeKind.Utc)).TotalSeconds
                >= ChatProtocol.LeaseSeconds)
            { Lost("Сервер не отвечает более 30 секунд. Подключитесь заново."); return; }
            try { SendPacket("Heartbeat", null); }
            catch (Exception ex) { Lost("Ошибка MSMQ: " + ex.Message); }
        }

        private void ReceiveLoop()
        {
            try
            {
                while (running)
                {
                    try
                    {
                        using (Message message = inbox.Receive(TimeSpan.FromMilliseconds(500)))
                        {
                            ChatPacket packet;
                            try { packet = message.Body as ChatPacket; }
                            catch (InvalidOperationException) { continue; }
                            if (!running || packet == null || packet.SessionId != session) continue;
                            Interlocked.Exchange(ref lastReplyTicks, DateTime.UtcNow.Ticks);
                            if (packet.Kind == "Registered")
                            { connected = true; registration.Set(); }
                            else if (packet.Kind == "Error" && !connected)
                            { registrationError = packet.Text; registration.Set(); }
                            else if (packet.Kind == "Kicked")
                            { removedByServer = true; Lost(packet.Text); }
                            else if (packet.Kind == "Stopped") Lost(packet.Text);
                            else if (connected && packet.Kind != "Pong" && Received != null) Received(packet);
                        }
                    }
                    catch (MessageQueueException ex)
                    {
                        if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout) throw;
                    }
                }
            }
            catch (Exception ex) { Lost("Ошибка приёма MSMQ: " + ex.Message); }
            finally { inbox.Dispose(); DeleteInbox(); }
        }

        private void DeleteInbox()
        {
            if (Interlocked.Exchange(ref inboxCleaned, 1) != 0) return;
            string error = null;
            try
            {
                if (MessageQueue.Exists(localPath)) MessageQueue.Delete(localPath);
            }
            catch (Exception ex) { error = ex.Message; }
            if (removedByServer)
            {
                try { SendPacket(error == null ? "QueueDeleted" : "QueueDeleteError", error); }
                catch (Exception) { }
            }
        }

        public void Dispose()
        {
            lock (lifecycle)
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0) return;
                if (heartbeat != null) heartbeat.Dispose();
                running = false;
                connected = false;
                registration.Set();
            }
            // Leave также отменяет регистрацию, ещё находящуюся в очереди сервера.
            try { SendPacket("Leave", null); } catch (Exception) { }
            if ((receiver.ThreadState & ThreadState.Unstarted) != 0) inbox.Dispose();
            else if (Thread.CurrentThread != receiver) receiver.Join(2000);
            if (!receiver.IsAlive) DeleteInbox();
            // registration может ещё ожидаться потоком Connect, поэтому закрытие handle
            // оставлено сборщику мусора вместе с короткоживущим объектом сеанса.
        }
    }
}
