using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform tacticalToolbar;
        TMP_Text moveRisk;
        void DrawTacticalTools()
        {
            if(tacticalToolbar==null)tacticalToolbar=Panel("Threat controls",new Vector2(0,1),new Vector2(0,1),new Vector2(12,-122),new Vector2(332,-84));
            tacticalToolbar.gameObject.SetActive(true);Clear(tacticalToolbar);
            Button(tacticalToolbar,battle.ThreatLabel+" · V",3,battle.CycleThreat,true,32);HalfButton(tacticalToolbar,0);
            Button(tacticalToolbar,"◇ 가능 / × 예고",3,()=>ShowTacticalHelp(),true,32);HalfButton(tacticalToolbar,1);
            foreach(var text in tacticalToolbar.GetComponentsInChildren<TMP_Text>())text.fontSize=13;
        }
        void ShowTacticalHelp()
        {
            ShowUnitDetails(battle.Session.Active);if(!UnitDetailsOpen)return;Clear(unitDetails);
            Label(unitDetails,"전술 표시와 조작",16,36,23);
            Label(unitDetails,"◇ 이동 후 공격 가능 · 현재 MP/배치 기준\n× 예고 위치 · 적이 다음 자기 턴에 공격\n사각형: 행동 / 마름모: 선택 / 삼각형: 방향\n청색 테두리: 턴 순서와 연결된 유닛\nM 이동 · A 공격 · S 기술 · G 방어 · W 대기\n방향키: 타일·방향 / Enter: 선택\nTab: 대상·메뉴 / Esc·우클릭: 취소\nZ: 이동 취소 / V: 위험 표시\n이동 뒤 행동하면 이동 취소가 잠깁니다.\n공격 후 새로 한 이동은 취소할 수 있습니다.",66,320,17);
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
        public void ShowMoveRisk(Vector2Int p,int enemies)
        {if(moveRisk!=null)moveRisk.text="도착 "+p+" · "+(enemies>0?"◇ 적 "+enemies+"명 공격 가능":"현재 위험 범위 밖");}
        void DrawTargetForecast()
        {
            var u=battle.Session.Active;var s=battle.SelectedSkill;var f=battle.Forecast;
            var name=Label(commands,s.DisplayName+" · MP "+battle.Session.Resolver.MPCost(u,s),6,26,15);name.enableAutoSizing=true;name.fontSizeMin=11;name.fontSizeMax=15;
            Label(commands,"사거리 "+s.MinRange+"–"+s.Range+" · "+SkillSummary.Describe(s),34,26,13);
            string text=f==null?"대상 위에 커서를 올리면 결과 예측\n대상 클릭으로 즉시 사용":string.Join("\n",f.Rows.Take(2).Select(r=>r.Text.Split('\n')[0]));
            var preview=Label(commands,text,64,48,14);preview.enableAutoSizing=true;preview.fontSizeMin=11;preview.fontSizeMax=14;
            targetContextHeight=180;
            if(f!=null)Button(commands,"전체 예측 · "+f.Rows.Count+"명 / 비용",118,()=>ShowForecast(0),true,28);
            Label(commands,"클릭/Enter 사용 · Tab 전환 · Esc 취소",152,26,12);
        }
        void ShowForecast(int page)
        {
            var forecast=battle.Forecast;if(forecast==null)return;
            ShowUnitDetails(battle.Session.Active);if(!UnitDetailsOpen)return;Clear(unitDetails);
            Label(unitDetails,"전체 전투 예측",16,36,23);
            Label(unitDetails,forecast.Cost+"\n"+forecast.Note,60,66,16);
            var text=Label(unitDetails,string.Join("\n\n",forecast.Rows.Skip(page*3).Take(3).Select(r=>r.Text)),132,230,18);text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=18;
            Button(unitDetails,"이전 대상",364,()=>ShowForecast(page-1),page>0,32);HalfButton(unitDetails,0);
            Button(unitDetails,"다음 대상",364,()=>ShowForecast(page+1),(page+1)*3<forecast.Rows.Count,32);HalfButton(unitDetails,1);
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
        void DrawDevelopment(CharacterData c)
        {
            Button(commands,"전술 특성 · "+TacticalDevelopment.TraitName(battle.Campaign.Get(c.Id).Trait),476,()=>ShowTraits(c),true,38);
            bool behind=battle.Campaign.Get(c.Id).Level<CampaignStages.Get(battle.SelectedStage).EntryLevel;
            Button(commands,"합류 훈련 · Lv"+CampaignStages.Get(battle.SelectedStage).EntryLevel,524,()=>
            {
                bool saved=TacticalDevelopment.CatchUp(battle.Campaign,c,battle.SelectedStage,battle.PersistCampaign);
                ShowGrowth(c,saved?"합류 훈련을 저장했습니다.":"훈련을 저장하지 못했습니다. 기존 성장을 유지합니다.");
            },behind&&!battle.TrainingMode&&battle.CanSave,38);
            Label(left,TacticalDevelopment.MasteryDescription,478,100,17);
        }
        void ShowTraits(CharacterData c)
        {
            BeginPreparation(c.DisplayName+" · 전술 특성");
            Label(left,"특성은 한 가지만 적용됩니다.\n변경은 준비 화면에서 무료이며\n저장 후 다음 전투에 적용됩니다.\n\n보호는 방어 중 인접한 동료에게\n자기 턴마다 한 번 적용됩니다.\n여러 보호자는 중첩되지 않습니다.",18,280,19);
            foreach(TacticalTrait trait in System.Enum.GetValues(typeof(TacticalTrait)))
            {
                var selected=trait;bool current=battle.Campaign.Get(c.Id).Trait==trait;
                Button(center,(current?"● ":"")+TacticalDevelopment.TraitName(trait)+" · 저장",20+(int)trait*110,()=>
                {TacticalDevelopment.SaveTrait(battle.Campaign,c,selected,battle.PersistCampaign);ShowTraits(c);},battle.CanSave&&!battle.TrainingMode,38);
                Label(center,TacticalDevelopment.TraitDescription(trait),64+(int)trait*110,52,17);
            }
            Button(commands,"성장으로",20,()=>ShowGrowth(c));Button(commands,"출전 준비로",76,ShowDeployment);
            Label(footer,battle.SaveNotice,14,70,18);
        }
    }
}
