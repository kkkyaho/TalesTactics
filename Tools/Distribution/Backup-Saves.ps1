param(
    [string]$SaveDirectory=(Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'),
    [string]$BackupDirectory=(Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'TalesTacticsBackups')
)
$ErrorActionPreference='Stop'
function FileHash([string]$Path) {
    $stream=[IO.File]::OpenRead($Path); $sha=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}
if(Get-Process -Name TalesTactics -ErrorAction SilentlyContinue){throw 'Close every TalesTactics player before backing up saves.'}
$source=[IO.Path]::GetFullPath($SaveDirectory)
$target=[IO.Path]::GetFullPath($BackupDirectory)
if($target.TrimEnd('\','/') -ieq $source.TrimEnd('\','/') -or $target.StartsWith($source.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Backup destination must be outside the save directory.'}
if(!(Test-Path -LiteralPath $source -PathType Container)){throw 'No save directory exists yet.'}
if(((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked save directory is not supported.'}
$pattern='^(campaign(?:-slot[23])?\.json|settings\.json)(\.bak|\.v1\.bak|\.corrupt-[a-fA-F0-9]+)?$'
$files=@(Get-ChildItem -LiteralPath $source -Force -File | Where-Object Name -match $pattern | Sort-Object Name)
if(!$files.Count){throw 'No save/settings files to back up.'}
$destination=Join-Path $target ((Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $destination | Out-Null
$records=@()
foreach($file in $files){
    if(($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Linked save file is not supported.'}
    $before=(FileHash $file.FullName)
    $copy=Join-Path $destination $file.Name
    Copy-Item -LiteralPath $file.FullName -Destination $copy
    if((FileHash $copy) -ne $before -or (FileHash $file.FullName) -ne $before){throw 'Save changed during backup. Close the game and try again; this folder is incomplete.'}
    $records+=@{path=$file.Name;bytes=(Get-Item -LiteralPath $copy).Length;sha256=$before.ToLowerInvariant()}
}
$after=@(Get-ChildItem -LiteralPath $source -Force -File | Where-Object Name -match $pattern | Sort-Object Name)
if(($after.Name -join '|') -ne ($files.Name -join '|')){throw 'Save file list changed during backup.'}
foreach($record in $records){if((FileHash (Join-Path $source $record.path)) -ine $record.sha256){throw 'Save changed during backup.'}}
@{schema=1;kind='save-backup';createdUtc=[DateTime]::UtcNow.ToString('o');files=$records}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $destination 'manifest.json') -Encoding UTF8
Write-Output $destination
