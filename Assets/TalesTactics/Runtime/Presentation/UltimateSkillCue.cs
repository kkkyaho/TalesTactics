using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        UltimateSkillCue ultimateCue;
        public bool UltimateCueVisible=>ultimateCue!=null&&ultimateCue.Visible;
        public Rect UltimateCueScreenBounds=>UltimateCueVisible?ultimateCue.ScreenBounds:default;
        public void ShowUltimateCue(CharacterData character,SkillData skill,float duration,bool reduced)
        {
            ClearUltimateCue();
            if(character==null||skill==null||!skill.IsUltimate||canvas==null)return;
            // Action execution hides the local command menu. The passive cut-in is an
            // independent HUD child in the same fixed upper band as ordinary skill names.
            var card=Panel("UltimateSkillCue",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(420,124));
            StatusPortrait(PlaceCueRect(card,"Ultimate portrait",new Vector2(14,20),new Vector2(94,104)),character);
            var name=Label(card,character.DisplayName.Split(' ')[0],15,28,17);
            Place(name.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(108,-43),new Vector2(-14,-15));
            name.color=new Color(.66f,.82f,.81f);name.textWrappingMode=TextWrappingModes.NoWrap;name.overflowMode=TextOverflowModes.Ellipsis;
            var title=Label(card,skill.DisplayName.Split('/')[0].Trim(),47,62,22);
            Place(title.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(108,-109),new Vector2(-14,-47));
            title.fontSizeMin=18;title.color=new Color(.92f,.84f,.65f);title.alignment=TextAlignmentOptions.MidlineLeft;
            foreach(var graphic in card.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            var group=card.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            ultimateCue=card.gameObject.AddComponent<UltimateSkillCue>();ultimateCue.Initialize(this,group,duration,reduced);
        }
        static RectTransform PlaceCueRect(Transform parent,string name,Vector2 low,Vector2 high)
        {
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;
            r.anchorMin=r.anchorMax=Vector2.zero;r.offsetMin=low;r.offsetMax=high;return r;
        }
        public void ClearUltimateCue(){if(ultimateCue!=null){ultimateCue.gameObject.SetActive(false);Destroy(ultimateCue.gameObject);}ultimateCue=null;}
    }
    // A short nonmodal portrait cue; it never needs the hidden command hierarchy to be active.
    [DefaultExecutionOrder(200)]
    public sealed class UltimateSkillCue:MonoBehaviour
    {
        BattleHud hud;CanvasGroup group;RectTransform rect;float age,duration;bool reduced;
        public bool Visible=>gameObject.activeInHierarchy&&group!=null&&group.alpha>.01f;
        public Rect ScreenBounds=>CombatLabelPlacement.ScreenRect(rect);
        public void Initialize(BattleHud owner,CanvasGroup canvasGroup,float lifetime,bool low)
        {
            hud=owner;group=canvasGroup;rect=(RectTransform)transform;
            duration=Mathf.Clamp(lifetime,.08f,.65f);reduced=low;Layout();UpdatePresentation();
        }
        void LateUpdate()
        {
            age+=Time.deltaTime;
            if(age>=duration){gameObject.SetActive(false);Destroy(gameObject);return;}
            Layout();UpdatePresentation();
        }
        void Layout()
        {
            float scale=Mathf.Max(.001f,hud.GetComponent<Canvas>().scaleFactor);
            var safe=CombatLabelPlacement.Viewport(hud);
            float width=Mathf.Min(420,safe.width/scale-16),height=Mathf.Min(124,safe.height/scale-16);
            rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,1);
            rect.sizeDelta=new Vector2(width,height);
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.10f));
            rect.anchoredPosition=new Vector2(safe.center.x/scale+(reduced?0:6*(1-enter)),safe.yMax/scale-8);
        }
        void UpdatePresentation()
        {
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.10f));
            float leave=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-duration+.10f)/.10f));
            group.alpha=hud.InputModalOpen||hud.TitleOpen?0:reduced?1:enter*leave;
        }
    }
}
