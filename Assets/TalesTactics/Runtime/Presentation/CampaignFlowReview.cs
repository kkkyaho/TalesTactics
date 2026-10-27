#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TalesTactics
{
    // A rendered automated playthrough of public Director/UI paths, not a human playtest.
    public sealed class CampaignFlowReview:MonoBehaviour
    {
        [Serializable] public sealed class UnitRow
        {
            public string id,team;public string[] equipment;public int entryLevel,entryEXP,exitLevel,exitEXP,battleExitLevel,battleExitEXP,actionEXP,entryMP,minMP,exitMP,maxMP,hp,maxHP,turns,skills,mpSpent,preparations,incapacitatedTurns,bossPhase;public bool alive;
        }
        [Serializable] public sealed class PurchaseRow {public string character,item;public int price,goldBefore,goldAfter;}
        [Serializable] public sealed class ActionRow
        {
            public int sequence,round,turn,mpBefore,mpAfter,hpAfter,x,y,bossPhase,intentPhase;public string actor,team,kind,skill,outcome;public float nominalWindupRecoverySeconds;
        }
        [Serializable] public sealed class StageRow
        {
            public int episode,stage,beforeLines,afterLines,pendingEndingCursorChecks,playerCommands,playerPhases,enemyTurns,enemySkills,bossPreparations,bossSkills,nodes,segment,finalBossPhase,saveReloadChecks,goldEntry,goldSpent,goldBeforeReward,goldReward,goldAfterReward;
            public string status="Preparing",stopReason,permanentStateBeforeReloadHash,permanentStateAfterReloadHash;public bool rewardApplied,pendingEndingSaved,completedEndingReloaded,endingCleared,nextStageUnlocked,duplicateRewardPrevented,permanentStateRoundTripPreserved;
            public double battleWallSeconds,scaledSimulationSeconds,nominalSkillAnimationSecondsProxy;public int measuredFrames;
            public string[] joined,party,beforeSpeakers,afterSpeakers,rewardItems,inventoryAfterReward;public UnitRow[] units;public PurchaseRow[] purchases;public ActionRow[] actions;public string[] captures;
        }
        [Serializable] public sealed class Report
        {
            public string status="Running",startedUtc,finishedUtc,unityVersion,output,policy="naturalGrowthPurchases",difficulty="Standard",stopReason;
            public int completedStages,checks,saveReloadChecks,enemySpeedMode;public float timeScale=4;public bool complete,userFilesUnchanged,teamTurns=true,reactions=false,autoTiming=false,missionEvents=false,purchases=true,ct,utilityEnemyAI,skipEnemyAnimations;
            public string purchasePolicy="Before each battle, use available owned upgrades then buy higher fixed-stat-score compatible items with current gold; highest improvement per gold first, no selling, no catch-up training. Real CampaignInventory.TryBuy and EquipmentLoadout.TrySave transactions.";
            public string[] limits={"Automated commands, not human playtime or a manual campaign completion.","Player movement/skills/facing use public BattleDirector paths; enemy turns use its normal coroutine. Story and result controls use rendered uGUI buttons.","Fresh isolated save; reward seed 17, production combat seed 17. No level/HP/outcome injection, no forced victories, no content regeneration.","timeScale accelerates presentation only. Wall seconds include automation/CPU/frame waits; scaled seconds and summed windup/recovery are separate proxies, not a human-duration estimate.","Fixed preferred party and one-action UtilityPlanner cannot establish optimal strategy, economy or balance. Dialogue is rendered and advanced automatically, not read by a person.","Pending after-story cursor is checked by disk reload after each advanced line; actual title reload is performed after each completed episode. This run does not restart a process mid-dialogue.","No optional personal scenarios, selling, manual timing success, free catch-up, rewinds or retry assistance."};
            public StageRow[] stages;public string[] errors;
        }
        static readonly string[] Preferred={"cless","mint","alphen","shionne","jade","kisara","farah","tear","velvet","natalia"};
        readonly List<StageRow> rows=new List<StageRow>();readonly List<string> errors=new List<string>();
        readonly List<PurchaseRow> purchases=new List<PurchaseRow>();readonly List<ActionRow> actions=new List<ActionRow>();readonly List<string> captures=new List<string>();
        readonly Dictionary<UnitRuntime,UnitRow> unitRows=new Dictionary<UnitRuntime,UnitRow>();
        readonly Dictionary<UnitRuntime,int> observedTurns=new Dictionary<UnitRuntime,int>();readonly Dictionary<UnitRuntime,string> observedIntent=new Dictionary<UnitRuntime,string>();
        readonly UtilityPlanner planner=new UtilityPlanner();
        BattleDirector battle;Report report;StageRow current;string directory,storage;float requestedTimeScale=4,oldTimeScale;bool oldBackground,finished;string[] userBefore;double began,scaledBegan;int frameBegan;

#if UNITY_EDITOR
        public static void BeginEditor(string output,float speed=4)
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode before starting the campaign flow review.");
            if(FindAnyObjectByType<CampaignFlowReview>()!=null)throw new InvalidOperationException("A campaign flow review already exists.");
            ValidateOutput(output);var review=new GameObject("Campaign flow review").AddComponent<CampaignFlowReview>();review.directory=Path.GetFullPath(output);review.requestedTimeScale=Mathf.Clamp(speed,1,8);
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--campaign-flow-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),"CampaignFlowReviews",args[i+1]);ValidateOutput(output);
            new GameObject("Campaign flow review").AddComponent<CampaignFlowReview>().directory=output;
        }
        static void ValidateOutput(string path)
        {
            string root=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(user,StringComparison.OrdinalIgnoreCase)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("User storage cannot be a review output.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Use a new empty review output.");
        }
        IEnumerator Start()
        {
            oldTimeScale=Time.timeScale;oldBackground=Application.runInBackground;Application.runInBackground=true;
            Directory.CreateDirectory(directory);userBefore=SnapshotFiles(Application.persistentDataPath);Application.logMessageReceived+=Log;
            report=new Report{startedUtc=DateTime.UtcNow.ToString("o"),unityVersion=Application.unityVersion,output=directory,timeScale=requestedTimeScale};Write();
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object item=null;bool next=false;Exception failure=null;
                try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){report.stopReason=failure.ToString();errors.Add(failure.ToString());if(current!=null){current.status="Stopped";current.stopReason=failure.Message;}break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(item is IEnumerator nested)stack.Push(nested);else yield return item;
            }
            Finish();
        }
        IEnumerator Run()
        {
            yield return null;battle=FindAnyObjectByType<BattleDirector>();Require(battle!=null&&battle.enabled&&battle.Catalog.CrossoverReady,"Current crossover battle scene is ready.");
            storage=Path.Combine(directory,"Storage");battle.ConfigureStorage(storage);battle.Hud.ShowTitle();Require(battle.StartTitleGame(0),"Fresh title game starts in isolated slot.");
            report.difficulty=battle.Preferences.Difficulty.ToString();report.teamTurns=!battle.UseCT&&!battle.UseFixedSpeedOrder;report.ct=battle.UseCT;report.utilityEnemyAI=battle.UseUtilityAI;report.reactions=battle.Preferences.Reactions;report.autoTiming=battle.Campaign.AutoTiming;report.missionEvents=battle.Preferences.MissionEvents;report.enemySpeedMode=battle.Preferences.EnemySpeedMode;report.skipEnemyAnimations=battle.Preferences.SkipEnemyAnimations;
            Require(report.teamTurns&&!report.ct&&!report.utilityEnemyAI&&!report.reactions&&!report.autoTiming&&!report.missionEvents&&report.difficulty=="Standard"&&report.enemySpeedMode==0&&!report.skipEnemyAnimations,"Fresh defaults match the documented review conditions.");
            var drops=new System.Random(17);battle.RewardRoll=()=>drops.Next(10000);
            battle.Audio.SetVolumes(0,0);battle.Audio.PersonalMusicEnabled=false;Time.timeScale=requestedTimeScale;
            for(int episode=1;episode<=20;episode++)
            {
                int stage=CrossoverCampaign.FirstStage+episode-1;
                Require(battle.Session==null&&!battle.StoryActive&&battle.SelectedStage==stage,"Main flow selects the expected next episode "+episode);
                purchases.Clear();actions.Clear();captures.Clear();unitRows.Clear();observedTurns.Clear();observedIntent.Clear();
                current=new StageRow{episode=episode,stage=stage,goldEntry=battle.Campaign.Gold};rows.Add(current);
                var joined=battle.Catalog.Characters.Where(battle.IsJoined).ToArray();current.joined=joined.Select(c=>c.Id).ToArray();
                Require(current.joined.OrderBy(s=>s).SequenceEqual(battle.Catalog.Characters.Where(c=>CrossoverCampaign.Joined(c.Id,episode)).Select(c=>c.Id).OrderBy(s=>s)),"Joined roster follows story progression.");
                var selected=Preferred.Select(id=>joined.FirstOrDefault(c=>c.Id==id)).Where(c=>c!=null).Take(battle.Catalog.Rules.MaxDeployment).ToArray();
                battle.Deployment.Clear();battle.Deployment.AddRange(selected.Select(c=>Array.IndexOf(battle.Catalog.Characters,c)));battle.NormalizeDeployment();
                PrepareEquipment(selected);
                foreach(var c in selected)if(CampaignPromotion.Unavailable(battle.Campaign.Get(c.Id),c,battle.Campaign.StoryProgress)==null)Require(CampaignPromotion.TrySave(battle.Campaign,c,battle.PersistCampaign),"Eligible promotion saves.");
                Require(battle.SavePreparation(),"Preparation saves.");CheckDisk();battle.Hud.ShowIntermissionHub();yield return Settle();
                current.party=selected.Select(c=>c.Id).ToArray();current.goldSpent=purchases.Sum(p=>p.price);current.goldBeforeReward=battle.Campaign.Gold;
                Click("전투 시작");yield return Settle();Require(battle.StoryActive&&battle.StoryStage==stage&&battle.Session==null,"Battle cannot begin before its story.");
                yield return ReadStory(false);
                if(episode==13){Require(battle.Session==null,"Episode 13 waits for route selection.");Click(PersonalScenarios.RouteName(stage,0)+"부터");}
                yield return Settle();Require(battle.Session!=null&&battle.Session.CampaignStage==stage,"Story completion begins actual battle.");
                Require(battle.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id).SequenceEqual(current.party),"Actual deployment matches selected joined party.");
                AttachBattle();current.status="Fighting";Write();
                if(episode==1||episode==11||episode==16||episode==20)yield return Capture("battle-start");
                began=Time.realtimeSinceStartupAsDouble;scaledBegan=Time.timeAsDouble;frameBegan=Time.frameCount;
                yield return Fight();ObserveUnits();
                current.battleWallSeconds=Time.realtimeSinceStartupAsDouble-began;current.scaledSimulationSeconds=Time.timeAsDouble-scaledBegan;current.measuredFrames=Time.frameCount-frameBegan;
                Require(battle.Session.Result==BattleResult.Victory,"Automated legal commands achieved victory in episode "+episode);
                Require(battle.State is BattleEndState&&!battle.RewardPending,"Actual victory state persisted rewards.");
                current.rewardApplied=true;current.goldReward=battle.GoldResult;current.goldAfterReward=battle.Campaign.Gold;current.nodes=battle.Session.Crossover.State.Nodes;current.segment=battle.Session.Crossover.State.Segment;current.finalBossPhase=battle.Session.Crossover.Boss.BossPhase;
                current.rewardItems=battle.ItemResults.Select(i=>i.Item.Id+":"+i.Count).ToArray();current.inventoryAfterReward=battle.Campaign.Inventory.Select(i=>i.Id+":"+i.Count).ToArray();
                Require(current.goldAfterReward==current.goldBeforeReward+current.goldReward,"Victory gold matches the result and transaction.");
                current.pendingEndingSaved=battle.Campaign.HasPendingAfterStory&&battle.Campaign.PendingAfterStoryStage==stage;Require(current.pendingEndingSaved,"Victory persists required ending.");
                CheckGrowth();CheckDisk();
                string rewards=RewardFingerprint(battle.Campaign);battle.CompleteBattle();battle.SaveBattleReward();current.duplicateRewardPrevented=rewards==RewardFingerprint(battle.Campaign);Require(current.duplicateRewardPrevented,"Repeated completion cannot duplicate rewards.");
                if(episode==1||episode==11||episode==16||episode==20)yield return Capture("battle-result");
                Click("결과 계속");yield return Settle();Require(battle.StoryActive,"Result Continue opens mandatory ending.");
                yield return ReadStory(true);
                Require(battle.Session==null&&!battle.StoryActive,"Ending finishes at preparation.");
                current.endingCleared=!battle.Campaign.HasPendingAfterStory;Require(current.endingCleared,"Ending pending state clears only after final line.");
                current.nextStageUnlocked=episode==20?battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(20)):CampaignStages.Unlocked(battle.Campaign,stage+1)&&battle.SelectedStage==stage+1;
                Require(current.nextStageUnlocked,"Next episode or completed main state is available.");CheckDisk();
                // Keep independent evidence from before ReturnToTitle writes the campaign again.
                string permanentBefore=PermanentState(battle.Campaign);int selectedBefore=battle.SelectedStage;var deploymentBefore=battle.Deployment.ToArray();
                current.permanentStateBeforeReloadHash=StateHash(permanentBefore);File.WriteAllText(Path.Combine(directory,"episode-"+episode.ToString("00")+"-before-title-reload.json"),permanentBefore);Write();
                battle.ReturnToTitle();yield return Settle();Require(battle.Hud.TitleOpen&&battle.LoadTitleGame(0,false),"Completed episode reloads through title.");yield return Settle();CheckDisk();
                string permanentAfter=PermanentState(battle.Campaign);current.permanentStateAfterReloadHash=StateHash(permanentAfter);File.WriteAllText(Path.Combine(directory,"episode-"+episode.ToString("00")+"-after-title-reload.json"),permanentAfter);
                current.permanentStateRoundTripPreserved=permanentBefore==permanentAfter&&battle.SelectedStage==selectedBefore&&battle.Deployment.SequenceEqual(deploymentBefore);
                Require(current.permanentStateRoundTripPreserved,"Title round trip preserves the entire campaign except SavedAt, plus live deployment and selected stage.");
                Require(!battle.StoryActive&&!battle.Campaign.HasPendingAfterStory,"Reload does not repeat a completed ending.");
                current.completedEndingReloaded=true;
                current.status="Completed";report.completedStages++;Write();
                Debug.Log("CAMPAIGN_FLOW episode="+episode+" commands="+current.playerCommands+" enemySkills="+current.enemySkills+" spent="+current.goldSpent+" reward="+current.goldReward);
            }
            Require(FindButton("전투 시작")!=null&&!FindButton("전투 시작").interactable,"Final main-complete hub does not start episode 21.");
            report.complete=true;
        }
        IEnumerator ReadStory(bool after)
        {
            var speakers=new List<string>();int count=0;
            int expectedLines=CampaignContent.Story(current.stage,after).Length;
            // The authored ending has one extra route record when this choice was committed.
            if(after&&(PersonalScenarios.Contains(current.stage)&&PersonalScenarios.Tier(current.stage)==1||current.stage==18))
            {
                string prefix=CampaignStages.Id(current.stage)+".route.";
                if(battle.Campaign.StoryProgress.Contains(prefix+"left")||battle.Campaign.StoryProgress.Contains(prefix+"right"))expectedLines++;
            }
            while(battle.StoryActive)
            {
                Require(count++<100,"Story line bound.");yield return Settle();
                var line=battle.ReadStoryHistory().Last();int index=battle.StoryIndex;
                Require(index==speakers.Count&&index<expectedLines,"Story renders each authored line once in order, starting at zero.");
                var body=battle.Hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DialogueBody"&&t.isActiveAndEnabled);
                Require(body.text==line.Text,"Rendered story text matches the active story line.");speakers.Add(index+":"+line.Speaker);
                if(index==0)yield return Capture(after?"after-story":"before-story");
                var next=FindButton("다음 대사")??FindButton("이야기 마치기");Require(next!=null,"Story exposes its next/finish control.");Click(next.name);yield return null;
                if(after&&battle.StoryActive)
                {
                    CheckDisk();Require(battle.Campaign.HasPendingAfterStory&&battle.Campaign.PendingAfterStoryStage==current.stage&&battle.Campaign.PendingAfterStoryLine==battle.StoryIndex,"Pending ending persists the next unread line.");current.pendingEndingCursorChecks++;
                }
            }
            Require(speakers.Count==expectedLines,"All authored "+(after?"after":"before")+"-story lines, including any committed route record, were rendered.");
            if(after){current.afterLines=speakers.Count;current.afterSpeakers=speakers.ToArray();}
            else{current.beforeLines=speakers.Count;current.beforeSpeakers=speakers.ToArray();}
        }
        void AttachBattle()
        {
            foreach(var u in battle.Session.Units)
            {
                unitRows.Add(u,new UnitRow{id=u.Data.Id,team=u.Team.ToString(),entryLevel=u.Level,entryEXP=u.EXP,exitLevel=u.Level,exitEXP=u.EXP,entryMP=u.CurrentMP,minMP=u.CurrentMP,equipment=u.Equipment.Select(e=>e!=null?e.Id:"").ToArray()});observedTurns[u]=u.TurnsStarted;observedIntent[u]="";
            }
            // The existing Crossover.OnResolved callback remains registered.
            battle.Session.Resolver.SkillResolved+=OnResolved;ObserveUnits();
        }
        void Update(){if(!finished&&battle?.Session!=null&&current!=null&&current.status=="Fighting")ObserveUnits();}
        void ObserveUnits()
        {
            if(battle?.Session==null)return;
            foreach(var u in battle.Session.Units)
            {
                if(!unitRows.TryGetValue(u,out var r))continue;
                r.minMP=Math.Min(r.minMP,u.CurrentMP);r.exitMP=u.CurrentMP;r.maxMP=u.Stats.MP;r.hp=u.CurrentHP;r.maxHP=u.Stats.HP;r.alive=u.Alive;r.turns=u.TurnsStarted;r.actionEXP=u.BattleEXP;r.bossPhase=u.BossPhase;r.battleExitLevel=u.Level;r.battleExitEXP=u.EXP;
                if(u.TurnsStarted>observedTurns[u]){if(u.TurnBeganIncapacitated)r.incapacitatedTurns++;observedTurns[u]=u.TurnsStarted;if(u.CrossoverBoss)ObserveAction(u,"boss-turn-start",u.TurnBeganIncapacitated?"Incapacitated":"Available");}
                string key=u.IntentPhase==1?u.IntentTurn+":"+u.IntentSkill:"";
                if(key!=""&&key!=observedIntent[u]){r.preparations++;if(u.CrossoverBoss)ObserveAction(u,"boss-preparation","Observed");}observedIntent[u]=key;
            }
            current.playerPhases=(battle.Session.Scheduler as TeamTurnScheduler)?.Round??0;
            current.enemyTurns=unitRows.Where(p=>p.Key.Team==Team.Enemy).Sum(p=>p.Value.turns);current.enemySkills=unitRows.Where(p=>p.Key.Team==Team.Enemy).Sum(p=>p.Value.skills);
            current.bossPreparations=unitRows.Where(p=>p.Key.CrossoverBoss).Sum(p=>p.Value.preparations);current.bossSkills=unitRows.Where(p=>p.Key.CrossoverBoss).Sum(p=>p.Value.skills);
        }
        void ObserveAction(UnitRuntime u,string kind,string outcome)
        {actions.Add(new ActionRow{sequence=actions.Count+1,round=(battle.Session.Scheduler as TeamTurnScheduler)?.Round??0,turn=u.TurnsStarted,actor=u.Data.Id,team=u.Team.ToString(),kind=kind,skill=u.IntentSkill,mpBefore=-1,mpAfter=u.CurrentMP,hpAfter=u.CurrentHP,x=u.Position.x,y=u.Position.y,bossPhase=u.BossPhase,intentPhase=u.IntentPhase,outcome=outcome});}
        void OnResolved(UnitRuntime actor,SkillData skill,Vector2Int aim)
        {
            if(!unitRows.TryGetValue(actor,out var u))return;u.skills++;int cost=battle.Session.Resolver.MPCost(actor,skill);u.mpSpent+=cost;
            float proxy=BoardView.Windup(skill)+BoardView.Recovery(skill);current.nominalSkillAnimationSecondsProxy+=proxy;
            actions.Add(new ActionRow{sequence=actions.Count+1,round=(battle.Session.Scheduler as TeamTurnScheduler)?.Round??0,turn=actor.TurnsStarted,actor=actor.Data.Id,team=actor.Team.ToString(),kind="skill-resolved",skill=skill.Id,mpBefore=-1,mpAfter=actor.CurrentMP,hpAfter=actor.CurrentHP,x=aim.x,y=aim.y,bossPhase=actor.BossPhase,intentPhase=actor.IntentPhase,outcome="Executed",nominalWindupRecoverySeconds=proxy});ObserveUnits();
        }
        IEnumerator Fight()
        {
            double timeout=Time.realtimeSinceStartupAsDouble+900;int seenActions=0;
            while(!(battle.State is BattleEndState))
            {
                Require(Time.realtimeSinceStartupAsDouble<timeout,"Battle exceeded 900s automation wall limit; no forced outcome.");
                Require(current.playerCommands<500,"Battle exceeded 500 player-command limit; no forced outcome.");
                if(!battle.IsPlayerCommand){yield return null;continue;}
                var session=battle.Session;var actor=session.Active;current.playerCommands++;
                var plan=ObjectivePlan(session,actor,planner.Plan(session,actor));
                if(plan.Destination!=actor.Position)
                {
                    var destination=plan.Destination;yield return battle.MoveUnit(destination);
                    actions.Add(new ActionRow{sequence=actions.Count+1,actor=actor.Data.Id,team="Player",kind="move",x=destination.x,y=destination.y,outcome=actor.Position==destination?"Moved":"Rejected",round=((TeamTurnScheduler)session.Scheduler).Round,turn=actor.TurnsStarted,mpAfter=actor.CurrentMP,hpAfter=actor.CurrentHP});
                }
                if(battle.State is BattleEndState)break;
                if(battle.IsPlayerCommand&&session.Active==actor&&plan.Skill!=null&&(plan.Aim.HasValue||plan.Target!=null))
                {
                    var aim=plan.Aim??plan.Target.Position;int mp=actor.CurrentMP,previous=actions.Count;
                    yield return battle.Execute(plan.Skill,aim);
                    if(actions.Count==previous)actions.Add(new ActionRow{sequence=actions.Count+1,actor=actor.Data.Id,team="Player",kind="skill-rejected",skill=plan.Skill.Id,mpBefore=mp,mpAfter=actor.CurrentMP,hpAfter=actor.CurrentHP,x=aim.x,y=aim.y,outcome=battle.Message,round=((TeamTurnScheduler)session.Scheduler).Round,turn=actor.TurnsStarted});
                }
                if(battle.State is BattleEndState)break;
                if(battle.IsPlayerCommand&&session.Active==actor)
                {
                    if(plan.Guard&&!actor.Acted)battle.Guard();else battle.WaitCommand();
                    Require(battle.State is FacingSelectionState,"Command finishes through actual facing selection.");battle.ChooseFacing(actor.Facing);
                }
                if(actions.Count>=seenActions+20){seenActions=actions.Count;Write();}
                yield return null;
            }
            yield return Settle();
        }
        EnemyPlan ObjectivePlan(BattleSession s,UnitRuntime u,EnemyPlan plan)
        {
            if(s.Units.Any(e=>e.Team==Team.Enemy&&e.Alive&&!e.CrossoverReserve))return plan;
            Vector2Int? goal=null;
            if(s.Crossover.UsesNodes&&s.Crossover.State.Nodes!=7)goal=current.episode==13&&s.Crossover.State.Nodes==0?CrossoverBattle.Nodes[s.Crossover.State.Choice]:CrossoverBattle.Nodes.Where((p,i)=>(s.Crossover.State.Nodes&(1<<i))==0).OrderBy(p=>GridMap.Distance(u.Position,p)).First();
            else if(s.Crossover.UsesExit)goal=CrossoverBattle.Exit;
            if(!goal.HasValue)return plan;
            var distance=new Dictionary<Vector2Int,int>{{goal.Value,0}};var queue=new Queue<Vector2Int>();queue.Enqueue(goal.Value);
            while(queue.Count>0){var p=queue.Dequeue();foreach(var tile in s.Grid.Neighbors(p)){if(!tile.Walkable||Math.Abs(tile.Height-s.Grid[p].Height)>u.Stats.JMP||tile.Occupant!=null&&tile.Occupant!=u||distance.ContainsKey(tile.Coordinate))continue;distance[tile.Coordinate]=distance[p]+1;queue.Enqueue(tile.Coordinate);}}
            return new EnemyPlan{Destination=s.Grid.Reachable(u,out _).Keys.Where(distance.ContainsKey).OrderBy(p=>distance[p]).ThenBy(p=>GridMap.Distance(u.Position,p)).DefaultIfEmpty(u.Position).First(),Guard=true};
        }
        void PrepareEquipment(CharacterData[] party)
        {
            foreach(var c in party)
            {
                var draft=new EquipmentLoadout(c,battle.Campaign.Get(c.Id),battle.Catalog.Equipment,battle.Campaign);bool changed=false;
                foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {var best=draft.Options(slot).OrderByDescending(Score).ThenBy(e=>e.Id,StringComparer.Ordinal).FirstOrDefault();if(best!=null&&Score(best)>Score(draft.Get(slot))&&draft.Equip(slot,best))changed=true;}
                if(changed)Require(draft.TrySave(battle.Campaign,battle.PersistCampaign),"Owned gear upgrade saves through loadout API.");
            }
            for(int attempt=0;attempt<party.Length*3;attempt++)
            {
                var choices=party.SelectMany(c=>battle.Catalog.Equipment.Where(e=>e!=null&&EquipmentLoadout.CanEquip(c,e,e.Slot)&&CampaignInventory.PurchaseUnavailable(battle.Campaign,e)==null)
                    .Select(e=>new {character=c,item=e,gain=Score(e)-Score(new EquipmentLoadout(c,battle.Campaign.Get(c.Id),battle.Catalog.Equipment,battle.Campaign).Get(e.Slot))})).Where(p=>p.gain>0).OrderByDescending(p=>p.gain/p.item.BuyPrice).ThenBy(p=>p.item.Id,StringComparer.Ordinal).ThenBy(p=>Array.IndexOf(party,p.character)).ToArray();
                if(choices.Length==0)break;var chosen=choices[0];int gold=battle.Campaign.Gold;
                Require(CampaignInventory.TryBuy(battle.Campaign,chosen.item,save=>{var loadout=new EquipmentLoadout(chosen.character,save.Get(chosen.character.Id),battle.Catalog.Equipment,save);return loadout.Equip(chosen.item.Slot,chosen.item)&&loadout.TrySave(save,battle.PersistCampaign);}),"Affordable unlocked purchase and equip commits atomically.");
                Require(battle.Campaign.Gold==gold-chosen.item.BuyPrice,"Purchase charged the advertised price.");purchases.Add(new PurchaseRow{character=chosen.character.Id,item=chosen.item.Id,price=chosen.item.BuyPrice,goldBefore=gold,goldAfter=battle.Campaign.Gold});CheckDisk();
            }
        }
        static double Score(EquipmentData item){if(item==null)return 0;var s=item.Bonus;return s.HP*.15+s.MP*.1+s.STR+s.MAG+s.DEF+s.MDF+s.SPD+s.MOV*8+s.JMP*4;}
        void CheckGrowth()
        {
            foreach(var pair in unitRows.Where(p=>p.Key.Team==Team.Player))
            {var r=pair.Value;var p=battle.Campaign.Get(pair.Key.Data.Id);r.exitLevel=p.Level;r.exitEXP=p.EXP;int level=r.entryLevel,exp=r.entryEXP;ExperienceRules.Advance(ref level,ref exp,pair.Key.BattleEXP+CampaignEconomy.Experience(current.stage,false));Require(level==p.Level&&exp==p.EXP,"Saved growth is action EXP plus mission EXP for "+r.id);}
        }
        void CheckDisk()
        {
            var file=new CampaignFile(battle.Profiles.PathFor(0));var read=file.Load();Require(file.CanSave&&string.IsNullOrEmpty(file.Notice),"Isolated campaign loads without backup or protection.");Require(JsonUtility.ToJson(read)==JsonUtility.ToJson(battle.Campaign),"Disk campaign equals the current committed campaign.");report.saveReloadChecks++;if(current!=null)current.saveReloadChecks++;
        }
        static string RewardFingerprint(CampaignSave save)=>save.Gold+"|"+string.Join(";",save.StoryProgress.OrderBy(s=>s))+"|"+string.Join(";",save.Inventory.OrderBy(i=>i.Id).Select(i=>i.Id+":"+i.Count))+"|"+string.Join(";",save.Characters.OrderBy(c=>c.Id).Select(c=>c.Id+":"+c.Level+":"+c.EXP+":"+c.Promoted+":"+string.Join(",",c.Equipment)));
        static string PermanentState(CampaignSave save)
        {
            // Serialize all current fields, including growth/traits/equipment, presets, deployment,
            // selected stage and pending story/checkpoint. Do not normalize or sort away differences.
            var snapshot=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));snapshot.SavedAt=null;return JsonUtility.ToJson(snapshot,true);
        }
        static string StateHash(string state){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(state))).Replace("-","").ToLowerInvariant();}
        UnityEngine.UI.Button FindButton(string name)=>battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);
        void Click(string name){var button=FindButton(name);Require(button!=null&&button.interactable,"Visible enabled button: "+name);ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);}
        static IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        IEnumerator Capture(string label)
        {yield return Settle();string path="episode-"+current.episode.ToString("00")+"-"+label+".png";var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,path),image.EncodeToPNG());Destroy(image);captures.Add(path);Write();}
        void Require(bool ok,string message){report.checks++;if(!ok)throw new InvalidOperationException(message);}
        void Write()
        {
            if(current!=null){current.units=unitRows.Values.ToArray();current.purchases=purchases.ToArray();current.actions=actions.ToArray();current.captures=captures.ToArray();}
            report.stages=rows.ToArray();report.errors=errors.ToArray();File.WriteAllText(Path.Combine(directory,"report.json"),JsonUtility.ToJson(report,true));
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);Write();}}
        static string[] SnapshotFiles(string root)
        {
            if(!Directory.Exists(root))return Array.Empty<string>();using(var sha=SHA256.Create())return Directory.GetFiles(root).OrderBy(f=>f,StringComparer.OrdinalIgnoreCase).Select(f=>Path.GetFileName(f)+":"+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f)))).ToArray();
        }
        void Finish()
        {
            if(finished)return;finished=true;int exit=1;Application.logMessageReceived-=Log;
            try
            {
                ObserveUnits();report.finishedUtc=DateTime.UtcNow.ToString("o");report.userFilesUnchanged=userBefore!=null&&userBefore.SequenceEqual(SnapshotFiles(Application.persistentDataPath));
                if(!report.userFilesUnchanged)errors.Add("User storage changed during the review; no files were restored by this review.");report.status=report.complete&&errors.Count==0?"Completed":"Stopped";Write();exit=report.status=="Completed"?0:1;
            }
            catch(Exception e)
            {
                errors.Add("Could not finalize campaign flow review: "+e);if(report!=null){report.status="Stopped";report.stopReason=e.ToString();}Debug.LogError("Campaign flow review finalization failed: "+e);
            }
            finally
            {
                try{Time.timeScale=oldTimeScale;Application.runInBackground=oldBackground;}
                catch(Exception e){exit=1;Debug.LogError("Campaign flow review restoration failed: "+e);}
                finally
                {
#if UNITY_EDITOR
                    try{UnityEditor.SessionState.SetInt("TalesTactics.CampaignFlowReviewExit",exit);}finally{UnityEditor.EditorApplication.isPlaying=false;}
#else
                    Application.Quit(exit);
#endif
                }
            }
        }
        void OnDestroy(){Application.logMessageReceived-=Log;Time.timeScale=oldTimeScale;Application.runInBackground=oldBackground;}
    }
}
#endif
