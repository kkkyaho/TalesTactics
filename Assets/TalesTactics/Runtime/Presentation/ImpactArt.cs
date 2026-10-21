using UnityEngine;
namespace TalesTactics
{
    // Authored flipbook silhouettes with procedural motion; all phase values are local to the effect.
    public sealed partial class ImpactParticles
    {
        readonly Geometry art=new Geometry(),backAtlas=new Geometry();
        SkillVisualPattern pattern;bool compact,ultimate,travelling;float travelDuration;
        public SkillVisualPattern Pattern=>pattern;
        public bool Compact=>compact;
        public bool Travelling=>travelling;
        static Vector2 CellUV(int cell,float x,float y)
        {const float inset=.0015f;return new Vector2((cell%4)*.25f+inset+x*(.25f-inset*2),(3-cell/4)*.25f+inset+y*(.25f-inset*2));}
        void CelStroke(Vector3 a,Vector3 b,float width,Color color,bool highlight=false)
        {
            if(color.a<=.002f||(b-a).sqrMagnitude<.000001f)return;
            Ribbon(a,b,width*1.65f,SkillVisualLanguage.Ink(color));
            Ribbon(a,b,width,color);
            if(highlight)Ribbon(a,b,width*.26f,Alpha(new Color(1,.98f,.87f),color.a));
        }
        void CelSlash(Vector3 center,float radius,float angle,float sweep,float width,Color color)
        {
            int segments=reduced?12:20;
            for(int j=0;j<segments;j++)
            {
                float u=j/(float)segments,v=(j+1f)/segments;
                float a=angle+sweep*u,b=angle+sweep*v;
                float taper=Mathf.Pow(Mathf.Sin((u+v)*.5f*Mathf.PI),.65f);
                CelStroke(center+Plane(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.68f),
                    center+Plane(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius*.68f),width*taper,color,true);
            }
        }
        void CelPetal(Vector3 center,Vector3 axis,Vector3 side,Color color)
        {
            // A pointed, asymmetric leaf with a drawn contour and a single lit face.
            int n=solid.V.Count;var ink=SkillVisualLanguage.Ink(color);
            var p0=axis;var p1=axis*.12f+side;var p2=-axis*.72f;var p3=-axis*.15f-side*.65f;
            solid.Vertex(center+p0*1.17f,ink,Vector2.zero);solid.Vertex(center+p1*1.17f,ink,Vector2.zero);
            solid.Vertex(center+p2*1.17f,ink,Vector2.zero);solid.Vertex(center+p3*1.17f,ink,Vector2.zero);
            solid.Triangle(n,n+1,n+2);solid.Triangle(n,n+2,n+3);n=solid.V.Count;
            solid.Vertex(center+p0,color,Vector2.zero);solid.Vertex(center+p1,color,Vector2.zero);
            solid.Vertex(center+p2,color,Vector2.zero);solid.Vertex(center+p3,color,Vector2.zero);
            solid.Vertex(center,Alpha(Color.Lerp(color,Color.white,.70f),color.a),Vector2.zero);
            for(int i=0;i<4;i++)solid.Triangle(n+i,n+(i+1)%4,n+4);
        }
        void CelContact(Vector3 center,float phase,Color color,float radius=.65f)
        {
            float a=SkillVisualLanguage.ContactEnvelope(phase,.32f)*color.a;if(a<=0)return;
            int count=reduced?4:7;float opening=.12f+Mathf.Clamp01(phase/.12f)*.15f;
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count+.19f,reach=radius*(i%2==0?1:.68f);
                var direction=Plane(Mathf.Cos(angle),Mathf.Sin(angle));
                var across=Plane(-Mathf.Sin(angle),Mathf.Cos(angle));
                CelPetal(center+direction*(opening+reach*.32f),direction*reach*.38f,
                    across*(i%2==0?.052f:.035f),Alpha(color,a));
            }
        }
        void HealingFiligree(Vector3 center,float phase,float opacity)
        {
            var green=new Color(.35f,.92f,.64f,opacity*.78f);
            int ribbons=reduced?1:2,segments=reduced?10:16;
            for(int r=0;r<ribbons;r++)for(int j=0;j<segments;j++)
            {
                float u=j/(float)segments,v=(j+1f)/segments;
                float a=u*Mathf.PI*1.65f+phase*1.6f+r*Mathf.PI,b=v*Mathf.PI*1.65f+phase*1.6f+r*Mathf.PI;
                var p=center+Plane(Mathf.Cos(a)*.39f,.08f+u*.76f+phase*.24f);
                var q=center+Plane(Mathf.Cos(b)*.39f,.08f+v*.76f+phase*.24f);
                CelStroke(p,q,.014f*Mathf.Sin((u+v)*.5f*Mathf.PI),green,true);
            }
            for(int i=0;i<(reduced?3:6);i++)
            {
                float angle=i*2.39996f+phase*.7f,y=.14f+Mathf.Repeat(i*.19f+phase*.65f,.92f);
                var point=center+Plane(Mathf.Cos(angle)*(.32f+phase*.12f),y);
                CelPetal(point,Plane(Mathf.Sin(angle)*.045f,.068f),Plane(.036f,0),new Color(.89f,1,.73f,opacity*.8f));
            }
        }
        void FrameQuad(Geometry mesh,Vector3 center,Vector3 right,Vector3 up,Color tint,float frame,int first,int last)
        {
            frame=Mathf.Clamp(frame,first,last);int a=Mathf.FloorToInt(frame),b=Mathf.Min(a+1,last),n=mesh.V.Count;float blend=frame-a;
            mesh.Vertex(center-right-up,tint,CellUV(a,0,0),CellUV(b,0,0),blend);
            mesh.Vertex(center-right+up,tint,CellUV(a,0,1),CellUV(b,0,1),blend);
            mesh.Vertex(center+right+up,tint,CellUV(a,1,1),CellUV(b,1,1),blend);
            mesh.Vertex(center+right-up,tint,CellUV(a,1,0),CellUV(b,1,0),blend);
            mesh.Triangle(n,n+1,n+2);mesh.Triangle(n,n+2,n+3);
        }
        void Art(Vector3 center,float width,float height,int row,float phase,Color tint,float angle=0)
        {
            float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
            FrameQuad(art,center,Plane(c*width,s*width),Plane(-s*height,c*height),tint,row*4+Mathf.Clamp01(phase)*3,row*4,row*4+3);
        }
        public static bool HasAuthoredArt(SkillVisualPattern p,Element e,CombatFeedback feedback)
        {
            if(feedback==CombatFeedback.Heal||feedback==CombatFeedback.Revive)return true;
            if(feedback==CombatFeedback.Support)return p==SkillVisualPattern.Cleanse;
            // Preserve the shield crest over its fire impact instead of flattening it into the rifle finale.
            if(p==SkillVisualPattern.ShieldFinale)return false;
            return e==Element.Fire||e==Element.Ice||e==Element.Lightning||
                p==SkillVisualPattern.Wave||p==SkillVisualPattern.Meteor||p==SkillVisualPattern.SwordFinale||
                p==SkillVisualPattern.ClawFinale||p==SkillVisualPattern.FlameFinale||p==SkillVisualPattern.AstralRain||p==SkillVisualPattern.Rain;
        }
        void DrawCompact(float t)
        {
            var c=feedback==CombatFeedback.Strike?new Color(.8f,.91f,1):new Color(.42f,1,.74f);
            float fade=1-Mathf.SmoothStep(0,1,t);
            if(feedback==CombatFeedback.Strike)CelContact(Vector3.zero,age,Alpha(c,fade),.28f);
            else HealingFiligree(Plane(0,-.12f),t,fade*.65f);
            for(int i=0;i<5;i++){float a=i*2.39996f;var p=Plane(Mathf.Cos(a),Mathf.Sin(a))*(.08f+t*.45f);Ribbon(p,p+Plane(Mathf.Cos(a),Mathf.Sin(a))*.1f,.018f,Alpha(c,fade));}
        }
        void DrawAuthored(float t)
        {
            float fade=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,1,t)))*(reduced?.62f:1);
            if(feedback!=CombatFeedback.Strike)
            {
                float rise=Mathf.Sin(t*Mathf.PI)*.35f;
                GroundGlow(.7f+t*.2f,new Color(.14f,.77f,.51f,fade*.3f));
                Art(Plane(0,rise),.95f,1.15f,3,t,new Color(1,1,1,fade*.9f));
                for(int i=0;i<(reduced?4:10);i++)
                {float a=i*2.39996f+t*2.2f,y=Mathf.Repeat(i*.19f+t,1.3f)-.5f;Glow(Plane(Mathf.Cos(a)*.48f,y),.028f,new Color(.7f,1,.72f,fade*.7f));}
                return;
            }
            if(pattern==SkillVisualPattern.AstralRain||pattern==SkillVisualPattern.Rain)
            {
                var light=new Color(.8f,.92f,1,fade*.8f);GroundGlow(1,new Color(.48f,.66f,.9f,fade*.25f));
                for(int i=0;i<(reduced?3:8);i++)
                {float a=i*2.39996f,r=.18f+t*(.7f+i%3*.15f);var pos=Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.5f);Glow(pos,.11f,light);Ribbon(pos,pos+Plane(.04f,.25f*(1-t)),.018f,light);}
                return;
            }
            if(element==Element.Fire){FireImpact(t);if(HasWeaponSweep(style))WeaponSweep(Mathf.Lerp(.7f,1,t),Mathf.Max(0,1-t*2));return;}
            if(element==Element.Ice)
            {
                GroundGlow(.95f,new Color(.26f,.68f,1,fade*.34f));
                Art(Plane(0,.08f),1.15f,1.3f,1,t,new Color(1,1,1,fade));
                if(!reduced)for(int i=0;i<7;i++)
                {float a=i*2.39996f,r=.3f+t*.8f;Shard(Mathf.Cos(a)*r,Mathf.Sin(a)*r-t*t*.55f,.035f,.13f,a+t,new Color(.56f,.88f,1,fade*.8f));}
                return;
            }
            if(element==Element.Lightning)
            {
                GroundGlow(.8f,new Color(.36f,.51f,1,fade*.28f));
                Art(Plane(0,.18f),1.0f,1.4f,2,Mathf.Min(1,t*1.25f),new Color(1,1,1,fade*.88f));
                Glow(Vector3.zero,.28f,new Color(.8f,.88f,1,Mathf.Max(0,1-t*5)*.6f));
                return;
            }
            float axis=Mathf.Atan2(Vector3.Dot(-from,view.transform.up),Vector3.Dot(-from,view.transform.right));
            bool finale=ultimate||pattern==SkillVisualPattern.SwordFinale||pattern==SkillVisualPattern.ClawFinale;
            int cuts=finale&&!reduced?3:1;
            for(int i=0;i<cuts;i++)
            {
                float p=Mathf.Clamp01((t-i*.09f)*1.2f);if(t<i*.09f)continue;
                bool claw=pattern==SkillVisualPattern.ClawFinale;
                float angle=claw?axis-.65f:axis+(i==1?1.25f:i==2?-.7f:0);
                Art(Plane(claw?(i-1)*.22f:0,.04f+i*.05f),claw?.95f:1.2f+p*.3f,claw?.62f:.95f,0,p,claw?new Color(.9f,.58f,1,fade*.8f):new Color(.89f,.96f,1,fade*(i==0?1:.58f)),angle);
            }
            GroundDust(t);
        }
        public void Travel(Camera camera,Vector3 origin,Vector3 target,Element kind,CombatVisualStyle visual,float scale,bool isUltimate,bool low,float duration,SkillVisualPattern visualPattern,SkillSignature signature=SkillSignature.None,ArteProfile arte=default)
        {
            Play(camera,origin,target,kind,visual,CombatFeedback.Strike,scale,isUltimate,low,visualPattern,false,signature,arte);
            travelling=true;travelDuration=Mathf.Max(.01f,duration);Draw();
        }
        void DrawTravel()
        {
            if(signature!=SkillSignature.None){DrawSignatureTravel(Mathf.Clamp01(age/travelDuration));return;}
            if(arte.Enabled){DrawArteTravel(Mathf.Clamp01(age/travelDuration));return;}
            float p=Mathf.Clamp01(age/travelDuration),axis=Mathf.Atan2(Vector3.Dot(-from,view.transform.up),Vector3.Dot(-from,view.transform.right));
            float opacity=reduced?.7f:1;
            if(pattern==SkillVisualPattern.AstralRain||pattern==SkillVisualPattern.Rain)
            {
                for(int i=0;i<(reduced?2:6);i++)
                {
                    float x=(i-(reduced?.5f:2.5f))*.24f;var end=Plane(x,-.16f);
                    var point=Vector3.Lerp(end+Plane(.65f,2.8f+i%2*.3f),end,p*p);
                    Ribbon(point+Plane(.08f,.38f),point,.025f,new Color(.65f,.86f,1,opacity*.75f));
                    Glow(point,.065f,new Color(.95f,.96f,.8f,opacity));
                }
            }
            else if(pattern==SkillVisualPattern.Meteor)
            {
                int count=reduced?1:3;
                for(int i=0;i<count;i++)
                {
                    float phase=Mathf.Clamp01((p-i*.08f)/(1-i*.08f));
                    Vector3 start=Plane(-1.2f+i*.65f,3.8f+i*.4f),end=Plane((i-(count-1)*.5f)*.3f,0);
                    Vector3 point=Vector3.Lerp(start,end,phase*phase),trail=(start-end).normalized;
                    for(int j=4;j>=0;j--)Flame(point+trail*j*.17f,.18f+j*.045f,.25f+j*.06f,4+(i+j)%4,new Color(1,.9f,.73f,opacity*(1-j*.13f)));
                    Glow(point,.18f,new Color(1,.91f,.55f,opacity));
                }
            }
            else if(element==Element.Fire)FireStream(p,opacity,age);
            else
            {
                var point=Vector3.Lerp(from,Vector3.zero,p);
                if(pattern==SkillVisualPattern.Wave||pattern==SkillVisualPattern.SwordFinale)
                {Art(point,.72f,.88f,0,.18f+p*.2f,new Color(.8f,.94f,1,opacity),axis);GroundTravelDust(point,p);}
                else if(element==Element.Ice)Art(point,.45f,.65f,1,.2f+p*.12f,new Color(1,1,1,opacity),axis-Mathf.PI*.5f);
                else if(element==Element.Lightning)Art(point,.55f,.7f,2,p*.3f,new Color(1,1,1,opacity),axis-Mathf.PI*.5f);
                else
                {
                    var tail=Vector3.Lerp(from,Vector3.zero,Mathf.Max(0,p-.22f));
                    Ribbon(tail,point,style==CombatVisualStyle.Rifle?.025f:.045f,new Color(.72f,.89f,1,opacity));
                    Glow(point,.09f,new Color(.95f,.98f,1,opacity));
                }
            }
        }
        void GroundTravelDust(Vector3 point,float t)
        {for(int i=0;i<(reduced?1:3);i++)FrameQuad(backAtlas,point+Plane(-i*.15f,-.48f),Plane(.3f,0),Plane(0,.15f),new Color(.63f,.62f,.53f,.22f),12+t*3,12,15);}
    }
}
