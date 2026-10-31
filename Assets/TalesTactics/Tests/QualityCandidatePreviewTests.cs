using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TalesTactics.Tests
{
    public sealed class QualityCandidatePreviewTests
    {
        readonly List<Object> owned=new List<Object>();
        T Own<T>(T value)where T:Object{owned.Add(value);return value;}
        Sprite Frame(string name)
        {
            var texture=Own(new Texture2D(8,8));var sprite=Own(Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.1f),8));sprite.name=name;return sprite;
        }
        [TearDown] public void Cleanup(){for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.DestroyImmediate(owned[i]);owned.Clear();}
        [Test] public void CandidateCloneRekeysBothWeaponSidesAndNestedMotionWithoutChangingOriginals()
        {
            var original=Frame("hero_0_0");var candidate=Frame("hero_0_0");var oldWeapon=Frame("hero_0_0");var newWeapon=Frame("hero_0_0");var unchanged=Frame("hero_other_0");
            var character=Own(ScriptableObject.CreateInstance<CharacterData>());character.Id="hero";character.Weapon=WeaponType.Sword;character.BaseStats=new Stats{HP=190,MP=85,STR=42};
            character.Sprites=new DirectionalSprites{Front=original,Back=unchanged,Left=original,Right=unchanged};
            character.Poses.AttackMotion=new DirectionalSpriteClip{Frames=new[]{character.Sprites,new DirectionalSprites{Front=original,Back=original}},ReleaseFrame=1};
            var appearance=Own(ScriptableObject.CreateInstance<EquipmentAppearance>());appearance.CharacterId="hero";appearance.EquipmentId="signature";appearance.Frames=new[]{new EquipmentAppearance.Frame{Original=original,Equipped=oldWeapon}};
            Assert.That(appearance.Resolve(original),Is.SameAs(oldWeapon),"Warm the original lookup cache first.");
            var weapon=Own(ScriptableObject.CreateInstance<EquipmentData>());weapon.Id="signature";weapon.ExclusiveCharacterId="hero";weapon.Slot=EquipmentSlot.Weapon;weapon.Weapon=WeaponType.Sword;weapon.Appearance=appearance;
            string characterBefore=EditorJsonUtility.ToJson(character),weaponBefore=EditorJsonUtility.ToJson(weapon),appearanceBefore=EditorJsonUtility.ToJson(appearance);
            var session=new QualityPresentationReview.CandidatePreviewAssets(new[]{new KeyValuePair<Sprite,Sprite>(original,candidate),new KeyValuePair<Sprite,Sprite>(oldWeapon,newWeapon)});
            CharacterData clone=null;EquipmentData equipped=null;EquipmentAppearance equippedAppearance=null;
            try
            {
                clone=session.Clone(character);equipped=session.Clone(weapon);equippedAppearance=equipped.Appearance;
                Assert.That(clone,Is.Not.SameAs(character));Assert.That(equippedAppearance,Is.Not.SameAs(appearance));
                Assert.That(clone.hideFlags,Is.EqualTo(HideFlags.HideAndDontSave));Assert.That(equipped.hideFlags,Is.EqualTo(HideFlags.HideAndDontSave));Assert.That(equippedAppearance.hideFlags,Is.EqualTo(HideFlags.HideAndDontSave));
                Assert.That(clone.Sprites.Front,Is.SameAs(candidate));Assert.That(clone.Sprites.Back,Is.SameAs(unchanged));
                Assert.That(clone.Poses.AttackMotion.Frames[1].Back,Is.SameAs(candidate));Assert.That(clone.Poses.AttackMotion.ReleaseFrame,Is.EqualTo(1));
                Assert.That(clone.BaseStats.HP,Is.EqualTo(character.BaseStats.HP));Assert.That(clone.BaseStats.STR,Is.EqualTo(character.BaseStats.STR));
                Assert.That(equippedAppearance.Frames[0].Original,Is.SameAs(candidate));Assert.That(equippedAppearance.Frames[0].Equipped,Is.SameAs(newWeapon));
                Assert.That(EquipmentAppearance.Resolve(clone,equipped,clone.Sprites.Front),Is.SameAs(newWeapon),"Remapping only Equipped would break lookup on the candidate base clip.");
                Assert.That(session.Clone(character),Is.SameAs(clone));Assert.That(session.Clone(weapon),Is.SameAs(equipped));Assert.That(session.CreatedCount,Is.EqualTo(3));
                session.AssertOriginalsUnchanged();
                Assert.That(EditorJsonUtility.ToJson(character),Is.EqualTo(characterBefore));Assert.That(EditorJsonUtility.ToJson(weapon),Is.EqualTo(weaponBefore));Assert.That(EditorJsonUtility.ToJson(appearance),Is.EqualTo(appearanceBefore));
            }
            finally{session.Dispose();}
            Assert.That(clone==null&&equipped==null&&equippedAppearance==null,Is.True);Assert.That(session.LiveCount,Is.Zero);Assert.That(session.DestroyedCount,Is.EqualTo(3));
            Assert.That(original!=null&&candidate!=null&&oldWeapon!=null&&newWeapon!=null,Is.True,"Borrowed source/candidate sprites must survive clone cleanup.");
            Assert.DoesNotThrow(session.Dispose);Assert.Throws<ObjectDisposedException>(()=>session.Clone(character));
        }
        [Test] public void CandidateMappingsRejectWrongNamesNullsAndAmbiguousOwnersBeforeCloning()
        {
            var a=Frame("pose");var b=Frame("pose");var c=Frame("pose");var wrong=Frame("other");
            Assert.Throws<InvalidOperationException>(()=>new QualityPresentationReview.CandidatePreviewAssets(new[]{new KeyValuePair<Sprite,Sprite>(a,wrong)}));
            Assert.Throws<InvalidOperationException>(()=>new QualityPresentationReview.CandidatePreviewAssets(new[]{new KeyValuePair<Sprite,Sprite>(a,null)}));
            Assert.Throws<InvalidOperationException>(()=>new QualityPresentationReview.CandidatePreviewAssets(new[]{new KeyValuePair<Sprite,Sprite>(a,b),new KeyValuePair<Sprite,Sprite>(a,c)}));
            Assert.Throws<InvalidOperationException>(()=>new QualityPresentationReview.CandidatePreviewAssets(new[]{new KeyValuePair<Sprite,Sprite>(a,b),new KeyValuePair<Sprite,Sprite>(c,b)}));
            Assert.Throws<InvalidOperationException>(()=>new QualityPresentationReview.CandidatePreviewAssets(Array.Empty<KeyValuePair<Sprite,Sprite>>()));
        }
        [Serializable] sealed class Selection {public int schemaVersion=1,sheets,frames;public Job[] jobs;}
        [Serializable] sealed class Job {public string source,candidate,measurement,currentSourceSha256,candidateSha256;public int frameCount;}
        [Test] public void PinnedSelectionRejectsChangedCandidateHashBeforeMetadataWithoutTouchingExistingAssets()
        {
            string artRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"TalesTactics/Art/SdCharacters"));
            string directoryName="_CandidateTest-"+Guid.NewGuid().ToString("N"),folder=Path.Combine(artRoot,directoryName);
            string source=Path.Combine(folder,"source.png"),candidate=Path.Combine(folder,"candidate.png"),fixture=Path.Combine(folder,"invalid-selection.json"),measurement=Path.Combine(folder,"not-created-measurement.json");
            Assert.That(Directory.Exists(folder)||File.Exists(folder+".meta"),Is.False,"Never reuse an existing asset directory or its metadata.");Directory.CreateDirectory(folder);
            try
            {
                // Real valid PNGs avoid import errors if the Editor notices this unique test
                // folder. No Refresh/ImportAsset is needed: hash rejection must precede all
                // metadata, measurement and AssetDatabase access in the candidate loader.
                var png=Own(new Texture2D(8,8,TextureFormat.RGBA32,false));
                png.SetPixels(Enumerable.Repeat(Color.red,64).ToArray());png.Apply(false,false);File.WriteAllBytes(source,png.EncodeToPNG());
                png.SetPixels(Enumerable.Repeat(Color.blue,64).ToArray());png.Apply(false,false);File.WriteAllBytes(candidate,png.EncodeToPNG());
                string sourceBefore=Hash(source),candidateBefore=Hash(candidate);
                var invalid=new Selection{sheets=1,frames=1,jobs=new[]{new Job{source=source,candidate=candidate,measurement=measurement,currentSourceSha256=sourceBefore,candidateSha256=new string('0',64),frameCount=1}}};
                File.WriteAllText(fixture,JsonUtility.ToJson(invalid));
                var error=Assert.Throws<InvalidOperationException>(()=>QualityPresentationReview.CandidatePreviewAssets.Load(fixture));Assert.That(error.Message,Does.Contain("Candidate hash mismatch"));
                Assert.That(Hash(source),Is.EqualTo(sourceBefore));Assert.That(Hash(candidate),Is.EqualTo(candidateBefore));Assert.That(File.Exists(measurement),Is.False);
            }
            finally
            {
                string resolved=Path.GetFullPath(folder),allowed=artRoot.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(!resolved.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(resolved)!=directoryName||(File.GetAttributes(resolved)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Refusing to delete a path other than this test's own new directory.");
                Directory.Delete(resolved,true);File.Delete(resolved+".meta");
            }
        }
        static string Hash(string path)
        {using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    }
}
