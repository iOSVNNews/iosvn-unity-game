using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Procedural ink-wash UI pieces: brush-stroke name tags, soft mist clouds, glows and rings,
    /// and a vertical brush banner. All textures are generated once and cached.
    /// </summary>
    public static class InkUi
    {
        private static Sprite brush, cloud, glow, ring, banner, paper, shadow, white, vignette;

        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (var i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                t.SetPixels32(px);
                t.Apply(false, true);
                white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
                return white;
            }
        }

        public static Sprite Brush => brush != null ? brush : (brush = MakeBrush(256, 72, 11));
        public static Sprite Banner => banner != null ? banner : (banner = MakeBanner(64, 256, 7));
        public static Sprite Cloud => cloud != null ? cloud : (cloud = MakeCloud(256, 128, 5));
        public static Sprite Glow => glow != null ? glow : (glow = MakeGlow(64));
        public static Sprite Ring => ring != null ? ring : (ring = MakeRing(64));
        public static Sprite Paper => paper != null ? paper : (paper = MakePaper(128));
        public static Sprite Shadow => shadow != null ? shadow : (shadow = MakeShadow(64, 24));
        public static Sprite Vignette => vignette != null ? vignette : (vignette = MakeVignette(128));

        private static Sprite MakeVignette(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var u = (x + .5f) / size * 2 - 1;
                    var v = (y + .5f) / size * 2 - 1;
                    var d = Mathf.Sqrt(u * u * .8f + v * v);
                    var a = Mathf.Clamp01((d - .35f) / .75f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            return Finish(texture, pixels);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff;
            }
        }

        private static float Noise(float x, float y, int seed)
        {
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);
            var fx = x - ix;
            var fy = y - iy;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            var a = Hash(ix, iy, seed);
            var b = Hash(ix + 1, iy, seed);
            var c = Hash(ix, iy + 1, seed);
            var d = Hash(ix + 1, iy + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static Sprite Finish(Texture2D texture, Color32[] pixels, Vector4 border = default)
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>Horizontal dry-brush stroke: dense ink core, ragged ends and bristle streaks.</summary>
        private static Sprite MakeBrush(int w, int h, int seed)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)(w - 1);
                    var v = (y + .5f) / h * 2f - 1f;
                    var thickness = .62f + .22f * Noise(u * 3, 0, seed) - .25f * Mathf.Pow(Mathf.Abs(u - .45f) * 2f, 4f);
                    var edge = Mathf.Abs(v) / Mathf.Max(.05f, thickness);
                    var rag = Noise(x * .35f, y * .9f, seed + 3) * .35f;
                    var a = Mathf.Clamp01((1.08f - edge - rag) * 5f);
                    var endFade = Mathf.Clamp01(u * 9f) * Mathf.Clamp01((1 - u) * 5f + Noise(y * .5f, 4, seed) * 1.5f);
                    a *= endFade;
                    var streak = Noise(x * .04f, y * 1.4f, seed + 9);
                    if (streak > .78f && u > .55f) a *= .55f;
                    var ink = (byte)(18 + streak * 18);
                    pixels[y * w + x] = new Color32(ink, ink, (byte)(ink + 4), (byte)(a * 245));
                }
            return Finish(texture, pixels, new Vector4(40, 8, 40, 8));
        }

        /// <summary>Vertical brush banner (for city names on the map, like a calligraphy scroll).</summary>
        private static Sprite MakeBanner(int w, int h, int seed)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var v = y / (float)(h - 1);
                    var u = (x + .5f) / w * 2f - 1f;
                    var thickness = .7f + .2f * Noise(0, v * 3, seed);
                    var edge = Mathf.Abs(u) / thickness;
                    var rag = Noise(x * .9f, y * .3f, seed + 3) * .3f;
                    var a = Mathf.Clamp01((1.05f - edge - rag) * 5f);
                    a *= Mathf.Clamp01(v * 12f) * Mathf.Clamp01((1 - v) * 6f + Noise(x * .5f, 7, seed) * 2f);
                    pixels[y * w + x] = new Color32(20, 20, 24, (byte)(a * 235));
                }
            return Finish(texture, pixels, new Vector4(8, 30, 8, 30));
        }

        private static Sprite MakeCloud(int w, int h, int seed)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)w * 2 - 1;
                    var v = y / (float)h * 2 - 1;
                    var d = Mathf.Sqrt(u * u + v * v * 1.1f);
                    var n = Noise(x * .03f, y * .05f, seed) * .6f + Noise(x * .09f, y * .12f, seed + 1) * .4f;
                    var a = Mathf.Clamp01((1f - d) * 1.6f + (n - .5f) * .9f);
                    a = a * a * (3 - 2 * a);
                    pixels[y * w + x] = new Color32(248, 247, 242, (byte)(a * 255));
                }
            return Finish(texture, pixels);
        }

        private static Sprite MakeGlow(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var u = (x + .5f) / size * 2 - 1;
                    var v = (y + .5f) / size * 2 - 1;
                    var a = Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            return Finish(texture, pixels);
        }

        private static Sprite MakeShadow(int w, int h)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var u = (x + .5f) / w * 2 - 1;
                    var v = (y + .5f) / h * 2 - 1;
                    var a = Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v));
                    pixels[y * w + x] = new Color32(20, 18, 26, (byte)(Mathf.Pow(a, .7f) * 120));
                }
            return Finish(texture, pixels);
        }

        private static Sprite MakeRing(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var u = (x + .5f) / size * 2 - 1;
                    var v = (y + .5f) / size * 2 - 1;
                    var d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - .82f);
                    var a = Mathf.Clamp01(1 - d * 9f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            return Finish(texture, pixels);
        }

        private static Sprite MakePaper(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var n = Noise(x * .15f, y * .15f, 3) * .6f + Hash(x, y, 5) * .4f;
                    var c = (byte)(222 + n * 22);
                    pixels[y * size + x] = new Color32(c, (byte)(c - 3), (byte)(c - 12), 255);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }

        // ------------------------------------------------------------------ widgets

        /// <summary>Black brush-stroke tag with white text (building / place name).</summary>
        public static RectTransform Tag(Transform parent, string text, int fontSize = 24, Color? textColor = null, float padding = 34f)
        {
            var rect = new GameObject("InkTag", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.sprite = Brush;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(rect, false);
            var lr = label.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(padding * .6f, 0); lr.offsetMax = new Vector2(-padding * .6f, 0);
            label.font = ModernUi.SemiBold;
            label.fontSize = fontSize;
            label.color = textColor ?? new Color32(246, 240, 226, 255);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            PixelUiSkin.ApplyTextTreatment(label);
            var width = label.preferredWidth + padding * 2f;
            rect.sizeDelta = new Vector2(Mathf.Max(120f, width), fontSize * 2.1f);
            return rect;
        }

        /// <summary>Vertical calligraphy banner: one word per line.</summary>
        public static RectTransform VerticalBanner(Transform parent, string text, int fontSize = 22)
        {
            var rect = new GameObject("InkBanner", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.sprite = Banner;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var words = (text ?? string.Empty).Split(' ');
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(rect, false);
            var lr = label.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(2, 14); lr.offsetMax = new Vector2(-2, -14);
            label.font = ModernUi.SemiBold;
            label.fontSize = fontSize;
            label.lineSpacing = .92f;
            label.color = new Color32(246, 240, 226, 255);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = string.Join("\n", words);
            var longest = 0;
            foreach (var w in words) longest = Mathf.Max(longest, w.Length);
            rect.sizeDelta = new Vector2(Mathf.Max(54f, longest * fontSize * .62f + 22f), words.Length * fontSize * 1.18f + 44f);
            return rect;
        }

        public static Image Simple(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
