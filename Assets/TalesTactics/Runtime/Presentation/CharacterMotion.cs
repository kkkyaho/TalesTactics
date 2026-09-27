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
        bool specialUltimate;
        bool timingSpin;
        float spinProgress;
        public AnimationKind Action=>action;
        public void Initialize(SpriteRenderer renderer,UnitRuntime owner,Camera camera)
        {sprite=renderer;unit=owner;cameraView=camera;Set(AnimationKind.Idle);}
        public void Set(AnimationKind kind)
        {
            presenting=false;timingSpin=false;
            if(action!=kind||kind==AnimationKind.Attack||kind==AnimationKind.Skill||kind==AnimationKind.Damage||kind==AnimationKind.Ultimate)
            {action=kind;started=Time.time;}
            facing=unit.Facing;
        }
        public void BeginSkill(SkillData skill,float prepare,float recover)
        {
            Set(skill.Animation);started=Time.time;presenting=skill.Animation!=AnimationKind.Attack;
            specialUltimate=skill.IsUltimate;released=false;windup=prepare;recovery=recover;
        }
        public void ReleaseSkill(){released=true;releasedAt=Time.time;}
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
                var clip=specialUltimate?unit.Data.Poses?.Ultimate:unit.Data.Poses?.Skill;
                int index=released?1:0;
                if(clip?.Frames!=null&&clip.Frames.Length>index)
                    frame=clip.Frames[index]?.Get(view);
                if(released&&Time.time-releasedAt>=recovery){presenting=false;frame=null;action=AnimationKind.Idle;}
            }
            var pose=unit.Data.Poses?.Get(action,view);
            if((action==AnimationKind.Attack||action==AnimationKind.Skill)&&(t<0.08f||t>=0.34f))pose=null;
            if(timingSpin)pose=unit.Data.Poses?.Skill?.Sample(view,0,false)??unit.Data.Poses?.Attack?.Get(view);
            if(action==AnimationKind.Damage&&t>=0.28f)pose=null;
            var art=frame!=null?frame:pose!=null?pose:unit.Data.Sprites?.Get(view);if(art!=null)sprite.sprite=art;
            float sign=view==Facing.Left?-1:1;
            var color=art!=null?Color.white:unit.Data.PlaceholderColor;
            switch(action)
            {
                case AnimationKind.Idle: scaleY=1+Mathf.Sin(t*3)*0.008f;break;
                case AnimationKind.Walk:if(frame==null){bob=Mathf.Abs(Mathf.Sin(t*14))*0.055f;angle=Mathf.Sin(t*14)*2;}break;
                case AnimationKind.Attack:case AnimationKind.Skill:
                    float motionTime=presenting?(released?windup+Time.time-releasedAt:Mathf.Min(t,windup)):t;
                    float swing=Mathf.Sin(Mathf.Clamp01(motionTime/(presenting?windup+recovery:0.42f))*Mathf.PI);x=sign*swing*0.14f;angle=-sign*swing*9;break;
                case AnimationKind.Cast:bob=0.025f+Mathf.Sin(t*8)*0.018f;color=Color.Lerp(Color.white,new Color(0.6f,0.85f,1),0.25f+Mathf.Sin(t*8)*0.15f);break;
                case AnimationKind.Guard:scaleY=pose!=null?1:0.92f;angle=pose!=null?0:-sign*4;break;
                case AnimationKind.Damage:x=sign*Mathf.Sin(t*55)*0.04f*Mathf.Max(0,1-t*3);color=Color.Lerp(new Color(1,0.3f,0.3f),color,Mathf.Clamp01(t*4));break;
                case AnimationKind.Dead:scaleY=frame!=null?1:0.25f;color=frame!=null?new Color(0.75f,0.75f,0.75f,0.85f):new Color(0.4f,0.4f,0.4f,0.5f);break;
                case AnimationKind.Ultimate:bob=0.12f+Mathf.Sin(t*6)*0.025f;scaleY=1.05f;color=Color.Lerp(Color.white,new Color(1,0.85f,0.5f),0.25f);break;
            }
            if(timingSpin){x=0;angle=0;bob=Mathf.Sin(spinProgress*Mathf.PI)*0.06f;}
            transform.localPosition=new Vector3(x,0.04f+bob,0);
            transform.rotation=cameraView.transform.rotation*Quaternion.Euler(0,0,angle);
            transform.localScale=new Vector3(1,scaleY,1);sprite.color=color;
        }
    }
}
