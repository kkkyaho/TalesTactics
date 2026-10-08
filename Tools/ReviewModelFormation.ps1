# Run against the open project in Play Mode; uses isolated, in-memory review progress.
$ErrorActionPreference='Stop'
function EvalFormation([string]$code) {
    $response=unity command eval --caller plugin --skill ui-ugui --format json -- $code | ConvertFrom-Json
    if(!$response.success -or !$response.data.result.success){throw ($response|ConvertTo-Json -Depth 10)}
    return $response.data.result.result
}
$evidence=Join-Path (Split-Path $PSScriptRoot -Parent) 'Docs/ModelFormation'
New-Item -ItemType Directory -Force $evidence | Out-Null
EvalFormation @'
var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(!UnityEditor.EditorApplication.isPlaying||d==null)throw new System.InvalidOperationException("Open TestBattle in Play Mode first");
d.ConfigureStorage(System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath,"FormationReview-"+System.Guid.NewGuid().ToString("N")));
d.PersistCampaign=_=>true;d.TrainingMode=false;d.Campaign=new TalesTactics.CampaignSave();d.SelectedStage=2;
d.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});
foreach(var c in d.Catalog.Characters)d.Campaign.Get(c.Id).Level=7;
d.Deployment.Clear();d.Deployment.AddRange(TalesTactics.TacticalDevelopment.Recommended(d.Catalog));
d.Campaign.Get("cless").Trait=TalesTactics.TacticalTrait.Swift;
d.Campaign.Inventory.Add(new TalesTactics.OwnedEquipment{Id="iron-sword",Count=1});
d.Campaign.Get("cless").Equipment[0]="iron-sword";
d.Hud.ShowDeployment();return true;
'@ | Out-Null
$rows=@()
foreach($pair in @(@(1024,768),@(1280,800),@(1366,768),@(1920,1080),@(2560,1080))){
    $w=$pair[0];$h=$pair[1]
    $resize=@'
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
var instance=t.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
var g=t.GetProperty("currentGroup").GetValue(instance);
var size=System.Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{System.Enum.Parse(a.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),WIDTH,HEIGHT,"Formation review WIDTHxHEIGHT"});
g.GetType().GetMethod("AddCustomSize").Invoke(g,new[]{size});var v=a.GetType("UnityEditor.GameView");
v.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(UnityEditor.EditorWindow.GetWindow(v),(int)g.GetType().GetMethod("GetTotalCount").Invoke(g,null)-1);
return UnityEngine.Time.frameCount;
'@
    EvalFormation $resize.Replace('WIDTH',"$w").Replace('HEIGHT',"$h") | Out-Null
    Start-Sleep -Milliseconds 700
    foreach($scale in @(1,1.3)){
        EvalFormation ('var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Preferences.TextScale='+$scale.ToString([Globalization.CultureInfo]::InvariantCulture)+'f;d.Hud.ShowDeployment();return UnityEngine.Time.frameCount;') | Out-Null
        Start-Sleep -Milliseconds 500
        $result=EvalFormation @'
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
        $rows+=$result
        $rows|ConvertTo-Json -Depth 8|Set-Content (Join-Path $evidence 'aspect-matrix.json') -Encoding utf8
        if($result.outside.Count -gt 0 -or $result.overflow.Count -gt 0){throw ($result|ConvertTo-Json -Depth 6)}
        if(($w -in @(1024,1920,2560) -and $scale -eq 1) -or ($w -eq 1024 -and $scale -gt 1)){
            # Let TMP upload any meshes dirtied by ForceMeshUpdate / auto sizing before capturing.
            Start-Sleep -Milliseconds 700
            $name="$w-$h-$scale.png"
            $capture=unity command capture_game_view --caller plugin --skill ui-ugui --format json -- --width $w --height $h --source screen --save_path "Docs/ModelFormation/$name" | ConvertFrom-Json
            if(!$capture.success){throw 'Formation capture failed'}
            Copy-Item $capture.data.result.savedPath (Join-Path $evidence $name)
        }
        Write-Output "$w x $h text=$scale buttons=$($result.buttons) overflow=$($result.overflow.Count) offscreen=$($result.outside.Count) frame=$($result.frame)"
    }
}
EvalFormation 'var d=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();d.Preferences.TextScale=1;return UnityEngine.Time.frameCount;' | Out-Null
