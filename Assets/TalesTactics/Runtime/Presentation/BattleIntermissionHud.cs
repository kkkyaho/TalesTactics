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
        }
        void BeginIntermission(string title,int tab)
        {
            BeginPreparation(title);intermissionLayout=true;IntermissionLayout();
            foreach(var p in new[]{header,left,center,commands,footer})p.GetComponent<UnityEngine.UI.Image>().color=new Color(.035f,.055f,.11f,1);
            var heading=header.GetComponentInChildren<TMP_Text>();heading.text=title;
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
            r.GetComponent<UnityEngine.UI.Image>().color=selected?new Color(.035f,.23f,.31f):new Color(.065f,.105f,.17f);
            var outline=r.GetComponent<UnityEngine.UI.Outline>();if(outline!=null)outline.effectColor=selected?formationGold:new Color(.40f,.34f,.23f);
        }
        RectTransform IntermissionPanel(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var r=FormationRect(parent,name,min,max,6);var img=r.gameObject.AddComponent<UnityEngine.UI.Image>();img.color=new Color(.035f,.065f,.11f,1);img.raycastTarget=false;
            var border=r.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(.5f,.4f,.25f);border.effectDistance=new Vector2(1,-1);return r;
        }
        void IntermissionPortrait(Transform parent,CharacterData c,Vector2 min,Vector2 max)
        {
            var r=FormationRect(parent,"Portrait-"+c.Id,min,max,4);var atlas=Resources.Load<Texture2D>("TalesTactics/FormationPortraits");int cell=Array.IndexOf(portraitIds,c.Id);
            if(atlas!=null&&cell>=0)
            {
                var art=FormationRect(r,"PortraitArt",Vector2.zero,Vector2.one);
                var img=art.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=atlas;img.uvRect=new Rect(cell%5*.2f,cell<5?.5f:0,.2f,.5f);img.raycastTarget=false;
                var fit=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            }
            else {var img=r.gameObject.AddComponent<UnityEngine.UI.Image>();img.sprite=c.Portrait!=null?c.Portrait:c.Sprites?.Front;img.preserveAspect=true;img.raycastTarget=false;}
        }
        void EquipmentArt(Transform parent,EquipmentData item,Vector2 min,Vector2 max)
        {
            if(item==null){FormationText(parent,"없음",min,max,18,TextAlignmentOptions.Center).color=new Color(.45f,.52f,.61f);return;}
            int cell=item==null?-1:item.Id=="healer-badge"?5:Array.IndexOf(equipmentArtIds,item.Id);var atlas=Resources.Load<Texture2D>("TalesTactics/EquipmentIcons");
            if(cell>=0&&atlas!=null)
            {
                var bounds=FormationRect(parent,"EquipmentArt-"+item.Id,min,max,4);var r=FormationRect(bounds,"ItemArt",Vector2.zero,Vector2.one);var img=r.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=atlas;
                img.uvRect=new Rect(cell%3/3f,(2-cell/3)/3f,1/3f,1/3f);img.raycastTarget=false;
                var fit=r.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            }
            else FormationIcon(parent,item==null?"wait":item.Slot==EquipmentSlot.Weapon?"attack":item.Slot==EquipmentSlot.Armor?"guard":"move",min,max,item==null?new Color(.35f,.43f,.52f):formationGold);
        }
        CharacterData IntermissionCharacter()=>battle.Catalog.Characters[Mathf.Clamp(selectedCharacter,0,battle.Catalog.Characters.Length-1)];
        void IntermissionRoster(Transform parent,CharacterData selected,Action<CharacterData> choose)
        {
            var chars=battle.Catalog.Characters;int pages=Math.Max(1,(chars.Length+9)/10);intermissionRosterPage=Mathf.Clamp(intermissionRosterPage,0,pages-1);
            var title=FormationText(parent,"캐릭터 선택",new Vector2(0,1),Vector2.one,18);title.color=formationGold;title.rectTransform.offsetMin=new Vector2(6,-34);title.rectTransform.offsetMax=new Vector2(-6,0);
            var grid=FormationRect(parent,"RosterGrid",Vector2.zero,Vector2.one);grid.offsetMax=new Vector2(0,-36);
            float bottom=pages>1?.17f:0;
            for(int i=0;i<Math.Min(10,chars.Length-intermissionRosterPage*10);i++)
            {
                int index=intermissionRosterPage*10+i;var c=chars[index];float row=(1-bottom)/2;
                var b=FormationButton(grid,c.DisplayName,"",new Vector2(i%5*.2f,bottom+(1-i/5)*row),new Vector2((i%5+1)*.2f,bottom+(2-i/5)*row),()=>{selectedCharacter=index;choose(c);});
                IntermissionSelection(b,c==selected);
                var artBounds=FormationRect(b,"PortraitBounds",Vector2.zero,Vector2.one);artBounds.offsetMin=new Vector2(0,32);
                IntermissionPortrait(artBounds,c,Vector2.zero,Vector2.one);
                var caption=FormationText(b,c.DisplayName.Split(' ')[0],Vector2.zero,new Vector2(1,0),16,TextAlignmentOptions.Center);caption.fontSizeMin=14;
                caption.rectTransform.offsetMin=new Vector2(2,0);caption.rectTransform.offsetMax=new Vector2(-2,32);
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
            FormationText(table,"능력치         현재  →  변경 후",new Vector2(.015f,.9f),new Vector2(.985f,1),17).color=formationTeal;
            string[] names={"HP","MP","공격","마력","방어","마방","민첩","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.MAG,before.DEF,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.MAG,after.DEF,after.MDF,after.SPD,after.MOV,after.JMP};
            for(int i=0;i<names.Length;i++)FormationText(table,StatChange(names[i],a[i],b[i]),new Vector2(.02f,.89f-(i+1)*.096f),new Vector2(.98f,.89f-i*.096f),18);
        }
        void IntermissionNotice(string text)=>FormationText(footer,text,new Vector2(.01f,.08f),new Vector2(.53f,.92f),17).color=formationGold;
        void IntermissionPrimary(string name,string label,Action action,bool enabled)
        {
            var b=FormationButton(footer,name,label,new Vector2(.75f,.14f),new Vector2(.99f,.86f),action,enabled,23);
            b.GetComponent<UnityEngine.UI.Image>().color=new Color(.04f,.29f,.42f);
        }
    }
}
