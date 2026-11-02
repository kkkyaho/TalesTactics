using System;
using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    // Recruitment is committed on entry to the introduction battle, never by browsing preparation.
    // Existing completion flags and checkpoints remain valid without a save-version migration.
    public static class CampaignRecruitment
    {
        public static string Flag(int episode)=>"crossover.recruited."+episode;
        public static bool Joined(CampaignSave save,string id)
        {
            int episode=CrossoverCampaign.Introduction(id);
            if(episode==1)return true;
            if(episode<1||save==null)return false;
            if(save.StoryProgress.Contains(Flag(episode)))return true;
            if(CompletedIntroduction(save,episode))return true;
            return save.SuspendedBattle!=null&&CrossoverCampaign.IsMain(save.SuspendedBattle.Stage)&&
                CrossoverCampaign.Episode(save.SuspendedBattle.Stage).Number>=episode;
        }
        static bool CompletedIntroduction(CampaignSave save,int episode)=>Enumerable.Range(episode,CrossoverCampaign.EpisodeCount-episode+1)
            .Any(n=>save.StoryProgress.Contains(CrossoverCampaign.Flag(n)));
        public static int PriorRosterLevel(CampaignSave save,BattleCatalog catalog,int episode)
        {
            var prior=catalog.Characters.Where(c=>CrossoverCampaign.Introduction(c.Id)>0&&
                CrossoverCampaign.Introduction(c.Id)<episode&&Joined(save,c.Id)).ToArray();
            if(prior.Length==0)return 1;
            // Integer division deliberately rounds down; reserves count and missing records are level one.
            return prior.Sum(c=>Math.Max(1,Math.Min(50,save.Characters.Find(p=>p.Id==c.Id)?.Level??1)))/prior.Length;
        }
        public static int[] BattleDeployment(CampaignSave save,BattleCatalog catalog,int stage,IEnumerable<int> selected)
        {
            var party=selected.Where(i=>i>=0&&i<catalog.Characters.Length).Distinct().ToList();
            if(!catalog.CrossoverReady||!CrossoverCampaign.IsMain(stage))return party.Take(catalog.Rules.MaxDeployment).ToArray();
            int episode=CrossoverCampaign.Episode(stage).Number;
            var forced=episode<=1||save.StoryProgress.Contains(CrossoverCampaign.Flag(episode))?Array.Empty<int>():
                Enumerable.Range(0,catalog.Characters.Length).Where(i=>CrossoverCampaign.Introduction(catalog.Characters[i].Id)==episode).ToArray();
            if(forced.Length>catalog.Rules.MaxDeployment)throw new InvalidOperationException("Introduction party exceeds deployment capacity.");
            return party.Where(i=>!forced.Contains(i)&&Joined(save,catalog.Characters[i].Id)&&CrossoverCampaign.Joined(catalog.Characters[i].Id,episode))
                .Take(catalog.Rules.MaxDeployment-forced.Length).Concat(forced).ToArray();
        }
        public static bool TryBegin(CampaignSave save,BattleCatalog catalog,int stage,Func<CampaignSave,bool> persist)
        {
            if(!catalog.CrossoverReady||!CrossoverCampaign.IsMain(stage))return true;
            int episode=CrossoverCampaign.Episode(stage).Number;
            var recruits=catalog.Characters.Where(c=>CrossoverCampaign.Introduction(c.Id)==episode&&!Joined(save,c.Id)).ToArray();
            if(episode<=1||recruits.Length==0)return true;
            if(persist==null||!CampaignStages.Unlocked(save,stage))return false;
            int level=PriorRosterLevel(save,catalog,episode);
            var originalCharacters=save.Characters.ToArray();
            var progress=recruits.Select(c=>save.Get(c.Id)).ToArray();
            var levels=progress.Select(p=>p.Level).ToArray();var skills=progress.Select(p=>p.UnlockedSkills).ToArray();
            bool saved=false;string flag=Flag(episode);save.StoryProgress.Add(flag);
            try
            {
                for(int i=0;i<recruits.Length;i++)
                {
                    // Preserve existing higher legacy growth, EXP, equipment, promotion and traits.
                    progress[i].Level=Math.Max(progress[i].Level,level);
                    progress[i].UnlockedSkills=progress[i].UnlockedSkills.Concat(recruits[i].Skills.Where(s=>s!=null&&s.UnlockLevel<=progress[i].Level).Select(s=>s.Id)).Distinct().ToList();
                }
                return saved=persist(save);
            }
            catch(Exception){return false;}
            finally
            {
                if(!saved)
                {
                    for(int i=0;i<progress.Length;i++){progress[i].Level=levels[i];progress[i].UnlockedSkills=skills[i];}
                    save.Characters.Clear();save.Characters.AddRange(originalCharacters);save.StoryProgress.Remove(flag);
                }
            }
        }
        // Any preparation save can consume a legacy checkpoint. Preserve its joined roster
        // in the same transaction, before removing the only remaining evidence of arrival.
        public static bool TryConsumeCheckpoint(CampaignSave save,Func<CampaignSave,bool> persist)
        {
            var checkpoint=save.SuspendedBattle;bool hadCheckpoint=save.HasSuspendedBattle;
            var added=new List<string>();bool saved=false;
            if(checkpoint!=null&&CrossoverCampaign.IsMain(checkpoint.Stage))
                for(int episode=2;episode<=Math.Min(5,CrossoverCampaign.Episode(checkpoint.Stage).Number);episode++)
                    if(!CompletedIntroduction(save,episode)&&!save.StoryProgress.Contains(Flag(episode)))
                    {save.StoryProgress.Add(Flag(episode));added.Add(Flag(episode));}
            save.SuspendedBattle=null;save.HasSuspendedBattle=false;
            try{return saved=persist!=null&&persist(save);}catch(Exception){return false;}
            finally
            {
                if(!saved)
                {
                    save.SuspendedBattle=checkpoint;save.HasSuspendedBattle=hadCheckpoint;
                    foreach(string flag in added)save.StoryProgress.Remove(flag);
                }
            }
        }
    }
}
