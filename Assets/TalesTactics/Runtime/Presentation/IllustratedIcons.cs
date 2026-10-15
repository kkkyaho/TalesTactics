using System;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public static class IllustratedIcons
    {
        [Serializable] public sealed class Region
        {
            public string key,atlas;public int x,y,width,height;
            public Texture2D Texture=>Load(atlas);
            public Rect UV=>new Rect(x/(float)Texture.width,1-(y+height)/(float)Texture.height,width/(float)Texture.width,height/(float)Texture.height);
        }
        [Serializable] sealed class Catalog {public Region[] regions=Array.Empty<Region>();}
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static Dictionary<string,Region> regions;
        static Texture2D Load(string name)
        {if(!textures.TryGetValue(name,out var texture))textures[name]=texture=Resources.Load<Texture2D>("TalesTactics/Ornate/"+name);return texture;}
        public static Region Get(string key,bool compact=false)
        {
            if(regions==null)
            {
                regions=new Dictionary<string,Region>();var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("TalesTactics/Ornate/IconRegions").text);
                foreach(var region in catalog.regions)regions.Add(region.key,region);
            }
            if(key=="check")key="deployed";if(key=="chevron")key="next";if(key=="exit")key="return";if(key=="crest")key="deployed";
            if(compact&&regions.TryGetValue("compact-"+key,out var small))return small;
            return key!=null&&regions.TryGetValue(key,out var result)?result:null;
        }
        public static Rect PartUV(Region region,int corner)
        {var uv=region.UV;if(corner<0)return uv;return new Rect(uv.x+(corner%2)*uv.width*.5f,uv.y+(corner/2)*uv.height*.5f,uv.width*.5f,uv.height*.5f);}
        public static Sprite Sprite(string key,int corner=-1)
        {
            string id=key+":"+corner;if(sprites.TryGetValue(id,out var sprite))return sprite;
            var region=Get(key);if(region==null)return null;var uv=PartUV(region,corner);var texture=region.Texture;
            var pixels=new Rect(uv.x*texture.width,uv.y*texture.height,uv.width*texture.width,uv.height*texture.height);
            sprite=UnityEngine.Sprite.Create(texture,pixels,new Vector2(.5f,.5f),Mathf.Max(pixels.width,pixels.height),0,SpriteMeshType.FullRect);
            sprite.name=id;return sprites[id]=sprite;
        }
        public static TacticalIcon Add(Transform parent,string kind,Vector2 min,Vector2 max)
        {
            var g=new GameObject("Illustrated-"+kind,typeof(RectTransform),typeof(TacticalIcon));g.transform.SetParent(parent,false);
            var r=(RectTransform)g.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var icon=g.GetComponent<TacticalIcon>();icon.Kind=kind;icon.raycastTarget=false;return icon;
        }
    }
    public sealed class IconSelectionCorners:MonoBehaviour
    {
        readonly List<RectTransform> corners=new List<RectTransform>();
        public static IconSelectionCorners Add(Transform parent)
        {var g=new GameObject("Selection ornaments",typeof(RectTransform),typeof(IconSelectionCorners));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return g.GetComponent<IconSelectionCorners>();}
        void Awake()
        {
            for(int i=0;i<4;i++){var anchor=new Vector2(i%2,i/2);var icon=IllustratedIcons.Add(transform,"selected",anchor,anchor);icon.Corner=i;icon.rectTransform.pivot=anchor;corners.Add(icon.rectTransform);}
        }
        void LateUpdate()
        {
            var rect=((RectTransform)transform).rect;float size=Mathf.Clamp(Mathf.Min(rect.width,rect.height)*.28f,8,26);
            foreach(var r in corners){r.sizeDelta=Vector2.one*size;r.anchoredPosition=new Vector2(r.pivot.x==0?3:-3,r.pivot.y==0?3:-3);}
        }
    }
}
