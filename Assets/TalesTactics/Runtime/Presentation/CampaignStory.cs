using System;
using System.Linq;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public bool StoryActive { get; private set; }
        public int StoryIndex { get; private set; }
        public int StoryStage { get; private set; }
        public StoryLine[] ReadStoryHistory() => storyLines == null ? Array.Empty<StoryLine>() : storyLines.Take(StoryIndex + 1).ToArray();
        StoryLine[] storyLines;
        Action storyFinished;
        public bool CanReadEnding => Session != null && !battleTraining && !RewardPending && Session.Result == BattleResult.Victory;

        public void RequestBattle()
        {
            if(Campaign.HasPendingAfterStory){ResumePendingEnding();return;}
            if (StoryActive || Session != null || Deployment.Count == 0 || !TrainingMode && !CampaignStages.Unlocked(Campaign, SelectedStage)) return;
            if (TrainingMode) { BeginBattle(); return; }
            if(!CanPlayCampaignStage(SelectedStage))return;
            if(PersonalScenarios.Contains(SelectedStage)&&Campaign.StoryProgress.Contains(PersonalScenarios.Flag(SelectedStage))){ReplayStory(true);return;}
            NormalizeDeployment();OpenStory(SelectedStage, false, BeginScenarioAfterStory);
        }
        public void ReplayStory(bool after)
        {
            if (StoryActive || Session != null || !CanPlayCampaignStage(SelectedStage) || !CampaignStages.Unlocked(Campaign, SelectedStage) ||
                after && !Campaign.StoryProgress.Contains(CampaignStages.Id(SelectedStage))) return;
            OpenStory(SelectedStage, after, Hud.ShowIntermissionHub);
        }
        public void ReadEnding()
        {
            if (StoryActive || !CanReadEnding) return;
            if(!ResumePendingEnding())OpenStory(battleStage,true,()=>{Restart();});
        }
        void OpenStory(int stage, bool after, Action finished)
        {
            storyLines = CampaignContent.Story(stage, after); storyFinished = finished;
            if(after&&(PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==1||stage==18))
            {
                string prefix=CampaignStages.Id(stage)+".route.";bool left=Campaign.StoryProgress.Contains(prefix+"left"),right=Campaign.StoryProgress.Contains(prefix+"right");
                if(left||right){int choice=left?0:1;storyLines=storyLines.Concat(new[]{new StoryLine("선택의 기록",PersonalScenarios.RouteName(stage,choice)+"을 먼저 연결했다. 동료들이 그 길을 지키는 동안 "+PersonalScenarios.RouteName(stage,1-choice)+"까지 이어 모두의 귀환로를 확보했다.")}).ToArray();}
            }
            StoryIndex = 0; StoryStage = stage; StoryActive = true; Audio.Play("story");
            Hud.ShowStoryLine(storyLines[StoryIndex], StoryIndex + 1, storyLines.Length);
        }
        public void AdvanceStory()
        {
            if (!StoryActive) return;
            if(StoryIndex+1>=storyLines.Length){FinishStory();return;}
            if(Campaign.HasPendingAfterStory&&Campaign.PendingAfterStoryStage==StoryStage)
            {
                int previous=Campaign.PendingAfterStoryLine;Campaign.PendingAfterStoryLine=StoryIndex+1;
                bool saved=false;try{saved=PersistCampaign(Campaign);}catch(Exception){}
                if(!saved){Campaign.PendingAfterStoryLine=previous;Hud.ShowStorySaveFailure();return;}
            }
            StoryIndex++;
            Hud.ShowStoryLine(storyLines[StoryIndex], StoryIndex + 1, storyLines.Length);
        }
        public void FinishStory()
        {
            if (!StoryActive) return;
            var finished = storyFinished; CloseStory(); finished?.Invoke();
        }
        void CloseStory()
        {
            StoryActive = false; storyFinished = null; storyLines = null; StoryIndex = 0;
            if (Hud != null) Hud.HideStory();
        }
    }

}
