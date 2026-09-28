using System;
using System.Linq;
using NUnit.Framework;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void CampaignEnemyRosterCoversMonstersAndReservesDhaosForFinale()
        {
            var ids=Enumerable.Range(0,CampaignStages.Count).SelectMany(s=>Enumerable.Range(0,4).Select(i=>CampaignEnemies.Id(s,i))).ToArray();
            Assert.That(ids.Distinct().Count(),Is.EqualTo(10));
            Assert.That(ids.Take(20),Does.Not.Contain("dhaos"));
            Assert.That(ids.Count(id=>id=="dhaos"),Is.EqualTo(1));
            Assert.That(CampaignEnemies.Id(5,0),Is.EqualTo("dhaos"));
            var catalog=New<BattleCatalog>();catalog.Enemy=data;
            Assert.That(CampaignEnemies.Resolve(catalog,0,0),Is.SameAs(data));
            catalog.Enemies=ids.Distinct().Select(id=>{var c=New<CharacterData>();c.Id=id;return c;}).ToArray();
            Assert.That(CampaignEnemies.Resolve(catalog,-1,0),Is.SameAs(data));
            for(int s=0;s<CampaignStages.Count;s++)for(int i=0;i<4;i++)
                Assert.That(CampaignEnemies.Resolve(catalog,s,i).Id,Is.EqualTo(CampaignEnemies.Id(s,i)));
            catalog.Enemies=catalog.Enemies.Where(c=>c.Id!="dhaos").ToArray();
            Assert.Throws<InvalidOperationException>(()=>CampaignEnemies.Resolve(catalog,5,0));
        }
    }
}
