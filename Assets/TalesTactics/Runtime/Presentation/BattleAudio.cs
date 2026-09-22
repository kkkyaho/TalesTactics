using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BattleAudio:MonoBehaviour
    {
        public AudioLibrary Library;
        AudioSource effects;
        public void Play(string id){var entry=Library?.Entries?.FirstOrDefault(x=>x.Id==id);var source=GetComponent<AudioSource>();if(entry?.Clip==null){source.Stop();return;}source.clip=entry.Clip;source.loop=id!="victory";source.Play();}
        public void PlayEffect(string id)
        {
            var entry=Library?.Entries?.FirstOrDefault(x=>x.Id==id);
            if(entry?.Clip==null)return;
            if(effects==null){effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=0.45f;}
            effects.PlayOneShot(entry.Clip);
        }
        public void StopAll(){GetComponent<AudioSource>().Stop();if(effects!=null)effects.Stop();}
    }
}
