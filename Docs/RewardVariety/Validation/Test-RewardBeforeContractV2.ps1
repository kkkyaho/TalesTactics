[CmdletBinding()]
param(
    [string]$ProjectRoot='C:/dev/TalesTactics',
    [Parameter(Mandatory=$true)][string]$BeforeValidation
)
# Read-only strengthening of a previously completed real Before run. No game/Unity launch.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($ProjectRoot)
$wrapper=Join-Path $root 'Docs/RewardVariety/Validation/Validate-RewardCampaignWindows.ps1'
$contract=Join-Path $root 'Docs/RewardVariety/Validation/Reward-CampaignContract.ps1'
$pricesPath=Join-Path $root 'Docs/RewardVariety/Validation/equipment-prices.json'
if(![IO.Path]::IsPathRooted($BeforeValidation) -or !(Test-Path -LiteralPath $BeforeValidation -PathType Leaf)){throw 'Use an existing absolute Before validation path.'}
$sourceDirectory=Split-Path -Parent $BeforeValidation
$output=Join-Path $root ('Logs/RewardVariety/BeforeContractV2-'+[Guid]::NewGuid().ToString('N'))
if(Test-Path -LiteralPath $output){throw 'Refusing to overwrite strengthening evidence.'}
[IO.Directory]::CreateDirectory($output)|Out-Null
$utf8=[Text.UTF8Encoding]::new($false)
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$tokens,[ref]$parseErrors)
if(@($parseErrors).Count){throw 'Wrapper syntax is invalid.'}
foreach($name in @('Hash','Under-Path','Same-Path')){
    $definitions=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name},$true))
    if($definitions.Count -ne 1){throw ('Missing helper '+$name)}
    Invoke-Expression $definitions[0].Extent.Text
}
. $contract
function Source-Files([string]$Directory){@([IO.Directory]::GetFiles($Directory,'*',[IO.SearchOption]::AllDirectories)|Sort-Object|ForEach-Object {[pscustomobject]@{path=$_;sha256=(Hash $_);bytes=(Get-Item -LiteralPath $_).Length}})}
$beforeFiles=Source-Files $sourceDirectory
$validationHash=Hash $BeforeValidation;$contractHash=Hash $contract;$wrapperHash=Hash $wrapper;$priceHash=Hash $pricesPath
$validation=Get-Content -LiteralPath $BeforeValidation -Raw -Encoding UTF8|ConvertFrom-Json
$report=$null;$reportHash=$null;$mappingHash=$null;$economy=$null;$savedFiles=@();$errorText=$null;$passed=$false
try{
    if(!$validation.passed -or $validation.phase -cne 'Before' -or $validation.mode -cne 'Review' -or $validation.validationKind -cne 'reward-variety-campaign-flow' -or $validation.exitCode -ne 0 -or $validation.forcedTermination -or $validation.cleanupRequested){throw 'Only the successful real Before run can receive supplemental checks.'}
    if(!(Under-Path $validation.sourceReport $validation.output) -or !(Test-Path -LiteralPath $validation.sourceReport -PathType Leaf)){throw 'Source report is missing or outside the recorded run.'}
    $reportHash=Hash $validation.sourceReport
    if($reportHash -cne $validation.sourceReportSHA256 -or $wrapperHash -cne $validation.wrapperSHA256 -or $priceHash -cne $validation.priceManifestSHA256){throw 'Original report, unchanged wrapper or price bytes do not match the completed run.'}
    $mappingHash=Hash $validation.sourceManifest.path
    if($mappingHash -cne $validation.sourceManifest.sha256){throw 'Original source/build mapping bytes changed.'}
    $report=Get-Content -LiteralPath $validation.sourceReport -Raw -Encoding UTF8|ConvertFrom-Json
    if(!$report.complete -or $report.status -cne 'Completed' -or $report.completedStages -ne 20 -or @($report.stages).Count -ne 20 -or @($report.errors).Count -ne 0){throw 'The actual source report is not complete and error-free.'}
    $priceData=Get-Content -LiteralPath $pricesPath -Raw -Encoding UTF8|ConvertFrom-Json
    Assert-PriceManifest $priceData $root
    $prices=@{};foreach($item in $priceData.items){$prices[$item.id]=[int]$item.buyPrice}
    foreach($stage in $report.stages){
        foreach($side in @('before','after')){
            $path=Join-Path $validation.output ('episode-'+([int]$stage.episode).ToString('00')+'-'+$side+'-title-reload.json')
            $hash=Hash $path
            $expected=if($side -ceq 'before'){$stage.permanentStateBeforeReloadHash}else{$stage.permanentStateAfterReloadHash}
            if($hash -cne $expected){throw ('Actual saved JSON no longer matches the original report hash: '+$path)}
            $savedFiles+=[pscustomobject]@{episode=$stage.episode;side=$side;path=$path;sha256=$hash;bytes=(Get-Item -LiteralPath $path).Length}
        }
    }
    if($savedFiles.Count -ne 40){throw 'Expected all 40 original save JSON files.'}
    $economy=Assert-RewardEconomy $report Before $prices $validation.output
    if(!$economy.passed -or $economy.contractVersion -ne 2 -or @($economy.stages|Where-Object {!$_.playerRosterComplete}).Count -gt 0){throw 'Strengthened economic/roster contract failed.'}
    foreach($file in $savedFiles){if((Hash $file.path) -cne $file.sha256){throw 'Original saved JSON changed during the read-only check.'}}
    if((Hash $validation.sourceReport) -cne $reportHash -or (Hash $validation.sourceManifest.path) -cne $mappingHash -or (Hash $BeforeValidation) -cne $validationHash){throw 'Original evidence changed during the read-only check.'}
    if((Hash $contract) -cne $contractHash -or (Hash $wrapper) -cne $wrapperHash -or (Hash $pricesPath) -cne $priceHash){throw 'Strengthening source changed during execution.'}
    $passed=$true
}catch{$errorText=$_.ToString()}
$afterFiles=Source-Files $sourceDirectory
$originalArtifactsUnchanged=Same-Value $beforeFiles $afterFiles
if(!$originalArtifactsUnchanged){$passed=$false;$errorText+=' Original Before artifact tree changed during supplemental checks.'}
$receipt=[ordered]@{
    passed=[bool]$passed;validationKind='supplemental-existing-before-reward-contract-v2';newPlayerRun=$false;gameOrUnityExecuted=$false;shellVersion=$PSVersionTable.PSVersion.ToString();utc=[DateTime]::UtcNow.ToString('o')
    sourceValidation=$BeforeValidation;sourceValidationSHA256=$validationHash;sourceRunId=$validation.runId;sourceReport=$validation.sourceReport;sourceReportSHA256=$reportHash;sourceLabel=$validation.sourceLabel;sourceBuildMapping=$validation.sourceManifest.path;sourceBuildMappingSHA256=$mappingHash
    originalContractSHA256=$validation.contractSHA256;strengthenedContract=$contract;strengthenedContractSHA256=$contractHash;unchangedWrapper=$wrapper;wrapperSHA256=$wrapperHash;priceManifest=$pricesPath;priceManifestSHA256=$priceHash;script=$PSCommandPath;scriptSHA256=(Hash $PSCommandPath)
    originalArtifactsUnchanged=$originalArtifactsUnchanged;originalArtifactFiles=$beforeFiles;savedJsonFiles=$savedFiles;economy=$economy;error=$errorText
    limits='This reads the original successful 20-episode Before report, source/build mapping and all 40 title-reload save JSON files. It adds complete one-to-one Player/party and growth/economy checks without running a new player or changing original artifacts. PNG, process, runtime and user-tree results remain the original wrapper evidence; no new Unity test, build, sale or human playtest is claimed.'
}
$receiptPath=Join-Path $output 'result.json'
[IO.File]::WriteAllText($receiptPath,(ConvertTo-Json -InputObject $receipt -Depth 100),$utf8)
if(!$passed){Write-Output ('FAILED '+$receiptPath);throw $errorText}
Write-Output ('PASS strengthened actual Before: 20 rosters, 40 saved JSON files, '+$receiptPath)