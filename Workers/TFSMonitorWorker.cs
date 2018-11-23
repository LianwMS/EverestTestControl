using System;
using System.Collections.Generic;
using System.Threading;

// RunTime Test not call the code due to have moved IR Worker agent code from IS branch to HDIS branch
namespace EverestTest
{
    internal class TFSMonitorWorker : Worker
    {
        private const string TFSBUILDFILE = "TFSBuilds.config";
        private const int TFS_MONITOR_INTERVAL = 900000; // 15 mins

        private List<BuildInfo> tfsBuilds;

        public TFSMonitorWorker(List<BuildInfo> builds)
            : base()
        {
            tfsBuilds = builds;
        }

        protected override void DoWork(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                Console.WriteLine("Fetching new builds from TFS");
                if (TFSHelper.TryGetNewBuildFromTFS(tfsBuilds))
                {
                    Console.WriteLine("Found new builds");
                    TFSHelper.WriteTFSBuildToFile(TFSBUILDFILE, tfsBuilds);
                }
                else
                {
                    Console.WriteLine("No new builds");
                }
                Thread.Sleep(TFS_MONITOR_INTERVAL);
            }
        }
    }
}
