using System;
using System.Messaging;

namespace MSMQChat
{
    public static class QueueTransport
    {
        public static MessageQueue OpenLocal(string name)
        {
            if (!ChatProtocol.ValidQueueName(name)) throw new ArgumentException("Неверное имя очереди.");
            string path = ".\\private$\\" + name;
            if (!MessageQueue.Exists(path))
                using (MessageQueue created = MessageQueue.Create(path, false))
                    created.Label = "Учебный чат MSMQ";
            MessageQueue queue = new MessageQueue(path, true, false, QueueAccessMode.Receive);
            queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(ChatPacket) });
            queue.MessageReadPropertyFilter.Body = true;
            queue.MessageReadPropertyFilter.Id = true;
            queue.MessageReadPropertyFilter.Label = true;
            return queue;
        }

        public static void Send(string address, ChatPacket packet)
        {
            using (MessageQueue queue = new MessageQueue(ChatProtocol.NormalizeAddress(address), QueueAccessMode.Send))
            using (Message message = new Message())
            {
                message.Formatter = new XmlMessageFormatter(new Type[] { typeof(ChatPacket) });
                message.Body = packet;
                message.Label = "MSMQChat " + packet.Kind;
                message.Recoverable = true;
                message.UseDeadLetterQueue = true;
                message.TimeToReachQueue = TimeSpan.FromMinutes(2);
                message.TimeToBeReceived = TimeSpan.FromMinutes(2);
                queue.Send(message);
            }
        }
    }
}
