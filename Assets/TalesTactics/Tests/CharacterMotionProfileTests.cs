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
    public sealed class CharacterMotionProfileTests
    {
        static readonly string[] Heroes={"cless","mint","farah","tear","jade","natalia","velvet","alphen","shionne","kisara"};
        readonly List<Object> temporary=new List<Object>();
        BattleCatalog Catalog=>AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
        static FieldInfo Field(string name)=>typeof(CharacterMotion).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        static void Step(CharacterMotion motion)=>typeof(CharacterMotion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motion,null);
        T Keep<T>(T value) where T:Object {temporary.Add(value);return value;}
        UnitRuntime Unit(string id)=>new UnitRuntime(Catalog.Characters.Single(c=>c.Id==id),Team.Player,Catalog.Rules);
        Sprite Frame(string name)
        {
            var frame=Keep(Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),new Vector2(.5f,0),2));
            frame.name=name;return frame;
        }
        DirectionalSpriteClip Clip(string prefix,int count)
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[count],ReleaseFrame=1};
            for(int i=0;i<count;i++)clip.Frames[i]=new DirectionalSprites{
                Front=Frame(prefix+"-front-"+i),Back=Frame(prefix+"-back-"+i),
                Left=Frame(prefix+"-left-"+i),Right=Frame(prefix+"-right-"+i)};
            return clip;
        }
        CharacterMotionProfile Profile(string id)
        {
            var profile=Keep(ScriptableObject.CreateInstance<CharacterMotionProfile>());
            profile.CharacterId=id;profile.Idle=Clip(id+"-idle",4);profile.Attack=Clip(id+"-attack",3);profile.Damage=Clip(id+"-damage",1);
            return profile;
        }
        EquipmentData Signature(string id)=>Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==id&&e.Slot==EquipmentSlot.Weapon&&e.Appearance!=null);
        CharacterMotion Actor(out SpriteRenderer renderer,out Camera camera)
        {
            var actor=Keep(new GameObject("Roster authored motion test"));
            renderer=actor.AddComponent<SpriteRenderer>();var motion=actor.AddComponent<CharacterMotion>();
            camera=Keep(new GameObject("Roster authored motion camera")).AddComponent<Camera>();camera.enabled=false;
            return motion;
        }
        [TearDown] public void Dispose()
        {
            for(int i=temporary.Count-1;i>=0;i--)if(temporary[i]!=null)Object.DestroyImmediate(temporary[i]);
            temporary.Clear();
        }

        [Test] public void RegistryOnlyResolvesThePlayableRosterAndReusesTheOriginalAlphenAsset()
        {
            var paths=new HashSet<string>();
            foreach(string id in Heroes)
            {
                string path=CharacterMotionRegistry.ResourcePathFor(id);
                Assert.That(path,Is.Not.Null,id);Assert.That(paths.Add(path),Is.True,id);
                Assert.That(path,Is.EqualTo(id=="alphen"?AlphenVisualPilotMotion.ResourcePath:CharacterMotionRegistry.ResourceRoot+id));
            }
            foreach(string id in new[]{null,"","sentinel","dhaos","Alphen","../alphen","cless/../mint"})
            {
                Assert.That(CharacterMotionRegistry.ResourcePathFor(id),Is.Null,id);
                Assert.That(CharacterMotionRegistry.Load(id),Is.Null,id);
            }
            var original=Resources.Load<AlphenVisualPilotMotion>(AlphenVisualPilotMotion.ResourcePath);
            Assert.That(original,Is.Not.Null);Assert.That(original.OwnerId,Is.EqualTo("alphen"));
            Assert.That(CharacterMotionRegistry.Load("alphen"),Is.SameAs(original),"The original resource and its sprite references are retained");
            var legacy=Keep(ScriptableObject.CreateInstance<AlphenVisualPilotMotion>());legacy.CharacterId="cless";
            Assert.That(legacy.OwnerId,Is.EqualTo("alphen"),"Legacy identity cannot be changed by a newly inherited serialized field");
        }

        [Test] public void EveryCharacterKeepsIdleReleaseAndRecoveryBoundariesWithoutChangingGameplay()
        {
            string random=JsonUtility.ToJson(UnityEngine.Random.state);float speed=Time.timeScale;
            foreach(string id in Heroes)
            {
                var profile=Profile(id);var unit=Unit(id);unit.Position=new Vector2Int(2,3);
                string before=JsonUtility.ToJson(unit),content=EditorJsonUtility.ToJson(unit.Data);
                foreach(Facing view in Enum.GetValues(typeof(Facing)))
                {
                    foreach(var sample in new[]{(time:0f,index:0),(time:1.8f,index:1),(time:2.7f,index:3),(time:4.85f,index:2),(time:5f,index:3)})
                        Assert.That(profile.Sample(unit,AnimationKind.Idle,view,sample.time),Is.SameAs(profile.Idle.Frames[sample.index].Get(view)),id);
                    Assert.That(profile.Sample(unit,AnimationKind.Attack,view,100,true,false,100,.6f),Is.SameAs(profile.Attack.Frames[0].Get(view)),id+" no early contact");
                    Assert.That(profile.Sample(unit,AnimationKind.Attack,view,0,true,true,0,.8f),Is.SameAs(profile.Attack.Frames[1].Get(view)),id);
                    Assert.That(profile.Sample(unit,AnimationKind.Attack,view,0,true,true,.6f,.8f),Is.SameAs(profile.Attack.Frames[2].Get(view)),id);
                    Assert.That(profile.Sample(unit,AnimationKind.Attack,view,.43f),Is.SameAs(profile.Idle.Frames[0].Get(view)),id);
                    Assert.That(profile.Sample(unit,AnimationKind.Damage,view,.1f),Is.SameAs(profile.Damage.Frames[0].Get(view)),id);
                    Assert.That(profile.Sample(unit,AnimationKind.Damage,view,.35f),Is.SameAs(profile.Idle.Frames[0].Get(view)),id+" no old art in recoil");
                }
                Assert.That(JsonUtility.ToJson(unit),Is.EqualTo(before),id+" gameplay state");
                Assert.That(EditorJsonUtility.ToJson(unit.Data),Is.EqualTo(content),id+" content");
            }
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));Assert.That(Time.timeScale,Is.EqualTo(speed));
        }

        [Test] public void MissingArtWrongOwnerAndUnsupportedActionsKeepTheEstablishedFallback()
        {
            foreach(string id in Heroes)
            {
                var profile=Profile(id);var unit=Unit(id);
                profile.Idle.Frames[1].Back=null;
                foreach(float t in new[]{0f,1.8f,2.7f,4.85f})
                    Assert.That(profile.Sample(unit,AnimationKind.Idle,Facing.Back,t),Is.Null,id+" incomplete direction");
                Assert.That(profile.Sample(unit,AnimationKind.Idle,Facing.Front,1.8f),Is.SameAs(profile.Idle.Frames[1].Front),id);
                profile.Attack.ReleaseFrame=2;
                Assert.That(profile.Sample(unit,AnimationKind.Attack,Facing.Front,0,true,false,0,.6f),Is.Null,id+" malformed phase split");
                profile.Damage.Frames[0].Left=null;
                Assert.That(profile.Sample(unit,AnimationKind.Damage,Facing.Left,0),Is.Null,id+" missing hit art");
                foreach(var kind in new[]{AnimationKind.Walk,AnimationKind.Skill,AnimationKind.Cast,AnimationKind.Guard,AnimationKind.Ultimate,AnimationKind.Dead})
                    Assert.That(profile.Sample(unit,kind,Facing.Front,0),Is.Null,id+" "+kind);
                foreach(float t in new[]{-.1f,float.NaN,float.PositiveInfinity})
                    Assert.That(profile.Sample(unit,AnimationKind.Damage,Facing.Front,t),Is.Null,id+" invalid time");
                Assert.That(profile.Sample(Unit(id=="alphen"?"cless":"alphen"),AnimationKind.Idle,Facing.Front,0),Is.Null,id+" wrong owner");
                unit.CurrentHP=0;Assert.That(profile.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.Null,id+" dead owner");
            }
        }

        [Test] public void EverySignatureWeaponRequiresItsExactAuthoredVariantForEachAction()
        {
            foreach(string id in Heroes)
            {
                var profile=Profile(id);var unit=Unit(id);var weapon=Signature(id);unit.Equipment[0]=weapon;
                Assert.That(EquipmentAppearance.Resolve(unit,unit.Data.Sprites.Front),Is.Not.SameAs(unit.Data.Sprites.Front),id+" established signature art");
                foreach(var kind in new[]{AnimationKind.Idle,AnimationKind.Attack,AnimationKind.Damage})
                    Assert.That(profile.Sample(unit,kind,Facing.Front,0),Is.Null,id+" base art must not hide signature weapon");
                var wrong=Keep(ScriptableObject.CreateInstance<EquipmentAppearance>());
                wrong.CharacterId=id;wrong.EquipmentId=weapon.Id;
                profile.EquipmentVariants=new[]{new CharacterMotionProfile.EquipmentVariant{Appearance=wrong,Idle=Clip("wrong",4)}};
                Assert.That(profile.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.Null,id+" strings do not authorize unrelated art");
                var equipped=Clip("equipped-"+id,4);
                profile.EquipmentVariants=new[]{new CharacterMotionProfile.EquipmentVariant{Appearance=weapon.Appearance,Idle=equipped}};
                Assert.That(profile.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.SameAs(equipped.Frames[0].Front),id);
                Assert.That(profile.Sample(unit,AnimationKind.Attack,Facing.Front,0),Is.Null,id+" missing variant action");
                Assert.That(profile.Sample(unit,AnimationKind.Damage,Facing.Front,0),Is.Null,id+" missing variant action");
                unit.Equipment[0]=null;
                Assert.That(profile.Sample(unit,AnimationKind.Idle,Facing.Front,0),Is.SameAs(profile.Idle.Frames[0].Front),id+" unequipped");
            }
        }

        [Test] public void ProductionProfilesContain320DistinctDirectionalPosesWithHeadAnchors()
        {
            var all=new HashSet<Sprite>();
            foreach(string id in Heroes)
            {
                var profile=CharacterMotionRegistry.Load(id);Assert.That(profile,Is.Not.Null,id+" production resource");
                Assert.That(profile.OwnerId,Is.EqualTo(id));
                Assert.That(profile.Idle.Frames,Has.Length.EqualTo(4),id);
                Assert.That(profile.Attack.Frames,Has.Length.EqualTo(3),id);
                Assert.That(profile.Damage.Frames,Has.Length.EqualTo(1),id);
                Assert.That(profile.Attack.ReleaseFrame,Is.EqualTo(1),id);
                string root=id=="alphen"?"Assets/TalesTactics/Art/VisualPilot/Alphen/":"Assets/TalesTactics/Art/RosterMotion20261125/"+id+"/";
                var unit=Unit(id);
                foreach(var clip in new[]{profile.Idle,profile.Attack,profile.Damage})
                    foreach(var frame in clip.Frames)
                        foreach(Facing view in Enum.GetValues(typeof(Facing)))
                        {
                            var sprite=frame.Get(view);Assert.That(sprite,Is.Not.Null,id+" "+view);
                            Assert.That(all.Add(sprite),Is.True,id+" unique directional pose");
                            Assert.That(AssetDatabase.GetAssetPath(sprite),Does.StartWith(root),id);
                            Assert.That(sprite.pixelsPerUnit,Is.GreaterThan(0),id);
                            Assert.That(UnitHeadAnchors.TryGetLocal(sprite,out var head),Is.True,id+" authored head anchor "+sprite.name);
                            Assert.That(float.IsNaN(head.x)||float.IsNaN(head.y)||float.IsInfinity(head.x)||float.IsInfinity(head.y),Is.False,id);
                        }
                foreach(Facing view in Enum.GetValues(typeof(Facing)))
                    Assert.That(profile.Sample(unit,AnimationKind.Idle,view,4.85f),Is.SameAs(profile.Idle.Frames[2].Get(view)),id+" production blink");
            }
            Assert.That(all,Has.Count.EqualTo(320));
        }

        [Test] public void RosterInitializationKeepsCameraFeetAndUvStableAndClearsWhenReused()
        {
            var motion=Actor(out var renderer,out var camera);var block=new MaterialPropertyBlock();
            foreach(string id in Heroes)
            {
                var unit=Unit(id);
                motion.Initialize(renderer,unit,camera);Step(motion);
                renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1),id+" legacy fallback");
                block.SetFloat("_Brightness",.71f);renderer.SetPropertyBlock(block);
                motion.InitializeRoster(renderer,unit,camera);
                var profile=motion.AuthoredProfile;Assert.That(profile,Is.SameAs(CharacterMotionRegistry.Load(id)));Assert.That(profile,Is.Not.Null,id);
                foreach(float yaw in new[]{-39f,51f,141f,231f})
                {
                    camera.transform.rotation=Quaternion.Euler(57,yaw,0);motion.Set(AnimationKind.Idle);Field("started").SetValue(motion,Time.time);Step(motion);
                    var view=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                    Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[0].Get(view)),id);
                    Assert.That(motion.transform.localPosition,Is.EqualTo(new Vector3(0,.04f,0)),id+" feet");
                    Assert.That(motion.transform.localScale,Is.EqualTo(Vector3.one),id+" proportions");
                    renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero),id);
                    Assert.That(block.GetVector("_IdleFace"),Is.EqualTo(Vector4.zero),id);Assert.That(block.GetFloat("_Brightness"),Is.EqualTo(.71f),id);
                    unit.Equipment[0]=Signature(id);Step(motion);
                    Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(unit,unit.Data.Sprites.Get(view))),id+" signature art");
                    renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1),id+" signature idle");
                    unit.Equipment[0]=null;
                }
                motion.Initialize(renderer,unit,camera,false);Step(motion);
                Assert.That(motion.AuthoredProfile,Is.Null,id+" explicit legacy opt out");
                Assert.That(renderer.sprite,Is.SameAs(unit.Data.Sprites.Get(CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation))),id);
            }
            var other=Keep(ScriptableObject.CreateInstance<CharacterData>());
            other.Id="not-in-roster";other.BaseStats=Unit("cless").Data.BaseStats;other.Sprites=Unit("cless").Data.Sprites;
            motion.InitializeRoster(renderer,new UnitRuntime(other,Team.Enemy,Catalog.Rules),camera);Step(motion);
            Assert.That(motion.AuthoredProfile,Is.Null,"Unregistered actors never inherit the preceding hero profile");
        }

        [Test] public void RosterIdleOffsetsAreStableDispersedAndNeverChangeLegacyOrGameplayTiming()
        {
            string random=JsonUtility.ToJson(UnityEngine.Random.state);float speed=Time.timeScale;
            var motion=Actor(out var renderer,out var camera);camera.transform.rotation=Quaternion.identity;
            var offsets=new Dictionary<string,float>();
            foreach(string id in Heroes)
            {
                var unit=Unit(id);string before=JsonUtility.ToJson(unit);
                // Reused actors must restart at rest, irrespective of their previous idle age.
                Field("started").SetValue(motion,Time.time-50);
                motion.InitializeRoster(renderer,unit,camera);Step(motion);
                float offset=motion.AuthoredIdleOffsetSeconds;offsets.Add(id,offset);
                Assert.That(offset,Is.InRange(0f,1.2f),id);
                var profile=motion.AuthoredProfile;Assert.That(profile,Is.Not.Null,id);
                var view=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[0].Get(view)),id+" starts resting");
                // Probe the middle of this actor's blink, adjusting only its idle clock.
                Field("started").SetValue(motion,Time.time-(4.85f-offset));Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[2].Get(view)),id+" individual blink");
                motion.Set(AnimationKind.Damage);Field("started").SetValue(motion,Time.time-.1f);Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(profile.Damage.Frames[0].Get(view)),id+" hit has no idle offset");
                Field("started").SetValue(motion,Time.time-.35f);Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[0].Get(view)),id+" hit recovery rests");
                motion.Set(AnimationKind.Attack);Field("started").SetValue(motion,Time.time-.1f);Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(profile.Attack.Frames[0].Get(view)),id+" preparation has no idle offset");
                motion.Initialize(renderer,unit,camera,true);
                Assert.That(motion.AuthoredIdleOffsetSeconds,Is.Zero,id+" legacy path");
                if(id=="alphen")
                {
                    Field("started").SetValue(motion,Time.time-4.85f);Step(motion);
                    Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[2].Get(view)),"legacy pilot clock retained");
                }
                motion.InitializeRoster(renderer,unit,camera);
                Assert.That(motion.AuthoredIdleOffsetSeconds,Is.EqualTo(offset),id+" stable after reuse");
                Assert.That(JsonUtility.ToJson(unit),Is.EqualTo(before),id+" no gameplay mutation");
            }
            Assert.That(offsets.Values.Distinct().Count(),Is.EqualTo(Heroes.Length),"Each hero has a separate blink phase");
            Assert.That(offsets.Values.Max()-offsets.Values.Min(),Is.GreaterThan(.6f),"Roster phases visibly spread");
            foreach(float offset in offsets.Values)
            {
                float elapsed=4.85f-offset;
                int blinking=offsets.Values.Count(other=>CharacterMotionProfile.IdleFrameIndex(elapsed+other)==2);
                Assert.That(blinking,Is.LessThan(Heroes.Length),"A shared idle start never makes the whole roster blink together");
            }
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));
            Assert.That(Time.timeScale,Is.EqualTo(speed));
        }

        [Test] public void RuntimeBasicAttackUsesNewArtWhileNamedSkillsKeepExistingClipsAndTiming()
        {
            var motion=Actor(out var renderer,out var camera);camera.transform.rotation=Quaternion.identity;
            foreach(string id in Heroes)
            {
                var unit=Unit(id);unit.Position=new Vector2Int(3,4);
                motion.InitializeRoster(renderer,unit,camera);var profile=motion.AuthoredProfile;Assert.That(profile,Is.Not.Null,id);
                string before=JsonUtility.ToJson(unit);var view=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                motion.BeginSkill(unit.Data.BasicAttack,.6f,.8f);Field("started").SetValue(motion,Time.time-10);Step(motion);
                Assert.That(motion.Presenting,Is.True,id);Assert.That(renderer.sprite,Is.SameAs(profile.Attack.Frames[0].Get(view)),id+" prepare");
                motion.ReleaseSkill();float release=(float)Field("releasedAt").GetValue(motion);
                motion.ReleaseSkill(10);Assert.That((float)Field("releasedAt").GetValue(motion),Is.EqualTo(release),id+" release only once");Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(profile.Attack.Frames[1].Get(view)),id+" contact");
                Field("releasedAt").SetValue(motion,Time.time-.65f);Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(profile.Attack.Frames[2].Get(view)),id+" recovery");
                Field("releasedAt").SetValue(motion,Time.time-.81f);Step(motion);
                Assert.That(motion.Presenting,Is.False,id);Assert.That(motion.Action,Is.EqualTo(AnimationKind.Idle),id);
                Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[0].Get(view)),id+" completed attack");
                var named=Keep(ScriptableObject.CreateInstance<SkillData>());named.Id=id+".authored-regression";named.Animation=AnimationKind.Attack;
                motion.BeginSkill(named,.6f,.8f);Field("started").SetValue(motion,Time.time-10);Step(motion);
                Assert.That(renderer.sprite,Is.SameAs(unit.Data.Poses.AttackMotion.SamplePhase(view,10,.6f,false,SpritePhasePacing.Action)),id+" named skill retains its original authored clip");
                motion.Set(AnimationKind.Damage);Step(motion);
                Assert.That(motion.Presenting,Is.False,id);Assert.That(renderer.sprite,Is.SameAs(profile.Damage.Frames[0].Get(view)),id);
                motion.ReleaseSkill();Assert.That(motion.Presenting,Is.False,id+" interrupted action cannot restart");
                Field("started").SetValue(motion,Time.time-.35f);Step(motion);
                Assert.That(motion.Action,Is.EqualTo(AnimationKind.Damage),id+" existing recoil state");
                Assert.That(renderer.sprite,Is.SameAs(profile.Idle.Frames[0].Get(view)),id+" no old art flash");
                Assert.That(JsonUtility.ToJson(unit),Is.EqualTo(before),id+" gameplay state");
            }
        }
    }
}
