using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>Original pixel rings for the combat controls.</summary>
    public static class PixelCombatHudArt
    {
        private static Sprite attack;
        private static Sprite skill;
        private static Sprite item;

        public static Sprite Circle(bool isAttack, bool isItem = false)
        {
            var cached = isAttack ? attack : isItem ? item : skill;
            if (cached != null) return cached;
            const int size = 64;
            var pixels = new Color32[size * size];
            var gold = new Color32(236, 188, 86, 255);
            var jade = new Color32(78, 197, 162, 255);
            var silver = new Color32(147, 172, 182, 255);
            var ring = isAttack ? gold : isItem ? silver : jade;
            var inner = isAttack ? new Color32(48, 31, 28, 245) : new Color32(19, 34, 43, 245);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - 31.5f;
                var dy = y - 31.5f;
                var distance = dx * dx + dy * dy;
                if (distance > 31f * 31f) continue;
                var step = ((x / 4 + y / 4) & 1) == 0;
                pixels[y * size + x] = distance > 27f * 27f ? new Color32(8, 14, 19, 255)
                    : distance > 23f * 23f ? step ? ring : Color32.Lerp(ring, Color.black, .22f)
                    : distance > 20f * 20f ? new Color32(10, 17, 24, 255)
                    : inner;
            }
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var x = Mathf.RoundToInt(31.5f + Mathf.Cos(angle) * 25f);
                var y = Mathf.RoundToInt(31.5f + Mathf.Sin(angle) * 25f);
                for (var px = x - 1; px <= x + 1; px++)
                for (var py = y - 1; py <= y + 1; py++) if (px >= 0 && px < size && py >= 0 && py < size) pixels[py * size + px] = ring;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = isAttack ? "CombatAttackRing" : isItem ? "CombatItemRing" : "CombatSkillRing",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            cached = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            if (isAttack) attack = cached; else if (isItem) item = cached; else skill = cached;
            return cached;
        }
    }
}
