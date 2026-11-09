using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace MSMQChat
{
    // Один и тот же XML-контракт подключён в оба проекта.
    public class ChatPacket
    {
        public string Kind { get; set; }
        public string SessionId { get; set; }
        public string Nick { get; set; }
        public string ReplyQueue { get; set; }
        public string Text { get; set; }
        public DateTime ExpiresUtc { get; set; }

        public ChatPacket() { }

        public ChatPacket(string kind, string sessionId, string nick, string text)
        {
            Kind = kind;
            SessionId = sessionId;
            Nick = nick;
            Text = text;
        }
    }

    public static class ChatProtocol
    {
        public const int MaxNickLength = 24;
        public const int MaxTextLength = 4000;
        public const int JoinTimeoutSeconds = 8;
        public const int HeartbeatSeconds = 5;
        public const int LeaseSeconds = 30;

        public static string ValidateNick(string nick)
        {
            if (String.IsNullOrEmpty(nick) || nick.Trim() != nick)
                return "Введите ник без пробелов в начале и конце.";
            if (nick.Length > MaxNickLength)
                return "Ник должен содержать не более 24 символов.";
            foreach (char c in nick)
                if (Char.IsControl(c) || c == ',' || c == '|' || c == '<' || c == '>')
                    return "Ник содержит служебные символы.";
            return null;
        }

        public static bool IsSession(string session)
        {
            return session != null && Regex.IsMatch(session, "\\A[a-fA-F0-9]{32}\\z");
        }

        public static bool ValidQueueName(string name)
        {
            return name != null && Regex.IsMatch(name, "\\A[A-Za-z0-9_-]{1,80}\\z");
        }

        // Exists используется только владельцем локальной очереди, не отправителем.
        public static string NormalizeAddress(string input)
        {
            string value = (input ?? "").Trim();
            string protocol = "OS";
            const string osPrefix = "FormatName:DIRECT=OS:";
            const string tcpPrefix = "FormatName:DIRECT=TCP:";
            if (value.StartsWith(osPrefix, StringComparison.OrdinalIgnoreCase))
                value = value.Substring(osPrefix.Length);
            else if (value.StartsWith(tcpPrefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(tcpPrefix.Length);
                protocol = "TCP";
            }
            string[] parts = value.Split('\\');
            if (parts.Length != 3 || !String.Equals(parts[1], "private$", StringComparison.OrdinalIgnoreCase)
                || !ValidQueueName(parts[2]))
                throw new ArgumentException("Укажите адрес COMPUTER\\private$\\QueueName.");
            string host = parts[0];
            if (host == "." || String.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                host = Environment.MachineName;
            if (!Regex.IsMatch(host, "\\A[A-Za-z0-9][A-Za-z0-9.-]{0,252}\\z"))
                throw new ArgumentException("Неверное имя компьютера в адресе очереди.");
            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                    throw new ArgumentException("Используйте имя компьютера или IPv4-адрес.");
                protocol = "TCP";
            }
            else if (protocol == "TCP")
                throw new ArgumentException("Для DIRECT=TCP требуется IPv4-адрес.");
            return "FormatName:DIRECT=" + protocol + ":" + host + "\\private$\\" + parts[2];
        }

        public static bool IsClientAddress(string address)
        {
            try
            {
                string normalized = NormalizeAddress(address);
                string queue = normalized.Substring(normalized.LastIndexOf('\\') + 1);
                return queue.StartsWith("MSMQChatClient_", StringComparison.Ordinal)
                    && IsSession(queue.Substring("MSMQChatClient_".Length));
            }
            catch (ArgumentException) { return false; }
        }

        // Удалять можно только персональную очередь на этом компьютере.
        // Очередь удалённого клиента удаляется самим клиентом по команде Kicked.
        public static string LocalClientQueuePath(string address)
        {
            if (!IsClientAddress(address))
                throw new ArgumentException("Удалять можно только персональную очередь клиента.");
            string normalized = NormalizeAddress(address);
            string[] parts = normalized.Split('\\');
            string host = parts[0].Substring(parts[0].LastIndexOf(':') + 1);
            bool local = String.Equals(host, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
            IPAddress ip;
            if (!local && IPAddress.TryParse(host, out ip))
            {
                local = IPAddress.IsLoopback(ip);
                if (!local)
                {
                    try
                    {
                        foreach (IPAddress own in Dns.GetHostAddresses(Environment.MachineName))
                            if (own.Equals(ip)) { local = true; break; }
                    }
                    catch (SocketException) { }
                }
            }
            return local ? ".\\private$\\" + parts[2] : null;
        }
    }
}
