using System;
using System.Linq;
namespace TalesTactics
{
    // IDs are independent of asset order. Legacy/training catalogs retain the sentinel fallback.
    public static class CampaignEnemies
    {
        static readonly string[][] formations = {
            new[]{"polwigle","wolf","slime","sentinel"},
            new[]{"golem","sentinel","mage","slime"},
            new[]{"eggbear","wolf","archer","sentinel"},
            new[]{"mage","golem","archer","sentinel"},
            new[]{"penguinist","polwigle","eggbear","archer"},
            new[]{"dhaos","golem","mage","sentinel"}
        };
        public static string Id(int stage,int slot)
        {
            if(stage<0||stage>=formations.Length)throw new ArgumentOutOfRangeException(nameof(stage));
            if(slot<0||slot>=4)throw new ArgumentOutOfRangeException(nameof(slot));
            return formations[stage][slot];
        }
        public static CharacterData Resolve(BattleCatalog catalog,int stage,int slot)
        {
            if(stage<0||catalog.Enemies==null||catalog.Enemies.Length==0)return catalog.Enemy;
            string id=Id(stage,slot);
            return catalog.Enemies.FirstOrDefault(e=>e!=null&&e.Id==id)
                ??throw new InvalidOperationException("Missing campaign enemy: "+id);
        }
    }
}
