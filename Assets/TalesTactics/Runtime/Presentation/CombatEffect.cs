using UnityEngine;
namespace TalesTactics
{
    public enum CombatFeedback { Strike, Heal, Revive, Support }

    // One short-lived effect per actual recipient. All renderers share the board material.
    public sealed partial class CombatEffect:MonoBehaviour
    {
        public CombatVisualStyle Style {get;private set;}
        public CombatFeedback Feedback {get;private set;}
        public UnitRuntime Recipient {get;private set;}
        public bool Ultimate {get;private set;}
        readonly LineRenderer[] lines=new LineRenderer[12];
        public SkillVisualPattern Pattern {get;private set;}
        int pulses=1;
        float size=1;
        Camera view;
        Vector3 origin,target;
        Color tint;
        float age,contactHold;bool authored,compact;
        public const float Lifetime=0.6f;
        public void Initialize(Material material,Camera camera,CombatVisualStyle style,CombatFeedback feedback,
            UnitRuntime recipient,Vector3 from,Vector3 to,Color color,bool ultimate,SkillPresentation presentation=null,Element element=Element.None,bool reduced=false,bool compact=false,SkillSignature signature=SkillSignature.None)
        {
            this.compact=compact;authored=signature!=SkillSignature.None||ImpactParticles.HasAuthoredArt(presentation?.Pattern??SkillVisualPattern.Automatic,element,feedback);
            Style=style;Feedback=feedback;Recipient=recipient;Ultimate=ultimate;
            Pattern=presentation?.Pattern??SkillVisualPattern.Automatic;
            pulses=Mathf.Clamp(presentation?.Pulses??1,1,6);size=SafeSize(presentation?.Size??1);
            view=camera;origin=from;target=to;tint=color;
            contactHold=feedback==CombatFeedback.Strike?CombatMotionProfile.For(style,ultimate).Hold:0;
            var parent=transform.parent!=null?transform.parent:transform;
            var pool=parent.GetComponent<ImpactPool>()??parent.gameObject.AddComponent<ImpactPool>();
            pool.Play(camera,from,to,element,style,feedback,compact?.65f:size,ultimate&&!compact,reduced,Pattern,compact,signature);
            if(authored||compact)return;
            for(int i=0;i<lines.Length;i++)
            {
                var child=new GameObject("Stroke "+i);child.transform.SetParent(transform,false);
                var line=child.AddComponent<LineRenderer>();lines[i]=line;
                line.sharedMaterial=material;line.startWidth=line.endWidth=ultimate?0.065f:0.035f;
                line.numCapVertices=2;line.sortingOrder=20;line.positionCount=0;
            }
            Draw(0);
        }
        void Update()
        {
            age+=Time.deltaTime;
            if(age>=Lifetime){Destroy(gameObject);return;}
            Draw(Mathf.Max(0,age-contactHold)/Mathf.Max(.01f,Lifetime-contactHold));
        }
        Vector3 Plane(float x,float y)=>target+view.transform.right*x+view.transform.up*y;
        void Stroke(int index,params Vector3[] points)
        {lines[index].positionCount=points.Length;lines[index].SetPositions(points);}
        void Arc(int index,float radius,float start,float sweep,float offsetY=0,int segments=20)
        {
            var line=lines[index];line.positionCount=segments+1;
            for(int i=0;i<=segments;i++)
            {float angle=(start+sweep*i/segments)*Mathf.Deg2Rad;line.SetPosition(i,Plane(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius+offsetY));}
        }
        void Draw(float t)
        {
            if(view==null||authored||compact)return;
            var color=tint;color.a=Mathf.Clamp01((1-t)*1.8f);
            foreach(var line in lines){line.positionCount=0;line.startColor=line.endColor=color;}
            if(Pattern!=SkillVisualPattern.Automatic&&Pattern!=SkillVisualPattern.CharacterStyle&&Feedback!=CombatFeedback.Heal&&Feedback!=CombatFeedback.Revive)
            {DrawPattern(t);return;}
            float radius=Mathf.Lerp(0.12f,Ultimate?0.95f:0.5f,t);
            if(Feedback==CombatFeedback.Heal||Feedback==CombatFeedback.Revive)
            {
                Arc(0,radius,0,360);Stroke(1,Plane(-0.2f,t*0.4f),Plane(0.2f,t*0.4f));
                Stroke(2,Plane(0,-0.2f+t*0.4f),Plane(0,0.2f+t*0.4f));
                if(Feedback==CombatFeedback.Revive)
                {Stroke(3,Plane(-0.25f,-0.6f),Plane(-0.25f,0.8f));Stroke(4,Plane(0.25f,-0.6f),Plane(0.25f,0.8f));}
            }
            else if(Feedback==CombatFeedback.Support)
            {
                Arc(0,radius,30,360,0,6);Arc(1,radius*0.65f,-30,360,0,6);
            }
            else switch(Style)
            {
                case CombatVisualStyle.Sword:
                    Arc(0,radius,-120+t*80,160);Arc(1,radius*0.8f,60-t*80,130);break;
                case CombatVisualStyle.Sacred:
                    Arc(0,radius,0,360);Stroke(1,Plane(-radius,0),Plane(radius,0));
                    Stroke(2,Plane(0,-radius),Plane(0,radius));break;
                case CombatVisualStyle.Claw:
                    for(int i=0;i<3;i++){float offset=(i-1)*0.18f;Stroke(i,Plane(-radius+offset,radius),Plane(radius+offset,-radius));}break;
                case CombatVisualStyle.Martial:
                    Arc(0,radius,0,360);Arc(1,radius*0.65f,0,360);
                    for(int i=0;i<4;i++){float a=i*Mathf.PI/2;Stroke(i+2,Plane(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),Plane(Mathf.Cos(a)*(radius+0.15f),Mathf.Sin(a)*(radius+0.15f)));}break;
                case CombatVisualStyle.Song:
                    for(int j=0;j<3;j++)
                    {
                        lines[j].positionCount=21;
                        for(int i=0;i<=20;i++){float x=(i/20f-0.5f)*1.1f;lines[j].SetPosition(i,Plane(x,Mathf.Sin(x*10-t*12)*0.09f+(j-1)*0.2f));}
                    }break;
                case CombatVisualStyle.Spear:
                    var direction=(target-origin).normalized;
                    if(direction.sqrMagnitude<0.1f)direction=view.transform.up;
                    var side=Vector3.Cross(view.transform.forward,direction).normalized;
                    Stroke(0,target-direction*(0.7f+radius),target+direction*radius);
                    Stroke(1,target+direction*radius-direction*0.2f+side*0.1f,target+direction*radius,target+direction*radius-direction*0.2f-side*0.1f);break;
                case CombatVisualStyle.Bow:
                    Arc(0,radius*.55f,0,360);Ray(1,30,.06f,radius);Ray(2,210,.06f,radius);break;
                case CombatVisualStyle.FlameSword:
                    Arc(0,radius,-140+t*70,230);
                    for(int i=0;i<3;i++){float x=(i-1)*0.22f;Stroke(i+1,Plane(x,-0.25f),Plane(x-0.07f,0.1f+radius*0.3f),Plane(x+0.06f,radius));}break;
                case CombatVisualStyle.Rifle:
                    Arc(0,radius*.4f,0,360);
                    Stroke(1,Plane(-radius,-radius),Plane(radius,radius));Stroke(2,Plane(-radius,radius),Plane(radius,-radius));break;
                case CombatVisualStyle.Shield:
                    Arc(0,radius,30,360,0,6);Arc(1,radius*0.72f,30,360,0,6);
                    Stroke(2,Plane(0,-radius),Plane(0,radius));break;
                default:Arc(0,radius,0,360);break;
            }
            if(Ultimate)
            {
                Arc(5,radius*1.2f,t*90,360,0,6);
                Arc(6,radius*0.95f,-t*90,360,0,3);
            }
        }
        public static CombatVisualStyle Resolve(CharacterData character,SkillData skill)
        {
            if(skill.VisualStyle!=CombatVisualStyle.Automatic)return skill.VisualStyle;
            if(character.VisualStyle!=CombatVisualStyle.Automatic)return character.VisualStyle;
            switch(character.Weapon)
            {
                case WeaponType.Staff:return CombatVisualStyle.Sacred;
                case WeaponType.Claw:return CombatVisualStyle.Claw;
                case WeaponType.Fist:return CombatVisualStyle.Martial;
                case WeaponType.Spear:return CombatVisualStyle.Spear;
                case WeaponType.Bow:return CombatVisualStyle.Bow;
                case WeaponType.Gun:return CombatVisualStyle.Rifle;
                case WeaponType.Shield:return CombatVisualStyle.Shield;
                default:return CombatVisualStyle.Sword;
            }
        }
    }
}
