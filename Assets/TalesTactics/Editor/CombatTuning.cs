using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace TalesTactics.Editor
{
    // Explicit, one-time baseline for all shipped skills. Does not recreate content.
    public static class CombatTuning
    {
        static readonly HashSet<string> Lines = new HashSet<string> { "cless.0", "cless.4", "alphen.0", "natalia.0", "shionne.0", "kisara.2" };
        static readonly HashSet<string> Cones = new HashSet<string> { "farah.0", "farah.5", "farah.6", "kisara.3", "kisara.5", "alphen.5", "alphen.6" };
        static readonly HashSet<string> Spatial = new HashSet<string> { "cless.5", "cless.6", "cless.ultimate", "shionne.5", "jade.ultimate" };
        static readonly HashSet<string> Jumping = new HashSet<string> { "cless.2", "cless.3", "farah.2", "farah.4", "alphen.1", "alphen.4" };
        static readonly Dictionary<string, Element[]> Elements = new Dictionary<string, Element[]> {
            { "cless", new[]{Element.None,Element.None,Element.None,Element.Fire,Element.None,Element.None,Element.Light} },
            { "mint", new[]{Element.None,Element.None,Element.None,Element.None,Element.None,Element.None,Element.None} },
            { "velvet", new[]{Element.None,Element.None,Element.Fire,Element.Dark} },
            { "farah", new[]{Element.None,Element.None,Element.None,Element.None,Element.None,Element.None,Element.None} },
            { "tear", new[]{Element.None,Element.Dark,Element.None,Element.None,Element.Light,Element.None,Element.Light} },
            { "jade", new[]{Element.Earth,Element.Water,Element.Earth,Element.Lightning,Element.Light,Element.Ice,Element.Fire,Element.Lightning} },
            { "natalia", new[]{Element.None,Element.Wind,Element.None,Element.Light,Element.None,Element.None,Element.None} },
            { "alphen", new[]{Element.None,Element.None,Element.None,Element.None,Element.Fire,Element.Fire,Element.Fire} },
            { "shionne", new[]{Element.Light,Element.None,Element.Water,Element.Wind,Element.None,Element.Dark,Element.None} },
            { "kisara", new[]{Element.None,Element.None,Element.None,Element.None,Element.Fire,Element.Fire,Element.Ice} }
        };

        [MenuItem("Tales Tactics/Apply Complete Combat Baseline")]
        public static void Apply()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("Catalog required");
            foreach (var c in catalog.Characters)
            {
                if (!Elements.ContainsKey(c.Id) || c.Skills.Length != Elements[c.Id].Length)
                    throw new InvalidOperationException("Unexpected skills for " + c.Id);
            }
            foreach (var c in catalog.Characters.Concat(new[]{catalog.Enemy}))
            {
                Tune(c, c.BasicAttack, Element.None);
                for (int i=0;i<c.Skills.Length;i++) Tune(c,c.Skills[i],Elements[c.Id][i]);
                if (c.UltimateSkill != null)
                {
                    Element element=c.Id=="alphen"||c.Id=="shionne"||c.Id=="kisara"?Element.Fire:
                        c.Id=="velvet"||c.Id=="jade"?Element.Dark:c.Id=="cless"||c.Id=="tear"||c.Id=="natalia"?Element.Light:Element.None;
                    Tune(c,c.UltimateSkill,element);
                }
                // Neutral is explicit for most heroes; these are prototype balance values,
                // not claims about canonical character resistances.
                if(c==catalog.Enemy)c.Affinities=new[]{Affinity(Element.Earth,0.75f),Affinity(Element.Lightning,1.25f)};
                else if(c.Id=="alphen")c.Affinities=new[]{Affinity(Element.Fire,0.75f),Affinity(Element.Water,1.25f)};
                else if(c.Id=="tear"||c.Id=="mint")c.Affinities=new[]{Affinity(Element.Light,0.75f),Affinity(Element.Dark,1.25f)};
                else if(c.Id=="velvet")c.Affinities=new[]{Affinity(Element.Dark,0.75f),Affinity(Element.Light,1.25f)};
                else c.Affinities=Array.Empty<ElementAffinity>();
                EditorUtility.SetDirty(c);
            }
            catalog.Rules.HeightDamagePerStep=0.1f;catalog.Rules.HeightDamageMaxSteps=2;
            EditorUtility.SetDirty(catalog.Rules);AssetDatabase.SaveAssets();Export(catalog);
        }
        static ElementAffinity Affinity(Element e,float value)=>new ElementAffinity{Element=e,Multiplier=value};
        static void Tune(CharacterData c,SkillData s,Element element)
        {
            bool damage=s.Effects.Any(e=>e.Kind==EffectKind.Damage);
            bool magic=s.Effects.Any(e=>e.Kind==EffectKind.Damage&&e.Magic);
            bool support=s.Target==TargetType.Ally||s.Target==TargetType.FallenAlly||s.Target==TargetType.Self;
            bool ranged=c.Weapon==WeaponType.Bow||c.Weapon==WeaponType.Gun;
            s.Element=element;s.Shape=SkillAreaShape.Diamond;
            s.MaxHeightDifference=support||magic?4:2;
            s.RequiresLineOfSight=!support&&!magic;
            s.UsesHeightDamage=damage&&!magic;s.HeightRangeLimit=damage&&!magic&&ranged?2:0;
            if(Lines.Contains(s.Id)){s.Shape=SkillAreaShape.Line;s.Area=0;}
            if(Cones.Contains(s.Id)){s.Shape=SkillAreaShape.Cone;s.Range=Math.Max(3,s.Range);s.Area=0;}
            if(Jumping.Contains(s.Id))s.MaxHeightDifference=3;
            if(Spatial.Contains(s.Id)){s.RequiresLineOfSight=false;s.MaxHeightDifference=4;}
            if(s.Range==0){s.HeightRangeLimit=0;s.RequiresLineOfSight=false;}
            if(s.IsUltimate){s.MaxHeightDifference=4;s.RequiresLineOfSight=false;}
            EditorUtility.SetDirty(s);
        }
        static void Export(BattleCatalog catalog)
        {
            var text=new StringBuilder("# 전체 전투 규칙 적용표\n\n프로젝트 SRPG용 초기 밸런스이며 원작의 확정 수치를 뜻하지 않는다. 메뉴 재실행은 기하/속성/저항을 덮어쓰므로 사용자 조정 이후 무조건 실행하지 않는다. 비용·위력·해금·게이트는 유지한다.\n\n|캐릭터|기술 ID|이름|형태|사거리|반경|높이차|시야|높이 사거리|물리 높이 피해|속성|\n|---|---|---|---|---|---|---|---|---|---|---|\n");
            foreach(var c in catalog.Characters.Concat(new[]{catalog.Enemy}))
                foreach(var s in new[]{c.BasicAttack}.Concat(c.Skills).Concat(new[]{c.UltimateSkill}).Where(s=>s!=null).Distinct())
                    text.AppendLine($"|{c.DisplayName}|{s.Id}|{s.DisplayName}|{s.Shape}|{s.MinRange}–{s.Range}|{s.Area}|{s.MaxHeightDifference}|{s.RequiresLineOfSight}|±{s.HeightRangeLimit}|{s.UsesHeightDamage}|{ElementalRules.Name(s.Element)}|");
            text.AppendLine("\n## 저항/약점\n\n미지정 속성·무속성은 ×1. 피해 배율0은 무효, 0~1은 저항, 1~2는 약점. 회복과 상태효과 확률에는 적용하지 않는다.\n");
            foreach(var c in catalog.Characters.Concat(new[]{catalog.Enemy}))
                text.AppendLine("- "+c.DisplayName+": "+(c.Affinities.Length==0?"모든 속성 중립":string.Join(", ",c.Affinities.Select(a=>ElementalRules.Name(a.Element)+" ×"+a.Multiplier))));
            File.WriteAllText("Docs/COMBAT_BALANCE.md",text.ToString());
        }
    }
}
