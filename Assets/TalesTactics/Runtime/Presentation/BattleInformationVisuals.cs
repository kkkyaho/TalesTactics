using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform InfoBlock(Transform parent,string name,float y,float height)
        {
            var r=Panel(name,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);r.SetParent(parent,false);
            Place(r,new Vector2(0,1),Vector2.one,new Vector2(12,-y-height),new Vector2(-12,-y));
            r.GetComponent<UnityEngine.UI.Image>().color=new Color(.025f,.055f,.085f,1);r.GetComponent<UnityEngine.UI.Outline>().enabled=false;return r;
        }
        void TintInfoTab(Transform parent,bool selected)
        {parent.GetChild(parent.childCount-1).GetComponent<UnityEngine.UI.Image>().color=selected?new Color(.1f,.37f,.39f):new Color(.06f,.1f,.16f);}
        UnityEngine.UI.Image InfoPortrait(Transform parent,CharacterData data,float x,float y,float width,float height)
        {
            var g=new GameObject("InfoPortrait",typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(parent,false);
            Place((RectTransform)g.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-height),new Vector2(x+width,-y));
            var image=g.GetComponent<UnityEngine.UI.Image>();image.sprite=data.Portrait!=null?data.Portrait:data.Sprites?.Front;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        void ForecastHealthBar(Transform parent,ForecastRow row,float y)
        {
            var bar=InfoBlock(parent,"ForecastHP",y,16);int max=Mathf.Max(1,row.Unit.Stats.HP);
            var image=bar.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.08f,.12f,.17f);bar.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            void Segment(string name,float low,float high,Color color)
            {
                var g=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(bar,false);
                Place((RectTransform)g.transform,new Vector2(Mathf.Clamp01(low),0),new Vector2(Mathf.Clamp01(high),1),Vector2.zero,Vector2.zero);
                g.GetComponent<UnityEngine.UI.Image>().color=color;g.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            }
            Segment("Remaining",0,Mathf.Min(row.BeforeHP,row.AfterHP)/(float)max,new Color(.12f,.55f,.37f));
            Segment("Change",Mathf.Min(row.BeforeHP,row.AfterHP)/(float)max,Mathf.Max(row.BeforeHP,row.AfterHP)/(float)max,row.AfterHP<row.BeforeHP?new Color(.95f,.3f,.3f):new Color(.35f,.9f,.7f));
        }
    }
}
