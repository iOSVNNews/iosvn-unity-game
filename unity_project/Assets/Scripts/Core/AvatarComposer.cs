using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Composes the layered figure from Resources/Avatar3 parts. Parts are grey-ramp PNGs (160 = base
    /// colour) recoloured per colour key; coloured pixels are kept, translucent ones are blended.
    /// The portrait (p_*) is a 384x640 front view: the figure faces the viewer, so the face has room
    /// for detailed eyes, nose and lips. The figure sheets used on the map and in battle (w_*, see
    /// HeroSprites) keep their own side-facing drawing of the same layers. Composition adds contact
    /// shadows under overlapping layers, a rim light and a coloured outer outline.
    /// </summary>
    public static class AvatarComposer
    {
        public const int W = 384, H = 640;
        /// <summary>Aura sheets behind / in front of the portrait are soft glows and stay at half that size.</summary>
        public const int AuraW = 192, AuraH = 320;
        public const string Folder = "Avatar3/";
        private static readonly Dictionary<string, Color32[]> PartCache = new Dictionary<string, Color32[]>();
        private static readonly Dictionary<string, Texture2D> LookCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> AuraCache = new Dictionary<string, Sprite[]>();

        public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>
        {
            { "fa", 4 }, { "ea", 3 }, { "ey", 8 }, { "br", 5 }, { "no", 4 }, { "mo", 5 }, { "bd", 5 }, { "ha", 10 },
            { "ti", 4 }, { "to", 6 }, { "pa", 4 }, { "sh", 3 }, { "be", 3 }, { "hat", 6 }, { "wp", 4 }, { "au", 6 },
        };

        public static readonly string[] HairColors = { "#1e1a1e", "#3a2a24", "#5a3a28", "#8a5a34", "#c8a070", "#d8d8e0", "#a03030", "#304070", "#205050", "#e8e0c8" };
        public static readonly string[] SkinColors = { "#f6dcc4", "#f0d2b4", "#e8c0a0", "#d8a880", "#c08860", "#9a6a4a", "#e0e8f0", "#c8d8c0" };
        public static readonly string[] EyeColors = { "#2a2a2a", "#5a3a20", "#3a8f7a", "#30a0d0", "#6a4ab0", "#c03030", "#d8a030", "#a0a0a8" };
        public static readonly string[] ClothColors = { "#e8e2d4", "#f0e8f0", "#3f6f6a", "#2f5f63", "#3060a0", "#b8d0e8", "#20242a", "#303038", "#7a2a3a", "#a03030", "#c8a050", "#6a6a72", "#5a6a3a", "#5a4a8a", "#8a6a3a", "#f0d080" };
        public static readonly string[] AuraColors = { "#8fe0ff", "#bfe8ff", "#9cff9c", "#ffd36a", "#ff8a3a", "#ff5050", "#b48cff", "#ffd6e8" };

        public static readonly string[] AuraNames = { "Không", "Linh vụ", "Hỏa diễm", "Lôi đình", "Liên hoa", "Kiếm trận" };

        public static bool Available => Resources.Load<TextAsset>(Folder + "p_body_m_0") != null;

        public static LookSpec Default(bool female)
        {
            return LookSpec.Parse(female
                ? "g=f;fa=2;ea=2;ey=2;ec=#6a4ab0;br=0;no=2;mo=3;bd=0;ha=9;hc=#24202c;ti=2;tc=#f2edf2;to=5;oc=#443b64;ac=#d6b875;pa=2;pc=#30283d;sh=1;sc=#262332;be=1;bc=#392f4d;hat=5;hac=#e2c57b;sk=#f6dcc4;wp=2;au=5;auc=#b48cff"
                : "g=m;fa=1;ea=0;ey=2;ec=#32a088;br=0;no=0;mo=3;bd=0;ha=6;hc=#181618;ti=3;tc=#e8e4dc;to=5;oc=#203f4b;ac=#d8b46a;pa=0;pc=#202a30;sh=1;sc=#20242a;be=1;bc=#262a30;hat=1;hac=#d8b46a;sk=#f2d8be;wp=2;au=5;auc=#8fe0ff");
        }

        // ------------------------------------------------------------------ parts

        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
        }

        internal static Color32[] Part(string name, int width, int height)
        {
            if (PartCache.TryGetValue(name, out var cached)) return cached;
            var asset = Resources.Load<TextAsset>(Folder + name);
            Color32[] pixels = null;
            if (asset != null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(asset.bytes) && texture.width == width && texture.height == height) pixels = texture.GetPixels32();
                Kill(texture);
            }
            // a portrait layer is a megabyte of pixels: browsing every option in the creator must not keep them all
            if (PartCache.Count > 72) PartCache.Clear();
            PartCache[name] = pixels;
            return pixels;
        }

        /// <summary>Ordered layers for a look: (part id, colour key or null, casts a contact shadow).</summary>
        internal static List<(string id, string color, bool shadow)> Layers(LookSpec look)
        {
            var g = look.Get("g", "m") == "f" ? "f" : "m";
            var list = new List<(string, string, bool)>();
            void Add(string lid, string key, string color, bool shadow) => list.Add(($"{lid}_{g}_{look.Int(key)}", color, shadow));
            Add("hb", "ha", "hc", false);
            var wp = look.Int("wp");
            if (wp == 1 || wp == 2) Add("wpb", "wp", null, false);
            Add("body", "fa", "sk", false);
            Add("ear", "ea", "sk", false);
            Add("pa", "pa", "pc", false);
            Add("sh", "sh", "sc", true);
            Add("ti", "ti", "tc", true);
            var to = look.Int("to");
            if (to > 0) { Add("to", "to", "oc", true); Add("tot", "to", "ac", false); }
            Add("be", "be", "bc", true);
            Add("tis", "ti", "tc", true);
            if (to == 1 || to == 4 || to == 5) { Add("tos", "to", "oc", true); Add("tost", "to", "ac", false); }
            // weapons carried in the far hand (4 kiếm, 5 chùy, 6 trượng, 7 búa, 8 bút) sit under the gripping hand;
            // the hip sword (3), gauntlet (9) and floating cauldron (10) are drawn over it. "wc" = quality colour.
            if (wp >= 4 && wp <= 8) Add("wpf", "wp", "wc", false);
            list.Add(($"hand_{g}_0", "sk", true));
            if (wp == 3 || wp >= 9) Add("wpf", "wp", "wc", false);
            Add("eyb", "ey", null, false);
            Add("eyi", "ey", "ec", false);
            Add("br", "br", "hc", false);
            Add("no", "no", "sk", false);
            Add("mo", "mo", null, false);
            if (look.Int("bd") > 0) Add("bd", "bd", "hc", true);
            Add("hf", "ha", "hc", true);
            if (look.Int("hat") > 0) Add("hat", "hat", "hac", true);
            return list;
        }

        /// <summary>Colour ramp for grey levels 0..255 with continuous smooth interpolation and Xianxia luster.</summary>
        public static Color32[] Ramp(string hex)
        {
            var c = HeroSprites.ParseColor(hex, new Color32(128, 128, 128, 255));
            Color.RGBToHSV(c, out var h, out var s, out var v);
            var keys = new[] { 0, 40, 64, 96, 128, 160, 192, 224, 255 };
            var keyColors = new Color[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                var level = keys[i];
                float h2, s2, v2;
                if (level <= 160)
                {
                    var k = level / 160f;
                    v2 = v * (.18f + .82f * k);
                    s2 = Mathf.Min(1f, s * (1.25f - .25f * k) + .06f * (1 - k));
                    h2 = h + .025f * (1 - k);
                }
                else
                {
                    var k = (level - 160) / 95f;
                    v2 = v + (1 - v) * .85f * k;
                    s2 = s * (1 - .42f * k);
                    h2 = h - .015f * k;
                }
                keyColors[i] = Color.HSVToRGB(Mathf.Repeat(h2, 1f), Mathf.Clamp01(s2), Mathf.Clamp01(v2));
            }

            var ramp = new Color32[256];
            for (var level = 0; level < 256; level++)
            {
                int seg = 0;
                while (seg < keys.Length - 2 && keys[seg + 1] < level) seg++;
                float t = (float)(level - keys[seg]) / Mathf.Max(1, keys[seg + 1] - keys[seg]);
                var col = Color.Lerp(keyColors[seg], keyColors[seg + 1], Mathf.Clamp01(t));
                ramp[level] = (Color32)col;
            }
            return ramp;
        }

        /// <summary>
        /// Alpha-over blend with grey-ramp tint. With shadow, canvas pixels just below/right of the
        /// layer (offset dx, dy in image pixels, light from the upper left) are darkened first.
        /// Arrays are Unity order (bottom row first) of the given width.
        /// </summary>
        public static void Blend(Color32[] dst, Color32[] src, Color32[] ramp, int width = 0, int shadowDx = 0, int shadowDy = 0, float shadow = 1f)
        {
            if (src == null) return;
            var n = Mathf.Min(dst.Length, src.Length);
            if (shadow < 1f && width > 0)
            {
                var height = n / width;
                for (var y = 0; y < height; y++)
                {
                    var sy = y + shadowDy;          // image "up" = larger Unity row
                    if (sy >= height) continue;
                    for (var x = shadowDx; x < width; x++)
                    {
                        var i = y * width + x;
                        if (src[i].a != 0 || dst[i].a == 0) continue;          // only where the layer itself leaves the canvas visible
                        if (src[sy * width + x - shadowDx].a == 0) continue;
                        var d = dst[i];
                        dst[i] = new Color32((byte)(d.r * shadow), (byte)(d.g * shadow), (byte)(d.b * shadow), d.a);
                    }
                }
            }
            for (var i = 0; i < n; i++)
            {
                var p = src[i];
                if (p.a == 0) continue;
                if (ramp != null && p.r == p.g && p.g == p.b)
                {
                    var r = ramp[p.r];
                    p = new Color32(r.r, r.g, r.b, p.a);
                }
                if (p.a == 255) { dst[i] = p; continue; }
                var a = p.a / 255f;
                var dd = dst[i];
                dst[i] = new Color32((byte)(p.r * a + dd.r * (1 - a)), (byte)(p.g * a + dd.g * (1 - a)), (byte)(p.b * a + dd.b * (1 - a)), (byte)Mathf.Max(dd.a, p.a));
            }
        }

        /// <summary>Coloured outer outline: transparent pixels touching the figure take the darkened neighbour average.</summary>
        public static void Outline(Color32[] canvas, int width, int height, int frameWidth = 0, float factor = .32f)
        {
            var copy = (Color32[])canvas.Clone();
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (copy[i].a != 0) continue;
                    int r = 0, g = 0, b = 0, c = 0;
                    var fx = frameWidth > 0 ? x % frameWidth : x;
                    var fw = frameWidth > 0 ? frameWidth : width;
                    if (fx > 0 && copy[i - 1].a != 0) { r += copy[i - 1].r; g += copy[i - 1].g; b += copy[i - 1].b; c++; }
                    if (fx < fw - 1 && x < width - 1 && copy[i + 1].a != 0) { r += copy[i + 1].r; g += copy[i + 1].g; b += copy[i + 1].b; c++; }
                    if (y > 0 && copy[i - width].a != 0) { r += copy[i - width].r; g += copy[i - width].g; b += copy[i - width].b; c++; }
                    if (y < height - 1 && copy[i + width].a != 0) { r += copy[i + width].r; g += copy[i + width].g; b += copy[i + width].b; c++; }
                    if (c == 0) continue;
                    canvas[i] = new Color32((byte)(r / c * factor), (byte)(g / c * factor), (byte)(b / c * factor), 255);
                }
        }

        /// <summary>Tiên Khí Quang Lực: Celestial rim lighting along the silhouette.</summary>
        private static void ApplyCelestialRim(Color32[] canvas, int width, int height, int frameWidth, Color32 auraCol)
        {
            var fw = frameWidth > 0 ? frameWidth : width;
            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var fx = frameWidth > 0 ? x % frameWidth : x;
                    if (fx <= 0 || fx >= fw - 1) continue;
                    var i = y * width + x;
                    var p = canvas[i];
                    if (p.a == 0) continue;

                    bool isTopEdge = canvas[(y + 1) * width + x].a == 0;
                    bool isLeftEdge = canvas[y * width + (x - 1)].a == 0;
                    if (isTopEdge || isLeftEdge)
                    {
                        float rimStrength = isTopEdge && isLeftEdge ? 0.36f : 0.22f;
                        byte nr = (byte)Mathf.Clamp(p.r * (1f - rimStrength) + auraCol.r * rimStrength + 12f, 0, 255);
                        byte ng = (byte)Mathf.Clamp(p.g * (1f - rimStrength) + auraCol.g * rimStrength + 12f, 0, 255);
                        byte nb = (byte)Mathf.Clamp(p.b * (1f - rimStrength) + auraCol.b * rimStrength + 12f, 0, 255);
                        canvas[i] = new Color32(nr, ng, nb, p.a);
                    }
                }
            }
        }

        /// <summary>Composes all layers of a look into a pixel array (portrait or world sheet).</summary>
        internal static Color32[] ComposePixels(LookSpec look, string prefix, int width, int height, int frameWidth, int sdx, int sdy)
        {
            var canvas = new Color32[width * height];
            var any = false;
            var auraCol = HeroSprites.ParseColor(look.Get("auc", "#8fe0ff"), new Color32(142, 224, 255, 255));

            foreach (var (id, color, shadow) in Layers(look))
            {
                var part = Part(prefix + id, width, height);
                if (part == null) continue;
                any = true;
                Blend(canvas, part, color == null ? null : Ramp(look.Get(color, "#888888")), width, sdx, sdy, shadow ? .8f : 1f);
            }
            if (!any) return null;

            // blush, lip colour and the light in the eyes are drawn in the portrait parts themselves
            ApplyCelestialRim(canvas, width, height, frameWidth, auraCol);
            Outline(canvas, width, height, frameWidth);
            return canvas;
        }

        public static Texture2D Compose(LookSpec look)
        {
            var key = look.ToString();
            if (LookCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var pixels = ComposePixels(look, "p_", W, H, 0, 2, 3) ?? new Color32[W * H];
            // the portrait is drawn at twice the old size and is shown at arbitrary scales: smooth sampling keeps fine lines even
            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Avatar" };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            if (LookCache.Count > 24)
            {
                foreach (var t in LookCache.Values) if (t != null) Kill(t);
                LookCache.Clear();
            }
            LookCache[key] = texture;
            return texture;
        }

        // ------------------------------------------------------------------ aura (khí tức)

        /// <summary>8 tinted frames of an aura layer. layer "b" (behind) or "f" (front); world = small sheet.</summary>
        public static Sprite[] AuraFrames(int style, string layer, bool world, string hex)
        {
            if (style <= 0) return null;
            var key = $"{style}_{layer}_{world}_{hex}";
            if (AuraCache.TryGetValue(key, out var cached) && (cached == null || (cached.Length > 0 && cached[0] != null))) return cached;
            var fw = world ? HeroSprites.FrameW : AuraW;
            var fh = world ? HeroSprites.FrameH : AuraH;
            var src = Part($"au_{layer}_{style}_{(world ? "w" : "p")}", fw * 8, fh);
            Sprite[] frames = null;
            if (src != null)
            {
                var ramp = Ramp(hex);
                var pixels = new Color32[src.Length];
                for (var i = 0; i < src.Length; i++)
                {
                    var p = src[i];
                    if (p.a == 0) continue;
                    var r = p.r == p.g && p.g == p.b ? ramp[p.r] : p;
                    pixels[i] = r.a == 0 ? p : new Color32(r.r, r.g, r.b, p.a);
                }
                var texture = new Texture2D(fw * 8, fh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Aura" };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                frames = new Sprite[8];
                var pivot = world ? HeroSprites.Pivot : new Vector2(.5f, 0f);
                for (var i = 0; i < 8; i++) frames[i] = Sprite.Create(texture, new Rect(i * fw, 0, fw, fh), pivot, 16f);
            }
            if (AuraCache.Count > 40) AuraCache.Clear();
            AuraCache[key] = frames;
            return frames;
        }

        /// <summary>Aura opacity for a realm index: faint for beginners, blazing for immortals.</summary>
        public static float AuraStrength(int realmIndex) => Mathf.Clamp(.42f + realmIndex * .055f, .42f, 1f);

        /// <summary>Adds the composed figure (with its animated aura) filling the parent, 3:5 aspect.</summary>
        public static RectTransform Build(RectTransform parent, LookSpec look, float auraStrength = .8f)
        {
            var rect = new GameObject("Avatar", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fit = rect.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = W / (float)H;
            Image Layer(string name)
            {
                var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(rect, false);
                var r = image.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                image.raycastTarget = false;
                image.enabled = false;
                return image;
            }
            var back = Layer("AuraBack");
            var raw = new GameObject("Figure", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(rect, false);
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one;
            raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
            raw.texture = Compose(look);
            raw.raycastTarget = false;
            var front = Layer("AuraFront");
            var anim = rect.gameObject.AddComponent<AuraAnimator>();
            anim.Back = back;
            anim.Front = front;
            anim.Set(look, false, auraStrength);
            rect.gameObject.AddComponent<AvatarIdleMotion>();
            return rect;
        }

        /// <summary>Refreshes a figure built with Build (creator preview).</summary>
        public static void Refresh(RectTransform avatar, LookSpec look, float auraStrength = .8f)
        {
            if (avatar == null) return;
            var raw = avatar.Find("Figure")?.GetComponent<RawImage>();
            if (raw != null) raw.texture = Compose(look);
            avatar.GetComponent<AuraAnimator>()?.Set(look, false, auraStrength);
        }

        /// <summary>World-sprite colours derived from a look (hair, robe, eyes).</summary>
        public static (Color32 hair, Color32 robe, Color32 eyes) WorldColors(LookSpec look)
        {
            var hair = HeroSprites.ParseColor(look.Get("hc"), new Color32(30, 26, 30, 255));
            var robe = HeroSprites.ParseColor(look.Int("to") > 0 ? look.Get("oc") : look.Get("tc"), new Color32(64, 110, 106, 255));
            var eyes = HeroSprites.ParseColor(look.Get("ec"), new Color32(60, 140, 130, 255));
            return (hair, robe, eyes);
        }
    }

    /// <summary>Loops the aura frames behind/in front of a figure; alpha pulses with the realm strength.</summary>
    public sealed class AuraAnimator : MonoBehaviour
    {
        public Image Back;
        public Image Front;
        public float Fps = 9f;
        public bool Flip;
        private Sprite[] back;
        private Sprite[] front;
        private float strength = 1f;
        private float time;
        private LookSpec look_;
        private bool world_;

        public void Set(LookSpec look, bool world, float auraStrength)
        {
            var style = look.Int("au");
            var hex = look.Get("auc", "#8fe0ff");
            back = AvatarComposer.AuraFrames(style, "b", world, hex);
            front = AvatarComposer.AuraFrames(style, "f", world, hex);
            strength = auraStrength;
            look_ = look;
            world_ = world;
            Update();
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            var frame = (int)(time * Fps) % 8;
            // sprites built at runtime can be unloaded with unused assets; rebuild them from the look
            if (look_ != null && ((back != null && back[0] == null) || (front != null && front[0] == null)))
            {
                var style = look_.Int("au");
                var hex = look_.Get("auc", "#8fe0ff");
                back = AvatarComposer.AuraFrames(style, "b", world_, hex);
                front = AvatarComposer.AuraFrames(style, "f", world_, hex);
            }
            var pulse = strength * (.88f + .12f * Mathf.Sin(time * 2.2f));
            // never show an Image without its sprite (it would draw a white box)
            if (Back != null) Back.enabled = back != null && back[frame] != null;
            if (Front != null) Front.enabled = front != null && front[frame] != null;
            if (back != null && Back != null && Back.enabled)
            {
                Back.sprite = back[frame];
                Back.color = new Color(1, 1, 1, pulse);
                Back.rectTransform.localScale = new Vector3(Flip ? -1 : 1, 1, 1);
            }
            if (front != null && Front != null && Front.enabled)
            {
                Front.sprite = front[frame];
                Front.color = new Color(1, 1, 1, pulse);
                Front.rectTransform.localScale = new Vector3(Flip ? -1 : 1, 1, 1);
            }
        }
    }

    /// <summary>A restrained breath and weight shift for the large portrait preview.</summary>
    public sealed class AvatarIdleMotion : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 rest;
        private bool ready;

        private void LateUpdate()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (rect == null) return;
            if (!ready) { rest = rect.anchoredPosition; ready = true; }
            var t = Time.unscaledTime;
            rect.anchoredPosition = rest + new Vector2(Mathf.Sin(t * 0.72f) * 1.2f, Mathf.Sin(t * 1.35f) * 2.4f);
            var breath = 1f + Mathf.Sin(t * 1.35f) * .008f;
            rect.localScale = new Vector3(breath, breath, 1f);
        }

        private void OnDisable()
        {
            if (rect == null || !ready) return;
            rect.anchoredPosition = rest;
            rect.localScale = Vector3.one;
            ready = false;
        }
    }
}
