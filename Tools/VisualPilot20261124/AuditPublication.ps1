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
    [string]$CommitId
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
    if ($length -gt 67108864) { throw "Blob exceeds this pilot's 64 MiB per-file limit: $Oid" }
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
    '.gitattributes','README.md','Docs/TODO.md','Docs/ARCHITECTURE.md','Docs/VALIDATION.md',
    'Assets/TalesTactics/Runtime/Presentation/BoardView.cs',
    'Assets/TalesTactics/Runtime/Presentation/CharacterMotion.cs',
    'Assets/TalesTactics/Runtime/Presentation/PixelBattlefield.cs',
    'Assets/TalesTactics/Runtime/Presentation/PixelLivingTerrain.cs',
    'Assets/TalesTactics/Runtime/Presentation/AlphenVisualPilotMotion.cs',
    'Assets/TalesTactics/Runtime/Presentation/AlphenVisualPilotMotion.cs.meta',
    'Assets/TalesTactics/Editor/VisualPilotArtImporter.cs',
    'Assets/TalesTactics/Editor/VisualPilotArtImporter.cs.meta',
    'Assets/TalesTactics/Editor/VisualPilotCapture.cs',
    'Assets/TalesTactics/Editor/VisualPilotCapture.cs.meta',
    'Assets/TalesTactics/Tests/AlphenVisualPilotMotionTests.cs',
    'Assets/TalesTactics/Tests/AlphenVisualPilotMotionTests.cs.meta',
    'Assets/TalesTactics/Tests/VisualPilotAssetIntegrationTests.cs',
    'Assets/TalesTactics/Tests/VisualPilotAssetIntegrationTests.cs.meta',
    'Assets/TalesTactics/PlayModeTests/VisualPilotEnvironmentTests.cs',
    'Assets/TalesTactics/PlayModeTests/VisualPilotEnvironmentTests.cs.meta',
    'Assets/TalesTactics/Resources/TalesTactics/UnitHeadAnchors.json',
    'Assets/TalesTactics/Resources/TalesTactics/VisualPilot.meta',
    'Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset',
    'Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset.meta',
    'Assets/TalesTactics/Art/VisualPilot.meta',
    'Assets/TalesTactics/Art/VisualPilot/Alphen.meta',
    'Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png',
    'Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png.meta',
    'Assets/TalesTactics/Art/VisualPilot/Alphen/action-v1.png',
    'Assets/TalesTactics/Art/VisualPilot/Alphen/action-v1.png.meta'
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
    $allowedDoc = $line -cmatch '^Docs/VisualPilot20261124/[A-Za-z0-9_./-]+\.(md|json|png|html|txt|xml|csv)$'
    $allowedTool = $line -cmatch '^Tools/VisualPilot20261124/[A-Za-z0-9_./-]+\.(ps1|py|cs|json|txt|md|html|cjs)$'
    if ($exactAllowed -cnotcontains $line -and -not $allowedDoc -and -not $allowedTool) {
        throw "Path is outside this pilot; pre-existing user changes/private assets are not allowed: $line"
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
    if ($mode -eq 'staged') {
        $entry = Invoke-PilotGit $publishFull @('ls-files','--stage','--',$path)
        if ($entry -notmatch '^100644 ([0-9a-f]{40}(?:[0-9a-f]{24})?) 0\t(.+)$' -or $Matches[2] -cne $path) {
            throw "Expected one regular, conflict-free index entry: $path"
        }
        $candidateOid = $Matches[1]
    }
    else { $candidateOid = Get-PilotTreeEntry $publishFull $target $path }
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
[ordered]@{
    passed=$true
    mode=$mode
    expectedParent=$expected
    publicHead=$publishHead
    candidateCommit=$target
    sourceCommit=$source
    changedFiles=$rows.Count
    singleParent=($mode -eq 'commit')
    sourceOnlyAncestryBasis='Staged HEAD equals expected public parent; committed candidate must have that parent alone.'
    scope='Exact manifest and actual Git blobs only. Existing remote history is not rewritten or declared free of prior audio. No network freshness, device testing, or whole-tree equality is asserted.'
    preservedDifferences='Paths outside the manifest are neither copied nor changed; source-only audio omissions and existing dirty user files remain excluded.'
    files=$rows
} | ConvertTo-Json -Depth 6
