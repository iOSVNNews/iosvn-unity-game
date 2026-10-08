using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Character creator with a jointed, painted 2D figure; sect, element and innate talents.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private LookSpec creatorLook;
        private string creatorCategory = "preset";
        private int creatorPresetIndex;
        private CultivatorPuppet2D creatorRig;
        private QcbhSkinnedActor2D creatorActor;
        private bool creatorFullBodyPreview;
        private RectTransform creatorPreviewTabs;
        private RectTransform creatorGender;
        private Text creatorStyleLabel;
        private RectTransform creatorSwatches;
        private RectTransform creatorChips;
        private RectTransform creatorTalentArea;
        private Text creatorTalentCount;

        private static readonly (string key, string label, string colorKey, string colorLabel)[] CreatorCategories =
        {
            ("preset", "Phong cách", "oc", "Màu áo"),
            ("hc", "Màu tóc", "hc", "Màu tóc"), ("sk", "Màu da", "sk", "Màu da"),
            ("oc", "Màu áo", "oc", "Màu áo"), ("hat", "Mũ / Quan", "hac", "Màu mũ"),
            ("wp", "Binh khí", null, null), ("au", "Khí tức", "auc", "Màu khí"),
        };
        private static readonly int[] CreatorWeaponStyles = { 0, 1, 4, 5, 6, 7, 8, 9, 10 };
        private static readonly string[] CreatorWeaponNames = { "Không", "Kiếm sau lưng", "Trường kiếm", "Trọng chùy", "Trượng tiên", "Thiết phủ", "Bút trận", "Linh châu", "Hồ lô" };
        private static readonly string[] CreatorAuraNames = { "Không", "Linh quang nhẹ", "Vòng linh khí", "Vân khí", "Vòng linh khí đậm", "Linh quang đậm" };

        private static readonly Dictionary<string, string[]> StyleNames = new Dictionary<string, string[]>
        {
            { "bo", new[] { "Mảnh mai", "Cân đối", "Rắn chắc", "Đầy đặn" } },
            { "fa", new[] { "Mặt hẹp", "Cân đối", "Mặt rộng", "Phúc hậu" } },
            { "ey", new[] { "Mắt mảnh", "Mắt nhỏ", "Thanh tú", "Cân đối", "Mắt sáng", "Mắt lớn", "Mắt tròn", "Mắt rộng" } },
            { "br", new[] { "Mày thấp", "Tự nhiên", "Cân đối", "Mày cao", "Mày cao rõ" } },
            { "no", new[] { "Mũi hẹp", "Cân đối", "Mũi rộng", "Mũi rộng rõ" } },
            { "mo", new[] { "Miệng hẹp", "Tự nhiên", "Cân đối", "Miệng rộng", "Miệng rộng rõ" } },
            { "hat", new[] { "Không", "Ngọc quan", "Đấu lạp", "Liên hoa quan", "Mạt ngạch", "Kim quan" } },
        };

        private static readonly string[] MalePresetNames =
        {
            "Thanh Phong Kiếm Tiên", "Thanh Vân Đạo Quân", "Xích Viêm Chiến Tôn", "Hạc Phát Dược Sư", "Ngọc Diện Thư Sinh",
            "Băng Lôi Võ Tướng", "Huyền Ảnh Ma Tu", "Trúc Ảnh Đan Sư", "Kim Giáp Hộ Pháp", "Cổ Kiếm Du Hiệp"
        };
        private static readonly string[] FemalePresetNames =
        {
            "Ngọc Liên Tiên Tử", "Hồng Liên Chiến Cơ", "Thanh Sương Linh Sư", "Tử Y Ma Nữ", "Kim Phượng Đạo Cô",
            "Lạc Hà Cầm Tu", "Trúc Vũ Du Tiên", "Huyền Nguyệt Linh Nữ", "Tinh Hà Kiếm Cơ", "Bạch Lộ Đan Tâm"
        };
        private static readonly string[] PresetHairColors = { "#1e1a1e", "#3a2a24", "#d8d8e0", "#702b38", "#285b56", "#dfc9a9", "#38264b", "#765237", "#263862", "#232128" };
        private static readonly string[] PresetSkinColors = { "#f6dcc4", "#f0d2b4", "#e8c0a0", "#f4d7c4", "#e0e8f0", "#d8a880", "#c8d8c0", "#f0e0cc" };
        private static readonly string[] PresetEyeColors = { "#2a2a2a", "#5a3a20", "#3a8f7a", "#30a0d0", "#6a4ab0", "#c03030", "#d8a030", "#a0a0a8" };
        private static readonly (string tc, string oc, string pc, string sc, string bc, string hac, string ac, string auc, string wc)[] PresetPalettes =
        {
            ("#e8e2d4", "#2f5f63", "#20242a", "#2a2a30", "#1e2226", "#ffd36a", "#ffd36a", "#8fe0ff", "#64b5f0"),
            ("#282428", "#20242a", "#1a1c20", "#181a1c", "#7a2a3a", "#c8a050", "#c8a050", "#ff5050", "#ff6a5c"),
            ("#f4eef6", "#b0c8ea", "#e8e2ea", "#e0d8e0", "#8a3a5a", "#ffd36a", "#d8b46a", "#bfe8ff", "#b69cff"),
            ("#f2ece6", "#a03030", "#2a2022", "#221a1c", "#c8a050", "#ffd36a", "#ffd36a", "#ff8a3a", "#f0a24e"),
            ("#e8f0f4", "#3060a0", "#202838", "#1a2030", "#2f5f63", "#ffd36a", "#ffd36a", "#9cff9c", "#7fd08a"),
            ("#f0e8f4", "#5a4a8a", "#302a3a", "#282230", "#4a3a6a", "#e0d8f0", "#e0d8f0", "#b48cff", "#b69cff"),
            ("#e8e4dc", "#46543e", "#20242a", "#2a2a30", "#644730", "#c8a050", "#ffd36a", "#ffd36a", "#f0a24e"),
            ("#d8d8dc", "#50545c", "#25252b", "#202024", "#78683f", "#e2c57b", "#e2c57b", "#ffd36a", "#64b5f0"),
            ("#f4eef6", "#553f6f", "#282232", "#201c28", "#9b657e", "#e8d6f0", "#e8d6f0", "#ffd6e8", "#b69cff"),
            ("#e8e4dc", "#203f4b", "#20242a", "#1e2226", "#5a382c", "#c8a050", "#ffd36a", "#8fe0ff", "#64b5f0"),
        };
        private static readonly int[] MalePresetPalettes = { 0, 4, 3, 7, 2, 4, 5, 6, 7, 1 };
        private static readonly int[] FemalePresetPalettes = { 0, 3, 4, 5, 7, 8, 6, 5, 7, 0 };


        private RectTransform creatorZoom;

        private bool creatorEditingExisting;

        private void ShowCreator(bool editingExisting = false)
        {
            creatorEditingExisting = editingExisting;
            if (!QcbhSkinnedActor2D.Available) { ShowCharacterCreationForm(resetSelection: true); return; }
            ClearContent();
            if (cityRoot != null) cityRoot.SetActive(false);
            if (worldView != null)
            {
                if (worldView.Player != null && !worldReturnTile.HasValue) worldReturnTile = worldView.Player.Pos;
                worldView.gameObject.SetActive(false);
            }
            authBackdrop = LoginBackdrop.Create(backgroundRoot, Resources.Load<Texture2D>("Brand/LoginLandscapePixel"));
            if (editingExisting && hub.IsObject && hub["player"].IsObject)
            {
                var savedLook = hub["player"]["look"].Str();
                creatorLook = string.IsNullOrEmpty(savedLook) ? LookOf(hub["player"]) : LookSpec.Parse(savedLook);
                gender = creatorLook.Get("g", hub["player"]["gender"].Str() == "nu" ? "f" : "m") == "f" ? "nu" : "nam";
                creatorPresetIndex = creatorLook.Int("preset", -1);
                creatorCategory = creatorPresetIndex >= 0 ? "preset" : "oc";
            }
            else if (creatorLook == null) { creatorPresetIndex = 0; creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex); creatorCategory = "preset"; }
            else if (creatorLook.Int("preset", -1) < 0) creatorCategory = "oc";
            var root = Anchored("Creator", content.transform, Vector2.zero, Vector2.one, new Vector2(-30, 0), new Vector2(30, 0));
            var background = root.gameObject.AddComponent<Image>();
            ModernUi.Fill(background, 28f);
            UiGradient.Apply(background, new Color32(22, 42, 44, 248), new Color32(9, 21, 27, 250));
            background.raycastTarget = false;
            var inner = Anchored("Inner", root, Vector2.zero, Vector2.one, new Vector2(28, 24), new Vector2(-28, -24));
            AnchoredText(inner, "Heading", creatorEditingExisting ? "DIỆN MẠO ĐẠO HỮU" : "KHỞI TẠO ĐẠO HỮU", ModernUi.Bold, 34, Cream, TextAnchor.UpperLeft,
                new Vector2(0, .92f), Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            AnchoredText(inner, "Subtitle", "Chọn diện mạo · Định căn cơ · Bắt đầu hành trình", ModernUi.Regular, 20, new Color32(170, 195, 190, 255), TextAnchor.LowerLeft,
                new Vector2(0, .90f), new Vector2(1, .95f), new Vector2(8, 0), new Vector2(-8, 0));
            BuildCreatorAvatar(inner);
            BuildCreatorCustomizer(inner);
            BuildCreatorDestiny(inner);
            BuildCreatorFooter(inner);
            BuildOverlays();
            RefreshCreator();
        }

        private void BuildCreatorAvatar(RectTransform inner)
        {
            var col = Anchored("AvatarCol", inner, new Vector2(0, .17f), new Vector2(.36f, .88f), Vector2.zero, Vector2.zero);
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 22f);
            panel.color = new Color32(228, 235, 225, 255);
            panel.raycastTarget = false;
            var halo = Anchored("Halo", col, new Vector2(.02f, .06f), new Vector2(.98f, .98f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            halo.sprite = InkUi.Cloud;
            halo.color = new Color(.42f, .60f, .54f, .32f);
            halo.raycastTarget = false;
            var view = Anchored("Preview", col, new Vector2(.04f, .12f), new Vector2(.96f, .98f), Vector2.zero, Vector2.zero);
            creatorZoom = Anchored("Zoom", view, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            creatorRig = CultivatorPuppet2D.Create(creatorZoom, creatorLook);
            var portraitRect = creatorRig.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(.5f, .5f);
            creatorActor = QcbhSkinnedActor2D.Create(creatorZoom, creatorLook);
            creatorActor.SetMotion(FighterAction.Idle, 0, false, false, 0);
            var actorRect = creatorActor.GetComponent<RectTransform>();
            actorRect.anchorMin = actorRect.anchorMax = actorRect.pivot = new Vector2(.5f, .5f);
            creatorPreviewTabs = Anchored("PreviewTabs", col, Vector2.zero, new Vector2(1, .105f), new Vector2(12, 8), new Vector2(-12, -2));
        }

        private void BuildCreatorCustomizer(RectTransform inner)
        {
            var col = Anchored("Custom", inner, new Vector2(.38f, .17f), new Vector2(.68f, .88f), new Vector2(6, 0), new Vector2(-6, 0));
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 24f);
            panel.color = new Color32(251, 248, 239, 232);
            panel.raycastTarget = false;
            ModernUi.Soft(col, 24f, new Color32(24, 38, 42, 28), new Vector2(0, -4), 2f);
            CreatorPanelBorder(col);
            AnchoredText(col, "Title", "DIỆN MẠO", ModernUi.Bold, 28, new Color32(46, 40, 38, 255), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -58), new Vector2(-20, -16));
            creatorChips = Anchored("Chips", col, new Vector2(0, .30f), new Vector2(1, 1), new Vector2(12, 0), new Vector2(-12, -72));
            var stepper = Anchored("Stepper", col, new Vector2(0, .20f), new Vector2(1, .30f), new Vector2(12, 0), new Vector2(-12, 0));
            var prev = Anchored("Prev", stepper, new Vector2(0, 0), new Vector2(.18f, 1), Vector2.zero, Vector2.zero);
            CreatorArrowButton(prev, "arrowLeft", () => StepCreatorStyle(-1));
            var next = Anchored("Next", stepper, new Vector2(.82f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            CreatorArrowButton(next, "arrowRight", () => StepCreatorStyle(1));
            creatorStyleLabel = AnchoredText(stepper, "Style", "", ModernUi.SemiBold, 21, new Color32(40, 34, 32, 255), TextAnchor.MiddleCenter, new Vector2(.18f, 0), new Vector2(.82f, 1), Vector2.zero, Vector2.zero);
            creatorSwatches = Anchored("Swatches", col, new Vector2(0, .02f), new Vector2(1, .20f), new Vector2(18, 0), new Vector2(-18, -8));

            // Relative widths keep the field inside its card on both iPhone and iPad.
            var nameRoot = Anchored("NameField", inner, new Vector2(0, .075f), new Vector2(.36f, .145f), new Vector2(4, 0), new Vector2(-4, 0));
            var nameFill = nameRoot.gameObject.AddComponent<Image>();
            ModernUi.Fill(nameFill, 14f);
            nameFill.color = new Color32(241, 244, 233, 255);
            nameInput = nameRoot.gameObject.AddComponent<InputField>();
            nameInput.targetGraphic = nameFill;
            nameInput.textComponent = AnchoredText(nameRoot, "Value", "", ModernUi.SemiBold, 25, new Color32(31, 56, 53, 255), TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0));
            nameInput.textComponent.supportRichText = false;
            nameInput.placeholder = AnchoredText(nameRoot, "Hint", "Đạo hiệu (2–24 ký tự)", ModernUi.Regular, 24, new Color32(99, 117, 108, 255), TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0));
            nameInput.characterLimit = 24;
            nameInput.lineType = InputField.LineType.SingleLine;
            nameInput.shouldHideMobileInput = true;
            nameInput.caretWidth = 2;
            nameInput.selectionColor = new Color32(85, 160, 143, 90);
            if (creatorEditingExisting && hub.IsObject && hub["player"].IsObject)
            {
                nameInput.text = Clean(hub["player"]["fullName"].Str(hub["player"]["name"].Str()));
                nameInput.interactable = false;
            }
            creatorGender = Anchored("Gender", inner, Vector2.zero, new Vector2(.36f, .063f), Vector2.zero, Vector2.zero);
        }

        private void BuildCreatorDestiny(RectTransform inner)
        {
            var col = Anchored("Destiny", inner, new Vector2(.70f, .17f), new Vector2(1, .88f), new Vector2(6, 0), new Vector2(-6, 0));
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 24f);
            panel.color = new Color32(251, 248, 239, 232);
            panel.raycastTarget = false;
            ModernUi.Soft(col, 24f, new Color32(24, 38, 42, 28), new Vector2(0, -4), 2f);
            CreatorPanelBorder(col);
            AnchoredText(col, "Title", "CĂN CƠ", ModernUi.Bold, 28, new Color32(46, 40, 38, 255), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -58), new Vector2(-20, -16));
            if (creatorEditingExisting)
            {
                creatorTalentArea = null;
                creatorTalentCount = null;
                AnchoredText(col, "ExistingDestiny", "Đạo hữu đã định căn cơ\n\nMôn phái: " + Clean(hub["player"]["monName"].Str())
                    + "\nNgũ hành: " + Clean(hub["player"]["heName"].Str())
                    + "\n\nLưu diện mạo để áp dụng hình dáng và màu sắc mới.\n\nTrang bị đang mặc có thể thay màu áo và binh khí khi vào game.",
                    ModernUi.Regular, 25, new Color32(47, 67, 63, 255), TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(22, 18), new Vector2(-22, -90));
                return;
            }
            sectNames = Names(currentCatalog?.mon);
            elementNames = Names(currentCatalog?.he);
            sectIndex = Mathf.Clamp(sectIndex, 0, Math.Max(0, sectNames.Length - 1));
            elementIndex = Mathf.Clamp(elementIndex, 0, Math.Max(0, elementNames.Length - 1));
            var sect = Anchored("Sect", col, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -146), new Vector2(-18, -72));
            CreatorCycler(sect, "Môn phái", () => sectNames.Length == 0 ? "—" : sectNames[sectIndex], d => { if (sectNames.Length > 0) sectIndex = (sectIndex + d + sectNames.Length) % sectNames.Length; });
            var element = Anchored("Element", col, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -228), new Vector2(-18, -154));
            CreatorCycler(element, "Ngũ hành", () => elementNames.Length == 0 ? "—" : elementNames[elementIndex], d => { if (elementNames.Length > 0) elementIndex = (elementIndex + d + elementNames.Length) % elementNames.Length; });
            creatorTalentCount = AnchoredText(col, "TalentTitle", "", ModernUi.SemiBold, 24, new Color32(60, 50, 44, 255), TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -280), new Vector2(-18, -236));
            creatorTalentArea = Anchored("Talents", col, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 12), new Vector2(-14, -288));
        }

        private void BuildCreatorFooter(RectTransform inner)
        {
            var footer = Anchored("Footer", inner, new Vector2(.38f, 0), new Vector2(1, .145f), Vector2.zero, Vector2.zero);
            var line = Anchored("Rule", footer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero).gameObject.AddComponent<Image>();
            line.color = new Color32(60, 50, 44, 90);
            var stats = new[] { ("Khí huyết", "500"), ("Linh lực", "200"), ("Công kích", "50") };
            for (var i = 0; i < stats.Length; i++)
            {
                var cell = Anchored("Stat" + i, footer, new Vector2(i * .115f, 0), new Vector2((i + 1) * .115f, 1), new Vector2(4, 6), new Vector2(-4, -8));
                AnchoredText(cell, "L", stats[i].Item1, ModernUi.Medium, 20, new Color32(170, 195, 190, 255), TextAnchor.UpperCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                AnchoredText(cell, "V", stats[i].Item2, ModernUi.SemiBold, 28, Cream, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            var back = Anchored("Back", footer, new Vector2(.37f, .05f), new Vector2(.58f, .90f), new Vector2(8, 0), new Vector2(-8, 0));
            PillButton(back, "Quay lại", "arrowLeft", false, BackFromCharacterCreation);
            var start = Anchored("Start", footer, new Vector2(.60f, .05f), new Vector2(1, .90f), new Vector2(8, 0), new Vector2(-18, 0));
            if (creatorEditingExisting)
                PillButton(start, "Lưu diện mạo", "arrowRight", true, SaveAppearance);
            else
                PillButton(start, "Bắt đầu tu luyện", "arrowRight", true, CreateCharacter);
            statusMin = new Vector2(.38f, .145f); statusMax = new Vector2(1, .17f);
        }

        private void SaveAppearance()
        {
            if (creatorLook == null) return;
            var text = creatorLook.ToString();
            ShowBusy(true);
            if (offlinePreview)
            {
                ShowBusy(false);
                if (hub.IsObject && hub["player"].IsObject)
                {
                    hub["player"].Set("look", text);
                    hub["player"].Set("lookWorn", text);
                }
                if (latestState?.player != null)
                {
                    latestState.player.look = text;
                    latestState.player.lookWorn = text;
                }
                PersistOfflineAppearance(text);
                Toast("Đã lưu diện mạo mới thành công!");
                OpenCharacterScreen();
                return;
            }
            client.Post("/player/look", Body("look", text), (result, error) =>
            {
                ShowBusy(false);
                if (error != null) { Toast(error, true); return; }
                if (result["state"].IsObject)
                {
                    hub = result["state"];
                    latestState = NetworkGameClient.ToGameState(hub);
                    Toast("Đã áp dụng diện mạo mới thành công!");
                    OpenCharacterScreen();
                }
                else RefreshHub(OpenCharacterScreen);
            });
        }

        private void CreatorPanelBorder(RectTransform parent)
        {
            var border = Anchored("Border", parent, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)).gameObject.AddComponent<Image>();
            ModernUi.Ring(border, 22f, 1.25f);
            border.color = new Color32(163, 133, 73, 118);
            border.raycastTarget = false;
        }

        private void CreatorChip(RectTransform parent, string label, Vector2 min, Vector2 max, bool active, Action click)
        {
            var rect = Anchored("Chip_" + label, parent, min, max, new Vector2(4, 4), new Vector2(-4, -4));
            var fill = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 12f);
            if (active)
            {
                fill.color = Color.white;
                UiGradient.Apply(fill, new Color32(38, 112, 102, 255), new Color32(24, 73, 73, 255));
            }
            else
            {
                fill.color = new Color32(238, 234, 222, 255);
                var border = Anchored("Border", rect, Vector2.zero, Vector2.one, Vector2.one, -Vector2.one).gameObject.AddComponent<Image>();
                ModernUi.Ring(border, 12f, 1f);
                border.color = new Color32(133, 120, 92, 92);
                border.raycastTarget = false;
            }
            var text = AnchoredText(rect, "Text", label, ModernUi.SemiBold, 22, active ? Color.white : new Color32(35, 49, 53, 255), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 15; text.resizeTextMaxSize = 22;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => click());
            rect.gameObject.AddComponent<UiPressScale>();
        }

        private void CreatorCycler(RectTransform row, string label, Func<string> value, Action<int> step)
        {
            AnchoredText(row, "Label", label, ModernUi.Medium, 20, new Color32(57, 75, 73, 255), TextAnchor.UpperLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var valueText = AnchoredText(row, "Value", value(), ModernUi.SemiBold, 27, new Color32(40, 34, 32, 255), TextAnchor.LowerCenter, new Vector2(.16f, 0), new Vector2(.84f, 1), Vector2.zero, Vector2.zero);
            CreatorArrowButton(Anchored("Prev", row, new Vector2(0, 0), new Vector2(.15f, .62f), Vector2.zero, Vector2.zero), "arrowLeft", () => { step(-1); valueText.text = value(); });
            CreatorArrowButton(Anchored("Next", row, new Vector2(.85f, 0), new Vector2(1, .62f), Vector2.zero, Vector2.zero), "arrowRight", () => { step(1); valueText.text = value(); });
        }

        private void CreatorArrowButton(RectTransform rect, string iconId, Action click)
        {
            // Keep the full rectangle as the touch target while drawing only the arrow.
            var hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            var arrow = Anchored("Arrow", rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-20, -20), new Vector2(20, 20)).gameObject.AddComponent<Image>();
            arrow.sprite = ModernUi.Icon(iconId);
            arrow.preserveAspect = true;
            arrow.color = new Color32(49, 58, 58, 255);
            arrow.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            button.onClick.AddListener(() => click());
        }

        private void RefreshCreator()
        {
            if (creatorRig == null) return;
            creatorRig.SetLook(creatorLook);
            creatorActor.SetLook(creatorLook);
            creatorRig.gameObject.SetActive(!creatorFullBodyPreview);
            creatorActor.gameObject.SetActive(creatorFullBodyPreview);
            ClearCreatorChildren(creatorPreviewTabs);
            CreatorChip(creatorPreviewTabs, "Chân dung", Vector2.zero, new Vector2(.5f, 1), !creatorFullBodyPreview,
                () => { creatorFullBodyPreview = false; RefreshCreator(); });
            CreatorChip(creatorPreviewTabs, "Toàn thân", new Vector2(.5f, 0), Vector2.one, creatorFullBodyPreview,
                () => { creatorFullBodyPreview = true; RefreshCreator(); });
            ApplyCreatorZoom();
            // Two columns with readable labels and generous touch targets.
            ClearCreatorChildren(creatorChips);
            for (var i = 0; i < CreatorCategories.Length; i++)
            {
                var c = CreatorCategories[i];
                var col = i % 2;
                var row = i / 2;
                var rows = (CreatorCategories.Length + 1) / 2;
                CreatorChip(creatorChips, c.label, new Vector2(col / 2f, 1f - (row + 1f) / rows), new Vector2((col + 1) / 2f, 1f - row / (float)rows),
                    c.key == creatorCategory, () =>
                    {
                        creatorCategory = c.key;
                        if (c.key == "preset" && creatorLook.Int("preset", -1) < 0)
                        {
                            creatorPresetIndex = Mathf.Clamp(creatorPresetIndex, 0, MalePresetNames.Length - 1);
                            creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex);
                        }
                        RefreshCreator();
                    });
            }
            ClearCreatorChildren(creatorGender);
            if (!creatorEditingExisting)
            {
                CreatorChip(creatorGender, "Nam", Vector2.zero, new Vector2(.28f, 1), gender != "nu", () => SetCreatorGender("nam"));
                CreatorChip(creatorGender, "Nữ", new Vector2(.29f, 0), new Vector2(.57f, 1), gender == "nu", () => SetCreatorGender("nu"));
            }
            else AnchoredText(creatorGender, "GenderLabel", gender == "nu" ? "Đạo hữu nữ" : "Đạo hữu nam", ModernUi.SemiBold, 23, Cream, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(.57f, 1), new Vector2(8, 0), Vector2.zero);
            CreatorChip(creatorGender, "Ngẫu nhiên", new Vector2(.58f, 0), Vector2.one, false, RandomizeLook);
            var key = creatorCategory;
            var names = StyleNames.TryGetValue(key, out var n) ? n : new[] { "Kiểu 1" };
            if (key == "au") names = CreatorAuraNames;
            var index = key == "preset" ? Mathf.Clamp(creatorPresetIndex, 0, MalePresetNames.Length - 1) : Mathf.Clamp(creatorLook.Int(key), 0, names.Length - 1);
            if (key == "preset") names = creatorLook.Get("g") == "f" ? FemalePresetNames : MalePresetNames;
            if (key == "wp") { names = CreatorWeaponNames; index = Mathf.Max(0, Array.IndexOf(CreatorWeaponStyles, creatorLook.Int("wp"))); }
            if (key == "au") names = CreatorAuraNames;
            creatorStyleLabel.text = IsCreatorColor(key) ? "Chọn màu bên dưới" : names[index] + $"  {index + 1}/{names.Length}";
            // swatches
            ClearCreatorChildren(creatorSwatches);
            string colorKey = null;
            foreach (var c in CreatorCategories) if (c.key == key) colorKey = c.colorKey;
            if (colorKey != null)
            {
                var palette = colorKey == "sk" ? AvatarComposer.SkinColors : colorKey == "hc" ? AvatarComposer.HairColors : colorKey == "ec" ? AvatarComposer.EyeColors
                    : colorKey == "auc" ? AvatarComposer.AuraColors : AvatarComposer.ClothColors;
                var count = palette.Length;
                for (var i = 0; i < count; i++)
                {
                    var hex = palette[i];
                    var columns = Mathf.Min(8, count);
                    var rows = (count + columns - 1) / columns;
                    var x = i % columns; var y = i / columns;
                    var rect = Anchored("Swatch" + i, creatorSwatches, new Vector2(x / (float)columns, 1f - (y + 1f) / rows), new Vector2((x + 1f) / columns, 1f - y / (float)rows), new Vector2(4, 4), new Vector2(-4, -4));
                    var fill = rect.gameObject.AddComponent<Image>();
                    ModernUi.Fill(fill, 10f);
                    fill.color = HeroSprites.ParseColor(hex, Color.gray);
                    if (string.Equals(creatorLook.Get(colorKey), hex, StringComparison.OrdinalIgnoreCase))
                    {
                        var ring = Anchored("Ring", rect, new Vector2(-.1f, -.1f), new Vector2(1.1f, 1.1f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                        ModernUi.Ring(ring, 12f, 2f);
                        ring.color = new Color32(40, 34, 32, 255);
                        ring.raycastTarget = false;
                    }
                    var button = rect.gameObject.AddComponent<Button>();
                    button.targetGraphic = fill;
                    var ck = colorKey;
                    button.onClick.AddListener(() => { creatorLook.Set(ck, hex); creatorLook.Set("preset", -1); RefreshCreator(); });
                }
            }
            // talents
            if (creatorTalentArea == null) return;
            ClearCreatorChildren(creatorTalentArea);
            creatorTalentCount.text = $"Tiên thiên khí vận  ·  {selectedTalents.Count}/3";
            for (var i = 0; i < CreationTalentIds.Length; i++)
            {
                var id = CreationTalentIds[i];
                var col = i % 2;
                var row = i / 2;
                var rows = (CreationTalentIds.Length + 1) / 2;
                CreatorChip(creatorTalentArea, CreationTalentNames[i], new Vector2(col / 2f, 1f - (row + 1f) / rows), new Vector2((col + 1) / 2f, 1f - row / (float)rows),
                    selectedTalents.Contains(id), () =>
                    {
                        if (selectedTalents.Contains(id)) selectedTalents.Remove(id);
                        else if (selectedTalents.Count >= 3) { ShowStatus("Chỉ chọn tối đa 3 khí vận."); return; }
                        else selectedTalents.Add(id);
                        RefreshCreator();
                    });
            }
        }

        /// <summary>Keep the entire portrait inside its frame at every resolution and for every edit category.</summary>
        private void ApplyCreatorZoom()
        {
            if (creatorZoom == null) return;
            creatorZoom.pivot = new Vector2(.5f, .5f);
            creatorZoom.anchorMin = Vector2.zero;
            creatorZoom.anchorMax = Vector2.one;
            creatorZoom.offsetMin = creatorZoom.offsetMax = Vector2.zero;
            creatorZoom.localScale = Vector3.one;
        }

        private void StepCreatorStyle(int delta)
        {
            if (creatorCategory == "preset")
            {
                creatorPresetIndex = (creatorPresetIndex + delta + MalePresetNames.Length) % MalePresetNames.Length;
                creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex);
                RefreshCreator();
                return;
            }
            var key = creatorCategory;
            if (IsCreatorColor(key))
            {
                var colors = key == "hc" ? AvatarComposer.HairColors : key == "sk" ? AvatarComposer.SkinColors : AvatarComposer.ClothColors;
                var index = Mathf.Max(0, Array.IndexOf(colors, creatorLook.Get(key)));
                creatorLook.Set(key, colors[(index + delta + colors.Length) % colors.Length]);
                creatorLook.Set("preset", -1);
                RefreshCreator();
                return;
            }
            if (key == "wp")
            {
                var index = Mathf.Max(0, Array.IndexOf(CreatorWeaponStyles, creatorLook.Int("wp")));
                creatorLook.Set("wp", CreatorWeaponStyles[(index + delta + CreatorWeaponStyles.Length) % CreatorWeaponStyles.Length]);
                creatorLook.Set("preset", -1);
                RefreshCreator();
                return;
            }
            var count = AvatarComposer.Counts.TryGetValue(key, out var c) ? c : 1;
            creatorLook.Set(key, (creatorLook.Int(key) + delta + count) % count);
            creatorLook.Set("preset", -1);
            RefreshCreator();
        }

        private void SetCreatorGender(string value)
        {
            if (creatorEditingExisting) return;
            gender = value == "nu" ? "nu" : "nam";
            creatorPresetIndex = 0;
            creatorCategory = "preset";
            creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex);
            RefreshCreator();
        }

        private static bool IsCreatorColor(string key) => key == "hc" || key == "sk" || key == "oc";

        private void PersistOfflineAppearance(string text)
        {
            var choice = ReadOfflineCharacterChoice() ?? new RegisterChoice
            {
                name = hub["player"]["name"].Str(), mon = hub["player"]["mon"].Str(), he = hub["player"]["he"].Str()
            };
            choice.look = text;
            choice.gender = LookSpec.Parse(text).Get("g", "m") == "f" ? "nu" : "nam";
            PlayerPrefs.SetString("tutien_offline_character_demo", JsonUtility.ToJson(choice));
            PlayerPrefs.SetString("tutien_offline_look", text);
            PlayerPrefs.Save();
        }

        private static void ClearCreatorChildren(RectTransform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                // Disable immediately so rapid taps cannot hit last frame's choices.
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        private static LookSpec BuildCreatorPreset(bool female, int preset)
        {
            var i = (preset % MalePresetNames.Length + MalePresetNames.Length) % MalePresetNames.Length;
            var look = AvatarComposer.Default(female);
            look.Set("g", female ? "f" : "m");
            look.Set("preset", i);
            look.Set("template", i);
            look.Set("bo", (i + (female ? 1 : 0)) % AvatarComposer.Counts["bo"]);
            look.Set("fa", (i * 3 + (female ? 1 : 0)) % AvatarComposer.Counts["fa"]);
            look.Set("ea", (i + (female ? 1 : 0)) % AvatarComposer.Counts["ea"]);
            look.Set("ey", (i * 3 + 1) % AvatarComposer.Counts["ey"]);
            look.Set("br", (i * 2 + 1) % AvatarComposer.Counts["br"]);
            look.Set("no", (i + 2) % AvatarComposer.Counts["no"]);
            look.Set("mo", (i * 2 + 3) % AvatarComposer.Counts["mo"]);
            look.Set("bd", female ? 0 : (i * 2) % AvatarComposer.Counts["bd"]);
            look.Set("ha", (i + (female ? 0 : 3)) % AvatarComposer.Counts["ha"]);
            look.Set("ti", i % AvatarComposer.Counts["ti"]);
            look.Set("to", (i * 5 + 1) % AvatarComposer.Counts["to"]);
            look.Set("tot", i % AvatarComposer.Counts["tot"]);
            look.Set("pa", (i * 3) % AvatarComposer.Counts["pa"]);
            look.Set("sh", i % AvatarComposer.Counts["sh"]);
            look.Set("be", (i + 1) % AvatarComposer.Counts["be"]);
            look.Set("hat", i % AvatarComposer.Counts["hat"]);
            look.Set("wp", CreatorWeaponStyles[(i + (female ? 1 : 2)) % CreatorWeaponStyles.Length]);
            look.Set("au", 1 + i % (AvatarComposer.Counts["au"] - 1));
            var palette = PresetPalettes[(female ? FemalePresetPalettes : MalePresetPalettes)[i]];
            look.Set("hc", PresetHairColors[i]);
            look.Set("sk", PresetSkinColors[(i * 3 + (female ? 0 : 1)) % PresetSkinColors.Length]);
            look.Set("ec", PresetEyeColors[(i * 3 + 2) % PresetEyeColors.Length]);
            look.Set("tc", palette.tc); look.Set("oc", palette.oc); look.Set("pc", palette.pc); look.Set("sc", palette.sc);
            look.Set("bc", palette.bc); look.Set("hac", palette.hac); look.Set("ac", palette.ac); look.Set("auc", palette.auc); look.Set("wc", palette.wc);
            return look;
        }

        private void RandomizeLook()
        {
            creatorCategory = "oc";
            creatorLook.Set("preset", -1);
            var rng = new System.Random();
            foreach (var category in CreatorCategories)
            {
                if (!AvatarComposer.Counts.TryGetValue(category.key, out var styleCount)) continue;
                var pair = new KeyValuePair<string, int>(category.key, styleCount);
                if (pair.Key == "bd" && creatorLook.Get("g") == "f") { creatorLook.Set("bd", 0); continue; }
                var max = pair.Value;
                var value = rng.Next(max);
                if (pair.Key == "hat" && rng.NextDouble() < .6) value = 0;
                if (pair.Key == "bd" && rng.NextDouble() < .6) value = 0;
                if (pair.Key == "ey" && value == 7 && rng.NextDouble() < .7) value = 0;
                creatorLook.Set(pair.Key, value);
            }
            creatorLook.Set("wp", CreatorWeaponStyles[rng.Next(CreatorWeaponStyles.Length)]);
            creatorLook.Set("hc", AvatarComposer.HairColors[rng.Next(AvatarComposer.HairColors.Length)]);
            creatorLook.Set("ec", AvatarComposer.EyeColors[rng.Next(AvatarComposer.EyeColors.Length)]);
            var palettes = new[]
            {
                new { tc = "#e8e4dc", oc = "#2f5f63", pc = "#20242a", sc = "#2a2a30", bc = "#1e2226", hac = "#ffd36a", ac = "#ffd36a" },
                new { tc = "#f4eef6", oc = "#b0c8ea", pc = "#e8e2ea", sc = "#e0d8e0", bc = "#8a3a5a", hac = "#ffd36a", ac = "#ffd36a" },
                new { tc = "#f0e8f4", oc = "#5a4a8a", pc = "#302a3a", sc = "#282230", bc = "#4a3a6a", hac = "#e0d8f0", ac = "#ffd36a" },
                new { tc = "#282428", oc = "#20242a", pc = "#1a1c20", sc = "#181a1c", bc = "#7a2a3a", hac = "#c8a050", ac = "#ffd36a" },
                new { tc = "#f2ece6", oc = "#a03030", pc = "#2a2022", sc = "#221a1c", bc = "#c8a050", hac = "#ffd36a", ac = "#ffd36a" },
                new { tc = "#e8f0f4", oc = "#3060a0", pc = "#202838", sc = "#1a2030", bc = "#2f5f63", hac = "#ffd36a", ac = "#ffd36a" }
            };
            var pal = palettes[rng.Next(palettes.Length)];
            creatorLook.Set("tc", pal.tc);
            creatorLook.Set("oc", pal.oc);
            creatorLook.Set("pc", pal.pc);
            creatorLook.Set("sc", pal.sc);
            creatorLook.Set("bc", pal.bc);
            creatorLook.Set("hac", pal.hac);
            creatorLook.Set("ac", pal.ac);
            creatorLook.Set("auc", AvatarComposer.AuraColors[rng.Next(AvatarComposer.AuraColors.Length)]);
            RefreshCreator();
        }
    }
}
