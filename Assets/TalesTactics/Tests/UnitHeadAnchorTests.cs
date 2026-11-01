using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Tests
{
    public sealed class UnitHeadAnchorTests
    {
        [Test] public void EveryShippedDirectionalBodyHasAnOfflineHeadAnchor()
        {
            int checkedSprites=0;
            foreach(var guid in AssetDatabase.FindAssets("t:CharacterData",new[]{"Assets/TalesTactics/Content"}))
            {
                var data=AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
                if(data?.Sprites==null)continue;
                foreach(Facing facing in Enum.GetValues(typeof(Facing)))
                {
                    var sprite=data.Sprites.Get(facing);if(sprite==null)continue;
                    Assert.That(UnitHeadAnchors.TryGetLocal(sprite,out var point),Is.True,data.Id+" "+facing+" needs offline head metadata.");
                    var uv=Normalize(sprite,point);
                    Assert.That(uv.x,Is.InRange(0f,1f));Assert.That(uv.y,Is.InRange(0f,1f));checkedSprites++;
                }
            }
            Assert.That(checkedSprites,Is.GreaterThanOrEqualTo(160));
        }
        [TestCase("sentinel",.78f,.88f)]
        [TestCase("jade",.76f,.84f)]
        public void LongSpearsDoNotSetTheFourDirectionalHeadPositions(string id,float low,float high)
        {
            var data=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/TalesTactics/Content/Characters/"+id+".asset");
            Assert.That(data,Is.Not.Null);
            foreach(Facing facing in Enum.GetValues(typeof(Facing)))
            {
                var sprite=data.Sprites.Get(facing);Assert.That(UnitHeadAnchors.TryGetLocal(sprite,out var point),Is.True);
                var uv=Normalize(sprite,point);
                Assert.That(uv.y,Is.InRange(low,high),id+" "+facing+": anchor the helmet/hair, not the much higher spear tip.");
                Assert.That(uv.x,Is.InRange(.30f,.66f),"Use the visible head center, not the weapon-biased sprite center.");
            }
        }
        [Test] public void FarahTransparentPaddingAndDirectionalPivotsDoNotLiftTheGauge()
        {
            var data=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/TalesTactics/Content/Characters/farah.asset");
            foreach(Facing facing in Enum.GetValues(typeof(Facing)))
            {
                var sprite=data.Sprites.Get(facing);Assert.That(UnitHeadAnchors.TryGetLocal(sprite,out var point),Is.True);
                var uv=Normalize(sprite,point);float topPadding=(1-uv.y)*sprite.rect.height;
                Assert.That(topPadding,Is.InRange(6f,16f),"The visible hair crown excludes the imported transparent top margin.");
                Assert.That(uv.x,Is.InRange(.40f,.65f),"The head location is measured from each view's art rather than a shared root pivot.");
            }
        }
        static Vector2 Normalize(Sprite sprite,Vector3 point)=>new Vector2(
            (point.x*sprite.pixelsPerUnit+sprite.pivot.x)/sprite.rect.width,
            (point.y*sprite.pixelsPerUnit+sprite.pivot.y)/sprite.rect.height);
    }
}
