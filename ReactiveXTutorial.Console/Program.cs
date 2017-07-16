using NLog;
using System;
using System.Threading;

namespace ReactiveXTutorial
{
    public class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            logger.Info("Starting application");

            CancellationTokenSource cancelSource = new CancellationTokenSource();

            Spider spider = new Spider(20, 2, cancelSource);
            spider.RunSpider();

            logger.Info("Press any key to stop");
            Console.ReadKey();

            cancelSource.Cancel();

            logger.Info("Press any key to exit");
            Console.ReadKey();
        }
    }
}