using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace EverestTest
{
    public class Program
    {
        private const int TEST_MONITOR_INTERVAL = 60000;

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            if (args.Length != 0)
            {
                string[] options = args;

                if (options[0] == "-Run")
                {
                    string sourceTag = options[1];
                    string fullOption = options[2];
                    bool isfull = fullOption == "-all" ? true : false;

                    string tag;

                    Console.WriteLine("Start Time: {0}", DateTimeOffset.Now);
                    if (!TestHelper.BuildDockerImage(sourceTag, out tag))
                    {
                        return;
                    }
                    Console.WriteLine("Image tag is {0}", tag);

                    var taskIds = TestHelper.StartTests(tag, isfull);

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
}
