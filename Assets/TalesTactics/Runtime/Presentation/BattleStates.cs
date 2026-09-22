using UnityEngine;
namespace TalesTactics
{
    public abstract class BattleState
    {
        protected readonly BattleDirector B;
        protected BattleState(BattleDirector b){B=b;}
        public virtual string Title=>GetType().Name.Replace("State","");
        public virtual void Enter(){B.Hud.Refresh();}
        public virtual void Tile(Vector2Int p){}
        public virtual void Cancel(){B.SetState(new CommandState(B));}
    }
    public sealed class BattleStartState:BattleState {public BattleStartState(BattleDirector b):base(b){}public override void Enter(){B.BeginBattle();}}
    public sealed class TurnStartState:BattleState
    {
        public TurnStartState(BattleDirector b):base(b){}
        public override void Enter(){if(B.Session.Result!=BattleResult.Ongoing){B.SetState(new BattleEndState(B));return;}B.Session.Advance();B.RefreshViews();if(B.Session.Active.Has(StatusKind.Stun)||B.Session.Active.Has(StatusKind.Sleep)){B.StartCoroutine(B.SkipTurn());return;}B.SetState(new CommandState(B));}
    }
    public sealed class CommandState:BattleState
    {
        public CommandState(BattleDirector b):base(b){}
        public override void Enter(){B.Board.ClearHighlights();base.Enter();if(B.Session.Active.Team==Team.Enemy)B.StartCoroutine(B.EnemyTurn());}
        public override void Tile(Vector2Int p){B.Hud.Inspect(p);}
    }
    public sealed class MoveSelectionState:BattleState
    {
        public MoveSelectionState(BattleDirector b):base(b){}
        public override void Enter(){B.Board.ShowRange(B.Session.Grid.Reachable(B.Session.Active,out _).Keys,new Color(0.15f,0.65f,1));base.Enter();}
        public override void Tile(Vector2Int p){B.StartCoroutine(B.MoveUnit(p));}
    }
    public sealed class ActionSelectionState:BattleState {public ActionSelectionState(BattleDirector b):base(b){} }
    public sealed class SkillDetailsState:BattleState
    {
        public readonly SkillData Skill;
        public SkillDetailsState(BattleDirector b,SkillData skill):base(b){Skill=skill;}
        public override string Title=>"스킬 상세";
        public override void Enter(){B.Board.ClearHighlights();base.Enter();}
        public override void Cancel(){B.SetState(new ActionSelectionState(B));}
    }
    public sealed class TargetSelectionState:BattleState
    {
        public TargetSelectionState(BattleDirector b):base(b){}
        public override void Enter(){B.Board.ShowSkillRange(B.Session.Active,B.SelectedSkill);base.Enter();}
        public override void Tile(Vector2Int p){B.SelectTarget(p);}
    }
    public sealed class ActionExecutionState:BattleState {public ActionExecutionState(BattleDirector b):base(b){}public override void Cancel(){} }
    public sealed class FacingSelectionState:BattleState {public FacingSelectionState(BattleDirector b):base(b){} }
    public sealed class TurnEndState:BattleState
    {
        public TurnEndState(BattleDirector b):base(b){}
        public override void Enter(){B.Session.EndTurn();B.SetState(new TurnStartState(B));}
    }
    public sealed class BattleEndState:BattleState
    {
        public BattleEndState(BattleDirector b):base(b){}
        public override void Enter(){B.CompleteBattle();base.Enter();}
        public override void Cancel(){}
    }
}
