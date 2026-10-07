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
        [UnityTest] public IEnumerator EntireRosterUsesAuthoredAttackPhasesAndCanBeInterrupted()
        {
            var camera=director.Board.BattleCamera;
            foreach(var data in director.Catalog.Characters.Concat(director.Catalog.Enemies))
            {
                var clip=data.Poses.AttackMotion;
                Assert.That(clip.Frames.Length,Is.EqualTo(4),data.Id);
                Assert.That(data.Poses.Skill.Frames.Length,Is.EqualTo(5),data.Id);
                Assert.That(data.Poses.Ultimate.Frames.Length,Is.EqualTo(4),data.Id);
                foreach(Facing direction in System.Enum.GetValues(typeof(Facing)))
                {
                    Assert.That(clip.Frames.Select(f=>f.Get(direction)).Distinct().Count(),Is.EqualTo(4),data.Id);
                    Assert.That(clip.Frames.All(f=>f.Get(direction)!=null),Is.True,data.Id);
                }
                var go=new GameObject("Animation phase review");go.transform.SetParent(director.transform);
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();
                var owner=new UnitRuntime(data,Team.Player,director.Catalog.Rules);
                motion.Initialize(renderer,owner,camera);
                var skill=ScriptableObject.CreateInstance<SkillData>();skill.Animation=AnimationKind.Attack;
                int hp=owner.CurrentHP,mp=owner.CurrentMP;
                motion.BeginSkill(skill,.2f,.6f);
                for(int rotation=0;rotation<4;rotation++)
                {
                    camera.transform.rotation=Quaternion.Euler(45,rotation*90,0);
                    yield return new WaitForEndOfFrame();
                    var facing=CharacterMotion.ViewFacing(owner.Facing,camera.transform.rotation);
                    Assert.That(renderer.sprite,Is.EqualTo(clip.Frames[0].Get(facing)));
                }
                yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();
                var view=CharacterMotion.ViewFacing(owner.Facing,camera.transform.rotation);
                Assert.That(renderer.sprite,Is.EqualTo(clip.Frames[0].Get(view)),"Must hold anticipation until release");
                motion.ReleaseSkill();var observed=new HashSet<Sprite>();float until=Time.time+.65f;
                while(Time.time<until){yield return new WaitForEndOfFrame();observed.Add(renderer.sprite);}
                foreach(var frame in clip.Frames.Skip(1))Assert.That(observed.Contains(frame.Get(view)),Is.True,data.Id);
                Assert.That(renderer.sprite,Is.EqualTo(data.Sprites.Get(view)));
                motion.BeginSkill(skill,.2f,.6f);motion.ReleaseSkill();motion.Set(AnimationKind.Dead);
                yield return new WaitForEndOfFrame();
                Assert.That(renderer.sprite,Is.EqualTo(data.Poses.Dead.Frames[0].Get(view)));
                motion.Set(AnimationKind.Idle);yield return new WaitForEndOfFrame();
                Assert.That(renderer.sprite,Is.EqualTo(data.Sprites.Get(view)));
                Assert.That(owner.CurrentHP,Is.EqualTo(hp));Assert.That(owner.CurrentMP,Is.EqualTo(mp));
                Object.Destroy(skill);Object.Destroy(go);
            }
        }
    }
}
