using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// A cutout character assembled from separate painted limbs. Each limb owns a
    /// pivot at its anatomical joint, so walking and skills move the arms and legs
    /// instead of stretching one full-body portrait.
    /// </summary>
    internal sealed class CultivatorRig2D : MonoBehaviour
    {
        public static bool Available => Resources.Load<Texture2D>("Characters/RigMalePartsV3") != null
            && Resources.Load<Texture2D>("Characters/RigFemalePartsV3") != null
            && Resources.Load<Texture2D>("Characters/RigMaleFaces4V1") != null
            && Resources.Load<Texture2D>("Characters/RigFemaleFaces4V1") != null;
        private static readonly Dictionary<string, Sprite> SliceCache = new Dictionary<string, Sprite>();
        private static readonly float[] X = { 0, 362, 724, 1086, 1448 };
        private static readonly float[] Y = { 0, 360, 602, 828, 1086 };
        private static readonly Color32[] Accents =
        {
            new Color32(221, 239, 225, 255), new Color32(204, 221, 247, 255),
            new Color32(250, 207, 199, 255), new Color32(228, 227, 226, 255),
            new Color32(251, 235, 195, 255), new Color32(204, 224, 251, 255),
            new Color32(226, 206, 246, 255), new Color32(209, 239, 218, 255),
            new Color32(250, 221, 177, 255), new Color32(219, 211, 202, 255)
        };

        private RectTransform rigRect, torsoJoint, headJoint, hairJoint, skirtJoint;
        private RectTransform leftShoulder, rightShoulder, leftElbow, rightElbow;
        private RectTransform leftWrist, rightWrist, leftHip, rightHip;
        private RectTransform leftKnee, rightKnee, leftAnkle, rightAnkle;
        private Image[] parts;
        private Image aura, face, frontHair, headgear, heldWeapon, backWeapon, waistAccessory;
        private Color[] partColors;
        private Color faceBaseColor = Color.white;
        private bool female;
        private int template = -1;
        private FighterAction action = FighterAction.Idle;
        private float progress, motionTime;
        private bool moving, faceRight;
        private float torsoAngle, headAngle, hairAngle, skirtAngle;
        private float leftArmAngle, rightArmAngle, leftElbowAngle, rightElbowAngle;
        private float leftHandAngle, rightHandAngle;
        private float leftLegAngle, rightLegAngle, leftKneeAngle, rightKneeAngle;
        private float leftFootAngle, rightFootAngle;
        private Vector2 bodyOffset, shoulderOffset;

        public static CultivatorRig2D Create(RectTransform parent, LookSpec look)
        {
            var rect = new GameObject("PaintedRig", typeof(RectTransform), typeof(CultivatorRig2D)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(250f, 430f);
            rect.anchoredPosition = Vector2.zero;
            var rig = rect.GetComponent<CultivatorRig2D>();
            rig.rigRect = rect;
            rig.Build();
            rig.SetLook(look);
            rig.Pose();
            return rig;
        }

        private void Build()
        {
            parts = new Image[16];
            aura = Part("SpiritAura", rigRect, -1, new Vector2(240, 330), new Vector2(.5f, .5f));
            aura.sprite = InkUi.Glow;
            aura.rectTransform.anchoredPosition = new Vector2(0, -20);
            aura.transform.SetAsFirstSibling();
            hairJoint = Joint("RearHair", rigRect, 0, 308);
            parts[3] = Part("RearHairArt", hairJoint, 3, new Vector2(178, 180), new Vector2(.5f, .50f));

            leftHip = Joint("LeftHip", rigRect, -28, 180);
            rightHip = Joint("RightHip", rigRect, 28, 180);
            parts[10] = Part("LeftThigh", leftHip, 10, new Vector2(124, 91), new Vector2(.24f, .86f));
            parts[13] = Part("RightThigh", rightHip, 13, new Vector2(124, 91), new Vector2(.24f, .86f));
            leftKnee = Joint("LeftKnee", leftHip, 36, -68);
            rightKnee = Joint("RightKnee", rightHip, -36, -68);
            parts[11] = Part("LeftCalf", leftKnee, 11, new Vector2(94, 100), new Vector2(.22f, .84f));
            parts[14] = Part("RightCalf", rightKnee, 14, new Vector2(94, 100), new Vector2(.78f, .84f));
            leftAnkle = Joint("LeftAnkle", leftKnee, 35, -65);
            rightAnkle = Joint("RightAnkle", rightKnee, -35, -65);
            parts[12] = Part("LeftBoot", leftAnkle, 12, new Vector2(78, 72), new Vector2(.55f, .78f));
            parts[15] = Part("RightBoot", rightAnkle, 15, new Vector2(78, 72), new Vector2(.45f, .78f));

            // The far arm is behind the body. Each elbow and wrist follows its own joint.
            leftShoulder = Joint("LeftShoulder", rigRect, -48, 329);
            parts[4] = Part("LeftUpperArm", leftShoulder, 4, new Vector2(110, 86), new Vector2(.81f, .85f));
            leftElbow = Joint("LeftElbow", leftShoulder, -77, -69);
            parts[5] = Part("LeftForearm", leftElbow, 5, new Vector2(110, 82), new Vector2(.18f, .85f));
            leftWrist = Joint("LeftWrist", leftElbow, 82, -68);
            parts[6] = Part("LeftHand", leftWrist, 6, new Vector2(80, 54), new Vector2(.21f, .78f));

            skirtJoint = Joint("Skirt", rigRect, 0, 177);
            // The robe hangs below the waist. Pivoting it near its top also keeps
            // the leg sockets covered while the knees move independently.
            parts[2] = Part("SkirtArt", skirtJoint, 2, new Vector2(172, 156), new Vector2(.5f, .85f));
            torsoJoint = Joint("Torso", rigRect, 0, 189);
            parts[1] = Part("TorsoArt", torsoJoint, 1, new Vector2(181, 166), new Vector2(.5f, .08f));

            rightShoulder = Joint("RightShoulder", rigRect, 48, 329);
            parts[7] = Part("RightUpperArm", rightShoulder, 7, new Vector2(110, 86), new Vector2(.19f, .85f));
            rightElbow = Joint("RightElbow", rightShoulder, 77, -69);
            parts[8] = Part("RightForearm", rightElbow, 8, new Vector2(110, 82), new Vector2(.82f, .85f));
            rightWrist = Joint("RightWrist", rightElbow, -82, -68);
            parts[9] = Part("RightHand", rightWrist, 9, new Vector2(80, 54), new Vector2(.79f, .78f));

            skirtJoint.SetParent(torsoJoint, false);
            skirtJoint.anchoredPosition = new Vector2(0, -12);
            leftShoulder.SetParent(torsoJoint, false);
            rightShoulder.SetParent(torsoJoint, false);
            leftShoulder.anchoredPosition = new Vector2(-48, 140);
            rightShoulder.anchoredPosition = new Vector2(48, 140);
            headJoint = Joint("Neck", torsoJoint, 0, 129);
            parts[0] = Part("PaintedHead", headJoint, 0, new Vector2(150, 150), new Vector2(.5f, .24f));
            face = Part("FaceFeatures", headJoint, -1, new Vector2(84, 88), new Vector2(.5f, .5f));
            face.rectTransform.anchoredPosition = new Vector2(0, 31);
            frontHair = Part("HairStyle", headJoint, -1, new Vector2(185, 185), new Vector2(.5f, .5f));
            frontHair.rectTransform.anchoredPosition = new Vector2(0, 22);
            headgear = Part("PaintedHeadgear", headJoint, -1, new Vector2(140, 140), new Vector2(.5f, .5f));
            headgear.rectTransform.anchoredPosition = new Vector2(0, 95);
            heldWeapon = Part("PaintedHeldWeapon", rightWrist, -1, new Vector2(145, 145), new Vector2(.5f, .22f));
            heldWeapon.rectTransform.anchoredPosition = new Vector2(31, -5);
            backWeapon = Part("PaintedBackWeapon", rigRect, -1, new Vector2(160, 250), new Vector2(.5f, .5f));
            backWeapon.rectTransform.anchoredPosition = new Vector2(-55, 256);
            backWeapon.rectTransform.localRotation = Quaternion.Euler(0, 0, 30);
            backWeapon.transform.SetAsFirstSibling();
            waistAccessory = Part("PaintedWaistAccessory", skirtJoint, -1, new Vector2(78, 78), new Vector2(.5f, .5f));
            waistAccessory.rectTransform.anchoredPosition = new Vector2(77, -23);
        }

        private static RectTransform Joint(string name, RectTransform parent, float x, float y)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = parent.name == "PaintedRig" ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static Image Part(string name, RectTransform joint, int index, Vector2 size, Vector2 pivot)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(joint, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.pivot = pivot;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = Vector2.zero;
            // The atlas cells are landscape even for vertical limbs. Fitting the
            // entire cell inside a narrow limb rectangle left visible joint gaps.
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        public void SetLook(LookSpec look)
        {
            if (look == null) return;
            var isFemale = look.Get("g", "m") == "f";
            var selected = look.Int("preset", -1);
            if (selected < 0) selected = look.Int("template", 0);
            selected = Mathf.Clamp(selected, 0, 9);
            var changedTemplate = selected != template || isFemale != female;
            female = isFemale;
            template = selected;
            var body = Resources.Load<Texture2D>("Characters/Rig" + (female ? "Female" : "Male") + "PartsV3");
            var faces = Resources.Load<Texture2D>("Characters/Rig" + (female ? "Female" : "Male") + "Faces4V1");
            var hair = Resources.Load<Texture2D>("Characters/Rig" + (female ? "Female" : "Male") + "Hair4V1");
            if (body == null || faces == null || hair == null) return;
            body.filterMode = FilterMode.Bilinear;
            faces.filterMode = FilterMode.Bilinear;
            hair.filterMode = FilterMode.Bilinear;
            var skin = HeroSprites.ParseColor(look.Get("sk", "#f0d2b4"), new Color32(240, 210, 180, 255));
            var skinTint = new Color(Mathf.Min(1f, skin.r / .94f), Mathf.Min(1f, skin.g / .82f), Mathf.Min(1f, skin.b / .71f), 1f);
            var auraStyle = Mathf.Clamp(look.Int("au", 0), 0, 5);
            aura.enabled = auraStyle > 0;
            var auraColor = HeroSprites.ParseColor(look.Get("auc", "#8fe0ff"), new Color32(143, 224, 255, 255));
            aura.color = new Color(auraColor.r, auraColor.g, auraColor.b, .10f + auraStyle * .025f);
            aura.rectTransform.sizeDelta = new Vector2(210f + auraStyle * 9f, 290f + auraStyle * 11f);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                if (changedTemplate || parts[i].sprite == null) parts[i].sprite = Slice(body, i);
                var key = i == 3 ? "hc" : i == 1 ? "tc" : i == 2 ? "pc" : i == 12 || i == 15 ? "sc" : "oc";
                var chosen = HeroSprites.ParseColor(look.Get(key, "#e8e2d4"), Accents[template]);
                parts[i].color = Color.Lerp(Color.white, chosen, i == 3 ? .12f : .24f);
                if (i == 6 || i == 9) parts[i].color = skinTint;
            }
            parts[3].enabled = false;
            face.enabled = frontHair.enabled = false; // The full painted head includes both.
            partColors = new Color[parts.Length];
            for (var i = 0; i < parts.Length; i++) if (parts[i] != null) partColors[i] = parts[i].color;
            face.sprite = SliceGrid(faces, Mathf.Abs(look.Int("fa", 0)) % 4, 2, "face");
            faceBaseColor = Color.Lerp(Color.white, skinTint, .85f);
            face.color = faceBaseColor;
            var hairStyle = Mathf.Abs(look.Int("ha", 0));
            frontHair.sprite = SliceGrid(hair, hairStyle % 4, 2, "hair");
            frontHair.rectTransform.localScale = new Vector3(hairStyle >= 4 && hairStyle < 8 ? -1f : 1f,
                hairStyle >= 8 ? .85f : 1f, 1f);
            var hairColor = HeroSprites.ParseColor(look.Get("hc", "#24202c"), new Color32(36, 32, 44, 255));
            frontHair.color = Color.Lerp(Color.white, hairColor, .35f);
            var equipment = Resources.Load<Texture2D>("Characters/RigEquipment16V1");
            if (equipment != null)
            {
                equipment.filterMode = FilterMode.Bilinear;
                var hat = Mathf.Clamp(look.Int("hat", 0), 0, 5);
                headgear.enabled = hat > 0;
                if (hat > 0) headgear.sprite = SliceGrid(equipment, hat - 1, 4, "equipment");
                headgear.rectTransform.sizeDelta = hat == 2 ? new Vector2(196, 125) : new Vector2(140, 140);
                var weapon = Mathf.Clamp(look.Int("wp", 0), 0, 10);
                var item = new[] { -1, 5, 6, 6, 6, 12, 8, 11, 13, 14, 15 }[weapon];
                heldWeapon.enabled = weapon >= 2 && weapon <= 9;
                backWeapon.enabled = weapon == 1;
                if (heldWeapon.enabled) heldWeapon.sprite = SliceGrid(equipment, item, 4, "equipment");
                if (backWeapon.enabled) backWeapon.sprite = SliceGrid(equipment, item, 4, "equipment");
                heldWeapon.rectTransform.sizeDelta = weapon == 5 || weapon == 6 || weapon == 7 ? new Vector2(155, 180) : new Vector2(145, 145);
                heldWeapon.rectTransform.localRotation = Quaternion.Euler(0, 0, weapon == 3 ? 45f : weapon == 8 ? -25f : 0f);
                waistAccessory.enabled = look.Int("be", 0) == 2 || weapon == 10;
                if (waistAccessory.enabled) waistAccessory.sprite = SliceGrid(equipment, 15, 4, "equipment");
            }
            else headgear.enabled = heldWeapon.enabled = backWeapon.enabled = waistAccessory.enabled = false;
            ApplyBodyShape(Mathf.Clamp(look.Int("bo", 1), 0, 3));
        }

        private void ApplyBodyShape(int shape)
        {
            shoulderOffset = Vector2.zero;
            var shoulder = new[] { 42f, 48f, female ? 51f : 54f, female ? 54f : 58f }[shape];
            var hip = new[] { 23f, 28f, female ? 34f : 30f, female ? 40f : 35f }[shape];
            var chestScale = new[] { .84f, 1f, female ? 1.08f : 1.16f, female ? 1.13f : 1.24f }[shape];
            var hipScale = new[] { .84f, 1f, female ? 1.18f : 1.08f, female ? 1.32f : 1.18f }[shape];
            leftShoulder.anchoredPosition = new Vector2(-shoulder, 140f);
            rightShoulder.anchoredPosition = new Vector2(shoulder, 140f);
            leftHip.anchoredPosition = new Vector2(-hip, 180f);
            rightHip.anchoredPosition = new Vector2(hip, 180f);
            parts[1].rectTransform.localScale = new Vector3(chestScale, 1f, 1f);
            foreach (var image in new[] { parts[2], parts[10], parts[13] })
                image.rectTransform.localScale = new Vector3(hipScale, 1f, 1f);
            // The supplied right thigh is painted in the same diagonal as the
            // left one. Mirror it so both knee joints point out from the hips.
            parts[13].rectTransform.localScale = new Vector3(-hipScale, 1f, 1f);
        }

        private static Sprite Slice(Texture2D texture, int index)
        {
            var key = texture.name + "_body_" + index;
            if (SliceCache.TryGetValue(key, out var cached) && cached != null && cached.texture != null) return cached;
            var column = index % 4;
            var row = index / 4;
            var left = X[column]; var right = X[column + 1];
            var top = Y[row]; var bottom = Y[row + 1];
            var sprite = Sprite.Create(texture, new Rect(left, texture.height - bottom, right - left, bottom - top), new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            SliceCache[key] = sprite;
            return sprite;
        }

        private static Sprite SliceGrid(Texture2D texture, int index, int columns, string role)
        {
            var key = texture.name + "_" + role + "_" + index;
            if (SliceCache.TryGetValue(key, out var cached) && cached != null && cached.texture != null) return cached;
            var width = texture.width / (float)columns;
            var height = texture.height / (float)columns;
            var column = index % columns;
            var row = index / columns;
            var sprite = Sprite.Create(texture,
                new Rect(column * width, texture.height - (row + 1) * height, width, height),
                new Vector2(.5f, .5f), 100f);
            sprite.name = key;
            SliceCache[key] = sprite;
            return sprite;
        }

        public void SetMotion(FighterAction next, float actionProgress, bool isMoving, bool facesRight, float time)
        {
            action = next;
            progress = Mathf.Clamp01(actionProgress);
            moving = isMoving;
            faceRight = facesRight;
            motionTime = time;
            Pose();
        }

        public void SetHit(bool hit)
        {
            var tint = hit ? new Color(1f, .62f, .58f) : Color.white;
            for (var i = 0; i < parts.Length; i++)
                if (parts[i] != null && partColors != null) parts[i].color = partColors[i] * tint;
            if (face != null) face.color = faceBaseColor * tint;
        }

        private void LateUpdate()
        {
            var parent = rigRect != null ? rigRect.parent as RectTransform : null;
            if (parent != null)
            {
                var bounds = parent.rect.size;
                if (bounds.x > 0 && bounds.y > 0)
                    rigRect.localScale = new Vector3(faceRight ? -1f : 1f, 1f, 1f) * Mathf.Min(bounds.x / 250f, bounds.y / 430f) * .94f;
            }
            if (action == FighterAction.Idle && !moving)
            {
                motionTime = Time.unscaledTime;
                Pose();
            }
        }

        private void Pose()
        {
            if (rigRect == null) return;
            rigRect.localRotation = Quaternion.Euler(0, 0, action == FighterAction.Down ? (faceRight ? -1f : 1f) * 78f * Mathf.SmoothStep(0f, 1f, progress) : 0f);
            var t = motionTime;
            var breath = Mathf.Sin(t * 1.35f);
            var sway = Mathf.Sin(t * .72f);
            var walk = action == FighterAction.Walk || moving;
            var step = Mathf.Sin(t * 9f);
            var peak = Mathf.Sin(Mathf.PI * progress);
            var targetTorso = sway * 1.2f;
            var targetHead = -sway * 1.4f + breath * .4f;
            var targetHair = -sway * 3f + Mathf.Sin(t * .95f) * 2f;
            var targetSkirt = -sway * 2f;
            var targetLeftArm = -8f + breath * 2f;
            var targetRightArm = 8f - breath * 2f;
            var targetLeftElbow = 9f + Mathf.Sin(t * 1.1f) * 2f;
            var targetRightElbow = -9f - Mathf.Sin(t * 1.1f) * 2f;
            var targetLeftHand = 2f;
            var targetRightHand = -2f;
            var targetLeftLeg = 0f; var targetRightLeg = 0f;
            var targetLeftKnee = 8f; var targetRightKnee = 8f;
            var targetLeftFoot = 0f; var targetRightFoot = 0f;
            var targetOffset = new Vector2(sway * 1.6f, breath * 2f);

            if (walk)
            {
                targetTorso = -step * 2.5f;
                targetHead = step * 1.5f;
                targetHair = -step * 5f;
                targetSkirt = step * 6f;
                targetLeftLeg = step * 23f;
                targetRightLeg = -step * 23f;
                targetLeftKnee = 8f + Mathf.Max(0f, -step) * 31f;
                targetRightKnee = 8f + Mathf.Max(0f, step) * 31f;
                targetLeftFoot = -targetLeftLeg * .42f - targetLeftKnee * .25f;
                targetRightFoot = -targetRightLeg * .42f - targetRightKnee * .25f;
                targetLeftArm = -step * 20f;
                targetRightArm = step * 20f;
                targetLeftElbow = 15f + Mathf.Max(0f, step) * 12f;
                targetRightElbow = -15f - Mathf.Max(0f, -step) * 12f;
                targetOffset = new Vector2(0f, Mathf.Abs(step) * 5f);
            }
            if (action == FighterAction.Attack)
            {
                var windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .28f));
                var release = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - .28f) / .38f));
                var settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - .66f) / .34f));
                var swing = Mathf.Lerp(-34f * windup, 102f, release) * (1f - settle);
                targetRightArm = swing;
                targetRightElbow = -32f * windup + 66f * release * (1f - settle);
                targetRightHand = -targetRightElbow * .35f;
                targetLeftArm = -24f * peak;
                targetTorso = 11f * peak;
                targetHead = -targetTorso * .4f;
                targetLeftLeg = -12f * peak;
                targetRightLeg = 18f * peak;
                targetRightKnee = 22f * peak;
                targetSkirt = -targetTorso * .6f;
                targetHair = -targetTorso * .8f;
                targetOffset = new Vector2(-14f * peak, 3f * peak);
            }
            else if (action == FighterAction.Cast)
            {
                targetLeftArm = -55f * peak;
                targetRightArm = 55f * peak;
                targetLeftElbow = 38f * peak;
                targetRightElbow = -38f * peak;
                targetLeftHand = -16f * peak;
                targetRightHand = 16f * peak;
                targetTorso = sway * 2f;
                targetSkirt = Mathf.Sin(t * 4f) * 7f * peak;
                targetHair = -targetSkirt * .8f;
                targetOffset = new Vector2(0f, 12f * peak);
            }
            else if (action == FighterAction.Hurt)
            {
                targetTorso = -16f * peak;
                targetHead = -targetTorso * .5f;
                targetLeftArm = -35f * peak;
                targetRightArm = 35f * peak;
                targetLeftKnee = targetRightKnee = 24f * peak;
                targetOffset = new Vector2(10f * peak, -7f * peak);
            }
            else if (action == FighterAction.Down)
            {
                var fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
                targetTorso = 12f * fall;
                targetHead = -targetTorso * .25f;
                targetLeftArm = -65f * fall;
                targetRightArm = 70f * fall;
                targetLeftLeg = -30f * fall;
                targetRightLeg = 32f * fall;
                targetOffset = new Vector2(0f, -8f * fall);
            }

            var follow = 1f - Mathf.Exp(-18f * Mathf.Max(.001f, Time.unscaledDeltaTime));
            torsoAngle = Mathf.LerpAngle(torsoAngle, targetTorso, follow);
            headAngle = Mathf.LerpAngle(headAngle, targetHead, follow);
            hairAngle = Mathf.LerpAngle(hairAngle, targetHair, follow * .65f);
            skirtAngle = Mathf.LerpAngle(skirtAngle, targetSkirt, follow * .75f);
            leftArmAngle = Mathf.LerpAngle(leftArmAngle, targetLeftArm, follow);
            rightArmAngle = Mathf.LerpAngle(rightArmAngle, targetRightArm, follow);
            leftElbowAngle = Mathf.LerpAngle(leftElbowAngle, targetLeftElbow, follow);
            rightElbowAngle = Mathf.LerpAngle(rightElbowAngle, targetRightElbow, follow);
            leftHandAngle = Mathf.LerpAngle(leftHandAngle, targetLeftHand, follow);
            rightHandAngle = Mathf.LerpAngle(rightHandAngle, targetRightHand, follow);
            leftLegAngle = Mathf.LerpAngle(leftLegAngle, targetLeftLeg, follow);
            rightLegAngle = Mathf.LerpAngle(rightLegAngle, targetRightLeg, follow);
            leftKneeAngle = Mathf.LerpAngle(leftKneeAngle, targetLeftKnee, follow);
            rightKneeAngle = Mathf.LerpAngle(rightKneeAngle, targetRightKnee, follow);
            leftFootAngle = Mathf.LerpAngle(leftFootAngle, targetLeftFoot, follow);
            rightFootAngle = Mathf.LerpAngle(rightFootAngle, targetRightFoot, follow);
            bodyOffset = Vector2.Lerp(bodyOffset, targetOffset, follow);

            torsoJoint.localRotation = Quaternion.Euler(0, 0, torsoAngle);
            headJoint.localRotation = Quaternion.Euler(0, 0, headAngle);
            hairJoint.localRotation = Quaternion.Euler(0, 0, hairAngle);
            if (frontHair != null) frontHair.rectTransform.localRotation = Quaternion.Euler(0, 0, hairAngle * .18f);
            skirtJoint.localRotation = Quaternion.Euler(0, 0, skirtAngle);
            leftShoulder.localRotation = Quaternion.Euler(0, 0, leftArmAngle);
            rightShoulder.localRotation = Quaternion.Euler(0, 0, rightArmAngle);
            leftElbow.localRotation = Quaternion.Euler(0, 0, leftElbowAngle);
            rightElbow.localRotation = Quaternion.Euler(0, 0, rightElbowAngle);
            leftWrist.localRotation = Quaternion.Euler(0, 0, leftHandAngle);
            rightWrist.localRotation = Quaternion.Euler(0, 0, rightHandAngle);
            leftHip.localRotation = Quaternion.Euler(0, 0, leftLegAngle);
            rightHip.localRotation = Quaternion.Euler(0, 0, rightLegAngle);
            leftKnee.localRotation = Quaternion.Euler(0, 0, leftKneeAngle);
            rightKnee.localRotation = Quaternion.Euler(0, 0, rightKneeAngle);
            leftAnkle.localRotation = Quaternion.Euler(0, 0, leftFootAngle);
            rightAnkle.localRotation = Quaternion.Euler(0, 0, rightFootAngle);
            torsoJoint.anchoredPosition = new Vector2(bodyOffset.x, 189f + bodyOffset.y);
            leftHip.anchoredPosition = new Vector2(leftHip.anchoredPosition.x - shoulderOffset.x, 180f) + bodyOffset;
            rightHip.anchoredPosition = new Vector2(rightHip.anchoredPosition.x - shoulderOffset.x, 180f) + bodyOffset;
            shoulderOffset = bodyOffset;
            skirtJoint.anchoredPosition = new Vector2(0, -12f);
            hairJoint.anchoredPosition = new Vector2(bodyOffset.x * .7f, 308f + bodyOffset.y);
        }
    }
}
