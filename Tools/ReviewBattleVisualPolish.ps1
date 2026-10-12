param([string[]]$Screens=@('formation','equipment','health','movement','facing'))
$ErrorActionPreference='Stop'
function EvalVisual([string]$code) {
    $r=unity command eval --caller plugin --skill ui-ugui --format json -- $code | ConvertFrom-Json
    if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
    return $r.data.result.result
}
$evidence=Join-Path (Split-Path $PSScriptRoot -Parent) 'Docs/BattleVisualPolish'
New-Item -ItemType Directory -Force $evidence | Out-Null
EvalVisual @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.Exception("Play TestBattle first");
d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"VisualPolish-"+System.Guid.NewGuid().ToString("N")));
d.PersistCampaign=_=>true;d.TrainingMode=false;d.Campaign=new TalesTactics.CampaignSave();
d.Deployment.Clear();d.Deployment.AddRange(d.Catalog.Characters.Select((c,i)=>new{c,i}).Where(x=>new[]{"cless","mint","jade","alphen","velvet","natalia"}.Contains(x.c.Id)).Select(x=>x.i));
return UnityEngine.Time.frameCount;
'@ | Out-Null
$rows=@()
if(Test-Path (Join-Path $evidence 'aspect-matrix.json')){$rows=@(Get-Content (Join-Path $evidence 'aspect-matrix.json') -Raw|ConvertFrom-Json|Where-Object {$_.screen -notin $Screens})}
foreach($pair in @(@(1024,768),@(1920,1080),@(2560,1080))){
 $w=$pair[0];$h=$pair[1]
 $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Visual review WIDTHxHEIGHT"});
g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");
v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);
return UnityEngine.Time.frameCount;
'@
 EvalVisual $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h") | Out-Null
 foreach($scale in @(1,1.3)){foreach($screen in $Screens){
  $code=@'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.StopAllCoroutines();if(d.Session!=null)d.Restart();
d.Hud.CloseUnitDetails();d.Hud.CloseSystemMenu();d.Hud.CloseMission();d.Preferences.TextScale=SCALEf;d.Hud.ShowDeployment();
string screen="SCREEN";
if(screen=="equipment")d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="장비 관리").onClick.Invoke();
else if(screen!="formation"){
 d.BeginBattle();if(d.StoryActive)d.FinishStory();d.StopAllCoroutines();
 var u=d.Session.Units.First(x=>x.Data.Id=="alphen");d.Session.Select(u);d.SetState(new TalesTactics.CommandState(d));
 int i=0;foreach(var v in d.Session.Units){v.CurrentHP=UnityEngine.Mathf.CeilToInt(v.Stats.HP*(i++%3==0?.22f:i%3==1?.65f:1));}
 d.RefreshViews();d.Board.FocusCurrent();
 if(screen=="movement"){d.MoveCommand();d.PreviewTile(d.Session.Grid.Reachable(u,out _).Keys.Where(p=>d.Session.Grid.Path(u,p).Count>2).OrderByDescending(p=>p.x).First());}
 if(screen=="facing"){d.WaitCommand();d.PreviewFacing(TalesTactics.Facing.Right);}
}
return UnityEngine.Time.frameCount;
'@
  EvalVisual $code.Replace('SCREEN',$screen).Replace('SCALE',$scale.ToString([Globalization.CultureInfo]::InvariantCulture)) | Out-Null
  Start-Sleep -Milliseconds 700
  $row=EvalVisual @'
UnityEngine.Canvas.ForceUpdateCanvases();var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
var outside=new System.Collections.Generic.List<string>();var overflow=new System.Collections.Generic.List<string>();
foreach(var b in d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>()){
 if(!b.isActiveAndEnabled)continue;var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)b.transform).GetWorldCorners(corners);
 if(corners.Any(c=>c.x < -1||c.y < -1||c.x>UnityEngine.Screen.width+1||c.y>UnityEngine.Screen.height+1))outside.Add(b.name);
}
foreach(var t in d.Hud.GetComponentsInChildren<TMPro.TMP_Text>()){
 if(!t.isActiveAndEnabled||string.IsNullOrWhiteSpace(t.text))continue;t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.text);
}
var health=UnityEngine.Object.FindObjectsByType<TalesTactics.UnitHealthDisplay>();
foreach(var bar in health){var r=(UnityEngine.RectTransform)bar.transform;var corners=new UnityEngine.Vector3[4];r.GetWorldCorners(corners);if(corners.Any(c=>c.x<0||c.y<0||c.x>UnityEngine.Screen.width||c.y>UnityEngine.Screen.height))outside.Add(bar.name);}
return new{width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,frame=UnityEngine.Time.frameCount,scale=d.Preferences.TextScale,outside=outside.ToArray(),overflow=overflow.ToArray(),healthBars=health.Length};
'@
  $row|Add-Member -NotePropertyName screen -NotePropertyValue $screen;$rows+=$row
  $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'aspect-matrix.json') -Encoding utf8
  if(($w -eq 1920 -and $scale -eq 1) -or ($w -eq 1024 -and $scale -gt 1)){
   $name="$screen-$w-$h-$scale.png"
   $r=unity command capture_game_view --caller plugin --skill ui-ugui --format json -- --width $w --height $h --source screen --save_path "Docs/BattleVisualPolish/$name" | ConvertFrom-Json
   if(!$r.success){throw 'Capture failed'}
   Copy-Item -LiteralPath $r.data.result.savedPath -Destination (Join-Path $evidence $name)
  }
  Write-Output "$screen $w x $h scale=$scale overflow=$($row.overflow.Count) outside=$($row.outside.Count)"
 }}
}
if($rows|Where-Object {$_.outside.Count -gt 0 -or $_.overflow.Count -gt 0}){throw 'UI layout validation failed'}
