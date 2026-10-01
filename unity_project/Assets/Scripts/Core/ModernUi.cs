using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Procedural, resolution-independent primitives for the account screens:
    /// anti-aliased rounded shapes, soft shadows, stroke icons and bundled fonts.
    /// Everything is generated once and cached, so no extra texture assets are needed.
    /// </summary>
    internal static class ModernUi
    {
        private const float ShapeRadiusPx = 32f;
        private const float ShadowRadiusPx = 24f;
        private const float ShadowBlurPx = 40f;
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Font Regular => LoadFont("BeVietnamPro-Regular");
        public static Font Medium => LoadFont("BeVietnamPro-Medium");
        public static Font SemiBold => LoadFont("BeVietnamPro-SemiBold");
        public static Font Bold => LoadFont("BeVietnamPro-Bold");
        public static Font Display => LoadFont("PlayfairDisplaySC-Bold");

        public static Font LoadFont(string name)
        {
            if (Fonts.TryGetValue(name, out var cached) && cached != null) return cached;
            var font = Resources.Load<Font>("Fonts/" + name);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Fonts[name] = font;
            return font;
        }

        /// <summary>Solid rounded rectangle. Tint with Image.color.</summary>
        public static void Fill(Image image, float radius)
        {
            image.sprite = Cached("fill", () => BuildShape(ShapeRadiusPx, 0f, 0f));
            Slice(image, ShapeRadiusPx / Mathf.Max(1f, radius));
        }

        /// <summary>Rounded outline with a stroke width in canvas units.</summary>
        public static void Ring(Image image, float radius, float stroke)
        {
            var multiplier = ShapeRadiusPx / Mathf.Max(1f, radius);
            var strokePx = Mathf.Max(1f, Mathf.Round(stroke * multiplier * 4f) / 4f);
            image.sprite = Cached("ring:" + strokePx, () => BuildShape(ShapeRadiusPx, strokePx, 0f));
            Slice(image, multiplier);
            image.fillCenter = true;
        }

        /// <summary>
        /// Soft blurred shape drawn behind <paramref name="like"/>. Returns the shadow image,
        /// which is inserted directly below the target in draw order.
        /// </summary>
        public static Image Soft(RectTransform like, float radius, Color color, Vector2 offset, float spread = 0f)
        {
            var obj = new GameObject(like.name + "_Soft", typeof(RectTransform), typeof(Image));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(like.parent, false);
            rect.SetSiblingIndex(like.GetSiblingIndex());
            var multiplier = ShadowRadiusPx / Mathf.Max(1f, radius);
            var pad = (ShadowBlurPx + 2f) / multiplier + spread;
            rect.anchorMin = like.anchorMin;
            rect.anchorMax = like.anchorMax;
            rect.pivot = like.pivot;
            rect.anchoredPosition = like.anchoredPosition + offset;
            rect.sizeDelta = like.sizeDelta + Vector2.one * pad * 2f;
            var image = obj.GetComponent<Image>();
            image.sprite = Cached("soft", () => BuildShape(ShadowRadiusPx, 0f, ShadowBlurPx));
            Slice(image, multiplier);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Sprite Icon(string name) => Cached("icon:" + name, () => BuildIcon(name));

        private static void Slice(Image image, float multiplier)
        {
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = multiplier;
        }

        private static Sprite Cached(string key, Func<Sprite> build)
        {
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = build();
            Sprites[key] = sprite;
            return sprite;
        }

        private static Sprite BuildShape(float radius, float stroke, float blur)
        {
            var pad = Mathf.Ceil(blur) + 2f;
            var size = Mathf.RoundToInt((radius + pad) * 2f + 2f);
            var half = size * .5f;
            var box = new Vector2(half - pad, half - pad);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var p = new Vector2(x + .5f - half, y + .5f - half);
                var d = RoundBox(p, box, radius);
                float alpha;
                if (blur > 0f)
                {
                    var t = Mathf.Clamp01((d + blur * .25f) / (blur * 1.25f));
                    alpha = (1f - t) * (1f - t) * (1f - t * .35f);
                }
                else if (stroke > 0f) alpha = Mathf.Clamp01(.5f - (Mathf.Abs(d + stroke * .5f) - stroke * .5f));
                else alpha = Mathf.Clamp01(.5f - d);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            var border = radius + pad;
            return CreateSprite(pixels, size, "TuTien_Shape_" + radius + "_" + stroke + "_" + blur, new Vector4(border, border, border, border));
        }

        private static Sprite BuildIcon(string name)
        {
            const int size = 96;
            const float unit = size / 24f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var p = new Vector2((x + .5f) / unit, 24f - (y + .5f) / unit);
                var alpha = Mathf.Clamp01(.5f - IconDistance(name, p) * unit);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            return CreateSprite(pixels, size, "TuTien_Icon_" + name, Vector4.zero);
        }

        private static Sprite CreateSprite(Color32[] pixels, int size, string name, Vector4 border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // ---- Signed distance helpers (24-unit design grid, y grows downward) ----
        private const float Stroke = 1f;

        private static float IconDistance(string name, Vector2 p)
        {
            switch (name)
            {
                case "user":
                    return User(p, 12f);
                case "userPlus":
                    return Mathf.Min(User(p, 9.5f), Mathf.Min(Segment(p, new Vector2(19.5f, 8f), new Vector2(19.5f, 14f)), Segment(p, new Vector2(16.5f, 11f), new Vector2(22.5f, 11f))));
                case "lock":
                {
                    var body = Mathf.Abs(RoundBox(p - new Vector2(12f, 16.5f), new Vector2(8.5f, 5.5f), 2f)) - Stroke;
                    var shackle = Mathf.Max(RingStroke(p, new Vector2(12f, 7.5f), 4.5f), p.y - 7.5f);
                    var legs = Mathf.Min(Segment(p, new Vector2(7.5f, 7.5f), new Vector2(7.5f, 11f)), Segment(p, new Vector2(16.5f, 7.5f), new Vector2(16.5f, 11f)));
                    return Mathf.Min(body, Mathf.Min(shackle, legs));
                }
                case "eye":
                    return Eye(p);
                case "eyeOff":
                    return Mathf.Min(Eye(p), Segment(p, new Vector2(3.5f, 3.5f), new Vector2(20.5f, 20.5f)));
                case "compass":
                {
                    var ring = RingStroke(p, new Vector2(12f, 12f), 9.5f);
                    var needle = Polygon(p, new[] { new Vector2(16.2f, 7.8f), new Vector2(14f, 14f), new Vector2(7.8f, 16.2f), new Vector2(10f, 10f) });
                    return Mathf.Min(ring, needle);
                }
                case "mail":
                {
                    var box = Mathf.Abs(RoundBox(p - new Vector2(12f, 12f), new Vector2(9.5f, 7f), 2f)) - Stroke;
                    var flap = Mathf.Min(Segment(p, new Vector2(3f, 7.5f), new Vector2(12f, 13.2f)), Segment(p, new Vector2(12f, 13.2f), new Vector2(21f, 7.5f)));
                    return Mathf.Min(box, flap);
                }
                case "arrowRight":
                    return Mathf.Min(Segment(p, new Vector2(5f, 12f), new Vector2(19f, 12f)), Mathf.Min(Segment(p, new Vector2(13f, 6f), new Vector2(19f, 12f)), Segment(p, new Vector2(13f, 18f), new Vector2(19f, 12f))));
                case "arrowLeft":
                    return Mathf.Min(Segment(p, new Vector2(5f, 12f), new Vector2(19f, 12f)), Mathf.Min(Segment(p, new Vector2(11f, 6f), new Vector2(5f, 12f)), Segment(p, new Vector2(11f, 18f), new Vector2(5f, 12f))));
                case "refresh":
                {
                    var arc = Mathf.Max(RingStroke(p, new Vector2(12f, 12f), 8f), -Mathf.Max(12f - p.x, p.y - 12f) - .2f);
                    var head = Mathf.Min(Segment(p, new Vector2(20f, 5f), new Vector2(20f, 10f)), Segment(p, new Vector2(20f, 10f), new Vector2(15f, 10f)));
                    return Mathf.Min(arc, head);
                }
                case "spinner":
                    return Mathf.Max(RingStroke(p, new Vector2(12f, 12f), 9f, 1.25f), -Mathf.Max(12f - p.x, p.y - 12f));
                case "alert":
                    return Mathf.Min(RingStroke(p, new Vector2(12f, 12f), 9.5f), Mathf.Min(Segment(p, new Vector2(12f, 7.5f), new Vector2(12f, 12.5f)), Circle(p, new Vector2(12f, 16.3f), 1.25f)));
                default:
                    return Circle(p, new Vector2(12f, 12f), 4f);
            }
        }

        private static float User(Vector2 p, float cx)
        {
            var head = RingStroke(p, new Vector2(cx, 8f), 3.8f);
            var shoulders = Mathf.Max(Mathf.Abs(RoundBox(p - new Vector2(cx, 20.2f), new Vector2(7f, 5f), 4.6f)) - Stroke, p.y - 21f);
            return Mathf.Min(head, shoulders);
        }

        private static float Eye(Vector2 p)
        {
            // Almond outline made from two arcs that meet at the eye corners.
            const float k = 3.643f;
            const float r = 10.643f;
            var upper = Mathf.Max(RingStroke(p, new Vector2(12f, 12f + k), r), p.y - 12f);
            var lower = Mathf.Max(RingStroke(p, new Vector2(12f, 12f - k), r), 12f - p.y);
            var pupil = RingStroke(p, new Vector2(12f, 12f), 2.8f);
            return Mathf.Min(Mathf.Min(upper, lower), pupil);
        }

        private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        private static float RingStroke(Vector2 p, Vector2 c, float r, float halfWidth = Stroke) => Mathf.Abs((p - c).magnitude - r) - halfWidth;

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a;
            var ba = b - a;
            var h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - Stroke;
        }

        private static float RoundBox(Vector2 p, Vector2 halfSize, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + Vector2.one * radius;
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Polygon(Vector2 p, Vector2[] points)
        {
            // Convex polygon: max of the signed distances to every edge line.
            var area = 0f;
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                area += a.x * b.y - b.x * a.y;
            }
            var sign = area > 0f ? 1f : -1f;
            var d = float.MinValue;
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                var edge = (b - a).normalized;
                var normal = new Vector2(edge.y, -edge.x) * sign;
                d = Mathf.Max(d, Vector2.Dot(p - a, normal));
            }
            return d;
        }
    }

    /// <summary>Two-colour vertex gradient for any uGUI graphic (sliced images and text included).</summary>
    [DisallowMultipleComponent]
    internal sealed class UiGradient : BaseMeshEffect
    {
        public Color from = Color.white;
        public Color to = Color.white;
        public bool horizontal;
        public bool mirror;

        public static UiGradient Apply(Graphic graphic, Color from, Color to, bool horizontal = false, bool mirror = false)
        {
            var gradient = graphic.GetComponent<UiGradient>();
            if (gradient == null) gradient = graphic.gameObject.AddComponent<UiGradient>();
            gradient.from = from;
            gradient.to = to;
            gradient.horizontal = horizontal;
            gradient.mirror = mirror;
            graphic.SetVerticesDirty();
            return gradient;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var vertex = new UIVertex();
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                var value = horizontal ? vertex.position.x : vertex.position.y;
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
            var span = Mathf.Max(.0001f, max - min);
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                var t = ((horizontal ? vertex.position.x : vertex.position.y) - min) / span;
                if (!horizontal) t = 1f - t; // "from" is the top edge
                if (mirror) t = 1f - Mathf.Abs(t * 2f - 1f);
                vertex.color = (Color)vertex.color * Color.Lerp(from, to, t);
                vh.SetUIVertex(vertex, i);
            }
        }
    }

    /// <summary>Keeps a fixed-size card inside its parent on small or unusual screens.</summary>
    [ExecuteAlways]
    internal sealed class UiFitScale : UIBehaviour
    {
        public RectTransform target;
        public Vector2 size;
        public float margin = .97f;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        protected override void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            if (target == null) return;
            var rect = ((RectTransform)transform).rect;
            if (rect.width <= 1f || rect.height <= 1f || size.x <= 0f || size.y <= 0f) return;
            var scale = Mathf.Min(1f, rect.width * margin / size.x, rect.height * margin / size.y);
            target.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Fade-and-rise entrance. Only animates in play mode, so editor captures show the final state.</summary>
    internal sealed class UiIntro : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 target;
        private Vector2 offset;
        private float start;
        private float duration;

        public static void Play(RectTransform rect, Vector2 offset, float duration = .42f, float delay = 0f)
        {
            if (!Application.isPlaying || rect == null) return;
            var intro = rect.gameObject.AddComponent<UiIntro>();
            intro.rect = rect;
            intro.group = rect.GetComponent<CanvasGroup>();
            if (intro.group == null) intro.group = rect.gameObject.AddComponent<CanvasGroup>();
            intro.target = rect.anchoredPosition;
            intro.offset = offset;
            intro.duration = duration;
            intro.start = Time.unscaledTime + delay;
            intro.group.alpha = 0f;
            rect.anchoredPosition = intro.target + offset;
        }

        private void Update()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = eased;
            rect.anchoredPosition = Vector2.LerpUnclamped(target + offset, target, eased);
            if (t >= 1f) Destroy(this);
        }
    }

    /// <summary>Short horizontal shake used to signal a rejected form.</summary>
    internal sealed class UiShake : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 origin;
        private float start;

        public static void Play(RectTransform rect)
        {
            if (!Application.isPlaying || rect == null || rect.GetComponent<UiShake>() != null) return;
            var shake = rect.gameObject.AddComponent<UiShake>();
            shake.rect = rect;
            shake.origin = rect.anchoredPosition;
            shake.start = Time.unscaledTime;
        }

        private void Update()
        {
            var t = (Time.unscaledTime - start) / .32f;
            if (t >= 1f) { rect.anchoredPosition = origin; Destroy(this); return; }
            rect.anchoredPosition = origin + Vector2.right * Mathf.Sin(t * Mathf.PI * 6f) * 9f * (1f - t);
        }
    }

    /// <summary>Slides an element from an offset to its resting position (used by the tab indicator).</summary>
    internal sealed class UiSlide : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 target;
        private Vector2 offset;
        private float start;

        public static void Play(RectTransform rect, Vector2 offset)
        {
            if (!Application.isPlaying || rect == null) return;
            var slide = rect.gameObject.AddComponent<UiSlide>();
            slide.rect = rect;
            slide.target = rect.anchoredPosition;
            slide.offset = offset;
            slide.start = Time.unscaledTime;
            rect.anchoredPosition = slide.target + offset;
        }

        private void Update()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - start) / .24f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            rect.anchoredPosition = Vector2.LerpUnclamped(target + offset, target, eased);
            if (t >= 1f) Destroy(this);
        }
    }

    /// <summary>Gives buttons a subtle press-down scale.</summary>
    internal sealed class UiPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Selectable selectable;
        private float target = 1f;

        private void Awake() => selectable = GetComponent<Selectable>();
        public void OnPointerDown(PointerEventData eventData) { if (selectable == null || selectable.IsInteractable()) target = .965f; }
        public void OnPointerUp(PointerEventData eventData) => target = 1f;
        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        private void Update()
        {
            if (!Application.isPlaying) return;
            var scale = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.4f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Continuously rotates a spinner icon while active.</summary>
    internal sealed class UiSpinner : MonoBehaviour
    {
        private void Update()
        {
            if (Application.isPlaying) transform.Rotate(0f, 0f, -300f * Time.unscaledDeltaTime);
        }
    }

    /// <summary>Animated focus and error styling for a text field.</summary>
    internal sealed class UiInputFocus : MonoBehaviour
    {
        public InputField input;
        public Image border;
        public Image glow;
        public Image icon;
        public Color borderIdle;
        public Color borderFocus;
        public Color borderError;
        public Color iconIdle;
        public Color iconFocus;
        private float focus;
        private float errorUntil;

        public void Flag() => errorUntil = Time.unscaledTime + 2.4f;

        public void Refresh(float focusAmount)
        {
            var error = Time.unscaledTime < errorUntil;
            var borderColor = Color.Lerp(borderIdle, borderFocus, focusAmount);
            if (error) borderColor = borderError;
            if (border != null) border.color = borderColor;
            if (glow != null)
            {
                var glowColor = error ? borderError : borderFocus;
                glowColor.a = (error ? .28f : .22f) * Mathf.Max(focusAmount, error ? 1f : 0f);
                glow.color = glowColor;
            }
            if (icon != null) icon.color = error ? borderError : Color.Lerp(iconIdle, iconFocus, focusAmount);
        }

        private void Update()
        {
            if (input == null) return;
            var target = input.isFocused ? 1f : 0f;
            focus = Application.isPlaying ? Mathf.MoveTowards(focus, target, Time.unscaledDeltaTime * 7f) : target;
            Refresh(focus);
        }
    }
}
