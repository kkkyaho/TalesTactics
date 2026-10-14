using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        CombatFeedbackHud combatFeedback;
        public CombatFeedbackHud CombatFeedback
        {
            get
            {
                if(combatFeedback==null){combatFeedback=gameObject.AddComponent<CombatFeedbackHud>();combatFeedback.Initialize(battle,this);}
                return combatFeedback;
            }
        }
        public float ExperienceBottom=>SkillPanel?SkillCardHeight+28:battle.State is TargetSelectionState?targetContextHeight+28:battle.State is MoveSelectionState?168:18;
        public void ClearCombatFeedback(){if(combatFeedback!=null)combatFeedback.Clear();}
        public bool HasHealthFeedback(UnitRuntime unit)=>combatFeedback!=null&&combatFeedback.HasNumber(unit);
    }
    // Screen-space presentation only. Snapshot values never read later mutable unit growth.
    public sealed class CombatFeedbackHud:MonoBehaviour
    {
        sealed class NumberView{public TMP_Text Text;public UnitRuntime Unit;public float Age;public bool Cost;public int Lane;}
        readonly Queue<ExperienceGain> queue=new Queue<ExperienceGain>();
        readonly List<NumberView> numbers=new List<NumberView>();
        readonly Stack<TMP_Text> freeNumbers=new Stack<TMP_Text>();
        BattleDirector battle;BattleHud hud;RectTransform layer,card,fill;TMP_Text nameText,levelText,amountText,valueText,caption;
        UnityEngine.UI.RawImage portrait;CanvasGroup group;ExperienceGain current;float elapsed;
        public int PendingCount=>queue.Count+(current==null?0:1);
        public bool HasNumber(UnitRuntime unit){foreach(var n in numbers)if(n.Unit==unit)return true;return false;}
        public void Initialize(BattleDirector owner,BattleHud ui){battle=owner;hud=ui;}
        RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=low;r.offsetMax=high;return r;}
        TMP_Text Text(string name,Transform parent,string value,float size,Vector2 low,Vector2 high)
        {
            var r=Rect(name,parent,Vector2.zero,Vector2.zero,low,high);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=hud.Font;t.fontSize=size;t.text=value;t.color=new Color(.91f,.95f,.96f);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;return t;
        }
        void Ensure()
        {
            if(layer!=null)return;
            layer=Rect("Combat feedback overlay",transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var c=layer.gameObject.AddComponent<Canvas>();c.overrideSorting=true;c.sortingOrder=15;
            group=layer.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            card=Rect("Experience feedback",layer,Vector2.one,Vector2.one,Vector2.zero,new Vector2(340,98));
            var bg=card.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new Color(.045f,.08f,.12f,.96f);bg.raycastTarget=false;
            var edge=card.gameObject.AddComponent<UnityEngine.UI.Outline>();edge.effectColor=new Color(.49f,.59f,.57f,.8f);edge.effectDistance=new Vector2(1,-1);
            portrait=Rect("Experience portrait",card,Vector2.zero,Vector2.zero,new Vector2(12,22),new Vector2(66,76)).gameObject.AddComponent<UnityEngine.UI.RawImage>();portrait.raycastTarget=false;
            portrait.texture=Resources.Load<Texture2D>("TalesTactics/FormationPortraits");
            nameText=Text("Experience name",card,"",17,new Vector2(80,61),new Vector2(245,87));
            levelText=Text("Experience level",card,"",15,new Vector2(247,61),new Vector2(326,87));levelText.alignment=TextAlignmentOptions.Right;
            var track=Rect("Experience track",card,Vector2.zero,Vector2.zero,new Vector2(80,48),new Vector2(326,53));
            var image=track.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.17f,.24f,.27f);image.raycastTarget=false;
            fill=Rect("Experience fill",track,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);image=fill.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.56f,.79f,.74f);image.raycastTarget=false;
            valueText=Text("Experience value",card,"",12,new Vector2(80,23),new Vector2(184,43));
            amountText=Text("Experience gain",card,"",12,new Vector2(186,23),new Vector2(326,43));amountText.alignment=TextAlignmentOptions.Right;amountText.color=new Color(.88f,.76f,.5f);
            caption=Text("Experience caption",card,"",11,new Vector2(80,4),new Vector2(326,22));card.gameObject.SetActive(false);
        }
        public void Enqueue(ExperienceGain gain)
        {
            if(gain==null||gain.Amount<=0)return;Ensure();
            queue.Enqueue(new ExperienceGain{Unit=gain.Unit,Amount=gain.Amount,BeforeLevel=gain.BeforeLevel,BeforeEXP=gain.BeforeEXP,AfterLevel=gain.AfterLevel,AfterEXP=gain.AfterEXP});
        }
        public void Number(UnitRuntime unit,int delta,bool cost)
        {
            Ensure();TMP_Text t;
            if(freeNumbers.Count>0){t=freeNumbers.Pop();t.gameObject.SetActive(true);}else t=Text("HP feedback",layer,"",26,Vector2.zero,new Vector2(180,48));
            t.text=cost?"HP "+delta:delta>0?"+"+delta:Mathf.Abs(delta).ToString();t.fontSize=cost?18:26;t.fontStyle=FontStyles.Bold;
            t.alignment=TextAlignmentOptions.Center;t.color=cost?new Color(1,.75f,.5f):delta>0?new Color(.64f,.94f,.8f):new Color(1,.94f,.83f);
            t.outlineWidth=.08f;t.outlineColor=new Color(.025f,.035f,.05f,.8f);
            int lane=0;foreach(var n in numbers)if(n.Unit==unit)lane++;
            numbers.Add(new NumberView{Text=t,Unit=unit,Cost=cost,Lane=lane});
        }
        void Update()
        {
            if(layer==null)return;if(battle.Session==null){Clear();return;}
            bool hidden=hud.InputModalOpen||battle.StoryActive;group.alpha=hidden?0:1;
            if(hidden)return;
            float scale=hud.GetComponent<Canvas>().scaleFactor,dt=Time.deltaTime;
            for(int i=numbers.Count-1;i>=0;i--)
            {
                var n=numbers[i];n.Age+=dt;
                if(n.Age>=.85f){n.Text.gameObject.SetActive(false);freeNumbers.Push(n.Text);numbers.RemoveAt(i);continue;}
                var screen=battle.Board.BattleCamera.WorldToScreenPoint(battle.Board.HeadWorld(n.Unit));
                n.Text.alpha=screen.z<=0?0:1-Mathf.InverseLerp(.55f,.85f,n.Age);
                n.Text.rectTransform.pivot=new Vector2(.5f,.5f);
                n.Text.rectTransform.anchoredPosition=new Vector2(Mathf.Clamp(screen.x/scale+(n.Cost?-42:24),90,Screen.width/scale-90),Mathf.Clamp(screen.y/scale+28+n.Age*26+n.Lane*24,24,Screen.height/scale-90));
            }
            if(current==null&&queue.Count>0)
            {
                current=queue.Dequeue();elapsed=0;card.gameObject.SetActive(true);
                string[] ids={"cless","mint","farah","jade","tear","natalia","velvet","alphen","shionne","kisara"};int index=System.Array.IndexOf(ids,current.Unit.Data.Id);
                portrait.enabled=index>=0;portrait.uvRect=new Rect((index%5)*.2f,index<5?.5f:0,.2f,.5f);
                nameText.text=current.Unit.Data.DisplayName.Split(' ')[0];amountText.text="EXP +"+current.Amount;
            }
            if(current==null)return;
            float textScale=battle.Preferences?.TextScale??1;float w=Screen.width/scale;
            float factor=Mathf.Min(textScale,(w-24)/340);card.localScale=Vector3.one*factor;
            card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.pivot=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(340,98);card.anchoredPosition=Vector2.zero;
            elapsed+=dt;float duration=Mathf.Min(3,1.1f+(current.AfterLevel-current.BeforeLevel)*.22f);
            int total=current.BeforeEXP+Mathf.FloorToInt(current.Amount*Mathf.Clamp01(elapsed/duration));int level=Mathf.Min(50,current.BeforeLevel+total/100),exp=level==50?0:total%100;
            fill.anchorMax=new Vector2(level==50?1:exp/100f,1);valueText.text=level==50?"MAX":exp+" / 100";levelText.text="Lv. "+level;
            caption.text=level>current.BeforeLevel?"LEVEL UP  ·  "+current.BeforeLevel+" → "+level:"행동 경험치";
            if(elapsed>=duration+.7f){current=null;card.gameObject.SetActive(false);}
        }
        public void Clear()
        {queue.Clear();current=null;numbers.Clear();freeNumbers.Clear();if(layer!=null)Destroy(layer.gameObject);layer=null;}
    }
}
