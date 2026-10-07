using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public BattleForecast Forecast {get;private set;}
        public UnitRuntime InspectedUnit,HoveredUnit;
        public Vector2Int? KeyboardTile {get;private set;}
        public int ThreatMode {get;private set;} // 0=off, 1=selected enemy, 2=all enemies
        public Dictionary<UnitRuntime,HashSet<Vector2Int>> Threats {get;private set;}=new Dictionary<UnitRuntime,HashSet<Vector2Int>>();
        public void CycleThreat(){ThreatMode=(ThreatMode+1)%3;Board.DrawThreats();Hud.Refresh();}
        public string ThreatLabel=>ThreatMode==2?"위험: 전체 적 ◇":ThreatMode==1?"위험: 선택 적 ◇":"위험: 숨김";
        public int DangerAt(Vector2Int p)=>Threats.Count(e=>e.Key.Alive&&e.Value.Contains(p));
        public void RebuildThreats(){if(Session==null)return;Threats=ThreatMap.Calculate(Session);Board.DrawThreats();}
        public void InspectUnit(UnitRuntime u){InspectedUnit=u;Board.DrawThreats();}
        public void PreviewTile(Vector2Int p)
        {
            if(Session?.Active==null||Hud.InputModalOpen||Session.Active.Team!=Team.Player)return;
            if(State is MoveSelectionState){Board.ShowPath(Session.Grid.Path(Session.Active,p));Hud.ShowMoveRisk(p,DangerAt(p));}
            if(State is TargetSelectionState&&Target!=p)
            {
                if(TutorialTargetAllowed(p)&&Session.Resolver.InRange(Session.Active,SelectedSkill,p)&&Session.Resolver.Targets(Session.Active,SelectedSkill,p).Any())SelectTarget(p);
                else if(KeyboardTile.HasValue&&Target.HasValue){Target=null;Forecast=null;Hud.Refresh();}
            }
        }
        void ClearTacticalSelection()
        {
            InspectedUnit=HoveredUnit=null;KeyboardTile=null;Forecast=null;Threats.Clear();
        }
        bool TacticalKeyboard(Keyboard k)
        {
            if(Session.Active.Team!=Team.Player||TimingActive||State is ActionExecutionState||State is BattleEndState)return false;
            if(k.vKey.wasPressedThisFrame){CycleThreat();return true;}
            if(IsPlayerCommand)
            {
                if(k.mKey.wasPressedThisFrame){MoveCommand();return true;}
                if(k.aKey.wasPressedThisFrame){AttackCommand();return true;}
                if(k.sKey.wasPressedThisFrame){SkillCommand();return true;}
                if(k.gKey.wasPressedThisFrame){Guard();return true;}
                if(k.wKey.wasPressedThisFrame){WaitCommand();return true;}
                if(k.zKey.wasPressedThisFrame){Undo();return true;}
            }
            if(State is FacingSelectionState)
            {
                if(k.upArrowKey.wasPressedThisFrame){ChooseFacing(Facing.Back);return true;}
                if(k.downArrowKey.wasPressedThisFrame){ChooseFacing(Facing.Front);return true;}
                if(k.leftArrowKey.wasPressedThisFrame){ChooseFacing(Facing.Left);return true;}
                if(k.rightArrowKey.wasPressedThisFrame){ChooseFacing(Facing.Right);return true;}
            }
            if(!(State is MoveSelectionState)&&!(State is TargetSelectionState))return false;
            var step=k.upArrowKey.wasPressedThisFrame?Vector2Int.up:k.downArrowKey.wasPressedThisFrame?Vector2Int.down:k.leftArrowKey.wasPressedThisFrame?Vector2Int.left:k.rightArrowKey.wasPressedThisFrame?Vector2Int.right:Vector2Int.zero;
            if(step!=Vector2Int.zero)
            {
                var next=(KeyboardTile??Target??Session.Active.Position)+step;
                if(Session.Grid[next]!=null){KeyboardTile=next;UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);PreviewTile(next);}return true;
            }
            if(State is MoveSelectionState&&(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)&&KeyboardTile.HasValue){State.Tile(KeyboardTile.Value);return true;}
            return false;
        }
    }
}
