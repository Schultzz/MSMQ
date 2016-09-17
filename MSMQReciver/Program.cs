using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSMQReciver
{
    class MSMQReciver
    {
        private MessageQueue reuquestQueue;
        private MessageQueue invalidMessageQueue;

        public MSMQReciver()
        {
            this.reuquestQueue = SetupChannel(@".\Private$\Request");
            this.invalidMessageQueue = SetupChannel(@".\Private$\InvalidMessage");

            reuquestQueue.MessageReadPropertyFilter.SetAll();
            ((XmlMessageFormatter)reuquestQueue.Formatter).TargetTypeNames = new string[] { "System.String,mscorlib" };

            //Start replyWatcher-Thread
            AddWatchingThread(reuquestQueue);
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

        public Thread AddWatchingThread(MessageQueue reuquestQueue)
        {
            Thread Watcher =
                new Thread(() => AsyncWatchQueue(reuquestQueue));
            Watcher.Start();
            return Watcher;
        }

        public void AsyncWatchQueue(MessageQueue reuquestQueue)
        {
            Message newMessage = null;

            if (reuquestQueue == null)
                return;

            try
            {
                if (reuquestQueue.CanRead)
                {
                    while (true)
                    {
                        newMessage = reuquestQueue.Receive();

                        ConsoleWrite(newMessage.Body.ToString(), newMessage.Label, newMessage.Id, 0);

                        //main-thread should do this. 
                        var repsonseMsg = QueueReplier(newMessage);

                        ConsoleWrite(repsonseMsg.Body.ToString(), repsonseMsg.Label, repsonseMsg.Id, 1);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }

            catch (
                ThreadAbortException e
                )
            {
                Console.WriteLine(string.Format("[{0}] ({1}) {2}", DateTime.Now.ToString("HH:mm:ss"), "Error!",
                    e.Message));
            }
            finally
            {
                reuquestQueue.Dispose();
            }
        }

        private Message QueueReplier(Message message)
        {
            MessageQueue replayMessageQueue = message.ResponseQueue;

            //Do logic for student

            Message responseMessage = new Message();
            responseMessage.Body = randomeChooser();
            responseMessage.Label = "CPHBusiness";
            responseMessage.CorrelationId = message.Id;

            replayMessageQueue.Send(responseMessage);


            return responseMessage;
        }


        public string randomeChooser()
        {
            Random random = new Random();

            if (random.Next(0, 100)%2 == 0)
                return "Good news!";

            return "Bad news!";
        }

        private void ConsoleWrite(string message, string label, string id, int level)
        {
            switch (level)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(string.Format("[{0}] << correlation-id:{1} ({2}) - {3}",
                        DateTime.Now.ToString("HH:mm:ss"), id, label, message));
                    return;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(string.Format("[{0}] >> msg-id:{1} ({2}) - {3}",
                        DateTime.Now.ToString("HH:mm:ss"), id, label, message));
                    return;
            }
    
        }

        static void Main(string[] args)
        {
            MSMQReciver msmq = new MSMQReciver();

            while (true)
            {
                Console.WriteLine("Processing other stuff..");
                Thread.Sleep(5000);
            }
        }
    }
}