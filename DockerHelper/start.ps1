$ErrorActionPreference = 'Stop'
$AzureFileServer = "everestdbfile.file.core.windows.net"
$AzureFileUser = "AZURE\everestdbfile"
$AzureFilePassword = "ceR3SaZjLX14QlQljx1FH9j/wGmFlqKdSFn9s3PjQbNTC+Gd+FvEN3qH9Kh++mSs4jUphJ/72ipJ+pazXEvVVQ=="
$AzureFileLogFolder = "\\everestdbfile.file.core.windows.net\dbfile\dll"

function OpenSQLConnectionWithRetry($conn) {
	$DBConnectRetryCount = 3
	$DBConnectRetryInterval = 10
	for ([int]$retryCount = 0; $retryCount -le $DBConnectRetryCount; $retryCount++) {
		try {
			$conn.Open()
			break
		} catch {
			if (($retryCount + 1) -eq $DBConnectRetryCount) {
				throw $_
			}
			$_
			Start-Sleep -Seconds $DBConnectRetryInterval
			'Retry connect #' + ($retryCount + 1)
		}
	}
}

Try {

$AgentId = [guid]::NewGuid()
$initializeDB = ($env:InitializeDB -ne $null -and $env:InitializeDB.ToLower() -eq 'true')

"Connection String: " + $env:CONNSTR
"Agent ID: " + $AgentId
"Initialize DB: " + $initializeDB
if ($initializeDB) {
	if ($env:DBTier -eq $null) {
		$env:DBTier = "S2"
	}
	"DB Tier: " + $env:DBTier
	
	if ($env:DBScript -eq $null) {
		$assembly = [System.Reflection.Assembly]::LoadFrom("C:\Program Files\Microsoft SQL Server\140\DTS\Binn\ssisdb\Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.dll")
		$currentVersion = [Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.PaasDBUpgradeHelper]::GetTargetDBVersion()
		$env:DBScript = "ProvisionToV" + $currentVersion + ".sql"
	}
	"DB Script: " + $env:DBScript
}

if ($initializeDB) {
	$connection = New-Object System.Data.SqlClient.SqlConnection
	$connection.ConnectionString = $env:CONNSTR
	OpenSQLConnectionWithRetry($connection)

	"Clean up Database"
	$command = New-Object System.Data.SqlClient.SqlCommand
	$command.CommandText = "DROP DATABASE IF EXISTS SSISDB"
	$command.Connection = $connection
	$command.ExecuteNonQuery() | Out-Null

	"Initializing Database"
	$command = New-Object System.Data.SqlClient.SqlCommand
	$command.CommandText = "CREATE DATABASE SSISDB COLLATE SQL_Latin1_General_CP1_CI_AS (SERVICE_OBJECTIVE = '" + $env:DBTier + "')"
	$command.Connection = $connection
	$command.CommandTimeout = 120
	$command.ExecuteNonQuery() | Out-Null

	$state = 1
	while ($state -ne 0) {
		$command = New-Object System.Data.SqlClient.SqlCommand
		$command.CommandText = "SELECT state FROM sys.databases WHERE name = 'SSISDB'"
		$command.Connection = $connection
		$reader = $command.ExecuteReader()
		if ($reader.Read()) {
			$state = $reader[0]
		}
		$reader.Close()
		if ($state -ne 0) {
			Start-Sleep -Seconds 1
		}
	}

	$connection.Close()

	#$script = Get-Content $env:DBScript
	
	if ($env:PeoductVerson -eq $null) {
		"Get DB script from local"
		Copy-Item "C:\ssis\Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.dll" "C:\"
	}
	else {
		$HomeDriveLetter = "x:"
		if (Test-Path -Path $HomeDriveLetter)
		{
			"net use exists, try to remove"
			net use $HomeDriveLetter /delete
		}

		"Get DB script from Azure file"
		"net use to " + $AzureFileLogFolder + " with user " + $AzureFileUser
		net use $HomeDriveLetter "$AzureFileLogFolder" /u:$AzureFileUser $AzureFilePassword

		$folderName = $HomeDriveLetter + "\" + $env:PeoductVerson
		"Copy Assembly from " + $folderName + " to C:\"
		Copy-Item (Join-Path $folderName "\Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.dll") "C:\"

		"Remove net use"
		net use $HomeDriveLetter /delete
	}

	$assembly = [System.Reflection.Assembly]::LoadFrom("C:\Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.dll")
	$stream = $assembly.GetManifestResourceStream("Microsoft.SqlServer.IntegrationServices.PaasDBUpgrade.Resource." + $env:DBScript);
	$reader = New-Object System.IO.StreamReader($stream)
	$script = $reader.ReadToEnd()
	$reader.Close()
	$stream.Close()

	$ss = [Regex]::Split($script, '\b[Gg][Oo]\b')
	$connection = New-Object System.Data.SqlClient.SqlConnection
	$connection.ConnectionString = $env:CONNSTR + "Initial Catalog=SSISDB;"
	OpenSQLConnectionWithRetry($connection)
	foreach ($s in $ss) {
		if ($s.Trim().Length -eq 0) { continue }
		$command = New-Object System.Data.SqlClient.SqlCommand
		$command.CommandText = $s
		$command.Connection = $connection
		$command.ExecuteNonQuery() | Out-Null
	}
	$connection.Close()
}

$workerConfigFile = 'C:\Program Files\Microsoft SQL Server\140\DTS\Binn\AgentHost\WorkerAgent.config'

$workerConfig = Get-Content $workerConfigFile -raw | ConvertFrom-Json
$workerConfig.AgentId = $AgentId
$workerConfig | ConvertTo-Json -Compress | Set-Content $workerConfigFile

$AISConfigFile = 'C:\Program Files\Microsoft SQL Server\140\DTS\Binn\AgentHost\AISAgentServiceSettings.config'

$AISConfig = Get-Content $AISConfigFile -raw | ConvertFrom-Json
$AISConfig.agentId = $AgentId
$AISConfig | ConvertTo-Json -Compress | Set-Content $AISConfigFile
'$$Status: Succeeded'
}
Catch
{
$_
'$$Status: Failed'
}

(Get-Service -Name "AISAgentService").Start(@("/test", "/conn:""$($env:CONNSTR + "Initial Catalog=SSISDB;")"""))

while ((Get-Service -Name "AISAgentService").Status -ne "Stopped") {
	Start-Sleep -Milliseconds 500
}
