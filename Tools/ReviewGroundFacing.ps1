$ErrorActionPreference='Stop'
function EvalFacing([string]$code) {
    $r=unity command eval --caller plugin --skill ui-ugui --format json -- $code | ConvertFrom-Json
    if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
    return $r.data.result.result
}
$evidence=Join-Path (Split-Path $PSScriptRoot -Parent) 'Docs/GroundFacing'
New-Item -ItemType Directory -Force $evidence | Out-Null
EvalFacing @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.Exception("Play TestBattle first");
d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"GroundFacing-"+System.Guid.NewGuid().ToString("N")));
d.PersistCampaign=_=>true;d.TrainingMode=false;d.Campaign=new TalesTactics.CampaignSave();
d.Deployment.Clear();d.Deployment.AddRange(d.Catalog.Characters.Select((c,i)=>new{c,i}).Where(x=>new[]{"cless","mint","jade","alphen","velvet","natalia"}.Contains(x.c.Id)).Select(x=>x.i));
return UnityEngine.Time.frameCount;
'@ | Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1920,1080))){
 $w=$pair[0];$h=$pair[1]
 $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Facing review WIDTHxHEIGHT"});
g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");
v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.StopAllCoroutines();if(d.Session!=null)d.Restart();
d.Preferences.TextScale=WIDTH==1024?1.3f:1;d.BeginBattle();if(d.StoryActive)d.FinishStory();d.StopAllCoroutines();
var u=d.Session.Units.First(x=>x.Data.Id=="cless");d.Session.Select(u);d.SetState(new TalesTactics.CommandState(d));
while(d.ThreatMode!=0)d.CycleThreat();d.RefreshViews();d.Board.FocusCurrent();d.WaitCommand();
return UnityEngine.Time.frameCount;
'@
 EvalFacing $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h") | Out-Null
 foreach($rotation in @(0,90,180,270)){
  EvalFacing "var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();if($rotation>0)d.Board.RotateCamera(90);d.PreviewFacing((TalesTactics.Facing)($rotation/90));return UnityEngine.Time.frameCount;" | Out-Null
  $row=EvalFacing @'
UnityEngine.Canvas.ForceUpdateCanvases();var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
var outside=new System.Collections.Generic.List<string>();var mismatches=new System.Collections.Generic.List<string>();
var events=UnityEngine.EventSystems.EventSystem.current;
foreach(var b in d.Hud.transform.Find("Commands").GetComponentsInChildren<UnityEngine.UI.Button>()){
 var rect=(UnityEngine.RectTransform)b.transform;var corners=new UnityEngine.Vector3[4];rect.GetWorldCorners(corners);
 if(corners.Any(c=>c.x<0||c.y<0||c.x>UnityEngine.Screen.width||c.y>UnityEngine.Screen.height))outside.Add(b.name);
 var data=new UnityEngine.EventSystems.PointerEventData(events){position=UnityEngine.RectTransformUtility.WorldToScreenPoint(null,rect.position)};
 var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(data,hits);
 if(hits.Count==0||hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()!=b)mismatches.Add(b.name);
}
return new{width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,frame=UnityEngine.Time.frameCount,scale=d.Preferences.TextScale,outside=outside.ToArray(),mismatches=mismatches.ToArray(),arrows=d.Board.GetComponentsInChildren<TalesTactics.GroundOverlay>().Count(a=>a.enabled&&a.name.StartsWith("Ground facing "))};
'@
  $row|Add-Member -NotePropertyName rotation -NotePropertyValue $rotation;$rows+=$row
  $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'camera-matrix.json') -Encoding utf8
  $name="facing-$w-$h-$rotation.png"
  $r=unity command capture_game_view --caller plugin --skill ui-ugui --format json -- --width $w --height $h --source screen --save_path "Docs/GroundFacing/$name" | ConvertFrom-Json
  if(!$r.success){throw 'Capture failed'}
  Copy-Item -LiteralPath $r.data.result.savedPath -Destination (Join-Path $evidence $name)
  Write-Output "$w x $h rotation=$rotation outside=$($row.outside.Count) mismatches=$($row.mismatches.Count)"
 }
}
if($rows|Where-Object {$_.outside.Count -gt 0 -or $_.mismatches.Count -gt 0 -or $_.arrows -ne 4}){throw 'Facing layout validation failed'}
