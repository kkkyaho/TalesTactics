using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BattleAudio:MonoBehaviour
    {
        public AudioLibrary Library;
        [Range(0,1)] public float MusicVolume=0.28f;
        AudioSource effects;
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
            var entry=Library?.Entries?.FirstOrDefault(x=>x.Id==id);
            if(entry?.Clip==null)return;
            if(effects==null){effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=0.45f;}
            effects.PlayOneShot(entry.Clip);
        }
        public void StopAll(){themeActive=false;resumeClip=null;GetComponent<AudioSource>().Stop();if(effects!=null)effects.Stop();}
    }
}
