using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>Painted combat HUD pieces (Resources/Art/BattleHud): joystick, button rings, item slot, bar frame.</summary>
    internal static class BattleHudArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name, Vector4 border = default)
        {
            if (Cache.TryGetValue(name, out var s) && s != null && s.texture != null) return s;
            var t = Resources.Load<Texture2D>("Art/BattleHud/" + name);
            if (t == null) return null;
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.name = "BattleHud_" + name;
            Cache[name] = s;
            return s;
        }

        public static Sprite StickBase => Get("stick_base");
        public static Sprite StickKnob => Get("stick_knob");
        public static Sprite AttackRing => Get("attack_ring");
        public static Sprite SkillRing => Get("skill_ring");
        public static Sprite ItemSlot => Get("item_slot");
        /// <summary>Bar frame, 994x95 texels: end caps ~95 texels, rim ~26 texels.</summary>
        public static Sprite BarFrame => Get("bar_frame", new Vector4(95, 26, 95, 26));
        public const float BarTexHeight = 95f;
    }
}
