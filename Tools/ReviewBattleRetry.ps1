# Run against the open project in Play Mode; uses isolated, in-memory review progress.
$ErrorActionPreference='Stop'
function EvalPolish([string]$code) {
    $response=unity command eval --caller plugin --skill ui-ugui --format json -- $code | ConvertFrom-Json
    if(!$response.success -or !$response.data.result.success){throw ($response|ConvertTo-Json -Depth 10)}
    return $response.data.result.result
}
$evidence=Join-Path (Split-Path $PSScriptRoot -Parent) 'Docs/BattleRetry'
New-Item -ItemType Directory -Force $evidence | Out-Null
EvalPolish @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.InvalidOperationException("Open TestBattle in Play Mode first");
d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"BattleRetryReview-"+System.Guid.NewGuid().ToString("N")));
d.PersistCampaign=_=>true;d.TrainingMode=false;d.Campaign=new TalesTactics.CampaignSave{Gold=1280};d.SelectedStage=2;
d.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});
foreach(var c in d.Catalog.Characters)d.Campaign.Get(c.Id).Level=19;
d.Deployment.Clear();d.Deployment.AddRange(TalesTactics.TacticalDevelopment.Recommended(d.Catalog));
d.Hud.ShowDeployment();return true;
'@ | Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1280,800),@(1366,768),@(1920,1080),@(2560,1080))){
    $w=$pair[0];$h=$pair[1]
    $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Intermission review WIDTHxHEIGHT"});
g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");
v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);
return UnityEngine.Time.frameCount;
'@
    EvalPolish $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h") | Out-Null
    Start-Sleep -Milliseconds 700
    foreach($scale in @(1,1.3)){ foreach($screen in @("menu","confirm","legacy")){
        EvalPolish ('var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.TrainingMode=false;d.Preferences.TextScale='+$scale.ToString([Globalization.CultureInfo]::InvariantCulture)+'f;if(d.Session!=null)d.Restart();d.Hud.CloseUnitDetails();d.Hud.ShowDeployment();return UnityEngine.Time.frameCount;') | Out-Null
        $show=@'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
System.Action<string> click=name=>d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name).onClick.Invoke();
d.SelectedStage=1;d.Campaign.StoryProgress.Clear();d.Campaign.StoryProgress.Add("chapter1");
foreach(var c in d.Catalog.Characters)d.Campaign.Get(c.Id).Level=2;
d.UseCT=d.UseFixedSpeedOrder=false;d.BeginBattle();if(d.StoryActive)d.FinishStory();
if("SCREEN"=="legacy")d.Session.Opening=null;
d.Hud.ShowSystemMenu("SCREEN"=="confirm"?4:0);
return UnityEngine.Time.frameCount;
'@
        EvalPolish $show.Replace('SCREEN',$screen) | Out-Null
        Start-Sleep -Milliseconds 500
        $result=EvalPolish @'
UnityEngine.Canvas.ForceUpdateCanvases();var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
var outside=new System.Collections.Generic.List<string>();var overflow=new System.Collections.Generic.List<string>();int count=0;
foreach(var b in d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>()){
 if(!b.isActiveAndEnabled)continue;count++;var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)b.transform).GetWorldCorners(corners);
 if(corners.Any(c=>c.x < -1||c.y < -1||c.x>UnityEngine.Screen.width+1||c.y>UnityEngine.Screen.height+1))outside.Add(b.name);
}
foreach(var t in d.Hud.GetComponentsInChildren<TMPro.TMP_Text>()){
 if(!t.isActiveAndEnabled||string.IsNullOrWhiteSpace(t.text))continue;t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.text);
}
return new {width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,frame=UnityEngine.Time.frameCount,textScale=d.Preferences.TextScale,buttons=count,outside=outside.ToArray(),overflow=overflow.ToArray()};
'@
        $result | Add-Member -NotePropertyName screen -NotePropertyValue $screen; $rows+=$result
        $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'aspect-matrix.json') -Encoding utf8
        if($result.outside.Count -gt 0 -or $result.overflow.Count -gt 0){Write-Warning ($result|ConvertTo-Json -Depth 6)}
        if(($w -eq 1920 -and $scale -eq 1) -or ($w -eq 1024 -and $scale -gt 1)){
            # Let TMP upload any meshes dirtied by ForceMeshUpdate / auto sizing before capturing.
            Start-Sleep -Milliseconds 700
            $name="$screen-$w-$h-$scale.png"
            $capture=unity command capture_game_view --caller plugin --skill ui-ugui --format json -- --width $w --height $h --source screen --save_path "Docs/BattleRetry/$name" | ConvertFrom-Json
            if(!$capture.success){throw 'Intermission capture failed'}
            Copy-Item $capture.data.result.savedPath (Join-Path $evidence $name)
        }
        Write-Output "$screen $w x $h text=$scale buttons=$($result.buttons) overflow=$($result.overflow.Count) offscreen=$($result.outside.Count) frame=$($result.frame)"
    }}
}
EvalPolish 'var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.TrainingMode=false;d.Preferences.TextScale=1;return UnityEngine.Time.frameCount;' | Out-Null
if($rows | Where-Object { $_.outside.Count -gt 0 -or $_.overflow.Count -gt 0 }){throw 'Intermission layout validation failed; inspect aspect-matrix.json'}
