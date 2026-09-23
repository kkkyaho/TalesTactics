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
        public AnimationKind Action=>action;
        public void Initialize(SpriteRenderer renderer,UnitRuntime owner,Camera camera)
        {sprite=renderer;unit=owner;cameraView=camera;Set(AnimationKind.Idle);}
        public void Set(AnimationKind kind)
        {
            if(action!=kind||kind==AnimationKind.Attack||kind==AnimationKind.Skill||kind==AnimationKind.Damage||kind==AnimationKind.Ultimate)
            {action=kind;started=Time.time;}
            facing=unit.Facing;
        }
        public void WalkFacing(Facing value){facing=value;}
        public static Facing ViewFacing(Facing world,Quaternion cameraRotation)
        {
            var f=SkillResolver.FacingVector(world);
            var local=Quaternion.Inverse(cameraRotation)*new Vector3(f.x,0,f.y);
            return Mathf.Abs(local.x)>Mathf.Abs(local.z)?(local.x<0?Facing.Left:Facing.Right):(local.z<0?Facing.Front:Facing.Back);
        }
        void LateUpdate()
        {
            if(sprite==null||unit==null||cameraView==null)return;
            var view=ViewFacing(facing,cameraView.transform.rotation);
            float t=Time.time-started, bob=0,angle=0,x=0,scaleY=1;
            var frame=action==AnimationKind.Walk?unit.Data.Poses?.Walk?.Sample(view,t,true):
                action==AnimationKind.Dead?unit.Data.Poses?.Dead?.Sample(view,t,false):null;
            var pose=unit.Data.Poses?.Get(action,view);
            if((action==AnimationKind.Attack||action==AnimationKind.Skill)&&(t<0.08f||t>=0.34f))pose=null;
            if(action==AnimationKind.Damage&&t>=0.28f)pose=null;
            var art=frame!=null?frame:pose!=null?pose:unit.Data.Sprites?.Get(view);if(art!=null)sprite.sprite=art;
            float sign=view==Facing.Left?-1:1;
            var color=art!=null?Color.white:unit.Data.PlaceholderColor;
            switch(action)
            {
                case AnimationKind.Idle: scaleY=1+Mathf.Sin(t*3)*0.008f;break;
                case AnimationKind.Walk:if(frame==null){bob=Mathf.Abs(Mathf.Sin(t*14))*0.055f;angle=Mathf.Sin(t*14)*2;}break;
                case AnimationKind.Attack:case AnimationKind.Skill:
                    float swing=Mathf.Sin(Mathf.Clamp01(t/0.42f)*Mathf.PI);x=sign*swing*0.14f;angle=-sign*swing*9;break;
                case AnimationKind.Cast:bob=0.025f+Mathf.Sin(t*8)*0.018f;color=Color.Lerp(Color.white,new Color(0.6f,0.85f,1),0.25f+Mathf.Sin(t*8)*0.15f);break;
                case AnimationKind.Guard:scaleY=pose!=null?1:0.92f;angle=pose!=null?0:-sign*4;break;
                case AnimationKind.Damage:x=sign*Mathf.Sin(t*55)*0.04f*Mathf.Max(0,1-t*3);color=Color.Lerp(new Color(1,0.3f,0.3f),color,Mathf.Clamp01(t*4));break;
                case AnimationKind.Dead:scaleY=frame!=null?1:0.25f;color=frame!=null?new Color(0.75f,0.75f,0.75f,0.85f):new Color(0.4f,0.4f,0.4f,0.5f);break;
                case AnimationKind.Ultimate:bob=0.12f+Mathf.Sin(t*6)*0.025f;scaleY=1.05f;color=Color.Lerp(Color.white,new Color(1,0.85f,0.5f),0.25f);break;
            }
            transform.localPosition=new Vector3(x,0.04f+bob,0);
            transform.rotation=cameraView.transform.rotation*Quaternion.Euler(0,0,angle);
            transform.localScale=new Vector3(1,scaleY,1);sprite.color=color;
        }
    }
}
