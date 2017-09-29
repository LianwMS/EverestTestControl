EverestTest
===========

The program monitors the builds on TFS, and triggers tests for new builds.

Usage
-----
Run `EverestTest.exe` directly to monitor TFS and auto trigger tests.  
Run `EverestTest.exe <drop folder>` to 

Prerequisites
-------------
* Docker for Windows (Windows containers)
* PowerShell (latest)
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

About Meri Files
================