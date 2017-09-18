$ErrorActionPreference = 'Stop'

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
		$env:DBScript = "C:\ssis\ISServerAll_paas_repeatable.sql"
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

	$script = Get-Content $env:DBScript
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

$workerConfigFile = 'C:\Program Files\Microsoft SQL Server\140\DTS\Binn\WorkerAgent.config'

$workerConfig = Get-Content $workerConfigFile -raw | ConvertFrom-Json
$workerConfig.AgentId = $AgentId
$workerConfig | ConvertTo-Json -Compress | Set-Content $workerConfigFile

$AISConfigFile = 'C:\Program Files\Microsoft SQL Server\140\DTS\Binn\AISAgentServiceSettings.config'

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
