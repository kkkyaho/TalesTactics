# Pure report/price checks. Dot-sourcing this file does not start a player or touch user storage.
# Values follow the approved candidate A; economic transactions are independently checked.
function Get-GuaranteedRewardId([int]$Episode,[string]$RewardPhase){
    if($Episode -lt 1 -or $Episode -gt 20 -or @('Before','After') -cnotcontains $RewardPhase){throw 'Invalid reward phase/episode.'}
    if($RewardPhase -ceq 'Before' -or $Episode -le 6){return 'guardian-medal'}
    return @('training-staff','steel-fist','infantry-rifle','training-spear','reinforced-bow','steel-claw','steel-shield','tempered-armor','swift-boots','tempered-armor','focus-charm','tempered-armor','healer-badge','tempered-armor')[$Episode-7]
}
function Assert-PriceManifest($Data,[string]$Root){
    if($Data.schema -ne 1 -or @($Data.items).Count -lt 14){throw 'Incomplete frozen price manifest.'}
    $seen=@{}
    foreach($item in $Data.items){
        if($seen.ContainsKey($item.id) -or $item.buyPrice -lt 0 -or $item.sellPrice -ne [Math]::Floor($item.buyPrice/2)){throw 'Invalid or duplicate price manifest item.'}
        $seen[$item.id]=$true;$path=[IO.Path]::GetFullPath((Join-Path $Root $item.source))
        if(!(Under-Path $path (Join-Path $Root 'Assets/TalesTactics/Content')) -or !(Test-Path -LiteralPath $path -PathType Leaf) -or (Hash $path) -cne $item.sourceSHA256){throw ('Price source bytes changed or escaped Content: '+$item.id)}
    }
}
function Read-ItemCounts($Values){
    $counts=@{}
    foreach($value in @($Values)){
        if($value -isnot [string] -or $value -cnotmatch '^([a-z0-9.-]+):([1-9][0-9]?)$'){throw ('Invalid item quantity entry: '+$value)}
        $id=$Matches[1];$count=[int]$Matches[2]
        if($counts.ContainsKey($id)){throw ('Duplicate item quantity entry: '+$id)}
        $counts[$id]=$count
    }
    return $counts
}
function Item-Count($Counts,[string]$Id){if($Counts.ContainsKey($Id)){return [int]$Counts[$Id]};return 0}
function Item-Rows($Counts){return @($Counts.Keys|Sort-Object|ForEach-Object {$_+':'+$Counts[$_]})}
function Same-Value($A,$B){return (ConvertTo-Json -InputObject $A -Depth 100 -Compress) -ceq (ConvertTo-Json -InputObject $B -Depth 100 -Compress)}
function Add-ItemCount($Counts,[string]$Id,[int]$Count){$Counts[$Id]=[Math]::Min(99,((Item-Count $Counts $Id)+$Count))}
function Read-Price($Prices,[string]$Id){if(!$Prices.ContainsKey($Id)){throw ('Missing frozen item price: '+$Id)};return [int]$Prices[$Id]}
function Assert-PlayerRoster($Stage){
    $party=@($Stage.party);$players=@($Stage.units|Where-Object {$_.team -ceq 'Player'})
    if($party.Count -lt 1 -or $party.Count -gt 6 -or $players.Count -lt 1 -or $players.Count -gt 6){throw ('Expected 1 through 6 party IDs and Player rows in episode '+$Stage.episode)}
    $partyIds=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($id in $party){if($id -isnot [string] -or [string]::IsNullOrWhiteSpace($id) -or !$partyIds.Add($id)){throw ('Missing or duplicate party ID in episode '+$Stage.episode)}}
    $playerIds=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($unit in $players){if($unit.id -isnot [string] -or [string]::IsNullOrWhiteSpace($unit.id) -or !$playerIds.Add($unit.id) -or !$partyIds.Contains($unit.id)){throw ('Missing, duplicate or unselected Player ID in episode '+$Stage.episode)}}
    if(!$partyIds.SetEquals($playerIds)){throw ('Player rows do not exactly cover the selected party in episode '+$Stage.episode)}
    return $players
}
function Assert-RewardEconomy($Data,[string]$RewardPhase,$Prices,[string]$RunDirectory){
    if(@($Data.stages).Count -ne 20 -or $Data.completedStages -ne 20){throw 'Economic validation requires all 20 episodes.'}
    $inventory=@{'bronze-sword'=1};$gold=300;$rows=@();$purchased=@{};$random=New-Object Random 17
    $totalSpent=0;$totalGold=0;$totalFixedSell=0;$laterFixedSell=0
    foreach($stage in $Data.stages){
        $ep=[int]$stage.episode
        $playerRows=@(Assert-PlayerRoster $stage)
        if($ep -ne $rows.Count+1 -or $stage.goldEntry -ne $gold){throw ('Episode order or carried gold differs: '+$ep)}
        $spent=0
        foreach($purchase in $stage.purchases){
            $price=Read-Price $Prices $purchase.item
            if($price -le 0 -or $purchase.price -ne $price -or $purchase.goldBefore -ne $gold -or $purchase.goldAfter -ne ($gold-$price) -or $gold -lt $price){throw ('Invalid price or purchase transaction in episode '+$ep)}
            if(@($stage.joined) -cnotcontains $purchase.character){throw ('Purchase recipient has not joined preparation in episode '+$ep)}
            $gold-=$price;$spent+=$price;Add-ItemCount $inventory $purchase.item 1;Add-ItemCount $purchased $purchase.item 1
        }
        if($stage.goldSpent -ne $spent -or $stage.goldBeforeReward -ne $gold -or $stage.goldReward -ne (120+50*$ep) -or $stage.goldAfterReward -ne ($gold+$stage.goldReward)){throw ('Gold reward or ledger differs in episode '+$ep)}
        $id=Get-GuaranteedRewardId $ep $RewardPhase;$fixedPrice=Read-Price $Prices $id
        $fixedSell=[int][Math]::Floor($fixedPrice/2);$expected=@{};Add-ItemCount $expected $id 1
        $roll=$random.Next(10000);$bonus=if($roll -lt 2500){'bronze-sword'}elseif($roll -lt 4000){'reinforced-armor'}else{''}
        if($bonus){Add-ItemCount $expected $bonus 1}
        $reward=Read-ItemCounts $stage.rewardItems
        if(!(Same-Value (Item-Rows $expected) (Item-Rows $reward))){throw ('Guaranteed reward or unchanged seed-17 additional drop differs in episode '+$ep)}
        $ownedBefore=Item-Count $inventory $id
        foreach($key in $reward.Keys){Add-ItemCount $inventory $key $reward[$key]}
        $actual=Read-ItemCounts $stage.inventoryAfterReward
        if(!(Same-Value (Item-Rows $inventory) (Item-Rows $actual))){throw ('Inventory is not previous inventory plus actual purchases and rewards in episode '+$ep)}
        $gold=[int]$stage.goldAfterReward;$totalSpent+=$spent;$totalGold+=$stage.goldReward;$totalFixedSell+=$fixedSell
        if($ep -ge 7){$laterFixedSell+=$fixedSell}
        $savePath=Join-Path $RunDirectory ('episode-'+$ep.ToString('00')+'-after-title-reload.json')
        $save=Get-Content -LiteralPath $savePath -Raw -Encoding UTF8|ConvertFrom-Json
        $savedInventory=Read-ItemCounts @($save.Inventory|ForEach-Object {$_.Id+':'+$_.Count})
        if($save.Gold -ne $gold -or !(Same-Value (Item-Rows $savedInventory) (Item-Rows $actual))){throw ('Saved inventory/gold differs from the reward report in episode '+$ep)}
        $equipped=@{}
        foreach($character in $save.Characters){foreach($itemId in $character.Equipment){if($itemId){Add-ItemCount $equipped $itemId 1}}}
        foreach($key in $equipped.Keys){if($equipped[$key] -gt (Item-Count $actual $key)){throw ('Equipped copies exceed ownership in episode '+$ep)}}
        foreach($unit in $playerRows){
            $level=[int]$unit.entryLevel;$exp=[int]$unit.entryEXP+[int]$unit.actionEXP+35
            if($unit.actionEXP -lt 0 -or $level -lt 1 -or $level -gt 50 -or $unit.entryEXP -lt 0 -or $unit.entryEXP -ge 100){throw 'Invalid player growth input.'}
            while($level -lt 50 -and $exp -ge 100){$exp-=100;$level++};if($level -ge 50){$exp=0}
            $saved=@($save.Characters|Where-Object {$_.Id -ceq $unit.id})
            if($unit.exitLevel -ne $level -or $unit.exitEXP -ne $exp -or $saved.Count -ne 1 -or $saved[0].Level -ne $level -or $saved[0].EXP -ne $exp){throw ('Growth is not action EXP plus unchanged 35 mission EXP: episode '+$ep+', '+$unit.id)}
        }
        $inventorySell=0
        foreach($key in $actual.Keys){$available=$actual[$key]-(Item-Count $equipped $key);$inventorySell+=$available*[int][Math]::Floor((Read-Price $Prices $key)/2)}
        $rows+= [pscustomobject]@{episode=$ep;playerRosterComplete=$true;validatedPlayerIds=@($playerRows|ForEach-Object id);guaranteedItem=$id;guaranteedCount=1;bonusItem=$bonus;rewardRoll=$roll;fixedRewardBuyPrice=$fixedPrice;fixedRewardSellValue=$fixedSell;alreadyOwnedAtReward=$ownedBefore;purchasedCopiesThroughThisEpisode=(Item-Count $purchased $id);ownedAfterReward=(Item-Count $actual $id);equippedAfterReward=(Item-Count $equipped $id);unequippedAfterReward=((Item-Count $actual $id)-(Item-Count $equipped $id));goldSpent=$spent;goldReward=$stage.goldReward;goldAfterReward=$gold;unequippedInventorySellValue=$inventorySell;inventory=(Item-Rows $actual)}
    }
    return [pscustomobject]@{contractVersion=2;passed=$true;phase=$RewardPhase;stages=$rows;totalGoldReward=$totalGold;totalPurchaseSpend=$totalSpent;finalGold=$gold;fixedRewardSellValue=$totalFixedSell;episode7To20FixedRewardSellValue=$laterFixedSell;startingGold=300;missionExperiencePerEpisode=35;rewardSeed=17;salesExecuted=0;limits='Sale values are price/2 arithmetic for unequipped ownership, not sale transactions or human economic/balance approval. Review source and binary identity are established separately; report, saved inventory, purchase arithmetic, fixed reward mapping, seeded additional drops and growth are checked here.'}
}
function Compare-RewardCampaign($BeforeValidation,$AfterReport,$AfterEconomy,[string]$AfterSource,[string]$AfterRuntimeHash,[string]$BaselinePath){
    $beforeReport=Get-Content -LiteralPath $BeforeValidation.sourceReport -Raw -Encoding UTF8|ConvertFrom-Json
    $beforeEconomy=$BeforeValidation.economy
    if(!$beforeEconomy.passed -or $beforeEconomy.phase -cne 'Before' -or @($beforeEconomy.stages).Count -ne 20){throw 'Baseline lacks full successful economic checks.'}
    $conditions=@('profile','requestedStages','policy','difficulty','timeScale','enemySpeedMode','teamTurns','reactions','autoTiming','missionEvents','purchases','ct','utilityEnemyAI','skipEnemyAnimations','purchasePolicy')
    $conditionChanges=@($conditions|Where-Object {!(Same-Value $beforeReport.$_ $AfterReport.$_)})
    $resultRows=@();$outcomesEqual=$true;$planMatches=$true;$dropsEqual=$true;$partyEqual=$true;$openingEqual=$true
    foreach($afterStage in $AfterReport.stages){
        $ep=[int]$afterStage.episode;$matches=@($beforeReport.stages|Where-Object {$_.episode -eq $ep})
        if($matches.Count -ne 1){throw 'Baseline is missing a main episode.'};$beforeStage=$matches[0]
        $beforeAudit=$beforeEconomy.stages[$ep-1];$afterAudit=$AfterEconomy.stages[$ep-1]
        $rewardExpected=Read-ItemCounts $beforeStage.rewardItems;$rewardExpected.Remove('guardian-medal')
        Add-ItemCount $rewardExpected (Get-GuaranteedRewardId $ep 'After') 1
        $rewardMatches=Same-Value (Item-Rows $rewardExpected) (Item-Rows (Read-ItemCounts $afterStage.rewardItems))
        $bonusEqual=$beforeAudit.bonusItem -ceq $afterAudit.bonusItem;$planMatches=$planMatches -and $rewardMatches;$dropsEqual=$dropsEqual -and $bonusEqual
        $sameParty=(Same-Value $beforeStage.party $afterStage.party) -and (Same-Value $beforeStage.joined $afterStage.joined);$partyEqual=$partyEqual -and $sameParty
        $fields=@('party','joined','units','purchases','playerCommands','playerPhases','enemyTurns','enemySkills','bossPreparations','bossSkills','goldEntry','goldSpent','goldBeforeReward','goldReward','goldAfterReward','rewardItems','inventoryAfterReward','permanentStateAfterReloadHash')
        $changes=@($fields|Where-Object {!(Same-Value $beforeStage.$_ $afterStage.$_)})
        $beforeActions=@($beforeStage.actions|Select-Object * -ExcludeProperty nominalWindupRecoverySeconds);$afterActions=@($afterStage.actions|Select-Object * -ExcludeProperty nominalWindupRecoverySeconds)
        if(!(Same-Value $beforeActions $afterActions)){$changes+=@('actions')}
        if($changes.Count -gt 0){$outcomesEqual=$false;if($ep -le 6){$openingEqual=$false}}
        $metrics=@()
        foreach($name in @('playerCommands','playerPhases','enemyTurns','enemySkills','goldEntry','goldSpent','goldReward','goldAfterReward','battleWallSeconds','playerMoveWallSeconds','playerSkillWallSeconds','enemyPhaseWallSeconds')){
            $a=[double]$beforeStage.$name;$b=[double]$afterStage.$name;$metrics+=[pscustomobject]@{metric=$name;before=$a;after=$b;delta=($b-$a)}
        }
        $beforeInventory=Read-ItemCounts $beforeStage.inventoryAfterReward;$afterInventory=Read-ItemCounts $afterStage.inventoryAfterReward;$inventoryChanges=@()
        foreach($id in @(@($beforeInventory.Keys)+@($afterInventory.Keys)|Sort-Object -Unique)){$a=Item-Count $beforeInventory $id;$b=Item-Count $afterInventory $id;if($a -ne $b){$inventoryChanges+=[pscustomobject]@{id=$id;before=$a;after=$b;delta=($b-$a)}}}
        $resultRows+=[pscustomobject]@{episode=$ep;outcomesEqual=($changes.Count -eq 0);differentOutcomeFields=$changes;rewardPlanMatches=$rewardMatches;bonusDropsEqual=$bonusEqual;partyPolicyEqual=$sameParty;beforeReward=$beforeAudit;afterReward=$afterAudit;beforePurchases=$beforeStage.purchases;afterPurchases=$afterStage.purchases;inventoryChanges=$inventoryChanges;metrics=$metrics;beforePlayerUnits=@($beforeStage.units|Where-Object {$_.team -ceq 'Player'});afterPlayerUnits=@($afterStage.units|Where-Object {$_.team -ceq 'Player'})}
    }
    return [pscustomobject]@{beforeSource=$BeforeValidation.sourceLabel;afterSource=$AfterSource;beforeRuntimeSHA256=$BeforeValidation.runtimeSHA256;afterRuntimeSHA256=$AfterRuntimeHash;beforeValidation=[IO.Path]::GetFullPath($BaselinePath);beforeValidationSHA256=(Hash $BaselinePath);conditionsEqual=($conditionChanges.Count -eq 0);differentConditions=$conditionChanges;rewardPlanMatches=[bool]$planMatches;bonusDropsEqual=[bool]$dropsEqual;partyPolicyEqual=[bool]$partyEqual;openingSixUnchanged=[bool]$openingEqual;deterministicOutcomesEqual=[bool]$outcomesEqual;beforePurchaseSpend=$beforeEconomy.totalPurchaseSpend;afterPurchaseSpend=$AfterEconomy.totalPurchaseSpend;beforeFinalGold=$beforeEconomy.finalGold;afterFinalGold=$AfterEconomy.finalGold;beforeLaterFixedRewardSellValue=$beforeEconomy.episode7To20FixedRewardSellValue;afterLaterFixedRewardSellValue=$AfterEconomy.episode7To20FixedRewardSellValue;laterFixedRewardSellValueDelta=($AfterEconomy.episode7To20FixedRewardSellValue-$beforeEconomy.episode7To20FixedRewardSellValue);stages=$resultRows;limitation='Single deterministic automated run per revision at timeScale 4. Different owned reward items can change later equipment, purchases, actions and growth; these are reported rather than forced equal. Opening episodes 1-6 must remain equal. No actual sales, human completion-time measurement, manual reward satisfaction or balance approval.'}
}