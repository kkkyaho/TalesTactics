param([string]$UnityData='C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$testOutput=Join-Path $projectRoot 'Temp/ManagedChecks'
New-Item -ItemType Directory -Force $testOutput | Out-Null
$framework=Get-ChildItem "$UnityData/NetCoreRuntime/shared/Microsoft.NETCore.App" -Directory | Select-Object -First 1
$compiler=Get-ChildItem "$UnityData/DotNetSdk/sdk/*/Roslyn/bincore/csc.dll" | Select-Object -First 1
$nunit="$UnityData/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll"
$compileArgs=@('-nologo','-target:exe','-langversion:9',('-out:"'+$testOutput+'/Rules.dll"'))
$compileArgs+=Get-ChildItem "$UnityData/DotNetSdk/packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll" | ForEach-Object {'-r:"'+$_.FullName+'"'}
$compileArgs+='-r:"'+$nunit+'"'
$compileArgs+=Get-ChildItem "$projectRoot/Assets/TalesTactics/Runtime/Core","$projectRoot/Assets/TalesTactics/Runtime/Data",$PSScriptRoot -Filter '*.cs' -Recurse | ForEach-Object {'"'+$_.FullName+'"'}
$compileArgs+='"'+$projectRoot+'/Assets/TalesTactics/Tests/BattleRuleTests.cs"'
$responsePath=Join-Path $testOutput 'compile.rsp'
$compileArgs | Set-Content $responsePath
& "$UnityData/NetCoreRuntime/dotnet.exe" $compiler.FullName ('@'+$responsePath)
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
Copy-Item $nunit $testOutput -Force
@{runtimeOptions=@{tfm='net8.0';framework=@{name='Microsoft.NETCore.App';version=$framework.Name}}} | ConvertTo-Json -Depth 4 | Set-Content "$testOutput/Rules.runtimeconfig.json"
& "$UnityData/NetCoreRuntime/dotnet.exe" "$testOutput/Rules.dll"
exit $LASTEXITCODE


