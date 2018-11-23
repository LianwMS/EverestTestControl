# docker run --rm everest -e CONNSTR="<ConnectionString>"

# Use Latest windows image cached by container service, Contact: Anders
ARG VERSION 
FROM everestruntime.azurecr.io/runtime/staging:$VERSION

# Copy Start Script
COPY start.ps1 C:/ssis/

# Run setup.bat to install
#RUN [ "CMD", "/C", "CD C:\\ssis\\ && C:\\ssis\\setup.bat && DEL C:\\ssis\\*.msi && DEL C:\\ssis\\*.exe && MKDIR C:\\ssis\\starttask" ]

# Install Cert and Service
RUN powershell -Command \
  sc.exe create AISAgentService binPath= \"C:\Program Files\Microsoft SQL Server\140\DTS\Binn\AgentHost\Microsoft.SqlServer.IntegrationServices.AISAgentServiceHost.exe\"

# Copy Configfile
COPY ["Config", "C:/Program Files/Microsoft SQL Server/140/DTS/Binn/AgentHost"]

# Start Cmd
CMD [ "POWERSHELL", "C:\\ssis\\start.ps1" ]

