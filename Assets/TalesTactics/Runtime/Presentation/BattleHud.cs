using System;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed class BattleHud:MonoBehaviour
    {
        public TMP_FontAsset Font;
        BattleDirector battle;
        RectTransform canvas,left,commands,header,footer;
        TMP_Text message,timing;
        readonly Color panel=new Color(0.045f,0.07f,0.1f,0.96f);
        public void Initialize(BattleDirector b)
        {
            battle=b;canvas=GetComponent<RectTransform>();
            if(Font==null)
            {
                Font=TMP_FontAsset.CreateFontAsset("Malgun Gothic","Regular",48);
                if(Font==null)Font=TMP_FontAsset.CreateFontAsset("Arial","Regular",48);
                if(Font==null)Font=TMP_Settings.defaultFontAsset;
            }
            header=Panel("Header",new Vector2(0,1),new Vector2(1,1),new Vector2(12,-94),new Vector2(-12,-12));
            left=Panel("Unit",new Vector2(0,0),new Vector2(0,1),new Vector2(12,138),new Vector2(280,-106));
            commands=Panel("Commands",new Vector2(1,0),new Vector2(1,1),new Vector2(-300,138),new Vector2(-12,-106));
            footer=Panel("Message",Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,126));
        }
        RectTransform Panel(string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {var g=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(canvas,false);var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;var image=g.GetComponent<UnityEngine.UI.Image>();image.color=panel;image.raycastTarget=true;return r;}
        void Clear(Transform parent){foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        TMP_Text Label(Transform parent,string value,float y,float height=40,int size=18)
        {
            var g=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(0.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(-28,height);
            var t=g.GetComponent<TextMeshProUGUI>();t.font=Font;t.text=value;t.fontSize=size;t.color=new Color(0.9f,0.94f,0.98f);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        void Button(Transform parent,string value,float y,Action click,bool enabled=true,float height=38)
        {
            var g=new GameObject(value,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(0.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(-24,height);
            g.GetComponent<UnityEngine.UI.Image>().color=new Color(0.12f,0.23f,0.29f);var button=g.GetComponent<UnityEngine.UI.Button>();button.interactable=enabled;button.onClick.AddListener(()=>click());var label=Label(g.transform,value,3,height-3,16);label.alignment=TextAlignmentOptions.Center;if(!enabled)label.color=Color.gray;
        }
        public void ShowDeployment()
        {
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS     ·     출전 준비",12,34,27);Label(header,"유적의 경계   /   모든 적 격파   /   최대 6명",49,28,16);
            Label(left,"ROSTER  ·  "+battle.Deployment.Count+" / 6",12,30,19);
            for(int i=0;i<battle.Catalog.Characters.Length;i++){int index=i;var c=battle.Catalog.Characters[i];Button(left,(battle.Deployment.Contains(i)?"● ":"○ ")+c.DisplayName,50+i*39,()=>{if(battle.Deployment.Contains(index))battle.Deployment.Remove(index);else if(battle.Deployment.Count<battle.Catalog.Rules.MaxDeployment)battle.Deployment.Add(index);ShowDeployment();},true,34);}
            Label(commands,"BATTLE SETTINGS",12,35,20);
            Button(commands,battle.TrainingMode?"훈련: Lv25 / 모든 일반 스킬":"캠페인: 저장된 성장 사용",58,()=>{battle.TrainingMode=!battle.TrainingMode;ShowDeployment();});
            Button(commands,battle.Campaign.AutoTiming?"파라 타이밍: 자동":"파라 타이밍: 수동",106,()=>{battle.Campaign.AutoTiming=!battle.Campaign.AutoTiming;CampaignStorage.Save(battle.Campaign);ShowDeployment();});
            Button(commands,"전투 시작",166,()=>battle.SetState(new BattleStartState(battle)),battle.Deployment.Count>0,48);
            Label(commands,"아트: 교체용 플레이스홀더\n음악: Audio Library에서 연결\n\n훈련 전투는 성장 저장을 변경하지 않습니다.",235,200,17);
            Label(footer,"조작: 타일 클릭 → 목표 미리보기 → 실행  |  이동/행동 순서 자유  |  ESC: 취소\n기본 스킬은 레벨에 따라 해금됩니다. 훈련 모드로 전체 일반 스킬을 확인할 수 있습니다.",14,88,18);
        }
        public void Refresh()
        {
            if(battle.Session==null)return;
            Clear(header);Clear(left);Clear(commands);Clear(footer);var u=battle.Session.Active;
            Label(header,"TALES / TACTICS     ·     "+battle.State.Title,10,34,25);
            Label(header,"NEXT   "+string.Join("  →  ",battle.Session.Scheduler.Preview(battle.Session.Units).Take(5).Select(x=>x.Data.DisplayName)),48,30,16);
            message=Label(footer,battle.Message,12,94,17);
            if(u==null)return;
            Label(left,u.Data.DisplayName,16,58,25);Label(left,u.Data.Job+" / Lv"+u.Level+" / "+u.Team,78,36,17);
            Label(left,$"HP  {u.CurrentHP} / {u.Stats.HP}\nMP  {u.CurrentMP} / {u.Stats.MP}\nGAUGE  {u.SpecialGauge} / 100\n\nSPD {u.Stats.SPD}   MOV {u.Stats.MOV}   JMP {u.Stats.JMP}\n\n이동: {(u.Moved?"사용":"가능")}\n행동: {(u.Acted?"사용":"가능")}\n\n{string.Join(" / ",u.Statuses.Select(s=>s.Kind+" "+s.Turns))}",125,285,20);
            Label(commands,battle.State.Title,12,36,20);
            if(battle.State is BattleEndState){Label(commands,battle.Session.Result.ToString(),70,60,32);Button(commands,"출전 화면 / Restart",150,battle.Restart);return;}
            if(battle.State is CommandState&&u.Team==Team.Player)
            {
                Button(commands,"Move / 이동",55,battle.MoveCommand,!u.Moved);Button(commands,"Attack / 공격",101,battle.AttackCommand,!u.Acted);
                Button(commands,"Skill / 스킬",147,battle.SkillCommand,!u.Acted||u.FlamingChain);Button(commands,"Guard / 가드",193,battle.Guard,!u.Acted);
                Button(commands,"Undo Move / 이동 취소",239,battle.Undo,u.CanUndoMove);Button(commands,"Wait / 방향 선택",285,battle.WaitCommand);Button(commands,"Restart",385,battle.Restart);
            }
            else if(battle.State is ActionSelectionState)
            {
                int i=0;foreach(var s in u.Data.Skills.Concat(new[]{u.Data.UltimateSkill}).Where(x=>x!=null))
                {
                    var skill=s;string error=battle.Session.Resolver.CanUse(u,s,battle.IsFollowup(s));
                    Button(commands,s.DisplayName+" · MP"+s.MPCost+(s.HPPercentCost>0?" HP"+Mathf.RoundToInt(s.HPPercentCost*100)+"%":"")+(u.Unlocked(s)?"":" Lv"+s.UnlockLevel),48+i*40,()=>battle.SelectSkill(skill),error==null,35);i++;
                }
                Button(commands,"취소",48+i*40,()=>battle.SetState(new CommandState(battle)));
            }
            else if(battle.State is TargetSelectionState)
            {
                Label(commands,battle.SelectedSkill.DisplayName+"\n타일을 클릭해 효과를 확인하세요.",60,100,20);Button(commands,"실행",180,battle.Confirm,battle.Target.HasValue);Button(commands,"취소",230,()=>battle.State.Cancel());
            }
            else if(battle.State is FacingSelectionState)
            {int i=0;foreach(Facing f in Enum.GetValues(typeof(Facing))){var facing=f;Button(commands,f.ToString(),60+i++*48,()=>battle.ChooseFacing(facing));}}
            else if(battle.State is MoveSelectionState){Label(commands,"푸른 타일: 이동 가능\n경로 위로 마우스를 움직여 확인하세요.",60,130);Button(commands,"취소",230,()=>battle.State.Cancel());}
            else if(battle.State is ActionExecutionState){timing=Label(commands,"행동 중…",70,180,21);Button(commands,"타이밍 입력 (Space)",280,battle.TimingInput);}
        }
        public void UpdateTiming(){if(timing!=null)timing.text=$"사자전후 → 사후폭쇄진\n\n진행 {battle.TimingProgress:P0}\n입력 구간 40%–75%\n{(battle.TimingSuccess?"성공":"Space / 버튼 입력")}";}
        public void Inspect(Vector2Int p){var t=battle.Session.Grid[p];var u=battle.Session.Units.FirstOrDefault(x=>x.Position==p&&x.Alive);battle.Message=$"Tile {p} / 높이 {t.Height} / {t.Terrain} / 이동비용 {t.MovementCost}"+(u!=null?$"\n{u.Data.DisplayName} HP {u.CurrentHP}/{u.Stats.HP}":"");Refresh();}
    }
}
