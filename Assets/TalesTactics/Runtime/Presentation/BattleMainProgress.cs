namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public void SelectCurrentMain()
        {
            if(Session!=null||StoryActive||TrainingMode||!Catalog.CrossoverReady)return;
            int stage=CrossoverCampaign.FirstStage;
            while(stage<CrossoverCampaign.LastStage&&Campaign.StoryProgress.Contains(CampaignStages.Id(stage)))stage++;
            if(SelectedStage!=stage){SelectedStage=stage;NormalizeDeployment();}
        }
        public void ReplayMainEnding(int stage)
        {
            if(Session!=null||StoryActive||!CrossoverCampaign.IsMain(stage)||!Campaign.StoryProgress.Contains(CampaignStages.Id(stage)))return;
            OpenStory(stage,true,Hud.ShowIntermissionHub);
        }
    }
}
