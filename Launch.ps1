$projectPath=$PSScriptRoot
$editor='C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe'
if(!(Test-Path -LiteralPath $editor)){throw 'Unity 6000.6.0f1 is not installed at the expected location.'}
# Explicitly launched by the user to open the interactive Editor.
Start-Process -FilePath $editor -ArgumentList @('-projectPath',('"'+$projectPath+'"'))
