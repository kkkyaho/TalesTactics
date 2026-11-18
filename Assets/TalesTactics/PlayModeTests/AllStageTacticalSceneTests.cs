using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        // Entry and presentation checks only. Prerequisite flags and levels are fixtures;
        // these tests never award a combat victory or claim a legal-command playthrough.
        void PrepareAllStageTacticalScene(int stage)
        {
            director.Restart();director.Campaign=new CampaignSave{SelectedStage=stage};
            director.TrainingMode=false;director.UseCT=false;director.UseFixedSpeedOrder=false;
            director.SelectedStage=stage;
            int completed=PersonalScenarios.Contains(stage)?20:CrossoverCampaign.Episode(stage).Number-1;
            for(int n=1;n<=completed;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            if(PersonalScenarios.Contains(stage))
                for(int previous=stage-PersonalScenarios.Tier(stage);previous<stage;previous++)
                    director.Campaign.StoryProgress.Add(PersonalScenarios.Flag(previous));
            foreach(var character in director.Catalog.Characters)director.Campaign.Get(character.Id).Level=35;
            director.Deployment.Clear();
            director.Deployment.AddRange(Enumerable.Range(0,director.Catalog.Characters.Length).Where(director.CanDeploy).Take(6));
            director.NormalizeDeployment();director.Hud.ShowDeployment();
            Assert.That(CampaignStages.Unlocked(director.Campaign,stage),Is.True,"Prerequisite fixture "+stage);
            Assert.That(Path.GetFullPath(director.Profiles.Root).TrimEnd(Path.DirectorySeparatorChar),
                Is.EqualTo(Path.GetFullPath(testStorage).TrimEnd(Path.DirectorySeparatorChar)),"Every write stays in the isolated scene fixture.");
        }
        static GridMap AuthoredAllStageMap(int stage)=>CrossoverOpeningMissions.Contains(stage)
            ?CrossoverOpeningMissions.Map(stage):CrossoverTacticalMissions.Get(stage).Map();
        static Vector2Int[] AuthoredAllStageDevices(int stage)
        {
            if(stage==CrossoverCampaign.FirstStage+1)return new[]{CrossoverOpeningMissions.EnemyPosition(2,4),CrossoverOpeningMissions.EnemyPosition(2,5)};
            return CrossoverTacticalMissions.RequiresDevices(stage)
                ?new[]{4,5,8}.Select(i=>CrossoverTacticalMissions.Get(stage).EnemySpawns[i]).ToArray():Array.Empty<Vector2Int>();
        }
        bool AllStageHudContains(string text)=>director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(text));
        void AssertAllStagePreview(int stage,GridMap expected)
        {
            var preview=director.Hud.GetComponentsInChildren<MissionMapPreview>().Single();
            Assert.That(preview.raycastTarget,Is.False);
            Canvas.ForceUpdateCanvases();var mesh=preview.canvasRenderer.GetMesh();
            Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.EqualTo(expected.Tiles.Count*4),"Preview tile geometry "+stage);
            var vertices=mesh.vertices;var colors=mesh.colors32;var rect=preview.rectTransform.rect;
            int width=expected.Tiles.Keys.Max(p=>p.x)+1,depth=expected.Tiles.Keys.Max(p=>p.y)+1;
            float size=Mathf.Min(rect.width/width,rect.height/depth);
            Assert.That(size,Is.GreaterThan(1.3f),"Preview cells must have a visible interior.");
            Vector2 origin=rect.center-new Vector2(width,depth)*size*.5f;
            var signals=CrossoverTacticalMissions.Signals(stage);var devices=AuthoredAllStageDevices(stage);
            bool contract=PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==2&&PersonalScenarios.Arc(stage).Owner=="cless";
            foreach(var tile in expected.Tiles.Values)
            {
                var center=origin+((Vector2)tile.Coordinate+Vector2.one*.5f)*size;
                var quad=Enumerable.Range(0,vertices.Length/4).Where(q=>Vector2.Distance((Vector2)((vertices[q*4]+vertices[q*4+2])*.5f),center)<.01f).ToArray();
                Assert.That(quad.Length,Is.EqualTo(1),"Rendered preview cell "+stage+" "+tile.Coordinate);
                Color color=tile.Terrain==TerrainType.Water?(tile.Walkable?new Color(.12f,.39f,.49f):new Color(.08f,.22f,.40f)):
                    !tile.Walkable?new Color(.20f,.26f,.31f):Color.Lerp(new Color(.30f,.41f,.46f),new Color(.51f,.60f,.61f),tile.Height*.22f);
                if(CrossoverBattle.HasExit(stage)&&tile.Coordinate==CrossoverTacticalMissions.Exit(stage))color=new Color(.94f,.73f,.29f);
                if(CrossoverBattle.HasSignals(stage)&&signals.Contains(tile.Coordinate))color=new Color(.2f,.9f,.7f);
                if(devices.Contains(tile.Coordinate))color=new Color(.96f,.52f,.24f);
                if(contract&&tile.Coordinate==CrossoverTacticalMissions.Contract(stage))color=new Color(.8f,.7f,1);
                for(int corner=0;corner<4;corner++)Assert.That(colors[quad[0]*4+corner],Is.EqualTo((Color32)color),"Preview terrain/objective color "+stage+" "+tile.Coordinate);
            }
        }
        void AssertAllStageFieldMarkers(string kind,Vector2Int[] expected)
        {
            var icons=director.Board.GetComponentsInChildren<SpriteRenderer>()
                .Where(s=>s.name=="Field icon - "+kind&&s.gameObject.activeInHierarchy&&s.enabled).ToArray();
            Assert.That(icons.Length,Is.EqualTo(expected.Distinct().Count()),kind+" count, stage "+director.SelectedStage);
            foreach(var coordinate in expected.Distinct())
            {
                var position=director.Session.Grid[coordinate].WorldPosition(director.Session.Rules.TileHeight)+new Vector3(.25f,.30f,.18f);
                var matches=icons.Where(i=>Vector3.Distance(i.transform.position,position)<.001f).ToArray();
                Assert.That(matches.Length,Is.EqualTo(1),kind+" at "+coordinate+", stage "+director.SelectedStage);
                Assert.That(matches[0].sprite,Is.Not.Null);
            }
        }
        void AssertAllStageBattleBoard(int stage,GridMap expected)
        {
            var b=director.Session;Assert.That(b,Is.Not.Null);Assert.That(b.CampaignStage,Is.EqualTo(stage));
            Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));Assert.That(director.State,Is.InstanceOf<CommandState>());
            Assert.That(b.Crossover.State.TacticsRevision,Is.EqualTo(CrossoverTacticalMissions.Contains(stage)?1:0));
            Assert.That(b.Crossover.State.OpeningRevision,Is.EqualTo(CrossoverOpeningMissions.Contains(stage)?1:0));
            Assert.That(b.Crossover.State.Nodes,Is.Zero);Assert.That(b.Crossover.State.TrialActions,Is.Zero);
            var scenery=director.Board.GetComponentInChildren<PixelBattlefield>();Assert.That(scenery,Is.Not.Null);
            Assert.That(scenery.GroundMap,Is.SameAs(b.Grid));Assert.That(b.Grid.Tiles.Count,Is.EqualTo(expected.Tiles.Count));
            var views=director.Board.GetComponentsInChildren<TileView>();Assert.That(views.Length,Is.EqualTo(expected.Tiles.Count));
            foreach(var wanted in expected.Tiles.Values)
            {
                var tile=b.Grid[wanted.Coordinate];Assert.That(tile,Is.Not.Null);
                Assert.That(tile.Height,Is.EqualTo(wanted.Height),"Height "+stage+" "+wanted.Coordinate);
                Assert.That(tile.Walkable,Is.EqualTo(wanted.Walkable),"Walkability "+stage+" "+wanted.Coordinate);
                Assert.That(tile.Terrain,Is.EqualTo(wanted.Terrain));Assert.That(tile.MovementCost,Is.EqualTo(wanted.MovementCost));
                var renderer=views.Single(v=>v.Coordinate==wanted.Coordinate).GetComponent<Renderer>();
                Assert.That(renderer.enabled,Is.True);Assert.That(renderer.sharedMaterial,Is.SameAs(scenery.Surface(tile)));
                if(tile.Terrain==TerrainType.Water&&!tile.Walkable)Assert.That(renderer.sharedMaterial.GetFloat("_Water"),Is.EqualTo(1));
            }
            var party=b.Units.Where(u=>u.Team==Team.Player).ToArray();var enemies=b.Units.Where(u=>u.Team==Team.Enemy).ToArray();
            Assert.That(party.Length,Is.InRange(2,6));Assert.That(enemies.Length,Is.EqualTo(9));
            bool splitFront=stage==CrossoverCampaign.FirstStage+10;
            for(int i=0;i<party.Length;i++)
            {
                var spawn=CrossoverOpeningMissions.Contains(stage)?CrossoverOpeningMissions.PlayerPosition(stage-CrossoverCampaign.FirstStage+1,i):CrossoverTacticalMissions.Get(stage).PlayerSpawns[i];
                // The preserved split-front rule packs each half by its actual party size.
                if(splitFront){int west=(party.Length+1)/2;bool east=i>=west;int index=east?i-west:i;spawn=new Vector2Int((east?9:1)+index%2,1+index/2);}
                Assert.That(party[i].Position,Is.EqualTo(spawn),"Player spawn "+stage+" "+party[i].Data.Id);
            }
            var enemySpawns=CrossoverTacticalMissions.EnemySpawns(stage);
            for(int i=0;i<enemies.Length;i++)
            {
                var spawn=splitFront&&i==0?new Vector2Int(3,11):splitFront&&i==3?new Vector2Int(3,8):splitFront&&i==4?new Vector2Int(10,11):enemySpawns[i];
                Assert.That(enemies[i].Position,Is.EqualTo(spawn),"Enemy slot "+stage+" "+i);
            }
            var active=b.Units.Where(u=>u.Alive&&!u.CrossoverReserve).ToArray();
            Assert.That(active.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(active.Length));
            foreach(var unit in active){Assert.That(b.Grid[unit.Position].Walkable,Is.True);Assert.That(b.Grid[unit.Position].Occupant,Is.SameAs(unit));}
            if(PersonalScenarios.Contains(stage))Assert.That(party.Any(u=>u.Data.Id==PersonalScenarios.Arc(stage).Owner),Is.True,"Personal owner must deploy.");
            var signals=CrossoverBattle.HasSignals(stage)?CrossoverTacticalMissions.Signals(stage):Array.Empty<Vector2Int>();
            var devices=AuthoredAllStageDevices(stage);
            Assert.That(b.Crossover.ObjectiveDevices.Select(u=>u.Position),Is.EquivalentTo(devices));
            Assert.That(b.Crossover.SignalPositions,Is.EqualTo(CrossoverTacticalMissions.Signals(stage)));
            Assert.That(b.Crossover.ExitPosition,Is.EqualTo(CrossoverTacticalMissions.Exit(stage)));
            AssertAllStageFieldMarkers("rescue",signals.Concat(devices).ToArray());
            AssertAllStageFieldMarkers("linked",Array.Empty<Vector2Int>());
            AssertAllStageFieldMarkers("portal",CrossoverBattle.HasExit(stage)?new[]{CrossoverTacticalMissions.Exit(stage)}:Array.Empty<Vector2Int>());
            AssertAllStageFieldMarkers("contract",b.Crossover.UsesContract?new[]{CrossoverTacticalMissions.Contract(stage)}:Array.Empty<Vector2Int>());
            AssertAllStageFieldMarkers("summon",enemies.Select((u,i)=>new{u,i}).Where(v=>v.u.Summoned&&!v.u.Alive).Select(v=>enemySpawns[v.i]).ToArray());
        }
        IEnumerator InspectAllStageCombatScene(int stage,int choice)
        {
            PrepareAllStageTacticalScene(stage);yield return null;
            var expected=AuthoredAllStageMap(stage);string before=JsonUtility.ToJson(director.Campaign);
            string path=director.Profiles.PathFor(director.Profiles.Slot),disk=File.ReadAllText(path);
            director.Hud.ShowMission();yield return null;yield return new WaitForEndOfFrame();
            AssertAllStagePreview(stage,expected);
            Click("임무 상세 보기");yield return null;
            Assert.That(AllStageHudContains(MissionBriefing.Terrain(stage)),Is.True,"Preparation route hint "+stage);
            director.Hud.CloseMission();
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before),"Map inspection must not modify progress.");
            Assert.That(File.ReadAllText(path),Is.EqualTo(disk),"Map inspection must not write a save.");
            director.RequestBattle();Assert.That(director.StoryActive,Is.True,"Before story "+stage);
            Assert.That(director.Session,Is.Null);director.FinishStory();
            bool ordered=PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==1||stage==CrossoverCampaign.FirstStage+12;
            if(ordered)
            {
                Assert.That(director.Session,Is.Null,"Ordered routes must wait for a choice.");yield return null;
                foreach(var signal in CrossoverTacticalMissions.Signals(stage))Assert.That(AllStageHudContains(signal.ToString()),Is.True,"Route choice must use authored signal coordinates "+stage);
                Click(PersonalScenarios.RouteName(stage,choice)+"부터");
            }
            Assert.That(director.Session,Is.Not.Null,"Battle creation "+stage);director.StopAllCoroutines();director.RefreshViews();
            yield return null;yield return new WaitForEndOfFrame();AssertAllStageBattleBoard(stage,expected);
            if(ordered)Assert.That(director.Session.Crossover.State.Choice,Is.EqualTo(choice));
            director.Hud.ShowMission();yield return null;
            Assert.That(director.Hud.MissionOpen,Is.True);Assert.That(AllStageHudContains(director.Session.Crossover.Description),Is.True,"Live objective "+stage);
            Click("임무 상세 보기");yield return null;
            Assert.That(AllStageHudContains(MissionBriefing.Terrain(stage)),Is.True,"Live route hint "+stage);
            director.Hud.CloseMission();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(director.Campaign.StoryProgress,Does.Not.Contain(CampaignStages.Id(stage)),"Entry inspection must not grant a combat clear.");
            Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Ongoing));
            director.Restart();yield return null;yield return null;
        }
        [UnityTest,Category("AllStageTactics")]
        public IEnumerator AllTwentyMainBattleScenesMatchAuthoredPreviewBoardAndLiveObjectives()
        {
            TitleSetup();Assert.That(director.StartTitleGame(0),Is.True);
            try
            {
                for(int stage=CrossoverCampaign.FirstStage;stage<CrossoverCampaign.FirstStage+CrossoverCampaign.EpisodeCount;stage++)
                    yield return InspectAllStageCombatScene(stage,1);
            }
            finally{director.Restart();}
        }
        [UnityTest,Category("AllStageTactics")]
        public IEnumerator AllTwentyPersonalCombatScenesMatchDynamicRoutesTrialsDevicesAndExitMarkers()
        {
            TitleSetup();Assert.That(director.StartTitleGame(0),Is.True);
            try
            {
                for(int stage=PersonalScenarios.FirstStage;stage<PersonalScenarios.FirstStage+PersonalScenarios.Count;stage++)
                    if(PersonalScenarios.Tier(stage)>0)yield return InspectAllStageCombatScene(stage,((stage-PersonalScenarios.FirstStage)/3)%2);
            }
            finally{director.Restart();}
        }
        [UnityTest,Category("AllStageTactics")]
        public IEnumerator TenPersonalFirstChaptersKeepRealDialogueCompletionAndReadOnlyReplayWithoutCombat()
        {
            TitleSetup();Assert.That(director.StartTitleGame(0),Is.True);
            try
            {
                for(int stage=PersonalScenarios.FirstStage;stage<PersonalScenarios.FirstStage+PersonalScenarios.Count;stage+=3)
                {
                    PrepareAllStageTacticalScene(stage);yield return null;
                    var arc=PersonalScenarios.Arc(stage);int weaponCount=CampaignInventory.Owned(director.Campaign,arc.WeaponId);
                    director.RequestBattle();Assert.That(director.StoryActive,Is.True);
                    foreach(bool after in new[]{false,true})
                    {
                        var lines=PersonalScenarios.Story(stage,after);
                        for(int index=0;index<lines.Length;index++)
                        {
                            yield return null;
                            Assert.That(director.Session,Is.Null,"Dialogue chapter must never build a battle.");
                            Assert.That(director.StoryActive,Is.True);Assert.That(director.StoryStage,Is.EqualTo(stage));
                            Assert.That(director.StoryIndex,Is.EqualTo(index));
                            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="DialogueBody").text,Is.EqualTo(lines[index].Text));
                            Click(index==lines.Length-1?"이야기 마치기":"다음 대사");
                        }
                    }
                    yield return null;Assert.That(director.StoryActive,Is.False);Assert.That(director.Session,Is.Null);
                    Assert.That(director.Campaign.StoryProgress,Has.Member(PersonalScenarios.Flag(stage)));
                    Assert.That(CampaignInventory.Owned(director.Campaign,arc.WeaponId),Is.EqualTo(weaponCount),"The first dialogue must not grant the trial weapon.");
                    var path=director.Profiles.PathFor(director.Profiles.Slot);
                    Assert.That(new CampaignFile(path).Load().StoryProgress,Has.Member(PersonalScenarios.Flag(stage)),"Dialogue completion must persist in isolated storage.");
                    string before=JsonUtility.ToJson(director.Campaign),disk=File.ReadAllText(path);
                    director.SelectedStage=stage;director.RequestBattle();Assert.That(director.StoryActive,Is.True);Assert.That(director.Session,Is.Null);
                    director.FinishStory();yield return null;
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
                }
            }
            finally{director.Restart();}
        }
    }
}
