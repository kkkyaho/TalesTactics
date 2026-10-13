using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class CrossoverBattle
    {
        UnitRuntime[] Party=>battle.Units.Where(u=>u.Team==Team.Player).ToArray();
        int WesternCount=>(Party.Length+1)/2;
        void Reserve(UnitRuntime unit)
        {unit.CrossoverReserve=true;if(battle.Grid[unit.Position]?.Occupant==unit)battle.Grid[unit.Position].Occupant=null;}
        void PrepareFronts()
        {
            var party=Party;
            foreach(var u in party)if(battle.Grid[u.Position]?.Occupant==u)battle.Grid[u.Position].Occupant=null;
            for(int i=0;i<party.Length;i++)
            {
                bool east=i>=WesternCount;int index=east?i-WesternCount:i;
                var p=new Vector2Int((east?9:1)+index%2,1+index/2);
                if(east){Reserve(party[i]);party[i].Position=p;}else battle.Grid.Place(party[i],p);
            }
            var enemies=Enemies;battle.Grid.Place(Boss,new Vector2Int(3,11));
            battle.Grid.Place(enemies[3],new Vector2Int(3,8));
            foreach(int i in new[]{2,4,5,7})Reserve(enemies[i]);
            enemies[4].Position=new Vector2Int(10,11);
        }
        void ActivateEasternFront()
        {
            State.Segment=1;var party=Party;
            if(party.Length>1)
            {
                foreach(var u in party.Take(WesternCount))if(u!=battle.Active)Reserve(u);
                foreach(var u in party.Skip(WesternCount)){u.CrossoverReserve=false;if(u.Alive)battle.Grid.Place(u,u.Position);}
            }
            var enemies=Enemies;
            foreach(int i in new[]{0,1,3,6,8})Reserve(enemies[i]);
            foreach(int i in new[]{2,4,5,7})
            {
                var u=enemies[i];u.CrossoverReserve=false;
                if(i==4){u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;}
                if(u.Alive)battle.Grid.Place(u,u.Position);
            }
        }
        void CompleteWesternTurn(UnitRuntime unit)
        {if(episode==11&&State.Segment==1&&Party.Length>1&&Party.Take(WesternCount).Contains(unit))Reserve(unit);}
        bool FrontDefeated=>State.Segment==0?!Party.Take(WesternCount).Any(u=>u.Alive):!(Party.Length>1?Party.Skip(WesternCount):Party).Any(u=>u.Alive);
    }
}
