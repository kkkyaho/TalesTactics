using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [Serializable] sealed class OpeningVisualCapture
        {
            public int episode,stage,width,height,partyCount,deepWaterCount;
            public float textScale;
            public string png,objective,terrainHint,isolatedStorage;
            public string[] party,signals;
            public string exit;
        }
        [Serializable] sealed class OpeningVisualReport
        {
            public bool completed;
            public string scope="Production scene wiring and available GameView captures; no battle completion, balance or human visual approval claim.";
            public List<OpeningVisualCapture> captures=new List<OpeningVisualCapture>();
        }
        void AssertOpeningSceneMarkers(string kind,Vector2Int[] expected)
        {
            var icons=director.Board.GetComponentsInChildren<SpriteRenderer>()
                .Where(s=>s.name=="Field icon - "+kind&&s.gameObject.activeInHierarchy&&s.enabled).ToArray();
            Assert.That(icons.Length,Is.EqualTo(expected.Length),kind+" marker count");
            foreach(var coordinate in expected)
            {
                var position=director.Session.Grid[coordinate].WorldPosition(director.Session.Rules.TileHeight)+new Vector3(.25f,.30f,.18f);
                var matches=icons.Where(i=>Vector3.Distance(i.transform.position,position)<.001f).ToArray();
                Assert.That(matches.Length,Is.EqualTo(1),kind+" marker at "+coordinate);Assert.That(matches[0].sprite,Is.Not.Null);
                var screen=director.Board.BattleCamera.WorldToScreenPoint(matches[0].transform.position);
                Assert.That(screen.z,Is.GreaterThan(0));
                Assert.That(director.Hud.BattlefieldViewport.Contains(new Vector2(screen.x/Screen.width,screen.y/Screen.height)),Is.True,kind+" marker outside safe battlefield at "+coordinate);
            }
        }
        [UnityTest,Category("CommercialSlice")]
        public IEnumerator OpeningThreeBattlefieldsExposeRealObjectivesTerrainAndJoinedDeploymentForVisualReview()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","CommercialSlice20261117","Opening-"+Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);var report=new OpeningVisualReport();
            string receipt=Path.Combine(output,"capture-report.json");File.WriteAllText(receipt,JsonUtility.ToJson(report,true));
            float priorScale=director.Preferences.TextScale;
            try
            {
                for(int episode=1;episode<=3;episode++)
                {
                    // Prerequisite flags are fixtures. Recruitment, battle creation, board and HUD use production paths.
                    PreparationGuidanceEpisode(episode);director.Preferences.TextScale=1;
                    Assert.That(Path.GetFullPath(director.Profiles.Root).TrimEnd(Path.DirectorySeparatorChar),Is.EqualTo(Path.GetFullPath(testStorage).TrimEnd(Path.DirectorySeparatorChar)),"All campaign writes must remain in the shared isolated fixture.");
                    director.Deployment.Clear();director.Deployment.AddRange(Enumerable.Range(0,director.Catalog.Characters.Length).Where(director.CanDeploy));
                    director.RequestBattle();Assert.That(director.StoryActive,Is.True);director.FinishStory();
                    Assert.That(director.Session,Is.Not.Null,"Episode "+episode);director.StopAllCoroutines();director.RefreshViews();
                    yield return null;yield return new WaitForEndOfFrame();
                    var b=director.Session;Assert.That(b.CampaignStage,Is.EqualTo(episode+5));Assert.That(b.Crossover.State.OpeningRevision,Is.EqualTo(1));
                    Assert.That(director.State,Is.InstanceOf<CommandState>());Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
                    var party=b.Units.Where(u=>u.Team==Team.Player).ToArray();Assert.That(party.Length,Is.EqualTo(episode==1?2:episode==2?4:5));
                    string[] expectedIds=episode==1?new[]{"cless","mint"}:episode==2?new[]{"cless","mint","alphen","shionne"}:new[]{"cless","mint","alphen","shionne","farah"};
                    Assert.That(party.Select(u=>u.Data.Id),Is.EquivalentTo(expectedIds));
                    var living=b.Units.Where(u=>u.Alive).ToArray();Assert.That(living.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(living.Length));
                    foreach(var unit in living){Assert.That(b.Grid[unit.Position].Walkable,Is.True,unit.Data.Id);Assert.That(b.Grid[unit.Position].Occupant,Is.SameAs(unit));}
                    for(int i=0;i<party.Length;i++)Assert.That(party[i].Position,Is.EqualTo(CampaignContent.PlayerSpawn(b.CampaignStage,i)),party[i].Data.Id);
                    if(episode==3)Assert.That(party.Select(u=>u.Position.x).Distinct().OrderBy(x=>x),Is.EqualTo(new[]{2,6,10}));
                    var scenery=director.Board.GetComponentInChildren<PixelBattlefield>();Assert.That(scenery,Is.Not.Null);Assert.That(scenery.GroundMap,Is.SameAs(b.Grid));
                    var views=director.Board.GetComponentsInChildren<TileView>();Assert.That(views.Length,Is.EqualTo(b.Grid.Tiles.Count));
                    foreach(var tile in b.Grid.Tiles.Values)
                    {
                        var view=views.Single(v=>v.Coordinate==tile.Coordinate);var renderer=view.GetComponent<Renderer>();
                        Assert.That(renderer.enabled,Is.True);Assert.That(renderer.sharedMaterial,Is.SameAs(scenery.Surface(tile)),"Actual rendered surface "+tile.Coordinate);
                        if(tile.Terrain==TerrainType.Water){Assert.That(tile.Walkable,Is.False);Assert.That(renderer.sharedMaterial.GetFloat("_Water"),Is.EqualTo(1));}
                    }
                    var signals=episode==1?new[]{new Vector2Int(6,6)}:episode==2?new[]{new Vector2Int(2,8),new Vector2Int(10,8)}:new[]{new Vector2Int(1,8),new Vector2Int(11,8),new Vector2Int(6,11)};
                    var exits=episode==1?Array.Empty<Vector2Int>():new[]{episode==2?new Vector2Int(6,1):new Vector2Int(6,12)};
                    AssertOpeningSceneMarkers("rescue",signals);AssertOpeningSceneMarkers("linked",Array.Empty<Vector2Int>());AssertOpeningSceneMarkers("portal",exits);
                    Assert.That(director.Hud.CanOpenMission,Is.True);director.Hud.ShowMission();yield return null;
                    Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(b.Crossover.Description)),Is.True,"Mission UI must show the live objective.");
                    Click("임무 상세 보기");yield return null;
                    Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(MissionBriefing.Terrain(b.CampaignStage))),Is.True,"Mission UI must show the new route hint.");
                    if(episode!=2)Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("깊은 물은 통과 불가")),Is.True);
                    director.Hud.CloseMission();
                    if(episode!=2)
                    {
                        director.Hud.Inspect(new Vector2Int(0,5));yield return null;
                        Assert.That(director.Message,Does.Contain("깊은 물 · 통과 불가"));
                        Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("깊은 물 · 통과 불가")),Is.True,"Tile inspection must distinguish deep water from movement cost 2.");
                    }
                    director.Board.ResetCamera();director.RefreshViews();yield return null;yield return new WaitForEndOfFrame();
                    AssertOpeningSceneMarkers("rescue",signals);AssertOpeningSceneMarkers("portal",exits);
                    var capture=new OpeningVisualCapture{episode=episode,stage=b.CampaignStage,width=Screen.width,height=Screen.height,textScale=director.Preferences.TextScale,partyCount=party.Length,
                        deepWaterCount=b.Grid.Tiles.Values.Count(t=>t.Terrain==TerrainType.Water&&!t.Walkable),png="episode-"+episode.ToString("00")+"-battle.png",objective=b.Crossover.Description,terrainHint=MissionBriefing.Terrain(b.CampaignStage),
                        isolatedStorage=director.Profiles.Root,party=party.Select(u=>u.Data.Id+" "+u.Position).ToArray(),signals=signals.Select(p=>p.ToString()).ToArray(),exit=exits.Length==0?"none":exits[0].ToString()};
                    var texture=ScreenCapture.CaptureScreenshotAsTexture();
                    try{Assert.That(texture.width,Is.EqualTo(capture.width));Assert.That(texture.height,Is.EqualTo(capture.height));File.WriteAllBytes(Path.Combine(output,capture.png),texture.EncodeToPNG());}
                    finally{UnityEngine.Object.Destroy(texture);}
                    report.captures.Add(capture);File.WriteAllText(receipt,JsonUtility.ToJson(report,true));
                    director.Restart();yield return null;yield return null;
                }
                report.completed=true;File.WriteAllText(receipt,JsonUtility.ToJson(report,true));
                Debug.Log("Opening visual captures: "+output+" (actual GameView sizes recorded in capture-report.json)");
            }
            finally{director.Restart();director.Preferences.TextScale=priorScale;}
        }
    }
}
