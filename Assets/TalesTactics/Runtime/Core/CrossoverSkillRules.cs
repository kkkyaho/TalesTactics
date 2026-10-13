using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public static class CrossoverSkillRules
    {
        static SkillEffect Hit(float power=1.4f,bool magic=false)=>new SkillEffect{Kind=EffectKind.Damage,Power=power,Magic=magic,Chance=1};
        static SkillEffect Status(StatusKind kind,int turns=1,bool caster=false)=>new SkillEffect{Kind=EffectKind.Status,Status=kind,Duration=turns,Chance=1,AffectCaster=caster};
        static SkillEffect Push(int distance=1)=>new SkillEffect{Kind=EffectKind.Push,Distance=distance,Chance=1};
        static void Shape(SkillData s,int range,int area=0,SkillAreaShape shape=SkillAreaShape.Diamond){s.Range=range;s.Area=area;s.Shape=shape;}
        static void Self(SkillData s,BossMechanic mechanic,params SkillEffect[] effects){s.Target=TargetType.Self;s.MinRange=s.Range=0;s.Area=0;s.BossMechanic=mechanic;s.Effects=effects;}
        public static void Configure(string id,int index,SkillData s)
        {
            if(index<0||index>2)throw new ArgumentOutOfRangeException(nameof(index));
            s.MPCost=0;s.MinRange=1;s.Range=4;s.Area=1;s.Target=TargetType.Enemy;s.Effects=new[]{Hit()};s.Animation=AnimationKind.Skill;
            s.Presentation=new SkillPresentation{Windup=.5f,Recovery=.65f,Pattern=SkillVisualPattern.Burst,Pulses=1,Size=1};
            switch(id)
            {
                case "dhaos":
                    if(index==0){Shape(s,6,0,SkillAreaShape.Line);s.Effects=new[]{Hit(1.65f,true)};s.Presentation.Pattern=SkillVisualPattern.Wave;}
                    if(index==1){Shape(s,1,2);s.Effects=new[]{Hit(1.2f,true),Push()};}
                    if(index==2){Shape(s,1);s.Effects=new[]{Hit(.65f),Hit(.65f),Hit(.65f),Hit(.65f)};s.Presentation.Pulses=4;}
                    break;
                case "demitel":
                    if(index==0){Shape(s,2,0,SkillAreaShape.Line);s.Element=Element.Dark;}
                    if(index==1)Self(s,BossMechanic.Summon);
                    if(index==2){Shape(s,4);s.Effects=new SkillEffect[0];s.BossMechanic=BossMechanic.DrainMP;}
                    break;
                case "jahmir":
                    if(index==0){s.Effects=new[]{Hit(1,true),Status(StatusKind.Slow,2)};s.Presentation.Pattern=SkillVisualPattern.Gravity;}
                    if(index==1){Shape(s,5,1);s.Element=Element.Earth;s.Presentation.Pattern=SkillVisualPattern.Pillar;}
                    if(index==2)Self(s,BossMechanic.Summon);
                    break;
                case "shizel":
                    if(index==0){s.Element=Element.Dark;s.BossMechanic=BossMechanic.FlameField;s.Effects=new[]{Hit(1.2f,true)};}
                    if(index==1)Self(s,BossMechanic.Summon);
                    if(index==2){Shape(s,6,4,SkillAreaShape.Lanes);s.Effects=new[]{Hit(2.4f,true)};s.BossMechanic=BossMechanic.Sanctuary;s.Presentation.Pattern=SkillVisualPattern.SwordFinale;}
                    break;
                case "hyades":
                    s.Element=index==1?Element.Earth:Element.Lightning;
                    if(index==0){Shape(s,2,1);s.Effects=new[]{Hit(1.1f,true)};}
                    if(index==1){Shape(s,4,2,SkillAreaShape.Cross);s.BossMechanic=BossMechanic.Collapse;}
                    if(index==2){Shape(s,5,1);s.Effects=new[]{Hit(1.7f,true)};s.Presentation.Pattern=SkillVisualPattern.Lightning;}
                    break;
                case "rassius":
                    if(index==0){Shape(s,3,0,SkillAreaShape.Line);s.Presentation.Pulses=3;s.Effects=new[]{Hit(.8f),Hit(.8f),Hit(.8f)};}
                    if(index==1){Shape(s,5,0,SkillAreaShape.Line);s.Effects=new[]{Hit(1.8f)};s.Presentation.Pattern=SkillVisualPattern.Thrust;}
                    if(index==2)Self(s,BossMechanic.Chivalry,Status(StatusKind.Guard,2));
                    break;
                case "van":
                    if(index==0){Shape(s,4,0,SkillAreaShape.Cone);s.Presentation.Pattern=SkillVisualPattern.Wave;}
                    if(index==1){Shape(s,2);s.Effects=new[]{Hit(.8f),Hit(.8f),Hit(.8f)};s.Presentation.Pulses=3;}
                    if(index==2)Self(s,BossMechanic.Summon);
                    break;
                case "asch":
                    if(index==0){Shape(s,2);s.Effects=new[]{Hit(2)};s.Presentation.Pattern=SkillVisualPattern.SwordFinale;}
                    if(index==1){Shape(s,1);s.Effects=new[]{Hit(.9f),Hit(.9f)};s.Presentation.Pulses=2;}
                    if(index==2)Self(s,BossMechanic.Counter,Status(StatusKind.Guard,2));
                    break;
                case "legretta":
                    s.Element=Element.Light;s.RequiresLineOfSight=true;
                    if(index==0){Shape(s,7,0,SkillAreaShape.Line);s.Presentation.Pattern=SkillVisualPattern.Wave;}
                    if(index==1){Shape(s,6,1);s.Effects=new[]{Hit(1.6f,true)};s.Presentation.Pattern=SkillVisualPattern.Pillar;}
                    if(index==2){Shape(s,7,4,SkillAreaShape.Lanes);s.Presentation.Pulses=3;s.Presentation.Pattern=SkillVisualPattern.AstralRain;}
                    break;
                case "artorius":
                    if(index==0){Shape(s,5,2,SkillAreaShape.Cross);s.Element=Element.Light;s.Effects=new[]{Hit(1.5f,true)};}
                    if(index==1){Shape(s,2);s.Effects=new[]{Hit(.8f),Hit(.8f),Hit(.8f)};s.Presentation.Pulses=3;s.Presentation.Pattern=SkillVisualPattern.SwordFinale;}
                    if(index==2){Shape(s,5,2);s.Effects=new[]{new SkillEffect{Kind=EffectKind.Gauge,Flat=-25,Chance=1}};s.BossMechanic=BossMechanic.GaugeSeal;}
                    break;
                case "shigure":
                    if(index==0){Shape(s,4,2,SkillAreaShape.Diagonal);s.Effects=new[]{Hit(2)};s.Presentation.Pattern=SkillVisualPattern.SwordFinale;}
                    if(index==1)Self(s,BossMechanic.Counter,Status(StatusKind.Guard,2));
                    if(index==2){Shape(s,6,0,SkillAreaShape.Line);s.Effects=new[]{Hit(2.3f)};s.Presentation.Pattern=SkillVisualPattern.Slash;}
                    break;
                case "oscar":
                    if(index==0){Shape(s,3,0,SkillAreaShape.Line);s.Presentation.Pattern=SkillVisualPattern.Slash;}
                    if(index==1)Self(s,BossMechanic.Command);
                    if(index==2){Shape(s,4);s.BossMechanic=BossMechanic.Cooperate;s.Presentation.Pattern=SkillVisualPattern.Thrust;}
                    break;
                case "vholran":
                    if(index==0){Shape(s,6,3);s.Element=Element.Water;s.BossMechanic=BossMechanic.HighWater;s.Presentation.Pattern=SkillVisualPattern.Wave;}
                    if(index==1){Shape(s,2,1);s.Effects=new[]{Hit(1.8f)};s.BossMechanic=BossMechanic.MarkedStrike;}
                    if(index==2){Shape(s,5,1);s.Element=Element.Ice;s.Effects=new[]{Hit(1.3f,true),Status(StatusKind.Root)};s.BossMechanic=BossMechanic.IceTrail;}
                    break;
                case "balseph":
                    s.Element=Element.Fire;
                    if(index==0){Shape(s,4,2);s.Effects=new[]{Hit(2)};s.Presentation.Pattern=SkillVisualPattern.Meteor;}
                    if(index==1){Shape(s,3,0,SkillAreaShape.Line);s.Effects=new[]{Hit(1.8f)};}
                    if(index==2){Shape(s,2,1);s.Effects=new[]{Hit(1.1f)};s.BossMechanic=BossMechanic.FlameField;}
                    break;
                case "ganabelt":
                    s.Element=Element.Light;
                    if(index==0){Shape(s,5,2,SkillAreaShape.Scatter);s.Effects=new SkillEffect[0];s.BossMechanic=BossMechanic.Mines;}
                    if(index==1){Shape(s,6,0,SkillAreaShape.Line);s.Effects=new[]{Hit(1.7f,true)};s.Presentation.Pattern=SkillVisualPattern.Wave;}
                    if(index==2){Shape(s,6,3);s.BossMechanic=BossMechanic.CloneNova;s.Effects=new[]{Hit(2.5f,true)};s.Presentation.Pattern=SkillVisualPattern.Lightning;}
                    break;
                case "ordo":
                    if(index==0){Shape(s,6,2);s.BossMechanic=BossMechanic.Replay;s.Effects=new[]{Hit(1.7f,true)};}
                    if(index==1)Self(s,BossMechanic.Mend);
                    if(index==2){Shape(s,6,4,SkillAreaShape.Lanes);s.BossMechanic=BossMechanic.FinalStop;s.Effects=new[]{Hit(1.5f,true),Status(StatusKind.Stun)};s.Presentation.Pattern=SkillVisualPattern.TimeStop;}
                    break;
                case "executor":
                    if(index==0){Shape(s,5,0,SkillAreaShape.Line);s.BossMechanic=BossMechanic.Boundary;s.Presentation.Pattern=SkillVisualPattern.Thrust;}
                    if(index==1){Shape(s,6);s.Effects=new[]{Hit(2.1f)};s.Presentation.Pattern=SkillVisualPattern.Thrust;}
                    if(index==2)Self(s,BossMechanic.LockFacing,Status(StatusKind.Guard,2),Status(StatusKind.Root,2));
                    break;
                case "golem":
                    if(index==0){Shape(s,3,2,SkillAreaShape.Cross);s.Effects=new[]{Hit(1.3f),Push()};}
                    if(index==1)Self(s,BossMechanic.CoreRecharge,new SkillEffect{Kind=EffectKind.Heal,Power=2,Flat=40,Chance=1});
                    if(index==2){Shape(s,4,2,SkillAreaShape.Scatter);s.BossMechanic=BossMechanic.Collapse;}
                    break;
                case "origin":
                    if(index==0){Shape(s,5,3,SkillAreaShape.Cross);s.Element=Element.Lightning;s.Effects=new[]{Hit(1.7f,true)};}
                    if(index==1)Self(s,BossMechanic.None,Status(StatusKind.Guard,2));
                    if(index==2){Shape(s,5,2,SkillAreaShape.Diagonal);s.BossMechanic=BossMechanic.Contract;s.Effects=new[]{Hit(1.1f,true),Push()};}
                    break;
                case "maxwell":
                    if(index==0){Shape(s,6,3,SkillAreaShape.Scatter);s.Effects=new[]{Hit(1.8f,true)};s.Presentation.Pattern=SkillVisualPattern.Meteor;}
                    if(index==1){Shape(s,5,1);s.Effects=new[]{Hit(1.3f,true),Status(StatusKind.Slow,2)};s.Presentation.Pattern=SkillVisualPattern.Gravity;}
                    if(index==2){Shape(s,4,2);s.Effects=new[]{Hit(1.2f,true),Push(2)};s.Element=Element.Wind;}
                    break;
                default:throw new ArgumentException("Unknown boss: "+id);
            }
            if(s.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.Magic)){s.Animation=AnimationKind.Attack;if(s.Presentation.Pattern==SkillVisualPattern.Burst)s.Presentation.Pattern=SkillVisualPattern.Slash;}
            s.IsUltimate=id=="shizel"&&index==2||id=="van"&&index==1||id=="asch"&&index==0||id=="legretta"&&index==2||id=="artorius"&&index==1||id=="shigure"&&index==0||id=="vholran"&&index==1||id=="balseph"&&index==0||id=="ganabelt"&&index==2||id=="ordo"&&index==2;
            if(s.IsUltimate){s.Animation=AnimationKind.Ultimate;s.Presentation.Windup=.75f;s.Presentation.Recovery=.9f;}
        }
    }
}
