using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Core campaign continuity audit, not a visual/story playthrough or a proof of human difficulty.
    // Uses the shipped planners and commands. Never changes combatant HP, levels or results in battle.
    public static class CampaignProgressionAudit
    {
        const string CatalogPath="Assets/TalesTactics/Content/BattleCatalog.asset";
        const int ActionLimit=500;
        const double StageSecondsLimit=600;
        static readonly string[] Preferred={"cless","mint","alphen","shionne","jade","kisara","farah","tear","velvet","natalia"};
        static Job active;
        public static bool Running=>active!=null;

        // seed controls the separate reward drop stream. BattleSession retains its production resolver seed 17.
        public static string Run(string output,string policy="natural",int seed=17)
        {
            if(Running)throw new InvalidOperationException("A campaign audit is already running.");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before running the core campaign audit.");
            if(policy!="natural"&&policy!="catchUp"&&policy!="rotatingCatchUp")throw new ArgumentException("Unknown audit policy: "+policy);
            string root=Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            string user=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(user,StringComparison.OrdinalIgnoreCase)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The player's save directory cannot be used for an audit.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Use a new, empty audit output directory.");
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>(CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Campaign catalog is missing.");
            Directory.CreateDirectory(root);
            active=new Job(root,policy,seed,catalog);
            active.Write();
            EditorApplication.update+=Tick;
            AssemblyReloadEvents.beforeAssemblyReload+=CancelBeforeReload;
            return root;
        }

        public static void Cancel()
        {
            if(active==null)return;
            active.Stop("Cancelled by caller; incomplete stages are not victories.");
            Detach();
        }
        static void CancelBeforeReload()=>Cancel();
        static void Detach(){EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=CancelBeforeReload;active=null;}
        static void Tick()
        {
            if(active==null){Detach();return;}
            try{if(active.Step())Detach();}
            catch(Exception e){active.Stop("Audit exception: "+e);Debug.LogException(e);Detach();}
        }

        [Serializable] public sealed class CharacterRow
        {
            public string id,name;
            public int beforeTrainingLevel,beforeTrainingEXP,entryLevel,entryEXP,battleLevel,battleEXP,actionEXP,exitLevel,exitEXP;
            public int trainingLevels,entrySkillCount,exitSkillCount,hpRemaining,maxHP;
            public bool alive,promoted;
            public string[] entryEquipment,exitEquipment,learnedSkills;
        }
        [Serializable] public sealed class ItemRow {public string id;public int count;}
        [Serializable] public sealed class ShopRow {public string id,unavailable;public int price;public bool unlocked,affordable;}
        [Serializable] public sealed class SkillUse {public string id,actor;public int count;}
        [Serializable] public sealed class EnemyRow {public string id;public int level,hp,maxHP,phase;public bool reserve;}
        [Serializable] public sealed class StageRow
        {
            public int episode,stage,recommendedLevel,enemyLevel,actions,playerPhases,playerMoves,enemyMoves,playerSkills,enemySkills,survivors;
            public int goldBefore,goldAfter,missionEXP,goldReward,rewardRoll=-1,nodes,segment,randomDraws,saveRoundTrips;
            public string title,result="NotStarted",stopReason,objective,guaranteedItem,bonusItem;
            public bool rewardApplied,saveReloadMatches;
            public double elapsedSeconds;
            public CharacterRow[] party;
            public ItemRow[] inventoryBefore,inventoryAfter;
            public ShopRow[] shop;
            public EnemyRow[] remainingEnemies;
            public SkillUse[] skills;
        }
        [Serializable] public sealed class Report
        {
            public string startedUtc,finishedUtc,policy,output,stopReason,status="Running";
            public int rewardSeed,combatSeed=17,attemptedStages,completedStages,saveRoundTrips,finalGold;
            public bool complete,saveReloadMatches=true,coreCampaignOnly=true,storyPresentationChecked=false,purchases=false;
            public string difficulty="Standard";
            public bool teamTurns=true,useCT=false,fixedSpeedOrder=false,utilityEnemyAI=false,reactions=false,autoTiming=false,missionEvents=false;
            public bool tacticalCombat=true,actionExperience=true,expandedEncounters=true;
            public string equipmentPolicy="Equip compatible owned items by a fixed stat score; no purchases, no selling, no personal-scenario rewards.";
            public string promotionPolicy="Use CampaignPromotion.TrySave when its real story and level requirements are satisfied; leave growth paths and traits at defaults.";
            public string plannerPolicy="Existing UtilityPlanner for allies and EnemyPlanner for enemies; one planned action per turn; existing legal node/exit approach after active enemies are gone.";
            public string trainingPolicy;
            public string limitations="Not human playtesting. Does not exercise dialogue/UI, follow-up timing inputs, deliberate boss telegraph dodging, optimal gear choice, optional scenarios, purchases or retries. A planner defeat/limit is an audit observation, not proof that players cannot clear the stage. Battle-start profile initialization mirrors BattleDirector.BeginBattle; there is no in-battle recovery or stat injection.";
            public StageRow[] stages;
        }

        sealed class Job
        {
            readonly BattleCatalog catalog;
            readonly string root;
            readonly CampaignFile file;
            readonly System.Random drops;
            readonly UtilityPlanner allies=new UtilityPlanner();
            readonly EnemyPlanner enemies=new EnemyPlanner();
            readonly List<StageRow> rows=new List<StageRow>();
            readonly Dictionary<string,int> appearances=new Dictionary<string,int>();
            readonly Dictionary<string,SkillUse> usedSkills=new Dictionary<string,SkillUse>();
            readonly Report report;
            CampaignSave save;
            BattleSession battle;
            StageRow row;
            double began;
            int nextStage=CrossoverCampaign.FirstStage;
            int roundTripsAtStart;

            public Job(string output,string policy,int seed,BattleCatalog data)
            {
                root=output;catalog=data;drops=new System.Random(seed);
                file=new CampaignFile(Path.Combine(root,"isolated-save","campaign.json"));
                save=file.Load();
                report=new Report{startedUtc=DateTime.UtcNow.ToString("o"),policy=policy,output=root,rewardSeed=seed,
                    trainingPolicy=policy=="natural"?"No catch-up training.":"Before each stage, selected allies below the real entry recommendation use TacticalDevelopment.CatchUp; EXP reset is recorded."};
                if(!file.CanSave||save.StoryProgress.Count!=0||save.Characters.Count!=0)throw new InvalidOperationException("Audit did not start from a fresh isolated campaign.");
                PersistAndReload();
            }

            public bool Step()
            {
                if(report.status!="Running")return true;
                if(battle==null){BeginStage();Write();return false;}
                if(battle.Result!=BattleResult.Ongoing){FinishStage();return report.status!="Running";}
                if(row.actions>=ActionLimit||EditorApplication.timeSinceStartup-began>StageSecondsLimit)
                {
                    Stop(row.actions>=ActionLimit?"Planner action limit reached (500); no forced outcome.":"Stage wall-time limit reached (600s); no forced outcome.");return true;
                }
                battle.Advance();
                var actor=battle.Active;
                if(actor==null){Stop("Scheduler returned no active unit while battle remained ongoing.");return true;}
                row.actions++;
                if(actor.Team==Team.Player)row.playerPhases=Math.Max(row.playerPhases,((TeamTurnScheduler)battle.Scheduler).Round);
                if(battle.Result==BattleResult.Ongoing&&!actor.Has(StatusKind.Stun)&&!actor.Has(StatusKind.Sleep))
                {
                    var plan=actor.Team==Team.Enemy?enemies.Plan(battle,actor):allies.Plan(battle,actor);
                    plan=ObjectivePlan(actor,plan);
                    EnemyTactics.Commit(actor,plan,battle);
                    if(plan.Destination!=actor.Position&&battle.Move(plan.Destination))
                    {if(actor.Team==Team.Player)row.playerMoves++;else row.enemyMoves++;}
                    if(battle.Result==BattleResult.Ongoing&&plan.Skill!=null&&(plan.Aim.HasValue||plan.Target!=null))
                    {
                        var aim=plan.Aim??plan.Target.Position;
                        var oldFacing=actor.Facing;
                        if(aim!=actor.Position)actor.Facing=SkillResolver.Toward(actor.Position,aim);
                        if(battle.Resolver.Execute(actor,plan.Skill,aim,out _))
                        {
                            if(actor.Team==Team.Player)row.playerSkills++;else row.enemySkills++;
                            string key=actor.Data.Id+"/"+plan.Skill.Id;
                            if(!usedSkills.TryGetValue(key,out var use)){use=new SkillUse{id=plan.Skill.Id,actor=actor.Data.Id};usedSkills.Add(key,use);}use.count++;
                        }
                        else if(actor.Team==Team.Player)actor.Facing=oldFacing;
                    }
                    else if(battle.Result==BattleResult.Ongoing&&plan.Guard&&!actor.Acted){actor.Acted=true;actor.AddStatus(StatusKind.Guard,2);}
                }
                // The Director enters BattleEndState immediately after a decisive move/skill.
                // Ending a finished turn here would incorrectly apply another hazard/status tick.
                if(battle.Result==BattleResult.Ongoing)battle.EndTurn();
                UpdateRow();
                if(row.actions%10==0)Write();
                if(battle.Result!=BattleResult.Ongoing)FinishStage();
                return report.status!="Running";
            }

            void BeginStage()
            {
                if(!CampaignStages.Unlocked(save,nextStage))throw new InvalidOperationException("Next main stage remained locked: "+nextStage);
                int episode=CrossoverCampaign.Episode(nextStage).Number;
                var joined=Preferred.Where(id=>CrossoverCampaign.Joined(id,episode)).Select(id=>catalog.Characters.First(c=>c.Id==id)).ToArray();
                var selected=report.policy=="rotatingCatchUp"?joined.OrderBy(c=>appearances.TryGetValue(c.Id,out int n)?n:0).ThenBy(c=>Array.IndexOf(Preferred,c.Id)).Take(catalog.Rules.MaxDeployment).ToArray():joined.Take(catalog.Rules.MaxDeployment).ToArray();
                row=new StageRow{stage=nextStage,episode=episode,title=CampaignStages.Title(nextStage),recommendedLevel=CampaignStages.Get(nextStage).EntryLevel,enemyLevel=CampaignStages.EnemyLevel(nextStage),goldBefore=save.Gold,inventoryBefore=Inventory(),party=selected.Select(c=>new CharacterRow{id=c.Id,name=c.DisplayName,beforeTrainingLevel=save.Get(c.Id).Level,beforeTrainingEXP=save.Get(c.Id).EXP}).ToArray()};
                rows.Add(row);roundTripsAtStart=report.saveRoundTrips;
                foreach(var character in selected)
                {
                    if(report.policy!="natural"&&save.Get(character.Id).Level<row.recommendedLevel&&!TacticalDevelopment.CatchUp(save,character,nextStage,file.Save))throw new InvalidOperationException("Catch-up save failed for "+character.Id);
                    if(CampaignPromotion.Unavailable(save.Get(character.Id),character,save.StoryProgress)==null&&!CampaignPromotion.TrySave(save,character,file.Save))throw new InvalidOperationException("Promotion save failed for "+character.Id);
                    EquipOwned(character);
                    appearances[character.Id]=(appearances.TryGetValue(character.Id,out int count)?count:0)+1;
                }
                save.SelectedStage=nextStage;save.Deployment=selected.Select(c=>Array.IndexOf(catalog.Characters,c)).ToArray();
                PersistAndReload();
                battle=new BattleSession(catalog,save.Deployment,1,row.enemyLevel,nextStage,useCT:report.useCT,utilityAI:report.utilityEnemyAI,teamTurns:report.teamTurns,phaseSurvival:true,tacticalCombat:report.tacticalCombat,bossEncounters:true,difficulty:BattleDifficulty.Standard,missionEvents:report.missionEvents,reactions:report.reactions,actionExperience:report.actionExperience,expandedEncounters:report.expandedEncounters);
                // Same saved-profile initialization as BattleDirector.BeginBattle, before the first turn.
                foreach(var unit in battle.Units.Where(u=>u.Team==Team.Player))
                {
                    var progress=save.Get(unit.Data.Id);unit.EXP=progress.EXP;unit.Level=Mathf.Clamp(progress.Level,1,50);unit.Promoted=progress.Promoted;
                    new EquipmentLoadout(unit.Data,progress,catalog.Equipment,save).Apply(unit);
                    TacticalDevelopment.Apply(unit,save,nextStage);
                    unit.CurrentHP=unit.Stats.HP;unit.CurrentMP=unit.Stats.MP;
                    var entry=row.party.First(p=>p.id==unit.Data.Id);
                    entry.entryLevel=progress.Level;entry.entryEXP=progress.EXP;entry.trainingLevels=entry.entryLevel-entry.beforeTrainingLevel;
                    entry.entrySkillCount=unit.Data.Skills.Count(unit.Unlocked);entry.entryEquipment=(string[])progress.Equipment.Clone();
                }
                if(battle.Crossover!=null)battle.Crossover.State.Choice=0;
                battle.Opening=BattleOpening.Capture(battle,catalog);
                usedSkills.Clear();began=EditorApplication.timeSinceStartup;row.result="Ongoing";
                row.shop=(catalog.Equipment??Array.Empty<EquipmentData>()).Where(e=>e!=null&&e.BuyPrice>0).Select(e=>new ShopRow{id=e.Id,price=e.BuyPrice,unlocked=CampaignInventory.PurchaseUnlockRequirement(save,e)==null,affordable=save.Gold>=e.BuyPrice,unavailable=CampaignInventory.PurchaseUnavailable(save,e)}).ToArray();
                UpdateRow();
            }

            void EquipOwned(CharacterData character)
            {
                var draft=new EquipmentLoadout(character,save.Get(character.Id),catalog.Equipment,save);
                bool changed=false;
                foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    var best=draft.Options(slot).OrderByDescending(ItemScore).ThenBy(e=>e.Id,StringComparer.Ordinal).FirstOrDefault();
                    var old=draft.Get(slot);
                    if(best!=null&&(old==null||ItemScore(best)>ItemScore(old))&&draft.Equip(slot,best))changed=true;
                }
                if(changed&&!draft.TrySave(save,file.Save))throw new InvalidOperationException("Equipment save failed for "+character.Id);
            }
            static double ItemScore(EquipmentData e){var s=e.Bonus;return s.HP*.15+s.MP*.1+s.STR+s.MAG+s.DEF+s.MDF+s.SPD+s.MOV*8+s.JMP*4;}

            EnemyPlan ObjectivePlan(UnitRuntime actor,EnemyPlan original)
            {
                if(actor.Team!=Team.Player||battle.Crossover==null||battle.Units.Any(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve))return original;
                Vector2Int? goal=null;
                if(battle.Crossover.UsesNodes&&battle.Crossover.State.Nodes!=7)
                    goal=row.episode==13&&battle.Crossover.State.Nodes==0?CrossoverBattle.Nodes[battle.Crossover.State.Choice]:CrossoverBattle.Nodes.Where((p,i)=>(battle.Crossover.State.Nodes&(1<<i))==0).OrderBy(p=>GridMap.Distance(actor.Position,p)).First();
                else if(battle.Crossover.UsesExit)goal=CrossoverBattle.Exit;
                return goal.HasValue?new EnemyPlan{Destination=Approach(battle,actor,goal.Value),Guard=true}:original;
            }

            void FinishStage()
            {
                UpdateRow();
                if(battle.Result!=BattleResult.Victory){Stop("Planner battle ended in "+battle.Result+" at episode "+row.episode+". Saved growth/rewards remain uncommitted.");return;}
                row.rewardRoll=drops.Next(10000);
                var reward=CampaignEconomy.Prepare(save,row.stage,row.rewardRoll,battle.Crossover?.State.Choice??-1);
                row.missionEXP=reward.Experience;row.goldReward=reward.Gold;row.guaranteedItem=CampaignStages.EquipmentReward(row.stage);row.bonusItem=reward.BonusEquipment;
                var party=battle.Units.Where(u=>u.Team==Team.Player).ToArray();
                if(!CampaignStages.TryReward(save,reward,party.Select(u=>u.Data).ToArray(),file.Save,party.ToDictionary(u=>u.Data.Id,u=>u.BattleEXP)))throw new InvalidOperationException("Victory reward failed to save.");
                row.rewardApplied=reward.Applied;
                PersistAndReload();
                foreach(var unit in party)
                {
                    var entry=row.party.First(p=>p.id==unit.Data.Id);var progress=save.Get(unit.Data.Id);
                    int expectedLevel=entry.entryLevel,expectedEXP=entry.entryEXP;
                    ExperienceRules.Advance(ref expectedLevel,ref expectedEXP,unit.BattleEXP+reward.Experience);
                    if(progress.Level!=expectedLevel||progress.EXP!=expectedEXP)throw new InvalidOperationException("Committed growth differs from action EXP plus mission EXP for "+entry.id);
                }
                UpdateRow();
                report.completedStages++;
                Debug.Log("CAMPAIGN_PROGRESSION "+report.policy+" episode="+row.episode+" result="+row.result+" phases="+row.playerPhases+" party="+string.Join(";",row.party.Select(p=>p.id+":"+p.entryLevel+"->"+p.exitLevel))+" gold="+row.goldAfter);
                if(nextStage==CrossoverCampaign.LastStage){report.complete=true;report.status="Completed";report.finishedUtc=DateTime.UtcNow.ToString("o");Write();return;}
                nextStage++;battle=null;Write();
            }

            void PersistAndReload()
            {
                if(!file.Save(save))throw new InvalidOperationException("Isolated campaign persistence failed: "+file.Notice);
                string expected=JsonUtility.ToJson(save);
                var loaded=new CampaignFile(Path.Combine(root,"isolated-save","campaign.json")).Load();
                report.saveRoundTrips++;
                if(JsonUtility.ToJson(loaded)!=expected){report.saveReloadMatches=false;throw new InvalidOperationException("Isolated save/reload changed campaign data.");}
                save=loaded;
            }
            ItemRow[] Inventory()=>save.Inventory.OrderBy(i=>i.Id,StringComparer.Ordinal).Select(i=>new ItemRow{id=i.Id,count=i.Count}).ToArray();
            void UpdateRow()
            {
                if(row==null)return;
                row.goldAfter=save.Gold;row.inventoryAfter=Inventory();row.saveRoundTrips=report.saveRoundTrips-roundTripsAtStart;row.saveReloadMatches=report.saveReloadMatches;
                if(battle==null)return;
                row.result=battle.Result.ToString();row.objective=battle.ObjectiveDescription;row.elapsedSeconds=EditorApplication.timeSinceStartup-began;
                row.survivors=battle.Units.Count(u=>u.Team==Team.Player&&u.Alive);row.randomDraws=battle.Resolver.RandomDraws;row.nodes=battle.Crossover?.State.Nodes??0;row.segment=battle.Crossover?.State.Segment??0;
                foreach(var unit in battle.Units.Where(u=>u.Team==Team.Player))
                {
                    var p=row.party.First(r=>r.id==unit.Data.Id);var progress=save.Get(unit.Data.Id);
                    p.battleLevel=unit.Level;p.battleEXP=unit.EXP;p.actionEXP=unit.BattleEXP;p.alive=unit.Alive;p.hpRemaining=unit.CurrentHP;p.maxHP=unit.Stats.HP;p.exitLevel=progress.Level;p.exitEXP=progress.EXP;p.exitEquipment=(string[])progress.Equipment.Clone();p.promoted=progress.Promoted;
                    p.exitSkillCount=unit.Data.Skills.Count(s=>s!=null&&s.UnlockLevel<=progress.Level);p.learnedSkills=unit.Data.Skills.Where(s=>s!=null&&s.UnlockLevel>p.entryLevel&&s.UnlockLevel<=progress.Level).Select(s=>s.Id).ToArray();
                }
                row.skills=usedSkills.Values.OrderBy(s=>s.actor,StringComparer.Ordinal).ThenBy(s=>s.id,StringComparer.Ordinal).ToArray();
                row.remainingEnemies=battle.Units.Where(u=>u.Team==Team.Enemy&&u.Alive).Select(u=>new EnemyRow{id=u.Data.Id,level=u.Level,hp=u.CurrentHP,maxHP=u.Stats.HP,phase=u.BossPhase,reserve=u.CrossoverReserve}).ToArray();
            }
            public void Stop(string reason)
            {
                report.status="Stopped";report.stopReason=reason;report.finishedUtc=DateTime.UtcNow.ToString("o");
                if(row!=null){row.stopReason=reason;UpdateRow();}
                Write();Debug.LogWarning("CAMPAIGN_PROGRESSION "+report.policy+" stopped: "+reason);
            }
            public void Write()
            {
                report.attemptedStages=rows.Count;report.finalGold=save.Gold;report.stages=rows.ToArray();
                File.WriteAllText(Path.Combine(root,"report.json"),JsonUtility.ToJson(report,true),new UTF8Encoding(false));
                var csv=new StringBuilder("policy,episode,result,actions,playerPhases,survivors,entryLevels,exitLevels,actionEXP,missionEXP,goldBefore,goldReward,goldAfter,guaranteedItem,bonusItem,nodes,rewardApplied,saveReloadMatches,elapsedSeconds,stopReason\n");
                foreach(var r in rows)csv.AppendLine(string.Join(",",new[]{report.policy,r.episode.ToString(),r.result,r.actions.ToString(),r.playerPhases.ToString(),r.survivors.ToString(),string.Join(";",r.party.Select(p=>p.id+":"+p.entryLevel)),string.Join(";",r.party.Select(p=>p.id+":"+p.exitLevel)),string.Join(";",r.party.Select(p=>p.id+":"+p.actionEXP)),r.missionEXP.ToString(),r.goldBefore.ToString(),r.goldReward.ToString(),r.goldAfter.ToString(),r.guaranteedItem,r.bonusItem,r.nodes.ToString(),r.rewardApplied.ToString(),r.saveReloadMatches.ToString(),r.elapsedSeconds.ToString("F2",CultureInfo.InvariantCulture),r.stopReason}.Select(Csv)));
                File.WriteAllText(Path.Combine(root,"stages.csv"),csv.ToString(),new UTF8Encoding(false));
            }
            static string Csv(string s)=>"\""+(s??"").Replace("\"","\"\"")+"\"";
        }

        // Same legal BFS approach as CrossoverBalanceAudit; unreachable goals keep the actor in place.
        static Vector2Int Approach(BattleSession battle,UnitRuntime actor,Vector2Int goal)
        {
            var distance=new Dictionary<Vector2Int,int>{{goal,0}};var queue=new Queue<Vector2Int>();queue.Enqueue(goal);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();foreach(var tile in battle.Grid.Neighbors(p))
                {
                    if(!tile.Walkable||Math.Abs(tile.Height-battle.Grid[p].Height)>actor.Stats.JMP||tile.Occupant!=null&&tile.Occupant!=actor||distance.ContainsKey(tile.Coordinate))continue;
                    distance[tile.Coordinate]=distance[p]+1;queue.Enqueue(tile.Coordinate);
                }
            }
            return battle.Grid.Reachable(actor,out _).Keys.Where(distance.ContainsKey).OrderBy(p=>distance[p]).ThenBy(p=>GridMap.Distance(actor.Position,p)).DefaultIfEmpty(actor.Position).First();
        }
    }
}
