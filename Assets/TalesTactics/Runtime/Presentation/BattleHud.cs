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
        public void ShowChapterPage(int page){chapterPage=Mathf.Clamp(page,0,(CampaignStages.Count-1)/3);ShowDeploymentMissions();}
        RectTransform canvas,left,commands,header,footer;
        TMP_Text message,timing;
        readonly System.Collections.Generic.List<TMP_FontAsset> ownedFonts=new System.Collections.Generic.List<TMP_FontAsset>();
        readonly Color panel=new Color(0.035f,0.055f,0.11f,0.98f);
        public bool CompactLayout=>battle!=null&&battle.Session!=null&&Screen.width<Screen.height*1.2f;
        void LateUpdate(){if(left!=null)UpdateLayout();if(tacticalToolbar!=null)tacticalToolbar.gameObject.SetActive(battle.Session!=null&&!battle.StoryActive&&!InputModalOpen);}
        void UpdateLayout()
        {
            if(battle.Session!=null){UpdateBattleLayout();return;}
            footer.GetComponent<UnityEngine.UI.Image>().enabled=true;
            footer.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            footer.GetComponent<UnityEngine.UI.Outline>().enabled=true;
            commands.GetComponent<UnityEngine.UI.Image>().enabled=true;
            commands.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            commands.GetComponent<UnityEngine.UI.Outline>().enabled=true;
            var footerGroup=footer.GetComponent<CanvasGroup>();if(footerGroup!=null){footerGroup.alpha=1;footerGroup.blocksRaycasts=true;}
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
            var scaler=GetComponent<UnityEngine.UI.CanvasScaler>();
            if(scaler!=null&&scaler.uiScaleMode==UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
                scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
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
        void Clear(Transform parent)
        {
            if(parent==header)
            {
                if(modelDeploymentLayout||intermissionLayout)foreach(var p in new[]{header,left,center,commands,footer})if(p!=null)p.GetComponent<UnityEngine.UI.Image>().color=panel;
                HideResult();preparationLayout=false;modelDeploymentLayout=false;intermissionLayout=false;if(center!=null)center.gameObject.SetActive(false);
            }
            foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        TMP_Text Label(Transform parent,string value,float y,float height=40,int size=18)
        {
            var g=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(0.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(-28,height);
            var t=g.GetComponent<TextMeshProUGUI>();t.font=Font;t.text=value;t.fontSize=size;t.color=new Color(0.9f,0.94f,0.98f);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;g.AddComponent<AccessibleText>();return t;
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
        void ShowGrowthRoster() => ShowGrowth(IntermissionCharacter());
        void ShowGrowth(CharacterData character,string notice=null) => RenderIntermissionGrowth(character,notice);
        public void Refresh()
        {
            if(battle.Session==null)return;
            CloseUnitDetails();
            UpdateLayout();
            Clear(header);Clear(left);Clear(commands);Clear(footer);var u=battle.Session.Active;
            DrawTacticalHeader();
            DrawTacticalTools();
            message=Label(footer,battle.TutorialActive&&!(battle.State is TargetSelectionState&&battle.Target.HasValue)?battle.TutorialInstruction:battle.Message,10,78,17);
            message.enableAutoSizing=true;message.fontSizeMin=15;message.fontSizeMax=17;
            if(u==null)return;
            DrawUnitSummary(u);DrawCameraControls();DrawHelpEntry(false);DrawSystemEntry(false);DrawMissionEntry();CompactHeaderEntries();
            if(!SkillPanel&&!(battle.State is CommandState)&&!(battle.State is FacingSelectionState)&&!(battle.State is MoveSelectionState)&&!(battle.State is TargetSelectionState))Label(commands,BattleTitle,12,36,20);
            if(battle.State is TutorialCompleteState){Label(commands,"입문 연습 완료",62,70,24);Button(commands,"출전 준비로",160,battle.Restart);Button(commands,"처음부터 연습",215,battle.RepeatTutorial);return;}
            if(battle.State is BattleEndState){DrawResult();return;}
            if(battle.State is CommandState&&u.Team==Team.Player)
            {
                Button(commands,"Move / 이동",55,battle.MoveCommand,!u.Moved);Button(commands,"Attack / 공격",101,battle.AttackCommand,!u.Acted);
                Button(commands,"Skill / 스킬",147,battle.SkillCommand,!u.Acted||u.FlamingChain);Button(commands,"Guard / 가드",193,battle.Guard,!u.Acted);
                if(u.CanUndoMove)Button(commands,"Undo Move / 이동 취소",239,battle.Undo);Button(commands,"Wait / 방향 선택",285,battle.WaitCommand);
                if(!battle.TutorialActive&&battle.Session.Scheduler is TeamTurnScheduler)Button(commands,"아군 턴 종료",331,ShowEndPhaseConfirmation);
                ArrangeCommandMenu();ApplyTutorialCommands();
            }
            else if(SkillPanel)DrawSkillCards(u);
            else if(battle.State is TargetSelectionState)
            {
                DrawTargetForecast();
            }
            else if(battle.State is FacingSelectionState)DrawFacingArrows();
            else if(battle.State is MoveSelectionState){Label(commands,"이동할 칸 선택",8,26,16);moveRisk=Label(commands,"방향키 또는 마우스로 위치 선택",38,44,13);Button(commands,"취소",86,()=>battle.State.Cancel(),true,26);}
            else if(battle.State is ActionExecutionState){timing=Label(commands,"행동 중…",70,180,21);Button(commands,"타이밍 입력 (Space)",280,battle.TimingInput);}
        }
        public void UpdateTiming(){if(timing!=null)timing.text=$"사자전후 → 사후폭쇄진\n\n진행 {battle.TimingProgress:P0}\n입력 구간 40%–75%\n{(battle.TimingSuccess?"성공":"Space / 버튼 입력")}";}
        public void Inspect(Vector2Int p){var t=battle.Session.Grid[p];var u=battle.Session.Units.FirstOrDefault(x=>x.Position==p&&x.Alive);battle.InspectUnit(u);battle.Message=$"Tile {p} / 높이 {t.Height} / {t.Terrain} / 이동비용 {t.MovementCost}"+(u!=null?$"\n{u.Data.DisplayName} HP {u.CurrentHP}/{u.Stats.HP} · "+StatusText.Describe(u)+(u.Team==Team.Enemy?"\n"+EnemyTactics.Describe(u):""):"");Refresh();}
    }
}
