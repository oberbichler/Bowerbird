# script/patch_assemblyinfo.ps1: Patch version numbers in AssemblyInfo.cs

param(
    [string] $path,
    [string] $tag
)

$ErrorActionPreference = "Stop"

$success = $tag -match "v(\d+)(?:\.(\d+)(?:\.(\d+)(?:\.(\d+))?)?)?$"

if ((-Not $success) -or ($Matches.Count -lt 2) -or ($Matches.Count -gt 5)) {
    $(throw "Invalid tag")
}

# Patch AssemblyInfo.cs

switch ($Matches.Count) {
    2 {$version = "$($Matches[1]).0.0.0"; Break}
    3 {$version = "$($Matches[1]).$($Matches[2]).0.0"; Break}
    4 {$version = "$($Matches[1]).$($Matches[2]).$($Matches[3]).0"; Break}
    5 {$version = "$($Matches[1]).$($Matches[2]).$($Matches[3]).$($Matches[4])"; Break}
}

$content = Get-Content -path $path\Properties\AssemblyInfo.cs -Encoding UTF8
$content = $content.Replace("[assembly: AssemblyVersion(`"1.0.0.0`")]", "[assembly: AssemblyVersion(`"$version`")]")
$content = $content.Replace("[assembly: AssemblyFileVersion(`"1.0.0.0`")]", "[assembly: AssemblyFileVersion(`"$version`")]")
Set-Content $content -Path $path\Properties\AssemblyInfo.cs -Encoding UTF8
