using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
        [Test] public void EnvironmentModeStartsInTheExactPerRunIsolatedStorage()
        {
            var args=new[]{"TalesTactics.exe","--environment-integration-review",Id,"-logFile",Path.Combine(Root,"environment.log")};
            string expected=Path.GetFullPath(Path.Combine(Root,"Build","EnvironmentIntegrationReviews",Id,"Storage"));
            Assert.That(ReviewStorageBootstrap.Resolve(args,Data,User),Is.EqualTo(expected));
            Assert.That(ReviewStorageBootstrap.Resolve(args,Data,User),Is.Not.EqualTo(Path.GetFullPath(User)));
            Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(new[]{"--environment-integration-review",Guid.Empty.ToString("N")},Data,User));
            Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(new[]{"--environment-integration-review","../User"},Data,User));
        }
        [Test] public void EnvironmentModeCannotMixWithAnotherReviewOrReuseItsOwnSwitch()
        {
            foreach(string other in new[]{"--quality-presentation-review","--campaign-flow-review","--skill-roster-vfx-review","--environment-integration-review","--crossover-review"})
                Assert.Throws<ArgumentException>(()=>ReviewStorageBootstrap.Resolve(new[]{"--environment-integration-review",Id,other,Id,"motion"},Data,User),other);
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
        [TestCase("EnvironmentIntegrationReview")][TestCase("BattleHudRedesignReview")]
        public void StorageSnapshotIgnoresOnlyLockedUnityLiveLogs(string reviewType)
        {
            string root=Path.Combine(Path.GetTempPath(),"TalesTactics-review-snapshot-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            try
            {
                var kept=new[]{"campaign.json","settings.json","campaign.json.tmp","notes.bin","Other.log","Player.log.bak"};
                foreach(string name in kept)File.WriteAllText(Path.Combine(root,name),"protected");
                using(var current=new FileStream(Path.Combine(root,"pLaYeR.LoG"),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None))
                using(var previous=new FileStream(Path.Combine(root,"Player-prev.log"),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None))
                {
                    var before=ReviewSnapshot(reviewType,root);
                    Assert.That(before.Select(s=>s.Substring(0,s.IndexOf(':'))),Is.EquivalentTo(kept));
                    current.WriteByte(1);current.Flush();previous.WriteByte(2);previous.Flush();
                    Assert.That(ReviewSnapshot(reviewType,root),Is.EqualTo(before),"Volatile locked logs must not invalidate save preservation.");
                }
            }
            finally{DeleteSnapshotFixture(root);}
        }
        [TestCase("EnvironmentIntegrationReview")][TestCase("BattleHudRedesignReview")]
        public void StorageSnapshotDetectsUnrelatedAndTemporaryFileChanges(string reviewType)
        {
            string root=Path.Combine(Path.GetTempPath(),"TalesTactics-review-snapshot-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(Path.Combine(root,"campaign.json"),"save");var before=ReviewSnapshot(reviewType,root);
                string other=Path.Combine(root,"notes.bin");File.WriteAllText(other,"before");var added=ReviewSnapshot(reviewType,root);
                Assert.That(added,Is.Not.EqualTo(before),"Unknown top-level files must be protected.");
                File.WriteAllText(other,"after!");var changed=ReviewSnapshot(reviewType,root);
                Assert.That(changed,Is.Not.EqualTo(added),"Equal-length edits must be detected by the hash.");
                string temporary=Path.Combine(root,"settings.json.tmp");File.WriteAllText(temporary,"pending");var withTemporary=ReviewSnapshot(reviewType,root);
                Assert.That(withTemporary,Is.Not.EqualTo(changed),"Temporary save files must not be silently excluded.");
                File.Delete(temporary);Assert.That(ReviewSnapshot(reviewType,root),Is.EqualTo(changed));
                File.Delete(other);Assert.That(ReviewSnapshot(reviewType,root),Is.EqualTo(before));
            }
            finally{DeleteSnapshotFixture(root);}
        }
        static string[] ReviewSnapshot(string reviewType,string root)
        {
            var type=typeof(BattleDirector).Assembly.GetType("TalesTactics."+reviewType);Assert.That(type,Is.Not.Null);
            var snapshot=type.GetMethod("Snapshot",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(snapshot,Is.Not.Null);
            return (string[])snapshot.Invoke(null,new object[]{root});
        }
        static void DeleteSnapshotFixture(string root)
        {
            var directory=new DirectoryInfo(root);
            if(!string.Equals(directory.Parent.FullName,Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)||!directory.Name.StartsWith("TalesTactics-review-snapshot-",StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing cleanup outside the owned temporary fixture.");
            if(directory.Exists)directory.Delete(true);
        }
    }
}
