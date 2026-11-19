using UnityEngine;

namespace TalesTactics
{
    // Keep the complete menu hierarchy inside the usable display without shrinking
    // the camera or painting an input-blocking image over the battlefield.
    public sealed class MobileUiLayout:MonoBehaviour
    {
        public RectTransform ContentRoot { get; private set; }
        public Rect SafeArea { get; private set; }
        Vector2 lastScreen;
        Rect lastSafeArea;

        public RectTransform Initialize(RectTransform root)
        {
            if(ContentRoot!=null)return ContentRoot;
            var children=new Transform[root.childCount];
            for(int i=0;i<children.Length;i++)children[i]=root.GetChild(i);
            ContentRoot=new GameObject("MobileSafeArea",typeof(RectTransform)).GetComponent<RectTransform>();
            ContentRoot.SetParent(root,false);
            // Independent screen-space canvases (damage numbers and gamepad cursor) keep pixel coordinates.
            foreach(var child in children)if(child.GetComponent<Canvas>()==null)child.SetParent(ContentRoot,false);
            // A recreated root has no anchors yet, even if the display cache is unchanged.
            ApplySafeArea(Screen.safeArea,new Vector2(Screen.width,Screen.height));
            return ContentRoot;
        }

        public void RefreshScreen()
        {
            var size=new Vector2(Screen.width,Screen.height);var area=Screen.safeArea;
            if(size==lastScreen&&area==lastSafeArea)return;
            ApplySafeArea(area,size);
        }

        public void ApplySafeArea(Rect area,Vector2 screenSize)
        {
            if(ContentRoot==null)return;
            lastScreen=screenSize;lastSafeArea=area;
            float width=Mathf.Max(1,screenSize.x),height=Mathf.Max(1,screenSize.y);
            float left=Mathf.Clamp(area.xMin,0,width),bottom=Mathf.Clamp(area.yMin,0,height);
            float right=Mathf.Clamp(area.xMax,left,width),top=Mathf.Clamp(area.yMax,bottom,height);
            // Some devices report zero while their display is being recreated.
            SafeArea=right-left<1||top-bottom<1?new Rect(0,0,width,height):Rect.MinMaxRect(left,bottom,right,top);
            ContentRoot.anchorMin=new Vector2(SafeArea.xMin/width,SafeArea.yMin/height);
            ContentRoot.anchorMax=new Vector2(SafeArea.xMax/width,SafeArea.yMax/height);
            ContentRoot.offsetMin=ContentRoot.offsetMax=Vector2.zero;
        }

        public void Restore(RectTransform root)
        {
            if(ContentRoot==null)return;
            while(ContentRoot.childCount>0)ContentRoot.GetChild(0).SetParent(root,false);
            Destroy(ContentRoot.gameObject);ContentRoot=null;
        }
    }
}
