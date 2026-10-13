using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        GroundOverlay rescueNodes,linkedNodes,crossoverExit,contractSeal,hazardTiles,collapseWarnings,summonCircles;
        int crossoverMarkerHash=int.MinValue;
        void CrossoverMarkers()
        {
            var b=battle.Session;if(root==null||b?.Crossover==null)return;
            var state=b.Crossover.State;
            int hash=state.Nodes;
            foreach(var h in state.Hazards)hash=unchecked(hash*31+h.X*173+h.Y*13+(int)h.Kind+(h.Triggered?1000:0));
            foreach(var u in b.Units.Where(u=>u.Summoned))hash=unchecked(hash*31+(u.Alive?1:0));
            if(hash==crossoverMarkerHash)return;crossoverMarkerHash=hash;
            var pending=new HashSet<Vector2Int>();var linked=new HashSet<Vector2Int>();
            if(b.Crossover.UsesNodes)for(int i=0;i<CrossoverBattle.Nodes.Length;i++)((state.Nodes&(1<<i))==0?pending:linked).Add(CrossoverBattle.Nodes[i]);
            Region(ref rescueNodes,"Rescue signals - unlinked",pending,new Color(.9f,.74f,.23f,.25f),new Color(1,.8f,.3f,.95f));
            Region(ref linkedNodes,"Rescue signals - connected",linked,new Color(.2f,.8f,.7f,.27f),new Color(.25f,1,.8f,.95f));
            Region(ref crossoverExit,"Return exit",b.Crossover.UsesExit?new HashSet<Vector2Int>{CrossoverBattle.Exit}:new HashSet<Vector2Int>(),new Color(.4f,.65f,1,.3f),new Color(.55f,.8f,1,.95f));
            Region(ref contractSeal,"Origin contract seal",b.Crossover.UsesContract?new HashSet<Vector2Int>{CrossoverBattle.ContractSeal}:new HashSet<Vector2Int>(),new Color(.6f,.5f,1,.35f),new Color(.8f,.7f,1,.95f));
            Region(ref hazardTiles,"Persistent boss hazards",new HashSet<Vector2Int>(state.Hazards.Where(h=>h.Kind!=BossMechanic.Collapse||h.Triggered).Select(h=>h.Position)),new Color(.8f,.2f,.35f,.3f),new Color(1,.35f,.45f,.95f));
            Region(ref collapseWarnings,"Delayed collapse warning",new HashSet<Vector2Int>(state.Hazards.Where(h=>h.Kind==BossMechanic.Collapse&&!h.Triggered).Select(h=>h.Position)),new Color(.95f,.7f,.2f,.18f),new Color(1,.8f,.3f,.9f));
            var enemies=b.Units.Where(u=>u.Team==Team.Enemy).ToArray();var circles=new HashSet<Vector2Int>();
            for(int i=0;i<enemies.Length;i++)if(enemies[i].Summoned&&!enemies[i].Alive)circles.Add(CrossoverCampaign.EnemySpawn(i));
            Region(ref summonCircles,"Summon circles - occupy to interrupt",circles,new Color(.6f,.3f,.9f,.2f),new Color(.8f,.5f,1,.9f));
        }
        void ResetCrossoverMarkers(){rescueNodes=linkedNodes=crossoverExit=contractSeal=hazardTiles=collapseWarnings=summonCircles=null;crossoverMarkerHash=int.MinValue;}
    }
}
