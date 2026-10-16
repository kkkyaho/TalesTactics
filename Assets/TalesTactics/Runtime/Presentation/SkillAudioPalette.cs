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
            if(skill.IsUltimate)return "ultimate";
            if(!skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)&&skill.Effects.Any(e=>e.Kind==EffectKind.Heal||e.Kind==EffectKind.Revive||e.Kind==EffectKind.Cleanse))return "heal";
            if(skill.Element==Element.Fire)return "fire";
            if(skill.Element==Element.Ice)return "ice";
            if(skill.Element==Element.Lightning)return "lightning";
            if(style==CombatVisualStyle.Sword||style==CombatVisualStyle.Claw||style==CombatVisualStyle.FlameSword||skill.Presentation?.Pattern==SkillVisualPattern.Wave)return "sword";
            return null;
        }
        public static AudioClip Create(string family,bool contact)
        {
            const int sampleRate=22050;
            float duration=contact?(family=="ultimate"?.68f:family=="heal"?.62f:.46f):.30f;
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
                samples[i]=body*attack*tail;peak=Mathf.Max(peak,Mathf.Abs(samples[i]));
            }
            // Leave headroom for simultaneous cues; the voice limiter adds further attenuation.
            float normalization=.72f/peak;for(int i=0;i<samples.Length;i++)samples[i]*=normalization;
            var clip=AudioClip.Create("Skill "+family+(contact?" contact":" preparation"),samples.Length,1,sampleRate,false);
            clip.SetData(samples,0);return clip;
        }
        static float Tone(float t,float frequency,float sweep)=>Mathf.Sin(2*Mathf.PI*(frequency*t+.5f*sweep*t*t));
    }
}
