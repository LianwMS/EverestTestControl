param([string]$dropFolder, [string]$dbScriptDropFolder)
$ErrorActionPreference = 'Stop'

$ACR_SERVER = "everestest.azurecr.io"
$ACR_REPO = "everest/everest"
$ACR_USER = "everestest"
$ACR_PASSWORD = "+O==/J+=senffG4fIk/+d=eneaHOL/=q"

if ($dbScriptDropFolder -eq "") {
    $dbScriptDropFolder = $dropFolder
}

function GetBuildVersion($drop) {
    $xmlFile = Join-Path $drop "buildinfo/BuildInfo.xml"
    [xml]$content = Get-Content $xmlFile
    return $content.build.buildNumber + "." + (Split-Path $content.build.buildRoot -leaf)
}

$version = GetBuildVersion($dropFolder)
$dbVersion = GetBuildVersion($dbScriptDropFolder)
$tag = $version + '-' + $dbVersion

# Create temporary work folder
$tmpdir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid())

"Drop Folder: " + $dropFolder
"Version: " + $version
"DB Version: " + $dbVersion
"Image Tag: " + $tag
"Work Folder: " + $tmpdir

"Copying to work folder"
$ssis = Join-Path $tmpdir "SSIS"
Copy-Item $PSScriptRoot $tmpdir -Recurse

Copy-Item (Join-Path $dbScriptDropFolder "retail\neutral\ISServerAll_paas_repeatable.sql") $ssis
Copy-Item (Join-Path $dropFolder "retail\amd64\1033\sqlncli.msi") $ssis
Copy-Item (Join-Path $dropFolder "retail\amd64\1033\SSISPaaS.msi") $ssis
Copy-Item (Join-Path $dropFolder "retail\amd64\1033\SSISScaleOut.msi") $ssis
Copy-Item (Join-Path $dropFolder "retail\amd64\1033\SQL_AS_OLEDB.msi") $ssis
Copy-Item (Join-Path $dropFolder "retail\amd64\1033\msodbcsql.msi") $ssis

pushd $tmpdir

# Build and push image
docker login $ACR_SERVER -u $ACR_USER -p $ACR_PASSWORD
docker build -t "${ACR_SERVER}/${ACR_REPO}:${tag}" .
docker push "${ACR_SERVER}/${ACR_REPO}:${tag}"

popd
Remove-Item $tmpdir -Recurse -Force