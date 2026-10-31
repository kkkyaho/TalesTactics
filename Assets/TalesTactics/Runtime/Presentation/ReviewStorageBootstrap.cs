#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.IO;
using System.Linq;

namespace TalesTactics
{
    // Pure argument/path validation. The live call site is standalone REVIEW-only;
    // normal players do not contain this type or change their storage behavior.
    public static class ReviewStorageBootstrap
    {
        static readonly string[] Switches={"--quality-presentation-review","--campaign-flow-review","--skill-roster-vfx-review"};
        public static string Resolve(string[] arguments,string playerDataPath,string userDataPath)
        {
            var args=arguments??Array.Empty<string>();var selected=args.Select((value,index)=>new{value,index}).Where(a=>Switches.Contains(a.value)).ToArray();
            if(selected.Length==0)return null;
            if(selected.Length!=1||args.Count(a=>a!=null&&a.StartsWith("--",StringComparison.Ordinal)&&a.EndsWith("-review",StringComparison.Ordinal))!=1)throw new ArgumentException("Choose exactly one supported review mode.");
            int position=selected[0].index;string option=selected[0].value;
            if(position+1>=args.Length||!Guid.TryParseExact(args[position+1],"N",out var id)||id==Guid.Empty)throw new ArgumentException("Review requires a non-empty 32-digit run ID.");
            string runId=args[position+1],folder,storage;
            if(option==Switches[0])
            {
                if(position+2>=args.Length||!new[]{"vfx","motion","performance"}.Contains(args[position+2]))throw new ArgumentException("Quality review requires vfx, motion or performance.");
                folder="QualityPresentationReviews";storage="isolated-save";
            }
            else if(option==Switches[1]){folder="CampaignFlowReviews";storage="Storage";}
            else
            {
                var phases=args.Select((value,index)=>new{value,index}).Where(a=>a.value=="--roster-phase").ToArray();
                if(phases.Length>1||phases.Length==1&&(phases[0].index+1>=args.Length||!new[]{"before","after"}.Contains(args[phases[0].index+1])))throw new ArgumentException("Roster review phase must be before or after.");
                folder="SkillRosterVfx";storage=Path.Combine("isolated-save",runId);
            }
            if(string.IsNullOrWhiteSpace(playerDataPath)||!Path.IsPathRooted(playerDataPath)||string.IsNullOrWhiteSpace(userDataPath)||!Path.IsPathRooted(userDataPath))throw new ArgumentException("Review storage requires absolute application paths.");
            string playerRoot=Path.GetDirectoryName(Path.GetFullPath(playerDataPath));
            if(string.IsNullOrEmpty(playerRoot))throw new ArgumentException("Player data path has no parent.");
            string root=Path.GetFullPath(Path.Combine(playerRoot,folder,runId,storage)),user=Path.GetFullPath(userDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(user,StringComparison.OrdinalIgnoreCase)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||user.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Review storage must not overlap user storage.");
            return root;
        }
    }
}
#endif
