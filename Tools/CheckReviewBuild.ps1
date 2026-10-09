param([Parameter(Mandatory=$true)][string]$PlayerRoot,[switch]$Development)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$cecil=Get-ChildItem (Join-Path $project 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.mono-cecil@*' | Select-Object -First 1
if(!$cecil){throw 'Import the Unity project before checking player assemblies'}
Add-Type -LiteralPath (Join-Path $cecil.FullName 'Mono.Cecil.dll')
$managed=Join-Path (Resolve-Path -LiteralPath $PlayerRoot).Path 'TalesTactics_Data/Managed'
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'TalesTactics.Runtime.dll'))
try {
    $names=@($assembly.MainModule.Types | ForEach-Object FullName)
    $expected=@('TalesTactics.CampaignPlayerReview','TalesTactics.ManualPlayerReview','TalesTactics.PersistencePlayerReview')
    $present=@($expected|Where-Object {$names -contains $_})
    $correct=if($Development){$present.Count -eq 3}else{$present.Count -eq 0}
    if(!$correct){throw "Review code inclusion mismatch: development=$Development present=$($present -join ', ')"}
    [pscustomobject]@{passed=$true;development=[bool]$Development;reviewTypes=$present;runtimeSHA256=(Get-FileHash -LiteralPath (Join-Path $managed 'TalesTactics.Runtime.dll')).Hash}
} finally {$assembly.Dispose()}
