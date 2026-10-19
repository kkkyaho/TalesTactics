using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        Vector3 cameraPan,appliedCameraPan;
        Vector2 dragScreen;
        public Vector3 CameraPanOffset=>cameraPan;
        public bool CameraDragging {get;private set;}
        public void BeginCameraDrag(Vector2 screen)
        {if(root==null||battle.Session==null)return;dragScreen=screen;CameraDragging=true;}
        public void DragCamera(Vector2 screen)
        {
            if(!CameraDragging||root==null||battle.Session==null)return;
            var plane=new Plane(Vector3.up,battlefieldBounds.center);
            var before=BattleCamera.ScreenPointToRay(dragScreen);
            var after=BattleCamera.ScreenPointToRay(screen);
            dragScreen=screen;
            if(!plane.Raycast(before,out float a)||!plane.Raycast(after,out float b))return;
            var previous=cameraPan;cameraPan+=before.GetPoint(a)-after.GetPoint(b);
            cameraPan.y=0;FitBattlefield(true);
            if((cameraPan-previous).sqrMagnitude>.0001f)battle.LearnContextHint(ContextHintKind.MapPan);
        }
        public void EndCameraDrag()=>CameraDragging=false;
        void RemoveAppliedCameraPan()
        {
            if(BattleCamera!=null)BattleCamera.transform.position-=appliedCameraPan;
            appliedCameraPan=Vector3.zero;
        }
        void ClearCameraPan()
        {RemoveAppliedCameraPan();cameraPan=Vector3.zero;CameraDragging=false;}
        void ApplyCameraPan()
        {
            // Keep the point being explored on the map, including after a zoom or rotation.
            var anchor=battlefieldBounds.center;
            if(focusAction&&battle.Session.Active!=null)
            {
                anchor=battle.Session.Grid[battle.Session.Active.Position].WorldPosition(battle.Catalog.Rules.TileHeight);
                if(battle.Target.HasValue)anchor=(anchor+battle.Session.Grid[battle.Target.Value].WorldPosition(battle.Catalog.Rules.TileHeight))*.5f;
            }
            cameraPan.x=Mathf.Clamp(anchor.x+cameraPan.x,battlefieldBounds.min.x,battlefieldBounds.max.x)-anchor.x;
            cameraPan.z=Mathf.Clamp(anchor.z+cameraPan.z,battlefieldBounds.min.z,battlefieldBounds.max.z)-anchor.z;
            cameraPan.y=0;appliedCameraPan=cameraPan;
            BattleCamera.transform.position+=appliedCameraPan;
        }
    }
}
