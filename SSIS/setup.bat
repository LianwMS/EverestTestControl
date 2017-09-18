reg add "HKLM\Software\Microsoft\StrongName\Verification\*,*" /f
reg add "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\*,*" /f
::dotNetFx46-x86-x64-AllOS.exe /passive /norestart
VCRuntime140_x64.exe /install /quiet
VCRuntime140_x86.exe /install /quiet
vcredist_x64.exe /install /quiet /norestart
vcredist_x86.exe /install /quiet /norestart
vsta_ls.exe /Full /Q /norestart
vsta_setup.exe /Full /Q /norestart
AccessDatabaseEngine_X64.exe /quiet /norestart

start /wait msiexec /i sharepointclientcomponents_15-4711-1001_x64_en-us.msi /qn /norestart
start /wait msiexec /i DB2OLEDBV6_x64.msi /qn /norestart
start /wait msiexec /i sqlncli.msi /qn /norestart IACCEPTSQLNCLILICENSETERMS=YES
start /wait msiexec /i SQL_AS_OLEDB.msi /qn /norestart
start /wait msiexec /i msodbcsql.msi /qn /norestart IACCEPTMSODBCSQLLICENSETERMS=YES

start /wait msiexec /i SSISPaaS.msi /qn /norestart
start /wait msiexec /i SSISScaleOut.msi /qn /norestart
start /wait msiexec /i SsisAzureFeaturePack_2017_x64.msi /qn /norestart

del /s /q /f %TEMP%\*
del /s /q /f %WINDIR%\temp\*
