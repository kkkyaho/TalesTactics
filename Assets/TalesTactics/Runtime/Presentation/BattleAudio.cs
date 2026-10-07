using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(AudioSource))]
    public sealed partial class BattleAudio:MonoBehaviour
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
            themeActive=false;resumeClip=null;resumeMusicId=null;currentMusicId=id;
            var source=GetComponent<AudioSource>();var clip=MusicClip(id);
            source.volume=MusicVolume;
            if(clip==null){source.Stop();source.clip=null;return;}
            source.loop=id!="victory";
            if(source.clip==clip&&source.isPlaying)return;
            source.clip=clip;source.Play();
        }
        public void PlayBattle(bool boss)=>Play(boss&&Entry("boss")?.Clip!=null?"boss":"battle");
        public void BeginTheme(string id)
        {
            var clip=MusicClip(id);if(clip==null||themeActive)return;
            var source=GetComponent<AudioSource>();
            if(source.clip==clip&&source.isPlaying)return;
            source.volume=MusicVolume;
            resumeMusicId=currentMusicId;currentMusicId=id;resumeClip=source.clip;resumeSample=source.timeSamples;resumeLoop=source.loop;resumePlaying=source.isPlaying;
            themeActive=true;source.clip=clip;source.loop=true;source.Play();
        }
        public void EndTheme()
        {
            if(!themeActive)return;themeActive=false;currentMusicId=resumeMusicId;resumeMusicId=null;
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
        public void StopAll(){currentMusicId=resumeMusicId=null;themeActive=false;resumeClip=null;effectEnds.Clear();effectStarts.Clear();LastEffectGain=0;GetComponent<AudioSource>().Stop();if(effects!=null)effects.Stop();}
    }
}
