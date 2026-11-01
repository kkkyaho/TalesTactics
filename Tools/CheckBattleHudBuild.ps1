[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PlayerRoot,
    [switch]$Review,
    [string]$OutputPath
)
# Metadata/IL inspection only. Does not load or run game code, launch Unity/player,
# modify a build, or access the user's campaign/settings. JSON is also sent to stdout.
# Examples: ./Tools/CheckBattleHudBuild.ps1 -PlayerRoot D:/TalesTacticsBuildArchive/HudRedesignNormal
#           ./Tools/CheckBattleHudBuild.ps1 -PlayerRoot D:/TalesTacticsBuildArchive/HudRedesignReview -Review -OutputPath C:/path/new-result.json
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$outputValidated=$false
$failures=New-Object 'System.Collections.Generic.List[string]'
$checks=New-Object 'System.Collections.Generic.List[object]'
$report=[ordered]@{
    schema=1;passed=$false;review=[bool]$Review;checkedUtc=[DateTime]::UtcNow.ToString('o')
    playerRoot=$PlayerRoot;runtimeSHA256=$null;exeSHA256=$null;cecilPath=$null
    assemblies=@();checks=@();reviewFixtureTypes=@();reviewIlReferences=@()
    editorOrTestTypes=@();editorOrTestAssemblies=@();editorOrTestReferences=@();testAttributes=@()
    checksScope='Mono.Cecil metadata and IL inspection; no Unity tests or player execution. Storage isolation at runtime, visual quality, build warnings and gameplay are separate checks.'
    failures=@()
}
$knownReview=@('BattleHudRedesignReview','ReviewStorageBootstrap','CampaignFlowReview','CombatClarityReview','CombatPresentationReview','CrossoverPlayerReview','EnvironmentIntegrationReview','HelpGuideReview','IntermissionUxReview','ManualPlayerReview','OriginalSkillTrioReview','OrnateReview','QualityPresentationReview','SdMotionDirectionReview','SignatureWeaponReview','SkillRosterVfxFixture','SkillRosterVfxReview','TitleScreenReview','UiRefinementReview','UiSafeLayoutReview','VfxQualityReview','VfxUpgradeReview')
$knownExcluded=@($knownReview+@('CampaignPlayerReview','PersistencePlayerReview','MenuPreviewBake','MenuPreviewBakeRunner') | ForEach-Object {'TalesTactics.'+$_})
function Is-KnownReviewType([string]$Name){
    if(!$Name){return $false}
    # Include compiler-generated/nested types only under an explicitly named tool.
    $outer=($Name -split '/',2)[0]
    return $knownExcluded -ccontains $outer
}
function Hash([string]$Path){(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function All-Types($Collection){foreach($type in $Collection){$type;if($type.HasNestedTypes){All-Types $type.NestedTypes}}}
function Add-Check([string]$Name,[bool]$Passed,$Observed){
    $checks.Add([pscustomobject]@{name=$Name;passed=$Passed;observed=$Observed})
    if(!$Passed){$failures.Add($Name)}
}
try{
    if($OutputPath){
        if(![IO.Path]::IsPathRooted($OutputPath)){throw 'OutputPath must be absolute.'}
        $OutputPath=[IO.Path]::GetFullPath($OutputPath)
        if(Test-Path -LiteralPath $OutputPath){throw 'Choose a new OutputPath; earlier evidence is never overwritten.'}
        if(!(Test-Path -LiteralPath (Split-Path $OutputPath -Parent) -PathType Container)){throw 'OutputPath parent must already exist.'}
        $outputValidated=$true
    }
    if(![IO.Path]::IsPathRooted($PlayerRoot)){throw 'PlayerRoot must be an absolute existing Windows Mono build directory.'}
    $root=(Resolve-Path -LiteralPath $PlayerRoot).ProviderPath
    $report.playerRoot=$root
    $managed=Join-Path $root 'TalesTactics_Data/Managed'
    $runtime=Join-Path $managed 'TalesTactics.Runtime.dll'
    $exe=Join-Path $root 'TalesTactics.exe'
    if(!(Test-Path -LiteralPath $runtime -PathType Leaf) -or !(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Missing TalesTactics.exe or TalesTactics_Data/Managed/TalesTactics.Runtime.dll.'}
    $report.runtimeSHA256=Hash $runtime;$report.exeSHA256=Hash $exe
    $cecil=Get-ChildItem -LiteralPath (Join-Path $project 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.mono-cecil@*' | Sort-Object Name | Select-Object -First 1
    if(!$cecil){throw 'Import the Unity project before checking player assemblies (Mono.Cecil package is missing).'}
    $report.cecilPath=Join-Path $cecil.FullName 'Mono.Cecil.dll'
    Add-Type -LiteralPath $report.cecilPath
    $allNames=@();$assemblyRows=@();$editorRefs=@();$reviewIl=@();$testAttributes=@()
    $bootstrapCalled=$false;$bootstrapBeforeStorage=$false;$bootstrapHasSwitch=$false;$hudHasInitializer=$false
    foreach($dll in @(Get-ChildItem -LiteralPath $managed -File -Filter 'TalesTactics*.dll' | Sort-Object Name)){
        $assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll.FullName)
        try{
            $types=@(All-Types $assembly.MainModule.Types)
            $names=@($types | ForEach-Object FullName);$allNames+=$names
            foreach($type in $types){
                foreach($attribute in $type.CustomAttributes){if($attribute.AttributeType.FullName -match '^(NUnit\.|UnityEngine\.TestTools\.)'){$testAttributes+=$type.FullName+' : '+$attribute.AttributeType.FullName}}
                foreach($method in $type.Methods){
                    foreach($attribute in $method.CustomAttributes){if($attribute.AttributeType.FullName -match '^(NUnit\.|UnityEngine\.TestTools\.)'){$testAttributes+=$method.FullName+' : '+$attribute.AttributeType.FullName}}
                    if($type.FullName -ceq 'TalesTactics.BattleHudRedesignReview' -and $method.Name -ceq 'Initialize'){
                        $hudHasInitializer=@($method.CustomAttributes | Where-Object {$_.AttributeType.FullName -ceq 'UnityEngine.RuntimeInitializeOnLoadMethodAttribute'}).Count -gt 0
                    }
                    if(!$method.HasBody){continue}
                    $instructions=$method.Body.Instructions;$resolveIndex=-1;$configureIndex=-1
                    for($i=0;$i -lt $instructions.Count;$i++){
                        $operand=$instructions[$i].Operand;$owner=$null
                        if($operand -is [Mono.Cecil.TypeReference]){$owner=$operand.FullName}
                        elseif($operand -is [Mono.Cecil.MemberReference] -and $null -ne $operand.DeclaringType){$owner=$operand.DeclaringType.FullName}
                        if(Is-KnownReviewType $owner){$reviewIl+=$method.FullName+' -> '+$owner}
                        if($type.FullName -ceq 'TalesTactics.ReviewStorageBootstrap' -and $operand -is [string] -and $operand -ceq '--battle-hud-redesign-review'){$bootstrapHasSwitch=$true}
                        if($operand -is [Mono.Cecil.MethodReference] -and $type.FullName -ceq 'TalesTactics.BattleDirector' -and $method.Name -ceq 'Start'){
                            if($owner -ceq 'TalesTactics.ReviewStorageBootstrap' -and $operand.Name -ceq 'Resolve'){$resolveIndex=$i;$bootstrapCalled=$true}
                            if($owner -ceq 'TalesTactics.BattleDirector' -and $operand.Name -ceq 'ConfigureStorage' -and $configureIndex -lt 0){$configureIndex=$i}
                        }
                    }
                    if($resolveIndex -ge 0 -and $configureIndex -gt $resolveIndex){$bootstrapBeforeStorage=$true}
                }
            }
            $refs=@($assembly.MainModule.AssemblyReferences | ForEach-Object Name)
            $editorRefs+=@($refs | Where-Object {$_ -match 'UnityEditor|TalesTactics\.(Editor|.*Tests)|TestRunner|nunit'})
            $assemblyRows+=[pscustomobject]@{file=$dll.Name;sha256=(Hash $dll.FullName);typeCount=$names.Count;references=$refs}
        }finally{$assembly.Dispose()}
    }
    $report.assemblies=@($assemblyRows)
    $report.reviewFixtureTypes=@($allNames | Where-Object {Is-KnownReviewType $_} | Sort-Object -Unique)
    $report.reviewIlReferences=@($reviewIl | Sort-Object -Unique)
    $report.editorOrTestTypes=@($allNames | Where-Object {$_ -match '^TalesTactics\.(Editor|Tests|PlayModeTests)(\.|/|$)' -or $_ -cin @('TalesTactics.MenuPreviewBake','TalesTactics.MenuPreviewBakeRunner')} | Sort-Object -Unique)
    $report.editorOrTestReferences=@($editorRefs | Sort-Object -Unique)
    $report.testAttributes=@($testAttributes | Sort-Object -Unique)
    $report.editorOrTestAssemblies=@(Get-ChildItem -LiteralPath $managed -File -Filter '*.dll' | Where-Object {$_.Name -match 'UnityEditor|TalesTactics\.(Editor|.*Tests)|nunit|TestRunner'} | ForEach-Object Name)
    $requiredProduction=@('TalesTactics.BattleDirector','TalesTactics.BattleHud','TalesTactics.BattleSession','TalesTactics.UltimateSkillCue')
    $missingProduction=@($requiredProduction | Where-Object {$allNames -cnotcontains $_})
    Add-Check 'Production battle/HUD types present' ($missingProduction.Count -eq 0) $missingProduction
    foreach($typeName in @('TalesTactics.BattleHudRedesignReview','TalesTactics.ReviewStorageBootstrap')){
        $present=$allNames -ccontains $typeName
        Add-Check ($typeName+' matches requested build mode') ($present -eq [bool]$Review) $present
    }
    # These historical tools are DEBUG-only; -Review means a non-development player.
    foreach($typeName in @('TalesTactics.CampaignPlayerReview','TalesTactics.PersistencePlayerReview')){
        Add-Check ($typeName+' excluded from non-development build') ($allNames -cnotcontains $typeName) ($allNames -ccontains $typeName)
    }
    if($Review){
        $missing=@($knownReview | ForEach-Object {'TalesTactics.'+$_} | Where-Object {$allNames -cnotcontains $_})
        Add-Check 'Known REVIEW tools present' ($missing.Count -eq 0) $missing
        Add-Check 'Director invokes review storage bootstrap' $bootstrapCalled $bootstrapCalled
        Add-Check 'Review storage resolves before ConfigureStorage' $bootstrapBeforeStorage $bootstrapBeforeStorage
        Add-Check 'Bootstrap supports HUD review switch' $bootstrapHasSwitch $bootstrapHasSwitch
        Add-Check 'HUD review has runtime initializer' $hudHasInitializer $hudHasInitializer
    }else{
        Add-Check 'Known review/fixture types excluded' ($report.reviewFixtureTypes.Count -eq 0) $report.reviewFixtureTypes
        Add-Check 'Known review/fixture IL references excluded' ($report.reviewIlReferences.Count -eq 0) $report.reviewIlReferences
        Add-Check 'Director has no review storage bootstrap call' (!$bootstrapCalled) $bootstrapCalled
    }
    Add-Check 'Editor/test types excluded' ($report.editorOrTestTypes.Count -eq 0) $report.editorOrTestTypes
    Add-Check 'Editor/test assemblies excluded' ($report.editorOrTestAssemblies.Count -eq 0) $report.editorOrTestAssemblies
    Add-Check 'Editor/test references excluded' ($report.editorOrTestReferences.Count -eq 0) $report.editorOrTestReferences
    Add-Check 'NUnit/UnityTest attributes excluded' ($report.testAttributes.Count -eq 0) $report.testAttributes
}catch{
    $failures.Add($_.Exception.ToString())
}
$report.checks=@($checks.ToArray());$report.failures=@($failures.ToArray())
$report.passed=$failures.Count -eq 0 -and $checks.Count -gt 0
$json=ConvertTo-Json -InputObject $report -Depth 12
if($outputValidated -and $OutputPath -and !(Test-Path -LiteralPath $OutputPath) -and (Test-Path -LiteralPath (Split-Path $OutputPath -Parent) -PathType Container)){
    [IO.File]::WriteAllText($OutputPath,$json,(New-Object Text.UTF8Encoding($false)))
}
Write-Output $json
if(!$report.passed){throw 'Battle HUD build inspection failed; see JSON checks/failures.'}