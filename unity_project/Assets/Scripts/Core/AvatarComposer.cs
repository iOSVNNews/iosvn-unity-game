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
        private static readonly Dictionary<string, Sprite> IllustrationCache = new Dictionary<string, Sprite>();

        public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>
        {
            { "bo", 4 },
            { "fa", 4 }, { "ea", 3 }, { "ey", 8 }, { "br", 5 }, { "no", 4 }, { "mo", 5 }, { "bd", 5 }, { "ha", 10 },
            { "ti", 4 }, { "to", 6 }, { "tot", 6 }, { "pa", 4 }, { "sh", 3 }, { "be", 3 }, { "hat", 6 }, { "wp", 11 }, { "au", 6 },
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
                ? "g=f;bo=2;fa=2;ea=2;ey=2;ec=#6a4ab0;br=0;no=2;mo=3;bd=0;ha=9;hc=#24202c;ti=2;tc=#f2edf2;to=5;oc=#443b64;ac=#d6b875;pa=2;pc=#30283d;sh=1;sc=#262332;be=1;bc=#392f4d;hat=5;hac=#e2c57b;sk=#f6dcc4;wp=2;au=5;auc=#b48cff"
                : "g=m;bo=1;fa=1;ea=0;ey=2;ec=#32a088;br=0;no=0;mo=3;bd=0;ha=6;hc=#181618;ti=3;tc=#e8e4dc;to=5;oc=#203f4b;ac=#d8b46a;pa=0;pc=#202a30;sh=1;sc=#20242a;be=1;bc=#262a30;hat=1;hac=#d8b46a;sk=#f2d8be;wp=2;au=5;auc=#8fe0ff");
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
            if (to > 0)
            {
                Add("to", "to", "oc", true);
                // Older looks have no independent trim key, so keep their former matching trim.
                var trim = look.Int("tot", to);
                if (trim > 0) list.Add(($"tot_{g}_{trim}", "ac", false));
            }
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

        /// <summary>Painted profile portrait with the player's selected, animated aura.</summary>
        public static RectTransform BuildIllustration(RectTransform parent, LookSpec look, float auraStrength = .8f)
        {
            var female = look != null && look.Get("g", "m") == "f";
            var selected = look != null ? look.Int("preset", -1) : 0;
            if (selected < 0) selected = look != null ? look.Int("template", 0) : 0;
            var portrait = PresetIllustration(female, selected);
            if (portrait == null) return Build(parent, look, auraStrength);

            var rect = new GameObject("IllustratedAvatar", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fit = rect.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = portrait.rect.width / portrait.rect.height;
            Image Layer(string name)
            {
                var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(rect, false);
                image.rectTransform.anchorMin = Vector2.zero; image.rectTransform.anchorMax = Vector2.one;
                image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
                image.raycastTarget = false;
                image.enabled = false;
                return image;
            }
            var back = Layer("AuraBack");
            back.sprite = Illustration("PortraitIllustrationAura");
            back.preserveAspect = true;
            back.enabled = back.sprite != null && (look == null || look.Int("au") > 0);
            var auraLook = look ?? Default(female);
            var auraColor = HeroSprites.ParseColor(auraLook.Get("auc", "#8fe0ff"), new Color32(143, 224, 255, 255));
            back.color = new Color32(auraColor.r, auraColor.g, auraColor.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(auraStrength * .72f)));
            var figure = new GameObject("Illustration", typeof(RectTransform), typeof(AnimatedPortraitImage)).GetComponent<AnimatedPortraitImage>();
            figure.transform.SetParent(rect, false);
            figure.rectTransform.anchorMin = Vector2.zero; figure.rectTransform.anchorMax = Vector2.one;
            figure.rectTransform.offsetMin = figure.rectTransform.offsetMax = Vector2.zero;
            figure.raycastTarget = false;
            figure.enabled = true;
            figure.sprite = portrait;
            figure.preserveAspect = true;
            figure.SetAppearance(look);
            var anim = rect.gameObject.AddComponent<IllustrationAuraMotion>();
            anim.Set(back, auraLook.Int("au"));
            rect.gameObject.AddComponent<AvatarIdleMotion>();
            return rect;
        }

        /// <summary>One of ten painterly portraits for the selected male/female creator template.</summary>
        private static Sprite PresetIllustration(bool female, int preset)
        {
            var index = Mathf.Clamp(preset, 0, 9);
            var key = "template_" + (female ? "female_" : "male_") + index;
            if (IllustrationCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("Characters/PortraitTemplates" + (female ? "Female" : "Male"));
            if (texture == null) return Illustration("PortraitIllustration" + (female ? "Female" : "Male"));
            texture.filterMode = FilterMode.Bilinear;
            const int columns = 5, rows = 2;
            var cellWidth = texture.width / (float)columns;
            var cellHeight = texture.height / (float)rows;
            var column = index % columns;
            var rowFromTop = index / columns;
            var rect = new Rect(column * cellWidth, texture.height - (rowFromTop + 1) * cellHeight, cellWidth, cellHeight);
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, 0f), 100f);
            sprite.name = key;
            IllustrationCache[key] = sprite;
            return sprite;
        }

        /// <summary>The hand-painted full-body template selected for live combat figures.</summary>
        internal static Sprite CombatIllustration(LookSpec look)
        {
            var female = look != null && look.Get("g", "m") == "f";
            var preset = look != null ? look.Int("preset", -1) : -1;
            if (preset < 0) preset = look != null ? look.Int("template", 0) : 0;
            return PresetIllustration(female, preset);
        }

        /// <summary>Updates the displayed template and matching colored aura after a creator choice.</summary>
        public static void RefreshIllustration(RectTransform avatar, LookSpec look, float auraStrength = .8f)
        {
            if (avatar == null || look == null) return;
            var female = look.Get("g", "m") == "f";
            var figure = avatar.Find("Illustration")?.GetComponent<Image>();
            var selected = look.Int("preset", -1);
            if (selected < 0) selected = look.Int("template", 0);
            if (figure != null) figure.sprite = PresetIllustration(female, selected);
            if (figure is AnimatedPortraitImage animatedFigure) animatedFigure.SetAppearance(look);
            var aura = avatar.Find("AuraBack")?.GetComponent<Image>();
            if (aura != null)
            {
                aura.enabled = look.Int("au") > 0 && aura.sprite != null;
                var color = HeroSprites.ParseColor(look.Get("auc", "#8fe0ff"), new Color32(143, 224, 255, 255));
                aura.color = new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(auraStrength * .72f)));
            }
            avatar.GetComponent<IllustrationAuraMotion>()?.Set(aura, look.Int("au"));
        }

        private static Sprite Illustration(string name)
        {
            if (IllustrationCache.TryGetValue(name, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("Characters/" + name);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, 0f), 100f);
            sprite.name = name;
            IllustrationCache[name] = sprite;
            return sprite;
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

    /// <summary>
    /// Draws an illustrated figure with soft atlas edges and restrained, continuous
    /// movement in the hair, shoulders and robe. The face stays rigid so the portrait
    /// does not stretch while the rest of the figure breathes or reacts in combat.
    /// </summary>
    internal sealed class AnimatedPortraitImage : Image
    {
        private FighterAction motion = FighterAction.Idle;
        private float progress;
        private float direction = 1f;
        private int bodyShape = 1;
        private bool femaleAppearance;
        private Color skinTone = Color.white;
        private bool integratedFace;
        private float eyeSize, browLift, noseWidth, mouthWidth;

        public void SetFaceCustomization(LookSpec look)
        {
            integratedFace = true;
            eyeSize = Mathf.Clamp(look.Int("ey"), 0, 7) * .018f;
            browLift = Mathf.Clamp(look.Int("br"), 0, 4) * .001f;
            noseWidth = Mathf.Clamp(look.Int("no"), 0, 3) * .025f;
            mouthWidth = Mathf.Clamp(look.Int("mo"), 0, 4) * .025f;
            SetVerticesDirty();
        }

        public void SetAppearance(LookSpec look)
        {
            bodyShape = Mathf.Clamp(look != null ? look.Int("bo", 1) : 1, 0, 3);
            femaleAppearance = look != null && look.Get("g", "m") == "f";
            var skin = HeroSprites.ParseColor(look != null ? look.Get("sk", "#f0d2b4") : "#f0d2b4",
                new Color32(240, 210, 180, 255));
            skinTone = new Color(Mathf.Clamp(skin.r / .94f, .45f, 1.1f),
                Mathf.Clamp(skin.g / .82f, .42f, 1.1f), Mathf.Clamp(skin.b / .71f, .4f, 1.1f), 1f);
            SetVerticesDirty();
        }

        public void SetMotion(FighterAction state, float actionProgress, bool faceRight)
        {
            motion = state;
            progress = Mathf.Clamp01(actionProgress);
            direction = faceRight ? 1f : -1f;
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            if (isActiveAndEnabled) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            if (vh.currentVertCount != 4) return;

            var corners = new UIVertex[4];
            var minX = float.MaxValue; var maxX = float.MinValue;
            var minY = float.MaxValue; var maxY = float.MinValue;
            for (var i = 0; i < 4; i++)
            {
                vh.PopulateUIVertex(ref corners[i], i);
                minX = Mathf.Min(minX, corners[i].position.x);
                maxX = Mathf.Max(maxX, corners[i].position.x);
                minY = Mathf.Min(minY, corners[i].position.y);
                maxY = Mathf.Max(maxY, corners[i].position.y);
            }
            if (maxX - minX < .01f || maxY - minY < .01f) return;

            UIVertex bottomLeft = default, bottomRight = default, topLeft = default, topRight = default;
            var midX = (minX + maxX) * .5f;
            var midY = (minY + maxY) * .5f;
            foreach (var corner in corners)
            {
                if (corner.position.x < midX)
                {
                    if (corner.position.y < midY) bottomLeft = corner;
                    else topLeft = corner;
                }
                else
                {
                    if (corner.position.y < midY) bottomRight = corner;
                    else topRight = corner;
                }
            }

            var columns = integratedFace ? 64 : 24;
            var rows = integratedFace ? 80 : 28;
            var time = Time.unscaledTime;
            var width = maxX - minX;
            var height = maxY - minY;
            var actionWave = Mathf.Sin(Mathf.PI * progress);
            var build = bodyShape == 0 ? .88f : bodyShape == 2 ? 1.08f : bodyShape == 3 ? 1.17f : 1f;
            var step = Mathf.Sin(time * 9.5f);
            var leftArmAngle = Mathf.Sin(time * 1.2f) * 1.4f;
            var rightArmAngle = -leftArmAngle;
            var leftLegAngle = 0f;
            var rightLegAngle = 0f;
            if (motion == FighterAction.Walk)
            {
                leftArmAngle -= step * 8f;
                rightArmAngle += step * 8f;
                leftLegAngle = step * 5f;
                rightLegAngle = -step * 5f;
            }
            else if (motion == FighterAction.Attack)
            {
                leftArmAngle -= actionWave * 9f;
                rightArmAngle += direction * actionWave * 27f;
                leftLegAngle = -actionWave * 3f;
                rightLegAngle = actionWave * 4f;
            }
            else if (motion == FighterAction.Cast)
            {
                leftArmAngle -= actionWave * 23f;
                rightArmAngle += actionWave * 23f;
            }
            else if (motion == FighterAction.Hurt || motion == FighterAction.Down)
            {
                leftArmAngle -= actionWave * 15f;
                rightArmAngle += actionWave * 15f;
            }
            vh.Clear();
            for (var row = 0; row <= rows; row++)
            {
                var v = row / (float)rows;
                for (var col = 0; col <= columns; col++)
                {
                    var u = col / (float)columns;
                    var lower = Mix(bottomLeft, bottomRight, u);
                    var upper = Mix(topLeft, topRight, u);
                    var vertex = Mix(lower, upper, v);

                    // A tiny torso breath and a delayed flow in the loose hair and hem.
                    var torso = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .25f) / .36f));
                    var hair = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .64f) / .32f));
                    var hem = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .06f) / .42f));
                    var x = width * (.006f * torso * Mathf.Sin(time * .75f + v * .4f)
                        + .008f * hair * Mathf.Sin(time * 1.05f + v * 2.5f)
                        + .009f * hem * Mathf.Sin(time * .9f + u * 3f));
                    var y = height * .003f * torso * Mathf.Sin(time * 1.4f);
                    x += (u - .5f) * width * .007f * torso * Mathf.Sin(time * 1.4f);

                    if (motion == FighterAction.Walk)
                    {
                        x += width * .014f * hem * Mathf.Sin(time * 9.5f + u * 2f);
                        y += height * .008f * hem * Mathf.Sin(time * 9.5f + u * 2f);
                    }
                    else if (motion == FighterAction.Attack)
                    {
                        x += direction * width * actionWave * (.025f * torso - .014f * hem);
                        y -= height * actionWave * .006f * torso;
                    }
                    else if (motion == FighterAction.Cast)
                    {
                        x += width * .013f * hair * actionWave * Mathf.Sin(time * 7f + v * 3f);
                        y += height * .009f * torso * actionWave;
                    }
                    else if (motion == FighterAction.Hurt)
                    {
                        x -= direction * width * .016f * torso * actionWave;
                    }
                    vertex.position += new Vector3(x, y, 0f);

                    // Move both painted sleeves and the hem around their own
                    // shoulder and hip regions. Smooth weights keep the torso
                    // joined to the moving limbs, with no cutout socket seams.
                    var armBand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .29f) / .15f))
                        * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((.82f - v) / .15f));
                    var leftArmWeight = armBand * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((.53f - u) / .20f));
                    var rightArmWeight = armBand * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - .47f) / .20f));
                    var legBand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((.47f - v) / .18f));
                    var leftLegWeight = legBand * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((.53f - u) / .18f));
                    var rightLegWeight = legBand * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - .47f) / .18f));
                    var point = new Vector2(vertex.position.x, vertex.position.y);
                    point += (RotateAround(point, new Vector2(minX + width * .34f, minY + height * .72f), leftArmAngle) - point) * leftArmWeight;
                    point += (RotateAround(point, new Vector2(minX + width * .66f, minY + height * .72f), rightArmAngle) - point) * rightArmWeight;
                    point += (RotateAround(point, new Vector2(minX + width * .43f, minY + height * .43f), leftLegAngle) - point) * leftLegWeight;
                    point += (RotateAround(point, new Vector2(minX + width * .57f, minY + height * .43f), rightLegAngle) - point) * rightLegWeight;
                    vertex.position = new Vector3(point.x, point.y, vertex.position.z);

                    // Keep the painted face unchanged while widening or
                    // narrowing the shoulders, waist and robe for body choices.
                    var bodyWeight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - .10f) / .18f))
                        * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((.79f - v) / .17f));
                    vertex.position.x += (u - .5f) * width * (build - 1f) * bodyWeight;
                    if (integratedFace)
                    {
                        var center = femaleAppearance ? .554f : .595f;
                        var eyeY = femaleAppearance ? .840f : .824f;
                        float Weight(float cx, float cy, float rx, float ry)
                        {
                            var dx = (u - cx) / rx; var dy = (v - cy) / ry;
                            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(1 - dx * dx - dy * dy));
                        }
                        // Change proportions in the same mesh; no independently moving face cutouts.
                        for (var side = -1; side <= 1; side += 2)
                        {
                            var eyeX = center + side * .034f;
                            var eyeWeight = Weight(eyeX, eyeY, .039f, .025f);
                            vertex.position.x += (u - eyeX) * width * eyeSize * eyeWeight;
                            vertex.position.y += (v - eyeY) * height * eyeSize * eyeWeight;
                            vertex.position.y += height * browLift * Weight(eyeX, eyeY + .019f, .04f, .015f);
                        }
                        vertex.position.x += (u - center) * width * noseWidth * Weight(center, .805f, .03f, .025f);
                        vertex.position.x += (u - center) * width * mouthWidth * Weight(center, .778f, .037f, .02f);
                    }

                    // Atlas figures reach their cell borders. Fade those last pixels
                    // into the parchment instead of showing a straight cut seam.
                    var left = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / .07f));
                    var right = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / .13f));
                    var tint = vertex.color;
                    tint.a = (byte)Mathf.RoundToInt(tint.a * Mathf.Min(left, right));
                    var faceX = integratedFace ? (femaleAppearance ? .554f : .595f) : (femaleAppearance ? .56f : .63f);
                    var faceDx = (u - faceX) / .105f;
                    var faceDy = (v - .83f) / .095f;
                    var faceDistance = faceDx * faceDx + faceDy * faceDy;
                    var skinWeight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - faceDistance)) * .85f;
                    var skinColor = Color.Lerp(Color.white, skinTone, skinWeight);
                    tint.r = (byte)Mathf.Clamp(Mathf.RoundToInt(tint.r * skinColor.r), 0, 255);
                    tint.g = (byte)Mathf.Clamp(Mathf.RoundToInt(tint.g * skinColor.g), 0, 255);
                    tint.b = (byte)Mathf.Clamp(Mathf.RoundToInt(tint.b * skinColor.b), 0, 255);
                    vertex.color = tint;
                    vh.AddVert(vertex);
                }
            }
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
            {
                var a = row * (columns + 1) + col;
                var b = a + columns + 1;
                vh.AddTriangle(a, b, b + 1);
                vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        private static UIVertex Mix(UIVertex from, UIVertex to, float t)
        {
            var vertex = from;
            vertex.position = Vector3.Lerp(from.position, to.position, t);
            vertex.uv0 = Vector4.Lerp(from.uv0, to.uv0, t);
            vertex.color = Color32.Lerp(from.color, to.color, t);
            return vertex;
        }

        private static Vector2 RotateAround(Vector2 point, Vector2 pivot, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sine = Mathf.Sin(radians);
            var cosine = Mathf.Cos(radians);
            var offset = point - pivot;
            return pivot + new Vector2(offset.x * cosine - offset.y * sine,
                offset.x * sine + offset.y * cosine);
        }
    }

    /// <summary>Slowly turns and breathes the painted aura around an illustrated portrait.</summary>
    public sealed class IllustrationAuraMotion : MonoBehaviour
    {
        public Image Aura;
        private Color baseColor;
        private int style;

        public void Set(Image aura, int auraStyle)
        {
            Aura = aura;
            style = auraStyle;
            if (Aura != null) baseColor = Aura.color;
        }

        private void LateUpdate()
        {
            if (Aura == null || !Aura.enabled || style <= 0) return;
            var t = Time.unscaledTime;
            var phase = t * (style == 2 ? 1.25f : style == 3 ? .8f : .42f);
            var scale = 1f + Mathf.Sin(phase) * .014f;
            Aura.rectTransform.localScale = new Vector3(scale, scale, 1f);
            Aura.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase * .53f) * (style == 3 ? 2.4f : 1.2f));
            var color = baseColor;
            color.a *= .82f + .18f * Mathf.Sin(phase * 1.4f);
            Aura.color = color;
        }

        private void OnDisable()
        {
            if (Aura == null) return;
            Aura.rectTransform.localScale = Vector3.one;
            Aura.rectTransform.localRotation = Quaternion.identity;
            Aura.color = baseColor;
        }
    }
}
