#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // This profile drives production commands. Planning previews restore position/facing;
    // health, gauges, objective progress, rewards and outcomes are never manufactured.
    public sealed partial class CampaignFlowReview
    {
#if UNITY_EDITOR
        public static void BeginAllStagesEditor(string output,float speed=4)
        {
            BeginEditor(output,speed);
            FindAnyObjectByType<CampaignFlowReview>().allStagesProfile=true;
        }
#endif
        CharacterData[] AllStagesMainParty(CharacterData[] joined)
        {
            // Reserve companions must earn their trial skill unlocks through real main battles.
            // Read the linked content requirements; never write a level or unlock flag here.
            var learners=joined.Where(c=>c.Id=="tear"||c.Id=="velvet").Where(c=>
            {
                var required=c.Skills.Where(k=>k!=null&&(c.Id=="tear"?k.Effects.Any(e=>e.Status==StatusKind.Song):k.Effects.Any(e=>e.Kind==EffectKind.Damage&&e.Drain>0)))
                    .Select(k=>k.UnlockLevel).DefaultIfEmpty(1).Min();
                return (battle.Campaign.Characters.Find(p=>p.Id==c.Id)?.Level??1)<required;
            });
            return new[]{"cless","mint"}.Select(id=>joined.FirstOrDefault(c=>c.Id==id)).Where(c=>c!=null)
                .Concat(learners).Concat(Preferred.Select(id=>joined.FirstOrDefault(c=>c.Id==id)).Where(c=>c!=null))
                .Distinct().Take(battle.Catalog.Rules.MaxDeployment).ToArray();
        }
        void ConfigureAllStagesReport()
        {
            report.limits=new[]{
                "Automated public Director commands and rendered uGUI controls, not a human balance/playtime assessment.",
                "Fresh isolated slot; normal production defaults and seed 17. No HP, level, gauge, objective, story-flag or victory injection.",
                "Twenty main battles followed by ten personal dialogue stages and twenty personal battles; one alternating route choice per arc, not both route permutations.",
                "Main deployment prioritizes available Tear/Velvet until their linked trial skills unlock through ordinary experience; no catch-up level assignment. Other profiles keep their original party policy.",
                "A trial-aware deterministic planner reserves opportunities for real healing, guard damage, interruption, drain and Flaming Edge. Passing proves this policy can finish, not optimal strategy.",
                "All fifty stories/results use disk checks and title reload; the process is not restarted mid-story. Every battle start/result and both story openings are captured.",
                "timeScale accelerates presentation; recorded wall/scaled times are automation measures, not human duration. No selling, training, rewind or retry assistance."};
        }
        IEnumerator RunPersonalArcs()
        {
            for(int stage=PersonalScenarios.FirstStage;stage<PersonalScenarios.FirstStage+PersonalScenarios.Count;stage++)
            {
                int tier=PersonalScenarios.Tier(stage),arcIndex=(stage-PersonalScenarios.FirstStage)/3;
                var arc=PersonalScenarios.Arc(stage);
                Require(battle.Session==null&&!battle.StoryActive&&PersonalScenarios.Unlocked(battle.Campaign,stage),"Personal stage unlock follows completed main/previous personal flags: "+stage);
                purchases.Clear();actions.Clear();captures.Clear();unitRows.Clear();observedTurns.Clear();observedIntent.Clear();
                current=new StageRow{stage=stage,tier=tier,owner=arc.Owner,kind=tier==0?"personal-dialogue":"personal-combat",capturePrefix="personal-"+arc.Owner+"-"+(tier+1),goldEntry=battle.Campaign.Gold};rows.Add(current);
                battle.SelectedStage=stage;
                var joined=battle.Catalog.Characters.Where(battle.IsJoined).ToArray();current.joined=joined.Select(c=>c.Id).ToArray();
                var selected=new[]{arc.Owner}.Concat(Preferred).Distinct().Select(id=>joined.FirstOrDefault(c=>c.Id==id)).Where(c=>c!=null).Take(battle.Catalog.Rules.MaxDeployment).ToArray();
                battle.Deployment.Clear();battle.Deployment.AddRange(selected.Select(c=>Array.IndexOf(battle.Catalog.Characters,c)));battle.NormalizeDeployment();
                if(tier>0)
                {
                    PrepareEquipment(selected);
                    foreach(var c in selected)if(CampaignPromotion.Unavailable(battle.Campaign.Get(c.Id),c,battle.Campaign.StoryProgress)==null)Require(CampaignPromotion.TrySave(battle.Campaign,c,battle.PersistCampaign),"Personal eligible promotion saves.");
                }
                Require(battle.SavePreparation(),"Personal preparation saves.");CheckDisk();
                current.party=CampaignRecruitment.BattleDeployment(battle.Campaign,battle.Catalog,stage,battle.Deployment).Select(i=>battle.Catalog.Characters[i].Id).ToArray();
                current.goldSpent=purchases.Sum(p=>p.price);current.goldBeforeReward=battle.Campaign.Gold;
                string[] inventoryBefore=battle.Campaign.Inventory.Select(i=>i.Id+":"+i.Count).ToArray();
                int ownerLevel=battle.Campaign.Get(arc.Owner).Level,ownerEXP=battle.Campaign.Get(arc.Owner).EXP;
                battle.Hud.ShowPersonalScenarios();yield return Settle();Click(PersonalScenarios.Title(stage));yield return Settle();
                Require(battle.StoryActive&&battle.StoryStage==stage&&battle.Session==null,"Personal stage begins through its rendered story button.");
                yield return ReadStory(false);
                if(tier==0)
                {
                    Require(battle.Session==null&&battle.StoryActive&&battle.Campaign.HasPendingAfterStory,"Dialogue-only stage commits then opens its ending without combat.");
                    current.rewardApplied=true;current.goldReward=CampaignEconomy.Gold(stage,false);current.goldAfterReward=battle.Campaign.Gold;
                    current.rewardItems=Array.Empty<string>();current.inventoryAfterReward=battle.Campaign.Inventory.Select(i=>i.Id+":"+i.Count).ToArray();
                    Require(inventoryBefore.SequenceEqual(current.inventoryAfterReward),"Dialogue-only reward does not invent inventory.");
                    ExperienceRules.Advance(ref ownerLevel,ref ownerEXP,CampaignEconomy.Experience(stage,false));
                    Require(battle.Campaign.Get(arc.Owner).Level==ownerLevel&&battle.Campaign.Get(arc.Owner).EXP==ownerEXP,"Dialogue owner receives the authored mission experience.");
                    report.completedDialogueStages++;
                }
                else
                {
                    if(tier==1){Require(battle.Session==null&&!battle.StoryActive,"Personal choice waits after its before-story.");Click(PersonalScenarios.RouteName(stage,arcIndex%2)+"부터");}
                    yield return Settle();Require(battle.Session!=null&&battle.Session.CampaignStage==stage,"Personal story/choice begins its actual battle.");
                    Require(battle.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id).SequenceEqual(current.party)&&current.party.Contains(arc.Owner),"Personal battle deploys its owner and saved party.");
                    AttachBattle();current.status="Fighting";Write();yield return Capture("battle-start");
                    began=Time.realtimeSinceStartupAsDouble;scaledBegan=Time.timeAsDouble;frameBegan=Time.frameCount;measuringBattle=true;
                    yield return Fight();measuringBattle=false;ObserveUnits();
                    current.battleWallSeconds=Time.realtimeSinceStartupAsDouble-began;current.scaledSimulationSeconds=Time.timeAsDouble-scaledBegan;current.measuredFrames=Time.frameCount-frameBegan;
                    Require(battle.Session.Result==BattleResult.Victory&&battle.State is BattleEndState&&!battle.RewardPending,"Personal victory and reward use production completion.");
                    current.rewardApplied=true;current.goldReward=battle.GoldResult;current.goldAfterReward=battle.Campaign.Gold;
                    current.nodes=battle.Session.Crossover.State.Nodes;current.trialProgress=battle.Session.Crossover.State.TrialProgress;current.trialActions=battle.Session.Crossover.State.TrialActions;
                    current.rewardItems=battle.ItemResults.Select(i=>i.Item.Id+":"+i.Count).ToArray();current.inventoryAfterReward=battle.Campaign.Inventory.Select(i=>i.Id+":"+i.Count).ToArray();
                    CheckGrowth();CheckDisk();string fingerprint=RewardFingerprint(battle.Campaign);battle.CompleteBattle();battle.SaveBattleReward();
                    current.duplicateRewardPrevented=fingerprint==RewardFingerprint(battle.Campaign);Require(current.duplicateRewardPrevented,"Repeated personal completion does not duplicate reward.");
                    yield return Capture("battle-result");Click("결과 계속");yield return Settle();
                    report.completedCombatStages++;
                }
                Require(current.goldAfterReward==current.goldBeforeReward+current.goldReward,"Personal gold matches the authored transaction.");
                current.pendingEndingSaved=battle.Campaign.HasPendingAfterStory&&battle.Campaign.PendingAfterStoryStage==stage;Require(current.pendingEndingSaved,"Personal ending is persisted before reading.");CheckDisk();
                yield return ReadStory(true);Require(battle.Session==null&&!battle.StoryActive,"Personal ending returns to preparation.");
                current.endingCleared=!battle.Campaign.HasPendingAfterStory;Require(current.endingCleared,"Personal ending clears after its final line.");
                current.nextStageUnlocked=tier==2?battle.Campaign.StoryProgress.Contains(PersonalScenarios.Flag(stage)):PersonalScenarios.Unlocked(battle.Campaign,stage+1);Require(current.nextStageUnlocked,"Personal progress unlocks the next tier or records the trial.");
                if(tier==2)Require(battle.Campaign.Inventory.Where(i=>i.Id==arc.WeaponId).Sum(i=>i.Count)==1,"Signature weapon is awarded exactly once: "+arc.WeaponId);
                string prior=RewardFingerprint(battle.Campaign);battle.Hud.ShowPersonalScenarios();yield return Settle();Click("완료 · "+PersonalScenarios.Title(stage));yield return Settle();
                Require(battle.StoryActive,"Completed personal story remains replayable.");int replayLines=0;
                while(battle.StoryActive){Require(replayLines++<100,"Replay line bound.");var button=FindButton("다음 대사")??FindButton("이야기 마치기");Require(button!=null,"Replay control exists.");Click(button.name);yield return Settle();}
                current.duplicateRewardPrevented=current.duplicateRewardPrevented||tier==0;
                current.duplicateRewardPrevented&=prior==RewardFingerprint(battle.Campaign);Require(current.duplicateRewardPrevented,"Story replay cannot repeat personal rewards.");
                Require(battle.SavePreparation(),"Replay return-to-main selection saves before the independent reload snapshot.");CheckDisk();yield return ReloadCompletedPersonal();
                current.status="Completed";report.completedStages++;report.completedPersonalStages++;Write();
                Debug.Log("ALL_STAGES personal="+stage+" owner="+arc.Owner+" tier="+tier+" commands="+current.playerCommands);
            }
            report.signatureWeapons=PersonalScenarios.Arcs.Select(a=>a.WeaponId).ToArray();
            Require(report.completedStages==50&&report.completedPersonalStages==30&&report.completedDialogueStages==10&&report.completedCombatStages==40,"All current stages completed with exact combat/dialogue coverage.");
            Require(PersonalScenarios.Arcs.All(a=>battle.Campaign.Inventory.Where(i=>i.Id==a.WeaponId).Sum(i=>i.Count)==1),"All ten signature weapons persist exactly once.");
        }
        IEnumerator ReloadCompletedPersonal()
        {
            string before=PermanentState(battle.Campaign);int selected=battle.SelectedStage;var deployment=battle.Deployment.ToArray();
            current.permanentStateBeforeReloadHash=StateHash(before);File.WriteAllText(Path.Combine(directory,current.capturePrefix+"-before-title-reload.json"),before);Write();
            battle.ReturnToTitle();yield return Settle();Require(battle.Hud.TitleOpen&&battle.LoadTitleGame(0,false),"Personal completion reloads through title.");yield return Settle();CheckDisk();
            string after=PermanentState(battle.Campaign);current.permanentStateAfterReloadHash=StateHash(after);File.WriteAllText(Path.Combine(directory,current.capturePrefix+"-after-title-reload.json"),after);
            current.permanentStateRoundTripPreserved=before==after&&selected==battle.SelectedStage&&deployment.SequenceEqual(battle.Deployment);
            Require(current.permanentStateRoundTripPreserved&&!battle.StoryActive&&!battle.Campaign.HasPendingAfterStory,"Personal title reload preserves all permanent fields, deployment and selected stage without repeating an ending.");current.completedEndingReloaded=true;
        }
        bool TrialPending(BattleSession s)
        {
            if(!PersonalScenarios.Contains(s.CampaignStage)||PersonalScenarios.Tier(s.CampaignStage)!=2)return false;
            var state=s.Crossover.State;switch(PersonalScenarios.Arc(s.CampaignStage).Owner)
            {case "cless":return state.TrialProgress!=7;case "mint":case "shionne":return state.TrialActions<3;case "tear":case "jade":case "velvet":case "alphen":case "kisara":return state.TrialProgress==0;default:return false;}
        }
        EnemyPlan AllStagesPlan(BattleSession s,UnitRuntime u)
        {
            var ordinary=planner.Plan(s,u);
            if(!TrialPending(s))return AllStagesObjectivePlan(s,u,ordinary);
            string owner=PersonalScenarios.Arc(s.CampaignStage).Owner;var state=s.Crossover.State;
            bool healing=owner=="mint"||owner=="shionne"||owner=="cless"&&(state.TrialProgress&2)==0;
            if(healing&&(u.Data.Id==owner||owner=="cless"))
            {
                var heal=FindNaturalAction(s,u,(skill,targets)=>EffectiveHealing(s,u,skill,targets),false,false);
                if(heal!=null)return heal;
            }
            if(owner=="cless"&&(state.TrialProgress&4)==0)return GuardTowardEnemy(s,u);
            if(u.Data.Id==owner)
            {
                EnemyPlan special=null;
                if(owner=="tear")
                {
                    special=FindNaturalAction(s,u,(skill,targets)=>skill.Effects.Any(e=>e.Status==StatusKind.Song)?1000:0);
                    // A low-level owner must earn action EXP rather than guard until the command cap.
                    if(special==null&&!u.Data.Skills.Any(k=>k!=null&&k.Effects.Any(e=>e.Status==StatusKind.Song)&&u.Unlocked(k)))
                        special=FindNaturalAction(s,u,(skill,targets)=>EffectiveHealing(s,u,skill,targets)>0?EffectiveHealing(s,u,skill,targets):
                            skill==u.Data.BasicAttack&&SkillDamage(s,u,skill,targets)>0?200:0);
                }
                if(owner=="jade")special=FindNaturalAction(s,u,(skill,targets)=>skill.Effects.Any(e=>e.Status==StatusKind.Cage||e.Status==StatusKind.Stun||e.Status==StatusKind.Sleep)&&targets.Any(t=>t.Team==Team.Enemy&&t.IntentPhase==1)?1000:0);
                if(owner=="velvet")
                {
                    if(!u.Has(StatusKind.ConsumeClaw))special=FindNaturalAction(s,u,(skill,targets)=>skill.Effects.Any(e=>e.Kind==EffectKind.ConsumeClaw)?1000:0);
                    else if(u.CurrentHP<u.Stats.HP)special=FindNaturalAction(s,u,(skill,targets)=>skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&e.Drain>0)&&targets.Any(t=>t.Team==Team.Enemy)?1000:0,false,false);
                }
                if(owner=="alphen")special=FindNaturalAction(s,u,(skill,targets)=>skill.StartsFlamingChain&&targets.Any(t=>t.Team==Team.Enemy)?1000:0);
                if(special!=null)return special;
                if(owner=="kisara")return GuardTowardEnemy(s,u);
                if(owner=="jade")
                {
                    // Build the real gauge with the weakest available attack while preserving the boss.
                    var build=FindNaturalAction(s,u,(skill,targets)=>skill.GaugeCost==0&&skill.Effects.Any(e=>e.Kind==EffectKind.Damage)?200-SkillDamage(s,u,skill,targets)*.1:0);
                    if(build!=null&&u.SpecialGauge<(u.Data.UltimateSkill?.GaugeCost??100))return build;
                }
            }
            if(owner=="cless"&&(state.TrialProgress&1)==0)
            {
                var attack=FindNaturalAction(s,u,(skill,targets)=>skill.Effects.Any(e=>e.Kind==EffectKind.Damage)?100:0);if(attack!=null)return attack;
            }
            // Other actors keep the encounter alive until the authored mechanic was observed.
            // Missing HP is produced by normal enemy attacks, never a fixture adjustment.
            if(!healing&&u.Data.Id!=owner)
            {
                // Consume Claw already paid real HP. Preserve that injury for Velvet's next drain;
                // emergency healing below 35% HP and revival remain available.
                var reserved=owner=="velvet"?s.Units.FirstOrDefault(a=>a.Team==Team.Player&&a.Data.Id==owner):null;
                var heal=FindNaturalAction(s,u,(skill,targets)=>EffectiveHealing(s,u,skill,targets,reserved),false,false);if(heal!=null)return heal;
            }
            return GuardTowardEnemy(s,u);
        }
        EnemyPlan AllStagesObjectivePlan(BattleSession s,UnitRuntime u,EnemyPlan ordinary)
        {
            bool personal=PersonalScenarios.Contains(s.CampaignStage);
            bool extraction=s.Crossover.UsesExit&&s.Crossover.ObjectiveDevices.All(d=>!d.Alive);
            if(!extraction&&s.Units.Any(e=>e.Team==Team.Enemy&&e.Alive&&!e.CrossoverReserve))return ordinary;
            Vector2Int? goal=null;
            if(s.Crossover.UsesNodes&&!s.Crossover.AllSignalsConnected)
            {
                bool ordered=personal&&(PersonalScenarios.Tier(s.CampaignStage)==1||PersonalScenarios.Arc(s.CampaignStage).Owner=="cless")||current.episode==13;
                int next=ordered?(s.Crossover.State.Nodes==0&&(PersonalScenarios.Tier(s.CampaignStage)==1||current.episode==13)?s.Crossover.State.Choice:Enumerable.Range(0,s.Crossover.SignalPositions.Length).First(i=>(s.Crossover.State.Nodes&(1<<i))==0)):-1;
                goal=next>=0?s.Crossover.SignalPositions[next]:s.Crossover.SignalPositions.Where((p,i)=>(s.Crossover.State.Nodes&(1<<i))==0).OrderBy(p=>GridMap.Distance(u.Position,p)).First();
            }
            else if(s.Crossover.UsesContract)
            {
                if(u.Data.Id=="cless")goal=s.Crossover.ContractPosition;
                else return ordinary.Skill!=null&&ordinary.Skill.Target!=TargetType.Enemy?ordinary:new EnemyPlan{Destination=u.Position,Guard=true};
            }
            else if(s.Crossover.UsesExit)goal=s.Crossover.ExitPosition;
            if(!goal.HasValue)return ordinary;
            var move=MoveToward(s,u,goal.Value);
            // A wounded extraction party may still heal before moving on the next command.
            if(ordinary.Skill!=null&&ordinary.Skill.Effects.Any(e=>e.Kind==EffectKind.Heal||e.Kind==EffectKind.Revive))return ordinary;
            // A blocked bridge still needs normal combat to open it; do not guard forever behind an enemy.
            if(move.Destination==u.Position&&u.Position!=goal.Value)return ordinary;
            return move;
        }
        EnemyPlan GuardTowardEnemy(BattleSession s,UnitRuntime u)
        {
            // Devices cannot attack. Compare every reachable approach instead of repeatedly
            // choosing a geometrically close tile behind a wall or another enemy.
            var approaches=s.Units.Where(e=>e.Team==Team.Enemy&&e.Alive&&!e.CrossoverReserve&&e.Data.Id!="boundary-device")
                .SelectMany(e=>s.Grid.Neighbors(e.Position)).Where(t=>t.Walkable&&(t.Occupant==null||t.Occupant==u))
                .Select(t=>t.Coordinate).Distinct().Select(p=>new{goal=p,distance=WalkingDistances(s,u,p)})
                .Where(a=>a.distance.ContainsKey(u.Position)).OrderBy(a=>a.distance[u.Position]).ThenBy(a=>a.goal.x).ThenBy(a=>a.goal.y).FirstOrDefault();
            return approaches==null?new EnemyPlan{Destination=u.Position,Guard=true}:MoveToward(s,u,approaches.goal);
        }
        static Dictionary<Vector2Int,int> WalkingDistances(BattleSession s,UnitRuntime u,Vector2Int goal)
        {
            var distance=new Dictionary<Vector2Int,int>{{goal,0}};var queue=new Queue<Vector2Int>();queue.Enqueue(goal);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();foreach(var tile in s.Grid.Neighbors(p))
                {
                    // Match GridMap walking: allies may be traversed, but never used as landing cells.
                    if(!tile.Walkable||Math.Abs(tile.Height-s.Grid[p].Height)>u.Stats.JMP||tile.Occupant!=null&&tile.Occupant.Team!=u.Team||distance.ContainsKey(tile.Coordinate))continue;
                    distance[tile.Coordinate]=distance[p]+1;queue.Enqueue(tile.Coordinate);
                }
            }
            return distance;
        }
        static EnemyPlan MoveToward(BattleSession s,UnitRuntime u,Vector2Int goal)
        {
            var distance=WalkingDistances(s,u,goal);
            var reachable=u.Moved?new[]{u.Position}:s.Grid.Reachable(u,out _).Keys.ToArray();
            return new EnemyPlan{Destination=reachable.Where(distance.ContainsKey).OrderBy(p=>distance[p]).ThenBy(p=>GridMap.Distance(u.Position,p)).DefaultIfEmpty(u.Position).First(),Guard=true};
        }
        static double EffectiveHealing(BattleSession s,UnitRuntime u,SkillData skill,UnitRuntime[] targets,UnitRuntime preserveInjury=null)
        {
            double result=0;
            foreach(var effect in skill.Effects)
            foreach(var target in effect.AffectCaster?new[]{u}:targets)
            {
                if(effect.Kind==EffectKind.Heal&&target==preserveInjury&&target.Alive&&target.CurrentHP>target.Stats.HP*.35f&&target.CurrentHP<target.Stats.HP)
                    return 0; // Reject the entire area heal too; scoring other targets must not erase this injury.
                if(effect.Kind==EffectKind.Heal&&target.Team==u.Team&&target.Alive)result+=Math.Min(target.Stats.HP-target.CurrentHP,s.Resolver.Healing(u,effect));
                if(effect.Kind==EffectKind.Revive&&target.Team==u.Team&&!target.Alive&&s.Grid[target.Position].Occupant==null)result+=100;
            }
            return result>0?1000+result:0;
        }
        static double SkillDamage(BattleSession s,UnitRuntime u,SkillData skill,UnitRuntime[] targets)
            =>skill.Effects.Where(e=>e.Kind==EffectKind.Damage).Sum(e=>targets.Where(t=>t.Team!=u.Team).Sum(t=>s.Resolver.DamagePreview(u,t,e,skill)));
        EnemyPlan FindNaturalAction(BattleSession s,UnitRuntime u,Func<SkillData,UnitRuntime[],double> value,bool followup=false,bool preserveBoss=true)
        {
            var origin=u.Position;var facing=u.Facing;EnemyPlan best=null;double score=0;
            var destinations=followup||u.Moved?new[]{origin}:s.Grid.Reachable(u,out _).Keys.OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();
            var skills=new[]{u.Data.BasicAttack,u.Data.UltimateSkill}.Concat(u.Data.Skills).Concat(u.TacticalSkills).Where(k=>k!=null&&(!followup||k.Gate==SkillGate.FlamingEdge)&&s.Resolver.CanUse(u,k,followup)==null).Distinct().ToArray();
            try
            {
                foreach(var position in destinations)
                {
                    if(!s.Grid.Place(u,position))continue;
                    foreach(var skill in skills)foreach(var aim in s.Grid.Tiles.Keys)
                    {
                        if(!s.Resolver.InRange(u,skill,aim))continue;var targets=s.Resolver.Targets(u,skill,aim).ToArray();if(targets.Length==0)continue;
                        u.Facing=aim==position?facing:SkillResolver.Toward(position,aim);
                        if(preserveBoss&&targets.Contains(s.Crossover.Boss)&&skill.Effects.Where(e=>e.Kind==EffectKind.Damage).Sum(e=>s.Resolver.DamagePreview(u,s.Crossover.Boss,e,skill))>=s.Crossover.Boss.CurrentHP*.75)continue;
                        double candidate=value(skill,targets);if(candidate<=0)continue;
                        candidate-=s.Resolver.MPCost(u,skill)*.05+GridMap.Distance(origin,position)*.01;
                        if(candidate>score){score=candidate;best=new EnemyPlan{Destination=position,Skill=skill,Aim=aim,Target=targets[0]};}
                    }
                    u.Facing=facing;
                }
            }
            finally{s.Grid.Place(u,origin);u.Facing=facing;}
            return best;
        }
        IEnumerator ExecuteNaturalFollowup(BattleSession s,UnitRuntime actor)
        {
            var plan=FindNaturalAction(s,actor,(skill,targets)=>SkillDamage(s,actor,skill,targets)+100,true,false);
            if(plan==null)yield break;
            double start=Time.realtimeSinceStartupAsDouble;yield return battle.Execute(plan.Skill,plan.Aim??plan.Target.Position,true);current.playerSkillWallSeconds+=Time.realtimeSinceStartupAsDouble-start;
        }
    }
}
#endif