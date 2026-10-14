using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform titleRoot,titleContent,titleOverlay,titleWindow,titleConfirm;
        UnityEngine.UI.RawImage titleBackdrop;
        TMP_FontAsset titleFont;
        TitleSlot[] titleSlots;
        int titleSlot,titlePage;
        bool titleNew;
        string titleNotice="";
        public bool TitleOpen=>titleRoot!=null&&titleRoot.gameObject.activeSelf;
        public void ShowTitle()
        {
            if(battle.Session!=null||battle.StoryActive)return;
            CloseMission();CloseHelp();CloseSystemMenu();CloseUnitDetails();battle.Audio.StopAll();
            if(titleRoot==null)
            {
                titleRoot=Panel("TitleScreen",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);titleRoot.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                var art=FormationRect(titleRoot,"TitleBackdrop",Vector2.zero,Vector2.one);titleBackdrop=art.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                titleBackdrop.texture=Resources.Load<Texture2D>("TalesTactics/TitleBackground");titleBackdrop.raycastTarget=false;
                var shade=FormationRect(titleRoot,"TitleShade",Vector2.zero,Vector2.one);shade.gameObject.AddComponent<TitleShade>().raycastTarget=false;
                titleContent=FormationRect(titleRoot,"TitleContent",new Vector2(.075f,.14f),new Vector2(.48f,.84f));
                MenuArt(titleRoot,0,new Vector2(.49f,.19f),new Vector2(.92f,.81f));
                TitleText(titleContent,"B E Y O N D   T H E   R I F T",.88f,.96f,20).color=formationGold;
                if(titleFont==null)titleFont=SystemFont("Georgia");
                var logo=TitleText(titleContent,"TALES",.68f,.89f,100);if(titleFont!=null)logo.font=titleFont;
                var logo2=TitleText(titleContent,"TACTICS",.50f,.70f,85);logo2.color=formationGold;if(titleFont!=null)logo2.font=titleFont;
                TitleText(titleContent,"경 계 를   잇 는   의 지",.40f,.49f,25);
                TitleText(titleContent,"갈라진 세계, 다시 이어지는 이야기.",.33f,.40f,17).color=new Color(.70f,.77f,.83f);
                var fresh=FormationButton(titleContent,"타이틀 새 게임","새 게임",new Vector2(0,.14f),new Vector2(.88f,.27f),()=>ShowTitleSlots(true),true,26);IntermissionGoldButton(fresh);
                FormationButton(titleContent,"타이틀 불러오기","저장된 게임 불러오기",new Vector2(0,0),new Vector2(.88f,.13f),()=>ShowTitleSlots(false),true,24);
                FormationText(titleRoot,"↑ ↓ / Tab 선택     Enter / 패드 A 확인     Esc / 패드 B 돌아가기",new Vector2(.08f,.025f),new Vector2(.72f,.075f),15);
                titleOverlay=Panel("TitleSlotsOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);titleOverlay.SetParent(titleRoot,false);titleOverlay.GetComponent<UnityEngine.UI.Image>().color=new Color(.01f,.03f,.07f,.88f);
                titleWindow=Panel("TitleSlotsWindow",Vector2.one*.5f,Vector2.one*.5f,new Vector2(-510,-360),new Vector2(510,360));titleWindow.SetParent(titleOverlay,false);
                titleConfirm=Panel("TitleConfirm",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);titleConfirm.SetParent(titleRoot,false);titleConfirm.GetComponent<UnityEngine.UI.Image>().color=new Color(.01f,.03f,.07f,.88f);
            }
            titleRoot.gameObject.SetActive(true);titleRoot.SetAsLastSibling();titleOverlay.gameObject.SetActive(false);titleConfirm.gameObject.SetActive(false);titlePage=0;titleNotice="";
            foreach(var p in new[]{header,left,commands,footer,center,resultPanel})if(p!=null)p.gameObject.SetActive(false);
            TitleInteraction(titleContent,true);UpdateTitleLayout();TitleFocus("타이틀 새 게임");
        }
        TMP_Text TitleText(Transform parent,string text,float bottom,float top,int size)=>FormationText(parent,text,new Vector2(parent.GetComponent<OrnatePanel>()!=null?.04f:0,bottom),new Vector2(parent.GetComponent<OrnatePanel>()!=null?.89f:1,top),size);
        void TitleInteraction(RectTransform rect,bool enabled)
        {var group=rect.GetComponent<CanvasGroup>();if(group==null)group=rect.gameObject.AddComponent<CanvasGroup>();group.interactable=enabled;group.blocksRaycasts=enabled;}
        void TitleFocus(string name)
        {
            var button=titleRoot.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name==name&&b.IsInteractable());
            if(button==null)return;var pointer=battle.GetComponent<GamepadPointer>();if(pointer!=null)pointer.FocusButton(button);else EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }
        void UpdateTitleLayout()
        {
            if(!TitleOpen)return;bool portrait=Screen.width<Screen.height;
            Place(titleContent,new Vector2(.075f,portrait?.24f:.14f),new Vector2(portrait?.93f:.48f,portrait?.76f:.84f),Vector2.zero,Vector2.zero);
            if(titleBackdrop.texture!=null)
            {
                float screen=(float)Screen.width/Screen.height,art=(float)titleBackdrop.texture.width/titleBackdrop.texture.height;
                titleBackdrop.uvRect=screen>art?new Rect(0,(1-art/screen)*.5f,1,art/screen):new Rect((1-screen/art)*.5f,0,screen/art,1);
            }
        }
        public void HideTitle()
        {
            if(!TitleOpen)return;titleRoot.gameObject.SetActive(false);titlePage=0;
            foreach(var p in new[]{header,left,commands,footer})if(p!=null)p.gameObject.SetActive(true);SetMainInteraction(true);
        }
        public void ShowTitleSlots(bool newGame)
        {
            if(!TitleOpen)return;titleNew=newGame;titleNotice="";titleSlots=battle.ReadTitleSlots();
            titleSlot=Array.FindIndex(titleSlots,s=>s.Writable&&(newGame?!s.Exists:s.Exists));
            if(titleSlot<0)titleSlot=Array.FindIndex(titleSlots,s=>s.Writable&&(newGame||s.Exists));
            DrawTitleSlots();
        }
        void DrawTitleSlots()
        {
            titlePage=1;titleConfirm.gameObject.SetActive(false);TitleInteraction(titleContent,false);TitleInteraction(titleWindow,true);titleOverlay.gameObject.SetActive(true);Clear(titleWindow);
            TitleText(titleWindow,titleNew?"NEW JOURNEY":"LOAD GAME",.88f,.95f,17).color=formationGold;
            TitleText(titleWindow,titleNew?"새 게임을 시작할 슬롯":"저장된 게임 불러오기",.80f,.89f,30);
            CloseIcon(titleWindow,"타이틀 슬롯 닫기",()=>CancelTitle());
            for(int i=0;i<3;i++)
            {
                int index=i;var slot=titleSlots[i];float top=.76f-i*.20f;
                var card=FormationButton(titleWindow,"타이틀 슬롯 "+(i+1),"",new Vector2(.04f,top-.18f),new Vector2(.96f,top),()=>{titleSlot=index;titleNotice="";if(titleNew)ConfirmTitleNew();else DrawTitleSlots();},slot.Writable&&(titleNew||slot.Exists));
                StyleButton(card,HudRole.Secondary,titleSlot==i);
                FormationText(card,(i+1).ToString("D2"),new Vector2(.02f,.18f),new Vector2(.10f,.86f),32).color=formationGold;
                string heading=!slot.Exists?"비어 있는 슬롯":!slot.Writable?"불러올 수 없는 저장":CampaignStages.Title(Mathf.Clamp(slot.Save.SelectedStage,0,CampaignStages.Count-1));
                FormationText(card,heading,new Vector2(.12f,.53f),new Vector2(.77f,.92f),23);
                int completed=Enumerable.Range(1,20).Count(n=>slot.Save.StoryProgress.Contains(CrossoverCampaign.Flag(n)));
                int level=slot.Save.Characters.FirstOrDefault(c=>c.Id=="cless")?.Level??1;
                string stamp=DateTime.TryParse(slot.Save.SavedAt,out var time)?time.ToLocalTime().ToString("yyyy.MM.dd HH:mm"):"저장 시각 없음";
                string detail=!slot.Exists?"아직 기록된 이야기가 없습니다.":!slot.Writable?slot.Notice:$"메인 {completed} / 20 완료 · 크레스 Lv.{level}\n{stamp}"+(string.IsNullOrEmpty(slot.Notice)?"":" · 백업 복구");
                FormationText(card,detail,new Vector2(.12f,.08f),new Vector2(.78f,.53f),16);
                string badge=!slot.Exists?"저장 없음":!slot.Writable?"원본 보호":slot.Resumable?"중단 전투":slot.Save.SuspendedBattle!=null?"거점만 가능":"거점 저장";
                FormationText(card,badge,new Vector2(.79f,.18f),new Vector2(.99f,.80f),16,TextAlignmentOptions.Center).color=new Color(.64f,.85f,.83f);
            }
            var choice=titleSlot>=0&&titleSlots[titleSlot].Writable&&(titleNew||titleSlots[titleSlot].Exists)?titleSlots[titleSlot]:null;
            string note=!string.IsNullOrEmpty(titleNotice)?titleNotice:choice==null?"불러올 수 있는 저장이 없습니다. 새 게임에서 빈 슬롯을 선택하세요.":titleNew?(choice.Exists?"기존 기록이 있습니다. 시작 전에 덮어쓰기를 확인합니다.":"빈 슬롯에 새로운 이야기를 시작합니다."):choice.Resumable?"중단 전투에서 이어하거나 거점으로 돌아갑니다.":"저장한 거점에서 이야기를 이어갑니다.";
            FormationText(titleWindow,note,new Vector2(.04f,.045f),new Vector2(titleNew?.94f:.51f,.17f),17);
            if(!titleNew)
            {
                if(choice?.Resumable==true)FormationButton(titleWindow,"타이틀 거점 불러오기","거점으로 불러오기",new Vector2(.52f,.04f),new Vector2(.74f,.15f),()=>ExecuteTitle(false),true,17);
                FormationButton(titleWindow,"타이틀 불러오기 실행",choice?.Resumable==true?"중단 전투 이어하기":"불러오기",new Vector2(.75f,.04f),new Vector2(.97f,.15f),()=>ExecuteTitle(choice?.Resumable==true),choice!=null,18);
            }
            TitleFocus(titleSlot>=0?"타이틀 슬롯 "+(titleSlot+1):"타이틀 슬롯 닫기");
        }
        void ConfirmTitleNew()
        {
            if(titleSlot<0)return;titlePage=2;TitleInteraction(titleWindow,false);titleConfirm.gameObject.SetActive(true);Clear(titleConfirm);
            var dialog=FormationRect(titleConfirm,"TitleConfirmationWindow",Vector2.one*.5f,Vector2.one*.5f);dialog.sizeDelta=new Vector2(700,360);var surface=dialog.gameObject.AddComponent<UnityEngine.UI.Image>();surface.color=panel;OrnatePanel.Apply(surface);
            CloseIcon(dialog,"타이틀 확인 닫기",()=>CancelTitle());
            var choice=titleSlots[titleSlot];TitleText(dialog,choice.Exists?"슬롯 "+(titleSlot+1)+"의 기록을 덮어쓸까요?":"새로운 이야기를 시작할까요?",.70f,.92f,27);
            TitleText(dialog,choice.Exists?"성장·장비·진행 기록과 중단 전투가 새 게임으로 교체됩니다. 이전 저장은 백업하며 다른 슬롯은 유지됩니다.":"슬롯 "+(titleSlot+1)+"에 새 여정을 기록합니다. 크레스와 민트의 첫 이야기에서 시작합니다.",.29f,.66f,22);
            FormationButton(dialog,"타이틀 새 게임 취소","취소",new Vector2(.44f,.07f),new Vector2(.64f,.23f),()=>CancelTitle(),true,22);
            var start=FormationButton(dialog,"타이틀 새 게임 확정","새 게임 시작",new Vector2(.66f,.07f),new Vector2(.97f,.23f),()=>ExecuteTitle(false),true,22);IntermissionGoldButton(start);TitleFocus("타이틀 새 게임 취소");
        }
        void ExecuteTitle(bool resume)
        {
            bool ok=titleNew?battle.StartTitleGame(titleSlot):battle.LoadTitleGame(titleSlot,resume);
            if(!ok){titleNotice=battle.SaveNotice;titleSlots=battle.ReadTitleSlots();DrawTitleSlots();}
        }
        public bool CancelTitle()
        {
            if(!TitleOpen)return false;if(titlePage==2){DrawTitleSlots();return true;}
            if(titlePage==1){ShowTitle();TitleFocus(titleNew?"타이틀 새 게임":"타이틀 불러오기");}return true;
        }
    }
    public sealed class TitleShade:UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(int i=0;i<4;i++){bool right=i>=2;var p=new Vector2(right?r.xMax:r.xMin,i==0||i==3?r.yMin:r.yMax);vh.AddVert(p,new Color(.015f,.035f,.07f,right?.03f:.65f),Vector2.zero);}
            vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
        }
    }
}
