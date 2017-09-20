# docker run --rm everest -e CONNSTR="<ConnectionString>"

# Use Latest windows image cached by container service, Contact: Anders
FROM microsoft/windowsservercore@sha256:34a0199e1f8c4c978d08e5a4e67af961752e41212b7bde9c1ea570ee29dcff2c

# Copy SSIS file needed
COPY SSIS C:/ssis/

# Run setup.bat to install
RUN [ "CMD", "/C", "CD C:\\ssis\\ && C:\\ssis\\setup.bat && DEL C:\\ssis\\*.msi && DEL C:\\ssis\\*.exe && MKDIR C:\\ssis\\starttask" ]

# Copy Cert
COPY Cert C:/ssis

# Copy Configfile
COPY ["Config", "C:/Program Files/Microsoft SQL Server/140/DTS/Binn"]

# Copy StartTask
#COPY StartTask C:/ssis/StartTask

# Install Cert and Service
RUN powershell -Command \
  $certPwd = ConvertTo-SecureString -String '123' -Force -AsPlainText; \
  Import-PfxCertificate -FilePath C:\ssis\ECert.pfx cert:\localMachine\my -Password $certPwd; \
  Import-PfxCertificate -FilePath C:\ssis\AzureSsisTest.pfx cert:\localMachine\my -Password $certPwd; \
  sc.exe create AISAgentService binPath= \"C:\Program Files\Microsoft SQL Server\140\DTS\Binn\Microsoft.SqlServer.IntegrationServices.AISAgentServiceHost.exe\"

CMD [ "POWERSHELL", "C:\\ssis\\start.ps1" ]

# Copy Start Script
COPY start.ps1 C:/ssis/
