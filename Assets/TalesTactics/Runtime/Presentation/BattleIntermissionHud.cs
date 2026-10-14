using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        bool intermissionLayout;
        int intermissionRosterPage;
        static readonly string[] equipmentArtIds={"bronze-sword","iron-sword","leather-armor","reinforced-armor","vital-charm","guardian-medal","tempered-armor","swift-boots","focus-charm"};

        void IntermissionLayout()
        {
            ModelDeploymentLayout();
            Place(center,Vector2.zero,new Vector2(.66f,1),new Vector2(12,126),new Vector2(-6,-142));
            if(focusedEquipmentLayout)Place(center,Vector2.zero,Vector2.one,new Vector2(12,126),new Vector2(-12,-142));
        }
        void BeginIntermission(string title,int tab)
        {
            BeginPreparation(title);intermissionLayout=true;IntermissionLayout();
            foreach(var p in new[]{header,left,center,commands,footer})p.GetComponent<UnityEngine.UI.Image>().color=new Color(.035f,.055f,.11f,1);
            var heading=header.GetComponentInChildren<TMP_Text>();heading.text="";
            FormationButton(header,"거점으로","‹ 거점 · "+title,Vector2.zero,new Vector2(.23f,1),ShowIntermissionHub,true,20);
            Place(heading.rectTransform,Vector2.zero,new Vector2(.23f,1),new Vector2(18,4),new Vector2(-6,-4));
            FormationText(header,battle.TrainingMode?"훈련 · 저장 보상 없음":CampaignStages.Title(battle.SelectedStage),new Vector2(.23f,0),new Vector2(.50f,1),20,TextAlignmentOptions.Center);
            FormationText(header,battle.Campaign.Gold.ToString("N0")+" G",new Vector2(.50f,0),new Vector2(.70f,1),23,TextAlignmentOptions.Center).color=formationGold;
            string[] names={"출전 편성","장비 관리","장비 상점","성장 · 승급","임무 · 적 정보"};
            Action[] actions={ShowDeployment,ShowEquipmentRoster,()=>ShowShop(),ShowGrowthRoster,ShowMission};
            for(int i=0;i<names.Length;i++)
            {
                var b=FormationButton(left,names[i],names[i],new Vector2(i*.2f,0),new Vector2((i+1)*.2f,1),actions[i],true,21);
                IntermissionSelection(b,i==tab);
            }
            var bg=FormationRect(center,"IntermissionBackdrop",Vector2.zero,Vector2.one);
            var img=bg.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=Resources.Load<Texture2D>("TalesTactics/FormationBackdrop");img.color=new Color(.6f,.7f,.85f,.36f);img.raycastTarget=false;
        }
        void IntermissionSelection(RectTransform r,bool selected)
        {
            StyleButton(r,HudRole.Tab,selected);
        }
        RectTransform IntermissionPanel(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var r=FormationRect(parent,name,min,max,6);var img=r.gameObject.AddComponent<UnityEngine.UI.Image>();img.color=new Color(.035f,.065f,.11f,1);img.raycastTarget=false;
            var border=r.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(.5f,.4f,.25f);border.effectDistance=new Vector2(1,-1);OrnatePanel.Apply(img);return r;
        }
        void IntermissionPortrait(Transform parent,CharacterData c,Vector2 min,Vector2 max)
        {
            var r=FormationRect(parent,"Portrait-"+c.Id,min,max,4);var atlas=Resources.Load<Texture2D>("TalesTactics/FormationPortraits");int cell=Array.IndexOf(portraitIds,c.Id);
            if(atlas!=null&&cell>=0)
            {
                var art=FormationRect(r,"PortraitArt",Vector2.zero,Vector2.one);
                var img=art.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=atlas;img.uvRect=new Rect(cell%5*.2f,cell<5?.5f:0,.2f,.5f);img.raycastTarget=false;
                var fit=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;OrnateButtonGraphic.Create(art,true);
            }
            else {var img=r.gameObject.AddComponent<UnityEngine.UI.Image>();img.sprite=c.Portrait!=null?c.Portrait:c.Sprites?.Front;img.preserveAspect=true;img.raycastTarget=false;}
        }
        void EquipmentArt(Transform parent,EquipmentData item,Vector2 min,Vector2 max)
        {
            if(item==null){FormationText(parent,"없음",min,max,18,TextAlignmentOptions.Center).color=new Color(.45f,.52f,.61f);return;}
            if(item.Icon!=null)
            {
                var art=FormationRect(parent,"EquipmentArt-"+item.Id,min,max,4);
                var icon=art.gameObject.AddComponent<UnityEngine.UI.Image>();icon.sprite=item.Icon;icon.preserveAspect=true;icon.useSpriteMesh=true;icon.raycastTarget=false;
                return;
            }
            int cell=item==null?-1:item.Id=="healer-badge"?5:Array.IndexOf(equipmentArtIds,item.Id);var atlas=Resources.Load<Texture2D>("TalesTactics/EquipmentIcons");
            if(cell>=0&&atlas!=null)
            {
                var bounds=FormationRect(parent,"EquipmentArt-"+item.Id,min,max,4);var r=FormationRect(bounds,"ItemArt",Vector2.zero,Vector2.one);var img=r.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=atlas;
                img.uvRect=new Rect(cell%3/3f,(2-cell/3)/3f,1/3f,1/3f);img.raycastTarget=false;
                var fit=r.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            }
            else FormationIcon(parent,item==null?"wait":item.Slot==EquipmentSlot.Weapon?"attack":item.Slot==EquipmentSlot.Armor?"guard":"move",min,max,item==null?new Color(.35f,.43f,.52f):formationGold);
        }
        CharacterData[] JoinedCharacters()=>battle.Catalog.Characters.Where(battle.IsJoined).ToArray();
        CharacterData IntermissionCharacter()
        {
            var c=battle.Catalog.Characters[Mathf.Clamp(selectedCharacter,0,battle.Catalog.Characters.Length-1)];
            if(!battle.IsJoined(c))c=JoinedCharacters().First();selectedCharacter=Array.IndexOf(battle.Catalog.Characters,c);return c;
        }
        void IntermissionRoster(Transform parent,CharacterData selected,Action<CharacterData> choose)
        {
            var chars=JoinedCharacters();int pages=Math.Max(1,(chars.Length+5)/6);intermissionRosterPage=Mathf.Clamp(intermissionRosterPage,0,pages-1);
            var title=FormationText(parent,"캐릭터 선택",new Vector2(0,1),Vector2.one,18);title.color=formationGold;title.rectTransform.offsetMin=new Vector2(6,-34);title.rectTransform.offsetMax=new Vector2(-6,0);
            var grid=FormationRect(parent,"RosterGrid",Vector2.zero,Vector2.one);grid.offsetMax=new Vector2(0,-36);
            float bottom=pages>1?.22f:0;
            for(int i=0;i<Math.Min(6,chars.Length-intermissionRosterPage*6);i++)
            {
                var c=chars[intermissionRosterPage*6+i];int index=Array.IndexOf(battle.Catalog.Characters,c);float row=(1-bottom)/2;
                var b=FormationButton(grid,c.DisplayName,"",new Vector2(i%3/3f,bottom+(1-i/3)*row),new Vector2((i%3+1)/3f,bottom+(2-i/3)*row),()=>{selectedCharacter=index;choose(c);});
                IntermissionSelection(b,c==selected);
                var artBounds=FormationRect(b,"PortraitBounds",new Vector2(.02f,.04f),new Vector2(.36f,.96f));
                IntermissionPortrait(artBounds,c,Vector2.zero,Vector2.one);
                FormationText(b,c.DisplayName.Split(' ')[0],new Vector2(.35f,.04f),new Vector2(.99f,.96f),13,TextAlignmentOptions.Center);
            }
            if(pages>1)
            {
                FormationButton(parent,"이전 캐릭터","이전",Vector2.zero,new Vector2(.5f,bottom),()=>{intermissionRosterPage--;choose(selected);},intermissionRosterPage>0);
                FormationButton(parent,"다음 캐릭터","다음",new Vector2(.5f,0),new Vector2(1,bottom),()=>{intermissionRosterPage++;choose(selected);},intermissionRosterPage+1<pages);
            }
        }
        string ReadableBonus(EquipmentData item)=>EquipmentBonusText(item).Replace("STR","공격").Replace("MAG","마력").Replace("DEF","방어").Replace("MDF","마방").Replace("SPD","민첩").Replace("MOV","이동").Replace("JMP","도약");
        void IntermissionStats(Transform parent,Stats before,Stats after,Vector2 min,Vector2 max)
        {
            var table=IntermissionPanel(parent,"StatComparison",min,max);
            FormationText(table,"능력치  ·  현재 → 변경 후",new Vector2(.015f,.85f),new Vector2(.985f,1),17).color=formationTeal;
            string[] names={"HP","MP","공격","마력","방어","마방","민첩","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.MAG,before.DEF,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.MAG,after.DEF,after.MDF,after.SPD,after.MOV,after.JMP};
            for(int i=0;i<names.Length;i++){float x=i%2*.50f,top=.84f-i/2*.165f;FormationText(table,StatChange(names[i],a[i],b[i]),new Vector2(x,top-.16f),new Vector2(x+.50f,top),17);}
        }
        void IntermissionNotice(string text)=>FormationText(footer,text,new Vector2(.01f,.08f),new Vector2(.53f,.92f),17).color=formationGold;
        void IntermissionPrimary(string name,string label,Action action,bool enabled)
        {
            var b=FormationButton(footer,name,label,new Vector2(.75f,.14f),new Vector2(.99f,.86f),action,enabled,23);
            IntermissionGoldButton(b);
        }
    }
}
