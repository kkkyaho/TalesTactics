using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator SignatureWeaponsFollowAllActionsDirectionsAndLiveUnequip()
        {
            var motions=new List<CharacterMotion>();var camera=director.Board.BattleCamera;
            foreach(var item in director.Catalog.Equipment.Where(e=>e.Appearance!=null))
            {
                var c=director.Catalog.Characters.Single(x=>x.Id==item.ExclusiveCharacterId);
                var unit=new UnitRuntime(c,Team.Player,director.Catalog.Rules);unit.Equipment[0]=item;
                var go=new GameObject("Signature motion "+c.Id);go.transform.SetParent(director.transform);
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();motion.Initialize(renderer,unit,camera);motions.Add(motion);
            }
            Assert.That(motions.Count,Is.EqualTo(10));
            for(int rotation=0;rotation<4;rotation++)
            {
                camera.transform.rotation=Quaternion.Euler(45,rotation*90,0);
                foreach(AnimationKind action in Enum.GetValues(typeof(AnimationKind)))
                {
                    foreach(var m in motions)m.Set(action);yield return new WaitForSeconds(.12f);yield return new WaitForEndOfFrame();
                    foreach(var m in motions)Assert.That(m.Owner.Equipment[0].Appearance.Frames.Any(f=>f.Equipped==m.GetComponent<SpriteRenderer>().sprite),Is.True,m.Owner.Data.Id+" "+action+" "+rotation);
                }
                foreach(var m in motions)m.BeginSkill(m.Owner.Data.UltimateSkill,.2f,.4f);
                yield return new WaitForSeconds(.12f);yield return new WaitForEndOfFrame();
                foreach(var m in motions){Assert.That(m.Owner.Equipment[0].Appearance.Frames.Any(f=>f.Equipped==m.GetComponent<SpriteRenderer>().sprite),Is.True);m.ReleaseSkill();}
                yield return new WaitForSeconds(.12f);yield return new WaitForEndOfFrame();
                foreach(var m in motions)Assert.That(m.Owner.Equipment[0].Appearance.Frames.Any(f=>f.Equipped==m.GetComponent<SpriteRenderer>().sprite),Is.True);
            }
            foreach(var m in motions){m.Owner.Equipment[0]=null;m.Set(AnimationKind.Idle);}yield return new WaitForEndOfFrame();
            foreach(var m in motions)Assert.That(m.GetComponent<SpriteRenderer>().sprite,Is.SameAs(m.Owner.Data.Sprites.Get(CharacterMotion.ViewFacing(m.Owner.Facing,camera.transform.rotation))));
        }
        CharacterModelImage SignaturePreview()=>director.Hud.GetComponentsInChildren<CharacterModelImage>().Single(i=>i.name=="EquippedModelPreview");
        void OpenSignatureEquipment(CharacterData c)
        {
            director.Deployment.Clear();director.Deployment.Add(Array.IndexOf(director.Catalog.Characters,c));
            director.Hud.ShowIntermissionHub();Click("거점 캐릭터: "+c.Id);
        }
        [UnityTest] public IEnumerator SignatureEquipmentPreviewCancelsSavesReloadsAndUnequipsAllTen()
        {
            director.TrainingMode=false;
            foreach(var item in director.Catalog.Equipment.Where(e=>e.Appearance!=null))
            {
                var c=director.Catalog.Characters.Single(x=>x.Id==item.ExclusiveCharacterId);
                director.Campaign.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});director.Campaign.Get(c.Id).Equipment[0]=null;
                OpenSignatureEquipment(c);yield return null;Assert.That(SignaturePreview().sprite,Is.SameAs(c.Sprites.Front));
                string name="장착 후보: "+item.DisplayName;
                while(!director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name==name)){Click("다음 장비 후보");yield return null;}
                Click(name);yield return null;var equipped=SignaturePreview().sprite;Assert.That(equipped,Is.Not.SameAs(c.Sprites.Front));
                Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Image>().Any(i=>i.sprite==item.Icon),Is.True);
                Assert.That(director.Campaign.Get(c.Id).Equipment[0],Is.Null);Click("돌아가기 (미적용 취소)");yield return null;
                Assert.That(SignaturePreview().sprite,Is.SameAs(c.Sprites.Front));
                while(!director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name==name)){Click("다음 장비 후보");yield return null;}
                Click(name);yield return null;director.PersistCampaign=_=>false;Click("적용 · 저장");yield return null;
                Assert.That(director.Campaign.Get(c.Id).Equipment[0],Is.Null);director.PersistCampaign=_=>true;Click("적용 · 저장");yield return null;
                Assert.That(director.Campaign.Get(c.Id).Equipment[0],Is.EqualTo(item.Id));
                director.Hud.ShowDeployment();yield return null;
                Assert.That(director.Hud.GetComponentsInChildren<CharacterModelImage>().Single(i=>i.Character==c).sprite,Is.SameAs(equipped));
                var detail=new UnitRuntime(c,Team.Player,director.Catalog.Rules);new EquipmentLoadout(c,director.Campaign.Get(c.Id),director.Catalog.Equipment,director.Campaign).Apply(detail);
                director.Hud.ShowUnitDetails(detail);yield return null;
                for(int turn=0;turn<4;turn++)
                {
                    var body=director.Hud.transform.Find("UnitDetails/StatusModel/Body").GetComponent<CharacterModelImage>();
                    Assert.That(item.Appearance.Frames.Any(f=>f.Equipped==body.sprite),Is.True);Click("모델 회전");yield return null;
                }
                Click("상태 장비");yield return null;Assert.That(director.Hud.transform.Find("UnitDetails/StatusContent").GetComponentsInChildren<UnityEngine.UI.Image>().Any(i=>i.sprite==item.Icon),Is.True);
                director.Hud.CloseUnitDetails();OpenSignatureEquipment(c);yield return null;
                Assert.That(SignaturePreview().sprite,Is.SameAs(equipped));Click("장비 해제");yield return null;Assert.That(SignaturePreview().sprite,Is.SameAs(c.Sprites.Front));
                Click("적용 · 저장");yield return null;Assert.That(director.Campaign.Get(c.Id).Equipment[0],Is.Null);
            }
        }
        [UnityTest] public IEnumerator SignatureMovementGhostUsesEquippedWeaponAndRestoresDefault()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Active;
            Assert.That(unit.Team,Is.EqualTo(Team.Player));var item=director.Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==unit.Data.Id&&e.Appearance!=null);unit.Equipment[0]=item;
            var destination=director.Session.Grid.Tiles.Keys.First(p=>p!=unit.Position);
            director.Board.ShowPath(new List<Vector2Int>{unit.Position,destination});yield return new WaitForEndOfFrame();
            var ghost=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Single(r=>r.name=="Destination ghost");
            Assert.That(item.Appearance.Frames.Any(f=>f.Equipped==ghost.sprite),Is.True);
            director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();
            var original=unit.Data.Sprites.Get(CharacterMotion.ViewFacing(unit.Facing,director.Board.BattleCamera.transform.rotation));
            Assert.That(ghost.sprite,Is.SameAs(EquipmentAppearance.Resolve(unit,original)));
            unit.Equipment[0]=null;yield return new WaitForEndOfFrame();Assert.That(ghost.sprite,Is.SameAs(original));
        }
    }
}
