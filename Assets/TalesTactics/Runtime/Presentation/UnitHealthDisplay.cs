using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace TalesTactics
{
    // A displaced health bar retains a visual link to its unit in crowded formations.
    public sealed class HealthBarLink:MaskableGraphic
    {
        Vector2 endpoint;
        public void Set(Vector2 target,Color tint)
        {
            bool visible=target.sqrMagnitude>16;
            if(endpoint==target&&color==tint&&enabled==visible)return;
            endpoint=target;color=tint;enabled=visible;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var start=Vector2.zero;var bend=new Vector2(endpoint.x,endpoint.y*.45f);
            Segment(vh,start,bend);Segment(vh,bend,endpoint);
        }
        void Segment(VertexHelper vh,Vector2 start,Vector2 end)
        {
            if((end-start).sqrMagnitude<.01f)return;
            var normal=new Vector2(-(end-start).y,(end-start).x).normalized*.65f;int n=vh.currentVertCount;
            vh.AddVert(start-normal,color,Vector2.zero);vh.AddVert(start+normal,color,Vector2.zero);
            vh.AddVert(end+normal,color,Vector2.zero);vh.AddVert(end-normal,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HealthBarGraphic:MaskableGraphic
    {
        public float Fraction {get;private set;}
        public float Trail {get;private set;}
        Color fill;
        public void Set(float value,float trail,Color tint)
        {if(Fraction==value&&Trail==trail&&fill==tint)return;Fraction=value;Trail=trail;fill=tint;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Quad(vh,r,new Color(.30f,.40f,.48f,.95f));
            r=new Rect(r.x+1,r.y+1,r.width-2,r.height-2);
            Quad(vh,r,new Color(.025f,.04f,.07f,.98f));
            Quad(vh,new Rect(r.x,r.y,r.width*Trail,r.height),new Color(.94f,.77f,.43f));
            Quad(vh,new Rect(r.x,r.y,r.width*Fraction,r.height),fill);
            for(int i=1;i<4;i++)Quad(vh,new Rect(r.x+r.width*i/4f,r.y,1,2),new Color(.75f,.83f,.86f,.65f));
        }
        static void Quad(VertexHelper vh,Rect r,Color c)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);
        }
    }
    public sealed class UnitHealthDisplay:MonoBehaviour
    {
        BattleDirector battle;UnitRuntime unit;Transform visual;
        RectTransform rect;HealthBarGraphic bar;TMP_Text label;HealthBarLink link;
        int lastHP;float trail,changedAt;
        public HealthBarGraphic Bar=>bar;
        public bool Detailed=>label!=null&&label.enabled;
        public Vector2 OccupiedSize=>Detailed?new Vector2(label.rectTransform.rect.width,rect.rect.height+label.rectTransform.offsetMax.y+4):rect.rect.size+new Vector2(6,5);
        public int Priority=>battle.Session.Active==unit?2:battle.HoveredUnit==unit||battle.HealthHoveredUnit==unit?1:0;
        public void Place(Vector2 position,Vector2 origin)
        {
            rect.anchoredPosition=position;
            link.Set(origin-position,unit.Team==Team.Player?new Color(.35f,.78f,.76f,.72f):new Color(.85f,.46f,.43f,.72f));
            link.enabled=bar.enabled&&link.enabled;
        }
        public void Initialize(BattleDirector owner,UnitRuntime model,Transform position)
        {
            battle=owner;unit=model;visual=position;lastHP=unit.CurrentHP;trail=Fraction;
            rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);
            bar=gameObject.AddComponent<HealthBarGraphic>();bar.raycastTarget=false;
            var lead=new GameObject("Health association",typeof(RectTransform),typeof(HealthBarLink));lead.transform.SetParent(transform,false);
            link=lead.GetComponent<HealthBarLink>();link.raycastTarget=false;link.enabled=false;
            link.rectTransform.anchorMin=link.rectTransform.anchorMax=new Vector2(.5f,0);link.rectTransform.sizeDelta=Vector2.one;
            var text=new GameObject("Health value",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(transform,false);
            label=text.GetComponent<TMP_Text>();label.font=battle.Hud.Font;label.fontSize=14;label.alignment=TextAlignmentOptions.Center;
            label.color=new Color(.93f,.96f,1);label.raycastTarget=false;
            label.outlineWidth=.18f;label.outlineColor=new Color(0,0,0,.9f);
            label.rectTransform.anchorMin=new Vector2(0,1);label.rectTransform.anchorMax=Vector2.one;label.rectTransform.pivot=new Vector2(.5f,0);
            label.rectTransform.offsetMin=new Vector2(-44,2);label.rectTransform.offsetMax=new Vector2(44,38);
        }
        float Fraction=>Mathf.Clamp01((float)unit.CurrentHP/Mathf.Max(1,unit.Stats.HP));
        void LateUpdate()
        {
            if(battle==null||visual==null)return;
            float f=Fraction;
            if(lastHP!=unit.CurrentHP){trail=unit.CurrentHP<lastHP?Mathf.Max(trail,(float)lastHP/unit.Stats.HP):f;lastHP=unit.CurrentHP;changedAt=Time.unscaledTime;}
            if(Time.unscaledTime-changedAt>.3f)trail=Mathf.MoveTowards(trail,f,Time.unscaledDeltaTime*1.8f);
            var cam=battle.Board.BattleCamera;
            float height=unit.Data.Sprites?.Front!=null?unit.Data.Sprites.Front.bounds.size.y*CharacterVisualSizing.Scale(unit.Data):1.3f;
            var screen=cam.WorldToScreenPoint(visual.position+cam.transform.up*(height+.18f));
            bool visible=unit.Alive&&!unit.CrossoverReserve&&screen.z>0&&!battle.StoryActive&&!battle.Hud.InputModalOpen;
            bar.enabled=visible;if(!visible)link.enabled=false;label.enabled=visible&&!(battle.State is FacingSelectionState)&&!battle.Hud.HasHealthFeedback(unit)&&(battle.Session.Active==unit||battle.HoveredUnit==unit||battle.HealthHoveredUnit==unit);
            float scale=Mathf.Clamp(Screen.height/1080f,.8f,1.5f);
            rect.sizeDelta=new Vector2(70*scale,10*scale);rect.anchoredPosition=new Vector2(Mathf.Clamp(screen.x,36*scale,Screen.width-36*scale),Mathf.Clamp(screen.y,8,Screen.height-24*scale));
            label.fontSize=13*scale;label.text=unit.Data.DisplayName.Split(' ')[0]+"\n"+unit.CurrentHP+" / "+unit.Stats.HP;
            Color tint=f<=.25f?new Color(1,.71f,.29f):unit.Team==Team.Player?new Color(.25f,.82f,.78f):new Color(.92f,.39f,.34f);
            bar.Set(f,Mathf.Clamp01(trail),tint);
        }
    }
}
