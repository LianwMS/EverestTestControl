﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace EverestTest
{
    public class Program
    {
        private const string WORKER_DROP_FOLDER = @"\\BLD-FS-DS-05-07\tfs\IS_Scale\3252823";
        private const string DB_DROP_FOLDER = "";
        private const string TEST_ASSEMBLY = "";

        private const string BUILD_FOLDER = @"DockerHelper";
        private const string BUILD_PS = @"DockerHelper\build.ps1";

        private const string TEST_ORGANIZER_EXE = @"MeriClient\LoadTestFramework.TestOrganizer.exe";
        private const string TEST_CONFIG = @"MeriTestConfig.xml";

        private const string IMAGE_TAG_TIP = "Image Tag: ";

        private const string TFSBUILDFILE = "TFSBuilds.config";

        private static List<TFSBuild> tfsBuilds = LoadTFSBuildFromFile(TFSBUILDFILE);

        static void Main(string[] args)
        {
            //string tag;
            //BuildDockerImage(WORKER_DROP_FOLDER, "", out tag);
            //StartTest(tag);
        }

        private static bool BuildDockerImage(string workerDropFolder, string dbDropFolder, out string tag)
        {
            string psScript = string.Empty;
            string psScriptPath = GenerateFilePath(BUILD_PS);

            Console.WriteLine("Start to run build ps1");

            string tagOut = string.Empty;
            int result = RunPSScript(psScriptPath, new Dictionary<string, string>(){
                { "dropFolder", WORKER_DROP_FOLDER },
                { "dbScriptDropFolder", DB_DROP_FOLDER }
            }, data =>
            {
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

        private static bool StartTest(string containerImageTag)
        {
            XmlDocument testConfig = new XmlDocument();
            testConfig.Load(GenerateFilePath(TEST_CONFIG));

            foreach (var node in testConfig.SelectNodes(@"Configuration\Meri\Parameters\Parameter").Cast<XmlNode>())
            {
                if (node.Attributes["key"].Value == "ContainerImageTag")
                {
                    node.Attributes["value"].Value = containerImageTag;
                }
            }

            string tempConfigFile = Path.GetTempFileName();
            testConfig.Save(tempConfigFile);

            // monitor will stay for 1 min
            int exitCode = RunCommand(GenerateFilePath(TEST_ORGANIZER_EXE), $"\"{tempConfigFile}\" 1");
            File.Delete(tempConfigFile);

            // todo: extract result
            return false;
        }

        private static string GenerateFilePath(string fileName)
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

        private static List<TFSBuild> LoadTFSBuildFromFile(string fileName)
        {
            var strs = File.ReadAllLines(GenerateFilePath(fileName));
            var tFSBuild = strs.Select(str => JsonConvert.DeserializeObject<TFSBuild>(str)).ToList<TFSBuild>();
            return tFSBuild;
        }

        private static void WriteTFSBuildToFile(string fileName)
        {
            string[] strs = tfsBuilds.Select(b => JsonConvert.SerializeObject(b)).ToArray();
            File.WriteAllLines(fileName, strs);
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
                    tfsBuilds.Add(new TFSBuild()
                    {
                        TFSBuildNumber = detail.BuildNumber,
                        BuildFinishedTime = detail.FinishTime,
                        BuildStatus = detail.Status.ToString(),
                        TestStatus = TestStatus.NotStart,
                    });
                    result = true;
                }
            }
            return result;
        }
    }
}
