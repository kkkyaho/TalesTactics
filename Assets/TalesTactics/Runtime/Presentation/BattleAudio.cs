using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BattleAudio:MonoBehaviour
    {
        public AudioLibrary Library;
        [Range(0,1)] public float MusicVolume=0.28f;
        public float EffectsVolume=.45f;
        public void SetVolumes(float music,float sound){MusicVolume=music;EffectsVolume=sound;GetComponent<AudioSource>().volume=music;if(effects!=null)effects.volume=sound;}
        AudioSource effects;
        readonly List<float> effectEnds=new List<float>();
        readonly Dictionary<string,float> effectStarts=new Dictionary<string,float>();
        public int ActiveEffectVoices {get{effectEnds.RemoveAll(t=>t<=Time.unscaledTime);return effectEnds.Count;}}
        public float LastEffectGain {get;private set;}
        AudioClip resumeClip;
        int resumeSample;
        bool resumeLoop,themeActive,resumePlaying;
        AudioEntry Entry(string id)=>Library?.Entries?.FirstOrDefault(x=>x!=null&&x.Id==id);
        public void Play(string id)
        {
            themeActive=false;resumeClip=null;
            var source=GetComponent<AudioSource>();var clip=Entry(id)?.Clip;
            source.volume=MusicVolume;
            if(clip==null){source.Stop();source.clip=null;return;}
            if(source.clip==clip&&source.isPlaying)return;
            source.clip=clip;source.loop=id!="victory";source.Play();
        }
        public void PlayBattle(bool boss)=>Play(boss&&Entry("boss")?.Clip!=null?"boss":"battle");
        public void BeginTheme(string id)
        {
            var clip=Entry(id)?.Clip;if(clip==null||themeActive)return;
            var source=GetComponent<AudioSource>();
            source.volume=MusicVolume;
            resumeClip=source.clip;resumeSample=source.timeSamples;resumeLoop=source.loop;resumePlaying=source.isPlaying;
            themeActive=true;source.clip=clip;source.loop=true;source.Play();
        }
        public void EndTheme()
        {
            if(!themeActive)return;themeActive=false;
            var source=GetComponent<AudioSource>();source.Stop();source.clip=resumeClip;source.loop=resumeLoop;
            if(resumeClip!=null&&resumePlaying){source.timeSamples=Mathf.Clamp(resumeSample,0,resumeClip.samples-1);source.Play();}
            resumeClip=null;
        }
        public void PlayEffect(string id)
        {
            var entry=Entry(id);
            if(entry?.Clip==null||EffectsVolume<=0)return;
            float now=Time.unscaledTime;
            if(effectStarts.TryGetValue(id,out var start)&&now-start<.045f)return;
            int voices=ActiveEffectVoices;if(voices>=4)return;
            if(effects==null){effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=0.45f;}
            LastEffectGain=1/Mathf.Sqrt(voices+1);effectStarts[id]=now;effectEnds.Add(now+entry.Clip.length);
            effects.volume=EffectsVolume;effects.PlayOneShot(entry.Clip,LastEffectGain);
        }
        public void StopAll(){themeActive=false;resumeClip=null;effectEnds.Clear();effectStarts.Clear();LastEffectGain=0;GetComponent<AudioSource>().Stop();if(effects!=null)effects.Stop();}
    }
}
