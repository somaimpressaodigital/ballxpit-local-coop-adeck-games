param([switch]$Complete)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
foreach($taskRequired in @('out\BepInEx\plugins\LocalCoopTwoGamepads\TwoGamepads.dll','out\BepInEx\plugins\LocalCoopBalance\CoopBalance.dll')){if(!(Test-Path -LiteralPath (Join-Path $taskRoot $taskRequired))){throw 'Run Build.ps1 before packaging.'}}
$taskDist=Join-Path $taskRoot 'dist'
New-Item -ItemType Directory -Path $taskDist -Force | Out-Null
$taskStage=Join-Path $taskDist ('stage-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskStage -Force | Out-Null
if($Complete){
 $taskArchive=Join-Path $taskRoot 'vendor\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip'
 if((Get-FileHash -LiteralPath $taskArchive).Hash -ne (Get-Content -LiteralPath (Join-Path $taskRoot 'vendor\framework.sha256') -Raw).Trim()){throw 'Framework checksum mismatch.'}
 Expand-Archive -LiteralPath $taskArchive -DestinationPath $taskStage
 $taskBase=Join-Path $taskStage 'BepInEx\plugins\BALLxPITLocalCoop'
 New-Item -ItemType Directory -Path $taskBase -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $taskRoot 'vendor\BALLxPITLocalCoop\BALLxPITLocalCoop.dll') -Destination $taskBase
 Copy-Item -LiteralPath (Join-Path $taskRoot 'vendor\licenses') -Destination (Join-Path $taskStage 'LICENSES') -Recurse
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'out\BepInEx') -Destination $taskStage -Recurse -Force
foreach($taskDoc in @('README.md','BUILDING.md','CREDITS.md','THIRD-PARTY-NOTICES.md','SECURITY.md','CHANGELOG.md','VALIDATION.md')){Copy-Item -LiteralPath (Join-Path $taskRoot $taskDoc) -Destination $taskStage}
$taskManifest=@(Get-ChildItem -LiteralPath $taskStage -File -Recurse | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($taskStage.Length+1).Replace('\','/');Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
ConvertTo-Json -InputObject $taskManifest -Depth 4 | Set-Content -LiteralPath (Join-Path $taskStage 'manifest.json') -Encoding utf8
$taskVersion=(Get-Content -LiteralPath (Join-Path $taskRoot 'VERSION') -Raw).Trim()
$taskKind=if($Complete){'complete'}else{'companion'}
$taskZip=Join-Path $taskDist ('ballxpit-coop-'+$taskVersion+'-'+$taskKind+'.zip')
Add-Type -AssemblyName System.IO.Compression.FileSystem
if(Test-Path -LiteralPath $taskZip){Remove-Item -LiteralPath $taskZip}
[IO.Compression.ZipFile]::CreateFromDirectory($taskStage,$taskZip,[IO.Compression.CompressionLevel]::Optimal,$false)
Get-FileHash -LiteralPath $taskZip | Format-List

