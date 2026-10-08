using System.Linq;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowPresets(string notice=null)
        {
            if(battle.Session!=null)return;
            ShowUnitDetails(new UnitRuntime(battle.Catalog.Characters[0],Team.Player,battle.Catalog.Rules));Clear(unitDetails);
            Label(unitDetails,"편성 · 장비 프리셋",16,36,24);
            Label(unitDetails,notice??"출전 순서와 전체 캐릭터의 장비를 보관합니다.\n불러오면 현재 편성과 장비를 교체하고 저장합니다.",60,64,17);
            for(int i=0;i<3;i++)
            {
                int slot=i;var preset=battle.Campaign.Presets!=null&&i<battle.Campaign.Presets.Length?battle.Campaign.Presets[i]:null;
                var membersLabel=Label(unitDetails,"슬롯 "+(i+1)+(preset?.Members==null?" · 비어 있음":" · "+string.Join(", ",preset.Members.Select(id=>battle.Catalog.Characters.FirstOrDefault(c=>c.Id==id)?.DisplayName.Split(' ')[0]??id))),134+i*85,28,15);membersLabel.enableAutoSizing=true;membersLabel.fontSizeMin=11;membersLabel.fontSizeMax=15;
                Button(unitDetails,preset==null?"현재 편성 저장":"현재 편성으로 덮어쓰기",164+i*85,()=>ShowPresets(FormationPresets.Store(battle.Campaign,battle.Catalog,battle.Deployment.ToArray(),slot,battle.PersistCampaign)?"프리셋을 저장했습니다.":"저장 실패. 기존 프리셋을 유지합니다."),battle.CanSave&&!battle.TrainingMode&&battle.Deployment.Count>0,34);HalfButton(unitDetails,0);
                Button(unitDetails,"불러오기 · 적용",164+i*85,()=>
                {
                    var reason=FormationPresets.Apply(battle.Campaign,battle.Catalog,slot,battle.PersistCampaign,out var members);
                    if(reason!=null){ShowPresets(reason);return;}
                    battle.Deployment.Clear();battle.Deployment.AddRange(members);CloseUnitDetails();ShowDeployment();
                },preset!=null&&battle.CanSave&&!battle.TrainingMode,34);HalfButton(unitDetails,1);
            }
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
    }
}
