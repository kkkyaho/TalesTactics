using System;
using UnityEngine;
namespace TalesTactics
{
    [Serializable]
    public sealed class DirectionalSpriteClip
    {
        public DirectionalSprites[] Frames=new DirectionalSprites[0];
        public float FramesPerSecond=8;
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
