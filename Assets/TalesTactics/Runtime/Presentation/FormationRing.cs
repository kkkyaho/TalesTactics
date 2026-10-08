using UnityEngine;
namespace TalesTactics
{
    // Thin ellipse drawn at UI resolution. Selection and deployment retain distinct colors.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FormationRing : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();const int segments=64;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                var p=new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f);
                var q=new Vector2(Mathf.Cos(b)*r.width*.5f,Mathf.Sin(b)*r.height*.5f);
                int start=mesh.currentVertCount;
                mesh.AddVert(r.center+p,color,Vector2.zero);mesh.AddVert(r.center+q,color,Vector2.zero);
                mesh.AddVert(r.center+q*.86f,color,Vector2.zero);mesh.AddVert(r.center+p*.86f,color,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
