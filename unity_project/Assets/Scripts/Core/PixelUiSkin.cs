using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Shared crisp, nine-sliced pixel trim for mobile HUD panels and controls.</summary>
    internal static class PixelUiSkin
    {
        private static Sprite frame;

        public static void ApplyFrame(GameObject target)
        {
            if (target == null) return;
            if (frame == null) frame = BuildFrame();
            if (target.transform.Find("PixelFrame") != null) return;

            var overlay = new GameObject("PixelFrame", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(target.transform, false);
            overlay.transform.SetAsFirstSibling();
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = overlay.GetComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.raycastTarget = false;
        }

        public static bool NeedsFrame(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Card") || name.Contains("Panel") || name.Contains("Hud") ||
                name.Contains("Header") || name.Contains("Frame") || name.Contains("Parchment") ||
                name == "BattleLocation" || name == "EnemyStatus" || name == "BattleWarning" ||
                name == "PlayerCombatHUD" || name == "LastCombatAction" ||
                name.StartsWith("Target_") || name.StartsWith("BattleMap_") || name.StartsWith("Town_") ||
                name.StartsWith("Dungeon_") || name.StartsWith("Opponent_") || name.StartsWith("Title_") ||
                name.StartsWith("Mode_") || name == "ModeMapOverview" || name == "Info";
        }

        public static void ApplyTextTreatment(Text text)
        {
            if (text == null) return;
            var outline = text.GetComponent<Outline>();
            if (outline == null) outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(14, 15, 17, 225);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        private static Sprite BuildFrame()
        {
            const int size = 32;
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;

            var edge = new Color32(25, 23, 23, 255);
            var shadow = new Color32(94, 66, 38, 255);
            var gold = new Color32(202, 157, 83, 255);
            var shine = new Color32(244, 214, 144, 255);
            var muted = new Color32(137, 103, 58, 255);

            for (var i = 0; i < size; i++)
            {
                Put(pixels, size, i, 0, edge); Put(pixels, size, i, 1, shadow); Put(pixels, size, i, 2, gold);
                Put(pixels, size, i, size - 1, edge); Put(pixels, size, i, size - 2, shadow);
                Put(pixels, size, i, size - 3, muted);
                Put(pixels, size, 0, i, edge); Put(pixels, size, 1, i, shadow); Put(pixels, size, 2, i, gold);
                Put(pixels, size, size - 1, i, edge); Put(pixels, size, size - 2, i, shadow);
                Put(pixels, size, size - 3, i, muted);
            }

            // Four inset corner seals remain crisp while the middle edges stretch.
            for (var corner = 0; corner < 4; corner++)
            {
                var left = (corner & 1) == 0;
                var top = (corner & 2) == 0;
                var ox = left ? 3 : size - 4;
                var oy = top ? 3 : size - 4;
                var sx = left ? 1 : -1;
                var sy = top ? 1 : -1;
                Put(pixels, size, ox, oy, shine);
                Put(pixels, size, ox + sx, oy, gold); Put(pixels, size, ox - sx, oy, gold);
                Put(pixels, size, ox, oy + sy, gold); Put(pixels, size, ox, oy - sy, gold);
                Put(pixels, size, ox + sx, oy + sy, shadow); Put(pixels, size, ox - sx, oy - sy, shadow);
                Put(pixels, size, ox + sx * 2, oy + sy, muted); Put(pixels, size, ox, oy + sy * 2, muted);
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "TuTien_PixelPanelFrame",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            frame = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size,
                0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
            frame.name = "TuTien_PixelPanelFrame_Sliced";
            frame.hideFlags = HideFlags.HideAndDontSave;
            return frame;
        }

        private static void Put(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            if (x >= 0 && x < width && y >= 0 && y < width) pixels[y * width + x] = color;
        }
    }
}
