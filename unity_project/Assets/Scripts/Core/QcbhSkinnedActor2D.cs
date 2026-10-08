using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    // QCBH's Human sample keeps the robe as a weighted sprite mesh. Keep the
    // same separation of portrait and battle art, without exposing cut joints.
    internal sealed class QcbhSkinnedActor2D : MonoBehaviour
    {
        public static bool Available => Resources.Load<Texture2D>("Characters/FullBodyActorsV2") != null || Resources.Load<Texture2D>("Characters/FullBodyActorsV1") != null;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Sprite> Weapons = new Dictionary<int, Sprite>();
        private RectTransform root;
        private SkinnedActorImage body;
        private Image weapon, headwear, aura;
        private bool faceRight;
        private FighterAction action;
        private float progress, clock;
        private bool moving, externallyDriven;
        private Color bodyColor = Color.white;
        private int weaponStyle;
        private Vector2 headPosition;

        public static QcbhSkinnedActor2D Create(RectTransform parent, LookSpec look)
        {
            var rect = new GameObject("SkinnedActor", typeof(RectTransform), typeof(QcbhSkinnedActor2D)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            var atlas = ActorAtlas(look);
            rect.sizeDelta = new Vector2(430f * atlas.width * .5f / atlas.height, 430f);
            var actor = rect.GetComponent<QcbhSkinnedActor2D>();
            actor.root = rect;
            actor.aura = new GameObject("AppearanceAura", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            actor.aura.transform.SetParent(rect, false);
            actor.aura.rectTransform.anchorMin = Vector2.zero;
            actor.aura.rectTransform.anchorMax = Vector2.one;
            actor.aura.rectTransform.offsetMin = new Vector2(-40, -15);
            actor.aura.rectTransform.offsetMax = new Vector2(40, 15);
            actor.aura.raycastTarget = false;
            actor.body = new GameObject("ContinuousBody", typeof(RectTransform), typeof(SkinnedActorImage)).GetComponent<SkinnedActorImage>();
            actor.body.transform.SetParent(rect, false);
            actor.body.rectTransform.anchorMin = Vector2.zero;
            actor.body.rectTransform.anchorMax = Vector2.one;
            actor.body.rectTransform.offsetMin = actor.body.rectTransform.offsetMax = Vector2.zero;
            actor.body.raycastTarget = false;
            actor.body.preserveAspect = true;
            actor.weapon = new GameObject("HeldWeapon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            actor.weapon.transform.SetParent(rect, false);
            actor.weapon.raycastTarget = false;
            actor.weapon.preserveAspect = true;
            actor.weapon.rectTransform.anchorMin = actor.weapon.rectTransform.anchorMax = new Vector2(.5f, 0f);
            actor.weapon.rectTransform.sizeDelta = new Vector2(145, 145);
            actor.headwear = new GameObject("Headwear", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            actor.headwear.transform.SetParent(rect, false);
            actor.headwear.raycastTarget = false;
            actor.headwear.preserveAspect = true;
            actor.headwear.rectTransform.sizeDelta = new Vector2(64, 64);
            actor.headwear.rectTransform.anchorMin = actor.headwear.rectTransform.anchorMax = new Vector2(.5f, 0f);
            actor.SetLook(look);
            actor.Apply();
            return actor;
        }

        internal static Texture2D ActorAtlas(LookSpec look)
        {
            return Resources.Load<Texture2D>("Characters/FullBodyActorsV2")
                ?? Resources.Load<Texture2D>("Characters/FullBodyActorsV1");
        }

        public void SetLook(LookSpec look)
        {
            var female = look != null && look.Get("g", "m") == "f";
            headPosition = CharacterAppearance.FaceCenter(female, false) + new Vector2(.02f, .085f);
            var texture = ActorAtlas(look);
            if (texture == null) return;
            var key = texture.name + (female ? "_female" : "_male");
            if (!Sprites.TryGetValue(key, out var sprite) || sprite == null)
            {
                texture.filterMode = FilterMode.Bilinear;
                var half = texture.width * .5f;
                sprite = Sprite.Create(texture, new Rect(female ? half : 0, 0, half, texture.height), new Vector2(.5f, .5f), 100f);
                sprite.name = key;
                Sprites[key] = sprite;
            }
            body.sprite = sprite;
            body.CenterX = texture.name == "FullBodyActorsV2" ? .5f : (female ? .46f : .60f);
            body.SetAppearance(look);
            CharacterAppearance.Apply(body, look);
            bodyColor = Color.white;
            body.color = bodyColor;
            weaponStyle = look != null ? Mathf.Clamp(look.Int("wp", 0), 0, 10) : 0;
            var equipment = Resources.Load<Texture2D>("Characters/RigEquipment16V1");
            var hat = look != null ? Mathf.Clamp(look.Int("hat", 0), 0, 5) : 0;
            headwear.enabled = equipment != null && hat > 0;
            if (headwear.enabled)
            {
                var index = hat - 1;
                var cell = equipment.width / 4f;
                if (!Weapons.TryGetValue(index, out var item) || item == null)
                {
                    item = Sprite.Create(equipment, new Rect(index % 4 * cell, equipment.height - (index / 4 + 1) * cell, cell, cell), new Vector2(.5f, .5f), 100f);
                    Weapons[index] = item;
                }
                headwear.sprite = item;
                headwear.color = HeroSprites.ParseColor(look.Get("hac", "#e2c57b"), Color.white);
                headwear.rectTransform.anchoredPosition = body.DrawingPoint(headPosition);
            }
            var auraStyle = look != null ? Mathf.Clamp(look.Int("au", 0), 0, 5) : 0;
            aura.enabled = auraStyle > 0;
            aura.sprite = auraStyle == 2 || auraStyle == 4 ? InkUi.Ring : auraStyle == 3 ? InkUi.Cloud : InkUi.Glow;
            var auraColor = HeroSprites.ParseColor(look != null ? look.Get("auc", "#8fe0ff") : "#8fe0ff", Color.white);
            aura.color = new Color(auraColor.r, auraColor.g, auraColor.b, .12f + auraStyle * .035f);
            weapon.enabled = equipment != null && weaponStyle > 0;
            if (weapon.enabled)
            {
                var index = new[] { 0, 6, 6, 6, 6, 12, 8, 11, 13, 14, 15 }[weaponStyle];
                var cell = equipment.width / 4f;
                if (!Weapons.TryGetValue(index, out var item) || item == null)
                {
                    item = Sprite.Create(equipment, new Rect(index % 4 * cell, equipment.height - (index / 4 + 1) * cell, cell, cell), new Vector2(.5f, .5f), 100f);
                    Weapons[index] = item;
                }
                weapon.sprite = item;
                weapon.rectTransform.pivot = index == 6 ? new Vector2(.78f, .84f) : new Vector2(.5f, .5f);
            }
        }

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

        internal void SetFacing(bool right) => faceRight = right;

        public void SetHit(bool hit) => body.color = bodyColor * (hit ? new Color(1f, .62f, .58f, 1f) : Color.white);

        internal void Ghost(RectTransform fighter, Color tint, float seconds)
        {
            foreach (var source in new[] { body })
            {
                if (!source.enabled || source.sprite == null) continue;
                var ghost = new GameObject("SkinnedAfterimage", typeof(RectTransform), typeof(SkinnedActorImage)).GetComponent<SkinnedActorImage>();
                var rect = ghost.rectTransform;
                rect.SetParent(fighter.parent, false);
                rect.anchorMin = fighter.anchorMin; rect.anchorMax = fighter.anchorMax; rect.pivot = fighter.pivot;
                rect.sizeDelta = fighter.sizeDelta;
                rect.anchoredPosition = fighter.anchoredPosition;
                rect.localScale = new Vector3(faceRight ? -1f : 1f, 1f, 1f);
                rect.SetSiblingIndex(fighter.GetSiblingIndex());
                ghost.sprite = source.sprite;
                ghost.preserveAspect = true;
                ghost.raycastTarget = false;
                ghost.CenterX = source.CenterX;
                ghost.SetAppearance(source.Appearance);
                CharacterAppearance.Apply(ghost, source.Appearance);
                ghost.color = new Color(tint.r, tint.g, tint.b, .42f);
                ghost.Pose(action == FighterAction.Idle && moving ? FighterAction.Walk : action, progress, clock);
                ghost.gameObject.AddComponent<FadeAway>().Duration = seconds;
            }
        }

        private void LateUpdate()
        {
            var parent = root.parent as RectTransform;
            if (parent != null && parent.rect.width > 0 && parent.rect.height > 0)
                root.localScale = new Vector3(faceRight ? -1f : 1f, 1f, 1f) * Mathf.Min(parent.rect.width / root.sizeDelta.x, parent.rect.height / root.sizeDelta.y);
            if (!externallyDriven) { clock = Time.unscaledTime; Apply(); }
        }

        private void Apply()
        {
            body.Pose(action == FighterAction.Idle && moving ? FighterAction.Walk : action, progress, clock);
            if (headwear.enabled) headwear.rectTransform.anchoredPosition = body.DrawingPoint(body.MapPoint(headPosition));
            if (weapon.enabled)
            {
                var hand = weaponStyle == 1 ? body.MapPoint(new Vector2(body.CenterX + .10f, .64f)) : body.MapPoint(new Vector2(body.CenterX - .21f, .49f));
                weapon.rectTransform.position = body.rectTransform.TransformPoint(body.DrawingPoint(hand));
                weapon.rectTransform.localRotation = Quaternion.Euler(0, 0, weaponStyle == 1 ? -25f : 8f + body.HandAngle);
                if (weaponStyle == 1) weapon.transform.SetAsFirstSibling();
                else weapon.transform.SetAsLastSibling();
            }
        }
    }

    internal sealed class SkinnedActorImage : Image
    {
        private static readonly Vector2[] Bind = {
            new Vector2(.5f, .08f), new Vector2(.5f, .62f), new Vector2(.46f, .84f),
            new Vector2(.37f, .72f), new Vector2(.31f, .58f),
            new Vector2(.63f, .72f), new Vector2(.69f, .58f),
            new Vector2(.44f, .44f), new Vector2(.43f, .25f),
            new Vector2(.56f, .44f), new Vector2(.57f, .25f)
        };
        private static readonly int[] Parent = { -1, 0, 1, 1, 3, 1, 5, 0, 7, 0, 9 };
        private readonly Matrix4x4[] skin = new Matrix4x4[11];
        private readonly Matrix4x4[] posed = new Matrix4x4[11];
        private readonly float[] angles = new float[11];
        internal LookSpec Appearance { get; private set; }
        internal void SetAppearance(LookSpec look) { Appearance = look; SetVerticesDirty(); }
        public float CenterX { get; set; } = .5f;
        public float HandAngle => angles[0] + angles[1] + angles[3] + angles[4];

        public void Pose(FighterAction action, float progress, float time)
        {
            System.Array.Clear(angles, 0, angles.Length);
            var step = Mathf.Sin(time * 9f);
            var wave = Mathf.Sin(Mathf.PI * progress);
            var bob = Mathf.Sin(time * 1.5f) * .003f;
            angles[1] = Mathf.Sin(time * .85f) * .7f;
            angles[2] = -angles[1] * .6f;
            if (action == FighterAction.Walk)
            {
                angles[3] = -step * 9f; angles[5] = step * 9f;
                angles[4] = Mathf.Max(0f, step) * 5f; angles[6] = -Mathf.Max(0f, -step) * 5f;
                angles[7] = step * 9f; angles[9] = -step * 9f;
                angles[8] = Mathf.Max(0f, -step) * 14f; angles[10] = Mathf.Max(0f, step) * 14f;
                bob = Mathf.Abs(step) * .008f;
            }
            else if (action == FighterAction.Attack)
            {
                angles[1] = 5f * wave; angles[3] = -28f * wave; angles[4] = -14f * wave;
                angles[5] = 7f * wave; angles[7] = -3f * wave; angles[9] = 3f * wave;
            }
            else if (action == FighterAction.Cast)
            {
                angles[3] = -28f * wave; angles[5] = 28f * wave;
                angles[4] = -12f * wave; angles[6] = 12f * wave; bob += .012f * wave;
            }
            else if (action == FighterAction.Hurt) angles[1] = -9f * wave;
            else if (action == FighterAction.Down) angles[0] = 78f * Mathf.SmoothStep(0, 1, progress);
            for (var i = 0; i < Bind.Length; i++)
            {
                var parent = Parent[i];
                var local = Matrix4x4.TRS(new Vector3(Bind[i].x - (parent < 0 ? 0 : Bind[parent].x), Bind[i].y - (parent < 0 ? 0 : Bind[parent].y) + (parent < 0 ? bob : 0), 0), Quaternion.Euler(0, 0, angles[i]), Vector3.one);
                posed[i] = parent < 0 ? local : posed[parent] * local;
                skin[i] = posed[i] * Matrix4x4.Translate(-new Vector3(Bind[i].x, Bind[i].y, 0));
            }
            SetVerticesDirty();
        }

        public Vector2 DrawingPoint(Vector2 normalized)
        {
            var rect = GetPixelAdjustedRect();
            if (preserveAspect && sprite != null && rect.width > 0 && rect.height > 0)
            {
                var aspect = sprite.rect.width / sprite.rect.height;
                if (rect.width / rect.height > aspect)
                {
                    var width = rect.height * aspect;
                    rect.x += (rect.width - width) * rectTransform.pivot.x;
                    rect.width = width;
                }
                else
                {
                    var height = rect.width / aspect;
                    rect.y += (rect.height - height) * rectTransform.pivot.y;
                    rect.height = height;
                }
            }
            return rect.min + Vector2.Scale(rect.size, normalized);
        }
        private static float Smooth(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));

        public Vector2 MapPoint(Vector2 p)
        {
            var shift = CenterX - .5f;
            p.x -= shift;
            var head = Smooth((p.y - .74f) / .10f);
            var legs = 1f - Smooth((p.y - .30f) / .18f);
            var armBand = Smooth((p.y - .36f) / .12f) * (1f - Smooth((p.y - .72f) / .08f));
            var leftArm = armBand * (1f - Smooth((p.x - .30f) / .13f));
            var rightArm = armBand * Smooth((p.x - .57f) / .13f);
            var forearm = 1f - Smooth((p.y - .51f) / .14f);
            var calf = 1f - Smooth((p.y - .19f) / .13f);
            var rightLeg = Smooth((p.x - .47f) / .06f);
            var torso = (1f - head) * (1f - legs) * (1f - leftArm - rightArm);
            var sum = head + legs + leftArm + rightArm + torso;
            Vector2 Transform(int bone) => skin[bone].MultiplyPoint3x4(p);
            var result = (Transform(2) * head + Transform(1) * torso
                + (Transform(3) * (1f - forearm) + Transform(4) * forearm) * leftArm
                + (Transform(5) * (1f - forearm) + Transform(6) * forearm) * rightArm
                + (Transform(7) * (1f - calf) + Transform(8) * calf) * legs * (1f - rightLeg)
                + (Transform(9) * (1f - calf) + Transform(10) * calf) * legs * rightLeg) / Mathf.Max(.001f, sum);
            result.x += shift;
            return result;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            if (vh.currentVertCount != 4) return;
            var corners = new UIVertex[4];
            for (var i = 0; i < 4; i++) vh.PopulateUIVertex(ref corners[i], i);
            var min = (Vector2)corners[0].position; var max = (Vector2)corners[2].position;
            const int columns = 40, rows = 64;
            vh.Clear();
            for (var row = 0; row <= rows; row++)
            for (var col = 0; col <= columns; col++)
            {
                var u = col / (float)columns; var v = row / (float)rows;
                var point = MapPoint(new Vector2(u, v));
                var vertex = UIVertex.simpleVert;
                vertex.position = min + Vector2.Scale(max - min, point);
                vertex.uv0 = Vector4.Lerp(Vector4.Lerp(corners[0].uv0, corners[3].uv0, u), Vector4.Lerp(corners[1].uv0, corners[2].uv0, u), v);
                vertex.color = color;
                vh.AddVert(vertex);
            }
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
            {
                var a = row * (columns + 1) + col; var b = a + columns + 1;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
            }
        }
    }
}
