using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HealthBarGraphic:UnityEngine.UI.MaskableGraphic
    {
        public float Fraction {get;private set;}
        public float Trail {get;private set;}
        public Color Fill=>fill;
        Color fill;
        public void Set(float value,float trail,Color tint)
        {if(Fraction==value&&Trail==trail&&fill==tint)return;Fraction=value;Trail=trail;fill=tint;SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Quad(vh,r,new Color(.025f,.04f,.065f,.98f));
            // A narrow brass seat and the authored laurel trim share the rest of the HUD's materials.
            r=new Rect(r.x+2,r.y+2,r.width-4,r.height-4);
            Quad(vh,r,new Color(.34f,.29f,.20f,.95f));
            r=new Rect(r.x+1,r.y+1,r.width-2,r.height-2);
            Quad(vh,r,new Color(.025f,.05f,.065f,.98f));
            Quad(vh,new Rect(r.x,r.y,r.width*Trail,r.height),Color.Lerp(fill,new Color(.43f,.39f,.32f),.65f));
            Quad(vh,new Rect(r.x,r.y,r.width*Fraction,r.height),fill);
            Quad(vh,new Rect(r.x,r.y+r.height*.62f,r.width*Fraction,r.height*.38f),Color.Lerp(fill,new Color(.70f,.77f,.72f),.22f));
        }
        static void Quad(UnityEngine.UI.VertexHelper vh,Rect r,Color c)
        {
            if(r.width<=0||r.height<=0)return;
            int n=vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);
        }
    }
    [DefaultExecutionOrder(300)]
    public sealed class UnitHealthDisplay:MonoBehaviour
    {
        public const float RecentChangeSeconds=3;
        public static readonly Color AllyTint=new Color32(73,157,151,255);
        public static readonly Color EnemyTint=new Color32(169,78,82,255);
        BattleDirector battle;UnitRuntime unit;Transform visual;
        RectTransform rect;HealthBarGraphic bar;OrnateButtonGraphic frame;
        int lastHP;float trail,changedAt=float.NegativeInfinity,scale;
        public HealthBarGraphic Bar=>bar;
        public UnitRuntime Unit=>unit;
        public BattleHud Hud=>battle.Hud;
        // Exact names and HP are read in the fixed status card, never duplicated over a sprite.
        public bool Detailed=>false;
        public bool RecentlyChanged=>Time.time-changedAt<RecentChangeSeconds;
        public Vector2 ScreenOrigin {get;private set;}
        public Rect ScreenBounds=>new Rect(rect.anchoredPosition.x-OccupiedSize.x*.5f,rect.anchoredPosition.y,OccupiedSize.x,OccupiedSize.y);
        public Vector2 OccupiedSize=>rect.rect.size+new Vector2(6,4)*scale;
        public int Priority=>Mathf.Max(battle.Board.PresentationLabelPriority(unit),battle.HoveredUnit==unit||battle.HealthHoveredUnit==unit||battle.InspectedUnit==unit?3:battle.Session.Active==unit?2:RecentlyChanged?1:0);
        public void Compact(){rect.sizeDelta=new Vector2(50*scale,11*scale);}
        public void Suppress(){bar.enabled=false;if(frame!=null)frame.gameObject.SetActive(false);}
        public void Place(Vector2 position,Vector2 origin){rect.anchoredPosition=position;}
        public void Initialize(BattleDirector owner,UnitRuntime model,Transform position)
        {
            battle=owner;unit=model;visual=position;lastHP=unit.CurrentHP;trail=Fraction;
            rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);
            bar=gameObject.AddComponent<HealthBarGraphic>();bar.raycastTarget=false;
            frame=OrnateButtonGraphic.Create(transform,true);
        }
        float Fraction=>Mathf.Clamp01((float)unit.CurrentHP/Mathf.Max(1,unit.Stats.HP));
        void LateUpdate()
        {
            if(battle==null||battle.Session==null||visual==null)return;
            float f=Fraction;
            if(lastHP!=unit.CurrentHP)
            {
                trail=unit.CurrentHP<lastHP?Mathf.Max(trail,(float)lastHP/Mathf.Max(1,unit.Stats.HP)):f;
                lastHP=unit.CurrentHP;changedAt=Time.time;
            }
            if(Time.time-changedAt>.3f)trail=Mathf.MoveTowards(trail,f,Time.deltaTime*1.8f);
            var screen=battle.Board.UnitHeadScreenOrigin(unit);ScreenOrigin=new Vector2(screen.x,screen.y);
            var safe=CombatLabelPlacement.Viewport(battle.Hud);
            // Do not pin off-screen health bars to an edge: they would look attached to another unit.
            bool relevant=Priority>0||f<1;
            bool visible=relevant&&unit.Alive&&!unit.CrossoverReserve&&screen.z>0&&safe.Contains(ScreenOrigin)
                &&!battle.StoryActive&&!battle.Hud.InputModalOpen&&!(battle.State is BattleEndState);
            bar.enabled=visible;frame.gameObject.SetActive(visible);
            scale=Mathf.Clamp(Screen.height/1080f,.8f,1.5f);
            rect.sizeDelta=new Vector2(68*scale,12*scale);rect.anchoredPosition=ScreenOrigin;
            // Team hue is preserved even at critical health. Empty gauge area carries the urgency.
            bar.Set(f,Mathf.Clamp01(trail),unit.Team==Team.Player?AllyTint:EnemyTint);
        }
    }
}
