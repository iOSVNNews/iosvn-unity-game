using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    // Use the same illustration as the game actor, with a still upper-body crop.
    // Cropping preserves the complete width, including sleeves and loose hair.
    internal sealed class CultivatorPuppet2D : MonoBehaviour
    {
        public static bool Available => QcbhSkinnedActor2D.Available;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private const float PortraitBottom = .40f;
        private RectTransform root;
        private AnimatedPortraitImage body;
        private Image aura, hat, weapon;
        private bool facesRight;

        public static Sprite Portrait(LookSpec look)
        {
            var texture = QcbhSkinnedActor2D.ActorAtlas(look);
            if (texture == null) return null;
            var female = look != null && look.Get("g", "m") == "f";
            var key = texture.name + (female ? "_female_face" : "_male_face");
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var half = texture.width * .5f;
            var center = texture.name == "FullBodyActorsV3"
                ? new Vector2(female ? .43f : .51f, female ? .84f : .86f)
                : CharacterAppearance.FaceCenter(female, false);
            var size = texture.height * .23f;
            sprite = Sprite.Create(texture, new Rect((female ? half : 0) + half * center.x - size * .5f,
                texture.height * center.y - size * .5f, size, size), new Vector2(.5f, .5f), 100);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        public static CultivatorPuppet2D Create(RectTransform parent, LookSpec look)
        {
            var rect = new GameObject("CultivatorPuppet", typeof(RectTransform), typeof(CultivatorPuppet2D)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
            var puppet = rect.GetComponent<CultivatorPuppet2D>();
            puppet.root = rect;
            puppet.aura = Layer<Image>("Aura", rect);
            puppet.body = Layer<AnimatedPortraitImage>("Body", rect);
            puppet.body.PreservePaintedShape = true;
            puppet.body.preserveAspect = false;
            // Accessories are separate from the still painted face.
            puppet.hat = Layer<Image>("Headwear", rect);
            puppet.weapon = Layer<Image>("Weapon", rect);
            puppet.SetLook(look);
            return puppet;
        }

        private static T Layer<T>(string name, RectTransform parent) where T : Image
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static Sprite Equipment(Texture2D texture, int index)
        {
            var key = texture.name + "_equipment_" + index;
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var cell = texture.width * .25f;
            sprite = Sprite.Create(texture, new Rect(index % 4 * cell, texture.height - (index / 4 + 1) * cell, cell, cell), new Vector2(.5f, .5f), 100);
            Sprites[key] = sprite;
            return sprite;
        }

        private void Place(Image image, Vector2 atlasPoint, Vector2 size)
        {
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, 0);
            image.rectTransform.pivot = new Vector2(.5f, .5f);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = new Vector2((atlasPoint.x - .5f) * root.sizeDelta.x,
                (atlasPoint.y - PortraitBottom) / (1 - PortraitBottom) * root.sizeDelta.y);
        }

        public void SetLook(LookSpec look)
        {
            if (look == null) return;
            var texture = QcbhSkinnedActor2D.ActorAtlas(look);
            if (texture == null) return;
            var female = look.Get("g", "m") == "f";
            var half = texture.width * .5f;
            var height = texture.height * (1 - PortraitBottom);
            root.sizeDelta = new Vector2(430 * half / height, 430);
            var key = texture.name + (female ? "_female_portrait" : "_male_portrait");
            if (!Sprites.TryGetValue(key, out var sprite) || sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(female ? half : 0, texture.height * PortraitBottom, half, height), new Vector2(.5f, .5f), 100);
                sprite.name = key;
                Sprites[key] = sprite;
            }
            body.sprite = sprite;
            body.color = Color.white;
            body.SetAppearance(look);
            CharacterAppearance.Apply(body, look);
            var equipment = Resources.Load<Texture2D>("Characters/RigEquipment16V1");
            var hatStyle = Mathf.Clamp(look.Int("hat", 0), 0, 5);
            hat.enabled = equipment != null && hatStyle > 0;
            if (hat.enabled)
            {
                hat.sprite = Equipment(equipment, hatStyle - 1);
                hat.color = HeroSprites.ParseColor(look.Get("hac", "#e2c57b"), Color.white);
                Place(hat, new Vector2(female ? .53f : .56f, female ? .94f : .965f), new Vector2(64, 64));
            }
            var weaponStyle = Mathf.Clamp(look.Int("wp", 0), 0, 10);
            weapon.enabled = equipment != null && weaponStyle > 0;
            if (weapon.enabled)
            {
                var index = new[] { 0, 6, 6, 6, 6, 12, 8, 11, 13, 14, 15 }[weaponStyle];
                weapon.sprite = Equipment(equipment, index);
                Place(weapon, weaponStyle == 1 ? new Vector2(.39f, .73f) : new Vector2(.72f, .52f), new Vector2(135, 135));
                weapon.rectTransform.localRotation = Quaternion.Euler(0, 0, weaponStyle == 1 ? 35 : -16);
                if (weaponStyle == 1) weapon.transform.SetSiblingIndex(1); else weapon.transform.SetAsLastSibling();
            }
            var auraStyle = Mathf.Clamp(look.Int("au", 0), 0, 5);
            aura.enabled = auraStyle > 0;
            aura.sprite = auraStyle == 2 || auraStyle == 4 ? InkUi.Ring : auraStyle == 3 ? InkUi.Cloud : InkUi.Glow;
            var color = HeroSprites.ParseColor(look.Get("auc", "#8fe0ff"), Color.white);
            aura.color = new Color(color.r, color.g, color.b, .10f + auraStyle * .02f);
        }

        public void SetMotion(FighterAction next, float progress, bool moving, bool faceRight, float time)
        {
            facesRight = faceRight;
            // Portraits keep their original drawing even when the game actor moves.
            body.SetMotion(FighterAction.Idle, 0, false);
        }

        public void SetHit(bool hit) => body.color = hit ? new Color(1, .62f, .58f) : Color.white;

        private void LateUpdate()
        {
            var parent = root.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            root.localScale = new Vector3(facesRight ? -1 : 1, 1, 1)
                * Mathf.Min(parent.rect.width / root.sizeDelta.x, parent.rect.height / root.sizeDelta.y) * .94f;
        }
    }
}