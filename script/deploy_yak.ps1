# script/deploy_yak.ps1: Create a yak package and push it to the server

param(
    [string] $path,
    [string] $tag
)

$ErrorActionPreference = "Stop"

$success = $tag -match "v(\d+)(?:\.(\d+)(?:\.(\d+)(?:\.(\d+))?)?)?$"

if ((-Not $success) -or ($Matches.Count -lt 2) -or ($Matches.Count -gt 5)) {
    $(throw "Invalid tag")
}

# Patch manifest.yml

Write-Host "Version: $version"

switch ($Matches.Count) {
    2 {$yakVersion = "$($Matches[1]).0.0"; Break}
    3 {$yakVersion = "$($Matches[1]).$($Matches[2]).0"; Break}
    4 {$yakVersion = "$($Matches[1]).$($Matches[2]).$($Matches[3])"; Break}
    5 {$yakVersion = "$($Matches[1]).$($Matches[2]).$($Matches[3])+$($Matches[4])"; Break}
}

Write-Host "Yak-Version: $yakVersion"

$content = Get-Content -path $path\dist\manifest.yml -Encoding UTF8
$content = $content.Replace("version: 0.0.0+0", "version: `"$yakVersion`"")
Set-Content $content -Path $path\dist\manifest.yml -Encoding UTF8

# copy files to dist

Get-ChildItem -Path "$path\bin\*" -Include *.rhp,*.gha,*.dll -Recurse | Copy-Item -Destination $path\dist -Force
New-Item -ItemType Directory -Path $path\dist\misc\ -Force
Copy-Item -Path .\README.md -Destination $path\dist\misc\ -Force
Copy-Item -Path .\LICENSE -Destination $path\dist\misc\LICENSE.txt -Force

# create package

Compress-Archive -Path $path\dist\* -DestinationPath $path\dist\build.zip -Force

# get yak.exe
Invoke-WebRequest -Uri 'http://files.mcneel.com/yak/tools/latest/yak.exe' -OutFile '.\yak.exe'

# publish
.\yak version
.\yak push $path\dist\build.zip
