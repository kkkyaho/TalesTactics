using UnityEditor;

namespace TalesTactics.Editor
{
    public static class SkillGeometryContent
    {
        [MenuItem("Tales Tactics/Apply Skill Geometry Samples")]
        public static void Apply()
        {
            Configure("natalia.0", SkillAreaShape.Line, 4, 2);
            Configure("farah.5", SkillAreaShape.Cone, 3, 1);
            Configure("natalia.attack", SkillAreaShape.Diamond, -1, 2);
            Configure("shionne.attack", SkillAreaShape.Diamond, -1, 2);
            AssetDatabase.SaveAssets();
        }
        static void Configure(string id, SkillAreaShape shape, int range, int height)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/TalesTactics/Content/Skills/" + id + ".asset");
            if (skill == null) throw new System.InvalidOperationException("Missing skill " + id);
            skill.Shape = shape; skill.RequiresLineOfSight = true; skill.MaxHeightDifference = height;
            if (range > 0) skill.Range = range;
            EditorUtility.SetDirty(skill);
        }
    }
}
