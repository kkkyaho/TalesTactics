using UnityEngine;
using TMPro;
namespace TalesTactics
{
    // Button object names remain stable for navigation and automation; only the visible glyph is replaced.
    public sealed class IllustratedButtonLabel:MonoBehaviour
    {
        TMP_Text label;TacticalIcon art;IconSelectionCorners selection;string rendered;Vector4 margin;bool right,center;
        void LateUpdate()
        {
            if(label==null){label=GetComponentInChildren<TMP_Text>();if(label==null)return;margin=label.margin;}
            if(label.text!=rendered)
            {
                string value=label.text,kind=null;bool selected=value.StartsWith("● ");right=false;center=false;
                if(selected||value.StartsWith("○ "))value=value.Substring(2);
                else if(value=="×"){kind="close";value="";center=true;}
                else if(value.StartsWith("‹")){kind=name=="거점으로"?"return":"previous";value=value.Substring(1).TrimStart();}
                else if(value.StartsWith("▶")){kind="next";value=value.Substring(1).TrimStart();}
                else if(value.StartsWith("◇")){kind="locked";value=value.Substring(1).TrimStart();}
                else if(value.EndsWith("›")){kind="next";value=value.Substring(0,value.Length-1).TrimEnd();right=true;}
                else if(value.EndsWith("◇")){kind="danger";value=value.Substring(0,value.Length-1).TrimEnd();right=true;}
                center|=value.Length==0;
                if(selected&&selection==null)selection=IconSelectionCorners.Add(transform);
                if(selection!=null)selection.gameObject.SetActive(selected);
                if(kind!=null&&art==null)art=IllustratedIcons.Add(transform,kind,Vector2.zero,Vector2.zero);
                if(art!=null){art.gameObject.SetActive(kind!=null);if(kind!=null)art.Kind=kind;}
                rendered=value;label.text=value;label.margin=margin;
            }
            if(art==null||!art.gameObject.activeSelf)return;
            var box=((RectTransform)transform).rect;float size=Mathf.Min(24,box.height-10,box.width-10);
            var r=art.rectTransform;r.anchorMin=r.anchorMax=new Vector2(center?.5f:right?1:0,.5f);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=Vector2.one*size;r.anchoredPosition=new Vector2(center?0:(right?-1:1)*(size*.5f+8),0);
            label.margin=margin+new Vector4(!center&&!right?size+4:0,0,!center&&right?size+4:0,0);
        }
    }
}
