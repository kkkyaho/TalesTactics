using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        public void ShowEnemySummary(UnitRuntime unit)
        {
            if(unit==null||unit.Team!=Team.Enemy||!battle.IsPlayerCommand||battle.TutorialActive||InputModalOpen)return;
            PrepareUnitModal(unit);Clear(unitDetails);
            var content=FormationRect(unitDetails,"EnemyInspectionContent",new Vector2(.03f,.03f),new Vector2(.97f,.97f));UpdateTesterFeedbackLayout();
            FormationText(content,"적 정보",new Vector2(0,.85f),new Vector2(.85f,1),24).color=formationGold;
            CloseIcon(unitDetails,"적 정보 닫기",()=>CloseUnitDetails());
            StatusPortrait(FormationRect(content,"EnemyPortrait",new Vector2(0,.60f),new Vector2(.24f,.82f)),unit.Data);
            FormationText(content,unit.Data.DisplayName+" · Lv"+unit.Level,new Vector2(.28f,.72f),new Vector2(1,.83f),23);
            FormationText(content,$"HP {unit.CurrentHP}/{unit.Stats.HP}    MP {unit.CurrentMP}/{unit.Stats.MP}",new Vector2(.28f,.60f),new Vector2(1,.71f),18);
            FormationText(content,EnemyTactics.Describe(unit)+(unit.CrossoverBoss||unit.BossWard?"":"\n"+EnemyRoles.Advice(unit.Data)),new Vector2(0,.18f),new Vector2(1,.57f),18);
            FormationButton(content,"적 기술 보기","기술 보기",Vector2.zero,new Vector2(.48f,.14f),()=>ShowUnitSkills(unit));
            FormationButton(content,"적 조회 닫기","닫기",new Vector2(.52f,0),new Vector2(1,.14f),()=>CloseUnitDetails());
        }
        public void ShowUnitSkills(UnitRuntime unit)
        {
            if(unit==null||HelpOpen||SystemMenuOpen||MissionOpen)return;
            ShowUnitDetails(unit);if(!CharacterStatusOpen||statusUnit!=unit)return;
            statusTab=1;statusPage=0;RenderCharacterStatus();
            if(unit.Team==Team.Enemy&&battle.Session!=null&&battle.Session.Units.Contains(unit))battle.LearnContextHint(ContextHintKind.EnemySkills);
        }
        UnitRuntime MissionEnemyPreview(CharacterData data,int index,int stage,bool training)
        {
            // A detached rules snapshot supplies the same tactical skills as entering battle.
            // It never replaces the active session or reads/writes a campaign save.
            var preview=new BattleSession(battle.Catalog,battle.Deployment.DefaultIfEmpty(0),training?25:1,training?25:CampaignStages.EnemyLevel(stage),training?-1:stage,
                battle.UseCT,battle.UseUtilityAI,training?battle.TrainingObjective:ObjectiveKind.Eliminate,
                teamTurns:!battle.UseCT&&!battle.UseFixedSpeedOrder,phaseSurvival:true,tacticalCombat:true,bossEncounters:true,
                difficulty:battle.Preferences.Difficulty,missionEvents:battle.Preferences.MissionEvents,reactions:battle.Preferences.Reactions,expandedEncounters:true);
            var enemy=preview.Units.Where(u=>u.Team==Team.Enemy).ElementAtOrDefault(index);
            if(enemy!=null&&enemy.Data==data)return enemy;
            enemy=new UnitRuntime(data,Team.Enemy,battle.Catalog.Rules,training?25:DifficultyRules.EnemyLevel(CampaignStages.EnemyLevel(stage),battle.Preferences.Difficulty));
            if(!training)EnemyTactics.ConfigureUnit(enemy,battle.Catalog);return enemy;
        }
    }
}
