using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// The redrawn art set, stored as PNG bytes under Resources/Art so it needs no import settings:
    ///   Art/Items/&lt;id&gt;       128 px item icon with its quality aura
    ///   Art/Monsters/&lt;id&gt;    4 body frames of 256 px (the body really moves: breathing, undulating, wing beats)
    ///   Art/MonsterFx/&lt;id&gt;   retired legacy effect sheets; painted monsters use their body frames only
    ///   Art/PaintedCombatFx         transparent 5 x 4 hand-painted effect atlas used for every skill
    /// Looked up by the same ids as the original PixelArt icons, which stay as the fallback.
    /// </summary>
    public static class ArtSprites
    {
        public const int MonsterSize = 256, BodyFrames = 4, AuraFrames = 6;

        public sealed class MonsterSet
        {
            public Sprite[] Body, Back, Front;
            public bool Alive => Body != null && Body.Length > 0 && Body[0] != null;
        }

        /// <summary>
        /// Effect sheets: frames, frame width, frame height, columns of the grid, loops, frames per second.
        /// Frames run left to right, top to bottom.
        /// </summary>
        public static readonly Dictionary<string, (int frames, int w, int h, int cols, bool loop, float fps)> FxInfo = new Dictionary<string, (int, int, int, int, bool, float)>
        {
            { "slash", (12, 240, 240, 4, false, 30f) }, { "hit", (10, 192, 192, 5, false, 30f) }, { "proj", (8, 192, 96, 4, true, 20f) },
            { "burst", (16, 288, 288, 4, false, 30f) }, { "cast", (12, 288, 144, 4, true, 14f) }, { "pillar", (14, 192, 384, 7, false, 26f) },
            { "guard", (10, 240, 240, 5, false, 24f) }, { "heal", (12, 192, 240, 6, true, 15f) }, { "claw", (10, 240, 240, 5, false, 26f) },
            { "bite", (10, 240, 240, 5, false, 22f) }, { "breath", (12, 288, 144, 4, false, 22f) }, { "wave", (10, 288, 288, 5, false, 25f) },
            { "quake", (12, 288, 192, 4, false, 25f) }, { "rain", (12, 240, 288, 6, false, 28f) }, { "sword", (6, 192, 72, 3, true, 18f) },
            { "palm", (12, 240, 240, 4, false, 28f) }, { "vortex", (12, 192, 288, 6, true, 18f) }, { "chain", (12, 240, 240, 4, false, 28f) },
            { "orb", (12, 144, 144, 6, true, 16f) },
            // cultivation set pieces
            { "giantsword", (14, 192, 384, 7, false, 20f) }, { "dragon", (8, 384, 192, 4, true, 14f) }, { "lotus", (14, 288, 192, 7, false, 20f) },
            { "talisman", (6, 144, 72, 3, true, 14f) },
            { "halo", (2, 288, 288, 2, true, 1f) },      // two layers turned by the game: 0 rim with trigrams, 1 hub with petals
        };

        public static readonly string[] Elements = { "kim", "moc", "thuy", "hoa", "tho", "phong", "loi", "bang", "ma", "thien" };

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly Dictionary<string, MonsterSet> Monsters = new Dictionary<string, MonsterSet>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Sprite[]> PaintedFxs = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.Ordinal);
        private static Texture2D paintedFxAtlas;

        /// <summary>Maps an original icon path (PixelArt/Monsters/id, PixelArt/Items/id) to the new sprite, or null.</summary>
        public static Sprite ForLegacyPath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            const string monsters = "PixelArt/Monsters/";
            const string items = "PixelArt/Items/";
            if (resourcePath.StartsWith(monsters, StringComparison.Ordinal)) return Monster(resourcePath.Substring(monsters.Length));
            if (resourcePath.StartsWith(items, StringComparison.Ordinal))
            {
                var id = resourcePath.Substring(items.Length);
                var painted = Item(id);
                if (painted != null) return painted;
                // skill jade slips (ngọc giản) have no painted item art of their own: show the painted scroll icon
                if (Resources.Load<Texture2D>("CombatPixel/Skills/" + id) != null) return UiIcon("scroll");
                return null;
            }
            const string ui = "PixelArt/UI/";
            if (resourcePath.StartsWith(ui, StringComparison.Ordinal)) return UiIcon(resourcePath.Substring(ui.Length));
            return null;
        }

        /// <summary>
        /// Drops a full cache and lets Unity free whatever is no longer on screen. Sprites still shown by an
        /// Image stay alive (they are referenced), so nothing visible ever loses its picture.
        /// </summary>
        private static void Evict<T>(Dictionary<string, T> cache, int limit)
        {
            if (cache.Count <= limit) return;
            cache.Clear();
            if (Application.isPlaying) Resources.UnloadUnusedAssets();
        }

        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
        }

        private static Texture2D LoadTexture(string path, bool mipmaps, bool readable)
        {
            if (Missing.Contains(path)) return null;
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) { Missing.Add(path); return null; }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipmaps);
            if (!texture.LoadImage(asset.bytes, !readable)) { Kill(texture); Missing.Add(path); return null; }
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = path;
            return texture;
        }

        public static Sprite Item(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var path = "Art/Items/" + id;
            if (Cache.TryGetValue(path, out var cached) && cached != null) return cached;
            var texture = LoadTexture(path, true, false);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 64f);
            sprite.name = path;
            Evict(Cache, 320);
            Cache[path] = sprite;
            return sprite;
        }

        /// <summary>Painted interface icon (Art/UI/id) replacing the old 32px pixel icon.</summary>
        public static Sprite UiIcon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var path = "Art/UI/" + id;
            if (Cache.TryGetValue(path, out var cached) && cached != null) return cached;
            var texture = LoadTexture(path, true, false);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 64f);
            sprite.name = path;
            Evict(Cache, 320);
            Cache[path] = sprite;
            return sprite;
        }

        /// <summary>One painted body frame for lists, the map and icons.</summary>
        public static Sprite Monster(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var key = "Art/MonsterStill/" + id;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var body = LoadTexture("Art/Monsters/" + id, false, true);
            if (body == null) return null;
            if (string.Equals(id, "da_lang", StringComparison.Ordinal)) body.filterMode = FilterMode.Bilinear;
            const int n = MonsterSize;
            var pixels = body.GetPixels32();
            var width = body.width;
            Kill(body);
            if (width < n || pixels.Length < width * n) return null;
            var still = new Color32[n * n];
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    still[y * n + x] = pixels[y * width + x];
                }
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = key };
            texture.SetPixels32(still);
            texture.Apply(true, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 64f);
            sprite.name = key;
            Evict(Cache, 320);
            Cache[key] = sprite;
            return sprite;
        }

        private static void Over(ref Color32 dst, Color32 src)
        {
            if (src.a == 0) return;
            if (src.a == 255 || dst.a == 0) { dst = src; return; }
            var a = src.a / 255f;
            dst = new Color32((byte)(dst.r + (src.r - dst.r) * a), (byte)(dst.g + (src.g - dst.g) * a), (byte)(dst.b + (src.b - dst.b) * a), (byte)Mathf.Max(dst.a, src.a));
        }

        /// <summary>Body frames and looping aura of a monster for the battle scenes, or null when it has no redrawn art.</summary>
        public static MonsterSet MonsterFrames(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Monsters.TryGetValue(id, out var cached) && cached != null && cached.Alive) return cached;
            var body = LoadTexture("Art/Monsters/" + id, false, false);
            if (body == null) return null;
            if (string.Equals(id, "da_lang", StringComparison.Ordinal)) body.filterMode = FilterMode.Bilinear;
            const int n = MonsterSize;
            var set = new MonsterSet { Body = new Sprite[Mathf.Max(1, Mathf.Min(BodyFrames, body.width / n))] };
            for (var i = 0; i < set.Body.Length; i++) set.Body[i] = Sprite.Create(body, new Rect(i * n, 0, n, n), new Vector2(.5f, 0f), 64f);
            Evict(Monsters, 4);        // each set is ~4 MB of pixels
            Monsters[id] = set;
            return set;
        }

        /// <summary>Hand-painted combat effect cell in the matching element palette.</summary>
        public static Sprite[] Fx(string effect, string element)
        {
            if (string.IsNullOrEmpty(effect) || !FxInfo.ContainsKey(effect)) return null;
            if (Array.IndexOf(Elements, element) < 0) element = "kim";
            var key = effect + "_" + element;
            if (PaintedFxs.TryGetValue(key, out var cached) && cached != null && cached.Length > 0 && cached[0] != null) return cached;
            if (paintedFxAtlas == null)
            {
                paintedFxAtlas = Resources.Load<Texture2D>("Art/PaintedCombatFx");
                if (paintedFxAtlas != null) paintedFxAtlas.filterMode = FilterMode.Bilinear;
            }
            if (paintedFxAtlas == null || paintedFxAtlas.width < 5 || paintedFxAtlas.height < 4) return null;

            var row = effect == "slash" || effect == "sword" || effect == "claw" || effect == "bite" || effect == "chain" || effect == "wave" || effect == "palm" ? 1
                : effect == "hit" || effect == "burst" || effect == "quake" ? 2
                : effect == "cast" || effect == "pillar" || effect == "guard" || effect == "heal" || effect == "rain" || effect == "vortex" || effect == "lotus" || effect == "giantsword" || effect == "halo" ? 3
                : 0;
            var column = element == "moc" || element == "phong" ? 1
                : element == "thuy" || element == "bang" ? 2
                : element == "hoa" ? 3
                : element == "loi" || element == "ma" ? 4
                : 0;
            var cellWidth = paintedFxAtlas.width / 5;
            var cellHeight = paintedFxAtlas.height / 4;
            var rect = new Rect(column * cellWidth, paintedFxAtlas.height - (row + 1) * cellHeight, cellWidth, cellHeight);
            var sprite = Sprite.Create(paintedFxAtlas, rect, new Vector2(.5f, .5f), 256f);
            sprite.name = "PaintedFx_" + row + "_" + column;
            var frames = new[] { sprite };
            PaintedFxs[key] = frames;
            return frames;
        }
    }
}
