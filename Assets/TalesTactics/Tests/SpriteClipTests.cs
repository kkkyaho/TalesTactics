using NUnit.Framework;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
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
