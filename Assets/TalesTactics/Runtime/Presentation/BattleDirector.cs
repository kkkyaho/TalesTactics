using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace TalesTactics
{
    public sealed partial class BattleDirector:MonoBehaviour
    {
        public BattleCatalog Catalog;
        public BattleHud Hud;
        public BoardView Board;
        public BattleAudio Audio;
        public BattleSession Session {get;private set;}
        public BattleState State {get;private set;}
        public SkillData SelectedSkill;
        public Vector2Int? Target;
        public readonly List<int> Deployment=new List<int>{0,1,3};
        public CampaignSave Campaign;
        public bool TrainingMode, TimingActive, TimingSuccess;
        public bool UseCT, UseUtilityAI, UseFixedSpeedOrder;
        public ObjectiveKind TrainingObjective;
        public float TimingProgress;
        public string Message="출전 인원을 선택하세요 (1–6명).";
        bool completed, timingAttempted, battleTraining;
        int battleStage;
        public int SelectedStage;
        public bool RewardPending {get;private set;}
        CampaignReward pendingReward;
        public System.Func<int> RewardRoll=()=>UnityEngine.Random.Range(0,10000);
        public System.Func<CampaignSave,bool> PersistCampaign=CampaignStorage.Save;
        public bool IsPlayerCommand=>Session!=null&&Session.Active!=null&&Session.Active.Team==Team.Player&&State is CommandState;
        void Start()
        {
            if(Hud==null||Board==null||Audio==null)
            {Debug.LogError("TalesTactics: BattleDirector requires HUD, Board and Audio references.");enabled=false;return;}
            ConfigureStorage(Application.persistentDataPath,true);Hud.Initialize(this);
            if(!CatalogValidation.TryValidate(Catalog,out var error))
            {Debug.LogError("TalesTactics: "+error);Hud.ShowSetupError(error);enabled=false;return;}
            Deployment.RemoveAll(i=>i<0||i>=Catalog.Characters.Length);
            if(Deployment.Count==0)Deployment.Add(0);
            Board.Initialize(this);Hud.ShowDeployment();
            gameObject.AddComponent<GamepadPointer>().Initialize(this);
        }
        void Update()
        {
            if(StoryActive)return;
            if(Keyboard.current!=null&&Keyboard.current.f5Key.wasPressedThisFrame){Hud.ShowSystemMenu();return;}
            if(Keyboard.current!=null&&Keyboard.current.f1Key.wasPressedThisFrame){if(!Hud.CloseHelp())Hud.ShowHelp();return;}
            if(Hud.InputModalOpen)
            {
                if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame||Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame){if(!Hud.CloseMission()&&!Hud.CloseSystemMenu()&&!Hud.CloseHelp())Hud.CloseUnitDetails();}
                return;
            }
            if(TimingActive&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)TimingInput();
            if(Session==null||State==null)return;
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(TacticalKeyboard(keyboard))return;
                if((keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame)&&State is TargetSelectionState){Confirm();return;}
                if(keyboard.tabKey.wasPressedThisFrame)CycleTarget(keyboard.shiftKey.isPressed?-1:1);
                if(keyboard.qKey.wasPressedThisFrame)Board.RotateCamera(-90);
                if(keyboard.eKey.wasPressedThisFrame)Board.RotateCamera(90);
                if(keyboard.homeKey.wasPressedThisFrame&&!TimingActive)Board.ResetCamera();
            }
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&!(State is ActionExecutionState))State.Cancel();
            if(Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame&&!(State is ActionExecutionState)){State.Cancel();return;}
            var mouse=Mouse.current;if(mouse==null)return;
            if(mouse.leftButton.wasPressedThisFrame){HandleBattleClick(mouse.position.ReadValue());return;}
            if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
            if(Board.BattleCamera.pixelRect.Contains(mouse.position.ReadValue()))
            {
                float scroll=mouse.scroll.ReadValue().y;
                if(scroll!=0)Board.ZoomCamera(scroll>0?0.9f:1.1f);
            }
            if(Board.Pick(mouse.position.ReadValue(),out var p))
            {
                if(mouse.delta.ReadValue().sqrMagnitude>0){KeyboardTile=null;PreviewTile(p);}

            }
        }
        public void HandleBattleClick(Vector2 screen)
        {
            if(Session?.Active==null||Session.Active.Team!=Team.Player||Hud.InputModalOpen||StoryActive||TimingActive||State is ActionExecutionState||State is BattleEndState||State is TutorialCompleteState)return;
            if(EventSystem.current!=null)
            {
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
                if(hits.Count>0)return;
            }
            bool picked=Board.Pick(screen,out var p);
            if(State is MoveSelectionState&&picked&&(!TutorialActive||p==TutorialDestination)&&Session.Grid.Path(Session.Active,p).Count>1){State.Tile(p);return;}
            if(State is TargetSelectionState&&picked&&TutorialTargetAllowed(p)&&Session.Resolver.InRange(Session.Active,SelectedSkill,p)&&Session.Resolver.Targets(Session.Active,SelectedSkill,p).Any()){SelectTarget(p);Confirm();return;}
            if(State is CommandState)
            {
                if(picked&&Session.Units.Any(u=>u.Alive&&u.Position==p)){State.Tile(p);return;}
                if(Session.Active.CanUndoMove&&!TutorialActive){Undo();return;}
                if(picked)State.Tile(p);return;
            }
            State?.Cancel();
        }
        public void SetState(BattleState state){State=state;Target=null;Forecast=null;KeyboardTile=null;state.Enter();Board.RefreshFocus();}
        public void BeginBattle()
        {
            if(Session!=null||Deployment.Count<1||!TrainingMode&&!CampaignStages.Unlocked(Campaign,SelectedStage))return;completed=false;RewardPending=false;
            battleStage=SelectedStage;battleTraining=TrainingMode;pendingReward=null;
            Session=new BattleSession(Catalog,Deployment,TrainingMode?25:1,TrainingMode?25:CampaignStages.EnemyLevel(SelectedStage),TrainingMode?-1:SelectedStage,UseCT,UseUtilityAI,TrainingMode?TrainingObjective:ObjectiveKind.Eliminate,teamTurns:!UseCT&&!UseFixedSpeedOrder);
            foreach(var u in Session.Units.Where(u=>u.Team==Team.Player))
            {
                var progress=Campaign.Get(u.Data.Id);
                if(!TrainingMode){u.Level=Mathf.Clamp(progress.Level,1,50);u.Promoted=progress.Promoted;}
                new EquipmentLoadout(u.Data,progress,Catalog.Equipment,Campaign).Apply(u);
                if(!TrainingMode)TacticalDevelopment.Apply(u,Campaign,SelectedStage);else u.Trait=progress.Trait;
                u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;
            }
            Board.Build(Session);Audio.PlayBattle(!TrainingMode&&CampaignStages.Get(SelectedStage).BossMusic);Message="청색 타일은 이동, 적색 타일은 스킬 사거리입니다.";SetState(new TurnStartState(this));
        }
        public void Restart(){Hud.CloseMission();Hud.CloseSystemMenu();Hud.CloseHelp();ResetTutorial();CloseStory();StopAllCoroutines();ClearTacticalSelection();TimingActive=false;RewardPending=false;Session=null;State=null;Board.ResetBoard();Audio.StopAll();Hud.ShowDeployment();}
        public void MoveCommand(){if(IsPlayerCommand&&!Session.Active.Moved&&TutorialAllows(TutorialStep.Movement))SetState(new MoveSelectionState(this));}
        public void AttackCommand(){if(IsPlayerCommand&&!Session.Active.Acted&&TutorialAllows(TutorialStep.Attack))SelectSkill(Session.Active.Data.BasicAttack);}
        public void SkillCommand(){if(IsPlayerCommand&&TutorialAllows(TutorialStep.Healing))SetState(new ActionSelectionState(this));}
        public void WaitCommand(){if(IsPlayerCommand&&TutorialAllows(TutorialStep.Waiting))SetState(new FacingSelectionState(this));}
        public void Undo(){if(!TutorialActive&&IsPlayerCommand&&Session.UndoMove()){RefreshViews();Hud.Refresh();}}
        public void Guard(){if(!TutorialActive&&IsPlayerCommand&&!Session.Active.Acted){Session.Active.Acted=true;Session.Active.CanUndoMove=false;Session.Active.AddStatus(StatusKind.Guard,2);SetState(new FacingSelectionState(this));}}
        public bool IsFollowup(SkillData s)=>Session.Active.Acted&&Session.Active.FlamingChain&&s.Gate==SkillGate.FlamingEdge;
        public void SelectSkill(SkillData s)
        {
            if(Session.Active.Team!=Team.Player||!TutorialSkillAllowed(s))return;
            var error=Session.Resolver.CanUse(Session.Active,s,IsFollowup(s));if(error!=null){Message=error;Hud.Refresh();return;}
            if(s!=Session.Active.Data.BasicAttack)RememberSkill(s);
            Target=null;SelectedSkill=s;SetState(new TargetSelectionState(this));
        }
        public void SelectTarget(Vector2Int p)
        {
            if(!TutorialTargetAllowed(p))return;
            var u=Session.Active;var targets=Session.Resolver.Targets(u,SelectedSkill,p).ToArray();
            if(!Session.Resolver.InRange(u,SelectedSkill,p)||targets.Length==0){Target=null;Message="유효한 타겟을 선택하세요.";Hud.Refresh();return;}
            Target=p;Forecast=BattleForecast.Create(Session,u,SelectedSkill,p,IsFollowup(SelectedSkill));Message=Forecast.Cost+"\n"+string.Join("\n",Forecast.Rows.Select(r=>r.Text));Board.ShowArea(p,SelectedSkill.Area);Board.RefreshFocus();Hud.Refresh();
        }
        public void Confirm(){if(State is TargetSelectionState&&Target.HasValue)StartCoroutine(Execute(SelectedSkill,Target.Value,IsFollowup(SelectedSkill)));}
        public void ChooseFacing(Facing f){if(!(State is FacingSelectionState)||!TutorialAllows(TutorialStep.Waiting))return;Session.Active.Facing=f;RefreshViews();if(!TutorialEndTurn())SetState(new TurnEndState(this));}
        public IEnumerator MoveUnit(Vector2Int p)
        {
            if(!TutorialAllows(TutorialStep.Movement)||TutorialActive&&p!=TutorialDestination)yield break;
            var u=Session.Active;var path=Session.Grid.Path(u,p);if(path.Count<2||!Session.Move(p))yield break;
            SetState(new ActionExecutionState(this));yield return Board.AnimateMove(u,path);TutorialMoved();RefreshViews();SetState(Session.Result==BattleResult.Ongoing?(BattleState)new CommandState(this):new BattleEndState(this));
        }
        public IEnumerator Execute(SkillData s,Vector2Int p,bool followup=false)
        {
            SetState(new ActionExecutionState(this));var u=Session.Active;
            if(s.IsLionHowl&&u.Data.Skills.All(u.Unlocked)&&Session.Resolver.CanUse(u,u.Data.UltimateSkill,true)==null)
            {
                TimingActive=true;TimingSuccess=false;timingAttempted=false;float start=Time.time;
                while(Time.time-start<Catalog.Rules.TimingDuration)
                {
                    TimingProgress=(Time.time-start)/Catalog.Rules.TimingDuration;
                    Board.ShowTimingSpin(u,TimingProgress);
                    if(Campaign.AutoTiming&&!timingAttempted&&TimingProgress>=Catalog.Rules.TimingWindowStart)TimingInput();
                    Hud.UpdateTiming();yield return null;
                }
                TimingActive=false;Board.ClearTiming();if(TimingSuccess){s=u.Data.UltimateSkill;followup=true;}
            }
            var old=u.Facing;if(p!=u.Position)u.Facing=SkillResolver.Toward(u.Position,p);
            Board.BeginSkill(u,s,p);
            yield return new WaitForSeconds(BoardView.Windup(s));
            var before=Board.CaptureHealth();
            var recipients=Session.Resolver.Targets(u,s,p).ToArray();
            bool executed=Session.Resolver.Execute(u,s,p,out var message,followup);
            if(!executed)u.Facing=old;else{Board.ReleaseSkill(u);Board.PresentImpact(u,s,p,before,recipients);}
            Message=message;yield return new WaitForSeconds(BoardView.Recovery(s));
            if(s.IsUltimate)Audio.EndTheme();
            TutorialExecuted(s,executed);
            RefreshViews();SetState(Session.Result==BattleResult.Ongoing?(BattleState)new CommandState(this):new BattleEndState(this));
        }
        public void TimingInput(){if(!TimingActive||timingAttempted)return;timingAttempted=true;TimingSuccess=TimingProgress>=Catalog.Rules.TimingWindowStart&&TimingProgress<=Catalog.Rules.TimingWindowEnd;}
        public IEnumerator SkipTurn(){Message=Session.Active.Data.DisplayName+" 행동 불가";Hud.Refresh();yield return new WaitForSeconds(0.35f);SetState(new TurnEndState(this));}
        public IEnumerator EnemyTurn()
        {
            float speed=1<<Mathf.Clamp(Preferences?.EnemySpeedMode??0,0,2);bool brief=Preferences?.SkipEnemyAnimations??false;
            SetState(new ActionExecutionState(this));yield return new WaitForSeconds(brief?.08f:.4f/speed);
            var u=Session.Active;var plan=new EnemyPlanner().Plan(Session,u);EnemyTactics.Commit(u,plan);
            if(plan.Destination!=u.Position){var path=Session.Grid.Path(u,plan.Destination);if(Session.Move(plan.Destination)){if(brief)Board.Sync();else yield return Board.AnimateMove(u,path,speed);}}
            if(plan.Skill!=null&&(plan.Aim.HasValue||plan.Target!=null))
            {
                var aim=plan.Aim??plan.Target.Position;
                if(aim!=u.Position)u.Facing=SkillResolver.Toward(u.Position,aim);
                if(!brief){Board.BeginSkill(u,plan.Skill,aim,speed);yield return new WaitForSeconds(BoardView.Windup(plan.Skill)/speed);}
                var before=Board.CaptureHealth();var recipients=Session.Resolver.Targets(u,plan.Skill,aim).ToArray();
                if(Session.Resolver.Execute(u,plan.Skill,aim,out var text)&&!brief){Board.ReleaseSkill(u);Board.PresentImpact(u,plan.Skill,aim,before,recipients);}
                Message=u.Data.DisplayName+" · "+plan.Skill.DisplayName+"\n"+text;Hud.Refresh();yield return new WaitForSeconds(brief?.2f:BoardView.Recovery(plan.Skill)/speed);
                if(plan.Skill.IsUltimate)Audio.EndTheme();
            }
            else if(plan.Guard&&!u.Acted){u.Acted=true;u.AddStatus(StatusKind.Guard,2);Message=u.Data.DisplayName+" : "+(u.IntentPhase==1?EnemyTactics.Describe(u):"가드 · 인접 보호");}
            else Message=u.Data.DisplayName+" : "+(u.IntentPhase==2?"휴식 · 받는 피해 +25%":"대기");
            RefreshViews();SetState(Session.Result==BattleResult.Ongoing?(BattleState)new TurnEndState(this):new BattleEndState(this));
        }
        public void RefreshViews(){Board.Sync();RebuildThreats();}
        public void CompleteBattle()
        {
            if(completed||Session==null||Session.Result==BattleResult.Ongoing)return;
            completed=true;BeginResultSummary();Message=Session.Result==BattleResult.Victory?"승리 — "+Session.ObjectiveDescription:"패배 — 다시 도전하세요.";
            if(Session.Result!=BattleResult.Victory)return;
            Audio.Play("victory");
            if(battleTraining){Message+="\n훈련: 경험치·골드·장비·장 완료 기록은 저장하지 않습니다.";return;}
            pendingReward=CampaignEconomy.Prepare(Campaign,battleStage,RewardRoll());RewardPending=true;SaveBattleReward();
        }
        public void SaveBattleReward()
        {
            if(!RewardPending||pendingReward==null)return;
            string ItemText(string id)
            {
                var item=Catalog.Equipment.FirstOrDefault(e=>e!=null&&e.Id==id);string name=item!=null?item.DisplayName:id;
                return name+(CampaignInventory.Owned(Campaign,id)>=CampaignInventory.MaxQuantity?" 미지급 (보유 상한 99개)":" +1");
            }
            string lootText=ItemText(CampaignStages.EquipmentReward(battleStage))+" · 추가: "+(pendingReward.BonusEquipment==null?"없음":ItemText(pendingReward.BonusEquipment));
            int goldGranted=System.Math.Min(pendingReward.Gold,CampaignInventory.MaxGold-Campaign.Gold);
            bool saved;
            try{saved=CampaignStages.TryReward(Campaign,pendingReward,Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data).ToArray(),PersistCampaign);}
            catch(System.Exception){saved=false;}
            RewardPending=!saved;UpdateResultSummary(saved);
            Message=saved?CampaignStages.Title(battleStage)+" 완료 · "+(pendingReward.Repeat?"반복":"최초")+" EXP +"+pendingReward.Experience+" / "+goldGranted+"G 저장"+(goldGranted<pendingReward.Gold?" (골드 상한 적용)":"")+"\n장비: "+lootText:"저장 실패 — 보상 미적용. 추첨 결과는 유지됩니다. 재시도하거나 출전 화면으로 돌아가 포기할 수 있습니다.";
            Hud.Refresh();
        }
    }
}
