using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>Painted combat controls sliced from one transparent 3x3 atlas.</summary>
    internal static class BattleInkIcons
    {
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        private static Sprite At(int index)
        {
            if (Cache.TryGetValue(index, out var cached) && cached != null && cached.texture != null) return cached;
            var atlas = Resources.Load<Texture2D>("Art/BattleUiInk");
            if (atlas == null) return null;
            atlas.filterMode = FilterMode.Bilinear;
            var w = atlas.width / 3f;
            var h = atlas.height / 3f;
            var column = index % 3;
            var row = index / 3;
            var sprite = Sprite.Create(atlas, new Rect(column * w, atlas.height - (row + 1) * h, w, h), new Vector2(.5f, .5f), 100f);
            sprite.name = "BattleInk_" + index;
            Cache[index] = sprite;
            return sprite;
        }

        public static Sprite Ui(string id)
        {
            switch (id)
            {
                case "swords": return At(0);
                case "road": case "spd": return At(1);
                case "lock": return At(2);
                case "quick_slot": case "backpack": return At(8);
                default: return At(3);
            }
        }

        public static Sprite Skill(string name, string element)
        {
            var n = (name ?? string.Empty).ToLowerInvariant();
            var e = (element ?? string.Empty).ToLowerInvariant();
            if (e == "loi" || n.Contains("lôi") || n.Contains("sấm")) return At(6);
            if (e == "hoa" || n.Contains("hỏa") || n.Contains("viêm")) return At(5);
            if (e == "thuy" || e == "bang" || n.Contains("thủy") || n.Contains("băng")) return At(4);
            return At(3);
        }

        public static Sprite Potion => At(7);
    }
}
