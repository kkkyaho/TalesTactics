# Audit the approved source-only publication without changing Git refs, index, or worktree.
# Run before pushing. This checks the local remote-tracking snapshot, not the live server.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RepositoryPath,
    [Parameter(Mandatory = $true)][string]$CandidateCommit,
    [Parameter(Mandatory = $true)][string]$ExpectedParent,
    [Parameter(Mandatory = $true)][string]$PrivateSource,
    [string]$OutputPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$approvedParent = '63f63ed23d0997a4b4bf51f14f3c0e05d4371bc6'
$approvedPrivateSource = '1cfabc77528b2ef37387fced7abc038da898e572'
$remoteTrackingRef = 'refs/remotes/origin/develop'
$musicRoot = 'Assets/TalesTactics/Audio/LocalMusic/'
$forbiddenMusic = [ordered]@{
    '9953EC3359F9562305.mp3' = 'd6795f8e39165e99d6f42393752a6f064400c1e2'
    '99852C3359F955BE1A.mp3' = '40784a547713c4a8681ab16233f6f386df0037d2'
    '99898D3359F955C018.mp3' = '987b794e077d371e2cbea60c9c2f880c0fab5944'
    '9993893359F955C218.mp3' = '1113e5a945637ea7d1160af1371ae4edca4dde5b'
    '99AA233359F955C515.mp3' = 'f0b10079ed09ff218bb22b58a645451cf8705a53'
    'Tales of Destiny 2 - Wilderness.mp3' = '0e332fff2c1634e9bff13f2d85f20c7fa5b9245e'
}
$forbiddenPaths = @('Assets/TalesTactics/Audio/LocalMusic.meta')
foreach ($name in $forbiddenMusic.Keys) {
    $forbiddenPaths += $musicRoot + $name
    $forbiddenPaths += $musicRoot + $name + '.meta'
}
$allowedFiles = @(
    '.gitignore',
    'Assets/TalesTactics/Content/AudioLibrary.asset',
    'Assets/TalesTactics/Tests/LocalMusicTests.cs',
    'Assets/TalesTactics/PlayModeTests/LocalMusicSceneTests.cs',
    'Docs/LocalMusicPolicy.md',
    'README.md',
    'AGENTS.md'
)
$checks = [System.Collections.Generic.List[object]]::new()
$details = [ordered]@{}
$report = [ordered]@{
    schemaVersion = 1
    purpose = 'Pre-push audit of the approved source-only develop publication'
    passed = $false
    candidateCommit = $CandidateCommit
    expectedParent = $ExpectedParent
    privateSource = $PrivateSource
    remoteContext = [ordered]@{
        targetRef = 'refs/heads/develop'
        localTrackingRef = $remoteTrackingRef
        approvedParent = $approvedParent
        liveServerChecked = $false
        requirement = 'Re-read the live origin develop ref before a non-force, single-ref push. It must still equal approvedParent.'
        historicalAudio = 'The existing remote parent history contains the six MP3 blobs. This audit does not claim their removal from existing history.'
    }
    checks = $checks
    details = $details
}

