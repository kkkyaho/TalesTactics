param(
 [string]$UnityData='C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data',
 [string]$PackageAssemblies
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
if(!$PackageAssemblies){$PackageAssemblies=Join-Path $projectRoot 'Library/ScriptAssemblies'}
$checkOutput=Join-Path $projectRoot 'Temp/ApiChecks'
New-Item -ItemType Directory -Force $checkOutput | Out-Null
$compiler=Get-ChildItem "$UnityData/DotNetSdk/sdk/*/Roslyn/bincore/csc.dll" | Select-Object -First 1
$baseRefs=@(Get-ChildItem "$UnityData/NetStandard/ref/2.1.0/*.dll")
$baseRefs+=@(Get-ChildItem "$UnityData/NetStandard/compat/2.1.0/shims/netfx/*.dll")
$baseRefs+=@(Get-ChildItem "$UnityData/Managed/UnityEngine/*.dll")
$baseRefs+=@(Get-Item "$PackageAssemblies/Unity.InputSystem.dll","$PackageAssemblies/Unity.TextMeshPro.dll","$PackageAssemblies/UnityEngine.UI.dll")
function CompilePart($name,$folder,$extraRefs) {
 $arguments=@('-nologo','-target:library','-langversion:9',('-out:"'+$checkOutput+'/'+$name+'.dll"'))
 $arguments+=($baseRefs+$extraRefs) | ForEach-Object {'-r:"'+$_.FullName+'"'}
 $arguments+=Get-ChildItem "$projectRoot/Assets/TalesTactics/$folder" -Recurse -Filter '*.cs' | ForEach-Object {'"'+$_.FullName+'"'}
 $rsp=Join-Path $checkOutput "$name.rsp"
 $arguments | Set-Content $rsp
 & "$UnityData/NetCoreRuntime/dotnet.exe" $compiler.FullName ('@'+$rsp)
 if($LASTEXITCODE -ne 0){throw "$name compilation failed"}
 Write-Output "PASS $name API compilation"
}
CompilePart 'TalesTactics.Runtime' 'Runtime' @()
$runtime=Get-Item "$checkOutput/TalesTactics.Runtime.dll"
$editorRefs=@(Get-ChildItem "$UnityData/Managed/UnityEditor*.dll" | Where-Object Name -ne 'UnityEditor.dll')
CompilePart 'TalesTactics.Editor' 'Editor' ($editorRefs+@($runtime))
$nunit=Get-Item "$UnityData/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net472/unity-custom/nunit.framework.dll"
CompilePart 'TalesTactics.Tests' 'Tests' @($runtime,$nunit)
$testRunner=Get-Item "$PackageAssemblies/UnityEngine.TestRunner.dll"
CompilePart 'TalesTactics.PlayModeTests' 'PlayModeTests' @($runtime,$nunit,$testRunner)
