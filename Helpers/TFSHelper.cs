using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Newtonsoft.Json;

namespace EverestTest
{
    public class TFSHelper
    {
        private const string TFSSERVER = "http://sqlbuvsts01:8080/";
        private const string PROJECTNAME = "SQL Server";
        private const string BUILDDEFINITIONNAME = "[O][CB][IS_Scale][Box][Std][GB][05]";

        public static IBuildDetail GetLastSuccessfulBuildDetails()
        {
            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(TFSSERVER));
            tfs.Authenticate();

            IBuildServer buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));
            var buildDefinition = buildServer.QueryBuildDefinitions(PROJECTNAME).FirstOrDefault(q => q.Name == BUILDDEFINITIONNAME);
            var builds = buildDefinition.QueryBuilds();

            // string latest = builds.FirstOrDefault().BuildNumber;
            var detail = builds.Where(b => b.Status == BuildStatus.Succeeded).LastOrDefault();

            Console.WriteLine("Latest: " + detail.BuildNumber);
            return detail;
        }

        public static IBuildDetail[] GetSuccessfulBuildDetails()
        {
            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(TFSSERVER));
            tfs.Authenticate();

            IBuildServer buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));
            var buildDefinition = buildServer.QueryBuildDefinitions(PROJECTNAME).FirstOrDefault(q => q.Name == BUILDDEFINITIONNAME);
            var builds = buildDefinition.QueryBuilds();

            // string latest = builds.FirstOrDefault().BuildNumber;
            var details = builds.Where(b => b.Status == BuildStatus.Succeeded).ToArray();

            return details;
        }

        public static void LoadTFSBuildFromFile(string fileName, out List<BuildInfo> tfsBuilds)
        {
            var strs = File.ReadAllLines(GenerateFilePath(fileName));
            tfsBuilds = strs.Select(str => JsonConvert.DeserializeObject<BuildInfo>(str)).ToList();
        }

        public static void WriteTFSBuildToFile(string fileName, List<BuildInfo> tfsBuilds)
        {
            string[] strs;
            lock (tfsBuilds)
            {
                strs = tfsBuilds.Select(b => JsonConvert.SerializeObject(b)).ToArray();
                File.WriteAllLines(fileName, strs);
            }
        }

        public static bool TryGetNewBuildFromTFS(List<BuildInfo> tfsBuilds)
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

        private static string GenerateFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        public static void RefreshTFSBuildsConfig()
        {
            var tfs = new List<BuildInfo>();
            LoadTFSBuildFromFile("TFSBuilds.config", out tfs);
            TryGetNewBuildFromTFS(tfs);
            WriteTFSBuildToFile("TFSBuilds.config", tfs);
        }
    }
}
