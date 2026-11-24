using UnityEngine;
namespace TalesTactics
{
    // Resolve only the authored playable roster. Loading is done once per actor
    // initialization, never per rendered frame, and missing art keeps the old poses.
    public static class CharacterMotionRegistry
    {
        public const string ResourceRoot="TalesTactics/CharacterMotion/";
        public static string ResourcePathFor(string characterId)
        {
            switch(characterId)
            {
                case "alphen":return AlphenVisualPilotMotion.ResourcePath;
                case "cless":case "farah":case "jade":case "kisara":case "mint":
                case "natalia":case "shionne":case "tear":case "velvet":
                    return ResourceRoot+characterId;
                default:return null;
            }
        }
        public static CharacterMotionProfile Load(string characterId)
        {
            string path=ResourcePathFor(characterId);
            if(path==null)return null;
            var profile=Resources.Load<CharacterMotionProfile>(path);
            return profile!=null&&profile.OwnerId==characterId?profile:null;
        }
    }
}
