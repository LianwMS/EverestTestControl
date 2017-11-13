using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;

namespace EverestTest
{
    internal class AzureFileHelper
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
        private const string SHAREDREFERENCENAME = "dbfile";
        private const string DIRECTORYNAME = "dll";

        public static bool UploadPaasDBUpgradeFileToAzureFile(string buildnumber, string sourcePath, string fileName = "Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.dll")
        {
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            CloudFileShare share = fileClient.GetShareReference(SHAREDREFERENCENAME);
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                // Get a reference to the directory we created previously.
                CloudFileDirectory sampleDir = rootDir.GetDirectoryReference(DIRECTORYNAME + "\\" + buildnumber);

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
    }
}
