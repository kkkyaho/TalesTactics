[CmdletBinding()]
param(
    [ValidateSet('Inspect','Review')][string]$Mode='Inspect',
    [string]$ProjectRoot='C:/dev/TalesTactics',
    [string]$NormalBuild='D:/TalesTacticsBuildArchive/PlaytestFeedbackWindows20261102Final2',
    [string]$ReviewBuild='D:/TalesTacticsBuildArchive/PlaytestFeedbackReview20261102Final3',
    [ValidateRange(30,180)][int]$TimeoutSeconds=180,
    [switch]$InteractiveWindow,
    [switch]$AllowExistingPlayers,
    [ValidateRange(0,2147483647)][int]$RequiredExistingPlayerId=0
)
# Adapted from Docs/CharacterDepth/Validation/Validate-CharacterDepthWindows.ps1.
# Inspect performs metadata/IL checks only. Review starts the isolated LocalMusicReview.
# Six supplied clips plus title/hub/story/battle/theme flows must produce 14 advancing,
# nonzero output rows. This is a device playback check, not whole-track listening.
# Hidden by default at 1024x768; opt in to a visible window with -InteractiveWindow.
# Preserves recursive user files/directories and existing player identities. Only this
# invocation's PID/start-time/executable may be signaled; no user files are restored.
# No Unity control, build, install, dependency download, or normal-player launch.
# Example: powershell -NoProfile -ExecutionPolicy Bypass -File <this-file> -Mode Review -AllowExistingPlayers
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
foreach($path in @($ProjectRoot,$NormalBuild,$ReviewBuild)){if(![IO.Path]::IsPathRooted($path)){throw 'Use absolute project/build paths.'}}
$project=[IO.Path]::GetFullPath($ProjectRoot);$normal=[IO.Path]::GetFullPath($NormalBuild);$review=[IO.Path]::GetFullPath($ReviewBuild)
$userRoot=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'))
$userTreeExcludedRootFiles=@('Player.log','Player-prev.log')
$runId=[Guid]::NewGuid().ToString('N');$started=[DateTime]::UtcNow
$windowStyle=if($InteractiveWindow){'Normal'}else{'Hidden'}
$evidence=Join-Path $project ('Logs/PlaytestFeedback20261102/LocalMusicWindows-'+$runId)
$runDirectory=Join-Path $review ('LocalMusicReviews/'+$runId)
$expectedStorage=Join-Path $runDirectory 'Storage';$reportPath=Join-Path $runDirectory 'result.json'
$playerLog=Join-Path $evidence 'player.log';$utf8=New-Object Text.UTF8Encoding($false)
$failures=New-Object 'System.Collections.Generic.List[string]'
$samples=New-Object 'System.Collections.Generic.List[object]'
$before=$null;$after=$null;$beforeCaptured=$false;$userUnchanged=$false;$stableBeforeLaunch=$false
$process=$null;$processIdentity=$null;$launchUtc=$null;$exitCode=$null;$forcedTermination=$false;$cleanupRequested=$false
$isolation=$null;$report=$null;$reportValid=$false;$audioManifest=@();$logFindings=@();$existingPids=@()
$existingPlayers=@();$existingPlayerResults=@();$existingPlayersStable=$true
$wrapperPath=$PSCommandPath;$wrapperHash=$null
$exe=Join-Path $review 'TalesTactics.exe';$runtime=Join-Path $review 'TalesTactics_Data/Managed/TalesTactics.Runtime.dll'
$exeHash=$null;$runtimeHash=$null;$finalEvidenceWritten=$false

