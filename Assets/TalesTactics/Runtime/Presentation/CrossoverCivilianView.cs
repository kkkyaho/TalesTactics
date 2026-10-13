using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Civilian groups share the rescue objective; they are not targetable combat units.
    public sealed class CrossoverCivilianView:MonoBehaviour
    {
        BattleDirector battle;SpriteRenderer[] people;bool escort,phantoms;
        public static void Create(BattleDirector battle,Transform parent)
        {
            int stage=battle.Session.CampaignStage;
            bool personal=PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)==1;
            int episode=CrossoverCampaign.IsMain(stage)?CrossoverCampaign.Episode(stage).Number:0;
            if(!personal&&!new[]{3,7,13,16,18}.Contains(episode))return;
            if(battle.Catalog.Civilians==null||battle.Catalog.Civilians.Length<4)return;
            var root=new GameObject("Civilian rescue groups");root.transform.SetParent(parent,false);
            var view=root.AddComponent<CrossoverCivilianView>();view.battle=battle;view.escort=episode==16;view.phantoms=episode==18;
            view.people=Enumerable.Range(0,3).Select(i=>{
                var person=new GameObject("Civilian group "+i);person.transform.SetParent(root.transform,false);
                var renderer=person.AddComponent<SpriteRenderer>();renderer.sharedMaterial=battle.Board.SpriteMaterial;
                person.transform.localScale=Vector3.one*.78f;
                person.transform.position=battle.Session.Grid[CrossoverBattle.Nodes[i]].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.04f;
                return renderer;
            }).ToArray();
        }
        void LateUpdate()
        {
            var session=battle?.Session;if(session?.Crossover==null)return;
            var lead=session.Units.First(u=>u.Team==Team.Player);var camera=battle.Board.BattleCamera;
            for(int i=0;i<people.Length;i++)
            {
                bool linked=(session.Crossover.State.Nodes&(1<<i))!=0;var person=people[i];
                person.gameObject.SetActive(!linked||escort);
                if(!person.gameObject.activeSelf)continue;
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
            }
        }
    }
}
