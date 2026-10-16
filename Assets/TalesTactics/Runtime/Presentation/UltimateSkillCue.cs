using TMPro;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        UltimateSkillCue ultimateCue;
        public void ShowUltimateCue(CharacterData character,SkillData skill,float duration,bool reduced)
        {
            ClearUltimateCue();
            if(character==null||skill==null||!skill.IsUltimate||commands==null)return;
            // The command dock is outside the fitted battlefield and below the six ally portraits.
            var card=Panel("UltimateSkillCue",new Vector2(0,1),Vector2.one,new Vector2(20,-150),new Vector2(-20,-26));
            card.SetParent(commands,false);
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
    // A short presentation cue inside the idle command space, never a modal or a camera cut.
    public sealed class UltimateSkillCue:MonoBehaviour
    {
        BattleHud hud;CanvasGroup group;RectTransform rect;Vector2 resting;float age,duration;bool reduced;
        readonly List<TMP_Text> obscuredLabels=new List<TMP_Text>();
        Transform commandSpace;
        public void Initialize(BattleHud owner,CanvasGroup canvasGroup,float lifetime,bool low)
        {
            hud=owner;group=canvasGroup;rect=(RectTransform)transform;resting=rect.anchoredPosition;
            commandSpace=rect.parent;duration=Mathf.Clamp(lifetime,.08f,.65f);reduced=low;HideOverlappingLabels();UpdatePresentation();
        }
        void Update()
        {
            age+=Time.deltaTime;
            if(age>=duration){gameObject.SetActive(false);Destroy(gameObject);return;}
            UpdatePresentation();
        }
        void UpdatePresentation()
        {
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.10f));
            float leave=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-duration+.10f)/.10f));
            rect.anchoredPosition=resting+new Vector2(reduced?0:6*(1-enter),0);
            group.alpha=hud.InputModalOpen?0:reduced?1:enter*leave;
        }
        void OnEnable(){if(rect!=null)HideOverlappingLabels();}
        void OnDisable(){RestoreLabels();}
        void OnDestroy(){RestoreLabels();}
        void HideOverlappingLabels()
        {
            if(commandSpace==null)return;
            Canvas.ForceUpdateCanvases();var bounds=RelativeBounds(rect,commandSpace);
            foreach(Transform sibling in commandSpace)
            {
                // Only direct contextual text; nested button labels retain their normal behavior.
                var label=sibling.GetComponent<TMP_Text>();
                if(label==null||!label.enabled||!label.gameObject.activeSelf||!bounds.Overlaps(RelativeBounds(label.rectTransform,commandSpace)))continue;
                obscuredLabels.Add(label);label.enabled=false;
            }
        }
        void RestoreLabels()
        {
            foreach(var label in obscuredLabels)
                if(label!=null&&label.transform.parent==commandSpace&&label.gameObject.activeSelf)label.enabled=true;
            obscuredLabels.Clear();
        }
        static Rect RelativeBounds(RectTransform child,Transform parent)
        {
            var corners=new Vector3[4];child.GetWorldCorners(corners);
            var low=(Vector2)parent.InverseTransformPoint(corners[0]);var high=low;
            for(int i=1;i<4;i++){var p=(Vector2)parent.InverseTransformPoint(corners[i]);low=Vector2.Min(low,p);high=Vector2.Max(high,p);}
            return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
        }
    }
}
