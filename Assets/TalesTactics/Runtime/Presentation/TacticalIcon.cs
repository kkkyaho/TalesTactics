using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TacticalIcon:UnityEngine.UI.MaskableGraphic
    {
        string kind;public int Corner=-1;protected override void OnEnable(){base.OnEnable();SdUiIconArt.Apply(this);}
        public string Kind {get=>kind;set{if(kind==value)return;kind=value;SetAllDirty();}}
        public IllustratedIcons.Region Region=>IllustratedIcons.Get(kind,Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)<=25);
        public override Texture mainTexture=>Region?.Texture;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var region=Region;if(region==null)return;
            var r=GetPixelAdjustedRect();float ratio=region.width/(float)region.height;
            if(r.width/r.height>ratio){float width=r.height*ratio;r.x+=(r.width-width)*.5f;r.width=width;}else{float height=r.width/ratio;r.y+=(r.height-height)*.5f;r.height=height;}
            var uv=IllustratedIcons.PartUV(region,Corner);
            // Legacy monochrome call sites retain disabled contrast without tinting painted art gold.
            float brightness=Mathf.Max(color.r,color.g,color.b)<.6f?.46f:1;
            var tint=new Color(brightness,brightness,brightness,color.a);
            mesh.AddVert(new Vector3(r.xMin,r.yMin),tint,new Vector2(uv.xMin,uv.yMin));mesh.AddVert(new Vector3(r.xMin,r.yMax),tint,new Vector2(uv.xMin,uv.yMax));
            mesh.AddVert(new Vector3(r.xMax,r.yMax),tint,new Vector2(uv.xMax,uv.yMax));mesh.AddVert(new Vector3(r.xMax,r.yMin),tint,new Vector2(uv.xMax,uv.yMin));
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
        }
        protected override void OnRectTransformDimensionsChange(){base.OnRectTransformDimensionsChange();SetMaterialDirty();}
    }
}
