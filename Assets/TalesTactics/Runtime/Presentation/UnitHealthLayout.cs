using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    [DefaultExecutionOrder(1000)]
    public sealed class UnitHealthLayout:MonoBehaviour
    {
        readonly List<UnitHealthDisplay> displays=new List<UnitHealthDisplay>();
        readonly List<Rect> placed=new List<Rect>();
        void LateUpdate()
        {
            GetComponentsInChildren(false,displays);placed.Clear();
            displays.Sort((a,b)=>a.Priority!=b.Priority?b.Priority.CompareTo(a.Priority):a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            foreach(var display in displays)
            {
                if(!display.Bar.enabled)continue;
                var rect=(RectTransform)display.transform;var original=rect.anchoredPosition;
                var position=original;float width=display.OccupiedSize.x;
                float height=display.OccupiedSize.y;
                Rect candidate=new Rect(position.x-width*.5f,position.y,width,height);
                for(int attempt=0;attempt<40;attempt++)
                {
                    bool overlap=false;foreach(var other in placed)if(candidate.Overlaps(other)){overlap=true;break;}
                    if(!overlap)break;
                    float offset=(attempt/2+1)*18f;
                    position.y=Mathf.Clamp(original.y+(attempt%2==0?offset:-offset),12,Screen.height-height-86);
                    candidate.position=new Vector2(position.x-width*.5f,position.y);
                }
                display.Place(position,original);placed.Add(candidate);
            }
        }
    }
}
