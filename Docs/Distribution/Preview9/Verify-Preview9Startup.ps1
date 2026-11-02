[CmdletBinding()]
param(
    [ValidateSet('Inspect','Startup')][string]$Mode='Inspect',
    [string]$ProjectRoot='C:/dev/TalesTactics',
    [string]$PlayerRoot='D:/TalesTacticsBuildArchive/Preview9Extracted',
    [string]$OutputDirectory,
    [ValidateRange(30,300)][int]$ObserveSeconds=30,
    [ValidateRange(5,60)][int]$ExitTimeoutSeconds=30,
    [switch]$AllowExistingPlayers,
    [switch]$InteractiveWindow,
    [switch]$RestoreWindowSettings
)
# Inspect never launches a player. Startup requires a verified extracted release manifest.
# Hidden by default. Only the owned PID/start time/executable may receive CloseMainWindow.
# No kill, Unity control, gameplay input, screenshot capture, or user-save restoration.
# Optional settings restoration is limited to exact backed-up settings.json/.bak bytes,
# window-only semantic changes, an otherwise unchanged user tree, no other players,
# and exclusive file locks with a fresh hash comparison. Concurrent changes fail closed.
# Run asynchronously from the caller, then poll the SAME shell session.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$utf8=New-Object Text.UTF8Encoding($false)
$project=[IO.Path]::GetFullPath($ProjectRoot);$player=[IO.Path]::GetFullPath($PlayerRoot)
$userRoot=Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'
$runId=[Guid]::NewGuid().ToString('N')
if(!$OutputDirectory){$OutputDirectory=Join-Path $project ('Logs/DistributionPreview9/Startup-'+$runId)}
if(![IO.Path]::IsPathRooted($OutputDirectory)){throw 'OutputDirectory must be a new absolute path.'}
$evidence=[IO.Path]::GetFullPath($OutputDirectory)
$exe=Join-Path $player 'TalesTactics.exe';$log=Join-Path $evidence 'player.log'
$errors=New-Object 'System.Collections.Generic.List[string]'
$samples=New-Object 'System.Collections.Generic.List[object]'
$before=$null;$after=$null;$observedAfter=$null;$buildBefore=$null;$buildAfter=$null
$existing=@();$existingAfter=@();$identity=$null;$process=$null;$launched=$false
$exitCode=$null;$normalCloseRequested=$false;$normalCloseAccepted=$false;$ownedStillRunning=$false
$observedSeconds=0.0;$initialized=$false;$responsive=$false;$logFindings=@()
$logCandidateCount=[long]0;$logScanComplete=$false;$logBytes=$null;$logSHA256=$null;$earlyRenderErrorObserved=$false
$settingsRestored=$false;$settingsDiff=@();$settingsBytes=@{};$inspection=$null;$otherPlayersObserved=$false
$packageBefore=$null;$packageAfter=$null;$started=[DateTime]::UtcNow
function Write-Json([string]$Path,$Value){
    if(Test-Path -LiteralPath $Path){throw ('Evidence already exists: '+$Path)}
    [IO.File]::WriteAllText($Path,(ConvertTo-Json -InputObject $Value -Depth 40),$utf8)
}
function Hash([string]$Path){(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Byte-Hash([byte[]]$Bytes){$sha=[Security.Cryptography.SHA256]::Create();try{[BitConverter]::ToString($sha.ComputeHash($Bytes)).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}}
function Read-LogPrefix([string]$Path){
    if(!(Test-Path -LiteralPath $Path -PathType Leaf)){return ''}
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try{
        $buffer=New-Object byte[] 16384;$used=0
        while($used -lt $buffer.Length){$count=$stream.Read($buffer,$used,$buffer.Length-$used);if($count -le 0){break};$used+=$count}
        return $utf8.GetString($buffer,0,$used)
    }finally{$stream.Dispose()}
}
function Same-Path([string]$A,[string]$B){[IO.Path]::GetFullPath($A).TrimEnd('\','/').Equals([IO.Path]::GetFullPath($B).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)}
function Under([string]$A,[string]$B){[IO.Path]::GetFullPath($A).StartsWith([IO.Path]::GetFullPath($B).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}
function Get-Tree([string]$Root,[bool]$User){
    $files=New-Object 'System.Collections.Generic.List[object]';$dirs=New-Object 'System.Collections.Generic.List[string]'
    $exists=Test-Path -LiteralPath $Root -PathType Container
    if($exists){
        $queue=New-Object 'System.Collections.Generic.Queue[string]';$queue.Enqueue($Root)
        while($queue.Count){
            $directory=$queue.Dequeue();$entry=Get-Item -LiteralPath $directory -Force
            if(($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked directories are not accepted.'}
            foreach($item in @(Get-ChildItem -LiteralPath $directory -Force)){
                if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked tree entries are not accepted.'}
                $name=$item.FullName.Substring($Root.TrimEnd('\','/').Length+1).Replace('\','/')
                if($item.PSIsContainer){$dirs.Add($name);$queue.Enqueue($item.FullName)}
                elseif(!($User -and $name -iin @('Player.log','Player-prev.log'))){$files.Add([pscustomobject]@{name=$name;bytes=$item.Length;sha256=(Hash $item.FullName)})}
            }
        }
    }
    [pscustomobject]@{exists=$exists;directories=@($dirs.ToArray()|Sort-Object);files=@($files.ToArray()|Sort-Object name)}
}
function Same-Tree($A,$B,[string[]]$Except=@()){
    if($null -eq $A -or $null -eq $B -or $A.exists -ne $B.exists){return $false}
    $left=@($A.files|Where-Object {$Except -inotcontains $_.name}|ForEach-Object {$_.name.ToLowerInvariant()+':'+$_.bytes+':'+$_.sha256})
    $right=@($B.files|Where-Object {$Except -inotcontains $_.name}|ForEach-Object {$_.name.ToLowerInvariant()+':'+$_.bytes+':'+$_.sha256})
    ($left.Count -eq $right.Count) -and (($left-join "`n") -ceq ($right-join "`n")) -and (($A.directories-join "`n") -ceq ($B.directories-join "`n"))
}
function Identity($P){[pscustomobject]@{id=$P.Id;startedTicks=$P.StartTime.ToUniversalTime().Ticks;exe=$P.Path}}
function Players{@(Get-Process -Name 'TalesTactics' -ErrorAction SilentlyContinue|ForEach-Object {Identity $_})}
function Same-Identity($A,$B){$null -ne $B -and $A.id -eq $B.id -and $A.startedTicks -eq $B.startedTicks -and (Same-Path $A.exe $B.exe)}
function Owned{
    if($null -eq $identity){return $null}
    $current=Get-Process -Id $identity.id -ErrorAction SilentlyContinue
    if($null -eq $current){return $null}
    if(!(Same-Identity $identity (Identity $current))){throw 'Owned PID identity changed; refusing to signal it.'}
    $current
}
function All-Types($Collection){foreach($type in $Collection){$type;if($type.HasNestedTypes){All-Types $type.NestedTypes}}}
function Inspect-Normal{
    $raw=& (Join-Path $project 'Tools/CheckBattleHudBuild.ps1') -PlayerRoot $player -OutputPath (Join-Path $evidence 'code-isolation-base.json')
    $base=($raw -join "`n")|ConvertFrom-Json
    if(!$base.passed){throw 'Existing normal-player metadata/IL inspection failed.'}
    $names=@();$badTypes=@();$badIl=@()
    foreach($dll in @(Get-ChildItem -LiteralPath (Join-Path $player 'TalesTactics_Data/Managed') -File -Filter 'TalesTactics*.dll')){
        $assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll.FullName)
        try{foreach($type in @(All-Types $assembly.MainModule.Types)){
            $names+=$type.FullName
            if($type.FullName -cmatch '(Review|Fixture)|MenuPreviewBake'){$badTypes+=$type.FullName}
            foreach($method in @($type.Methods|Where-Object HasBody)){foreach($instruction in $method.Body.Instructions){
                $operand=$instruction.Operand;$owner=$null
                if($operand -is [Mono.Cecil.TypeReference]){$owner=$operand.FullName}
                elseif($operand -is [Mono.Cecil.MemberReference] -and $null -ne $operand.DeclaringType){$owner=$operand.DeclaringType.FullName}
                if($owner -cmatch '(Review|Fixture)|MenuPreviewBake'){$badIl+=$method.FullName+' -> '+$owner}
            }}
        }}finally{$assembly.Dispose()}
    }
    $required=@('TalesTactics.StandingSpriteDepth','TalesTactics.CharacterIdlePresentation','TalesTactics.CampaignRecruitment','TalesTactics.BattleMusicProgram','TalesTactics.ProjectedUnitShadow')
    $missing=@($required|Where-Object {$names -cnotcontains $_})
    $result=[pscustomobject]@{passed=($base.passed -and !$badTypes.Count -and !$badIl.Count -and !$missing.Count);exeSHA256=$base.exeSHA256;runtimeSHA256=$base.runtimeSHA256;reviewTypes=$badTypes;reviewIlReferences=$badIl;missingProductionTypes=$missing;method='Existing CheckBattleHudBuild plus case-sensitive Review/Fixture metadata and IL scan; no assembly execution.'}
    Write-Json (Join-Path $evidence 'code-isolation.json') $result
    if(!$result.passed){throw 'Preview9 production/review isolation failed.'}
    $result
}
function Window-Only-Diff($Original,$Current){
    $a=@($Original.PSObject.Properties.Name|Sort-Object);$b=@($Current.PSObject.Properties.Name|Sort-Object)
    if(($a-join '|') -cne ($b-join '|')){throw 'Settings schema changed; refusing restoration.'}
    $changed=@()
    foreach($name in $a){
        if((ConvertTo-Json -InputObject $Original.$name -Compress -Depth 10) -cne (ConvertTo-Json -InputObject $Current.$name -Compress -Depth 10)){
            if($name -cnotin @('Width','Height','WindowMaximized')){throw ('Non-window setting changed: '+$name)}
            $changed+=$name
        }
    }
    if($Current.Width -lt 640 -or $Current.Width -gt 7680 -or $Current.Height -lt 480 -or $Current.Height -gt 4320 -or $Current.WindowMaximized -isnot [bool]){throw 'Invalid automatic window settings; refusing restoration.'}
    return $changed
}
function Restore-Settings($Observed){
    if(!$RestoreWindowSettings -or $existing.Count -ne 0 -or $otherPlayersObserved -or @(Players).Count -ne 0){throw 'Changed user files cannot be restored while another player may write, or without the explicit restoration option.'}
    if(!(Same-Tree $before $Observed @('settings.json','settings.json.bak'))){throw 'Non-settings user tree changed; nothing will be restored.'}
    $original=$utf8.GetString($settingsBytes['settings.json'])|ConvertFrom-Json
    $locks=@();$diffs=@()
    try{
        foreach($name in @('settings.json','settings.json.bak')){
            if(!$settingsBytes.ContainsKey($name)){throw 'Both existing settings files must have exact prelaunch backups.'}
            $path=Join-Path $userRoot $name
            $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            $locks+=@{name=$name;stream=$stream}
            $buffer=New-Object byte[] ([int]$stream.Length);$read=0
            while($read -lt $buffer.Length){$count=$stream.Read($buffer,$read,$buffer.Length-$read);if($count -le 0){throw 'Unexpected settings end of file.'};$read+=$count}
            $expected=@($Observed.files|Where-Object name -CEQ $name)
            if($expected.Count -ne 1 -or (Byte-Hash $buffer) -cne $expected[0].sha256){throw 'Settings changed after observation; refusing restoration.'}
            $previous=Byte-Hash $settingsBytes[$name]
            if((Byte-Hash $buffer) -cne $previous){$fields=@(Window-Only-Diff $original ($utf8.GetString($buffer)|ConvertFrom-Json));$diffs+=[pscustomobject]@{name=$name;changedFields=$fields;observedSHA256=(Byte-Hash $buffer);restoreSHA256=$previous}}
        }
        if(@(Players).Count -ne 0){throw 'A player appeared before restoration; refusing to overwrite settings.'}
        foreach($item in $locks){$bytes=$settingsBytes[$item.name];$item.stream.Position=0;$item.stream.Write($bytes,0,$bytes.Length);$item.stream.SetLength($bytes.Length);$item.stream.Flush($true)}
    }finally{foreach($item in $locks){$item.stream.Dispose()}}
    return $diffs
}
if((Same-Path $evidence $player) -or (Under $evidence $player) -or (Same-Path $evidence $userRoot) -or (Under $evidence $userRoot) -or (Under $userRoot $evidence)){throw 'Evidence must not overlap the player or user storage.'}
if(!(Test-Path -LiteralPath $exe -PathType Leaf) -or (Test-Path -LiteralPath $evidence)){throw 'Existing player and new evidence directory required.'}
[IO.Directory]::CreateDirectory($evidence)|Out-Null
$wrapperHash=Hash $PSCommandPath
try{
    $before=Get-Tree $userRoot $true;Write-Json (Join-Path $evidence 'user-tree-before.json') $before
    $existing=@(Players);$otherPlayersObserved=$existing.Count -gt 0;Write-Json (Join-Path $evidence 'existing-players-before.json') $existing
    $inspection=Inspect-Normal
    if($Mode -eq 'Startup'){
        if($existing.Count -gt 0 -and !$AllowExistingPlayers){throw 'Existing players are preserved; use AllowExistingPlayers to observe concurrently without restoring settings.'}
        $packageBefore=& (Join-Path $project 'Tools/Distribution/Verify-Package.ps1') -Directory $player
        if(!$packageBefore.passed -or $packageBefore.kind -cne 'release'){throw 'Startup requires a verified extracted release package.'}
        Write-Json (Join-Path $evidence 'package-before.json') $packageBefore
        $buildBefore=Get-Tree $player $false
        if($RestoreWindowSettings){foreach($name in @('settings.json','settings.json.bak')){
            $path=Join-Path $userRoot $name
            if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw 'Window-settings restoration requires two existing settings files.'}
            $settingsBytes[$name]=[IO.File]::ReadAllBytes($path)
            [IO.File]::WriteAllBytes((Join-Path $evidence ('original-'+$name)),$settingsBytes[$name])
        }}
        if(!(Same-Tree $before (Get-Tree $userRoot $true))){throw 'User tree changed during preflight; player not launched.'}
        $freshPlayers=@(Players)
        if($freshPlayers.Count -ne $existing.Count -or @($existing|Where-Object {$old=$_;@($freshPlayers|Where-Object {Same-Identity $old $_}).Count -ne 1}).Count){throw 'Player identities changed during preflight; refresh the baseline before launching.'}
        $style=if($InteractiveWindow){'Normal'}else{'Hidden'}
        if($log.Contains('"')){throw 'Unexpected quote in log path.'}
        $arguments=@('-logFile',('"'+$log+'"'))
        Write-Json (Join-Path $evidence 'launch-plan.json') ([ordered]@{exe=$exe;arguments=$arguments;windowStyle=$style;observeSeconds=$ObserveSeconds;exitTimeoutSeconds=$ExitTimeoutSeconds})
        $launch=[DateTime]::UtcNow
        $process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $player -WindowStyle $style -PassThru
        $identity=Identity $process;$launched=$true;Write-Json (Join-Path $evidence 'own-process.json') $identity
        Write-Output ('STARTED owned PID '+$identity.id+'; evidence '+$evidence)
        while(([DateTime]::UtcNow-$launch).TotalSeconds -lt $ObserveSeconds){
            if($process.WaitForExit(1000)){throw 'Player exited before the observation interval completed.'}
            $live=Owned;if($null -eq $live){throw 'Owned player disappeared.'};$live.Refresh()
            if(@(Players|Where-Object {!(Same-Identity $identity $_)}).Count){$otherPlayersObserved=$true}
            $samples.Add([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');seconds=([DateTime]::UtcNow-$launch).TotalSeconds;responding=$live.Responding;window=$live.MainWindowHandle.ToInt64();cpuSeconds=$live.TotalProcessorTime.TotalSeconds})
            $observedSeconds=([DateTime]::UtcNow-$launch).TotalSeconds
            if((Read-LogPrefix $log) -match 'NullReferenceException|Cannot create required material because shader is null'){
                $earlyRenderErrorObserved=$true
                throw 'Initial render failure detected in the first 16 KiB of the player log; stopping observation and requesting only the owned normal close.'
            }
        }
        $observedSeconds=([DateTime]::UtcNow-$launch).TotalSeconds
        $responsive=$samples.Count -gt 0 -and $samples[$samples.Count-1].responding
        if(!$responsive){throw 'Player is not responding at the end of the observation interval.'}
        $live=Owned;$normalCloseRequested=$true;$normalCloseAccepted=$live.CloseMainWindow()
        if(!$normalCloseAccepted -or !$live.WaitForExit($ExitTimeoutSeconds*1000)){throw 'Owned player did not exit normally after CloseMainWindow; no forced termination is attempted.'}
        $process.WaitForExit();$exitCode=$process.ExitCode
        if($exitCode -ne 0){throw ('Player exit code '+$exitCode)}
    }
}catch{$message=$_.Exception.Message;$errors.Add($message.Substring(0,[Math]::Min(1000,$message.Length)))}
finally{
    try{
        if($launched){$live=Owned;if($null -ne $live -and !$normalCloseRequested){$normalCloseRequested=$true;$normalCloseAccepted=$live.CloseMainWindow();if($normalCloseAccepted){$null=$live.WaitForExit($ExitTimeoutSeconds*1000)}};$ownedStillRunning=$null -ne (Owned);if($process.HasExited){$process.WaitForExit();$exitCode=$process.ExitCode}}
        if($ownedStillRunning){$errors.Add('Owned process remains running; only its exact identity is reported, not killed.')}
    }catch{$errors.Add('Owned process final check: '+$_.Exception.Message)}
    try{
        $current=@(Players)
        $existingAfter=@($existing|ForEach-Object {$old=$_;$same=@($current|Where-Object {Same-Identity $old $_}).Count -eq 1;[pscustomobject]@{identity=$old;sameProcessStillRunning=$same}})
        if(@($existingAfter|Where-Object {!$_.sameProcessStillRunning}).Count){$errors.Add('A pre-existing player changed identity or exited; it was not signaled.')}
        Write-Json (Join-Path $evidence 'existing-players-after.json') $existingAfter
        $observedAfter=Get-Tree $userRoot $true;Write-Json (Join-Path $evidence 'user-tree-observed-after.json') $observedAfter
        if($null -ne $before -and !(Same-Tree $before $observedAfter) -and $launched -and !$ownedStillRunning -and $RestoreWindowSettings){$settingsDiff=@(Restore-Settings $observedAfter);$settingsRestored=$true}
        $after=Get-Tree $userRoot $true;Write-Json (Join-Path $evidence 'user-tree-after.json') $after
        if(!(Same-Tree $before $after)){$errors.Add('User tree differs; no unconditional restoration is performed.')}
    }catch{$errors.Add('User storage preservation: '+$_.Exception.Message)}
    try{
        if($launched){
            if(!(Test-Path -LiteralPath $log -PathType Leaf)){throw 'Player log missing.'}
            if($ownedStillRunning){throw 'Player still writes its log; final complete log evidence is unavailable.'}
            $pattern='(?i)(\b[A-Za-z.]*Exception\b|(^|\s)(error|assertion failed)\s*[:!]|crash!!!|fatal error|could not load|failed to load|shader error|d3d11: failed|D3D device.*lost|Cannot create required material because shader is null)'
            $engineSeen=$false;$inputSeen=$false;$physicsSeen=$false;$lineNumber=[long]0
            $retained=New-Object 'System.Collections.Generic.List[object]'
            $stream=[IO.File]::Open($log,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
            $reader=New-Object IO.StreamReader($stream,$utf8,$true,4096)
            try{
                while($null -ne ($line=$reader.ReadLine())){
                    $lineNumber++
                    if($line.Contains('Initialize engine version: 6000.6.0f1')){$engineSeen=$true}
                    if($line.Contains('Input System module state changed to: Initialized.')){$inputSeen=$true}
                    if($line.Contains('[Physics::Module] Selected backend.')){$physicsSeen=$true}
                    if($line -match $pattern){$logCandidateCount++;if($retained.Count -lt 100){$retained.Add([pscustomobject]@{line=$lineNumber;text=$line})}}
                }
            }finally{$reader.Dispose();$stream.Dispose()}
            $logFindings=$retained.ToArray();$logScanComplete=$true
            $logBytes=(Get-Item -LiteralPath $log).Length;$logSHA256=Hash $log
            $initialized=$engineSeen -and $inputSeen -and $physicsSeen
            if(!$initialized -or $logCandidateCount -gt 0){$errors.Add('Initialization evidence missing or player log contains error candidates.')}
            $packageAfter=& (Join-Path $project 'Tools/Distribution/Verify-Package.ps1') -Directory $player
            $buildAfter=Get-Tree $player $false
            if(!$packageAfter.passed -or !(Same-Tree $buildBefore $buildAfter)){throw 'Extracted package changed during startup.'}
            Write-Json (Join-Path $evidence 'package-after.json') $packageAfter
        }
        if((Hash $PSCommandPath) -cne $wrapperHash){throw 'Startup wrapper changed during execution.'}
    }catch{$errors.Add('Player evidence: '+$_.Exception.Message)}
    Write-Json (Join-Path $evidence 'process-samples.json') @($samples.ToArray())
    Write-Json (Join-Path $evidence 'log-findings.json') ([ordered]@{schema=2;sourceLog=$log;sourceBytes=$logBytes;sourceSHA256=$logSHA256;scanComplete=$logScanComplete;candidateCount=$logCandidateCount;retainedCount=$logFindings.Count;truncated=($logCandidateCount -gt $logFindings.Count);candidates=$logFindings})
    $passed=$errors.Count -eq 0 -and $null -ne $inspection -and $inspection.passed -and (Same-Tree $before $after) -and ($Mode -eq 'Inspect' -or ($observedSeconds -ge $ObserveSeconds -and $responsive -and $initialized -and $normalCloseAccepted -and !$ownedStillRunning -and $exitCode -eq 0))
    $result=[ordered]@{schema=2;runId=$runId;mode=$Mode;passed=[bool]$passed;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');player=$player;evidence=$evidence;wrapperSHA256=$wrapperHash;inspection=$inspection;launched=$launched;ownProcess=$identity;secondsObserved=$observedSeconds;responding=$responsive;initialized=$initialized;normalCloseRequested=$normalCloseRequested;normalCloseAccepted=$normalCloseAccepted;exitCode=$exitCode;forcedTermination=$false;ownedStillRunning=$ownedStillRunning;existingPlayers=$existing;existingPlayerChecks=$existingAfter;userFilesUnchangedBeforeRestoration=(Same-Tree $before $observedAfter);userFilesUnchanged=(Same-Tree $before $after);userTreeExcludedRootFiles=@('Player.log','Player-prev.log');userTreeBefore=$before;userTreeAfter=$after;settingsRestored=$settingsRestored;settingsChanges=$settingsDiff;packageBefore=$packageBefore;packageAfter=$packageAfter;earlyRenderErrorObserved=$earlyRenderErrorObserved;playerLog=$log;playerLogBytes=$logBytes;playerLogSHA256=$logSHA256;playerLogScanComplete=$logScanComplete;logErrorCandidateCount=$logCandidateCount;logErrorCandidates=$logFindings;logFindingsTruncated=($logCandidateCount -gt $logFindings.Count);manualVisualVerification=$false;errors=@($errors.ToArray());limitations='Startup/initialization and normal-close evidence only; visual capture is separate. User-tree preservation excludes only root Unity live logs. Existing players are never signaled. No save restoration or forced process termination. Complete error-candidate count and first 100 candidates are recorded; the original player log is preserved with its size and SHA-256.'}
    Write-Json (Join-Path $evidence 'startup.json') $result
    Write-Output ([pscustomobject]@{passed=$passed;mode=$Mode;ownPid=$(if($null -ne $identity){$identity.id}else{$null});evidence=$evidence;errors=@($errors.ToArray())}|ConvertTo-Json -Depth 4)
}
if(!$passed){exit 1}
