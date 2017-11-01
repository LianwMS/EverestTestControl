using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EverestTest
{
    internal class TestWorker : Worker
    {
        private const int TEST_WAIT_INTERVAL = 5000;
        private const int TEST_MONITOR_INTERVAL = 60000;

        private const string TFSBUILDFILE = "TFSBuilds.config";

        private List<BuildInfo> tfsBuilds;

        public TestWorker(List<BuildInfo> builds)
        {
            tfsBuilds = builds;
        }

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
                        TFSHelper.WriteTFSBuildToFile(TFSBUILDFILE, tfsBuilds);
                        Console.WriteLine("Prepare test for {0}", build.TFSBuildNumber);
                    }
                    else if (build.TestStatus == TestStatus.TestPrepare)
                    {
                        string tag = build.ImageTag;
                        if (tag == null)
                        {
                            if (!TestHelper.BuildDockerImage(build.DropFolder, out tag))
                            {
                                build.TestStatus = TestStatus.Finished;
                                TFSHelper.WriteTFSBuildToFile(TFSBUILDFILE, tfsBuilds);
                                continue;
                            }
                            build.ImageTag = tag;
                            TFSHelper.WriteTFSBuildToFile(TFSBUILDFILE, tfsBuilds);
                        }
                        Console.WriteLine("Image tag for {0} is {1}", build.TFSBuildNumber, build.ImageTag);

                        build.TestStartTime = DateTimeOffset.Now;
                        build.TestTaskId = TestHelper.StartTest(0, tag);
                        build.TestStatus = TestStatus.Testing;
                        TFSHelper.WriteTFSBuildToFile(TFSBUILDFILE, tfsBuilds);
                        Console.WriteLine("Start testing {0} taskId = {1}", build.TFSBuildNumber, build.TestTaskId);
                    }
                    else if (build.TestStatus == TestStatus.Testing)
                    {
                        if (TestHelper.CheckFinished(build.TestTaskId))
                        {
                            build.TestFinishedTime = DateTimeOffset.Now;
                            build.TestStatus = TestStatus.Finished;
                            TFSHelper.WriteTFSBuildToFile(TFSBUILDFILE, tfsBuilds);
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

        public BuildInfo FindNextTestNotFinished()
        {
            lock (tfsBuilds)
            {
                return tfsBuilds.FirstOrDefault(b => b.TestStatus != TestStatus.Finished);
            }
        }
    }
}
