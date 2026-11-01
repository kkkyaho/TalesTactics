using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    [DefaultExecutionOrder(1100)]
    public sealed class UnitHealthLayout:MonoBehaviour
    {
        public const float MaximumHorizontalShift=34,MaximumVerticalShift=24;
        readonly List<UnitHealthDisplay> displays=new List<UnitHealthDisplay>();
        readonly List<Rect> occupied=new List<Rect>();
        readonly List<Rect> placed=new List<Rect>();
        public IReadOnlyList<Rect> PlacedBounds=>placed;
        void LateUpdate()
        {
            GetComponentsInChildren(false,displays);placed.Clear();occupied.Clear();
            if(displays.Count==0)return;
            var hud=displays[0].Hud;var safe=CombatLabelPlacement.Viewport(hud);hud.AppendCombatLabelReservations(occupied);
            float scale=Mathf.Clamp(Screen.height/1080f,.8f,1.5f);
            displays.Sort((a,b)=>a.Priority!=b.Priority?b.Priority.CompareTo(a.Priority):a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            foreach(var display in displays)
            {
                if(!display.Bar.enabled)continue;
                var original=display.ScreenOrigin;
                if(!TryPlaceNearHead(original,display.OccupiedSize,safe,occupied,scale,out var candidate))
                {
                    display.Compact();
                    if(!TryPlaceNearHead(original,display.OccupiedSize,safe,occupied,scale,out candidate))
                    {
                        // The fixed status panel remains available; never detach a gauge from its actor.
                        display.Suppress();continue;
                    }
                }
                display.Place(new Vector2(candidate.center.x,candidate.yMin),original);placed.Add(candidate);occupied.Add(candidate);
            }
        }
        public static bool TryPlaceNearHead(Vector2 origin,Vector2 size,Rect safe,IList<Rect> occupied,float scale,out Rect result)
        {
            result=default;
            if(size.x>safe.width||size.y>safe.height)return false;
            float stepX=MaximumHorizontalShift*scale*.5f,stepY=MaximumVerticalShift*scale*.5f;
            float bestDistance=float.PositiveInfinity;bool found=false;
            // Search only the immediate head neighbourhood, preferring a short vertical correction.
            // Large HUD or damage-number reservations suppress a gauge rather than creating remote labels.
            for(int ring=0;ring<=2;ring++)
            for(int y=ring;y>=-ring;y--)for(int x=-ring;x<=ring;x++)
            {
                if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(y))!=ring)continue;
                Vector2 point=origin+new Vector2(x*stepX,y*stepY);
                point.x=Mathf.Clamp(point.x,safe.xMin+size.x*.5f,safe.xMax-size.x*.5f);
                point.y=Mathf.Clamp(point.y,safe.yMin,safe.yMax-size.y);
                Vector2 shift=point-origin;
                if(Mathf.Abs(shift.x)>MaximumHorizontalShift*scale+.01f||Mathf.Abs(shift.y)>MaximumVerticalShift*scale+.01f)continue;
                var candidate=new Rect(point.x-size.x*.5f,point.y,size.x,size.y);
                bool collision=false;
                foreach(var other in occupied)
                {
                    var padding=new Rect(other.x-2*scale,other.y-2*scale,other.width+4*scale,other.height+4*scale);
                    if(candidate.Overlaps(padding)){collision=true;break;}
                }
                if(collision)continue;
                float distance=shift.sqrMagnitude;
                if(distance>=bestDistance)continue;
                bestDistance=distance;result=candidate;found=true;
            }
            return found;
        }
    }
}
