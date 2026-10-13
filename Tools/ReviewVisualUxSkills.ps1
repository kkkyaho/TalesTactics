$ErrorActionPreference='Stop'
$project='C:/dev/TalesTactics'
$evidence=Join-Path $project 'Docs/VisualUx'
function EvalUx([string]$code){
 $r=unity command eval --project-path $project --caller plugin --skill unity-cli --format json -- $code|ConvertFrom-Json
 if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
 return $r.data.result.result
}
function TickUx {
 $r=unity command wait_for --project-path $project --caller plugin --skill unity-cli --format json -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' --timeout_s 10|ConvertFrom-Json
 if(!$r.success -or !$r.data.result.met){throw 'Player loop not advancing'}
}
EvalUx @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Hud.CloseMission();d.Hud.CloseUnitDetails();d.StopAllCoroutines();if(d.Session!=null)d.Restart();d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"VisualUxSkills-"+System.Guid.NewGuid().ToString("N")));d.PersistCampaign=_=>true;d.TrainingMode=true;d.Campaign=new TalesTactics.CampaignSave();return true;
'@|Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1920,1080),@(2560,1080))){
 $w=$pair[0];$h=$pair[1]
 $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Visual UX review WIDTHxHEIGHT"});g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);return UnityEngine.Time.frameCount;
'@
 EvalUx $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h")|Out-Null
 foreach($scale in @(1,1.3)){
  for($index=0;$index -lt 10;$index++){
   $setup=@'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.StopAllCoroutines();if(d.Session!=null)d.Restart();d.Preferences.TextScale=SCALEf;d.Deployment.Clear();d.Deployment.Add(INDEX);d.BeginBattle();if(d.StoryActive)d.FinishStory();d.StopAllCoroutines();d.Session.Select(d.Session.Units.First(u=>u.Team==TalesTactics.Team.Player));d.SetState(new TalesTactics.ActionSelectionState(d));var all=d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name=="기술 · 사용 가능");if(all!=null)all.onClick.Invoke();return UnityEngine.Time.frameCount;
'@
   EvalUx $setup.Replace('SCALE',"$scale").Replace('INDEX',"$index")|Out-Null;TickUx
   for($page=0;$page -lt 5;$page++){
    $row=EvalUx @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();UnityEngine.Canvas.ForceUpdateCanvases();var overflow=new System.Collections.Generic.List<string>();var outside=new System.Collections.Generic.List<string>();var panel=d.Hud.transform.Find("Commands");foreach(var t in panel.GetComponentsInChildren<TMPro.TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.text);}foreach(var b in panel.GetComponentsInChildren<UnityEngine.UI.Button>()){var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)b.transform).GetWorldCorners(corners);if(corners.Any(c=>c.x<0||c.y<0||c.x>UnityEngine.Screen.width||c.y>UnityEngine.Screen.height))outside.Add(b.name);}var next=panel.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name=="다음 기술 페이지");return new{id=d.Session.Active.Data.Id,width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,textScale=d.Preferences.TextScale,overflow=overflow.ToArray(),outside=outside.ToArray(),hasNext=next!=null&&next.interactable};
'@
    $row|Add-Member -NotePropertyName page -NotePropertyValue $page;$rows+=$row
    if($page -eq 0 -and (($w -eq 1920 -and $scale -eq 1 -and $index -eq 0) -or ($w -eq 1024 -and $scale -eq 1.3 -and $index -eq 7))){
     TickUx
     $name="skills-$w-$index.png"
     $c=unity command capture_game_view --project-path $project --caller plugin --skill unity-cli --format json -- --width $w --height $h --source screen --save_path "Docs/VisualUx/$name"|ConvertFrom-Json
     if(!$c.success){throw 'Capture failed'};Copy-Item -LiteralPath $c.data.result.savedPath -Destination (Join-Path $evidence $name)
    }
    if(!$row.hasNext){break}
    EvalUx 'var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="다음 기술 페이지").onClick.Invoke();return true;'|Out-Null;TickUx
   }
   $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'skill-menu-matrix.json') -Encoding utf8
  }
  Write-Output "Skill menus $w x $h scale=$scale checked for 10 heroes"
 }
}
if($rows|Where-Object {$_.overflow.Count -gt 0 -or $_.outside.Count -gt 0}){throw 'Skill menu review failed'}
