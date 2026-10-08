using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// A continuous body and a separate clothing layer share the same deforming
    /// mesh. Facial features and hair sit above the body; no limb sockets are
    /// exposed when the figure moves.
    /// </summary>
    internal sealed class CultivatorPuppet2D : MonoBehaviour
    {
        public static bool Available => Resources.Load<Texture2D>("Characters/PuppetMaleV3") != null
            && Resources.Load<Texture2D>("Characters/PuppetFemaleV3") != null;

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private RectTransform root;
        private Image aura, leftEye, rightEye, leftBrow, rightBrow, nose, mouth, hat, weapon;
        private Image[] facialFeatures;
        private Vector2[] facialPositions;
        private AnimatedPortraitImage body, clothing;
        private bool female;
        private int weaponStyle;
        private FighterAction action = FighterAction.Idle;
        private bool moving, facesRight;

        public static Sprite Portrait(LookSpec look)
        {
            var female = look != null && look.Get("g", "m") == "f";
            var sheet = Resources.Load<Texture2D>("Characters/Puppet" + (female ? "Female" : "Male") + "V3");
            if (sheet == null) return null;
            var key = sheet.name + "_portrait";
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var size = sheet.height * .30f;
            var centerX = sheet.width * .5f * (female ? .554f : .595f);
            var centerY = sheet.height * .84f;
            var rect = new Rect(Mathf.Clamp(centerX - size * .5f, 0, sheet.width * .5f - size),
                Mathf.Clamp(centerY - size * .5f, 0, sheet.height - size), size, size);
            var sprite = Sprite.Create(sheet, rect, new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        public static CultivatorPuppet2D Create(RectTransform parent, LookSpec look)
        {
            var rect = new GameObject("CultivatorPuppet", typeof(RectTransform), typeof(CultivatorPuppet2D)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(400f, 430f);
            rect.anchoredPosition = Vector2.zero;
            var puppet = rect.GetComponent<CultivatorPuppet2D>();
            puppet.root = rect;
            puppet.Build();
            puppet.SetLook(look);
            return puppet;
        }

        private void Build()
        {
            aura = Layer<Image>("Aura", root);
            aura.sprite = InkUi.Glow;
            aura.rectTransform.anchoredPosition = new Vector2(0, 210);
            aura.rectTransform.sizeDelta = new Vector2(280, 370);

            body = Layer<AnimatedPortraitImage>("Body", root);
            Fit(body.rectTransform);
            body.preserveAspect = true;
            clothing = Layer<AnimatedPortraitImage>("Clothing", root);
            Fit(clothing.rectTransform);
            clothing.preserveAspect = true;

            leftEye = Layer<Image>("LeftEye", root);
            rightEye = Layer<Image>("RightEye", root);
            leftBrow = Layer<Image>("LeftBrow", root);
            rightBrow = Layer<Image>("RightBrow", root);
            nose = Layer<Image>("Nose", root);
            mouth = Layer<Image>("Mouth", root);
            facialFeatures = new[] { leftEye, rightEye, leftBrow, rightBrow, nose, mouth };
            facialPositions = new Vector2[facialFeatures.Length];
            hat = Layer<Image>("Headwear", root);
            hat.rectTransform.anchoredPosition = new Vector2(0, 424);
            hat.rectTransform.sizeDelta = new Vector2(90, 80);
            weapon = Layer<Image>("Weapon", root);
            weapon.rectTransform.anchoredPosition = new Vector2(95, 195);
            weapon.rectTransform.sizeDelta = new Vector2(150, 150);
        }

        private static T Layer<T>(string name, RectTransform parent) where T : Image
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, 0f);
            image.rectTransform.pivot = new Vector2(.5f, .5f);
            image.raycastTarget = false;
            image.preserveAspect = false;
            return image;
        }

        private static void Fit(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
        }

        private static Sprite Half(Texture2D sheet, bool right)
        {
            var key = sheet.name + (right ? "_clothes" : "_body");
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var half = sheet.width * .5f;
            var sprite = Sprite.Create(sheet, new Rect(right ? half : 0f, 0f, half, sheet.height),
                new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static Sprite Quarter(Texture2D sheet, int index)
        {
            var key = sheet.name + "_" + index;
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var size = sheet.width * .5f;
            var sprite = Sprite.Create(sheet,
                new Rect((index % 2) * size, sheet.height - (index / 2 + 1) * size, size, size),
                new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static Sprite Equipment(Texture2D sheet, int index)
        {
            var key = sheet.name + "_equipment_" + index;
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var size = sheet.width * .25f;
            var sprite = Sprite.Create(sheet,
                new Rect((index % 4) * size, sheet.height - (index / 4 + 1) * size, size, size),
                new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static Sprite Feature(Texture2D sheet, string part, int x, int top, int width, int height)
        {
            var key = sheet.name + "_" + part;
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var sprite = Sprite.Create(sheet, new Rect(x, sheet.height - top - height, width, height),
                new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static void Place(Image image, Sprite sprite, float x, float y, float width, float height)
        {
            image.sprite = sprite;
            image.rectTransform.anchoredPosition = new Vector2(x, y);
            image.rectTransform.sizeDelta = new Vector2(width, height);
        }

        public void SetLook(LookSpec look)
        {
            if (look == null) return;
            female = look.Get("g", "m") == "f";
            var sex = female ? "Female" : "Male";
            var sheet = Resources.Load<Texture2D>("Characters/Puppet" + sex + "V3");
            var features = Resources.Load<Texture2D>("Characters/Puppet" + sex + "FacePartsV1");
            if (sheet == null || features == null) return;
            sheet.filterMode = features.filterMode = FilterMode.Bilinear;
            body.sprite = Half(sheet, false);
            clothing.sprite = Half(sheet, true);
            body.SetAppearance(look);
            body.SetFaceCustomization(look);
            clothing.SetAppearance(look);
            // V3 is finished painted art: keep its face and silhouette intact.
            body.PreservePaintedShape = clothing.PreservePaintedShape = true;

            var skin = HeroSprites.ParseColor(look.Get("sk", "#f0d2b4"), new Color32(240, 210, 180, 255));
            body.color = new Color(Mathf.Clamp(skin.r / .94f, .48f, 1f),
                Mathf.Clamp(skin.g / .82f, .46f, 1f), Mathf.Clamp(skin.b / .71f, .44f, 1f), 1f);
            var cloth = HeroSprites.ParseColor(look.Get("oc", "#e8e2d4"), Color.white);
            clothing.color = Color.Lerp(Color.white, cloth, .16f);
            body.color = clothing.color = Color.white;
            CharacterAppearance.Apply(body, look);
            CharacterAppearance.Apply(clothing, look);

            // The painted head and hair belong to the continuous body. Only the
            // facial features are attached above it, so no face-shaped patch or
            // separate hair cap is visible around the jaw and temples.
            var cx = female ? 14f : 25f;
            var eyeY = female ? 352f : 350f;
            var eyeWidth = female ? 22f : 21f;
            var eyeHeight = female ? 12f : 10f;
            var spacing = female ? 13f : 12f;
            Place(leftEye, Feature(features, "eyeL", 15, 275, 315, 200), cx - spacing, eyeY, eyeWidth, eyeHeight);
            Place(rightEye, Feature(features, "eyeR", 335, 275, 310, 200), cx + spacing, eyeY, eyeWidth, eyeHeight);
            Place(leftBrow, Feature(features, "browL", 635, 275, 305, 165), cx - spacing, eyeY + 11f, 24f, 7f);
            Place(rightBrow, Feature(features, "browR", 950, 275, 300, 165), cx + spacing, eyeY + 11f, 24f, 7f);
            Place(nose, Feature(features, "nose", 180, 675, 320, 470), cx, eyeY - 15f, 16f, 20f);
            Place(mouth, Feature(features, "mouth", 680, 850, 500, 310), cx, eyeY - 30f, 25f, 11f);
            for (var i = 0; i < facialFeatures.Length; i++)
            {
                facialPositions[i] = facialFeatures[i].rectTransform.anchoredPosition;
                // The repaired face is painted into the continuous body texture.
                // Keep the old patches out of both the portrait and creator.
                facialFeatures[i].enabled = false;
            }
            var eyeStyle = Mathf.Abs(look.Int("ey", 0));
            var browStyle = Mathf.Abs(look.Int("br", 0));
            var noseStyle = Mathf.Abs(look.Int("no", 0));
            var mouthStyle = Mathf.Abs(look.Int("mo", 0));
            var eyeScale = .91f + (eyeStyle % 5) * .045f;
            leftEye.rectTransform.localScale = rightEye.rectTransform.localScale = new Vector3(eyeScale, eyeScale, 1f);
            leftBrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -4f + browStyle % 5 * 2f);
            rightBrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f - browStyle % 5 * 2f);
            nose.rectTransform.localScale = new Vector3(.9f + noseStyle % 4 * .055f, 1f, 1f);
            mouth.rectTransform.localScale = new Vector3(.9f + mouthStyle % 5 * .04f, 1f, 1f);
            var eyeColor = HeroSprites.ParseColor(look.Get("ec", "#4d3732"), Color.white);
            leftEye.color = rightEye.color = Color.Lerp(Color.white, eyeColor, .10f);

            var accessories = Resources.Load<Texture2D>("Characters/RigEquipment16V1");
            if (accessories != null)
            {
                accessories.filterMode = FilterMode.Bilinear;
                var headwear = Mathf.Clamp(look.Int("hat", 0), 0, 5);
                hat.enabled = headwear > 0;
                if (hat.enabled) hat.sprite = Equipment(accessories, headwear - 1);
                var hatColor = HeroSprites.ParseColor(look.Get("hac", "#e2c57b"), Color.white);
                hat.color = Color.Lerp(Color.white, hatColor, .18f);
                weaponStyle = Mathf.Clamp(look.Int("wp", 0), 0, 10);
                var item = new[] { -1, 6, 6, 6, 6, 12, 8, 11, 13, 14, 15 }[weaponStyle];
                weapon.enabled = item >= 0;
                if (weapon.enabled) weapon.sprite = Equipment(accessories, item);
                var weaponColor = HeroSprites.ParseColor(look.Get("wc", "#e2c57b"), Color.white);
                weapon.color = Color.Lerp(Color.white, weaponColor, .14f);
                weapon.rectTransform.anchoredPosition = weaponStyle == 1 ? new Vector2(-105, 306) : new Vector2(95, 195);
                weapon.rectTransform.localRotation = Quaternion.Euler(0, 0, weaponStyle == 1 ? 38f : -16f);
                weapon.rectTransform.sizeDelta = weaponStyle == 10 ? new Vector2(115, 115) : new Vector2(155, 155);
                if (weaponStyle == 1) weapon.transform.SetSiblingIndex(2);
                else weapon.transform.SetAsLastSibling();
            }
            else hat.enabled = weapon.enabled = false;

            var style = Mathf.Clamp(look.Int("au", 0), 0, 5);
            aura.enabled = style > 0;
            var glow = HeroSprites.ParseColor(look.Get("auc", "#8fe0ff"), new Color32(143, 224, 255, 255));
            aura.color = new Color(glow.r, glow.g, glow.b, .10f + style * .02f);
        }

        public void SetMotion(FighterAction next, float progress, bool isMoving, bool faceRight, float time)
        {
            action = next;
            moving = isMoving;
            facesRight = faceRight;
            var state = next == FighterAction.Idle && isMoving ? FighterAction.Walk : next;
            body.SetMotion(state, progress, faceRight);
            clothing.SetMotion(state, progress, faceRight);
            var sway = Mathf.Sin(time * 1.1f);
            var headShift = new Vector2(sway * (isMoving ? 1.8f : .6f), Mathf.Sin(time * 1.4f) * .35f);
            for (var i = 0; i < facialFeatures.Length; i++)
                facialFeatures[i].rectTransform.anchoredPosition = facialPositions[i] + headShift;
            if (weapon.enabled && weaponStyle != 1)
            {
                var swing = next == FighterAction.Attack ? Mathf.Sin(Mathf.PI * progress) : 0f;
                weapon.rectTransform.localRotation = Quaternion.Euler(0, 0, -16f - swing * 45f);
                weapon.rectTransform.anchoredPosition = new Vector2(95f + swing * 18f, 195f + swing * 28f);
            }
            root.localRotation = Quaternion.Euler(0, 0, next == FighterAction.Down ? 70f * progress : 0f);
        }

        public void SetHit(bool hit)
        {
            var flash = hit ? new Color(1f, .62f, .58f, 1f) : Color.white;
            body.canvasRenderer.SetColor(flash);
            clothing.canvasRenderer.SetColor(flash);
        }

        private void LateUpdate()
        {
            if (root == null) return;
            var parent = root.parent as RectTransform;
            if (parent != null && parent.rect.width > 0f && parent.rect.height > 0f)
                root.localScale = new Vector3(facesRight ? -1f : 1f, 1f, 1f) * Mathf.Min(parent.rect.width / 400f, parent.rect.height / 430f) * .94f;
            if (action == FighterAction.Idle && !moving)
                SetMotion(FighterAction.Idle, 0f, false, facesRight, Time.unscaledTime);
        }
    }
}
