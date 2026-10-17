using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Small local sound palette, generated once per cue. Never uses the combat/global random stream.
    public static class SkillAudioPalette
    {
        public static string Family(SkillData skill,CombatVisualStyle style)
        {
            if(skill==null)return null;
            switch(SkillSignatures.Resolve(skill))
            {
                case SkillSignature.DemonFang:return "demon-fang";
                case SkillSignature.MeteorStorm:return "meteor-storm";
                case SkillSignature.ThunderBlade:return "thunder-blade";
            }
            var profile=ArteProfiles.Resolve(skill);
            if(profile.Enabled)return ProfileFamily(profile);
            if(skill.IsUltimate)return "ultimate";
            if(!skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)&&skill.Effects.Any(e=>e.Kind==EffectKind.Heal||e.Kind==EffectKind.Revive||e.Kind==EffectKind.Cleanse))return "heal";
            if(skill.Element==Element.Fire)return "fire";
            if(skill.Element==Element.Ice)return "ice";
            if(skill.Element==Element.Lightning)return "lightning";
            if(style==CombatVisualStyle.Sword||style==CombatVisualStyle.Claw||style==CombatVisualStyle.FlameSword||skill.Presentation?.Pattern==SkillVisualPattern.Wave)return "sword";
            return null;
        }
        static string ProfileFamily(ArteProfile profile)
        {
            switch(profile.Motif)
            {
                case ArteMotif.Arrow:case ArteMotif.ArrowRain:return "arte-arrow";
                case ArteMotif.Rifle:return "arte-rifle";
                case ArteMotif.Grenade:
                    return profile.Accent==ArteMotif.Water?"arte-water":profile.Accent==ArteMotif.Wind?"arte-wind":profile.Accent==ArteMotif.FlameFinale||profile.Accent==ArteMotif.Fire?"fire":"arte-rifle";
                case ArteMotif.Shield:case ArteMotif.Barrier:return "arte-shield";
                case ArteMotif.Earth:case ArteMotif.Meteor:return "arte-earth";
                case ArteMotif.Water:return "arte-water";
                case ArteMotif.Wind:return "arte-wind";
                case ArteMotif.Prism:case ArteMotif.Judgment:case ArteMotif.Radiance:
                case ArteMotif.Cleanse:case ArteMotif.Revive:return "arte-holy";
                case ArteMotif.Song:return "arte-song";
                case ArteMotif.Gravity:case ArteMotif.Sleep:case ArteMotif.Cage:case ArteMotif.Clock:return "arte-gravity";
                case ArteMotif.Claw:return "arte-claw";
                case ArteMotif.Hammer:return "arte-hammer";
                case ArteMotif.Martial:case ArteMotif.Lion:return "arte-martial";
                case ArteMotif.Heal:return "heal";
                case ArteMotif.Ice:return "ice";
                case ArteMotif.Lightning:return "lightning";
                case ArteMotif.Fire:case ArteMotif.Phoenix:case ArteMotif.FlameFinale:return "fire";
                default:return profile.Accent==ArteMotif.Fire?"fire":"sword";
            }
        }
        public static AudioClip Create(string family,bool contact)
        {
            const int sampleRate=22050;
            float duration=CueDuration(family,contact);
            var samples=new float[Mathf.CeilToInt(duration*sampleRate)];
            uint noiseState=0x9e3779b9u;float smooth=0,previous=0,peak=.01f;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)sampleRate,p=t/duration;
                noiseState^=noiseState<<13;noiseState^=noiseState>>17;noiseState^=noiseState<<5;
                float noise=(noiseState&65535)/32767.5f-1;
                smooth+=.12f*(noise-smooth);float high=noise-previous;previous=noise;
                float body;
                switch(family)
                {
                    case "demon-fang":
                        body=contact?(smooth*1.2f+Tone(t,95,-130)*.48f)*Mathf.Exp(-t*9)+high*.12f*Mathf.Exp(-t*40):
                            (high*.22f+smooth*.65f+Tone(t,1550,-2700)*.13f)*Mathf.Exp(-t*2.5f);break;
                    case "meteor-storm":
                        if(!contact)body=smooth*.45f+Tone(t,1350,-1450)*.22f+Tone(t,230,120)*.13f;
                        else
                        {
                            body=0;
                            // Four arrivals share one clip and one voice; they never trigger more combat hits.
                            for(int arrival=0;arrival<4;arrival++)
                            {
                                float delay=arrival==0?0:arrival==1?.20f:arrival==2?.43f:.66f;
                                float local=t-delay;if(local<0)continue;
                                float envelope=(1-Mathf.Exp(-local*500))*Mathf.Exp(-local*7)*(1-arrival*.13f);
                                body+=(smooth*1.35f+Tone(local,78+arrival*13,-90)*.55f+
                                    high*.14f*Mathf.Exp(-local*32))*envelope;
                            }
                        }
                        break;
                    case "thunder-blade":
                        if(!contact)
                        {
                            float charge=.25f+.75f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*83)),4);
                            body=high*.11f*charge+Tone(t,260,1150)*.17f+Tone(t,780,600)*.09f;
                        }
                        else
                        {
                            float crack=.2f+.8f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*190)),5);
                            body=high*.24f*Mathf.Exp(-t*34)+noise*.2f*crack*Mathf.Exp(-t*12)+
                                Tone(t,90,-65)*.5f*Mathf.Exp(-t*5)+Tone(t,180,-130)*.15f*Mathf.Exp(-t*7);
                        }
                        break;
                    case "arte-arrow":
                        body=contact?(high*.12f+smooth*.55f)*Mathf.Exp(-t*12)+Tone(t,240,-280)*.2f*Mathf.Exp(-t*25):
                            Tone(t,410,-190)*.22f*Mathf.Exp(-t*7)+smooth*.35f*(.3f+p);break;
                    case "arte-rifle":
                        body=contact?high*.42f*Mathf.Exp(-t*75)+smooth*1.4f*Mathf.Exp(-t*11)+Tone(t,105,-85)*.36f*Mathf.Exp(-t*10):
                            high*.12f*Mathf.Exp(-t*35)+Tone(t,950,-500)*.17f*Mathf.Exp(-t*22)+Tone(t,390,0)*.1f*Mathf.Exp(-t*11);break;
                    case "arte-shield":
                        body=(Tone(t,310,0)*.24f+Tone(t,713,0)*.17f+Tone(t,1041,0)*.09f)*Mathf.Exp(-t*(contact?5:3));
                        body+=contact?smooth*.6f*Mathf.Exp(-t*18)+high*.16f*Mathf.Exp(-t*40):smooth*.08f;break;
                    case "arte-earth":
                        body=smooth*(contact?1.8f:.8f)+Tone(t,contact?66:115,-45)*.34f;
                        if(contact)body+=high*.12f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*67)),4)*Mathf.Exp(-t*5);
                        else body+=Tone(t,710,-680)*.1f;break;
                    case "arte-water":
                        float ripple=.55f+.45f*Mathf.Sin(t*35);
                        body=smooth*(contact?1.1f:.45f)*ripple+Tone(t,contact?920:560,contact?-1250:420)*.13f;
                        body+=Tone(t,1320,-1700)*.09f*Mathf.Exp(-t*8);break;
                    case "arte-wind":
                        float gust=.35f+.65f*Mathf.Sin(Mathf.PI*p);
                        body=(smooth*.9f+high*.045f)*gust+Tone(t,contact?720:390,contact?-620:540)*.055f;
                        body*=.85f+.15f*Mathf.Sin(t*27);break;
                    case "arte-holy":
                        body=(Tone(t,659.25f,contact?0:75)*.22f+Tone(t,987.77f,0)*.16f+Tone(t,1318.5f,0)*.1f)*Mathf.Exp(-t*(contact?3:1));
                        body+=Tone(t,2637,0)*.04f*Mathf.Exp(-t*12);break;
                    case "arte-song":
                        float breath=.88f+.12f*Mathf.Sin(t*24);
                        body=(Tone(t,392,0)*.18f+Tone(t,493.88f,0)*.14f+Tone(t,587.33f,0)*.11f)*breath;
                        body+=smooth*.035f+Tone(t,784,0)*.04f;break;
                    case "arte-gravity":
                        float hollow=.45f+.55f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*31)),2);
                        body=(Tone(t,contact?105:220,contact?-65:-145)*.3f+Tone(t,contact?211:443,-120)*.12f)*hollow+smooth*.45f;
                        body+=high*.045f*Mathf.Exp(-t*9);break;
                    case "arte-claw":
                        body=0;
                        for(int slash=0;slash<(contact?3:1);slash++)
                        {
                            float local=t-slash*.095f;if(local<0)continue;
                            body+=(high*.2f+smooth*.55f+Tone(local,1700,-2700)*.12f)*Mathf.Exp(-local*(contact?18:5));
                        }
                        break;
                    case "arte-hammer":
                        body=contact?(Tone(t,390,-620)*.34f+Tone(t,97,-75)*.28f+smooth*.7f)*Mathf.Exp(-t*7)+high*.1f*Mathf.Exp(-t*35):
                            (Tone(t,784,280)*.17f+Tone(t,1176,420)*.12f)*Mathf.Exp(-t*3);break;
                    case "arte-martial":
                        body=(smooth*(contact?1.35f:.65f)+Tone(t,contact?125:320,contact?-170:-210)*.28f)*Mathf.Exp(-t*(contact?11:3));
                        body+=contact?high*.15f*Mathf.Exp(-t*38):high*.045f;break;
                    case "fire":
                        body=smooth*2.1f+Tone(t,contact?83:190,-55)*.35f+noise*.12f*Mathf.Sin(t*97);break;
                    case "ice":
                        body=(Tone(t,1580,contact?-280:320)*.26f+Tone(t,2410,-430)*.16f+high*.12f)*Mathf.Exp(-t*(contact?5:1));break;
                    case "lightning":
                        float pulse=.35f+.65f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*118)),5);
                        body=noise*.38f*pulse+Tone(t,contact?125:640,-100)*.25f+high*.13f*Mathf.Exp(-t*25);break;
                    case "heal":
                        body=(Tone(t,523.25f,0)+Tone(t,659.25f,0)*.65f+Tone(t,783.99f,0)*.45f)*.23f;
                        body*=.85f+.15f*Mathf.Sin(t*18);break;
                    case "ultimate":
                        body=smooth*1.6f+Tone(t,contact?62:150,contact?-34:280)*.4f+Tone(t,contact?124:300,contact?-68:560)*.16f;
                        body+=high*.18f*Mathf.Exp(-t*22);break;
                    default:
                        body=high*.23f+smooth*.7f+Tone(t,contact?1250:720,contact?-1800:1600)*.12f;
                        body*=Mathf.Exp(-t*(contact?7:1));break;
                }
                float attack=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/(contact?.006f:.05f)));
                float tail=Mathf.Pow(1-p,contact?1.6f:1.1f);
                if(family=="meteor-storm"&&contact)tail=Mathf.SmoothStep(0,1,Mathf.Clamp01((duration-t)/.1f));
                samples[i]=body*attack*tail;peak=Mathf.Max(peak,Mathf.Abs(samples[i]));
            }
            // Leave headroom for simultaneous cues; the voice limiter adds further attenuation.
            float normalization=.72f/peak;for(int i=0;i<samples.Length;i++)samples[i]*=normalization;
            var clip=AudioClip.Create("Skill "+family+(contact?" contact":" preparation"),samples.Length,1,sampleRate,false);
            clip.SetData(samples,0);return clip;
        }
        static float CueDuration(string family,bool contact)
        {
            switch(family)
            {
                case "demon-fang":return contact?.34f:.30f;
                case "meteor-storm":return contact?.95f:.55f;
                case "thunder-blade":return contact?.72f:.55f;
                case "arte-arrow":return contact?.32f:.28f;
                case "arte-rifle":return contact?.3f:.25f;
                case "arte-shield":return contact?.65f:.4f;
                case "arte-earth":return contact?.8f:.5f;
                case "arte-water":return contact?.65f:.42f;
                case "arte-wind":return contact?.62f:.45f;
                case "arte-holy":return contact?.8f:.52f;
                case "arte-song":return contact?.95f:.6f;
                case "arte-gravity":return contact?.8f:.55f;
                case "arte-claw":return contact?.55f:.32f;
                case "arte-hammer":return contact?.52f:.4f;
                case "arte-martial":return contact?.34f:.28f;
                default:return contact?(family=="ultimate"?.68f:family=="heal"?.62f:.46f):.30f;
            }
        }
        static float Tone(float t,float frequency,float sweep)=>Mathf.Sin(2*Mathf.PI*(frequency*t+.5f*sweep*t*t));
    }
}
