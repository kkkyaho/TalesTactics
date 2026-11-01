using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        static void AssertEnvironmentAnchor(GroundedUnitVisual grounding,SpriteRenderer sprite)
        {
            var anchor=grounding.GroundAnchor;var root=grounding.transform;
            Assert.That(anchor.x,Is.EqualTo(sprite.transform.position.x).Within(.001f));
            Assert.That(anchor.y,Is.EqualTo(root.position.y).Within(.001f),"A lifted sprite must not lift its ground projection.");
            Assert.That(anchor.z,Is.EqualTo(sprite.transform.position.z).Within(.001f));
            var shadow=root.Find("Contact shadow");Assert.That(shadow,Is.Not.Null);
            Assert.That(Vector3.Distance(shadow.position,anchor+Vector3.up*.018f),Is.LessThan(.001f));
            Assert.That(Vector3.Dot(shadow.up,Vector3.up),Is.GreaterThan(.999f),"The ground shadow must not billboard with the actor.");
            var block=new MaterialPropertyBlock();sprite.GetPropertyBlock(block);var stored=block.GetVector("_GroundAnchor");
            Assert.That(Vector3.Distance(new Vector3(stored.x,stored.y,stored.z),anchor),Is.LessThan(.001f));Assert.That(stored.w,Is.EqualTo(1));
            Assert.That(block.GetVector("_SpriteSpan").y,Is.EqualTo(sprite.sprite.bounds.size.y).Within(.001f),"Lighting uses the selected native sprite span, not another direction's bounds.");
        }
        [UnityTest] public IEnumerator EnvironmentGroundingTracksRealMovementAndFourCameraDirections()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var unit=director.Session.Active;
            var motion=director.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==unit);var sprite=motion.GetComponent<SpriteRenderer>();
            var grounding=motion.GetComponentInParent<GroundedUnitVisual>();Assert.That(grounding,Is.Not.Null);
            int hp=unit.CurrentHP,mp=unit.CurrentMP,draws=director.Session.Resolver.RandomDraws;string campaign=JsonUtility.ToJson(director.Campaign);
            var path=director.Session.Grid.Reachable(unit,out _).Keys.Select(p=>director.Session.Grid.Path(unit,p)).Where(p=>p.Count>=3).OrderBy(p=>p.Count).ThenBy(p=>p.Last().x).ThenBy(p=>p.Last().y).First();
            var start=grounding.transform.position;var end=director.Session.Grid[path.Last()].WorldPosition(director.Catalog.Rules.TileHeight);bool observedIntermediate=false;int observations=0;
            director.StartCoroutine(director.MoveUnit(path.Last()));double until=Time.realtimeSinceStartupAsDouble+10;
            while(director.State is ActionExecutionState&&Time.realtimeSinceStartupAsDouble<until)
            {
                yield return new WaitForEndOfFrame();AssertEnvironmentAnchor(grounding,sprite);observations++;
                var position=grounding.transform.position;observedIntermediate|=Vector3.Distance(position,start)>.01f&&Vector3.Distance(position,end)>.01f;
                float nearest=float.PositiveInfinity;
                for(int i=1;i<path.Count;i++)
                {var a=director.Session.Grid[path[i-1]].WorldPosition(director.Catalog.Rules.TileHeight);var b=director.Session.Grid[path[i]].WorldPosition(director.Catalog.Rules.TileHeight);var d=b-a;nearest=Mathf.Min(nearest,Vector3.Distance(position,a+d*Mathf.Clamp01(Vector3.Dot(position-a,d)/d.sqrMagnitude)));}
                Assert.That(nearest,Is.LessThan(.002f),"The actual animated root must stay on the legal terrain path.");
            }
            Assert.That(director.State,Is.InstanceOf<CommandState>());Assert.That(observations,Is.GreaterThan(1));Assert.That(observedIntermediate,Is.True);
            Assert.That(unit.Position,Is.EqualTo(path.Last()));Assert.That(Vector3.Distance(grounding.transform.position,end),Is.LessThan(.001f));
            var facings=new HashSet<Facing>();
            for(int i=0;i<4;i++)
            {
                director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();AssertEnvironmentAnchor(grounding,sprite);
                var facing=CharacterMotion.ViewFacing(unit.Facing,director.Board.BattleCamera.transform.rotation);facings.Add(facing);
                Assert.That(sprite.sprite,Is.SameAs(EquipmentAppearance.Resolve(unit,unit.Data.Sprites.Get(facing))),"Ground lighting must preserve the actual directional idle sprite.");
            }
            Assert.That(facings.Count,Is.EqualTo(4));Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
        }
        [UnityTest] public IEnumerator EnvironmentLiftAndContactPreserveCompletedToneAndSharedMaterial()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var unit=director.Session.Active;var team=(TeamTurnScheduler)director.Session.Scheduler;
            var motion=director.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==unit);var sprite=motion.GetComponent<SpriteRenderer>();var grounding=motion.GetComponentInParent<GroundedUnitVisual>();
            var shadow=grounding.transform.Find("Contact shadow");var shadowRenderer=shadow.GetComponent<MeshRenderer>();var shared=sprite.sharedMaterial;var block=new MaterialPropertyBlock();
            team.Complete(unit);Assert.That(director.Session.Select(team.Preview(director.Session.Units).First()),Is.True);director.SetState(new CommandState(director));
            yield return new WaitForEndOfFrame();sprite.GetPropertyBlock(block);Assert.That(block.GetFloat("_Saturation"),Is.EqualTo(.38f));Assert.That(block.GetFloat("_Brightness"),Is.EqualTo(.84f));
            float restingScale=shadow.localScale.x;var originalPosition=sprite.transform.localPosition;int hp=unit.CurrentHP;var position=unit.Position;var facing=unit.Facing;int draws=director.Session.Resolver.RandomDraws;
            // Synthetic presentation offset isolates lift/recoil from gameplay mutation.
            motion.enabled=false;
            try
            {
                sprite.transform.localPosition=new Vector3(.28f,.65f,-.16f);yield return new WaitForEndOfFrame();AssertEnvironmentAnchor(grounding,sprite);
                Assert.That(shadow.localScale.x,Is.GreaterThan(restingScale));shadowRenderer.GetPropertyBlock(block);Assert.That(block.GetColor("_Color").a,Is.InRange(.45f,.99f));
                sprite.GetPropertyBlock(block);Assert.That(block.GetFloat("_Saturation"),Is.EqualTo(.38f));Assert.That(block.GetFloat("_Brightness"),Is.EqualTo(.84f));
                Assert.That(sprite.sharedMaterial,Is.SameAs(shared));Assert.That(shadowRenderer.sharedMaterial,Is.SameAs(shared));
                Assert.That(unit.Position,Is.EqualTo(position));Assert.That(unit.Facing,Is.EqualTo(facing));Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));
                unit.CurrentHP=0;yield return new WaitForEndOfFrame();Assert.That(shadow.gameObject.activeSelf,Is.False,"Dead units must not retain live contact shadows.");
                unit.CurrentHP=hp;sprite.transform.localPosition=originalPosition;yield return new WaitForEndOfFrame();Assert.That(shadow.gameObject.activeSelf,Is.True);AssertEnvironmentAnchor(grounding,sprite);
            }
            finally{unit.CurrentHP=hp;sprite.transform.localPosition=originalPosition;motion.enabled=true;}
        }
        [UnityTest] public IEnumerator EnvironmentPaletteMatchesActualCampaignTerrainAndSelectedNativeSprites()
        {
            director.Catalog.CrossoverReady=true;director.TrainingMode=false;director.PersistCampaign=_=>false;var sunlight=new HashSet<Color>();
            foreach(int episode in new[]{1,11,16,20})
            {
                director.Restart();yield return null;director.SelectedStage=CrossoverCampaign.FirstStage+episode-1;director.Campaign=new CampaignSave();
                for(int n=1;n<episode;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
                director.Deployment.Clear();director.Deployment.AddRange(Enumerable.Range(0,director.Catalog.Characters.Length).Where(director.CanDeploy).Take(6));
                director.BeginBattle();yield return new WaitForEndOfFrame();director.StopAllCoroutines();Assert.That(director.Session,Is.Not.Null);
                var terrain=director.Board.GetComponentInChildren<PixelBattlefield>();var tile=director.Board.GetComponentInChildren<TileView>();var groundMaterial=tile.GetComponent<Renderer>().sharedMaterial;
                var key=groundMaterial.GetColor("_KeyColor");var fill=groundMaterial.GetColor("_FillColor");sunlight.Add(key);
                Assert.That(terrain.Chapter,Is.EqualTo(CrossoverCampaign.TerrainTheme(director.SelectedStage)));
                foreach(var grounding in director.Board.GetComponentsInChildren<GroundedUnitVisual>())
                {
                    var sprite=grounding.GetComponentInChildren<CharacterMotion>().GetComponent<SpriteRenderer>();var block=new MaterialPropertyBlock();sprite.GetPropertyBlock(block);
                    Assert.That(grounding.Theme,Is.EqualTo(terrain.Chapter));Assert.That(block.GetColor("_KeyColor"),Is.EqualTo(key));Assert.That(block.GetColor("_FillColor"),Is.EqualTo(fill));
                    Assert.That(block.GetFloat("_EnvironmentLight"),Is.GreaterThan(0));Assert.That(block.GetFloat("_GroundShade"),Is.InRange(.66f,1));
                    Assert.That(sprite.sharedMaterial.shader.isSupported,Is.True);AssertEnvironmentAnchor(grounding,sprite);
                }
            }
            Assert.That(sunlight.Count,Is.GreaterThanOrEqualTo(3),"Forest, warm canyon and cool late maps must not silently share one environment theme.");
        }
        [UnityTest] public IEnumerator EnvironmentGroundShadeRejectsOtherElevationsAndDistantCasters()
        {
            var probe=new GameObject("Ground shade test");probe.transform.SetParent(director.transform);var terrain=probe.AddComponent<PixelBattlefield>();
            try
            {
                var field=typeof(PixelBattlefield).GetField("shadowCasters",BindingFlags.Instance|BindingFlags.NonPublic);Assert.That(field,Is.Not.Null);
                var casters=(List<Vector4>)field.GetValue(terrain);Assert.That(terrain.GroundShadeAt(Vector3.zero),Is.EqualTo(1));
                casters.Add(new Vector4(2,3,2,1));var foot=new Vector3(2.4f,1,3.3f);
                Assert.That(terrain.GroundShadeAt(foot),Is.InRange(.66f,.8f));
                Assert.That(terrain.GroundShadeAt(foot+Vector3.up),Is.EqualTo(1),"A prop on a lower ledge must not shade another elevation.");
                Assert.That(terrain.GroundShadeAt(foot+Vector3.right*20),Is.EqualTo(1).Within(.0001f));
                for(int i=0;i<40;i++)casters.Add(new Vector4(2,3,2,1));Assert.That(terrain.GroundShadeAt(foot),Is.EqualTo(.66f).Within(.0001f),"Dense props must not black out the unit.");
            }
            finally{Object.Destroy(probe);}
            yield return null;
        }
        [UnityTest] public IEnumerator EnvironmentRebuildReleasesShadowMeshesAndDoesNotClonePerUnitMaterials()
        {
            for(int repeat=0;repeat<3;repeat++)
            {
                director.BeginBattle();yield return new WaitForEndOfFrame();director.StopAllCoroutines();
                var units=director.Board.GetComponentsInChildren<GroundedUnitVisual>();Assert.That(units.Length,Is.EqualTo(director.Session.Units.Count));
                var sprites=units.Select(g=>g.GetComponentInChildren<CharacterMotion>().GetComponent<SpriteRenderer>()).ToArray();
                var unitMaterial=sprites.First().sharedMaterial;Assert.That(sprites.All(s=>s.sharedMaterial==unitMaterial),Is.True,"Environment lighting must use per-renderer blocks, not one material per actor.");
                var shadows=units.Select(g=>g.transform.Find("Contact shadow")).ToArray();var meshes=shadows.Select(t=>t.GetComponent<MeshFilter>().sharedMesh).ToArray();
                Assert.That(shadows.All(t=>t.GetComponent<MeshRenderer>().sharedMaterial==unitMaterial),Is.True);
                foreach(var mesh in meshes){Assert.That(mesh.colors.Any(c=>c.a==0),Is.True);Assert.That(mesh.colors.Any(c=>c.a>0),Is.True);Assert.That(mesh.vertices.All(v=>Mathf.Abs(v.y)<.00001f),Is.True,"Shadow geometry remains flat on the contact plane.");}
                var terrainMaterials=director.Board.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).Where(m=>m!=null&&(m.name=="Terrain foliage tone"||m.name=="Continuous chapter pavement")).Distinct().ToArray();Assert.That(terrainMaterials,Is.Not.Empty);
                director.Restart();yield return null;yield return null;
                Assert.That(units.All(g=>g==null),Is.True);Assert.That(meshes.All(m=>m==null),Is.True,"GroundOverlay must release its owned meshes.");
                Assert.That(unitMaterial==null,Is.True);Assert.That(terrainMaterials.All(m=>m==null),Is.True,"The prior board must release its owned environment materials.");
                Assert.That(director.Board.GetComponentsInChildren<GroundedUnitVisual>(),Is.Empty);
            }
        }
    }
}
