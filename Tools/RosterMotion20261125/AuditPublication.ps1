# Read-only publication audit. Does not stage, commit, fetch, push, or edit a repository.
# First commit the selected ROOT task files. Manifest contains exactly one relative path per line.
# Staged mode compares PUBLIC index blobs to ROOT SourceRef blobs; CommitId mode checks a sole-parent commit.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ExpectedParent,
    [Parameter(Mandatory=$true)][string]$ManifestPath,
    [string]$RootPath = 'C:\dev\TalesTactics',
    [string]$PublishPath = 'C:\dev\TalesTactics\Builds\SourceOnlyPublish20261118\Project',
    [string]$SourceRef = 'HEAD',
    [string]$CommitId,
    [string]$FreezePath = 'C:\dev\TalesTactics\Logs\RosterMotion20261125\validation-product-freeze.json'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-PilotGit {
    param([string]$Repository, [string[]]$Arguments)
    $result = @(& git -C $Repository @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) { throw ("git read failed: " + ($result -join ' ')) }
    return ($result -join [Environment]::NewLine).TrimEnd([char]13,[char]10)
}
function Resolve-PilotCommit {
    param([string]$Repository, [string]$Reference)
    if ($Reference -notmatch '^[A-Za-z0-9_./-]+$' -or $Reference.StartsWith('-')) {
        throw "Unsafe commit reference: $Reference"
    }
    return Invoke-PilotGit $Repository @('rev-parse','--verify',($Reference + '^{commit}'))
}
function Get-PilotSha256 {
    param([byte[]]$Bytes)
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($hasher.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant() }
    finally { $hasher.Dispose() }
}
function Read-PilotBlob {
    param([string]$Repository, [string]$Oid)
    if ($Oid -notmatch '^[0-9a-f]{40}([0-9a-f]{24})?$') { throw "Invalid blob id: $Oid" }
    $length = [long](Invoke-PilotGit $Repository @('cat-file','-s',$Oid))
    if ($length -gt 67108864) { throw "Blob exceeds this roster task's 64 MiB per-file limit: $Oid" }
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = (Get-Command git -CommandType Application | Select-Object -First 1).Source
    $start.Arguments = '-C "' + $Repository + '" cat-file blob ' + $Oid
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $stream = [IO.MemoryStream]::new()
    try {
        if (-not $process.Start()) { throw 'Unable to start blob reader.' }
        $process.StandardOutput.BaseStream.CopyTo($stream)
        $errorText = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "Blob read failed: $errorText" }
        if ($stream.Length -ne $length) { throw "Blob length mismatch: $Oid" }
        return ,$stream.ToArray()
    }
    finally { $stream.Dispose(); $process.Dispose() }
}
function Get-PilotTreeEntry {
    param([string]$Repository, [string]$Commit, [string]$Path)
    $entry = Invoke-PilotGit $Repository @('ls-tree',$Commit,'--',$Path)
    if ($entry -notmatch '^100644 blob ([0-9a-f]{40}(?:[0-9a-f]{24})?)\t(.+)$' -or $Matches[2] -cne $Path) {
        throw "Expected one regular source blob at $Commit : $Path"
    }
    return $Matches[1]
}

