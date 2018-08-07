﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace EverestTest
{
    internal class TestHelper
    {
        private const string IMAGE_TAG_TIP = "Image Tag: ";

        private const string BUILD_PS = @"DockerHelper\build.ps1";

        private const string TEST_ORGANIZER_EXE = @"MeriClient\LoadTestFramework.TestOrganizer.exe";
        private const string TEST_CONFIG = @"MeriTestConfig.xml";

        private const string MERI_URL = "https://meri.cloudapp.net";

        private static string meriToken = null;

        public static bool BuildDockerImage(string workerDropFolder, out string tag)
        {
            string psScript = string.Empty;
            string psScriptPath = GenerateFilePath(BUILD_PS);

            Console.WriteLine("Start to run build ps1");

            string tagOut = string.Empty;
            int result = RunPSScript(
                psScriptPath,
                new Dictionary<string, string>()
                {
                    { "dropFolder", workerDropFolder }
                },
                data =>
                {
                    if (data == null) return;
                    Console.WriteLine(data);
                    if (data.StartsWith(IMAGE_TAG_TIP))
                    {
                        tagOut = data.Substring(IMAGE_TAG_TIP.Length);
                    }
                });

            tag = tagOut;
            if (result == 0)
            {
                Console.WriteLine("Docker image build and upload finished");
                return true;
            }
            else
            {
                Console.WriteLine("Docker image build and upload failed");
                return false;
            }
        }

        public static bool UploadDBDll()
        {
            return true;
        }

        public static List<Guid> StartTests(string currentContainerImageTag, bool isFull = false)
        {
            var taskIds = new List<Guid>();

            var taskId1 = StartTest(0, currentContainerImageTag);
            taskIds.Add(taskId1);

            if (isFull)
            {
                var taskId2 = StartTest(1, currentContainerImageTag, GetCurrentDBProvisionScript());
                taskIds.Add(taskId2);

                var taskId3 = StartTest(2, GetCurrentProductImageTag(), GetLatestDBProvisionScript(), currentContainerImageTag);
                taskIds.Add(taskId3);
            }

            return taskIds;
        }

        public static Guid StartTest(int testType, string containerImageTag, string dbScript = null, string dbImageVersion = null)
        {
            XmlDocument testConfig = new XmlDocument();
            testConfig.Load(GenerateFilePath(TEST_CONFIG));

            foreach (var node in testConfig.SelectNodes(@"//Configuration/Meri/Parameters/Parameter").Cast<XmlNode>())
            {
                switch (node.Attributes["key"].Value)
                {
                    case "ContainerImageTag":
                        node.Attributes["value"].Value = containerImageTag;
                        break;
                    case "DBScript":
                        if (!string.IsNullOrEmpty(dbScript))
                        {
                            node.Attributes["value"].Value = dbScript;
                        }
                        break;
                    case "DBImageVersion":
                        if (!string.IsNullOrEmpty(dbImageVersion))
                        {
                            node.Attributes["value"].Value = dbImageVersion;
                        }
                        break;
                    case "TestType":
                        node.Attributes["value"].Value = testType.ToString();
                        break;
                    default:
                        break;
                }
            }

            testConfig.SelectSingleNode("//Name").InnerText = string.Format("Everest({0}){1}-{2}", testType, containerImageTag, dbScript);

            string tempConfigFile = Path.GetTempFileName();
            testConfig.Save(tempConfigFile);

            Guid taskId = Guid.Empty;
            Console.WriteLine("{0} {1}", GenerateFilePath(TEST_ORGANIZER_EXE), $"\"{tempConfigFile}\" 1");
            // client will stay for 1 min
            int exitCode = RunCommand(GenerateFilePath(TEST_ORGANIZER_EXE), $"\"{tempConfigFile}\" 1", data =>
            {
                if (data == null) return;
                const string taskIdTip = "Task Id = ";
                int pos = data.IndexOf(taskIdTip);
                if (pos != -1)
                {
                    taskId = Guid.Parse(data.Substring(pos + taskIdTip.Length));
                }
            });
            File.Delete(tempConfigFile);

            if (taskId == Guid.Empty)
            {
                Console.WriteLine("Task Id is not found.");
            }
            else
            {
                Console.WriteLine("Task Id = {0}", taskId);
            }

            return taskId;
        }

        private static string GetMeriToken()
        {
            if (meriToken == null)
            {
                XmlDocument testConfig = new XmlDocument();
                testConfig.Load(GenerateFilePath(TEST_CONFIG));
                meriToken = testConfig.SelectSingleNode("//Token").InnerText;
            }
            return meriToken;
        }

        public static bool CheckFinished(Guid taskId)
        {
            try
            {
                Meri.SDK.Service.AzureMeriService ams = new Meri.SDK.Service.AzureMeriService(new Uri(MERI_URL), GetMeriToken());
                long running = 0;
                long passed = 0, failed = 0;
                foreach (var item in ams.GetWorkItems(taskId))
                {
                    if (item.Name == "Meri.Aggregator") continue;
                    foreach (var run in ams.GetWorkItemRuns(taskId, item.Id))
                    {
                        if (run.State == Meri.SDK.ObjectModel.WorkItemRunState.Initializing || run.State == Meri.SDK.ObjectModel.WorkItemRunState.Running)
                        {
                            running++;
                        }
                        if (run.RunResult != null)
                        {
                            passed += run.RunResult.Passed;
                            failed += run.RunResult.Failed;
                        }
                    }
                }
                Console.WriteLine("Task {0} has {1} items running; passed {2}, failed {3}", taskId, running, passed, failed);
                return running == 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static string GenerateFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        private static int RunPSScript(string scriptPath, Dictionary<string, string> arguments, OutputHandler stdoutHandler = null, OutputHandler stderrHandler = null)
        {
            string psArgs = string.Join(" ", arguments.Select(pair => string.Format("-{0} '{1}'", pair.Key, pair.Value.Replace("'", "''"))));
            return RunCommand("powershell.exe", string.Format("-Command \"& '{0}' {1}\"", scriptPath, psArgs.Replace("\"", "\"\"")), stdoutHandler, stderrHandler);
        }

        delegate void OutputHandler(string data);
        private static int RunCommand(string fileName, string args, OutputHandler stdoutHandler = null, OutputHandler stderrHandler = null)
        {
            if (stdoutHandler == null)
            {
                stdoutHandler = Console.WriteLine;
            }
            if (stderrHandler == null)
            {
                stderrHandler = Console.Error.WriteLine;
            }
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += (sender, e) => stdoutHandler(e.Data);
            p.ErrorDataReceived += (sender, e) => stderrHandler(e.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode;
        }

        private static string GetCurrentProductImageTag()
        {
            // Sample: 14.0.900.5478.3272278
            return AzureFileHelper.GetProductBackendWorkerVersion();
        }

        private static string GetCurrentDBProvisionScript()
        {
            // Sample: "ProvisionToV6_0.sql"
            return string.Format("ProvisionToV{0}.sql", AzureFileHelper.GetProductDBVersion());
        }

        private static string GetLatestDBProvisionScript()
        {
            return "ProvisionToV17.0.sql";
        }
    }
}
