using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace EverestTest
{
    public class Program
    {
        private const int TEST_MONITOR_INTERVAL = 60000;
        private const string TFSBUILDFILE = "TFSBuilds.config";

        private static List<BuildInfo> tfsBuilds;

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            if (args.Length == 0)
            {
                TFSHelper.LoadTFSBuildFromFile(TFSBUILDFILE, out tfsBuilds);
                TFSMonitorWorker tfsMonitor = new TFSMonitorWorker(tfsBuilds);
                tfsMonitor.Start();
                TestWorker testWorker = new TestWorker(tfsBuilds);
                testWorker.Start();
            }
            else
            {
                string dropFolder = args[0];
                string tag;
                Console.WriteLine("Test triggered for {0}", dropFolder);
                Console.WriteLine("Start Time: {0}", DateTimeOffset.Now);
                if (!TestHelper.BuildDockerImage(dropFolder, out tag))
                {
                    return;
                }
                Console.WriteLine("Image tag is {0}", tag);

                var taskIds = TestHelper.StartTests(tag);

                taskIds.ForEach(i => Console.WriteLine("Start Testing: {0}", i));

                var stop = false;
                while (!stop)
                {
                    stop = true;
                    foreach (Guid taskId in taskIds)
                    {
                        bool result;
                        if (result = TestHelper.CheckFinished(taskId))
                        {
                            Console.WriteLine("Test task {0} completed", taskId);
                        }
                        stop = stop && result;
                    }
                    Thread.Sleep(TEST_MONITOR_INTERVAL);
                }
                Console.WriteLine("Completed!!!");
            }
        }
    }
}
