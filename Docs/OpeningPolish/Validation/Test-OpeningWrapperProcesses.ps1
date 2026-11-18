[CmdletBinding()]
param([string]$ProjectRoot='C:/dev/TalesTactics')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$wrapper=Join-Path $ProjectRoot 'Docs/OpeningPolish/Validation/Validate-OpeningCampaignWindows.ps1'
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$tokens,[ref]$parseErrors)
if(@($parseErrors).Count -ne 0){throw 'Wrapper syntax is invalid.'}
$names=@('Same-Path','Read-ProcessIdentitySnapshot','Get-VerifiedProcessIdentity','Current-OwnedProcess','Existing-PlayerIdentity','Check-ExistingPlayers')
foreach($name in $names){
    $definitions=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name},$true))
    if($definitions.Count -ne 1){throw ('Expected one wrapper function: '+$name)}
    Invoke-Expression $definitions[0].Extent.Text
}
# Only the selected pure identity helpers above are loaded. The wrapper body is
# never invoked; this command replaces Get-Process with synthetic objects.
$script:FakeProcesses=@();$script:QueryCount=0
function Get-Process {
    param([int]$Id,[string]$ErrorAction)
    $index=[Math]::Min($script:QueryCount,$script:FakeProcesses.Count-1)
    $script:QueryCount++
    if($index -lt 0){throw 'No mocked process response configured.'}
    return $script:FakeProcesses[$index]
}
function Set-Fakes([object[]]$Values){$script:FakeProcesses=$Values;$script:QueryCount=0}
$start=[DateTime]::SpecifyKind([DateTime]'2026-11-03T12:00:00',[DateTimeKind]::Utc)
$ticks=$start.Ticks;$expectedPath='C:/identity-probe/TalesTactics.exe'
function Fake([int]$Id=42,[string]$Path=$expectedPath,[long]$TickOffset=0,[bool]$Exited=$false){
    $module=if([string]::IsNullOrWhiteSpace($Path)){$null}else{[pscustomobject]@{FileName=$Path}}
    $value=[pscustomobject]@{Id=$Id;HasExited=$Exited;StartTime=$start.AddTicks($TickOffset);MainModule=$module}
    $value|Add-Member -MemberType ScriptMethod -Name Refresh -Value {}
    return $value
}
function Assert([bool]$Condition,[string]$Message){if(!$Condition){throw $Message}}
function Reject([scriptblock]$Action,[string]$Pattern){
    $errorText=$null
    try{$null=& $Action}catch{$errorText=$_.Exception.Message}
    Assert ($null -ne $errorText) 'Expected rejection did not occur.'
    Assert ($errorText -like $Pattern) ('Unexpected rejection: '+$errorText)
}
$rows=New-Object 'System.Collections.Generic.List[object]'
function Check([string]$Name,[scriptblock]$Action){
    $clock=[Diagnostics.Stopwatch]::StartNew()
    try{
        & $Action
        $rows.Add([pscustomobject]@{name=$Name;passed=$true;queries=$script:QueryCount;elapsedMilliseconds=$clock.ElapsedMilliseconds;error=$null})
    }catch{
        $rows.Add([pscustomobject]@{name=$Name;passed=$false;queries=$script:QueryCount;elapsedMilliseconds=$clock.ElapsedMilliseconds;error=$_.Exception.Message})
    }
}
Check 'Null main module becomes valid before deadline' {
    $valid=Fake;Set-Fakes @((Fake -Path ''),$valid)
    $value=Get-VerifiedProcessIdentity 42 $ticks $expectedPath
    Assert ($value.process -eq $valid -and $script:QueryCount -eq 2) 'Expected second snapshot to establish identity.'
}
Check 'Never-valid path rejects within bounded wait' {
    Set-Fakes @((Fake -Path ''));$timer=[Diagnostics.Stopwatch]::StartNew()
    Reject {Get-VerifiedProcessIdentity 42 $ticks $expectedPath 25} '*bounded verification wait*'
    Assert ($script:QueryCount -ge 2 -and $timer.ElapsedMilliseconds -lt 1000) 'Retry was absent or exceeded the test deadline allowance.'
}
Check 'Absent process is a normal exit' {
    Set-Fakes @($null)
    Assert ($null -eq (Get-VerifiedProcessIdentity 42 $ticks $expectedPath)) 'Absent process was not returned as null.'
}
Check 'HasExited snapshot is a normal exit' {
    Set-Fakes @((Fake -Exited $true))
    Assert ($null -eq (Get-VerifiedProcessIdentity 42 $ticks $expectedPath)) 'Exited snapshot was not returned as null.'
}
Check 'Null module followed by exit is a normal exit' {
    Set-Fakes @((Fake -Path ''),$null)
    Assert ($null -eq (Get-VerifiedProcessIdentity 42 $ticks $expectedPath)) 'Exit during retry was not returned as null.'
    Assert ($script:QueryCount -eq 2) 'Unexpected retry count.'
}
Check 'PID mismatch rejects immediately' {
    Set-Fakes @((Fake -Id 43))
    Reject {Get-VerifiedProcessIdentity 42 $ticks $expectedPath} '*PID identity changed*'
    Assert ($script:QueryCount -eq 1) 'PID mismatch was retried.'
}
Check 'Start-time mismatch rejects immediately' {
    Set-Fakes @((Fake -TickOffset 1))
    Reject {Get-VerifiedProcessIdentity 42 $ticks $expectedPath} '*start time changed*'
    Assert ($script:QueryCount -eq 1) 'Start-time mismatch was retried.'
}
Check 'Executable mismatch rejects immediately' {
    Set-Fakes @((Fake -Path 'C:/identity-probe/Other.exe'))
    Reject {Get-VerifiedProcessIdentity 42 $ticks $expectedPath} '*Executable identity changed*'
    Assert ($script:QueryCount -eq 1) 'Executable mismatch was retried.'
}
Check 'Unpinned baseline rejects PID reuse during module wait' {
    Set-Fakes @((Fake -Path ''),(Fake -TickOffset 1))
    Reject {Get-VerifiedProcessIdentity 42} '*start time changed*'
    Assert ($script:QueryCount -eq 2) 'Baseline did not pin its first start time.'
}
Check 'Equivalent path casing is accepted' {
    Set-Fakes @((Fake -Path 'c:\IDENTITY-PROBE\TALESTACTICS.EXE'))
    Assert ($null -ne (Get-VerifiedProcessIdentity 42 $ticks $expectedPath)) 'Equivalent absolute path was rejected.'
}
Check 'Owned-process guard uses shared retry and exact identity' {
    $script:processIdentity=[pscustomobject]@{id=42;startedTicks=$ticks;exe=$expectedPath}
    $valid=Fake;Set-Fakes @((Fake -Path ''),$valid)
    Assert ((Current-OwnedProcess) -eq $valid) 'Owned guard returned an unverified or different process.'
    Assert ($script:QueryCount -eq 2) 'Owned guard did not retry the missing module.'
}
Check 'Existing baseline captures only a verified identity' {
    Set-Fakes @((Fake -Path ''),(Fake))
    $identity=Existing-PlayerIdentity ([pscustomobject]@{Id=42})
    Assert ($identity.id -eq 42 -and $identity.startedTicks -eq $ticks -and $identity.exe -ceq $expectedPath) 'Baseline identity is incomplete.'
    Assert ($script:QueryCount -eq 2) 'Existing baseline did not use the common getter.'
}
Check 'Exited baseline emits no null identity row' {
    Set-Fakes @($null)
    $identities=@(Existing-PlayerIdentity ([pscustomobject]@{Id=42}))
    Assert ($identities.Count -eq 0) 'Exited baseline emitted a null pipeline item.'
}
Check 'Existing-player exit is recorded as not preserved' {
    $script:existingPlayers=@([pscustomobject]@{id=42;startedTicks=$ticks;exe=$expectedPath})
    Set-Fakes @($null);$checks=@(Check-ExistingPlayers)
    Assert ($checks.Count -eq 1 -and !$checks[0].sameProcessStillRunning -and !$checks[0].signaledByWrapper) 'Exited existing player was falsely marked preserved.'
}
Check 'Existing-player mismatch still rejects' {
    $script:existingPlayers=@([pscustomobject]@{id=42;startedTicks=$ticks;exe=$expectedPath})
    Set-Fakes @((Fake -Path 'C:/identity-probe/Other.exe'))
    Reject {Check-ExistingPlayers} '*Executable identity changed*'
    Assert ($script:QueryCount -eq 1) 'Existing player mismatch was retried.'
}
Check 'Live property-read failure is not suppressed' {
    $bad=Fake;$bad.PSObject.Properties.Remove('MainModule')
    $bad|Add-Member -MemberType ScriptProperty -Name MainModule -Value {throw 'synthetic access denied'}
    Set-Fakes @($bad)
    Reject {Get-VerifiedProcessIdentity 42 $ticks $expectedPath 25} '*'
}
Check 'Property-read race followed by confirmed exit returns null' {
    $bad=Fake;$bad.PSObject.Properties.Remove('MainModule')
    $bad|Add-Member -MemberType ScriptProperty -Name MainModule -Value {throw 'synthetic read race'}
    Set-Fakes @($bad,$null)
    Assert ($null -eq (Get-VerifiedProcessIdentity 42 $ticks $expectedPath)) 'Confirmed exit after a read race was not returned as null.'
}
$evidence=Join-Path $ProjectRoot ('Logs/OpeningPolish/WrapperProcessProbe-'+[Guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory -Path $evidence
$result=[pscustomobject]@{
    validationKind='offline-wrapper-process-identity';startedUtc=[DateTime]::UtcNow.ToString('o')
    powershellVersion=$PSVersionTable.PSVersion.ToString()
    gameOrUnityExecuted=$false;realProcessAccess=$false;processSignalsSent=$false
    wrapper=$wrapper;wrapperSHA256=(Get-FileHash -LiteralPath $wrapper -Algorithm SHA256).Hash.ToLowerInvariant()
    testScriptSHA256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    passed=(@($rows|Where-Object {!$_.passed}).Count -eq 0);tests=$rows.Count;results=$rows.ToArray()
}
$resultPath=Join-Path $evidence 'result.json'
[IO.File]::WriteAllText($resultPath,(ConvertTo-Json -InputObject $result -Depth 20),(New-Object Text.UTF8Encoding($false)))
[pscustomobject]@{passed=$result.passed;tests=$result.tests;powershellVersion=$result.powershellVersion;result=$resultPath}|ConvertTo-Json
if(!$result.passed){$rows|Where-Object {!$_.passed}|Format-List;exit 1}
