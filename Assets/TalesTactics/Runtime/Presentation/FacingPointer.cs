using UnityEngine;
using UnityEngine.EventSystems;
namespace TalesTactics
{
    public sealed class FacingPointer:MonoBehaviour,IPointerEnterHandler
    {
        public BattleDirector Battle;public Facing Direction;
        public void OnPointerEnter(PointerEventData data){Battle.PreviewFacing(Direction);}
    }
}
