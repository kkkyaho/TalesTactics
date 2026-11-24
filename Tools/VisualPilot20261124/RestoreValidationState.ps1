$ErrorActionPreference='Stop'
$taskRoot='C:/dev/TalesTactics'
$publicRoot="$taskRoot/Builds/SourceOnlyPublish20261118/Project"
$baseline="$taskRoot/Logs/VisualPilot20261124/Baseline-c21b08a8b25048449368f587e4eb51c3"
$evidence="$taskRoot/Logs/VisualPilot20261124/BeforeBuildRestore"
if(Test-Path -LiteralPath $evidence){throw 'Fresh restore evidence required'}
$inventory=Get-Content -LiteralPath "$baseline/protected-current.json" -Raw|ConvertFrom-Json
$font=$inventory|Where-Object {$_.checkout -eq 'validation' -and $_.relative -eq 'Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset'}
$settings=$inventory|Where-Object {$_.checkout -eq 'validation' -and $_.relative -eq 'ProjectSettings/ProjectSettings.asset'}
if(!$font -or !$settings){throw 'Baseline bytes missing'}
foreach($row in @($font,$settings)){if((Get-FileHash -LiteralPath $row.copy).Hash -ne $row.sha256){throw 'Immutable baseline changed'}}
$currentSettings=Get-Content -LiteralPath $settings.path -Raw
$originalSettings=Get-Content -LiteralPath $settings.copy -Raw
if($currentSettings.Replace('runInBackground: 1','runInBackground: 0') -ne $originalSettings){throw 'Unexpected settings differences outside owned background change'}
[IO.Directory]::CreateDirectory($evidence)|Out-Null
Copy-Item -LiteralPath $font.path -Destination "$evidence/KoreanFallback.before.asset"
Copy-Item -LiteralPath $settings.path -Destination "$evidence/ProjectSettings.before.asset"
$cli='C:/Users/USER-PC/AppData/Local/Unity/bin/unity.exe'
$raw=& $cli command eval --project-path $publicRoot --caller plugin --skill unity-pipeline --format json -- --code 'if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling||UnityEditor.BuildPipeline.isBuildingPlayer)throw new System.InvalidOperationException("Idle Editor required");UnityEngine.Application.runInBackground=false;UnityEditor.PlayerSettings.runInBackground=false;var font=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset");if(font!=null)UnityEditor.EditorUtility.ClearDirty(font);UnityEditor.AssetDatabase.SaveAssets();return "Owned background and font dirty flag restored";'
$raw|Set-Content -LiteralPath "$evidence/editor-restore.json"
$r=$raw|ConvertFrom-Json
if(!$r.success -or !$r.data.result.success){throw 'Live restore did not succeed'}
Copy-Item -LiteralPath $font.copy -Destination $font.path
Copy-Item -LiteralPath $settings.copy -Destination $settings.path
$raw=& $cli command eval --project-path $publicRoot --caller plugin --skill unity-pipeline --format json -- --code 'UnityEditor.AssetDatabase.ImportAsset("Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset",UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);return new {background=UnityEngine.Application.runInBackground,playerBackground=UnityEditor.PlayerSettings.runInBackground};'
$raw|Set-Content -LiteralPath "$evidence/font-reimport.json"
$r=$raw|ConvertFrom-Json
if(!$r.success -or !$r.data.result.success){throw 'Restored font import did not succeed'}
$checks=@();foreach($row in @($inventory|Where-Object {$_.checkout -eq 'validation' -and ($_.relative -like 'ProjectSettings/*' -or $_.relative -like '*KoreanFallback*')})){$sha=(Get-FileHash -LiteralPath $row.path).Hash;$checks+=[pscustomobject]@{relative=$row.relative;before=$row.sha256;after=$sha;matches=$sha -eq $row.sha256}}
$passed=@($checks|Where-Object {!$_.matches}).Count -eq 0
[pscustomobject]@{passed=$passed;checkedUtc=[DateTime]::UtcNow.ToString('o');files=$checks}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$evidence/result.json"
if(!$passed){throw 'Unexpected protected settings/font differences remain'}
Write-Output 'Original pre-task settings and font bytes restored; evidence preserved.'