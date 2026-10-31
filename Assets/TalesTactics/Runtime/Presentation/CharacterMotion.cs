using UnityEngine;
namespace TalesTactics
{
    // Authored directional poses plus transform motion; missing poses use the idle art.
    // A supplied Animator remains authoritative and bypasses this fallback.
    public sealed class CharacterMotion:MonoBehaviour
    {
        SpriteRenderer sprite;
        UnitRuntime unit;
        Camera cameraView;
        AnimationKind action;
        float started;
        Facing facing;
        bool presenting, released;
        float windup=0.18f,recovery=0.3f,releasedAt;
        bool specialUltimate,casting;SkillSignature signature;ArteProfile arte;
        DirectionalSpriteClip phaseClip;
        CombatMotionProfile profile;
        float hold, damageStrength=1;
        Vector3 damageDirection;
        public bool Presenting=>presenting;
        bool timingSpin;
        float spinProgress;
        public AnimationKind Action=>action;
        public UnitRuntime Owner=>unit;
        public void Initialize(SpriteRenderer renderer,UnitRuntime owner,Camera camera)
        {sprite=renderer;unit=owner;cameraView=camera;Set(AnimationKind.Idle);}
        public void Set(AnimationKind kind)
        {
            presenting=false;timingSpin=false;signature=SkillSignature.None;arte=default;casting=false;phaseClip=null;
            if(action!=kind||kind==AnimationKind.Attack||kind==AnimationKind.Skill||kind==AnimationKind.Damage||kind==AnimationKind.Ultimate)
            {action=kind;started=Time.time;}
            facing=unit.Facing;
        }
        public void BeginSkill(SkillData skill,float prepare,float recover)
        {
            Set(skill.Animation);started=Time.time;presenting=true;
            specialUltimate=skill.IsUltimate;released=false;windup=prepare;recovery=recover;signature=SkillSignatures.Resolve(skill);arte=ArteProfiles.Resolve(skill);
            casting=!specialUltimate&&(SkillSignatures.IsSpell(signature)||arte.IsSpell||skill.Animation==AnimationKind.Cast);
            phaseClip=specialUltimate?unit.Data.Poses?.Ultimate:casting?CharacterMotionClips.Casting(unit.Data):
                action==AnimationKind.Attack||signature==SkillSignature.DemonFang?unit.Data.Poses?.AttackMotion:unit.Data.Poses?.Skill;
            profile=arte.Enabled?ArteMotionProfile.For(arte,CombatEffect.Resolve(unit.Data,skill)):casting?new CombatMotionProfile(.015f,.005f,.025f,0,0):
                signature==SkillSignature.DemonFang?new CombatMotionProfile(.3f,.12f,0,17,.065f):
                CombatMotionProfile.For(CombatEffect.Resolve(unit.Data,skill),specialUltimate);
            hold=Mathf.Min(profile.Hold,recovery*.18f);
        }
        public void ReleaseSkill(float travelDuration=0)
        {
            if(!presenting||released)return;
            // Keep the signature pose until its projectile and contact sequence have both completed.
            if((signature!=SkillSignature.None||arte.Enabled)&&!float.IsNaN(travelDuration)&&!float.IsInfinity(travelDuration))recovery+=Mathf.Max(0,travelDuration);
            released=true;releasedAt=Time.time;
        }
        public void Impact(Vector3 direction,bool heavy)
        {damageDirection=direction;damageStrength=heavy?1.4f:1;}
        public void Spin(float progress){timingSpin=true;spinProgress=Mathf.Clamp01(progress);}
        public void EndSpin(){timingSpin=false;Set(AnimationKind.Idle);}
        public void WalkFacing(Facing value){facing=value;}
        public static Facing ViewFacing(Facing world,Quaternion cameraRotation)
        {
            var f=SkillResolver.FacingVector(world);
            return ViewDirection(new Vector3(f.x,0,f.y),cameraRotation);
        }
        static Facing ViewDirection(Vector3 direction,Quaternion cameraRotation)
        {
            // Pitch must not collapse front/back to side views at an isometric camera angle.
            var local=Quaternion.Inverse(Quaternion.Euler(0,cameraRotation.eulerAngles.y,0))*direction;
            float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            int sector=Mathf.FloorToInt(Mathf.Repeat(angle+45.001f,360)/90);
            return sector==0?Facing.Back:sector==1?Facing.Right:sector==2?Facing.Front:Facing.Left;
        }
        void LateUpdate()
        {
            if(sprite==null||unit==null||cameraView==null)return;
            var view=ViewFacing(facing,cameraView.transform.rotation);
            if(timingSpin)
            {
                var direction=SkillResolver.FacingVector(facing);
                view=ViewDirection(Quaternion.Euler(0,spinProgress*720,0)*new Vector3(direction.x,0,direction.y),cameraView.transform.rotation);
            }
            float t=Time.time-started, bob=0,angle=0,x=0,scaleY=1;
            var frame=action==AnimationKind.Walk?unit.Data.Poses?.Walk?.Sample(view,t,true):
                action==AnimationKind.Dead?unit.Data.Poses?.Dead?.Sample(view,t,false):null;
            if(presenting)
            {
                float phaseTime=released?Mathf.Max(0,Time.time-releasedAt-hold):t;
                frame=phaseClip?.SamplePhase(view,phaseTime,released?Mathf.Max(.01f,recovery-hold):windup,released,
                    casting?SpritePhasePacing.Cast:SpritePhasePacing.Action);
                if(arte.Enabled&&arte.Motion==ArteMotion.Guard&&!specialUltimate)frame=unit.Data.Poses?.Guard?.Get(view)??frame;
                if(released&&Time.time-releasedAt>=recovery){presenting=false;phaseClip=null;frame=null;action=AnimationKind.Idle;started=Time.time;t=0;}
            }
            var pose=unit.Data.Poses?.Get(action,view);
            if((action==AnimationKind.Attack||action==AnimationKind.Skill)&&(t<0.08f||t>=0.34f))pose=null;
            if(timingSpin)pose=unit.Data.Poses?.Skill?.Sample(view,0,false)??unit.Data.Poses?.Attack?.Get(view);
            if(action==AnimationKind.Damage&&t>=0.28f)pose=null;
            var art=EquipmentAppearance.Resolve(unit,frame!=null?frame:pose!=null?pose:unit.Data.Sprites?.Get(view));if(art!=null)sprite.sprite=art;
            float sign=view==Facing.Left?-1:1;
            var color=art!=null?Color.white:unit.Data.PlaceholderColor;
            switch(action)
            {
                case AnimationKind.Idle: bob=Mathf.Sin(t*3)*.004f;break;
                case AnimationKind.Walk:if(frame==null){bob=Mathf.Abs(Mathf.Sin(t*14))*0.055f;angle=Mathf.Sin(t*14)*2;}break;
                case AnimationKind.Attack:case AnimationKind.Skill:
                    float motionTime=presenting?(released?windup+Time.time-releasedAt:Mathf.Min(t,windup)):t;
                    float swing=Mathf.Sin(Mathf.Clamp01(motionTime/(presenting?windup+recovery:0.42f))*Mathf.PI);x=sign*swing*0.14f;angle=-sign*swing*9;break;
                case AnimationKind.Cast:bob=0.025f+Mathf.Sin(t*8)*0.018f;color=Color.Lerp(Color.white,new Color(0.6f,0.85f,1),0.25f+Mathf.Sin(t*8)*0.15f);break;
                case AnimationKind.Guard:scaleY=pose!=null?1:0.92f;angle=pose!=null?0:-sign*4;break;
                case AnimationKind.Damage:x=sign*Mathf.Sin(t*55)*0.04f*Mathf.Max(0,1-t*3);color=Color.Lerp(new Color(1,0.3f,0.3f),color,Mathf.Clamp01(t*4));break;
                case AnimationKind.Dead:scaleY=frame!=null?1:0.25f;color=frame!=null?new Color(0.75f,0.75f,0.75f,0.85f):new Color(0.4f,0.4f,0.4f,0.5f);break;
                case AnimationKind.Ultimate:bob=0.12f+Mathf.Sin(t*6)*0.025f;color=Color.Lerp(Color.white,new Color(1,0.85f,0.5f),0.25f);break;
            }
            if(timingSpin){x=0;angle=0;bob=Mathf.Sin(spinProgress*Mathf.PI)*0.06f;}
            Vector3 offset=new Vector3(x,0.04f+bob,0);
            if(presenting&&!timingSpin)
            {
                var direction=SkillResolver.FacingVector(facing);
                var world=new Vector3(direction.x,0,direction.y);
                float prep=Mathf.Clamp01(t/Mathf.Max(.01f,windup));
                float release=Mathf.Clamp01((Time.time-releasedAt-hold)/Mathf.Max(.01f,recovery-hold));
                var sample=profile.Sample(released?release:prep,released);
                offset=world*sample.Travel+Vector3.up*(.04f+sample.Lift);
                angle=-sign*sample.Tilt;scaleY=1; // Authored faces and bodies are never squashed.
            }
            if(action==AnimationKind.Damage)
            {
                float recoil=(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.07f)/.30f)))*.12f*damageStrength;
                offset=damageDirection*recoil+Vector3.up*.04f;
                angle=-sign*recoil*32;
                color=Color.Lerp(Color.white*1.6f,color,Mathf.Clamp01(t/.08f));
            }
            if(presenting&&arte.Enabled&&!timingSpin)
            {
                float phase=released?Mathf.Clamp01((Time.time-releasedAt)/Mathf.Max(.01f,recovery)):Mathf.Clamp01(t/Mathf.Max(.01f,windup));
                if(arte.Motion==ArteMotion.Blink)color.a*=released?Mathf.Lerp(.35f,1,Mathf.Clamp01(phase*4)):Mathf.Lerp(1,.35f,phase);
                if(arte.Motion==ArteMotion.Spin)angle+=sign*Mathf.Sin(phase*Mathf.PI*2)*28;
            }
            transform.localPosition=offset;
            transform.rotation=cameraView.transform.rotation*Quaternion.Euler(0,0,angle);
            transform.localScale=new Vector3(1,scaleY,1);sprite.color=color;
        }
    }
}
