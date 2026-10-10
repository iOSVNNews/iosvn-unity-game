using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Plays one effect sheet: once, or looping for a lifetime, then removes itself. Neighbouring
    /// frames are cross-faded, so a 12-frame sheet moves as smoothly as the screen refreshes, and
    /// one-shot effects arrive with a small punch of scale.
    /// </summary>
    internal sealed class FxPlayer : MonoBehaviour
    {
        public Sprite[] Frames;
        public float Fps = 24f;
        public float Delay;
        public float Life;                 // > 0: loop for this long; 0: play the frames once
        public bool Frozen;                // previews: stay on StartFrame
        public int StartFrame;
        public float Punch = .12f;         // extra scale at the first instant of a one-shot
        public float Spin;                 // degrees per second (law wheels turn slowly)
        public float Pulse = .07f;
        public int HoldFrame = -1;         // >= 0: show only this frame for the whole life (layers of a wheel)
        public Action Done;
        private Image image, blend;
        private float time;
        private Vector3 baseScale;
        private bool started;

        private void Begin()
        {
            if (started) return;
            started = true;
            image = GetComponent<Image>();
            baseScale = transform.localScale;
            var go = new GameObject("Blend", typeof(RectTransform), typeof(Image));
            var r = (RectTransform)go.transform;
            r.SetParent(transform, false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            blend = go.GetComponent<Image>();
            blend.raycastTarget = false;
            blend.enabled = false;
        }

        private void Start() { Begin(); Show(); }

        private void Update()
        {
            if (!Frozen) time += Time.unscaledDeltaTime;
            Show();
        }

        private void Show()
        {
            Begin();
            if (Frames == null || Frames.Length == 0 || image == null) { Finish(); return; }
            var t = time - Delay;
            if (t < 0) { image.enabled = false; blend.enabled = false; return; }
            var position = StartFrame + t * Fps;
            var index = (int)position;
            var frac = Frozen ? 0f : position - index;
            if (HoldFrame >= 0) { index = Mathf.Min(HoldFrame, Frames.Length - 1); frac = 0f; }
            var alpha = 1f;
            int next;
            if (Life > 0)
            {
                if (t >= Life && !Frozen) { Finish(); return; }
                index %= Frames.Length;
                next = HoldFrame >= 0 ? index : (index + 1) % Frames.Length;
                alpha = Mathf.Clamp01(t / .1f) * Mathf.Clamp01((Life - t) / .18f);
                if (Frozen) alpha = 1f;
                if (!Frozen)
                {
                    var pulse = 1f + Pulse * Mathf.Sin(t * (HoldFrame >= 0 ? 2.2f : 4.2f));
                    transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, 1f);
                }
            }
            else
            {
                if (index >= Frames.Length)
                {
                    if (!Frozen) { Finish(); return; }
                    index = Frames.Length - 1;
                }
                next = Mathf.Min(index + 1, Frames.Length - 1);
                var k = 1f + Punch * (1f - Mathf.SmoothStep(0f, 1f, t / .12f));
                transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, 1f);
            }
            if (Spin != 0f) transform.localRotation = Quaternion.Euler(0, 0, Spin * t);
            var sprite = Frames[index];
            image.enabled = sprite != null;      // never show an Image without its sprite (it would draw a white box)
            image.sprite = sprite;
            // cross-fade: the current frame eases out while the next one eases in
            image.color = new Color(1, 1, 1, alpha * (1f - .6f * frac));
            var other = Frames[next];
            blend.enabled = other != null && next != index && frac > .01f;
            if (blend.enabled)
            {
                blend.sprite = other;
                blend.color = new Color(1, 1, 1, alpha * frac);
            }
        }

        private void Finish()
        {
            var done = Done;
            Done = null;
            done?.Invoke();
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }

    /// <summary>Moves an effect from A to B, then reports the arrival.</summary>
    internal sealed class FxMover : MonoBehaviour
    {
        public Vector2 From, To;
        public float Duration = .3f, Delay, Arc;
        public bool EaseIn;
        public Action Arrive;
        private float time;

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01((time - Delay) / Mathf.Max(.01f, Duration));
            var e = EaseIn ? t * t : t * t * (3 - 2 * t);
            var p = Vector2.Lerp(From, To, e);
            p.y += Mathf.Sin(t * Mathf.PI) * Arc;
            ((RectTransform)transform).anchoredPosition = p;
            if (time - Delay >= Duration)
            {
                var arrive = Arrive;
                Arrive = null;
                arrive?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Fades a graphic out and removes it (screen flashes, afterimages).</summary>
    internal sealed class FadeAway : MonoBehaviour
    {
        public float Duration = .2f;
        private Graphic graphic;
        private float start, from;

        private void Start() { graphic = GetComponent<Graphic>(); start = Time.unscaledTime; from = graphic != null ? graphic.color.a : 0f; }

        private void Update()
        {
            var t = (Time.unscaledTime - start) / Mathf.Max(.01f, Duration);
            if (graphic != null) { var c = graphic.color; c.a = from * (1f - t) * (1f - t); graphic.color = c; }
            if (t >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>Shared combat presentation helpers: effect spawning, element lookup, flash material.</summary>
    internal static class BattleFx
    {
        /// <summary>Canvas units per effect pixel (effects are drawn about as fine as the monsters).</summary>
        public const float PixelScale = 1f;
        private const float EffectVisualScale = .48f;

        /// <summary>How far above its centre an effect's ground line sits, in effect pixels.</summary>
        private static readonly Dictionary<string, float> GroundOffset = new Dictionary<string, float>
        {
            { "pillar", 152f }, { "giantsword", 142f }, { "quake", 58f }, { "rain", 112f }, { "vortex", 120f }, { "cast", 8.6f },
            { "lotus", 30.7f }, { "heal", 98f },
        };

        private static Material flash;
        private static bool flashTried;

        /// <summary>Draws a sprite as a flat silhouette in the Image colour (the font shader uses only the texture alpha).</summary>
        public static Material Flash
        {
            get
            {
                if (flash != null || flashTried) return flash;
                flashTried = true;
                Shader shader = null;
                try { shader = ModernUi.Regular != null && ModernUi.Regular.material != null ? ModernUi.Regular.material.shader : null; } catch (Exception) { }
                if (shader == null) shader = Shader.Find("GUI/Text Shader");
                if (shader != null) flash = new Material(shader) { name = "HitFlash" };
                return flash;
            }
        }

        public static Color ElementColor(string element)
        {
            switch (element)
            {
                case "moc": return new Color32(120, 214, 96, 255);
                case "thuy": return new Color32(90, 170, 240, 255);
                case "hoa": return new Color32(250, 140, 50, 255);
                case "tho": return new Color32(206, 150, 80, 255);
                case "phong": return new Color32(130, 224, 204, 255);
                case "loi": return new Color32(190, 140, 255, 255);
                case "bang": return new Color32(160, 214, 250, 255);
                case "ma": return new Color32(220, 60, 100, 255);
                case "thien": return new Color32(250, 226, 150, 255);
                default: return new Color32(240, 200, 96, 255);
            }
        }

        public static bool Has(string text, params string[] words)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var w in words) if (text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>The element a skill is drawn in: read from its name, else the caster's own element.</summary>
        public static string ElementOfSkill(string name, string fallback)
        {
            if (Has(name, "Hỏa", "Viêm", "Diễm", "Phần", "Dương", "Nhật")) return "hoa";
            if (Has(name, "Lôi", "Điện", "Sấm")) return "loi";
            if (Has(name, "Băng", "Hàn", "Tuyết", "Sương")) return "bang";
            if (Has(name, "Thủy", "Hải", "Lãng", "Triều")) return "thuy";
            if (Has(name, "Mộc", "Thảo", "Đằng", "Diệp", "Liên")) return "moc";
            if (Has(name, "Thổ", "Sơn", "Thạch", "Địa", "Nham")) return "tho";
            if (Has(name, "Ma", "Huyết", "Quỷ", "Âm", "Độc", "Sát", "Hồn", "U Minh")) return "ma";
            if (Has(name, "Thiên", "Quang", "Thánh", "Tiên", "Thần", "Tinh")) return "thien";
            if (Has(name, "Phong", "Vân", "Ảnh")) return "phong";
            if (Has(name, "Kim", "Kiếm", "Nhận", "Đao")) return "kim";
            return string.IsNullOrEmpty(fallback) ? "kim" : fallback;
        }

        public static uint Hash(string text)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var ch in text ?? string.Empty) { h ^= ch; h *= 16777619u; }
                return h;
            }
        }

        /// <summary>Spawns an effect centred on pos (arena coordinates).</summary>
        public static FxPlayer Spawn(RectTransform parent, string effect, string element, Vector2 pos, float scale = 1f, bool flip = false,
            float rotation = 0f, float delay = 0f, float life = 0f, float fps = 0f)
        {
            if (parent == null) return null;
            var frames = ArtSprites.Fx(effect, element);
            if (frames == null || !ArtSprites.FxInfo.TryGetValue(effect, out var info)) return null;
            var go = new GameObject("Fx_" + effect, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(info.w, info.h) * PixelScale * EffectVisualScale * scale;
            rect.anchoredPosition = pos;
            rect.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
            rect.localRotation = Quaternion.Euler(0, 0, rotation);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            var player = go.AddComponent<FxPlayer>();
            player.Frames = frames;
            player.Fps = fps > 0 ? fps : info.fps;
            player.Delay = delay;
            player.Life = life > 0 ? life : (info.loop ? .7f : 0f);
            player.Spin = effect == "halo" ? 9f : effect == "vortex" ? -22f : 0f;
            player.Pulse = effect == "halo" || effect == "vortex" || effect == "cast" ? .1f : .055f;
            return player;
        }

        /// <summary>Spawns an effect standing on a ground point (pillars, arrays, lotuses, falling swords).</summary>
        public static FxPlayer OnGround(RectTransform parent, string effect, string element, Vector2 ground, float scale = 1f, bool flip = false, float delay = 0f, float life = 0f)
        {
            GroundOffset.TryGetValue(effect, out var up);
            return Spawn(parent, effect, element, ground + new Vector2(0, up * PixelScale * EffectVisualScale * scale), scale, flip, 0f, delay, life);
        }

        /// <summary>A full-size empty layer of a battle scene (effects behind the fighters, the fighters, effects in front).</summary>
        public static RectTransform Layer(RectTransform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = Vector2.zero; layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            return layer;
        }

        /// <summary>Where a floating number may start: clear of the bars along the top, inside the screen, and
        /// under a number that appeared on the same spot a moment earlier.</summary>
        public static Vector2 FloatSpot(RectTransform layer, Vector2 at, ref Vector2 last, ref float lastTime)
        {
            var area = layer != null ? layer.rect : default;
            if (area.height > 0f) at.y = Mathf.Min(at.y, area.height * .5f - 420f);
            if (area.width > 680f) at.x = Mathf.Clamp(at.x, -area.width * .5f + 340f, area.width * .5f - 340f);
            if (Time.time - lastTime < .45f && Mathf.Abs(at.x - last.x) < 260f && Mathf.Abs(at.y - last.y) < 50f) at.y = last.y - 56f;
            last = at;
            lastTime = Time.time;
            return at;
        }

        /// <summary>Pháp luân: the wheel of law — a rim of trigrams turning one way, a hub of petals the other.</summary>
        public static FxPlayer[] Wheel(RectTransform parent, string element, Vector2 pos, float scale, float life, RectTransform behind)
        {
            var rim = Spawn(parent, "halo", element, pos, scale, false, 0f, 0f, life);
            var hub = Spawn(parent, "halo", element, pos, scale, false, 0f, 0f, life);
            if (rim != null) { rim.HoldFrame = 0; rim.Spin = 26f; Behind(rim, behind); }
            if (hub != null) { hub.HoldFrame = 1; hub.Spin = -70f; Behind(hub, behind); }
            return new[] { rim, hub };
        }

        /// <summary>Moves an effect to the layer behind the fighters (law wheels, ground arrays under the feet).</summary>
        public static FxPlayer Behind(FxPlayer fx, RectTransform back)
        {
            if (fx != null && back != null) fx.transform.SetParent(back, false);
            return fx;
        }

        /// <summary>A looping effect that flies from A to B (drawn travelling left, so it is turned to its heading).</summary>
        public static void Projectile(RectTransform parent, string effect, string element, Vector2 from, Vector2 to, float duration, float scale, float delay, float arc, Action arrive, bool easeIn = false)
        {
            var fx = Spawn(parent, effect, element, from, scale, false, 0f, delay, duration + delay + .05f);
            if (fx == null) { arrive?.Invoke(); return; }
            var d = to - from;
            if (d.x > 0)
            {
                // heading right: mirror instead of turning upside down, then tilt to the heading
                fx.transform.localScale = new Vector3(-1f, 1f, 1f);
                fx.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            else fx.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 180f);
            var mover = fx.gameObject.AddComponent<FxMover>();
            mover.From = from; mover.To = to; mover.Duration = duration; mover.Delay = delay; mover.Arc = arc; mover.Arrive = arrive; mover.EaseIn = easeIn;
        }

        public static void After(MonoBehaviour host, float delay, Action action)
        {
            if (host == null || !host.isActiveAndEnabled || !Application.isPlaying) return;
            host.StartCoroutine(Wait(delay, action));
        }

        private static IEnumerator Wait(float delay, Action action)
        {
            var end = Time.unscaledTime + delay;
            while (Time.unscaledTime < end) yield return null;
            action?.Invoke();
        }

        /// <summary>A white wash over the whole field that fades at once (big impacts).</summary>
        public static void ScreenFlash(RectTransform arena, Color color, float seconds = .18f)
        {
            if (arena == null || !Application.isPlaying) return;
            var image = InkUi.Simple(arena, "Flash", InkUi.White, color, Vector2.zero);
            image.rectTransform.anchorMin = Vector2.zero; image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = false;
            image.gameObject.AddComponent<FadeAway>().Duration = seconds;
        }
    }

    /// <summary>Where an attack is staged: who casts, who is hit, in which element.</summary>
    internal sealed class StageContext
    {
        public MonoBehaviour Host;
        public RectTransform Arena;
        public Func<Vector2> Caster, Target;       // feet positions in arena coordinates
        public float CasterHeight = 300f, TargetHeight = 300f;
        public string Element = "kim";
        public Action<float> Shake;                // camera kick 0..1
        public Action<float> Dim;                  // darken the field for this many seconds (great techniques)
        public RectTransform Back;                 // layer under the fighters (Arena is the layer over them)

        public Vector2 CasterCentre => Caster() + new Vector2(0, CasterHeight * .5f);
        public Vector2 TargetCentre => Target() + new Vector2(0, TargetHeight * .45f);
        public Vector2 TargetGround => Target() + new Vector2(0, 14f);
        public Vector2 CasterGround => Caster() + new Vector2(0, 14f);
        /// <summary>True when the target stands to the right of the caster (effects are drawn facing left).</summary>
        public bool Right => Target().x > Caster().x;

        public void Impact(float strength)
        {
            Shake?.Invoke(strength);
            if (strength >= .7f) BattleFx.ScreenFlash(Arena, new Color(1f, .97f, .9f, .16f + .16f * strength));
        }
    }

    /// <summary>
    /// Stages skills as cultivation set pieces. Every player skill gets its own choreography from its
    /// kind, the words in its name and its id: sword qi, flying swords, a colossal sword from the sky,
    /// dragons of energy, lotuses, talismans, giant palms, bagua arrays and pillars of heaven's wrath.
    /// Great techniques open the wheel of law behind the caster and darken the field. Every monster
    /// move has its own staging too, and each ultimate is a multi-stage finisher in its element.
    /// Each method returns the delay until the blow lands, so damage numbers can be timed to it.
    /// </summary>
    internal static class SkillStage
    {
        private static string Form(string name, string kind, uint h)
        {
            switch (kind)
            {
                case "buff": return "buff";
                case "shield": case "reflect": return "shield";
                case "heal": return "heal";
                case "mana": return "mana";
                case "escape": return "escape";
            }
            if (BattleFx.Has(name, "Long", "Rồng", "Giao")) return "dragon";
            if (BattleFx.Has(name, "Kiếm")) return "sword";
            if (BattleFx.Has(name, "Liên", "Sen")) return "lotus";
            if (BattleFx.Has(name, "Phù", "Chú")) return "talisman";
            if (BattleFx.Has(name, "Chưởng", "Ấn", "Quyền", "Thủ", "Chỉ")) return "palm";
            if (BattleFx.Has(name, "Trảm", "Đao", "Nhận", "Trảo")) return "slash";
            if (BattleFx.Has(name, "Trận", "Cấm", "Bát Quái", "Thái Cực")) return "array";
            if (BattleFx.Has(name, "Vũ", "Tiễn", "Châm")) return "rain";
            if (BattleFx.Has(name, "Toàn", "Quyển", "Bão")) return "vortex";
            if (BattleFx.Has(name, "Tỏa", "Trói", "Định", "Phược")) return "chain";
            if (BattleFx.Has(name, "Lôi", "Phạt", "Trụ", "Quang")) return "pillar";
            if (kind == "multi") return h % 2 == 0 ? "rain" : "sword";
            if (kind == "stun") return "chain";
            if (kind == "dot") return "vortex";
            return new[] { "proj", "slash", "pillar", "palm", "sword" }[h % 5];
        }

        /// <summary>The opening of a great technique: the wheel of law behind the caster, an array at the feet, the field darkens.</summary>
        private static void Open(StageContext c, string el, float seconds, float size = 1f)
        {
            c.Dim?.Invoke(seconds + .25f);
            BattleFx.Wheel(c.Arena, el, c.CasterCentre + new Vector2(0, c.CasterHeight * .12f), 1.3f * size, seconds + .2f, c.Back);
            BattleFx.Behind(BattleFx.OnGround(c.Arena, "cast", el, c.CasterGround, 1.35f * size, false, 0, seconds + .1f), c.Back);
        }

        /// <summary>A player's skill. The basic attack is kind "attack".</summary>
        public static float Player(StageContext c, string id, string name, string kind, bool big)
        {
            if (c?.Arena == null) return 0f;
            var h = BattleFx.Hash(id + "|" + name);
            var el = BattleFx.ElementOfSkill(name, c.Element);
            var right = c.Right;
            var size = (big ? 1.35f : 1.05f) + ((h >> 4) % 3) * .08f;
            var variant = (int)((h >> 8) % 3);
            var arena = c.Arena;
            if (kind == "attack")
            {
                BattleFx.Spawn(arena, "slash", el, c.TargetCentre + new Vector2(right ? -40 : 40, 0), 1.15f, right, variant * 16f - 16f, .08f);
                BattleFx.After(c.Host, .2f, () => BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1f));
                return .2f;
            }
            var form = Form(name, kind, h);
            var offensive = form != "buff" && form != "shield" && form != "heal" && form != "mana" && form != "escape";
            if (offensive && big) Open(c, el, .9f);
            else if (offensive) BattleFx.Behind(BattleFx.OnGround(arena, "cast", el, c.CasterGround, 1.05f, false, 0, .55f), c.Back);
            var lead = offensive && big ? .22f : 0f;        // great techniques gather for a moment first
            switch (form)
            {
                case "sword":
                {
                    if (big)
                    {
                        // ten thousand swords: a volley from behind the caster, then the colossal sword falls
                        for (var i = 0; i < 7; i++)
                        {
                            var spread = (i - 3) * 46f;
                            var from = c.CasterCentre + new Vector2(right ? -130f : 130f, 190f + Mathf.Abs(spread) * .3f + spread * .4f);
                            var to = c.TargetCentre + new Vector2((i - 3) * 22f, (i % 2) * 40f - 20f);
                            BattleFx.Projectile(arena, "sword", el, from, to, .22f, 1.35f, lead + i * .05f, 0, () => BattleFx.Spawn(arena, "hit", el, to, .8f), true);
                        }
                        BattleFx.OnGround(arena, "giantsword", el, c.TargetGround, 1.25f * size, false, lead + .32f);
                        BattleFx.After(c.Host, lead + .54f, () => c.Impact(1f));
                        return lead + .54f;
                    }
                    var count = 2 + (int)(h % 3) + (kind == "multi" ? 2 : 0);
                    for (var i = 0; i < count; i++)
                    {
                        var spread = (i - (count - 1) * .5f) * 70f;
                        var from = c.CasterCentre + new Vector2(right ? -100f : 100f, 160f + Mathf.Abs(spread) * .4f + spread * .5f);
                        var to = c.TargetCentre + new Vector2(0, spread * .25f);
                        var last = i == count - 1;
                        BattleFx.Projectile(arena, "sword", el, from, to, .22f, 1.3f * size, .06f + i * .07f, 0, () =>
                        {
                            BattleFx.Spawn(arena, "hit", el, to, .9f);
                            if (last) { BattleFx.Spawn(arena, "slash", el, c.TargetCentre, 1.2f * size, right); c.Impact(.4f); }
                        }, true);
                    }
                    return .28f + (count - 1) * .035f;
                }
                case "dragon":
                {
                    var from = c.CasterCentre + new Vector2(right ? -60 : 60, 60);
                    var through = c.TargetCentre + new Vector2(right ? 260 : -260, 30);
                    BattleFx.Projectile(arena, "dragon", el, from, through, .5f, 1.25f * size, lead + .05f, 50f, null);
                    BattleFx.After(c.Host, lead + .3f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.25f * size); c.Impact(big ? 1f : .6f); });
                    if (big) BattleFx.Projectile(arena, "dragon", el, from + new Vector2(0, 130), through + new Vector2(0, -60), .5f, 1.0f, lead + .2f, -40f, null);
                    return lead + .3f;
                }
                case "lotus":
                    BattleFx.OnGround(arena, "lotus", el, c.TargetGround, 1.3f * size, false, lead);
                    BattleFx.After(c.Host, lead + .32f, () => { BattleFx.OnGround(arena, "pillar", el, c.TargetGround, .9f * size); c.Impact(big ? .9f : .5f); });
                    return lead + .36f;
                case "talisman":
                {
                    var count = big ? 5 : 3;
                    for (var i = 0; i < count; i++)
                    {
                        var from = c.CasterCentre + new Vector2(right ? 50 : -50, 40 + (i - count / 2) * 46f);
                        var to = c.TargetCentre + new Vector2(0, (i - count / 2) * 26f);
                        var last = i == count - 1;
                        BattleFx.Projectile(arena, "talisman", el, from, to, .26f, 1.25f, lead + i * .06f, 30f - i * 12f, () =>
                        {
                            BattleFx.Spawn(arena, "hit", el, to, .9f);
                            if (last) { BattleFx.Spawn(arena, big ? "burst" : "chain", el, c.TargetCentre, 1.2f * size); c.Impact(big ? .9f : .45f); }
                        });
                    }
                    return lead + .28f + count * .05f;
                }
                case "palm":
                    BattleFx.Spawn(arena, "orb", el, c.CasterCentre + new Vector2(right ? 70 : -70, 30), .9f, false, 0, lead, .25f);
                    BattleFx.Spawn(arena, "palm", el, c.TargetCentre + new Vector2(0, big ? 40 : 0), (big ? 1.7f : 1.25f) * size, right, 0, lead + .14f);
                    BattleFx.After(c.Host, lead + .3f, () =>
                    {
                        BattleFx.Spawn(arena, "wave", el, c.TargetCentre, .9f * size, right);
                        if (big) BattleFx.OnGround(arena, "quake", el, c.TargetGround, 1.3f);
                        c.Impact(big ? 1f : .45f);
                    });
                    return lead + .3f;
                case "slash":
                    BattleFx.Spawn(arena, "slash", el, c.TargetCentre + new Vector2(right ? -50 : 50, 0), 1.45f * size, right, 0, lead + .06f);
                    if (variant >= 1 || big) BattleFx.Spawn(arena, "slash", el, c.TargetCentre + new Vector2(right ? -50 : 50, 10), 1.4f * size, right, 38f, lead + .18f);
                    if (variant == 2 || big) BattleFx.Spawn(arena, "slash", el, c.TargetCentre + new Vector2(right ? -50 : 50, -10), 1.55f * size, right, -40f, lead + .3f);
                    BattleFx.After(c.Host, lead + .2f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.2f); c.Impact(big ? .9f : .35f); });
                    return lead + .2f;
                case "array":
                    BattleFx.Behind(BattleFx.OnGround(arena, "cast", el, c.TargetGround, 1.5f * size, false, lead, 1.1f), c.Back);
                    BattleFx.After(c.Host, lead + .34f, () => { BattleFx.OnGround(arena, "pillar", el, c.TargetGround, 1.1f * size); c.Impact(big ? .9f : .5f); });
                    if (big) BattleFx.After(c.Host, lead + .6f, () => BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.3f));
                    return lead + .5f;
                case "rain":
                    BattleFx.OnGround(arena, "rain", el, c.TargetGround, 1.3f * size, !right, lead);
                    if (variant >= 1 || big) BattleFx.OnGround(arena, "rain", el, c.TargetGround + new Vector2(right ? 70 : -70, 0), 1.1f * size, !right, lead + .14f);
                    if (big) BattleFx.After(c.Host, lead + .5f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.35f); c.Impact(.9f); });
                    return lead + .36f;
                case "vortex":
                    BattleFx.OnGround(arena, "vortex", el, c.TargetGround, 1.25f * size, false, lead, 1.1f);
                    BattleFx.After(c.Host, lead + .3f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.1f); c.Impact(.3f); });
                    BattleFx.After(c.Host, lead + .6f, () => BattleFx.Spawn(arena, "slash", el, c.TargetCentre, 1.1f, right, 20f));
                    return lead + .3f;
                case "chain":
                    BattleFx.Spawn(arena, "chain", el, c.TargetCentre, 1.35f * size, false, 0, lead);
                    BattleFx.After(c.Host, lead + .3f, () => { BattleFx.Spawn(arena, big ? "burst" : "hit", el, c.TargetCentre, 1.1f); c.Impact(big ? .8f : .35f); });
                    return lead + .3f;
                case "pillar":
                    BattleFx.OnGround(arena, "pillar", el, c.TargetGround, 1.15f * size, false, lead);
                    if (variant >= 1 || big) BattleFx.OnGround(arena, "pillar", el, c.TargetGround + new Vector2(right ? 120 : -120, 0), .85f, false, lead + .12f);
                    if (big)
                    {
                        BattleFx.OnGround(arena, "pillar", el, c.TargetGround + new Vector2(right ? -120 : 120, 0), .85f, false, lead + .2f);
                        BattleFx.After(c.Host, lead + .3f, () => BattleFx.OnGround(arena, "quake", el, c.TargetGround, 1.3f));
                    }
                    BattleFx.After(c.Host, lead + .26f, () => c.Impact(big ? 1f : .5f));
                    return lead + .26f;
                case "buff":
                    BattleFx.Wheel(arena, el, c.CasterCentre + new Vector2(0, c.CasterHeight * .12f), 1.2f, .9f, c.Back);
                    BattleFx.Behind(BattleFx.OnGround(arena, "cast", el, c.CasterGround, 1.2f, false, 0, .9f), c.Back);
                    BattleFx.OnGround(arena, "heal", el, c.CasterGround, 1.15f, false, .1f, .8f);
                    return .2f;
                case "shield":
                    BattleFx.Spawn(arena, "guard", el, c.CasterCentre, 1.3f * size);
                    BattleFx.Behind(BattleFx.OnGround(arena, "cast", el, c.CasterGround, 1.1f, false, 0, .6f), c.Back);
                    return .15f;
                case "heal":
                    BattleFx.Behind(BattleFx.OnGround(arena, "lotus", "moc", c.CasterGround, 1.05f), c.Back);
                    BattleFx.OnGround(arena, "heal", "moc", c.CasterGround, 1.2f, false, 0, .9f);
                    return .2f;
                case "mana":
                    BattleFx.Spawn(arena, "orb", "thuy", c.CasterCentre, 1.4f, false, 0, 0, .8f);
                    BattleFx.OnGround(arena, "heal", "thuy", c.CasterGround, 1.1f, false, .1f, .8f);
                    return .2f;
                case "escape":
                    BattleFx.Spawn(arena, "wave", "phong", c.CasterCentre, .95f, !right);
                    BattleFx.OnGround(arena, "vortex", "phong", c.CasterGround, .9f, false, 0, .5f);
                    return .15f;
                default:    // a bolt of energy that bursts on the target
                {
                    var from = c.CasterCentre + new Vector2(right ? 60 : -60, 20);
                    BattleFx.Spawn(arena, "orb", el, from, .8f, false, 0, lead, .16f);
                    BattleFx.Projectile(arena, "proj", el, from, c.TargetCentre, .24f, 1.2f * size, lead + .1f, variant * 40f, () =>
                    {
                        BattleFx.Spawn(arena, "burst", el, c.TargetCentre, big ? 1.4f : 1.0f);
                        c.Impact(big ? .9f : .35f);
                    });
                    return lead + .34f;
                }
            }
        }

        /// <summary>A monster's move (fx from the server's kit; v = variant 0..3).</summary>
        public static float Monster(StageContext c, string fx, int v, bool big, string element)
        {
            if (c?.Arena == null) return 0f;
            var el = element;
            var right = c.Right;
            var arena = c.Arena;
            var s = 1.1f + v * .08f;
            var mouth = c.CasterCentre + new Vector2(right ? c.CasterHeight * .3f : -c.CasterHeight * .3f, c.CasterHeight * .12f);
            switch (fx)
            {
                case "claw": case "talon":
                    BattleFx.Spawn(arena, "claw", el, c.TargetCentre, 1.25f * s, right, v * 12f - 12f, .08f);
                    if (v >= 2) BattleFx.Spawn(arena, "claw", el, c.TargetCentre, 1.2f * s, !right, 20f, .22f);
                    BattleFx.After(c.Host, .22f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.1f); c.Impact(.35f); });
                    return .22f;
                case "bite":
                    BattleFx.Spawn(arena, "bite", el, c.TargetCentre, 1.3f * s, false, 0, .06f);
                    BattleFx.After(c.Host, .26f, () => c.Impact(.45f));
                    return .28f;
                case "charge": case "dive":
                    BattleFx.Spawn(arena, "wave", el, c.CasterCentre, 1f * s, right);
                    BattleFx.After(c.Host, .2f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.4f * s); BattleFx.Spawn(arena, "slash", el, c.TargetCentre, 1.3f * s, right); c.Impact(.55f); });
                    return .22f;
                case "roar": case "gust":
                    BattleFx.Spawn(arena, "wave", el, mouth + new Vector2(right ? 170 : -170, 0), 1.3f * s, right);
                    if (fx == "gust" || v >= 2) BattleFx.Spawn(arena, "wave", el, mouth + new Vector2(right ? 250 : -250, 20), 1.1f * s, right, 0, .16f);
                    BattleFx.After(c.Host, .28f, () => c.Impact(.5f));
                    return .3f;
                case "tail": case "slash":
                    BattleFx.Spawn(arena, "slash", el, c.TargetCentre + new Vector2(right ? -50 : 50, -30), 1.5f * s, right, v % 2 == 0 ? -20f : 25f, .1f);
                    BattleFx.After(c.Host, .24f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.2f); c.Impact(.4f); });
                    return .24f;
                case "quake":
                    BattleFx.OnGround(arena, "quake", el, c.TargetGround, 1.3f * s, false, .1f);
                    BattleFx.After(c.Host, .22f, () => c.Impact(.75f));
                    return .28f;
                case "volley": case "swarm":
                    BattleFx.OnGround(arena, "rain", el, c.TargetGround, 1.3f * s, !right);
                    if (v >= 1) BattleFx.OnGround(arena, "rain", el, c.TargetGround + new Vector2(right ? 70 : -70, 0), 1.1f, !right, .15f);
                    BattleFx.After(c.Host, .36f, () => c.Impact(.35f));
                    return .38f;
                case "coil": case "web":
                    BattleFx.Spawn(arena, "chain", el, c.TargetCentre, 1.3f * s);
                    BattleFx.After(c.Host, .3f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.1f); c.Impact(.4f); });
                    return .3f;
                case "breath":
                {
                    var d = c.TargetCentre - mouth;
                    var stream = BattleFx.Spawn(arena, "breath", el, mouth + d * .5f, Mathf.Max(.9f, d.magnitude / 370f), right, 0, .05f);
                    if (stream != null) stream.transform.localRotation = Quaternion.Euler(0, 0, right ? Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg : Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 180f);
                    BattleFx.After(c.Host, .32f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1f * s); c.Impact(.5f); });
                    return .34f;
                }
                case "spit": case "sting": case "bolt":
                    BattleFx.Projectile(arena, fx == "sting" ? "sword" : "proj", el, mouth, c.TargetCentre, .26f, 1.2f * s, .06f, fx == "spit" ? 60f : 0f, () =>
                    {
                        BattleFx.Spawn(arena, fx == "bolt" ? "burst" : "hit", el, c.TargetCentre, 1.1f * s);
                        c.Impact(.35f);
                    });
                    if (v >= 2) BattleFx.Projectile(arena, fx == "sting" ? "sword" : "proj", el, mouth, c.TargetCentre + new Vector2(0, 40), .26f, 1f, .2f, 0, () => BattleFx.Spawn(arena, "hit", el, c.TargetCentre, .9f));
                    return .32f;
                case "palm":
                    BattleFx.Spawn(arena, "palm", el, c.TargetCentre, 1.3f * s, right, 0, .1f);
                    BattleFx.After(c.Host, .28f, () => c.Impact(.5f));
                    return .3f;
                case "curse":
                    BattleFx.Spawn(arena, "orb", el, c.TargetCentre + new Vector2(0, c.TargetHeight * .5f), 1.3f, false, 0, 0, .5f);
                    BattleFx.Spawn(arena, "chain", el, c.TargetCentre, 1.2f * s, false, 0, .2f);
                    for (var i = 0; i < 2; i++)
                    {
                        var to = c.TargetCentre + new Vector2(0, i * 40 - 20);
                        BattleFx.Projectile(arena, "talisman", el, mouth + new Vector2(0, i * 60 - 30), to, .26f, 1.2f, i * .08f, 30f, () => BattleFx.Spawn(arena, "hit", el, to, .9f));
                    }
                    return .4f;
                case "seal":
                    BattleFx.Behind(BattleFx.OnGround(arena, "cast", el, c.TargetGround, 1.4f * s, false, 0, .9f), c.Back);
                    BattleFx.After(c.Host, .3f, () => { BattleFx.OnGround(arena, "pillar", el, c.TargetGround, 1.05f * s); c.Impact(.55f); });
                    return .5f;
                case "vortex":
                    BattleFx.OnGround(arena, "vortex", el, c.TargetGround, 1.2f * s, false, 0, .9f);
                    BattleFx.After(c.Host, .3f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.1f); c.Impact(.3f); });
                    return .3f;
                case "burst":
                    BattleFx.Spawn(arena, "orb", el, c.TargetCentre, 1.3f, false, 0, 0, .25f);
                    BattleFx.After(c.Host, .22f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.25f * s); c.Impact(.55f); });
                    return .3f;
                case "ult":
                    return Ultimate(c, el, v);
                default:
                    if (fx != null && fx.StartsWith("skill:", StringComparison.Ordinal))
                        return Player(c, "npc" + v, "", fx.Substring(6), big);
                    BattleFx.Spawn(arena, "slash", el, c.TargetCentre, 1.3f, right, 0, .08f);
                    BattleFx.After(c.Host, .2f, () => { BattleFx.Spawn(arena, "hit", el, c.TargetCentre, 1.1f); c.Impact(.3f); });
                    return .2f;
            }
        }

        /// <summary>
        /// The strongest move: the field darkens, the wheel of law opens behind the monster and a seal
        /// under the victim, then the element's finisher falls on it.
        /// </summary>
        private static float Ultimate(StageContext c, string el, int v)
        {
            var arena = c.Arena;
            var right = c.Right;
            Open(c, el, 1.25f, 1.25f);
            BattleFx.Behind(BattleFx.OnGround(arena, "cast", el, c.TargetGround, 1.6f, false, .1f, 1.3f), c.Back);
            const float t0 = .46f;
            var far = c.TargetCentre + new Vector2(right ? 280 : -280, 20);
            switch (el)
            {
                case "hoa":     // a dragon of fire, then the sky rains flame
                    BattleFx.Projectile(arena, "dragon", el, c.CasterCentre + new Vector2(0, 80), far, .5f, 1.5f, t0 - .16f, 60f, null);
                    BattleFx.After(c.Host, t0, () => { BattleFx.OnGround(arena, "rain", el, c.TargetGround, 1.6f, !right); BattleFx.OnGround(arena, "rain", el, c.TargetGround + new Vector2(right ? 90 : -90, 0), 1.3f, !right, .12f); });
                    BattleFx.After(c.Host, t0 + .34f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.8f); c.Impact(1f); });
                    break;
                case "loi":     // nine heavens' thunder: three bolts, the last the heaviest
                    for (var i = 0; i < 3; i++)
                    {
                        var k = i;
                        BattleFx.After(c.Host, t0 + i * .13f, () => { BattleFx.OnGround(arena, "pillar", el, c.TargetGround + new Vector2((k - 1) * 130f, 0), 1.15f + (k == 1 ? .35f : 0f)); c.Impact(.75f); });
                    }
                    BattleFx.After(c.Host, t0 + .4f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.5f); c.Impact(1f); });
                    break;
                case "bang":    // the ground freezes into spikes, a lotus of ice closes over the victim
                    BattleFx.After(c.Host, t0, () => BattleFx.OnGround(arena, "quake", el, c.TargetGround, 1.7f));
                    BattleFx.After(c.Host, t0 + .16f, () => BattleFx.OnGround(arena, "lotus", el, c.TargetGround, 1.5f));
                    BattleFx.After(c.Host, t0 + .34f, () => { BattleFx.OnGround(arena, "pillar", el, c.TargetGround, 1.3f); c.Impact(1f); });
                    break;
                case "kim":     // ten thousand blades, then the colossal sword
                    for (var i = 0; i < 9; i++)
                    {
                        var to = c.TargetCentre + new Vector2((i - 4) * 24f, (i % 2) * 34f - 17f);
                        BattleFx.Projectile(arena, "sword", el, to + new Vector2((right ? -1 : 1) * (170f + i * 12f), 400f), to, .2f, 1.4f, t0 - .1f + i * .045f, 0, () => BattleFx.Spawn(arena, "hit", el, to, .85f), true);
                    }
                    BattleFx.OnGround(arena, "giantsword", el, c.TargetGround, 1.5f, false, t0 + .2f);
                    BattleFx.After(c.Host, t0 + .42f, () => c.Impact(1f));
                    return t0 + .42f;
                case "moc":     // vines bind, a thorned lotus opens, the earth breaks
                    BattleFx.After(c.Host, t0, () => BattleFx.Spawn(arena, "chain", el, c.TargetCentre, 1.6f));
                    BattleFx.After(c.Host, t0 + .14f, () => BattleFx.OnGround(arena, "lotus", el, c.TargetGround, 1.6f));
                    BattleFx.After(c.Host, t0 + .32f, () => { BattleFx.OnGround(arena, "quake", el, c.TargetGround, 1.6f); c.Impact(1f); });
                    break;
                case "thuy":    // a water dragon and the wave behind it
                    BattleFx.Projectile(arena, "dragon", el, c.CasterCentre + new Vector2(0, 60), far, .5f, 1.6f, t0 - .16f, 40f, null);
                    BattleFx.After(c.Host, t0, () => { BattleFx.Spawn(arena, "wave", el, c.TargetCentre + new Vector2(right ? -120 : 120, 0), 1.7f, right); BattleFx.Spawn(arena, "wave", el, c.TargetCentre, 1.5f, right, 0, .14f); });
                    BattleFx.After(c.Host, t0 + .3f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.7f); c.Impact(1f); });
                    break;
                case "tho":     // mountains fall, the ground splits twice
                    BattleFx.After(c.Host, t0, () => { BattleFx.OnGround(arena, "quake", el, c.TargetGround, 1.8f); c.Impact(.9f); });
                    BattleFx.After(c.Host, t0 + .2f, () => { BattleFx.OnGround(arena, "giantsword", el, c.TargetGround, 1.3f); BattleFx.OnGround(arena, "quake", el, c.TargetGround + new Vector2(right ? 90 : -90, 0), 1.3f, false, .1f); });
                    BattleFx.After(c.Host, t0 + .42f, () => c.Impact(1f));
                    return t0 + .42f;
                case "phong":   // a storm column and blades of wind
                    BattleFx.After(c.Host, t0, () => BattleFx.OnGround(arena, "vortex", el, c.TargetGround, 1.7f, false, 0, 1.1f));
                    BattleFx.After(c.Host, t0 + .16f, () => { BattleFx.Spawn(arena, "slash", el, c.TargetCentre, 1.7f, right, 20f); BattleFx.Spawn(arena, "slash", el, c.TargetCentre, 1.6f, !right, -30f, .1f); });
                    BattleFx.After(c.Host, t0 + .3f, () => { BattleFx.Spawn(arena, "wave", el, c.TargetCentre, 1.6f, right); c.Impact(1f); });
                    break;
                case "ma":      // chains of the underworld, then the void opens
                    BattleFx.After(c.Host, t0, () => BattleFx.Spawn(arena, "chain", el, c.TargetCentre, 1.6f));
                    BattleFx.Projectile(arena, "dragon", el, c.CasterCentre + new Vector2(0, 90), far, .5f, 1.4f, t0 - .1f, -50f, null);
                    BattleFx.After(c.Host, t0 + .3f, () => { BattleFx.Spawn(arena, "burst", el, c.TargetCentre, 1.9f); c.Impact(1f); });
                    break;
                default:        // heaven's judgement: the sword of light and a pillar through it
                    BattleFx.OnGround(arena, "giantsword", el, c.TargetGround, 1.5f, false, t0 - .1f);
                    BattleFx.After(c.Host, t0 + .12f, () => { BattleFx.OnGround(arena, "pillar", el, c.TargetGround, 1.5f); c.Impact(1f); });
                    BattleFx.After(c.Host, t0 + .3f, () => BattleFx.OnGround(arena, "lotus", el, c.TargetGround, 1.4f));
                    return t0 + .14f;
            }
            return t0 + .32f;
        }
    }

    /// <summary>A monster on the field: moving body frames, looping aura, hit flash, lunge and death.</summary>
    internal sealed class MonsterView : MonoBehaviour
    {
        public RectTransform Rect { get; private set; }
        private Image glow, back, backNext, body, bodyNext, front, frontNext, flash, shadow;
        private ArtSprites.MonsterSet set;
        private Sprite still;
        private string monsterId;
        private Color glowColor = Color.clear;
        private float time, flashUntil, dieAt = -1f, phase;
        public bool FaceRight;
        public float Height => Rect != null ? Rect.sizeDelta.y : 300f;

        public static MonsterView Create(RectTransform parent, string name, string monsterId, Sprite fallback, Vector2 size, string element = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = size;
            var view = rect.gameObject.AddComponent<MonsterView>();
            view.Rect = rect;
            view.monsterId = monsterId;
            view.phase = (BattleFx.Hash(name) % 100) / 100f;
            view.set = ArtSprites.MonsterFrames(monsterId);
            view.still = fallback;
            view.shadow = InkUi.Simple(rect, "Shadow", InkUi.Shadow, Color.white, new Vector2(size.x * .72f, size.x * .16f));
            view.shadow.rectTransform.anchorMin = view.shadow.rectTransform.anchorMax = new Vector2(.5f, 0f);
            view.shadow.rectTransform.anchoredPosition = new Vector2(0, 3);
            view.shadow.raycastTarget = false;
            if (!string.IsNullOrEmpty(element))
            {
                // a soft light of the monster's element behind it, breathing with its aura
                view.glowColor = BattleFx.ElementColor(element);
                view.glow = InkUi.Simple(rect, "Glow", InkUi.Glow, Color.clear, Vector2.zero);
                view.glow.rectTransform.anchorMin = new Vector2(-.12f, -.02f); view.glow.rectTransform.anchorMax = new Vector2(1.12f, 1.1f);
                view.glow.rectTransform.offsetMin = view.glow.rectTransform.offsetMax = Vector2.zero;
            }
            view.back = Layer(rect, "Aura");
            view.backNext = Layer(rect, "AuraBlend");
            view.body = Layer(rect, "Body");
            view.bodyNext = Layer(rect, "BodyBlend");
            view.front = Layer(rect, "Embers");
            view.frontNext = Layer(rect, "EmbersBlend");
            view.flash = Layer(rect, "Flash");
            var material = BattleFx.Flash;
            if (material != null) view.flash.material = material;
            view.flash.color = new Color(1, 1, 1, 0);
            view.flash.enabled = false;
            view.Show();
            return view;
        }

        private static Image Layer(RectTransform parent, string name)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            var r = image.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            r.pivot = new Vector2(.5f, 0f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public void Hit(float seconds = .16f) { flashUntil = Time.unscaledTime + seconds; }

        public void Die() { if (dieAt < 0) dieAt = Time.unscaledTime; }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            Show();
        }

        private static void Pair(Image a, Image b, Sprite[] frames, float position, float alpha, Vector3 scale)
        {
            var n = frames.Length;
            var i = (int)position % n;
            var f = position - Mathf.Floor(position);
            a.sprite = frames[i];
            b.sprite = frames[(i + 1) % n];
            a.enabled = a.sprite != null;
            b.enabled = b.sprite != null && n > 1 && f > .01f;
            a.color = new Color(1, 1, 1, alpha * (1f - .6f * f));
            b.color = new Color(1, 1, 1, alpha * f);
            a.rectTransform.localScale = scale;
            b.rectTransform.localScale = scale;
        }

        private void Show()
        {
            if (set != null && !set.Alive) set = ArtSprites.MonsterFrames(monsterId);     // rebuilt if Unity unloaded the textures
            var alive = set != null && set.Alive;
            var t = time + phase * 2f;
            var sx = string.Equals(monsterId, "da_lang", StringComparison.Ordinal)
                ? (FaceRight ? 1f : -1f) : (FaceRight ? -1f : 1f);
            var fade = 1f;
            var squash = 1f;
            if (dieAt >= 0)
            {
                var d = Mathf.Clamp01((Time.unscaledTime - dieAt) / .9f);
                fade = 1f - d;
                squash = 1f - d * .35f;
            }
            else squash = 1f + Mathf.Sin(t * 2.4f) * .012f;
            var scale = new Vector3(sx, squash, 1f);
            Sprite bodySprite;
            if (alive)
            {
                // Discrete painted frames keep one clean silhouette instead of a
                // translucent second body around moving legs and horns.
                var position = t * 5f;
                var n = set.Body.Length;
                var i = (int)position % n;
                var f = position - Mathf.Floor(position);
                bodySprite = set.Body[i];
                body.sprite = bodySprite;
                bodyNext.sprite = set.Body[(i + 1) % n];
                bodyNext.enabled = false;
                bodyNext.color = new Color(1, 1, 1, f);
                bodyNext.rectTransform.localScale = scale;
            }
            else
            {
                bodySprite = still;
                body.sprite = bodySprite;
                bodyNext.enabled = false;
            }
            body.enabled = bodySprite != null;
            body.rectTransform.localScale = scale;
            flash.rectTransform.localScale = scale;
            shadow.color = new Color(1, 1, 1, fade * .85f);
            var aura = alive && set.Back != null && set.Back.Length > 0;
            if (aura)
            {
                Pair(back, backNext, set.Back, t * 9f, fade, scale);
                Pair(front, frontNext, set.Front, t * 9f, dieAt >= 0 ? 0f : 1f, scale);
            }
            else { back.enabled = backNext.enabled = front.enabled = frontNext.enabled = false; }
            if (glow != null) glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, (.075f + .025f * Mathf.Sin(t * 2.6f)) * fade);
            var hit = Time.unscaledTime < flashUntil;
            var hasFlash = flash.material != null && flash.material == BattleFx.Flash;
            body.color = dieAt >= 0 ? new Color(.7f, .7f, .8f, fade) : (hit && !hasFlash ? new Color(1f, .55f, .5f) : Color.white);
            flash.enabled = hit && hasFlash && bodySprite != null;
            if (flash.enabled)
            {
                flash.sprite = bodySprite;
                flash.color = new Color(1f, .96f, .9f, Mathf.Clamp01((flashUntil - Time.unscaledTime) / .16f) * .85f);
            }
        }
    }

    public enum FighterAction { Idle, Walk, Attack, Cast, Hurt, Down }

    /// <summary>A layered figure on the field with its aura: idle / walk / attack / cast / hurt / down.</summary>
    internal sealed class FighterView : MonoBehaviour
    {
        internal const float BattleScale = 1.2f;
        public const float AttackTime = .36f, CastTime = .5f, HurtTime = .28f;
        public RectTransform Rect { get; private set; }
        public bool FaceRight;
        public bool Moving;
        private Image body, flash;
        private Sprite[] frames;
        private Sprite paintedBody;
        private CultivatorFigure2D rig;
        private AuraAnimator aura;
        private LookSpec look;
        private FighterAction action = FighterAction.Idle;
        private float actionStart, time, flashUntil;
        public float Height => Rect != null ? Rect.sizeDelta.y : 300f;
        public FighterAction Action => action;
        public bool Busy => action == FighterAction.Attack || action == FighterAction.Cast;

        public static FighterView Create(RectTransform parent, string name, LookSpec look, float pixelScale, float auraStrength)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, 0f);
            var scale = Mathf.Clamp(pixelScale, .65f, 2.9f);
            var painted = AvatarComposer.CombatIllustration(look);
            var characterHeight = 110f * scale;
            rect.sizeDelta = painted != null
                ? new Vector2(characterHeight * painted.rect.width / painted.rect.height, characterHeight)
                : new Vector2(HeroSprites.FrameW, HeroSprites.FrameH) * scale;
            var shadow = InkUi.Simple(rect, "Shadow", InkUi.Shadow, Color.white, new Vector2(rect.sizeDelta.y * .62f, rect.sizeDelta.y * .12f));
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(.5f, 0f);
            shadow.rectTransform.anchoredPosition = new Vector2(0, 8);
            var view = rect.gameObject.AddComponent<FighterView>();
            view.Rect = rect;
            view.look = look;
            view.paintedBody = painted;
            view.frames = painted == null ? HeroSprites.Get(look) : null;
            var auraBack = Layer(rect, "AuraBack");
            if (CultivatorFigure2D.Available)
            {
                rect.sizeDelta = new Vector2(characterHeight * CultivatorFigure2D.Width / CultivatorFigure2D.Height, characterHeight);
                view.rig = CultivatorFigure2D.Create(rect, look);
                view.rig.Running = true;
            }
            view.body = Layer(rect, "Body", painted != null);
            if (view.rig != null) view.body.enabled = false;
            if (view.body is AnimatedPortraitImage animatedFigure) animatedFigure.SetAppearance(look);
            var auraFront = Layer(rect, "AuraFront");
            view.flash = Layer(rect, "Flash");
            var material = BattleFx.Flash;
            if (material != null) view.flash.material = material;
            view.flash.enabled = false;
            if (look != null && auraStrength > 0f)
            {
                view.aura = rect.gameObject.AddComponent<AuraAnimator>();
                view.aura.Back = auraBack;
                view.aura.Front = auraFront;
                view.aura.Set(look, true, auraStrength);
            }
            else { auraBack.enabled = false; auraFront.enabled = false; }
            view.Show();
            return view;
        }

        private static Image Layer(RectTransform parent, string name, bool animated = false)
        {
            var image = new GameObject(name, typeof(RectTransform), animated ? typeof(AnimatedPortraitImage) : typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            var r = image.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public void Play(FighterAction next)
        {
            if (action == FighterAction.Down && next != FighterAction.Idle) return;
            action = next;
            actionStart = Time.unscaledTime;
            if (next == FighterAction.Hurt) flashUntil = Time.unscaledTime + .14f;
        }

        /// <summary>Leaves a fading afterimage of the figure where it stands now (dashes and lunges).</summary>
        public void Ghost(Color tint, float seconds = .26f)
        {
            if (rig != null && Application.isPlaying) { rig.Ghost(Rect, tint, seconds); return; }
            if (body == null || body.sprite == null || !Application.isPlaying) return;
            var image = new GameObject("Afterimage", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var r = image.rectTransform;
            r.SetParent(Rect.parent, false);
            r.anchorMin = Rect.anchorMin; r.anchorMax = Rect.anchorMax; r.pivot = Rect.pivot;
            r.sizeDelta = Rect.sizeDelta;
            r.anchoredPosition = Rect.anchoredPosition;
            r.localScale = new Vector3(FaceRight ? -1f : 1f, 1f, 1f);
            r.SetSiblingIndex(Rect.GetSiblingIndex());
            image.sprite = body.sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var material = BattleFx.Flash;
            if (material != null) image.material = material;
            image.color = new Color(tint.r, tint.g, tint.b, .42f);
            image.gameObject.AddComponent<FadeAway>().Duration = seconds;
        }

        /// <summary>Previews: hold one frame of an action.</summary>
        public void Freeze(FighterAction held, float progress)
        {
            action = held;
            actionStart = Time.unscaledTime - progress * (held == FighterAction.Cast ? CastTime : AttackTime);
            Show();
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            Show();
        }

        private void Show()
        {
            if (rig != null)
            {
                var elapsedRig = Time.unscaledTime - actionStart;
                var duration = action == FighterAction.Attack ? AttackTime : action == FighterAction.Cast ? CastTime : HurtTime;
                var progressRig = Mathf.Clamp01(elapsedRig / duration);
                if (action == FighterAction.Down) progressRig = Mathf.Clamp01(elapsedRig / .35f);
                rig.SetMotion(action, progressRig, Moving, FaceRight, time);
                rig.SetHit(Time.unscaledTime < flashUntil);
                if (aura != null) aura.Flip = FaceRight;
                if (Application.isPlaying && ((action == FighterAction.Attack && elapsedRig >= AttackTime)
                    || (action == FighterAction.Cast && elapsedRig >= CastTime)
                    || (action == FighterAction.Hurt && elapsedRig >= HurtTime))) action = FighterAction.Idle;
                return;
            }
            if (paintedBody == null && (frames == null || frames.Length < HeroSprites.Total || frames[0] == null)) frames = HeroSprites.Get(look);
            var elapsed = Time.unscaledTime - actionStart;
            int index;
            var tilt = 0f;
            switch (action)
            {
                case FighterAction.Attack:
                    index = HeroSprites.AttackFrame(elapsed / AttackTime);
                    if (elapsed >= AttackTime && Application.isPlaying) action = FighterAction.Idle;
                    break;
                case FighterAction.Cast:
                    index = HeroSprites.CastFrame(elapsed / CastTime);
                    if (elapsed >= CastTime && Application.isPlaying) action = FighterAction.Idle;
                    break;
                case FighterAction.Hurt:
                    index = HeroSprites.HurtFrame;
                    if (elapsed >= HurtTime && Application.isPlaying) action = FighterAction.Idle;
                    break;
                case FighterAction.Down:
                    index = HeroSprites.HurtFrame;
                    tilt = Mathf.Clamp01(elapsed / .35f) * 78f;
                    break;
                default:
                case FighterAction.Idle:
                case FighterAction.Walk:
                    index = HeroSprites.FrameIndex(Moving, time);
                    break;
            }
            var sprite = paintedBody != null ? paintedBody : frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            body.sprite = sprite;
            body.enabled = sprite != null;
            if (body is AnimatedPortraitImage animatedBody)
            {
                var duration = action == FighterAction.Attack ? AttackTime : action == FighterAction.Cast ? CastTime : HurtTime;
                animatedBody.SetMotion(action == FighterAction.Idle && Moving ? FighterAction.Walk : action,
                    Mathf.Clamp01(elapsed / duration), FaceRight);
            }
            var sx = FaceRight ? -1f : 1f;
            var bodyRect = body.rectTransform;
            var flashRect = flash.rectTransform;
            bodyRect.localScale = new Vector3(paintedBody != null ? 1f : sx, 1f, 1f);
            flashRect.localScale = bodyRect.localScale;
            Rect.localRotation = Quaternion.Euler(0, 0, paintedBody != null ? 0f : FaceRight ? tilt : -tilt);
            bodyRect.localPosition = flashRect.localPosition = Vector3.zero;
            bodyRect.localRotation = flashRect.localRotation = Quaternion.identity;
            if (paintedBody != null)
            {
                var progress = Mathf.Clamp01(elapsed / (action == FighterAction.Attack ? AttackTime : action == FighterAction.Cast ? CastTime : HurtTime));
                var toward = FaceRight ? 1f : -1f;
                var poseX = 0f; var bob = 0f; var poseTilt = 0f; var poseScaleX = 1f; var poseScaleY = 1f;
                if (action == FighterAction.Attack)
                {
                    var strike = Mathf.Sin(Mathf.PI * progress);
                    poseX = toward * (8f + strike * 28f);
                    bob = strike * 5f;
                    poseTilt = toward * (12f * strike - 7f * (1f - progress));
                    poseScaleX = 1f + .045f * strike;
                    poseScaleY = 1f - .035f * strike;
                }
                else if (action == FighterAction.Cast)
                {
                    var lift = Mathf.Sin(Mathf.PI * progress);
                    bob = 5f + lift * 12f + Mathf.Sin(time * 8f) * 2f;
                    poseTilt = toward * (2f + lift * 5f);
                    poseScaleX = 1f - .025f * lift;
                    poseScaleY = 1f + .06f * lift;
                }
                else if (action == FighterAction.Hurt)
                {
                    var recoil = Mathf.Sin(Mathf.PI * progress);
                    poseX = -toward * recoil * 15f;
                    poseTilt = -toward * recoil * 14f;
                    poseScaleX = 1f + .06f * recoil;
                    poseScaleY = 1f - .04f * recoil;
                }
                else if (action == FighterAction.Down)
                {
                    poseTilt = -toward * Mathf.Clamp01(elapsed / .35f) * 76f;
                    poseX = -toward * 12f;
                    poseScaleY = .94f;
                }
                else if (action == FighterAction.Walk || Moving)
                {
                    var step = Mathf.Sin(time * 11f);
                    bob = Mathf.Abs(step) * 4f;
                    poseX = step * 2f;
                    poseTilt = -toward * step * 4f;
                    poseScaleY = 1f + Mathf.Abs(step) * .025f;
                }
                else
                {
                    bob = Mathf.Sin(time * 2.2f) * 1.7f;
                    poseScaleY = 1f + Mathf.Sin(time * 2.2f) * .018f;
                }
                var posePosition = new Vector3(poseX, bob, 0f);
                var poseScale = new Vector3(poseScaleX, poseScaleY, 1f);
                var poseRotation = Quaternion.Euler(0, 0, poseTilt);
                bodyRect.localPosition = flashRect.localPosition = posePosition;
                bodyRect.localScale = flashRect.localScale = poseScale;
                bodyRect.localRotation = flashRect.localRotation = poseRotation;
            }
            if (aura != null) aura.Flip = FaceRight;
            var hit = Time.unscaledTime < flashUntil;
            var hasFlash = flash.material != null && flash.material == BattleFx.Flash;
            body.color = hit && !hasFlash ? new Color(1f, .5f, .45f) : Color.white;
            flash.enabled = hit && hasFlash && sprite != null;
            if (flash.enabled)
            {
                flash.sprite = sprite;
                flash.color = new Color(1f, .5f, .42f, Mathf.Clamp01((flashUntil - Time.unscaledTime) / .14f) * .8f);
            }
        }
    }
}
