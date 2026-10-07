using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TalesTactics
{
    public sealed partial class BattleAudio
    {
        public bool PersonalMusicEnabled=true;
        public static string PersonalMusicDirectory=>Path.Combine(Application.persistentDataPath,"PersonalMusic");
        readonly Dictionary<string,AudioClip> personalClips=new Dictionary<string,AudioClip>();
        string currentMusicId,resumeMusicId;
        public int LoadedPersonalTracks=>personalClips.Count;

        void Start()
        {
            if(PersonalMusicEnabled)
            {
                StartCoroutine(LoadPersonalTrack("battle"));
                StartCoroutine(LoadPersonalTrack("story"));
            }
        }
        static string PersonalKey(string id)
        {
            if(id=="battle"||id=="boss"||id!=null&&id.EndsWith(".theme"))return "battle";
            if(id=="story"||id=="victory")return "story";
            return null;
        }
        AudioClip MusicClip(string id)
        {
            var original=Entry(id)?.Clip;var key=PersonalKey(id);
            return original!=null&&PersonalMusicEnabled&&key!=null&&personalClips.TryGetValue(key,out var clip)?clip:original;
        }
        IEnumerator LoadPersonalTrack(string key)
        {
            var path=Path.Combine(PersonalMusicDirectory,key+".mp3");
            if(!File.Exists(path))yield break;
            using(var request=UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri,AudioType.MPEG))
            {
                request.timeout=30;
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Personal music could not be loaded: "+key+". Keeping the original music.");
                    yield break;
                }
                var clip=DownloadHandlerAudioClip.GetContent(request);
                if(clip==null||clip.samples==0)yield break;
                clip.name="Personal music: "+key;personalClips[key]=clip;
                if(!PersonalMusicEnabled)yield break;
                if(PersonalKey(resumeMusicId)==key){resumeClip=clip;resumeSample=0;}
                var source=GetComponent<AudioSource>();
                if(PersonalKey(currentMusicId)==key&&source.isPlaying)
                {
                    source.clip=clip;source.Play();
                }
            }
        }
        void OnDestroy()
        {
            foreach(var clip in personalClips.Values)if(clip!=null)Destroy(clip);
            personalClips.Clear();
        }
    }
}
