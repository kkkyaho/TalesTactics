#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
namespace TalesTactics
{
    public static class MenuPreviewBake
    {
        public static IEnumerator Run(BattleDirector battle,string directory)
        {
            Directory.CreateDirectory(directory);
            for(int n=1;n<=20;n++)if(!battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(n)))battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            for(int stage=CrossoverCampaign.FirstStage;stage<=CrossoverCampaign.LastStage;stage++)
            {
                battle.SelectedStage=stage;battle.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
                var camera=battle.Board.BattleCamera;var rt=new RenderTexture(640,360,24);
                var oldTarget=camera.targetTexture;var oldPosition=camera.transform.position;float oldSize=camera.orthographicSize;var oldActive=RenderTexture.active;
                try
                {
                    camera.targetTexture=rt;camera.orthographicSize=10;
                    camera.transform.position=new Vector3(6,1,6)-camera.transform.forward*24;
                    camera.Render();RenderTexture.active=rt;
                    var capture=new Texture2D(640,360,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,640,360),0,0);capture.Apply();
                    File.WriteAllBytes(Path.Combine(directory,"Stage"+stage+".png"),capture.EncodeToPNG());Object.Destroy(capture);
                }
                finally{camera.targetTexture=oldTarget;camera.transform.position=oldPosition;camera.orthographicSize=oldSize;RenderTexture.active=oldActive;rt.Release();Object.Destroy(rt);}
                battle.Restart();yield return null;
            }
            battle.Hud.ShowTitle();battle.StartTitleGame(0);
        }
    }
}
#endif
