using UnityEngine;
namespace TalesTactics
{
    // Shared surface treatment only. Uses the existing four meshes and local effect clock.
    public sealed partial class ImpactParticles
    {
        void TrailBand(Vector3 tail,Vector3 head,Vector3 normal,float width,Color color)
        {
            var across=Vector3.Cross(normal,head-tail).normalized*width*size;
            if(across.sqrMagnitude<.0000001f||color.a<=.002f)return;
            int n=solid.V.Count,segments=reduced?6:10;
            for(int i=0;i<=segments;i++)
            {
                float p=i/(float)segments,w=SkillVisualLanguage.TrailWidth(p);
                var point=Vector3.Lerp(tail,head,p);var tint=Alpha(color,color.a*Mathf.SmoothStep(.08f,1,p));
                solid.Vertex(point-across*w,tint,Vector2.zero);solid.Vertex(point+across*w,tint,Vector2.one);
                if(i>0){int k=n+i*2;solid.Triangle(k-2,k-1,k);solid.Triangle(k,k-1,k+1);}
            }
        }
        void CelTrail(Vector3 tail,Vector3 head,float width,Color color,bool ground=false)
        {
            var normal=ground?Vector3.up:view.transform.forward;
            TrailBand(tail,head,normal,width*1.35f,SkillVisualLanguage.Ink(Alpha(color,color.a*.72f)));
            TrailBand(tail,head,normal,width,color);
            TrailBand(tail,head,normal,width*.28f,SkillVisualLanguage.Highlight(color));
        }
        void CrescentBand(Vector3 center,Vector3 xAxis,Vector3 yAxis,float radius,float height,
            float angle,float sweep,float width,Color color)
        {
            if(color.a<=.002f)return;
            int n=solid.V.Count,segments=reduced?16:28;
            for(int i=0;i<=segments;i++)
            {
                float p=i/(float)segments,a=angle+sweep*p;
                float taper=SkillVisualLanguage.TrailWidth(p);
                var radial=xAxis*(Mathf.Cos(a)*radius)+yAxis*(Mathf.Sin(a)*height);
                var normal=(xAxis*Mathf.Cos(a)+yAxis*Mathf.Sin(a)).normalized;
                var tint=Alpha(color,color.a*Mathf.Lerp(.30f,1,Mathf.Sin(p*Mathf.PI*.5f)));
                solid.Vertex(center+radial-normal*width*taper*size,tint,Vector2.zero);
                solid.Vertex(center+radial+normal*width*taper*size,tint,Vector2.one);
                if(i>0){int k=n+i*2;solid.Triangle(k-2,k-1,k);solid.Triangle(k,k-1,k+1);}
            }
        }
        void LayeredCrescent(Vector3 center,Vector3 xAxis,Vector3 yAxis,float radius,float height,
            float angle,float sweep,float width,Color color)
        {
            CrescentBand(center,xAxis,yAxis,radius,height,angle,sweep,width*1.30f,SkillVisualLanguage.Ink(Alpha(color,color.a*.68f)));
            CrescentBand(center,xAxis,yAxis,radius,height,angle,sweep,width,color);
            CrescentBand(center,xAxis,yAxis,radius,height,angle,sweep,width*.28f,SkillVisualLanguage.Highlight(color));
        }
        void HealingPetal(Vector3 center,Vector3 axis,Vector3 side,float opacity)
        {
            if(opacity<=.002f)return;
            int n=solid.V.Count;const int segments=8;
            for(int i=0;i<=segments;i++)
            {
                float p=i/(float)segments,bend=Mathf.Sin(p*Mathf.PI),w=Mathf.Pow(Mathf.Max(0,bend),.76f);
                var middle=center+axis*(p-.46f)+side*bend*.28f;
                solid.Vertex(middle-side*w,Alpha(SkillVisualLanguage.HealingLeaf,opacity*.85f),Vector2.zero);
                solid.Vertex(middle,Alpha(SkillVisualLanguage.HealingIvory,opacity),Vector2.one*.5f);
                solid.Vertex(middle+side*w*.72f,Alpha(Color.Lerp(SkillVisualLanguage.HealingLeaf,SkillVisualLanguage.HealingIvory,.55f),opacity),Vector2.one);
                if(i>0){int k=n+i*3;solid.Triangle(k-3,k-2,k);solid.Triangle(k,k-2,k+1);solid.Triangle(k-2,k-1,k+1);solid.Triangle(k+1,k-1,k+2);}
            }
        }
        void HealingBloom(Vector3 ground,float phase,float opacity)
        {
            float opening=Mathf.SmoothStep(0,1,Mathf.Clamp01(phase*3));
            float radius=.35f+opening*.23f;
            var ivory=Alpha(SkillVisualLanguage.HealingIvory,opacity*.72f);
            FloorRing(ground+Vector3.up*.025f,radius,.009f,ivory);
            int count=reduced?4:8;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count+phase*.12f;
                var outward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-outward.z,0,outward.x);
                HealingPetal(ground+Vector3.up*.028f+outward*(radius*.69f*size),outward*(.23f*size),side*(.068f*size),opacity*.68f);
            }
        }
        void FirstAidPreparation(Vector3 ground,float phase,float opacity)
        {
            HealingBloom(ground,.15f+phase*.3f,opacity*.55f);
            var point=ground+Vector3.up*.44f+view.transform.right*.24f;
            Glow(point,.075f,Alpha(SkillVisualLanguage.HealingIvory,opacity*.65f));
            for(int i=0;i<(reduced?2:4);i++)
            {
                float a=i*2.39996f,r=.28f*(1-phase);
                HealingPetal(point+Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r),Plane(.045f,.10f),Plane(.025f,0),opacity*.6f);
            }
        }
        void FirstAidTravel(float phase,float opacity)
        {
            var head=Vector3.Lerp(from,Vector3.zero,phase)+Vector3.up*.16f;
            var tail=Vector3.Lerp(from,Vector3.zero,Mathf.Max(0,phase-.24f))+Vector3.up*.16f;
            CelTrail(tail,head,.018f,Alpha(SkillVisualLanguage.HealingLeaf,opacity*.5f));
            HealingPetal(head,Plane(.06f,.13f),Plane(.035f,0),opacity*.8f);
            HealingBloom(Vector3.zero,phase,opacity*phase*.4f);
        }
        void FirstAidImpact(float phase)
        {
            float opacity=Envelope(age,.98f)*ArteOpacity;
            HealingBloom(Vector3.zero,phase,opacity);
            // The illustrated seal is horizontal and low, never a face-sized billboard.
            Seal(Vector3.zero,.56f,opacity*.28f,-phase*.12f);
            HealingFiligree(Vector3.zero,phase,opacity);
        }
    }
}
