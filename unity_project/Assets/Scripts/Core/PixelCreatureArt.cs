using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Turns each catalog monster's own sprite into idle, lunge and hit poses.</summary>
    public static class PixelCreatureArt
    {
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>(StringComparer.OrdinalIgnoreCase);

        public static Sprite[] Frames(string monsterId, int variant = 0)
        {
            if (string.IsNullOrWhiteSpace(monsterId)) return null;
            for (var i = 0; i < monsterId.Length; i++)
            {
                var c = monsterId[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return null;
            }
            var key = monsterId + ":" + variant;
            if (Cache.TryGetValue(key, out var cached) && cached != null && cached.Length > 0 && cached[0] != null) return cached;
            var source = monsterId.StartsWith("player_", StringComparison.Ordinal)
                ? Resources.Load<Texture2D>("CombatPixel/Characters/" + monsterId)
                : Resources.Load<Texture2D>("CombatPixel/Monsters/" + monsterId);
            if (source == null) return null;
            source.filterMode = FilterMode.Point;
            Color32[] original;
            try { original = source.GetPixels32(); }
            catch (UnityException) { return null; }
            var width = source.width;
            var height = source.height;
            var scale = Mathf.Max(1, Mathf.RoundToInt(width / 64f));
            var frames = new Sprite[8];
            for (var frame = 0; frame < frames.Length; frame++)
            {
                var output = new Color32[original.Length];
                var wave = frame < 4 ? new[] { 0, 1, 0, -1 }[frame]
                    : frame == 4 ? 2 : frame == 5 ? 4 : frame == 6 ? -2 : -1;
                for (var y = 0; y < height; y++)
                {
                    var upper = y > height * .42f;
                    var lower = y < height * .27f;
                    var bend = upper ? wave : lower ? -wave : wave / 2;
                    var stride = lower && frame < 4 ? (frame % 2 == 0 ? -1 : 1) : 0;
                    var dx = (bend + stride) * scale;
                    var dy = frame == 6 ? -scale : frame == 7 ? -2 * scale : 0;
                    for (var x = 0; x < width; x++)
                    {
                        var pixel = original[y * width + x];
                        if (pixel.a < 24) continue;
                        if (variant > 0 && pixel.a > 100)
                            pixel = Color32.Lerp(pixel, new Color32(183, 149, 231, pixel.a), Mathf.Min(.32f, variant * .08f));
                        if (frame == 6 && pixel.a > 100)
                            pixel = Color32.Lerp(pixel, new Color32(255, 94, 76, pixel.a), .42f);
                        var px = x + dx;
                        var py = y + dy;
                        if (px >= 0 && px < width && py >= 0 && py < height) output[py * width + px] = pixel;
                    }
                }
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Creature_" + monsterId + "_" + frame,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixels32(output);
                texture.Apply(false, true);
                frames[frame] = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .1f), width);
            }
            Cache[key] = frames;
            return frames;
        }
    }

    /// <summary>Uses the same catalog poses in the movable offline PvE arena.</summary>
    public sealed class PixelCreatureAnimator : MonoBehaviour
    {
        private Image target;
        private Sprite[] frames;
        private float attackUntil;
        private float hitUntil;

        public void SetMonster(string id)
        {
            target = GetComponent<Image>();
            frames = PixelCreatureArt.Frames(id);
            if (target != null && frames != null && frames.Length > 0) target.sprite = frames[0];
        }

        public void Attack() { attackUntil = Time.unscaledTime + .34f; }
        public void Hit() { hitUntil = Time.unscaledTime + .28f; }

        private void Update()
        {
            if (target == null || frames == null || frames.Length < 8) return;
            var now = Time.unscaledTime;
            var index = hitUntil > now ? 6 + Mathf.FloorToInt(now * 9f) % 2
                : attackUntil > now ? 4 + Mathf.FloorToInt(now * 9f) % 2
                : Mathf.FloorToInt(now * 5f) % 4;
            target.sprite = frames[index];
        }
    }
}
