namespace TalesTactics
{
    // Selects existing directional art only; it never edits CharacterData or equipment mappings.
    public static class CharacterMotionClips
    {
        public static DirectionalSpriteClip Casting(CharacterData character)
        {
            var poses=character?.Poses;var skill=poses?.Skill;
            var idle=character?.Sprites;var cast=poses?.Cast??idle;
            var frames=skill?.Frames;
            // MotionPremiumImporter explicitly places the channel/release poses (f16/f17)
            // at the end of this layout. Its attack windup and spear thrust are not casting art.
            if(CharacterVisualSizing.IsHero(character)&&frames?.Length==14&&skill.ReleaseFrame==7&&
                Same(frames[6],poses.Cast)&&Same(frames[7],poses.Attack))
                return new DirectionalSpriteClip{Frames=new[]{idle,cast,frames[12],frames[13],frames[12],cast,idle},ReleaseFrame=3};
            // AnimationPolish's legacy enemy clip shares a physical Attack release. Reuse its
            // preparation in reverse instead. Some enemies also share the Cast/Attack art itself;
            // keep that authored pose. Dedicated crossover boss spell clips stay intact.
            if(frames?.Length==5&&skill.ReleaseFrame==2&&Same(frames[1],poses.Cast)&&Same(frames[2],poses.Attack))
                return new DirectionalSpriteClip{Frames=new[]{frames[0],cast,cast,frames[0],idle},ReleaseFrame=2};
            if(frames!=null&&frames.Length>0)return skill;
            return new DirectionalSpriteClip{Frames=new[]{idle,cast,cast,idle},ReleaseFrame=2};
        }
        static bool Same(DirectionalSprites a,DirectionalSprites b)=>a!=null&&b!=null&&
            a.Front!=null&&a.Front==b.Front&&a.Back==b.Back&&a.Left==b.Left&&a.Right==b.Right;
    }
}
