using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using TalesTactics;
using TalesTactics.Editor;
public static class RosterStageMatrix
{
    static readonly string[] Ids={"cless","mint","farah","tear","jade","natalia","velvet","alphen","shionne","kisara"};
    public static string Begin(string output) { var task=Main(output);if(task.IsFaulted)throw task.Exception;return output; }
    public static async Task<object> Main(string output)
    {
        string expected="C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project";
        if(!Path.GetFullPath(Path.Combine(Application.dataPath,"..")).Replace('\\','/').Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Validation checkout required.");
        output=Path.GetFullPath(output);if(!output.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/",StringComparison.OrdinalIgnoreCase)||File.Exists(output)||Directory.Exists(output))throw new InvalidOperationException("Fresh task evidence path required.");
        var battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
        if(!Application.isPlaying||EditorApplication.isPaused||battle==null||battle.enabled||battle.Session==null||!battle.Profiles.Root.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Disabled Director with isolated storage and existing board required.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var prior=battle.Session;var random=UnityEngine.Random.state;string state=SkillRosterVfxFixture.State(prior.Units),grid=SkillRosterVfxFixture.GridState(prior.Grid,prior.Units),campaign=JsonUtility.ToJson(battle.Campaign);
        string content=EditorJsonUtility.ToJson(battle.Catalog);int draws=prior.Resolver.RandomDraws;var setter=typeof(BattleDirector).GetProperty("Session").GetSetMethod(true);
        var rows=new List<object>();var reserveObservations=new List<object>();int reserveOverrides=0;var errors=new List<string>();bool restored=false;string started=DateTime.UtcNow.ToString("o");
        try
        {
            foreach(var id in Ids)if(CharacterMotionRegistry.Load(id)?.OwnerId!=id)throw new InvalidOperationException("Authored profile missing: "+id);
            var indexes=Ids.Select(id=>Array.FindIndex(battle.Catalog.Characters,c=>c.Id==id)).ToArray();if(indexes.Any(i=>i<0))throw new InvalidOperationException("Roster missing from catalog.");
            foreach(int stage in Enumerable.Range(-1,CampaignStages.Count+1))
            for(int offset=0;offset<indexes.Length;offset+=battle.Catalog.Rules.MaxDeployment)
            {
                var deployment=indexes.Skip(offset).Take(battle.Catalog.Rules.MaxDeployment).ToArray();
                var session=new BattleSession(battle.Catalog,deployment,level:25,enemyLevel:25,campaignStage:stage,teamTurns:true,tacticalCombat:true,bossEncounters:true,expandedEncounters:true);
                setter.Invoke(battle,new object[]{session});session.Advance();battle.Board.Build(session);battle.Board.Sync();
                for(int i=0;i<3;i++)await Awaitable.NextFrameAsync();
                string unitState=SkillRosterVfxFixture.State(session.Units),gridState=SkillRosterVfxFixture.GridState(session.Grid,session.Units);int sessionDraws=session.Resolver.RandomDraws;
                var allies=session.Units.Where(u=>u.Team==Team.Player).ToArray();
                if(allies.Length!=deployment.Length)throw new InvalidOperationException("Unexpected deployment size.");
                var originalFacing=allies.Select(u=>u.Facing).ToArray();
                var originalReserve=allies.Select(u=>u.CrossoverReserve).ToArray();
                foreach(var unit in allies)
                {
                    var originalMotion=battle.Board.GetComponentsInChildren<CharacterMotion>(true).Single(m=>m.Owner==unit);
                    bool active=originalMotion.gameObject.activeInHierarchy;
                    if(active==unit.CrossoverReserve)throw new InvalidOperationException("Native reserve visibility mismatch: "+stage+"/"+unit.Data.Id);
                    if(unit.CrossoverReserve)
                    {
                        reserveObservations.Add(new{stage,batch=offset/battle.Catalog.Rules.MaxDeployment,characterId=unit.Data.Id,nativeReserve=true,nativeActive=active,hiddenAsExpected=!active,temporaryPresentationOverride=true});
                        unit.CrossoverReserve=false;reserveOverrides++;
                    }
                }
                // Only this temporary matrix session bypasses split-front reserves; native hiding is checked above.
                // Active presentation still advances through real Board.Sync and Unity frames for every assertion.
                battle.Board.Sync();
                foreach(Facing view in new[]{Facing.Front,Facing.Back,Facing.Left,Facing.Right})
                {
                    foreach(var unit in allies)unit.Facing=Enum.GetValues(typeof(Facing)).Cast<Facing>().Single(f=>CharacterMotion.ViewFacing(f,battle.Board.BattleCamera.transform.rotation)==view);
                    // Model-facing changes enter presentation through the same public Sync used by gameplay.
                    battle.Board.Sync();
                    for(int i=0;i<2;i++)await Awaitable.NextFrameAsync();
                    foreach(var unit in allies)
                    {
                        var motion=battle.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==unit);var sr=motion.GetComponent<SpriteRenderer>();var profile=CharacterMotionRegistry.Load(unit.Data.Id);
                        var root=motion.transform.parent.parent;var contact=root.Find("Contact shadow");var shadow=root.GetComponentInChildren<ProjectedUnitShadow>();
                        bool correct=motion.AuthoredProfile==profile&&profile.Idle.Frames.Any(f=>f.Get(view)==sr.sprite);
                        bool anchor=UnitHeadAnchors.TryGetLocal(sr.sprite,out var head);bool projected=shadow!=null&&shadow.SourceSprite==sr.sprite;
                        bool grounded=Vector3.Distance(root.position,session.Grid[unit.Position].WorldPosition(session.Rules.TileHeight))<.025f;
                        var health=battle.Board.GetComponentsInChildren<UnitHealthDisplay>(true).SingleOrDefault(h=>h.Unit==unit);
                        var headScreen=battle.Board.UnitHeadScreenOrigin(unit);bool finite=!float.IsNaN(headScreen.x)&&!float.IsInfinity(headScreen.x)&&!float.IsNaN(headScreen.y)&&!float.IsInfinity(headScreen.y);
                        bool enemyFallback=battle.Board.GetComponentsInChildren<CharacterMotion>().Where(m=>m.Owner.Team!=Team.Player).All(m=>m.AuthoredProfile==null);
                        bool passed=correct&&anchor&&projected&&grounded&&contact!=null&&health!=null&&finite&&enemyFallback;
                        rows.Add(new{stage,batch=offset/battle.Catalog.Rules.MaxDeployment,characterId=unit.Data.Id,view=view.ToString(),sprite=sr.sprite?.name,authoredProfile=motion.AuthoredProfile?.OwnerId,correct,anchor,projected,grounded,healthPresent=health!=null,finiteHead=finite,enemyFallback,passed});
                        if(!passed)throw new InvalidOperationException("Board linkage check failed: "+stage+"/"+unit.Data.Id+"/"+view);
                    }
                }
                for(int i=0;i<allies.Length;i++){allies[i].Facing=originalFacing[i];allies[i].CrossoverReserve=originalReserve[i];}
                if(unitState!=SkillRosterVfxFixture.State(session.Units)||gridState!=SkillRosterVfxFixture.GridState(session.Grid,session.Units)||sessionDraws!=session.Resolver.RandomDraws)throw new InvalidOperationException("Presentation check changed fixture gameplay.");
                File.WriteAllText(output+".progress.json",JsonConvert.SerializeObject(new{stage,rows=rows.Count,expectedRows=(CampaignStages.Count+1)*Ids.Length*4}));
            }
        }
        catch(Exception e){errors.Add(e.ToString());}
        finally
        {
            try{setter.Invoke(battle,new object[]{prior});battle.Board.Build(prior);battle.Board.Sync();for(int i=0;i<3;i++)await Awaitable.NextFrameAsync();UnityEngine.Random.state=random;restored=battle.Session==prior&&state==SkillRosterVfxFixture.State(prior.Units)&&grid==SkillRosterVfxFixture.GridState(prior.Grid,prior.Units)&&campaign==JsonUtility.ToJson(battle.Campaign)&&draws==prior.Resolver.RandomDraws&&content==EditorJsonUtility.ToJson(battle.Catalog);}
            catch(Exception e){errors.Add("Restoration: "+e);}
        }
        var result=new{passed=errors.Count==0&&restored&&rows.Count==(CampaignStages.Count+1)*Ids.Length*4,startedUtc=started,finishedUtc=DateTime.UtcNow.ToString("o"),stageCount=CampaignStages.Count+1,roster=Ids,expectedRows=(CampaignStages.Count+1)*Ids.Length*4,observedRows=rows.Count,reserveOverrides,reserveObservations,restored,errors,rows,scope="Actual BoardView.Build for training and every campaign stage in two presentation-only full-roster batches, with real advancing Unity frames and four view directions. Temporary BattleSession values explicitly bypass progression/deployment eligibility to test renderer linkage. Native CrossoverReserve visibility is first asserted and recorded, then reserve flags are temporarily cleared only in matrix sessions so every actor can undergo active rendered-direction checks; original flags are restored before gameplay-preservation comparison. This does not claim natural campaign traversal, unlock changes, damage execution, or Android-device playback. Original isolated session and global RNG are restored."};
        using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write(JsonConvert.SerializeObject(result,Formatting.Indented));
        return new{result.passed,result.stageCount,result.observedRows,result.restored,output,errors};
    }
}