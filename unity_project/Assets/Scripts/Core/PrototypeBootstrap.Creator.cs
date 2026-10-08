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
        private Text creatorStyleLabel;
        private RectTransform creatorSwatches;
        private RectTransform creatorChips;
        private RectTransform creatorTalentArea;
        private Text creatorTalentCount;

        private static readonly (string key, string label, string colorKey, string colorLabel)[] CreatorCategories =
        {
            ("preset", "Mẫu", "tc", "Màu áo"),
            ("bo", "Dáng người", "sk", "Màu da"),
            ("fa", "Khuôn mặt", "sk", "Màu da"), ("ey", "Mắt", "ec", "Màu mắt"), ("br", "Lông mày", "hc", "Màu tóc"),
            ("no", "Mũi", "sk", "Màu da"), ("mo", "Miệng", null, null), ("ea", "Tai", "sk", "Màu da"),
            ("bd", "Râu", "hc", "Màu tóc"), ("ha", "Kiểu tóc", "hc", "Màu tóc"), ("hat", "Mũ / Quan", "hac", "Màu mũ"),
            ("ti", "Áo trong", "tc", "Màu áo"), ("to", "Áo ngoài", "oc", "Màu áo"), ("tot", "Viền áo", "ac", "Màu viền"),
            ("pa", "Quần / Váy", "pc", "Màu quần"), ("sh", "Giày", "sc", "Màu giày"), ("be", "Đai lưng", "bc", "Màu đai"),
            ("wp", "Binh khí", null, null), ("au", "Khí tức", "auc", "Màu khí"),
        };

        private static readonly Dictionary<string, string[]> StyleNames = new Dictionary<string, string[]>
        {
            { "bo", new[] { "Mảnh mai", "Cân đối", "Rắn chắc", "Đầy đặn" } },
            { "fa", new[] { "Tuấn tú", "Góc cạnh", "Thanh tú", "Phúc hậu" } },
            { "ey", new[] { "Phượng nhãn", "Tuấn mục", "Lãnh mâu", "Hiền nhãn", "Mi dài", "Hung mục", "Đào hoa", "Bế mục tĩnh tọa" } },
            { "br", new[] { "Kiếm mi", "Mày ngang", "Mày dựng", "Mày cong", "Lá liễu" } },
            { "no", new[] { "Thanh tú", "Sống cao", "Nhỏ nhắn", "Rộng" } },
            { "mo", new[] { "Mím chặt", "Nhếch mép", "Môi nhỏ", "Nghiêm nghị", "Mỉm cười" } },
            { "ea", new[] { "Thường", "Tai nhọn", "Khuyên ngọc" } },
            { "bd", new[] { "Không", "Chòm dê", "Râu tiên ông", "Râu quai nón", "Ria đạo sĩ" } },
            { "ha", new[] { "Búi tó kiếm tiên", "Nửa búi thư sinh", "Rẽ ngôi xõa dài", "Đuôi ngựa cao", "Đầu trọc", "Búi đạo sĩ", "Cuồng phát ma tu", "Tết bím", "Mái lệch", "Ngắn bù xù" } },
            { "ti", new[] { "Trường bào giao lĩnh", "Kình trang võ phục", "Nhu quần", "Đạo bào" } },
            { "to", new[] { "Không", "Đại sưởng", "Bối tử", "Phi phong", "Sa y", "Giáp trụ" } },
            { "tot", new[] { "Không viền", "Vân mây", "Liên hoa", "Lôi văn", "Cổ triện", "Kim tuyến" } },
            { "pa", new[] { "Quần vải", "Xà cạp", "Váy xếp ly", "Quần ống rộng" } },
            { "sh", new[] { "Ủng vải", "Vân lý", "Dép cỏ" } },
            { "be", new[] { "Đai lụa", "Ngọc đái", "Dây thừng hồ lô" } },
            { "hat", new[] { "Không", "Kim quan", "Đấu lạp", "Liên hoa quan", "Mạt ngạch", "Ngọc quan bộ dao" } },
            { "wp", new[] { "Không", "Kiếm sau lưng", "Phi kiếm", "Kiếm bên hông", "Kiếm trong tay", "Trọng chùy", "Trượng tiên", "Thiết phủ", "Bút trận", "Hộ thủ quyền", "Lò đỉnh hộ thân" } },
            { "au", AvatarComposer.AuraNames },
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

        private static readonly string[] HairNamesFemale = { "Song búi tiên nữ", "Búi cao xõa dài", "Rẽ ngôi xõa", "Đuôi ngựa cao", "Nửa búi", "Bím lệch", "Hai bím", "Búi cung trang", "Tóc ngắn", "Vương miện tết" };

        private RectTransform creatorZoom;

        private bool creatorEditingExisting;

        private void ShowCreator(bool editingExisting = false)
        {
            creatorEditingExisting = editingExisting;
            if (!CultivatorPuppet2D.Available) { ShowCharacterCreationForm(resetSelection: true); return; }
            ClearContent();
            authBackdrop = LoginBackdrop.Create(backgroundRoot, Resources.Load<Texture2D>("Brand/LoginLandscapePixel"));
            if (editingExisting && hub.IsObject && hub["player"].IsObject)
            {
                creatorLook = LookOf(hub["player"]);
                gender = creatorLook.Get("g", hub["player"]["gender"].Str() == "nu" ? "f" : "m") == "f" ? "nu" : "nam";
                creatorPresetIndex = creatorLook.Int("preset", -1);
                creatorCategory = creatorPresetIndex >= 0 ? "preset" : "ha";
            }
            else if (creatorLook == null) { creatorPresetIndex = 0; creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex); creatorCategory = "preset"; }
            var root = Anchored("Creator", content.transform, new Vector2(-.05f, -.03f), new Vector2(1.05f, 1.03f), Vector2.zero, Vector2.zero);
            // scroll: two rollers and parchment
            var paper = Anchored("Paper", root, Vector2.zero, Vector2.one, new Vector2(70, 18), new Vector2(-70, -18));
            var paperImage = paper.gameObject.AddComponent<Image>();
            paperImage.sprite = InkUi.Paper;
            paperImage.type = Image.Type.Tiled;
            paperImage.color = new Color32(246, 242, 232, 255);
            var wash = Anchored("Wash", paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            wash.sprite = InkUi.Cloud;
            wash.color = new Color(.85f, .86f, .84f, .55f);
            wash.raycastTarget = false;
            foreach (var side in new[] { 0f, 1f })
            {
                var roller = Anchored("Roller", root, new Vector2(side, 0), new Vector2(side, 1), new Vector2(side == 0 ? 6 : -94, 0), new Vector2(side == 0 ? 94 : -6, 0));
                var rimg = roller.gameObject.AddComponent<Image>();
                ModernUi.Fill(rimg, 36f);
                UiGradient.Apply(rimg, new Color32(96, 82, 70, 255), new Color32(38, 32, 30, 255), horizontal: true, mirror: true);
                rimg.raycastTarget = false;
                foreach (var end in new[] { 0f, 1f })
                {
                    var cap = Anchored("Cap", roller, new Vector2(-.1f, end), new Vector2(1.1f, end), new Vector2(0, end == 0 ? -10 : -40), new Vector2(0, end == 0 ? 40 : 10)).gameObject.AddComponent<Image>();
                    ModernUi.Fill(cap, 14f);
                    cap.color = new Color32(48, 40, 38, 255);
                    cap.raycastTarget = false;
                }
            }
            var inner = Anchored("Inner", paper, Vector2.zero, Vector2.one, new Vector2(40, 30), new Vector2(-40, -30));
            BuildCreatorAvatar(inner);
            BuildCreatorCustomizer(inner);
            BuildCreatorDestiny(inner);
            BuildCreatorFooter(inner);
            BuildOverlays();
            RefreshCreator();
        }

        private void BuildCreatorAvatar(RectTransform inner)
        {
            // Keep the cultivator in the visual center, with the appearance and destiny panels on either side.
            var col = Anchored("AvatarCol", inner, new Vector2(.32f, .14f), new Vector2(.68f, 1), Vector2.zero, Vector2.zero);
            var halo = Anchored("Halo", col, new Vector2(.05f, .18f), new Vector2(.95f, .98f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            halo.sprite = InkUi.Cloud;
            halo.color = new Color(.62f, .66f, .66f, .55f);
            halo.raycastTarget = false;
            var view = Anchored("Preview", col, new Vector2(.02f, .19f), new Vector2(.98f, 1f), new Vector2(18, 12), new Vector2(-18, -12));
            creatorZoom = Anchored("Zoom", view, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            creatorRig = CultivatorPuppet2D.Create(creatorZoom, creatorLook);
        }

        private void BuildCreatorCustomizer(RectTransform inner)
        {
            var col = Anchored("Custom", inner, new Vector2(.015f, .14f), new Vector2(.30f, 1), new Vector2(6, 0), new Vector2(-6, 0));
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 24f);
            panel.color = new Color32(251, 248, 239, 232);
            panel.raycastTarget = false;
            ModernUi.Soft(col, 24f, new Color32(24, 38, 42, 28), new Vector2(0, -4), 2f);
            CreatorPanelBorder(col);
            AnchoredText(col, "Title", "DIỆN MẠO", ModernUi.Display, 34, new Color32(46, 40, 38, 255), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), Vector2.zero);
            creatorChips = Anchored("Chips", col, new Vector2(0, .46f), new Vector2(1, 1), new Vector2(16, 0), new Vector2(-16, -76));
            var stepper = Anchored("Stepper", inner, new Vector2(.34f, .21f), new Vector2(.66f, .29f), Vector2.zero, Vector2.zero);
            var prev = Anchored("Prev", stepper, new Vector2(0, 0), new Vector2(.18f, 1), Vector2.zero, Vector2.zero);
            CreatorArrowButton(prev, "arrowLeft", () => StepCreatorStyle(-1));
            var next = Anchored("Next", stepper, new Vector2(.82f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            CreatorArrowButton(next, "arrowRight", () => StepCreatorStyle(1));
            creatorStyleLabel = AnchoredText(stepper, "Style", "", ModernUi.SemiBold, 27, new Color32(40, 34, 32, 255), TextAnchor.MiddleCenter, new Vector2(.18f, 0), new Vector2(.82f, 1), Vector2.zero, Vector2.zero);
            creatorSwatches = Anchored("Swatches", inner, new Vector2(.35f, .15f), new Vector2(.65f, .21f), Vector2.zero, Vector2.zero);

            var nameBox = Anchored("NameBox", col, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-230, 62), new Vector2(230, 62 + 38 + AuthFieldHeight));
            nameInput = AuthField(nameBox, "name", "Đạo hiệu", "Tên nhân vật (2–24 ký tự)", "user", 0f, 0f, 460f, false);
            nameInput.characterLimit = 24;
            nameInput.textComponent.color = new Color32(248, 248, 239, 255);
            if (nameInput.placeholder is Text nameHint) nameHint.color = new Color32(207, 221, 216, 255);
            var nameLabel = nameBox.Find("nameLabel")?.GetComponent<Text>();
            if (nameLabel != null)
            {
                nameLabel.color = new Color32(48, 75, 70, 255);
                nameLabel.font = ModernUi.SemiBold;
            }
            if (creatorEditingExisting && hub.IsObject && hub["player"].IsObject)
            {
                nameInput.text = Clean(hub["player"]["fullName"].Str(hub["player"]["name"].Str()));
                nameInput.interactable = false;
            }
            var genderRow = Anchored("Gender", col, new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 0), new Vector2(-10, 56));
            CreatorChip(genderRow, "Nam", new Vector2(0, 0), new Vector2(.33f, 1), gender != "nu", () => SetCreatorGender("nam"));
            CreatorChip(genderRow, "Nữ", new Vector2(.34f, 0), new Vector2(.66f, 1), gender == "nu", () => SetCreatorGender("nu"));
            CreatorChip(genderRow, "Ngẫu nhiên", new Vector2(.67f, 0), new Vector2(1, 1), false, RandomizeLook);
        }

        private void BuildCreatorDestiny(RectTransform inner)
        {
            var col = Anchored("Destiny", inner, new Vector2(.70f, .14f), new Vector2(.99f, 1), new Vector2(6, 0), new Vector2(-6, 0));
            var panel = col.gameObject.AddComponent<Image>();
            ModernUi.Fill(panel, 24f);
            panel.color = new Color32(251, 248, 239, 232);
            panel.raycastTarget = false;
            ModernUi.Soft(col, 24f, new Color32(24, 38, 42, 28), new Vector2(0, -4), 2f);
            CreatorPanelBorder(col);
            AnchoredText(col, "Title", "CĂN CƠ", ModernUi.Display, 34, new Color32(46, 40, 38, 255), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), Vector2.zero);
            sectNames = Names(currentCatalog?.mon);
            elementNames = Names(currentCatalog?.he);
            sectIndex = Mathf.Clamp(sectIndex, 0, Math.Max(0, sectNames.Length - 1));
            elementIndex = Mathf.Clamp(elementIndex, 0, Math.Max(0, elementNames.Length - 1));
            var sect = Anchored("Sect", col, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -136), new Vector2(-18, -62));
            CreatorCycler(sect, "Môn phái", () => sectNames.Length == 0 ? "—" : sectNames[sectIndex], d => { if (sectNames.Length > 0) sectIndex = (sectIndex + d + sectNames.Length) % sectNames.Length; });
            var element = Anchored("Element", col, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -218), new Vector2(-18, -144));
            CreatorCycler(element, "Ngũ hành", () => elementNames.Length == 0 ? "—" : elementNames[elementIndex], d => { if (elementNames.Length > 0) elementIndex = (elementIndex + d + elementNames.Length) % elementNames.Length; });
            creatorTalentCount = AnchoredText(col, "TalentTitle", "", ModernUi.SemiBold, 24, new Color32(60, 50, 44, 255), TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -268), new Vector2(-18, -228));
            creatorTalentArea = Anchored("Talents", col, new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 0), new Vector2(-18, -276));
        }

        private void BuildCreatorFooter(RectTransform inner)
        {
            var footer = Anchored("Footer", inner, Vector2.zero, new Vector2(1, .13f), Vector2.zero, Vector2.zero);
            var line = Anchored("Rule", footer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero).gameObject.AddComponent<Image>();
            line.color = new Color32(60, 50, 44, 90);
            var stats = new[] { ("Thể chất", "500"), ("Linh lực", "200"), ("Công kích", "50"), ("Phòng ngự", "30"), ("Tốc độ", "10"), ("Thần thức", "10") };
            for (var i = 0; i < stats.Length; i++)
            {
                var cell = Anchored("Stat" + i, footer, new Vector2(i * .1f, 0), new Vector2((i + 1) * .1f, 1), new Vector2(4, 6), new Vector2(-4, -8));
                AnchoredText(cell, "L", stats[i].Item1, ModernUi.Medium, 20, new Color32(61, 76, 76, 255), TextAnchor.UpperCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                AnchoredText(cell, "V", stats[i].Item2, ModernUi.SemiBold, 28, new Color32(40, 34, 32, 255), TextAnchor.LowerCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            var back = Anchored("Back", footer, new Vector2(.61f, .1f), new Vector2(.75f, .9f), new Vector2(8, 0), new Vector2(-8, 0));
            PillButton(back, "Quay lại", "arrowLeft", false, BackFromCharacterCreation);
            var start = Anchored("Start", footer, new Vector2(.76f, .1f), new Vector2(1, .9f), new Vector2(8, 0), new Vector2(-18, 0));
            if (creatorEditingExisting)
                PillButton(start, "Lưu diện mạo", "arrowRight", true, SaveAppearance);
            else
                PillButton(start, "Bắt đầu tu luyện", "arrowRight", true, CreateCharacter);
            statusMin = new Vector2(.3f, .135f); statusMax = new Vector2(.7f, .17f);
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
                PlayerPrefs.SetString("tutien_offline_look", text);
                PlayerPrefs.Save();
                Toast("Đã lưu diện mạo mới thành công!");
                OpenCharacterScreen();
                return;
            }
            client.Post("/player/look", Body("look", text), (result, error) =>
            {
                ShowBusy(false);
                if (error != null) { Toast(error, true); return; }
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
                Toast("Đã áp dụng diện mạo mới thành công!");
                OpenCharacterScreen();
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
            ApplyCreatorZoom();
            // category chips (3 columns)
            for (var i = creatorChips.childCount - 1; i >= 0; i--) Destroy(creatorChips.GetChild(i).gameObject);
            for (var i = 0; i < CreatorCategories.Length; i++)
            {
                var c = CreatorCategories[i];
                var col = i % 3;
                var row = i / 3;
                var rows = (CreatorCategories.Length + 2) / 3;
                CreatorChip(creatorChips, c.label, new Vector2(col / 3f, 1f - (row + 1f) / rows), new Vector2((col + 1) / 3f, 1f - row / (float)rows),
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
            var key = creatorCategory;
            var names = key == "ha" && creatorLook.Get("g") == "f" ? HairNamesFemale : StyleNames.TryGetValue(key, out var n) ? n : new[] { "Kiểu 1" };
            var index = key == "preset" ? Mathf.Clamp(creatorPresetIndex, 0, MalePresetNames.Length - 1) : Mathf.Clamp(creatorLook.Int(key), 0, names.Length - 1);
            if (key == "preset") names = creatorLook.Get("g") == "f" ? FemalePresetNames : MalePresetNames;
            creatorStyleLabel.text = names[index] + $"   ({index + 1}/{names.Length})";
            // swatches
            for (var i = creatorSwatches.childCount - 1; i >= 0; i--) Destroy(creatorSwatches.GetChild(i).gameObject);
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
                    var rect = Anchored("Swatch" + i, creatorSwatches, new Vector2(i / (float)count, .1f), new Vector2((i + 1) / (float)count, .9f), new Vector2(4, 4), new Vector2(-4, -4));
                    var fill = rect.gameObject.AddComponent<Image>();
                    fill.sprite = InkUi.Glow;
                    fill.color = HeroSprites.ParseColor(hex, Color.gray);
                    if (string.Equals(creatorLook.Get(colorKey), hex, StringComparison.OrdinalIgnoreCase))
                    {
                        var ring = Anchored("Ring", rect, new Vector2(-.1f, -.1f), new Vector2(1.1f, 1.1f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                        ring.sprite = InkUi.Ring;
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
            for (var i = creatorTalentArea.childCount - 1; i >= 0; i--) Destroy(creatorTalentArea.GetChild(i).gameObject);
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
            var count = AvatarComposer.Counts.TryGetValue(key, out var c) ? c : 1;
            creatorLook.Set(key, (creatorLook.Int(key) + delta + count) % count);
            creatorLook.Set("preset", -1);
            RefreshCreator();
        }

        private void SetCreatorGender(string value)
        {
            gender = value == "nu" ? "nu" : "nam";
            var saved = nameInput != null ? nameInput.text : "";
            creatorPresetIndex = 0;
            creatorCategory = "preset";
            creatorLook = BuildCreatorPreset(gender == "nu", creatorPresetIndex);
            ShowCreator();
            if (nameInput != null) nameInput.text = saved;
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
            look.Set("wp", (i * 7 + (female ? 2 : 4)) % AvatarComposer.Counts["wp"]);
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
            creatorCategory = "ha";
            creatorLook.Set("preset", -1);
            var rng = new System.Random();
            foreach (var pair in AvatarComposer.Counts)
            {
                if (pair.Key == "bd" && creatorLook.Get("g") == "f") { creatorLook.Set("bd", 0); continue; }
                var max = pair.Value;
                var value = rng.Next(max);
                if (pair.Key == "hat" && rng.NextDouble() < .6) value = 0;
                if (pair.Key == "bd" && rng.NextDouble() < .6) value = 0;
                if (pair.Key == "ey" && value == 7 && rng.NextDouble() < .7) value = 0;
                creatorLook.Set(pair.Key, value);
            }
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
