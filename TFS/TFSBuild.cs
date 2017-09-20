using System;

namespace EverestTest
{
    public class TFSBuild
    {
        public string TFSBuildNumber { get; set; }

        public string BuildStatus { get; set; }

        public TestStatus TestStatus { get; set; }

        public DateTimeOffset BuildFinishedTime { get; set; }

        public DateTimeOffset TestStartTime { get; set; }

        public DateTimeOffset TestFinishedTime { get; set; }
    }
}
