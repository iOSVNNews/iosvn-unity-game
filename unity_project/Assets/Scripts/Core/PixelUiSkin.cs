using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Shared crisp, nine-sliced pixel trim for mobile HUD panels and controls.</summary>
    internal static class PixelUiSkin
    {
        public static void ApplyFrame(GameObject target)
        {
            if (target == null) return;
            if (target.transform.Find("PixelFrame") != null) return;

            var overlay = new GameObject("PixelFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            // Decoration only: never let a layout group treat the frame as a row item.
            overlay.GetComponent<LayoutElement>().ignoreLayout = true;
            overlay.transform.SetParent(target.transform, false);
            overlay.transform.SetAsFirstSibling();
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = overlay.GetComponent<Image>();
            ModernUi.Ring(image, 14f, 1f);
            image.color = new Color32(224, 190, 126, 170);
            image.raycastTarget = false;
        }

        public static bool NeedsFrame(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Card") || name.Contains("Panel") || name.Contains("Hud") ||
                name.Contains("Header") || name.Contains("Frame") || name.Contains("Parchment") || name.Contains("TopBar") ||
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

    }
}
