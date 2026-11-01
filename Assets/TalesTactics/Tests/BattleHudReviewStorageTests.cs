using System;
using System.IO;
using NUnit.Framework;
namespace TalesTactics.Tests
{
    public sealed class BattleHudReviewStorageTests
    {
        const string Id="0123456789abcdef0123456789abcdef";
        [Test] public void HudReviewStartsInItsExactIsolatedDirectory()
        {
            string root=Path.Combine(Path.GetTempPath(),"TalesTactics-HudReviewStorage"),data=Path.Combine(root,"Build","TalesTactics_Data"),user=Path.Combine(root,"User");
            Assert.That(ReviewStorageBootstrap.Resolve(new[]{"TalesTactics.exe","--battle-hud-redesign-review",Id},data,user),
                Is.EqualTo(Path.GetFullPath(Path.Combine(root,"Build","BattleHudRedesignReviews",Id,"Storage"))));
        }
        [Test] public void HudReviewRejectsBadIdsMixedModesAndUserStorageOverlap()
        {
            string root=Path.Combine(Path.GetTempPath(),"TalesTactics-HudReviewStorage"),data=Path.Combine(root,"Build","TalesTactics_Data"),user=Path.Combine(root,"User");
            foreach(string id in new[]{"../User",Guid.Empty.ToString("N"),"bad"})
                Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(new[]{"--battle-hud-redesign-review",id},data,user));
            foreach(string other in new[]{"--battle-hud-redesign-review","--environment-integration-review","--campaign-flow-review"})
                Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(new[]{"--battle-hud-redesign-review",Id,other,Id},data,user));
            Assert.Throws<InvalidOperationException>(()=>ReviewStorageBootstrap.Resolve(new[]{"--battle-hud-redesign-review",Id},Path.Combine(user,"Build","TalesTactics_Data"),user));
        }
    }
}
