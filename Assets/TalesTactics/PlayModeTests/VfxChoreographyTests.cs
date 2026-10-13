using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator WeaponSweepEndpointsStayFiniteDuringPreparationAndImpact()
        {
            var root=new GameObject("Sweep regression");var caster=new GameObject("Sweep caster");
            var cameraObject=new GameObject("Sweep camera");var camera=cameraObject.AddComponent<Camera>();
            var pool=root.AddComponent<ImpactPool>();caster.transform.position=Vector3.left;
            try
            {
                foreach(var style in new[]{CombatVisualStyle.Sword,CombatVisualStyle.Claw,CombatVisualStyle.FlameSword})
                {
                    pool.Prepare(camera,caster.transform,Vector3.right,Element.Fire,style,1,false,false,.4f);
                    yield return new WaitForSeconds(.32f);AssertFinite();
                    pool.Play(camera,Vector3.left,Vector3.right,Element.Fire,style,CombatFeedback.Strike,1,false,false);
                    AssertFinite();yield return null;
                }
                void AssertFinite()
                {
                    var meshes=root.GetComponentsInChildren<MeshFilter>();Assert.That(meshes.Length,Is.GreaterThan(0));
                    foreach(var mesh in meshes)foreach(var v in mesh.sharedMesh.vertices)
                        Assert.That(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z),Is.False,"Invalid sweep vertex");
                }
            }
            finally{Object.Destroy(root);Object.Destroy(caster);Object.Destroy(cameraObject);}
        }
        [UnityTest] public IEnumerator FirePreparationCannotApplyDamageAndRestartClearsItsPool()
        {
            director.BeginBattle();yield return null;var actor=director.Session.Active;var target=director.Session.Units.First(u=>u.Team==Team.Enemy);
            var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=10,Power=0,IgnoreDefense=true});
            skill.Element=Element.Fire;skill.Animation=AnimationKind.Cast;skill.VisualStyle=CombatVisualStyle.Sacred;
            try
            {
                int hp=target.CurrentHP;var cell=actor.Position;var random=JsonUtility.ToJson(Random.state);float scale=Time.timeScale;
                director.Board.BeginSkill(actor,skill,target.Position);yield return new WaitForSeconds(BoardView.Windup(skill)*.75f);
                var prep=director.Board.GetComponentsInChildren<ImpactParticles>().Single(p=>p.Preparing);
                Assert.That(prep.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.vertexCount),Is.GreaterThan(100));
                Assert.That(target.CurrentHP,Is.EqualTo(hp));Assert.That(actor.Position,Is.EqualTo(cell));Assert.That(Time.timeScale,Is.EqualTo(scale));Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
                var before=director.Board.CaptureHealth();Assert.That(director.Session.Resolver.Execute(actor,skill,target.Position,out var error),Is.True,error);
                director.Board.ReleaseSkill(actor);director.Board.PresentImpact(actor,skill,target.Position,before,new[]{target});int after=target.CurrentHP;
                Assert.That(after,Is.LessThan(hp));yield return new WaitForSeconds(1.2f);Assert.That(target.CurrentHP,Is.EqualTo(after));
                director.Restart();yield return null;Assert.That(director.Board.GetComponentsInChildren<ImpactPool>().Length,Is.Zero);
            }
            finally{Object.Destroy(skill);}
        }
    }
}
