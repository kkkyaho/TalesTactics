using UnityEngine;
namespace TalesTactics
{
    // Resolution-independent HUD pictograms; no dependency on font symbol coverage.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TacticalIcon : UnityEngine.UI.MaskableGraphic
    {
        public string Kind;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            switch(Kind)
            {
                case "move": Line(mesh,.25f,.3f,.65f,.3f);Line(mesh,.65f,.3f,.65f,.65f);Line(mesh,.45f,.5f,.65f,.7f);Line(mesh,.65f,.7f,.85f,.5f);break;
                case "guard": Poly(mesh,new[]{new Vector2(.2f,.8f),new Vector2(.8f,.8f),new Vector2(.75f,.35f),new Vector2(.5f,.15f),new Vector2(.25f,.35f)});break;
                case "wait": Line(mesh,.25f,.85f,.75f,.85f);Line(mesh,.25f,.15f,.75f,.15f);Line(mesh,.3f,.8f,.7f,.2f);Line(mesh,.7f,.8f,.3f,.2f);break;
                case "heal": Line(mesh,.5f,.15f,.5f,.85f,.18f);Line(mesh,.15f,.5f,.85f,.5f,.18f);break;
                case "martial": Poly(mesh,new[]{new Vector2(.25f,.25f),new Vector2(.18f,.52f),new Vector2(.27f,.78f),new Vector2(.72f,.78f),new Vector2(.82f,.62f),new Vector2(.73f,.25f)});Line(mesh,.3f,.15f,.7f,.15f,.1f);break;
                case "bow": Line(mesh,.35f,.15f,.6f,.35f);Line(mesh,.6f,.35f,.65f,.5f);Line(mesh,.65f,.5f,.6f,.65f);Line(mesh,.6f,.65f,.35f,.85f);Line(mesh,.35f,.15f,.35f,.85f,.035f);Line(mesh,.15f,.5f,.9f,.5f);Line(mesh,.7f,.65f,.9f,.5f);Line(mesh,.7f,.35f,.9f,.5f);break;
                case "song": Line(mesh,.4f,.25f,.4f,.8f);Line(mesh,.4f,.8f,.8f,.9f);Line(mesh,.8f,.9f,.8f,.35f);Poly(mesh,new[]{new Vector2(.15f,.2f),new Vector2(.4f,.2f),new Vector2(.4f,.4f),new Vector2(.15f,.35f)});Poly(mesh,new[]{new Vector2(.55f,.25f),new Vector2(.8f,.25f),new Vector2(.8f,.45f),new Vector2(.55f,.4f)});break;
                case "rifle": Line(mesh,.2f,.65f,.9f,.65f,.16f);Line(mesh,.35f,.65f,.25f,.2f,.17f);Line(mesh,.55f,.6f,.55f,.4f);break;
                case "spear": Line(mesh,.2f,.15f,.7f,.75f);Poly(mesh,new[]{new Vector2(.5f,.72f),new Vector2(.9f,.95f),new Vector2(.75f,.55f)});break;
                case "skill": Poly(mesh,new[]{new Vector2(.5f,.95f),new Vector2(.3f,.6f),new Vector2(.55f,.55f),new Vector2(.35f,.05f),new Vector2(.8f,.6f),new Vector2(.55f,.65f)});break;
                case "undo": Line(mesh,.2f,.65f,.7f,.65f);Line(mesh,.7f,.65f,.7f,.25f);Line(mesh,.7f,.25f,.45f,.25f);Line(mesh,.2f,.65f,.4f,.85f);Line(mesh,.2f,.65f,.4f,.45f);break;
                case "exit": Line(mesh,.2f,.2f,.2f,.8f);Line(mesh,.2f,.8f,.5f,.8f);Line(mesh,.2f,.2f,.5f,.2f);Line(mesh,.4f,.5f,.85f,.5f);Line(mesh,.65f,.7f,.85f,.5f);Line(mesh,.65f,.3f,.85f,.5f);break;
                default: Poly(mesh,new[]{new Vector2(.35f,.35f),new Vector2(.72f,.86f),new Vector2(.86f,.9f),new Vector2(.85f,.72f),new Vector2(.45f,.3f)});Line(mesh,.2f,.45f,.55f,.2f);Line(mesh,.35f,.32f,.2f,.12f);break;
            }
        }
        Vector2 Point(Vector2 p){var r=GetPixelAdjustedRect();return new Vector2(r.x+p.x*r.width,r.y+p.y*r.height);}
        void Poly(UnityEngine.UI.VertexHelper mesh,Vector2[] points)
        {int start=mesh.currentVertCount;foreach(var p in points)mesh.AddVert(Point(p),color,Vector2.zero);for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);}
        void Line(UnityEngine.UI.VertexHelper mesh,float x,float y,float endX,float endY,float width=.075f)
        {var a=new Vector2(x,y);var b=new Vector2(endX,endY);var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;Poly(mesh,new[]{a-n,a+n,b+n,b-n});}
    }
}
