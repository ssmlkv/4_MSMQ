using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using MSMQChat;

namespace MSMQ
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string name = args.Length == 0 ? "ServerQueue_" + Process.GetCurrentProcess().Id : args[0];
            if (args.Length > 1 || !ChatProtocol.ValidQueueName(name))
            { MessageBox.Show("Использование: MSMQServer.exe [имя_очереди]", "Сервер MSMQ"); return; }
            bool acquired;
            using (Mutex mutex = new Mutex(true, "Global\\MSMQChat_" + name.ToLowerInvariant(), out acquired))
            {
                if (!acquired)
                { MessageBox.Show("Эта очередь уже обслуживается другим сервером.", "Сервер MSMQ"); return; }
                try { Application.Run(new frmMain(name)); }
                catch (Exception ex)
                { MessageBox.Show("Не удалось запустить сервер. Проверьте установку MSMQ и права очереди.\n" + ex.Message, "Сервер MSMQ"); }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
