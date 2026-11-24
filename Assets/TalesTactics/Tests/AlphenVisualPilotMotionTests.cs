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
    public sealed class AlphenVisualPilotMotionTests
    {
        readonly List<Object> temporary=new List<Object>();
        BattleCatalog Catalog=>AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
        static FieldInfo Field(string name)=>typeof(CharacterMotion).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        static void Step(CharacterMotion motion)=>typeof(CharacterMotion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motion,null);
        T Keep<T>(T value) where T:Object {temporary.Add(value);return value;}
        UnitRuntime Unit(string id="alphen")=>new UnitRuntime(Catalog.Characters.Single(c=>c.Id==id),Team.Player,Catalog.Rules);
        Sprite Frame(string name)
        {
            var sprite=Keep(Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),new Vector2(.5f,0),2));
            sprite.name=name;return sprite;
        }
        DirectionalSpriteClip Clip(string name,int count)
        {
            var frames=new DirectionalSprites[count];
            for(int i=0;i<count;i++)frames[i]=new DirectionalSprites{
                Front=Frame(name+"-front-"+i),Back=Frame(name+"-back-"+i),
                Left=Frame(name+"-left-"+i),Right=Frame(name+"-right-"+i)};
            return new DirectionalSpriteClip{Frames=frames,ReleaseFrame=1};
        }
        AlphenVisualPilotMotion Pilot()
        {
            var pilot=Keep(ScriptableObject.CreateInstance<AlphenVisualPilotMotion>());
            pilot.Idle=Clip("idle",4);pilot.Attack=Clip("attack",3);pilot.Damage=Clip("damage",1);return pilot;
        }
        EquipmentData Signature()=>Catalog.Equipment.Single(e=>e.ExclusiveCharacterId=="alphen"&&e.Slot==EquipmentSlot.Weapon&&e.Appearance!=null);
        CharacterMotion Motion(UnitRuntime unit,out SpriteRenderer renderer,out Camera camera)
        {
            var actor=Keep(new GameObject("Alphen visual pilot test"));
            renderer=actor.AddComponent<SpriteRenderer>();var motion=actor.AddComponent<CharacterMotion>();
            camera=Keep(new GameObject("Alphen pilot test camera")).AddComponent<Camera>();
            motion.Initialize(renderer,unit,camera);return motion;
        }
        [TearDown] public void Dispose()
        {for(int i=temporary.Count-1;i>=0;i--)if(temporary[i]!=null)Object.DestroyImmediate(temporary[i]);temporary.Clear();}

        [Test] public void IdleUsesBriefBlinkSlowBreathingAndNoGameplayRandom()
        {
            var random=JsonUtility.ToJson(UnityEngine.Random.state);var seen=new HashSet<int>();int closed=0;
            for(int i=0;i<5400;i++)
            {
                int index=AlphenVisualPilotMotion.IdleFrameIndex(i*.001f);seen.Add(index);if(index==2)closed++;
            }
            Assert.That(seen,Is.EquivalentTo(new[]{0,1,2,3}));Assert.That(closed,Is.InRange(99,101));
            foreach(float time in new[]{.1f,1.8f,2.7f,4.85f,5f,5.25f})
                Assert.That(AlphenVisualPilotMotion.IdleFrameIndex(time+AlphenVisualPilotMotion.IdleCycleSeconds),
                    Is.EqualTo(AlphenVisualPilotMotion.IdleFrameIndex(time)));
            Assert.That(AlphenVisualPilotMotion.IdleFrameIndex(float.NaN),Is.Zero);
            Assert.That(AlphenVisualPilotMotion.IdleFrameIndex(float.PositiveInfinity),Is.Zero);
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));
        }

        [Test] public void AttackCannotShowContactBeforeReleaseAndKeepsBothRecoveryFrames()
        {
            var pilot=Pilot();var unit=Unit();
            foreach(Facing facing in Enum.GetValues(typeof(Facing)))
            {
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,100,true,false,100,.6f),Is.SameAs(pilot.Attack.Frames[0].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,1,true,true,0,.8f),Is.SameAs(pilot.Attack.Frames[1].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,1,true,true,.39f,.8f),Is.SameAs(pilot.Attack.Frames[1].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,1,true,true,.6f,.8f),Is.SameAs(pilot.Attack.Frames[2].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,.1f),Is.SameAs(pilot.Attack.Frames[0].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,.2f),Is.SameAs(pilot.Attack.Frames[1].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,.4f),Is.SameAs(pilot.Attack.Frames[2].Get(facing)));
                Assert.That(pilot.Sample(unit,AnimationKind.Attack,facing,.43f),Is.SameAs(pilot.Idle.Frames[0].Get(facing)));
            }
            Assert.That(pilot.Attack.ReleaseFrame,Is.EqualTo(1));
            pilot.Attack.ReleaseFrame=2;
            Assert.That(pilot.Sample(unit,AnimationKind.Attack,Facing.Front,1,true,true,.6f,.8f),Is.Null,"Malformed pilot timing must retain original combat art");
        }

        [Test] public void MissingDirectionalArtFallsBackForEntireClipWithoutBorrowingFrontArt()
        {
            var pilot=Pilot();var unit=Unit();pilot.Idle.Frames[1].Back=null;
            foreach(float time in new[]{0f,1.8f,2.7f,4.85f})
                Assert.That(pilot.Sample(unit,AnimationKind.Idle,Facing.Back,time),Is.Null);
            Assert.That(pilot.Sample(unit,AnimationKind.Idle,Facing.Front,1.8f),Is.SameAs(pilot.Idle.Frames[1].Front));
            pilot.Attack.Frames[2].Left=null;
            Assert.That(pilot.Sample(unit,AnimationKind.Attack,Facing.Left,0,true,false,0,1),Is.Null);
            Assert.That(pilot.Sample(unit,AnimationKind.Damage,Facing.Right,.1f),Is.SameAs(pilot.Damage.Frames[0].Right));
            Assert.That(pilot.Sample(unit,AnimationKind.Damage,Facing.Right,.3f),Is.SameAs(pilot.Idle.Frames[0].Right));
            pilot.Idle.Frames[0].Right=null;
            Assert.That(pilot.Sample(unit,AnimationKind.Damage,Facing.Right,.3f),Is.Null,"Missing recovery art must retain the established fallback");
            foreach(var action in new[]{AnimationKind.Walk,AnimationKind.Skill,AnimationKind.Cast,AnimationKind.Guard,AnimationKind.Ultimate,AnimationKind.Dead})
                Assert.That(pilot.Sample(unit,action,Facing.Front,0),Is.Null,action.ToString());
            Assert.That(pilot.Sample(Unit("cless"),AnimationKind.Idle,Facing.Front,0),Is.Null);
            unit.CurrentHP=0;Assert.That(pilot.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.Null);
        }

        [Test] public void SignatureWeaponNeedsItsOwnExactVariantAndNeverUsesTheBaseSword()
        {
            var pilot=Pilot();var unit=Unit();var weapon=Signature();unit.Equipment[0]=weapon;
            var original=unit.Data.Sprites.Front;
            Assert.That(EquipmentAppearance.Resolve(unit,original),Is.Not.SameAs(original));
            foreach(var action in new[]{AnimationKind.Idle,AnimationKind.Attack,AnimationKind.Damage})
                Assert.That(pilot.Sample(unit,action,Facing.Front,0),Is.Null);
            var wrong=Keep(ScriptableObject.CreateInstance<EquipmentAppearance>());
            wrong.CharacterId=weapon.Appearance.CharacterId;wrong.EquipmentId=weapon.Appearance.EquipmentId;
            pilot.EquipmentVariants=new[]{new AlphenVisualPilotMotion.EquipmentVariant{Appearance=wrong,Idle=Clip("wrong",4)}};
            Assert.That(pilot.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.Null,"Matching strings alone do not select unrelated art");
            var equipped=Clip("equipped",4);
            pilot.EquipmentVariants=new[]{new AlphenVisualPilotMotion.EquipmentVariant{Appearance=weapon.Appearance,Idle=equipped}};
            Assert.That(pilot.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.SameAs(equipped.Frames[0].Front));
            Assert.That(pilot.Sample(unit,AnimationKind.Attack,Facing.Front,0),Is.Null,"Missing equipment action does not use base art");
            unit.Equipment[0]=null;
            Assert.That(pilot.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.SameAs(pilot.Idle.Frames[0].Front));
        }

        [Test] public void PilotIdleKeepsFeetFixedClearsUvMotionAndRestoresFallbackAcrossCameras()
        {
            var pilot=Pilot();var unit=Unit();var motion=Motion(unit,out var renderer,out var camera);
            var block=new MaterialPropertyBlock();block.SetFloat("_Brightness",.71f);renderer.SetPropertyBlock(block);
            Step(motion);renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1));
            motion.Initialize(renderer,unit,camera,true);Field("visualPilotMotion").SetValue(motion,pilot);
            foreach(float yaw in new[]{-39f,51f,141f,231f})
            {
                camera.transform.rotation=Quaternion.Euler(57,yaw,0);motion.Set(AnimationKind.Idle);Field("started").SetValue(motion,Time.time);
                Step(motion);var facing=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                Assert.That(renderer.sprite,Is.SameAs(pilot.Idle.Frames[0].Get(facing)));
                Assert.That(motion.transform.localPosition,Is.EqualTo(new Vector3(0,.04f,0)));Assert.That(motion.transform.localScale,Is.EqualTo(Vector3.one));
                renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero));Assert.That(block.GetVector("_IdleFace"),Is.EqualTo(Vector4.zero));
                Assert.That(block.GetFloat("_Brightness"),Is.EqualTo(.71f));
                unit.Equipment[0]=Signature();Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(unit,unit.Data.Sprites.Get(facing))));
                renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1));
                unit.Equipment[0]=null;
            }
            motion.Initialize(renderer,unit,camera,false);Step(motion);
            Assert.That(Field("visualPilotMotion").GetValue(motion),Is.Null,"Other stages must not retain an earlier pilot");
            Assert.That(renderer.sprite,Is.SameAs(unit.Data.Sprites.Get(CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation))));
            var other=Unit("cless");motion.Initialize(renderer,other,camera,true);Step(motion);
            Assert.That(Field("visualPilotMotion").GetValue(motion),Is.Null,"Only Alphen is in the pilot");
            Assert.That(renderer.sprite,Is.SameAs(other.Data.Sprites.Get(CharacterMotion.ViewFacing(other.Facing,camera.transform.rotation))));
        }

        [Test] public void RuntimeReleaseDamageAndRecoveryKeepExistingTimingAndGameplayState()
        {
            var pilot=Pilot();var unit=Unit();var motion=Motion(unit,out var renderer,out var camera);
            motion.Initialize(renderer,unit,camera,true);Field("visualPilotMotion").SetValue(motion,pilot);
            var skill=unit.Data.BasicAttack;
            int hp=unit.CurrentHP,mp=unit.CurrentMP;var position=unit.Position;var facing=unit.Facing;
            var view=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
            motion.BeginSkill(skill,.6f,.8f);Field("started").SetValue(motion,Time.time-10);Step(motion);
            Assert.That(motion.Presenting,Is.True);Assert.That(renderer.sprite,Is.SameAs(pilot.Attack.Frames[0].Get(view)));
            motion.ReleaseSkill();float releaseAt=(float)Field("releasedAt").GetValue(motion);
            motion.ReleaseSkill(10);Assert.That((float)Field("releasedAt").GetValue(motion),Is.EqualTo(releaseAt));
            Step(motion);Assert.That(renderer.sprite,Is.SameAs(pilot.Attack.Frames[1].Get(view)));
            Field("releasedAt").SetValue(motion,Time.time-.65f);Step(motion);
            Assert.That(renderer.sprite,Is.SameAs(pilot.Attack.Frames[2].Get(view)));
            Field("releasedAt").SetValue(motion,Time.time-.81f);Step(motion);
            Assert.That(motion.Presenting,Is.False);Assert.That(motion.Action,Is.EqualTo(AnimationKind.Idle));
            Assert.That(renderer.sprite,Is.SameAs(pilot.Idle.Frames[0].Get(view)));
            motion.BeginSkill(skill,.6f,.8f);motion.Set(AnimationKind.Damage);Step(motion);
            Assert.That(motion.Presenting,Is.False);Assert.That(renderer.sprite,Is.SameAs(pilot.Damage.Frames[0].Get(view)));
            motion.ReleaseSkill();Assert.That(motion.Presenting,Is.False,"An interrupted action cannot restart via release");
            Field("started").SetValue(motion,Time.time-.35f);Step(motion);
            Assert.That(motion.Action,Is.EqualTo(AnimationKind.Damage),"The artwork must not change recoil or combat state");
            Assert.That(renderer.sprite,Is.SameAs(pilot.Idle.Frames[0].Get(view)),"Recovery must not flash the original idle drawing");
            Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            Assert.That(unit.Position,Is.EqualTo(position));Assert.That(unit.Facing,Is.EqualTo(facing));
        }
    }
}
