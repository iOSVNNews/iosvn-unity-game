using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Layered cultivator (paper-doll + bones), the same figure for the creator, profile, world and battle.
    /// Parts come from Resources/Characters/Modular/&lt;g&gt;.json + &lt;g&gt;_N.png (tools/character_art builds them):
    /// face, eyes, brows, nose, mouth, beard, mark, hair front/back, robe pieces, hat. Grey parts are tinted
    /// with the look colours, eyes blink, hair / robe / sleeves sway, and the bones play idle, walk, run,
    /// attack, cast, hurt, down and flying poses. The figure faces right; it is mirrored to face left.
    /// </summary>
    public sealed class CultivatorFigure2D : MonoBehaviour
    {
        public enum Framing { Full, Bust, Head }

        // ------------------------------------------------------------------ data

        internal sealed class Rig
        {
            public string Gender;
            public Texture2D[] Pages;
            public string[] BoneNames;
            public int[] BoneParent;
            public Vector2[] BoneBind;
            public float[] BoneBindAngle;
            public readonly Dictionary<string, int> BoneIndex = new Dictionary<string, int>();
            public readonly Dictionary<string, Vector2> Face = new Dictionary<string, Vector2>();
            public LayerDef[] Layers;
            public readonly Dictionary<string, SpriteDef> Sprites = new Dictionary<string, SpriteDef>();
        }

        internal sealed class LayerDef
        {
            public string Id, Bone, Tint;
            public string[] Options;
            public bool Flex, Far, Mirror, Eye;
            public string Anchor;
            public int BoneIndex;
        }

        internal sealed class SpriteDef
        {
            public int Page;
            public Rect Rect;
            public Vector2 Pivot;      // pixels from the bottom-left of the rect
            public float Scale;        // pixels per rig unit
            public Sprite Sprite;
        }

        private static readonly Dictionary<string, Rig> Rigs = new Dictionary<string, Rig>();

        public static bool Available => Load("m") != null;

        internal static Rig Load(string gender)
        {
            if (Rigs.TryGetValue(gender, out var cached)) return cached;
            Rig rig = null;
            try
            {
                var text = Resources.Load<TextAsset>("Characters/Modular/" + gender);
                if (text != null) rig = Parse(gender, text.text);
            }
            catch (Exception ex) { Debug.LogWarning("Modular character data: " + ex.Message); }
            Rigs[gender] = rig;
            return rig;
        }

        private static float F(object o) => o is double d ? (float)d : o is long l ? l : 0f;

        private static Rig Parse(string gender, string json)
        {
            if (!(Json.Parse(json) is Dictionary<string, object> root)) return null;
            var rig = new Rig { Gender = gender };
            var pages = (int)F(root["pages"]);
            rig.Pages = new Texture2D[pages];
            for (var i = 0; i < pages; i++)
            {
                rig.Pages[i] = Resources.Load<Texture2D>("Characters/Modular/" + gender + "_" + i);
                if (rig.Pages[i] == null) return null;
            }
            var bones = (List<object>)root["bones"];
            rig.BoneNames = new string[bones.Count];
            rig.BoneParent = new int[bones.Count];
            rig.BoneBind = new Vector2[bones.Count];
            rig.BoneBindAngle = new float[bones.Count];
            for (var i = 0; i < bones.Count; i++)
            {
                var b = (Dictionary<string, object>)bones[i];
                rig.BoneNames[i] = (string)b["name"];
                rig.BoneBind[i] = new Vector2(F(b["x"]), F(b["y"]));
                rig.BoneBindAngle[i] = b.TryGetValue("a", out var ba) ? F(ba) : 0f;
                rig.BoneIndex[rig.BoneNames[i]] = i;
            }
            for (var i = 0; i < bones.Count; i++)
            {
                var parent = (string)((Dictionary<string, object>)bones[i])["parent"];
                rig.BoneParent[i] = string.IsNullOrEmpty(parent) ? -1 : rig.BoneIndex[parent];
            }
            foreach (var pair in (Dictionary<string, object>)root["face"])
                if (pair.Value is List<object> v && v.Count >= 2) rig.Face[pair.Key] = new Vector2(F(v[0]), F(v[1]));
            var layers = (List<object>)root["layers"];
            rig.Layers = new LayerDef[layers.Count];
            for (var i = 0; i < layers.Count; i++)
            {
                var l = (Dictionary<string, object>)layers[i];
                var flags = ((string)l["flags"] ?? "").Split(' ');
                var def = new LayerDef
                {
                    Id = (string)l["id"], Bone = (string)l["bone"], Tint = (string)l["tint"],
                    Options = ((string)l["sprite"]).Split('|'),
                };
                foreach (var f in flags)
                {
                    if (f == "flex") def.Flex = true;
                    else if (f == "far") def.Far = true;
                    else if (f == "mirror") def.Mirror = true;
                    else if (f == "eye") def.Eye = true;
                    else if (f.StartsWith("anchor:")) def.Anchor = f.Substring(7);
                }
                def.BoneIndex = rig.BoneIndex.TryGetValue(def.Bone, out var bi) ? bi : 0;
                rig.Layers[i] = def;
            }
            foreach (var pair in (Dictionary<string, object>)root["sprites"])
            {
                var v = (List<object>)pair.Value;
                rig.Sprites[pair.Key] = new SpriteDef
                {
                    Page = (int)F(v[0]),
                    Rect = new Rect(F(v[1]), F(v[2]), F(v[3]), F(v[4])),
                    Pivot = new Vector2(F(v[5]), F(v[6])),
                    Scale = Mathf.Max(.01f, F(v[7])),
                };
            }
            return rig;
        }

        private static Sprite SpriteOf(Rig rig, SpriteDef def)
        {
            if (def.Sprite != null) return def.Sprite;
            var page = rig.Pages[Mathf.Clamp(def.Page, 0, rig.Pages.Length - 1)];
            var pivot = new Vector2(def.Pivot.x / Mathf.Max(1f, def.Rect.width), def.Pivot.y / Mathf.Max(1f, def.Rect.height));
            def.Sprite = Sprite.Create(page, def.Rect, pivot, 100f, 0, SpriteMeshType.FullRect);
            def.Sprite.name = "Modular_" + rig.Gender;
            return def.Sprite;
        }

        // ------------------------------------------------------------------ instance

        private sealed class Slot
        {
            public LayerDef Def;
            public Image Image;
            public FlexImage Flex;
            public SpriteDef Sprite;
            public bool Mirrored;
            public Color Tint = Color.white;
        }

        public const float Width = 560f, Height = 1400f;

        private RectTransform root, layerRoot;
        private Rig rig;
        private Slot[] slots = new Slot[0];
        private Image weapon, aura;
        private int weaponStyle, auraStyle;
        private LookSpec look;
        private Framing framing = Framing.Full;
        private bool faceRight = true;
        private FighterAction action;
        private float progress, clock, blinkAt, blinkStart = -10f;
        private bool moving, externallyDriven, hit;
        private Vector2[] boneWorld = new Vector2[0];
        private float[] boneAngle = new float[0];
        private Vector2[] boneScale = new Vector2[0];
        private float[] poseAngle = new float[0];
        private Vector2[] poseOffset = new Vector2[0];
        private Vector2[] poseScale = new Vector2[0];
        private readonly Dictionary<string, Vector2> anchors = new Dictionary<string, Vector2>();
        private float eyeScale = 1f;
        private float bendHair, bendSkirt, bendSleeve, bendCape;
        private int boneHead = -1, boneHandN = -1, boneBack = -1;

        /// <summary>Running instead of walking when moving (battle).</summary>
        public bool Running { get; set; }
        /// <summary>Standing on a flying sword / riding the wind.</summary>
        public bool Airborne { get; set; }
        /// <summary>Extra zoom in the creator (1 = framing default).</summary>
        public float Zoom { get; set; } = 1f;
        public bool FacesRight => faceRight;

        public static CultivatorFigure2D Create(RectTransform parent, LookSpec look, Framing framing = Framing.Full)
        {
            var rect = new GameObject("CultivatorFigure", typeof(RectTransform), typeof(CultivatorFigure2D)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(Width, Height);
            var figure = rect.GetComponent<CultivatorFigure2D>();
            figure.root = rect;
            figure.framing = framing;
            figure.aura = new GameObject("Aura", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            figure.aura.transform.SetParent(rect, false);
            var ar = figure.aura.rectTransform;
            ar.anchorMin = ar.anchorMax = new Vector2(.5f, 0f);
            ar.sizeDelta = new Vector2(Width * 1.3f, Height * .95f);
            ar.anchoredPosition = new Vector2(0, Height * .47f);
            figure.aura.raycastTarget = false;
            figure.layerRoot = new GameObject("Layers", typeof(RectTransform)).GetComponent<RectTransform>();
            figure.layerRoot.SetParent(rect, false);
            figure.layerRoot.anchorMin = figure.layerRoot.anchorMax = new Vector2(.5f, 0f);
            figure.layerRoot.pivot = new Vector2(.5f, 0f);
            figure.layerRoot.sizeDelta = Vector2.zero;
            figure.blinkAt = Time.unscaledTime + UnityEngine.Random.Range(1f, 3.5f);
            figure.SetLook(look);
            figure.Apply();
            return figure;
        }

        public void SetFraming(Framing value) { framing = value; Fit(); }

        public void SetLook(LookSpec value)
        {
            look = value ?? AvatarComposer.Default(false);
            var gender = look.Get("g", "m") == "f" ? "f" : "m";
            var next = Load(gender) ?? Load("m");
            if (next == null) return;
            if (next != rig) Build(next);
            Dress();
        }

        private void Build(Rig next)
        {
            foreach (var s in slots) if (s.Image != null) Kill(s.Image.gameObject);
            if (weapon != null) Kill(weapon.gameObject);
            rig = next;
            var n = rig.BoneNames.Length;
            boneWorld = new Vector2[n]; boneAngle = new float[n]; boneScale = new Vector2[n];
            poseAngle = new float[n]; poseOffset = new Vector2[n]; poseScale = new Vector2[n];
            boneHead = Bone("head"); boneHandN = Bone("handN"); boneBack = Bone("back");
            slots = new Slot[rig.Layers.Length];
            for (var i = 0; i < rig.Layers.Length; i++)
            {
                var def = rig.Layers[i];
                var go = new GameObject(def.Id, typeof(RectTransform), def.Flex ? typeof(FlexImage) : typeof(Image));
                go.transform.SetParent(layerRoot, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                var r = image.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(.5f, 0f);
                slots[i] = new Slot { Def = def, Image = image, Flex = image as FlexImage };
            }
            weapon = new GameObject("Weapon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            weapon.transform.SetParent(layerRoot, false);
            weapon.raycastTarget = false;
            weapon.preserveAspect = true;
            weapon.rectTransform.anchorMin = weapon.rectTransform.anchorMax = new Vector2(.5f, 0f);
        }

        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private int Bone(string name) => rig != null && rig.BoneIndex.TryGetValue(name, out var i) ? i : -1;

        private static readonly string[] StyleKeys = { "to", "ha", "fa", "ey", "br", "no", "mo", "bd", "ma", "hat" };
        private static readonly Dictionary<string, int> StyleCounts = new Dictionary<string, int>
        {
            { "to", 6 }, { "ha", 10 }, { "fa", 4 }, { "ey", 8 }, { "br", 5 }, { "no", 4 }, { "mo", 5 }, { "bd", 5 }, { "ma", 6 }, { "hat", 6 },
        };

        private string Resolve(string option)
        {
            var text = option;
            foreach (var key in StyleKeys)
            {
                var token = "{" + key + "}";
                if (text.IndexOf(token, StringComparison.Ordinal) < 0) continue;
                var v = Mathf.Clamp(look.Int(key, 0), 0, StyleCounts[key] - 1);
                if (key == "bd" && rig.Gender == "f") v = 0;
                text = text.Replace(token, v.ToString());
            }
            return text;
        }

        private Color TintOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return Color.white;
            string fallback;
            switch (key)
            {
                case "sk": fallback = "#f2d8be"; break;
                case "hc": fallback = "#1e1a1e"; break;
                case "ec": fallback = "#4a3020"; break;
                case "oc": fallback = "#2f5f63"; break;
                case "tc": fallback = "#e8e4dc"; break;
                case "pc": fallback = "#20242a"; break;
                case "sc": fallback = "#24282e"; break;
                case "bc": fallback = "#262a30"; break;
                case "hac": fallback = "#d8b46a"; break;
                case "mc": fallback = look.Get("ac", "#c8303c"); break;
                default: fallback = "#ffffff"; break;
            }
            var c = HeroSprites.ParseColor(look.Get(key, fallback), Color.white);
            // painted hair is multiplied by its colour; a near-black colour would flatten the painted
            // highlights to nothing, so dark hair colours are lifted (the dark strands stay dark)
            if (key == "hc") c = new Color(c.r + (1f - c.r) * HairLift, c.g + (1f - c.g) * HairLift, c.b + (1f - c.b) * HairLift, 1f);
            return c;
        }

        private const float HairLift = .16f;

        private void Dress()
        {
            if (rig == null) return;
            foreach (var s in slots)
            {
                s.Sprite = null;
                s.Mirrored = false;
                for (var o = 0; o < s.Def.Options.Length; o++)
                {
                    var name = Resolve(s.Def.Options[o]);
                    if (rig.Sprites.TryGetValue(name, out var def))
                    {
                        s.Sprite = def;
                        s.Mirrored = s.Def.Mirror && o == s.Def.Options.Length - 1;
                        break;
                    }
                }
                var image = s.Image;
                image.enabled = s.Sprite != null;
                if (s.Sprite == null) continue;
                image.sprite = SpriteOf(rig, s.Sprite);
                var r = image.rectTransform;
                r.sizeDelta = new Vector2(s.Sprite.Rect.width, s.Sprite.Rect.height) / s.Sprite.Scale;
                r.pivot = new Vector2(s.Sprite.Pivot.x / s.Sprite.Rect.width, s.Sprite.Pivot.y / s.Sprite.Rect.height);
                var tint = TintOf(s.Def.Tint);
                if (s.Def.Far) tint = new Color(tint.r * .82f, tint.g * .82f, tint.b * .84f, 1f);
                s.Tint = tint;
            }
            // face sliders
            anchors.Clear();
            Vector2 Face(string k, Vector2 fb) => rig.Face.TryGetValue(k, out var v) ? v : fb;
            float Slider(string key) => (Mathf.Clamp(look.Int(key, 10), 0, 20) - 10) / 10f;
            var eye = Face("eye", new Vector2(25, 86));
            var brow = Face("brow", new Vector2(25, 107));
            var es = Slider("es") * 6f; var eh = Slider("eh") * 5f; var bh = Slider("bh") * 5f;
            var nh = Slider("nh") * 4f; var mh = Slider("mh") * 4f;
            eyeScale = 1f + Slider("ez") * .12f;
            anchors["eyeL"] = new Vector2(-eye.x - es, eye.y + eh);
            anchors["eyeR"] = new Vector2(eye.x + es, eye.y + eh);
            anchors["browL"] = new Vector2(-brow.x - es * .8f, brow.y + eh + bh);
            anchors["browR"] = new Vector2(brow.x + es * .8f, brow.y + eh + bh);
            var nose = Face("nose", new Vector2(0, 54));
            var mouth = Face("mouth", new Vector2(0, 30));
            anchors["nose"] = new Vector2(nose.x, nose.y + nh);
            anchors["mouth"] = new Vector2(mouth.x, mouth.y + mh);
            anchors["mark"] = Face("mark", new Vector2(0, 132));
            // weapon
            weaponStyle = Mathf.Clamp(look.Int("wp", 0), 0, 10);
            var equipment = Resources.Load<Texture2D>("Characters/RigEquipment16V1");
            weapon.enabled = equipment != null && weaponStyle > 0;
            if (weapon.enabled)
            {
                var index = new[] { 0, 6, 6, 6, 6, 12, 8, 11, 13, 14, 15 }[weaponStyle];
                weapon.sprite = EquipmentSprite(equipment, index);
                weapon.rectTransform.sizeDelta = new Vector2(360f, 360f);
                weapon.rectTransform.pivot = index == 6 ? new Vector2(.78f, .84f) : new Vector2(.5f, .5f);
                weapon.color = Color.white;
                // a sword worn on the back sits behind the body, a held weapon in front of the hand
                if (weaponStyle == 1) weapon.transform.SetSiblingIndex(Mathf.Min(2, layerRoot.childCount - 1));
                else weapon.transform.SetSiblingIndex(Mathf.Max(0, layerRoot.childCount - 2));
            }
            auraStyle = Mathf.Clamp(look.Int("au", 0), 0, 5);
            aura.enabled = auraStyle > 0;
            aura.sprite = auraStyle == 2 || auraStyle == 4 ? InkUi.Ring : auraStyle == 3 ? InkUi.Cloud : InkUi.Glow;
            var auraColor = HeroSprites.ParseColor(look.Get("auc", "#8fe0ff"), Color.white);
            aura.color = new Color(auraColor.r, auraColor.g, auraColor.b, .12f + auraStyle * .035f);
            ApplyColors();
        }

        private static readonly Dictionary<int, Sprite> Equipment = new Dictionary<int, Sprite>();

        private static Sprite EquipmentSprite(Texture2D texture, int index)
        {
            if (Equipment.TryGetValue(index, out var s) && s != null) return s;
            var cell = texture.width / 4f;
            s = Sprite.Create(texture, new Rect(index % 4 * cell, texture.height - (index / 4 + 1) * cell, cell, cell), new Vector2(.5f, .5f), 100f);
            Equipment[index] = s;
            return s;
        }

        private void ApplyColors()
        {
            var flash = hit ? new Color(1f, .62f, .58f, 1f) : Color.white;
            foreach (var s in slots)
                if (s.Image != null) s.Image.color = s.Tint * flash;
            if (weapon != null) weapon.color = flash;
        }

        // ------------------------------------------------------------------ motion API (same as the old skinned actor)

        public void SetMotion(FighterAction next, float actionProgress, bool isMoving, bool facesRight, float time)
        {
            externallyDriven = true;
            action = next;
            progress = Mathf.Clamp01(actionProgress);
            moving = isMoving;
            faceRight = facesRight;
            clock = time;
            Apply();
        }

        public void SetFacing(bool right) => faceRight = right;

        public void SetPreviewMotion(FighterAction next, bool run = false)
        {
            externallyDriven = false;
            action = next;
            moving = next == FighterAction.Walk;
            Running = run;
        }

        public void SetHit(bool value)
        {
            if (hit == value) return;
            hit = value;
            ApplyColors();
        }

        /// <summary>A fading copy of the current pose (dashes, lunges).</summary>
        internal void Ghost(RectTransform fighter, Color tint, float seconds)
        {
            if (fighter == null || fighter.parent == null) return;
            var copy = Instantiate(root.gameObject, fighter.parent, false);
            var figure = copy.GetComponent<CultivatorFigure2D>();
            if (figure != null) Kill(figure);
            var rect = copy.GetComponent<RectTransform>();
            rect.position = root.position;
            rect.localScale = root.lossyScale.x == 0 ? root.localScale : root.localScale;
            rect.SetSiblingIndex(fighter.GetSiblingIndex());
            foreach (var g in copy.GetComponentsInChildren<Graphic>()) g.color = new Color(tint.r, tint.g, tint.b, g.color.a * .5f);
            var group = copy.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            copy.AddComponent<GhostFade>().Duration = seconds;
        }

        private void LateUpdate()
        {
            Fit();
            if (!externallyDriven)
            {
                clock = Time.unscaledTime;
                progress = Mathf.Repeat(clock, 1.1f) / 1.1f;
                Apply();
            }
        }

        private void Fit()
        {
            if (root == null) return;
            var parent = root.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            float scale;
            var y = 0f;
            switch (framing)
            {
                case Framing.Bust:
                    // waist up: the upper 58% of the figure fills the frame, feet hang below the mask
                    scale = Mathf.Min(parent.rect.width / (Width * 1.05f), parent.rect.height / (Height * .60f)) * Zoom;
                    y = -Height * .40f * scale;
                    break;
                case Framing.Head:
                    scale = Mathf.Min(parent.rect.width / 190f, parent.rect.height / 230f) * Zoom;
                    y = -(rig != null && boneHead >= 0 ? boneWorld[boneHead].y + 70f : 840f) * scale + parent.rect.height * .5f;
                    break;
                default:
                    scale = Mathf.Min(parent.rect.width / Width, parent.rect.height / Height) * Zoom;
                    break;
            }
            root.anchorMin = root.anchorMax = new Vector2(.5f, 0f);
            root.anchoredPosition = new Vector2(0, y);
            root.localScale = new Vector3(faceRight ? scale : -scale, scale, 1f);
        }

        // ------------------------------------------------------------------ posing

        private void Pose(int bone, float angle, float dx = 0, float dy = 0, float sx = 1, float sy = 1)
        {
            if (bone < 0) return;
            poseAngle[bone] += angle;
            poseOffset[bone] += new Vector2(dx, dy);
            poseScale[bone] = new Vector2(poseScale[bone].x * sx, poseScale[bone].y * sy);
        }

        private void Pose(string bone, float angle, float dx = 0, float dy = 0, float sx = 1, float sy = 1) => Pose(Bone(bone), angle, dx, dy, sx, sy);

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private void BuildPose()
        {
            for (var i = 0; i < poseAngle.Length; i++) { poseAngle[i] = 0; poseOffset[i] = Vector2.zero; poseScale[i] = Vector2.one; }
            var t = clock;
            var walking = moving && action == FighterAction.Idle || action == FighterAction.Walk;
            // ---- base: breathing idle
            var breathe = Mathf.Sin(t * 2.1f);
            Pose("torso", Mathf.Sin(t * .8f) * .8f, 0, 0, 1f, 1f + breathe * .007f);
            Pose("head", Mathf.Sin(t * .9f + .6f) * 1.4f);
            Pose("armN_up", 4f + Mathf.Sin(t * 1.1f) * 1.5f);
            Pose("armN_lo", 6f + Mathf.Sin(t * 1.1f + .5f) * 1.5f);
            Pose("armF_up", -3f - Mathf.Sin(t * 1.1f) * 1.5f);
            Pose("armF_lo", 5f);
            Pose("hips", 0, 0, breathe * 1.2f);
            bendHair = Mathf.Sin(t * 1.3f) * 5f;
            bendSkirt = Mathf.Sin(t * 1.05f + 1f) * 4f;
            bendSleeve = Mathf.Sin(t * 1.4f + 2f) * 4f;
            bendCape = Mathf.Sin(t * 1.2f) * 8f;

            if (Airborne)
            {
                // standing on the flying sword: knees soft, robe and hair streaming back
                var bob = Mathf.Sin(t * 2.2f) * 7f;
                Pose("hips", 0, 0, bob);
                Pose("legN_up", -6f); Pose("legN_lo", 10f);
                Pose("legF_up", 10f); Pose("legF_lo", -4f);
                Pose("torso", moving ? -6f : -2f);
                Pose("armN_up", moving ? -24f : -10f); Pose("armN_lo", 12f);
                Pose("armF_up", moving ? -30f : -12f);
                var wind = moving ? 1f : .35f;
                bendHair = -40f * wind + Mathf.Sin(t * 6f) * 5f;
                bendSkirt = -34f * wind + Mathf.Sin(t * 7f) * 6f;
                bendSleeve = -30f * wind + Mathf.Sin(t * 8f) * 5f;
                bendCape = -60f * wind + Mathf.Sin(t * 6f) * 10f;
            }
            else if (walking && action == FighterAction.Idle || action == FighterAction.Walk)
            {
                var run = Running;
                var p = t * (run ? 11f : 7.6f);
                var s = Mathf.Sin(p);
                var amp = run ? 34f : 22f;
                Pose("legN_up", amp * s);
                Pose("legF_up", -amp * s);
                Pose("legN_lo", -(run ? 34f : 20f) * Mathf.Max(0f, Mathf.Sin(p + 1.4f)));
                Pose("legF_lo", -(run ? 34f : 20f) * Mathf.Max(0f, Mathf.Sin(p + 1.4f + Mathf.PI)));
                Pose("hips", 0, 0, (run ? -10f : -7f) * Mathf.Abs(s) + 3f);
                Pose("torso", (run ? -9f : -3f) + s * 1f);
                Pose("head", run ? 5f : 2f);
                Pose("armN_up", -(run ? 32f : 18f) * s);
                Pose("armN_lo", (run ? 28f : 8f) + 6f * Mathf.Max(0f, -s));
                Pose("armF_up", (run ? 32f : 18f) * s);
                Pose("armF_lo", (run ? 28f : 8f) + 6f * Mathf.Max(0f, s));
                bendHair = (run ? -30f : -12f) - 4f * Mathf.Sin(2 * p);
                bendSkirt = (run ? -26f : -10f) - 6f * Mathf.Abs(s);
                bendSleeve = (run ? -22f : -8f) + 4f * s;
                bendCape = (run ? -50f : -22f) - 6f * Mathf.Abs(s);
            }

            var k = progress;
            switch (action)
            {
                case FighterAction.Attack:
                {
                    // wind up overhead, sweep down and forward, recover
                    float swing;
                    if (k < .32f) swing = Ease(k / .32f) * 1f;
                    else if (k < .55f) swing = 1f - Ease((k - .32f) / .23f) * 1.9f;
                    else swing = -.9f * (1f - Ease((k - .55f) / .45f));
                    var lunge = k < .32f ? 0f : k < .7f ? Ease((k - .32f) / .38f) : 1f - Ease((k - .7f) / .3f);
                    Pose("armN_up", 140f * Mathf.Max(0f, swing) + 40f * Mathf.Min(0f, swing));
                    Pose("armN_lo", 18f * Mathf.Max(0f, swing));
                    Pose("torso", 6f * Mathf.Max(0f, swing) - 12f * lunge);
                    Pose("hips", 0, 22f * lunge, -6f * lunge);
                    Pose("legN_up", 24f * lunge); Pose("legF_up", -18f * lunge); Pose("legN_lo", -10f * lunge);
                    Pose("armF_up", -20f * lunge);
                    bendSkirt += -16f * lunge; bendHair += -14f * lunge; bendSleeve += -20f * lunge;
                    break;
                }
                case FighterAction.Cast:
                {
                    var w = Mathf.Sin(Mathf.PI * Mathf.Clamp01(k * 1.2f));
                    Pose("armN_up", 72f * w); Pose("armN_lo", 34f * w);
                    Pose("armF_up", 88f * w); Pose("armF_lo", 28f * w);
                    Pose("torso", 4f * w); Pose("head", 4f * w);
                    Pose("hips", 0, 0, 10f * w);
                    Pose("legN_lo", -8f * w); Pose("legF_lo", -6f * w);
                    bendSkirt += Mathf.Sin(t * 14f) * 8f * w + 10f * w;
                    bendSleeve += Mathf.Sin(t * 15f) * 8f * w;
                    bendHair += 10f * w;
                    break;
                }
                case FighterAction.Hurt:
                {
                    var w = Mathf.Sin(Mathf.PI * k);
                    Pose("torso", 13f * w); Pose("head", 9f * w);
                    Pose("hips", 0, -14f * w, -4f * w);
                    Pose("armN_up", -24f * w); Pose("armF_up", 30f * w);
                    bendHair += 16f * w; bendSkirt += 12f * w;
                    break;
                }
                case FighterAction.Down:
                {
                    var w = Ease(k);
                    Pose("root", 82f * w, -30f * w, 40f * w);
                    Pose("legN_up", 20f * w); Pose("legN_lo", -40f * w);
                    Pose("armN_up", 60f * w); Pose("armF_up", 80f * w);
                    Pose("head", 10f * w);
                    break;
                }
            }
        }

        private void Apply()
        {
            if (rig == null || slots.Length == 0) return;
            BuildPose();
            var n = rig.BoneNames.Length;
            for (var i = 0; i < n; i++)
            {
                var p = rig.BoneParent[i];
                var local = rig.BoneBind[i] + poseOffset[i];
                if (p < 0)
                {
                    boneWorld[i] = local;
                    boneAngle[i] = rig.BoneBindAngle[i] + poseAngle[i];
                    boneScale[i] = poseScale[i];
                }
                else
                {
                    var a = boneAngle[p] * Mathf.Deg2Rad;
                    var sc = boneScale[p];
                    var lx = local.x * sc.x; var ly = local.y * sc.y;
                    boneWorld[i] = boneWorld[p] + new Vector2(lx * Mathf.Cos(a) - ly * Mathf.Sin(a), lx * Mathf.Sin(a) + ly * Mathf.Cos(a));
                    boneAngle[i] = boneAngle[p] + rig.BoneBindAngle[i] + poseAngle[i];
                    boneScale[i] = new Vector2(sc.x * poseScale[i].x, sc.y * poseScale[i].y);
                }
            }
            // blink every few seconds (both eyes, 0.14 s)
            var now = Time.unscaledTime;
            if (now >= blinkAt) { blinkStart = now; blinkAt = now + UnityEngine.Random.Range(2.2f, 5.5f); }
            var bt = (now - blinkStart) / .14f;
            var blink = bt >= 0f && bt <= 1f ? Mathf.Lerp(1f, .08f, 1f - Mathf.Abs(bt * 2f - 1f)) : 1f;
            if (action == FighterAction.Down && progress > .5f) blink = .08f;

            foreach (var s in slots)
            {
                if (s.Sprite == null) continue;
                var b = s.Def.BoneIndex;
                var a = boneAngle[b];
                var rad = a * Mathf.Deg2Rad;
                var offset = Vector2.zero;
                if (s.Def.Anchor != null && anchors.TryGetValue(s.Def.Anchor, out var anchor)) offset = anchor;
                var sc = boneScale[b];
                var ox = offset.x * sc.x; var oy = offset.y * sc.y;
                var pos = boneWorld[b] + new Vector2(ox * Mathf.Cos(rad) - oy * Mathf.Sin(rad), ox * Mathf.Sin(rad) + oy * Mathf.Cos(rad));
                var r = s.Image.rectTransform;
                r.anchoredPosition = pos;
                r.localRotation = Quaternion.Euler(0, 0, a);
                var sy = sc.y;
                var sx = sc.x * (s.Mirrored ? -1f : 1f);
                if (s.Def.Eye) { sy *= blink * eyeScale; sx *= eyeScale; }
                r.localScale = new Vector3(sx, sy, 1f);
                if (s.Flex != null)
                {
                    var id = s.Def.Id;
                    var bend = id == "hairB" ? bendHair : id == "cape" ? bendCape : id.StartsWith("arm") ? bendSleeve : bendSkirt;
                    if (s.Mirrored) bend = -bend;
                    s.Flex.Bend = bend;
                }
            }
            if (weapon != null && weapon.enabled)
            {
                var wr = weapon.rectTransform;
                if (weaponStyle == 1 && boneBack >= 0)
                {
                    wr.anchoredPosition = boneWorld[boneBack] + new Vector2(-10f, 10f);
                    wr.localRotation = Quaternion.Euler(0, 0, boneAngle[boneBack] - 150f);
                    wr.localScale = new Vector3(-1f, 1f, 1f);
                }
                else if (boneHandN >= 0)
                {
                    var a = boneAngle[boneHandN];
                    var rad = a * Mathf.Deg2Rad;
                    var grip = new Vector2(0, -20f);
                    wr.anchoredPosition = boneWorld[boneHandN] + new Vector2(grip.x * Mathf.Cos(rad) - grip.y * Mathf.Sin(rad), grip.x * Mathf.Sin(rad) + grip.y * Mathf.Cos(rad));
                    wr.localRotation = Quaternion.Euler(0, 0, a - 8f);
                    wr.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
        }
    }

    /// <summary>An Image whose lower part bends sideways (hair, skirts, sleeves, capes).</summary>
    internal sealed class FlexImage : Image
    {
        private float bend;
        /// <summary>Sideways displacement (rig units) of the far end of the sprite.</summary>
        public float Bend
        {
            get => bend;
            set { if (Mathf.Abs(value - bend) > .05f) { bend = value; SetVerticesDirty(); } }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (sprite == null) { base.OnPopulateMesh(vh); return; }
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var outer = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            const int rows = 10;
            // the pivot is where the part hangs from; bending grows with distance below it
            var pivotY = rect.yMin + rect.height * rectTransform.pivot.y;
            var reach = Mathf.Max(1f, pivotY - rect.yMin);
            var c = color;
            for (var row = 0; row <= rows; row++)
            {
                var v = row / (float)rows;
                var y = Mathf.Lerp(rect.yMin, rect.yMax, v);
                var t = Mathf.Clamp01((pivotY - y) / reach);
                var dx = bend * t * t;
                for (var col = 0; col <= 1; col++)
                {
                    var x = col == 0 ? rect.xMin : rect.xMax;
                    var vert = UIVertex.simpleVert;
                    vert.position = new Vector3(x + dx, y);
                    vert.uv0 = new Vector2(Mathf.Lerp(outer.x, outer.z, col), Mathf.Lerp(outer.y, outer.w, v));
                    vert.color = c;
                    vh.AddVert(vert);
                }
            }
            for (var row = 0; row < rows; row++)
            {
                var a = row * 2;
                vh.AddTriangle(a, a + 2, a + 3);
                vh.AddTriangle(a, a + 3, a + 1);
            }
        }
    }

    /// <summary>Fades a copied figure out and removes it.</summary>
    internal sealed class GhostFade : MonoBehaviour
    {
        public float Duration = .26f;
        private CanvasGroup group;
        private float start;
        private void Start() { group = GetComponent<CanvasGroup>(); start = Time.unscaledTime; }
        private void Update()
        {
            var t = (Time.unscaledTime - start) / Mathf.Max(.01f, Duration);
            if (group != null) group.alpha = (1f - t) * (1f - t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
