using UnityEngine;
namespace TalesTactics
{
    // Explicit atlas UVs on a shared quad keep neighbouring cells out of small foliage details.
    public sealed partial class PixelBattlefield
    {
        readonly Mesh[] foliageQuads=new Mesh[16];
        Material foliageMaterial;
        Transform FoliageQuad(int index,Vector3 position,float height)
        {
            if(foliage==null)return null;
            if(foliageMaterial==null){foliageMaterial=new Material(Resources.Load<Shader>("TalesTactics/UnitTone"));foliageMaterial.mainTexture=foliage;foliageMaterial.name="Foliage atlas details";owned.Add(foliageMaterial);}
            if(foliageQuads[index]==null)
            {
                float u=index%4*.25f,v=(3-index/4)*.25f,inset=1f/foliage.width;
                var mesh=new Mesh{name="Foliage cell "+index};mesh.vertices=new[]{new Vector3(-.5f,0),new Vector3(-.5f,1),new Vector3(.5f,1),new Vector3(.5f,0)};
                mesh.uv=new[]{new Vector2(u+inset,v+inset),new Vector2(u+inset,v+.25f-inset),new Vector2(u+.25f-inset,v+.25f-inset),new Vector2(u+.25f-inset,v+inset)};
                mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();foliageQuads[index]=mesh;
            }
            var go=new GameObject("Foliage detail "+index,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*height;go.transform.rotation=view.transform.rotation;
            go.GetComponent<MeshFilter>().sharedMesh=foliageQuads[index];var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=foliageMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;billboards.Add(go.transform);return go.transform;
        }
        void DisposeDetailQuads(){foreach(var mesh in foliageQuads)if(mesh!=null)Destroy(mesh);}
    }
}
