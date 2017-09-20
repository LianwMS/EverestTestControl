using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace EverestTest
{
    public class TFSHelper
    {
        private const string TFSSERVER = "http://sqlbuvsts01:8080/";
        private const string PROJECTNAME = "SQL Server";
        private const string BUILDDEFINITIONNAME = "[O][CB][IS_Scale][Box][GB][05]";

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

        public static void TestWrite()
        {
            var details = GetSuccessfulBuildDetails();
            TFSBuild[] builds = new TFSBuild[details.Count()];
            string[] strs = new string[details.Count()];
            for (int i = 0; i < details.Count(); ++i)
            {
                var detail = details[i];
                builds[i] = new TFSBuild()
                {
                    TFSBuildNumber = detail.BuildNumber,
                    BuildFinishedTime = new DateTimeOffset(detail.FinishTime),
                    BuildStatus = detail.Status.ToString(),
                    TestStatus = TestStatus.Finished,
                    TestStartTime = new DateTimeOffset(0, new TimeSpan(0)),
                    TestFinishedTime = new DateTimeOffset(0, new TimeSpan(0)),
                };
                strs[i] = JsonConvert.SerializeObject(builds[i]);
                Console.WriteLine(strs[i]);
            }

            File.WriteAllLines("TFSBuilds.config", strs);

        }
    }
}
