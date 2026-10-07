param([string]$Directory=$PSScriptRoot)
$ErrorActionPreference='Stop'
function FileHash([string]$Path) {
    $stream=[IO.File]::OpenRead($Path); $sha=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant() }
    finally { $stream.Dispose(); $sha.Dispose() }
}
$root=(Resolve-Path -LiteralPath $Directory).Path
$manifest=Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($manifest.schema -ne 1 -or $manifest.kind -notin @('release','save-backup') -or $null -eq $manifest.files){throw 'Unsupported manifest'}
$seen=@{}
foreach($item in $manifest.files){
    $name=[string]$item.path
    if(!$name -or $name -match '(^/|\\|:|(^|/)\.\.?(/|$)|[<>"|?*])' -or $name.EndsWith('/') -or $name -ieq 'manifest.json' -or $seen.ContainsKey($name)){throw "Unsafe or duplicate path: $name"}
    $seen[$name]=$true
    $path=Join-Path $root $name
    $cursor=$root
    foreach($part in $name.Split('/')){
        $cursor=Join-Path $cursor $part
        $entry=Get-Item -LiteralPath $cursor -Force
        if(($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "Linked file/directory: $name"}
    }
    $file=Get-Item -LiteralPath $path -Force
    if($file.PSIsContainer -or $file.Length -ne $item.bytes -or (FileHash $path) -ine $item.sha256){throw "Missing or changed file: $name"}
}
foreach($file in Get-ChildItem -LiteralPath $root -Recurse -Force){
    if(($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "Linked entry: $($file.Name)"}
    if($file.PSIsContainer){continue}
    $name=$file.FullName.Substring($root.Length+1).Replace('\','/')
    if($name -ine 'manifest.json' -and !$seen.ContainsKey($name)){throw "Unexpected file: $name"}
}
[pscustomobject]@{passed=$true;kind=$manifest.kind;files=$seen.Count;directory=$root}
