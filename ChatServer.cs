using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;

namespace MSMQChat
{
    public sealed class ChatServer : IDisposable
    {
        private readonly ChatRoom room = new ChatRoom();
        private readonly MessageQueue queue;
        private readonly Thread worker;
        private volatile bool running = true;
        private volatile bool paused;
        private readonly Queue<string> removals = new Queue<string>();
        private sealed class QueueCleanup
        {
            public ChatMember Member;
            public DateTime DueUtc;
        }
        private readonly Dictionary<string, QueueCleanup> cleanup = new Dictionary<string, QueueCleanup>();
        public event Action<string> Log;
        public event Action<ChatMember[]> MembersChanged;
        public event Action<string> Failed;
        public string Address { get; private set; }
        public bool Paused { get { return paused; } set { paused = value; } }

        public ChatServer(string queueName)
        {
            queue = QueueTransport.OpenLocal(queueName);
            Address = ChatProtocol.NormalizeAddress(".\\private$\\" + queueName);
            worker = new Thread(ReceiveLoop);
            worker.IsBackground = true;
            worker.Name = "MSMQ chat server";
        }

        public void Start() { worker.Start(); }

        public void RemoveUser(string sessionId)
        {
            if (!ChatProtocol.IsSession(sessionId)) throw new ArgumentException("Неверный сеанс участника.");
            lock (removals)
            {
                if (!running) throw new InvalidOperationException("Сервер остановлен.");
                removals.Enqueue(sessionId);
            }
        }

        private void ProcessRemovals()
        {
            while (running)
            {
                string session;
                lock (removals)
                {
                    if (removals.Count == 0) break;
                    session = removals.Dequeue();
                }
                Dispatch(room.RemoveUser(session));
            }
        }

        private void WriteLog(string text) { if (Log != null) Log(text); }

        private void Dispatch(RoomResult result)
        {
            foreach (string text in result.Log) WriteLog(text);
            foreach (ChatMember removed in result.RemovedMembers)
                cleanup[removed.SessionId] = new QueueCleanup {
                    Member = removed, DueUtc = DateTime.UtcNow.AddSeconds(3) };
            foreach (Delivery delivery in result.Deliveries)
            {
                try { QueueTransport.Send(delivery.Address, delivery.Packet); }
                catch (Exception ex)
                {
                    MessageQueueException mqError = ex as MessageQueueException;
                    string code = mqError == null ? ex.GetType().Name : mqError.MessageQueueErrorCode.ToString();
                    WriteLog("Не удалось поставить ответ в очередь " + delivery.Address
                        + " [" + code + "]: " + ex.Message);
                }
            }
            if (result.MembersChanged && MembersChanged != null) MembersChanged(room.Participants());
        }

        private bool HandleCleanupReply(ChatPacket packet)
        {
            if (packet == null || (packet.Kind != "QueueDeleted" && packet.Kind != "QueueDeleteError"))
                return false;
            QueueCleanup pending;
            if (packet.SessionId == null || !cleanup.TryGetValue(packet.SessionId, out pending)) return true;
            if (packet.Kind == "QueueDeleteError")
                WriteLog("Клиент " + pending.Member.Nick + " не смог удалить очередь: " + packet.Text);
            else
            {
                try
                {
                    string localPath = ChatProtocol.LocalClientQueuePath(pending.Member.Address);
                    if (localPath != null && MessageQueue.Exists(localPath)) return true;
                    cleanup.Remove(packet.SessionId);
                    WriteLog("Удаление очереди " + pending.Member.Nick + " подтверждено клиентом: "
                        + pending.Member.Address);
                }
                catch (Exception ex)
                { WriteLog("Не удалось проверить удаление очереди " + pending.Member.Address + ": " + ex.Message); }
            }
            return true;
        }

        private void ProcessQueueCleanups(DateTime nowUtc)
        {
            List<string> due = new List<string>();
            foreach (KeyValuePair<string, QueueCleanup> entry in cleanup)
                if (entry.Value.DueUtc <= nowUtc) due.Add(entry.Key);
            foreach (string session in due)
            {
                ChatMember member = cleanup[session].Member;
                try
                {
                    string path = ChatProtocol.LocalClientQueuePath(member.Address);
                    if (path == null)
                        WriteLog("Нет подтверждения удаления очереди " + member.Nick
                            + " на другом компьютере. Проверьте клиент: " + member.Address);
                    else
                    {
                        if (MessageQueue.Exists(path)) MessageQueue.Delete(path);
                        WriteLog("Персональная очередь " + member.Nick + " удалена: " + path);
                    }
                }
                catch (Exception ex)
                { WriteLog("Не удалось удалить очередь " + member.Address + ": " + ex.Message); }
                cleanup.Remove(session);
            }
        }

        private Message ReceiveNext()
        {
            if (!paused) return queue.Receive(TimeSpan.FromMilliseconds(500));

            // Во время демонстрационной паузы текст остаётся в настоящей очереди.
            // Heartbeat извлекается по Id даже за накопленными сообщениями:
            // иначе клиенты считают сервер недоступным и удаляют очереди ответов.
            string heartbeatId = null;
            using (MessageEnumerator messages = queue.GetMessageEnumerator2())
            {
                while (running && paused && messages.MoveNext(TimeSpan.Zero))
                {
                    using (Message candidate = messages.Current)
                    {
                        if (candidate.Label == "MSMQChat Heartbeat"
                            || candidate.Label == "MSMQChat QueueDeleted"
                            || candidate.Label == "MSMQChat QueueDeleteError")
                        {
                            heartbeatId = candidate.Id;
                            break;
                        }
                    }
                }
            }
            return heartbeatId == null ? null : queue.ReceiveById(heartbeatId, TimeSpan.Zero);
        }

        private void ReceiveLoop()
        {
            try
            {
                while (running)
                {
                    ProcessRemovals();
                    try
                    {
                        using (Message message = ReceiveNext())
                        {
                            if (message != null)
                            {
                                ChatPacket packet;
                                try { packet = message.Body as ChatPacket; }
                                catch (InvalidOperationException ex)
                                { WriteLog("Пропущен повреждённый XML-пакет: " + ex.Message); continue; }
                                if (!HandleCleanupReply(packet))
                                    Dispatch(room.Process(packet, DateTime.UtcNow));
                            }
                            else Thread.Sleep(100);
                        }
                    }
                    catch (MessageQueueException ex)
                    {
                        if (ex.MessageQueueErrorCode != MessageQueueErrorCode.IOTimeout) throw;
                    }
                    Dispatch(room.Expire(DateTime.UtcNow));
                    ProcessQueueCleanups(DateTime.UtcNow);
                }
                Dispatch(room.Shutdown());
                ProcessQueueCleanups(DateTime.MaxValue);
            }
            catch (Exception ex)
            {
                running = false;
                if (Failed != null) Failed(ex.Message);
            }
            finally { queue.Dispose(); }
        }

        public void Dispose()
        {
            running = false;
            if ((worker.ThreadState & ThreadState.Unstarted) != 0)
                queue.Dispose();
            else if (Thread.CurrentThread != worker) worker.Join(2000);
            // Серверная очередь сохраняется для изучения асинхронного обмена.
        }
    }
}
