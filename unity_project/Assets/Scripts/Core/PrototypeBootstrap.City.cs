using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// "Trong thành": a painted terraced city (one panorama per province biome) with brush-tag
    /// hotspots for every service — teleport, market, rankings, bounties, healer, crafting, sect...
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        [Serializable] private sealed class CityHotspot { public string id; public string label; public float x; public float y; public float w; public float h; }
        [Serializable] private sealed class CityLayout { public int w = 800; public int h = 360; public CityHotspot[] hotspots; }

        private GameObject cityRoot;
        private static CityLayout cityLayout;
        private static readonly Dictionary<string, Texture2D> CityPaintings = new Dictionary<string, Texture2D>();
        private string cityTownId;
        private PvpOpponent[] cityNearbyPlayers;
        private RectTransform cityPresencePanel;
        private RectTransform cityPresencePlayersRow;
        private RectTransform cityPresenceRoster;
        private Text cityPresenceStatus;
        private Text cityPresenceExpandLabel;
        private bool cityPresenceExpanded;
        private int cityPresenceCategory;
        private int cityPresenceRequest;

        private static readonly Dictionary<string, string> CityIcons = new Dictionary<string, string>
        {
            { "rank", "power" }, { "lord", "location" }, { "sect", "tong_mon" }, { "bounty", "scroll" }, { "craft", "dan_duoc" },
            { "codex", "thu_cac" }, { "heal", "y_quan" }, { "tavern", "ban_be" }, { "shop", "coin" }, { "teleport", "teleport" },
            { "market", "phuong_thi" }, { "inbox", "mail" }, { "pvp", "swords" }, { "exit", "road" },
        };

        private Texture2D CityPainting(string biome)
        {
            if (CityPaintings.TryGetValue(biome, out var cached) && cached != null) return cached;
            var asset = Resources.Load<TextAsset>("World/city_" + biome) ?? Resources.Load<TextAsset>("World/city_verdant");
            if (asset == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!texture.LoadImage(asset.bytes, true)) return null;
            texture.filterMode = FilterMode.Point;
            CityPaintings[biome] = texture;
            return texture;
        }

        private void ShowCity(string townId)
        {
            if (hub.IsNull) { RefreshHub(() => ShowCity(townId)); return; }
            if (cityLayout == null)
            {
                var asset = Resources.Load<TextAsset>("World/city_layout");
                cityLayout = asset != null ? JsonUtility.FromJson<CityLayout>(asset.text) : new CityLayout { hotspots = Array.Empty<CityHotspot>() };
            }
            var town = hub["town"];
            var mapId = town["mapId"].Str();
            var data = WorldMapData.Load(mapId);
            var biome = data?.biome ?? "verdant";
            var painting = CityPainting(biome);
            if (worldView != null) { Destroy(worldView.gameObject); worldView = null; }
            if (cityRoot != null) { Destroy(cityRoot); cityRoot = null; }
            HideHudAction();
            if (wayfinderRoot != null) { Destroy(wayfinderRoot.gameObject); wayfinderRoot = null; }
            wayfinders.Clear();
            ClearContent();
            ClearBattleScene();
            cityTownId = townId;
            var presenceRequest = ++cityPresenceRequest;
            cityPresenceExpanded = false;
            cityPresenceCategory = 0;
            if (!offlinePreview) cityNearbyPlayers = null;
            else if (cityNearbyPlayers == null) cityNearbyPlayers = Array.Empty<PvpOpponent>();
            // panorama under the safe-area UI
            var root = new GameObject("CityPanorama", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            root.SetParent(backgroundRoot, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
            root.GetComponent<Image>().color = new Color32(232, 226, 214, 255);
            cityRoot = root.gameObject;
            var view = new GameObject("Painting", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
            view.SetParent(root, false);
            view.anchorMin = view.anchorMax = new Vector2(.5f, .5f);
            view.pivot = new Vector2(.5f, .5f);
            var raw = view.GetComponent<RawImage>();
            raw.texture = painting;
            raw.raycastTarget = false;
            var pan = root.gameObject.AddComponent<CityPan>();
            pan.View = view;
            pan.Aspect = cityLayout.w / (float)Mathf.Max(1, cityLayout.h);
            pan.Refit();
            // drifting mist over the city
            for (var i = 0; i < 6; i++)
            {
                var cloud = InkUi.Simple(view, "Mist" + i, InkUi.Cloud, new Color(1, 1, 1, .22f + i % 3 * .06f), new Vector2(140 + i * 40, 60 + i * 12));
                cloud.rectTransform.anchorMin = cloud.rectTransform.anchorMax = new Vector2((i * .19f) % 1f, .25f + (i % 3) * .2f);
                cloud.gameObject.AddComponent<UiDrift>().speed = 6f + i * 2.5f;
            }
            foreach (var spot in cityLayout.hotspots ?? Array.Empty<CityHotspot>()) AddCityHotspot(view, spot);
            try { AddCityNpcs(view, townId); }
            catch (Exception ex) { Debug.LogException(ex); }
            // HUD
            var hud = HudRoot();
            BuildOverlays();
            BuildAvatarCard(hud);
            var cityAvatar = hud.Find("Avatar") as RectTransform;
            if (cityAvatar != null) cityAvatar.anchoredPosition += new Vector2(0f, 120f);
            var title = Anchored("CityTitle", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -116), new Vector2(350, -14));
            ModernSurface(title, new Color32(14, 20, 26, 225), 14f, new Color32(232, 196, 120, 150));
            var townName = Clean(town["name"].Str());
            if (townId != town["id"].Str()) townName = Clean(data?.Town(townId)?.name ?? townName);
            var heading = AnchoredText(title, "Name", townName, ModernUi.Display, 29, HudCream, TextAnchor.MiddleLeft,
                new Vector2(0, .46f), Vector2.one, new Vector2(18, 0), new Vector2(-12, -8));
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 20;
            heading.resizeTextMaxSize = 29;
            heading.horizontalOverflow = HorizontalWrapMode.Overflow;
            heading.verticalOverflow = VerticalWrapMode.Truncate;
            UiGradient.Apply(heading, AuthGoldTop, AuthGoldBottom);
            var lordLine = $"{Clean(town["lordTitle"].Str("Thành chủ"))} {Clean(town["lordName"].Str("—"))} · {Clean(town["realmMinName"].Str("Phàm Nhân"))}";
            AnchoredText(title, "Lord", lordLine, ModernUi.Regular, 14, new Color32(218, 224, 226, 255), TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(1, .48f), new Vector2(18, 3), new Vector2(-12, 0));

            BuildCityPresence(hud, presenceRequest);

            // Clear bottom quick dock for all essential city services
            BuildCityQuickDock(hud);
        }

        private void BuildCityPresence(RectTransform hud, int request)
        {
            var bar = Anchored("CityPresence", hud, new Vector2(.29f, 1f), new Vector2(1f, 1f), new Vector2(0f, -118f), new Vector2(-18f, -12f));
            ModernSurface(bar, new Color32(14, 20, 26, 230), 13f, new Color32(232, 196, 120, 130));

            var heading = AnchoredText(bar, "Heading", "NGƯỜI CHƠI", ModernUi.SemiBold, 17, HudGold,
                TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(.19f, 1f), new Vector2(6f, 0f), new Vector2(-4f, 0f));
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 12;
            heading.resizeTextMaxSize = 17;
            heading.raycastTarget = false;

            var viewport = Anchored("PlayersViewport", bar, new Vector2(.19f, .06f), new Vector2(.83f, .94f), Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, .015f);
            var row = Anchored("Players", viewport, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            cityPresencePlayersRow = row;
            row.pivot = new Vector2(0f, .5f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(3, 3, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = bar.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = row;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 28f;

            var expand = Anchored("ExpandRoster", bar, new Vector2(.835f, 0f), Vector2.one, new Vector2(3f, 4f), new Vector2(-4f, -4f));
            var expandFill = ModernSurface(expand, new Color32(42, 51, 57, 255), 10f, new Color32(232, 196, 120, 180), true);
            var expandButton = expand.gameObject.AddComponent<Button>();
            expandButton.targetGraphic = expandFill;
            expandButton.onClick.AddListener(ToggleCityPresence);
            cityPresenceExpandLabel = AnchoredText(expand, "Label", "MỞ RỘNG  ▾", ModernUi.SemiBold, 14, Cream, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(3f, 0f), new Vector2(-3f, 0f));
            cityPresenceExpandLabel.raycastTarget = false;

            BuildCityPlayerCards(row);
            BuildCityRosterPanel(hud);
            if (!offlinePreview && client != null) LoadCityNearbyPlayers(request);
        }

        private void BuildCityPlayerCards(RectTransform row)
        {
            for (var i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
            if (cityNearbyPlayers == null || cityNearbyPlayers.Length == 0)
            {
                var empty = new GameObject("NoNearbyPlayers", typeof(RectTransform), typeof(LayoutElement), typeof(Text));
                empty.transform.SetParent(row, false);
                empty.GetComponent<LayoutElement>().preferredWidth = 370f;
                empty.GetComponent<LayoutElement>().preferredHeight = 72f;
                var text = empty.GetComponent<Text>();
                text.font = BuiltinFont();
                text.fontSize = 14;
                text.color = new Color32(198, 208, 214, 255);
                text.alignment = TextAnchor.MiddleLeft;
                text.text = cityNearbyPlayers == null ? "Đang tải đạo hữu cùng thành…" : "Chưa có đạo hữu nào cùng thành.";
                text.raycastTarget = false;
                return;
            }

            for (var i = 0; i < cityNearbyPlayers.Length; i++)
            {
                var player = cityNearbyPlayers[i];
                if (player == null || string.IsNullOrEmpty(player.userId)) continue;
                var card = new GameObject("Player_" + player.userId, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
                card.transform.SetParent(row, false);
                card.GetComponent<LayoutElement>().preferredWidth = 102f;
                card.GetComponent<LayoutElement>().preferredHeight = 88f;
                var fill = card.GetComponent<Image>();
                fill.color = new Color32(31, 40, 47, 255);
                var button = card.GetComponent<Button>();
                button.targetGraphic = fill;
                var target = player.userId;
                button.onClick.AddListener(() => Confirm("Quyết đấu", "Mời " + Clean(player.name) + " lên lôi đài?", "Quyết đấu", () => StartPvp(target)));

                var look = HeroSprites.RandomLook(player.userId, i % 2 == 1);
                var portraitTexture = AvatarComposer.Available ? AvatarComposer.Compose(look) : null;
                if (portraitTexture != null)
                {
                    var portrait = Anchored("Portrait", card.transform, new Vector2(.25f, .34f), new Vector2(.75f, .96f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
                    portrait.texture = portraitTexture;
                    portrait.uvRect = new Rect(.28f, .69f, .44f, .2625f);
                    portrait.raycastTarget = false;
                }
                else
                {
                    var frames = HeroSprites.Get(look);
                    if (frames != null && frames.Length > 0)
                    {
                        var portrait = Anchored("Portrait", card.transform, new Vector2(.25f, .34f), new Vector2(.75f, .96f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                        portrait.sprite = frames[0];
                        portrait.preserveAspect = true;
                        portrait.raycastTarget = false;
                    }
                }
                var name = AnchoredText(card.GetComponent<RectTransform>(), "Name", Clean(player.name), ModernUi.SemiBold, 15,
                    Cream, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, .34f), new Vector2(3f, 1f), new Vector2(-3f, -1f));
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 11;
                name.resizeTextMaxSize = 15;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.raycastTarget = false;
            }
        }

        private void BuildCityRosterPanel(RectTransform hud)
        {
            cityPresencePanel = Anchored("CityRosterPanel", hud, new Vector2(.53f, 1f), new Vector2(.985f, 1f), new Vector2(0f, -484f), new Vector2(0f, -128f));
            var fill = ModernSurface(cityPresencePanel, new Color32(13, 19, 26, 248), 15f, new Color32(232, 196, 120, 210));
            fill.raycastTarget = true;
            cityPresencePanel.gameObject.SetActive(false);

            var title = AnchoredText(cityPresencePanel, "Title", "NGƯỜI TRONG THÀNH", ModernUi.SemiBold, 16, HudGold,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(.47f, 1f), new Vector2(17f, -48f), new Vector2(-5f, -8f));
            title.raycastTarget = false;
            AddCityPresenceTab("PlayersTab", "Người Chơi", 0, new Vector2(.48f, 1f), new Vector2(.735f, 1f),
                new Vector2(4f, -45f), new Vector2(-5f, -7f));
            AddCityPresenceTab("NpcTab", "NPC", 1, new Vector2(.745f, 1f), new Vector2(.99f, 1f),
                new Vector2(3f, -45f), new Vector2(-5f, -7f));

            var viewport = Anchored("RosterViewport", cityPresencePanel, Vector2.zero, Vector2.one, new Vector2(12f, 11f), new Vector2(-12f, -54f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var image = viewport.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, .012f);
            cityPresenceStatus = AnchoredText(viewport, "Empty", "", ModernUi.Regular, 15, new Color32(200, 209, 216, 255), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            cityPresenceStatus.raycastTarget = false;
            cityPresenceRoster = Anchored("Rows", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            cityPresenceRoster.pivot = new Vector2(.5f, 1f);
            var layout = cityPresenceRoster.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            cityPresenceRoster.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = cityPresencePanel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = cityPresenceRoster;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            SelectCityPresenceCategory(cityPresenceCategory);
        }

        private void AddCityPresenceTab(string name, string label, int category, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var tab = Anchored(name, cityPresencePanel, min, max, offMin, offMax);
            var fill = ModernSurface(tab, new Color32(39, 48, 55, 255), 9f, new Color32(126, 140, 148, 130), true);
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => SelectCityPresenceCategory(category));
            var text = AnchoredText(tab, "Label", label, ModernUi.SemiBold, 14, Cream, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(2f, 0f), new Vector2(-2f, 0f));
            text.raycastTarget = false;
        }

        private void LoadCityNearbyPlayers(int request)
        {
            client.LoadPvp((data, error) =>
            {
                if (request != cityPresenceRequest || cityRoot == null) return;
                cityNearbyPlayers = data?.sameTownPlayers ?? Array.Empty<PvpOpponent>();
                if (cityPresencePlayersRow != null) BuildCityPlayerCards(cityPresencePlayersRow);
                RebuildCityPresenceList();
            });
        }

        private void ToggleCityPresence()
        {
            cityPresenceExpanded = !cityPresenceExpanded;
            if (cityPresencePanel != null) cityPresencePanel.gameObject.SetActive(cityPresenceExpanded);
            if (cityPresenceExpandLabel != null) cityPresenceExpandLabel.text = cityPresenceExpanded ? "THU GỌN  ▴" : "MỞ RỘNG  ▾";
        }

        private void SelectCityPresenceCategory(int category)
        {
            cityPresenceCategory = category;
            var header = cityPresencePanel != null ? cityPresencePanel.Find("Title")?.GetComponent<Text>() : null;
            if (header != null) header.text = category == 0 ? "NGƯỜI CHƠI CÙNG THÀNH" : "NPC TRONG THÀNH";
            var playerTab = cityPresencePanel != null ? cityPresencePanel.Find("PlayersTab")?.GetComponent<Image>() : null;
            var npcTab = cityPresencePanel != null ? cityPresencePanel.Find("NpcTab")?.GetComponent<Image>() : null;
            if (playerTab != null) playerTab.color = category == 0 ? new Color32(77, 62, 39, 255) : new Color32(39, 48, 55, 255);
            if (npcTab != null) npcTab.color = category == 1 ? new Color32(77, 62, 39, 255) : new Color32(39, 48, 55, 255);
            RebuildCityPresenceList();
        }

        private void RebuildCityPresenceList()
        {
            if (cityPresenceRoster == null || cityPresenceStatus == null) return;
            for (var i = cityPresenceRoster.childCount - 1; i >= 0; i--)
            {
                var child = cityPresenceRoster.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            if (cityPresenceCategory == 0)
            {
                var players = cityNearbyPlayers ?? Array.Empty<PvpOpponent>();
                for (var i = 0; i < players.Length; i++) AddCityRosterPlayer(players[i]);
                cityPresenceStatus.text = cityNearbyPlayers == null ? "Đang tải danh sách…" : players.Length == 0 ? "Chưa có người chơi cùng thành." : "";
            }
            else
            {
                var picks = new List<(int score, J npc)>();
                var realm = hub["town"]["realmMin"].Int();
                foreach (var npc in hub["npcs"].Items)
                {
                    if (string.IsNullOrEmpty(npc["id"].Str()) || npc["isDead"].Bool()) continue;
                    var gap = Mathf.Abs(npc["realm"].Int() - (realm + 2));
                    picks.Add((gap * 1000 + Mathf.Abs(npc["id"].Str().GetHashCode() % 997), npc));
                }
                picks.Sort((a, b) => a.score.CompareTo(b.score));
                for (var i = 0; i < picks.Count; i++) AddCityRosterNpc(picks[i].npc);
                cityPresenceStatus.text = picks.Count == 0 ? "Hiện chưa có NPC để gặp." : "";
            }
            Canvas.ForceUpdateCanvases();
            if (cityPresenceRoster != null) cityPresenceRoster.anchoredPosition = new Vector2(0f, cityPresenceRoster.anchoredPosition.y);
        }

        private void AddCityRosterPlayer(PvpOpponent player)
        {
            if (player == null || string.IsNullOrEmpty(player.userId)) return;
            var row = new GameObject("NearbyPlayer_" + player.userId, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
            row.transform.SetParent(cityPresenceRoster, false);
            row.GetComponent<LayoutElement>().preferredHeight = 68f;
            var fill = row.GetComponent<Image>();
            fill.color = new Color32(31, 40, 47, 255);
            var button = row.GetComponent<Button>();
            button.targetGraphic = fill;
            var target = player.userId;
            button.onClick.AddListener(() => Confirm("Quyết đấu", "Mời " + Clean(player.name) + " lên lôi đài?", "Quyết đấu", () => StartPvp(target)));
            var name = AnchoredText(row.GetComponent<RectTransform>(), "Name", Clean(player.fullName ?? player.name), ModernUi.SemiBold, 15, Cream,
                TextAnchor.MiddleLeft, new Vector2(0f, .46f), Vector2.one, new Vector2(14f, 2f), new Vector2(-150f, -3f));
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;
            AnchoredText(row.GetComponent<RectTransform>(), "Realm", Clean(player.realmName) + " · " + Vn(player.power) + " chiến lực", ModernUi.Regular, 12,
                new Color32(181, 195, 203, 255), TextAnchor.MiddleLeft, Vector2.zero, new Vector2(1f, .46f), new Vector2(14f, 0f), new Vector2(-150f, 0f)).raycastTarget = false;
            var duel = Anchored("Duel", row.GetComponent<RectTransform>(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-126f, -22f), new Vector2(-12f, 22f));
            var duelFill = ModernSurface(duel, new Color32(111, 74, 37, 255), 8f, new Color32(232, 196, 120, 210), true);
            var duelButton = duel.gameObject.AddComponent<Button>();
            duelButton.targetGraphic = duelFill;
            duelButton.onClick.AddListener(() => Confirm("Quyết đấu", "Mời " + Clean(player.name) + " lên lôi đài?", "Quyết đấu", () => StartPvp(target)));
            AnchoredText(duel, "Label", "MỜI ĐẤU", ModernUi.SemiBold, 12, Cream, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;
        }

        private void AddCityRosterNpc(J npc)
        {
            var id = npc["id"].Str();
            var row = new GameObject("CityNpc_" + id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
            row.transform.SetParent(cityPresenceRoster, false);
            row.GetComponent<LayoutElement>().preferredHeight = 68f;
            var fill = row.GetComponent<Image>();
            fill.color = new Color32(31, 40, 47, 255);
            var button = row.GetComponent<Button>();
            button.targetGraphic = fill;
            var target = npc;
            button.onClick.AddListener(() => OpenNpcDialog(target));
            var name = AnchoredText(row.GetComponent<RectTransform>(), "Name", Clean(npc["name"].Str()) + " · " + Clean(npc["title"].Str()),
                ModernUi.SemiBold, 15, Cream, TextAnchor.MiddleLeft, new Vector2(0f, .46f), Vector2.one, new Vector2(14f, 2f), new Vector2(-130f, -3f));
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;
            AnchoredText(row.GetComponent<RectTransform>(), "Realm", Clean(npc["realmName"].Str()) + " · " + Vn(npc["power"]) + " chiến lực",
                ModernUi.Regular, 12, new Color32(181, 195, 203, 255), TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(1f, .46f), new Vector2(14f, 0f), new Vector2(-130f, 0f)).raycastTarget = false;
            var meet = Anchored("Meet", row.GetComponent<RectTransform>(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-112f, -22f), new Vector2(-12f, 22f));
            var meetFill = ModernSurface(meet, new Color32(39, 66, 57, 255), 8f, new Color32(122, 183, 143, 200), true);
            var meetButton = meet.gameObject.AddComponent<Button>();
            meetButton.targetGraphic = meetFill;
            meetButton.onClick.AddListener(() => OpenNpcDialog(target));
            AnchoredText(meet, "Label", "GẶP MẶT", ModernUi.SemiBold, 12, Cream, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;
        }

        private void BuildCityQuickDock(RectTransform hud)
        {
            var dock = Anchored("CityQuickDock", hud, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 16f), new Vector2(-24f, 114f));
            var bg = dock.gameObject.AddComponent<Image>();
            bg.color = Panel;
            PixelUiSkin.ApplyFrame(dock.gameObject);

            var services = new (string icon, string label, string id)[]
            {
                ("phuong_thi", "Phường Thị", "market"),
                ("thu_cac", "Tàng Kinh", "codex"),
                ("dan_duoc", "Luyện Đan", "craft"),
                ("y_quan", "Dược Quán", "heal"),
                ("swords", "Lôi Đài", "pvp"),
                ("tong_mon", "Tông Môn", "sect"),
                ("location", "Thành Chủ", "lord"),
                ("teleport", "Truyền Tống", "teleport"),
                ("road", "Rời thành", "exit"),
            };

            var count = services.Length;
            for (var i = 0; i < count; i++)
            {
                var s = services[i];
                var minX = (float)i / count;
                var maxX = (float)(i + 1) / count;
                var btnRect = Anchored("Dock_" + s.id, dock, new Vector2(minX, 0f), new Vector2(maxX, 1f), new Vector2(4f, 6f), new Vector2(-4f, -6f));
                var btnFill = btnRect.gameObject.AddComponent<Image>();
                var isExit = s.id == "exit";
                btnFill.color = isExit ? new Color32(52, 24, 24, 255) : Panel;
                PixelUiSkin.ApplyFrame(btnRect.gameObject);

                var iconRect = Anchored("Icon", btnRect, new Vector2(0f, .42f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -4f));
                var icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(s.icon);
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var label = AnchoredText(btnRect, "Label", s.label, ModernUi.SemiBold, 19,
                    isExit ? new Color32(255, 180, 170, 255) : Cream,
                    TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, .48f), new Vector2(2f, 2f), new Vector2(-2f, 0f));
                label.supportRichText = false;
                label.raycastTarget = false;
                PixelUiSkin.ApplyTextTreatment(label);

                var button = btnRect.gameObject.AddComponent<Button>();
                button.targetGraphic = btnFill;
                var btnColors = button.colors;
                btnColors.normalColor = Color.white;
                btnColors.highlightedColor = new Color(1f, .96f, .82f);
                btnColors.pressedColor = new Color(.76f, .83f, .79f);
                button.colors = btnColors;
                var targetId = s.id;
                button.onClick.AddListener(() => OpenCityService(targetId));
                btnRect.gameObject.AddComponent<UiPressScale>();
            }
        }

        private void AddCityHotspot(RectTransform view, CityHotspot spot)
        {
            var w = cityLayout.w;
            var h = cityLayout.h;
            var cx = (spot.x + spot.w * .5f) / w;
            var cy = 1f - (spot.y + spot.h * .5f) / h;
            var id = spot.id;

            // Invisible hit box covering the building area so tapping the building directly opens its service
            var hitArea = Anchored("Hit_" + spot.id, view,
                new Vector2(spot.x / w, 1f - (spot.y + spot.h) / h),
                new Vector2((spot.x + spot.w) / w, 1f - spot.y / h),
                Vector2.zero, Vector2.zero);
            var hitImg = hitArea.gameObject.AddComponent<Image>();
            hitImg.color = new Color(0, 0, 0, 0);
            var hitBtn = hitArea.gameObject.AddComponent<Button>();
            hitBtn.transition = Selectable.Transition.None;
            hitBtn.targetGraphic = hitImg;
            hitBtn.onClick.AddListener(() => OpenCityService(id));

            // Elegant compact medallion with icon only (replaces clutter of giant black rectangular textboxes)
            const float badgeSize = 38f;
            var badge = Anchored("Spot_" + spot.id, view, new Vector2(cx, cy), new Vector2(cx, cy),
                new Vector2(-badgeSize * .5f, -badgeSize * .5f), new Vector2(badgeSize * .5f, badgeSize * .5f));

            var fill = badge.gameObject.AddComponent<Image>();
            fill.sprite = InkUi.Glow;
            fill.color = new Color32(14, 22, 30, 230);

            var border = Anchored("Border", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            border.sprite = InkUi.Ring;
            border.color = new Color32(235, 196, 120, 200);
            border.raycastTarget = false;

            var glow = Anchored("Glow", badge, new Vector2(-.2f, -.2f), new Vector2(1.2f, 1.2f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            glow.sprite = InkUi.Glow;
            glow.color = new Color(1f, .92f, .7f, 0f);
            glow.raycastTarget = false;

            if (CityIcons.TryGetValue(spot.id, out var iconId))
            {
                var icon = Anchored("Icon", badge, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-13f, -13f), new Vector2(13f, 13f)).gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(iconId);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            var button = badge.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = fill;
            button.onClick.AddListener(() => OpenCityService(id));

            var trigger = badge.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => glow.color = new Color(1f, .92f, .7f, .45f));
            trigger.triggers.Add(down);
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => glow.color = new Color(1f, .92f, .7f, 0f));
            trigger.triggers.Add(up);

            badge.gameObject.AddComponent<UiPressScale>();
        }

        /// <summary>Feet positions (layout units) on the terraces, beside the buildings.</summary>
        private static readonly Vector2[] CityNpcSpots =
        {
            new Vector2(245, 125), new Vector2(607, 235), new Vector2(318, 186), new Vector2(405, 221), new Vector2(208, 261), new Vector2(505, 149),
        };

        /// <summary>Cultivators are met inside the cities: a few stand on the terraces; tap one to talk or spar.</summary>
        private void AddCityNpcs(RectTransform view, string townId)
        {
            if (!AvatarComposer.Available || cityLayout == null) return;
            var realm = hub["town"]["realmMin"].Int();
            var picks = new List<(int score, J npc)>();
            foreach (var npc in hub["npcs"].Items)
            {
                var id = npc["id"].Str();
                if (string.IsNullOrEmpty(id) || npc["isDead"].Bool()) continue;
                // residents near the city's own realm, and the same faces on every visit
                var gap = Mathf.Abs(npc["realm"].Int() - (realm + 2));
                picks.Add((gap * 1000 + Mathf.Abs((id + townId).GetHashCode() % 997), npc));
            }
            picks.Sort((a, b) => a.score.CompareTo(b.score));
            float w = cityLayout.w, h = cityLayout.h;
            const float frameW = 36f, frameH = 41f;
            for (var i = 0; i < CityNpcSpots.Length && i < picks.Count; i++)
            {
                var npc = picks[i].npc;
                var frames = HeroSprites.Get(HeroSprites.RandomLook(npc["id"].Str(), npc["gender"].Str() == "nu"));
                if (frames == null || frames.Length == 0) continue;
                var spot = CityNpcSpots[i];
                var rect = Anchored("Npc_" + npc["id"].Str(), view,
                    new Vector2((spot.x - frameW / 2) / w, 1f - spot.y / h), new Vector2((spot.x + frameW / 2) / w, 1f - (spot.y - frameH) / h), Vector2.zero, Vector2.zero);
                var hit = rect.gameObject.AddComponent<Image>();
                hit.color = new Color(1, 1, 1, 0);
                var body = Anchored("Body", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                body.sprite = frames[Mathf.Min(frames.Length - 1, HeroSprites.FrameIndex(false, 0f))];
                body.preserveAspect = true;
                body.raycastTarget = false;
                if (i % 2 == 1) body.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                var idle = body.gameObject.AddComponent<FigureIdle>();
                idle.Frames = frames;
                idle.Offset = i * .37f;
                var tag = InkUi.Tag(rect, Clean(npc["name"].Str()) + " · " + Clean(npc["realmName"].Str()), 17, new Color32(190, 226, 255, 255), 26f);
                tag.anchorMin = tag.anchorMax = new Vector2(.5f, 1f);
                tag.pivot = new Vector2(.5f, 0f);
                tag.anchoredPosition = new Vector2(0, 2);
                var button = rect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                var target = npc;
                button.onClick.AddListener(() => OpenNpcDialog(target));
            }
        }

        private void OpenCityService(string id)
        {
            switch (id)
            {
                case "exit": LeaveCity(); break;
                case "teleport": OpenTeleportScreen(false); break;
                case "market": OpenMarketScreen(); break;
                case "shop": OpenShopScreen(); break;
                case "rank": OpenRankScreen(); break;
                case "bounty": OpenBountyScreen(); break;
                case "heal": OpenHealScreen(); break;
                case "craft": OpenCraftScreen(); break;
                case "codex": OpenCodexScreen(); break;
                case "sect": OpenSectScreen(); break;
                case "tavern": OpenSocialScreen(); break;
                case "inbox": OpenInboxScreen(); break;
                case "pvp": OpenPvpScreen(); break;
                case "lord": OpenLordScreen(); break;
                default: Toast("Khu vực đang được tu sửa.", true); break;
            }
        }

        /// <summary>Back from a city service screen.</summary>
        private void BackToCity()
        {
            if (!string.IsNullOrEmpty(cityTownId)) ShowCity(cityTownId); else ShowWorld();
        }

        private void LeaveCity()
        {
            if (cityRoot != null) { Destroy(cityRoot); cityRoot = null; }
            var data = WorldMapData.Load(hub["town"]["mapId"].Str());
            var town = data?.Town(cityTownId);
            cityTownId = null;
            if (town?.spawn != null) worldReturnTile = new Vector2(town.spawn[0], town.spawn[1]);
            ShowWorld();
        }
    }

    /// <summary>Keeps the city panorama covering the screen height and lets the player pan sideways.</summary>
    internal sealed class CityPan : MonoBehaviour, IDragHandler
    {
        public RectTransform View;
        public float Aspect = 800f / 360f;
        private float offset;
        private Vector2 lastSize;

        public void Refit()
        {
            var root = (RectTransform)transform;
            var size = root.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            var height = size.y;
            var width = height * Aspect;
            if (width < size.x) { width = size.x; height = width / Aspect; }
            View.sizeDelta = new Vector2(width, height);
            var limit = Mathf.Max(0, (width - size.x) * .5f);
            offset = Mathf.Clamp(offset, -limit, limit);
            View.anchoredPosition = new Vector2(offset, 0);
            lastSize = size;
        }

        private void Update()
        {
            var size = ((RectTransform)transform).rect.size;
            if (size != lastSize) Refit();
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = transform.lossyScale.x <= 0 ? 1f : transform.lossyScale.x;
            offset += eventData.delta.x / scale;
            Refit();
        }
    }

    /// <summary>A standing figure that breathes: cycles the idle frames of a hero sheet.</summary>
    internal sealed class FigureIdle : MonoBehaviour
    {
        public Sprite[] Frames;
        public float Offset;
        private Image image;

        private void Update()
        {
            if (image == null) image = GetComponent<Image>();
            if (image == null || Frames == null || Frames.Length < HeroSprites.Total) return;
            image.sprite = Frames[HeroSprites.FrameIndex(false, Time.time + Offset)];
        }
    }

    /// <summary>Slow horizontal drift (mist, clouds) within the parent's width.</summary>
    internal sealed class UiDrift : MonoBehaviour
    {
        public float speed = 8f;
        private void Update()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            var p = rect.anchoredPosition;
            p.x += speed * Time.deltaTime;
            if (p.x > parent.rect.width * .6f) p.x = -parent.rect.width * .6f;
            rect.anchoredPosition = p;
        }
    }
}
