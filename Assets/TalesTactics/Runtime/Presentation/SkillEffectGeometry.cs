using UnityEngine;
namespace TalesTactics
{
    public sealed partial class CombatEffect
    {
        static float SafeSize(float value)=>float.IsNaN(value)||float.IsInfinity(value)?1:Mathf.Clamp(value,0.3f,2);
        void Ray(int index,float angle,float inner,float outer)
        {
            float a=angle*Mathf.Deg2Rad;
            Stroke(index,Plane(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner),Plane(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer));
        }
        void Column(int index,float x,float bottom,float top,float width)
        {Stroke(index,Plane(x-width,bottom),Plane(x,top),Plane(x+width,bottom));}
        void DrawPattern(float t)
        {
            float phase=Mathf.Repeat(t*pulses,1),r=(0.18f+phase*0.55f)*size;
            switch(Pattern)
            {
                case SkillVisualPattern.Slash:
                    Arc(0,r,-140+phase*140,190);Arc(1,r*0.7f,40-phase*90,150);break;
                case SkillVisualPattern.Thrust:
                    Stroke(0,origin,target);Ray(1,45,0,r);Ray(2,225,0,r);break;
                case SkillVisualPattern.Rising:
                    Arc(0,r,-80+phase*90,130,phase*0.4f);Column(1,0,-0.5f,phase*0.8f,0.2f);break;
                case SkillVisualPattern.Wave:
                    for(int i=0;i<3;i++)Arc(i,r+i*0.12f,-60,120);break;
                case SkillVisualPattern.Burst:
                    Arc(0,r,0,360);for(int i=0;i<8;i++)Ray(i+1,i*45,r*0.5f,r*1.4f);break;
                case SkillVisualPattern.Rain:case SkillVisualPattern.AstralRain:
                    for(int i=0;i<(Ultimate?9:5);i++)
                    {float x=(i-(Ultimate?4:2))*0.18f,y=0.9f-Mathf.Repeat(t*2+i*0.13f,1)*1.5f;Stroke(i,Plane(x+0.14f,y+0.35f),Plane(x,y));}
                    Arc(10,r,0,360,-0.4f);break;
                case SkillVisualPattern.Pillar:case SkillVisualPattern.Radiance:
                    for(int i=0;i<5;i++)Column(i,(i-2)*0.16f,-0.5f,(1-Mathf.Abs(i-2)*0.18f)*size,0.04f);
                    Arc(5,r,0,360);if(Ultimate){Arc(6,r*1.2f,t*180,360,0,8);Arc(7,r*0.8f,-t*180,360,0,4);}break;
                case SkillVisualPattern.Lightning:
                    for(int i=0;i<3;i++){float x=(i-1)*0.3f;Stroke(i,Plane(x,0.9f),Plane(x-0.13f,0.35f),Plane(x+0.12f,0.4f),Plane(x-0.05f,-0.5f));}break;
                case SkillVisualPattern.Ice:
                    for(int i=0;i<5;i++)Column(i,(i-2)*0.2f,-0.45f,(0.9f-Mathf.Abs(i-2)*0.2f)*size,0.12f);
                    Arc(5,r,30,360,-0.3f,6);break;
                case SkillVisualPattern.Meteor:
                    for(int i=0;i<4;i++){float x=(i-1.5f)*0.3f,y=1.2f-Mathf.Repeat(t*2+i*0.2f,1)*1.7f;Stroke(i,Plane(x+0.35f,y+0.6f),Plane(x,y));}
                    Arc(4,r,0,360,-0.4f);break;
                case SkillVisualPattern.Heal:case SkillVisualPattern.Revive:case SkillVisualPattern.Cleanse:
                    Arc(0,r,0,360);for(int i=0;i<4;i++)Ray(i+1,i*90+45,r*0.3f,r);
                    Stroke(5,Plane(-0.2f,0),Plane(0.2f,0));Stroke(6,Plane(0,-0.2f),Plane(0,0.2f));break;
                case SkillVisualPattern.Barrier:
                    Arc(0,r,30,360,0,6);Arc(1,r*0.8f,-30,360,0,6);Stroke(2,Plane(-r*0.5f,0),Plane(0,r*0.5f),Plane(r*0.5f,0));break;
                case SkillVisualPattern.Sleep:
                    for(int i=0;i<3;i++)Arc(i,0.14f+i*0.13f,t*80+i*60,240,phase*0.2f);break;
                case SkillVisualPattern.Gravity:
                    for(int i=0;i<5;i++)Arc(i,(1-phase)*(0.25f+i*0.12f),i*60+t*180,260);break;
                case SkillVisualPattern.Claw:case SkillVisualPattern.ClawFinale:
                    for(int i=0;i<3;i++){float x=(i-1)*0.2f;Stroke(i,Plane(x-r,r),Plane(x+r,-r));}
                    if(Ultimate){for(int i=0;i<3;i++)Arc(i+3,r+i*0.12f,phase*200+i*120,180);Arc(6,r*1.4f,0,360);}break;
                case SkillVisualPattern.SwordFinale:
                    for(int i=0;i<5;i++)Arc(i,r+i*0.09f,i*72+phase*210,140);
                    Stroke(5,Plane(0,-1),Plane(0,1.1f));Ray(6,0,0,r);Ray(7,180,0,r);break;
                case SkillVisualPattern.TimeStop:
                    Arc(0,0.8f*size,0,360);for(int i=0;i<8;i++)Ray(i+1,i*45,0.65f*size,0.8f*size);
                    Ray(9,90,0,0.55f*size);Ray(10,30,0,0.4f*size);break;
                case SkillVisualPattern.LionFinale:
                    Arc(0,r,0,360);Arc(1,r*1.3f,0,360);
                    for(int i=0;i<8;i++)Ray(i+2,i*45+phase*15,r*0.7f,r*1.6f);break;
                case SkillVisualPattern.Cage:
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;float x=Mathf.Cos(a)*0.65f;Stroke(i,Plane(x,-0.6f),Plane(x,0.9f));}
                    Arc(6,0.7f,30,360,-0.6f,6);Arc(7,0.7f,30,360,0.9f,6);Arc(8,r,0,360);break;
                case SkillVisualPattern.FlameFinale:case SkillVisualPattern.Wildfire:
                    for(int i=0;i<7;i++)Column(i,(i-3)*0.19f,-0.5f,(0.5f+Mathf.Sin(i*1.6f+phase*4)*0.2f)*size,0.1f);
                    Arc(7,r*1.4f,0,360,-0.3f);if(Pattern==SkillVisualPattern.FlameFinale)Arc(8,r,-100+phase*120,220);
                    else{Stroke(8,origin,target);Arc(9,r*0.7f,45,360,0,4);}break;
                case SkillVisualPattern.ShieldFinale:
                    Arc(0,r,30,360,0,6);Arc(1,r*0.7f,30,360,0,6);
                    for(int i=0;i<6;i++)Ray(i+2,i*60,r,r*1.5f);Column(8,0,-0.6f,0.9f,0.12f);break;
                default:Arc(0,r,0,360);break;
            }
        }
    }
}
