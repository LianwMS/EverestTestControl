param([string]$sourceTag, [string]$userName,[string]$password)
$ErrorActionPreference = 'Stop'

$ACR_SERVER = "everestest.azurecr.io"
$ACR_REPO = "everest/everest"
$ACR_USER = "everestest"
$ACR_PASSWORD = "ru8Jx8J8VxicGPa3aBoCaun=BpV8VcbM"

# Create temporary work folder
$tmpdir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid())

"Work Folder: " + $tmpdir

"Copying to work folder"
Copy-Item $PSScriptRoot $tmpdir -Recurse

pushd $tmpdir

# Build and push image
docker login $ACR_SERVER -u $ACR_USER -p $ACR_PASSWORD
docker login --username $userName --password $password everestruntime.azurecr.io
docker build --build-arg VERSION=$sourceTag -t "${ACR_SERVER}/${ACR_REPO}:${sourceTag}" .
docker push "${ACR_SERVER}/${ACR_REPO}:${sourceTag}"

popd
Remove-Item $tmpdir -Recurse -Force