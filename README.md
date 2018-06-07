EverestTest
===========

The program monitors the builds on TFS, and triggers tests for new builds.

Usage
-----
Run `EverestTest.exe` directly to monitor TFS and auto trigger tests.  
Run `EverestTest.exe <drop folder>` to manually trigger tests. Note: please specify different SQL Servers in MeriTestConfig.xml.

Prerequisites
-------------
* Visual Studio 2017
* Docker for Windows (Windows containers)
* PowerShell (latest)
* Azure CLI (optional)
* Installers placed under `DockerHelper\SSIS\`
  - AccessDatabaseEngine_X64.exe
  - DB2OLEDBV6_x64.msi
  - sharepointclientcomponents_15-4711-1001_x64_en-us.msi
  - SsisAzureFeaturePack_2017_x64.msi
  - vcredist_x64.exe
  - vcredist_x86.exe
  - VCRuntime140_x64.exe
  - VCRuntime140_x86.exe
  - vsta_ls.exe
  - vsta_setup.exe
* Meri Client placed under `MeriClient\`

About Docker Image
==================
Build image: `build.ps1 -dropFolder <drop folder>`  
Azure Container Registry credential is hard coded in `build.ps1`  
Tag for the image is `{build version}.{drop folder number}`. E.g. `14.0.900.4674.3265074`

To build the image, docker for windows must be installed, and windows containers must be
selected. (Right click on tray icon -> Switch to Windows containers)

The `build.ps1` will build the image using `Dockerfile`.  
`Dockerfile` will run the `setup.bat` **inside container**.  
The `start.ps1` is run **inside container** when starting a container.

Manually run the container locally:  
`docker run -e CONNSTR="<connection string>" -e InitializeDB=True --rm everestest.azurecr.io/everest/everest:<tag>`  
Docker for windows required, and login to ACR using docker before running.

Manually provision the container on Azure:  
`az container create -g everestest -n instance --image everestest.azurecr.io/everest/everest:<tag> --registry-username everestest --registry-password <password to ACR> --os-type windows --cpu 2 --memory 3.5 -e CONNSTR="<connection string>" InitializeDB=True`

Sample connection string:  
`Server=tcp:everestest-1.databse.windows.net,1433;User ID=everest;Password=User@123User@123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`

About Meri Files
================
Now we have 4 Meri File Sets transmitted to Meri. They are:
* amd64  
  Test binaries + some static files, from
  - `<drop folder>\testbin\retail\dts\dts\amd64`
  - Part of TFS `$\testsrc\dts\Test\PackageRepository`
  - Part of `\\sqlcl\Team\IS\Test` mapped to `LocalFolder`
* bits  
  Installers:
  - azure-cli-2.0.16.msi
  - msodbcsql.msi
  - SharedManagementObjects.msi
  - sqlls.msi
  - sqlncli.msi
  - sqlncli11.msi
  - SQLSysClrTypes.msi
  - SQL_AS_OLEDB.msi
  - SsisAzureFeaturePack_2017_x64.msi
  - SSISPaaS.msi
  - vsta_ls.exe
  - vsta_setup.exe
  Scripts:
  - setup.cmd
  - SkipVer.reg
* config
  - TestCases.config
* PipelineReworkFiles
  - PipelineRework_Denali.zip: Zipped from `\\sqlcl\Team\IS\Test\PipelineRework_FVT\PipelineRework_Denali` (including the outside folder)
Check the current content of the files to make sure they are correct.

All these filesets will extracted into the same working folder when running tests on Meri.
It is possible to move files from one fileset to another, merge many filesets into one or split one to many.

Meri will use cached filesets when IDs are provided.
