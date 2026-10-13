param([switch]$Gallery)
$ErrorActionPreference='Stop'
$project='C:/dev/TalesTactics'
$evidence=Join-Path $project 'Docs/MotionStatus'
function EvalMotion([string]$code){
 $r=unity command eval --project-path $project --caller plugin --skill unity-cli --format json -- $code|ConvertFrom-Json
 if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
 return $r.data.result.result
}
function ResizeMotion([int]$w,[int]$h){
 $code=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Motion review WIDTHxHEIGHT"});g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);return UnityEngine.Time.frameCount;
'@
 EvalMotion $code.Replace('WIDTH',"$w").Replace('HEIGHT',"$h")|Out-Null
}
function CaptureMotion([string]$name,[int]$w,[int]$h){
 $r=unity command capture_game_view --project-path $project --caller plugin --skill unity-cli --format json -- --width $w --height $h --source screen --save_path "Docs/MotionStatus/$name.png"|ConvertFrom-Json
 if(!$r.success){throw 'Capture failed'}
 Copy-Item -LiteralPath $r.data.result.savedPath -Destination (Join-Path $evidence "$name.png")
}
function TickMotion {
 $r=unity command wait_for --project-path $project --caller plugin --skill unity-cli --format json -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' --timeout_s 10|ConvertFrom-Json
 if(!$r.success -or !$r.data.result.met){throw 'Player loop not advancing'}
}
if($Gallery){
 ResizeMotion 1920 1080
 $code=Get-Content (Join-Path $PSScriptRoot 'ReviewMotionGallery.cs.txt') -Raw
 foreach($facing in @('Front','Back','Right','Left')){
  EvalMotion $code.Replace('REVIEW_FACING',$facing)|Out-Null
  TickMotion
  CaptureMotion "heroes-$($facing.ToLower())" 1920 1080
 }
 exit
}
EvalMotion @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.Exception("Play TestBattle first");
UnityEngine.Application.runInBackground=true;d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"MotionReview-"+System.Guid.NewGuid().ToString("N")));d.PersistCampaign=_=>true;d.TrainingMode=true;d.Campaign=new TalesTactics.CampaignSave();d.Deployment.Clear();d.Deployment.AddRange(new[]{0,1,2,3,4,5});d.BeginBattle();if(d.StoryActive)d.FinishStory();d.StopAllCoroutines();d.Session.Select(d.Session.Units.First(x=>x.Team==TalesTactics.Team.Player));d.SetState(new TalesTactics.CommandState(d));d.RefreshViews();return UnityEngine.Time.frameCount;
'@|Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1920,1080),@(2560,1080))){
 $w=$pair[0];$h=$pair[1];ResizeMotion $w $h
 foreach($scale in @(1,1.3)){
  EvalMotion "var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Preferences.TextScale=$($scale)f;d.RefreshViews();return true;"|Out-Null
  foreach($tab in @('능력','기술','장비')){
   $code=@'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();var rows=new System.Collections.Generic.List<object>();
foreach(var data in d.Catalog.Characters.Concat(d.Catalog.Enemies)){
 var u=new TalesTactics.UnitRuntime(data,TalesTactics.CharacterVisualSizing.IsHero(data)?TalesTactics.Team.Player:TalesTactics.Team.Enemy,d.Catalog.Rules,25);
 foreach(TalesTactics.StatusKind kind in System.Enum.GetValues(typeof(TalesTactics.StatusKind)))if(kind!=TalesTactics.StatusKind.None)u.AddStatus(kind,2);
 if(data.Id=="mint")u.CurrentHP=0; if(u.Team==TalesTactics.Team.Enemy){u.IntentPhase=1;u.IntentSkill=data.BasicAttack.Id;u.IntentAim=new UnityEngine.Vector2Int(3,4);}
 d.Hud.ShowUnitDetails(u);d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="상태 TAB").onClick.Invoke();
 var page=d.Hud.transform.Find("UnitDetails");UnityEngine.Canvas.ForceUpdateCanvases();var overflow=new System.Collections.Generic.List<string>();var outside=new System.Collections.Generic.List<string>();
 foreach(var t in page.GetComponentsInChildren<TMPro.TMP_Text>()){t.fontSizeMax*=d.Preferences.TextScale;t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.text);}
 foreach(var b in page.GetComponentsInChildren<UnityEngine.UI.Button>()){var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)b.transform).GetWorldCorners(corners);if(corners.Any(c=>c.x<0||c.y<0||c.x>UnityEngine.Screen.width||c.y>UnityEngine.Screen.height))outside.Add(b.name);}
 rows.Add(new{id=data.Id,width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,textScale=d.Preferences.TextScale,tab="TAB",overflow=overflow.ToArray(),outside=outside.ToArray()});
}
d.Hud.ShowUnitDetails(d.Session.Units.First(u=>u.Team==TalesTactics.Team.Player));d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="상태 TAB").onClick.Invoke();return rows;
'@
   $rows+=@(EvalMotion $code.Replace('TAB',$tab));TickMotion
   if(($w -eq 1920 -and $scale -eq 1) -or ($w -eq 1024 -and $scale -eq 1.3)){CaptureMotion "status-$tab-$w-$scale" $w $h}
   $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'status-matrix.json') -Encoding utf8
   Write-Output "Status $($w)x$h scale=$scale tab=$tab"
  }
 }
}
if($rows|Where-Object {$_.outside.Count -gt 0 -or $_.overflow.Count -gt 0}){throw 'Status review failed'}
EvalMotion 'var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Hud.CloseUnitDetails();d.Preferences.TextScale=1;d.RefreshViews();return true;'|Out-Null
ResizeMotion 1920 1080
TickMotion
CaptureMotion 'battle-hud' 1920 1080
