using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform storyPanel, storyControls, storyOverlay;
        bool storyAutomatic;
        float storyNextTime;
        int storyAdvanceFrame = -1, storyHistoryPage;
        TMP_Text storyAutoLabel;
        public bool StoryDialogOpen => storyOverlay != null && storyOverlay.gameObject.activeSelf;
        public bool StoryAutomatic => storyAutomatic;

        public CharacterData StoryPortrait(string speaker)
        {
            // Script names may use the short name; never give narration an unrelated portrait.
            if (speaker == "클레스") speaker = "크레스";
            return battle.Catalog.Characters.FirstOrDefault(c => c.DisplayName == speaker || c.DisplayName.Split(' ')[0] == speaker);
        }
        void StoryBackdrop(Transform parent)
        {
            var r = FormationRect(parent, "StoryBackdrop", Vector2.zero, Vector2.one);
            var image = r.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = Resources.Load<Texture2D>("TalesTactics/FormationBackdrop");
            image.color = new Color(.65f,.72f,.85f,1); image.raycastTarget = false;
        }
        public void ShowStoryLine(StoryLine line, int number, int count)
        {
            if (storyPanel == null) storyPanel = Panel("CampaignStory", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (number == 1) storyAutomatic = false;
            storyPanel.gameObject.SetActive(true); storyPanel.SetAsLastSibling(); Clear(storyPanel); SetMainInteraction(false);
            storyOverlay = null; StoryBackdrop(storyPanel);
            var title = FormationText(storyPanel, CampaignStages.Title(battle.StoryStage), new Vector2(.04f,.86f), new Vector2(.96f,.96f), 25);
            title.color = formationGold;
            storyControls = FormationRect(storyPanel,"StoryControls",Vector2.zero,Vector2.one);
            var box = IntermissionPanel(storyControls,"DialogueBox",new Vector2(.04f,.105f),new Vector2(.96f,.52f));
            var portrait = StoryPortrait(line.Speaker);
            float textLeft = portrait != null ? .27f : .035f;
            if (portrait != null) IntermissionPortrait(box,portrait,new Vector2(.01f,.02f),new Vector2(.25f,.98f));
            FormationText(box,line.Speaker,new Vector2(textLeft,.77f),new Vector2(.96f,.95f),25).color=formationGold;
            var body = FormationText(box,line.Text,new Vector2(textLeft,.23f),new Vector2(.96f,.77f),29,TextAlignmentOptions.TopLeft);
            body.name="DialogueBody"; body.fontSizeMin=22;
            FormationText(box,number.ToString("D2")+" / "+count.ToString("D2"),new Vector2(textLeft,.04f),new Vector2(textLeft+.16f,.19f),16);
            var next=FormationButton(box,number==count?"이야기 마치기":"다음 대사",number==count?"이야기 마치기  ›":"다음  ›",new Vector2(.73f,.03f),new Vector2(.97f,.21f),AdvanceStoryFromHud,true,21);
            StyleButton(next,HudRole.Quiet);
            FormationText(storyControls,"SPACE 다음 · TAB 메뉴 선택",new Vector2(.04f,.015f),new Vector2(.48f,.085f),15);
            FormationButton(storyControls,"대화 기록","대화 기록",new Vector2(.50f,.015f),new Vector2(.65f,.085f),()=>{storyHistoryPage=Math.Max(0,(battle.ReadStoryHistory().Length-1)/2);ShowStoryHistory();});
            var auto=FormationButton(storyControls,"대화 자동 진행",storyAutomatic?"자동 ON":"자동 OFF",new Vector2(.65f,.015f),new Vector2(.80f,.085f),ToggleStoryAuto);
            storyAutoLabel=auto.GetComponentInChildren<TMP_Text>();
            FormationButton(storyControls,"이야기 건너뛰기","건너뛰기  »",new Vector2(.80f,.015f),new Vector2(.96f,.085f),ConfirmStorySkip);
            foreach(var button in storyControls.GetComponentsInChildren<UnityEngine.UI.Button>())StyleButton(button.transform,HudRole.Quiet);
            storyNextTime=Time.unscaledTime+Mathf.Max(4.5f,line.Text.Length*.075f);
            EventSystem.current?.SetSelectedGameObject(next.gameObject);
        }
        void ToggleStoryAuto()
        {
            storyAutomatic=!storyAutomatic;
            if(storyAutoLabel!=null)storyAutoLabel.text=storyAutomatic?"자동 ON":"자동 OFF";
            storyNextTime=Time.unscaledTime+Mathf.Max(4.5f,battle.ReadStoryHistory().LastOrDefault().Text?.Length*.075f??4.5f);
        }
        void AdvanceStoryFromHud()
        {
            if(!battle.StoryActive||StoryDialogOpen||storyAdvanceFrame==Time.frameCount)return;
            storyAdvanceFrame=Time.frameCount;battle.AdvanceStory();
        }
        void UpdateStoryInput()
        {
            if(battle==null||!battle.StoryActive)return;
            var keyboard=Keyboard.current;
            if(StoryDialogOpen)
            {
                if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)CloseStoryDialog();
                return;
            }
            // Enter belongs to EventSystem. Space also advances the default next button;
            // the frame guard prevents a submit mapping from advancing the same line twice.
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(keyboard!=null&&keyboard.spaceKey.wasPressedThisFrame&&(selected==null||selected.name=="다음 대사"||selected.name=="이야기 마치기"))AdvanceStoryFromHud();
            if(storyAutomatic&&Application.isFocused&&Time.unscaledTime>=storyNextTime)AdvanceStoryFromHud();
        }
        RectTransform BeginStoryDialog(string title)
        {
            storyAutomatic=false;if(storyAutoLabel!=null)storyAutoLabel.text="자동 OFF";
            if(storyOverlay==null)
            {
                storyOverlay=FormationRect(storyPanel,"StoryDialogOverlay",Vector2.zero,Vector2.one);
                var shade=storyOverlay.gameObject.AddComponent<UnityEngine.UI.Image>();shade.color=new Color(0,0,0,.85f);
            }
            storyOverlay.gameObject.SetActive(true);storyOverlay.SetAsLastSibling();Clear(storyOverlay);
            var group=storyControls.GetComponent<CanvasGroup>();if(group==null)group=storyControls.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            var window=IntermissionPanel(storyOverlay,"StoryDialog",new Vector2(.17f,.16f),new Vector2(.83f,.84f));
            FormationText(window,title,new Vector2(.035f,.85f),new Vector2(.965f,.97f),26).color=formationGold;
            return window;
        }
        public bool CloseStoryDialog()
        {
            if(!StoryDialogOpen)return false;
            storyOverlay.gameObject.SetActive(false);var group=storyControls.GetComponent<CanvasGroup>();group.interactable=true;group.blocksRaycasts=true;
            var next=storyControls.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name=="다음 대사"||b.name=="이야기 마치기");
            EventSystem.current?.SetSelectedGameObject(next?.gameObject);return true;
        }
        void ConfirmStorySkip()
        {
            var window=BeginStoryDialog("이야기를 건너뛸까요?");
            FormationText(window,"현재 대화를 마치고 다음 단계로 진행합니다.",new Vector2(.05f,.42f),new Vector2(.95f,.75f),24);
            var cancel=FormationButton(window,"이야기 계속 읽기","계속 읽기",new Vector2(.10f,.12f),new Vector2(.48f,.28f),()=>CloseStoryDialog(),true,21);
            FormationButton(window,"이야기 건너뛰기 확인","건너뛰기",new Vector2(.52f,.12f),new Vector2(.90f,.28f),battle.FinishStory,true,21);
            EventSystem.current?.SetSelectedGameObject(cancel.gameObject);
        }
        void ShowStoryHistory()
        {
            var history=battle.ReadStoryHistory();int pages=Math.Max(1,(history.Length+1)/2);storyHistoryPage=Mathf.Clamp(storyHistoryPage,0,pages-1);
            var window=BeginStoryDialog("대화 기록  ·  "+(storyHistoryPage+1)+" / "+pages);
            for(int i=0;i<2&&storyHistoryPage*2+i<history.Length;i++)
            {
                var line=history[storyHistoryPage*2+i];float top=.82f-i*.31f;
                FormationText(window,line.Speaker,new Vector2(.05f,top-.07f),new Vector2(.95f,top),20).color=formationGold;
                FormationText(window,line.Text,new Vector2(.05f,top-.29f),new Vector2(.95f,top-.08f),22,TextAlignmentOptions.TopLeft);
            }
            FormationButton(window,"이전 대화 기록","이전",new Vector2(.04f,.03f),new Vector2(.29f,.16f),()=>{storyHistoryPage--;ShowStoryHistory();},storyHistoryPage>0);
            FormationButton(window,"다음 대화 기록","다음",new Vector2(.30f,.03f),new Vector2(.55f,.16f),()=>{storyHistoryPage++;ShowStoryHistory();},storyHistoryPage+1<pages);
            var close=FormationButton(window,"대화 기록 닫기","닫기",new Vector2(.68f,.03f),new Vector2(.96f,.16f),()=>CloseStoryDialog());
            EventSystem.current?.SetSelectedGameObject(close.gameObject);
        }
        public void HideStory()
        {
            storyAutomatic=false;storyOverlay=null;
            if(storyPanel!=null)storyPanel.gameObject.SetActive(false);
            SetMainInteraction(true);
        }
    }
}
