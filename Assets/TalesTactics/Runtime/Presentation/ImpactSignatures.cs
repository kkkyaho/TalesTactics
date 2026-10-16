using UnityEngine;
namespace TalesTactics
{
    // Three observed skill identities, rebuilt for the board camera with original art.
    // The follow-up rocks and lightning branches are visual only; the resolver owns contact.
    public sealed partial class ImpactParticles
    {
        SkillSignature signature;Material defaultArt,signatureArt;MeshRenderer artRenderer;
        public SkillSignature Signature=>signature;
        static readonly Color WaveColor=new Color(.28f,.82f,1);
        static readonly Color BoltColor=new Color(.58f,.40f,1);
        void SignatureQuad(Vector3 center,Vector3 right,Vector3 up,int cell,Color tint)
        {
            int n=art.V.Count;float x=(cell%2)*.5f+.002f,y=(1-cell/2)*.5f+.002f,w=.496f;
            art.Vertex(center-right-up,tint,new Vector2(x,y));art.Vertex(center-right+up,tint,new Vector2(x,y+w));
            art.Vertex(center+right+up,tint,new Vector2(x+w,y+w));art.Vertex(center+right-up,tint,new Vector2(x+w,y));
            art.Triangle(n,n+1,n+2);art.Triangle(n,n+2,n+3);
        }
        void FloorRing(Vector3 center,float radius,float width,Color color,float rotation=0,int segments=48)
        {
            radius*=size;width*=size;
            for(int i=0;i<segments;i++)
            {
                float a=rotation+i*Mathf.PI*2/segments,b=rotation+(i+1)*Mathf.PI*2/segments;int n=solid.V.Count;
                var av=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var bv=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                solid.Vertex(center+av*(radius-width),color,Vector2.zero);solid.Vertex(center+av*(radius+width),color,Vector2.up);
                solid.Vertex(center+bv*(radius+width),color,Vector2.one);solid.Vertex(center+bv*(radius-width),color,Vector2.right);
                solid.Triangle(n,n+1,n+2);solid.Triangle(n,n+2,n+3);
            }
        }
        void CastingCircle(Vector3 center,float p,float alpha)
        {
            var c=new Color(.73f,1,.91f,alpha);float radius=.50f+.08f*p;
            FloorRing(center,radius,.012f,c);FloorRing(center,radius*.78f,.008f,Alpha(c,alpha*.55f));
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4+p*.3f;var v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-v.z,0,v.x);
                var point=center+v*radius*size;Ribbon(point-side*.035f,point+side*.035f,.009f,c);
                Ribbon(point,point+v*.075f,.009f,c);
                if(i%2==0&&!reduced)Glow(center+v*.32f*size+Vector3.up*(.12f+Mathf.Repeat(p+i*.13f,.65f)),.028f,Alpha(c,alpha*.8f));
            }
        }
        void DrawSignaturePreparation(float p)
        {
            var ground=casterAnchor.position-transform.position+Vector3.up*.06f;
            float alpha=Mathf.Sin(p*Mathf.PI)*(reduced?.48f:.8f);
            if(signature==SkillSignature.DemonFang)
            {
                if(p>.58f){float q=Mathf.InverseLerp(.58f,1,p);var dir=Vector3.ProjectOnPlane(-ground,Vector3.up).normalized;
                    var tip=ground+dir*(.12f+q*.4f)+Vector3.up*(.12f+q*.25f);
                    Ribbon(tip-dir*.22f,tip,.025f,Alpha(WaveColor,alpha));Glow(tip,.10f,Alpha(WaveColor,alpha*.5f));}
                return;
            }
            CastingCircle(ground,p,alpha);
            if(signature==SkillSignature.ThunderBlade)
            {
                var halo=Vector3.up*2.35f*size;FloorRing(halo,.55f,.016f,new Color(.48f,.55f,1,alpha*.65f));
                FloorRing(halo,.38f,.008f,new Color(.8f,.82f,1,alpha*.5f));
            }
        }
        void GroundWave(Vector3 point,float alpha)
        {
            float flip=Vector3.Dot(-from,view.transform.right)<0?-1:1;
            SignatureQuad(point+Vector3.up*.25f*size,Plane(.85f*flip,0),Plane(0,.46f),0,new Color(.85f,1,1,alpha));
            var dir=Vector3.ProjectOnPlane(-from,Vector3.up).normalized;
            for(int i=1;i<=(reduced?1:3);i++)FrameQuad(backAtlas,point-dir*i*.18f+Vector3.up*.04f,Plane(.20f,0),Plane(0,.09f),new Color(.69f,.74f,.68f,alpha*.22f),12+i*.6f,12,15);
        }
        void FallingRock(Vector3 point,float radius,float alpha,float tilt=0)
        {
            var up=(view.transform.up+view.transform.right*tilt).normalized;
            var right=Vector3.Cross(up,view.transform.forward).normalized;
            // The rock core sits below the atlas centre; anchor it at the flight/contact point.
            SignatureQuad(point+up*radius*.58f,right*radius*1.4f,up*radius*1.4f,1,new Color(1,1,1,alpha));
            Glow(point,radius*.40f,new Color(1,.38f,.035f,alpha*.5f));
        }
        void ElectricBlade(Vector3 tip,float alpha)
        {
            var up=(view.transform.up-view.transform.right*.30f).normalized;var right=Vector3.Cross(up,view.transform.forward).normalized;
            float h=1.75f*size;
            SignatureQuad(tip+up*h*.80f,right*.66f*size,up*h,2,new Color(1,1,1,alpha));
        }
        void DrawSignatureTravel(float p)
        {
            float opacity=reduced?.72f:1;
            if(signature==SkillSignature.DemonFang){GroundWave(Vector3.Lerp(from,Vector3.zero,p),opacity);return;}
            if(signature==SkillSignature.MeteorStorm)
            {
                var start=Plane(-.8f,3.7f);var point=Vector3.Lerp(start,Vector3.zero,p*p);
                FallingRock(point,1.05f*size,opacity,-.15f);
                if(!reduced&&p>.6f)FallingRock(Plane(.55f,3.8f-(p-.6f)*3),.78f*size,opacity*.65f,.1f);
                return;
            }
            FloorRing(Vector3.up*2.35f*size,.55f,.012f,new Color(.53f,.60f,1,(1-p)*opacity*.65f));
            ElectricBlade(Vector3.Lerp(Plane(-.65f,2.5f),Vector3.zero,p*p),opacity);
        }
        Vector3 ShowerPoint(int i)
        {if(i==0)return Vector3.zero;float a=i*2.39996f;return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.45f+i*.12f)*size;}
        void MeteorBurst(Vector3 center,float p,float opacity)
        {
            float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.38f,p));if(fade<=0)return;
            float expansion=.3f+Mathf.Min(1,p*5)*.6f;
            SignatureQuad(center+Vector3.up*.025f,Vector3.right*expansion*size,Vector3.forward*expansion*size,3,new Color(1,1,1,fade*opacity*.9f));
            FloorRing(center+Vector3.up*.035f,expansion,.022f,new Color(1,.57f,.17f,fade*opacity*.65f));
            Flame(center+Vector3.up*(.18f+p*.35f)*size,.65f,.70f,Mathf.Clamp(p*20,0,10),new Color(1,.89f,.6f,fade*opacity*.9f));
            Glow(center+Vector3.up*.13f,.45f,new Color(1,.57f,.1f,fade*opacity*.65f));
            if(!reduced)FrameQuad(backAtlas,center+Vector3.up*(.3f+p*.6f),Plane(.48f+p*.3f,0),Plane(0,.35f+p*.4f),new Color(.29f,.26f,.25f,fade*.32f),12+p*5,12,15);
        }
        void DrawSignatureImpact(float t)
        {
            float opacity=reduced?.62f:1;
            if(signature==SkillSignature.DemonFang)
            {
                float fade=Mathf.Max(0,1-t*2.6f);var top=Vector3.up*(.65f+t*.35f)*size;
                Ribbon(Vector3.zero,top,.035f*fade,new Color(.78f,.96f,1,fade*opacity));
                Glow(top*.35f,.23f,Alpha(WaveColor,fade*opacity*.6f));
                var dir=(-from).normalized;
                for(int i=0;i<(reduced?3:7);i++){float a=i*2.39996f;var p=dir*(t*.65f)+Plane(Mathf.Cos(a),Mathf.Sin(a))*(t*.4f)+Vector3.up*.2f;
                    Ribbon(p,p+dir*.11f,.012f,Alpha(WaveColor,fade*opacity));}
                return;
            }
            if(signature==SkillSignature.MeteorStorm)
            {
                int count=reduced?3:4;
                for(int i=0;i<count;i++)
                {
                    float hit=reduced?i*.25f:i==0?0:i==1?.20f:i==2?.43f:.66f,local=age-hit;var point=ShowerPoint(i);
                    if(local<0&&local>-.27f){float q=(local+.27f)/.27f;FallingRock(point+Plane(.3f,3.2f)*(1-q*q),.78f*size,opacity,.10f);}
                    if(local>=0)MeteorBurst(point,local,opacity*(i==0?1:.8f));
                }
                return;
            }
            float bladeFade=Mathf.Max(0,1-t*2.7f);ElectricBlade(Vector3.zero,bladeFade*opacity);
            float fadeOut=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.85f,t));
            FloorRing(Vector3.up*.025f,.22f+t*1.3f,.014f,new Color(.65f,.70f,1,fadeOut*opacity*.6f));
            for(int i=0;i<(reduced?4:8);i++)
            {
                float a=i*Mathf.PI/4+.12f;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-dir.z,0,dir.x);
                Vector3 previous=Vector3.up*.055f;
                for(int j=1;j<=5;j++)
                {
                    var next=dir*(j*.22f*(.6f+t)*size)+side*(Mathf.Sin(i*7+j*5+Mathf.Floor(t*18))*.10f*size)+Vector3.up*(.04f+(j%2)*.04f);
                    Ribbon(previous,next,.028f,Alpha(BoltColor,fadeOut*opacity*.65f));
                    Ribbon(previous,next,.008f,new Color(.86f,.89f,1,fadeOut*opacity));previous=next;
                }
            }
            Glow(Vector3.up*.13f,.34f,new Color(.6f,.67f,1,bladeFade*opacity*.6f));
        }
    }
}
