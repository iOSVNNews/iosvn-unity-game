using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Hand-painted art added after the pixel era (all PNG bytes in Resources, read with LoadImage):
    ///   Art/Skills/&lt;skill id&gt;        one 256 px icon per player skill
    ///   Art/MonsterPoses/&lt;monster id&gt;  1024x256 strip, facing left: idle, run, attack (bite / claw), pounce
    /// </summary>
    public static class PaintedArt
    {
        public const int PoseIdle = 0, PoseRun = 1, PoseAttack = 2, PosePounce = 3;
        private const int Cell = 256;

        // Idle bodies are alpha-trimmed by ArtSprites; action poses keep their full
        // 256 px canvas. Match the same pixel scale and feet when swapping sprites.
        public static float PoseCanvasScale(Sprite idleBody, Vector2 displaySize)
        {
            if (idleBody == null) return 1f;
            var pixelsToScreen = Mathf.Min(displaySize.x / idleBody.rect.width, displaySize.y / idleBody.rect.height);
            return pixelsToScreen * Cell / Mathf.Max(1f, Mathf.Min(displaySize.x, displaySize.y));
        }

        public static float PoseFeetOffset(Sprite idleBody, Vector2 displaySize)
        {
            if (idleBody == null) return 0f;
            var pixelsToScreen = Mathf.Min(displaySize.x / idleBody.rect.width, displaySize.y / idleBody.rect.height);
            return -idleBody.rect.y * pixelsToScreen;
        }
        private static readonly Dictionary<string, Sprite> Skills = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Sprite[]> Poses = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.Ordinal);

        private static Texture2D Load(string path)
        {
            if (Missing.Contains(path)) return null;
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) { Missing.Add(path); return null; }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(asset.bytes, true)) { UnityEngine.Object.Destroy(texture); Missing.Add(path); return null; }
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = path;
            Resources.UnloadAsset(asset);
            return texture;
        }

        /// <summary>The painted icon of a player skill, or null when it has none yet.</summary>
        public static Sprite Skill(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Skills.TryGetValue(id, out var cached) && cached != null && cached.texture != null) return cached;
            var texture = Load("Art/Skills/" + id);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            sprite.name = "Skill_" + id;
            if (Skills.Count > 200) Skills.Clear();
            Skills[id] = sprite;
            return sprite;
        }

        /// <summary>The four action poses of a monster species (idle, run, attack, pounce), or null.</summary>
        public static Sprite[] MonsterPoses(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Poses.TryGetValue(id, out var cached) && cached != null && cached.Length == 4 && cached[0] != null && cached[0].texture != null) return cached;
            var texture = Load("Art/MonsterPoses/" + id);
            if (texture == null || texture.width < Cell * 4) return null;
            var set = new Sprite[4];
            for (var i = 0; i < 4; i++)
            {
                // pivot at the feet line (the poses stand on y = 8 px of their cell)
                set[i] = Sprite.Create(texture, new Rect(i * Cell, texture.height - Cell, Cell, Cell), new Vector2(.5f, 0f), 100f);
                set[i].name = id + "_pose" + i;
            }
            if (Poses.Count > 60) Poses.Clear();
            Poses[id] = set;
            return set;
        }
    }
}
