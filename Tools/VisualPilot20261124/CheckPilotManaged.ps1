[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('Windows','Android')][string]$Platform,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{32}$')][string]$ExpectedRunId,
 [Parameter(Mandatory=$true)][string]$NotBeforeUtc
)
# Read-only Mono.Cecil metadata/IL inspection. No Unity, native/game execution,
# player modification, signing, installation, network, or real user-save access.
# Android refuses cached ManagedStripped input. Windows copy timestamps can be
# inherited from cached player inputs, so freshness is the new output/run report.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics'
$validation=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project'
$evidence=Join-Path $repo 'Logs/VisualPilot20261124/Builds'
$platformLower=$Platform.ToLowerInvariant()
$buildRoot=Join-Path $validation ('Builds/VisualPilot20261124/'+$Platform)
$managed=if($Platform -eq 'Android'){Join-Path $validation 'Library/Bee/artifacts/Android/ManagedStripped'}else{Join-Path $buildRoot 'TalesTactics_Data/Managed'}
$runtime=Join-Path $managed 'TalesTactics.Runtime.dll'
$output=Join-Path $evidence ($platformLower+'-managed-isolation.json')
if(Test-Path -LiteralPath $output){throw 'Report already exists; previous evidence will not be overwritten.'}
$cecilPackage=Get-ChildItem -LiteralPath (Join-Path $validation 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.mono-cecil@*' | Sort-Object Name | Select-Object -First 1
if(!$cecilPackage){throw 'Mono.Cecil package is missing from the exact validation checkout.'}
$cecil=Join-Path $cecilPackage.FullName 'Mono.Cecil.dll'
$checks=New-Object 'System.Collections.Generic.List[object]'
$failures=New-Object 'System.Collections.Generic.List[string]'
$reviewIl=New-Object 'System.Collections.Generic.List[string]'
$editorTestIl=New-Object 'System.Collections.Generic.List[string]'
$testAttributes=New-Object 'System.Collections.Generic.List[string]'
$knownReview=@('BattleHudRedesignReview','ReviewStorageBootstrap','CampaignFlowReview','CombatClarityReview','CombatPresentationReview','CrossoverPlayerReview','EnvironmentIntegrationReview','HelpGuideReview','IntermissionUxReview','LocalMusicReview','ManualPlayerReview','OriginalSkillTrioReview','OrnateReview','QualityPresentationReview','SdMotionDirectionReview','SignatureWeaponReview','SkillRosterVfxFixture','SkillRosterVfxReview','TitleScreenReview','UiRefinementReview','UiSafeLayoutReview','VfxQualityReview','VfxUpgradeReview','CampaignPlayerReview','PersistencePlayerReview','MenuPreviewBake','MenuPreviewBakeRunner','VisualPilotCapture','VisualPilotArtImporter') | ForEach-Object {'TalesTactics.'+$_}
$required=@('TalesTactics.BattleDirector','TalesTactics.BattleHud','TalesTactics.MobileUiLayout','TalesTactics.AlphenVisualPilotMotion')
if($Platform -eq 'Android'){$required+='TalesTactics.AndroidRuntimeSettings'}
$report=[ordered]@{
 schema=2;passed=$false;platform=$Platform;checkedUtc=[DateTime]::UtcNow.ToString('o')
 scope='Mono.Cecil metadata and IL inspection of the Windows player runtime assembly or Android IL2CPP ManagedStripped input. No game assembly execution, Unity tests, native binary verification, APK installation, Android device execution, rendering or gameplay validation. Windows copied DLL timestamps may precede the build; the current build report identifies the fresh output. Android ManagedStripped timestamp must be within this build.'
 runtimePath=$runtime;runtimeSHA256=$null;runtimeSHA256After=$null;runtimeBytes=$null;runtimeLastWriteUtc=$null;cecilPath=$cecil;cecilSHA256=$null
 requiredProductionTypes=$required;knownExcludedReviewTypes=@($knownReview);productionTypes=@();assemblyReferences=@();typeCount=0;methodBodiesInspected=0;instructionsInspected=0
 reviewFixtureTypes=@();reviewTypeReferences=@();reviewMemberReferences=@();reviewIlReferences=@();editorOrTestTypes=@();editorOrTestAssemblies=@();editorOrTestReferences=@();editorOrTestTypeReferences=@();editorOrTestMemberReferences=@();editorOrTestIlReferences=@();testAttributes=@();androidInitializer=$null;checks=@();failures=@()
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function All-Types($types){foreach($type in $types){$type;if($type.HasNestedTypes){All-Types $type.NestedTypes}}}
function Is-Review([string]$name){if(!$name){return $false};$outer=($name -split '/',2)[0];return $knownReview -ccontains $outer}
function Is-EditorTest([string]$name){return $name -match '^(UnityEditor(?:\.|/|$)|NUnit(?:\.|/|$)|UnityEngine\.TestTools(?:\.|/|$)|TalesTactics\.(?:Editor|Tests|PlayModeTests)(?:\.|/|$))'}
function Is-EditorTestAssembly([string]$name){return $name -match 'UnityEditor|TalesTactics\.(Editor|.*Tests)|TestRunner|nunit'}
function Check([string]$name,[bool]$passed,$observed){$checks.Add([pscustomobject]@{name=$name;passed=$passed;observed=$observed});if(!$passed){$failures.Add($name)}}
function Scan-Attributes($provider,[string]$owner){foreach($attr in $provider.CustomAttributes){if(Is-EditorTest $attr.AttributeType.FullName){$testAttributes.Add($owner+' : '+$attr.AttributeType.FullName)}}}
function Utc([string]$value){if($value -notmatch '(Z|[+-]\d{2}:\d{2})$'){throw 'Timestamp needs an explicit UTC offset.'};return [DateTimeOffset]::Parse($value).UtcDateTime}
$buildResultPath=Join-Path $buildRoot 'build-result.json'
$buildJson=[IO.File]::ReadAllText($buildResultPath)
$build=$buildJson | ConvertFrom-Json
if($build.runId -cne $ExpectedRunId){throw 'Build report does not identify the explicitly requested current build.'}
if($build.result -cne 'Succeeded' -or $build.errors -ne 0){throw 'Wait for the requested build to finish successfully before inspection.'}
if($build.sourceOnly -ne $true -or $build.localMusicChecked -ne $true -or $build.containsLocalMusic -ne $false){throw 'Expected a verified source-only build without user-provided music.'}
$artifact=Join-Path $buildRoot $(if($Platform -eq 'Android'){'TalesTactics.apk'}else{'TalesTactics.exe'})
if([IO.Path]::GetFullPath($build.outputPath) -ine [IO.Path]::GetFullPath($artifact)){throw 'Unexpected player output path in build result.'}
$artifactFile=Get-Item -LiteralPath $artifact
$buildFile=Get-Item -LiteralPath $buildResultPath
$startedText=[regex]::Match($buildJson,'"startedUtc"\s*:\s*"([^"]+)"').Groups[1].Value
$started=Utc $startedText
$notBefore=Utc $NotBeforeUtc
if($started -lt $notBefore -or $started -gt [DateTime]::UtcNow){throw 'Build start is not within the requested current build interval.'}
if($buildFile.LastWriteTimeUtc -lt $started){throw 'Build result predates the requested build start time.'}
if($Platform -eq 'Android' -and $artifactFile.LastWriteTimeUtc -lt $started){throw 'APK predates this build start.'}
if($buildFile.LastWriteTimeUtc -lt $artifactFile.LastWriteTimeUtc){throw 'Final build result predates output; build may still be running.'}
$finalPath=Join-Path $evidence ($platformLower+'-build-final.json')
$finalJson=[IO.File]::ReadAllText($finalPath)
$final=$finalJson | ConvertFrom-Json
$finishedText=[regex]::Match($finalJson,'"finishedUtc"\s*:\s*"([^"]+)"').Groups[1].Value
$finished=Utc $finishedText
if($final.passed -ne $true -or $final.settingsEqual -ne $true -or $final.result -cne 'Succeeded' -or $final.errors -ne 0 -or $final.sourceOnly -ne $true){throw 'Final builder/settings restoration report did not pass.'}
if($finished -lt $started -or $finished -gt [DateTime]::UtcNow){throw 'Final builder report does not belong to the current build interval.'}
if([IO.Path]::GetFullPath($final.output) -ine [IO.Path]::GetFullPath($artifact)){throw 'Final builder output path mismatch.'}
$report.buildContext=[ordered]@{
 resultPath=$buildResultPath;resultSHA256=Hash $buildResultPath;runId=$build.runId;startedUtc=$started.ToString('o');notBeforeUtc=$notBefore.ToString('o');result=$build.result;errors=$build.errors;warnings=$build.warnings;resultLastWriteUtc=$buildFile.LastWriteTimeUtc.ToString('o')
 artifactPath=$artifact;artifactSHA256=Hash $artifact;artifactBytes=$artifactFile.Length;artifactLastWriteUtc=$artifactFile.LastWriteTimeUtc.ToString('o');artifactWrittenDuringThisBuild=($artifactFile.LastWriteTimeUtc -ge $started)
 finalPath=$finalPath;finalSHA256=Hash $finalPath;finishedUtc=$finished.ToString('o');settingsEqual=$final.settingsEqual
 runtimeWrittenDuringThisBuild=$false;resultAndArtifactUnchangedDuringInspection=$false
}
$assembly=$null
try{
 $before=Get-Item -LiteralPath $runtime
 $report.buildContext.runtimeWrittenDuringThisBuild=$before.LastWriteTimeUtc -ge $started
 if($Platform -eq 'Android' -and !$report.buildContext.runtimeWrittenDuringThisBuild){throw 'ManagedStripped runtime predates this build; refusing to treat cached assembly evidence as a fresh current-build inspection.'}
 $report.runtimeSHA256=Hash $runtime;$report.runtimeBytes=$before.Length;$report.runtimeLastWriteUtc=$before.LastWriteTimeUtc.ToString('o');$report.cecilSHA256=Hash $cecil
 Add-Type -LiteralPath $cecil
 $reader=New-Object Mono.Cecil.ReaderParameters
 $reader.InMemory=$true;$reader.ReadSymbols=$false
 $assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($runtime,$reader)
 $module=$assembly.MainModule
 $types=@(All-Types $module.Types)
 $names=@($types | ForEach-Object FullName)
 $report.typeCount=$types.Count
 $report.assemblyReferences=@($module.AssemblyReferences | ForEach-Object Name | Sort-Object)
 $typeReferences=@($module.GetTypeReferences() | ForEach-Object FullName | Sort-Object -Unique)
 $memberReferences=@($module.GetMemberReferences())
 $report.productionTypes=@($types | Where-Object {$required -ccontains $_.FullName} | ForEach-Object {[pscustomobject]@{name=$_.FullName;methodCount=$_.Methods.Count;fieldCount=$_.Fields.Count}})
 $report.reviewFixtureTypes=@($names | Where-Object {Is-Review $_} | Sort-Object -Unique)
 $report.reviewTypeReferences=@($typeReferences | Where-Object {Is-Review $_})
 $report.reviewMemberReferences=@($memberReferences | Where-Object {Is-Review $_.DeclaringType.FullName} | ForEach-Object FullName | Sort-Object -Unique)
 $report.editorOrTestTypes=@($names | Where-Object {Is-EditorTest $_} | Sort-Object -Unique)
 $report.editorOrTestTypeReferences=@($typeReferences | Where-Object {Is-EditorTest $_})
 $report.editorOrTestMemberReferences=@($memberReferences | Where-Object {Is-EditorTest $_.DeclaringType.FullName} | ForEach-Object FullName | Sort-Object -Unique)
 $report.editorOrTestReferences=@($report.assemblyReferences | Where-Object {Is-EditorTestAssembly $_})
 $report.editorOrTestAssemblies=@(Get-ChildItem -LiteralPath $managed -Filter '*.dll' -File | Where-Object {Is-EditorTestAssembly $_.Name} | ForEach-Object Name | Sort-Object)
 Scan-Attributes $assembly $assembly.Name.Name
 Scan-Attributes $module $module.Name
 foreach($type in $types){
  Scan-Attributes $type $type.FullName
  foreach($field in $type.Fields){Scan-Attributes $field $field.FullName}
  foreach($property in $type.Properties){Scan-Attributes $property $property.FullName}
  foreach($event in $type.Events){Scan-Attributes $event $event.FullName}
  foreach($method in $type.Methods){
   Scan-Attributes $method $method.FullName
   foreach($param in $method.Parameters){Scan-Attributes $param ($method.FullName+' parameter '+$param.Name)}
   if(!$method.HasBody){continue}
   $report.methodBodiesInspected++
   $report.instructionsInspected+=$method.Body.Instructions.Count
   $initializerCalls=New-Object 'System.Collections.Generic.List[string]'
   foreach($instruction in $method.Body.Instructions){
    $operand=$instruction.Operand;$owner=$null
    if($operand -is [Mono.Cecil.TypeReference]){$owner=$operand.FullName}
    elseif($operand -is [Mono.Cecil.MemberReference] -and $null -ne $operand.DeclaringType){$owner=$operand.DeclaringType.FullName}
    if(Is-Review $owner){$reviewIl.Add($method.FullName+' -> '+$operand.FullName)}
    if(Is-EditorTest $owner){$editorTestIl.Add($method.FullName+' -> '+$operand.FullName)}
    if($type.FullName -ceq 'TalesTactics.AndroidRuntimeSettings' -and $method.Name -ceq 'Initialize' -and $operand -is [Mono.Cecil.MethodReference]){$initializerCalls.Add($operand.FullName)}
   }
   if($type.FullName -ceq 'TalesTactics.AndroidRuntimeSettings' -and $method.Name -ceq 'Initialize'){
    $report.androidInitializer=[pscustomobject]@{method=$method.FullName;hasBody=$method.HasBody;attributes=@($method.CustomAttributes | ForEach-Object {$_.AttributeType.FullName});calls=@($initializerCalls.ToArray())}
   }
  }
 }
 $report.reviewIlReferences=@($reviewIl.ToArray() | Sort-Object -Unique)
 $report.editorOrTestIlReferences=@($editorTestIl.ToArray() | Sort-Object -Unique)
 $report.testAttributes=@($testAttributes.ToArray() | Sort-Object -Unique)
 $missing=@($required | Where-Object {$names -cnotcontains $_})
 Check 'Required platform, battle HUD and Alphen pilot production types retained' ($missing.Count -eq 0) $missing
 if($Platform -eq 'Android'){Check 'Android initialization method body retained' ($null -ne $report.androidInitializer -and $report.androidInitializer.hasBody) $report.androidInitializer}
 foreach($field in @('reviewFixtureTypes','reviewTypeReferences','reviewMemberReferences','reviewIlReferences','editorOrTestTypes','editorOrTestAssemblies','editorOrTestReferences','editorOrTestTypeReferences','editorOrTestMemberReferences','editorOrTestIlReferences','testAttributes')){
  Check ($field+' excluded') (@($report[$field]).Count -eq 0) $report[$field]
 }
 $report.buildContext.resultAndArtifactUnchangedDuringInspection=((Hash $buildResultPath) -ceq $report.buildContext.resultSHA256 -and (Hash $artifact) -ceq $report.buildContext.artifactSHA256 -and (Hash $finalPath) -ceq $report.buildContext.finalSHA256)
 if(!$report.buildContext.resultAndArtifactUnchangedDuringInspection){throw 'Build result, final settings report or output changed during assembly inspection.'}
 $report.runtimeSHA256After=Hash $runtime
 Check 'Runtime assembly unchanged during inspection' ($report.runtimeSHA256 -ceq $report.runtimeSHA256After) $report.runtimeSHA256After
 Check 'Nonempty method body inspection performed' ($report.methodBodiesInspected -gt 0 -and $report.instructionsInspected -gt 0) ([pscustomobject]@{methods=$report.methodBodiesInspected;instructions=$report.instructionsInspected})
}catch{$failures.Add($_.Exception.ToString())}
finally{if($null -ne $assembly){$assembly.Dispose()}}
$report.checks=@($checks.ToArray());$report.failures=@($failures.ToArray());$report.passed=$failures.Count -eq 0 -and $checks.Count -gt 0
$json=ConvertTo-Json -InputObject $report -Depth 12
[IO.File]::WriteAllText($output,$json,(New-Object Text.UTF8Encoding($false)))
[pscustomobject]@{passed=$report.passed;report=$output;runtimeSHA256=$report.runtimeSHA256;types=$report.typeCount;methods=$report.methodBodiesInspected;instructions=$report.instructionsInspected;checks=$checks.Count;failures=$report.failures} | ConvertTo-Json -Depth 4
if(!$report.passed){throw 'Player assembly isolation inspection failed; see report.'}