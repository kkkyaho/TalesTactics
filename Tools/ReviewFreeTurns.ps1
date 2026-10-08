# Run after the Unity TestBattle scene enters Play Mode. Saves use an isolated root.
$ErrorActionPreference='Stop'
function EvalFreeTurns([string]$code) {
    $r=unity command eval --caller plugin --skill ui-ugui --format json -- $code | ConvertFrom-Json
    if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 12)}
    return $r.data.result.result
}
$evidence=Join-Path (Split-Path $PSScriptRoot -Parent) 'Docs/FreeTurns'
New-Item -ItemType Directory -Force $evidence | Out-Null
EvalFreeTurns @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.InvalidOperationException("Open TestBattle in Play Mode first");
d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"FreeTurnReview-"+System.Guid.NewGuid().ToString("N")));
d.Audio.PersonalMusicEnabled=false;d.PersistCampaign=_=>true;d.TrainingMode=true;d.UseCT=d.UseFixedSpeedOrder=false;
d.Deployment.Clear();d.Deployment.AddRange(TalesTactics.TacticalDevelopment.Recommended(d.Catalog));d.BeginBattle();
d.SelectPlayerUnit(d.Session.Units.First(u=>u.Team==TalesTactics.Team.Player&&u.Data.Id=="mint"));d.WaitCommand();d.ChooseFacing(TalesTactics.Facing.Front);
d.SelectPlayerUnit(d.Session.Units.First(u=>u.Team==TalesTactics.Team.Player&&u.Data.Id=="cless"));d.Board.ResetCamera();
return new {frame=UnityEngine.Time.frameCount,active=d.Session.Active.Data.Id};
'@ | Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1280,800),@(1366,768),@(1920,1080),@(2560,1080))){
    $w=$pair[0];$h=$pair[1]
    $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Free turns WIDTHxHEIGHT"});
g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");
v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);
return UnityEngine.Time.frameCount;
'@
    EvalFreeTurns $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h") | Out-Null
    foreach($scale in @(1,1.3)){
        EvalFreeTurns ('var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Preferences.TextScale='+$scale.ToString([Globalization.CultureInfo]::InvariantCulture)+'f;d.Hud.Refresh();return UnityEngine.Time.frameCount;') | Out-Null
        Start-Sleep -Milliseconds 600
        $result=EvalFreeTurns @'
UnityEngine.Canvas.ForceUpdateCanvases();var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
var outside=new System.Collections.Generic.List<string>();var overflow=new System.Collections.Generic.List<string>();var overlap=new System.Collections.Generic.List<string>();
var buttons=d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.isActiveAndEnabled).ToArray();
System.Func<UnityEngine.RectTransform,UnityEngine.Rect> rect=r=>{var corners=new UnityEngine.Vector3[4];r.GetWorldCorners(corners);return UnityEngine.Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);};
foreach(var b in buttons){var r=rect((UnityEngine.RectTransform)b.transform);if(r.xMin< -1||r.yMin< -1||r.xMax>UnityEngine.Screen.width+1||r.yMax>UnityEngine.Screen.height+1)outside.Add(b.name);}
foreach(var t in d.Hud.GetComponentsInChildren<TMPro.TMP_Text>()){if(!t.isActiveAndEnabled||string.IsNullOrWhiteSpace(t.text))continue;t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.text);}
var header=buttons.Where(b=>b.transform.parent.name=="Header").ToArray();
for(int i=0;i<header.Length;i++)for(int j=i+1;j<header.Length;j++)if(rect((UnityEngine.RectTransform)header[i].transform).Overlaps(rect((UnityEngine.RectTransform)header[j].transform)))overlap.Add(header[i].name+" / "+header[j].name);
var allies=buttons.Where(b=>b.name.StartsWith("아군 선택: ")).ToArray();
return new {width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,frame=UnityEngine.Time.frameCount,textScale=d.Preferences.TextScale,buttons=buttons.Length,allies=allies.Length,selectable=allies.Count(b=>b.interactable),outside=outside.ToArray(),overflow=overflow.ToArray(),overlap=overlap.ToArray()};
'@
        $rows+=$result;$rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'aspect-matrix.json') -Encoding utf8
        if($result.outside.Count -or $result.overflow.Count -or $result.overlap.Count -or $result.allies -ne 6 -or $result.selectable -ne 5){throw ($result|ConvertTo-Json -Depth 8)}
        if(($w -in @(1024,1920,2560) -and $scale -eq 1) -or ($w -eq 1024 -and $scale -gt 1)){
            Start-Sleep -Milliseconds 600
            $name="allied-phase-$w-$h-$scale.png"
            $capture=unity command capture_game_view --caller plugin --skill ui-ugui --format json -- --width $w --height $h --source screen --save_path "Docs/FreeTurns/$name"|ConvertFrom-Json
            if(!$capture.success){throw 'Game View capture failed'}
            Copy-Item $capture.data.result.savedPath (Join-Path $evidence $name)
        }
        Write-Output "$w x $h text=$scale allies=$($result.allies) selectable=$($result.selectable) overflow=$($result.overflow.Count) overlap=$($result.overlap.Count) frame=$($result.frame)"
    }
}
