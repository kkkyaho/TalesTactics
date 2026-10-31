using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TalesTactics.Tests
{
    public sealed class MotionPacingTests
    {
        BattleCatalog Catalog=>AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
        [TestCase(4,1)] [TestCase(5,2)] [TestCase(10,5)] [TestCase(14,7)] [TestCase(22,10)]
        public void PacingKeepsEveryFrameOrderedAndCannotCrossTheReleaseBoundary(int length,int split)
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[length],ReleaseFrame=split};
            foreach(var pacing in new[]{SpritePhasePacing.Action,SpritePhasePacing.Cast})
                foreach(bool released in new[]{false,true})
                {
                    int first=released?split:0,last=released?length-1:split-1,previous=first;
                    var seen=new HashSet<int>();
                    for(int step=0;step<=1000;step++)
                    {
                        int frame=clip.PhaseFrameIndex(step/1000f,1,released,pacing);
                        Assert.That(frame,Is.InRange(previous,last));previous=frame;seen.Add(frame);
                    }
                    Assert.That(seen,Is.EquivalentTo(Enumerable.Range(first,last-first+1)));
                    Assert.That(clip.PhaseFrameIndex(100,1,released,pacing),Is.EqualTo(last));
                    Assert.That(clip.PhaseFrameIndex(float.NaN,1,released,pacing),Is.EqualTo(first));
                }
            Assert.That(clip.ReleaseFrame,Is.EqualTo(split));
        }
        [Test] public void ActionPacingHoldsAnticipationAndContactLongerThanPassingPoses()
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[10],ReleaseFrame=5};
            var prep=new int[10];var release=new int[10];
            for(int i=0;i<1000;i++)
            {
                prep[clip.PhaseFrameIndex(i/1000f,1,false,SpritePhasePacing.Action)]++;
                release[clip.PhaseFrameIndex(i/1000f,1,true,SpritePhasePacing.Action)]++;
            }
            Assert.That(prep[4],Is.GreaterThan(prep[3]*1.5f));
            Assert.That(release[5],Is.GreaterThan(release[6]*1.5f));
            Assert.That(release[9],Is.GreaterThan(release[8]));
        }
        [Test] public void RootCurvesJoinAtReleaseAndSettleWithinTheirAuthoredTravelAndLift()
        {
            var profiles=Enum.GetValues(typeof(CombatVisualStyle)).Cast<CombatVisualStyle>().Select(s=>CombatMotionProfile.For(s,true))
                .Concat(new[]{new CombatMotionProfile(.46f,.16f,.56f,19,.075f),new CombatMotionProfile(.025f,.018f,.055f,2,.025f)});
            foreach(var profile in profiles)
            {
                var start=profile.Sample(0,false);var ready=profile.Sample(1,false);var released=profile.Sample(0,true);var end=profile.Sample(1,true);
                Assert.That(new Vector3(start.Travel,start.Lift,start.Tilt),Is.EqualTo(Vector3.zero));
                Assert.That(released.Travel,Is.EqualTo(ready.Travel).Within(.00001f));
                Assert.That(released.Lift,Is.EqualTo(ready.Lift).Within(.00001f),"Release must not drop the elevated body");
                Assert.That(released.Tilt,Is.EqualTo(ready.Tilt).Within(.00001f));
                Assert.That(new Vector3(end.Travel,end.Lift,end.Tilt),Is.EqualTo(Vector3.zero));
                foreach(bool phase in new[]{false,true})for(int i=0;i<=100;i++)
                {
                    var sample=profile.Sample(i/100f,phase);
                    Assert.That(sample.Travel,Is.InRange(-profile.Recoil-.00001f,profile.Advance+profile.Recoil*.12f+.00001f));
                    Assert.That(sample.Lift,Is.InRange(0,profile.Lift+.00001f));
                }
            }
        }
        [Test] public void CastingUsesExistingSpellArtAndKeepsEveryEquippedReplacement()
        {
            var catalog=Catalog;int legacy=0,bosses=0;
            foreach(var character in catalog.Characters.Concat(catalog.Enemies).Concat(catalog.CrossoverVariants))
            {
                if(character.Id=="boundary-device")continue;
                var poses=character.Poses;string before=EditorJsonUtility.ToJson(character);
                var clip=CharacterMotionClips.Casting(character);
                var item=catalog.Equipment.FirstOrDefault(e=>e.Appearance!=null&&e.ExclusiveCharacterId==character.Id);
                foreach(Facing facing in Enum.GetValues(typeof(Facing)))
                {
                    var art=clip.Frames.Select(f=>f?.Get(facing)).ToArray();Assert.That(art.All(s=>s!=null),Is.True,character.Id);
                    Assert.That(art.Distinct().Count(),Is.GreaterThanOrEqualTo(3),character.Id);
                    if(poses.Cast.Get(facing)!=poses.Attack.Get(facing))
                        Assert.That(art,Has.No.Member(poses.Attack.Get(facing)),"No physical attack inserted in casting: "+character.Id);
                    if(item!=null)foreach(var sprite in art)
                        Assert.That(EquipmentAppearance.Resolve(character,item,sprite),Is.Not.SameAs(sprite),sprite.name);
                }
                if(CharacterVisualSizing.IsHero(character))
                {Assert.That(clip.Frames[clip.ReleaseFrame],Is.SameAs(poses.Skill.Frames[13]));Assert.That(clip.ReleaseFrame,Is.EqualTo(3));}
                else if(poses.Skill.Frames[2].Front==poses.Attack.Front)
                {legacy++;Assert.That(clip,Is.Not.SameAs(poses.Skill));}
                else{bosses++;Assert.That(clip,Is.SameAs(poses.Skill),"Preserve the authored boss spell sequence");}
                Assert.That(EditorJsonUtility.ToJson(character),Is.EqualTo(before),"Sampling never edits Content");
            }
            Assert.That(legacy,Is.GreaterThan(0));Assert.That(bosses,Is.GreaterThan(0));
        }
        static FieldInfo Clock(string name)=>typeof(CharacterMotion).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        static void Render(CharacterMotion motion)=>typeof(CharacterMotion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motion,null);
        [Test] public void OrdinarySpellRuntimeShowsPhasesAndRecoversWithStableReleaseAndEquipment()
        {
            var catalog=Catalog;var cameraObject=new GameObject("Motion pacing test camera");var camera=cameraObject.AddComponent<Camera>();
            camera.transform.rotation=Quaternion.identity;
            int heroes=0,enemies=0;
            try
            {
                foreach(var character in catalog.Characters.Concat(catalog.Enemies).Concat(catalog.CrossoverVariants))
                {
                    if(character.Id=="boundary-device")continue;
                    var skill=(character.Skills??Array.Empty<SkillData>()).FirstOrDefault(s=>s!=null&&!s.IsUltimate&&
                        (ArteProfiles.Resolve(s).IsSpell||SkillSignatures.IsSpell(SkillSignatures.Resolve(s))||s.Animation==AnimationKind.Cast));
                    if(skill==null)continue;
                    if(CharacterVisualSizing.IsHero(character))heroes++;else enemies++;
                    var item=catalog.Equipment.FirstOrDefault(e=>e.Appearance!=null&&e.ExclusiveCharacterId==character.Id);
                    var owner=new UnitRuntime(character,Team.Player,catalog.Rules){Position=new Vector2Int(2,3)};
                    var go=new GameObject("Motion pacing test actor");var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();
                    try
                    {
                        int hp=owner.CurrentHP,mp=owner.CurrentMP;var position=owner.Position;float speed=Time.timeScale;
                        foreach(var equipment in item==null?new EquipmentData[]{null}:new[]{null,item})
                            foreach(Facing facing in Enum.GetValues(typeof(Facing)))
                            {
                                owner.Facing=facing;owner.Equipment[0]=equipment;motion.Initialize(renderer,owner,camera);
                                var clip=CharacterMotionClips.Casting(character);motion.BeginSkill(skill,.6f,.8f);
                                Clock("started").SetValue(motion,Time.time-2);Render(motion);
                                Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(owner,clip.Frames[clip.ReleaseFrame-1].Get(facing))),character.Id);
                                Assert.That(motion.Presenting,Is.True,"Elapsed windup must not release itself");
                                motion.ReleaseSkill(.2f);float releaseAt=(float)Clock("releasedAt").GetValue(motion),duration=(float)Clock("recovery").GetValue(motion);
                                motion.ReleaseSkill(10);
                                Assert.That((float)Clock("releasedAt").GetValue(motion),Is.EqualTo(releaseAt));
                                Assert.That((float)Clock("recovery").GetValue(motion),Is.EqualTo(duration));
                                Render(motion);
                                Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(owner,clip.Frames[clip.ReleaseFrame].Get(facing))),character.Id);
                                var seen=new HashSet<Sprite>();
                                for(int i=0;i<20;i++)
                                {
                                    Clock("releasedAt").SetValue(motion,Time.time-duration*i/20);Render(motion);seen.Add(renderer.sprite);
                                    Assert.That(motion.transform.localScale,Is.EqualTo(Vector3.one),"Authored proportions: "+character.Id);
                                }
                                Assert.That(seen.Count,Is.GreaterThanOrEqualTo(2),"Cast must not overwrite every release/recovery pose: "+character.Id);
                                Clock("releasedAt").SetValue(motion,Time.time-duration-.01f);Render(motion);
                                Assert.That(motion.Presenting,Is.False);Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(owner,character.Sprites.Get(facing))));
                                Assert.That(owner.Position,Is.EqualTo(position));Assert.That(owner.Facing,Is.EqualTo(facing));
                            }
                        Assert.That(owner.CurrentHP,Is.EqualTo(hp));Assert.That(owner.CurrentMP,Is.EqualTo(mp));Assert.That(Time.timeScale,Is.EqualTo(speed));
                    }
                    finally{Object.DestroyImmediate(go);}
                }
                Assert.That(heroes,Is.GreaterThan(0));Assert.That(enemies,Is.GreaterThan(0));
            }
            finally{Object.DestroyImmediate(cameraObject);}
        }
    }
}
