using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        bool hubLayout, focusedEquipmentLayout;
        public bool IntermissionHubOpen => hubLayout;
        void IntermissionGoldButton(RectTransform button)
        {
            StyleButton(button,HudRole.Primary);
            button.GetComponent<UnityEngine.UI.Image>().color=new Color(.075f,.23f,.27f);
            button.GetComponentInChildren<TMP_Text>().color=new Color(.92f,.95f,.98f);
            button.GetComponent<UnityEngine.UI.Outline>().effectColor=new Color(.53f,.76f,.76f);
        }
        void EquipmentChangedStats(Transform parent,Stats before,Stats after)
        {
            string[] names={"HP","MP","공격","마력","방어","마방","민첩","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.MAG,before.DEF,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.MAG,after.DEF,after.MDF,after.SPD,after.MOV,after.JMP};
            var changed=Enumerable.Range(0,names.Length).Where(i=>a[i]!=b[i]).ToArray();
            FormationText(parent,"장착 시 변화",new Vector2(.02f,.70f),new Vector2(.98f,.97f),18).color=formationGold;
            for(int j=0;j<Math.Min(3,changed.Length);j++)
            {int i=changed[j];float x=j/3f;FormationText(parent,StatChange(names[i],a[i],b[i]),new Vector2(x,.28f),new Vector2(x+1/3f,.70f),24);}
            if(changed.Length==0)FormationText(parent,"능력치 변화 없음",new Vector2(.02f,.28f),new Vector2(.98f,.70f),23);
            FormationButton(parent,"전체 능력치 보기",changed.Length>3?"전체 비교 · 추가 "+(changed.Length-3)+"개":"전체 능력치 보기",new Vector2(.02f,0),new Vector2(.48f,.28f),()=>
            {
                PrepareUnitModal(new UnitRuntime(IntermissionCharacter(),Team.Player,battle.Catalog.Rules));Clear(unitDetails);
                FormationText(unitDetails,"능력치 비교",new Vector2(.04f,.86f),new Vector2(.96f,.98f),25).color=formationGold;
                IntermissionStats(unitDetails,before,after,new Vector2(.04f,.16f),new Vector2(.96f,.86f));
                FormationButton(unitDetails,"능력치 비교 닫기","닫기",new Vector2(.5f,.025f),new Vector2(.96f,.145f),()=>CloseUnitDetails());
            });
        }
    }
}
