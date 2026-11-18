using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        Button growthDetailsSource;
        int growthFocusRevision;
        void FocusGrowthControl(Button control)
        {
            if(control==null||!control.gameObject.activeInHierarchy||!control.IsInteractable())return;
            foreach(var frame in control.GetComponentsInParent<FramedContent>())frame.Fit();
            var pointer=battle.GetComponent<GamepadPointer>();
            if(pointer!=null)pointer.FocusButton(control);
            else UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(control.gameObject);
            StartCoroutine(RefreshGrowthFocus(control,++growthFocusRevision));
        }
        IEnumerator RefreshGrowthFocus(Button control,int revision)
        {
            // Closing may already have fitted the old tree this frame. Wait for the next
            // LateUpdate to lay out the rebuilt tree before measuring its final button center.
            yield return null;
            yield return new WaitForEndOfFrame();
            if(revision!=growthFocusRevision||control==null||!control.gameObject.activeInHierarchy||!control.IsInteractable()||UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject!=control.gameObject)yield break;
            battle.GetComponent<GamepadPointer>()?.FocusButton(control);
        }
        void RestoreGrowthPathFocus()
        {
            var source=growthDetailsSource;growthDetailsSource=null;FocusGrowthControl(source);
        }
        void ShowGrowthPath(CharacterData character)
        {
            if(battle.Session!=null||InputModalOpen||battle.StoryActive||!battle.IsJoined(character))return;
            growthDetailsSource=GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="성장 방향 선택");
            var p=FormationProgress(character);PrepareUnitModal(new UnitRuntime(character,Team.Player,battle.Catalog.Rules,p.Level));Clear(unitDetails);
            float scale=Mathf.Clamp(battle.Preferences?.TextScale??1,1,1.3f);
            unitDetails.sizeDelta=new Vector2(Mathf.Min(1000*scale,canvas.rect.width-48),Mathf.Min(760*scale,canvas.rect.height-48));unitDetails.anchoredPosition=Vector2.zero;
            var backdrop=FormationRect(unitDetails,"GrowthPathBackdrop",new Vector2(.012f,.012f),new Vector2(.988f,.988f));
            var backing=backdrop.gameObject.AddComponent<Image>();backing.color=new Color(.018f,.035f,.065f,1);backing.raycastTarget=false;
            var content=FormationRect(unitDetails,"GrowthPathContent",new Vector2(.025f,.02f),new Vector2(.975f,.98f));
            FormationText(content,"성장 방향 · "+character.DisplayName,new Vector2(.02f,.91f),new Vector2(.98f,1),28).color=formationGold;
            string available=p.Level<10?"Lv10부터 선택할 수 있습니다.":battle.TrainingMode?"훈련 중에는 저장된 성장만 조회합니다.":!battle.CanSave?"저장 보호 상태입니다.":"준비 중 무료 변경 · 선택하면 저장 후 다음 전투부터 적용";
            FormationText(content,available+"\n현재: "+GrowthPaths.Name(p.Growth)+" · "+GrowthPaths.Role(character,p.Growth),new Vector2(.02f,.79f),new Vector2(.98f,.91f),19);
            var controls=new List<Button>();
            foreach(GrowthPath path in Enum.GetValues(typeof(GrowthPath)))
            {
                var choice=path;float top=.785f-.225f*(int)path;
                var card=FormationButton(content,"성장 방향 "+GrowthPaths.Name(path),"",new Vector2(.02f,top-.215f),new Vector2(.98f,top),()=>
                {
                    if(battle.Session!=null||battle.TrainingMode||!battle.CanSave)return;
                    bool saved=GrowthPaths.Save(battle.Campaign,character,choice,battle.PersistCampaign);
                    CloseUnitDetails();ShowGrowth(character,saved?"성장 방향을 저장했습니다. 다음 전투부터 적용됩니다.":"저장하지 못했습니다. 기존 성장 방향을 유지합니다.");
                    FocusGrowthControl(GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="성장 방향 선택"));
                },p.Level>=10&&!battle.TrainingMode&&battle.CanSave);
                card.name=GrowthPaths.Name(path)+" · "+GrowthPaths.Role(character,path);IntermissionSelection(card,p.Growth==path);
                FormationText(card,GrowthPaths.Name(path)+" · "+GrowthPaths.Role(character,path)+(p.Growth==path?"  [적용 중]":""),new Vector2(.02f,.70f),new Vector2(.98f,1),22).color=formationGold;
                FormationText(card,GrowthPaths.Description(path),new Vector2(.02f,.46f),new Vector2(.98f,.70f),18).color=formationTeal;
                FormationText(card,GrowthPaths.CombatDescription(character,path),new Vector2(.02f,.01f),new Vector2(.98f,.46f),18);
                if(card.GetComponent<Button>().interactable)controls.Add(card.GetComponent<Button>());
            }
            var mastery=FormationButton(content,"성장 방향 기술 숙련 안내","기술 숙련 안내",new Vector2(.02f,.015f),new Vector2(.52f,.10f),()=>ShowMastery(character));
            var close=FormationButton(content,"성장 방향 닫기","닫기",new Vector2(.66f,.015f),new Vector2(.98f,.10f),()=>CloseUnitDetails());
            controls.Add(mastery.GetComponent<Button>());controls.Add(close.GetComponent<Button>());
            for(int i=0;i<controls.Count;i++)
            {
                var navigation=new Navigation{mode=Navigation.Mode.Explicit};
                navigation.selectOnUp=navigation.selectOnLeft=controls[(i+controls.Count-1)%controls.Count];
                navigation.selectOnDown=navigation.selectOnRight=controls[(i+1)%controls.Count];controls[i].navigation=navigation;
            }
            FocusGrowthControl(close.GetComponent<Button>());
        }
    }
}
