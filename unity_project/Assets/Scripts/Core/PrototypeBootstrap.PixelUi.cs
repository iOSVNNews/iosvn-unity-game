using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// The Telegram game never shows raw emoji: it maps them to its 32x32 HUD pixel icons
    /// (public/icons/ui). Unity's text renderer cannot draw colour emoji either, so the IPA
    /// uses the same mapping and the same icons, imported into Resources/PixelArt/UI.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        // Mirrors TOWN_ICON_PIXEL in tutien/public/app.js, plus the other emoji used by catalog data and this client.
        private static readonly Dictionary<string, string> EmojiPixelIcons = new Dictionary<string, string>
        {
            { "🏡", "dong_phu" }, { "🏯", "dong_phu" }, { "🍁", "phuong_thi" }, { "🏘", "phuong_thi" }, { "⚔", "swords" },
            { "🏰", "dong_phu" }, { "🏺", "co_dong" }, { "⛰", "road" }, { "⛵", "road" }, { "🌲", "dong_phu" },
            { "⛩", "tong_mon" }, { "🌊", "road" }, { "👑", "power" }, { "🌫", "road" }, { "🏝", "road" }, { "⚓", "road" },
            { "✨", "sun" }, { "💀", "world_boss" }, { "🗿", "co_dong" }, { "❄", "moon" }, { "⚡", "spd" }, { "🍃", "dan_duoc" },
            { "🔥", "fire" }, { "🌋", "fire" }, { "💔", "heart" }, { "👹", "world_boss" }, { "🌅", "sun" }, { "🪐", "moon" },
            { "☯", "bat_quai" }, { "🦅", "mon_thu" }, { "🌌", "moon" }, { "🏛", "dong_phu" }, { "☸", "bat_quai" }, { "🌸", "can_cot" },
            { "🏙", "phuong_thi" }, { "🌍", "road" }, { "🌎", "road" }, { "🌏", "road" }, { "🏔", "co_dong" }, { "🗺", "road" },
            { "🐉", "world_boss" }, { "🌪", "spd" }, { "🏮", "phuong_thi" }, { "📜", "scroll" }, { "🩸", "injury" }, { "🌑", "moon" },
            { "🌠", "sun" }, { "🪷", "can_cot" }, { "💠", "bat_quai" }, { "🌀", "teleport" }, { "⌛", "hourglass" }, { "🛡", "def" },
            { "🗡", "mon_kiem" }, { "📍", "location" }, { "🐾", "san_yeu" }, { "🌾", "nguyen_lieu" }, { "🌙", "moon" },
        };

        private static Sprite UiPixelIcon(string id) => string.IsNullOrEmpty(id) ? null : LoadPixelIcon("PixelArt/UI/" + id);

        /// <summary>Pixel icon for a catalog emoji such as town.icon, with the same fallback as the Telegram app.</summary>
        private static string PixelIconForEmoji(string emoji, string fallback = "phuong_thi")
        {
            if (string.IsNullOrEmpty(emoji)) return fallback;
            var key = emoji.Replace("️", string.Empty).Replace("‍", string.Empty).Trim();
            return EmojiPixelIcons.TryGetValue(key, out var id) ? id : fallback;
        }

        /// <summary>Removes colour emoji (which the legacy text renderer cannot draw) and tidies the spacing.</summary>
        private static string StripEmoji(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) { i++; continue; }
                if (c == '️' || c == '‍' || c == '⃣') continue;
                if (c >= '☀' && c <= '➿' && c != '✓' && c != '✦' && c != '✧') continue;
                if (c >= '⌀' && c <= '⏿') continue;
                if (c >= '⬀' && c <= '⯿') continue;
                builder.Append(c);
            }
            var text = builder.ToString();
            while (text.Contains("  ")) text = text.Replace("  ", " ");
            return text.Replace("\n ", "\n").Trim();
        }

        /// <summary>Leading emoji of a status line mapped to its pixel icon, or null.</summary>
        private static string LeadingPixelIcon(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            var trimmed = value.TrimStart();
            var length = char.IsHighSurrogate(trimmed[0]) && trimmed.Length > 1 ? 2 : 1;
            var key = trimmed.Substring(0, length);
            return EmojiPixelIcons.TryGetValue(key, out var id) ? id : null;
        }

        /// <summary>Adds a pixel icon as the first child of a horizontal layout row.</summary>
        private static Image AddRowPixelIcon(GameObject row, Sprite sprite, float size)
        {
            if (row == null || sprite == null) return null;
            var obj = new GameObject("PixelIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            obj.transform.SetParent(row.transform, false);
            obj.transform.SetSiblingIndex(0);
            var image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = obj.GetComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = size;
            element.preferredHeight = element.minHeight = size;
            element.flexibleWidth = 0f;
            return image;
        }

        /// <summary>Places a pixel icon inside an anchored rect (used by fixed HUD layouts).</summary>
        private static Image PlacePixelIcon(Transform parent, string iconId, Vector2 min, Vector2 max)
        {
            var sprite = UiPixelIcon(iconId);
            if (parent == null || sprite == null) return null;
            var obj = new GameObject("PixelIcon_" + iconId, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), min, max);
            var image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Puts a pixel icon at the left edge of a button and shifts its label to make room.</summary>
        private static void AddButtonPixelIcon(Button button, string iconId)
        {
            var sprite = UiPixelIcon(iconId);
            if (button == null || sprite == null) return;
            var obj = new GameObject("PixelIcon_" + iconId, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            obj.transform.SetParent(button.transform, false);
            var iconRect = obj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, .12f);
            iconRect.anchorMax = new Vector2(0f, .88f);
            iconRect.pivot = new Vector2(0f, .5f);
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            var fitter = obj.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            var image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var label = button.transform.Find("Text") as RectTransform;
            if (label != null) label.offsetMin = new Vector2(Mathf.Max(label.offsetMin.x, 54f), label.offsetMin.y);
        }
    }
}