function Add-AuditCheck([string]$Name, [bool]$Passed, [object]$Detail) {
    $checks.Add([pscustomobject]@{ name = $Name; passed = $Passed; detail = $Detail })
}
function Invoke-ReadOnlyGit([string[]]$Arguments, [int[]]$AllowedExitCodes = @(0)) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $lines = @(& git --no-replace-objects -C $RepositoryPath -c core.quotepath=false --no-pager @Arguments 2>&1)
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    $resultText = ($lines | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
    if ($AllowedExitCodes -notcontains $code) {
        throw "Read-only git command failed (exit $code): git $($Arguments -join ' '). $resultText"
    }
    return [pscustomobject]@{ exitCode = $code; text = $resultText }
}
function Get-TreeRecords([string]$Commit) {
    $raw = (Invoke-ReadOnlyGit -Arguments @('ls-tree', '-r', '-z', $Commit)).text
    foreach ($entry in $raw.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
        if ($entry -notmatch '^([0-7]{6}) (blob|commit) ([0-9a-f]{40})\t(.+)$') {
            throw "Unexpected tree record at commit $Commit."
        }
        [pscustomobject]@{ mode = $Matches[1]; type = $Matches[2]; oid = $Matches[3]; path = $Matches[4] }
    }
}
function Test-ExactMember([string[]]$Values, [string]$Value) {
    foreach ($item in $Values) {
        if ([string]::Equals($item, $Value, [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}

try {
    foreach ($value in @($CandidateCommit, $ExpectedParent, $PrivateSource)) {
        if ($value -notmatch '^[0-9a-fA-F]{40}$') { throw 'Commit parameters must be full 40-character SHA-1 commit IDs.' }
    }
    $CandidateCommit = $CandidateCommit.ToLowerInvariant()
    $ExpectedParent = $ExpectedParent.ToLowerInvariant()
    $PrivateSource = $PrivateSource.ToLowerInvariant()
    $report.candidateCommit = $CandidateCommit
    $report.expectedParent = $ExpectedParent
    $report.privateSource = $PrivateSource
    if ($ExpectedParent -cne $approvedParent -or $PrivateSource -cne $approvedPrivateSource) {
        throw 'Inputs differ from the specifically approved remote parent or private source. This audit is scoped to the 2026-11-18 publication.'
    }
    $RepositoryPath = (Resolve-Path -LiteralPath $RepositoryPath).Path
    $report.repositoryPath = $RepositoryPath
    $details.repositoryHead = (Invoke-ReadOnlyGit -Arguments @('rev-parse', 'HEAD')).text.Trim()
    foreach ($value in @($CandidateCommit, $ExpectedParent, $PrivateSource)) {
        $resolved = (Invoke-ReadOnlyGit -Arguments @('rev-parse', '--verify', ($value + '^{commit}'))).text.Trim()
        if ($resolved -cne $value) { throw "Commit does not resolve exactly: $value" }
    }
    $tracking = (Invoke-ReadOnlyGit -Arguments @('rev-parse', '--verify', $remoteTrackingRef)).text.Trim()
    $report.remoteContext.localTrackingCommit = $tracking
    Add-AuditCheck 'approved-local-remote-tracking-snapshot' ($tracking -ceq $ExpectedParent) $tracking

    $parentParts = ((Invoke-ReadOnlyGit -Arguments @('rev-list', '--parents', '-n', '1', $CandidateCommit)).text.Trim() -split '\s+')
    $singleParent = $parentParts.Count -eq 2 -and $parentParts[0] -ceq $CandidateCommit -and $parentParts[1] -ceq $ExpectedParent
    Add-AuditCheck 'one-parent-exactly-approved-remote-develop' $singleParent @($parentParts | Select-Object -Skip 1)
    $outgoing = @(((Invoke-ReadOnlyGit -Arguments @('rev-list', ($ExpectedParent + '..' + $CandidateCommit))).text -split '\r?\n') | Where-Object { $_ })
    Add-AuditCheck 'exactly-one-outgoing-commit' ($outgoing.Count -eq 1 -and $outgoing[0] -ceq $CandidateCommit) $outgoing
    $sourceAncestry = Invoke-ReadOnlyGit -Arguments @('merge-base', '--is-ancestor', $ExpectedParent, $PrivateSource) -AllowedExitCodes @(0, 1)
    Add-AuditCheck 'private-source-descends-from-approved-parent' ($sourceAncestry.exitCode -eq 0) $sourceAncestry.exitCode
    $privateCommits = @(((Invoke-ReadOnlyGit -Arguments @('rev-list', ($ExpectedParent + '..' + $PrivateSource))).text -split '\r?\n') | Where-Object { $_ })
    Add-AuditCheck 'six-known-private-commits' ($privateCommits.Count -eq 6) $privateCommits
    $privateAncestry = @(
        foreach ($privateCommit in $privateCommits) {
            $answer = Invoke-ReadOnlyGit -Arguments @('merge-base', '--is-ancestor', $privateCommit, $CandidateCommit) -AllowedExitCodes @(0, 1)
            [pscustomobject]@{ commit = $privateCommit; isAncestor = $answer.exitCode -eq 0 }
        }
    )
    Add-AuditCheck 'private-commits-excluded-from-candidate-ancestry' (@($privateAncestry | Where-Object { $_.isAncestor }).Count -eq 0) $privateAncestry

    $privateTree = @(Get-TreeRecords $PrivateSource)
    $candidateTree = @(Get-TreeRecords $CandidateCommit)
    $forbiddenBlobIds = @($forbiddenMusic.Values)
    $sourceMusicChecks = @(
        foreach ($name in $forbiddenMusic.Keys) {
            $rows = @($privateTree | Where-Object { $_.path -ceq ($musicRoot + $name) })
            [pscustomobject]@{
                path = $musicRoot + $name
                expectedBlob = $forbiddenMusic[$name]
                matches = $rows.Count -eq 1 -and $rows[0].type -ceq 'blob' -and $rows[0].oid -ceq $forbiddenMusic[$name]
            }
        }
    )
    Add-AuditCheck 'private-source-music-identities-pinned' (@($sourceMusicChecks | Where-Object { -not $_.matches }).Count -eq 0) $sourceMusicChecks
    $sourceProtectedPaths = @($privateTree | Where-Object { Test-ExactMember $forbiddenPaths $_.path })
    Add-AuditCheck 'all-thirteen-protected-paths-in-private-source' ($sourceProtectedPaths.Count -eq 13) $sourceProtectedPaths.Count
    $musicPathsInCandidate = @($candidateTree | Where-Object {
        $_.path.StartsWith($musicRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($_.path, 'Assets/TalesTactics/Audio/LocalMusic.meta', [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($_.path, 'Assets/TalesTactics/Audio/LocalMusic', [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object { $_.path })
    Add-AuditCheck 'user-music-paths-absent-from-candidate-tree' ($musicPathsInCandidate.Count -eq 0) $musicPathsInCandidate
    $musicBlobsInCandidate = @($candidateTree | Where-Object { $_.type -ceq 'blob' -and $forbiddenBlobIds -contains $_.oid })
    Add-AuditCheck 'six-music-blobs-absent-everywhere-in-candidate-tree' ($musicBlobsInCandidate.Count -eq 0) $musicBlobsInCandidate
    $newObjects = @(((Invoke-ReadOnlyGit -Arguments @('rev-list', '--objects', '--no-object-names', $CandidateCommit, ('^' + $ExpectedParent))).text -split '\r?\n') | Where-Object { $_ })
    $newForbiddenObjects = @($newObjects | Where-Object { $forbiddenBlobIds -contains $_ })
    Add-AuditCheck 'new-reachable-objects-contain-no-user-music-blobs' ($newForbiddenObjects.Count -eq 0) $newForbiddenObjects
    $details.newReachableObjectCount = $newObjects.Count
    $details.candidateTreeEntryCount = $candidateTree.Count

    $diffRaw = (Invoke-ReadOnlyGit -Arguments @('diff', '--name-status', '--no-renames', '-z', $PrivateSource, $CandidateCommit, '--')).text
    $diffFields = @($diffRaw.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries))
    if (($diffFields.Count % 2) -ne 0) { throw 'Unexpected name-status diff structure.' }
    $diffRows = @(
        for ($index = 0; $index -lt $diffFields.Count; $index += 2) {
            $status = $diffFields[$index]
            $path = $diffFields[$index + 1]
            $isMusic = Test-ExactMember $forbiddenPaths $path
            $isAllowedFile = Test-ExactMember $allowedFiles $path
            $isPublicationDoc = $path.StartsWith('Docs/SourceOnlyPublish/', [StringComparison]::Ordinal)
            $allowed = ($isMusic -and $status -ceq 'D') -or
                (($isAllowedFile -or $isPublicationDoc) -and ($status -ceq 'M' -or $status -ceq 'A'))
            [pscustomobject]@{ status = $status; path = $path; allowed = $allowed }
        }
    )
    $unexpected = @($diffRows | Where-Object { -not $_.allowed })
    Add-AuditCheck 'private-source-diff-confined-to-approved-paths-and-actions' ($unexpected.Count -eq 0) $unexpected
    $deletedMusicPaths = @($diffRows | Where-Object { $_.status -ceq 'D' -and (Test-ExactMember $forbiddenPaths $_.path) })
    Add-AuditCheck 'exactly-thirteen-approved-music-deletions' ($deletedMusicPaths.Count -eq 13) $deletedMusicPaths.Count
    $details.privateSourceDiff = $diffRows
    $details.scope = 'Git object and path audit only; does not validate Unity compilation, fallback playback, or remote server state.'
    $report.passed = @($checks | Where-Object { -not $_.passed }).Count -eq 0
}
catch {
    $report.error = $_.Exception.Message
    $report.passed = $false
}
$report.checkedAtUtc = [DateTime]::UtcNow.ToString('o')
$json = $report | ConvertTo-Json -Depth 12 -Compress
if ($OutputPath) {
    try {
        $fullOutput = [IO.Path]::GetFullPath($OutputPath)
        $stream = [IO.File]::Open($fullOutput, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try {
            $bytes = [Text.UTF8Encoding]::new($false).GetBytes($json + [Environment]::NewLine)
            $stream.Write($bytes, 0, $bytes.Length)
        }
        finally { $stream.Dispose() }
    }
    catch {
        $report.passed = $false
        $report.outputError = $_.Exception.Message
        $json = $report | ConvertTo-Json -Depth 12 -Compress
    }
}
Write-Output $json
if (-not $report.passed) { exit 1 }
exit 0
