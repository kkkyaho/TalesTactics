using System;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public class AudioEntry { public string Id, Usage, SourceMetadata; public AudioClip Clip; }
    [CreateAssetMenu(menuName="Tales Tactics/Audio Library")]
    public class AudioLibrary : ScriptableObject { public AudioEntry[] Entries=new AudioEntry[0]; }
}
