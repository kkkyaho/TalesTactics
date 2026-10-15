using System;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        // Shared illustrated atlas: emblem, party, equipment, shop, growth, close.
        void MenuArt(Transform parent,int cell,Vector2 min,Vector2 max)
        {
            var bounds=FormationRect(parent,"MenuArt-"+cell,min,max);
            var art=FormationRect(bounds,"Illustration",Vector2.zero,Vector2.one);
            var image=art.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture=Resources.Load<Texture2D>("TalesTactics/Ornate/MenuIcons");
            image.uvRect=new Rect(cell%3/3f,cell<3?.5f:0,1/3f,.5f);image.raycastTarget=false;
            var fit=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio=image.texture!=null?image.texture.width/3f/(image.texture.height/2f):1;
        }
        RectTransform CloseIcon(Transform parent,string name,Action close)
        {
            var button=FormationButton(parent,name,"",Vector2.one,Vector2.one,close);
            button.pivot=Vector2.one;button.sizeDelta=new Vector2(44,44);button.anchoredPosition=new Vector2(-22,-22);
            FormationIcon(button,"close",new Vector2(.15f,.15f),new Vector2(.85f,.85f),Color.white);return button;
        }
    }
}
