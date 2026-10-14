using System.Collections.Generic;
using UnityEngine;

namespace TalesTactics
{
    // Retains the existing hierarchy and authored anchors. Maps its content coordinates into
    // a padded rectangle so legacy fixed and proportional layouts share the same frame inset.
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class FramedContent : MonoBehaviour
    {
        sealed class Placement { public Vector2 Low,High,AppliedLow,AppliedHigh; public bool Applied; }
        readonly Dictionary<RectTransform,Placement> placements=new Dictionary<RectTransform,Placement>();
        readonly List<RectTransform> children=new List<RectTransform>();
        int lastFrame=-1;
        public Vector2 Padding { get; private set; }
        public Rect ContentRect { get; private set; }
        public static bool IsDecoration(Transform t)=>t.name.Contains("Backdrop")||t.name=="Frame";
        void LateUpdate()=>Fit();
        public void Fit()
        {
            if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            var ancestor=transform.parent!=null?transform.parent.GetComponentInParent<FramedContent>():null;
            if(ancestor!=null)ancestor.Fit();
            var image=GetComponent<UnityEngine.UI.Image>();if(image==null||!image.enabled)return;
            var parent=(RectTransform)transform;var rect=parent.rect;if(rect.width<=0||rect.height<=0)return;
            Padding=new Vector2(Mathf.Min(24,rect.width*.06f),Mathf.Min(20,rect.height*.08f));
            ContentRect=new Rect(rect.min+Padding,rect.size-2*Padding);
            children.Clear();var sourceMin=Vector2.zero;var sourceMax=rect.size;
            foreach(Transform child in transform)
            {
                if(!child.gameObject.activeSelf||IsDecoration(child)||!(child is RectTransform r))continue;
                if(!placements.TryGetValue(r,out var p)){p=new Placement{Low=r.offsetMin,High=r.offsetMax};placements.Add(r,p);}
                else {if(!p.Applied||(r.offsetMin-p.AppliedLow).sqrMagnitude>.01f)p.Low=r.offsetMin;if(!p.Applied||(r.offsetMax-p.AppliedHigh).sqrMagnitude>.01f)p.High=r.offsetMax;}
                children.Add(r);
                sourceMin=Vector2.Min(sourceMin,Vector2.Scale(r.anchorMin,rect.size)+p.Low);
                sourceMax=Vector2.Max(sourceMax,Vector2.Scale(r.anchorMax,rect.size)+p.High);
            }
            var factor=new Vector2(ContentRect.width/Mathf.Max(1,sourceMax.x-sourceMin.x),ContentRect.height/Mathf.Max(1,sourceMax.y-sourceMin.y));
            foreach(var r in children)
            {
                var p=placements[r];var low=Vector2.Scale(r.anchorMin,rect.size)+p.Low;var high=Vector2.Scale(r.anchorMax,rect.size)+p.High;
                r.offsetMin=Padding+Vector2.Scale(low-sourceMin,factor)-Vector2.Scale(r.anchorMin,rect.size);
                r.offsetMax=Padding+Vector2.Scale(high-sourceMin,factor)-Vector2.Scale(r.anchorMax,rect.size);
                p.AppliedLow=r.offsetMin;p.AppliedHigh=r.offsetMax;p.Applied=true;
            }
            // Rebuilt menus destroy their former children at the end of the frame.
            if(placements.Count>children.Count+64){var removed=new List<RectTransform>();foreach(var r in placements.Keys)if(r==null)removed.Add(r);foreach(var r in removed)placements.Remove(r);}
        }
    }
}
