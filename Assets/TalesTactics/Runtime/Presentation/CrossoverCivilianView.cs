using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Civilian groups share the rescue objective; they are not targetable combat units.
    public sealed class CrossoverCivilianView:MonoBehaviour
    {
        BattleDirector battle;SpriteRenderer[] people;bool escort,phantoms;
        Material environment;MaterialPropertyBlock properties;GroundOverlay[] shadows;ProjectedUnitShadow[] projectedShadows;PixelBattlefield terrain;int theme;
        public static void Create(BattleDirector battle,Transform parent)
        {
            int stage=battle.Session.CampaignStage;
            bool personal=PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==1;
            int episode=CrossoverCampaign.IsMain(stage)?CrossoverCampaign.Episode(stage).Number:0;
            if(!personal&&!new[]{3,7,13,16,18}.Contains(episode))return;
            if(battle.Catalog.Civilians==null||battle.Catalog.Civilians.Length<4)return;
            var root=new GameObject("Civilian rescue groups");root.transform.SetParent(parent,false);
            var view=root.AddComponent<CrossoverCivilianView>();view.battle=battle;view.escort=episode==16;view.phantoms=episode==18;
            view.theme=BattleEnvironmentPalette.Theme(stage);view.terrain=parent.GetComponent<PixelBattlefield>();
            view.environment=new Material(Resources.Load<Shader>("TalesTactics/UnitTone")){name="Civilian environment tone"};
            view.properties=new MaterialPropertyBlock();view.shadows=new GroundOverlay[3];view.projectedShadows=new ProjectedUnitShadow[3];
            view.people=Enumerable.Range(0,3).Select(i=>{
                var person=new GameObject("Civilian group "+i);person.transform.SetParent(root.transform,false);
                var renderer=person.AddComponent<SpriteRenderer>();renderer.sharedMaterial=view.environment;
                person.transform.localScale=Vector3.one*.78f;
                person.transform.position=battle.Session.Grid[CrossoverBattle.Nodes[i]].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.04f;
                var shade=new GameObject("Civilian contact shadow "+i);shade.transform.SetParent(root.transform,false);
                var contact=shade.AddComponent<GroundOverlay>();contact.Initialize(view.environment,-3);contact.Shadow(BattleEnvironmentPalette.Shadow(view.theme));shade.transform.localScale=Vector3.one*.78f;view.shadows[i]=contact;
                var cast=new GameObject("Civilian projected shadow "+i);cast.transform.SetParent(root.transform,false);
                var projected=cast.AddComponent<ProjectedUnitShadow>();projected.Initialize(renderer,battle.Session.Grid,battle.Session.Rules.TileHeight,view.theme,view.terrain!=null?view.terrain.ProjectedShadowMaterial:null);view.projectedShadows[i]=projected;
                return renderer;
            }).ToArray();
        }
        void OnDestroy(){if(environment!=null)Destroy(environment);}
        void OnDisable(){if(projectedShadows!=null)foreach(var cast in projectedShadows)if(cast!=null)cast.UpdateProjection(cast.GroundAnchor,0,false);}
        void LateUpdate()
        {
            var session=battle?.Session;if(session?.Crossover==null)return;
            var lead=session.Units.First(u=>u.Team==Team.Player);var camera=battle.Board.BattleCamera;
            for(int i=0;i<people.Length;i++)
            {
                bool linked=(session.Crossover.State.Nodes&(1<<i))!=0;var person=people[i];
                person.gameObject.SetActive(!linked||escort);shadows[i].gameObject.SetActive(person.gameObject.activeSelf);
                if(!person.gameObject.activeSelf){projectedShadows[i].UpdateProjection(person.transform.position,0,false);continue;}
                Facing facing=escort&&linked?lead.Facing:Facing.Front;
                person.sprite=battle.Catalog.Civilians[i].Get(CharacterMotion.ViewFacing(facing,camera.transform.rotation));
                person.color=phantoms?new Color(.65f,.85f,1,.72f):Color.white;
                person.transform.rotation=camera.transform.rotation;
                var target=session.Grid[linked&&escort?lead.Position:CrossoverBattle.Nodes[i]].WorldPosition(session.Rules.TileHeight);
                if(linked&&escort)
                {
                    var direction=SkillResolver.FacingVector(lead.Facing);
                    target+=new Vector3(-direction.x,0,-direction.y)*(.55f+i*.3f)+camera.transform.right*((i%2==0?1:-1)*.22f);
                }
                else target+=camera.transform.right*.24f;
                person.transform.position=Vector3.MoveTowards(person.transform.position,target+Vector3.up*.04f,Time.deltaTime*6);
                var foot=person.transform.position-Vector3.up*.04f;shadows[i].transform.position=foot+Vector3.up*.018f;
                if(person.sprite!=null){person.GetPropertyBlock(properties);BattleEnvironmentPalette.ApplyUnit(properties,theme,foot,person.sprite.bounds,terrain!=null?terrain.GroundShadeAt(foot):1);person.SetPropertyBlock(properties);}
                projectedShadows[i].UpdateProjection(foot,0,true);
            }
        }
    }
}
