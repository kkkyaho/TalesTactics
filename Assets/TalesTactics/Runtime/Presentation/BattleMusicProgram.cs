namespace TalesTactics
{
    // Presentation-only choices: no random draws, combat state or saved stage changes.
    public static class BattleMusicProgram
    {
        public const string Title="title.local", Intermission="intermission.local";
        public const string Spirit="battle.spirit", Sky="battle.sky", GoodWine="battle.good-wine", Decisive="battle.decisive";

        public static string Battle(int stage,bool boss)
        {
            if(stage<0||stage>=CampaignStages.Count)return boss?"boss":"battle";
            if(stage==CrossoverCampaign.LastStage)return Decisive;
            if(PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==2)return Spirit;
            if(boss)return Spirit;
            switch(BattleEnvironmentPalette.Theme(stage))
            {
                case 1:case 4:return GoodWine;
                case 2:case 3:return Sky;
                case 5:return "boss";
                default:return "battle";
            }
        }

        public static string Story(int stage,bool after)
        {
            if(stage<0||stage>=CampaignStages.Count)return "story";
            if(PersonalScenarios.Contains(stage))return PersonalScenarios.Arc(stage).Owner+".theme";
            if(!after&&stage==CrossoverCampaign.LastStage)return Decisive;
            if(!after&&CampaignStages.Get(stage).BossMusic)return Sky;
            return "story";
        }
    }

    public sealed partial class BattleAudio
    {
        public string CurrentMusicId=>currentMusicId;
        public void PlayTitle()=>PlayAvailable(BattleMusicProgram.Title,"story");
        public void PlayIntermission()=>PlayAvailable(BattleMusicProgram.Intermission,"story");
        public void PlayStory(int stage,bool after)=>PlayAvailable(BattleMusicProgram.Story(stage,after),"story");
        public void PlayBattle(bool boss)
        {
            // The running session remains authoritative after resume, rewind and retry.
            var director=GetComponent<BattleDirector>();
            int stage=director!=null&&director.Session!=null?director.Session.CampaignStage:-1;
            PlayAvailable(BattleMusicProgram.Battle(stage,boss),boss&&Entry("boss")?.Clip!=null?"boss":"battle");
        }
        void PlayAvailable(string id,string fallback)=>Play(Entry(id)?.Clip!=null?id:fallback);
    }
}
