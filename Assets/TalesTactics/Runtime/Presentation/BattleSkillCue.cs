using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        BattleSkillCue battleSkillCue;
        public bool BattleSkillCueVisible=>battleSkillCue!=null&&battleSkillCue.Visible;
        public Rect BattleSkillCueScreenBounds=>BattleSkillCueVisible?battleSkillCue.ScreenBounds:default;
        public void ShowBattleSkillCue(UnitRuntime caster,SkillData skill,float duration)
        {
            ClearBattleSkillCue();
            // Ultimates already have a portrait cue in the command dock. Never duplicate it.
            if(caster==null||skill==null||skill.IsUltimate||skill.Animation==AnimationKind.Attack)return;
            var card=Panel("BattleSkillCue",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(480,78));
            card.GetComponent<Image>().raycastTarget=false;
            var actor=Label(card,(caster.Team==Team.Player?"아군 · ":"적군 · ")+caster.Data.DisplayName.Split(' ')[0],8,24,16);
            actor.gameObject.name="Battle skill actor";actor.color=new Color(.65f,.85f,.83f);
            var title=Label(card,skill.DisplayName,32,36,22);title.gameObject.name="Battle skill title";
            title.color=new Color(.94f,.96f,.98f);title.alignment=TextAlignmentOptions.Midline;
            actor.alignment=TextAlignmentOptions.Midline;actor.textWrappingMode=TextWrappingModes.NoWrap;
            title.textWrappingMode=TextWrappingModes.NoWrap;title.enableAutoSizing=true;title.fontSizeMin=17;title.fontSizeMax=22;
            var group=card.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            foreach(var graphic in card.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            battleSkillCue=card.gameObject.AddComponent<BattleSkillCue>();battleSkillCue.Initialize(battle,this,actor,title,group,duration);
        }
        public void ClearBattleSkillCue()
        {if(battleSkillCue!=null){battleSkillCue.gameObject.SetActive(false);Destroy(battleSkillCue.gameObject);}battleSkillCue=null;}
    }
    // A passive, fixed viewport band. Readable dwell never delays an action or queues old skills.
    [DefaultExecutionOrder(200)]
    public sealed class BattleSkillCue:MonoBehaviour
    {
        BattleDirector battle;BattleHud hud;RectTransform rect;TMP_Text actor,title;CanvasGroup group;float age,duration;
        public bool Visible=>gameObject.activeInHierarchy&&group!=null&&group.alpha>.01f;
        public Rect ScreenBounds=>CombatLabelPlacement.ScreenRect(rect);
        public void Initialize(BattleDirector owner,BattleHud ui,TMP_Text actorText,TMP_Text titleText,CanvasGroup canvasGroup,float lifetime)
        {battle=owner;hud=ui;actor=actorText;title=titleText;group=canvasGroup;rect=(RectTransform)transform;duration=Mathf.Clamp(lifetime,.95f,2.4f);Layout();}
        void LateUpdate()
        {
            if(battle.Session==null||hud.TitleOpen||battle.State is BattleEndState){gameObject.SetActive(false);Destroy(gameObject);return;}
            bool hidden=hud.InputModalOpen||battle.StoryActive||battle.TimingActive;group.alpha=hidden?0:1;
            if(hidden)return;
            age+=Time.deltaTime;if(age>=duration){gameObject.SetActive(false);Destroy(gameObject);return;}Layout();
        }
        void Layout()
        {
            float scale=Mathf.Max(.001f,hud.GetComponent<Canvas>().scaleFactor),textScale=battle.Preferences?.TextScale??1;
            var safe=CombatLabelPlacement.Viewport(hud);float width=Mathf.Min(520*scale,safe.width-16),height=Mathf.Min(82*scale*textScale,safe.height*.3f);
            rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,1);
            rect.sizeDelta=new Vector2(width/scale,height/scale);rect.anchoredPosition=new Vector2(safe.center.x/scale,(safe.yMax-8)/scale);
            actor.fontSize=16*textScale;title.fontSizeMax=22*textScale;title.fontSizeMin=17*textScale;
            actor.rectTransform.anchorMin=new Vector2(0,.58f);actor.rectTransform.anchorMax=Vector2.one;actor.rectTransform.offsetMin=new Vector2(8,0);actor.rectTransform.offsetMax=new Vector2(-8,-2);
            title.rectTransform.anchorMin=Vector2.zero;title.rectTransform.anchorMax=new Vector2(1,.62f);title.rectTransform.offsetMin=new Vector2(8,2);title.rectTransform.offsetMax=new Vector2(-8,0);
        }
    }
}
