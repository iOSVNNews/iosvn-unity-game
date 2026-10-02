using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Shared scaffold for the native system screens: glass top bar with back button and
    /// resource chips, segmented tabs, scroll lists and slot grids, a detail panel with
    /// actions, modal dialogs and toasts. Every screen reads the dynamic server views (J).
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private static readonly Color[] RarityPalette =
        {
            new Color32(169, 163, 145, 255), new Color32(127, 208, 138, 255), new Color32(100, 181, 240, 255),
            new Color32(182, 156, 255, 255), new Color32(240, 162, 78, 255), new Color32(255, 106, 92, 255), new Color32(241, 209, 90, 255),
        };
        private static readonly Color SurfaceRow = new Color32(255, 255, 255, 11);
        private static readonly Color SurfaceSelected = new Color32(225, 185, 104, 40);
        private static readonly Color PositiveText = new Color32(150, 220, 160, 255);

        private J hub;                 // latest dynamic player view (GET /api/state)
        private bool actionPending;
        private RectTransform modalRoot;
        private RectTransform toastRoot;
        private Image busySpinner;

        // ================================================================ data helpers

        private static string Vn(double value) => Math.Round(value).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
        private static string Vn(J value) => Vn(value.Num());

        private static Dictionary<string, object> Body(params object[] pairs)
        {
            var body = new Dictionary<string, object>();
            for (var i = 0; i + 1 < pairs.Length; i += 2) body[(string)pairs[i]] = pairs[i + 1];
            return body;
        }

        private static int RarityIndex(J item)
        {
            switch (item["tier"].Str())
            {
                case "pham": return 0; case "hoang": return 1; case "huyen": return 2; case "dia": return 3; case "thien": return 4; case "tien": return 5;
            }
            switch (item["rarity"].Str())
            {
                case "pt": return 0; case "hiem": return 1; case "cuchiem": return 2; case "tt": return 3; case "cam": return 4; case "docban": return 5; case "vang": return 6;
            }
            var rank = item["qualityRank"].Int(-1);
            return rank < 0 ? 0 : Mathf.Clamp(rank, 0, 5);
        }

        private static Color RarityColor(J item) => RarityPalette[RarityIndex(item)];

        private static Sprite ItemSprite(J item, string fallbackUi = "backpack")
        {
            var id = item["id"].Str();
            var sprite = string.IsNullOrEmpty(id) ? null : LoadPixelIcon("PixelArt/Items/" + id);
            if (sprite == null && !string.IsNullOrEmpty(item["monsterId"].Str())) sprite = LoadPixelIcon("PixelArt/Monsters/" + item["monsterId"].Str());
            return sprite ?? UiPixelIcon(fallbackUi);
        }

        private static Sprite MonsterSprite(string monsterId) => string.IsNullOrEmpty(monsterId) ? UiPixelIcon("san_yeu") : (LoadPixelIcon("PixelArt/Monsters/" + monsterId) ?? UiPixelIcon("san_yeu"));

        private static string Clean(J value) => StripEmoji(value.Str());
        private static string Clean(string value) => StripEmoji(value ?? string.Empty);

        private static string FormatDuration(double seconds)
        {
            var s = Math.Max(0, (long)Math.Round(seconds));
            if (s >= 86400) return $"{s / 86400} ngày {s % 86400 / 3600} giờ";
            if (s >= 3600) return $"{s / 3600} giờ {s % 3600 / 60} phút";
            if (s >= 60) return $"{s / 60} phút {s % 60} giây";
            return $"{s} giây";
        }

        private double ServerNow => hub["now"].Num(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        // ================================================================ server calls

        private void Fetch(string route, Action<J> then)
        {
            ShowBusy(true);
            client.Get(route, (data, error) =>
            {
                ShowBusy(false);
                if (error != null) { Toast(error, true); return; }
                then?.Invoke(data);
            });
        }

        /// <summary>POSTs an action, refreshes the cached player view, shows the server toast and opens fights.</summary>
        private void Act(string route, Dictionary<string, object> body, Action<J> after = null)
        {
            if (actionPending) return;
            actionPending = true;
            ShowBusy(true);
            client.Post(route, body, (result, error) =>
            {
                actionPending = false;
                ShowBusy(false);
                if (error != null) { Toast(error, true); return; }
                if (result["state"].IsObject) AcceptState(result["state"]);
                else if (result["player"].IsObject && result["realm"].IsObject) AcceptState(result);
                var toast = result["toast"].Str();
                if (!string.IsNullOrEmpty(toast)) Toast(Clean(toast));
                var kind = result["battleKind"].Str();
                if ((kind == "pve" || kind == "pvp") && result["battle"].IsObject) { OpenBattle(kind, result["battle"]); return; }
                after?.Invoke(result);
            });
        }

        private void AcceptState(J state)
        {
            hub = state;
            var typed = NetworkGameClient.ToGameState(state);
            if (typed != null) latestState = typed;
        }

        private void OpenBattle(string kind, J battle)
        {
            var json = Json.Serialize(battle.Raw);
            if (kind == "pvp") ShowPvpBattle(JsonUtility.FromJson<PvpBattle>(json));
            else ShowBattle(JsonUtility.FromJson<BattleView>(json));
        }

        /// <summary>Reloads the player view, then runs <paramref name="then"/> (defaults to the hub).</summary>
        private void RefreshHub(Action then = null)
        {
            ShowBusy(true);
            client.LoadStateBoth((typed, raw, error) =>
            {
                ShowBusy(false);
                if (error != null) { Toast(error, true); return; }
                hub = raw;
                if (typed != null) latestState = typed;
                if (then != null) then(); else ShowHub();
            });
        }

        /// <summary>Interim hub: the classic home until the modern hub replaces it.</summary>
        private void ShowHub() { if (latestState != null) ShowHome(latestState); else LoadState(); }

        // ================================================================ scaffold

        /// <summary>Clears the screen and builds the glass frame. Returns the body area under the top bar.</summary>
        private RectTransform OpenScreen(string title, string subtitle, string iconId, Action back)
        {
            ClearContent();
            authBackdrop = LoginBackdrop.Create(backgroundRoot, Resources.Load<Texture2D>("Brand/LoginLandscapePixel"));
            var dim = new GameObject("ScreenDim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(authBackdrop.transform, false);
            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero; dimRect.anchorMax = Vector2.one; dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = Color.white;
            dimImage.raycastTarget = false;
            UiGradient.Apply(dimImage, new Color32(5, 9, 14, 214), new Color32(3, 6, 10, 236));

            var root = AuthStretch("SystemScreen", content.transform);
            var top = Anchored("TopBar", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -112), Vector2.zero);
            GlassPanel(top, 26f, new Color32(15, 24, 32, 228), new Color32(9, 15, 21, 226));

            var backButton = IconButton(top, "arrowLeft", new Vector2(0, .5f), new Vector2(16, 0), 80f, back ?? (() => ShowHub()));
            backButton.name = "Back";
            var iconRect = Anchored("ScreenIcon", top, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(112, -32), new Vector2(176, 32));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = UiPixelIcon(iconId);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var heading = AnchoredText(top, "Title", Clean(title), ModernUi.Display, 40, AuthGoldTop, TextAnchor.LowerLeft,
                new Vector2(0, .5f), new Vector2(.62f, 1), new Vector2(192, -4), new Vector2(0, -12));
            UiGradient.Apply(heading, AuthGoldTop, AuthGoldBottom);
            AnchoredText(top, "Subtitle", Clean(subtitle), ModernUi.Regular, 22, AuthTextSecondary, TextAnchor.UpperLeft,
                new Vector2(0, 0), new Vector2(.62f, .5f), new Vector2(194, 12), new Vector2(0, -2));
            ResourceChips(top);

            var body = Anchored("Body", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -128));
            BuildOverlays();
            return body;
        }

        private void ResourceChips(RectTransform top)
        {
            var player = hub["player"];
            var chips = new List<(string icon, string text)>
            {
                ("coin", Vn(player["stones"])),
                ("stamina", $"{player["stamina"].Long()}/{player["staminaMax"].Long()}"),
                ("heart", $"{Vn(player["hp"])}/{Vn(player["maxHp"])}"),
            };
            var x = -20f;
            for (var i = chips.Count - 1; i >= 0; i--)
            {
                var (iconId, value) = chips[i];
                var width = 70f + value.Length * 15f;
                var chip = Anchored("Chip_" + iconId, top, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(x - width, -30), new Vector2(x, 30));
                var fill = chip.gameObject.AddComponent<Image>();
                ModernUi.Fill(fill, 30f);
                fill.color = new Color32(0, 0, 0, 90);
                fill.raycastTarget = false;
                var glyph = Anchored("Icon", chip, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -20), new Vector2(52, 20)).gameObject.AddComponent<Image>();
                glyph.sprite = UiPixelIcon(iconId);
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                AnchoredText(chip, "Value", value, ModernUi.SemiBold, 24, AuthTextPrimary, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(58, 0), new Vector2(-10, 0));
                x -= width + 12f;
            }
        }

        /// <summary>Segmented tabs at the top of <paramref name="area"/>; returns the area below them.</summary>
        private RectTransform Tabs(RectTransform area, string[] labels, int active, Action<int> select)
        {
            var bar = Anchored("Tabs", area, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), Vector2.zero);
            var fill = bar.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 22f);
            fill.color = new Color32(2, 7, 12, 150);
            fill.raycastTarget = false;
            var count = Mathf.Max(1, labels.Length);
            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var cell = Anchored("Tab_" + i, bar, new Vector2((float)i / count, 0), new Vector2((float)(i + 1) / count, 1), new Vector2(5, 6), new Vector2(-5, -6));
                var image = cell.gameObject.AddComponent<Image>();
                ModernUi.Fill(image, 17f);
                if (i == active) UiGradient.Apply(image, AuthGoldTop, AuthGoldBottom);
                else image.color = new Color(1, 1, 1, 0);
                var button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = image;
                if (i != active) button.onClick.AddListener(() => select?.Invoke(index));
                var label = AnchoredText(cell, "Text", labels[i], ModernUi.SemiBold, 24, i == active ? AuthInkOnGold : AuthTextSecondary, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 16;
                label.resizeTextMaxSize = 24;
            }
            return Anchored("BelowTabs", area, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -90));
        }

        /// <summary>Splits an area into a list (left) and a detail panel (right).</summary>
        private (RectTransform list, RectTransform detail) Split(RectTransform area, float listFraction = .58f)
        {
            var list = Anchored("ListPane", area, Vector2.zero, new Vector2(listFraction, 1), Vector2.zero, new Vector2(-10, 0));
            var detail = Anchored("DetailPane", area, new Vector2(listFraction, 0), Vector2.one, new Vector2(10, 0), Vector2.zero);
            return (list, detail);
        }

        private void GlassPanel(RectTransform rect, float radius, Color top, Color bottom)
        {
            var fill = rect.gameObject.GetComponent<Image>();
            if (fill == null) fill = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, radius);
            fill.color = Color.white;
            fill.raycastTarget = false;
            UiGradient.Apply(fill, top, bottom);
            var edge = Anchored("Edge", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(edge, radius, 1.4f);
            edge.raycastTarget = false;
            UiGradient.Apply(edge, new Color32(242, 208, 136, 110), new Color32(225, 185, 104, 26));
        }

        /// <summary>Scrollable vertical stack; returns the transform rows are added to.</summary>
        private Transform ScrollColumn(RectTransform area, float spacing = 10f, int padding = 12)
        {
            var viewport = Anchored("Scroll", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 40f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            rows.SetParent(viewport, false);
            rows.anchorMin = new Vector2(0, 1);
            rows.anchorMax = new Vector2(1, 1);
            rows.pivot = new Vector2(.5f, 1);
            rows.offsetMin = rows.offsetMax = Vector2.zero;
            var layout = rows.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            rows.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = rows;
            return rows;
        }

        /// <summary>Scrollable grid of square slots (inventory style).</summary>
        private Transform ScrollGrid(RectTransform area, float cell = 132f, float spacing = 12f)
        {
            var viewport = Anchored("Scroll", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            grid.SetParent(viewport, false);
            grid.anchorMin = new Vector2(0, 1);
            grid.anchorMax = new Vector2(1, 1);
            grid.pivot = new Vector2(.5f, 1);
            grid.offsetMin = grid.offsetMax = Vector2.zero;
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cell, cell);
            layout.spacing = new Vector2(spacing, spacing);
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.constraint = GridLayoutGroup.Constraint.Flexible;
            layout.childAlignment = TextAnchor.UpperLeft;
            grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = grid;
            return grid;
        }

        // ================================================================ rows & slots

        private Button Row(Transform list, Sprite icon, Color accent, string title, string subtitle, string right = null, string rightSub = null,
            bool selected = false, Action click = null, float height = 108f, bool dim = false)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.SetParent(list, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            var fill = row.GetComponent<Image>();
            ModernUi.Fill(fill, 18f);
            fill.color = selected ? SurfaceSelected : SurfaceRow;
            var edge = Anchored("Edge", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(edge, 18f, 1.3f);
            edge.color = selected ? new Color32(240, 200, 120, 200) : new Color32(255, 255, 255, 16);
            edge.raycastTarget = false;

            var textLeft = 24f;
            if (icon != null)
            {
                var slotSize = height - 24f;
                var slot = Anchored("Slot", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -slotSize / 2), new Vector2(12 + slotSize, slotSize / 2));
                IconSlot(slot, icon, accent, slotSize);
                textLeft = 28f + slotSize;
            }
            var rightWidth = string.IsNullOrEmpty(right) && string.IsNullOrEmpty(rightSub) ? 16f : 230f;
            var hasSub = !string.IsNullOrEmpty(subtitle);
            var titleText = AnchoredText(row, "Title", Clean(title), ModernUi.SemiBold, 26, dim ? AuthTextTertiary : AuthTextPrimary, hasSub ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft,
                new Vector2(0, hasSub ? .5f : 0), Vector2.one, new Vector2(textLeft, hasSub ? 0 : 0), new Vector2(-rightWidth, hasSub ? -6 : 0));
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            ClipText(titleText);
            if (hasSub)
            {
                var sub = AnchoredText(row, "Subtitle", Clean(subtitle), ModernUi.Regular, 21, AuthTextSecondary, TextAnchor.UpperLeft,
                    Vector2.zero, new Vector2(1, .5f), new Vector2(textLeft, 4), new Vector2(-rightWidth, 0));
                sub.verticalOverflow = VerticalWrapMode.Truncate;
            }
            if (!string.IsNullOrEmpty(right))
                AnchoredText(row, "Right", Clean(right), ModernUi.SemiBold, 24, AuthGoldAccent, string.IsNullOrEmpty(rightSub) ? TextAnchor.MiddleRight : TextAnchor.LowerRight,
                    new Vector2(1, string.IsNullOrEmpty(rightSub) ? 0 : .5f), Vector2.one, new Vector2(-rightWidth + 8, 0), new Vector2(-18, string.IsNullOrEmpty(rightSub) ? 0 : -4));
            if (!string.IsNullOrEmpty(rightSub))
                AnchoredText(row, "RightSub", Clean(rightSub), ModernUi.Regular, 20, AuthTextTertiary, string.IsNullOrEmpty(right) ? TextAnchor.MiddleRight : TextAnchor.UpperRight,
                    Vector2.zero, new Vector2(1, string.IsNullOrEmpty(right) ? 1 : .5f), new Vector2(-rightWidth + 8, 2), new Vector2(-18, 0));

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.05f);
            colors.pressedColor = new Color(.8f, .78f, .74f);
            button.colors = colors;
            if (click != null) button.onClick.AddListener(() => click());
            else button.interactable = false;
            row.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        /// <summary>Rounded icon slot with a rarity-coloured rim.</summary>
        private static void IconSlot(RectTransform slot, Sprite icon, Color accent, float size)
        {
            var back = slot.gameObject.AddComponent<Image>();
            ModernUi.Fill(back, Mathf.Min(18f, size * .2f));
            back.color = new Color(0, 0, 0, .38f);
            back.raycastTarget = false;
            var rim = Anchored("Rim", slot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(rim, Mathf.Min(18f, size * .2f), 2f);
            rim.color = new Color(accent.r, accent.g, accent.b, .85f);
            rim.raycastTarget = false;
            var image = Anchored("Icon", slot, Vector2.zero, Vector2.one, new Vector2(size * .12f, size * .12f), new Vector2(-size * .12f, -size * .12f)).gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private Button Slot(Transform grid, Sprite icon, Color accent, string qty, bool selected, Action click, string badge = null)
        {
            var cell = new GameObject("Slot", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cell.SetParent(grid, false);
            var fill = cell.GetComponent<Image>();
            ModernUi.Fill(fill, 20f);
            fill.color = selected ? SurfaceSelected : new Color32(0, 0, 0, 70);
            var rim = Anchored("Rim", cell, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(rim, 20f, selected ? 3f : 2f);
            rim.color = selected ? (Color)new Color32(250, 214, 140, 255) : new Color(accent.r, accent.g, accent.b, .75f);
            rim.raycastTarget = false;
            if (icon != null)
            {
                var image = Anchored("Icon", cell, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -16)).gameObject.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            if (!string.IsNullOrEmpty(qty))
            {
                var label = AnchoredText(cell, "Qty", qty, ModernUi.Bold, 22, AuthTextPrimary, TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-10, -4));
                var shadow = label.gameObject.AddComponent<Outline>();
                shadow.effectColor = new Color(0, 0, 0, .85f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
            if (!string.IsNullOrEmpty(badge))
            {
                var tag = Anchored("Badge", cell, new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -34), new Vector2(6 + 26 + badge.Length * 13, -6));
                var tagFill = tag.gameObject.AddComponent<Image>();
                ModernUi.Fill(tagFill, 10f);
                UiGradient.Apply(tagFill, AuthGoldTop, AuthGoldBottom);
                tagFill.raycastTarget = false;
                AnchoredText(tag, "Text", badge, ModernUi.Bold, 17, AuthInkOnGold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            if (click != null) button.onClick.AddListener(() => click());
            cell.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        private Text SectionLabel(Transform list, string text)
        {
            var holder = new GameObject("Section", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(list, false);
            holder.GetComponent<LayoutElement>().preferredHeight = 46f;
            return AnchoredText(holder, "Text", Clean(text).ToUpperInvariant(), ModernUi.SemiBold, 20, AuthTextTertiary, TextAnchor.LowerLeft, Vector2.zero, Vector2.one, new Vector2(8, 4), Vector2.zero);
        }

        private Text Paragraph(Transform list, string text, int size = 23, Color? color = null)
        {
            var holder = new GameObject("Paragraph", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            holder.transform.SetParent(list, false);
            var label = holder.GetComponent<Text>();
            label.font = ModernUi.Regular;
            label.fontSize = size;
            label.color = color ?? AuthTextSecondary;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.08f;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.text = Clean(text);
            var fitter = holder.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return label;
        }

        private void StatLine(Transform list, string label, string value, Color? valueColor = null)
        {
            var holder = new GameObject("Stat", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(list, false);
            holder.GetComponent<LayoutElement>().preferredHeight = 38f;
            AnchoredText(holder, "Label", Clean(label), ModernUi.Regular, 22, AuthTextSecondary, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(.55f, 1), new Vector2(4, 0), Vector2.zero);
            AnchoredText(holder, "Value", Clean(value), ModernUi.SemiBold, 23, valueColor ?? AuthTextPrimary, TextAnchor.MiddleRight, new Vector2(.45f, 0), Vector2.one, Vector2.zero, new Vector2(-4, 0));
        }

        private void EmptyState(Transform list, string iconId, string message)
        {
            var holder = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(list, false);
            holder.GetComponent<LayoutElement>().preferredHeight = 260f;
            var icon = Anchored("Icon", holder, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-44, -128), new Vector2(44, -40)).gameObject.AddComponent<Image>();
            icon.sprite = UiPixelIcon(iconId);
            icon.preserveAspect = true;
            icon.color = new Color(1, 1, 1, .55f);
            icon.raycastTarget = false;
            AnchoredText(holder, "Text", Clean(message), ModernUi.Regular, 23, AuthTextTertiary, TextAnchor.UpperCenter, Vector2.zero, new Vector2(1, 1), new Vector2(30, 0), new Vector2(-30, -146));
        }

        // ================================================================ detail panel

        private sealed class DetailPanel
        {
            public RectTransform Root;
            public Transform Info;
            public RectTransform Actions;
            public int ActionCount;
        }

        private DetailPanel Detail(RectTransform area, Sprite icon, Color accent, string title, string chip, string subtitle, int actionRows = 2)
        {
            var root = Anchored("Detail", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GlassPanel(root, 26f, new Color32(18, 28, 37, 232), new Color32(9, 15, 21, 230));
            var headerHeight = 150f;
            var slot = Anchored("Slot", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -22 - 118), new Vector2(22 + 118, -22));
            if (icon != null) IconSlot(slot, icon, accent, 118f);
            var titleText = AnchoredText(root, "Title", Clean(title), ModernUi.SemiBold, 30, AuthTextPrimary, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -88), new Vector2(-20, -22));
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            if (!string.IsNullOrEmpty(chip))
            {
                var chipWidth = 30f + Clean(chip).Length * 13f;
                var chipRect = Anchored("Chip", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -128), new Vector2(160 + chipWidth, -94));
                var chipFill = chipRect.gameObject.AddComponent<Image>();
                ModernUi.Fill(chipFill, 12f);
                chipFill.color = new Color(accent.r, accent.g, accent.b, .22f);
                chipFill.raycastTarget = false;
                AnchoredText(chipRect, "Text", Clean(chip), ModernUi.SemiBold, 19, accent, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            if (!string.IsNullOrEmpty(subtitle))
                AnchoredText(root, "Subtitle", Clean(subtitle), ModernUi.Regular, 21, AuthTextSecondary, TextAnchor.UpperLeft,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(string.IsNullOrEmpty(chip) ? 160 : 160 + 30f + Clean(chip).Length * 13f + 14f, -128), new Vector2(-20, -96));
            var actionsHeight = actionRows <= 0 ? 0f : actionRows * 86f + 16f;
            var actions = Anchored("Actions", root, Vector2.zero, new Vector2(1, 0), new Vector2(18, 16), new Vector2(-18, 16 + actionsHeight));
            var infoArea = Anchored("InfoArea", root, Vector2.zero, Vector2.one, new Vector2(10, actionsHeight + 20), new Vector2(-10, -headerHeight - 10));
            var info = ScrollColumn(infoArea, 4f, 10);
            return new DetailPanel { Root = root, Info = info, Actions = actions };
        }

        /// <summary>Adds an action button to a detail panel (filled left-to-right, two per row).</summary>
        private Button DetailAction(DetailPanel panel, string label, string iconId, bool primary, Action click, bool enabled = true)
        {
            var index = panel.ActionCount++;
            var column = index % 2;
            var row = index / 2;
            var rect = Anchored("Action_" + index, panel.Actions, new Vector2(column * .5f, 1), new Vector2(column * .5f + .5f, 1),
                new Vector2(column == 0 ? 0 : 7, -(row + 1) * 86f + 6), new Vector2(column == 0 ? -7 : 0, -row * 86f));
            return PillButton(rect, label, iconId, primary, click, enabled);
        }

        private Button PillButton(RectTransform rect, string label, string iconId, bool primary, Action click, bool enabled = true)
        {
            var fill = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 20f);
            if (primary) UiGradient.Apply(fill, AuthGoldTop, AuthGoldBottom);
            else fill.color = new Color32(255, 255, 255, 16);
            if (!primary)
            {
                var edge = Anchored("Edge", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                ModernUi.Ring(edge, 20f, 1.3f);
                edge.color = new Color32(240, 228, 204, 60);
                edge.raycastTarget = false;
            }
            var textColor = primary ? AuthInkOnGold : AuthTextPrimary;
            var hasIcon = !string.IsNullOrEmpty(iconId);
            if (hasIcon)
            {
                var icon = Anchored("Icon", rect, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, -18), new Vector2(54, 18)).gameObject.AddComponent<Image>();
                icon.sprite = iconId.StartsWith("ui:") ? UiPixelIcon(iconId.Substring(3)) : ModernUi.Icon(iconId);
                icon.preserveAspect = true;
                icon.color = iconId.StartsWith("ui:") ? Color.white : textColor;
                icon.raycastTarget = false;
            }
            var text = AnchoredText(rect, "Text", Clean(label), ModernUi.SemiBold, 25, textColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(hasIcon ? 56 : 12, 0), new Vector2(-12, 0));
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 25;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.pressedColor = new Color(.85f, .82f, .78f);
            colors.disabledColor = new Color(.6f, .6f, .6f, .5f);
            button.colors = colors;
            button.interactable = enabled;
            if (click != null) button.onClick.AddListener(() => click());
            rect.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        private Button IconButton(RectTransform parent, string iconId, Vector2 anchor, Vector2 offset, float size, Action click)
        {
            var rect = Anchored("IconButton", parent, anchor, anchor, offset + new Vector2(0, -size / 2), offset + new Vector2(size, size / 2));
            var fill = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, size * .3f);
            fill.color = new Color32(255, 255, 255, 18);
            var icon = Anchored("Icon", rect, Vector2.zero, Vector2.one, new Vector2(size * .26f, size * .26f), new Vector2(-size * .26f, -size * .26f)).gameObject.AddComponent<Image>();
            icon.sprite = iconId.StartsWith("ui:") ? UiPixelIcon(iconId.Substring(3)) : ModernUi.Icon(iconId);
            icon.preserveAspect = true;
            icon.color = iconId.StartsWith("ui:") ? Color.white : AuthTextPrimary;
            icon.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            if (click != null) button.onClick.AddListener(() => click());
            rect.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        // ================================================================ dialogs, toast, busy

        private void BuildOverlays()
        {
            modalRoot = AuthStretch("Modals", content.transform);
            toastRoot = Anchored("Toasts", content.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-560, 18), new Vector2(560, 104));
            var spinnerRect = Anchored("Busy", content.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -164), new Vector2(-24, -124));
            busySpinner = spinnerRect.gameObject.AddComponent<Image>();
            busySpinner.sprite = ModernUi.Icon("spinner");
            busySpinner.color = AuthGoldAccent;
            busySpinner.raycastTarget = false;
            spinnerRect.gameObject.AddComponent<UiSpinner>();
            busySpinner.gameObject.SetActive(false);
        }

        private void ShowBusy(bool busy)
        {
            if (busySpinner != null) busySpinner.gameObject.SetActive(busy);
        }

        private void Toast(string message, bool error = false)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (toastRoot == null) { ShowStatus(message); return; }
            for (var i = toastRoot.childCount - 1; i >= 0; i--) Destroy(toastRoot.GetChild(i).gameObject);
            var text = Clean(message);
            var width = Mathf.Clamp(120f + text.Length * 13f, 360f, 1120f);
            var pill = Anchored("Toast", toastRoot, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-width / 2, 0), new Vector2(width / 2, 86));
            var fill = pill.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 26f);
            fill.color = error ? new Color32(70, 20, 22, 238) : new Color32(14, 24, 30, 240);
            fill.raycastTarget = false;
            var edge = Anchored("Edge", pill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(edge, 26f, 1.5f);
            edge.color = error ? AuthError : new Color32(240, 200, 120, 170);
            edge.raycastTarget = false;
            var icon = Anchored("Icon", pill, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(22, -17), new Vector2(56, 17)).gameObject.AddComponent<Image>();
            icon.sprite = ModernUi.Icon(error ? "alert" : "spinner");
            if (!error) icon.sprite = UiPixelIcon("sun");
            icon.preserveAspect = true;
            icon.color = error ? AuthError : Color.white;
            icon.raycastTarget = false;
            var label = AnchoredText(pill, "Text", text, ModernUi.Medium, 23, error ? new Color32(255, 214, 206, 255) : AuthTextPrimary, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(70, 4), new Vector2(-22, -4));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 23;
            pill.gameObject.AddComponent<UiAutoHide>().lifetime = error ? 4.5f : 3f;
            UiIntro.Play(pill, new Vector2(0, -16), .25f);
        }

        private RectTransform Modal(string title, float width, float height, out Action close)
        {
            if (modalRoot == null) BuildOverlays();
            for (var i = modalRoot.childCount - 1; i >= 0; i--) Destroy(modalRoot.GetChild(i).gameObject);
            var layer = AuthStretch("Modal", modalRoot);
            var shade = layer.gameObject.AddComponent<Image>();
            shade.color = new Color(0, 0, 0, .62f);
            var shadeButton = layer.gameObject.AddComponent<Button>();
            shadeButton.transition = Selectable.Transition.None;
            var card = Anchored("Card", layer, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-width / 2, -height / 2), new Vector2(width / 2, height / 2));
            GlassPanel(card, 30f, new Color32(20, 31, 40, 248), new Color32(10, 16, 22, 248));
            card.gameObject.GetComponent<Image>().raycastTarget = true; // swallow taps inside the card
            var heading = AnchoredText(card, "Title", Clean(title), ModernUi.Display, 34, AuthGoldTop, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -86), new Vector2(-90, -18));
            UiGradient.Apply(heading, AuthGoldTop, AuthGoldBottom);
            var layerObject = layer.gameObject;
            Action closeAction = () => { if (layerObject != null) Destroy(layerObject); };
            close = closeAction;
            shadeButton.onClick.AddListener(() => closeAction());
            IconButton(card, "arrowLeft", new Vector2(1, 1), new Vector2(-84, -52), 64f, closeAction);
            UiIntro.Play(card, new Vector2(0, -20), .22f);
            return card;
        }

        private void Confirm(string title, string message, string okLabel, Action onOk, bool danger = false)
        {
            var card = Modal(title, 820f, 430f, out var close);
            var body = Anchored("Message", card, Vector2.zero, Vector2.one, new Vector2(32, 120), new Vector2(-32, -96));
            var text = body.gameObject.AddComponent<Text>();
            text.font = ModernUi.Regular; text.fontSize = 25; text.color = AuthTextSecondary; text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.lineSpacing = 1.1f;
            text.text = Clean(message); text.raycastTarget = false;
            PillButton(Anchored("Cancel", card, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(28, 24), new Vector2(-8, 104)), "Hủy", "arrowLeft", false, close);
            PillButton(Anchored("Ok", card, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 24), new Vector2(-28, 104)), okLabel, danger ? "alert" : "arrowRight", true, () => { close(); onOk?.Invoke(); });
        }

        private void PromptNumber(string title, string message, long min, long max, long initial, string okLabel, Action<long> onOk)
        {
            var card = Modal(title, 820f, 520f, out var close);
            AnchoredText(card, "Message", Clean(message), ModernUi.Regular, 23, AuthTextSecondary, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -170), new Vector2(-32, -96));
            var field = AuthField(card, "amount", $"Từ {Vn(min)} đến {Vn(max)}", Vn(initial), "lock", 32f, 180f, 820f - 64f, false);
            field.contentType = InputField.ContentType.IntegerNumber;
            field.keyboardType = TouchScreenKeyboardType.NumberPad;
            field.text = initial.ToString(CultureInfo.InvariantCulture);
            var glyph = field.transform.Find("Icon")?.GetComponent<Image>();
            if (glyph != null) glyph.sprite = UiPixelIcon("coin");
            long Value() => long.TryParse(field.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? Math.Max(min, Math.Min(max, v)) : initial;
            PillButton(Anchored("Min", card, new Vector2(0, 0), new Vector2(.25f, 0), new Vector2(28, 132), new Vector2(-6, 206)), "Tối thiểu", null, false, () => field.text = min.ToString(CultureInfo.InvariantCulture));
            PillButton(Anchored("Max", card, new Vector2(.25f, 0), new Vector2(.5f, 0), new Vector2(6, 132), new Vector2(-8, 206)), "Tối đa", null, false, () => field.text = max.ToString(CultureInfo.InvariantCulture));
            PillButton(Anchored("Cancel", card, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(28, 24), new Vector2(-8, 104)), "Hủy", "arrowLeft", false, close);
            PillButton(Anchored("Ok", card, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 24), new Vector2(-28, 104)), okLabel, "arrowRight", true, () => { var v = Value(); close(); onOk?.Invoke(v); });
        }

        private void PromptText(string title, string message, string label, string placeholder, string okLabel, Action<string> onOk, int limit = 40)
        {
            var card = Modal(title, 820f, 470f, out var close);
            AnchoredText(card, "Message", Clean(message), ModernUi.Regular, 23, AuthTextSecondary, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -150), new Vector2(-32, -96));
            var field = AuthField(card, "text", label, placeholder, "user", 32f, 150f, 820f - 64f, false);
            field.characterLimit = limit;
            PillButton(Anchored("Cancel", card, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(28, 24), new Vector2(-8, 104)), "Hủy", "arrowLeft", false, close);
            PillButton(Anchored("Ok", card, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 24), new Vector2(-28, 104)), okLabel, "arrowRight", true, () =>
            {
                var value = field.text.Trim();
                if (value.Length == 0) { FlagAuthInput(field); return; }
                close();
                onOk?.Invoke(value);
            });
        }

        private sealed class PickOption
        {
            public string Label;
            public string Sub;
            public Sprite Icon;
            public Color Accent = new Color32(169, 163, 145, 255);
            public string Right;
            public Action Choose;
        }

        private void PickOne(string title, List<PickOption> options, string emptyMessage = "Không có lựa chọn phù hợp.")
        {
            var card = Modal(title, 980f, 760f, out var close);
            var area = Anchored("List", card, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -100));
            var list = ScrollColumn(area, 8f, 6);
            if (options.Count == 0) EmptyState(list, "search", emptyMessage);
            foreach (var option in options)
            {
                var chosen = option;
                Row(list, option.Icon, option.Accent, option.Label, option.Sub, option.Right, null, false, () => { close(); chosen.Choose?.Invoke(); }, 96f);
            }
        }

        // ================================================================ layout primitives (anchors + offsets)

        private static RectTransform Anchored(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text AnchoredText(Transform parent, string name, string value, Font font, int size, Color color, TextAnchor anchor,
            Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var text = Anchored(name, parent, min, max, offsetMin, offsetMax).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value ?? string.Empty;
            return text;
        }

        /// <summary>Keeps single-line titles inside their box by shrinking the font slightly when needed.</summary>
        private static void ClipText(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, text.fontSize - 8);
            text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
    }

    /// <summary>Fades out and removes a toast after its lifetime (play mode only).</summary>
    internal sealed class UiAutoHide : MonoBehaviour
    {
        public float lifetime = 3f;
        private float born;
        private CanvasGroup group;

        private void Start()
        {
            born = Time.unscaledTime;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            var age = Time.unscaledTime - born;
            if (age > lifetime) group.alpha = Mathf.Clamp01(1f - (age - lifetime) / .35f);
            if (age > lifetime + .4f) Destroy(gameObject);
        }
    }
}
