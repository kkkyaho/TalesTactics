using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        bool modelDeploymentLayout;
        int formationFilter, formationSort, formationPage;
        readonly Color formationGold = new Color(.83f,.70f,.43f);
        readonly Color formationTeal = new Color(.26f,.83f,.83f);
        static readonly string[] portraitIds = {"cless","mint","farah","jade","tear","natalia","velvet","alphen","shionne","kisara"};
        static readonly string[] formationFilters = {"전체 병과","근접 병과","원거리 병과","마법 · 지원","출전 인원"};
        static readonly string[] formationSorts = {"기본순","레벨순","이름순"};

        void ModelDeploymentLayout()
        {
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-76),new Vector2(-12,-12));
            Place(left,new Vector2(0,1),Vector2.one,new Vector2(12,-130),new Vector2(-12,-86));
            Place(center,Vector2.zero,new Vector2(.66f,1),new Vector2(12,126),new Vector2(-6,-142));
            Place(commands,new Vector2(.66f,0),Vector2.one,new Vector2(6,126),new Vector2(-12,-142));
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,114));
        }

        // All positions are relative to the existing expanding Canvas; no second input system.
        RectTransform FormationRect(Transform parent,string name,Vector2 min,Vector2 max,float inset=0)
        {
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);
            var r=(RectTransform)g.transform;Place(r,min,max,Vector2.one*inset,Vector2.one*-inset);return r;
        }
        TMP_Text FormationText(Transform parent,string value,Vector2 min,Vector2 max,int size=20,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft)
        {
            var t=Label(parent,value,0,40,size);Place(t.rectTransform,min,max,new Vector2(6,2),new Vector2(-6,-2));
            t.alignment=alignment;t.enableAutoSizing=true;t.fontSizeMin=Mathf.Min(size,15);t.fontSizeMax=size;return t;
        }
        RectTransform FormationButton(Transform parent,string name,string text,Vector2 min,Vector2 max,Action action,bool enabled=true,int size=18)
        {
            Button(parent,name,0,action,enabled);var r=(RectTransform)parent.GetChild(parent.childCount-1);
            Place(r,min,max,new Vector2(6,4),new Vector2(-6,-4));
            var button=r.GetComponent<UnityEngine.UI.Button>();var colors=button.colors;colors.disabledColor=new Color(.5f,.55f,.65f,1);button.colors=colors;
            var t=r.GetComponentInChildren<TMP_Text>();t.text=text;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=14;t.fontSizeMax=size;
            Place(t.rectTransform,Vector2.zero,Vector2.one,new Vector2(8,2),new Vector2(-8,-2));return r;
        }
        void FormationIcon(Transform parent,string kind,Vector2 min,Vector2 max,Color color)
        {
            var r=FormationRect(parent,"Icon",min,max);var icon=r.gameObject.AddComponent<TacticalIcon>();icon.Kind=kind;icon.color=color;icon.raycastTarget=false;
        }
        string FormationWeaponIcon(CharacterData c)
        {
            switch(c.Id){case "mint":return "heal";case "farah":return "martial";case "jade":return "spear";case "tear":return "song";case "natalia":return "bow";case "shionne":return "rifle";case "kisara":return "guard";default:return "attack";}
        }
        CharacterProgress FormationProgress(CharacterData c)=>battle.Campaign.Characters.FirstOrDefault(p=>p.Id==c.Id)??new CharacterProgress{Id=c.Id};
        int FormationLevel(int index)=>battle.TrainingMode?25:FormationProgress(battle.Catalog.Characters[index]).Level;
        int[] FormationRoster()
        {
            var indices=Enumerable.Range(0,battle.Catalog.Characters.Length).Where(i=>
            {
                var id=battle.Catalog.Characters[i].Id;
                bool ranged=id=="natalia"||id=="shionne",magic=id=="mint"||id=="jade"||id=="tear";
                return formationFilter==0||formationFilter==1&&!ranged&&!magic||formationFilter==2&&ranged||formationFilter==3&&magic||formationFilter==4&&battle.Deployment.Contains(i);
            });
            if(formationSort==0)indices=indices.OrderBy(i=>{int order=Array.IndexOf(portraitIds,battle.Catalog.Characters[i].Id);return order<0?100+i:order;});
            if(formationSort==1)indices=indices.OrderByDescending(FormationLevel).ThenBy(i=>i);
            if(formationSort==2)indices=indices.OrderBy(i=>battle.Catalog.Characters[i].DisplayName,StringComparer.Ordinal);
            return indices.ToArray();
        }
        void ToggleFormation(int index)
        {
            if(battle.Session!=null||InputModalOpen)return;
            if(battle.Deployment.Contains(index))battle.Deployment.Remove(index);
            else if(battle.Deployment.Count<battle.Catalog.Rules.MaxDeployment)battle.Deployment.Add(index);
            selectedCharacter=index;RenderDeployment();
        }
        void RenderModelDeployment()
        {
            if(battle.Session!=null)return;
            BeginPreparation("출전 편성");modelDeploymentLayout=true;ModelDeploymentLayout();
            foreach(var p in new[]{header,left,center,commands,footer})p.GetComponent<UnityEngine.UI.Image>().color=new Color(.035f,.055f,.11f,1);
            selectedCharacter=Mathf.Clamp(selectedCharacter,0,battle.Catalog.Characters.Length-1);
            var heading=header.GetComponentInChildren<TMP_Text>();heading.text="출전 편성";
            Place(heading.rectTransform,new Vector2(0,0),new Vector2(.19f,1),new Vector2(18,4),new Vector2(-6,-4));
            FormationText(header,battle.TrainingMode?"훈련 · Lv25":CampaignStages.Title(battle.SelectedStage),new Vector2(.19f,0),new Vector2(.49f,1),22,TextAlignmentOptions.Center);
            FormationText(header,battle.Campaign.Gold.ToString("N0")+" G",new Vector2(.49f,0),new Vector2(.70f,1),22,TextAlignmentOptions.Center).color=formationGold;
            FormationButton(left,"임무 선택","임무 선택 · 권장 Lv"+CampaignStages.Get(battle.SelectedStage).EntryLevel,new Vector2(0,0),new Vector2(.25f,1),()=>ShowChapterPage(battle.SelectedStage/3));
            FormationButton(left,"임무 · 적 정보","임무 · 적 정보",new Vector2(.25f,0),new Vector2(.40f,1),ShowMission);
            FormationButton(left,"장비 관리","장비 관리",new Vector2(.40f,0),new Vector2(.54f,1),ShowEquipmentRoster);
            FormationButton(left,"장비 상점","장비 상점",new Vector2(.54f,0),new Vector2(.68f,1),()=>ShowShop());
            FormationButton(left,"성장 · 승급","성장 · 승급",new Vector2(.68f,0),new Vector2(.82f,1),ShowGrowthRoster);
            FormationButton(left,"전투 규칙 설정","훈련 · 전투 설정",new Vector2(.82f,0),Vector2.one,ShowBattleOptions);

            var backdrop=Resources.Load<Texture2D>("TalesTactics/FormationBackdrop");
            if(backdrop!=null)
            {
                var bg=FormationRect(center,"FormationBackdrop",Vector2.zero,Vector2.one);
                var img=bg.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=backdrop;img.color=new Color(.8f,.85f,1,.85f);img.raycastTarget=false;
            }
            var toolbar=FormationRect(center,"FormationToolbar",new Vector2(0,1),Vector2.one);
            toolbar.offsetMin=new Vector2(6,-62);toolbar.offsetMax=new Vector2(-6,-6);
            FormationText(toolbar,"출전 부대  <color=#E3C27D>"+battle.Deployment.Count+"</color> / "+battle.Catalog.Rules.MaxDeployment,Vector2.zero,new Vector2(.45f,1),25);
            FormationButton(toolbar,"병과 필터",formationFilters[formationFilter]+" ›",new Vector2(.45f,0),new Vector2(.75f,1),()=>{formationFilter=(formationFilter+1)%formationFilters.Length;formationPage=0;RenderDeployment();});
            FormationButton(toolbar,"편성 정렬",formationSorts[formationSort]+" ›",new Vector2(.75f,0),Vector2.one,()=>{formationSort=(formationSort+1)%formationSorts.Length;RenderDeployment();});
            var indices=FormationRoster();int pages=Math.Max(1,(indices.Length+9)/10);formationPage=Mathf.Clamp(formationPage,0,pages-1);
            var grid=FormationRect(center,"FormationModels",Vector2.zero,Vector2.one);
            grid.offsetMin=new Vector2(12,pages>1?52:14);grid.offsetMax=new Vector2(-12,-70);
            for(int slot=0;slot<Math.Min(10,indices.Length-formationPage*10);slot++)
                DrawFormationModel(grid,indices[formationPage*10+slot],slot);
            if(indices.Length==0)FormationText(grid,"조건에 맞는 캐릭터가 없습니다.\n병과 필터를 변경하세요.",Vector2.zero,Vector2.one,23,TextAlignmentOptions.Center);
            if(pages>1)
            {
                var paging=FormationRect(center,"RosterPages",Vector2.zero,new Vector2(1,0));paging.offsetMin=new Vector2(8,6);paging.offsetMax=new Vector2(-8,50);
                FormationButton(paging,"이전 캐릭터","이전",Vector2.zero,new Vector2(.3f,1),()=>{formationPage--;RenderDeployment();},formationPage>0);
                FormationText(paging,(formationPage+1)+" / "+pages,new Vector2(.3f,0),new Vector2(.7f,1),18,TextAlignmentOptions.Center);
                FormationButton(paging,"다음 캐릭터","다음",new Vector2(.7f,0),Vector2.one,()=>{formationPage++;RenderDeployment();},formationPage+1<pages);
            }
            DrawFormationDetails();
            FormationText(footer,"모델 선택: 정보 보기   ·   출전 표시: 편성 변경",new Vector2(0,.48f),new Vector2(.32f,1),17);
            string notice=string.IsNullOrEmpty(battle.SaveNotice)?(battle.TrainingMode?"훈련 · 저장 보상 없음":CampaignMissions.Description(battle.SelectedStage)):battle.SaveNotice;
            FormationText(footer,notice,Vector2.zero,new Vector2(.32f,.48f),16).color=new Color(.7f,.77f,.83f);
            FormationButton(footer,"편성 프리셋","프리셋",new Vector2(.32f,.16f),new Vector2(.46f,.84f),()=>ShowPresets(),true,20);
            FormationButton(footer,"전체 해제","전체 해제",new Vector2(.46f,.16f),new Vector2(.60f,.84f),()=>{battle.Deployment.Clear();RenderDeployment();},battle.Deployment.Count>0,21);
            FormationButton(footer,"균형 6인 추천 편성","추천 편성",new Vector2(.60f,.16f),new Vector2(.75f,.84f),()=>{battle.Deployment.Clear();battle.Deployment.AddRange(TacticalDevelopment.Recommended(battle.Catalog));RenderDeployment();},true,21);
            var start=FormationButton(footer,"전투 시작","출전 확정",new Vector2(.76f,.12f),new Vector2(1,.88f),battle.RequestBattle,battle.Deployment.Count>0,28);
            start.GetComponent<UnityEngine.UI.Image>().color=new Color(.04f,.29f,.42f);
        }
        void DrawFormationModel(RectTransform grid,int index,int slot)
        {
            var c=battle.Catalog.Characters[index];bool deployed=battle.Deployment.Contains(index),selected=index==selectedCharacter;
            float x=slot%5/5f,y=slot/5*.5f;
            var cell=FormationRect(grid,"ModelCell-"+c.Id,new Vector2(x,.5f-y),new Vector2(x+.2f,1-y),3);
            var ring=FormationRect(cell,"DeploymentRing",new Vector2(.08f,0),new Vector2(.92f,0));
            ring.offsetMin=new Vector2(0,74);ring.offsetMax=new Vector2(0,94);
            var ellipse=ring.gameObject.AddComponent<FormationRing>();ellipse.color=selected?formationGold:deployed?formationTeal:new Color(.38f,.43f,.49f,.45f);ellipse.raycastTarget=false;
            var model=FormationButton(cell,(deployed?"● ":"○ ")+c.DisplayName,"",Vector2.zero,Vector2.one,()=>{selectedCharacter=index;RenderDeployment();});
            model.offsetMin=new Vector2(8,78);model.offsetMax=new Vector2(-8,-20);
            model.GetComponent<UnityEngine.UI.Image>().color=Color.white;model.GetComponent<UnityEngine.UI.Outline>().enabled=false;
            var button=model.GetComponent<UnityEngine.UI.Button>();var colors=button.colors;colors.normalColor=Color.clear;
            colors.highlightedColor=colors.selectedColor=new Color(.2f,.45f,.65f,.16f);colors.pressedColor=new Color(.3f,.65f,.75f,.24f);button.colors=colors;
            var body=FormationRect(model,"BattleModel",Vector2.zero,Vector2.one);
            body.pivot=new Vector2(.5f,0);Place(body,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var sprite=body.gameObject.AddComponent<UnityEngine.UI.Image>();sprite.sprite=c.Sprites?.Front;sprite.preserveAspect=true;sprite.raycastTarget=false;
            FormationIcon(cell,FormationWeaponIcon(c),new Vector2(.04f,.81f),new Vector2(.21f,.94f),formationGold);
            if(selected)FormationText(cell,"▼",new Vector2(.32f,.89f),new Vector2(.68f,1),23,TextAlignmentOptions.Center).color=formationGold;
            var caption=FormationRect(cell,"ModelCaption",Vector2.zero,new Vector2(1,0));caption.offsetMax=new Vector2(0,88);
            var toggle=FormationButton(caption,"편성 변경: "+c.Id,deployed?"출전 중":"출전 추가",new Vector2(.14f,.59f),new Vector2(.86f,1),()=>ToggleFormation(index),deployed||battle.Deployment.Count<battle.Catalog.Rules.MaxDeployment,17);
            toggle.GetComponent<UnityEngine.UI.Image>().color=deployed?new Color(.025f,.24f,.29f):new Color(.06f,.09f,.14f,1);
            toggle.GetComponentInChildren<TMP_Text>().color=deployed?formationTeal:new Color(.66f,.72f,.8f);
            FormationText(caption,"Lv "+FormationLevel(index),new Vector2(0,.32f),new Vector2(1,.62f),18,TextAlignmentOptions.Center).color=formationGold;
            FormationText(caption,c.DisplayName.Split(' ')[0],Vector2.zero,new Vector2(1,.34f),21,TextAlignmentOptions.Center);
        }
        void DrawFormationDetails()
        {
            var c=battle.Catalog.Characters[selectedCharacter];var progress=FormationProgress(c);
            int level=FormationLevel(selectedCharacter);bool promoted=progress.Promoted&&!battle.TrainingMode;
            var gear=new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign);var stats=gear.Preview(level,promoted);
            FormationText(commands,"캐릭터 정보",new Vector2(.02f,.92f),new Vector2(.98f,1),25).color=formationGold;
            var portrait=FormationRect(commands,"CharacterPortrait",new Vector2(.035f,.46f),new Vector2(.49f,.91f));
            var atlas=Resources.Load<Texture2D>("TalesTactics/FormationPortraits");int cell=Array.IndexOf(portraitIds,c.Id);
            if(atlas!=null&&cell>=0)
            {
                var art=FormationRect(portrait,"PortraitArt",Vector2.zero,Vector2.one);
                var img=art.gameObject.AddComponent<UnityEngine.UI.RawImage>();img.texture=atlas;
                img.uvRect=new Rect(cell%5*.2f,cell<5?.5f:0,.2f,.5f);img.raycastTarget=false;
                var aspect=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            }
            else {var img=portrait.gameObject.AddComponent<UnityEngine.UI.Image>();img.sprite=c.Portrait!=null?c.Portrait:c.Sprites?.Front;img.preserveAspect=true;img.raycastTarget=false;}
            FormationText(commands,c.DisplayName,new Vector2(.52f,.83f),new Vector2(.98f,.92f),25);
            FormationText(commands,(promoted?c.PromotionJob:c.Job)+" · Lv "+level,new Vector2(.52f,.77f),new Vector2(.98f,.83f),17).color=formationGold;
            FormationMeter(commands,"HP",stats.HP,new Vector2(.52f,.68f),new Vector2(.97f,.76f),new Color(.32f,.68f,.38f));
            FormationMeter(commands,"MP",stats.MP,new Vector2(.52f,.59f),new Vector2(.97f,.67f),new Color(.18f,.57f,.79f));
            FormationText(commands,$"공격  {stats.STR}    방어  {stats.DEF}\n마력  {stats.MAG}    마방  {stats.MDF}\n민첩  {stats.SPD}    이동  {stats.MOV}",new Vector2(.52f,.43f),new Vector2(.98f,.59f),18);
            FormationText(commands,TacticalDevelopment.Role(c),new Vector2(.03f,.40f),new Vector2(.51f,.46f),17,TextAlignmentOptions.Center).color=formationTeal;
            FormationText(commands,"착용 장비",new Vector2(.03f,.345f),new Vector2(.97f,.405f),21).color=formationGold;
            for(int i=0;i<3;i++)
            {
                var slot=(EquipmentSlot)i;var item=gear.Get(slot);
                var button=FormationButton(commands,"장비 슬롯: "+slot,"",new Vector2(.025f+i*.32f,.18f),new Vector2(.345f+i*.32f,.345f),()=>{selectedEquipmentSlot=slot;equipmentPage=0;ShowEquipment(c);});
                FormationIcon(button,i==0?FormationWeaponIcon(c):i==1?"guard":"move",new Vector2(.34f,.40f),new Vector2(.66f,.93f),item==null?new Color(.38f,.44f,.52f):formationGold);
                FormationText(button,item==null?SlotName(slot)+" · 없음":item.DisplayName,new Vector2(0,0),new Vector2(1,.40f),16,TextAlignmentOptions.Center);
            }
            FormationText(commands,"특성 · "+TacticalDevelopment.TraitName(progress.Trait),new Vector2(.03f,.11f),new Vector2(.65f,.17f),19).color=formationTeal;
            bool chosen=battle.Deployment.Contains(selectedCharacter);
            FormationButton(commands,chosen?"편성에서 제외":"출전 편성에 추가",chosen?"출전 제외":"출전 추가",new Vector2(.64f,.10f),new Vector2(.98f,.17f),()=>ToggleFormation(selectedCharacter),chosen||battle.Deployment.Count<battle.Catalog.Rules.MaxDeployment,16);
            FormationButton(commands,"편성 캐릭터 능력","능력",new Vector2(.025f,.015f),new Vector2(.345f,.09f),()=>
            {
                var unit=new UnitRuntime(c,Team.Player,battle.Catalog.Rules,level){Promoted=promoted,Trait=progress.Trait};gear.Apply(unit);unit.CurrentHP=unit.Stats.HP;unit.CurrentMP=unit.Stats.MP;ShowUnitDetails(unit);
            },true,21);
            FormationButton(commands,"선택 캐릭터 장비","장비",new Vector2(.345f,.015f),new Vector2(.665f,.09f),()=>ShowEquipment(c),true,21);
            FormationButton(commands,"선택 캐릭터 성장","성장",new Vector2(.665f,.015f),new Vector2(.985f,.09f),()=>ShowGrowth(c),true,21);
        }
        void FormationMeter(Transform parent,string title,int value,Vector2 min,Vector2 max,Color color)
        {
            var r=FormationRect(parent,title+" meter",min,max);
            FormationText(r,title+"   "+value+" / "+value,new Vector2(0,.28f),Vector2.one,19);
            var bar=FormationRect(r,"Fill",Vector2.zero,new Vector2(1,.19f));var img=bar.gameObject.AddComponent<UnityEngine.UI.Image>();img.color=color;img.raycastTarget=false;
        }
        void ShowDeploymentMissions()
        {
            if(battle.Session!=null)return;BeginPreparation("임무 선택");
            Label(left,"장 목록 · "+(chapterPage+1)+" / "+((CampaignStages.Count+2)/3),12,36,22);
            for(int i=chapterPage*3;i<Math.Min(CampaignStages.Count,(chapterPage+1)*3);i++)
            {
                int stage=i;bool unlocked=CampaignStages.Unlocked(battle.Campaign,i);
                Button(left,(battle.SelectedStage==i?"● ":"")+CampaignStages.Title(i)+(battle.Campaign.StoryProgress.Contains(CampaignStages.Id(i))?" (완료)":unlocked?"":" (잠김)"),66+(i%3)*82,()=>{battle.SelectedStage=stage;ShowDeploymentMissions();},unlocked,66);
            }
            Button(left,"이전 장 목록",332,()=>ShowChapterPage(chapterPage-1),chapterPage>0,40);HalfButton(left,0);
            Button(left,"다음 장 목록",332,()=>ShowChapterPage(chapterPage+1),(chapterPage+1)*3<CampaignStages.Count,40);HalfButton(left,1);
            Label(center,CampaignStages.Title(battle.SelectedStage),20,50,26);
            Label(center,(battle.TrainingMode?ObjectiveNames.Name(battle.TrainingObjective):CampaignMissions.Description(battle.SelectedStage))+"\n\n권장 Lv"+CampaignStages.Get(battle.SelectedStage).EntryLevel+" · 6인 · 적 Lv"+CampaignStages.EnemyLevel(battle.SelectedStage)+"\n\n"+CampaignEconomy.Preview(battle.Campaign,battle.SelectedStage),95,270,20);
            Button(commands,"임무 · 적 정보",22,ShowMission);
            Button(commands,"전투 전 이야기",80,()=>battle.ReplayStory(false));
            Button(commands,"전투 후 이야기",138,()=>battle.ReplayStory(true),battle.Campaign.StoryProgress.Contains(CampaignStages.Id(battle.SelectedStage)));
            Button(commands,"출전 준비로",224,ShowDeployment,true,54);
            Label(footer,"임무를 선택한 뒤 출전 준비로 돌아가 편성을 확정하세요.",20,60,20);
        }
    }
}
