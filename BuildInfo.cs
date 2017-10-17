using System;

namespace EverestTest
{
    public class BuildInfo
    {
        public string TFSBuildNumber { get; set; }

        public string DropFolder { get; set; }

        public string BuildStatus { get; set; }

        public DateTimeOffset BuildFinishedTime { get; set; }

        public string ImageTag { get; set; }

        public TestStatus TestStatus { get; set; }

        public Guid TestTaskId { get; set; }

        public DateTimeOffset TestStartTime { get; set; }

        public DateTimeOffset TestFinishedTime { get; set; }
    }
}
