using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Animated server-synchronized fallback presentation shared by PvE and PvP.</summary>
    public sealed class PixelCombatPresentation : MonoBehaviour
    {
        private sealed class Fighter
        {
            public RectTransform rect;
            public RectTransform shadow;
            public Image image;
            public RectTransform weapon;
            public Sprite[] frames;
            public Vector2 home;
            public float phase;
            public int role;
            public float attackUntil;
            public float hitUntil;
        }

        private sealed class Projectile
        {
            public RectTransform rect;
            public Image image;
            public Sprite[] frames;
            public Vector2 start;
            public Vector2 end;
            public float launched;
            public float duration;
            public float angle;
            public bool radial;
            public bool aura;
            public Color tint;
        }

        private sealed class SkillFxInfo { public string id; public string name; public string kind; }

        private static readonly Color Gold = new Color32(225, 185, 104, 255);
        private static readonly Color Cream = new Color32(239, 228, 203, 255);
        private static readonly Color Panel = new Color32(25, 33, 43, 255);
        private static readonly Vector2[] SkillCenters = {
            new Vector2(.75f, .145f), new Vector2(.795f, .282f), new Vector2(.86f, .39f),
            new Vector2(.935f, .40f), new Vector2(.97f, .28f)
        };
        private static readonly Dictionary<string, Texture2D> TerrainCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> IllustratedGroundCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> FighterCache = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> ItemSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> itemIdsByName;
        private readonly Dictionary<string, SkillFxInfo> knownSkills = new Dictionary<string, SkillFxInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Text> skillCooldownLabels = new Dictionary<string, Text>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Button> skillButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private static Sprite projectileSprite;
        private static Sprite shadowSprite;

        private readonly List<Fighter> fighters = new List<Fighter>();
        private readonly List<Projectile> projectiles = new List<Projectile>();
        private NetworkGameClient client;
        private RectTransform root;
        private Text statusText;
        private Text logText;
        private string lastLog;
        private bool immortalRealm;
        private bool pvp;
        private string battleId;
        private string playerName;
        private string enemyName;
        private string enemyId;
        private string enemyElement;
        private Sprite activeWeaponSprite;
        private Button attackButton;
        private float controlScale = 1f;
        private Action<string> pveAction;
        private Action<BattleSkill> pveSkillAction;
        private Action<string, string, string, string> pvpAction;
        private static float nextAutoPveActionAt;
        private static float nextAutoPvpActionAt;
        private static string autoBattleId;
        private static string autoPvpBattleId;
        private Coroutine sceneLoop;
        private Coroutine pollLoop;
        private Action<BattleView> pveComplete;
        private Action<PvpBattle> pvpComplete;

        public void BuildPve(BattleView battle, NetworkGameClient network, AppearanceColors appearance, string playerClass, string weaponId, bool immortal,
            string initialAction, string initialSkillId, string initialSkillName, string initialSkillKind,
            Action<string> act, Action<BattleSkill> useSkill, Action<BattleItem> useItem, Action nextStage, Action exit,
            Action<BattleView> completed)
        {
            pvp = false;
            client = network;
            immortalRealm = immortal;
            battleId = battle.id;
            if (autoBattleId != battleId) { autoBattleId = battleId; nextAutoPveActionAt = Time.unscaledTime + 0.65f; }
            playerName = battle.p?.name ?? "Đạo hữu";
            enemyName = battle.m?.name ?? "Yêu thú";
            enemyId = battle.m?.id;
            enemyElement = battle.m?.element ?? "";
            activeWeaponSprite = PixelWeaponArt.ForId(weaponId) ?? PixelWeaponArt.ForClass(playerClass);
            pveAction = act;
            pveSkillAction = useSkill;
            pveComplete = completed;
            BuildGround(battle.battleMap);
            Canvas.ForceUpdateCanvases();
            controlScale = Mathf.Clamp(Mathf.Min(root.rect.width / 1500f, root.rect.height / 790f), .72f, 1.22f);
            BuildHeader(enemyName, battle.m?.hp ?? 0, battle.m?.maxHp ?? 0,
                "PVE · " + (battle.battleMap?.name ?? "Chiến trường"), battle.m?.warn != null ? "CẢNH BÁO · YÊU THÚ SẮP TUNG CHIÊU" : null);
            AddPveFighters(battle, appearance, playerClass, weaponId);
            BuildPlayerHud(playerName, battle.p?.hp ?? 0, battle.p?.maxHp ?? 0, battle.p?.mp ?? 0, battle.p?.maxMp ?? 0);
            BuildLog(LastPveLog(battle));
            BuildStatus();
            if (!battle.over)
            {
                var visible = new List<BattleSkill>();
                foreach (var skill in battle.skills ?? Array.Empty<BattleSkill>())
                {
                    RememberSkill(skill?.id, skill?.name, skill?.kind);
                    if (skill != null && !skill.locked && !string.IsNullOrEmpty(skill.id) && visible.Count < 5) visible.Add(skill);
                }
                BuildPveActions(visible.ToArray(), battle, act, useSkill, useItem, nextStage, exit, battle.over);
                SetStatus("Thao tác và sát thương do máy chủ xử lý.");
            }
            else if (!string.IsNullOrEmpty(battle.dungeonLeaderId) && (battle.result == "win" || battle.result == "win_down"))
            {
                AddButton("MỞ ẢI TIẾP THEO", new Vector2(0.66f, 0.05f), new Vector2(0.96f, 0.21f), Gold, nextStage);
                SetStatus("Chiến thắng · bí cảnh còn ải tiếp theo.");
            }
            else
            {
                AddButton("TRỞ VỀ", new Vector2(0.66f, 0.05f), new Vector2(0.96f, 0.21f), Gold, exit);
                SetStatus("Trận đã kết thúc: " + battle.result);
            }
            StartScene(!battle.over);
            PlayAction(initialAction, initialSkillId, initialSkillName, initialSkillKind);
            if (!battle.over) pollLoop = StartCoroutine(PollPve());
        }

        public void BuildPvp(PvpBattle battle, NetworkGameClient network, AppearanceColors appearance, string playerClass, string weaponId, bool immortal,
            string initialAction, string initialSkillId, string initialSkillName, string initialSkillKind,
            Action<string, string, string, string> act, Action refresh, Action exit,
            Action<PvpBattle> completed)
        {
            pvp = true;
            client = network;
            immortalRealm = immortal;
            battleId = battle.id;
            playerName = battle.me?.name ?? "Đạo hữu";
            pvpAction = act;
            if (autoPvpBattleId != battleId) { autoPvpBattleId = battleId; nextAutoPvpActionAt = Time.unscaledTime + .65f; }
            enemyName = battle.opponent?.name ?? "Đối thủ";
            activeWeaponSprite = PixelWeaponArt.ForId(weaponId) ?? PixelWeaponArt.ForClass(playerClass);
            pvpComplete = completed;
            BuildGround(battle.battleMap);
            Canvas.ForceUpdateCanvases();
            controlScale = Mathf.Clamp(Mathf.Min(root.rect.width / 1500f, root.rect.height / 790f), .72f, 1.22f);
            BuildHeader(enemyName, battle.opponent?.hp ?? 0, battle.opponent?.maxHp ?? 0,
                "LÔI ĐÀI · " + (battle.battleMap?.name ?? "Chiến trường PvP"), battle.over ? (battle.isWin ? "CHIẾN THẮNG" : "KẾT THÚC") : null);
            AddPvpFighters(battle, appearance, playerClass, weaponId);
            BuildPlayerHud(playerName, battle.me?.hp ?? 0, battle.me?.maxHp ?? 0, battle.me?.mp ?? 0, battle.me?.maxMp ?? 0);
            BuildLog(LastPvpLog(battle));
            BuildStatus();
            if (!battle.over)
            {
                AddButton("LÀM MỚI", new Vector2(0.84f, 0.86f), new Vector2(0.98f, 0.93f), Panel, refresh);
                BuildPvpActions(battle, act);
                SetStatus(battle.myTurn ? "Bạn có thể ra chiêu · máy chủ đồng bộ cả hai người chơi." : "Đang chờ trạng thái của đối thủ.");
            }
            else
            {
                AddButton("TRỞ VỀ", new Vector2(0.66f, 0.05f), new Vector2(0.96f, 0.21f), Gold, exit);
                SetStatus(battle.isWin ? "Đạo hữu đã thắng trận PvP." : "Trận PvP đã kết thúc.");
            }
            StartScene(!battle.over);
            PlayAction(initialAction, initialSkillId, initialSkillName, initialSkillKind);
            if (!battle.over) pollLoop = StartCoroutine(PollPvp());
        }

        private void Awake()
        {
            root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        private void BuildGround(BattleMapInfo map)
        {
            var texture = pvp ? PvpGround() : GroundFor(map, immortalRealm);
            var go = new GameObject("PixelBattleGround", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            Place(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
        }

        /// <summary>Procedurally paints the dedicated terrain returned for the active PvE/PvP mode.</summary>
        internal static Texture2D GroundFor(BattleMapInfo map, bool immortal)
        {
            var illustrated = IllustratedGroundFor(map);
            if (illustrated != null) return illustrated;
            illustrated = LoadIllustratedGround("battle-pham-pve-hoang-nguyen");
            return illustrated ?? LoadIllustratedGround("battle-pham-small-monster");
        }

        internal static Texture2D PvpGround()
        {
            var arena = LoadIllustratedGround("battle-pham-duel");
            return arena ?? LoadIllustratedGround("battle-pham-small-monster");
        }

        private static Texture2D IllustratedGroundFor(BattleMapInfo map)
        {
            var theme = (map?.visualThemeId ?? string.Empty).ToLowerInvariant();
            var layout = (map?.layout ?? string.Empty).ToLowerInvariant();
            string asset = null;
            if (theme.Contains("bang-lien") || theme.Contains("frost") || theme.Contains("snow") || theme.Contains("ice") || layout.Contains("snow") || layout.Contains("ice"))
                asset = "battle-pham-pve-bang-lien";
            else if (theme.Contains("hoang-nguyen") || theme.Contains("wasteland") || layout.Contains("barren") || layout.Contains("ash_field"))
                asset = "battle-pham-pve-hoang-nguyen";
            else if (theme.Contains("/small-monster") || layout.Contains("forest") || layout.Contains("trail"))
                asset = "battle-pham-small-monster";
            else if (theme.Contains("/elite-boss") || theme.Contains("/cave-") || layout.Contains("cave") || layout.Contains("cavern"))
                asset = "battle-pham-boss-cave";
            else if (theme.Contains("/duel") || layout.Contains("duel"))
                asset = "battle-pham-duel";
            else if (theme.Contains("/ranked") || theme.Contains("/sect") || layout.Contains("courtyard"))
                asset = "battle-pham-sect-arena";
            else if (theme.Contains("/world-boss") || layout.Contains("world_field") || layout.Contains("worldboss"))
                asset = "battle-pham-pve-hoang-nguyen";
            else if (theme.Contains("/sat-phat") || layout.Contains("broken") || layout.Contains("shattered"))
                asset = "battle-pham-sat-phat";
            return asset == null ? null : LoadIllustratedGround(asset);
        }

        private static Texture2D LoadIllustratedGround(string asset)
        {
            if (string.IsNullOrEmpty(asset)) return null;
            if (!IllustratedGroundCache.TryGetValue(asset, out var texture) || texture == null)
            {
                texture = Resources.Load<Texture2D>("BattleMaps/" + asset);
                if (texture != null) IllustratedGroundCache[asset] = texture;
            }
            return texture;
        }

        private void BuildHeader(string foe, long hp, long maxHp, string location, string warning)
        {
            var map = AddPanel("BattleLocation", new Vector2(0.02f, 0.86f), new Vector2(0.215f, 0.98f), new Color(0.04f, 0.06f, 0.08f, 0.92f));
            AddText(map.transform, "Location", 15, Gold, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f)).text = location;
            var header = AddPanel("EnemyStatus", new Vector2(0.225f, 0.835f), new Vector2(0.775f, 0.985f), new Color(0.03f, 0.045f, 0.065f, 0.93f));
            AddText(header.transform, "Name", 20, Cream, TextAnchor.MiddleCenter, new Vector2(0.03f, 0.57f), new Vector2(0.97f, 0.96f)).text = foe;
            AddMeter(header.transform, "EnemyHP", "HP", hp, maxHp, new Color32(188, 65, 58, 255), new Vector2(0.035f, 0.12f), new Vector2(0.965f, 0.53f));
            if (!string.IsNullOrWhiteSpace(warning))
            {
                var box = AddPanel("BattleWarning", new Vector2(0.785f, 0.86f), new Vector2(0.98f, 0.98f), new Color(0.30f, 0.045f, 0.035f, 0.94f));
                AddText(box.transform, "WarningText", 14, new Color32(255, 196, 139, 255), TextAnchor.MiddleCenter, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f)).text = warning;
            }
        }

        private void BuildPlayerHud(string name, long hp, long maxHp, long mp, long maxMp)
        {
            var box = AddPanel("PlayerCombatHUD", new Vector2(0.015f, 0.025f), new Vector2(0.345f, 0.225f), new Color(0.035f, 0.055f, 0.07f, 0.95f));
            AddText(box.transform, "PlayerName", 17, Gold, TextAnchor.MiddleLeft, new Vector2(0.045f, 0.72f), new Vector2(0.95f, 0.98f)).text = name;
            AddMeter(box.transform, "PlayerHP", "KHÍ HUYẾT", hp, maxHp, new Color32(70, 188, 106, 255), new Vector2(0.045f, 0.405f), new Vector2(0.955f, 0.68f));
            AddMeter(box.transform, "PlayerMP", "LINH LỰC", mp, maxMp, new Color32(63, 156, 224, 255), new Vector2(0.045f, 0.10f), new Vector2(0.955f, 0.375f));
        }

        private void BuildPveActions(BattleSkill[] skills, BattleView battle, Action<string> act, Action<BattleSkill> useSkill, Action<BattleItem> useItem, Action next, Action exit, bool over)
        {
            if (over) return;
            foreach (var skill in skills) RememberSkill(skill?.id, skill?.name, skill?.kind);
            AddAttackCircle(() => act?.Invoke("attack"));
            for (var i = 0; i < SkillCenters.Length; i++)
            {
                var selected = i < skills.Length ? skills[i] : null;
                AddSkillCircle(selected, SkillCenters[i], i + 1,
                    selected == null ? null : (Action)(() => useSkill?.Invoke(selected)));
            }
            UpdatePveCooldownLabels(battle);
            for (var i = 0; i < 3; i++)
            {
                var item = battle.items != null && i < battle.items.Length ? battle.items[i] : null;
                AddItemCircle(item, new Vector2(.51f + i * .068f, .105f), () => { if (item != null) useItem?.Invoke(item); });
            }
            AddButton("RÚT LUI", new Vector2(0.02f, 0.775f), new Vector2(0.15f, 0.84f), new Color32(53, 48, 43, 255), () => act?.Invoke("flee"));
        }

        private void BuildPvpActions(PvpBattle battle, Action<string, string, string, string> act)
        {
            if (battle.over) return;
            foreach (var skill in battle.me?.skills ?? Array.Empty<PvpSkill>()) RememberSkill(skill?.id, skill?.name, skill?.kind);
            foreach (var skill in battle.opponent?.skills ?? Array.Empty<PvpSkill>()) RememberSkill(skill?.id, skill?.name, skill?.kind);
            AddAttackCircle(() => act?.Invoke("attack", null, null, null));
            if (attackButton != null) attackButton.interactable = battle.me?.attackCdLeft <= 0;
            var index = 0;
            foreach (var skill in battle.me?.skills ?? Array.Empty<PvpSkill>())
            {
                if (skill == null || index >= SkillCenters.Length) continue;
                var skillId = skill.id;
                var button = AddSkillCircle(skill.id, skill.name, skill.kind, SkillCenters[index], index + 1,
                    () => act?.Invoke("skill", skillId, skill.name, skill.kind));
                button.interactable = skill.canUse;
                var cooldown = button.transform.Find("Cooldown")?.GetComponent<Text>();
                if (cooldown != null) cooldown.text = skill.cdLeft > 0 ? skill.cdLeft.ToString() : "";
                index++;
            }
            for (; index < SkillCenters.Length; index++) AddSkillCircle(null, SkillCenters[index], index + 1, null);
        }

        private Button AddCircle(string name, Vector2 center, float diameter, bool attack, bool item, Action click)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = center;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * diameter * controlScale;
            var image = go.GetComponent<Image>(); image.sprite = PixelCombatHudArt.Circle(attack, item); image.preserveAspect = true;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 1f, .78f); colors.pressedColor = Gold;
            colors.disabledColor = new Color(.50f, .58f, .61f, .7f); button.colors = colors;
            button.interactable = click != null;
            if (click != null) button.onClick.AddListener(() => click());
            return button;
        }

        private Image AddCircleIcon(Button button, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var rect = go.GetComponent<RectTransform>(); Place(rect, min, max);
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        private void AddAttackCircle(Action click)
        {
            var button = AddCircle("AttackSword", new Vector2(.91f, .14f), 112f, true, false, click);
            attackButton = button;
            AddCircleIcon(button, "WeaponPixel", activeWeaponSprite ?? PixelWeaponArt.ForClass("Kiếm"), new Vector2(.18f, .19f), new Vector2(.82f, .87f));
            var label = AddText(button.transform, "AttackLabel", 13, Gold, TextAnchor.MiddleCenter, new Vector2(.15f, .015f), new Vector2(.85f, .22f));
            label.text = "ĐÁNH"; label.raycastTarget = false;
        }

        private Button AddSkillCircle(BattleSkill skill, Vector2 center, int slot, Action click) =>
            AddSkillCircle(skill?.id, skill?.name, skill?.kind, center, slot, click);

        private Button AddSkillCircle(string id, string name, string kind, Vector2 center, int slot, Action click)
        {
            var button = AddCircle("SkillSlot" + slot, center, 68f, false, false, click);
            if (!string.IsNullOrEmpty(id))
            {
                var icon = AddCircleIcon(button, "SkillPixel", null, new Vector2(.16f, .16f), new Vector2(.84f, .84f));
                PixelSkillArt.Animate(icon, id, name, kind, immortalRealm, slot * .37f);
                skillButtons[id] = button;
                skillCooldownLabels[id] = AddText(button.transform, "Cooldown", 25, Cream, TextAnchor.MiddleCenter, new Vector2(.10f, .10f), new Vector2(.90f, .90f));
                skillCooldownLabels[id].raycastTarget = false;
            }
            var label = AddText(button.transform, "Slot", 11, Gold, TextAnchor.LowerRight, new Vector2(.58f, .04f), new Vector2(.86f, .33f));
            label.text = slot.ToString(); label.raycastTarget = false;
            return button;
        }

        private static string ResolveItemId(BattleItem item)
        {
            if (item == null) return null;
            if (!string.IsNullOrEmpty(item.id)) return item.id;
            if (itemIdsByName == null)
            {
                itemIdsByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in OfflineHuntCatalogData.Load()?.items ?? Array.Empty<OfflineItemData>())
                    if (entry != null && !string.IsNullOrEmpty(entry.name)) itemIdsByName[entry.name] = entry.id;
            }
            return !string.IsNullOrEmpty(item.name) && itemIdsByName.TryGetValue(item.name, out var id) ? id : null;
        }

        private void AddItemCircle(BattleItem item, Vector2 center, Action click)
        {
            var id = ResolveItemId(item);
            var button = AddCircle("BattleItem" + (item?.i ?? 0), center, 58f, false, true,
                item != null && !string.IsNullOrEmpty(item.uid) && item.qty > 0 ? click : null);
            if (!string.IsNullOrEmpty(id))
            {
                if (!ItemSpriteCache.TryGetValue(id, out var sprite) || sprite == null)
                {
                    var texture = Resources.Load<Texture2D>("CombatPixel/Items/" + id);
                    sprite = texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 64f);
                    ItemSpriteCache[id] = sprite;
                }
                if (sprite != null) AddCircleIcon(button, "ItemPixel", sprite, new Vector2(.17f, .16f), new Vector2(.83f, .84f));
            }
            if (item != null)
            {
                var label = AddText(button.transform, "Quantity", 12, Cream, TextAnchor.LowerRight, new Vector2(.54f, .03f), new Vector2(.93f, .37f));
                label.text = item.qty.ToString(); label.raycastTarget = false;
            }
        }

        private void UpdatePveCooldownLabels(BattleView battle)
        {
            var now = battle.now > 0 ? battle.now : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (attackButton != null) attackButton.interactable = battle.p != null && battle.p.atkReadyAt <= now && battle.p.stunUntil <= now;
            foreach (var skill in battle.skills ?? Array.Empty<BattleSkill>())
            {
                if (skill == null || string.IsNullOrEmpty(skill.id)) continue;
                var seconds = Mathf.CeilToInt(Mathf.Max(0, skill.readyAt - now) / 1000f);
                if (skillCooldownLabels.TryGetValue(skill.id, out var label) && label != null) label.text = seconds > 0 ? seconds.ToString() : "";
                if (skillButtons.TryGetValue(skill.id, out var button) && button != null)
                    button.interactable = !skill.locked && seconds == 0 && (battle.p?.mp ?? 0) >= skill.mp;
            }
        }

        private void UpdatePvpCooldownLabels(PvpBattle battle)
        {
            if (attackButton != null) attackButton.interactable = battle.me?.attackCdLeft <= 0 && battle.me?.stunTurns <= 0;
            foreach (var skill in battle.me?.skills ?? Array.Empty<PvpSkill>())
            {
                if (skill == null || string.IsNullOrEmpty(skill.id)) continue;
                if (skillButtons.TryGetValue(skill.id, out var button) && button != null) button.interactable = skill.canUse;
                if (skillCooldownLabels.TryGetValue(skill.id, out var label) && label != null) label.text = skill.cdLeft > 0 ? skill.cdLeft.ToString() : "";
            }
        }

        private void TryAutoPveAction(BattleView battle)
        {
            if (client == null || battle == null || battle.over || Time.unscaledTime < nextAutoPveActionAt) return;
            var now = battle.now > 0 ? battle.now : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (battle.p == null || battle.p.stunUntil > now || battle.p.atkReadyAt > now) return;
            nextAutoPveActionAt = Time.unscaledTime + 1.15f;
            foreach (var skill in battle.skills ?? Array.Empty<BattleSkill>())
            {
                if (skill == null || skill.locked || skill.readyAt > now || skill.mp > battle.p.mp || skill.kind == "escape") continue;
                pveSkillAction?.Invoke(skill); return;
            }
            pveAction?.Invoke("attack");
        }

        private void TryAutoPvpAction(PvpBattle battle)
        {
            if (client == null || battle == null || battle.over || !battle.myTurn || Time.unscaledTime < nextAutoPvpActionAt) return;
            if (battle.me == null || battle.me.stunTurns > 0) return;
            nextAutoPvpActionAt = Time.unscaledTime + 1.25f;
            foreach (var skill in battle.me.skills ?? Array.Empty<PvpSkill>())
            {
                if (skill == null || !skill.canUse || skill.kind == "escape") continue;
                pvpAction?.Invoke("skill", skill.id, skill.name, skill.kind); return;
            }
            if (battle.me.attackCdLeft <= 0) pvpAction?.Invoke("attack", null, null, null);
        }

        private void AttachSkillIcon(Button button, string id, string name, string kind)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(.24f, .03f);
                label.rectTransform.anchorMax = new Vector2(.98f, .97f);
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = TextAnchor.MiddleCenter;
            }
            var go = new GameObject("AnimatedSkillPixel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.015f, .19f); rect.anchorMax = new Vector2(.25f, .81f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
            PixelSkillArt.Animate(image, id, name, kind, immortalRealm, UnityEngine.Random.value);
        }

        private void RememberSkill(string id, string name, string kind)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            knownSkills[name] = new SkillFxInfo { id = id, name = name, kind = kind };
        }

        private void BuildStatus()
        {
            statusText = AddText(transform, "BattleStatus", 15, Cream, TextAnchor.MiddleCenter, new Vector2(0.255f, 0.245f), new Vector2(0.745f, 0.28f));
        }

        private void SetStatus(string text) { if (statusText != null) statusText.text = text; }

        private void BuildLog(string line)
        {
            lastLog = line;
            if (string.IsNullOrWhiteSpace(line)) return;
            var box = AddPanel("LastCombatAction", new Vector2(0.255f, 0.285f), new Vector2(0.745f, 0.345f), new Color(0.035f, 0.055f, 0.07f, 0.88f));
            logText = AddText(box.transform, "Text", 15, Cream, TextAnchor.MiddleCenter, new Vector2(0.025f, 0.04f), new Vector2(0.975f, 0.96f));
            logText.text = line;
        }

        private void AddPveFighters(BattleView battle, AppearanceColors appearance, string playerClass, string weaponId)
        {
            var monsterName = battle.m?.name ?? "Yêu thú";
            var monsterId = battle.m?.id;
            AddFighter("SpiritBeast", GetBeastFrames(immortalRealm, monsterId, monsterName, 0), new Vector2(0.31f, 0.47f), new Vector2(190f, 190f), 1, UnityEngine.Random.value * 2.4f);
            AddFighter("PlayerPixelFighter", GetPlayerFrames(appearance, "player"), new Vector2(0.70f, 0.45f), new Vector2(112f, 158f), 0, UnityEngine.Random.value * 2.4f);
            AddPlayerWeapon(playerClass, weaponId);
            var count = Mathf.Clamp(battle.m?.minionCount ?? 0, 0, 4);
            var places = new[] { new Vector2(0.17f, 0.38f), new Vector2(0.19f, 0.60f), new Vector2(0.42f, 0.61f), new Vector2(0.40f, 0.34f) };
            for (var i = 0; i < count; i++) AddFighter("SpiritMinion" + i, GetBeastFrames(immortalRealm, monsterId, monsterName, i + 1), places[i], new Vector2(78f, 82f), i + 2, UnityEngine.Random.value * 3f);
        }

        private void AddPvpFighters(PvpBattle battle, AppearanceColors appearance, string playerClass, string weaponId)
        {
            var opponent = new AppearanceColors { hair = "#B6B9C6", outfit = "#4C6698", eyes = "#8CD8F0" };
            AddFighter("OpponentPixelFighter", GetPlayerFrames(opponent, "opponent"), new Vector2(0.30f, 0.45f), new Vector2(130f, 174f), 1, UnityEngine.Random.value * 2.3f);
            AddFighter("PlayerPixelFighter", GetPlayerFrames(appearance, "player"), new Vector2(0.70f, 0.45f), new Vector2(130f, 174f), 0, UnityEngine.Random.value * 2.3f);
            AddPlayerWeapon(playerClass, weaponId);
            AddText(transform, "OpponentName", 17, Cream, TextAnchor.MiddleCenter, new Vector2(0.19f, 0.66f), new Vector2(0.41f, 0.71f)).text = battle.opponent?.name ?? "Đối thủ";
            AddText(transform, "Versus", 23, Gold, TextAnchor.MiddleCenter, new Vector2(0.46f, 0.43f), new Vector2(0.54f, 0.53f)).text = "VS";
            AddText(transform, "PlayerNameLabel", 17, Cream, TextAnchor.MiddleCenter, new Vector2(0.59f, 0.66f), new Vector2(0.81f, 0.71f)).text = battle.me?.name ?? "Đạo hữu";
        }

        private void AddFighter(string name, Sprite[] frames, Vector2 position, Vector2 size, int role, float phase)
        {
            var shadowObject = new GameObject(name + "Shadow", typeof(RectTransform), typeof(Image));
            shadowObject.transform.SetParent(transform, false);
            var shadow = shadowObject.GetComponent<RectTransform>();
            shadow.anchorMin = shadow.anchorMax = position;
            shadow.sizeDelta = new Vector2(size.x * 0.72f, Mathf.Max(10f, size.y * 0.14f));
            shadow.anchoredPosition = new Vector2(0f, -size.y * 0.34f);
            var shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = GetShadowSprite();
            shadowImage.raycastTarget = false;

            var body = new GameObject(name, typeof(RectTransform), typeof(Image));
            body.transform.SetParent(transform, false);
            var rect = body.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = position;
            rect.sizeDelta = size;
            var image = body.GetComponent<Image>();
            image.sprite = frames[0]; image.preserveAspect = true; image.raycastTarget = false;
            fighters.Add(new Fighter { rect = rect, shadow = shadow, image = image, frames = frames, home = position, phase = phase, role = role });
        }

        private void AddPlayerWeapon(string playerClass, string weaponId)
        {
            if (fighters.Count == 0) return;
            var fighter = fighters[fighters.Count - 1];
            if (fighter.role != 0) return;
            var sprite = PixelWeaponArt.ForId(weaponId) ?? PixelWeaponArt.ForClass(playerClass);
            if (sprite == null) return;
            var go = new GameObject("EquippedPixelWeapon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(fighter.rect, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.25f, .54f);
            rect.sizeDelta = new Vector2(62f, 62f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            fighter.weapon = rect;
        }

        private void StartScene(bool active)
        {
            sceneLoop = StartCoroutine(AnimateScene(active));
        }

        private IEnumerator AnimateScene(bool active)
        {
            while (fighters.Count > 0)
            {
                var now = Time.unscaledTime;
                for (var i = 0; i < fighters.Count; i++)
                {
                    var fighter = fighters[i];
                    if (fighter.rect == null || fighter.image == null) continue;
                    var time = now + fighter.phase;
                    var striking = active && fighter.attackUntil > now;
                    var hurt = fighter.hitUntil > now;
                    var dash = striking ? Mathf.Sin((1f - (fighter.attackUntil - now) / .34f) * Mathf.PI) : 0f;
                    var direction = fighter.role == 0 || fighter.role >= 2 && fighter.role % 2 == 0 ? -1f : 1f;
                    var duelist = fighter.role < 2;
                    var pursuit = active && duelist ? .5f + .5f * Mathf.Sin(now * 1.12f) : 0f;
                    var chaseX = duelist ? (fighter.role == 0 ? -.13f : .13f) * pursuit : 0f;
                    var runX = active ? Mathf.Sin(time * (duelist ? 1.75f : 1.4f)) * (duelist ? .055f : .032f) : Mathf.Sin(time * 1.4f) * .008f;
                    var runY = active ? Mathf.Cos(time * (duelist ? 1.53f : 1.91f)) * (duelist ? .065f : .045f) : Mathf.Sin(time * 2f) * .011f;
                    if (fighter.role >= 2 && active)
                    {
                        // Pack monsters weave through the clearing instead of bobbing in a fixed ring.
                        runX += Mathf.Sin(time * .68f + fighter.phase) * .065f;
                        runY += Mathf.Cos(time * .91f + fighter.phase) * .075f;
                    }
                    var x = fighter.home.x + chaseX + runX + direction * dash * (duelist ? .10f : .045f);
                    var y = fighter.home.y + runY + dash * .022f;
                    fighter.rect.anchorMin = fighter.rect.anchorMax = new Vector2(Mathf.Clamp(x, 0.08f, 0.92f), Mathf.Clamp(y, 0.28f, 0.72f));
                    if (fighter.shadow != null)
                    {
                        fighter.shadow.anchorMin = fighter.shadow.anchorMax = fighter.rect.anchorMin;
                        fighter.shadow.anchoredPosition = new Vector2(0f, -fighter.rect.sizeDelta.y * 0.34f);
                    }
                    var frame = hurt ? 6 + Mathf.FloorToInt(now * 9f) % 2
                        : striking ? 4 + Mathf.FloorToInt(now * 9f) % 2
                        : Mathf.FloorToInt(time * (active ? 8f : 2f)) % 4;
                    frame %= fighter.frames.Length;
                    fighter.image.sprite = fighter.frames[frame];
                    fighter.image.rectTransform.localScale = new Vector3(fighter.role == 1 && pvp ? -1f : 1f, 1f, 1f);
                    if (fighter.weapon != null)
                    {
                        var swing = striking ? Mathf.Sin((1f - (fighter.attackUntil - now) / .34f) * Mathf.PI) * 65f : Mathf.Sin(time * 2f) * 4f;
                        fighter.weapon.localRotation = Quaternion.Euler(0f, 0f, swing);
                    }
                }
                UpdateProjectiles(now);
                yield return null;
            }
            sceneLoop = null;
        }

        private void ShowImpact(bool fromPlayer)
        {
            var now = Time.unscaledTime;
            foreach (var fighter in fighters)
            {
                if (fighter.role == (fromPlayer ? 0 : 1)) fighter.attackUntil = now + .34f;
                else if (fighter.role == (fromPlayer ? 1 : 0)) fighter.hitUntil = now + .28f;
            }
        }

        private void SpawnVolley(Vector2 from, Vector2 to, Color tint, bool radial, int count)
        {
            var sprite = GetProjectileSprite();
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("PixelCombatVFX", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = from; rect.sizeDelta = new Vector2(32f, 32f);
                var image = go.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
                var hidden = tint; hidden.a = 0; image.color = hidden;
                projectiles.Add(new Projectile { rect = rect, image = image, start = from, end = to, launched = Time.unscaledTime + i * 0.045f, duration = radial ? 0.36f : 0.52f, angle = i * Mathf.PI * 2f / count, radial = radial, tint = tint });
            }
        }

        private void SpawnSkillEffect(Vector2 from, Vector2 to, string skillId, string skillName, string skillKind)
        {
            var kind = string.IsNullOrWhiteSpace(skillKind) ? "atk" : skillKind.ToLowerInvariant();
            var aura = kind == "shield" || kind == "reflect" || kind == "buff" || kind == "heal" || kind == "mana";
            var escape = kind == "escape";
            var impactAtCaster = aura || escape;
            var frames = PixelSkillArt.Frames(skillId, skillName, kind, immortalRealm);
            var count = kind == "multi" ? Mathf.Clamp(4 + ((skillId ?? skillName ?? "").Length % 4), 4, 7)
                : kind == "dot" ? 3 : 1;
            var accent = PixelSkillArt.Accent(skillId, skillName, kind, immortalRealm);
            for (var i = 0; i < count; i++)
            {
                var origin = from;
                var destination = impactAtCaster ? from : to;
                if (escape) destination = from + new Vector2(pvp ? .17f : -.14f, .035f);
                if (aura && count > 1)
                {
                    var angle = i * Mathf.PI * 2f / count;
                    destination = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .035f;
                }
                if (kind == "dot")
                {
                    var angle = i * Mathf.PI * 2f / count;
                    origin = to + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .025f;
                    destination = to;
                }
                var go = new GameObject("PixelSkillEffect_" + (skillId ?? "skill") + "_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = origin;
                rect.sizeDelta = new Vector2(20f, 20f);
                var image = go.GetComponent<Image>();
                image.sprite = frames[0]; image.preserveAspect = true; image.raycastTarget = false;
                projectiles.Add(new Projectile
                {
                    rect = rect, image = image, frames = frames, start = origin, end = destination,
                    launched = Time.unscaledTime + i * (kind == "multi" ? .075f : .06f),
                    duration = aura ? .94f : escape ? .72f : kind == "dot" ? 1.12f : 1.02f,
                    angle = i * Mathf.PI * 2f / Mathf.Max(1, count), radial = kind == "dot",
                    aura = aura || escape, tint = accent
                });
            }
        }

        private void UpdateProjectiles(float now)
        {
            for (var i = projectiles.Count - 1; i >= 0; i--)
            {
                var shot = projectiles[i];
                if (shot.rect == null || shot.image == null) { projectiles.RemoveAt(i); continue; }
                var elapsed = Time.unscaledTime - shot.launched;
                if (elapsed >= shot.duration) { Destroy(shot.rect.gameObject); projectiles.RemoveAt(i); continue; }
                var t = Mathf.Clamp01(elapsed / shot.duration);
                Vector2 pos;
                if (shot.aura)
                {
                    var radius = .012f + .012f * Mathf.Sin(t * Mathf.PI);
                    pos = shot.start + new Vector2(Mathf.Cos(shot.angle + t * 5.2f), Mathf.Sin(shot.angle + t * 5.2f)) * radius;
                }
                else if (shot.radial)
                    pos = shot.start + Vector2.Lerp(Vector2.zero, new Vector2(Mathf.Cos(shot.angle), Mathf.Sin(shot.angle)) * .03f, Mathf.Sin(t * Mathf.PI));
                else
                {
                    var travel = Mathf.Clamp01(t / .72f);
                    var arch = Mathf.Sin(travel * Mathf.PI) * (.035f + Mathf.Abs(shot.angle) % .035f);
                    pos = Vector2.Lerp(shot.start, shot.end, travel) + new Vector2(0f, arch);
                }
                shot.rect.anchorMin = shot.rect.anchorMax = pos;
                var impact = shot.aura ? Mathf.Sin(t * Mathf.PI) : Mathf.Clamp01((t - .68f) / .32f);
                var size = shot.frames == null ? 24f + Mathf.Sin(t * Mathf.PI) * 18f
                    : shot.aura ? 38f + impact * 30f : 32f + impact * 38f;
                shot.rect.sizeDelta = new Vector2(size, size);
                if (shot.frames == null)
                {
                    var color = shot.tint; color.a = elapsed < 0 ? 0f : Mathf.Sin(t * Mathf.PI) * 0.96f;
                    shot.image.color = color;
                }
                else
                {
                    var frame = Mathf.FloorToInt(Mathf.Max(0f, elapsed) * 15f) % shot.frames.Length;
                    shot.image.sprite = shot.frames[frame];
                    var alpha = elapsed < 0f ? 0f : shot.aura ? .65f + impact * .35f : t < .74f ? Mathf.Clamp01(t * 4f) : 1f - Mathf.Clamp01((t - .74f) / .26f);
                    shot.image.color = new Color(1f, 1f, 1f, alpha);
                    var rotation = shot.aura ? (float)Time.unscaledTime * 65f : Mathf.Atan2(shot.end.y - shot.start.y, shot.end.x - shot.start.x) * Mathf.Rad2Deg;
                    shot.rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
                }
            }
        }

        private void PlayAction(string action, string skillId, string skillName, string skillKind)
        {
            if (string.IsNullOrEmpty(action)) return;
            var player = FighterAnchor(0);
            var target = FighterAnchor(1);
            if (action == "item") SpawnSkillEffect(player, player, skillId, skillName, "heal");
            else if (action == "dodge") SpawnSkillEffect(player, player, "phu_don_quyet", "Vạn Dặm Thần Hành Phù", "escape");
            else if (action == "skill") SpawnSkillEffect(player, target, skillId, skillName, skillKind);
            else SpawnVolley(player, target, new Color32(255, 204, 94, 255), false, 7);
            if (action != "dodge" && action != "item") ShowImpact(true);
        }

        private Vector2 FighterAnchor(int role)
        {
            foreach (var fighter in fighters)
                if (fighter.role == role && fighter.rect != null) return fighter.rect.anchorMin;
            return role == 0 ? new Vector2(.70f, .45f) : new Vector2(.30f, .45f);
        }

        private IEnumerator PollPve()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.85f);
                var complete = false;
                client.LoadCurrentBattle((battle, error) =>
                {
                    complete = true;
                    if (battle == null || battle.id != battleId) return;
                    if (battle.over) { pveComplete?.Invoke(battle); return; }
                    UpdateMeter("EnemyStatus/EnemyHP", "HP", battle.m?.hp ?? 0, battle.m?.maxHp ?? 0);
                    UpdateMeter("PlayerCombatHUD/PlayerHP", "KHÍ HUYẾT", battle.p?.hp ?? 0, battle.p?.maxHp ?? 0);
                    UpdateMeter("PlayerCombatHUD/PlayerMP", "LINH LỰC", battle.p?.mp ?? 0, battle.p?.maxMp ?? 0);
                    var warning = transform.Find("BattleWarning");
                    if (warning != null) warning.gameObject.SetActive(battle.m?.warn != null);
                    UpdateLog(LastPveLog(battle), battle.p?.name, battle.m?.name);
                    UpdatePveCooldownLabels(battle);
                    TryAutoPveAction(battle);
                });
                while (!complete) yield return null;
            }
        }

        private IEnumerator PollPvp()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1.2f);
                var complete = false;
                client.LoadPvpBattle((battle, error) =>
                {
                    complete = true;
                    if (battle == null || battle.none || battle.id != battleId) return;
                    if (battle.over) { pvpComplete?.Invoke(battle); return; }
                    UpdateMeter("EnemyStatus/EnemyHP", "HP", battle.opponent?.hp ?? 0, battle.opponent?.maxHp ?? 0);
                    UpdateMeter("PlayerCombatHUD/PlayerHP", "KHÍ HUYẾT", battle.me?.hp ?? 0, battle.me?.maxHp ?? 0);
                    UpdateMeter("PlayerCombatHUD/PlayerMP", "LINH LỰC", battle.me?.mp ?? 0, battle.me?.maxMp ?? 0);
                    UpdateLog(LastPvpLog(battle), battle.me?.name, battle.opponent?.name);
                    UpdatePvpCooldownLabels(battle);
                    TryAutoPvpAction(battle);
                });
                while (!complete) yield return null;
            }
        }

        private void UpdateMeter(string path, string title, long value, long maximum)
        {
            var track = transform.Find(path);
            if (track == null) return;
            var fill = track.Find("Fill")?.GetComponent<Image>();
            if (fill != null) fill.fillAmount = maximum <= 0 ? 0f : Mathf.Clamp01((float)value / maximum);
            var text = track.Find("Value")?.GetComponent<Text>();
            if (text != null) text.text = $"{title}  {Math.Max(0, value):N0} / {Math.Max(0, maximum):N0}";
        }

        private void UpdateLog(string line, string attackerName, string targetName)
        {
            if (string.IsNullOrWhiteSpace(line) || line == lastLog) return;
            lastLog = line;
            if (logText != null) logText.text = line;
            if (!(line.Contains("tấn công") || line.Contains("chiêu") || line.Contains("liên kích") || line.Contains("dùng") || line.Contains("cắn xé") || line.Contains("−"))) return;
            var playerHit = !string.IsNullOrEmpty(attackerName) && line.Contains(attackerName) || line.Contains("Bạn tấn công") || line.Contains("Bạn dùng");
            var enemyHit = !string.IsNullOrEmpty(targetName) && line.Contains(targetName) || line.Contains("Bầy tiểu yêu");
            if (!playerHit && !enemyHit) return;
            var from = FighterAnchor(playerHit ? 0 : 1);
            var to = FighterAnchor(playerHit ? 1 : 0);
            ShowImpact(playerHit);
            foreach (var skill in knownSkills.Values)
            {
                if (string.IsNullOrWhiteSpace(skill.name) || line.IndexOf(skill.name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                SpawnSkillEffect(from, to, skill.id, skill.name, skill.kind);
                return;
            }
            if (!playerHit && !pvp && !string.IsNullOrEmpty(enemyElement))
            {
                SpawnSkillEffect(from, to, "quai_" + enemyId, enemyName + " " + enemyElement, "atk");
                return;
            }
            SpawnVolley(from, to, playerHit ? new Color32(255, 207, 92, 255) : new Color32(112, 215, 248, 255), false, 7);
        }

        private GameObject AddPanel(string name, Vector2 min, Vector2 max, Color color, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent ?? transform, false);
            Place(go.GetComponent<RectTransform>(), min, max);
            go.GetComponent<Image>().color = color;
            if (PixelUiSkin.NeedsFrame(name)) PixelUiSkin.ApplyFrame(go);
            return go;
        }

        private Text AddText(Transform parent, string name, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), min, max);
            var text = go.GetComponent<Text>(); text.font = ModernUi.Regular; text.fontSize = Mathf.RoundToInt(size * 1.4f); text.color = color; text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            PixelUiSkin.ApplyTextTreatment(text);
            return text;
        }

        private Button AddButton(string label, Vector2 min, Vector2 max, Color color, Action click)
        {
            var rootButton = AddPanel("Action_" + label, min, max, color);
            var button = rootButton.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = color; colors.highlightedColor = new Color(1f, 0.9f, 0.68f); colors.pressedColor = Gold; button.colors = colors;
            var text = AddText(rootButton.transform, "Text", 17, color == Gold ? new Color32(22, 24, 27, 255) : Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.text = label; text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 11; text.resizeTextMaxSize = 17;
            PixelUiSkin.ApplyFrame(rootButton);
            button.onClick.AddListener(() => click?.Invoke());
            return button;
        }

        private static void SetButtonFont(Button button, int size)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) { text.fontSize = size; text.resizeTextMaxSize = size; text.resizeTextMinSize = Mathf.Min(11, size); }
        }

        private void AddMeter(Transform parent, string name, string title, long value, long maximum, Color fillColor, Vector2 min, Vector2 max)
        {
            var track = AddPanel(name, min, max, new Color(0.01f, 0.015f, 0.02f, 0.96f), parent);
            var fillObject = AddPanel("Fill", Vector2.zero, Vector2.one, fillColor, track.transform);
            var fill = fillObject.GetComponent<Image>(); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            fill.fillAmount = maximum <= 0 ? 0f : Mathf.Clamp01((float)value / maximum); fill.raycastTarget = false;
            AddText(track.transform, "Value", 14, Cream, TextAnchor.MiddleCenter, new Vector2(0.015f, 0.015f), new Vector2(0.985f, 0.985f)).text = $"{title}  {Math.Max(0, value):N0} / {Math.Max(0, maximum):N0}";
        }

        private static Texture2D PixelGround(string mapId, bool immortal, string[] mapPalette, string layout)
        {
            var key = (immortal ? "tien:" : "pham:") + (mapId ?? "grassland") + ":" + (layout ?? string.Empty) + ":" + string.Join(",", mapPalette ?? Array.Empty<string>());
            if (TerrainCache.TryGetValue(key, out var cached) && cached != null) return cached;
            const int width = 512, height = 288, tile = 4;
            unchecked
            {
                var seed = immortal ? 739391 : 19349663;
                foreach (var c in key) seed = seed * 31 + c;
                var random = new System.Random(seed & 0x7fffffff);
                var pixels = new Color32[width * height];
                var primary = immortal ? new Color32(54, 81, 104, 255) : new Color32(74, 105, 65, 255);
                var secondary = immortal ? new Color32(92, 124, 143, 255) : new Color32(119, 143, 77, 255);
                var dark = immortal ? new Color32(42, 64, 83, 255) : new Color32(52, 78, 50, 255);
                var light = immortal ? new Color32(139, 166, 175, 255) : new Color32(166, 177, 111, 255);
                var accent = immortal ? new Color32(184, 171, 217, 255) : new Color32(192, 160, 91, 255);
                if (mapPalette != null && mapPalette.Length >= 3 &&
                    ColorUtility.TryParseHtmlString(mapPalette[0], out var mapPrimary) &&
                    ColorUtility.TryParseHtmlString(mapPalette[1], out var mapSecondary) &&
                    ColorUtility.TryParseHtmlString(mapPalette[2], out var mapAccent))
                {
                    var primary32 = (Color32)mapPrimary;
                    var secondary32 = (Color32)mapSecondary;
                    accent = (Color32)mapAccent;
                    dark = Color32.Lerp(primary32, new Color32(18, 22, 28, 255), 0.18f);
                    light = secondary32;
                    primary = Color32.Lerp(primary32, secondary32, .16f);
                    secondary = Color32.Lerp(primary32, secondary32, .72f);
                }

                var layoutKey = (layout ?? string.Empty).ToLowerInvariant();
                var caveLayout = layoutKey.Contains("cave") || layoutKey.Contains("cavern") || layoutKey.Contains("sealed") || layoutKey.Contains("grotto");
                var arenaLayout = layoutKey.Contains("duel") || layoutKey.Contains("ring") || layoutKey.Contains("courtyard") || layoutKey.Contains("platform");
                var forestLayout = layoutKey.Contains("forest") || layoutKey.Contains("trail") || layoutKey.Contains("grass");
                var brokenLayout = layoutKey.Contains("broken") || layoutKey.Contains("shattered") || layoutKey.Contains("void");
                var wideBossLayout = layoutKey.Contains("boss") || layoutKey.Contains("world_field") || layoutKey.Contains("worldboss");
                if (caveLayout)
                {
                    primary = Color32.Lerp(primary, new Color32(119, 96, 72, 255), .4f);
                    secondary = Color32.Lerp(secondary, new Color32(172, 132, 82, 255), .32f);
                }
                if (arenaLayout)
                {
                    primary = Color32.Lerp(primary, new Color32(107, 111, 112, 255), .32f);
                    secondary = Color32.Lerp(secondary, new Color32(165, 151, 120, 255), .36f);
                }
                if (brokenLayout)
                {
                    primary = Color32.Lerp(primary, new Color32(92, 58, 61, 255), .25f);
                    secondary = Color32.Lerp(secondary, new Color32(140, 83, 76, 255), .27f);
                }

                // Perlin bands create broad, painterly terrain changes instead of a tiled checkerboard.
                var ox = (seed & 1023) * .013f;
                var oy = ((seed >> 9) & 1023) * .013f;
                for (var y = 0; y < height; y += tile)
                for (var x = 0; x < width; x += tile)
                {
                    var noise = Mathf.PerlinNoise(x * .009f + ox, y * .011f + oy);
                    var patch = Mathf.Clamp01(.08f + noise * .78f + Mathf.PerlinNoise(x * .025f + oy, y * .026f + ox) * .18f);
                    var baseColor = Color32.Lerp(primary, secondary, patch);
                    for (var py = y; py < Mathf.Min(y + tile, height); py++)
                    for (var px = x; px < Mathf.Min(x + tile, width); px++) pixels[py * width + px] = baseColor;
                }

                var trail = Color32.Lerp(accent, light, .34f);
                var trailShade = Color32.Lerp(dark, primary, .62f);
                if (forestLayout)
                {
                    for (var x = 0; x < width; x += 2)
                    {
                        var y = height / 2 + Mathf.RoundToInt(Mathf.Sin(x * .014f + seed % 31) * 43f) + Mathf.RoundToInt(Mathf.Sin(x * .038f) * 17f);
                        DrawRect(pixels, width, height, x, y - 23, 3, 47, trailShade);
                        DrawRect(pixels, width, height, x, y - 17, 2, 35, trail);
                    }
                }
                else if (caveLayout || brokenLayout)
                {
                    for (var x = 0; x < width; x += 2)
                    {
                        var y = height / 2 + Mathf.RoundToInt(Mathf.Sin(x * .013f + seed % 31) * 35f) + Mathf.RoundToInt(Mathf.Sin(x * .034f) * 13f);
                        DrawRect(pixels, width, height, x, y - 18, 3, 37, trailShade);
                        DrawRect(pixels, width, height, x, y - 13, 2, 27, trail);
                    }
                }

                if (caveLayout)
                {
                    // Irregular cave walls frame the arena; the middle stays clear for combat.
                    for (var x = 0; x < width; x += 4)
                    {
                        var top = 8 + Mathf.RoundToInt(Mathf.PerlinNoise(x * .023f + ox, oy) * 22f);
                        var bottom = height - 8 - Mathf.RoundToInt(Mathf.PerlinNoise(x * .021f + oy, ox) * 24f);
                        DrawRect(pixels, width, height, x, 0, 4, top, Color32.Lerp(dark, primary, .26f));
                        DrawRect(pixels, width, height, x, top, 4, 3, Color32.Lerp(dark, secondary, .38f));
                        DrawRect(pixels, width, height, x, bottom, 4, height - bottom, Color32.Lerp(dark, primary, .3f));
                        DrawRect(pixels, width, height, x, bottom, 4, 3, Color32.Lerp(dark, secondary, .42f));
                    }
                    for (var i = 0; i < 18; i++)
                    {
                        var x = random.Next(12, width - 12);
                        var top = i % 2 == 0;
                        var y = top ? random.Next(16, 54) : random.Next(height - 58, height - 18);
                        var crystalH = random.Next(7, 18);
                        DrawRect(pixels, width, height, x - 4, y, 8, crystalH, dark);
                        DrawRect(pixels, width, height, x - 2, y + 2, 4, crystalH - 2, accent);
                        DrawRect(pixels, width, height, x - 1, y + 3, 2, crystalH - 5, light);
                    }
                }

                // Arena floors use a subtle stone grid and a large central duelling ring.
                if (arenaLayout)
                {
                    var grid = Color32.Lerp(dark, secondary, .42f);
                    for (var y = 18; y < height - 18; y += 32) DrawRect(pixels, width, height, 0, y, width, 1, grid);
                    for (var x = 18; x < width - 18; x += 32) DrawRect(pixels, width, height, x, 0, 1, height, grid);
                    DrawEllipse(pixels, width, height, width / 2, height / 2 + 1, 148, 72, trailShade, ringOnly: true, thickness: 12);
                    DrawEllipse(pixels, width, height, width / 2, height / 2 + 1, 125, 56, trail, ringOnly: true, thickness: 4);
                    DrawEllipse(pixels, width, height, width / 2, height / 2 + 1, 108, 43, Color32.Lerp(primary, secondary, .54f), ringOnly: false);
                    for (var post = 0; post < 4; post++)
                    {
                        var x = post % 2 == 0 ? 70 : width - 76;
                        var y = post < 2 ? 35 : height - 55;
                        DrawRect(pixels, width, height, x, y, 8, 22, dark);
                        DrawRect(pixels, width, height, x + 2, y + 2, 3, 7, accent);
                    }
                }
                else if (wideBossLayout)
                {
                    DrawEllipse(pixels, width, height, width / 2, height / 2 + 3, 88, 52, trailShade, ringOnly: true, thickness: 10);
                    DrawEllipse(pixels, width, height, width / 2, height / 2 + 3, 68, 38, trail, ringOnly: true, thickness: 4);
                }

                // Rocks, shrubs, crystals and broken stone break up the open ground without hiding the fighters.
                var clusters = caveLayout ? 23 : arenaLayout ? 0 : 34;
                for (var i = 0; i < clusters; i++)
                {
                    var side = i % 3;
                    var x = side == 0 ? random.Next(10, width / 4) : side == 1 ? random.Next(width * 3 / 4, width - 16) : random.Next(12, width - 18);
                    var y = random.Next(16, height - 28);
                    var rw = random.Next(caveLayout ? 12 : 7, caveLayout ? 26 : 19);
                    var rh = random.Next(caveLayout ? 8 : 5, caveLayout ? 19 : 13);
                    if (caveLayout && Mathf.Abs(x - width / 2) < 116 && Mathf.Abs(y - height / 2) < 62) continue;
                    var shadow = Color32.Lerp(dark, new Color32(24, 29, 34, 255), .12f);
                    var stone = Color32.Lerp(secondary, light, caveLayout ? .48f : .36f);
                    DrawEllipse(pixels, width, height, x + rw / 2, y + rh / 2, rw / 2 + 3, rh / 2 + 3, shadow, ringOnly: false);
                    DrawEllipse(pixels, width, height, x + rw / 2, y + rh / 2 + 1, rw / 2, rh / 2, stone, ringOnly: false);
                    if (forestLayout)
                    {
                        DrawEllipse(pixels, width, height, x + rw / 2 - 3, y + rh / 2 - 2, Mathf.Max(2, rw / 5), Mathf.Max(2, rh / 5), light, ringOnly: false);
                        DrawRect(pixels, width, height, x + rw / 2, y + rh / 2, 2, 6, dark);
                    }
                    if (caveLayout && i % 3 == 0)
                    {
                        DrawRect(pixels, width, height, x + rw / 2 - 2, y + rh / 2 - 5, 4, 12, accent);
                        DrawRect(pixels, width, height, x + rw / 2 - 4, y + rh / 2 - 2, 8, 5, light);
                    }
                }

                var foliage = immortal ? Color32.Lerp(light, new Color32(115, 194, 193, 255), .3f) : Color32.Lerp(light, new Color32(112, 157, 78, 255), .38f);
                if (forestLayout)
                {
                    for (var i = 0; i < 38; i++)
                    {
                        var x = random.Next(18, width - 18); var y = random.Next(18, height - 18);
                        if (Mathf.Abs(x - width / 2) < 112 && Mathf.Abs(y - height / 2) < 58) continue;
                        var rx = random.Next(7, 13); var ry = random.Next(5, 10);
                        DrawEllipse(pixels, width, height, x + 1, y - 2, rx + 2, ry + 2, dark, ringOnly: false);
                        DrawEllipse(pixels, width, height, x, y, rx, ry, foliage, ringOnly: false);
                        DrawEllipse(pixels, width, height, x - 2, y + 2, Mathf.Max(2, rx / 3), Mathf.Max(2, ry / 3), light, ringOnly: false);
                        DrawRect(pixels, width, height, x, y - ry - 4, 2, 6, trailShade);
                    }
                }
                else
                {
                    for (var i = 0; i < 46; i++)
                    {
                        var x = random.Next(4, width - 4); var y = random.Next(4, height - 4);
                        if (arenaLayout && Mathf.Abs(x - width / 2) < 110 && Mathf.Abs(y - height / 2) < 54) continue;
                        var size = random.Next(2, 5);
                        DrawRect(pixels, width, height, x, y, 2, size, foliage);
                        DrawRect(pixels, width, height, x - 2, y + 1, 2, 2, Color32.Lerp(foliage, secondary, .4f));
                    }
                }

                // A light weather veil keeps palette colors readable while leaving the terrain crisp.
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "PixelBattleGround_" + (mapId ?? "field"), filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixels32(pixels); texture.Apply(false, true); TerrainCache[key] = texture;
                return texture;
            }
        }

        private static void DrawEllipse(Color32[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color32 color, bool ringOnly, int thickness = 2)
        {
            if (rx <= 0 || ry <= 0) return;
            var x0 = Mathf.Max(0, cx - rx - 1); var x1 = Mathf.Min(width - 1, cx + rx + 1);
            var y0 = Mathf.Max(0, cy - ry - 1); var y1 = Mathf.Min(height - 1, cy + ry + 1);
            var innerRx = Mathf.Max(1f, rx - thickness); var innerRy = Mathf.Max(1f, ry - thickness);
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                var dx = (x - cx) / (float)rx; var dy = (y - cy) / (float)ry;
                var distance = dx * dx + dy * dy;
                if (distance > 1f) continue;
                if (ringOnly)
                {
                    var ix = (x - cx) / innerRx; var iy = (y - cy) / innerRy;
                    if (ix * ix + iy * iy < 1f) continue;
                }
                pixels[y * width + x] = color;
            }
        }

        private static void DrawRect(Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
        {
            for (var py = y; py < y + h; py++)
            for (var px = x; px < x + w; px++) if (px >= 0 && px < width && py >= 0 && py < height) pixels[py * width + px] = color;
        }

        private Sprite[] GetPlayerFrames(AppearanceColors appearance, string variant)
        {
            var commissioned = PixelCreatureArt.Frames("player_cultivator");
            if (commissioned != null) return commissioned;
            var hair = ParsePixelColor(appearance?.hair, new Color32(45, 36, 37, 255));
            var robe = ParsePixelColor(appearance?.outfit, variant == "opponent" ? new Color32(76, 102, 152, 255) : new Color32(61, 121, 99, 255));
            var eyes = ParsePixelColor(appearance?.eyes, new Color32(70, 177, 165, 255));
            var key = $"player:{variant}:{robe.r}-{robe.g}-{robe.b}:{hair.r}-{hair.g}-{hair.b}";
            if (!FighterCache.TryGetValue(key, out var frames) || frames == null || frames.Length == 0 || frames[0] == null)
                FighterCache[key] = frames = MakePlayerFrames(robe, hair, eyes);
            return frames;
        }

        private Sprite[] GetBeastFrames(bool immortal, string monsterId, string monsterName, int variant)
        {
            var catalogFrames = PixelCreatureArt.Frames(monsterId, variant);
            if (catalogFrames != null) return catalogFrames;
            var name = (monsterName ?? string.Empty).ToLowerInvariant();
            var archetype = name.Contains("hồ") || name.Contains("ly") || name.Contains("fox") || name.Contains("cửu vĩ") ? "fox"
                : name.Contains("long") || name.Contains("rồng") || name.Contains("giao") ? "dragon"
                : name.Contains("phượng") || name.Contains("điểu") || name.Contains("chim") ? "bird"
                : "demon";
            var key = $"beast:{(immortal ? "tien" : "pham")}:{archetype}:{variant}";
            if (!FighterCache.TryGetValue(key, out var frames) || frames == null || frames.Length == 0 || frames[0] == null)
                FighterCache[key] = frames = MakeBeastFrames(immortal, archetype, variant);
            return frames;
        }

        private static Sprite[] MakePlayerFrames(Color32 robe, Color32 hair, Color32 eyes)
        {
            const int size = 32; var frames = new Sprite[4]; var trim = new Color32(209, 180, 112, 255); var skin = new Color32(225, 187, 151, 255);
            for (var frame = 0; frame < frames.Length; frame++)
            {
                var p = new Color32[size * size]; var bob = frame % 2;
                DrawRect(p, size, size, 12, 22 - bob, 9, 7, hair); DrawRect(p, size, size, 13, 19 - bob, 7, 5, skin);
                DrawRect(p, size, size, 14, 21 - bob, 1, 1, eyes); DrawRect(p, size, size, 18, 21 - bob, 1, 1, eyes);
                DrawRect(p, size, size, 10, 12 - bob, 13, 8, robe); DrawRect(p, size, size, 8, 8 - bob, 17, 6, robe);
                DrawRect(p, size, size, 13, 9 - bob, 6, 10, trim);
                var shift = frame == 1 ? -2 : frame == 3 ? 2 : 0;
                DrawRect(p, size, size, 11 + shift, 2, 4, 7, new Color32(41, 48, 48, 255)); DrawRect(p, size, size, 19 - shift, 2, 4, 7, new Color32(41, 48, 48, 255));
                DrawRect(p, size, size, 4, 13 - bob, 6, 4, robe); DrawRect(p, size, size, 21, 13 - bob, 5, 4, robe);
                frames[frame] = MakeSprite(p, size, size, "PixelCultivator_" + frame);
            }
            return frames;
        }

        private static Sprite[] MakeBeastFrames(bool immortal, string archetype, int variant)
        {
            const int size = 40; var frames = new Sprite[4];
            var fur = archetype == "dragon" ? new Color32(91, 175, 155, 255) : archetype == "bird" ? new Color32(221, 189, 112, 255)
                : archetype == "demon" ? new Color32(164, 103, 112, 255) : immortal ? new Color32(187, 184, 213, 255) : new Color32(220, 209, 184, 255);
            if (variant > 0) fur = Color32.Lerp(fur, new Color32(196, 111, 209, 255), Mathf.Clamp01(variant * 0.11f));
            var shade = archetype == "dragon" ? new Color32(48, 101, 101, 255) : archetype == "bird" ? new Color32(139, 93, 66, 255)
                : archetype == "demon" ? new Color32(79, 57, 82, 255) : immortal ? new Color32(108, 131, 181, 255) : new Color32(160, 143, 115, 255);
            var spirit = archetype == "dragon" ? new Color32(255, 124, 79, 255) : archetype == "bird" ? new Color32(255, 221, 127, 255)
                : archetype == "demon" ? new Color32(227, 91, 119, 255) : immortal ? new Color32(115, 219, 247, 255) : new Color32(230, 179, 81, 255);
            for (var frame = 0; frame < frames.Length; frame++)
            {
                var p = new Color32[size * size];
                if (archetype == "fox")
                {
                    for (var tail = 0; tail < 5; tail++)
                    {
                        var y = 9 + tail * 4 + ((frame + tail) % 3 - 1);
                        DrawRect(p, size, size, 1, y, 5, 3, shade); DrawRect(p, size, size, 5, y + tail % 2, 5, 3, fur);
                        DrawRect(p, size, size, 9, y + 1, 5, 2, tail % 2 == 0 ? fur : shade); DrawRect(p, size, size, 13, y + 1, 3, 2, spirit);
                    }
                }
                else if (archetype == "dragon")
                {
                    for (var segment = 0; segment < 5; segment++)
                    {
                        var y = 10 + segment * 3 + Mathf.RoundToInt(Mathf.Sin(frame + segment) * 2f);
                        DrawRect(p, size, size, 2 + segment * 4, y, 7, 5, segment % 2 == 0 ? fur : shade);
                        DrawRect(p, size, size, 4 + segment * 4, y - 2, 2, 2, spirit);
                    }
                    DrawRect(p, size, size, 25, 11, 10, 9, fur); DrawRect(p, size, size, 29, 8, 2, 4, spirit);
                    DrawRect(p, size, size, 34, 14, 5, 3, spirit); DrawRect(p, size, size, 29, 19, 2, 4, shade);
                }
                else if (archetype == "bird")
                {
                    var flap = frame % 2 == 0 ? -3 : 3;
                    for (var feather = 0; feather < 5; feather++)
                    {
                        DrawRect(p, size, size, 4 + feather * 2, 17 + flap - feather, 8, 3, feather % 2 == 0 ? fur : shade);
                        DrawRect(p, size, size, 19 + feather * 2, 17 + flap + feather, 8, 3, feather % 2 == 0 ? fur : shade);
                    }
                    DrawRect(p, size, size, 14, 14, 15, 13, shade); DrawRect(p, size, size, 18, 11, 13, 13, fur);
                    DrawRect(p, size, size, 29, 15, 7, 3, spirit); DrawRect(p, size, size, 23, 8, 3, 5, spirit);
                    DrawRect(p, size, size, 18, 27, 3, 7, shade); DrawRect(p, size, size, 26, 27, 3, 7, shade);
                }
                else
                {
                    DrawRect(p, size, size, 7, 17, 25, 13, shade); DrawRect(p, size, size, 11, 13, 23, 14, fur);
                    DrawRect(p, size, size, 14, 9, 5, 7, spirit); DrawRect(p, size, size, 27, 9, 5, 7, spirit);
                    DrawRect(p, size, size, 2, 20, 8, 5, fur); DrawRect(p, size, size, 29, 22, 8, 5, fur);
                    DrawRect(p, size, size, 11, 29, 4, 7, shade); DrawRect(p, size, size, 25, 29, 4, 7, shade);
                }
                if (archetype != "bird")
                {
                    DrawRect(p, size, size, 25, 25, 3, 6, shade); DrawRect(p, size, size, 31, 24, 3, 7, shade);
                    DrawRect(p, size, size, 25, 27, 2, 2, new Color32(255, 87, 64, 255)); DrawRect(p, size, size, 32, 27, 2, 2, new Color32(255, 87, 64, 255));
                    DrawRect(p, size, size, 27, 20, 2, 2, new Color32(255, 209, 111, 255)); DrawRect(p, size, size, 22, 15, 3, 5, shade);
                    DrawRect(p, size, size, 28, 15, 3, 5, shade); DrawRect(p, size, size, 34, 19, 3, 2, spirit);
                }
                frames[frame] = MakeSprite(p, size, size, "PixelSpiritBeast_" + archetype + "_" + variant + "_" + frame);
            }
            return frames;
        }


        private static Sprite MakeSprite(Color32[] pixels, int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.1f), width);
        }

        private static Sprite GetProjectileSprite()
        {
            if (projectileSprite != null) return projectileSprite;
            const int size = 12; var p = new Color32[size * size];
            DrawRect(p, size, size, 4, 1, 4, 10, new Color32(94, 198, 255, 255));
            DrawRect(p, size, size, 2, 3, 8, 6, new Color32(94, 198, 255, 255));
            DrawRect(p, size, size, 4, 4, 4, 4, new Color32(229, 250, 255, 255));
            projectileSprite = MakeSprite(p, size, size, "PixelCombatProjectile");
            return projectileSprite;
        }

        private static Sprite GetShadowSprite()
        {
            if (shadowSprite != null) return shadowSprite;
            const int width = 20, height = 8; var p = new Color32[width * height];
            for (var y = 1; y < height - 1; y++) for (var x = 1; x < width - 1; x++)
                if (Mathf.Pow((x - 9.5f) / 9f, 2f) + Mathf.Pow((y - 3.5f) / 3f, 2f) <= 1f) p[y * width + x] = new Color32(8, 14, 17, 220);
            shadowSprite = MakeSprite(p, width, height, "PixelCombatShadow");
            return shadowSprite;
        }

        private static Color32 ParsePixelColor(string value, Color32 fallback)
        {
            if (ColorUtility.TryParseHtmlString(string.IsNullOrEmpty(value) ? "" : value.StartsWith("#") ? value : "#" + value, out var parsed)) return parsed;
            return fallback;
        }

        private static string LastPveLog(BattleView battle) => battle.log == null || battle.log.Length == 0 ? null : battle.log[battle.log.Length - 1]?.text;
        private static string LastPvpLog(PvpBattle battle) => battle.log == null || battle.log.Length == 0 ? null : battle.log[battle.log.Length - 1]?.text;

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }
}
