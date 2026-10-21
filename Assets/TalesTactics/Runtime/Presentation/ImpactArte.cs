using UnityEngine;
namespace TalesTactics
{
    // Authored silhouettes and per-ID motion recipes. All echoes are visual only.
    public sealed partial class ImpactParticles
    {
        ArteProfile arte;Material weaponArt,spellArt,defaultBack;MeshRenderer backRenderer;
        public ArteProfile Arte=>arte;
        void SetArte(ArteProfile value)
        {
            arte=value;
            artRenderer.sharedMaterial=value.Enabled?weaponArt:signature==SkillSignature.None?defaultArt:signatureArt;
            backRenderer.sharedMaterial=value.Enabled?spellArt:defaultBack;
            backRenderer.sortingOrder=value.Enabled?22:-1;
        }
        static bool SpellSheet(ArteMotif motif)
        {
            switch(motif)
            {
                case ArteMotif.Earth:case ArteMotif.Water:case ArteMotif.Wind:case ArteMotif.Ice:
                case ArteMotif.Fire:case ArteMotif.Lightning:case ArteMotif.Prism:case ArteMotif.Heal:
                case ArteMotif.Cleanse:case ArteMotif.Revive:case ArteMotif.Barrier:case ArteMotif.Sleep:
                case ArteMotif.Song:case ArteMotif.Gravity:case ArteMotif.Judgment:case ArteMotif.Radiance:
                case ArteMotif.Meteor:return true;
                default:return false;
            }
        }
        static int ArteCell(ArteMotif motif)
        {
            switch(motif)
            {
                case ArteMotif.Hammer:return 1;
                case ArteMotif.RisingSlash:return 2;
                case ArteMotif.Thrust:return 3;
                case ArteMotif.Lion:return 4;
                case ArteMotif.Claw:return 5;
                case ArteMotif.FlameFinale:return 6;
                case ArteMotif.Arrow:case ArteMotif.ArrowRain:return 7;
                case ArteMotif.Rifle:return 8;
                case ArteMotif.Grenade:return 9;
                case ArteMotif.Shield:return 10;
                case ArteMotif.Phoenix:return 11;
                case ArteMotif.Dragon:return 12;
                case ArteMotif.Martial:return 13;
                case ArteMotif.Clock:return 14;
                case ArteMotif.Cage:return 15;
                case ArteMotif.Ice:return 0;
                case ArteMotif.Water:return 1;
                case ArteMotif.Wind:return 2;
                case ArteMotif.Earth:return 3;
                case ArteMotif.Fire:return 4;
                case ArteMotif.Lightning:case ArteMotif.Judgment:return 5;
                case ArteMotif.Prism:return 6;
                case ArteMotif.Heal:return 7;
                case ArteMotif.Cleanse:return 8;
                case ArteMotif.Revive:return 9;
                case ArteMotif.Barrier:return 10;
                case ArteMotif.Sleep:return 11;
                case ArteMotif.Gravity:return 12;
                case ArteMotif.Radiance:return 13;
                case ArteMotif.Song:return 14;
                case ArteMotif.Meteor:return 15;
                default:return 0;
            }
        }
        void ArteQuad(ArteMotif motif,Vector3 center,Vector3 right,Vector3 up,float opacity)
        {
            if(motif==ArteMotif.None||opacity<=.002f)return;
            var mesh=SpellSheet(motif)?backAtlas:art;int cell=ArteCell(motif),n=mesh.V.Count;
            // Keep UVs inside one cell; the dedicated material feathers only these atlas borders.
            float x=(cell%4)*.25f+.001f,y=(3-cell/4)*.25f+.001f,w=.248f;
            var color=Color.Lerp(Color.white,arte.Tint,motif==ArteMotif.Hammer&&!arte.IsSpell?.85f:.18f);color.a=opacity;
            mesh.Vertex(center-right-up,color,new Vector2(x,y));mesh.Vertex(center-right+up,color,new Vector2(x,y+w));
            mesh.Vertex(center+right+up,color,new Vector2(x+w,y+w));mesh.Vertex(center+right-up,color,new Vector2(x+w,y));
            mesh.Triangle(n,n+1,n+2);mesh.Triangle(n,n+2,n+3);
        }
        void Glyph(ArteMotif motif,Vector3 point,float width,float height,float alpha,float angle=0)
        {
            float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
            ArteQuad(motif,point,Plane(c*width,s*width),Plane(-s*height,c*height),alpha);
        }
        void Seal(Vector3 point,float radius,float alpha,float rotation=0)
        {
            var right=new Vector3(Mathf.Cos(rotation),0,Mathf.Sin(rotation))*radius*size;
            var up=new Vector3(-Mathf.Sin(rotation),0,Mathf.Cos(rotation))*radius*size;
            ArteQuad(ArteMotif.Radiance,point+Vector3.up*.045f,right,up,alpha);
        }
        Vector3 ArteDirection
        {
            get{var v=Vector3.ProjectOnPlane(-from,Vector3.up);return v.sqrMagnitude>.001f?v.normalized:Vector3.right;}
        }
        Vector3 ArtePoint(int index,int count)
        {
            if(count<=1)return Vector3.zero;
            float r=arte.Spread*size,angle=index*2.39996f+arte.Angle;
            switch(arte.Variant%4)
            {
                case 1:return ArteDirection*((index/(float)(count-1)-.5f)*r*1.5f);
                case 2:return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*r*(index%2==0?.7f:.4f);
                case 3:return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*r*.6f+Vector3.up*(index%3)*.08f;
                default:return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*r*.55f;
            }
        }
        int ArteCount=>Mathf.Clamp(reduced?(arte.Count+1)/2:arte.Count,1,8);
        float ArteOpacity=>reduced?.62f:.92f;
        static float Envelope(float t,float length)
        {return SkillVisualLanguage.ContactEnvelope(t,length);}
        void DrawArtePreparation(float p)
        {
            var ground=casterAnchor.position-transform.position+Vector3.up*.06f;
            float a=Mathf.Sin(p*Mathf.PI)*ArteOpacity;
            if(arte.IsSpell)
            {
                Seal(ground,.6f+.12f*p,a*.7f,p*.12f);
                if(arte.Motif==ArteMotif.Song)Glyph(ArteMotif.Song,ground+Vector3.up*.65f,.5f,.35f,a*.5f,p*.2f);
                for(int i=0;i<(reduced?3:6);i++)
                {float q=i*2.39996f;Glow(ground+new Vector3(Mathf.Cos(q),0,Mathf.Sin(q))*.36f+Vector3.up*(.12f+p*.55f),.025f,Alpha(arte.Tint,a*.5f));}
            }
            else
            {
                var point=ground+Vector3.up*.45f;
                if(arte.Motion==ArteMotion.Guard)Glyph(arte.Motif==ArteMotif.Claw?ArteMotif.Claw:ArteMotif.Shield,point,.45f,.5f,a*.5f);
                else if(arte.Motion==ArteMotion.Shoot)
                {Glyph(arte.Motif==ArteMotif.Rifle||arte.Motif==ArteMotif.Grenade?ArteMotif.Rifle:ArteMotif.Arrow,point+ArteDirection*.2f,.28f,.2f,a*.4f,arte.Angle);}
                else if(p>.55f)
                {float q=Mathf.InverseLerp(.55f,1,p);Glyph(arte.Motif,point+ArteDirection*(.1f+q*.2f),.3f+q*.2f,.3f,a*.55f,arte.Angle+q*.6f);}
            }
            if(arte.Ultimate)Seal(ground,.82f,a*.45f,-p*.2f);
        }
        bool Falls(ArteMotif motif)=>motif==ArteMotif.ArrowRain||motif==ArteMotif.Prism||motif==ArteMotif.Judgment||motif==ArteMotif.Meteor||motif==ArteMotif.Lightning||motif==ArteMotif.Hammer&&arte.IsSpell;
        void FallingGlyph(ArteMotif motif,Vector3 tip,float alpha,float scale=1)
        {
            float width=motif==ArteMotif.Meteor?.65f:motif==ArteMotif.Hammer?.55f:.28f;
            float height=motif==ArteMotif.Meteor?.78f:motif==ArteMotif.Hammer?.55f:.85f;
            if(motif==ArteMotif.ArrowRain)
                Glyph(ArteMotif.Arrow,tip+Plane(-.18f,.38f),.68f*scale,.18f*scale,alpha,-1.12f);
            else Glyph(motif,tip+Vector3.up*(motif==ArteMotif.Hammer?.3f:.50f)*size,width*scale,height*scale,alpha,motif==ArteMotif.Hammer?-.5f:arte.Angle*.25f);
        }
        void DrawArteTravel(float p)
        {
            p=SkillVisualLanguage.Snap(p);
            float a=ArteOpacity;var motif=arte.Motif;
            if(Falls(motif))
            {
                FallingGlyph(motif,Vector3.Lerp(Plane(-.4f,2.5f),Vector3.zero,p*p),a);
                if(ArteCount>1&&!reduced)FallingGlyph(motif,ArtePoint(1,ArteCount)+Plane(.3f,3.2f)*(1-p*p),a*.55f,.75f);
                return;
            }
            if(motif==ArteMotif.Earth||motif==ArteMotif.Ice||motif==ArteMotif.Gravity||motif==ArteMotif.Clock||motif==ArteMotif.Cage)
            {Seal(Vector3.zero,.35f+p*.45f,a*p*.45f);return;}
            var point=Vector3.Lerp(from,Vector3.zero,p);
            float angle=Mathf.Atan2(Vector3.Dot(-from,view.transform.up),Vector3.Dot(-from,view.transform.right));
            if(motif==ArteMotif.Wave||motif==ArteMotif.Water)
            {
                int count=Mathf.Min(3,ArteCount);
                for(int i=count-1;i>=0;i--){var head=Vector3.Lerp(from,Vector3.zero,Mathf.Clamp01(p-i*.12f));
                    Glyph(motif,head+Vector3.up*.3f,.65f,.4f,a*(1-i*.2f),angle*.2f);}
                return;
            }
            if(motif==ArteMotif.Grenade)
            {point+=Vector3.up*Mathf.Sin(p*Mathf.PI)*1.1f;Glow(point,.10f,Alpha(arte.Tint,a));Glyph(arte.Accent==ArteMotif.None?ArteMotif.Fire:arte.Accent,point,.22f,.22f,a*.7f,p*2);return;}
            if(motif==ArteMotif.Rifle||motif==ArteMotif.Arrow||motif==ArteMotif.Thrust)
            {
                Glyph(motif,point+Vector3.up*.45f,.5f,.19f,a,angle);
                CelStroke(point+Vector3.up*.45f-ArteDirection*.44f,point+Vector3.up*.45f,.018f,Alpha(arte.Tint,a*.8f),true);return;
            }
            if(motif==ArteMotif.Phoenix||motif==ArteMotif.Dragon||motif==ArteMotif.Lion)
            {Glyph(motif,point+Vector3.up*(.5f+Mathf.Sin(p*Mathf.PI)*.35f),.68f,.65f,a,arte.Angle*.15f);return;}
            Glyph(motif,point+Vector3.up*.4f,.35f,.4f,a*.8f,arte.Angle+p*.3f);
        }
        void StrikeSilhouette(ArteMotif motif,Vector3 point,float q,float a,int index)
        {
            float angle=arte.Angle;
            switch(motif)
            {
                case ArteMotif.Slash:case ArteMotif.CrossSlash:case ArteMotif.RisingSlash:case ArteMotif.Claw:
                    angle+=(index%2==0?1:-1)*q*1.1f;
                    if(motif==ArteMotif.CrossSlash)angle+=index%2==0?-.65f:.65f;
                    var slashPoint=point+Vector3.up*(.38f+(motif==ArteMotif.RisingSlash?q*.65f:0));
                    Glyph(motif,slashPoint,.82f+q*.20f,motif==ArteMotif.Claw?.62f:.47f,a*.78f,angle);
                    if(index==0||!reduced)CelSlash(slashPoint,.63f+q*.18f,angle-.9f,2.15f,.045f,Alpha(arte.Tint,a));break;
                case ArteMotif.Thrust:
                    Glyph(motif,point+Vector3.up*.5f+ArteDirection*q*.3f,.8f,.19f,a,angle+(index%2)*.08f);
                    CelStroke(point-ArteDirection*.4f+Vector3.up*.4f,point+ArteDirection*(.24f+q*.34f)+Vector3.up*.4f,.023f,Alpha(arte.Tint,a),true);break;
                case ArteMotif.Martial:
                    Glyph(motif,point+Vector3.up*(.4f+(index%3)*.12f),.45f+q*.3f,.4f+q*.2f,a,angle+index*.75f);
                    FloorRing(point+Vector3.up*.04f,.15f+q*.6f,.013f,Alpha(arte.Tint,a*.4f));break;
                case ArteMotif.Wave:
                    Glyph(motif,point+ArteDirection*q*.65f+Vector3.up*.28f,.8f,.42f,a*.8f,angle*.3f);
                    CelSlash(point+ArteDirection*q*.65f+Vector3.up*.22f,.66f,-.35f+angle*.3f,2.3f,.052f,Alpha(arte.Tint,a));break;
                case ArteMotif.Phoenix:case ArteMotif.Dragon:case ArteMotif.Lion:
                    Glyph(motif,point+Vector3.up*(.6f+q*.3f)+ArteDirection*q*.25f,.75f+q*.4f,.7f+q*.3f,a,angle*.12f);
                    FloorRing(point+Vector3.up*.035f,.2f+q*.8f,.014f,Alpha(arte.Tint,a*.45f));break;
                case ArteMotif.Arrow:case ArteMotif.Rifle:
                    Glyph(motif,point+Vector3.up*.5f,.48f,.24f,a,angle+index*.12f);
                    for(int j=0;j<(reduced?2:4);j++){float k=j*2.399f;var v=Plane(Mathf.Cos(k),Mathf.Sin(k));Ribbon(point+Vector3.up*.5f+v*q*.5f,point+Vector3.up*.5f+v*(q*.5f+.08f),.012f,Alpha(arte.Tint,a));}break;
                case ArteMotif.Grenade:
                    Glyph(motif,point+Vector3.up*.35f,.55f+q*.5f,.5f+q*.45f,a);
                    FloorRing(point+Vector3.up*.03f,.15f+q*.9f,.014f,Alpha(arte.Tint,a*.55f));break;
                case ArteMotif.Shield:
                    Glyph(motif,point+Vector3.up*.58f,.58f,.65f,a*(.85f-.25f*q),angle*.12f);
                    FloorRing(point+Vector3.up*.03f,.2f+q*.85f,.025f,Alpha(arte.Tint,a*.5f));break;
                case ArteMotif.Hammer:
                    Glyph(motif,point+Vector3.up*(.5f+q*.2f),.62f,.65f,a,-.65f+q*.25f);
                    Glow(point+Vector3.up*.2f,.24f,Alpha(new Color(1,.75f,.85f),a*.55f));break;
                case ArteMotif.FlameFinale:
                    Glyph(motif,point+Vector3.up*.55f,.9f+q*.25f,.9f,a,angle+index*.55f);
                    Flame(point+Vector3.up*.35f,.35f,.55f,q*10,new Color(1,.8f,.45f,a*.6f));break;
            }
        }
        void SpellSilhouette(ArteMotif motif,Vector3 point,float q,float a,int index)
        {
            switch(motif)
            {
                case ArteMotif.Earth:case ArteMotif.Ice:
                    float height=.7f+Mathf.Sin(Mathf.Clamp01(q*3)*Mathf.PI*.5f)*.45f;
                    Glyph(motif,point+Vector3.up*(height*.55f)*size,.38f,height*.65f,a,arte.Angle*.1f);
                    if(motif==ArteMotif.Earth&&q<.7f)FloorRing(point+Vector3.up*.03f,.12f+q*.4f,.011f,Alpha(arte.Tint,a*.3f));break;
                case ArteMotif.Water:
                    Glyph(motif,point+Vector3.up*.4f+ArteDirection*q*.35f,.65f+q*.3f,.6f,a,arte.Angle*.25f);break;
                case ArteMotif.Wind:
                    Glyph(motif,point+Vector3.up*(.55f+q*.3f),.55f,.85f,a,Mathf.Sin(q*6+index)*.12f);break;
                case ArteMotif.Fire:
                    Glyph(motif,point+Vector3.up*.7f,.45f,.85f,a);Flame(point+Vector3.up*.3f,.32f,.5f,q*10,new Color(1,.85f,.55f,a*.5f));break;
                case ArteMotif.Lightning:
                    Glyph(motif,point+Vector3.up*.95f,.4f,1.15f,a);
                    for(int branch=0;branch<(reduced?2:4);branch++)
                    {var prev=point+Vector3.up*2.2f;for(int j=1;j<7;j++){var next=point+Vector3.up*(2.2f-j*.34f)+Plane(Mathf.Sin(j*7+branch*13+Mathf.Floor(q*10))*.15f,0);CelStroke(prev,next,.018f,Alpha(new Color(.64f,.57f,1),a),true);prev=next;}}break;
                case ArteMotif.Prism:case ArteMotif.Judgment:case ArteMotif.ArrowRain:
                    FallingGlyph(motif,point+Vector3.up*.1f,a,.8f);Glow(point+Vector3.up*.08f,.3f,Alpha(arte.Tint,a*.5f));break;
                case ArteMotif.Meteor:
                    Glyph(ArteMotif.Fire,point+Vector3.up*.45f,.65f,.65f,a);
                    FloorRing(point+Vector3.up*.04f,.2f+q*.9f,.022f,Alpha(arte.Tint,a*.6f));break;
                case ArteMotif.Heal:
                    Glyph(motif,point+Vector3.up*(.22f+q*.5f),.36f,.35f,a*.65f);
                    if(index==0)HealingFiligree(point,q,a);
                    Seal(point,.42f,a*.4f,-q*.3f);break;
                case ArteMotif.Cleanse:
                    Glyph(motif,point+Vector3.up*(.3f+q*.7f),.38f,.38f,a,Mathf.Sin(q*5+index)*.25f);
                    if(index==0)HealingFiligree(point,q,a*.65f);break;
                case ArteMotif.Revive:
                    Glyph(motif,point+Vector3.up*(.55f+q*.4f),.6f,.65f,a);
                    if(index==0)HealingFiligree(point,q,a);
                    if(index==0)Glyph(ArteMotif.Judgment,point+Vector3.up*.7f,.28f,1f,a*.35f);break;
                case ArteMotif.Barrier:
                    Glyph(motif,point+Vector3.up*.6f,.62f+q*.12f,.75f,a*.72f);break;
                case ArteMotif.Sleep:
                    Glyph(motif,point+Vector3.up*(.7f+q*.3f),.42f,.42f,a,-q*.2f);break;
                case ArteMotif.Song:
                    Glyph(motif,point+Vector3.up*(.45f+q*.65f),.55f,.4f,a,arte.Angle+(index%2==0?q:-q)*.2f);break;
                case ArteMotif.Gravity:
                    ArteQuad(motif,point+Vector3.up*.05f,Vector3.right*(.6f+q*.3f)*size,Vector3.forward*(.6f+q*.3f)*size,a*.85f);break;
                case ArteMotif.Radiance:
                    Seal(point,.6f+q*.4f,a*.7f,q*.3f);
                    Glyph(ArteMotif.Judgment,point+Vector3.up*.85f,.35f,1.05f,a*.65f);break;
                case ArteMotif.Clock:
                    Glyph(motif,point+Vector3.up*.7f,.9f,.9f,a*.7f,-q*.10f);break;
                case ArteMotif.Cage:
                    Glyph(motif,point+Vector3.up*.65f,1f,1.05f,a*.82f);break;
            }
        }
        void DrawArteImpact(float t)
        {
            int count=ArteCount;var motif=arte.Motif;
            // One readable creature silhouette; rapid follow-ups are shock accents, not
            // multiple large faces covering the nearby squad at the same time.
            bool creature=motif==ArteMotif.Lion||motif==ArteMotif.Dragon||motif==ArteMotif.Phoenix;
            if(creature)
            {
                float length=count>1?.85f:.54f;
                StrikeSilhouette(motif,Vector3.zero,Mathf.Clamp01(age/length),Envelope(age,length)*ArteOpacity,0);
            }
            bool single=motif==ArteMotif.Clock||motif==ArteMotif.Cage||motif==ArteMotif.Gravity||motif==ArteMotif.Barrier;
            int pulses=single?1:count;
            for(int i=0;i<pulses;i++)
            {
                float delay=pulses<=1?0:i*.48f/(pulses-1),local=age-delay;
                var point=ArtePoint(i,pulses);float a=Envelope(local,.54f)*ArteOpacity;
                if(creature)
                {
                    if(i>0&&local>=0&&local<.32f)
                    {
                        float echoProgress=local/.32f,echo=Envelope(local,.32f)*ArteOpacity;
                        var spark=point*.5f+Vector3.up*.25f;
                        Glow(spark,.11f*(1-echoProgress),Alpha(arte.Tint,echo*.5f));
                        FloorRing(point*.35f+Vector3.up*.035f,.12f+echoProgress*.42f,.012f,Alpha(arte.Tint,echo*.4f));
                    }
                    continue;
                }
                if(single)a=Envelope(age,.99f)*ArteOpacity;
                if(Falls(motif)&&i>0&&local<0&&local>-.22f)FallingGlyph(motif,point+Plane(.25f,2.5f)*(1-Mathf.Pow((local+.22f)/.22f,2)),ArteOpacity*.75f,.7f);
                if(a<=0)continue;float q=SkillVisualLanguage.Snap(local/.54f);
                if(feedback==CombatFeedback.Strike&&!single)
                    CelContact(point+Vector3.up*.24f,local,Alpha(arte.Tint,ArteOpacity*(i==0?1:.55f)),i==0?.58f:.34f);
                if(SpellSheet(motif)||motif==ArteMotif.Clock||motif==ArteMotif.Cage||motif==ArteMotif.ArrowRain)SpellSilhouette(motif,point,q,a,i);
                else StrikeSilhouette(motif,point,q,a,i);
            }
            if(arte.Accent!=ArteMotif.None)
            {
                float q=age-(motif==ArteMotif.Hammer&&!arte.IsSpell?0:.24f),a=Envelope(q,.76f)*ArteOpacity*.48f;
                if(a>0){if(SpellSheet(arte.Accent)||arte.Accent==ArteMotif.Clock||arte.Accent==ArteMotif.Cage)SpellSilhouette(arte.Accent,Vector3.zero,Mathf.Clamp01(q/.76f),a,0);
                    else StrikeSilhouette(arte.Accent,Vector3.zero,Mathf.Clamp01(q/.76f),a,0);}
            }
            float fade=Envelope(age,.98f)*ArteOpacity;
            // Small motes follow each skill's authored spread, without hiding recipient faces.
            if(!reduced)for(int i=0;i<Mathf.Min(arte.Count+2,8);i++)
            {float angle=i*2.39996f+arte.Variant*.5f;var p=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.22f+t*arte.Spread)*size+Vector3.up*(.12f+t*.45f);Glow(p,.022f,Alpha(arte.Tint,fade*.55f));}
        }
    }
}
