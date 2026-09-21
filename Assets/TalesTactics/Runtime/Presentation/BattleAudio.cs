using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BattleAudio:MonoBehaviour
    {
        public AudioLibrary Library;
        public void Play(string id){var entry=Library?.Entries?.FirstOrDefault(x=>x.Id==id);var source=GetComponent<AudioSource>();if(entry?.Clip==null){source.Stop();return;}source.clip=entry.Clip;source.loop=id!="victory";source.Play();}
    }
}
