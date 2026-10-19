using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // All coordinates here are screen pixels, shared by the HUD canvas and unscaled HP canvas.
    public static class CombatLabelPlacement
    {
        public static Rect Viewport(BattleHud hud)
        {
            var v=hud.BattlefieldViewport;
            return Rect.MinMaxRect(v.xMin*Screen.width+6,v.yMin*Screen.height+6,v.xMax*Screen.width-6,v.yMax*Screen.height-6);
        }
        public static Rect ScreenRect(RectTransform rect)
        {
            if(rect==null)return default;
            var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }
        public static Rect Place(Vector2 centerBottom,Vector2 size,Rect safe,IList<Rect> occupied,out bool fits)
        {
            size=new Vector2(Mathf.Min(size.x,safe.width),Mathf.Min(size.y,safe.height));
            Rect best=Clamp(centerBottom,size,safe);float bestScore=float.PositiveInfinity;fits=false;
            for(int ring=0;ring<=10;ring++)
            {
                for(int y=ring;y>=-ring;y--)for(int x=-ring;x<=ring;x++)
                {
                    if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(y))!=ring)continue;
                    var candidate=Clamp(centerBottom+new Vector2(x*(size.x*.55f+4),y*(size.y+5)),size,safe);
                    float overlap=0;
                    foreach(var other in occupied)
                    {
                        float w=Mathf.Min(candidate.xMax+3,other.xMax)-Mathf.Max(candidate.xMin-3,other.xMin);
                        float h=Mathf.Min(candidate.yMax+3,other.yMax)-Mathf.Max(candidate.yMin-3,other.yMin);
                        if(w>0&&h>0)overlap+=w*h;
                    }
                    float distance=(new Vector2(candidate.center.x,candidate.yMin)-centerBottom).sqrMagnitude;
                    if(overlap<=0){fits=true;return candidate;}
                    float score=overlap*100000+distance;if(score<bestScore){bestScore=score;best=candidate;}
                }
            }
            return best;
        }
        static Rect Clamp(Vector2 origin,Vector2 size,Rect safe)=>new Rect(
            Mathf.Clamp(origin.x-size.x*.5f,safe.xMin,Mathf.Max(safe.xMin,safe.xMax-size.x)),
            Mathf.Clamp(origin.y,safe.yMin,Mathf.Max(safe.yMin,safe.yMax-size.y)),size.x,size.y);
    }
}
