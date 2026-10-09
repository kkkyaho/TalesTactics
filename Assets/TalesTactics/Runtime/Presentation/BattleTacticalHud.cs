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
            Button(tacticalToolbar,"전술 도움말",3,()=>ShowTacticalHelp(),true,32);HalfButton(tacticalToolbar,1);
            foreach(var text in tacticalToolbar.GetComponentsInChildren<TMP_Text>())text.fontSize=13;
        }
        void ShowTacticalHelp()
        {
            ShowUnitDetails(battle.Session.Active);if(!UnitDetailsOpen)return;Clear(unitDetails);
            Label(unitDetails,"전술 표시와 조작",16,36,23);
            Label(unitDetails,"◇ 이동 후 공격 가능 · 현재 MP/배치 기준\n× 예고 위치 · 적이 다음 자기 턴에 공격\n사각형: 행동 / 마름모: 선택 / 삼각형: 방향\n청색 테두리: 턴 순서와 연결된 유닛\nM 이동 · A 공격 · S 기술 · G 방어 · W 대기\n방향키: 타일·방향 / Enter: 선택\nTab: 대상·메뉴 / Esc·우클릭: 취소\nZ: 이동 취소 / V: 위험 표시\n이동 뒤 행동하면 이동 취소가 잠깁니다.\n메뉴 F5: 행동 되감기 · 전투당 3회\n반격/지원: 능력창에서 남은 횟수 확인",66,320,17);
            Button(unitDetails,"닫기",410,()=>CloseUnitDetails());
        }
        public void ShowMoveRisk(Vector2Int p,int enemies)
        {if(moveRisk!=null)moveRisk.text="도착 "+p+" · "+(enemies>0?"◇ 적 "+enemies+"명 공격 가능":"현재 위험 범위 밖");}
        BattleForecast shownForecast;
        int forecastRowIndex;
        void DrawTargetForecast()
        {
            commands.GetComponent<UnityEngine.UI.Image>().color=new Color(.035f,.065f,.11f,1);
            var u=battle.Session.Active;var skill=battle.SelectedSkill;var f=battle.Forecast;
            if(shownForecast!=f){shownForecast=f;forecastRowIndex=0;}
            var name=Label(commands,skill.DisplayName,10,34,21);name.rectTransform.offsetMax=new Vector2(-66,name.rectTransform.offsetMax.y);
            Button(commands,"예측 취소",8,()=>battle.State.Cancel(),true,36);var back=(RectTransform)commands.GetChild(commands.childCount-1);back.anchorMin=new Vector2(1,1);back.sizeDelta=new Vector2(44,36);back.anchoredPosition=new Vector2(-28,-8);back.GetComponentInChildren<TMP_Text>().text="×";
            var costs=new System.Collections.Generic.List<string>();int mp=battle.Session.Resolver.MPCost(u,skill),hp=battle.Session.Resolver.HPCost(u,skill);
            if(mp>0)costs.Add("MP "+mp);if(hp>0)costs.Add("HP "+hp);if(skill.GaugeCost>0)costs.Add("SP "+skill.GaugeCost);
            if(f==null||f.Rows.Count==0)
            {
                Label(commands,f==null?"대상을 선택하세요":f.Note,64,60,19);Label(commands,"사거리 "+skill.MinRange+"–"+skill.Range,128,32,18);
                Label(commands,string.Join(" · ",costs),178,42,18);targetContextHeight=240;return;
            }
            var targets=battle.Target.HasValue?battle.Session.Resolver.Targets(u,skill,battle.Target.Value).ToArray():new UnitRuntime[0];
            var rows=f.Rows.OrderByDescending(r=>targets.Contains(r.Unit)).ThenBy(r=>r.Unit.Team==u.Team).ToArray();
            forecastRowIndex=Mathf.Clamp(forecastRowIndex,0,rows.Length-1);var row=rows[forecastRowIndex];
            InfoPortrait(commands,row.Unit.Data,14,58,88,108);
            var enemy=Label(commands,row.Unit.Data.DisplayName,56,34,19);enemy.rectTransform.offsetMin=new Vector2(116,enemy.rectTransform.offsetMin.y);
            string caption=row.AfterHP>row.BeforeHP?"예상 회복":row.DirectDamage?"예상 피해":"효과 변화";
            var kind=Label(commands,caption,96,28,16);kind.rectTransform.offsetMin=new Vector2(116,kind.rectTransform.offsetMin.y);
            var amount=Label(commands,row.Immune?"면역":row.AfterHP==row.BeforeHP&&!row.DirectDamage?"—":Mathf.Abs(row.BeforeHP-row.AfterHP).ToString(),112,86,46);amount.rectTransform.offsetMin=new Vector2(116,amount.rectTransform.offsetMin.y);amount.color=row.AfterHP>row.BeforeHP?new Color(.35f,.9f,.7f):new Color(1,.43f,.4f);
            Label(commands,"HP "+row.BeforeHP+" → "+row.AfterHP+(row.AfterHP==0?" · 전투불능":""),194,30,18);ForecastHealthBar(commands,row,230);
            var notices=new System.Collections.Generic.List<string>();
            if(!string.IsNullOrEmpty(f.Reactions))notices.Add(f.Reactions);
            if(row.Unit.BossWard)notices.Add("방벽 "+row.BeforeWard+"%"+(row.BeforeWard!=row.AfterWard?" → "+row.AfterWard+"%":""));
            if(!string.IsNullOrEmpty(row.ImportantEffects))notices.Add(row.ImportantEffects);
            float lower=258;
            if(notices.Count>0){float height=notices.Count>1||!string.IsNullOrEmpty(row.ImportantEffects)?72:34;var alerts=Label(commands,string.Join(" · ",notices),lower,height,16);alerts.enableAutoSizing=true;alerts.fontSizeMin=14;alerts.fontSizeMax=16;lower+=height+8;}
            if(costs.Count>0){Label(commands,string.Join(" · ",costs),lower,30,17);lower+=38;}
            if(rows.Length>1)
            {
                Button(commands,"예측 이전 대상",lower,()=>{forecastRowIndex=(forecastRowIndex+rows.Length-1)%rows.Length;battle.Hud.Refresh();},true,34);HalfButton(commands,0);
                Button(commands,"예측 다음 대상",lower,()=>{forecastRowIndex=(forecastRowIndex+1)%rows.Length;battle.Hud.Refresh();},true,34);HalfButton(commands,1);
                var buttons=commands.GetComponentsInChildren<UnityEngine.UI.Button>();buttons[buttons.Length-2].GetComponentInChildren<TMP_Text>().text="‹  대상 "+(forecastRowIndex+1)+" / "+rows.Length;buttons[buttons.Length-1].GetComponentInChildren<TMP_Text>().text="다음 대상  ›";lower+=42;
            }
            float detailsY=lower;
            Button(commands,"전체 예측 · "+rows.Length+"명 / 비용",detailsY,()=>ShowForecast(0),true,36);commands.GetChild(commands.childCount-1).GetComponentInChildren<TMP_Text>().text="상세 보기  ›";
            targetContextHeight=detailsY+50;
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
        void ShowTraits(CharacterData c) => ShowGrowth(c);
    }
}