function Get-PilotFreshOrigin {
    param([string]$Repository, [string]$Expected)
    $approvedUrl = 'https://github.com/kkkyaho/TalesTactics.git'
    $fetchUrls = @((Invoke-PilotGit $Repository @('remote','get-url','--all','origin')) -split '[\r\n]+' | Where-Object { $_ })
    $pushUrls = @((Invoke-PilotGit $Repository @('remote','get-url','--push','--all','origin')) -split '[\r\n]+' | Where-Object { $_ })
    if ($fetchUrls.Count -ne 1 -or $pushUrls.Count -ne 1 -or $fetchUrls[0] -cne $approvedUrl -or $pushUrls[0] -cne $approvedUrl) {
        throw 'PUBLIC origin fetch and push URLs must both equal the single approved TalesTactics HTTPS URL.'
    }
    $remote = Invoke-PilotGit $Repository @('ls-remote','--exit-code','--refs','origin','refs/heads/develop')
    if ($remote -notmatch '^([0-9a-f]{40}(?:[0-9a-f]{24})?)\trefs/heads/develop$') {
        throw 'Fresh origin/develop lookup did not return exactly one full commit id.'
    }
    $actual = $Matches[1]
    if ($actual -cne $Expected) { throw "Fresh origin/develop differs from ExpectedParent: expected $Expected, remote $actual. Stop and prepare a new candidate from the current public parent." }
    return [pscustomobject]@{ url=$approvedUrl; ref='refs/heads/develop'; commit=$actual; checkedUtc=[DateTime]::UtcNow.ToString('o'); fetchAndPushUrlsVerified=$true }
}
function Get-PilotCandidateEntry {
    param([string]$Path)
    if ($mode -eq 'staged') {
        $entry = Invoke-PilotGit $publishFull @('ls-files','--stage','--',$Path)
        if ($entry -notmatch '^100644 ([0-9a-f]{40}(?:[0-9a-f]{24})?) 0\t(.+)$' -or $Matches[2] -cne $Path) {
            throw "Expected one regular, conflict-free index entry: $Path"
        }
        return $Matches[1]
    }
    return Get-PilotTreeEntry $publishFull $target $Path
}
function Read-PilotJsonBlob {
    param([string]$Repository, [string]$Oid)
    $data = Read-PilotBlob $Repository $Oid
    $decoder = [Text.UTF8Encoding]::new($false,$true)
    return ($decoder.GetString($data).TrimStart([char]0xFEFF) | ConvertFrom-Json)
}
function Get-PilotPngDimensions {
    param([byte[]]$Bytes)
    if ($Bytes.Length -lt 24 -or ([BitConverter]::ToString($Bytes,0,8)) -cne '89-50-4E-47-0D-0A-1A-0A' -or [Text.Encoding]::ASCII.GetString($Bytes,12,4) -cne 'IHDR') {
        throw 'Native sprite blob is not a PNG with an IHDR header.'
    }
    $width = [long]$Bytes[16]*16777216 + [long]$Bytes[17]*65536 + [long]$Bytes[18]*256 + [long]$Bytes[19]
    $height = [long]$Bytes[20]*16777216 + [long]$Bytes[21]*65536 + [long]$Bytes[22]*256 + [long]$Bytes[23]
    if ($width -le 0 -or $height -le 0) { throw 'PNG dimensions must be positive.' }
    return [pscustomobject]@{ width=$width; height=$height }
}

