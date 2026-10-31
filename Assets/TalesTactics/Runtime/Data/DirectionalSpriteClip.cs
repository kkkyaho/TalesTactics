using System;
using UnityEngine;
namespace TalesTactics
{
    public enum SpritePhasePacing { Uniform, Action, Cast }
    [Serializable]
    public sealed class DirectionalSpriteClip
    {
        public DirectionalSprites[] Frames=new DirectionalSprites[0];
        public float FramesPerSecond=8;
        // Preparation and release advance independently, so long windups never show a hit early.
        public int ReleaseFrame=1;
        public int PhaseFrameIndex(float elapsed,float duration,bool released,SpritePhasePacing pacing=SpritePhasePacing.Uniform)
        {
            if(Frames==null||Frames.Length==0)return -1;
            int split=Math.Max(1,Math.Min(ReleaseFrame,Frames.Length));
            int first=released?Math.Min(split,Frames.Length-1):0;
            int count=released?Frames.Length-first:split;
            float progress=duration>0?elapsed/duration:0;
            if(float.IsNaN(progress)||float.IsInfinity(progress))progress=0;
            if(pacing!=SpritePhasePacing.Uniform)
            {
                if(progress>=1)return first+count-1;
                // Normalize dwell inside each phase. The caller still owns the release instant
                // and total duration; every authored frame remains in its original order.
                float total=0;
                for(int i=0;i<count;i++)total+=Dwell(i,count,released,pacing);
                float remaining=Math.Max(0,progress)*total;
                for(int i=0;i<count;i++)
                {
                    remaining-=Dwell(i,count,released,pacing);
                    if(remaining<0)return first+i;
                }
                return first+count-1;
            }
            return first+(int)Math.Min(Math.Floor(Math.Max(0,progress)*count),count-1);
        }
        static float Dwell(int index,int count,bool released,SpritePhasePacing pacing)
        {
            if(count==1)return 1;
            if(pacing==SpritePhasePacing.Cast)
                return released?(index==0?1.9f:index==count-1?.6f:1):index==0?.45f:index==count-1?1.6f:1;
            if(count==2)return 1;
            return released?(index==0?1.5f:index==count-1?1.35f:.85f):
                index==count-1?1.65f:index==0?1.15f:index>=count*.55f?.75f:1;
        }
        public Sprite SamplePhase(Facing facing,float elapsed,float duration,bool released,SpritePhasePacing pacing=SpritePhasePacing.Uniform)
        {
            int index=PhaseFrameIndex(elapsed,duration,released,pacing);
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
