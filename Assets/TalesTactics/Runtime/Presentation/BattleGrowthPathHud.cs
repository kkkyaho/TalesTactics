using System;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowGrowthPath(CharacterData character)
        {
            var p=FormationProgress(character);ShowUnitDetails(new UnitRuntime(character,Team.Player,battle.Catalog.Rules,p.Level));Clear(unitDetails);
            Label(unitDetails,"성장 방향 · "+character.DisplayName,16,40,24);
            Label(unitDetails,"Lv10부터 선택 · 준비 중 무료 변경\n기존 직업·기술·전술 특성을 유지합니다.\n현재: "+GrowthPaths.Name(p.Growth)+"\n"+GrowthPaths.Description(p.Growth),70,112,18);
            foreach(GrowthPath path in Enum.GetValues(typeof(GrowthPath)))
            {
                var choice=path;
                Button(unitDetails,GrowthPaths.Name(path)+" · "+GrowthPaths.Description(path),194+58*(int)path,()=>
                {
                    bool saved=GrowthPaths.Save(battle.Campaign,character,choice,battle.PersistCampaign);
                    CloseUnitDetails();ShowGrowth(character,saved?"성장 방향을 저장했습니다.":"저장하지 못했습니다. 기존 성장 방향을 유지합니다.");
                },p.Level>=10&&!battle.TrainingMode&&battle.CanSave,50);
            }
            Button(unitDetails,"기술 숙련 안내",374,()=>ShowMastery(character),true,30);
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
    }
}
