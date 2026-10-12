param([Parameter(Mandatory=$true)][string]$GamePath)
$ErrorActionPreference='Stop'
$taskGame=(Resolve-Path -LiteralPath $GamePath).Path
if(!(Test-Path -LiteralPath (Join-Path $taskGame 'Balls.exe'))){throw 'GamePath must contain Balls.exe.'}
foreach($taskProcess in @(Get-Process -Name Balls -ErrorAction SilentlyContinue)){if($taskProcess.Path -eq (Join-Path $taskGame 'Balls.exe')){throw 'Close this game before installing dependencies.'}}
$taskFramework=Join-Path $PSScriptRoot 'vendor\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788.zip'
$taskExpected=(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vendor\framework.sha256') -Raw).Trim()
if((Get-FileHash -LiteralPath $taskFramework).Hash -ne $taskExpected){throw 'Framework archive checksum mismatch.'}
Write-Host 'Extracting the bundled BepInEx framework. Back up any existing framework/configuration before updating.'
Expand-Archive -LiteralPath $taskFramework -DestinationPath $taskGame -Force
$taskBaseFolder=Join-Path $taskGame 'BepInEx\plugins\BALLxPITLocalCoop'
New-Item -ItemType Directory -Path $taskBaseFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vendor\BALLxPITLocalCoop\BALLxPITLocalCoop.dll') -Destination $taskBaseFolder -Force
Write-Host 'Dependencies installed. Launch Balls.exe once, wait for BepInEx interop generation, then close the game before compiling/installing.'

