# docker run --rm everest -e CONNSTR="<ConnectionString>"

# Use Latest windows image cached by container service, Contact: Anders
FROM microsoft/windowsservercore@sha256:34a0199e1f8c4c978d08e5a4e67af961752e41212b7bde9c1ea570ee29dcff2c

# Copy SSIS file needed
COPY SSIS C:/ssis/

# Copy Start Script
COPY start.ps1 C:/ssis/

# Run setup.bat to install
#RUN [ "CMD", "/C", "CD C:\\ssis\\ && C:\\ssis\\setup.bat && DEL C:\\ssis\\*.msi && DEL C:\\ssis\\*.exe && MKDIR C:\\ssis\\starttask" ]

# Install Cert and Service
RUN powershell -Command \
  C:\ssis\setup.bat; \
  DEL C:\ssis\*.msi; \
  DEL C:\ssis\*.exe; \
  $certPwd = ConvertTo-SecureString -String '123' -Force -AsPlainText; \
  Import-PfxCertificate -FilePath C:\ssis\ECert.pfx cert:\localMachine\my -Password $certPwd; \
  Import-PfxCertificate -FilePath C:\ssis\AzureSsisTest.pfx cert:\localMachine\my -Password $certPwd; \
  sc.exe create AISAgentService binPath= \"C:\Program Files\Microsoft SQL Server\140\DTS\Binn\Microsoft.SqlServer.IntegrationServices.AISAgentServiceHost.exe\"

# Copy Configfile
COPY ["Config", "C:/Program Files/Microsoft SQL Server/140/DTS/Binn"]

CMD [ "POWERSHELL", "C:\\ssis\\start.ps1" ]