function Write-Json([string]$Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-Json -InputObject $Value -Depth 100),$utf8)}
function Hash([string]$Path){(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Same-Path([string]$A,[string]$B){[IO.Path]::GetFullPath($A).TrimEnd('\','/').Equals([IO.Path]::GetFullPath($B).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)}
function Under-Path([string]$Child,[string]$Parent){[IO.Path]::GetFullPath($Child).StartsWith([IO.Path]::GetFullPath($Parent).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}
function Overlap-Path([string]$A,[string]$B){(Same-Path $A $B) -or (Under-Path $A $B) -or (Under-Path $B $A)}
function Get-UserTree {
    $exists=Test-Path -LiteralPath $userRoot -PathType Container;$files=@();$directories=@();$excludedFiles=@()
    if($exists){
        $rootItem=Get-Item -LiteralPath $userRoot -Force
        if(($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'User storage is a reparse point; refusing incomplete tree evidence.'}
        $items=@(Get-ChildItem -LiteralPath $userRoot -Recurse -Force | Sort-Object FullName)
        foreach($item in $items){
            if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw ('User tree contains a reparse point: '+$item.FullName)}
            $name=$item.FullName.Substring($userRoot.TrimEnd('\','/').Length+1).Replace('\','/')
            # Only Unity's two root live logs are volatile process output. Nested names,
            # other .log files, backups, temporary files and unknown files remain protected.
            if(!$item.PSIsContainer -and $userTreeExcludedRootFiles -icontains $name){$excludedFiles+=$name;continue}
            if($item.PSIsContainer){$directories+=$name}else{$files+=[pscustomobject]@{name=$name;bytes=$item.Length;sha256=(Hash $item.FullName)}}
        }
    }
    [pscustomobject]@{exists=[bool]$exists;directories=@($directories);files=@($files);excludedRootFileNames=$userTreeExcludedRootFiles;observedExcludedFiles=@($excludedFiles)}
}
function Same-Tree($A,$B){
    if($null -eq $A -or $null -eq $B -or $A.exists -ne $B.exists){return $false}
    $left=@($A.files|Sort-Object name|ForEach-Object {$_.name.ToLowerInvariant()+':'+$_.bytes+':'+$_.sha256})
    $right=@($B.files|Sort-Object name|ForEach-Object {$_.name.ToLowerInvariant()+':'+$_.bytes+':'+$_.sha256})
    $dirsA=@($A.directories|ForEach-Object {$_.ToLowerInvariant()}|Sort-Object);$dirsB=@($B.directories|ForEach-Object {$_.ToLowerInvariant()}|Sort-Object)
    ($left.Count -eq $right.Count) -and (($left -join "`n") -ceq ($right -join "`n")) -and ($dirsA.Count -eq $dirsB.Count) -and (($dirsA -join "`n") -ceq ($dirsB -join "`n"))
}
function All-Types($Collection){foreach($type in $Collection){$type;if($type.HasNestedTypes){All-Types $type.NestedTypes}}}
function Inspect-Player([string]$Root,[bool]$IsReview){
    $managed=Join-Path $Root 'TalesTactics_Data/Managed';$runtimePath=Join-Path $managed 'TalesTactics.Runtime.dll'
    if(!(Test-Path -LiteralPath (Join-Path $Root 'TalesTactics.exe') -PathType Leaf) -or !(Test-Path -LiteralPath $runtimePath -PathType Leaf)){throw "Missing Mono Windows build: $Root"}
    $assemblyRows=@();$allNames=@();$editorRefs=@();$reviewIlRefs=@();$backgroundCalls=@()
    $bootstrapCalled=$false;$bootstrapBeforeStorage=$false;$bootstrapHasSwitch=$false;$reviewHasInitializer=$false
    foreach($dll in @(Get-ChildItem -LiteralPath $managed -File -Filter 'TalesTactics*.dll'|Sort-Object Name)){
        $assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll.FullName)
        try{
            $types=@(All-Types $assembly.MainModule.Types);$names=@($types|ForEach-Object FullName);$allNames+=$names
            foreach($type in $types){foreach($method in @($type.Methods|Where-Object HasBody)){
                $instructions=$method.Body.Instructions;$resolveIndex=-1;$configureIndex=-1
                for($i=0;$i -lt $instructions.Count;$i++){
                    $instruction=$instructions[$i];$operand=$instruction.Operand;$owner=$null
                    if($operand -is [Mono.Cecil.TypeReference]){$owner=$operand.FullName}
                    elseif($operand -is [Mono.Cecil.MemberReference] -and $null -ne $operand.DeclaringType){$owner=$operand.DeclaringType.FullName}
                    if($null -ne $owner -and $owner -cmatch '(Review|Fixture)'){$reviewIlRefs+=$method.FullName+' -> '+$owner}
                    if($type.FullName -ceq 'TalesTactics.ReviewStorageBootstrap' -and $operand -is [string] -and $operand -ceq '--local-music-review'){$bootstrapHasSwitch=$true}
                    if($operand -isnot [Mono.Cecil.MethodReference]){continue}
                    if($type.FullName -ceq 'TalesTactics.BattleDirector' -and $method.Name -ceq 'Start'){
                        if($owner -ceq 'TalesTactics.ReviewStorageBootstrap' -and $operand.Name -ceq 'Resolve'){$resolveIndex=$i;$bootstrapCalled=$true}
                        if($owner -ceq 'TalesTactics.BattleDirector' -and $operand.Name -ceq 'ConfigureStorage' -and $configureIndex -lt 0){$configureIndex=$i}
                    }
                    if($type.FullName.StartsWith('TalesTactics.LocalMusicReview',[StringComparison]::Ordinal) -and $owner -ceq 'UnityEngine.Application' -and $operand.Name -ceq 'set_runInBackground'){
                        $previous=$i-1;while($previous -ge 0 -and $instructions[$previous].OpCode.Code.ToString() -ceq 'Nop'){$previous--}
                        if($previous -ge 0 -and $instructions[$previous].OpCode.Code.ToString() -ceq 'Ldc_I4_1'){$backgroundCalls+=$method.FullName}
                    }
                }
                if($resolveIndex -ge 0 -and $configureIndex -gt $resolveIndex){$bootstrapBeforeStorage=$true}
                if($type.FullName -ceq 'TalesTactics.LocalMusicReview' -and $method.Name -ceq 'Initialize'){
                    $reviewHasInitializer=@($method.CustomAttributes|Where-Object {$_.AttributeType.FullName -ceq 'UnityEngine.RuntimeInitializeOnLoadMethodAttribute'}).Count -gt 0
                }
            }}
            $refs=@($assembly.MainModule.AssemblyReferences|ForEach-Object Name)
            $editorRefs+=@($refs|Where-Object {$_ -match 'UnityEditor|TalesTactics\.Editor|TestRunner|nunit'})
            $assemblyRows+=[pscustomobject]@{file=$dll.Name;sha256=(Hash $dll.FullName);types=$names.Count;references=$refs}
        }finally{$assembly.Dispose()}
    }
    $reviewTypes=@($allNames|Where-Object {$_ -cmatch '(Review|Fixture)' -or $_ -cmatch 'MenuPreviewBake'}|Sort-Object -Unique)
    $editorTypes=@($allNames|Where-Object {$_ -match '^TalesTactics\.Editor\.'}|Sort-Object -Unique)
    $forbidden=@(Get-ChildItem -LiteralPath $managed -File -Filter '*.dll'|Where-Object {$_.Name -match 'UnityEditor|TalesTactics\.(Editor|.*Tests)|nunit|TestRunner'}|ForEach-Object Name)
    $required=@('TalesTactics.BattleDirector','TalesTactics.BattleAudio','TalesTactics.BattleMusicProgram')
    if($IsReview){$required+=@('TalesTactics.LocalMusicReview','TalesTactics.ReviewStorageBootstrap')}
    $missing=@($required|Where-Object {$allNames -cnotcontains $_})
    $debugPresent=@(@('TalesTactics.CampaignPlayerReview','TalesTactics.PersistencePlayerReview')|Where-Object {$allNames -ccontains $_})
    $reviewIlRefs=@($reviewIlRefs|Sort-Object -Unique)
    $contract=if($IsReview){$bootstrapCalled -and $bootstrapBeforeStorage -and $bootstrapHasSwitch -and $reviewHasInitializer -and $backgroundCalls.Count -gt 0}else{!$bootstrapCalled -and $reviewTypes.Count -eq 0 -and $reviewIlRefs.Count -eq 0}
    $passed=$contract -and $missing.Count -eq 0 -and $editorTypes.Count -eq 0 -and $editorRefs.Count -eq 0 -and $forbidden.Count -eq 0 -and $debugPresent.Count -eq 0
    [pscustomobject]@{passed=[bool]$passed;kind=$(if($IsReview){'TALESTACTICS_REVIEW, non-DEBUG'}else{'normal'});root=$Root;exeSHA256=(Hash (Join-Path $Root 'TalesTactics.exe'));runtimeSHA256=(Hash $runtimePath);assemblies=$assemblyRows;reviewFixtureTypes=$reviewTypes;reviewIlReferences=$reviewIlRefs;editorTypes=$editorTypes;editorOrTestReferences=@($editorRefs|Sort-Object -Unique);editorOrTestAssemblies=$forbidden;missingRequiredTypes=$missing;legacyDebugTypes=$debugPresent;directorCallsReviewBootstrap=$bootstrapCalled;bootstrapPrecedesConfigureStorage=$bootstrapBeforeStorage;bootstrapSupportsLocalMusicSwitch=$bootstrapHasSwitch;localMusicRuntimeInitializer=$reviewHasInitializer;backgroundTrueSetters=@($backgroundCalls|Sort-Object -Unique);method='Mono.Cecil metadata/IL only; no game assembly execution. Runtime storage/report evidence is a separate gate.'}
}
function Current-OwnedProcess {
    if($null -eq $processIdentity){return $null}
    $live=Get-Process -Id $processIdentity.id -ErrorAction SilentlyContinue
    if($null -eq $live -or $live.HasExited){return $null}
    if($live.StartTime.ToUniversalTime().Ticks -ne $processIdentity.startedTicks -or !(Same-Path $live.MainModule.FileName $processIdentity.exe)){throw 'PID identity changed; refusing to signal any process.'}
    return $live
}
function Existing-PlayerIdentity($Player){
    [pscustomobject]@{id=$Player.Id;startedTicks=$Player.StartTime.ToUniversalTime().Ticks;exe=$Player.MainModule.FileName}
}
function Check-ExistingPlayers {
    $checks=New-Object 'System.Collections.Generic.List[object]'
    foreach($identity in $existingPlayers){
        $live=Get-Process -Id $identity.id -ErrorAction SilentlyContinue
        $same=$null -ne $live -and !$live.HasExited
        if($same){$same=$live.StartTime.ToUniversalTime().Ticks -eq $identity.startedTicks -and (Same-Path $live.MainModule.FileName $identity.exe)}
        $checks.Add([pscustomobject]@{id=$identity.id;startedTicks=$identity.startedTicks;exe=$identity.exe;sameProcessStillRunning=[bool]$same;signaledByWrapper=$false})
    }
    return $checks.ToArray()
}
function Assert-FinitePositive($Value,[string]$Label){
    if($null -eq $Value){throw ('Missing numeric audio evidence: '+$Label)}
    $number=[double]$Value
    if([double]::IsNaN($number) -or [double]::IsInfinity($number) -or $number -le 0){throw ('Invalid numeric audio evidence: '+$Label)}
}
function Assert-Report($Data){
    if($null -eq $Data -or $Data.schema -ne 1 -or !$Data.complete -or !$Data.passed -or $Data.status -cne 'Passed'){throw 'No completed current-schema music report.'}
    if($Data.expectedRows -ne 14 -or $Data.completedRows -ne 14 -or @($Data.rows).Count -ne 14 -or @($Data.errors).Count -ne 0){throw 'Expected 14 completed audio rows and zero report errors.'}
    if(!$Data.userFilesUnchanged -or !$Data.directPlaybackCampaignUnchanged -or !$Data.directPlaybackRandomUnchanged -or !$Data.savedVolumeRestored -or !$Data.stopped){throw 'Music report failed a save/RNG/volume/stop preservation check.'}
    if($Data.originalTracks -ne 14 -or $Data.listeners -ne 1){throw 'Expected 14 original tracks retained and exactly one active listener.'}
    Assert-FinitePositive $Data.outputSampleRate 'output sample rate'
    Assert-FinitePositive $Data.dspBufferLength 'DSP buffer length'
    Assert-FinitePositive $Data.dspBufferCount 'DSP buffer count'
    if(!(Same-Path $Data.output $runDirectory) -or !(Same-Path $Data.storage $expectedStorage) -or !(Test-Path -LiteralPath $expectedStorage -PathType Container)){throw 'Runtime output/storage is not this new isolated run.'}
    $reportStarted=[DateTime]::Parse($Data.startedUtc).ToUniversalTime();$reportFinished=[DateTime]::Parse($Data.finishedUtc).ToUniversalTime()
    if($reportStarted -lt $launchUtc.AddSeconds(-5) -or $reportFinished -lt $reportStarted -or $reportFinished -gt [DateTime]::UtcNow.AddSeconds(5)){throw 'Report timestamps do not match this invocation.'}
    $expected=@(
        'title|title.local','new-game-hub|intermission.local','opening-story|story','opening-battle|battle','return-hub|intermission.local','return-title|title.local',
        'supplied-track|title.local','supplied-track|intermission.local','supplied-track|battle.spirit','supplied-track|battle.sky','supplied-track|battle.good-wine','supplied-track|battle.decisive',
        'ultimate-theme|cless.theme','ultimate-return|battle.decisive'
    )
    $newClipNames=@{
        'title.local'='99AA233359F955C515';'intermission.local'='Tales of Destiny 2 - Wilderness';'battle.spirit'='9953EC3359F9562305';
        'battle.sky'='9993893359F955C218';'battle.good-wine'='99898D3359F955C018';'battle.decisive'='99852C3359F955BE1A'
    }
    $seen=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $verified=New-Object 'System.Collections.Generic.List[object]'
    foreach($row in $Data.rows){
        $key=$row.scenario+'|'+$row.id
        if($expected -cnotcontains $key -or !$seen.Add($key)){throw ('Unexpected or duplicate audio row: '+$key)}
        if(!$row.playing -or !$row.loop -or $row.startSample -lt 0 -or $row.endSample -le $row.startSample){throw ('Music did not keep playing with an advancing cursor: '+$key)}
        if($row.loadState -cne 'Loaded' -or $row.channels -ne 2 -or $row.frequency -ne 44100 -or [Math]::Abs([double]$row.volume-.19) -gt .001){throw ('Unexpected playback/loading/volume format: '+$key)}
        foreach($field in @('length','peak','rms','dspEnd','volume')){Assert-FinitePositive $row.$field ($key+' '+$field)}
        if([double]::IsNaN([double]$row.dspStart) -or [double]::IsInfinity([double]$row.dspStart) -or $row.dspStart -lt 0 -or $row.dspEnd -le $row.dspStart){throw ('DSP did not advance: '+$key)}
        if($row.peak -le .00001 -or $row.rms -gt $row.peak+0.000001 -or $row.peak -gt 1.001){throw ('Invalid or silent output signal: '+$key)}
        if($newClipNames.ContainsKey($row.id)){
            if($row.clip -cne $newClipNames[$row.id] -or $row.loadType -cne 'Streaming' -or $row.length -le 80){throw ('Supplied clip identity or streaming contract mismatch: '+$key)}
        }elseif($row.clip -cne $row.id){throw ('Original clip identity mismatch: '+$key)}
        $verified.Add([pscustomobject]@{scenario=$row.scenario;id=$row.id;clip=$row.clip;loadType=$row.loadType;seconds=$row.length;startSample=$row.startSample;endSample=$row.endSample;dspStart=$row.dspStart;dspEnd=$row.dspEnd;peak=$row.peak;rms=$row.rms;volume=$row.volume;channels=$row.channels;frequency=$row.frequency})
    }
    if($seen.Count -ne 14 -or @($expected|Where-Object {!$seen.Contains($_)}).Count -ne 0){throw 'Audio scenario coverage incomplete.'}
    return $verified.ToArray()
}
if(!(Under-Path $evidence (Join-Path $project 'Logs/PlaytestFeedback20261102')) -or (Overlap-Path $evidence $userRoot)){throw 'Unsafe evidence path.'}
if(Test-Path -LiteralPath $evidence){throw 'Generated evidence path already exists; refusing to reuse it.'}
[IO.Directory]::CreateDirectory($evidence)|Out-Null
try{
    $wrapperHash=Hash $wrapperPath
    foreach($path in @($normal,$review,$runDirectory)){if(Overlap-Path $path $userRoot){throw 'Build/review paths must not overlap user storage.'}}
    if(Same-Path $normal $review){throw 'Normal and review builds must be distinct.'}
    if(!(Under-Path $runDirectory $review) -or !(Under-Path $expectedStorage $runDirectory)){throw 'Review output/storage escaped the dedicated build directory.'}
    $before=Get-UserTree;$beforeCaptured=$true;Write-Json (Join-Path $evidence 'user-tree-before.json') $before
    $cecil=@(Get-ChildItem -LiteralPath (Join-Path $project 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.mono-cecil@*'|Sort-Object Name)
    if($cecil.Count -ne 1){throw 'Expected one already imported Mono.Cecil package; no package will be installed.'}
    if($null -eq ('Mono.Cecil.AssemblyDefinition' -as [type])){Add-Type -LiteralPath (Join-Path $cecil[0].FullName 'Mono.Cecil.dll')}
    $normalInfo=Inspect-Player $normal $false;$reviewInfo=Inspect-Player $review $true
    $isolation=[pscustomobject]@{passed=($normalInfo.passed -and $reviewInfo.passed);normal=$normalInfo;review=$reviewInfo}
    Write-Json (Join-Path $evidence 'player-type-isolation.json') $isolation
    if(!$isolation.passed){throw 'Normal/review metadata or IL isolation failed; no player will start.'}
    $stableBeforeLaunch=Same-Tree $before (Get-UserTree)
    if(!$stableBeforeLaunch){throw 'User tree changed during read-only preflight; no player will start and nothing will be restored.'}
    if($Mode -eq 'Review'){
        $existingPlayers=@(Get-Process -Name 'TalesTactics' -ErrorAction SilentlyContinue|ForEach-Object {Existing-PlayerIdentity $_})
        $existingPids=@($existingPlayers|ForEach-Object id)
        Write-Json (Join-Path $evidence 'existing-players-before.json') $existingPlayers
        if($RequiredExistingPlayerId -gt 0 -and $existingPids -notcontains $RequiredExistingPlayerId){throw ('Required existing player is not running: '+$RequiredExistingPlayerId)}
        if($existingPids.Count -gt 0 -and !$AllowExistingPlayers){throw ('Existing TalesTactics processes are preserved. Use -AllowExistingPlayers to validate concurrently while checking their identities: '+($existingPids -join ', '))}
        if(Test-Path -LiteralPath $runDirectory){throw 'Run output already exists; refusing to overwrite or delete evidence.'}
        $exeHash=Hash $exe;$runtimeHash=Hash $runtime
        if($playerLog.Contains('"')){throw 'Invalid quote in generated log path.'}
        # Audio review uses a bounded hidden 1024x768 window; no graphics capture or resize requirement.
        $arguments=@('--local-music-review',$runId,'-screen-fullscreen','0','-screen-width','1024','-screen-height','768','-logFile',('"'+$playerLog+'"'))
        Write-Json (Join-Path $evidence 'launch-plan.json') ([ordered]@{exe=$exe;arguments=$arguments;workingDirectory=$review;windowStyle=$windowStyle;timeoutSeconds=$TimeoutSeconds;runDirectory=$runDirectory;expectedStorage=$expectedStorage;expectedReport=$reportPath;runInBackground='Set true by the music harness; confirmed in its iterator IL.';normalPlayerStarted=$false})
        $launchUtc=[DateTime]::UtcNow
        $process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $review -WindowStyle $windowStyle -PassThru
        $processIdentity=[pscustomobject]@{id=$process.Id;startedTicks=$process.StartTime.ToUniversalTime().Ticks;exe=$exe}
        Write-Json (Join-Path $evidence 'own-process.json') $processIdentity
        $deadline=$launchUtc.AddSeconds($TimeoutSeconds);$nextSample=$launchUtc
        while(!$process.WaitForExit(1000)){
            $now=[DateTime]::UtcNow
            if($now -ge $deadline){throw "Music review exceeded $TimeoutSeconds seconds; partial evidence is not a pass."}
            if($now -ge $nextSample){
                $live=Current-OwnedProcess
                if($null -ne $live){$samples.Add([pscustomobject]@{utc=$now.ToString('o');seconds=($now-$launchUtc).TotalSeconds;responding=$live.Responding;window=$live.MainWindowHandle.ToInt64();workingSet=$live.WorkingSet64;cpuSeconds=$live.TotalProcessorTime.TotalSeconds})}
                Write-Json (Join-Path $evidence 'process-samples.json') @($samples.ToArray())
                if(!(Same-Tree $before (Get-UserTree))){throw 'User tree changed during review; stopping only the owned player, without restoration.'}
                $existingPlayerResults=@(Check-ExistingPlayers)
                if(@($existingPlayerResults|Where-Object {!$_.sameProcessStillRunning}).Count -gt 0){throw 'A pre-existing player exited or changed identity during review. It was not signaled; this concurrent run cannot pass its preservation check.'}
                Write-Output ('RUNNING LocalMusic '+[Math]::Round(($now-$launchUtc).TotalSeconds)+'s '+$runDirectory)
                $nextSample=$now.AddSeconds(30)
            }
        }
        $process.WaitForExit();$exitCode=$process.ExitCode
        if(!(Test-Path -LiteralPath $reportPath -PathType Leaf)){throw 'Player exited without a final report.'}
        [IO.File]::Copy($reportPath,(Join-Path $evidence 'player-report.json'),$false)
        $report=Get-Content -LiteralPath $reportPath -Raw|ConvertFrom-Json
        if($exitCode -ne 0){throw "Music player exited $exitCode; preserving its report."}
        $audioManifest=@(Assert-Report $report);$reportValid=$true
        Write-Json (Join-Path $evidence 'audio-manifest.json') $audioManifest
        if((Hash $exe) -cne $exeHash -or (Hash $runtime) -cne $runtimeHash){throw 'Executable or runtime DLL changed during review.'}
    }
}catch{$failures.Add($_.ToString())}
finally{
    if($null -ne $process){
        try{
            $live=Current-OwnedProcess
            if($null -ne $live){
                $cleanupRequested=$true;$null=$live.CloseMainWindow()
                if(!$live.WaitForExit(3000)){
                    $live=Current-OwnedProcess
                    if($null -ne $live){$forcedTermination=$true;$live.Kill();if(!$live.WaitForExit(5000)){$failures.Add('Owned player did not exit after termination.')}}
                }
                $failures.Add('Owned player needed cleanup; only its own normal completion can pass.')
            }
            if($process.HasExited){$process.WaitForExit();$exitCode=$process.ExitCode}
        }catch{$failures.Add('Owned-process cleanup: '+$_.ToString())}
    }
    try{
        $existingPlayerResults=@(Check-ExistingPlayers)
        $existingPlayersStable=@($existingPlayerResults|Where-Object {!$_.sameProcessStillRunning}).Count -eq 0
        Write-Json (Join-Path $evidence 'existing-players-after.json') $existingPlayerResults
        if(!$existingPlayersStable){$failures.Add('A pre-existing player did not retain its running PID/start-time/executable identity. The wrapper did not signal it.')}
    }catch{$existingPlayersStable=$false;$failures.Add('Existing-player identity evidence: '+$_.ToString())}
    try{
        if(Test-Path -LiteralPath $reportPath -PathType Leaf){
            if(!(Test-Path -LiteralPath (Join-Path $evidence 'player-report.json'))){[IO.File]::Copy($reportPath,(Join-Path $evidence 'player-report.json'),$false)}
        }
        $after=Get-UserTree;Write-Json (Join-Path $evidence 'user-tree-after.json') $after
        $userUnchanged=$beforeCaptured -and (Same-Tree $before $after)
        if(!$userUnchanged){$failures.Add('User tree excluding only the two root Unity logs is not unchanged or could not be verified. No user files were restored.')}
    }catch{$failures.Add('Final user-tree/report evidence: '+$_.ToString())}
    try{
        if(Test-Path -LiteralPath $playerLog -PathType Leaf){
            $pattern='(?i)(\b[A-Za-z.]*Exception\b|(^|\s)(error|assertion failed)\s*[:!]|crash!!!|fatal error|could not load|failed to load|shader error|d3d11: failed|D3D device.*lost)'
            $logFindings=@(Select-String -LiteralPath $playerLog -Pattern $pattern|ForEach-Object {[pscustomobject]@{line=$_.LineNumber;text=$_.Line}})
            if($logFindings.Count -gt 0){$failures.Add('Player log contains error candidates; inspect without silently whitelisting.')}
            Write-Json (Join-Path $evidence 'log-findings.json') $logFindings
        }elseif($null -ne $launchUtc){$failures.Add('Player did not create the external log.')}
    }catch{$failures.Add('Player log evidence: '+$_.ToString())}
    try{
        if($null -ne $isolation){foreach($build in @($isolation.normal,$isolation.review)){
            if((Hash (Join-Path $build.root 'TalesTactics.exe')) -cne $build.exeSHA256){throw ('Executable changed after metadata inspection: '+$build.root)}
            $managed=Join-Path $build.root 'TalesTactics_Data/Managed'
            if(@(Get-ChildItem -LiteralPath $managed -File -Filter 'TalesTactics*.dll').Count -ne @($build.assemblies).Count){throw ('Assembly set changed after metadata inspection: '+$build.root)}
            foreach($dll in $build.assemblies){if((Hash (Join-Path $managed $dll.file)) -cne $dll.sha256){throw ('Assembly changed after metadata inspection: '+$dll.file)}}
        }}
    }catch{$failures.Add('Build stability evidence: '+$_.ToString())}
    try{
        if((Hash $wrapperPath) -cne $wrapperHash){$failures.Add('Validation wrapper changed during its run.')}
        $passed=$failures.Count -eq 0 -and $userUnchanged -and $stableBeforeLaunch -and $existingPlayersStable -and $null -ne $isolation -and $isolation.passed -and ($Mode -eq 'Inspect' -or ($reportValid -and $exitCode -eq 0 -and !$cleanupRequested -and !$forcedTermination))
        Write-Json (Join-Path $evidence 'validation.json') ([ordered]@{
            schema=1;validationKind='local-music';mode=$Mode;runId=$runId
            startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');passed=[bool]$passed
            wrapper=$wrapperPath;wrapperSHA256=$wrapperHash
            normalPlayerStarted=$false;executedReviewPlayer=($null -ne $launchUtc);windowStyle=$windowStyle;timeoutSeconds=$TimeoutSeconds
            normalBuild=$normal;reviewBuild=$review;sourceReport=$reportPath;output=$runDirectory;expectedStorage=$expectedStorage
            reportValid=$reportValid;expectedRows=14;verifiedAudioRows=$audioManifest.Count
            userRoot=$userRoot;userFilesUnchanged=$userUnchanged;preflightUserTreeStable=$stableBeforeLaunch
            userTreeExcludedRootFiles=$userTreeExcludedRootFiles
            baseline='Fresh recursive user-tree SHA256 snapshot excluding only root Player.log and Player-prev.log; no prior baseline dependency.'
            userFilesBefore=$before;userFilesAfter=$after
            allowExistingPlayers=[bool]$AllowExistingPlayers;requiredExistingPlayerId=$RequiredExistingPlayerId;existingPidsPreserved=$existingPids
            existingPlayers=$existingPlayers;existingPlayerChecks=$existingPlayerResults;existingPlayersUnchanged=$existingPlayersStable
            ownProcess=$processIdentity;exitCode=$exitCode;forcedTermination=$forcedTermination;cleanupRequested=$cleanupRequested
            exeSHA256=$exeHash;runtimeSHA256=$runtimeHash;playerLog=$playerLog;errors=@($failures.ToArray())
            limits=@(
                'Metadata/IL inspection is distinct from execution. Inspect mode never starts a player.'
                'The REVIEW run validates six supplied MP3s and 14 start/transition/theme playback intervals with advancing samples/DSP and nonzero output, not full songs, loop seams, listening quality, all campaign encounters or balance.'
                'User-tree evidence includes all files and empty directory paths except the exact case-insensitive root names Player.log and Player-prev.log, which are volatile Unity output and may be locked by existing players. Nested Player.log, other logs, unknown files, backups and temporary files remain included. Reparse points fail before launch. No user file is written, deleted, backed up or restored.'
                'Only the PID plus start time plus executable identity created here may be signaled. Existing players block launch unless -AllowExistingPlayers is supplied, then their identities are checked without signaling them.'
                'Concurrent existing players may independently change user storage. Any such change fails preservation evidence; this wrapper neither attributes the change nor restores files.'
                'Build success and normal-release startup must be established separately; this script does not build or start the normal player.'
            )
        })
        $finalEvidenceWritten=$true
    }catch{$failures.Add('Validation summary write: '+$_.ToString())}
    if($null -ne $process){$process.Dispose()}
}
if($failures.Count -gt 0 -or !$finalEvidenceWritten){Write-Output ('FAILED '+$evidence);throw ($failures -join "`n")}
Write-Output ('PASS '+$Mode+' '+$evidence)
