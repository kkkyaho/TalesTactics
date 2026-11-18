[CmdletBinding()]
param([string]$ProjectRoot='C:/dev/TalesTactics')
# Offline only: reads historic evidence, writes synthetic fixtures under this new GUID.
# It never starts Unity/game, reads current user storage, or changes historical receipts.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($ProjectRoot)
$wrapper=Join-Path $root 'Docs/RewardVariety/Validation/Validate-RewardCampaignWindows.ps1'
$original=Join-Path $root 'Docs/OpeningPolish/Validation/Validate-OpeningCampaignWindows.ps1'
$contract=Join-Path $root 'Docs/RewardVariety/Validation/Reward-CampaignContract.ps1'
$pricesPath=Join-Path $root 'Docs/RewardVariety/Validation/equipment-prices.json'
$output=Join-Path $root ('Logs/RewardVariety/WrapperContractProbe-'+[Guid]::NewGuid().ToString('N'))
if(Test-Path -LiteralPath $output){throw 'Refusing to overwrite probe evidence.'}
[IO.Directory]::CreateDirectory($output)|Out-Null
$utf8=[Text.UTF8Encoding]::new($false)
function Save([string]$Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-Json -InputObject $Value -Depth 100),$utf8)}
function Clone($Value){return ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $Value -Depth 100)}
function Assert([bool]$Condition,[string]$Message){if(!$Condition){throw $Message}}
function Parse([string]$Path){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if(@($errors).Count){throw ('Parse errors: '+$Path)};return $ast}
function Definition($Ast,[string]$Name){$found=@($Ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $Name},$true));if($found.Count -ne 1){throw ('Function count mismatch: '+$Name)};return $found[0].Extent.Text}
$ast=Parse $wrapper;$oldAst=Parse $original;$null=Parse $contract
foreach($name in @('Hash','Under-Path','Same-Path')){Invoke-Expression (Definition $ast $name)}
. $contract
$priceData=Get-Content -LiteralPath $pricesPath -Raw -Encoding UTF8|ConvertFrom-Json
$prices=@{};foreach($item in $priceData.items){$prices[$item.id]=[int]$item.buyPrice}
$rows=New-Object 'System.Collections.Generic.List[object]'
function Check([string]$Name,[scriptblock]$Action,[bool]$Reject=$false){
    $errorText=$null;try{& $Action}catch{$errorText=$_.Exception.Message}
    $passed=($null -ne $errorText) -eq $Reject
    $rows.Add([pscustomobject]@{name=$Name;passed=$passed;expectedRejected=$Reject;observedError=$errorText})
}
Check 'Storage/process/UTC/PNG functions match verified OpeningPolish after newline normalization' {
    foreach($name in @('Same-Path','Under-Path','Overlap-Path','Get-UserTree','Same-Tree','Read-ProcessIdentitySnapshot','Get-VerifiedProcessIdentity','Current-OwnedProcess','Existing-PlayerIdentity','Check-ExistingPlayers','Read-PngSize','Convert-ReportTimestampUtc','Assert-ReportTimestamps')){
        Assert ((Definition $ast $name).Replace("`r`n","`n") -ceq (Definition $oldAst $name).Replace("`r`n","`n")) ('Changed proven protection: '+$name)
    }
}
Check 'All 27 frozen equipment source bytes and integer sale prices match' {Assert-PriceManifest $priceData $root;Assert (@($priceData.items).Count -eq 27) 'Unexpected source item count.'}
Check 'Candidate A expected IDs cover all 20 episodes' {
    $expected=@('guardian-medal','guardian-medal','guardian-medal','guardian-medal','guardian-medal','guardian-medal','training-staff','steel-fist','infantry-rifle','training-spear','reinforced-bow','steel-claw','steel-shield','tempered-armor','swift-boots','tempered-armor','focus-charm','tempered-armor','healer-badge','tempered-armor')
    foreach($ep in 1..20){Assert ((Get-GuaranteedRewardId $ep After) -ceq $expected[$ep-1]) ('Wrong mapping '+$ep);Assert ((Get-GuaranteedRewardId $ep Before) -ceq 'guardian-medal') 'Baseline mapping changed.'}
}
Check 'Out-of-range episode is rejected' {$null=Get-GuaranteedRewardId 21 After} $true
Check 'Unknown phase is rejected' {$null=Get-GuaranteedRewardId 7 Invalid} $true
Check 'Duplicate inventory entries are rejected' {$null=Read-ItemCounts @('guardian-medal:1','guardian-medal:1')} $true
Check 'Zero inventory count is rejected' {$null=Read-ItemCounts @('guardian-medal:0')} $true
Check 'Inventory above existing cap is rejected' {$null=Read-ItemCounts @('guardian-medal:100')} $true
Check 'Malformed inventory entry is rejected' {$null=Read-ItemCounts @('../guardian-medal:1')} $true
Check 'Missing item price is rejected' {$null=Read-Price $prices 'missing-item'} $true
$historicPath=Join-Path $root 'Docs/PlaytestFeedback/Validation/windows-campaign-player-report.json'
$historic=Get-Content -LiteralPath $historicPath -Raw -Encoding UTF8|ConvertFrom-Json
$historicHash=Hash $historicPath
$before=Clone $historic
$before|Add-Member -NotePropertyName profile -NotePropertyValue 'full-campaign' -Force
$before|Add-Member -NotePropertyName requestedStages -NotePropertyValue 20 -Force
foreach($stage in $before.stages){foreach($name in @('playerMoveWallSeconds','playerSkillWallSeconds','enemyPhaseWallSeconds')){if($null -eq $stage.PSObject.Properties[$name]){$stage|Add-Member -NotePropertyName $name -NotePropertyValue 0}}}
$beforePath=Join-Path $output 'synthetic-before-report.json';Save $beforePath $before
$beforeEconomy=Assert-RewardEconomy $before Before $prices $historic.output
Check 'Historic complete campaign satisfies independent unchanged economic ledger' {
    Assert ($beforeEconomy.passed -and $beforeEconomy.totalGoldReward -eq 12900 -and $beforeEconomy.fixedRewardSellValue -eq 4800 -and $beforeEconomy.episode7To20FixedRewardSellValue -eq 3360) 'Baseline economic summary differs.'
}
$after=Clone $before;$afterDirectory=Join-Path $output 'SyntheticAfter';[IO.Directory]::CreateDirectory($afterDirectory)|Out-Null
$rewardDelta=@{}
foreach($stage in $after.stages){
    $ep=[int]$stage.episode
    if($ep -ge 7){
        $id=Get-GuaranteedRewardId $ep After;$rewardDelta['guardian-medal']=[int]$rewardDelta['guardian-medal']-1;$rewardDelta[$id]=[int]$rewardDelta[$id]+1
        $reward=Read-ItemCounts $stage.rewardItems;$reward.Remove('guardian-medal');Add-ItemCount $reward $id 1;$stage.rewardItems=@(Item-Rows $reward)
    }
    $inventory=Read-ItemCounts $stage.inventoryAfterReward
    foreach($key in $rewardDelta.Keys){$inventory[$key]=(Item-Count $inventory $key)+$rewardDelta[$key]}
    if($ep -ge 7){$stage.inventoryAfterReward=@(Item-Rows $inventory)}
    $saved=Get-Content -LiteralPath (Join-Path $historic.output ('episode-'+$ep.ToString('00')+'-after-title-reload.json')) -Raw -Encoding UTF8|ConvertFrom-Json
    $saved.Inventory=@($inventory.Keys|Sort-Object|ForEach-Object {[pscustomobject]@{Id=$_;Count=$inventory[$_]}})
    Save (Join-Path $afterDirectory ('episode-'+$ep.ToString('00')+'-after-title-reload.json')) $saved
}
$afterEconomy=Assert-RewardEconomy $after After $prices $afterDirectory
Check 'Synthetic candidate A validates changed rewards and exact inventory transitions' {
    Assert ($afterEconomy.passed -and $afterEconomy.fixedRewardSellValue -eq 3850 -and $afterEconomy.episode7To20FixedRewardSellValue -eq 2410 -and $afterEconomy.finalGold -eq $beforeEconomy.finalGold) 'Candidate summary differs.'
}
$baseline=[pscustomobject]@{sourceReport=$beforePath;economy=$beforeEconomy;sourceLabel='Synthetic fixture derived from historic report, not a new game run';runtimeSHA256='synthetic'}
$baselinePath=Join-Path $output 'synthetic-before-validation.json';Save $baselinePath $baseline
function Compare-Fixture($Report){Compare-RewardCampaign $baseline $Report $afterEconomy 'Synthetic candidate A fixture' 'synthetic' $baselinePath}
Check 'Comparison passes invariant gates while reporting designed inventory differences' {
    $value=Compare-Fixture $after
    Assert ($value.conditionsEqual -and $value.rewardPlanMatches -and $value.bonusDropsEqual -and $value.partyPolicyEqual -and $value.openingSixUnchanged -and !$value.deterministicOutcomesEqual -and $value.laterFixedRewardSellValueDelta -eq -950) 'Incorrect comparison gates.'
}
Check 'Changed post-reward battle commands are reported without rejecting reward semantics' {
    $changed=Clone $after;$changed.stages[7].playerCommands++
    $value=Compare-Fixture $changed
    Assert ($value.conditionsEqual -and $value.rewardPlanMatches -and $value.openingSixUnchanged -and $value.stages[7].differentOutcomeFields -contains 'playerCommands') 'Later commands were lost or treated as equality requirement.'
}
Check 'Changed opening-six outcomes fail the opening preservation gate' {
    $changed=Clone $after;$changed.stages[0].playerCommands++;$value=Compare-Fixture $changed
    Assert (!$value.openingSixUnchanged) 'Opening change was not detected.'
}
Check 'Changed execution policy fails the conditions gate' {
    $changed=Clone $after;$changed.timeScale=2;$value=Compare-Fixture $changed;Assert (!$value.conditionsEqual) 'Time-scale drift was not detected.'
}
Check 'Missing phase-A guaranteed reward is rejected' {$changed=Clone $after;$changed.stages[6].rewardItems=@('guardian-medal:1');$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Changed deterministic bonus drop is rejected' {$changed=Clone $after;$changed.stages[0].rewardItems+=@('bronze-sword:1');$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Changed gold reward is rejected' {$changed=Clone $after;$changed.stages[6].goldReward++;$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Changed purchase price is rejected' {$changed=Clone $after;$changed.stages[0].purchases[0].price++;$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Changed purchase balance is rejected' {$changed=Clone $after;$changed.stages[0].purchases[0].goldAfter++;$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Unjoined purchaser is rejected' {$changed=Clone $after;$changed.stages[0].purchases[0].character='unknown';$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Missing cumulative reward inventory is rejected' {$changed=Clone $after;$changed.stages[6].inventoryAfterReward=@($changed.stages[6].inventoryAfterReward|Where-Object {$_ -cnotlike 'training-staff:*'});$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Changed saved growth is rejected' {$changed=Clone $after;@($changed.stages[6].units|Where-Object {$_.team -ceq 'Player'})[0].exitEXP++;$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Empty Player evidence cannot vacuously pass growth validation' {$changed=Clone $after;$changed.stages[6].units=@();$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'One missing Player row is rejected' {$changed=Clone $after;$id=$changed.stages[6].party[0];$changed.stages[6].units=@($changed.stages[6].units|Where-Object {!($_.team -ceq 'Player' -and $_.id -ceq $id)});$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Duplicate Player row is rejected' {$changed=Clone $after;$duplicate=Clone @($changed.stages[0].units|Where-Object {$_.team -ceq 'Player'})[0];$changed.stages[0].units+=@($duplicate);$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Additional unselected Player row is rejected' {$changed=Clone $after;$extra=Clone @($changed.stages[0].units|Where-Object {$_.team -ceq 'Player'})[0];$extra.id='unselected';$changed.stages[0].units+=@($extra);$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Same-size Player replacement outside the party is rejected' {$changed=Clone $after;@($changed.stages[6].units|Where-Object {$_.team -ceq 'Player'})[0].id='unselected';$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Duplicate party ID is rejected' {$changed=Clone $after;$changed.stages[0].party[1]=$changed.stages[0].party[0];$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Empty party is rejected' {$changed=Clone $after;$changed.stages[0].party=@();$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Seven party IDs are rejected' {$changed=Clone $after;$changed.stages[6].party+=@('seventh');$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Blank Player ID is rejected' {$changed=Clone $after;@($changed.stages[6].units|Where-Object {$_.team -ceq 'Player'})[0].id='';$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Case-altered Player ID is not an exact party match' {$changed=Clone $after;$unit=@($changed.stages[6].units|Where-Object {$_.team -ceq 'Player'})[0];$unit.id=$unit.id.ToUpperInvariant();$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Incomplete campaign cannot pass economic validation' {$changed=Clone $after;$changed.stages=@($changed.stages|Select-Object -First 19);$null=Assert-RewardEconomy $changed After $prices $afterDirectory} $true
Check 'Historical source report was not modified' {Assert ((Hash $historicPath) -ceq $historicHash) 'Historical evidence changed.'}
$passed=@($rows|Where-Object {!$_.passed}).Count -eq 0
$receipt=[ordered]@{passed=$passed;validationKind='offline-reward-wrapper-contract-v2';gameOrUnityExecuted=$false;shellVersion=$PSVersionTable.PSVersion.ToString();wrapper=$wrapper;wrapperSHA256=(Hash $wrapper);contract=$contract;contractSHA256=(Hash $contract);priceManifest=$pricesPath;priceManifestSHA256=(Hash $pricesPath);historicReport=$historicPath;historicReportSHA256=$historicHash;testScript=$PSCommandPath;testScriptSHA256=(Hash $PSCommandPath);tests=$rows.ToArray();limits='Pure logic checks plus read-only validation of earlier campaign evidence. Candidate A report/save fixtures are synthetic. This is not a new game playthrough, build, Unity test, sale or approval of prior failed evidence.'}
$receiptPath=Join-Path $output 'result.json';Save $receiptPath $receipt
if(!$passed){Write-Output ('FAILED '+$receiptPath);throw (@($rows|Where-Object {!$_.passed}|ForEach-Object {$_.name+': '+$_.observedError}) -join '; ')}
Write-Output ('PASS '+$rows.Count+' offline wrapper contract checks: '+$receiptPath)