$rootFull = [IO.Path]::GetFullPath($RootPath).TrimEnd('\','/')
$publishFull = [IO.Path]::GetFullPath($PublishPath).TrimEnd('\','/')
if ($rootFull -ine 'C:\dev\TalesTactics' -or
    $publishFull -ine 'C:\dev\TalesTactics\Builds\SourceOnlyPublish20261118\Project') {
    throw 'This task-specific audit is restricted to the ROOT and source-only PUBLIC checkouts.'
}
if ((Invoke-PilotGit $rootFull @('branch','--show-current')) -cne 'develop') { throw 'ROOT must remain on develop.' }
if ((Invoke-PilotGit $publishFull @('branch','--show-current')) -cne 'codex/publish-source-20261118') {
    throw 'PUBLIC must remain on the approved source-only publication branch.'
}
if ($ExpectedParent -notmatch '^[0-9a-fA-F]{40}([0-9a-fA-F]{24})?$') { throw 'ExpectedParent must be a full commit id.' }
$expected = Resolve-PilotCommit $publishFull $ExpectedParent
if ($expected -cne $ExpectedParent.ToLowerInvariant()) { throw 'Expected parent did not resolve exactly.' }
$originBefore = Get-PilotFreshOrigin $publishFull $expected
$source = Resolve-PilotCommit $rootFull $SourceRef
$publishHead = Resolve-PilotCommit $publishFull 'HEAD'
$mode = 'staged'
$target = $null
if ($CommitId) {
    if ($CommitId -notmatch '^[0-9a-fA-F]{40}([0-9a-fA-F]{24})?$') { throw 'CommitId must be a full commit id.' }
    $mode = 'commit'
    $target = Resolve-PilotCommit $publishFull $CommitId
    if ($target -cne $publishHead) { throw 'CommitId must equal PUBLIC HEAD.' }
    $parents = (Invoke-PilotGit $publishFull @('show','-s','--format=%P',$target)).Split(' ',[StringSplitOptions]::RemoveEmptyEntries)
    if ($parents.Count -ne 1 -or $parents[0] -cne $expected) { throw 'Candidate must have exactly ExpectedParent as its sole parent.' }
    if (Invoke-PilotGit $publishFull @('diff','--cached','--name-only')) { throw 'Commit mode requires an empty PUBLIC index diff.' }
}
elseif ($publishHead -cne $expected) { throw 'PUBLIC HEAD must equal ExpectedParent before committing.' }

$exactAllowed = @(
    'Assets/TalesTactics/PlayModeTests/CharacterPoseSceneTests.cs',
    'Assets/TalesTactics/PlayModeTests/CombatEffectsSceneTests.cs',
    'Assets/TalesTactics/PlayModeTests/EnvironmentGroundingTests.cs',
    'Assets/TalesTactics/PlayModeTests/PresentationSceneTests.cs',
    'Assets/TalesTactics/PlayModeTests/LocomotionSceneTests.cs',
    'README.md',
    'Docs/TODO.md',
    'Docs/ARCHITECTURE.md',
    'Docs/VALIDATION.md',
    'Assets/TalesTactics/Runtime/Presentation/BoardView.cs',
    'Assets/TalesTactics/Runtime/Presentation/CharacterMotion.cs',
    'Assets/TalesTactics/Runtime/Presentation/AlphenVisualPilotMotion.cs',
    'Assets/TalesTactics/Resources/TalesTactics/UnitHeadAnchors.json',
    'Assets/TalesTactics/Art/RosterMotion20261125.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion.meta',
    'Assets/TalesTactics/Runtime/Presentation/CharacterMotionProfile.cs',
    'Assets/TalesTactics/Runtime/Presentation/CharacterMotionProfile.cs.meta',
    'Assets/TalesTactics/Runtime/Presentation/CharacterMotionRegistry.cs',
    'Assets/TalesTactics/Runtime/Presentation/CharacterMotionRegistry.cs.meta',
    'Assets/TalesTactics/Tests/CharacterMotionProfileTests.cs',
    'Assets/TalesTactics/Tests/CharacterMotionProfileTests.cs.meta',
    'Assets/TalesTactics/Editor/RosterMotionArtImporter.cs',
    'Assets/TalesTactics/Editor/RosterMotionArtImporter.cs.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/cless.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/cless/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/cless/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/cless/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/cless/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/cless.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/cless.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/farah.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/farah/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/farah/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/farah/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/farah/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/farah.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/farah.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/jade.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/jade/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/jade/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/jade/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/jade/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/jade.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/jade.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/kisara.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/kisara/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/kisara/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/kisara/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/kisara/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/kisara.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/kisara.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/mint.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/mint/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/mint/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/mint/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/mint/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/mint.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/mint.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/natalia.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/natalia/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/natalia/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/natalia/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/natalia/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/natalia.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/natalia.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/shionne.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/shionne/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/shionne/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/shionne/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/shionne/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/shionne.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/shionne.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/tear.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/tear/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/tear/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/tear/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/tear/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/tear.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/tear.asset.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/velvet.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/velvet/idle-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/velvet/idle-v1.png.meta',
    'Assets/TalesTactics/Art/RosterMotion20261125/velvet/action-v1.png',
    'Assets/TalesTactics/Art/RosterMotion20261125/velvet/action-v1.png.meta',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/velvet.asset',
    'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/velvet.asset.meta'
)
$manifest = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$caseFolded = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($line in [IO.File]::ReadAllLines([IO.Path]::GetFullPath($ManifestPath))) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
    if ($line -cne $line.Trim() -or $line -notmatch '^[A-Za-z0-9_./-]+$' -or
        $line.StartsWith('/') -or $line -match '(^|/)\.{1,2}(/|$)' -or $line.Contains('//')) {
        throw "Manifest requires canonical relative paths: $line"
    }
    if (-not $manifest.Add($line) -or -not $caseFolded.Add($line)) { throw "Duplicate/case-colliding manifest path: $line" }
    if ($line -match '(?i)(^|/)(LocalMusic|Audio|Builds|Logs|Library|Temp|UserSettings|ProtectedBytes|Saves)(/|\.|$)' -or
        $line -match '(?i)\.(mp3|wav|ogg|flac|m4a|aac|mp4|webm|apk|aab|zip|7z|rar|exe|dll|pdb)(\.|$)') {
        throw "Private music, build, backup, or binary payload path blocked: $line"
    }
    $allowedDoc = $line -cmatch '^Docs/RosterMotion20261125/[A-Za-z0-9_./-]+\.(md|json|png|html|txt|xml|csv|sha256)$'
    $allowedTool = $line -cmatch '^Tools/RosterMotion20261125/[A-Za-z0-9_./-]+\.(ps1|py|cs|json|txt|md|html|cjs)$'
    if ($exactAllowed -cnotcontains $line -and -not $allowedDoc -and -not $allowedTool) {
        throw "Path is outside this roster task; pre-existing user changes/private assets are not allowed: $line"
    }
}
if ($manifest.Count -eq 0) { throw 'Manifest is empty.' }

$diffArgs = @('diff','--no-renames','--name-status','--diff-filter=ACDMRTUXB')
if ($mode -eq 'staged') { $diffArgs += @('--cached',$expected,'--') }
else { $diffArgs += @($expected,$target,'--') }
$diffText = Invoke-PilotGit $publishFull $diffArgs
$changed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($line in ($diffText -split '[\r\n]+')) {
    if (-not $line) { continue }
    if ($line -notmatch '^([AM])\t(.+)$') { throw "Only added/modified regular files are allowed: $line" }
    $path = $Matches[2]
    if (-not $manifest.Contains($path)) { throw "Unapproved staged/committed change: $path" }
    [void]$changed.Add($path)
}
if (-not $changed.SetEquals($manifest)) {
    $missing = @($manifest | Where-Object { -not $changed.Contains($_) })
    throw ("Manifest must exactly match the candidate diff; absent: " + ($missing -join ', '))
}

$rows = @()
$utf8Strict = [Text.UTF8Encoding]::new($false,$true)
foreach ($path in ($manifest | Sort-Object)) {
    $sourceOid = Get-PilotTreeEntry $rootFull $source $path
    $candidateOid = Get-PilotCandidateEntry $path
    $sourceBytes = Read-PilotBlob $rootFull $sourceOid
    $candidateBytes = Read-PilotBlob $publishFull $candidateOid
    $sourceSha = Get-PilotSha256 $sourceBytes
    $candidateSha = Get-PilotSha256 $candidateBytes
    if ($sourceSha -cne $candidateSha -or $sourceBytes.Length -ne $candidateBytes.Length) {
        throw "Candidate blob differs from committed ROOT source: $path"
    }
    if ($path.EndsWith('.png',[StringComparison]::Ordinal)) {
        if ($candidateBytes.Length -lt 8 -or ([BitConverter]::ToString($candidateBytes,0,8)) -cne '89-50-4E-47-0D-0A-1A-0A') {
            throw "Expected native PNG bytes: $path"
        }
    }
    else {
        $content = $utf8Strict.GetString($candidateBytes)
        if ($content.IndexOf([char]0) -ge 0) { throw "Binary content under a text path is blocked: $path" }
    }
    $rows += [ordered]@{ path=$path; sourceBlob=$sourceOid; candidateBlob=$candidateOid; bytes=$candidateBytes.Length; sha256=$candidateSha }
}
# Bind immutable source/candidate Git blobs to the exact bytes used by final product validation.
$freezeFull = [IO.Path]::GetFullPath($FreezePath)
if ($freezeFull -ine 'C:\dev\TalesTactics\Logs\RosterMotion20261125\validation-product-freeze.json') {
    throw 'FreezePath must name this task''s original local validation-product-freeze.json.'
}
$freezeBytes = [IO.File]::ReadAllBytes($freezeFull)
$freezeSHA256 = Get-PilotSha256 $freezeBytes
$freeze = $utf8Strict.GetString($freezeBytes).TrimStart([char]0xFEFF) | ConvertFrom-Json
if ($freeze.schemaVersion -ne 1 -or @($freeze.files).Count -ne 83) { throw 'Expected the exact 83-file product/import-layout freeze.' }
$frozenPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$frozenRows = @()
$frozenByPath = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
foreach ($entry in $freeze.files) {
    $path = [string]$entry.relative
    if ($path -notmatch '^[A-Za-z0-9_./-]+$' -or $path.StartsWith('/') -or $path.Contains('//') -or $path -match '(^|/)\.{1,2}(/|$)' -or -not $frozenPaths.Add($path)) {
        throw "Invalid or duplicate frozen product path: $path"
    }
    if (($exactAllowed -cnotcontains $path -and $path -cne 'Docs/RosterMotion20261125/art-layout.json') -or $entry.sha256 -cnotmatch '^[0-9a-f]{64}$' -or $entry.bytes -lt 0) {
        throw "Unexpected frozen product path or fingerprint: $path"
    }
    $sourceOid = Get-PilotTreeEntry $rootFull $source $path
    $candidateOid = Get-PilotCandidateEntry $path
    $sourceBytes = Read-PilotBlob $rootFull $sourceOid
    $candidateBytes = Read-PilotBlob $publishFull $candidateOid
    $sourceSha = Get-PilotSha256 $sourceBytes
    $candidateSha = Get-PilotSha256 $candidateBytes
    if ($sourceSha -cne $entry.sha256 -or $candidateSha -cne $entry.sha256 -or $sourceBytes.Length -ne $entry.bytes -or $candidateBytes.Length -ne $entry.bytes) {
        throw "Frozen product byte mismatch in committed ROOT or PUBLIC candidate: $path. Preserve the validated bytes when staging; do not replace the freeze to accept unvalidated content."
    }
    $row = [pscustomobject]@{ path=$path; sourceBlob=$sourceOid; candidateBlob=$candidateOid; expectedSHA256=$entry.sha256; sourceSHA256=$sourceSha; candidateSHA256=$candidateSha; bytes=$entry.bytes; passed=$true }
    $frozenRows += $row
    $frozenByPath.Add($path,$row)
}
$layoutPath = 'Docs/RosterMotion20261125/art-layout.json'
if (-not $frozenByPath.ContainsKey($layoutPath)) { throw 'The exact product freeze must include art-layout.json.' }
$layout = Read-PilotJsonBlob $rootFull $frozenByPath[$layoutPath].sourceBlob
$owners = @('cless','farah','jade','kisara','mint','natalia','shionne','tear','velvet')
if ($layout.schemaVersion -ne 1 -or @($layout.characters).Count -ne 9) { throw 'Expected nine characters in the committed art layout.' }
$nativeRows = @()
$provenanceRows = @()
foreach ($id in $owners) {
    $characters = @($layout.characters | Where-Object { $_.characterId -ceq $id })
    if ($characters.Count -ne 1 -or @($characters[0].sheets).Count -ne 2) { throw "Expected exactly two committed native sheets for $id" }
    $provenancePath = 'Docs/RosterMotion20261125/art-'+$id+'.json'
    $sourceOid = Get-PilotTreeEntry $rootFull $source $provenancePath
    $candidateOid = Get-PilotCandidateEntry $provenancePath
    $sourceProvenance = Read-PilotBlob $rootFull $sourceOid
    $candidateProvenance = Read-PilotBlob $publishFull $candidateOid
    $provenanceSha = Get-PilotSha256 $sourceProvenance
    if ($provenanceSha -cne (Get-PilotSha256 $candidateProvenance)) { throw "Candidate provenance differs from committed ROOT: $provenancePath" }
    $provenance = $utf8Strict.GetString($sourceProvenance).TrimStart([char]0xFEFF) | ConvertFrom-Json
    if ($provenance.PSObject.Properties.Name -contains 'sheets') { $provenanceId=$provenance.characterId; $images=@($provenance.sheets); $pathProperty='path' }
    elseif ($provenance.PSObject.Properties.Name -contains 'images') { $provenanceId=$provenance.character; $images=@($provenance.images); $pathProperty='destination' }
    else { throw "Unknown committed art provenance schema: $provenancePath" }
    if ($provenanceId -cne $id -or $images.Count -ne 2) { throw "Provenance owner or sheet count differs: $provenancePath" }
    $provenanceRows += [pscustomobject]@{ path=$provenancePath; sourceBlob=$sourceOid; candidateBlob=$candidateOid; sha256=$provenanceSha; passed=$true }
    foreach ($kind in @('idle','action')) {
        $path = 'Assets/TalesTactics/Art/RosterMotion20261125/'+$id+'/'+$kind+'-v1.png'
        $sheets = @($characters[0].sheets | Where-Object { $_.path -ceq $path })
        $imagesForPath = @($images | Where-Object { $_.$pathProperty -ceq $path -and $_.kind -ceq $kind })
        if ($sheets.Count -ne 1 -or $imagesForPath.Count -ne 1 -or -not $frozenByPath.ContainsKey($path)) { throw "Missing or duplicate native sheet/layout/provenance: $path" }
        $sheet=$sheets[0]; $image=$imagesForPath[0]; $frozen=$frozenByPath[$path]
        if ($sheet.sha256 -cne $frozen.expectedSHA256 -or $image.sha256 -cne $frozen.expectedSHA256) { throw "Native PNG SHA differs from validated freeze/layout/provenance: $path" }
        if ($image.PSObject.Properties.Name -contains 'sourceSha256' -and $image.sourceSha256 -cne $frozen.expectedSHA256) { throw "Native generator source SHA differs: $path" }
        $data = Read-PilotBlob $rootFull $frozen.sourceBlob
        $dimensions = Get-PilotPngDimensions $data
        if ($dimensions.width -ne $sheet.width -or $dimensions.height -ne $sheet.height -or $dimensions.width -ne $image.width -or $dimensions.height -ne $image.height) { throw "Native PNG dimensions differ from layout/provenance: $path" }
        $nativeRows += [pscustomobject]@{ path=$path; characterId=$id; kind=$kind; sha256=$frozen.expectedSHA256; width=$dimensions.width; height=$dimensions.height; provenance=$provenancePath; passed=$true }
    }
}
if ($nativeRows.Count -ne 18 -or $provenanceRows.Count -ne 9) { throw 'Native art audit did not cover all 18 PNGs and nine provenance documents.' }
if ((Get-PilotSha256 ([IO.File]::ReadAllBytes($freezeFull))) -cne $freezeSHA256) { throw 'The validation-product-freeze document changed during inspection.' }
$originAfter = Get-PilotFreshOrigin $publishFull $expected
[ordered]@{
    passed=$true
    mode=$mode
    expectedParent=$expected
    publicHead=$publishHead
    candidateCommit=$target
    sourceCommit=$source
    changedFiles=$rows.Count
    singleParent=($mode -eq 'commit')
    originBefore=$originBefore
    originAfter=$originAfter
    sourceFreezeSHA256=$freezeSHA256
    frozenProductMatched=$true
    frozenProductFileCount=$frozenRows.Count
    frozenProductFiles=$frozenRows
    nativePngVerified=$true
    nativePngCount=$nativeRows.Count
    nativePngs=$nativeRows
    artProvenance=$provenanceRows
    sourceOnlyAncestryBasis='ExpectedParent matched freshly queried approved origin/develop before and after the audit; staged HEAD equals that parent, and a committed candidate must have it as the sole parent.'
    scope='Exact manifest and actual Git blobs, the 83-file validation freeze, and 18 native PNGs against committed layout/provenance. Approved origin/develop freshness is observed only at the recorded query times; re-audit immediately before a normal PUBLIC HEAD:refs/heads/develop push. This tool does not push. Existing remote history is not rewritten or declared free of prior audio. No device testing or whole-tree equality is asserted.'
    preservedDifferences='Paths outside the manifest are neither copied nor changed; source-only audio omissions and existing dirty user files remain excluded.'
    files=$rows
} | ConvertTo-Json -Depth 6
