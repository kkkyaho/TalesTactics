using UnityEngine;
namespace TalesTactics
{
    // Shared preparation/contact geometry. No simulation, random, camera zoom or timeScale writes.
    public sealed partial class ImpactParticles
    {
        bool preparing;float prepareDuration;Transform casterAnchor;
        public bool Preparing=>preparing;
        public static bool HasWeaponSweep(CombatVisualStyle value)=>value==CombatVisualStyle.Sword||value==CombatVisualStyle.FlameSword||value==CombatVisualStyle.Claw;
        public void Prepare(Camera camera,Transform caster,Vector3 target,Element kind,CombatVisualStyle visual,float scale,bool ultimate,bool low,float duration,SkillVisualPattern visualPattern=SkillVisualPattern.Automatic,SkillSignature signature=SkillSignature.None)
        {
            Play(camera,caster.position+camera.transform.up*.65f,target,kind,visual,CombatFeedback.Strike,scale,ultimate,low,visualPattern,false,signature);
            preparing=true;casterAnchor=caster;prepareDuration=Mathf.Max(.01f,duration);Draw();
        }
        void DrawPreparation()
        {
            if(casterAnchor==null){gameObject.SetActive(false);return;}
            from=casterAnchor.position+view.transform.up*.65f-transform.position;
            float p=Mathf.Clamp01(age/prepareDuration);
            if(signature!=SkillSignature.None){DrawSignaturePreparation(p);return;}
            if(element==Element.Fire)
            {
                float radius=.13f+.30f*Mathf.Sin(p*Mathf.PI*.75f);
                Glow(from,radius*2.3f,new Color(1,.25f,.02f,(reduced?.2f:.45f)*Mathf.Sin(p*Mathf.PI)));
                Flame(from,radius,radius*1.2f,Mathf.Min(5,(int)(p*6)),new Color(1,.93f,.7f,.9f));
                for(int i=0;i<(reduced?5:11);i++)
                {
                    float a=i*2.39996f+p*5,r=.7f*(1-p)+.16f;
                    Glow(from+Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r),.035f,new Color(1,.58f,.13f,.85f));
                }

            }
            if(element!=Element.Fire&&!HasWeaponSweep(style))
            {
                var color=element==Element.Ice?new Color(.35f,.8f,1):element==Element.Lightning?new Color(.6f,.63f,1):new Color(.35f,1,.69f);
                int row=element==Element.Ice?1:element==Element.Lightning?2:3;
                Art(from+Plane(0,-.12f),.45f+p*.25f,.65f+p*.15f,row,p*.18f,Alpha(color,Mathf.Sin(p*Mathf.PI)*.65f));
                for(int i=0;i<(reduced?3:7);i++){float a=i*2.39996f+p*3,r=.65f*(1-p)+.12f;Glow(from+Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r),.035f,Alpha(color,.5f));}
            }
            if(HasWeaponSweep(style)&&p>.58f)WeaponSweep(Mathf.Lerp(.05f,.68f,Mathf.InverseLerp(.58f,1,p)),Mathf.InverseLerp(.58f,.78f,p));
        }
        void Glow(Vector3 center,float radius,Color tint)
        {
            int n=solid.V.Count;solid.Vertex(center,tint,Vector2.one*.5f);
            for(int i=0;i<=24;i++)
            {float a=i*Mathf.PI/12;solid.Vertex(center+Plane(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),Alpha(tint,0),Vector2.zero);if(i>0)solid.Triangle(n,n+i,n+i+1);}
        }
        void Flame(Vector3 center,float width,float height,float frame,Color tint)
        {FrameQuad(atlas,center,Plane(width,0),Plane(0,height),tint,frame,0,11);}
        void FireStream(float head,float opacity,float time)
        {
            if(from.sqrMagnitude<.16f)return;
            var axis=(-from).normalized;var side=Vector3.Cross(view.transform.forward,axis).normalized;
            int count=reduced?7:13;
            for(int i=0;i<count;i++)
            {
                float p=(i+.5f)/count;if(p>head)continue;
                var center=Vector3.Lerp(from,Vector3.zero,p)+view.transform.up*(Mathf.Sin(p*17-time*20)*.12f*size);
                float width=(.32f+p*.32f)*size;
                float frame=3+Mathf.Repeat(i+time*18,5);
                FrameQuad(atlas,center,side*width,axis*(.54f*size),new Color(1,.95f,.78f,opacity*(reduced?.5f:.72f)),frame,0,11);
                if(i>0&&i%3==0)Ribbon(center-axis*.18f,center+axis*.18f,.045f,new Color(1,.94f,.6f,opacity*.3f));
            }
        }
        void GroundGlow(float radius,Color tint)
        {
            int n=solid.V.Count;var center=-view.transform.up*.65f+Vector3.up*.035f;solid.Vertex(center,tint,Vector2.one*.5f);
            for(int i=0;i<=32;i++)
            {float a=i*Mathf.PI/16;solid.Vertex(center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*size,Alpha(tint,0),Vector2.zero);if(i>0)solid.Triangle(n,n+i,n+i+1);}
        }
        void FireImpact(float t)
        {
            float flameFade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.88f,t));
            float strength=reduced?.66f:1;
            GroundGlow(1.35f,new Color(1,.32f,.025f,flameFade*.65f*strength));
            Glow(Plane(0,.15f),1.35f,new Color(1,.26f,.01f,(1-t)*.42f*strength));

            // Back layer is occluded by characters; asymmetric front wisps keep faces visible.
            int count=reduced?2:4;
            for(int i=0;i<count;i++)
            {
                float phase=Mathf.Clamp01(t+(i%3)*.08f),spread=(i-(count-1)*.5f)*.29f;
                float rise=Mathf.Sin(Mathf.Clamp01(t/.8f)*Mathf.PI)*.22f;
                float h=(i%2==0?.9f:.75f)*(1+t*.18f);
                FrameQuad(backAtlas,Plane(spread,.15f+rise),Plane(.48f,0),Plane(0,h),new Color(1,.95f,.86f,flameFade*.78f*strength),2+phase*9,0,11);
            }
            for(int i=0;i<(reduced?1:2);i++)
                Flame(Plane((i==0?-.45f:.43f)*(1+t*.35f),-.15f+t*.16f),.39f,.58f,2+Mathf.Clamp01(t+i*.11f)*9,new Color(1,.98f,.9f,flameFade*.72f*strength));
            if(t>.24f)
            {
                float p=Mathf.InverseLerp(.24f,1,t);
                for(int i=0;i<(reduced?2:5);i++)
                {float x=(i-(reduced?.5f:2))*.28f,y=.55f+p*.95f;FrameQuad(backAtlas,Plane(x,y),Plane(.32f+p*.3f,0),Plane(0,.35f+p*.36f),new Color(.39f,.38f,.4f,Mathf.Sin(p*Mathf.PI)*.4f*strength),12+p*3,12,15);}
            }
            for(int i=0;i<(reduced?12:34);i++)
            {
                float a=i*2.39996f,r=.2f+t*(.8f+i%4*.28f),y=Mathf.Sin(a)*r+t*.75f;
                var point=Plane(Mathf.Cos(a)*r,y);Glow(point,.035f+(i%3)*.009f,new Color(1,.49f,.055f,(1-t)*strength));
                if(i%3==0)Ribbon(point-Plane(.02f,.08f),point,.012f,new Color(1,.82f,.25f,(1-t)*strength));
            }
        }
        void SweepBand(Vector3 center,float radius,float start,float sweep,float width,Color tint)
        {
            int n=solid.V.Count;const int segments=48;
            for(int i=0;i<=segments;i++)
            {
                float p=i/(float)segments,a=(start+sweep*p)*Mathf.Deg2Rad;
                // Sin(PI) can round slightly below zero; fractional powers must stay non-negative.
                float w=width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(p*Mathf.PI)),.65f);
                var radial=Plane(Mathf.Cos(a),Mathf.Sin(a)*.85f);
                var c=Alpha(tint,tint.a*Mathf.Sin(p*Mathf.PI*.5f));
                solid.Vertex(center+radial*(radius-w),c,Vector2.zero);
                solid.Vertex(center+radial*(radius+w),Alpha(c,c.a*.4f),Vector2.one);
                if(i>0){int k=n+i*2;solid.Triangle(k-2,k-1,k);solid.Triangle(k,k-1,k+1);}
            }
        }
        void WeaponSweep(float progress,float opacity)
        {
            if(opacity<=0)return;
            var aim=-from;float axis=Mathf.Atan2(Vector3.Dot(aim,view.transform.up),Vector3.Dot(aim,view.transform.right))*Mathf.Rad2Deg;
            float sign=Vector3.Dot(aim,view.transform.right)>=0?1:-1;
            float blade=axis+Mathf.Lerp(130,-60,progress)*sign;
            float length=Mathf.Lerp(35,135,Mathf.Clamp01(progress*2));
            float radius=Mathf.Clamp(new Vector2(Vector3.Dot(aim,view.transform.right),Vector3.Dot(aim,view.transform.up)).magnitude/size+.45f,1.65f,2.6f);
            var center=from+Plane(0,-.10f);float alpha=opacity*(reduced?.65f:1);
            int cuts=style==CombatVisualStyle.Claw?3:1;
            for(int i=0;i<cuts;i++)
            {
                float r=radius+i*.19f;
                SweepBand(center,r,blade+length*sign,-length*sign,.30f,new Color(.38f,.64f,1,.45f*alpha));
                SweepBand(center,r,blade+length*sign,-length*sign,.17f,new Color(.87f,.94f,1,.95f*alpha));
                SweepBand(center,r+.04f,blade+length*.82f*sign,-length*.82f*sign,.042f,new Color(1,1,1,alpha));
                if(style==CombatVisualStyle.FlameSword)
                    for(int j=0;j<7;j++){float a=(blade+j*13*sign)*Mathf.Deg2Rad;Flame(center+Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.85f),.35f,.48f,3+j%5,new Color(1,1,1,alpha));}
            }
        }
        void GroundDust(float t)
        {
            for(int i=0;i<(reduced?2:5);i++)
            {float p=Mathf.Clamp01(t+i*.035f);var center=Plane((i-2)*(.16f+t*.4f),-.55f+t*.15f);Quad(atlas,center,Plane(.27f+t*.3f,0),Plane(0,.14f+t*.12f),new Color(.72f,.64f,.49f,(1-p)*.45f),12+Mathf.Min(3,(int)(p*4)));}
        }
    }
}
