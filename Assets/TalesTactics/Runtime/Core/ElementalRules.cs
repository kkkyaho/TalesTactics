using System;
using System.Linq;
using UnityEngine;

namespace TalesTactics
{
    [Serializable] public sealed class ElementAffinity
    {
        public Element Element;
        public float Multiplier = 1;
    }
    public static class ElementalRules
    {
        public static float Multiplier(CharacterData target, Element element)
        {
            if (element == Element.None) return 1;
            var entry = target.Affinities?.FirstOrDefault(a => a != null && a.Element == element);
            return entry == null || float.IsNaN(entry.Multiplier) || float.IsInfinity(entry.Multiplier)
                ? 1 : Math.Max(0f, Math.Min(2f, entry.Multiplier));
        }
        public static string Name(Element element)
        {
            switch (element)
            {
                case Element.Fire: return "불"; case Element.Water: return "물";
                case Element.Wind: return "바람"; case Element.Earth: return "땅";
                case Element.Light: return "빛"; case Element.Dark: return "어둠";
                case Element.Lightning: return "번개"; case Element.Ice: return "얼음";
                default: return "무속성";
            }
        }
    }
}
