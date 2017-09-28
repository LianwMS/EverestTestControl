using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;

namespace EverestTest
{
    public class Program
    {
        private const int TFS_MONITOR_INTERVAL = 60000;
        private const int TEST_WAIT_INTERVAL = 5000;
        private const int TEST_MONITOR_INTERVAL = 60000;

        private const string TFSBUILDFILE = "TFSBuilds.config";

        private static List<BuildInfo> tfsBuilds;

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            if (args.Length == 0)
            {
                LoadTFSBuildFromFile(TFSBUILDFILE);
                TFSMonitorWorker tfsMonitor = new TFSMonitorWorker();
                tfsMonitor.Start();
                TestWorker testWorker = new TestWorker();
                testWorker.Start();
            }
            else
            {
                string dropFolder = args[0];
                string tag;
                Console.WriteLine("Test triggered for {0}", dropFolder);
                Console.WriteLine("Start Time: {0}", DateTimeOffset.Now);
                TestHelper.BuildDockerImage(dropFolder, dropFolder, out tag);
                Console.WriteLine("Image tag is {0}", tag);

                Guid taskId = TestHelper.StartTest(tag);
                Console.WriteLine("Start testing {0}", taskId);
                while (!TestHelper.CheckFinished(taskId))
                {
                    Thread.Sleep(TEST_MONITOR_INTERVAL);
                }
                Console.WriteLine("Test finished");
                Console.WriteLine("Finish Time: {0}", DateTimeOffset.Now);
            }
        }
        
        private static string GenerateFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        #region TFS
        private static void LoadTFSBuildFromFile(string fileName)
        {
            var strs = File.ReadAllLines(GenerateFilePath(fileName));
            tfsBuilds = strs.Select(str => JsonConvert.DeserializeObject<BuildInfo>(str)).ToList();
        }

        private static void WriteTFSBuildToFile(string fileName)
        {
            string[] strs;
            lock (tfsBuilds)
            {
                strs = tfsBuilds.Select(b => JsonConvert.SerializeObject(b)).ToArray();
                File.WriteAllLines(fileName, strs);
            }
        }

        private static bool TryGetNewBuildFromTFS()
        {
            bool result = false;
            var details = TFSHelper.GetSuccessfulBuildDetails();

            foreach (var detail in details)
            {
                var isIncluded = tfsBuilds.Where(t => t.TFSBuildNumber.Equals(detail.BuildNumber)).Count();
                if (isIncluded == 0)
                {
                    Console.WriteLine("Found: {0}", detail.BuildNumber);
                    lock (tfsBuilds)
                    {
                        tfsBuilds.Add(new BuildInfo()
                        {
                            TFSBuildNumber = detail.BuildNumber,
                            DropFolder = detail.DropLocation,
                            BuildStatus = detail.Status.ToString(),
                            BuildFinishedTime = detail.FinishTime,
                            TestStatus = TestStatus.NotStart,
                        });
                    }
                    result = true;
                }
            }
            return result;
        }
        
        class TFSMonitorWorker : Worker
        {
            protected override void DoWork(CancellationToken cancel)
            {
                while (!cancel.IsCancellationRequested)
                {
                    Console.WriteLine("Fetching new builds from TFS");
                    if (TryGetNewBuildFromTFS())
                    {
                        Console.WriteLine("Found new builds");
                        WriteTFSBuildToFile(TFSBUILDFILE);
                    }
                    else
                    {
                        Console.WriteLine("No new builds");
                    }
                    Thread.Sleep(TFS_MONITOR_INTERVAL);
                }
            }
        }
        #endregion TFS

        #region Meri
        class TestWorker : Worker
        {
            protected override void DoWork(CancellationToken cancel)
            {
                while (!cancel.IsCancellationRequested)
                {
                    try
                    {
                        var build = FindNextTestNotFinished();
                        if (build == null)
                        {
                            Thread.Sleep(TEST_WAIT_INTERVAL);
                        }
                        else if (build.TestStatus == TestStatus.NotStart)
                        {
                            build.TestStatus = TestStatus.TestPrepare;
                            WriteTFSBuildToFile(TFSBUILDFILE);
                            Console.WriteLine("Prepare test for {0}", build.TFSBuildNumber);
                        }
                        else if (build.TestStatus == TestStatus.TestPrepare)
                        {
                            string tag = build.ImageTag;
                            if (tag == null)
                            {
                                TestHelper.BuildDockerImage(build.DropFolder, build.DropFolder, out tag);
                                build.ImageTag = tag;
                                WriteTFSBuildToFile(TFSBUILDFILE);
                            }
                            Console.WriteLine("Image tag for {0} is {1}", build.TFSBuildNumber, build.ImageTag);

                            build.TestStartTime = DateTimeOffset.Now;
                            build.TestTaskId = TestHelper.StartTest(tag);
                            build.TestStatus = TestStatus.Testing;
                            WriteTFSBuildToFile(TFSBUILDFILE);
                            Console.WriteLine("Start testing {0} taskId = {1}", build.TFSBuildNumber, build.TestTaskId);
                        }
                        else if (build.TestStatus == TestStatus.Testing)
                        {
                            if (TestHelper.CheckFinished(build.TestTaskId))
                            {
                                build.TestFinishedTime = DateTimeOffset.Now;
                                build.TestStatus = TestStatus.Finished;
                                WriteTFSBuildToFile(TFSBUILDFILE);
                                Console.WriteLine("{0} finished", build.TFSBuildNumber);
                            }
                            else
                            {
                                Thread.Sleep(TEST_MONITOR_INTERVAL);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Thread.Sleep(TEST_WAIT_INTERVAL);
                    }
                }
            }
        }
        
        static BuildInfo FindNextTestNotFinished()
        {
            lock (tfsBuilds)
            {
                return tfsBuilds.FirstOrDefault(b => b.TestStatus != TestStatus.Finished);
            }
        }
        #endregion Meri
    }
}
