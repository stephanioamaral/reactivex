using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveXTutorial
{
    public class Spider
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private Random random = new Random();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private int Length;
        private int ThreadNumber;
        
        private CancellationTokenSource CancelSource;

        private Action<string> ParseAction;
        private Action<string> PersistAction;        

        private IDisposable ParseEvent;
        private IDisposable PersistEvent;

        public Spider(int length, int threads, CancellationTokenSource cancelSource)
        {
            Length = length;
            ThreadNumber = threads;
            CancelSource = cancelSource;

            IScheduler scheduler = Scheduler.Default;

            var parseObservable = Observable.FromEvent<string>(e => ParseAction += e, e => ParseAction -= e);  

            var persistObservable = Observable.FromEvent<string>(e => PersistAction += e, e => PersistAction -= e)
                                              .Buffer(TimeSpan.FromMilliseconds(10000), 5, scheduler);

            ParseEvent = parseObservable.Subscribe(x => Parse(x));

            PersistEvent = persistObservable.Subscribe(x => Persist(x));                      
        }       

        public void RunSpider()
        {
            for (int i = 0; i < ThreadNumber; i++)
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        while (true)
                        {
                            CancelSource.Token.ThrowIfCancellationRequested();

                            Capture();

                            int number = random.Next(1, 5) * 1000;

                            Thread.Sleep(number);
                        }
                    }
                    catch (Exception)
                    {
                        logger.Info($"Cancelation requested [Thread {Thread.CurrentThread.ManagedThreadId}]");
                        Destroy();
                    }
                });
            }            
        }

        private void Capture()
        {            
            var value = new string(Enumerable.Repeat(Chars, Length).Select(s => s[random.Next(s.Length)]).ToArray());
            logger.Info($"Captured: {value} [Thread {Thread.CurrentThread.ManagedThreadId}]");
            ParseAction?.Invoke(value);
        }

        private void Parse(string item)
        {
            logger.Info($"Parsing: {item}");
            PersistAction?.Invoke(item);
        }

        private void Persist(IList<string> list)
        {
            if(list.Count() > 0)
                logger.Info($"Persisting: [{string.Join(",", list.ToArray())}]");
        }

        private void Destroy()
        {
            logger.Info($"Disposing events [Thread {Thread.CurrentThread.ManagedThreadId}]");
            ParseEvent?.Dispose();
            PersistEvent?.Dispose();
        }
    }
}
