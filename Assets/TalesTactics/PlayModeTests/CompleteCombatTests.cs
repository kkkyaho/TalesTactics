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
        [UnityTest] public IEnumerator AllCatalogSkillsExecuteWithMatchingPreviewAndFiniteCosts()
        {
            var report=new List<string>{"id,level,mpCost,hpCost,previewDamage,actualDamage"};
            int count=0;
            foreach(var owner in director.Catalog.Characters.Concat(new[]{director.Catalog.Enemy}))
            foreach(var skill in new[]{owner.BasicAttack}.Concat(owner.Skills).Concat(new[]{owner.UltimateSkill}).Where(s=>s!=null).Distinct())
            {
                foreach(int level in new[]{Math.Max(1,skill.UnlockLevel),50}.Distinct())
                {
                    var map=new GridMap();
                    for(int x=0;x<16;x++)for(int y=0;y<16;y++){var p=new Vector2Int(x,y);map.Tiles.Add(p,new GridTile{Coordinate=p});}
                    var caster=new UnitRuntime(owner,Team.Player,director.Catalog.Rules,level);
                    caster.SpecialGauge=100;caster.FlamingChain=true;caster.GuardIgnition=true;caster.ClawAttacks=1;
                    caster.AddStatus(StatusKind.ConsumeClaw,3);
                    map.Place(caster,new Vector2Int(6,6));
                    var target=new UnitRuntime(director.Catalog.Enemy,skill.Target==TargetType.Enemy?Team.Enemy:Team.Player,director.Catalog.Rules,50);
                    if(skill.Target==TargetType.FallenAlly)target.CurrentHP=0;
                    else if(skill.Target==TargetType.Ally)target.CurrentHP/=2;
                    map.Place(target,new Vector2Int(6+Math.Max(1,skill.MinRange),6));
                    var resolver=new SkillResolver(map,new[]{caster,target},director.Catalog.Rules);
                    var aim=skill.Range==0||skill.Target==TargetType.Self?caster.Position:target.Position;
                    var recipient=skill.Target==TargetType.Self?caster:target;
                    bool followup=skill.Gate==SkillGate.FarahTiming;
                    Assert.That(resolver.CanUse(caster,skill,followup),Is.Null,skill.Id+" Lv"+level);
                    Assert.That(resolver.InRange(caster,skill,aim),Is.True,skill.Id);
                    Assert.That(resolver.Targets(caster,skill,aim).Contains(recipient),Is.True,skill.Id);
                    var damage=skill.Effects.FirstOrDefault(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster);
                    int preview=damage==null?0:resolver.DamagePreview(caster,recipient,damage,skill);
                    int hp=recipient.CurrentHP,mp=caster.CurrentMP,hpCost=resolver.HPCost(caster,skill);
                    Assert.That(resolver.Execute(caster,skill,aim,out var message,followup),Is.True,skill.Id+": "+message);
                    Assert.That(caster.CurrentMP,Is.EqualTo(mp-skill.MPCost),skill.Id);
                    if(damage!=null&&damage.Chance>=1)
                        Assert.That(hp-recipient.CurrentHP,Is.EqualTo(Math.Min(hp,preview)),skill.Id);
                    Assert.That(caster.CurrentHP>0,Is.True,skill.Id);
                    report.Add($"{skill.Id},{level},{skill.MPCost},{hpCost},{preview},{hp-recipient.CurrentHP}");
                }
                count++;
            }
            Assert.That(count,Is.EqualTo(89));
            File.WriteAllLines(Path.Combine(Application.dataPath,"../Docs/combat-skill-matrix.csv"),report);
            yield return null;
        }

        [UnityTest] public IEnumerator CampaignBalanceScenariosFinishAndReportCasualties()
        {
            var report=new List<string>{"stage,party,level,result,turns,survivors,totalHP"};
            int[][] parties={new[]{0,1,3},new[]{0,2,3,6,7,9},new[]{1,4,5,6,8,9}};
            for(int stage=0;stage<CampaignStages.Count;stage++)foreach(var party in parties)
            {
                int level=stage+1;
                var session=new BattleSession(director.Catalog,party,level,CampaignStages.EnemyLevel(stage),stage);
                var ai=new EnemyPlanner();int turns=0;
                while(session.Result==BattleResult.Ongoing&&turns++<400)
                {
                    session.Advance();var u=session.Active;var plan=ai.Plan(session,u);
                    if(plan.Destination!=u.Position)session.Move(plan.Destination);
                    if(plan.Target!=null)session.Resolver.Execute(u,plan.Skill,plan.Target.Position,out _);
                }
                report.Add($"{stage+1},{string.Join("-",party)},{level},{session.Result},{turns},{session.Units.Count(u=>u.Team==Team.Player&&u.Alive)},{session.Units.Where(u=>u.Team==Team.Player).Sum(u=>u.CurrentHP)}");
                Assert.That(session.Result!=BattleResult.Ongoing,Is.True,"Battle stuck on stage "+stage);
                // Understaffed parties are challenge scenarios; their outcome is recorded,
                // not a promise that healing characters can win using basic attacks alone.
                if(party.Length==6)
                    Assert.That(session.Result,Is.EqualTo(BattleResult.Victory),"Full-party baseline should win: "+string.Join("-",party));
            }
            File.WriteAllLines(Path.Combine(Application.dataPath,"../Docs/combat-party-matrix.csv"),report);
            yield return null;
        }
    }
}
