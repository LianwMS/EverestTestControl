using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EverestTest
{
    class TestHelper
    {
        private const string IMAGE_TAG_TIP = "Image Tag: ";

        private const string BUILD_PS = @"DockerHelper\build.ps1";

        private const string TEST_ORGANIZER_EXE = @"MeriClient\LoadTestFramework.TestOrganizer.exe";
        private const string TEST_CONFIG = @"MeriTestConfig.xml";

        private const string MERI_URL = "https://meri.cloudapp.net";

        private static string meriToken = null;

        public static bool BuildDockerImage(string workerDropFolder, string dbDropFolder, out string tag)
        {
            string psScript = string.Empty;
            string psScriptPath = GenerateFilePath(BUILD_PS);

            Console.WriteLine("Start to run build ps1");

            string tagOut = string.Empty;
            int result = RunPSScript(psScriptPath, new Dictionary<string, string>(){
                { "dropFolder", workerDropFolder },
                { "dbScriptDropFolder", dbDropFolder }
            }, data =>
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

        public static Guid StartTest(string containerImageTag)
        {
            XmlDocument testConfig = new XmlDocument();
            testConfig.Load(GenerateFilePath(TEST_CONFIG));

            foreach (var node in testConfig.SelectNodes(@"//Configuration/Meri/Parameters/Parameter").Cast<XmlNode>())
            {
                if (node.Attributes["key"].Value == "ContainerImageTag")
                {
                    node.Attributes["value"].Value = containerImageTag;
                }
            }

            string tempConfigFile = Path.GetTempFileName();
            testConfig.Save(tempConfigFile);
            
            Guid taskId = Guid.Empty;
            Console.WriteLine("{0} {1}", GenerateFilePath(TEST_ORGANIZER_EXE), $"\"{tempConfigFile}\" 1");
            // monitor will stay for 1 min
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
                foreach (var item in ams.GetWorkItems(taskId))
                {
                    foreach (var run in ams.GetWorkItemRuns(taskId, item.Id))
                    {
                        if (item.Name != "Meri.Aggregator" && (run.State == Meri.SDK.ObjectModel.WorkItemRunState.Initializing || run.State == Meri.SDK.ObjectModel.WorkItemRunState.Running))
                        {
                            running++;
                        }
                    }
                }
                Console.WriteLine("Task {0} has {1} items running", taskId, running);
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
    }
}
