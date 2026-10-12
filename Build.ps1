param(
 [Parameter(Mandatory=$true)][string]$GamePath,
 [string]$CompilerPath,
 [string]$DotnetPath,
 [string]$OutputPath
)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$taskGame=(Resolve-Path -LiteralPath $GamePath).Path
if(!(Test-Path -LiteralPath (Join-Path $taskGame 'Balls.exe'))){throw 'GamePath must contain Balls.exe.'}
if(!$OutputPath){$OutputPath=Join-Path $taskRoot 'out'}
$taskOutput=[IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
if(!$DotnetPath){
 $taskX64=if($env:ProgramW6432){Join-Path $env:ProgramW6432 'dotnet\dotnet.exe'}else{$null}
 $DotnetPath=if($taskX64 -and (Test-Path -LiteralPath $taskX64)){$taskX64}else{(Get-Command dotnet -ErrorAction Stop).Source}
}
$taskDotnet=(Resolve-Path -LiteralPath $DotnetPath).Path
if(!$CompilerPath){
 $taskSdks=@(& $taskDotnet --list-sdks)
 foreach($taskSdk in $taskSdks){if($taskSdk -match '^([^ ]+) \[(.+)\]$'){$CompilerPath=Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn\bincore\csc.dll'}}
 if(!$CompilerPath){throw 'Install the .NET 8 SDK or newer, or supply -CompilerPath pointing to a compatible Roslyn csc.dll.'}
}
$taskCompiler=(Resolve-Path -LiteralPath $CompilerPath).Path
$taskReferences=@()
$taskRuntime=Join-Path $taskGame 'dotnet'
if(!(Test-Path -LiteralPath $taskRuntime)){throw 'BepInEx IL2CPP runtime is missing. Run InstallDependencies.ps1 first.'}
foreach($taskDll in Get-ChildItem -LiteralPath $taskRuntime -Filter '*.dll' -File){
 try{[Reflection.AssemblyName]::GetAssemblyName($taskDll.FullName) | Out-Null;$taskReferences+=$taskDll.FullName}catch{} # Skip native runtime DLLs.
}
if($taskReferences.Count -eq 0){throw 'No managed runtime reference assemblies found.'}
foreach($taskRelative in (Get-Content -LiteralPath (Join-Path $taskRoot 'references.json') -Raw | ConvertFrom-Json)){
 $taskReference=Join-Path $taskGame $taskRelative
 if(!(Test-Path -LiteralPath $taskReference)){throw ('Missing reference: '+$taskRelative+'. Install dependencies and launch the game once to generate BepInEx/interop.')}
 $taskReferences+=$taskReference
}
foreach($taskPlugin in @('TwoGamepads','CoopBalance')){
 $taskFolder=if($taskPlugin -eq 'TwoGamepads'){'LocalCoopTwoGamepads'}else{'LocalCoopBalance'}
 $taskDestination=Join-Path $taskOutput ('BepInEx\plugins\'+$taskFolder)
 New-Item -ItemType Directory -Path $taskDestination -Force | Out-Null
 $taskDll=Join-Path $taskDestination ($taskPlugin+'.dll')
 $taskLines=@('/nostdlib+','/target:library','/optimize+','/deterministic+','/langversion:latest',('/out:"'+$taskDll+'"'))
 $taskLines+=@($taskReferences | Sort-Object -Unique | ForEach-Object {'/reference:"'+$_+'"'})
 $taskLines+=@(Get-ChildItem -LiteralPath (Join-Path $taskRoot ('src\'+$taskPlugin)) -Filter '*.cs' -File | Sort-Object Name | ForEach-Object {'"'+$_.FullName+'"'})
 $taskRsp=Join-Path $taskOutput ($taskPlugin+'.rsp')
 Set-Content -LiteralPath $taskRsp -Value $taskLines -Encoding utf8
 & $taskDotnet $taskCompiler ('@'+$taskRsp)
 if($LASTEXITCODE -ne 0){throw ($taskPlugin+' compilation failed.')}
}
$taskConfig=Join-Path $taskOutput 'BepInEx\config'
New-Item -ItemType Directory -Path $taskConfig -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskRoot 'config') -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $taskConfig -Force}
Get-ChildItem -LiteralPath (Join-Path $taskOutput 'BepInEx\plugins') -Filter '*.dll' -File -Recurse | Get-FileHash | Format-Table -AutoSize
Write-Host ('Build complete: '+(Join-Path $taskOutput 'BepInEx'))
Write-Host 'Close the game before copying the built BepInEx folder into GamePath.'

