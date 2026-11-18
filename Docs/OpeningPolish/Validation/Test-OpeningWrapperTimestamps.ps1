[CmdletBinding()]
param([string]$ProjectRoot='C:/dev/TalesTactics')
# Offline checks only: loads two pure functions from the wrapper AST; no player,
# Unity, user save access, network, or rewrite of an existing validation receipt.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$wrapper=Join-Path $ProjectRoot 'Docs/OpeningPolish/Validation/Validate-OpeningCampaignWindows.ps1'
$tokens=$null;$parseErrors=$null
$ast=[System.Management.Automation.Language.Parser]::ParseFile($wrapper,[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count -gt 0){throw 'Opening wrapper syntax failed.'}
foreach($name in @('Convert-ReportTimestampUtc','Assert-ReportTimestamps')){
    $definition=$ast.Find({param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$true)
    if($null -eq $definition){throw ('Missing pure wrapper function: '+$name)}
    Invoke-Expression $definition.Extent.Text
}
$rows=New-Object 'System.Collections.Generic.List[object]'
function Check([string]$Name,[scriptblock]$Action,[bool]$Reject=$false){
    $rejected=$false
    try{& $Action}catch{$rejected=$true}
    if($rejected -ne $Reject){throw ('Timestamp check failed: '+$Name)}
    $rows.Add([pscustomobject]@{name=$Name;passed=$true;expectedRejected=$Reject})
}
$startIso='2026-11-03T13:35:10.1234567Z';$finishIso='2026-11-03T13:38:27.7654321Z'
$start=[DateTimeOffset]::Parse($startIso,[Globalization.CultureInfo]::InvariantCulture).UtcDateTime
$finish=[DateTimeOffset]::Parse($finishIso,[Globalization.CultureInfo]::InvariantCulture).UtcDateTime
$launch=$start.AddSeconds(-41);$now=$finish.AddSeconds(4)
Check 'PowerShell 5 string timestamps retain UTC and fractional ticks' {
    if((Convert-ReportTimestampUtc $startIso).Ticks -ne $start.Ticks){throw 'UTC ticks changed.'}
    Assert-ReportTimestamps $startIso $finishIso $launch $now
}
Check 'Typed UTC DateTime remains UTC' {
    $value=Convert-ReportTimestampUtc $start
    if($value.Kind -ne [DateTimeKind]::Utc -or $value.Ticks -ne $start.Ticks){throw 'Typed UTC changed.'}
    Assert-ReportTimestamps $start $finish $launch $now
}
Check 'Typed local DateTime converts once' {
    if((Convert-ReportTimestampUtc $start.ToLocalTime()).Ticks -ne $start.Ticks){throw 'Local time converted incorrectly.'}
    Assert-ReportTimestamps $start.ToLocalTime() $finish.ToLocalTime() $launch $now
}
Check 'Typed DateTimeOffset +09:00 represents the same instant' {
    $offset=[DateTimeOffset]::new($start).ToOffset([TimeSpan]::FromHours(9))
    if((Convert-ReportTimestampUtc $offset).Ticks -ne $start.Ticks){throw 'Offset changed instant.'}
    Assert-ReportTimestamps $offset ([DateTimeOffset]::new($finish).ToOffset([TimeSpan]::FromHours(9))) $launch $now
}
Check 'ISO +09:00 string represents the same instant' {
    $offset=[DateTimeOffset]::new($start).ToOffset([TimeSpan]::FromHours(9)).ToString('o')
    if((Convert-ReportTimestampUtc $offset).Ticks -ne $start.Ticks){throw 'Offset string changed instant.'}
}
$decoded=('{"startedUtc":"'+$startIso+'","finishedUtc":"'+$finishIso+'"}')|ConvertFrom-Json
Check 'Current PowerShell ConvertFrom-Json timestamp types work without string coercion' {
    if((Convert-ReportTimestampUtc $decoded.startedUtc).Ticks -ne $start.Ticks){throw 'Decoded instant changed.'}
    Assert-ReportTimestamps $decoded.startedUtc $decoded.finishedUtc $launch $now
}
Check 'Missing timezone is rejected' {Convert-ReportTimestampUtc '2026-11-03T13:35:10'} $true
Check 'Unspecified DateTime kind is rejected' {Convert-ReportTimestampUtc ([DateTime]::SpecifyKind($start,[DateTimeKind]::Unspecified))} $true
Check 'Malformed ISO value is rejected' {Convert-ReportTimestampUtc '2026-99-03T13:35:10Z'} $true
Check 'Non-date input is rejected' {Convert-ReportTimestampUtc 42} $true
Check 'Start earlier than launch minus 5 seconds is rejected' {Assert-ReportTimestamps $launch.AddSeconds(-5.001) $finish $launch $now} $true
Check 'Finish earlier than start is rejected' {Assert-ReportTimestamps $start $start.AddTicks(-1) $launch $now} $true
Check 'Finish beyond now plus 5 seconds is rejected' {Assert-ReportTimestamps $start $now.AddSeconds(5.001) $launch $now} $true
Check 'Existing exactly 5 second tolerance boundaries remain valid' {Assert-ReportTimestamps $launch.AddSeconds(-5) $now.AddSeconds(5) $launch $now}
$output=Join-Path $ProjectRoot ('Logs/OpeningPolish/WrapperTimestampProbe-'+[Guid]::NewGuid().ToString('N'))
if(Test-Path -LiteralPath $output){throw 'Refusing to reuse evidence directory.'}
[IO.Directory]::CreateDirectory($output)|Out-Null
$receipt=[ordered]@{
    passed=$true;validationKind='offline-wrapper-timestamp-logic';gameOrUnityExecuted=$false
    shellVersion=$PSVersionTable.PSVersion.ToString();decodedJsonTimestampType=$decoded.startedUtc.GetType().FullName
    wrapper=$wrapper;wrapperSHA256=(Get-FileHash -LiteralPath $wrapper -Algorithm SHA256).Hash.ToLowerInvariant()
    testScript=$PSCommandPath;testScriptSHA256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    tests=$rows.ToArray();limits='Tests only timestamp normalization and the unchanged invocation bounds. No player report or prior failed wrapper run is promoted to a successful execution.'
}
$receiptPath=Join-Path $output 'result.json'
[IO.File]::WriteAllText($receiptPath,($receipt|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
Write-Output ('PASS '+$rows.Count+' offline timestamp checks: '+$receiptPath)
