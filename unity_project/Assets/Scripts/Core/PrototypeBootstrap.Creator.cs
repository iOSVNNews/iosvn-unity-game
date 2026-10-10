using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Character creator in the layered style: one live figure on the left (zooms onto the face for
    /// facial parts), a vertical category list, a grid of option thumbnails drawn with the real parts,
    /// sliders for feature placement, colour swatches, motion previews, then the destiny step.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private LookSpec creatorLook;
        private string creatorCategory = "preset";
        private int creatorPresetIndex;
        private int creatorStep;
        private CultivatorFigure2D creatorFigure;
        private RectTransform creatorZoom;
        private RectTransform creatorMotionControls;
        private string creatorMotion = "idle";
        private bool creatorFacesRight = true;
        private RectTransform creatorGender;
        private RectTransform creatorCategoryList;
        private RectTransform creatorOptions;
        private RectTransform creatorControls;
        private RectTransform creatorAppearancePane;
        private RectTransform creatorDestinyPane;
        private RectTransform creatorStepTabs;
        private RectTransform creatorFooter;
        private RectTransform creatorTalentArea;
        private Text creatorTalentCount;
        private bool creatorEditingExisting;
        private static readonly string[][] CreatorGroups =
        {
            new[] { "preset" }, new[] { "fa", "ey", "br", "no", "mo", "bd", "ma" },
            new[] { "ha", "to", "hat" }, new[] { "wp", "au" }
        };
        private static readonly string[] CreatorGroupNames = { "Phong cách", "Gương mặt", "Tóc & y phục", "Binh khí & khí tức" };

        private sealed class CreatorCategory
        {
            public string Key, Label;
            public string[] Colors, ColorLabels, Sliders, SliderLabels;
            public CultivatorFigure2D.Framing Preview, Thumb;
            public bool MaleOnly;
        }

        private static readonly CreatorCategory[] CreatorCategoriesV2 =
        {
            new CreatorCategory { Key = "preset", Label = "Phong cách", Preview = CultivatorFigure2D.Framing.Full, Thumb = CultivatorFigure2D.Framing.Bust },
            new CreatorCategory { Key = "fa", Label = "Khuôn mặt", Colors = new[] { "sk" }, ColorLabels = new[] { "Màu da" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head },
            new CreatorCategory { Key = "ey", Label = "Mắt", Colors = new[] { "ec" }, ColorLabels = new[] { "Màu mắt" }, Sliders = new[] { "ez", "es", "eh" }, SliderLabels = new[] { "Cỡ mắt", "Khoảng cách", "Cao thấp" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head },
            new CreatorCategory { Key = "br", Label = "Lông mày", Colors = new[] { "hc" }, ColorLabels = new[] { "Màu mày / tóc" }, Sliders = new[] { "bh" }, SliderLabels = new[] { "Cao thấp" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head },
            new CreatorCategory { Key = "no", Label = "Mũi", Sliders = new[] { "nh" }, SliderLabels = new[] { "Cao thấp" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head },
            new CreatorCategory { Key = "mo", Label = "Miệng", Sliders = new[] { "mh" }, SliderLabels = new[] { "Cao thấp" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head },
            new CreatorCategory { Key = "bd", Label = "Râu", Colors = new[] { "hc" }, ColorLabels = new[] { "Màu râu / tóc" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head, MaleOnly = true },
            new CreatorCategory { Key = "ha", Label = "Kiểu tóc", Colors = new[] { "hc" }, ColorLabels = new[] { "Màu tóc" }, Preview = CultivatorFigure2D.Framing.Bust, Thumb = CultivatorFigure2D.Framing.Bust },
            new CreatorCategory { Key = "ma", Label = "Ấn ký", Colors = new[] { "mc" }, ColorLabels = new[] { "Màu ấn ký" }, Preview = CultivatorFigure2D.Framing.Head, Thumb = CultivatorFigure2D.Framing.Head },
            new CreatorCategory { Key = "to", Label = "Y phục", Colors = new[] { "oc", "tc" }, ColorLabels = new[] { "Màu áo", "Màu viền" }, Preview = CultivatorFigure2D.Framing.Full, Thumb = CultivatorFigure2D.Framing.Full },
            new CreatorCategory { Key = "hat", Label = "Mũ / Quan", Colors = new[] { "hac" }, ColorLabels = new[] { "Màu mũ" }, Preview = CultivatorFigure2D.Framing.Bust, Thumb = CultivatorFigure2D.Framing.Bust },
            new CreatorCategory { Key = "wp", Label = "Binh khí", Preview = CultivatorFigure2D.Framing.Full },
            new CreatorCategory { Key = "au", Label = "Khí tức", Colors = new[] { "auc" }, ColorLabels = new[] { "Màu khí" }, Preview = CultivatorFigure2D.Framing.Full },
        };

        private static readonly int[] CreatorWeaponStyles = { 0, 1, 4, 5, 6, 7, 8, 9, 10 };
        private static readonly string[] CreatorWeaponNames = { "Không", "Kiếm sau lưng", "Trường kiếm", "Trọng chùy", "Trượng tiên", "Thiết phủ", "Bút trận", "Linh châu", "Hồ lô" };
        private static readonly string[] CreatorAuraNames = { "Không", "Linh quang nhẹ", "Vòng linh khí", "Vân khí", "Vòng linh khí đậm", "Linh quang đậm" };
        private static readonly string[] MarkColors = { "#c8303c", "#e0a030", "#3a8fd0", "#7a3ab0", "#2a9a6a", "#202020", "#f0f0f0", "#ff7a3a" };
        private static readonly string[] HatColors = { "#d8b46a", "#e2c57b", "#9fd8c0", "#d8d8e0", "#7a5a3a", "#303038", "#c8a050", "#b0c8ea" };

        private static readonly Dictionary<string, string[]> OptionNames = new Dictionary<string, string[]>
        {
            { "fa", new[] { "Trái xoan", "Thon gọn", "Vuông vức", "Tròn đầy" } },
            { "ey", new[] { "Mắt hẹp", "Mắt nhỏ", "Hạnh nhân", "Cân đối", "Mắt sáng", "Mắt lớn", "Mắt tròn", "Mắt phượng" } },
            { "br", new[] { "Mày thẳng", "Tự nhiên", "Mày kiếm", "Mày cong", "Mày rậm" } },
            { "no", new[] { "Mũi nhỏ", "Mũi thẳng", "Sống cao", "Mũi rộng" } },
            { "mo", new[] { "Môi khép", "Mỉm cười", "Môi mỏng", "Môi đầy", "Nhếch mép" } },
            { "bd", new[] { "Không", "Ria mảnh", "Ria chòm", "Râu dài", "Quai nón" } },
            { "ma", new[] { "Không", "Hỏa ấn", "Thiên nhãn", "Liên hoa", "Nguyệt ấn", "Chu sa" } },
            { "to", new[] { "Võ phục ngắn", "Đạo bào", "Kiếm khách bào", "Giáp nhẹ", "Nho sam", "Áo choàng" } },
            { "hat", new[] { "Không", "Ngọc quan", "Đấu lạp", "Liên hoa quan", "Mạt ngạch", "Kim quan" } },
        };
        private static readonly string[] MaleHairNames = { "Búi tóc dài", "Búi cao xõa", "Tóc ngắn rối", "Xõa rẽ ngôi", "Búi cài trâm", "Tóc ngắn vuốt", "Nửa búi lệch", "Tóc hoang dã", "Băng đô xõa", "Búi rủ tóc mai" };
        private static readonly string[] FemaleHairNames = { "Xõa dài", "Búi cao xõa", "Song búi", "Búi cao cài trâm", "Bím lệch vai", "Búi cài trâm hoa", "Ngang vai mái bằng", "Đuôi ngựa lệch", "Hai bím buộc nơ", "Xõa cài hoa" };

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
        // fa, ey, br, no, mo, bd, ha, to, hat, ma, wp, au | hc, sk, ec, oc, tc, pc, sc, bc, hac, mc, auc
        private static readonly string[] MalePresets =
        {
            "1,2,1,1,0,0,0,2,1,0,1,1|#1e1a1e,#f2d8be,#3a2a24,#2f5f63,#e8e4dc,#20242a,#24282e,#1e2226,#d8b46a,#c8303c,#8fe0ff",
            "0,3,1,2,1,0,4,1,5,2,4,5|#232128,#f0d2b4,#3a8f7a,#e8e2d4,#3060a0,#202838,#1a2030,#3060a0,#e2c57b,#3a8fd0,#bfe8ff",
            "2,7,4,3,2,4,8,3,0,1,5,2|#702b38,#e8c0a0,#c03030,#7a2a3a,#c8a050,#2a2022,#221a1c,#c8a050,#c8a050,#ff7a3a,#ff8a3a",
            "1,1,0,1,1,3,5,4,1,0,9,1|#e8e0c8,#f0d2b4,#5a3a20,#e8e4dc,#5a6a3a,#20242a,#2a2a30,#5a6a3a,#9fd8c0,#2a9a6a,#9cff9c",
            "0,4,3,0,1,0,3,4,4,0,6,1|#1e1a1e,#f6dcc4,#2a2a2a,#f0e8f0,#3f6f6a,#e8e2ea,#4a4048,#3f6f6a,#d8d8e0,#c8303c,#bfe8ff",
            "2,0,2,2,2,2,1,3,0,2,6,3|#304070,#e8c0a0,#30a0d0,#3060a0,#d8d8e0,#202838,#1a2030,#d8d8e0,#d8d8e0,#3a8fd0,#8fe0ff",
            "1,7,2,1,4,0,6,5,0,4,4,4|#20242a,#e0e8f0,#6a4ab0,#20242a,#7a2a3a,#1a1c20,#181a1c,#7a2a3a,#303038,#7a3ab0,#b48cff",
            "3,3,1,0,1,2,9,2,4,5,8,1|#3a2a24,#f0d2b4,#5a3a20,#46543e,#e8e4dc,#20242a,#2a2a30,#644730,#c8a050,#e0a030,#ffd36a",
            "2,5,4,3,0,4,2,3,5,0,7,5|#3a2a24,#d8a880,#d8a030,#8a6a3a,#f0d080,#2a2022,#221a1c,#7a2a3a,#e2c57b,#e0a030,#ffd36a",
            "1,2,5,1,4,0,7,5,2,0,1,2|#232128,#f0d2b4,#3a2a24,#50545c,#d8d8dc,#25252b,#202024,#78683f,#c8a050,#c8303c,#8fe0ff",
        };
        private static readonly string[] FemalePresets =
        {
            "1,4,1,0,3,0,0,1,0,1,4,4|#24202c,#f6dcc4,#3a2a28,#b0c8ea,#f4eef6,#e8e2ea,#c8b8d0,#8a3a5a,#e2c57b,#c8303c,#ffd6e8",
            "0,7,2,1,4,0,3,3,0,1,2,2|#702b38,#f4d7c4,#c03030,#a03030,#f2ece6,#2a2022,#221a1c,#c8a050,#c8a050,#c8303c,#ff5050",
            "1,5,3,0,1,0,5,1,4,3,8,1|#d8d8e0,#f6dcc4,#30a0d0,#e8f0f4,#3060a0,#202838,#1a2030,#3060a0,#d8d8e0,#3a8fd0,#bfe8ff",
            "0,7,2,0,4,0,6,5,0,4,7,4|#38264b,#e0e8f0,#6a4ab0,#5a4a8a,#302a3a,#302a3a,#282230,#4a3a6a,#e0d8f0,#7a3ab0,#b48cff",
            "1,2,1,1,1,0,1,4,5,1,6,3|#232128,#f0e0cc,#d8a030,#f0d080,#8a3a5a,#e8e2ea,#4a4048,#8a3a5a,#e2c57b,#e0a030,#ffd36a",
            "3,6,3,0,1,0,4,1,0,5,0,1|#3a2a24,#f6dcc4,#5a3a20,#f0e8f4,#9b657e,#f4eef6,#c8b8d0,#9b657e,#e8d6f0,#c8303c,#ffd6e8",
            "1,3,1,0,3,0,8,2,4,0,1,2|#285b56,#f0d2b4,#3a8f7a,#46543e,#e8e4dc,#20242a,#2a2a30,#644730,#9fd8c0,#2a9a6a,#9cff9c",
            "0,5,0,1,0,0,9,4,1,4,7,3|#263862,#f6dcc4,#a0a0a8,#553f6f,#e8d6f0,#282232,#201c28,#9b657e,#e8d6f0,#7a3ab0,#b48cff",
            "1,7,2,0,4,0,3,2,0,2,4,5|#232128,#f0d2b4,#30a0d0,#b0c8ea,#f4eef6,#e8e2ea,#c8b8d0,#3060a0,#d8d8e0,#3a8fd0,#bfe8ff",
            "3,4,1,0,1,0,2,1,3,3,9,1|#dfc9a9,#f6dcc4,#5a3a20,#e8e2d4,#c8a050,#e8e2ea,#c8b8d0,#c8a050,#e2c57b,#e0a030,#ffd36a",
        };

        private void ShowCreator(bool editingExisting = false)
        {
            creatorEditingExisting = editingExisting;
            if (!CultivatorFigure2D.Available) { ShowCharacterCreationForm(resetSelection: true); return; }
            ClearContent();
            if (cityRoot != null) cityRoot.SetActive(false);
            if (worldView != null)
            {
                if (worldView.Player != null && !worldReturnTile.HasValue) worldReturnTile = worldView.Player.Pos;
                worldView.gameObject.SetActive(false);
            }
            authBackdrop = LoginBackdrop.Create(backgroundRoot, Resources.Load<Texture2D>("Brand/LoginLandscapePixel"));
            creatorStep = 0;
            if (editingExisting && hub.IsObject && hub["player"].IsObject)
            {
                var savedLook = hub["player"]["look"].Str();
                creatorLook = string.IsNullOrEmpty(savedLook) ? LookOf(hub["player"]) : LookSpec.Parse(savedLook);
                gender = creatorLook.Get("g", hub["player"]["gender"].Str() == "nu" ? "f" : "m") == "f" ? "nu" : "nam";
                creatorLook.Fill(BuildCreatorPreset(gender == "nu", 0));
                creatorPresetIndex = creatorLook.Int("preset", -1);
                creatorCategory = "fa";
            }
            else if (creatorLook == null) { creatorPresetIndex = 0; creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex); creatorCategory = "preset"; }
            else creatorLook.Fill(BuildCreatorPreset(creatorLook.Get("g") == "f", 0));
            var root = Anchored("Creator", content.transform, Vector2.zero, Vector2.one, new Vector2(-30, 0), new Vector2(30, 0));
            var background = root.gameObject.AddComponent<Image>();
            ModernUi.Fill(background, 28f);
            UiGradient.Apply(background, new Color32(22, 42, 44, 248), new Color32(9, 21, 27, 250));
            background.raycastTarget = false;
            var inner = Anchored("Inner", root, Vector2.zero, Vector2.one, new Vector2(28, 24), new Vector2(-28, -24));
            AnchoredText(inner, "Heading", creatorEditingExisting ? "DIỆN MẠO ĐẠO HỮU" : "KHỞI TẠO ĐẠO HỮU", ModernUi.Bold, 34, Cream, TextAnchor.UpperLeft,
                new Vector2(0, .92f), new Vector2(.5f, 1), new Vector2(8, 0), new Vector2(-8, 0));
            BuildCreatorAvatar(inner);
            BuildCreatorRight(inner);
            BuildCreatorFooter(inner);
            BuildOverlays();
            RefreshCreator();
        }

        private void BuildCreatorAvatar(RectTransform inner)
        {
            var col = Anchored("AvatarCol", inner, new Vector2(0, .16f), new Vector2(.34f, .90f), Vector2.zero, Vector2.zero);
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 22f);
            UiGradient.Apply(panel, new Color32(236, 232, 218, 255), new Color32(214, 220, 206, 255));
            panel.raycastTarget = false;
            var halo = Anchored("Halo", col, new Vector2(.02f, .10f), new Vector2(.98f, .98f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            halo.sprite = InkUi.Cloud;
            halo.color = new Color(.42f, .60f, .54f, .26f);
            halo.raycastTarget = false;
            var view = Anchored("Preview", col, new Vector2(.03f, .12f), new Vector2(.97f, .985f), Vector2.zero, Vector2.zero);
            view.gameObject.AddComponent<RectMask2D>();
            creatorZoom = Anchored("Zoom", view, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            creatorFigure = CultivatorFigure2D.Create(creatorZoom, creatorLook);
            creatorFigure.SafePreview = true;
            creatorMotionControls = Anchored("MotionControls", col, Vector2.zero, new Vector2(1, .11f), new Vector2(10, 8), new Vector2(-10, -2));

            var nameRoot = Anchored("NameField", inner, new Vector2(0, .075f), new Vector2(.34f, .145f), new Vector2(4, 0), new Vector2(-4, 0));
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
            creatorGender = Anchored("Gender", inner, Vector2.zero, new Vector2(.34f, .063f), Vector2.zero, Vector2.zero);
        }

        private void BuildCreatorRight(RectTransform inner)
        {
            var area = Anchored("RightArea", inner, new Vector2(.355f, .16f), new Vector2(1, .90f), Vector2.zero, Vector2.zero);
            creatorStepTabs = Anchored("StepTabs", inner, new Vector2(.52f, .915f), new Vector2(1, 1), Vector2.zero, new Vector2(0, -2));
            creatorAppearancePane = Anchored("Appearance", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var listPanel = Anchored("CategoryPanel", creatorAppearancePane, new Vector2(0, .78f), Vector2.one, Vector2.zero, Vector2.zero);
            var listFill = listPanel.gameObject.AddComponent<Image>();
            ModernUi.Fill(listFill, 20f);
            listFill.color = new Color32(14, 30, 34, 235);
            listFill.raycastTarget = false;
            creatorCategoryList = Anchored("Categories", listPanel, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            var optionPanel = Anchored("OptionPanel", creatorAppearancePane, Vector2.zero, new Vector2(1, .76f), Vector2.zero, Vector2.zero);
            var optionFill = optionPanel.gameObject.AddComponent<Image>();
            ModernUi.Fill(optionFill, 22f);
            optionFill.color = new Color32(251, 248, 239, 236);
            optionFill.raycastTarget = false;
            CreatorPanelBorder(optionPanel);
            creatorOptions = Anchored("Options", optionPanel, new Vector2(0, .24f), Vector2.one, new Vector2(14, 0), new Vector2(-14, -14));
            creatorControls = Anchored("Controls", optionPanel, Vector2.zero, new Vector2(1, .24f), new Vector2(18, 12), new Vector2(-18, -4));
            creatorDestinyPane = Anchored("Destiny", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildCreatorDestiny(creatorDestinyPane);
        }

        private void BuildCreatorDestiny(RectTransform col)
        {
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 24f);
            panel.color = new Color32(251, 248, 239, 236);
            panel.raycastTarget = false;
            CreatorPanelBorder(col);
            AnchoredText(col, "Title", "CĂN CƠ", ModernUi.Bold, 28, new Color32(46, 40, 38, 255), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -58), new Vector2(-20, -16));
            if (creatorEditingExisting)
            {
                creatorTalentArea = null;
                creatorTalentCount = null;
                return;
            }
            sectNames = Names(currentCatalog?.mon);
            elementNames = Names(currentCatalog?.he);
            sectIndex = Mathf.Clamp(sectIndex, 0, Math.Max(0, sectNames.Length - 1));
            elementIndex = Mathf.Clamp(elementIndex, 0, Math.Max(0, elementNames.Length - 1));
            var sect = Anchored("Sect", col, new Vector2(0, 1), new Vector2(.5f, 1), new Vector2(22, -150), new Vector2(-12, -72));
            CreatorCycler(sect, "Môn phái", () => sectNames.Length == 0 ? "—" : sectNames[sectIndex], d => { if (sectNames.Length > 0) sectIndex = (sectIndex + d + sectNames.Length) % sectNames.Length; });
            var element = Anchored("Element", col, new Vector2(.5f, 1), new Vector2(1, 1), new Vector2(12, -150), new Vector2(-22, -72));
            CreatorCycler(element, "Ngũ hành", () => elementNames.Length == 0 ? "—" : elementNames[elementIndex], d => { if (elementNames.Length > 0) elementIndex = (elementIndex + d + elementNames.Length) % elementNames.Length; });
            creatorTalentCount = AnchoredText(col, "TalentTitle", "", ModernUi.SemiBold, 24, new Color32(60, 50, 44, 255), TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -206), new Vector2(-18, -160));
            creatorTalentArea = Anchored("Talents", col, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 14), new Vector2(-16, -212));
        }

        private void BuildCreatorFooter(RectTransform inner)
        {
            creatorFooter = Anchored("Footer", inner, new Vector2(.355f, 0), new Vector2(1, .145f), Vector2.zero, Vector2.zero);
            statusMin = new Vector2(.355f, .145f); statusMax = new Vector2(1, .16f);
        }

        private void RefreshCreatorFooter()
        {
            ClearCreatorChildren(creatorFooter);
            var line = Anchored("Rule", creatorFooter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero).gameObject.AddComponent<Image>();
            line.color = new Color32(60, 50, 44, 90);
            if (!creatorEditingExisting)
            {
                var stats = new[] { ("Khí huyết", "500"), ("Linh lực", "200"), ("Công kích", "50") };
                for (var i = 0; i < stats.Length; i++)
                {
                    var cell = Anchored("Stat" + i, creatorFooter, new Vector2(i * .115f, 0), new Vector2((i + 1) * .115f, 1), new Vector2(4, 6), new Vector2(-4, -8));
                    AnchoredText(cell, "L", stats[i].Item1, ModernUi.Medium, 20, new Color32(170, 195, 190, 255), TextAnchor.UpperCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    AnchoredText(cell, "V", stats[i].Item2, ModernUi.SemiBold, 28, Cream, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
            }
            var back = Anchored("Back", creatorFooter, new Vector2(.37f, .05f), new Vector2(.58f, .90f), new Vector2(8, 0), new Vector2(-8, 0));
            PillButton(back, creatorStep == 1 ? "Diện mạo" : "Quay lại", "arrowLeft", false, () =>
            {
                if (creatorStep == 1) { creatorStep = 0; RefreshCreator(); }
                else BackFromCharacterCreation();
            });
            var start = Anchored("Start", creatorFooter, new Vector2(.60f, .05f), new Vector2(1, .90f), new Vector2(8, 0), new Vector2(-18, 0));
            if (creatorEditingExisting) PillButton(start, "Lưu diện mạo", "arrowRight", true, SaveAppearance);
            else if (creatorStep == 0) PillButton(start, "Tiếp: Căn cơ", "arrowRight", true, () => { creatorStep = 1; RefreshCreator(); });
            else PillButton(start, "Bắt đầu tu luyện", "arrowRight", true, CreateCharacter);
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

        private RectTransform CreatorChip(RectTransform parent, string label, Vector2 min, Vector2 max, bool active, Action click, bool dark = false)
        {
            var rect = Anchored("Chip_" + label, parent, min, max, new Vector2(4, 4), new Vector2(-4, -4));
            var fill = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 12f);
            if (active)
            {
                fill.color = Color.white;
                UiGradient.Apply(fill, new Color32(196, 160, 92, 255), new Color32(150, 112, 58, 255));
            }
            else
            {
                fill.color = dark ? new Color32(30, 52, 56, 255) : new Color32(238, 234, 222, 255);
                var border = Anchored("Border", rect, Vector2.zero, Vector2.one, Vector2.one, -Vector2.one).gameObject.AddComponent<Image>();
                ModernUi.Ring(border, 12f, 1f);
                border.color = dark ? new Color32(120, 150, 140, 90) : new Color32(133, 120, 92, 92);
                border.raycastTarget = false;
            }
            var color = active ? (Color)new Color32(32, 24, 16, 255) : dark ? (Color)new Color32(214, 226, 218, 255) : new Color32(35, 49, 53, 255);
            var text = AnchoredText(rect, "Text", label, ModernUi.SemiBold, 22, color, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 22;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => click());
            rect.gameObject.AddComponent<UiPressScale>();
            return rect;
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

        private CreatorCategory CurrentCategory()
        {
            foreach (var c in CreatorCategoriesV2) if (c.Key == creatorCategory) return c;
            return CreatorCategoriesV2[0];
        }

        private bool CreatorFemale => creatorLook != null && creatorLook.Get("g", "m") == "f";

        private string[] NamesFor(string key)
        {
            if (key == "preset") return CreatorFemale ? FemalePresetNames : MalePresetNames;
            if (key == "ha") return CreatorFemale ? FemaleHairNames : MaleHairNames;
            if (key == "wp") return CreatorWeaponNames;
            if (key == "au") return CreatorAuraNames;
            return OptionNames.TryGetValue(key, out var n) ? n : new[] { "Kiểu 1" };
        }

        private int SelectedIndex(string key)
        {
            if (key == "preset") return creatorLook.Int("preset", -1);
            if (key == "wp") return Mathf.Max(0, Array.IndexOf(CreatorWeaponStyles, creatorLook.Int("wp")));
            return creatorLook.Int(key, 0);
        }

        private void ChooseOption(string key, int index)
        {
            if (key == "preset")
            {
                creatorPresetIndex = index;
                creatorLook = BuildCreatorPreset(CreatorFemale, index);
            }
            else
            {
                creatorLook.Set(key, key == "wp" ? CreatorWeaponStyles[index] : index);
                creatorLook.Set("preset", -1);
            }
            RefreshCreator();
        }

        private void RefreshCreator()
        {
            if (creatorFigure == null) return;
            var editing = creatorEditingExisting;
            if (editing) creatorStep = 0;
            var cat = CurrentCategory();
            if (cat.MaleOnly && CreatorFemale) { creatorCategory = "fa"; cat = CurrentCategory(); }
            creatorFigure.SetLook(creatorLook);
            creatorFigure.SetFraming(creatorStep == 0 && creatorMotion == "idle" ? cat.Preview : CultivatorFigure2D.Framing.Full);
            creatorFigure.SetFacing(creatorFacesRight);
            switch (creatorMotion)
            {
                case "walk": creatorFigure.Airborne = false; creatorFigure.SetPreviewMotion(FighterAction.Walk); break;
                case "run": creatorFigure.Airborne = false; creatorFigure.SetPreviewMotion(FighterAction.Walk, true); break;
                case "attack": creatorFigure.Airborne = false; creatorFigure.SetPreviewMotion(FighterAction.Attack); break;
                case "cast": creatorFigure.Airborne = false; creatorFigure.SetPreviewMotion(FighterAction.Cast); break;
                case "fly": creatorFigure.Airborne = true; creatorFigure.SetPreviewMotion(FighterAction.Walk); break;
                default: creatorFigure.Airborne = false; creatorFigure.SetPreviewMotion(FighterAction.Idle); break;
            }
            // motion chips
            ClearCreatorChildren(creatorMotionControls);
            var motions = new[] { "idle", "walk", "run", "attack", "cast", "fly" };
            var motionNames = new[] { "Đứng", "Đi bộ", "Chạy", "Đánh", "Thi pháp", "Ngự kiếm" };
            var motionIndex = Mathf.Max(0, Array.IndexOf(motions, creatorMotion));
            CreatorChip(creatorMotionControls, "‹", Vector2.zero, new Vector2(.16f, 1), false,
                () => { creatorMotion = motions[(motionIndex + motions.Length - 1) % motions.Length]; RefreshCreator(); });
            CreatorChip(creatorMotionControls, motionNames[motionIndex], new Vector2(.16f, 0), new Vector2(.64f, 1), true,
                () => { creatorMotion = motions[(motionIndex + 1) % motions.Length]; RefreshCreator(); });
            CreatorChip(creatorMotionControls, "›", new Vector2(.64f, 0), new Vector2(.8f, 1), false,
                () => { creatorMotion = motions[(motionIndex + 1) % motions.Length]; RefreshCreator(); });
            CreatorChip(creatorMotionControls, "⇄", new Vector2(.8f, 0), Vector2.one, false, () => { creatorFacesRight = !creatorFacesRight; RefreshCreator(); });
            // gender / random
            ClearCreatorChildren(creatorGender);
            if (!editing)
            {
                CreatorChip(creatorGender, "Nam", Vector2.zero, new Vector2(.28f, 1), gender != "nu", () => SetCreatorGender("nam"), true);
                CreatorChip(creatorGender, "Nữ", new Vector2(.29f, 0), new Vector2(.57f, 1), gender == "nu", () => SetCreatorGender("nu"), true);
            }
            else AnchoredText(creatorGender, "GenderLabel", gender == "nu" ? "Đạo hữu nữ" : "Đạo hữu nam", ModernUi.SemiBold, 23, Cream, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(.57f, 1), new Vector2(8, 0), Vector2.zero);
            CreatorChip(creatorGender, "Ngẫu nhiên", new Vector2(.58f, 0), Vector2.one, false, RandomizeLook, true);
            // step tabs
            ClearCreatorChildren(creatorStepTabs);
            if (!editing)
            {
                CreatorChip(creatorStepTabs, "1 · Diện mạo", Vector2.zero, new Vector2(.5f, 1), creatorStep == 0, () => { creatorStep = 0; RefreshCreator(); }, true);
                CreatorChip(creatorStepTabs, "2 · Căn cơ", new Vector2(.5f, 0), Vector2.one, creatorStep == 1, () => { creatorStep = 1; RefreshCreator(); }, true);
            }
            creatorAppearancePane.gameObject.SetActive(creatorStep == 0);
            creatorDestinyPane.gameObject.SetActive(creatorStep == 1 && !editing);
            RefreshCreatorFooter();
            if (creatorStep == 0) RefreshAppearancePane(cat);
            else RefreshTalents();
        }

        private void RefreshAppearancePane(CreatorCategory cat)
        {
            // category list
            ClearCreatorChildren(creatorCategoryList);
            var group = 0;
            for (var g = 0; g < CreatorGroups.Length; g++) if (Array.IndexOf(CreatorGroups[g], cat.Key) >= 0) group = g;
            for (var g = 0; g < CreatorGroups.Length; g++)
            {
                var index = g;
                CreatorChip(creatorCategoryList, CreatorGroupNames[g], new Vector2(g / 4f, .5f), new Vector2((g + 1) / 4f, 1), group == g,
                    () => { creatorCategory = CreatorGroups[index][0]; RefreshCreator(); }, true);
            }
            var visible = new List<CreatorCategory>();
            foreach (var c in CreatorCategoriesV2) if (Array.IndexOf(CreatorGroups[group], c.Key) >= 0 && !(c.MaleOnly && CreatorFemale)) visible.Add(c);
            for (var i = 0; i < visible.Count; i++)
            {
                var c = visible[i];
                CreatorChip(creatorCategoryList, c.Label, new Vector2(i / (float)visible.Count, 0), new Vector2((i + 1f) / visible.Count, .48f),
                    c.Key == creatorCategory, () => { creatorCategory = c.Key; RefreshCreator(); }, true);
            }
            // option grid with live thumbnails
            ClearCreatorChildren(creatorOptions);
            var names = NamesFor(cat.Key);
            var selected = SelectedIndex(cat.Key);
            var count = names.Length;
            var columns = count <= 4 ? 4 : count <= 6 ? 3 : 5;
            if (cat.Key == "wp" || cat.Key == "au") columns = 3;
            var rows = Mathf.CeilToInt(count / (float)columns);
            var controlRows = (cat.Sliders?.Length ?? 0) + (cat.Colors?.Length ?? 0);
            var controlHeight = controlRows == 0 ? .13f : controlRows == 1 ? .22f : Mathf.Min(.48f, .14f * controlRows);
            creatorOptions.anchorMin = new Vector2(0, controlHeight);
            creatorControls.anchorMax = new Vector2(1, controlHeight);
            for (var i = 0; i < count; i++)
            {
                var x = i % columns; var y = i / columns;
                var min = new Vector2(x / (float)columns, 1f - (y + 1f) / rows);
                var max = new Vector2((x + 1f) / columns, 1f - y / (float)rows);
                var index = i;
                var cell = Anchored("Option" + i, creatorOptions, min, max, new Vector2(5, 5), new Vector2(-5, -5));
                var fill = cell.gameObject.AddComponent<Image>();
                ModernUi.Fill(fill, 14f);
                fill.color = index == selected ? new Color32(214, 186, 120, 255) : new Color32(232, 228, 214, 255);
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.onClick.AddListener(() => ChooseOption(cat.Key, index));
                cell.gameObject.AddComponent<UiPressScale>();
                var frame = Anchored("Thumb", cell, new Vector2(0, .22f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -4));
                frame.gameObject.AddComponent<RectMask2D>();
                var look = cat.Key == "preset" ? BuildCreatorPreset(CreatorFemale, index) : LookSpec.Parse(creatorLook.ToString());
                if (cat.Key != "preset") look.Set(cat.Key, cat.Key == "wp" ? CreatorWeaponStyles[index] : index);
                var thumb = CultivatorFigure2D.Create(frame, look, cat.Thumb);
                thumb.SafePreview = cat.Thumb == CultivatorFigure2D.Framing.Full;
                thumb.SetMotion(FighterAction.Idle, 0, false, true, .4f);
                var label = AnchoredText(cell, "Label", names[i], ModernUi.SemiBold, 18, new Color32(40, 34, 32, 255), TextAnchor.MiddleCenter,
                    Vector2.zero, new Vector2(1, .22f), new Vector2(4, 0), new Vector2(-4, 0));
                label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = 18;
            }
            // sliders and colours
            ClearCreatorChildren(creatorControls);
            var sliderCount = cat.Sliders?.Length ?? 0;
            var colorCount = cat.Colors?.Length ?? 0;
            var rowsTotal = Mathf.Max(1, sliderCount + colorCount);
            var row = 0;
            for (var i = 0; i < sliderCount; i++, row++)
                CreatorSlider(Anchored("Slider" + i, creatorControls, new Vector2(0, 1f - (row + 1f) / rowsTotal), new Vector2(1, 1f - row / (float)rowsTotal), Vector2.zero, Vector2.zero),
                    cat.SliderLabels[i], cat.Sliders[i]);
            for (var i = 0; i < colorCount; i++, row++)
                CreatorSwatches(Anchored("Colors" + i, creatorControls, new Vector2(0, 1f - (row + 1f) / rowsTotal), new Vector2(1, 1f - row / (float)rowsTotal), Vector2.zero, Vector2.zero),
                    cat.ColorLabels[i], cat.Colors[i]);
            if (rowsTotal == 1 && sliderCount + colorCount == 0)
                AnchoredText(creatorControls, "Hint", cat.Key == "preset" ? "Chọn một phong cách rồi tinh chỉnh từng bộ phận ở danh mục bên trái." : "Chọn kiểu ở trên.",
                    ModernUi.Regular, 21, new Color32(70, 80, 76, 255), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void CreatorSlider(RectTransform row, string label, string key)
        {
            AnchoredText(row, "Label", label, ModernUi.SemiBold, 20, new Color32(50, 44, 40, 255), TextAnchor.MiddleLeft, Vector2.zero, new Vector2(.24f, 1), Vector2.zero, Vector2.zero);
            var track = Anchored("Track", row, new Vector2(.26f, .5f), new Vector2(.97f, .5f), new Vector2(0, -6), new Vector2(0, 6));
            var trackImage = track.gameObject.AddComponent<Image>();
            ModernUi.Fill(trackImage, 6f);
            trackImage.color = new Color32(200, 190, 168, 255);
            var fillArea = Anchored("FillArea", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = Anchored("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImage = fill.gameObject.AddComponent<Image>();
            ModernUi.Fill(fillImage, 6f);
            fillImage.color = new Color32(150, 112, 58, 255);
            var handleArea = Anchored("HandleArea", track, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var handle = Anchored("Handle", handleArea, Vector2.zero, new Vector2(0, 1), new Vector2(-14, -12), new Vector2(14, 12));
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = InkUi.Glow;
            ModernUi.Fill(handleImage, 14f);
            handleImage.color = new Color32(60, 46, 30, 255);
            var slider = row.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 20; slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(Mathf.Clamp(creatorLook.Int(key, 10), 0, 20));
            slider.onValueChanged.AddListener(v =>
            {
                creatorLook.Set(key, Mathf.RoundToInt(v));
                creatorLook.Set("preset", -1);
                creatorFigure.SetLook(creatorLook);   // live, without rebuilding the panel under the finger
            });
        }

        private void CreatorSwatches(RectTransform row, string label, string key)
        {
            AnchoredText(row, "Label", label, ModernUi.SemiBold, 20, new Color32(50, 44, 40, 255), TextAnchor.MiddleLeft, Vector2.zero, new Vector2(.24f, 1), Vector2.zero, Vector2.zero);
            var palette = key == "sk" ? AvatarComposer.SkinColors : key == "hc" ? AvatarComposer.HairColors : key == "ec" ? AvatarComposer.EyeColors
                : key == "auc" ? AvatarComposer.AuraColors : key == "mc" ? MarkColors : key == "hac" ? HatColors : AvatarComposer.ClothColors;
            var count = Mathf.Min(palette.Length, 12);
            var area = Anchored("Swatches", row, new Vector2(.26f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            for (var i = 0; i < count; i++)
            {
                var hex = palette[i];
                var rect = Anchored("Swatch" + i, area, new Vector2(i / (float)count, .5f), new Vector2((i + 1f) / count, .5f), new Vector2(3, -18), new Vector2(-3, 18));
                var fill = rect.gameObject.AddComponent<Image>();
                ModernUi.Fill(fill, 10f);
                fill.color = HeroSprites.ParseColor(hex, Color.gray);
                if (string.Equals(creatorLook.Get(key), hex, StringComparison.OrdinalIgnoreCase))
                {
                    var ring = Anchored("Ring", rect, new Vector2(-.12f, -.12f), new Vector2(1.12f, 1.12f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                    ModernUi.Ring(ring, 12f, 2.5f);
                    ring.color = new Color32(40, 34, 32, 255);
                    ring.raycastTarget = false;
                }
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.onClick.AddListener(() => { creatorLook.Set(key, hex); if (key == "auc" && creatorLook.Int("au") == 0) creatorLook.Set("au", 1); creatorLook.Set("preset", -1); RefreshCreator(); });
            }
        }

        private void RefreshTalents()
        {
            if (creatorTalentArea == null) return;
            ClearCreatorChildren(creatorTalentArea);
            creatorTalentCount.text = $"Tiên thiên khí vận  ·  {selectedTalents.Count}/3";
            for (var i = 0; i < CreationTalentIds.Length; i++)
            {
                var id = CreationTalentIds[i];
                var col = i % 3;
                var row = i / 3;
                var rows = (CreationTalentIds.Length + 2) / 3;
                CreatorChip(creatorTalentArea, CreationTalentNames[i], new Vector2(col / 3f, 1f - (row + 1f) / rows), new Vector2((col + 1) / 3f, 1f - row / (float)rows),
                    selectedTalents.Contains(id), () =>
                    {
                        if (selectedTalents.Contains(id)) selectedTalents.Remove(id);
                        else if (selectedTalents.Count >= 3) { ShowStatus("Chỉ chọn tối đa 3 khí vận."); return; }
                        else selectedTalents.Add(id);
                        RefreshCreator();
                    });
            }
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
            if (parent == null) return;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                // Disable immediately so rapid taps cannot hit last frame's choices.
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        private static readonly string[] PresetStyleKeys = { "fa", "ey", "br", "no", "mo", "bd", "ha", "to", "hat", "ma", "wp", "au" };
        private static readonly string[] PresetColorKeys = { "hc", "sk", "ec", "oc", "tc", "pc", "sc", "bc", "hac", "mc", "auc" };

        private static LookSpec BuildCreatorPreset(bool female, int preset)
        {
            var list = female ? FemalePresets : MalePresets;
            var i = (preset % list.Length + list.Length) % list.Length;
            var look = AvatarComposer.Default(female);
            look.Set("g", female ? "f" : "m");
            look.Set("preset", i);
            var halves = list[i].Split('|');
            var styles = halves[0].Split(',');
            var colors = halves[1].Split(',');
            for (var k = 0; k < PresetStyleKeys.Length && k < styles.Length; k++) look.Set(PresetStyleKeys[k], styles[k].Trim());
            for (var k = 0; k < PresetColorKeys.Length && k < colors.Length; k++) look.Set(PresetColorKeys[k], colors[k].Trim());
            if (female) look.Set("bd", 0);
            foreach (var key in new[] { "ez", "es", "eh", "bh", "nh", "mh" }) look.Set(key, 10);
            return look;
        }

        private void RandomizeLook()
        {
            var rng = new System.Random();
            var female = CreatorFemale;
            var look = BuildCreatorPreset(female, rng.Next(10));
            look.Set("preset", -1);
            look.Set("fa", rng.Next(4)); look.Set("ey", rng.Next(8)); look.Set("br", rng.Next(5)); look.Set("no", rng.Next(4)); look.Set("mo", rng.Next(5));
            look.Set("bd", female || rng.NextDouble() < .6 ? 0 : 1 + rng.Next(4));
            look.Set("ha", rng.Next(10)); look.Set("to", rng.Next(6));
            look.Set("hat", rng.NextDouble() < .55 ? 0 : 1 + rng.Next(5));
            look.Set("ma", rng.NextDouble() < .6 ? 0 : 1 + rng.Next(5));
            look.Set("wp", CreatorWeaponStyles[rng.Next(CreatorWeaponStyles.Length)]);
            look.Set("hc", AvatarComposer.HairColors[rng.Next(AvatarComposer.HairColors.Length)]);
            look.Set("ec", AvatarComposer.EyeColors[rng.Next(AvatarComposer.EyeColors.Length)]);
            look.Set("oc", AvatarComposer.ClothColors[rng.Next(AvatarComposer.ClothColors.Length)]);
            look.Set("auc", AvatarComposer.AuraColors[rng.Next(AvatarComposer.AuraColors.Length)]);
            foreach (var key in new[] { "ez", "es", "eh", "bh", "nh", "mh" }) look.Set(key, 7 + rng.Next(7));
            creatorLook = look;
            RefreshCreator();
        }
    }
}
