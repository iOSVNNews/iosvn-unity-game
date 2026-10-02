using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    public static class PixelWeaponArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static Sprite ForClass(string className)
        {
            var key = (className ?? "").ToLowerInvariant();
            var id = key.Contains("pháp") || key.Contains("phap") ? "moc_truong"
                : key.Contains("thể") || key.Contains("the") ? "thiet_chuy"
                : key.Contains("quyền") || key.Contains("quyen") || key.Contains("thú") || key.Contains("thu") ? "bo_quyen_sao"
                : key.Contains("đan") || key.Contains("dan") ? "dong_dinh"
                : key.Contains("rèn") || key.Contains("ren") ? "moc_bua"
                : key.Contains("phù") || key.Contains("phu") ? "truc_but"
                : "moc_kiem";
            return ForId(id);
        }

        public static Sprite ForId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Cache.TryGetValue(id, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>("CombatPixel/Weapons/" + id);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width);
            Cache[id] = sprite;
            return sprite;
        }
    }
}
