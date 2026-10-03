using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Native system screens (bag, character, skills, shop, market, healer, teleport, bounties,
    /// rankings, mail, events, crafting, library, sect, social, PvP, city lord). All data comes
    /// from the IPA server views; every action goes through Act() which refreshes the player view.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private string bagTab = "bag";
        private string bagSelected;
        private string skillSelected;
        private int rankTab;
        private int craftTab;
        private string craftSelected;
        private int socialTab;
        private int teleportTab;

        private Action BackTarget() => cityTownId != null ? (Action)BackToCity : ShowWorld;

        private static readonly string[] EquipOrder = { "weapon", "armor", "acc1", "acc2", "ring1", "ring2", "nhanNaDi", "phiKiem", "loDinh", "nhanTruDo" };
        private static readonly string[] EquipIcons = { "atk", "def", "equipped", "equipped", "equipped", "equipped", "na_di", "flight", "dong_phu", "hanh_trang" };

        // ================================================================== Hành trang

        private string bagFilter = "all";
        private static readonly string[] BagFilterKeys = { "all", "equip", "cons", "mat", "other" };
        private static readonly string[] BagFilterNames = { "Tất cả", "Trang bị", "Đan dược", "Nguyên liệu", "Khác" };

        private static string BagCategory(J item)
        {
            switch (item["kind"].Str())
            {
                case "equip": return "equip";
                case "cons": return "cons";
                case "mat": return "mat";
                default: return "other";
            }
        }

        /// <summary>
        /// Hành trang, QCBH style: the character wearing its gear with the ten equipment slots around it,
        /// the bag as a grid of quality-coloured cells (empty cells show the remaining room), and the
        /// selected item's sheet on the right with its stats compared against what is equipped.
        /// </summary>
        private void OpenBagScreen()
        {
            var back = BackTarget();
            var player = hub["player"];
            var body = OpenScreen("Hành Trang", $"{player["bag"].Count}/{player["bagSize"].Int(30)} ô · Kho {player["kho"].Count}/{player["khoSize"].Int(80)}", "hanh_trang", back);
            var tabs = new[] { "Hành trang", "Kho", "Két an toàn" };
            var keys = new[] { "bag", "kho", "safe" };
            var active = Array.IndexOf(keys, bagTab);
            var area = Tabs(body, tabs, Mathf.Max(0, active), i => { bagTab = keys[i]; bagSelected = null; OpenBagScreen(); });
            var eq = player["equip"];

            // ---- left: the figure in its gear, five slots on each side
            var doll = Anchored("Doll", area, Vector2.zero, new Vector2(.27f, 1), Vector2.zero, new Vector2(-8, 0));
            GlassPanel(doll, 26f, new Color32(26, 30, 34, 228), new Color32(12, 15, 18, 232));
            var halo = InkUi.Simple(doll, "Halo", InkUi.Glow, new Color(.92f, .76f, .46f, .16f), Vector2.zero);
            halo.rectTransform.anchorMin = new Vector2(.1f, .12f); halo.rectTransform.anchorMax = new Vector2(.9f, .92f);
            halo.rectTransform.offsetMin = halo.rectTransform.offsetMax = Vector2.zero;
            if (AvatarComposer.Available)
            {
                // the figure fits its parent, so a frame keeps it clear of the name above and the realm line below
                var frame = Anchored("Figure", doll, Vector2.zero, Vector2.one, new Vector2(70, 46), new Vector2(-70, -54));
                AvatarComposer.Build(frame, LookOf(player), AvatarComposer.AuraStrength(hub["realm"]["index"].Int()));
            }
            var nameText = AnchoredText(doll, "Name", Clean(player["name"].Str()), ModernUi.SemiBold, 26, AuthGoldAccent, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(110, -46), new Vector2(-110, -10));
            nameText.resizeTextForBestFit = true; nameText.resizeTextMinSize = 16; nameText.resizeTextMaxSize = 26;
            AnchoredText(doll, "Realm", Clean(hub["realm"]["name"].Str()) + (string.IsNullOrEmpty(player["heName"].Str()) ? "" : " · hệ " + Clean(player["heName"].Str())),
                ModernUi.Regular, 19, AuthTextSecondary, TextAnchor.LowerCenter, Vector2.zero, new Vector2(1, 0), new Vector2(104, 8), new Vector2(-104, 40));
            const float slotSize = 92f, slotGap = 10f, slotTop = 12f;
            for (var i = 0; i < EquipOrder.Length; i++)
            {
                var slot = EquipOrder[i];
                var item = eq[slot];
                var rightSide = i >= 5;
                var row = i % 5;
                var top = slotTop + row * (slotSize + slotGap);
                var cell = rightSide
                    ? Anchored("Slot_" + slot, doll, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10 - slotSize, -top - slotSize), new Vector2(-10, -top))
                    : Anchored("Slot_" + slot, doll, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -top - slotSize), new Vector2(10 + slotSize, -top));
                var uid = item["uid"].Str();
                var slotName = Clean(player["slotNames"][slot].Str(slot));
                Button button;
                if (item.IsObject)
                    button = BagCell(cell, item, uid == bagSelected && uid != "", null, () => { bagSelected = uid; OpenBagScreen(); }, ItemSprite(item, EquipIcons[i]));
                else
                {
                    button = Slot(cell, UiPixelIcon(EquipIcons[i]), new Color32(90, 90, 90, 255), null, false, () => Toast(slotName + " đang trống."));
                    button.transform.Find("Icon")?.GetComponent<Image>()?.CrossFadeAlpha(.3f, 0, true);
                }
                var r = (RectTransform)button.transform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            }

            // ---- middle: filters + grid
            var list = player[bagTab];
            var middle = Anchored("Middle", area, new Vector2(.27f, 0), new Vector2(.7f, 1), new Vector2(4, 0), new Vector2(-4, 0));
            var counts = new int[BagFilterKeys.Length];
            foreach (var item in list.Items)
            {
                counts[0]++;
                var index = Array.IndexOf(BagFilterKeys, BagCategory(item));
                if (index > 0) counts[index]++;
            }
            var chips = Anchored("Filters", middle, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -52), Vector2.zero);
            for (var i = 0; i < BagFilterKeys.Length; i++)
            {
                var key = BagFilterKeys[i];
                var on = key == bagFilter;
                var chip = Anchored("Filter_" + key, chips, new Vector2(i / (float)BagFilterKeys.Length, 0), new Vector2((i + 1) / (float)BagFilterKeys.Length, 1), new Vector2(3, 2), new Vector2(-3, -2));
                var chipFill = chip.gameObject.AddComponent<Image>();
                ModernUi.Fill(chipFill, 16f);
                if (on) UiGradient.Apply(chipFill, AuthGoldTop, AuthGoldBottom); else chipFill.color = new Color32(255, 255, 255, 16);
                var chipLabel = AnchoredText(chip, "Text", BagFilterNames[i] + " " + counts[i], ModernUi.SemiBold, 19, on ? AuthInkOnGold : AuthTextSecondary, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));
                chipLabel.resizeTextForBestFit = true; chipLabel.resizeTextMinSize = 13; chipLabel.resizeTextMaxSize = 19;
                var chipButton = chip.gameObject.AddComponent<Button>();
                chipButton.targetGraphic = chipFill;
                chipButton.transition = Selectable.Transition.None;
                if (!on) chipButton.onClick.AddListener(() => { bagFilter = key; OpenBagScreen(); });
            }
            var gridArea = Anchored("GridArea", middle, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -60));
            GlassPanel(gridArea, 22f, new Color32(10, 14, 18, 190), new Color32(6, 9, 12, 200));
            var gridInner = Anchored("Inner", gridArea, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            var grid = ScrollGrid(gridInner, 108f, 8f);
            J selected = J.Null;
            var shown = 0;
            foreach (var item in list.Items)
            {
                var uid = item["uid"].Str();
                if (uid == bagSelected) selected = item;
                if (bagFilter != "all" && BagCategory(item) != bagFilter) continue;
                var it = item;
                shown++;
                BagCell(grid, item, uid == bagSelected, IsQuick(player, uid) ? "Nhanh" : null, () => { bagSelected = it["uid"].Str(); OpenBagScreen(); });
            }
            // empty cells show how much room is left
            var safeStorage = player["safeStorage"];
            var capacity = bagTab == "bag" ? player["bagSize"].Int(30) : bagTab == "kho" ? player["khoSize"].Int(80) : safeStorage["active"].Bool() ? safeStorage["slots"].Int() : 0;
            if (bagFilter == "all")
                for (var i = list.Count; i < Mathf.Min(capacity, list.Count + 60); i++) BagEmptyCell(grid);
            if (shown == 0 && (bagFilter != "all" || capacity == 0))
                EmptyState(grid, "hanh_trang", bagTab == "safe" && !safeStorage["active"].Bool() ? "Két an toàn chưa thuê." : "Không có vật phẩm loại này.");

            // ---- right: the selected item
            var detailArea = Anchored("DetailPane", area, new Vector2(.7f, 0), Vector2.one, new Vector2(8, 0), Vector2.zero);
            if (selected.IsNull) foreach (var slot in EquipOrder) if (eq[slot]["uid"].Str() == bagSelected && !string.IsNullOrEmpty(bagSelected)) selected = eq[slot];
            if (selected.IsNull)
            {
                var info = Detail(detailArea, UiPixelIcon("hanh_trang"), AuthGoldAccent, "Hành trang", null, "Chạm một vật phẩm để xem chi tiết.", 1);
                StatLine(info.Info, "Linh thạch", Vn(player["stones"]));
                StatLine(info.Info, "Thể lực", $"{player["stamina"].Int()}/{player["staminaMax"].Int()}");
                StatLine(info.Info, "Túi", $"{player["bag"].Count}/{player["bagSize"].Int(30)} ô");
                StatLine(info.Info, "Kho", $"{player["kho"].Count}/{player["khoSize"].Int(80)} ô");
                StatLine(info.Info, "Két an toàn", safeStorage["active"].Bool() ? $"{safeStorage["used"].Int()}/{safeStorage["slots"].Int()} ô" : "Chưa thuê");
                if (bagTab == "safe" && !safeStorage["active"].Bool())
                    DetailAction(info, $"Thuê két ({Vn(safeStorage["monthlyCost"])})", "ui:lock", true, () => Act("/safe-storage/rent", Body(), _ => OpenBagScreen()));
                return;
            }
            ItemDetail(detailArea, selected, player);
        }

        /// <summary>An inventory cell tinted by the item's quality: dark at the top, glowing in its colour toward the bottom.</summary>
        private Button BagCell(Transform grid, J item, bool selected, string badge, Action click, Sprite iconOverride = null)
        {
            var accent = RarityColor(item);
            var rank = RarityIndex(item);
            var cell = new GameObject("Item", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cell.SetParent(grid, false);
            var fill = cell.GetComponent<Image>();
            ModernUi.Fill(fill, 18f);
            UiGradient.Apply(fill, new Color(accent.r * .14f + .05f, accent.g * .14f + .05f, accent.b * .14f + .07f, .97f),
                new Color(accent.r * .46f + .02f, accent.g * .46f + .02f, accent.b * .46f + .02f, .97f));
            if (rank >= 2)
            {
                var glow = InkUi.Simple(cell, "Glow", InkUi.Glow, new Color(accent.r, accent.g, accent.b, .14f + .07f * rank), Vector2.zero);
                glow.rectTransform.anchorMin = new Vector2(.02f, .02f); glow.rectTransform.anchorMax = new Vector2(.98f, .98f);
                glow.rectTransform.offsetMin = glow.rectTransform.offsetMax = Vector2.zero;
            }
            var rim = Anchored("Rim", cell, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(rim, 18f, selected ? 4f : 2f);
            rim.color = selected ? (Color)new Color32(255, 226, 150, 255) : new Color(accent.r, accent.g, accent.b, .9f);
            rim.raycastTarget = false;
            var sprite = iconOverride ?? ItemSprite(item);
            if (sprite != null)
            {
                var image = Anchored("Icon", cell, Vector2.zero, Vector2.one, new Vector2(7, 7), new Vector2(-7, -7)).gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            var qty = item["qty"].Int(1);
            if (qty > 1)
            {
                var label = AnchoredText(cell, "Qty", qty.ToString(), ModernUi.Bold, 21, AuthTextPrimary, TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(4, 3), new Vector2(-9, -4));
                var shadow = label.gameObject.AddComponent<Outline>();
                shadow.effectColor = new Color(0, 0, 0, .9f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
            if (item.Has("dur") && item["dur"].Int(100) < 40)
            {
                var worn = Anchored("Worn", cell, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(-8, -8)).gameObject.AddComponent<Image>();
                worn.sprite = InkUi.Glow;
                worn.color = new Color32(255, 96, 80, 255);
                worn.raycastTarget = false;
            }
            if (!string.IsNullOrEmpty(badge))
            {
                var tag = Anchored("Badge", cell, new Vector2(0, 1), new Vector2(0, 1), new Vector2(5, -30), new Vector2(5 + 22 + badge.Length * 12, -5));
                var tagFill = tag.gameObject.AddComponent<Image>();
                ModernUi.Fill(tagFill, 10f);
                UiGradient.Apply(tagFill, AuthGoldTop, AuthGoldBottom);
                tagFill.raycastTarget = false;
                AnchoredText(tag, "Text", badge, ModernUi.Bold, 16, AuthInkOnGold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            if (click != null) button.onClick.AddListener(() => click());
            cell.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        private void BagEmptyCell(Transform grid)
        {
            var cell = new GameObject("Empty", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cell.SetParent(grid, false);
            var fill = cell.GetComponent<Image>();
            ModernUi.Fill(fill, 18f);
            fill.color = new Color32(255, 255, 255, 9);
            fill.raycastTarget = false;
            var rim = Anchored("Rim", cell, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(rim, 18f, 1.5f);
            rim.color = new Color32(255, 255, 255, 22);
            rim.raycastTarget = false;
        }

        /// <summary>The equipped item an unequipped piece would replace (for the stat comparison).</summary>
        private static J EquippedCounterpart(J player, J item)
        {
            if (item["kind"].Str() != "equip" || item["place"].Str() == "equip") return J.Null;
            var eq = player["equip"];
            switch (item["slot"].Str())
            {
                case "weapon": return eq["weapon"];
                case "armor": return eq["armor"];
                case "acc": return eq["acc1"].IsObject ? eq["acc1"] : eq["acc2"];
                case "vong": return eq["ring1"].IsObject ? eq["ring1"] : eq["ring2"];
                case "phi_kiem": return eq["phiKiem"];
                case "lo_dinh": return eq["loDinh"];
                case "nhan_tru_do": return eq["nhanTruDo"];
                default: return J.Null;
            }
        }

        private Sprite SkillIcon(J skill)
        {
            try
            {
                var frames = PixelSkillArt.Frames(skill["id"].Str(), skill["name"].Str(), skill["kind"].Str(), hub["player"]["ascended"].Bool());
                if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
            }
            catch (Exception) { }
            return UiPixelIcon("cong_phap");
        }

        private static bool IsQuick(J player, string uid)
        {
            foreach (var q in player["quick"].Items) if (q.Str() == uid && !string.IsNullOrEmpty(uid)) return true;
            return false;
        }

        private void ItemDetail(RectTransform area, J item, J player)
        {
            var kind = item["kind"].Str();
            var place = item["place"].Str();
            var chip = Clean(item["qualityName"].Str(item["tierName"].Str()));
            var sub = Clean(item["realmMinName"].Str());
            var panel = Detail(area, ItemSprite(item), RarityColor(item), Clean(item["name"].Str()) + (item["qty"].Int(1) > 1 ? $" ×{item["qty"].Int()}" : ""), chip,
                string.IsNullOrEmpty(sub) ? null : "Yêu cầu " + sub, 3);
            if (!string.IsNullOrEmpty(item["desc"].Str())) Paragraph(panel.Info, item["desc"].Str());
            var worn = EquippedCounterpart(player, item);
            if (worn.IsObject) Paragraph(panel.Info, "So với đang mặc: " + Clean(worn["name"].Str()), 19, AuthTextTertiary);
            foreach (var stat in item["stats"].Pairs)
            {
                var value = stat.Value.Num();
                var text = value < 1 && value > 0 ? $"+{Math.Round(value * 100)}%" : "+" + Vn(stat.Value);
                var color = PositiveText;
                if (worn.IsObject)
                {
                    var delta = value - worn["stats"][stat.Key].Num();
                    if (Math.Abs(delta) > 1e-6)
                    {
                        var shown = value < 1 && value > 0 ? Math.Round(Math.Abs(delta) * 100) + "%" : Vn(Math.Abs(delta));
                        text += delta > 0 ? "  (+" + shown + ")" : "  (−" + shown + ")";
                        if (delta < 0) color = new Color32(255, 130, 110, 255);
                    }
                }
                StatLine(panel.Info, StatName(stat.Key), text, color);
            }
            if (worn.IsObject)
                foreach (var stat in worn["stats"].Pairs)
                    if (!item["stats"].Has(stat.Key))
                        StatLine(panel.Info, StatName(stat.Key), "mất " + (stat.Value.Num() < 1 && stat.Value.Num() > 0 ? Math.Round(stat.Value.Num() * 100) + "%" : Vn(stat.Value)), new Color32(255, 130, 110, 255));
            if (item.Has("dur")) StatLine(panel.Info, "Độ bền", item["dur"].Int() + "/100");
            if (item["refineAt"].Num() > ServerNow) StatLine(panel.Info, "Luyện hóa", "còn " + FormatDuration((item["refineAt"].Num() - ServerNow) / 1000));
            StatLine(panel.Info, "Giá bán", Vn(item["sell"]) + " linh thạch");
            var uid = item["uid"].Str();
            if (place == "equip")
            {
                DetailAction(panel, "Tháo ra", "ui:equipped", true, () => Act("/unequip", Body("slot", item["slot"].Str()), _ => OpenBagScreen()));
                if (item["dur"].Int(100) < 100) DetailAction(panel, "Sửa chữa", "ui:repair", false, () => Act("/repair", Body("uid", uid), _ => OpenBagScreen()));
                return;
            }
            if (kind == "equip") DetailAction(panel, "Trang bị", "ui:equipped", true, () => Act("/equip", Body("uid", uid), _ => { bagSelected = null; OpenBagScreen(); }));
            if (kind == "scroll" || kind == "manual" || item["id"].Str().StartsWith("cp_") || item.Has("skillId"))
                DetailAction(panel, "Lĩnh ngộ", "ui:cong_phap", true, () => Act("/learn", Body("uid", uid), _ => { bagSelected = null; OpenBagScreen(); }));
            if (kind == "cons" && item["usable"].Bool(true))
            {
                DetailAction(panel, "Sử dụng", "ui:dan_duoc", true, () => Act("/use", Body("uid", uid), _ => OpenBagScreen()));
                if (item["battle"].Bool())
                    DetailAction(panel, IsQuick(player, uid) ? "Bỏ ô nhanh" : "Đặt ô nhanh", "ui:quick_slot", false, () => SetQuick(player, uid));
            }
            if (kind == "fragment" || item.Has("fragmentOf"))
                DetailAction(panel, "Hợp thành", "ui:bat_quai", true, () => Act("/fragment/combine", Body("fragmentId", item["id"].Str()), _ => OpenBagScreen()));
            if (place == "bag") DetailAction(panel, "Cất vào kho", "ui:dong_phu", false, () => Act("/move", Body("uid", uid, "to", "kho"), _ => OpenBagScreen()));
            else DetailAction(panel, "Lấy ra túi", "ui:hanh_trang", false, () => Act("/move", Body("uid", uid, "to", "bag"), _ => OpenBagScreen()));
            if (place != "safe" && player["safeStorage"]["active"].Bool())
                DetailAction(panel, "Vào két an toàn", "ui:lock", false, () => Act("/move", Body("uid", uid, "to", "safe"), _ => OpenBagScreen()));
            if (!item["bound"].Bool())
            {
                var qty = item["qty"].Int(1);
                DetailAction(panel, "Bán", "ui:coin", false, () =>
                {
                    if (qty <= 1) Confirm("Bán vật phẩm", $"Bán {Clean(item["name"].Str())} lấy {Vn(item["sell"])} linh thạch?", "Bán", () => Act("/sell", Body("uid", uid, "qty", 1), _ => { bagSelected = null; OpenBagScreen(); }));
                    else PromptNumber("Bán vật phẩm", $"Số lượng {Clean(item["name"].Str())} muốn bán (giá {Vn(item["sell"])}/cái).", 1, qty, qty, "Bán",
                        n => Act("/sell", Body("uid", uid, "qty", n), _ => { bagSelected = null; OpenBagScreen(); }));
                });
            }
        }

        private void SetQuick(J player, string uid)
        {
            var quick = new List<string>();
            foreach (var q in player["quick"].Items) quick.Add(q.Str());
            var index = quick.IndexOf(uid);
            if (index >= 0) { Act("/quick", Body("i", index, "uid", null), _ => OpenBagScreen()); return; }
            index = quick.FindIndex(string.IsNullOrEmpty);
            if (index < 0) index = 0;
            Act("/quick", Body("i", index, "uid", uid), _ => OpenBagScreen());
        }

        private static string StatName(string key)
        {
            switch (key)
            {
                case "atk": return "Công kích";
                case "def": return "Phòng ngự";
                case "hp": case "maxHp": return "Khí huyết";
                case "mp": return "Linh lực";
                case "spd": return "Tốc độ";
                case "sense": return "Thần thức";
                case "crit": case "critRate": return "Bạo kích";
                case "critDmg": return "Sát thương bạo";
                case "accuracy": return "Chính xác";
                case "dodge": return "Né tránh";
                case "dmgReduction": return "Giảm sát thương";
                case "power": return "Chiến lực";
                default: return key;
            }
        }

        // ================================================================== Nhân vật

        private void OpenCharacterScreen()
        {
            var back = BackTarget();
            var player = hub["player"];
            var body = OpenScreen("Nhân Vật", Clean(player["fullName"].Str(player["name"].Str())), "ho_so", back);
            var (left, right) = Split(body, .42f);
            var portraitArea = Anchored("Portrait", left, new Vector2(0, .22f), Vector2.one, Vector2.zero, Vector2.zero);
            GlassPanel(portraitArea, 26f, new Color32(26, 30, 34, 220), new Color32(14, 18, 20, 220));
            if (AvatarComposer.Available)
            {
                var look = LookOf(player);
                var artPortrait = look.Int("preset", -1) >= 0;
                var avatar = artPortrait
                    ? AvatarComposer.BuildIllustration(portraitArea, look, AvatarComposer.AuraStrength(hub["realm"]["index"].Int()))
                    : AvatarComposer.Build(portraitArea, look, AvatarComposer.AuraStrength(hub["realm"]["index"].Int()));
                avatar.anchorMin = new Vector2(.08f, .03f); avatar.anchorMax = new Vector2(.92f, .97f);
                avatar.offsetMin = avatar.offsetMax = Vector2.zero;
            }
            else
            {
                var hero = Anchored("Hero", portraitArea, new Vector2(.3f, .1f), new Vector2(.7f, .9f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                hero.sprite = HeroFramesFor(player)[HeroSprites.WalkFrames];
                hero.preserveAspect = true;
                hero.raycastTarget = false;
            }
            var dichDungBtn = Anchored("DichDungBtn", portraitArea, new Vector2(.56f, .03f), new Vector2(.96f, .125f), Vector2.zero, Vector2.zero);
            PillButton(dichDungBtn, "Dịch dung", "ui:ho_so", false, () => ShowCreator(true));
            var actions = Anchored("Actions", left, Vector2.zero, new Vector2(1, .22f), Vector2.zero, new Vector2(0, -14));
            PillButton(Anchored("Break", actions, new Vector2(0, .5f), new Vector2(.5f, 1), new Vector2(0, 6), new Vector2(-6, 0)), "Đột phá", "ui:sense", true, () => Act("/breakthrough", Body(), _ => OpenCharacterScreen()));
            PillButton(Anchored("Ascend", actions, new Vector2(.5f, .5f), new Vector2(1, 1), new Vector2(6, 6), new Vector2(0, 0)), "Phi thăng", "ui:flight", false, () =>
                Confirm("Nghi thức Phi Thăng", "Đủ cảnh giới và Thiên Đạo Nguyên Ấn sẽ mở khóa Cổng Phi Thăng. Sau đó hãy tự đến cổng tại Man Châu để chuyển sang bản đồ Tiên Giới.", "Mở khóa cổng", () => Act("/ascend", Body(), _ => OpenCharacterScreen())));
            PillButton(Anchored("Account", actions, new Vector2(0, 0), new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(-6, -6)), "Tài khoản", "ui:ho_so", false, () => { if (latestState != null) ShowAccountLinks(latestState); });
            PillButton(Anchored("Logout", actions, new Vector2(.5f, 0), new Vector2(1, .5f), new Vector2(6, 0), new Vector2(0, -6)), offlinePreview ? "Thoát ra" : "Đăng xuất", "arrowLeft", false, () =>
                Confirm(offlinePreview ? "Rời thế giới" : "Đăng xuất", offlinePreview ? "Quay lại màn hình đăng nhập?" : "Thoát khỏi tài khoản trên thiết bị này?", offlinePreview ? "Thoát ra" : "Đăng xuất", () => { if (offlinePreview) ExitOfflineWorld(); else client.Logout(_ => ShowLogin()); }));
            var info = ScrollColumn(right, 6f, 14);
            var realm = hub["realm"];
            SectionLabel(info, "Cảnh giới");
            StatLine(info, Clean(realm["name"].Str()) + " · " + Clean(realm["sub"].Str()), $"{Vn(realm["experience"])} / {Vn(realm["levelCap"])} tu vi", AuthGoldAccent);
            var bt = hub["breakthrough"];
            if (bt["atBottleneck"].Bool()) Paragraph(info, "Đã chạm bình cảnh — có thể đột phá.", 22, PositiveText);
            SectionLabel(info, "Căn cơ");
            StatLine(info, "Môn phái", Clean(player["monName"].Str()));
            StatLine(info, "Ngũ hành", Clean(player["heName"].Str()));
            StatLine(info, "Linh căn", Clean(player["linhCan"].Str()));
            StatLine(info, "Đạo tâm / Ma tính", $"{player["daoScore"].Int()} / {player["maScore"].Int()}");
            SectionLabel(info, "Chiến đấu");
            foreach (var stat in player["detailedStats"].Pairs)
            {
                var v = stat.Value.Num();
                var text = stat.Key == "critRate" || stat.Key == "critDmg" || stat.Key == "dmgReduction" || stat.Key == "dodge" ? Vn(v) + "%" : Vn(v);
                StatLine(info, StatName(stat.Key), text);
            }
            SectionLabel(info, "Lôi đài");
            var pvp = player["pvp"];
            StatLine(info, "Điểm", Vn(pvp["points"]));
            StatLine(info, "Thắng / Bại", $"{pvp["wins"].Int()} / {pvp["losses"].Int()}");
            SectionLabel(info, "Danh hiệu");
            foreach (var title in player["titles"].Items)
                StatLine(info, Clean(title["name"].Str()), title["active"].Bool() ? "Đang có · " + Clean(title["buff"].Str()) : "Chưa đạt", title["active"].Bool() ? (Color?)PositiveText : AuthTextTertiary);
            if (player["beasts"].Count > 0)
            {
                SectionLabel(info, "Linh thú");
                foreach (var beast in player["beasts"].Items)
                {
                    var b = beast;
                    Row(info, MonsterSprite(b["monsterId"].Str(b["id"].Str())), AuthGoldAccent, Clean(b["name"].Str()), Clean(b["realmName"].Str()), null, null, false,
                        () => Act("/beast/mount", Body("beastId", b["id"].Str()), _ => OpenCharacterScreen()), 92f);
                }
            }
        }

        // ================================================================== Công pháp

        private void OpenSkillsScreen()
        {
            var back = BackTarget();
            var player = hub["player"];
            var body = OpenScreen("Công Pháp", "Gắn kỹ năng vào ô để dùng trong trận", "cong_phap", back);
            var (left, right) = Split(body, .58f);
            var slotsArea = Anchored("Slots", left, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -170), Vector2.zero);
            var slots = player["slots"];
            var realms = player["slotRealms"];
            var count = Mathf.Max(5, realms.Count);
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var skill = slots[i];
                var open = realms[i]["open"].Bool(i < 3);
                var cell = Anchored("Slot" + i, slotsArea, new Vector2(i / (float)count, 0), new Vector2((i + 1) / (float)count, 1), new Vector2(6, 34), new Vector2(-6, 0));
                var button = Slot(cell, skill.IsObject ? SkillIcon(skill) : UiPixelIcon(open ? "quick_slot" : "lock"),
                    skill.IsObject ? RarityColor(skill) : (Color)new Color32(80, 80, 80, 255), null, false, () =>
                    {
                        if (!open) { Toast("Mở ô ở " + Clean(realms[index]["name"].Str()) + ".", true); return; }
                        if (skill.IsObject) Confirm("Tháo kỹ năng", "Tháo " + Clean(skill["name"].Str()) + " khỏi ô " + (index + 1) + "?", "Tháo", () => Act("/skill-slot", Body("i", index, "id", null), _ => OpenSkillsScreen()));
                        else if (!string.IsNullOrEmpty(skillSelected)) Act("/skill-slot", Body("i", index, "id", skillSelected), _ => OpenSkillsScreen());
                        else Toast("Chọn một công pháp bên dưới rồi chạm ô trống.");
                    });
                var r = (RectTransform)button.transform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                AnchoredText(cell, "Label", skill.IsObject ? Clean(skill["name"].Str()) : (open ? "Ô " + (i + 1) : Clean(realms[i]["name"].Str())), ModernUi.Regular, 15, AuthTextTertiary,
                    TextAnchor.UpperCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-10, -30), new Vector2(10, -2));
            }
            var listArea = Anchored("List", left, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -180));
            var list = ScrollColumn(listArea, 8f, 6);
            J selected = J.Null;
            foreach (var skill in player["skills"].Items)
            {
                var s = skill;
                var id = s["id"].Str();
                if (id == skillSelected) selected = s;
                Row(list, SkillIcon(s), RarityColor(s), Clean(s["name"].Str()),
                    $"{Clean(s["rarityName"].Str())} · hồi {s["cd"].Int()}s · {s["mp"].Int()} linh lực", s["locked"].Bool() ? "Chưa đủ" : null, null, id == skillSelected,
                    () => { skillSelected = id; OpenSkillsScreen(); }, 96f, s["locked"].Bool());
            }
            if (player["skills"].Count == 0) EmptyState(list, "cong_phap", "Chưa lĩnh ngộ công pháp nào.");
            if (selected.IsNull)
            {
                var d = Detail(right, UiPixelIcon("cong_phap"), AuthGoldAccent, "Công pháp", null, "Chọn công pháp rồi chạm ô kỹ năng để gắn.", 0);
                Paragraph(d.Info, "Ngọc giản công pháp rơi từ yêu thú và bí cảnh. Dùng ngọc giản trong Hành trang để lĩnh ngộ.");
                return;
            }
            var panel = Detail(right, SkillIcon(selected), RarityColor(selected), Clean(selected["name"].Str()),
                Clean(selected["rarityName"].Str()), Clean(selected["monName"].Str()), 1);
            Paragraph(panel.Info, selected["desc"].Str());
            StatLine(panel.Info, "Hồi chiêu", selected["cd"].Int() + " giây");
            StatLine(panel.Info, "Linh lực", selected["mp"].Int().ToString());
            StatLine(panel.Info, "Cảnh giới", Clean(selected["realmMinName"].Str()));
            if (selected["reqDao"].Int() > 0) StatLine(panel.Info, "Đạo tâm", selected["reqDao"].Int().ToString());
            if (selected["reqMa"].Int() > 0) StatLine(panel.Info, "Ma tính", selected["reqMa"].Int().ToString());
            var firstFree = -1;
            for (var i = 0; i < count; i++) if (!slots[i].IsObject && realms[i]["open"].Bool(i < 3)) { firstFree = i; break; }
            DetailAction(panel, firstFree >= 0 ? "Gắn vào ô " + (firstFree + 1) : "Hết ô trống", "ui:quick_slot", true,
                () => Act("/skill-slot", Body("i", firstFree, "id", selected["id"].Str()), _ => OpenSkillsScreen()), firstFree >= 0 && !selected["locked"].Bool());
        }

        // ================================================================== Tàng Bảo Các (shop)

        private void OpenShopScreen()
        {
            var back = BackTarget();
            var body = OpenScreen("Tàng Bảo Các", "Cửa hàng của thành · làm mới mỗi ngày", "coin", back);
            var list = ScrollColumn(body, 8f, 6);
            var stones = hub["player"]["stones"].Num();
            foreach (var item in hub["shop"].Items)
            {
                var it = item;
                var price = it["price"].Num();
                Row(list, ItemSprite(it), RarityColor(it), Clean(it["name"].Str()), Clean(it["desc"].Str()), Vn(price) + " LT", it["stock"].IsNull ? null : "Còn " + it["stock"].Int(), false, () =>
                {
                    var max = (long)Math.Max(1, Math.Min(99, Math.Floor(stones / Math.Max(1, price))));
                    if (max < 1 || stones < price) { Toast("Không đủ linh thạch.", true); return; }
                    PromptNumber("Mua " + Clean(it["name"].Str()), $"Giá {Vn(price)} linh thạch mỗi cái. Bạn có {Vn(stones)}.", 1, max, 1, "Mua",
                        n => Act("/buy", Body("id", it["id"].Str(), "qty", n), _ => OpenShopScreen()));
                }, 104f, stones < price);
            }
            if (hub["shop"].Count == 0) EmptyState(list, "coin", "Hôm nay cửa hàng chưa nhập hàng.");
        }

        // ================================================================== Phường thị (market)

        private void OpenMarketScreen()
        {
            var back = BackTarget();
            Fetch("/market", market =>
            {
                var body = OpenScreen("Phường Thị", $"Chợ giữa các đạo hữu · thuế {Math.Round(market["tax"].Num() * 100)}%", "phuong_thi", back);
                var (listArea, side) = Split(body, .62f);
                var list = ScrollColumn(listArea, 8f, 6);
                SectionLabel(list, "Hàng đang bán");
                foreach (var listing in market["listings"].Items)
                {
                    var l = listing;
                    var item = l["item"];
                    var mine = l["mine"].Bool();
                    Row(list, ItemSprite(item), RarityColor(item), Clean(item["name"].Str()) + (item["qty"].Int(1) > 1 ? " ×" + item["qty"].Int() : ""),
                        (mine ? "Của bạn" : Clean(l["sellerName"].Str())) + " · " + Clean(l["townName"].Str()), Vn(l["price"]) + " LT", null, false, () =>
                        {
                            if (mine) Confirm("Hủy đăng bán", "Thu hồi " + Clean(item["name"].Str()) + "?", "Thu hồi", () => Act("/market/cancel", Body("id", l["id"].Str()), _ => OpenMarketScreen()));
                            else Confirm("Mua hàng", $"Mua {Clean(item["name"].Str())} giá {Vn(l["price"])} linh thạch?", "Mua", () => Act("/market/buy", Body("id", l["id"].Str()), _ => OpenMarketScreen()));
                        }, 100f);
                }
                if (market["listings"].Count == 0) EmptyState(list, "phuong_thi", "Chưa có ai bày bán.");
                var panel = Detail(side, UiPixelIcon("phuong_thi"), AuthGoldAccent, "Bày bán", null, "Đăng vật phẩm từ hành trang lên chợ.", 1);
                foreach (var rare in market["rareShopSchedule"].Items)
                {
                    StatLine(panel.Info, Clean(rare["itemName"].Str()), Vn(rare["price"]) + " · " + Clean(rare["townName"].Str()));
                }
                DetailAction(panel, "Đăng bán", "ui:coin", true, () =>
                {
                    var options = new List<PickOption>();
                    foreach (var item in hub["player"]["bag"].Items)
                    {
                        if (item["bound"].Bool()) continue;
                        var it = item;
                        options.Add(new PickOption
                        {
                            Label = Clean(it["name"].Str()), Sub = "Bán NPC: " + Vn(it["sell"]), Icon = ItemSprite(it), Accent = RarityColor(it),
                            Right = it["qty"].Int(1) > 1 ? "×" + it["qty"].Int() : null,
                            Choose = () => PromptNumber("Giá bán", "Đặt giá (linh thạch) cho " + Clean(it["name"].Str()) + ".", 1, 999999999, Math.Max(1, it["sell"].Long() * 2), "Đăng",
                                price => Act("/market/list", Body("uid", it["uid"].Str(), "price", price, "qty", it["qty"].Int(1)), _ => OpenMarketScreen())),
                        });
                    }
                    PickOne("Chọn vật phẩm", options, "Không có vật phẩm có thể giao dịch.");
                });
            });
        }

        // ================================================================== Y Quán

        private void OpenHealScreen()
        {
            var back = BackTarget();
            var player = hub["player"];
            var town = hub["town"];
            var body = OpenScreen("Y Quán", Clean(town["name"].Str()), "y_quan", back);
            var panel = Detail(body, UiPixelIcon("y_quan"), new Color32(120, 200, 150, 255), "Chữa thương", null, "Hồi phục toàn bộ khí huyết", 1);
            StatLine(panel.Info, "Khí huyết", $"{Vn(player["hp"])} / {Vn(player["maxHp"])}");
            var injured = player["injuredUntil"].Num() - ServerNow;
            if (injured > 0) StatLine(panel.Info, "Trọng thương", "còn " + FormatDuration(injured / 1000), AuthError);
            StatLine(panel.Info, "Chi phí", Vn(town["healingCost"]) + " linh thạch");
            Paragraph(panel.Info, "Thầy thuốc Y Quán dùng linh dược trị thương tức thì. Đan dược hồi phục cũng bán ở Tàng Bảo Các.");
            DetailAction(panel, "Chữa thương", "ui:heart", true, () => Act("/town/heal", Body(), _ => OpenHealScreen()), player["hp"].Num() < player["maxHp"].Num() || injured > 0);
        }

        // ================================================================== Truyền Tống Trận

        private void OpenTeleportScreen(bool fromWorld)
        {
            var back = BackTarget();
            var body = OpenScreen("Truyền Tống Trận", "Dịch chuyển tức thì giữa các thành trấn (tốn linh thạch)", "teleport", back);
            var maps = new List<J>();
            foreach (var m in hub["allMaps"].Items) maps.Add(m);
            var labels = new[] { "Phàm Giới", "Tiên Giới" };
            var area = Tabs(body, labels, teleportTab, i => { teleportTab = i; OpenTeleportScreen(fromWorld); });
            var list = ScrollColumn(area, 6f, 6);
            var here = hub["town"]["id"].Str();
            var realmIndex = hub["realm"]["index"].Int();
            foreach (var map in maps)
            {
                var immortal = map["ascensionRequired"].Bool(map["id"].Str().Length >= 5 && int.TryParse(map["id"].Str().Substring(4), out var n) && n >= 9);
                if ((teleportTab == 1) != immortal) continue;
                SectionLabel(list, Clean(map["provinceName"].Str(map["name"].Str())) + " · " + Clean(map["realmMinName"].Str()));
                foreach (var town in hub["allTowns"].Items)
                {
                    if (town["mapId"].Str() != map["id"].Str()) continue;
                    var t = town;
                    var current = t["id"].Str() == here;
                    var targetMapId = t["mapId"].Str();
                    var immortalTarget = targetMapId.StartsWith("map_") && int.TryParse(targetMapId.Substring(4), out var targetMapNumber) && targetMapNumber >= 9;
                    var locked = realmIndex < t["realmMin"].Int() || (immortalTarget && !hub["player"]["ascended"].Bool());
                    var lockReason = immortalTarget && !hub["player"]["ascended"].Bool()
                        ? "Cần Phi Thăng" : locked ? "Cần " + Clean(t["realmMinName"].Str()) : null;
                    Row(list, UiPixelIcon(current ? "location" : "teleport"), current ? AuthGoldAccent : (Color)new Color32(140, 170, 220, 255), Clean(t["name"].Str()),
                        Clean(t["desc"].Str()), current ? "Đang ở đây" : Vn(t["teleportCost"]) + " LT", lockReason, current, () =>
                        {
                            if (current) { Toast("Đạo hữu đang ở " + Clean(t["name"].Str()) + "."); return; }
                            Confirm("Truyền tống", $"Truyền tống tới {Clean(t["name"].Str())} với giá {Vn(t["teleportCost"])} linh thạch?", "Truyền tống",
                                () => TeleportToTown(t["id"].Str()));
                        }, 96f, locked);
                }
            }
        }

        private void TeleportToTown(string targetTownId)
        {
            J target = J.Null;
            foreach (var town in hub["allTowns"].Items)
                if (town["id"].Str() == targetTownId) { target = town; break; }
            if (!target.IsObject) { Toast("Không tìm thấy thành trấn đích.", true); return; }
            if (targetTownId == hub["town"]["id"].Str()) { Toast("Đạo hữu đang ở thành này."); return; }

            var targetMapId = target["mapId"].Str();
            var currentMapId = hub["town"]["mapId"].Str();
            var currentImmortal = TryProvinceNumber(currentMapId, out var currentMapNumber) && currentMapNumber >= 9;
            var targetImmortal = TryProvinceNumber(targetMapId, out var targetMapNumber) && targetMapNumber >= 9;
            if (currentImmortal != targetImmortal)
            {
                Toast("Chỉ có thể đổi giữa Phàm Giới và Tiên Giới tại Cổng Phi Thăng riêng.", true);
                return;
            }
            if (WorldMapData.Load(targetMapId) == null || Resources.Load<TextAsset>("World/" + targetMapId + "_map") == null)
            {
                Toast("Bản đồ đích chưa có trong bản cài.", true);
                return;
            }
            var realm = offlinePreview ? offlineProgress.realmIndex : hub["realm"]["index"].Int();
            if (realm < target["realmMin"].Int())
            {
                Toast("Cần đạt " + Clean(target["realmMinName"].Str()) + " để đến đây.", true);
                return;
            }
            var immortal = targetMapId.StartsWith("map_") && int.TryParse(targetMapId.Substring(4), out var mapNumber) && mapNumber >= 9;
            if (immortal && !(hub["player"]["ascended"].Bool() || (offlinePreview && realm >= 11)))
            {
                Toast("Cần hoàn thành Phi Thăng trước khi đến Tiên Giới.", true);
                return;
            }
            if (offlinePreview)
            {
                var cost = Mathf.Max(0, target["teleportCost"].Int());
                if (offlineProgress.stones < cost)
                {
                    Toast("Không đủ linh thạch để truyền tống.", true);
                    return;
                }
                offlineProgress.stones -= cost;
                offlineProgress.currentTownId = targetTownId;
                hub.Set("town", target.Raw);
                hub["player"].Set("stones", offlineProgress.stones);
                if (offlinePreviewState != null)
                {
                    foreach (var town in offlinePreviewState.allTowns ?? Array.Empty<TownInfo>())
                        if (town != null && town.id == targetTownId) { offlinePreviewState.town = town; break; }
                    offlinePreviewState.player.stones = offlineProgress.stones;
                }
                SaveOfflineProgress();
                FinishWorldTeleport();
                ShowWorld();
                Toast("Đã truyền tống đến " + Clean(target["name"].Str()) + ".");
                return;
            }
            Act("/market/teleport", Body("toTownId", targetTownId), _ =>
            {
                FinishWorldTeleport();
                ShowWorld();
                Toast("Đã truyền tống đến " + Clean(target["name"].Str()) + ".");
            });
        }

        private void FinishWorldTeleport()
        {
            cityTownId = null;
            worldReturnTile = null;
            hub["player"].Remove("worldPosition");
            if (latestState?.player != null) latestState.player.worldPosition = null;
            PlayerPrefs.DeleteKey("tt_offline_world_x");
            PlayerPrefs.DeleteKey("tt_offline_world_y");
            PlayerPrefs.DeleteKey("tt_offline_world_mapId");
            PlayerPrefs.Save();
        }

        // ================================================================== Nhiệm Vụ Đường (bounties)

        private void OpenBountyScreen()
        {
            var back = BackTarget();
            Fetch("/town/bounties", data =>
            {
                var board = data["board"];
                var body = OpenScreen("Nhiệm Vụ Đường", $"{Clean(board["townName"].Str())} · {board["availableCount"].Int()} nhiệm vụ mở · đang nhận {board["myActiveCount"].Int()}", "scroll", back);
                var list = ScrollColumn(body, 8f, 6);
                foreach (var bounty in board["bounties"].Items)
                {
                    var b = bounty;
                    var status = b["status"].Str();
                    var progress = $"{b["currentCount"].Int()}/{b["targetCount"].Int()}";
                    var reward = $"{Vn(b["rewardStones"])} LT · {Vn(b["rewardExp"])} tu vi";
                    var label = status == "open" ? "Nhận" : status == "done" || status == "completed" ? "Đã xong" : b["currentCount"].Int() >= b["targetCount"].Int() ? "Trả" : progress;
                    Row(list, UiPixelIcon("scroll"), status == "open" ? AuthGoldAccent : (Color)PositiveText, Clean(b["title"].Str()), Clean(b["desc"].Str()) + " · " + reward, label, null, false, () =>
                    {
                        if (status == "open") Act("/town/bounty/accept", Body("bountyId", b["id"].Str()), _ => OpenBountyScreen());
                        else if (b["currentCount"].Int() >= b["targetCount"].Int() && status != "done" && status != "completed")
                            Act("/town/bounty/claim", Body("bountyId", b["id"].Str()), _ => OpenBountyScreen());
                        else Toast("Tiến độ " + progress + ".");
                    }, 108f, status == "done" || status == "completed");
                }
                if (board["bounties"].Count == 0) EmptyState(list, "scroll", "Bảng treo thưởng đang trống.");
            });
        }

        // ================================================================== Bảng xếp hạng

        private void OpenRankScreen()
        {
            var back = BackTarget();
            var tabs = new[] { "Tu vi", "Lôi đài", "Bổn thành", "Phong Thần", "Ma đạo", "Tiên bảng" };
            var routes = new[] { "/leaderboard", "/pvp/leaderboard", "/pvp/town-rank", "/pvp/dai-phong-than", "/pvp/demon-rank", "/pvp/immortal-rank" };
            Fetch(routes[rankTab], data =>
            {
                var body = OpenScreen("Thiên Kiêu Bảng", "Xếp hạng tu sĩ thiên hạ", "power", back);
                var area = Tabs(body, tabs, rankTab, i => { rankTab = i; OpenRankScreen(); });
                var list = ScrollColumn(area, 6f, 6);
                var rank = 0;
                foreach (var row in data["list"].Items)
                {
                    rank++;
                    var accent = rank == 1 ? (Color)new Color32(241, 209, 90, 255) : rank == 2 ? (Color)new Color32(200, 210, 220, 255) : rank == 3 ? (Color)new Color32(214, 150, 96, 255) : AuthTextTertiary;
                    var right = rankTab == 0 ? Vn(row["power"]) : Vn(row["points"]) + " điểm";
                    var sub = Clean(row["realmName"].Str(row["realm"].Str())) + (row.Has("sectName") ? " · " + Clean(row["sectName"].Str()) : "") +
                              (row.Has("wins") ? $" · {row["wins"].Int()}T/{row["losses"].Int()}B" : "");
                    Row(list, null, accent, $"{rank}.  {Clean(row["name"].Str())}", sub, right, rankTab == 0 ? "chiến lực" : null, row["userId"].Str() == hub["player"]["userId"].Str(), null, 88f);
                }
                if (data["list"].Count == 0) EmptyState(list, "power", "Chưa có ai trên bảng này.");
            });
        }

        // ================================================================== Hòm thư

        private void OpenInboxScreen()
        {
            var back = BackTarget();
            Fetch("/inbox", data =>
            {
                var body = OpenScreen("Dịch Trạm", "Hòm thư và phần thưởng", "mail", back);
                var list = ScrollColumn(body, 8f, 6);
                foreach (var mail in data["list"].Items)
                {
                    var m = mail;
                    var rewards = m["hasRewards"].Bool() && !m["claimed"].Bool();
                    Row(list, UiPixelIcon("mail"), m["read"].Bool() ? AuthTextTertiary : AuthGoldAccent, Clean(m["title"].Str()), Clean(m["sender"].Str()) + " · " + Clean(m["content"].Str()),
                        rewards ? "Nhận" : (m["claimed"].Bool() ? "Đã nhận" : null), null, false, () =>
                        {
                            var card = Modal(Clean(m["title"].Str()), 980f, 640f, out var close);
                            var text = Clean(m["content"].Str());
                            if (m["stones"].Num() > 0) text += $"\n\n+{Vn(m["stones"])} linh thạch";
                            if (m["exp"].Num() > 0) text += $"\n+{Vn(m["exp"])} tu vi";
                            foreach (var item in m["items"].Items) text += $"\n+ {Clean(item["name"].Str(item["id"].Str()))} ×{item["qty"].Int(1)}";
                            AnchoredText(card, "Body", text, ModernUi.Regular, 24, AuthTextSecondary, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(34, 130), new Vector2(-34, -100));
                            PillButton(Anchored("Delete", card, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(28, 24), new Vector2(-8, 104)), "Xóa thư", "alert", false,
                                () => { close(); Act("/inbox/delete", Body("mailId", m["id"].Str()), _ => OpenInboxScreen()); });
                            PillButton(Anchored("Claim", card, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 24), new Vector2(-28, 104)), rewards ? "Nhận thưởng" : "Đã đọc", "arrowRight", true,
                                () => { close(); Act(rewards ? "/inbox/claim" : "/inbox/read", Body("mailId", m["id"].Str()), _ => OpenInboxScreen()); });
                        }, 100f);
                }
                if (data["list"].Count == 0) EmptyState(list, "mail", "Hòm thư trống.");
            });
        }

        // ================================================================== Sự kiện

        private void OpenEventsScreen()
        {
            var back = BackTarget();
            Fetch("/world/events", world =>
            {
                client.Get("/personal/events", (personal, _) =>
                {
                    var body = OpenScreen("Sự Kiện", "Biến cố thiên hạ và nhân quả của bản thân", "sun", back);
                    var (left, right) = Split(body, .55f);
                    var list = ScrollColumn(left, 8f, 6);
                    SectionLabel(list, "Thiên hạ");
                    foreach (var ev in world["list"].Items)
                    {
                        var e = ev;
                        Row(list, UiPixelIcon("sun"), e["isCurrentTown"].Bool() ? AuthGoldAccent : AuthTextTertiary, Clean(e["name"].Str()), Clean(e["townName"].Str()) + " · " + Clean(e["desc"].Str()),
                            "còn " + FormatDuration(e["timeLeftSec"].Num()), null, false, () =>
                            {
                                if (!e["canExplore"].Bool()) { Toast("Hãy tới " + Clean(e["townName"].Str()) + " để thám hiểm.", true); return; }
                                Act("/world/event/explore", Body("eventId", e["id"].Str()), _ => OpenEventsScreen());
                            }, 104f);
                    }
                    if (world["list"].Count == 0) EmptyState(list, "sun", "Thiên hạ thái bình.");
                    var log = ScrollColumn(right, 4f, 10);
                    SectionLabel(log, "Nhân quả gần đây");
                    foreach (var item in personal["list"].Items) Paragraph(log, "• " + Clean(item["text"].Str()), 21);
                    if (personal["unreadCount"].Int() > 0) client.Post("/personal/events/read", Body(), (_, __) => { });
                });
            });
        }

        // ================================================================== Luyện chế

        private void OpenCraftScreen()
        {
            var back = BackTarget();
            Fetch("/crafting", data =>
            {
                var body = OpenScreen("Luyện Chế Thất", "Luyện khí · Luyện đan · Chế phù", "dan_duoc", back);
                var tabs = new[] { "Luyện khí", "Luyện đan", "Chế phù" };
                var keys = new[] { "equipRecipes", "potionRecipes", "talismanRecipes" };
                var routes = new[] { "/craft/equip", "/craft/potion", "/craft/talisman" };
                var area = Tabs(body, tabs, craftTab, i => { craftTab = i; craftSelected = null; OpenCraftScreen(); });
                var (left, right) = Split(area, .56f);
                var list = ScrollColumn(left, 6f, 6);
                var owned = new Dictionary<string, int>();
                foreach (var mat in data["materials"].Items) owned[mat["id"].Str()] = mat["count"].Int();
                J selected = J.Null;
                var shown = 0;
                foreach (var recipe in data[keys[craftTab]].Items)
                {
                    if (shown++ > 120) break;
                    var r = recipe;
                    var id = r["id"].Str();
                    if (id == craftSelected) selected = r;
                    var name = Clean(r["equipName"].Str(r["consName"].Str(r["name"].Str())));
                    var ready = HasMaterials(r, owned);
                    Row(list, ItemSprite(MakeItemRef(r)), ready ? PositiveText : AuthTextTertiary,
                        name, Clean(r["tierName"].Str(r["equipSlotName"].Str(r["craftGroup"].Str()))), Vn(r["stones"]) + " LT", null, id == craftSelected, () => { craftSelected = id; OpenCraftScreen(); }, 90f, !ready);
                }
                if (selected.IsNull)
                {
                    var hint = Detail(right, UiPixelIcon("dan_duoc"), AuthGoldAccent, "Luyện chế", null, "Chọn công thức để xem nguyên liệu.", 0);
                    StatLine(hint.Info, "Linh thạch", Vn(data["stones"]));
                    StatLine(hint.Info, "Chế phù", Clean(data["scribeProgress"]["title"].Str()));
                    return;
                }
                var panel = Detail(right, ItemSprite(MakeItemRef(selected)), AuthGoldAccent, Clean(selected["equipName"].Str(selected["consName"].Str(selected["name"].Str()))), null,
                    Clean(selected["equipDesc"].Str(selected["consDesc"].Str(selected["desc"].Str()))), 1);
                foreach (var mat in selected["materials"].Pairs)
                {
                    owned.TryGetValue(mat.Key, out var have);
                    var need = mat.Value.Int();
                    StatLine(panel.Info, MaterialName(data, mat.Key), $"{have}/{need}", have >= need ? (Color?)PositiveText : AuthError);
                }
                foreach (var mat in selected["materials"].Items)
                {
                    var id = mat["id"].Str();
                    owned.TryGetValue(id, out var have);
                    StatLine(panel.Info, MaterialName(data, id), $"{have}/{mat["qty"].Int(1)}", have >= mat["qty"].Int(1) ? (Color?)PositiveText : AuthError);
                }
                StatLine(panel.Info, "Linh thạch", Vn(selected["stones"]));
                if (selected.Has("successRate")) StatLine(panel.Info, "Tỉ lệ thành công", Math.Round(selected["successRate"].Num() * 100) + "%");
                DetailAction(panel, "Luyện chế", "ui:fire", true, () => Act(routes[craftTab], Body("recipeId", selected["id"].Str()), _ => OpenCraftScreen()), HasMaterials(selected, owned));
            });
        }

        private static J MakeItemRef(J recipe)
        {
            var id = recipe["equipId"].Str(recipe["consId"].Str(recipe["talismanId"].Str()));
            return new J(new Dictionary<string, object> { { "id", id } });
        }

        private static bool HasMaterials(J recipe, Dictionary<string, int> owned)
        {
            var mats = recipe["materials"];
            foreach (var pair in mats.Pairs) { owned.TryGetValue(pair.Key, out var have); if (have < pair.Value.Int()) return false; }
            foreach (var mat in mats.Items) { owned.TryGetValue(mat["id"].Str(), out var have); if (have < mat["qty"].Int(1)) return false; }
            return true;
        }

        private static string MaterialName(J data, string id)
        {
            foreach (var mat in data["materials"].Items) if (mat["id"].Str() == id) return Clean(mat["name"].Str());
            return id;
        }

        // ================================================================== Tàng Kinh Các

        private void OpenCodexScreen()
        {
            var back = BackTarget();
            Fetch("/codex", data =>
            {
                var body = OpenScreen("Tàng Kinh Các", "Điển tịch: công pháp, vật phẩm, yêu thú", "thu_cac", back);
                var list = ScrollColumn(body, 6f, 6);
                foreach (var section in data.Pairs)
                {
                    if (!section.Value.IsArray || section.Value.Count == 0) continue;
                    SectionLabel(list, CodexSection(section.Key) + " · " + section.Value.Count);
                    var shown = 0;
                    foreach (var entry in section.Value.Items)
                    {
                        if (shown++ >= 40) break;
                        var e = entry;
                        var icon = section.Key.Contains("onster") ? MonsterSprite(e["id"].Str()) : section.Key.Contains("kill") ? SkillIcon(e) : ItemSprite(e);
                        Row(list, icon, RarityColor(e), Clean(e["name"].Str()), Clean(e["desc"].Str(e["realmName"].Str())), Clean(e["realmMinName"].Str(e["tierName"].Str())), null, false, null, 88f);
                    }
                }
                Paragraph(list, "Tra nguồn rơi của vật phẩm bằng nút bên dưới.");
                var search = Anchored("Search", body, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-420, 10), new Vector2(-10, 96));
                PillButton(search, "Tra nguồn rơi", "ui:search", true, () => PromptText("Tra nguồn rơi", "Nhập tên vật phẩm cần tìm.", "Vật phẩm", "VD: Hồi Xuân Đan", "Tìm", q =>
                    Act("/item/sources", Body("query", q), result =>
                    {
                        var options = new List<PickOption>();
                        foreach (var r in result["results"].Items)
                            options.Add(new PickOption { Label = Clean(r["name"].Str(r["itemName"].Str())), Sub = Clean(r["source"].Str(r["where"].Str(r["desc"].Str()))) });
                        PickOne("Nguồn rơi", options, "Không tìm thấy nguồn rơi.");
                    })));
            });
        }

        private static string CodexSection(string key)
        {
            switch (key)
            {
                case "skills": return "Công pháp";
                case "items": return "Vật phẩm";
                case "equipment": case "equips": return "Trang bị";
                case "monsters": return "Yêu thú";
                case "materials": return "Nguyên liệu";
                default: return key;
            }
        }

        // ================================================================== Tông môn

        private void OpenSectScreen()
        {
            var back = BackTarget();
            Fetch("/sect", data =>
            {
                var my = data["mySect"];
                var body = OpenScreen("Tông Môn", my.IsObject ? Clean(my["name"].Str()) : "Gia nhập hoặc khai tông lập phái", "tong_mon", back);
                if (!my.IsObject)
                {
                    var (left, right) = Split(body, .62f);
                    var list = ScrollColumn(left, 8f, 6);
                    foreach (var sect in data["list"].Items)
                    {
                        var s = sect;
                        Row(list, UiPixelIcon("tong_mon"), AuthGoldAccent, Clean(s["name"].Str()), $"Cấp {s["level"].Int()} · {s["memberCount"].Int()} đệ tử · {Clean(s["buffDesc"].Str())}", "Gia nhập", null, false,
                            () => Confirm("Gia nhập tông môn", "Bái nhập " + Clean(s["name"].Str()) + "?", "Gia nhập", () => Act("/sect/join", Body("sectId", s["id"].Str()), _ => OpenSectScreen())), 104f);
                    }
                    if (data["list"].Count == 0) EmptyState(list, "tong_mon", "Chưa có tông môn nào.");
                    var createPanel = Detail(right, UiPixelIcon("tong_mon"), AuthGoldAccent, "Khai tông lập phái", null, "Tự lập tông môn của riêng bạn.", 1);
                    DetailAction(createPanel, "Lập tông môn", "ui:tong_mon", true, () => PromptText("Lập tông môn", "Đặt tên cho tông môn mới.", "Tên tông môn", "VD: Thanh Vân Môn", "Lập",
                        name => Act("/sect/create", Body("name", name), _ => OpenSectScreen()), 24), data["canCreate"].Bool(true));
                    return;
                }
                var (info, actions) = Split(body, .6f);
                var col = ScrollColumn(info, 6f, 10);
                StatLine(col, "Tông chủ", Clean(my["leaderName"].Str()));
                StatLine(col, "Cấp", my["level"].Int().ToString());
                StatLine(col, "Đệ tử", my["memberCount"].Int().ToString());
                StatLine(col, "Cống hiến", Vn(my["totalContribution"]));
                StatLine(col, "Chức vị của bạn", Clean(hub["player"]["sectRole"].Str()));
                StatLine(col, "Điểm tông môn", Vn(hub["player"]["sectCoins"]));
                if (!string.IsNullOrEmpty(my["buffDesc"].Str())) Paragraph(col, "Phúc lợi: " + my["buffDesc"].Str(), 22, PositiveText);
                foreach (var member in my["members"].Items)
                    StatLine(col, Clean(member["name"].Str()), Clean(member["roleName"].Str(member["role"].Str())) + " · " + Vn(member["contribution"]));
                var panel = Detail(actions, UiPixelIcon("tong_mon"), AuthGoldAccent, Clean(my["name"].Str()), null, Clean(my["desc"].Str()), 3);
                DetailAction(panel, "Điểm danh", "ui:sun", true, () => Act("/sect/daily", Body(), _ => OpenSectScreen()), data["canClaimDaily"].Bool(true));
                DetailAction(panel, "Cống hiến", "ui:coin", false, () => PromptNumber("Cống hiến", "Số linh thạch muốn cống hiến.", 100, Math.Max(100, hub["player"]["stones"].Long()), 1000, "Cống hiến",
                    n => Act("/sect/donate", Body("stones", n), _ => OpenSectScreen())));
                DetailAction(panel, "Lãnh địa", "ui:location", false, OpenTerritoryScreen);
                DetailAction(panel, "Thăng chức", "ui:power", false, () => Act("/sect/promote", Body(), _ => OpenSectScreen()));
                DetailAction(panel, "Rời tông", "alert", false, () => Confirm("Rời tông môn", "Rời khỏi tông môn hiện tại?", "Rời tông", () => Act("/sect/leave", Body(), _ => OpenSectScreen()), true));
            });
        }

        private void OpenTerritoryScreen()
        {
            Fetch("/sect/territories", data =>
            {
                var body = OpenScreen("Lãnh Địa Tông Môn", "Chiếm lãnh địa để thu tài nguyên mỗi giờ", "location", OpenSectScreen);
                var list = ScrollColumn(body, 6f, 6);
                foreach (var t in data["list"].Items)
                {
                    var terr = t;
                    var mine = terr["isMySect"].Bool();
                    Row(list, UiPixelIcon("location"), mine ? PositiveText : AuthTextTertiary, Clean(terr["name"].Str()),
                        $"{Clean(terr["resource"].Str())} · {Vn(terr["yieldPerHour"])}/giờ · {(string.IsNullOrEmpty(terr["occupiedSectName"].Str()) ? "Vô chủ" : Clean(terr["occupiedSectName"].Str()))}",
                        mine ? "Thu " + Vn(terr["accumulated"]) : "Chiếm", null, false,
                        () => Act(mine ? "/sect/territory/harvest" : "/sect/territory/claim", Body("territoryId", terr["id"].Str()), _ => OpenTerritoryScreen()), 100f);
                }
            });
        }

        // ================================================================== Tửu Lâu (social)

        private void OpenSocialScreen()
        {
            var back = BackTarget();
            Fetch("/social", data =>
            {
                var body = OpenScreen("Tửu Lâu", "Đạo hữu · Tổ đội · Đạo lữ", "ban_be", back);
                var tabs = new[] { "Đạo hữu", "Lời mời", "Tổ đội", "Đạo lữ" };
                var area = Tabs(body, tabs, socialTab, i => { socialTab = i; OpenSocialScreen(); });
                var list = ScrollColumn(area, 6f, 6);
                switch (socialTab)
                {
                    case 0:
                        foreach (var f in data["friends"].Items)
                        {
                            var friend = f;
                            Row(list, UiPixelIcon("ban_be"), AuthGoldAccent, Clean(friend["name"].Str()), Clean(friend["realmName"].Str()) + " · " + Clean(friend["townName"].Str()), null, null, false,
                                () => Confirm("Đạo hữu", "Hủy kết giao với " + Clean(friend["name"].Str()) + "?", "Hủy kết giao", () => Act("/social/friend/remove", Body("targetId", friend["userId"].Str(friend["id"].Str())), _ => OpenSocialScreen()), true), 92f);
                        }
                        if (data["friends"].Count == 0) EmptyState(list, "ban_be", "Chưa có đạo hữu. Gặp tu sĩ khác ở Lôi Đài hoặc bảng xếp hạng để kết giao.");
                        break;
                    case 1:
                        foreach (var r in data["friendRequests"].Items)
                        {
                            var req = r;
                            var fromId = req["fromId"].Str(req["userId"].Str());
                            Row(list, UiPixelIcon("ban_be"), PositiveText, Clean(req["name"].Str(req["fromName"].Str())), "Muốn kết giao", "Đồng ý", null, false,
                                () => Act("/social/friend/accept", Body("fromId", fromId), _ => OpenSocialScreen()), 92f);
                        }
                        if (data["friendRequests"].Count == 0) EmptyState(list, "mail", "Không có lời mời mới.");
                        break;
                    case 2:
                        client.Get("/party/status", (party, _) =>
                        {
                            if (list == null) return;
                            var p = party["party"];
                            if (!p.IsObject)
                            {
                                Paragraph(list, "Chưa có tổ đội. Lập tổ đội để cùng đánh Boss thế giới và bí cảnh nhiều người.");
                                Row(list, UiPixelIcon("ban_be"), AuthGoldAccent, "Lập tổ đội", "Nhận mã mời 4 ký tự", null, null, false, () => Act("/party/create", Body(), __ => OpenSocialScreen()), 92f);
                                Row(list, UiPixelIcon("search"), AuthGoldAccent, "Vào tổ đội", "Nhập mã mời", null, null, false, () => PromptText("Vào tổ đội", "Nhập mã tổ đội.", "Mã", "ABCD", "Vào",
                                    code => Act("/party/join", Body("code", code.ToUpperInvariant()), __ => OpenSocialScreen()), 6), 92f);
                                return;
                            }
                            StatLine(list, "Mã tổ đội", Clean(p["code"].Str()), AuthGoldAccent);
                            foreach (var m in p["members"].Items)
                                StatLine(list, Clean(m["name"].Str()), (m["ready"].Bool() ? "Sẵn sàng" : "Chưa sẵn sàng") + " · " + Clean(m["realmName"].Str()));
                            Row(list, UiPixelIcon("power"), PositiveText, "Sẵn sàng / Hủy", null, null, null, false, () => Act("/party/ready", Body("ready", true), __ => OpenSocialScreen()), 80f);
                            Row(list, UiPixelIcon("road"), AuthError, "Rời tổ đội", null, null, null, false, () => Act("/party/leave", Body(), __ => OpenSocialScreen()), 80f);
                        });
                        break;
                    case 3:
                        client.Get("/companion", (comp, _) =>
                        {
                            if (list == null) return;
                            var c = comp["companion"];
                            if (c.IsObject)
                            {
                                StatLine(list, "Đạo lữ", Clean(c["name"].Str()), AuthGoldAccent);
                                StatLine(list, "Thân mật", Vn(c["intimacy"]));
                                Row(list, UiPixelIcon("heart"), PositiveText, "Song tu", "Tăng tu vi cùng đạo lữ", null, null, false, () => Act("/companion/songtu", Body(), __ => OpenSocialScreen()), 90f);
                                Row(list, UiPixelIcon("coin"), AuthGoldAccent, "Tặng quà", null, null, null, false, () => Act("/companion/gift", Body("type", "flower"), __ => OpenSocialScreen()), 80f);
                                return;
                            }
                            foreach (var p in comp["proposals"].Items)
                            {
                                var prop = p;
                                Row(list, UiPixelIcon("heart"), PositiveText, Clean(prop["name"].Str(prop["fromName"].Str())), "Cầu kết đạo lữ", "Đồng ý", null, false,
                                    () => Act("/companion/accept", Body("fromId", prop["fromId"].Str(prop["userId"].Str())), __ => OpenSocialScreen()), 92f);
                            }
                            Paragraph(list, "Chưa có đạo lữ. Kết giao đạo hữu khác giới rồi cầu thân để cùng song tu.");
                        });
                        break;
                }
            });
        }

        // ================================================================== Lôi đài (PvP)

        private void OpenPvpScreen()
        {
            var back = BackTarget();
            Fetch("/pvp", data =>
            {
                var me = data["me"];
                var body = OpenScreen("Lôi Đài", $"{Vn(me["points"])} điểm · {me["wins"].Int()} thắng / {me["losses"].Int()} bại · còn {me["dailyPvpRemaining"].Int()} lượt", "swords", back);
                var list = ScrollColumn(body, 6f, 6);
                foreach (var challenge in data["challenges"]["received"].Items)
                {
                    var ch = challenge;
                    Row(list, UiPixelIcon("swords"), AuthError, Clean(ch["fromName"].Str()) + " khiêu chiến", "Chấp nhận để vào trận", "Nhận", null, false,
                        () => Act("/pvp/respond", Body("challengeId", ch["id"].Str(), "accept", true)), 92f);
                }
                SectionLabel(list, "Đối thủ cùng thành");
                foreach (var op in data["sameTownPlayers"].Items) PvpRow(list, op);
                SectionLabel(list, "Đối thủ khác");
                foreach (var op in data["opponents"].Items) PvpRow(list, op);
                if (data["sameTownPlayers"].Count + data["opponents"].Count == 0) EmptyState(list, "swords", "Chưa có đối thủ phù hợp. Quay lại sau.");
            });
        }

        private void PvpRow(Transform list, J op)
        {
            var target = op["userId"].Str();
            Row(list, UiPixelIcon(op["isDemon"].Bool() ? "siren" : "swords"), op["isDemon"].Bool() ? AuthError : AuthGoldAccent, Clean(op["name"].Str()),
                $"{Clean(op["realmName"].Str())} · {Clean(op["townName"].Str())} · {op["wins"].Int()}T/{op["losses"].Int()}B", Vn(op["power"]), "chiến lực", false,
                () => Confirm("Khiêu chiến", "Khiêu chiến " + Clean(op["name"].Str()) + " trên lôi đài?", "Khiêu chiến", () => StartPvp(target)), 96f);
        }

        // ================================================================== Thành Chủ Phủ

        private void OpenLordScreen()
        {
            var back = BackTarget();
            Fetch("/npcs", data =>
            {
                var town = hub["town"];
                var body = OpenScreen("Thành Chủ Phủ", $"{Clean(town["lordTitle"].Str("Thành chủ"))} {Clean(town["lordName"].Str())} · {Clean(town["lordRealmName"].Str())}", "location", back);
                var (left, right) = Split(body, .58f);
                var list = ScrollColumn(left, 6f, 6);
                SectionLabel(list, $"Cao thủ thiên hạ · còn {data["daily"]["remaining"].Int()} lượt luận bàn");
                foreach (var npc in data["list"].Items)
                {
                    var n = npc;
                    Row(list, UiPixelIcon("swords"), AuthGoldAccent, Clean(n["name"].Str()) + " · " + Clean(n["title"].Str()), $"{Clean(n["realmName"].Str())} · {Clean(n["monName"].Str())}", Vn(n["power"]), "chiến lực", false,
                        () => OpenNpcDialog(n), 96f);
                }
                var panel = Detail(right, UiPixelIcon("location"), AuthGoldAccent, Clean(town["name"].Str()), Clean(town["realmMinName"].Str()), null, 0);
                Paragraph(panel.Info, town["desc"].Str());
                StatLine(panel.Info, "Phí chữa thương", Vn(town["healingCost"]));
                foreach (var tide in hub["activeBeastTides"].Items)
                    Paragraph(panel.Info, "Thú triều: " + Clean(tide["townName"].Str()) + " · " + Clean(tide["desc"].Str()), 21, AuthError);
            });
        }
    }
}
