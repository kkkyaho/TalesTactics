param(
    [Parameter(Mandatory=$true)][int]$TargetProcessId,
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [int]$Minutes=30
)
$ErrorActionPreference='Stop'
if($Minutes -lt 1 -or $Minutes -gt 240){throw 'Minutes must be between 1 and 240.'}
$player=Get-Process -Id $TargetProcessId
if($player.ProcessName -ne 'TalesTactics'){throw 'Target must be a TalesTactics player.'}
$started=[DateTime]::UtcNow
$deadline=$started.AddMinutes($Minutes)
$rows=@()
while([DateTime]::UtcNow -lt $deadline){
    $player=Get-Process -Id $TargetProcessId -ErrorAction SilentlyContinue
    if(!$player){break}
    $rows+=[pscustomobject]@{
        utc=[DateTime]::UtcNow.ToString('o')
        seconds=([DateTime]::UtcNow-$started).TotalSeconds
        workingSetMB=$player.WorkingSet64/1MB
        privateMB=$player.PrivateMemorySize64/1MB
        cpuSeconds=$player.CPU
        handles=$player.HandleCount
    }
    $rows|Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding utf8
    Start-Sleep -Seconds 15
}
"Saved $($rows.Count) process samples to $OutputPath"
