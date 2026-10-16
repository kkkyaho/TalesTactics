using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        static readonly string[] OriginalTrioIds={"cless.0","jade.6","jade.3"};
        static readonly string[] OriginalTrioParty={"cless","mint","jade","farah","tear","alphen"};

        [Test] public void OriginalTrioDispatchSelectsOnlyExactIdsAndLeavesOtherContentUntouched()
        {
            var expected=new Dictionary<string,SkillSignature>{{"cless.0",SkillSignature.DemonFang},{"jade.6",SkillSignature.MeteorStorm},{"jade.3",SkillSignature.ThunderBlade}};
            var skills=director.Catalog.Characters.SelectMany(c=>c.Skills.Concat(new[]{c.BasicAttack,c.UltimateSkill})).Where(s=>s!=null).Distinct().ToArray();
            var content=skills.Select(JsonUtility.ToJson).ToArray();int matches=0;
            foreach(var skill in skills)
            {
                var signature=SkillSignatures.Resolve(skill);
                Assert.That(signature,Is.EqualTo(expected.TryGetValue(skill.Id,out var value)?value:SkillSignature.None),skill.Id);
                if(signature!=SkillSignature.None)matches++;
            }
            Assert.That(matches,Is.EqualTo(3));Assert.That(SkillSignatures.Resolve(null),Is.EqualTo(SkillSignature.None));
            Assert.That(skills.Select(JsonUtility.ToJson),Is.EqualTo(content),"Dispatch must not rewrite authored combat or presentation data");
            var copied=Object.Instantiate(skills.Single(s=>s.Id=="cless.0"));
            try
            {
                copied.Id="cless.0.variant";
                Assert.That(SkillSignatures.Resolve(copied),Is.EqualTo(SkillSignature.None),"A shared Wave pattern must not opt other skills into Demon Fang");
                copied.Id="CLess.0";Assert.That(SkillSignatures.Resolve(copied),Is.EqualTo(SkillSignature.None));
            }
            finally{Object.Destroy(copied);}
        }

        [UnityTest] public IEnumerator OriginalTrioActualActionsWaitForContactAndResolveCostDamageAndRandomOnce()
        {
            director.Deployment.Clear();director.Deployment.AddRange(OriginalTrioParty.Select(id=>System.Array.FindIndex(director.Catalog.Characters,c=>c.Id==id)));
            director.Preferences.Reactions=false;director.Preferences.MissionEvents=false;
            director.BeginBattle();director.StopAllCoroutines();yield return null;
            var catalogSkills=director.Catalog.Characters.SelectMany(c=>c.Skills).Where(s=>s!=null).ToArray();
            var content=catalogSkills.Select(JsonUtility.ToJson).ToArray();
            var normalDamage=new Dictionary<string,int>();
            foreach(bool reduced in new[]{false,true})foreach(string id in OriginalTrioIds)
            {
                var actor=director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id==id.Split('.')[0]);
                var target=director.Session.Units.First(u=>u.Team==Team.Enemy);
                var skill=actor.Data.Skills.Single(s=>s.Id==id);var signature=SkillSignatures.Resolve(skill);
                OriginalTrioArrange(actor,target);
                foreach(var unit in director.Session.Units)
                {
                    unit.Level=unit.Team==Team.Player?25:50;unit.EXP=unit.BattleEXP=0;unit.Statuses.Clear();unit.Cooldowns.Clear();
                    unit.CurrentHP=unit.Stats.HP;unit.CurrentMP=unit.Stats.MP;unit.SpecialGauge=100;unit.Acted=false;
                }
                actor.BeginTurn();director.Session.Active=actor;director.Preferences.ReducedEffects=reduced;
                director.SetState(new CommandState(director));director.RefreshViews();director.Hud.ClearCombatFeedback();yield return null;
                Assert.That(director.Session.Resolver.CanUse(actor,skill),Is.Null,id);
                Assert.That(director.Session.Resolver.InRange(actor,skill,target.Position),Is.True,id);
                var expected=OriginalTrioReference(out var copies);
                int actorIndex=director.Session.Units.IndexOf(actor),targetIndex=director.Session.Units.IndexOf(target);
                copies[actorIndex].Facing=SkillResolver.Toward(actor.Position,target.Position);
                Assert.That(expected.Execute(copies[actorIndex],skill,target.Position,out var message),Is.True,message);
                var health=director.Session.Units.Select(u=>u.CurrentHP).ToArray();var mp=director.Session.Units.Select(u=>u.CurrentMP).ToArray();
                var positions=director.Session.Units.Select(u=>u.Position).ToArray();string random=JsonUtility.ToJson(Random.state);
                int expectedRecipients=director.Session.Resolver.Targets(actor,skill,target.Position).Count();
                int events=0,hpChanges=0,mpChanges=0,lastHP=target.CurrentHP,lastMP=actor.CurrentMP;bool flightSeen=false,numberSeen=false;
                System.Action<UnitRuntime,SkillData,Vector2Int> resolved=(unit,used,aim)=>{if(unit==actor&&used==skill)events++;};
                director.Session.Resolver.SkillResolved+=resolved;
                float deadline=Time.realtimeSinceStartup+12;
                try
                {
                    director.StartCoroutine(director.Execute(skill,target.Position));
                    do
                    {
                        yield return null;
                        var particles=director.Board.GetComponentsInChildren<ImpactParticles>();
                        if(particles.Any(p=>p.Travelling&&p.Signature==signature))flightSeen=true;
                        if(events==0)
                        {
                            Assert.That(director.Session.Units.Select(u=>u.CurrentHP),Is.EqualTo(health),id+" hit before arrival");
                            Assert.That(director.Session.Units.Select(u=>u.CurrentMP),Is.EqualTo(mp),id+" charged before arrival");
                            Assert.That(director.Hud.HasHealthFeedback(target),Is.False);
                        }
                        if(target.CurrentHP!=lastHP)
                        {
                            hpChanges++;lastHP=target.CurrentHP;numberSeen=director.Hud.HasHealthFeedback(target);
                            int numbers=director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Count(t=>t.name=="HP feedback");
                            Assert.That(numbers,Is.EqualTo(expectedRecipients),id+" must show one damage number per actual target, not per visual meteor/bolt");
                        }
                        if(actor.CurrentMP!=lastMP){mpChanges++;lastMP=actor.CurrentMP;}
                        if(!(director.State is ActionExecutionState)&&particles.Length==0&&director.Board.GetComponentsInChildren<CombatEffect>().Length==0)break;
                    }while(Time.realtimeSinceStartup<deadline);
                }
                finally{director.Session.Resolver.SkillResolved-=resolved;}
                Assert.That(flightSeen,Is.True,id+" signature travel was not rendered");
                Assert.That(director.State,Is.TypeOf<CommandState>(),id+" action did not finish");
                Assert.That(events,Is.EqualTo(1));Assert.That(hpChanges,Is.EqualTo(1));Assert.That(mpChanges,Is.EqualTo(1));Assert.That(numberSeen,Is.True);
                Assert.That(director.Session.Units.Select(u=>u.CurrentHP),Is.EqualTo(copies.Select(u=>u.CurrentHP)),id+" independent resolver HP");
                Assert.That(director.Session.Units.Select(u=>u.CurrentMP),Is.EqualTo(copies.Select(u=>u.CurrentMP)),id+" independent resolver MP");
                Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(expected.RandomDraws),id+" extra combat RNG consumption");
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random),id+" presentation consumed global RNG");
                Assert.That(director.Session.Units.Select(u=>u.Position),Is.EqualTo(positions));
                Assert.That(director.Session.Units.Count(u=>u.Team==Team.Player),Is.EqualTo(6));
                Assert.That(director.Board.GetComponentsInChildren<CharacterMotion>().Count(m=>m.Owner?.Team==Team.Player),Is.EqualTo(6));
                Assert.That(director.Board.GetComponentsInChildren<CombatEffect>(),Is.Empty);
                Assert.That(director.Board.GetComponentsInChildren<ImpactPool>(true).All(p=>p.ActiveCount==0),Is.True);
                int damage=health[targetIndex]-target.CurrentHP;
                if(!reduced)normalDamage[id]=damage;else Assert.That(damage,Is.EqualTo(normalDamage[id]));
            }
            Assert.That(catalogSkills.Select(JsonUtility.ToJson),Is.EqualTo(content),"Actual presentation must not mutate other content assets");
        }

        [UnityTest] public IEnumerator OriginalTrioMeshesDifferReusePoolAndReleaseOwnedResourcesWithoutSimulationChanges()
        {
            director.BeginBattle();yield return null;
            var holder=new GameObject("Original skill trio effect fixture");var pool=holder.AddComponent<ImpactPool>();
            var health=director.Session.Units.Select(u=>u.CurrentHP).ToArray();var positions=director.Session.Units.Select(u=>u.Position).ToArray();
            string random=JsonUtility.ToJson(Random.state);float timeScale=Time.timeScale;
            var meshes=new List<Mesh>();var materials=new List<Material>();ImpactParticles first=null;
            try
            {
                foreach(bool reduced in new[]{false,true})
                {
                    var shapes=new List<string>();
                    foreach(string id in OriginalTrioIds)
                    {
                        var character=director.Catalog.Characters.Single(c=>c.Id==id.Split('.')[0]);var skill=character.Skills.Single(s=>s.Id==id);
                        var signature=SkillSignatures.Resolve(skill);
                        pool.Play(director.Board.BattleCamera,Vector3.left*2,Vector3.zero,skill.Element,CombatEffect.Resolve(character,skill),CombatFeedback.Strike,1,false,reduced,skill.Presentation.Pattern,false,signature);
                        yield return new WaitForSeconds(.22f);
                        var effect=pool.GetComponentsInChildren<ImpactParticles>().Single();
                        Assert.That(effect.Signature,Is.EqualTo(signature));
                        if(first==null)first=effect;else Assert.That(effect,Is.SameAs(first),"Sequential signatures must reuse their expired slot");
                        shapes.Add(OriginalTrioGeometry(effect));
                        yield return new WaitForSeconds(ImpactParticles.Duration+.12f);Assert.That(pool.ActiveCount,Is.Zero,id+" did not expire");
                    }
                    Assert.That(shapes.Distinct().Count(),Is.EqualTo(3),"The three signature impacts must not share an identical mesh silhouette");
                }
                // A reused slot must also return to a legacy non-signature skill cleanly.
                pool.Play(director.Board.BattleCamera,Vector3.left,Vector3.zero,Element.Ice,CombatVisualStyle.Sacred,CombatFeedback.Strike,1,false,false,SkillVisualPattern.Ice);
                var legacy=pool.GetComponentsInChildren<ImpactParticles>().Single();Assert.That(legacy,Is.SameAs(first));Assert.That(legacy.Signature,Is.EqualTo(SkillSignature.None));OriginalTrioGeometry(legacy);
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));Assert.That(Time.timeScale,Is.EqualTo(timeScale));
                Assert.That(director.Session.Units.Select(u=>u.CurrentHP),Is.EqualTo(health));Assert.That(director.Session.Units.Select(u=>u.Position),Is.EqualTo(positions));
                meshes.AddRange(pool.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh));
                materials.AddRange(pool.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).Distinct());
            }
            finally{Object.Destroy(holder);}
            yield return null;yield return null;
            Assert.That(first==null,Is.True);Assert.That(meshes.All(m=>m==null),Is.True);Assert.That(materials.All(m=>m==null),Is.True);
        }

        static string OriginalTrioGeometry(ImpactParticles effect)
        {
            var values=new List<string>();int count=0;
            foreach(var filter in effect.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;count+=mesh.vertexCount;
                foreach(var vertex in mesh.vertices)Assert.That(float.IsNaN(vertex.x)||float.IsNaN(vertex.y)||float.IsNaN(vertex.z)||float.IsInfinity(vertex.x)||float.IsInfinity(vertex.y)||float.IsInfinity(vertex.z),Is.False);
                var bounds=mesh.bounds;
                values.Add(mesh.vertexCount+":"+Mathf.RoundToInt(bounds.size.x*100)+":"+Mathf.RoundToInt(bounds.size.y*100)+":"+Mathf.RoundToInt(bounds.size.z*100));
            }
            Assert.That(count,Is.GreaterThan(0));return string.Join("|",values);
        }
        void OriginalTrioArrange(UnitRuntime actor,UnitRuntime target)
        {
            var grid=director.Session.Grid;
            var a=grid.Tiles.Values.Where(t=>t.Walkable).OrderBy(t=>GridMap.Distance(t.Coordinate,new Vector2Int(5,5))).First(t=>
                grid[t.Coordinate+Vector2Int.right]?.Walkable==true&&grid[t.Coordinate+Vector2Int.right*2]?.Walkable==true&&
                grid[t.Coordinate+Vector2Int.right].Height==t.Height&&grid[t.Coordinate+Vector2Int.right*2].Height==t.Height).Coordinate;
            var b=a+Vector2Int.right*2;foreach(var tile in grid.Tiles.Values)tile.Occupant=null;
            grid.Place(actor,a);grid.Place(target,b);
            var rest=director.Session.Units.Where(u=>u!=actor&&u!=target).OrderBy(u=>u.Team==Team.Player?0:1);
            var cells=grid.Tiles.Values.Where(t=>t.Walkable&&t.Coordinate!=a&&t.Coordinate!=b&&t.Coordinate!=a+Vector2Int.right).OrderBy(t=>GridMap.Distance(t.Coordinate,a)).ToList();
            foreach(var unit in rest){var tile=unit.Team==Team.Player?cells.First():cells.Last();cells.Remove(tile);Assert.That(grid.Place(unit,tile.Coordinate),Is.True);}
        }
        SkillResolver OriginalTrioReference(out UnitRuntime[] units)
        {
            var source=director.Session;var grid=new GridMap();
            foreach(var tile in source.Grid.Tiles.Values)grid.Tiles.Add(tile.Coordinate,new GridTile{Coordinate=tile.Coordinate,Height=tile.Height,MovementCost=tile.MovementCost,Walkable=tile.Walkable,Terrain=tile.Terrain});
            units=source.Units.Select(u=>new UnitRuntime(u.Data,u.Team,u.Rules,u.Level)).ToArray();
            for(int i=0;i<units.Length;i++)new CheckpointUnit(source.Units[i]).Apply(units[i],grid,director.Catalog);
            foreach(var tile in source.Grid.Tiles.Values)grid[tile.Coordinate].Occupant=tile.Occupant==null?null:units[source.Units.IndexOf(tile.Occupant)];
            var resolver=new SkillResolver(grid,units,source.Rules,tacticalCombat:source.Resolver.TacticalCombat,reactions:source.Resolver.ReactionsEnabled,actionExperience:source.Resolver.ActionExperienceEnabled);
            resolver.CrossoverState=source.Resolver.CrossoverState?.Copy();resolver.RestoreRandom(source.Resolver.RandomDraws);return resolver;
        }
    }
}
