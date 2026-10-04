using NUnit.Framework;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void ActionPhasesHoldPreparationUntilExplicitRelease()
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[5],ReleaseFrame=2};
            Assert.That(clip.PhaseFrameIndex(0,1,false),Is.EqualTo(0));
            Assert.That(clip.PhaseFrameIndex(.5f,1,false),Is.EqualTo(1));
            Assert.That(clip.PhaseFrameIndex(100,1,false),Is.EqualTo(1));
            Assert.That(clip.PhaseFrameIndex(0,1,true),Is.EqualTo(2));
            Assert.That(clip.PhaseFrameIndex(.4f,1,true),Is.EqualTo(3));
            Assert.That(clip.PhaseFrameIndex(.8f,1,true),Is.EqualTo(4));
            Assert.That(clip.PhaseFrameIndex(100,1,true),Is.EqualTo(4));
        }
        [Test] public void ActionPhasesKeepLegacyPairsAndHandleInvalidData()
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[2]};
            Assert.That(clip.PhaseFrameIndex(100,1,false),Is.Zero);
            Assert.That(clip.PhaseFrameIndex(100,1,true),Is.EqualTo(1));
            clip.ReleaseFrame=99;
            Assert.That(clip.PhaseFrameIndex(100,1,true),Is.EqualTo(1));
            clip.ReleaseFrame=-2;
            Assert.That(clip.PhaseFrameIndex(float.NaN,1,false),Is.Zero);
            Assert.That(clip.PhaseFrameIndex(-2,0,true),Is.EqualTo(1));
            clip.Frames=null;
            Assert.That(clip.SamplePhase(Facing.Front,1,1,true),Is.Null);
        }
        [Test] public void SpriteClipLoopsAtFrameBoundariesAndClampsCollapse()
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[4],FramesPerSecond=8};
            Assert.That(clip.FrameIndex(0,true),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(0.124f,true),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(0.125f,true),Is.EqualTo(1));
            Assert.That(clip.FrameIndex(0.499f,true),Is.EqualTo(3));
            Assert.That(clip.FrameIndex(0.5f,true),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(1000,false),Is.EqualTo(3));
        }
        [Test] public void SpriteClipMissingFramesAndInvalidTimingAreSafe()
        {
            var clip=new DirectionalSpriteClip();
            Assert.That(clip.FrameIndex(1,true),Is.EqualTo(-1));
            Assert.That(clip.Sample(Facing.Front,1,true),Is.Null);
            clip.Frames=null;Assert.That(clip.FrameIndex(1,false),Is.EqualTo(-1));
            clip.Frames=new DirectionalSprites[3];
            Assert.That(clip.Sample(Facing.Front,0,true),Is.Null);
            Assert.That(clip.FrameIndex(-1,true),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(float.NaN,true),Is.EqualTo(0));
            Assert.That(clip.FrameIndex(float.PositiveInfinity,false),Is.EqualTo(0));
            clip.FramesPerSecond=0;Assert.That(clip.FrameIndex(100,true),Is.EqualTo(0));
            clip.FramesPerSecond=-8;Assert.That(clip.FrameIndex(100,true),Is.EqualTo(0));
            clip.FramesPerSecond=float.PositiveInfinity;Assert.That(clip.FrameIndex(100,true),Is.EqualTo(0));
        }
    }
}
