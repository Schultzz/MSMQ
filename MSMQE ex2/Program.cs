using System;
using System.Linq;
using System.Messaging;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace QueueApplication
{
    class Demo
    {
        private MessageQueue reuquestQueue;
        private MessageQueue replyQueue;
        private MessageQueue invalidMessageQueue;

        public Demo()
        {
            this.reuquestQueue = SetupChannel(@".\Private$\Request");
            this.replyQueue = SetupChannel(@".\Private$\Reply");
            this.invalidMessageQueue = SetupChannel(@".\Private$\InvalidMessage");

            replyQueue.MessageReadPropertyFilter.SetAll();
            ((XmlMessageFormatter)replyQueue.Formatter).TargetTypeNames = new string[] { "System.String,mscorlib" };


            //Start replyWatcher-Thread
            AddWatchingThread(replyQueue);
        }


        private void SendRequestQueue(string message, string label)
        {
            Message msg = new Message();
            msg.ResponseQueue = replyQueue;
            msg.Body = message;
            msg.Label = label;
            reuquestQueue.Send(msg);
            ConsoleWrite(message, label, msg.Id, 0);
            Console.ForegroundColor = ConsoleColor.White;
        }

        
        private void ConsoleWrite(string message, string label, string correlationId, int level)
        {
            switch (level)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(string.Format("[{0}] >> msg-id:{1} ({2}) - {3}",
                        DateTime.Now.ToString("HH:mm:ss"), correlationId, label, message));
                    return;
                case 1:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(string.Format("[{0}] << correlation-id:{1} ({2}) - {3}",
                        DateTime.Now.ToString("HH:mm:ss"), correlationId, label, message));
                    return;
            }
        }

        private MessageQueue SetupChannel(string queueName)
        {
            MessageQueue newQueue = null;

            if (!MessageQueue.Exists(queueName))
                newQueue = MessageQueue.Create(queueName);
            else
            {
                newQueue = new MessageQueue(queueName);
            }
            return newQueue;
        }

        public Thread AddWatchingThread(MessageQueue replyQueue)
        {
            Thread Watcher =
                new Thread(() => AsyncWatchQueue(replyQueue));
            Watcher.Start();
            return Watcher;
        }

        public void AsyncWatchQueue(MessageQueue replyQueue)
        {
            Message newMessage = null;

            if (replyQueue == null)
                return;

            try
            {
                if (replyQueue.CanRead)
                {
                    while (true)
                    {
                        newMessage = replyQueue.Receive();

                        var message = newMessage.Body.ToString();
                        var label = newMessage.Label;
                        var correlationId = newMessage.CorrelationId;

                        ConsoleWrite(message, label, correlationId, 1);
                    }
                }
            }

            catch (ThreadAbortException e)
            {
                Console.WriteLine(string.Format("[{0}] ({1}) {2}", DateTime.Now.ToString("HH:mm:ss"), "Error!",
                    e.Message));
            }
            finally

            {
                replyQueue.Dispose();
            }
        }

        static void Main(string[] args)
        {
            Demo d = new Demo();
            Console.Write("Payload:");
            while (true)
            {
                d.SendRequestQueue(Console.ReadLine(), "Joe Doe");
            }
        }
    }
}