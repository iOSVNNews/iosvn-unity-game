using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Figure sheets composed from the same layers and colours as the portrait (Resources/Avatar3/w_*):
    /// 20 frames of 112x128 facing left — 8 walk, 4 idle (wind flutter), 4 attack (the weapon or
    /// sword seal thrusts forward), 3 cast (hand raised) and 1 hurt. Facing right is the mirrored
    /// sprite (QCBH style: characters only turn left/right). The frame is wide so a held weapon
    /// stays inside it at full reach; the figure stands on the centre line.
    /// </summary>
    public static class HeroSprites
    {
        public const int FrameW = 112, FrameH = 128;
        public const int WalkFrames = 8, IdleFrames = 4, AttackFrames = 4, CastFrames = 3, Total = 20;
        public const int AttackStart = 12, CastStart = 16, HurtFrame = 19;
        public static readonly Vector2 Pivot = new Vector2(.5f, 6f / FrameH);
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        private static readonly Dictionary<string, Color32> Named = new Dictionary<string, Color32>(StringComparer.OrdinalIgnoreCase)
        {
            { "Tóc đen", new Color32(30, 26, 30, 255) }, { "Tóc bạc", new Color32(214, 214, 222, 255) }, { "Tóc nâu", new Color32(90, 58, 40, 255) },
            { "Áo xanh ngọc", new Color32(63, 111, 106, 255) }, { "Áo xích hắc", new Color32(122, 42, 58, 255) }, { "Áo bạch lam", new Color32(96, 136, 178, 255) },
            { "Mắt lục", new Color32(58, 143, 122, 255) }, { "Mắt hổ phách", new Color32(216, 160, 48, 255) }, { "Mắt lam", new Color32(48, 160, 208, 255) },
        };

        public static Color32 ParseColor(string value, Color32 fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            if (Named.TryGetValue(value.Trim(), out var named)) return named;
            if (ColorUtility.TryParseHtmlString(value.StartsWith("#") ? value : "#" + value, out var parsed)) return parsed;
            return fallback;
        }

        private static readonly string[] WeaponColors = { "#a9a391", "#7fd08a", "#64b5f0", "#b69cff", "#f0a24e", "#ff6a5c" };

        public static Color32 Shade(Color32 c, float f) => new Color32((byte)Mathf.Clamp(c.r * f, 0, 255), (byte)Mathf.Clamp(c.g * f, 0, 255), (byte)Mathf.Clamp(c.b * f, 0, 255), c.a);

        /// <summary>Builds a full look for players created before the layered creator existed.</summary>
        public static LookSpec LegacyLook(bool female, string hair, string outfit, string eyes)
        {
            var look = AvatarComposer.Default(female);
            look.Set("hc", "#" + ColorUtility.ToHtmlStringRGB(ParseColor(hair, new Color32(30, 26, 30, 255))));
            var robe = "#" + ColorUtility.ToHtmlStringRGB(ParseColor(outfit, new Color32(63, 111, 106, 255)));
            look.Set("oc", robe);
            look.Set("ec", "#" + ColorUtility.ToHtmlStringRGB(ParseColor(eyes, new Color32(58, 143, 122, 255))));
            return look;
        }

        /// <summary>A deterministic NPC look derived from an id.</summary>
        public static LookSpec RandomLook(string seed, bool female)
        {
            var rng = new System.Random(seed?.GetHashCode() ?? 1);
            var look = AvatarComposer.Default(female);
            look.Set("template", rng.Next(10));
            foreach (var pair in AvatarComposer.Counts)
            {
                var v = rng.Next(pair.Value);
                if (pair.Key == "hat" && rng.NextDouble() < .45) v = 0;
                if (pair.Key == "bd" && (female || rng.NextDouble() < .6)) v = 0;
                if (pair.Key == "ey" && v == 7) v = 0;
                if (pair.Key == "au" && rng.NextDouble() < .55) v = 0;
                look.Set(pair.Key, v);
            }
            if (rng.NextDouble() < .6)       // most wanderers carry a weapon in hand
            {
                look.Set("wp", 4 + rng.Next(7));
                look.Set("wc", WeaponColors[rng.Next(WeaponColors.Length)]);
            }
            look.Set("hc", AvatarComposer.HairColors[rng.Next(6)]);
            look.Set("ec", AvatarComposer.EyeColors[rng.Next(AvatarComposer.EyeColors.Length)]);
            // Harmonious Xianxia color themes
            var palettes = new[]
            {
                new { tc = "#e8e4dc", oc = "#2f5f63", pc = "#20242a", sc = "#2a2a30", bc = "#1e2226", hac = "#ffd36a", ac = "#ffd36a" },
                new { tc = "#f4eef6", oc = "#b0c8ea", pc = "#e8e2ea", sc = "#e0d8e0", bc = "#8a3a5a", hac = "#ffd36a", ac = "#ffd36a" },
                new { tc = "#f0e8f4", oc = "#5a4a8a", pc = "#302a3a", sc = "#282230", bc = "#4a3a6a", hac = "#e0d8f0", ac = "#ffd36a" },
                new { tc = "#282428", oc = "#20242a", pc = "#1a1c20", sc = "#181a1c", bc = "#7a2a3a", hac = "#c8a050", ac = "#ffd36a" },
                new { tc = "#f2ece6", oc = "#a03030", pc = "#2a2022", sc = "#221a1c", bc = "#c8a050", hac = "#ffd36a", ac = "#ffd36a" },
                new { tc = "#e8f0f4", oc = "#3060a0", pc = "#202838", sc = "#1a2030", bc = "#2f5f63", hac = "#ffd36a", ac = "#ffd36a" }
            };
            var pal = palettes[rng.Next(palettes.Length)];
            look.Set("tc", pal.tc);
            look.Set("oc", pal.oc);
            look.Set("pc", pal.pc);
            look.Set("sc", pal.sc);
            look.Set("bc", pal.bc);
            look.Set("hac", pal.hac);
            look.Set("ac", pal.ac);
            look.Set("auc", AvatarComposer.AuraColors[rng.Next(AvatarComposer.AuraColors.Length)]);
            return look;
        }

        /// <summary>All 20 frames for one look, facing left (see the layout above).</summary>
        public static Sprite[] Get(LookSpec look)
        {
            var key = look.ToString();
            if (Cache.TryGetValue(key, out var cached) && cached != null && cached[0] != null) return cached;
            var w = FrameW * Total;
            var h = FrameH;
            var pixels = AvatarComposer.ComposePixels(look, "w_", w, h, FrameW, 1, 1);
            if (pixels == null) return Fallback();
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Hero" };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var frames = new Sprite[Total];
            for (var i = 0; i < Total; i++) frames[i] = Sprite.Create(texture, new Rect(i * FrameW, 0, FrameW, FrameH), Pivot, 16f);
            if (Cache.Count > 48) Cache.Clear();
            Cache[key] = frames;
            return frames;
        }

        /// <summary>The frame to show: walking cycles 0-7, standing breathes through 8-11.</summary>
        public static int FrameIndex(bool moving, float time) => moving ? (int)(time * 11f) % WalkFrames : WalkFrames + (int)(time * 4f) % IdleFrames;

        /// <summary>Attack frame for progress 0..1 (wind-up, strike, reach, recover).</summary>
        public static int AttackFrame(float t) => AttackStart + Mathf.Clamp((int)(Mathf.Clamp01(t) * AttackFrames), 0, AttackFrames - 1);

        /// <summary>Cast frame for progress 0..1: the hand rises, then holds high.</summary>
        public static int CastFrame(float t) => CastStart + Mathf.Clamp((int)(Mathf.Clamp01(t) * 3.6f), 0, CastFrames - 1);

        private static Sprite[] Fallback()
        {
            var texture = Texture2D.whiteTexture;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, 0f));
            var frames = new Sprite[Total];
            for (var i = 0; i < Total; i++) frames[i] = sprite;
            return frames;
        }
    }
}
