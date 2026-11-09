using System;
using System.Collections.Generic;

namespace MSMQChat
{
    public sealed class ChatMember
    {
        public string SessionId { get; private set; }
        public string Nick { get; private set; }
        public string Address { get; private set; }

        public ChatMember(string sessionId, string nick, string address)
        { SessionId = sessionId; Nick = nick; Address = address; }

        public override string ToString() { return Nick; }
    }

    public class Delivery
    {
        public string Address;
        public ChatPacket Packet;
        public Delivery(string address, ChatPacket packet) { Address = address; Packet = packet; }
    }

    public class RoomResult
    {
        public readonly List<Delivery> Deliveries = new List<Delivery>();
        public readonly List<string> Log = new List<string>();
        public readonly List<ChatMember> RemovedMembers = new List<ChatMember>();
        public bool MembersChanged;
    }

    // Все методы вызываются одним приёмным потоком сервера.
    public class ChatRoom
    {
        private class Participant
        {
            public string Session, Nick, Address;
            public DateTime SeenUtc;
        }
        private readonly Dictionary<string, Participant> clients = new Dictionary<string, Participant>();
        private readonly HashSet<string> removedSessions = new HashSet<string>();

        public string[] Members()
        {
            List<string> names = new List<string>();
            foreach (Participant p in clients.Values) names.Add(p.Nick);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names.ToArray();
        }

        public ChatMember[] Participants()
        {
            List<ChatMember> result = new List<ChatMember>();
            foreach (Participant p in clients.Values)
                result.Add(new ChatMember(p.Session, p.Nick, p.Address));
            result.Sort(delegate(ChatMember a, ChatMember b)
            { return StringComparer.OrdinalIgnoreCase.Compare(a.Nick, b.Nick); });
            return result.ToArray();
        }

        public RoomResult RemoveUser(string sessionId)
        {
            RoomResult result = new RoomResult();
            Participant target;
            if (sessionId == null || !clients.TryGetValue(sessionId, out target))
            {
                result.Log.Add("Выбранный сеанс уже завершён.");
                return result;
            }
            clients.Remove(sessionId);
            removedSessions.Add(sessionId);
            result.Deliveries.Add(new Delivery(target.Address,
                new ChatPacket("Kicked", target.Session, target.Nick,
                    "Вы удалены из чата администратором сервера.")));
            result.RemovedMembers.Add(new ChatMember(target.Session, target.Nick, target.Address));
            Changed(result, target.Nick + " удалён из чата администратором.");
            return result;
        }

        private static void Reply(RoomResult result, string address, ChatPacket request, string kind, string text)
        {
            result.Deliveries.Add(new Delivery(address,
                new ChatPacket(kind, request.SessionId, request.Nick, text)));
        }

        private void Broadcast(RoomResult result, string kind, string nick, string text)
        {
            foreach (Participant p in clients.Values)
                result.Deliveries.Add(new Delivery(p.Address,
                    new ChatPacket(kind, p.Session, nick, text)));
        }

        private void Changed(RoomResult result, string text)
        {
            result.MembersChanged = true;
            result.Log.Add(text);
            Broadcast(result, "System", null, text);
            Broadcast(result, "Users", null, String.Join(", ", Members()));
        }

        public RoomResult Process(ChatPacket packet, DateTime nowUtc)
        {
            RoomResult result = new RoomResult();
            if (packet == null || !ChatProtocol.IsSession(packet.SessionId))
            {
                result.Log.Add("Пропущен пакет с неверным идентификатором сеанса.");
                return result;
            }
            if (packet.Kind == "Register")
            {
                if (removedSessions.Contains(packet.SessionId))
                {
                    result.Log.Add("Запрос регистрации удалённого сеанса отклонён.");
                    return result;
                }
                if (!ChatProtocol.IsClientAddress(packet.ReplyQueue)
                    || !packet.ReplyQueue.EndsWith(packet.SessionId, StringComparison.Ordinal))
                {
                    result.Log.Add("Пропущен запрос с неверной обратной очередью.");
                    return result;
                }
                string address = ChatProtocol.NormalizeAddress(packet.ReplyQueue);
                string error = ChatProtocol.ValidateNick(packet.Nick);
                if (packet.ExpiresUtc <= nowUtc || packet.ExpiresUtc > nowUtc.AddMinutes(1))
                    error = "Время ожидания входа истекло. Проверьте часы компьютеров.";
                Participant existing;
                if (error == null && clients.TryGetValue(packet.SessionId, out existing))
                {
                    if (existing.Nick == packet.Nick && existing.Address == address)
                    {
                        existing.SeenUtc = nowUtc;
                        Reply(result, address, packet, "Registered", "Вход выполнен.");
                        Reply(result, address, packet, "Users", String.Join(", ", Members()));
                        return result;
                    }
                    error = "Идентификатор сеанса уже используется.";
                }
                foreach (Participant p in clients.Values)
                    if (String.Equals(p.Nick, packet.Nick, StringComparison.OrdinalIgnoreCase))
                        error = "Ник уже занят: " + packet.Nick;
                if (error != null)
                {
                    Reply(result, address, packet, "Error", error);
                    return result;
                }
                clients.Add(packet.SessionId, new Participant {
                    Session = packet.SessionId, Nick = packet.Nick, Address = address, SeenUtc = nowUtc });
                Reply(result, address, packet, "Registered", "Вход выполнен.");
                Changed(result, packet.Nick + " вошёл в чат.");
                return result;
            }
            Participant sender;
            if (!clients.TryGetValue(packet.SessionId, out sender)) return result;
            // Ник берётся из регистрации, поле Nick входящего сообщения не доверенное.
            sender.SeenUtc = nowUtc;
            if (packet.Kind == "Message")
            {
                if (String.IsNullOrEmpty(packet.Text) || packet.Text.Trim().Length == 0
                    || packet.Text.Length > ChatProtocol.MaxTextLength)
                {
                    Reply(result, sender.Address, packet, "Error", "Сообщение должно содержать от 1 до 4000 символов.");
                    return result;
                }
                result.Log.Add(sender.Nick + ": " + packet.Text);
                Broadcast(result, "Chat", sender.Nick, packet.Text);
            }
            else if (packet.Kind == "Heartbeat")
                Reply(result, sender.Address, packet, "Pong", null);
            else if (packet.Kind == "Leave")
            {
                clients.Remove(packet.SessionId);
                Changed(result, sender.Nick + " вышел из чата.");
            }
            return result;
        }

        public RoomResult Expire(DateTime nowUtc)
        {
            RoomResult result = new RoomResult();
            List<string> expired = new List<string>();
            foreach (Participant p in clients.Values)
                if ((nowUtc - p.SeenUtc).TotalSeconds >= ChatProtocol.LeaseSeconds) expired.Add(p.Session);
            foreach (string session in expired)
            {
                string nick = clients[session].Nick;
                clients.Remove(session);
                Changed(result, nick + " отключён по тайм-ауту.");
            }
            return result;
        }

        public RoomResult Shutdown()
        {
            RoomResult result = new RoomResult();
            Broadcast(result, "Stopped", null, "Сервер завершил работу.");
            clients.Clear();
            removedSessions.Clear();
            result.MembersChanged = true;
            return result;
        }
    }
}
