param([int[]]$Stages=@(0,1,2,3,4,5),[string]$MatrixName='camera-matrix.json')
$ErrorActionPreference='Stop'
function EvalTerrain([string]$code){
 $r=unity command eval --project-path C:/dev/TalesTactics --caller plugin --skill unity-cli --format json -- $code|ConvertFrom-Json
 if(!$r.success -or !$r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
 return $r.data.result.result
}
$evidence=Join-Path (Split-Path $PSScriptRoot -Parent) 'Docs/VisualUx'
New-Item -ItemType Directory -Force $evidence|Out-Null
EvalTerrain @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.Exception("Play TestBattle first");
UnityEngine.Application.runInBackground=true;d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"TerrainReview-"+System.Guid.NewGuid().ToString("N")));d.PersistCampaign=_=>true;d.TrainingMode=false;d.Campaign=new TalesTactics.CampaignSave();
for(int stage=0;stage<6;stage++)d.Campaign.StoryProgress.Add(TalesTactics.CampaignStages.Id(stage));
d.Deployment.Clear();d.Deployment.AddRange(d.Catalog.Characters.Select((c,i)=>new{c,i}).Where(x=>new[]{"cless","mint","jade","alphen","velvet","natalia"}.Contains(x.c.Id)).Select(x=>x.i));
return UnityEngine.Time.frameCount;
'@|Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1920,1080),@(2560,1080))){
 $w=$pair[0];$h=$pair[1]
 $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Terrain review WIDTHxHEIGHT"});g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);
return UnityEngine.Time.frameCount;
'@
 EvalTerrain $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h")|Out-Null
 foreach($stage in $Stages){
  $setup=@'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.StopAllCoroutines();if(d.Session!=null)d.Restart();d.SelectedStage=STAGE;d.Preferences.TextScale=WIDTH==1024?1.3f:1;
d.BeginBattle();if(d.StoryActive)d.FinishStory();d.StopAllCoroutines();d.Session.Select(d.Session.Units.First(x=>x.Team==TalesTactics.Team.Player));d.SetState(new TalesTactics.CommandState(d));while(d.ThreatMode!=0)d.CycleThreat();d.RefreshViews();return UnityEngine.Time.frameCount;
'@
  EvalTerrain $setup.Replace('STAGE',"$stage").Replace('WIDTH',"$w")|Out-Null
  foreach($rotation in @(0,90,180,270)){
   if($rotation -gt 0){EvalTerrain 'var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Board.RotateCamera(90);return UnityEngine.Time.frameCount;'|Out-Null}
   $tick=unity command wait_for --project-path C:/dev/TalesTactics --caller plugin --skill unity-cli --format json -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' --timeout_s 10|ConvertFrom-Json
   if(!$tick.success -or !$tick.data.result.met){throw 'Player loop not advancing'}
   $row=EvalTerrain @'
UnityEngine.Canvas.ForceUpdateCanvases();var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();var bad=new System.Collections.Generic.List<string>();var outside=new System.Collections.Generic.List<string>();var overflow=new System.Collections.Generic.List<string>();
foreach(var tile in d.Session.Grid.Tiles.Values.Where(t=>t.Walkable)){var screen=d.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(d.Catalog.Rules.TileHeight));if(!d.Board.Pick(screen,out var picked)||picked!=tile.Coordinate)bad.Add(tile.Coordinate.ToString());}
foreach(var t in d.Hud.GetComponentsInChildren<TMPro.TMP_Text>()){if(!t.isActiveAndEnabled||string.IsNullOrWhiteSpace(t.text))continue;t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.text);}
foreach(var b in d.Hud.GetComponentsInChildren<UnityEngine.UI.Button>()){if(!b.IsActive()||!b.IsInteractable())continue;var r=(UnityEngine.RectTransform)b.transform;var corners=new UnityEngine.Vector3[4];r.GetWorldCorners(corners);if(corners.Any(c=>c.x<0||c.y<0||c.x>UnityEngine.Screen.width||c.y>UnityEngine.Screen.height))outside.Add(b.name);}
foreach(var bar in UnityEngine.Object.FindObjectsByType<TalesTactics.UnitHealthDisplay>()){var rect=(UnityEngine.RectTransform)bar.transform;var corners=new UnityEngine.Vector3[4];rect.GetWorldCorners(corners);if(corners.Any(c=>c.x<0||c.y<0||c.x>UnityEngine.Screen.width||c.y>UnityEngine.Screen.height))outside.Add(bar.name);}
var bars=UnityEngine.Object.FindObjectsByType<TalesTactics.UnitHealthDisplay>().Where(b=>b.Bar.enabled).Select(b=>{var r=(UnityEngine.RectTransform)b.transform;var p=r.anchoredPosition;return new UnityEngine.Rect(p.x-r.rect.width*.5f,p.y,r.rect.width,r.rect.height);}).ToArray();int hpOverlaps=0;for(int i=0;i<bars.Length;i++)for(int j=i+1;j<bars.Length;j++)if(bars[i].Overlaps(bars[j]))hpOverlaps++;
var terrain=d.Board.GetComponentInChildren<TalesTactics.PixelBattlefield>();return new{hpOverlaps,stage=d.SelectedStage+1,width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,frame=UnityEngine.Time.frameCount,badPicks=bad.ToArray(),outside=outside.ToArray(),overflow=overflow.ToArray(),decorationMeshes=terrain.DecorationMeshCount,renderers=terrain.GetComponentsInChildren<UnityEngine.Renderer>().Length};
'@
   $row|Add-Member -NotePropertyName rotation -NotePropertyValue $rotation;$rows+=$row
   $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence $MatrixName) -Encoding utf8
   if(($w -eq 1920 -and $rotation -eq 0) -or ($w -eq 1024 -and $rotation -eq 90 -and $stage -in @(0,3,4))){
    $name="chapter$($stage+1)-$w-$rotation.png"
    $r=unity command capture_game_view --project-path C:/dev/TalesTactics --caller plugin --skill unity-cli --format json -- --width $w --height $h --source screen --save_path "Docs/VisualUx/$name"|ConvertFrom-Json
    if(!$r.success){throw 'Capture failed'}
    Copy-Item -LiteralPath $r.data.result.savedPath -Destination (Join-Path $evidence $name)
   }
   Write-Output "Chapter $($stage+1) ${w}x$h rotation=$rotation badPicks=$($row.badPicks.Count) outside=$($row.outside.Count) overflow=$($row.overflow.Count)"
  }
 }
}
if($rows|Where-Object {$_.hpOverlaps -gt 0 -or $_.badPicks.Count -gt 0 -or $_.outside.Count -gt 0 -or $_.overflow.Count -gt 0}){throw 'Terrain review failed'}
