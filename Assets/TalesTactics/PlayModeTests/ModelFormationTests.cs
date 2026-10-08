using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        UnityEngine.UI.Button FormationControl(string name)=>director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
        void PointAndSubmit(string name)
        {
            var b=FormationControl(name);var r=(RectTransform)b.transform;
            var p=director.GetComponent<GamepadPointer>();p.MoveTo(RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)));p.Submit();
        }
        [UnityTest] public IEnumerator ModelFormationPointerInspectionAndToggleAreSeparateAndCapped()
        {
            director.Deployment.Clear();director.Deployment.AddRange(TacticalDevelopment.Recommended(director.Catalog));director.Hud.ShowDeployment();yield return null;
            var party=director.Deployment.ToArray();int index=System.Array.FindIndex(director.Catalog.Characters,c=>c.Id=="velvet");var c=director.Catalog.Characters[index];
            PointAndSubmit("○ "+c.DisplayName);yield return null;
            Assert.That(director.Deployment,Is.EqualTo(party));
            Assert.That(FormationControl("출전 편성에 추가").interactable,Is.False);
            Assert.That(FormationControl("편성 변경: velvet").interactable,Is.False);
            PointAndSubmit("편성 변경: cless");yield return null;
            Assert.That(director.Deployment.Count,Is.EqualTo(party.Length-1));
            PointAndSubmit("편성 변경: velvet");yield return null;
            Assert.That(director.Deployment.Contains(index),Is.True);Assert.That(director.Deployment.Count,Is.EqualTo(director.Catalog.Rules.MaxDeployment));
            Assert.That(director.Deployment.Distinct().Count(),Is.EqualTo(director.Deployment.Count));
            Click("전체 해제");yield return null;
            Assert.That(director.Deployment,Is.Empty);Assert.That(FormationControl("전투 시작").interactable,Is.False);
            Click("균형 6인 추천 편성");yield return null;
            Assert.That(director.Deployment,Is.EqualTo(TacticalDevelopment.Recommended(director.Catalog)));
        }
        [UnityTest] public IEnumerator ModelFormationFiltersSortAndPortraitFollowCharacterIdentity()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();
            string original=JsonUtility.ToJson(director.Campaign);director.Hud.ShowDeployment();yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original),"Inspecting a roster must not add progress records");
            int jade=System.Array.FindIndex(director.Catalog.Characters,c=>c.Id=="jade");director.Campaign.Get("jade").Level=12;
            director.Hud.ShowDeployment();yield return null;var party=director.Deployment.ToArray();
            Click("편성 정렬");yield return null;
            var models=director.Hud.transform.Find("PreparationContent/FormationModels");
            Assert.That(models.GetChild(0).name,Is.EqualTo("ModelCell-jade"));
            Click((director.Deployment.Contains(jade)?"● ":"○ ")+director.Catalog.Characters[jade].DisplayName);yield return null;
            var portrait=director.Hud.transform.Find("Commands/CharacterPortrait/PortraitArt").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(portrait.texture.name,Is.EqualTo("FormationPortraits"));Assert.That(portrait.uvRect.x,Is.EqualTo(.6f).Within(.001f));
            Click("병과 필터");yield return null;Click("병과 필터");yield return null;
            models=director.Hud.transform.Find("PreparationContent/FormationModels");
            Assert.That(models.Cast<Transform>().Select(t=>t.name),Is.EquivalentTo(new[]{"ModelCell-natalia","ModelCell-shionne"}));
            Assert.That(director.Deployment,Is.EqualTo(party));
            Click("병과 필터");yield return null;Click("병과 필터");yield return null;
            Click("전체 해제");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("조건에 맞는 캐릭터")),Is.True);
            Click("균형 6인 추천 편성");yield return null;
            Assert.That(director.Hud.transform.Find("PreparationContent/FormationModels").childCount,Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator ModelFormationGearTraitsAndModalUseRealPreviewAndPreserveParty()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();var c=director.Catalog.Characters[0];var p=director.Campaign.Get(c.Id);
            p.Level=7;p.Trait=TacticalTrait.Swift;
            var item=director.Catalog.Equipment.First(e=>e.Slot==EquipmentSlot.Weapon&&e.Weapon==c.Weapon);
            director.Campaign.Inventory.RemoveAll(e=>e.Id==item.Id);director.Campaign.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});p.Equipment[0]=item.Id;
            director.Hud.ShowDeployment();yield return null;var party=director.Deployment.ToArray();
            Assert.That(FormationControl("장비 슬롯: Weapon").GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text==item.DisplayName),Is.True);
            Click("편성 캐릭터 능력");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            var before=director.Deployment.ToArray();FormationControl("편성 변경: "+c.Id).onClick.Invoke();
            Assert.That(director.Deployment,Is.EqualTo(before),"Modal blocks roster changes even through direct callbacks");
            var s=new EquipmentLoadout(c,p,director.Catalog.Equipment,director.Campaign).Preview(7,false);
            Assert.That(director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("MOV "+s.MOV)&&t.text.Contains("STR "+s.STR)),Is.True);
            director.GetComponent<GamepadPointer>().Cancel();yield return null;Assert.That(director.Hud.UnitDetailsOpen,Is.False);
            Click("장비 슬롯: Armor");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("방어구 · 보유한 호환 장비")),Is.True);
            Click("출전 준비로");yield return null;Assert.That(director.Deployment,Is.EqualTo(party));
            Click("임무 선택");yield return null;Click("출전 준비로");yield return null;
            Click("전투 시작");yield return null;
            Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id),Is.EquivalentTo(party.Select(i=>director.Catalog.Characters[i].Id)));
        }
    }
}
