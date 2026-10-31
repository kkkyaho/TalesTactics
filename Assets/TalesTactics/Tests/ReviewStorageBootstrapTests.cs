using System;
using System.IO;
using NUnit.Framework;

namespace TalesTactics.Tests
{
    public sealed class ReviewStorageBootstrapTests
    {
        const string Id="0123456789abcdef0123456789abcdef";
        string Root=>Path.Combine(Path.GetTempPath(),"TalesTactics-review-bootstrap-tests");
        string Data=>Path.Combine(Root,"Build","TalesTactics_Data");
        string User=>Path.Combine(Root,"User");
        [TestCase("vfx")][TestCase("motion")][TestCase("performance")]
        public void QualityModeStartsInTheSamePerRunIsolatedSaveAsItsRunner(string mode)
        {
            var args=new[]{"TalesTactics.exe","--quality-presentation-review",Id,mode,"-logFile",Path.Combine(Root,"external.log")};
            Assert.That(ReviewStorageBootstrap.Resolve(args,Data,User),Is.EqualTo(Path.GetFullPath(Path.Combine(Root,"Build","QualityPresentationReviews",Id,"isolated-save"))));
        }
        [Test] public void CampaignAndRosterPathsStaySeparateAndRosterResolutionIsStable()
        {
            var flow=new[]{"TalesTactics.exe","--campaign-flow-review",Id};var roster=new[]{"TalesTactics.exe","--skill-roster-vfx-review",Id,"--roster-phase","after","--roster-no-contact-captures"};
            string flowRoot=ReviewStorageBootstrap.Resolve(flow,Data,User),rosterRoot=ReviewStorageBootstrap.Resolve(roster,Data,User);
            Assert.That(flowRoot,Is.EqualTo(Path.GetFullPath(Path.Combine(Root,"Build","CampaignFlowReviews",Id,"Storage"))));
            Assert.That(rosterRoot,Is.EqualTo(Path.GetFullPath(Path.Combine(Root,"Build","SkillRosterVfx",Id,"isolated-save",Id))));
            Assert.That(rosterRoot,Is.Not.EqualTo(flowRoot));Assert.That(ReviewStorageBootstrap.Resolve(roster,Data,User),Is.EqualTo(rosterRoot));
        }
        [Test] public void InvalidReviewArgumentsCannotFallBackToUserStorage()
        {
            foreach(var args in new[]{
                new[]{"--campaign-flow-review"},new[]{"--campaign-flow-review","../save"},new[]{"--campaign-flow-review",Guid.Empty.ToString("N")},
                new[]{"--quality-presentation-review",Id},new[]{"--quality-presentation-review",Id,"unknown"},
                new[]{"--campaign-flow-review",Id,"--campaign-flow-review",Id},new[]{"--campaign-flow-review",Id,"--quality-presentation-review",Id,"motion"},
                new[]{"--campaign-flow-review",Id,"--crossover-review",Id},new[]{"--skill-roster-vfx-review",Id,"--roster-phase","invalid"}})
                Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(args,Data,User),string.Join(" ",args));
        }
        [Test] public void PlayerUnderUserStorageIsRejectedBeforeAnySaveObjectIsCreated()
        {
            var args=new[]{"--quality-presentation-review",Id,"performance"};
            Assert.Throws<InvalidOperationException>(()=>ReviewStorageBootstrap.Resolve(args,Path.Combine(User,"Build","TalesTactics_Data"),User));
            Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(args,"relative-data",User));
        }
        [Test] public void NoSupportedReviewSwitchLeavesNormalAndLegacyBootSelectionUnchanged()
        {
            Assert.That(ReviewStorageBootstrap.Resolve(new[]{"TalesTactics.exe","-logFile","external.log"},Data,User),Is.Null);
            Assert.That(ReviewStorageBootstrap.Resolve(new[]{"TalesTactics.exe","--crossover-review",Id},Data,User),Is.Null);
            Assert.That(ReviewStorageBootstrap.Resolve(null,Data,User),Is.Null);
        }
    }
}
