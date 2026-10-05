using System;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud:MonoBehaviour
    {
        public TMP_FontAsset Font;
        BattleDirector battle;
        int chapterPage;
        public int ChapterPage=>chapterPage;
        public void ShowChapterPage(int page){chapterPage=Mathf.Clamp(page,0,(CampaignStages.Count-1)/3);ShowDeployment();}
        RectTransform canvas,left,commands,header,footer;
        TMP_Text message,timing;
        readonly System.Collections.Generic.List<TMP_FontAsset> ownedFonts=new System.Collections.Generic.List<TMP_FontAsset>();
        readonly Color panel=new Color(0.035f,0.055f,0.11f,0.98f);
        public bool CompactLayout=>battle!=null&&battle.Session!=null&&Screen.width<Screen.height*1.2f;
        void LateUpdate(){if(left!=null)UpdateLayout();}
        void UpdateLayout()
        {
            if(battle.Session!=null){UpdateBattleLayout();return;}
            PreparationLayout();
        }
        static void Place(RectTransform r,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
        {r.anchorMin=min;r.anchorMax=max;r.offsetMin=low;r.offsetMax=high;}
        public Rect BattlefieldViewport
        {
            get
            {
                float scale=GetComponent<Canvas>().scaleFactor;
                if(battle.Session!=null)return BattleViewport(scale);
                bool compact=CompactLayout;
                float x=(compact?12:292)*scale,y=(compact?670:138)*scale;
                return new Rect(x/Screen.width,y/Screen.height,
                    Mathf.Max(1,Screen.width-x-(compact?12:312)*scale)/Screen.width,
                    Mathf.Max(1,Screen.height-y-106*scale)/Screen.height);
            }
        }
        public void Initialize(BattleDirector b)
        {
            battle=b;canvas=GetComponent<RectTransform>();
            if(Font==null)
            {
                Font=Resources.Load<TMP_FontAsset>("TalesTactics/Korean");
                if(Font==null)Font=SystemFont("Malgun Gothic");
                if(Font==null)Font=SystemFont("Arial");
                if(Font==null)Font=TMP_Settings.defaultFontAsset;
                else if(ownedFonts.Contains(Font))
                {
                    var japanese=SystemFont("Yu Gothic");
                    if(japanese!=null)
                    {
                        if(Font.fallbackFontAssetTable==null)
                            Font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
                        Font.fallbackFontAssetTable.Add(japanese);
                    }
                }
            }
            header=Panel("Header",new Vector2(0,1),new Vector2(1,1),new Vector2(12,-94),new Vector2(-12,-12));
            left=Panel("Unit",new Vector2(0,0),new Vector2(0,1),new Vector2(12,138),new Vector2(280,-106));
            commands=Panel("Commands",new Vector2(1,0),new Vector2(1,1),new Vector2(-300,138),new Vector2(-12,-106));
            footer=Panel("Message",Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,126));
        }
        TMP_FontAsset SystemFont(string family)
        {
            var font=TMP_FontAsset.CreateFontAsset(family,"Regular",48);
            if(font!=null){font.name=family+" Runtime";ownedFonts.Add(font);}
            return font;
        }
        void OnDestroy()
        {
            foreach(var font in ownedFonts)
            {
                if(font==null)continue;
                foreach(var texture in font.atlasTextures)if(texture!=null)Destroy(texture);
                if(font.material!=null)Destroy(font.material);
                Destroy(font);
            }
        }
        public void ShowSetupError(string detail)
        {
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS",12,45,27);
            Label(left,"전투 데이터를 불러오지 못했습니다.",20,120,22);
            Label(footer,detail,12,94,17);
        }
        RectTransform Panel(string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {var g=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(canvas,false);var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;var image=g.GetComponent<UnityEngine.UI.Image>();image.color=panel;image.raycastTarget=true;var border=g.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(0.61f,0.46f,0.25f,0.9f);border.effectDistance=new Vector2(1,-1);return r;}
        void Clear(Transform parent){if(parent==header){HideResult();preparationLayout=false;if(center!=null)center.gameObject.SetActive(false);}foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        TMP_Text Label(Transform parent,string value,float y,float height=40,int size=18)
        {
            var g=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(0.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(-28,height);
            var t=g.GetComponent<TextMeshProUGUI>();t.font=Font;t.text=value;t.fontSize=size;t.color=new Color(0.9f,0.94f,0.98f);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        void Button(Transform parent,string value,float y,Action click,bool enabled=true,float height=38)
        {
            var g=new GameObject(value,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(0.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(-24,height);
            g.GetComponent<UnityEngine.UI.Image>().color=new Color(0.10f,0.16f,0.24f);var border=g.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(0.46f,0.36f,0.22f,0.8f);border.effectDistance=new Vector2(1,-1);var button=g.GetComponent<UnityEngine.UI.Button>();button.interactable=enabled;button.onClick.AddListener(()=>click());var label=Label(g.transform,value,3,height-3,16);label.alignment=TextAlignmentOptions.Center;if(!enabled)label.color=Color.gray;
        }
        void CameraButton(string text,int column,int count,float y,Action click,bool enabled=true)
        {
            Button(left,text,y,click,enabled,32);
            var r=(RectTransform)left.GetChild(left.childCount-1);
            r.anchorMin=new Vector2((float)column/count,1);r.anchorMax=new Vector2((float)(column+1)/count,1);
            r.sizeDelta=new Vector2(-12,32);
        }
        public void ShowDeployment(){RenderDeployment();}
        void ShowEquipmentRoster(){RenderEquipmentRoster();}
        void ShowEquipment(CharacterData character,EquipmentLoadout draft=null,string notice=null)
        {RenderEquipment(character,draft,notice);}
        void ShowGrowthRoster()
        {
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS     ·     성장 · 승급",12,45,25);
            Label(left,"캐릭터 선택",12,35,20);
            foreach(var character in battle.Catalog.Characters)
            {var selected=character;Button(left,character.DisplayName,50+Array.IndexOf(battle.Catalog.Characters,character)*39,()=>ShowGrowth(selected),true,34);}
            Label(commands,"성장을 확인할 캐릭터를\n선택하세요.",24,100,20);
            Button(commands,"출전 준비로",150,ShowDeployment);
            Label(footer,"캐릭터마다 고정 직업에서 한 번만 승급합니다.\n저장된 레벨과 스토리 조건을 모두 충족해야 합니다.",14,88,17);
        }
        void ShowGrowth(CharacterData character,string notice=null)
        {
            if(battle.Session!=null)return;
            var progress=battle.Campaign.Get(character.Id);
            var gear=new EquipmentLoadout(character,progress,battle.Catalog.Equipment,battle.Campaign);
            var before=gear.Preview(progress.Level,progress.Promoted);var after=gear.Preview(progress.Level,true);
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS     ·     성장 · 승급",12,45,25);
            Label(left,character.DisplayName,12,65,23);
            string exp=progress.Level>=50?"EXP MAX":$"EXP {progress.EXP} / {progress.Level*100}";
            Label(left,$"Lv{progress.Level} · {exp}\n현재 → 승급 후\n\nHP {before.HP} → {after.HP}\nMP {before.MP} → {after.MP}\nSTR {before.STR} → {after.STR}\nMAG {before.MAG} → {after.MAG}\nDEF {before.DEF} → {after.DEF}\nMDF {before.MDF} → {after.MDF}\nSPD {before.SPD} → {after.SPD}\nMOV {before.MOV} → {after.MOV}\nJMP {before.JMP} → {after.JMP}",85,370,18);
            Label(commands,character.Job+"\n→ "+character.PromotionJob,12,95,19);
            bool story=battle.Campaign.StoryProgress.Contains(character.PromotionStoryFlag);
            string storyName=character.PromotionStoryFlag=="chapter2"?"2장 완료":character.PromotionStoryFlag;
            Label(commands,$"레벨 조건: Lv{character.PromotionLevel}\n{(progress.Level>=character.PromotionLevel?"충족":"미충족")} (현재 Lv{progress.Level})\n\n스토리 조건: {storyName}\n{(story?"충족":"미충족")}",115,170,17);
            string reason=CampaignPromotion.Unavailable(progress,character,battle.Campaign.StoryProgress);
            if(battle.TrainingMode)reason="훈련 모드에서는 승급할 수 없습니다.";
            else if(!battle.CanSave)reason="저장 보호 상태에서는 승급할 수 없습니다.";
            Label(commands,reason??"승급할 수 있습니다.",290,70,17);
            Button(commands,progress.Promoted?"승급 완료":"승급 · 저장",380,()=>
            {
                if(battle.TrainingMode||!battle.CanSave)return;
                bool saved=CampaignPromotion.TrySave(battle.Campaign,character,battle.PersistCampaign);
                ShowGrowth(character,saved?"승급을 저장했습니다. 다음 캠페인 전투에 적용됩니다.":"승급하지 못했습니다. 조건과 저장 상태를 확인하세요.");
            },reason==null);
            Button(commands,"캐릭터 목록으로",428,ShowGrowthRoster);
            Label(footer,notice??"저장된 성장과 장비 기준의 능력치입니다. 훈련 레벨은 적용하지 않습니다.\n캠페인 1장 → 2장 승리로 스토리 조건을 충족합니다.",14,88,17);
        }        public void Refresh()
        {
            if(battle.Session==null)return;
            CloseUnitDetails();
            UpdateLayout();
            Clear(header);Clear(left);Clear(commands);Clear(footer);var u=battle.Session.Active;
            var heading=Label(header,BattleTitle+"  ·  "+(battle.TutorialActive?"입문 연습":battle.Session.ObjectiveDescription),8,26,19);
            heading.rectTransform.offsetMax=new Vector2(-720,heading.rectTransform.offsetMax.y);
            var next=Label(header,battle.TutorialActive?"이동 → 공격 → 회복 → 대기 · 적은 기다리는 연습 전장입니다.":"NEXT   "+string.Join("  →  ",battle.Session.Scheduler.Preview(battle.Session.Units).Take(3).Select(x=>x.Data.DisplayName)),34,24,15);
            next.rectTransform.offsetMax=new Vector2(-720,next.rectTransform.offsetMax.y);
            message=Label(footer,battle.TutorialActive?battle.TutorialInstruction:battle.Message,12,65,17);
            Label(footer,battle.TutorialActive?(battle.State is TargetSelectionState&&battle.Target.HasValue?battle.Message:"입문 연습 · 보상/저장 없음"):MissionBriefing.Progress(battle.Session)+" · "+(battle.Session.Objective==ObjectiveKind.Escort?"호위/아군 전멸 시 패배":"아군 전멸 시 패배"),83,battle.TutorialActive?36:26,15);
            if(u==null)return;
            DrawUnitSummary(u);DrawCameraControls();DrawHelpEntry(false);DrawSystemEntry(false);DrawMissionEntry();
            Label(commands,BattleTitle,12,36,20);
            if(battle.State is TutorialCompleteState){Label(commands,"입문 연습 완료",62,70,24);Button(commands,"출전 준비로",160,battle.Restart);Button(commands,"처음부터 연습",215,battle.RepeatTutorial);return;}
            if(battle.State is BattleEndState){DrawResult();return;}
            if(battle.State is CommandState&&u.Team==Team.Player)
            {
                Button(commands,"Move / 이동",55,battle.MoveCommand,!u.Moved);Button(commands,"Attack / 공격",101,battle.AttackCommand,!u.Acted);
                Button(commands,"Skill / 스킬",147,battle.SkillCommand,!u.Acted||u.FlamingChain);Button(commands,"Guard / 가드",193,battle.Guard,!u.Acted);
                Button(commands,"Undo Move / 이동 취소",239,battle.Undo,u.CanUndoMove);Button(commands,"Wait / 방향 선택",285,battle.WaitCommand);Button(commands,"Restart",385,battle.Restart);
                ArrangeCommandMenu();ApplyTutorialCommands();
            }
            else if(battle.State is ActionSelectionState)
            {
                int i=0;foreach(var s in u.Data.Skills.Concat(new[]{u.Data.UltimateSkill}).Where(x=>x!=null&&(!battle.TutorialActive||battle.TutorialSkillAllowed(x))))
                {
                    var skill=s;string error=battle.Session.Resolver.CanUse(u,s,battle.IsFollowup(s));
                    Button(commands,s.DisplayName+" · MP"+s.MPCost+(error!=null?" · 조건 확인":""),48+i*40,()=>battle.SetState(new SkillDetailsState(battle,skill)),true,35);RememberedSkillButton(skill);i++;
                }
                Button(commands,"취소",48+i*40,()=>battle.SetState(new CommandState(battle)));
            }
            else if(battle.State is SkillDetailsState detail)
            {
                string error=battle.Session.Resolver.CanUse(u,detail.Skill,battle.IsFollowup(detail.Skill));
                Label(commands,battle.Session.Resolver.Describe(u,detail.Skill),52,275,17);
                var reason=Label(commands,SkillResolver.ExplainUnavailable(error),335,60,17);
                reason.color=error==null?new Color(0.5f,1,0.7f):new Color(1,0.75f,0.4f);
                Button(commands,"목표 선택",405,()=>battle.SelectSkill(detail.Skill),error==null);
                Button(commands,"스킬 목록으로",453,()=>battle.State.Cancel());
            }
            else if(battle.State is TargetSelectionState)
            {
                Label(commands,battle.SelectedSkill.DisplayName,45,35,17);Button(commands,"실행",88,battle.Confirm,battle.Target.HasValue,32);Button(commands,"취소",128,()=>battle.State.Cancel(),true,32);
                bool available=battle.AvailableTargets().Length>0;
                Button(footer,"이전 대상",128,()=>battle.CycleTarget(-1),available,32);HalfButton(footer,0);
                Button(footer,"다음 대상 · Tab",128,()=>battle.CycleTarget(1),available,32);HalfButton(footer,1);
            }
            else if(battle.State is FacingSelectionState)
            {int i=0;foreach(Facing f in Enum.GetValues(typeof(Facing))){var facing=f;Button(commands,f.ToString(),60+i++*48,()=>battle.ChooseFacing(facing));var label=commands.GetChild(commands.childCount-1).GetComponentInChildren<TMP_Text>();label.text=f==Facing.Front?"앞쪽":f==Facing.Back?"뒤쪽":f==Facing.Left?"왼쪽":"오른쪽";}}
            else if(battle.State is MoveSelectionState){Label(commands,battle.TutorialActive?"푸른 목표 타일을 선택하세요.\n이동을 마치면 공격을 연습합니다.":"푸른 타일: 이동 가능\n경로 위로 마우스를 움직여 확인하세요.",60,130);Button(commands,"취소",230,()=>battle.State.Cancel());}
            else if(battle.State is ActionExecutionState){timing=Label(commands,"행동 중…",70,180,21);Button(commands,"타이밍 입력 (Space)",280,battle.TimingInput);}
        }
        public void UpdateTiming(){if(timing!=null)timing.text=$"사자전후 → 사후폭쇄진\n\n진행 {battle.TimingProgress:P0}\n입력 구간 40%–75%\n{(battle.TimingSuccess?"성공":"Space / 버튼 입력")}";}
        public void Inspect(Vector2Int p){var t=battle.Session.Grid[p];var u=battle.Session.Units.FirstOrDefault(x=>x.Position==p&&x.Alive);battle.Message=$"Tile {p} / 높이 {t.Height} / {t.Terrain} / 이동비용 {t.MovementCost}"+(u!=null?$"\n{u.Data.DisplayName} HP {u.CurrentHP}/{u.Stats.HP}":"");Refresh();}
    }
}
