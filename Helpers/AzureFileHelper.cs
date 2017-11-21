using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Newtonsoft.Json;

namespace EverestTest
{
    internal class AzureFileHelper
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

        private const string DBFile_SHARED_REFERENCE_NAME = "dbfile";
        private const string DBFile_DIRECTORY_NAME = "dll";

        private const string BACKEND_INFO_SHARED_REFERENCE_NAME = "productinfo";
        private const string BACKEND_INFO_FILE_NAME = "productsInfo.txt";

        public static bool UploadPaasDBUpgradeFileToAzureFile(string buildnumber, string sourcePath, string fileName = "Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.dll")
        {
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            CloudFileShare share = fileClient.GetShareReference(DBFile_SHARED_REFERENCE_NAME);
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                // Get a reference to the directory we created previously.
                CloudFileDirectory sampleDir = rootDir.GetDirectoryReference(DBFile_DIRECTORY_NAME + "\\" + buildnumber);

                // Create the file directory
                sampleDir.CreateIfNotExists();

                // Create file reference
                CloudFile destFile = sampleDir.GetFileReference(fileName);

                // Upload File
                destFile.UploadFromFile(sourcePath + fileName, System.IO.FileMode.Open);

                return true;
            }
            return false;
        }

        public static BackendInfo GetCurrentProductBackendInfo()
        {
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(BACKEND_INFO_SHARED_REFERENCE_NAME);

            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                CloudFile sourceFile = rootDir.GetFileReference(BACKEND_INFO_FILE_NAME);

                if (sourceFile.Exists())
                {
                    List<BackendInfo> products;
                    using (var stream = sourceFile.OpenRead())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            var str = reader.ReadToEnd();
                            products = JsonConvert.DeserializeObject<List<BackendInfo>>(str);
                        }
                    }
                    if (products != null)
                    {
                        var current = products.Where(p => p.CurrentOnProd == true).FirstOrDefault();
                        return current;
                    }
                }
            }
            return null;
        }

        public static string GetProductBackendWorkerVersion()
        {
            return GetCurrentProductBackendInfo().WorkerVersion;
        }

        public static string GetProductDBVersion()
        {
            return GetCurrentProductBackendInfo().DBVersion;
        }
    }
}
