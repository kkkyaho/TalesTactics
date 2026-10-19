using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    [DefaultExecutionOrder(1100)]
    public sealed class UnitHealthLayout:MonoBehaviour
    {
        readonly List<UnitHealthDisplay> displays=new List<UnitHealthDisplay>();
        readonly List<Rect> occupied=new List<Rect>();
        readonly List<Rect> placed=new List<Rect>();
        public IReadOnlyList<Rect> PlacedBounds=>placed;
        void LateUpdate()
        {
            GetComponentsInChildren(false,displays);placed.Clear();occupied.Clear();
            if(displays.Count==0)return;
            var hud=displays[0].Hud;var safe=CombatLabelPlacement.Viewport(hud);hud.AppendCombatLabelReservations(occupied);
            displays.Sort((a,b)=>a.Priority!=b.Priority?b.Priority.CompareTo(a.Priority):a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            foreach(var display in displays)
            {
                if(!display.Bar.enabled)continue;
                var original=display.ScreenOrigin;
                var candidate=CombatLabelPlacement.Place(original,display.OccupiedSize,safe,occupied,out bool fits);
                if(!fits&&display.Priority==0)
                {
                    display.Compact();candidate=CombatLabelPlacement.Place(original,display.OccupiedSize,safe,occupied,out fits);
                    // In an exceptionally tiny view retain focused information; distant bars resume when space returns.
                    if(!fits){display.Suppress();continue;}
                }
                display.Place(new Vector2(candidate.center.x,candidate.yMin),original);placed.Add(candidate);occupied.Add(candidate);
            }
        }
    }
}
