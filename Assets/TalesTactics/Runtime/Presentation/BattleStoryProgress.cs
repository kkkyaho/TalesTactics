using System;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public bool ResumePendingEnding()
        {
            int stage=Campaign.PendingAfterStoryStage;
            if(StoryActive||!Campaign.HasPendingAfterStory||stage<0||stage>=CampaignStages.Count||!Campaign.StoryProgress.Contains(CampaignStages.Id(stage)))return false;
            OpenStory(stage,true,CompleteRequiredEnding);
            StoryIndex=Math.Min(Math.Max(0,Campaign.PendingAfterStoryLine),storyLines.Length-1);
            Hud.ShowStoryLine(storyLines[StoryIndex],StoryIndex+1,storyLines.Length);return true;
        }
        void CompleteRequiredEnding()
        {
            int stage=Campaign.PendingAfterStoryStage,line=Campaign.PendingAfterStoryLine,selected=SelectedStage;
            if(stage<0){Restart();return;}
            Campaign.HasPendingAfterStory=false;Campaign.PendingAfterStoryStage=-1;Campaign.PendingAfterStoryLine=0;
            if(CampaignStages.HasNext(stage))SelectedStage=stage+1;
            bool saved=false;try{saved=PersistCampaign(Campaign);}catch(Exception){}
            if(!saved)
            {
                Campaign.HasPendingAfterStory=true;Campaign.PendingAfterStoryStage=stage;Campaign.PendingAfterStoryLine=line;SelectedStage=selected;
                ResumePendingEnding();Hud.ShowStorySaveFailure();return;
            }
            Restart();
            if(PersonalScenarios.Contains(stage)){SelectedStage=stage;Hud.ShowPersonalScenarios();}
        }
        bool SaveRewardAndEnding(CampaignSave save)=>SaveRewardAndEnding(save,battleStage);
        bool SaveRewardAndEnding(CampaignSave save,int stage)
        {
            int oldStage=save.PendingAfterStoryStage,oldLine=save.PendingAfterStoryLine;bool saved=false,oldPending=save.HasPendingAfterStory;
            save.HasPendingAfterStory=true;save.PendingAfterStoryStage=stage;save.PendingAfterStoryLine=0;
            try{saved=PersistCampaign(save);return saved;}
            finally{if(!saved){save.HasPendingAfterStory=oldPending;save.PendingAfterStoryStage=oldStage;save.PendingAfterStoryLine=oldLine;}}
        }
    }
}
