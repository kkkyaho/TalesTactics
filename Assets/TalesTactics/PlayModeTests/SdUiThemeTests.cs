using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator SdThemeKeepsPaintedFramesInsideButtonsAndReadableFocusStates()
        {
            director.Hud.ShowIntermissionHub();yield return new WaitForEndOfFrame();
            Assert.That(SdUiTheme.PanelArt,Is.Not.Null);
            Assert.That(SdUiTheme.PanelArt.border,Is.EqualTo(Vector4.one*240),"SD artwork must retain its authored nine-slice when reimported");
            foreach(var panel in director.Hud.GetComponentsInChildren<OrnatePanel>())
                Assert.That(panel.GetComponent<Image>().sprite,Is.SameAs(SdUiTheme.PanelArt));
            var button=director.Hud.GetComponentsInChildren<Button>().First(b=>b.IsInteractable()&&b.GetComponent<HudButtonAccent>()!=null);
            var rect=(RectTransform)button.transform;var before=rect.rect;
            EventSystem.current.SetSelectedGameObject(button.gameObject);yield return new WaitForEndOfFrame();
            Assert.That(button.image.color,Is.EqualTo(SdUiTheme.Selected));
            Assert.That(UiContrast(SdUiTheme.Text,SdUiTheme.Selected),Is.GreaterThan(4.5f));
            var title=button.GetComponentInChildren<TMP_Text>();Assert.That(title.color,Is.EqualTo(SdUiTheme.Text));
            foreach(var trim in director.Hud.GetComponentsInChildren<OrnateButtonGraphic>())
            {
                Assert.That(trim.raycastTarget,Is.False);
                var frame=trim.transform.Find("Frame").GetComponent<Image>();
                Assert.That(frame.sprite,Is.SameAs(SdUiTheme.PanelArt));Assert.That(frame.fillCenter,Is.False);Assert.That(frame.raycastTarget,Is.False);
                Assert.That(frame.rectTransform.offsetMin,Is.EqualTo(Vector2.zero));Assert.That(frame.rectTransform.offsetMax,Is.EqualTo(Vector2.zero));
            }
            button.interactable=false;yield return new WaitForEndOfFrame();
            Assert.That(button.image.color,Is.EqualTo(SdUiTheme.Disabled));Assert.That(title.color,Is.EqualTo(SdUiTheme.Muted));
            Assert.That(UiContrast(SdUiTheme.Muted,SdUiTheme.Disabled),Is.GreaterThan(3));
            Assert.That(rect.rect,Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator SdIconMaterialPreservesUiStencilMaskAndOriginalAlphaContract()
        {
            director.Hud.ShowIntermissionHub();yield return null;
            var canvas=director.Hud.GetComponentInChildren<Canvas>();if(canvas==null)canvas=director.Hud.GetComponent<Canvas>();
            var maskObject=new GameObject("Theme mask probe",typeof(RectTransform),typeof(Image),typeof(Mask));maskObject.transform.SetParent(canvas.transform,false);
            var maskRect=(RectTransform)maskObject.transform;maskRect.sizeDelta=new Vector2(60,60);maskObject.GetComponent<Image>().raycastTarget=false;maskObject.GetComponent<Mask>().showMaskGraphic=false;
            var artObject=new GameObject("Theme icon probe",typeof(RectTransform),typeof(RawImage));artObject.transform.SetParent(maskObject.transform,false);
            var art=artObject.GetComponent<RawImage>();art.texture=Texture2D.whiteTexture;art.raycastTarget=false;art.rectTransform.sizeDelta=new Vector2(90,90);art.color=new Color(.8f,.8f,.8f,.35f);
            var originalColor=art.color;SdUiIconArt.Apply(art);yield return new WaitForEndOfFrame();
            Assert.That(art.material.shader.name,Is.EqualTo("TalesTactics/SD UI Ink"));Assert.That(art.material.shader.isSupported,Is.True);
            Assert.That(art.materialForRendering.GetFloat("_Stencil"),Is.GreaterThan(0));Assert.That(art.materialForRendering.HasProperty("_StencilComp"),Is.True);
            Assert.That(art.color,Is.EqualTo(originalColor));Assert.That(art.raycastTarget,Is.False);
            Object.Destroy(maskObject);yield return null;
        }
        [UnityTest] public IEnumerator SdButtonsKeepFlatNavySurfacesAndAvoidNestedPortraitFrames()
        {
            director.Hud.ShowIntermissionHub();yield return new WaitForEndOfFrame();
            var cards=director.Hud.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("거점 캐릭터:")).ToArray();
            Assert.That(cards.Length,Is.GreaterThan(0));
            foreach(var card in cards)
            {
                var trims=card.GetComponentsInChildren<OrnateButtonGraphic>();
                Assert.That(trims.Count(t=>t.NestedPortrait),Is.EqualTo(1));
                foreach(var trim in trims)
                {
                    var frame=trim.transform.Find("Frame").GetComponent<Image>();
                    Assert.That(frame.enabled,Is.EqualTo(!trim.NestedPortrait),"Clickable portrait cards have one outer frame");
                    Assert.That(frame.pixelsPerUnitMultiplier,Is.GreaterThanOrEqualTo(SdUiTheme.SlicePixels(frame.sprite)/10f));
                    var mesh=trim.canvasRenderer.GetMesh();
                    Assert.That(mesh==null||mesh.colors32.All(c=>c.r<30&&c.g<30&&c.b<30),Is.True,"No bright overlay may turn the flat navy surface into a grey bevel");
                }
            }
        }
        static float UiContrast(Color a,Color b)
        {
            float L(Color c){var v=c.linear;return .2126f*v.r+.7152f*v.g+.0722f*v.b;}
            float x=L(a),y=L(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);
        }
    }
}
