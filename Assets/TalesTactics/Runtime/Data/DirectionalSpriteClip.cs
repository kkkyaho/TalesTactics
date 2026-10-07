using System;
using UnityEngine;
namespace TalesTactics
{
    [Serializable]
    public sealed class DirectionalSpriteClip
    {
        public DirectionalSprites[] Frames=new DirectionalSprites[0];
        public float FramesPerSecond=8;
        // Preparation and release advance independently, so long windups never show a hit early.
        public int ReleaseFrame=1;
        public int PhaseFrameIndex(float elapsed,float duration,bool released)
        {
            if(Frames==null||Frames.Length==0)return -1;
            int split=Math.Max(1,Math.Min(ReleaseFrame,Frames.Length));
            int first=released?Math.Min(split,Frames.Length-1):0;
            int count=released?Frames.Length-first:split;
            float progress=duration>0?elapsed/duration:0;
            if(float.IsNaN(progress)||float.IsInfinity(progress))progress=0;
            return first+(int)Math.Min(Math.Floor(Math.Max(0,progress)*count),count-1);
        }
        public Sprite SamplePhase(Facing facing,float elapsed,float duration,bool released)
        {
            int index=PhaseFrameIndex(elapsed,duration,released);
            return index<0?null:Frames[index]?.Get(facing);
        }
        public int FrameIndex(float elapsed,bool loop)
        {
            if(Frames==null||Frames.Length==0)return -1;
            double frame=Math.Floor(Math.Max(0,(double)elapsed)*Math.Max(0,(double)FramesPerSecond));
            if(double.IsNaN(frame)||double.IsInfinity(frame))frame=0;
            return loop?(int)(frame%Frames.Length):(int)Math.Min(frame,Frames.Length-1);
        }
        public Sprite Sample(Facing facing,float elapsed,bool loop)
        {
            int index=FrameIndex(elapsed,loop);
            return index<0?null:Frames[index]?.Get(facing);
        }
    }
}
