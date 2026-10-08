using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// "Ngoài thành": the painted province map with the player character, roaming monsters and
    /// world bosses, cities, secret realms (bí cảnh) and the main HUD (Quỷ Cốc Bát Hoang layout).
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private ProvinceWorld worldView;
        private WorldMapData worldData;
        private string worldMapId;
        private string activePaintingId;
        private static readonly Dictionary<string, Texture2D> PaintingCache = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, WorldActor> worldMonsterActors = new Dictionary<string, WorldActor>();
        private float nextWorldSave;
        private float nextMonsterRefresh;
        private Vector2Int lastSavedTile = new Vector2Int(-1, -1);
        private Vector2? worldReturnTile;
        private Text hudLocation;
        private Text hudPhase;
        private RectTransform hudActionRect;
        private Text hudActionLabel;
        private Action hudActionCallback;
        private RectTransform miniPlayerDot;
        private RectTransform miniMapRect;
        private RectTransform miniViewport, miniMapFrame, miniMapRoot;
        private Rect miniTileBounds;
        private string miniRegionId;
        private WorldHudTicker hudTicker;
        private bool hudActionAuto;
        private WorldPoi promptPoi;
        private bool worldCityEntryPending;
        private string worldActorEngagedId;
        private string worldActorConfirmId;
        private TravelMode worldTravel;
        private Text travelLabel;
        /// <summary>Bumped when coordinate space changes so province-local saves are not mistaken for world coordinates.</summary>
        private const int WorldLayoutRev = 3;

        private static readonly Color32 HudInk = new Color32(22, 24, 26, 230);
        private static readonly Color32 HudGold = new Color32(232, 196, 120, 255);
        private static readonly Color32 HudCream = new Color32(244, 236, 220, 255);

        private Texture2D GetPainting(string mapId)
        {
            if (PaintingCache.TryGetValue(mapId, out var cached) && cached != null) return cached;
            var texture = WorldMapData.LoadPainting(mapId);
            if (texture != null)
            {
                // keep at most two provinces decoded
                if (PaintingCache.Count >= 2)
                {
                    foreach (var key in new List<string>(PaintingCache.Keys))
                        if (key != activePaintingId && PaintingCache[key] != null) { Destroy(PaintingCache[key]); PaintingCache.Remove(key); break; }
                }
                PaintingCache[mapId] = texture;
            }
            return texture;
        }

        /// <summary>Main game screen. Loads the player's complete realm and places them in their province.</summary>
        private void ShowWorld()
        {
            if (hub.IsNull) { RefreshHub(ShowWorld); return; }
            var town = hub["town"];
            var mapId = town["mapId"].Str("map_1");
            if (string.IsNullOrEmpty(mapId)) mapId = "map_1";
            var data = WorldMapData.LoadWorldForProvince(mapId);
            activePaintingId = data?.id;
            var painting = data == null ? null : GetPainting(data.id);
            if (data == null || painting == null)
            {
                if (latestState != null) { ShowHome(latestState); }
                else { ShowWorldRecoveryScreen("Thiếu dữ liệu bản đồ " + mapId + " trong bản cài."); }
                Toast("Thiếu dữ liệu bản đồ " + mapId + " trong bản cài.", true);
                return;
            }
            ApplyRegionThresholds(data);
            SetAtlasOrientation(false);
            ClearContent();
            ClearBattleScene();
            if (cityRoot != null) { cityRoot.SetActive(false); Destroy(cityRoot); cityRoot = null; }
            worldData = data;
            worldMapId = mapId;
            worldCityEntryPending = false;
            worldActorEngagedId = null;
            worldActorConfirmId = null;
            worldMonsterActors.Clear();
            if (worldView != null) { worldView.gameObject.SetActive(false); Destroy(worldView.gameObject); worldView = null; }
            worldView = ProvinceWorld.Build(backgroundRoot, data, painting);
            worldView.transform.SetAsFirstSibling();
            var player = hub["player"];
            var spawn = ResolveWorldSpawn(data, player, town["id"].Str());
            var me = worldView.AddActor("me", "player", spawn, HeroFramesFor(player), null, HeroSize,
                Clean(player["name"].Str("Đạo hữu")), new Color32(255, 240, 200, 255));
            worldView.WalkSpeed = 3.2f;
            me.Speed = worldView.WalkSpeed;
            worldView.Player = me;
            worldView.Teleport(me, spawn);
            lastSavedTile = worldView.TileOf(spawn);
            worldView.OnGroundTap = cell =>
            {
                if (!worldView.WalkTo(cell, SaveWorldTile)) Toast("Không có đường tới đó.", true);
                HideHudAction();
            };
            worldView.OnPoiTap = HandleWorldPoi;
            worldView.OnActorTap = HandleWorldActor;
            worldView.OnPlayerStep = OnWorldStep;

            try
            {
                var myRealm = hub["realm"]["index"].Int();
                worldView.SetAura(me, LookOf(player), AvatarComposer.AuraStrength(myRealm));
                me.Pressure = myRealm >= 6;
                me.PressureColor = HeroSprites.ParseColor(LookOf(player).Get("auc", "#8fe0ff"), new Color32(140, 220, 255, 255));
            }
            catch (Exception ex) { Debug.LogWarning("SetAura error: " + ex.Message); }

            try { BuildWorldLabels(data); }
            catch (Exception ex) { Debug.LogWarning("BuildWorldLabels error: " + ex.Message); }

            try { SyncWorldMonsters(hub["worldMonsters"]); }
            catch (Exception ex) { Debug.LogWarning("SyncWorldMonsters error: " + ex.Message); }

            // cultivators (NPCs) are met inside the cities, not out on the map
            try
            {
                var phase = hub["timePhase"]["phase"].Str();
                worldView.SetNight(phase == "night" ? 1f : phase == "evening" ? .55f : phase == "dawn" ? .25f : 0f);
            }
            catch (Exception ex) { Debug.LogWarning("SetNight error: " + ex.Message); }

            promptPoi = null;

            try { BuildWorldHud(data); }
            catch (Exception ex) { Debug.LogWarning("BuildWorldHud error: " + ex.Message); }

            nextMonsterRefresh = Time.time + 15f;
            worldReturnTile = null;
            if (worldTravel != TravelMode.Walk) ApplyWorldTravel(true);
            try { UpdatePlacePrompt(worldView.TileOf(me.Pos)); }
            catch (Exception ex) { Debug.LogWarning("UpdatePlacePrompt error: " + ex.Message); }
        }

        private Vector2 ResolveWorldSpawn(WorldMapData data, J player, string townId)
        {
            if (worldReturnTile.HasValue)
            {
                var t = worldReturnTile.Value;
                var open = data.NearestOpen(new Vector2Int(Mathf.RoundToInt(t.x), Mathf.RoundToInt(t.y)), 40);
                if (!data.IsBlocked(open.x, open.y)) return open;
            }
            if (offlinePreview && PlayerPrefs.HasKey("tt_offline_world_x"))
            {
                var savedMap = PlayerPrefs.GetString("tt_offline_world_mapId", "");
                if (WorldIdForProvince(savedMap) == data.id && PlayerPrefs.GetInt("tt_world_layout", 1) == WorldLayoutRev)
                {
                    var cell = new Vector2Int(PlayerPrefs.GetInt("tt_offline_world_x"), PlayerPrefs.GetInt("tt_offline_world_y"));
                    if (data.InBounds(cell.x, cell.y) && !data.IsBlocked(cell.x, cell.y)) return cell;
                }
            }
            var pos = player["worldPosition"];
            if (pos.IsObject && WorldIdForProvince(pos["mapId"].Str()) == data.id && PlayerPrefs.GetInt("tt_world_layout", 1) == WorldLayoutRev)
            {
                var cell = new Vector2Int(pos["x"].Int(), pos["y"].Int());
                if (data.InBounds(cell.x, cell.y) && !data.IsBlocked(cell.x, cell.y)) return cell;
            }
            var town = data.Town(townId);
            if (town?.spawn != null && town.spawn.Length >= 2) return data.NearestOpen(new Vector2Int(town.spawn[0], town.spawn[1]));
            return data.NearestOpen(new Vector2Int(data.w / 2, data.h / 2), 20);
        }

        private static string WorldIdForProvince(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) || !mapId.StartsWith("map_", StringComparison.Ordinal)
                || !int.TryParse(mapId.Substring(4), out var number)) return null;
            return number >= 9 ? "world_tien" : "world_pham";
        }

        /// <summary>Keep the cultivator readable on an iPhone while cities still dominate the landscape.</summary>
        private static readonly Vector2 HeroSize = new Vector2(HeroSprites.FrameW * 2f, HeroSprites.FrameH * 2f);

        /// <summary>The player's layered look (creator look, or one derived from the legacy appearance).</summary>
        private static LookSpec LookOf(J player)
        {
            if (player.IsNull) return AvatarComposer.Default(false);
            try
            {
                // lookWorn = the creator look with the equipped weapon / armour applied by the server
                var text = player["lookWorn"].Str(player["look"].Str());
                if (!string.IsNullOrEmpty(text))
                {
                    var look = LookSpec.Parse(text);
                    return look.Fill(AvatarComposer.Default(look.Get("g", player["gender"].Str() == "nu" ? "f" : "m") == "f"));
                }
                var colors = player["appearanceColors"];
                var isFemale = player["gender"].Str() == "nu";
                var hair = colors.IsObject ? colors["hair"].Str() : "";
                var outfit = colors.IsObject ? colors["outfit"].Str() : "";
                var eyes = colors.IsObject ? colors["eyes"].Str() : "";
                return HeroSprites.LegacyLook(isFemale, hair, outfit, eyes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("LookOf fallback: " + ex.Message);
                return AvatarComposer.Default(false);
            }
        }

        private Sprite[] HeroFramesFor(J player)
        {
            try { return HeroSprites.Get(LookOf(player)); }
            catch (Exception ex)
            {
                Debug.LogWarning("HeroFramesFor fallback: " + ex.Message);
                return HeroSprites.Get(AvatarComposer.Default(false));
            }
        }

        // ------------------------------------------------------------------ labels

        private void BuildWorldLabels(WorldMapData data)
        {
            foreach (var poi in data.pois)
            {
                if (poi == null) continue;
                switch (poi.kind)
                {
                    case "city":
                        var town = data.Town(poi.townId);
                        var cityName = Clean(town?.name ?? poi.label);
                        var cityTag = PlaceTag(cityName, "location", 16, new Color32(255, 239, 192, 255));
                        worldView.AddLabel(cityTag, new Vector2(poi.x, poi.y - 5.2f), Vector2.zero, .72f);
                        break;
                    case "dungeon":
                        worldView.AddLabel(PlaceTag(poi.label, "co_dong", 18, new Color32(226, 206, 255, 255)), new Vector2(poi.x, poi.y - 3.6f), Vector2.zero, .88f);
                        break;
                    case "landmark":
                        if (!string.IsNullOrEmpty(poi.label)) worldView.AddLabel(InkUi.Tag(worldView.LabelLayer, poi.label, 16), new Vector2(poi.x, poi.y - 3.2f), Vector2.zero, 1f);
                        break;
                    case "province_gate":
                        worldView.AddLabel(PlaceTag("Cổng châu", "teleport", 15, new Color32(170, 226, 255, 255)), new Vector2(poi.x, poi.y - 3.5f), Vector2.zero, .78f);
                        break;
                    case "ascension_gate":
                        worldView.AddLabel(PlaceTag("CỔNG PHI THĂNG", "teleport", 19, new Color32(255, 232, 164, 255)), new Vector2(poi.x, poi.y - 4.2f), Vector2.zero, .64f);
                        break;
                }
            }
            foreach (var region in data.regions)
            {
                if (region == null) continue;
                var label = InkUi.Tag(worldView.LabelLayer, region.name, 18, new Color32(240, 232, 210, 190));
                worldView.AddLabel(label, new Vector2(region.x + region.w * .5f, region.y + region.h * .5f), Vector2.zero, .85f);
            }
            foreach (var zone in data.zones)
            {
                if (zone == null) continue;
                var tag = PlaceTag("Bãi yêu thú", "swords", 19, new Color32(255, 204, 180, 255));
                worldView.AddLabel(tag, new Vector2(zone.x + zone.w * .5f, zone.y - .4f), Vector2.zero, .88f);
            }
        }

        /// <summary>Map label with an icon in front: caves and hunting grounds must be easy to spot.</summary>
        private RectTransform PlaceTag(string text, string iconId, int fontSize, Color color)
        {
            var tag = InkUi.Tag(worldView.LabelLayer, Clean(text), fontSize, color, 46f);
            var icon = Anchored("Icon", tag, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, -17), new Vector2(44, 17)).gameObject.AddComponent<Image>();
            icon.sprite = UiPixelIcon(iconId);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (icon.sprite == null) icon.color = new Color(1, 1, 1, 0);
            return tag;
        }

        // ------------------------------------------------------------------ monsters & bosses

        private static bool IsBossMonster(J m) => m["isBoss"].Bool() || m["isWorldBoss"].Bool() || m["isBeastTideBoss"].Bool() || m["isInvasion"].Bool();

        private void SyncWorldMonsters(J list)
        {
            if (worldView == null) return;
            var seen = new HashSet<string>();
            foreach (var m in list.Items)
            {
                var uid = m["uid"].Str();
                if (string.IsNullOrEmpty(uid) || (m.Has("isSpawned") && !m["isSpawned"].Bool())) continue;
                if (m["townId"].Str() != hub["town"]["id"].Str() && !IsBossMonster(m)) continue;
                seen.Add(uid);
                if (worldMonsterActors.TryGetValue(uid, out var existing) && existing.Rect != null)
                {
                    existing.Data = m;
                    worldView.SetHp(existing, m["hp"].Num(), m["maxHp"].Num(1));
                    continue;
                }
                var boss = IsBossMonster(m);
                var zone = worldData.ZoneFor(m["townId"].Str());
                var rng = new System.Random(uid.GetHashCode());
                Vector2 tile;
                RectInt range;
                if (zone != null)
                {
                    tile = new Vector2(zone.x + 1 + rng.Next(Math.Max(1, zone.w - 2)), zone.y + 1 + rng.Next(Math.Max(1, zone.h - 2)));
                    // Small beasts keep to their ground. A great beast ranges further the higher its realm,
                    // and a world boss walks the whole province.
                    if (m["isWorldBoss"].Bool()) range = new RectInt(10, 10, Mathf.Max(1, worldData.w - 20), Mathf.Max(1, worldData.h - 20));
                    else if (boss)
                    {
                        var reach = 24 + Mathf.Min(48, m["realm"].Int() * 3);
                        var x0 = Mathf.Max(10, zone.x - reach);
                        var y0 = Mathf.Max(10, zone.y - reach * 3 / 4);
                        range = new RectInt(x0, y0, Mathf.Min(worldData.w - 10, zone.x + zone.w + reach) - x0, Mathf.Min(worldData.h - 10, zone.y + zone.h + reach * 3 / 4) - y0);
                    }
                    else range = new RectInt(zone.x, zone.y, zone.w, zone.h);
                }
                else
                {
                    var town = worldData.Town(hub["town"]["id"].Str());
                    var center = town != null ? new Vector2Int(town.gate[0], town.gate[1] + 6) : new Vector2Int(worldData.w / 2, worldData.h / 2);
                    tile = center;
                    range = new RectInt(center.x - 8, center.y - 4, 16, 10);
                }
                tile = worldData.NearestOpen(new Vector2Int(Mathf.RoundToInt(tile.x), Mathf.RoundToInt(tile.y)));
                var sprite = MonsterSprite(m["monsterId"].Str());
                var size = boss ? new Vector2(58, 58) : new Vector2(34, 34);
                var name = Clean(m["name"].Str());
                var displayName = (boss ? "Boss · " : "") + name;
                Color tagColor = boss ? (Color)new Color32(255, 120, 96, 255) : (Color)new Color32(250, 232, 210, 255);
                var actor = worldView.AddActor(uid, boss ? "boss" : "monster", tile, null, sprite, size, displayName, tagColor, aura: boss);
                actor.Data = m;
                actor.Pressure = boss;
                actor.PressureColor = m["isWorldBoss"].Bool() ? new Color(1f, .78f, .3f, 1f) : new Color(1f, .3f, .22f, 1f);
                actor.Roams = true;
                actor.Range = range;
                actor.Speed = boss ? 1.1f : 1.3f;
                actor.Hop = m["isWorldBoss"].Bool() ? 30 : boss ? 20 : 7;
                actor.NextWander = Time.time + (float)rng.NextDouble() * 3f;
                worldView.SetHp(actor, m["hp"].Num(), m["maxHp"].Num(1));
                worldMonsterActors[uid] = actor;
            }
            foreach (var uid in new List<string>(worldMonsterActors.Keys))
            {
                if (seen.Contains(uid)) continue;
                worldView.RemoveActor(worldMonsterActors[uid]);
                worldMonsterActors.Remove(uid);
            }
        }

        // ------------------------------------------------------------------ interactions

        private void HandleWorldPoi(WorldPoi poi)
        {
            if (worldView == null) return;
            switch (poi.kind)
            {
                case "province_gate":
                    WalkToPoi(poi, () => OpenProvinceGate(poi));
                    break;
                case "ascension_gate":
                    WalkToPoi(poi, () => OpenAscensionGate(poi));
                    break;
                case "city":
                    ShowHudAction("Vào thành · " + poi.label, () => WalkToPoi(poi, () => EnterCityFromWorld(poi.townId)));
                    WalkToPoi(poi, () => EnterCityFromWorld(poi.townId));
                    break;
                case "dungeon":
                    WalkToPoi(poi, () => OpenDungeonDialog(poi));
                    break;
                case "landmark":
                    WalkToPoi(poi, () => Toast(poi.label + " · Nơi linh khí hội tụ, đạo hữu có thể dừng chân ngắm cảnh."));
                    break;
                default:
                    worldView.WalkTo(new Vector2Int(poi.x, poi.y), SaveWorldTile);
                    break;
            }
        }

        private void WalkToPoi(WorldPoi poi, Action arrive)
        {
            var target = new Vector2Int(poi.x, poi.y);
            var me = worldView.TileOf(worldView.Player.Pos);
            if (Vector2Int.Distance(me, target) <= 1.5f) { arrive(); return; }
            if (!worldView.WalkTo(target, () => { SaveWorldTile(); arrive(); })) Toast("Không tìm được đường tới " + poi.label + ".", true);
        }

        private void OpenProvinceGate(WorldPoi poi)
        {
            var destination = RegionById(poi.targetMapId);
            var required = RegionRealmMin(poi.targetMapId);
            var current = offlinePreview ? offlineProgress.realmIndex : hub["realm"]["index"].Int();
            if (current < required)
            {
                Toast("Cần đạt " + RegionRealmName(poi.targetMapId) + " để qua cổng tới " + Clean(destination?.name ?? poi.label) + ".", true);
                return;
            }
            Confirm("Cổng dịch chuyển châu", "Qua cổng miễn phí tới " + Clean(destination?.name ?? poi.label) + "? Yêu cầu cảnh giới " + RegionRealmName(poi.targetMapId) + ".", "Qua cổng", () => CrossProvinceGate(poi));
        }

        private void OpenAscensionGate(WorldPoi poi)
        {
            var ascended = hub["player"]["ascended"].Bool() || (offlinePreview && offlineProgress.realmIndex >= 11);
            if (!ascended)
            {
                Toast("Hãy hoàn thành nghi thức Phi Thăng trong mục Nhân vật trước khi qua cổng.", true);
                return;
            }
            var enteringImmortal = worldData?.worldId == "world_pham";
            var targetName = enteringImmortal ? "Tiên Giới" : "Phàm Giới";
            Confirm("Cổng Phi Thăng", "Qua cổng riêng để chuyển sang bản đồ " + targetName + "?", "Qua cổng", CrossAscensionGate);
        }

        private void CrossProvinceGate(WorldPoi poi)
        {
            if (offlinePreview)
            {
                var target = FirstTownForMap(poi.targetMapId);
                if (target.IsNull) { Toast("Chưa tìm thấy thành trấn ở châu bên kia cổng.", true); return; }
                SetOfflineWorldDestination(target, poi.targetMapId, poi.x, poi.y);
                Toast("Cổng dịch chuyển miễn phí đưa đạo hữu sang " + Clean(RegionById(poi.targetMapId)?.name ?? poi.label) + ".");
                return;
            }
            SaveWorldTile(saved =>
            {
                if (!saved) { Toast("Không lưu được vị trí tại cổng. Hãy thử lại.", true); return; }
                ShowBusy(true);
                client.Post("/world/province-gate", Body("gateId", poi.gateId), (result, error) =>
                {
                    ShowBusy(false);
                    if (error != null) { Toast(error, true); return; }
                    if (result["state"].IsObject) AcceptState(result["state"]);
                    ClearLocalWorldPosition();
                    ShowWorld();
                    Toast(result["toast"].Str("Đã qua cổng sang châu kế bên."));
                });
            });
        }

        private void CrossAscensionGate()
        {
            if (offlinePreview)
            {
                var targetMapId = worldData?.worldId == "world_pham" ? "map_9" : "map_8";
                var targetTown = FirstTownForMap(targetMapId);
                if (targetTown.IsNull) { Toast("Chưa tìm thấy đầu cổng bên kia.", true); return; }
                var x = targetMapId == "map_9" ? 14 : 502;
                var y = targetMapId == "map_9" ? 80 : 400;
                hub["player"].Set("ascended", true);
                SetOfflineWorldDestination(targetTown, targetMapId, x, y);
                Toast("Cổng Phi Thăng đưa đạo hữu sang " + (targetMapId == "map_9" ? "Tiên Giới" : "Phàm Giới") + ".");
                return;
            }
            SaveWorldTile(saved =>
            {
                if (!saved) { Toast("Không lưu được vị trí tại cổng. Hãy thử lại.", true); return; }
                ShowBusy(true);
                client.Post("/world/ascension-gate", Body(), (result, error) =>
                {
                    ShowBusy(false);
                    if (error != null) { Toast(error, true); return; }
                    if (result["state"].IsObject) AcceptState(result["state"]);
                    ClearLocalWorldPosition();
                    ShowWorld();
                    Toast(result["toast"].Str("Đã chuyển sang bản đồ cõi bên kia."));
                });
            });
        }

        private void SetOfflineWorldDestination(J targetTown, string mapId, int x, int y)
        {
            offlineProgress.currentTownId = targetTown["id"].Str();
            targetTown.Set("mapId", mapId);
            hub.Set("town", targetTown.Raw);
            if (hub["player"].IsObject)
            {
                var position = hub["player"]["worldPosition"];
                if (!position.IsObject) { position = new J(new Dictionary<string, object>()); hub["player"].Set("worldPosition", position.Raw); }
                position.Set("mapId", mapId); position.Set("x", x); position.Set("y", y);
            }
            if (offlinePreviewState != null)
            {
                foreach (var town in offlinePreviewState.allTowns ?? Array.Empty<TownInfo>())
                    if (town != null && town.id == targetTown["id"].Str()) { town.mapId = mapId; offlinePreviewState.town = town; break; }
                if (offlinePreviewState.player != null)
                {
                    offlinePreviewState.player.ascended = true;
                    if (offlinePreviewState.player.worldPosition == null) offlinePreviewState.player.worldPosition = new WorldMapPosition();
                    offlinePreviewState.player.worldPosition.mapId = mapId;
                    offlinePreviewState.player.worldPosition.x = x;
                    offlinePreviewState.player.worldPosition.y = y;
                }
                latestState = offlinePreviewState;
            }
            SaveOfflineProgress();
            ClearLocalWorldPosition();
            ShowWorld();
        }

        private void ClearLocalWorldPosition()
        {
            worldReturnTile = null;
            lastSavedTile = new Vector2Int(-999, -999);
            PlayerPrefs.DeleteKey("tt_offline_world_x");
            PlayerPrefs.DeleteKey("tt_offline_world_y");
            PlayerPrefs.DeleteKey("tt_offline_world_mapId");
            PlayerPrefs.Save();
        }

        private J FirstTownForMap(string mapId)
        {
            foreach (var town in hub["allTowns"].Items)
                if (town["mapId"].Str() == mapId) return town;
            return J.Null;
        }

        private WorldRegionMeta RegionById(string mapId)
        {
            foreach (var region in worldData?.regions ?? Array.Empty<WorldRegionMeta>())
                if (region?.id == mapId) return region;
            return null;
        }

        private int RegionRealmMin(string mapId)
        {
            foreach (var map in hub["allMaps"].Items)
                if (map["id"].Str() == mapId) return map["realmMin"].Int();
            return RegionById(mapId)?.realmMin ?? 0;
        }

        private string RegionRealmName(string mapId)
        {
            foreach (var map in hub["allMaps"].Items)
                if (map["id"].Str() == mapId) return Clean(map["realmMinName"].Str("cảnh giới phù hợp"));
            return Clean(RegionById(mapId)?.realmMinName ?? "cảnh giới phù hợp");
        }

        private void ApplyRegionThresholds(WorldMapData data)
        {
            if (data?.regions == null) return;
            foreach (var region in data.regions)
            {
                if (region == null) continue;
                foreach (var map in hub["allMaps"].Items)
                {
                    if (map["id"].Str() != region.id) continue;
                    region.realmMin = map["realmMin"].Int();
                    region.realmMinName = map["realmMinName"].Str();
                    break;
                }
            }
        }

        private void HandleWorldActor(WorldActor actor)
        {
            if (actor.Kind == "monster" || actor.Kind == "boss")
            {
                var m = actor.Data is J j ? j : J.Null;
                if (m["isLocked"].Bool() && !string.IsNullOrEmpty(m["lockedByName"].Str()))
                {
                    Toast($"{Clean(m["name"].Str())} đang giao chiến với {Clean(m["lockedByName"].Str())}.", true);
                    return;
                }
                var uid = actor.Id;
                if (worldActorEngagedId == uid || worldActorConfirmId == uid) return;
                Action arrive = () => StartWorldBattle(uid, m);
                ShowHudAction("Khiêu chiến · " + Clean(m["name"].Str()), () => worldView.Approach(actor, 1.4f, arrive));
                worldView.Approach(actor, 1.4f, arrive);
            }
            else if (actor.Kind == "npc")
            {
                var npc = actor.Data is J j ? j : J.Null;
                worldView.Approach(actor, 1.6f, () => OpenNpcDialog(npc));
            }
        }

        private void StartWorldBattle(string uid, J monster)
        {
            if (worldActorEngagedId == uid) return;
            if (IsBossMonster(monster) && monster["requiredPartySize"].Int(1) > 1 && !monster["partyOk"].Bool(true))
            {
                if (worldActorConfirmId == uid) return;
                worldActorConfirmId = uid;
                Confirm("Boss cần tổ đội", $"{Clean(monster["name"].Str())} yêu cầu tổ đội tối thiểu {monster["requiredPartySize"].Int()} người. Vẫn thử khiêu chiến?",
                    "Khiêu chiến", () =>
                    {
                        worldActorConfirmId = null;
                        worldActorEngagedId = uid;
                        SaveWorldTile();
                        worldReturnTile = worldView != null ? worldView.Player.Pos : (Vector2?)null;
                        Hunt(uid);
                    });
                return;
            }
            worldActorConfirmId = null;
            worldActorEngagedId = uid;
            SaveWorldTile();
            worldReturnTile = worldView != null ? worldView.Player.Pos : (Vector2?)null;
            Hunt(uid);
        }

        private void OpenDungeonDialog(WorldPoi poi)
        {
            J info = J.Null;
            foreach (var d in hub["dungeons"].Items) if (d["id"].Str() == poi.dungeonId) { info = d; break; }
            var card = Modal(Clean(poi.label), 900f, 520f, out var close);
            var desc = info.IsNull ? "Cổ động ẩn chứa yêu thú và bảo vật. Mỗi lần thám hiểm tiêu tốn thể lực." : Clean(info["desc"].Str());
            AnchoredText(card, "Desc", desc, ModernUi.Regular, 24, AuthTextSecondary, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(34, -260), new Vector2(-34, -100));
            var meta = info.IsNull ? "" : $"Thể lực {info["stamina"].Int()}  ·  Yêu cầu {Clean(info["realmMinName"].Str())}";
            AnchoredText(card, "Meta", meta, ModernUi.SemiBold, 24, AuthGoldAccent, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(34, 120), new Vector2(-34, 170));
            PillButton(Anchored("Leave", card, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(28, 24), new Vector2(-8, 104)), "Rời đi", "arrowLeft", false, close);
            PillButton(Anchored("Enter", card, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 24), new Vector2(-28, 104)), "Vào bí cảnh", "ui:co_dong", true, () =>
            {
                close();
                SaveWorldTile();
                worldReturnTile = worldView != null ? worldView.Player.Pos : (Vector2?)null;
                if (poi.townId != hub["town"]["id"].Str()) { EnterTownThen(poi.townId, () => EnterDungeon(poi.dungeonId)); return; }
                EnterDungeon(poi.dungeonId);
            });
        }

        private void OpenNpcDialog(J npc)
        {
            var card = Modal(Clean(npc["name"].Str()) + " · " + Clean(npc["title"].Str()), 940f, 560f, out var close);
            var text = $"{Clean(npc["monName"].Str())} · {Clean(npc["heName"].Str())} · {Clean(npc["realmName"].Str())}\nChiến lực {Vn(npc["power"])} · {npc["wins"].Int()} thắng / {npc["losses"].Int()} bại\n\n{Clean(npc["desc"].Str())}\n{Clean(npc["activity"].Str())}";
            AnchoredText(card, "Body", text, ModernUi.Regular, 23, AuthTextSecondary, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(34, 130), new Vector2(-34, -100));
            PillButton(Anchored("Close", card, new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(28, 24), new Vector2(-8, 104)), "Đóng", "arrowLeft", false, close);
            PillButton(Anchored("Fight", card, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(8, 24), new Vector2(-28, 104)), "Luận bàn", "ui:swords", true, () =>
            {
                close();
                worldReturnTile = worldView != null ? worldView.Player.Pos : (Vector2?)null;
                Act("/npc/manual_battle", Body("npcId", npc["id"].Str()));
            });
        }

        private void EnterCityFromWorld(string townId)
        {
            if (string.IsNullOrEmpty(townId) || worldCityEntryPending) return;
            worldCityEntryPending = true;
            worldReturnTile = worldView != null ? worldView.Player.Pos : (Vector2?)null;
            var gate = worldData?.Town(townId)?.gate;
            if (gate != null && gate.Length >= 2) worldReturnTile = new Vector2(gate[0], gate[1] + 1);
            worldTravel = TravelMode.Walk;
            void Open()
            {
                if (worldView != null) { worldView.Stop(); Destroy(worldView.gameObject); worldView = null; }
                try { ShowCity(townId); }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    worldCityEntryPending = false;
                    SafeShowWorld();
                    Toast("Không mở được thành: " + ex.Message, true);
                }
            }
            if (townId == hub["town"]["id"].Str()) { Open(); return; }
            EnterTownThen(townId, Open);
        }

        // ------------------------------------------------------------------ flying sword & mount

        private J TravelItem() => hub["player"]["equip"]["phiKiem"];

        private bool HasTravelItem()
        {
            var item = TravelItem();
            return item.IsObject && !string.IsNullOrEmpty(item["id"].Str());
        }

        private static bool IsMountItem(J item) => item["mount"].Bool() || item["id"].Str().StartsWith("toa_ky", StringComparison.Ordinal);

        /// <summary>Dynamic battle speed in PvE and PvP scaled by equipped flying sword or mount.</summary>
        internal float BattleMoveSpeed()
        {
            var baseSpeed = 490f;
            var item = TravelItem();
            if (item.IsObject && !string.IsNullOrEmpty(item["id"].Str()))
            {
                var isMount = IsMountItem(item);
                var speedVal = (float)item["flySpeed"].Num(.16 + item["qualityRank"].Int() * .06);
                var mult = isMount ? 1.45f + speedVal * 1.8f : 1.65f + speedVal * 2.2f;
                return baseSpeed * Mathf.Clamp(mult, 1.25f, 2.85f);
            }
            return baseSpeed;
        }

        /// <summary>Mounts borrow the animated art of the matching beast.</summary>
        private static string MountArtId(string name)
        {
            name = name ?? "";
            if (name.Contains("Hổ")) return "bach_ho_anh";
            if (name.Contains("Lang")) return "thanh_lang_vuong";
            if (name.Contains("Ưng")) return "loi_ung";
            if (name.Contains("Quy")) return "ty_rua_nuoc";
            if (name.Contains("Sư")) return "phong_lang";
            if (name.Contains("Báo")) return "doc_giac_bao";
            if (name.Contains("Long")) return "thuy_ky_lan";
            if (name.Contains("Phượng")) return "ngu_sac_khong_tuoc";
            if (name.Contains("Lân")) return "huyet_ky_lan";
            if (name.Contains("Lộc")) return "bach_loc_linh_thu";
            return "bac_han_tien_hac";
        }

        /// <summary>Takes off on the equipped flying sword / mount, or comes down (fly = false).</summary>
        private void ApplyWorldTravel(bool fly)
        {
            if (worldView?.Player == null) return;
            var item = TravelItem();
            var mode = TravelMode.Walk;
            Sprite[] frames = null;
            var color = (Color)HudGold;
            var factor = 1f;
            if (fly && HasTravelItem())
            {
                mode = IsMountItem(item) ? TravelMode.Mount : TravelMode.Sword;
                color = RarityColor(item);
                var speed = (float)item["flySpeed"].Num(.14 + item["qualityRank"].Int() * .05);
                factor = mode == TravelMode.Mount ? 2.2f + speed * 3f : 2.8f + speed * 3.5f;
                if (mode == TravelMode.Mount)
                {
                    var art = ArtSprites.MonsterFrames(MountArtId(item["name"].Str()));
                    frames = art?.Body;
                    if (frames == null || frames.Length == 0) mode = TravelMode.Sword;
                }
            }
            worldView.SetTravel(mode, frames, color, factor);
            worldTravel = mode;
            if (travelLabel != null) travelLabel.text = mode != TravelMode.Walk ? "Hạ xuống" : HasTravelItem() && IsMountItem(item) ? "Cưỡi tọa kỵ" : "Ngự kiếm";
        }

        private void ToggleWorldTravel()
        {
            if (worldView?.Player == null) return;
            if (!worldView.Flying) { ApplyWorldTravel(true); return; }
            // come down on ground a walker can stand on: glide to it first when hovering over rock, trees or water
            if (!worldView.LandingSpot(out var spot)) { Toast("Quanh đây không có chỗ đáp, hãy bay tới vùng đất trống.", true); return; }
            void Land() { ApplyWorldTravel(false); SaveWorldTile(); }
            if (spot == worldView.TileOf(worldView.Player.Pos)) Land();
            else if (!worldView.WalkTo(spot, Land)) Land();
        }

        private void BuildTravelButton(RectTransform root)
        {
            travelLabel = null;
            if (!HasTravelItem()) return;
            var item = TravelItem();
            var rect = Anchored("Travel", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(252, 262), new Vector2(368, 378));
            var disc = rect.gameObject.AddComponent<Image>();
            disc.sprite = InkUi.Glow;
            disc.color = new Color32(26, 28, 30, 235);
            var ring = Anchored("Ring", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ring.sprite = InkUi.Ring;
            ring.color = RarityColor(item);
            ring.raycastTarget = false;
            var icon = Anchored("Icon", rect, Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -24)).gameObject.AddComponent<Image>();
            icon.sprite = ItemSprite(item);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (icon.sprite == null) icon.color = new Color(1, 1, 1, 0);
            travelLabel = AnchoredText(rect, "Label", "", ModernUi.SemiBold, 18, HudCream, TextAnchor.LowerCenter, new Vector2(-.4f, 0), new Vector2(1.4f, 0), new Vector2(0, -26), new Vector2(0, 0));
            travelLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            travelLabel.raycastTarget = false;
            travelLabel.text = IsMountItem(item) ? "Cưỡi tọa kỵ" : "Ngự kiếm";
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            button.onClick.AddListener(ToggleWorldTravel);
            rect.gameObject.AddComponent<UiPressScale>();
        }

        /// <summary>Standing at a city gate, a cave mouth or the sword gate offers the matching action without a tap on the map.</summary>
        private void UpdatePlacePrompt(Vector2Int tile)
        {
            if (worldData == null || hudActionRect == null) return;
            WorldPoi near = null;
            var best = 2.8f;
            foreach (var poi in worldData.pois)
            {
                if (poi == null || (poi.kind != "city" && poi.kind != "dungeon" && poi.kind != "province_gate" && poi.kind != "ascension_gate")) continue;
                var d = Vector2Int.Distance(tile, new Vector2Int(poi.x, poi.y));
                // from the air the whole city counts, not only its gate
                if (poi.kind == "city" && poi.rect != null && poi.rect.Length >= 4 && worldView != null && worldView.Flying
                    && tile.x >= poi.rect[0] && tile.x < poi.rect[0] + poi.rect[2] && tile.y >= poi.rect[1] && tile.y <= poi.rect[1] + poi.rect[3] + 1) d = 0f;
                if (d < best) { best = d; near = poi; }
            }
            if (near == promptPoi) return;
            promptPoi = near;
            if (near == null) { if (hudActionAuto) HideHudAction(); return; }
            var target = near;
            if (near.kind == "city") ShowHudAction("Vào thành · " + near.label, () => EnterCityFromWorld(target.townId));
            else if (near.kind == "dungeon") ShowHudAction("Bí cảnh · " + near.label, () => OpenDungeonDialog(target));
            else if (near.kind == "province_gate") ShowHudAction("Cổng châu · " + near.label, () => HandleWorldPoi(target));
            else if (near.kind == "ascension_gate") ShowHudAction("Cổng Phi Thăng", () => HandleWorldPoi(target));
            hudActionAuto = true;
        }

        /// <summary>Walking into another city of the same province makes it the player's current town.</summary>
        private void EnterTownThen(string townId, Action then)
        {
            var tile = worldView != null ? worldView.TileOf(worldView.Player.Pos) : new Vector2Int(-1, -1);
            if (offlinePreview)
            {
                offlineProgress.currentTownId = townId;
                SaveOfflineProgress();
                if (hub.IsObject && hub["town"].IsObject)
                {
                    hub["town"].Set("id", townId);
                    var townMeta = worldData?.Town(townId);
                    if (townMeta != null)
                    {
                        hub["town"].Set("name", townMeta.name);
                        hub["town"].Set("mapId", townMeta.regionId);
                    }
                }
                then?.Invoke();
                return;
            }
            ShowBusy(true);
            client.Post("/world/enter-town", Body("townId", townId, "mapId", worldMapId, "x", tile.x, "y", tile.y), (result, error) =>
            {
                ShowBusy(false);
                if (error != null) { worldCityEntryPending = false; Toast(error, true); return; }
                if (result["state"].IsObject) AcceptState(result["state"]);
                PlayerPrefs.SetInt("tt_world_layout", WorldLayoutRev);
                PlayerPrefs.Save();
                then?.Invoke();
            });
        }

        // ------------------------------------------------------------------ position sync

        private void OnWorldStep()
        {
            if (worldView?.Player == null) return;
            var tile = worldView.TileOf(worldView.Player.Pos);
            TryAutoEnterNearbyWorldTarget();
            if (worldView?.Player == null) return;
            UpdateMiniPlayer();
            UpdatePlacePrompt(tile);
            if (Time.time >= nextWorldSave && tile != lastSavedTile)
            {
                nextWorldSave = Time.time + 6f;
                SaveWorldTile();
            }
            UpdateHudLocation(tile);
        }

        private void TryAutoEnterNearbyWorldTarget()
        {
            if (worldView?.Player == null || worldView.Flying || worldCityEntryPending) return;

            WorldPoi nearestCity = null;
            var cityDistance = 1.65f;
            foreach (var poi in worldData?.pois ?? Array.Empty<WorldPoi>())
            {
                if (poi?.kind != "city" || string.IsNullOrEmpty(poi.townId)) continue;
                var distance = Vector2.Distance(worldView.Player.Pos, new Vector2(poi.x, poi.y));
                if (distance < cityDistance) { cityDistance = distance; nearestCity = poi; }
            }
            if (nearestCity != null)
            {
                EnterCityFromWorld(nearestCity.townId);
                return;
            }

            WorldActor nearestMonster = null;
            var monsterDistance = 1.45f;
            foreach (var actor in worldView.Actors)
            {
                if (actor == null || actor.Hidden || (actor.Kind != "monster" && actor.Kind != "boss")) continue;
                if (actor.Data is J monsterData && monsterData["isLocked"].Bool()) continue;
                var distance = Vector2.Distance(worldView.Player.Pos, actor.Pos + new Vector2(0f, -.6f));
                if (distance < monsterDistance) { monsterDistance = distance; nearestMonster = actor; }
            }
            if (nearestMonster != null) HandleWorldActor(nearestMonster);

            if (!string.IsNullOrEmpty(worldActorConfirmId))
            {
                var confirmedActorStillNear = false;
                foreach (var actor in worldView.Actors)
                {
                    if (actor == null || actor.Id != worldActorConfirmId || actor.Hidden) continue;
                    confirmedActorStillNear = Vector2.Distance(worldView.Player.Pos, actor.Pos + new Vector2(0f, -.6f)) <= 2.8f;
                    break;
                }
                if (!confirmedActorStillNear) worldActorConfirmId = null;
            }
        }

        private void SaveWorldTile()
        {
            SaveWorldTile(null);
        }

        private void SaveWorldTile(Action<bool> afterSaved)
        {
            if (worldView?.Player == null) { afterSaved?.Invoke(false); return; }
            var tile = worldView.TileOf(worldView.Player.Pos);
            // in the air the saved tile is the ground below (or beside) the rider, so a later login lands somewhere walkable
            if (worldData != null && worldData.IsBlocked(tile.x, tile.y))
            {
                tile = worldData.NearestOpen(tile, 30);
                if (worldData.IsBlocked(tile.x, tile.y)) { afterSaved?.Invoke(false); return; }
            }
            if (tile == lastSavedTile && offlinePreview) { afterSaved?.Invoke(true); return; }
            lastSavedTile = tile;
            if (offlinePreview)
            {
                PlayerPrefs.SetInt("tt_offline_world_x", tile.x);
                PlayerPrefs.SetInt("tt_offline_world_y", tile.y);
                PlayerPrefs.SetString("tt_offline_world_mapId", worldMapId);
                PlayerPrefs.SetInt("tt_world_layout", WorldLayoutRev);
                PlayerPrefs.Save();
                afterSaved?.Invoke(true);
                return;
            }
            client.SaveWorldPosition(worldMapId, tile.x, tile.y, (ok, error) =>
            {
                if (ok) PlayerPrefs.SetInt("tt_world_layout", WorldLayoutRev);
                else Debug.LogWarning("world/move: " + error);
                afterSaved?.Invoke(ok);
            });
        }

        private void TickWorld()
        {
            if (worldView == null) return;
            UpdateWayfinders();
            if (offlinePreview) return;
            if (Time.time >= nextMonsterRefresh)
            {
                nextMonsterRefresh = Time.time + 20f;
                client.Get("/world/monsters", (data, error) =>
                {
                    if (error == null && worldView != null) SyncWorldMonsters(data["list"]);
                });
            }
        }

        // ------------------------------------------------------------------ HUD

        private RectTransform HudRoot()
        {
            // content has 6%/4% margins; the HUD uses the whole safe area
            var root = Anchored("WorldHud", content.transform, new Vector2(-.068f, -.043f), new Vector2(1.068f, 1.043f), Vector2.zero, Vector2.zero);
            return root;
        }

        private void BuildWorldHud(WorldMapData data)
        {
            var root = HudRoot();
            BuildOverlays();
            hudTicker = root.gameObject.AddComponent<WorldHudTicker>();
            hudTicker.Owner = this;
            BuildAvatarCard(root);
            BuildMiniMap(root, data);
            BuildTravelButton(root);
            BuildWayfinders(root);
            BuildMenuColumn(root);
            var loc = Anchored("Location", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -88), new Vector2(600, -18));
            ModernSurface(loc, new Color32(12, 18, 24, 238), 14f, new Color32(232, 196, 120, 150));
            hudLocation = AnchoredText(loc, "Name", "", ModernUi.SemiBold, 22, HudCream, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -39), new Vector2(-14, -7));
            hudLocation.horizontalOverflow = HorizontalWrapMode.Wrap;
            hudLocation.verticalOverflow = VerticalWrapMode.Truncate;
            hudLocation.resizeTextForBestFit = true;
            hudLocation.resizeTextMinSize = 16;
            hudLocation.resizeTextMaxSize = 22;
            PixelUiSkin.ApplyTextTreatment(hudLocation);
            hudPhase = AnchoredText(loc, "Phase", "", ModernUi.Regular, 14, new Color32(196, 204, 208, 255), TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 6), new Vector2(-14, 29));
            hudPhase.horizontalOverflow = HorizontalWrapMode.Wrap;
            hudPhase.verticalOverflow = VerticalWrapMode.Truncate;
            PixelUiSkin.ApplyTextTreatment(hudPhase);
            UpdateHudLocation(worldView.TileOf(worldView.Player.Pos));
            hudActionRect = Anchored("Action", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-420, 18), new Vector2(-20, 82));
            var actionFill = ModernSurface(hudActionRect, new Color32(226, 190, 112, 255), 14f, new Color32(255, 232, 176, 255), true);
            var actionButton = hudActionRect.gameObject.AddComponent<Button>();
            actionButton.targetGraphic = actionFill;
            var actionColors = actionButton.colors;
            actionColors.normalColor = Color.white;
            actionColors.highlightedColor = new Color(1f, .96f, .82f);
            actionColors.pressedColor = new Color(.76f, .83f, .79f);
            actionButton.colors = actionColors;
            actionButton.onClick.AddListener(() => hudActionCallback?.Invoke());
            hudActionRect.gameObject.AddComponent<UiPressScale>();
            hudActionLabel = AnchoredText(hudActionRect, "Text", "", ModernUi.Bold, 20, new Color32(28, 31, 33, 255), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0));
            hudActionLabel.fontStyle = FontStyle.Bold;
            hudActionLabel.resizeTextForBestFit = true; hudActionLabel.resizeTextMinSize = 15; hudActionLabel.resizeTextMaxSize = 20;
            hudActionLabel.raycastTarget = false;
            PixelUiSkin.ApplyTextTreatment(hudActionLabel);
            HideHudAction();
        }

        private static Image ModernSurface(RectTransform rect, Color fill, float radius, Color stroke, bool raycastTarget = false)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(image, radius);
            image.color = fill;
            image.raycastTarget = raycastTarget;
            var edge = Anchored("ModernEdge", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(edge, radius, 1f);
            edge.color = stroke;
            edge.raycastTarget = false;
            return image;
        }

        private void ShowHudAction(string label, Action click)
        {
            if (hudActionRect == null) return;
            hudActionLabel.text = Clean(label);
            hudActionCallback = click;
            hudActionAuto = false;
            hudActionRect.gameObject.SetActive(true);
        }

        private void HideHudAction()
        {
            if (hudActionRect != null) hudActionRect.gameObject.SetActive(false);
            hudActionCallback = null;
            hudActionAuto = false;
            promptPoi = null;
        }

        private void UpdateHudLocation(Vector2Int tile)
        {
            if (hudLocation == null || worldData == null) return;
            string place = null;
            foreach (var town in worldData.towns)
            {
                if (town?.gate == null) continue;
                if (Mathf.Abs(tile.x - town.gate[0]) < 10 && Mathf.Abs(tile.y - town.gate[1]) < 8) { place = town.name; break; }
            }
            foreach (var zone in worldData.zones)
                if (zone != null && tile.x >= zone.x && tile.x < zone.x + zone.w && tile.y >= zone.y && tile.y < zone.y + zone.h) place = "Bãi Yêu Thú";
            var region = worldData.RegionAt(tile.x, tile.y);
            var where = region != null ? "  ·  " + Clean(region.name) : "";
            if (place != null) where += "  ·  " + Clean(place);
            hudLocation.text = Clean(worldData.name).ToUpperInvariant() + where;
            var phase = hub["timePhase"];
            hudPhase.text = Clean(phase["name"].Str("Ban ngày")) + "  ·  " + Clean(hub["realm"]["name"].Str()) + " " + Clean(hub["realm"]["sub"].Str());
        }

        private void BuildAvatarCard(RectTransform root)
        {
            var player = hub["player"];
            var card = Anchored("Avatar", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 18), new Vector2(590, 166));
            ModernSurface(card, new Color32(12, 18, 24, 238), 16f, new Color32(232, 196, 120, 140));
            // portrait disc
            var disc = Anchored("Disc", card, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(8, -58), new Vector2(124, 58));
            var discFill = disc.gameObject.AddComponent<Image>();
            discFill.sprite = InkUi.Glow;
            discFill.color = new Color32(232, 214, 170, 255);
            var ring = Anchored("Ring", disc, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ring.sprite = InkUi.Ring;
            ring.color = HudGold;
            ring.raycastTarget = false;
            var portraitMask = Anchored("PortraitMask", disc, Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18));
            var maskImage = portraitMask.gameObject.AddComponent<Image>();
            maskImage.sprite = InkUi.Glow;
            maskImage.color = new Color(1, 1, 1, .02f);
            portraitMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var portrait = Anchored("Portrait", portraitMask, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            try
            {
                portrait.texture = AvatarComposer.Available ? AvatarComposer.Compose(LookOf(player)) : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Avatar compose error: " + ex.Message);
                portrait.texture = null;
            }
            portrait.uvRect = new Rect(.28f, .69f, .44f, .2625f);   // head and shoulders of the front-view portrait
            portrait.raycastTarget = false;
            var discButton = disc.gameObject.AddComponent<Button>();
            discButton.onClick.AddListener(OpenCharacterScreen);
            disc.gameObject.AddComponent<UiPressScale>();
            AnchoredText(card, "Name", Clean(player["name"].Str("Đạo hữu")), ModernUi.SemiBold, 21, HudCream, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(142, -38), new Vector2(-16, -8));
            AnchoredText(card, "Realm", Clean(hub["realm"]["name"].Str()) + " · " + Clean(player["monName"].Str()), ModernUi.Regular, 14, HudGold, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(142, -61), new Vector2(-16, -39));
            HudBar(card, "Khí huyết", player["hp"].Num(), player["maxHp"].Num(1), new Color32(196, 62, 54, 255), 64);
            var stamina = player["stamina"].Num();
            HudBar(card, "Thể lực", stamina, player["staminaMax"].Num(1), new Color32(92, 170, 110, 255), 40);
            var coin = Anchored("Stones", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(142, 8), new Vector2(-16, 31));
            var coinIcon = Anchored("Icon", coin, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -12), new Vector2(24, 12)).gameObject.AddComponent<Image>();
            coinIcon.sprite = UiPixelIcon("coin");
            coinIcon.preserveAspect = true;
            coinIcon.raycastTarget = false;
            AnchoredText(coin, "Value", Vn(player["stones"]) + " linh thạch", ModernUi.SemiBold, 15, HudCream, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(32, 0), Vector2.zero);
        }

        private void HudBar(RectTransform card, string label, double value, double max, Color color, float y)
        {
            var bar = Anchored("Bar_" + label, card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(142, y), new Vector2(-16, y + 18));
            var track = bar.gameObject.AddComponent<Image>();
            ModernUi.Fill(track, 12f);
            track.color = new Color32(30, 37, 42, 255);
            track.raycastTarget = false;
            var fill = Anchored("Fill", bar, Vector2.zero, new Vector2(Mathf.Clamp01((float)(value / Math.Max(1, max))), 1), new Vector2(1, 1), new Vector2(-1, -1)).gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 8f);
            fill.color = color;
            fill.raycastTarget = false;
            AnchoredText(bar, "Text", $"{label}  {Vn(value)}/{Vn(max)}", ModernUi.SemiBold, 14, HudCream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void BuildMiniMap(RectTransform root, WorldMapData data)
        {
            if (miniMapFrame != null) { miniMapFrame.gameObject.SetActive(false); Destroy(miniMapFrame.gameObject); }
            miniMapRoot = root;
            var playerRegion = data.RegionAt(Mathf.FloorToInt(worldView.Player?.Pos.x ?? 0f), Mathf.FloorToInt(worldView.Player?.Pos.y ?? 0f));
            miniRegionId = playerRegion?.id;
            miniTileBounds = playerRegion == null ? new Rect(0, 0, data.w, data.h)
                : new Rect(playerRegion.x, playerRegion.y, playerRegion.w, playerRegion.h);
            var miniHeight = 240f;
            var miniWidth = miniHeight * miniTileBounds.width / miniTileBounds.height;
            var frameWidth = miniWidth + 16f;
            var frameHeight = miniHeight + 33f;
            var frame = Anchored("MiniMap", root, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-frameWidth - 20f, -frameHeight - 20f), new Vector2(-20, -20));
            miniMapFrame = frame;
            var bg = ModernSurface(frame, new Color32(12, 18, 24, 242), 14f, new Color32(232, 196, 120, 165), true);

            var map = Anchored("Map", frame, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -25));
            var texture = worldView.Painting.texture as Texture2D;
            map.gameObject.AddComponent<RectMask2D>();
            var image = map.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            var crop = worldView.Painting.uvRect;
            image.uvRect = new Rect(crop.x + crop.width * miniTileBounds.x / data.w,
                crop.y + crop.height * (1f - miniTileBounds.yMax / data.h),
                crop.width * miniTileBounds.width / data.w, crop.height * miniTileBounds.height / data.h);
            image.color = Color.white;
            image.raycastTarget = false;
            miniMapRect = map;

            var realmIndex = offlinePreview ? offlineProgress.realmIndex : hub["realm"]["index"].Int();
            foreach (var region in data.regions)
            {
                if (region == null || (playerRegion != null && region.id != playerRegion.id)) continue;
                var xMin = (region.x - miniTileBounds.x) / miniTileBounds.width;
                var xMax = (region.x + region.w - miniTileBounds.x) / miniTileBounds.width;
                var yMin = 1f - (region.y + region.h - miniTileBounds.y) / miniTileBounds.height;
                var yMax = 1f - (region.y - miniTileBounds.y) / miniTileBounds.height;
                var area = Anchored("Province_" + region.id, map, new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);
                var isCurrent = playerRegion != null && playerRegion.id == region.id || worldMapId == region.id;
                var unlocked = region.realmMin <= realmIndex;
                var border = area.gameObject.AddComponent<Image>();
                ModernUi.Ring(border, 8f, isCurrent ? 1.5f : .8f);
                border.color = isCurrent ? new Color32(255, 215, 126, 230)
                    : unlocked ? new Color32(244, 238, 218, 170) : new Color32(157, 170, 176, 125);
                border.raycastTarget = false;
            }
            foreach (var town in data.towns)
            {
                if (town?.gate == null || !miniTileBounds.Contains(new Vector2(town.gate[0], town.gate[1]))) continue;
                var townHalf = town.big ? 5.8f : 4.4f;
                var townAt = MiniMapPoint(new Vector2(town.gate[0] + .5f, town.gate[1] + .5f));
                var dot = Anchored("Town", map, townAt, townAt, new Vector2(-townHalf, -townHalf), new Vector2(townHalf, townHalf)).gameObject.AddComponent<Image>();
                dot.sprite = InkUi.Glow;
                dot.color = HudGold;
                dot.raycastTarget = false;
            }
            // Keep gates visible on this compact map. Towns, caves and hunting grounds are
            // available as individually selectable layers on the full atlas.
            foreach (var poi in data.pois)
            {
                if (poi == null || (poi.kind != "province_gate" && poi.kind != "ascension_gate")) continue;
                if (!miniTileBounds.Contains(new Vector2(poi.x, poi.y))) continue;
                var at = MiniMapPoint(new Vector2(poi.x + .5f, poi.y + .5f));
                var half = poi.kind == "ascension_gate" ? 6f : 4.5f;
                var mark = Anchored("Mark_" + poi.kind, map, at, at, new Vector2(-half, -half), new Vector2(half, half)).gameObject.AddComponent<Image>();
                mark.sprite = InkUi.Glow;
                mark.color = poi.kind == "ascension_gate" ? new Color32(255, 226, 144, 255)
                    : new Color32(135, 221, 255, 235);
                mark.raycastTarget = false;
            }
            miniViewport = Anchored("CameraViewport", map, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var viewBorder = miniViewport.gameObject.AddComponent<Image>();
            ModernUi.Ring(viewBorder, 6f, 1.4f);
            viewBorder.fillCenter = false;
            viewBorder.color = new Color32(255, 250, 224, 210);
            viewBorder.raycastTarget = false;
            miniPlayerDot = Anchored("Me", map, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-9f, -9f), new Vector2(9f, 9f));
            var me = miniPlayerDot.gameObject.AddComponent<Image>();
            ModernUi.Fill(me, 9f);
            me.color = new Color32(255, 80, 60, 255);
            me.raycastTarget = false;
            UpdateMiniPlayer();
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => OpenWorldAtlas(latestState ?? NetworkGameClient.ToGameState(hub)));
            frame.gameObject.AddComponent<UiPressScale>();

            var northBadge = Anchored("North", frame, new Vector2(0, 1), new Vector2(0, 1), new Vector2(9, -24), new Vector2(53, -5));
            var nimg = northBadge.gameObject.AddComponent<Image>();
            ModernSurface(northBadge, new Color32(12, 18, 24, 225), 9f, new Color32(232, 196, 120, 160));
            nimg.raycastTarget = false;
            var nText = AnchoredText(northBadge, "N", "↑ BẮC", ModernUi.SemiBold, 11, Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            nText.raycastTarget = false;
            PixelUiSkin.ApplyTextTreatment(nText);

            var hintPlate = Anchored("HintPlate", frame, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-126, 5), new Vector2(-8, 24));
            var hintBack = hintPlate.gameObject.AddComponent<Image>();
            ModernSurface(hintPlate, new Color32(12, 18, 24, 230), 9f, new Color32(232, 196, 120, 160));
            hintBack.raycastTarget = false;
            var hint = AnchoredText(hintPlate, "Hint", "BẢN ĐỒ LỚN", ModernUi.SemiBold, 11, HudCream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));
            hint.raycastTarget = false;
            PixelUiSkin.ApplyTextTreatment(hint);
        }

        private void BuildVirtualDpad(RectTransform root)
        {
            var dpad = Anchored("Dpad", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 236), new Vector2(230, 426));
            var padBg = dpad.gameObject.AddComponent<Image>();
            padBg.sprite = InkUi.Glow;
            padBg.color = new Color32(20, 22, 26, 170);
            padBg.raycastTarget = false;

            var border = Anchored("Border", dpad, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(border, 85f, 1.2f);
            border.color = new Color32(220, 190, 120, 100);
            border.raycastTarget = false;

            void Arrow(string name, Vector2 dir, Vector2 anchorMin, Vector2 anchorMax, float turn)
            {
                var btnRect = Anchored(name, dpad, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
                var btnImg = btnRect.gameObject.AddComponent<Image>();
                ModernUi.Fill(btnImg, 14f);
                btnImg.color = new Color32(34, 38, 46, 200);
                // the game fonts have no arrow glyphs: use the arrow icon, turned
                var glyph = Anchored("Icon", btnRect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-17, -17), new Vector2(17, 17)).gameObject.AddComponent<Image>();
                glyph.sprite = ModernUi.Icon("arrowRight");
                glyph.color = HudCream;
                glyph.raycastTarget = false;
                glyph.rectTransform.localRotation = Quaternion.Euler(0, 0, turn);
                var trigger = btnRect.gameObject.AddComponent<EventTrigger>();
                AddBattlePointerEvent(trigger, EventTriggerType.PointerDown, () => { if (worldView != null) worldView.VirtualStick = dir; });
                AddBattlePointerEvent(trigger, EventTriggerType.PointerUp, () => { if (worldView != null) worldView.VirtualStick = Vector2.zero; });
                AddBattlePointerEvent(trigger, EventTriggerType.PointerExit, () => { if (worldView != null) worldView.VirtualStick = Vector2.zero; });
                btnRect.gameObject.AddComponent<UiPressScale>();
            }

            Arrow("Up", new Vector2(0, 1), new Vector2(.32f, .65f), new Vector2(.68f, .98f), 90f);
            Arrow("Down", new Vector2(0, -1), new Vector2(.32f, .02f), new Vector2(.68f, .35f), -90f);
            Arrow("Left", new Vector2(-1, 0), new Vector2(.02f, .32f), new Vector2(.35f, .68f), 180f);
            Arrow("Right", new Vector2(1, 0), new Vector2(.65f, .32f), new Vector2(.98f, .68f), 0f);

            var center = Anchored("Center", dpad, new Vector2(.40f, .40f), new Vector2(.60f, .60f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            center.sprite = InkUi.Glow;
            center.color = new Color32(232, 196, 120, 150);
            center.raycastTarget = false;
        }

        private void UpdateMiniPlayer()
        {
            if (miniPlayerDot == null || worldView?.Player == null || worldData == null) return;
            var p = worldView.Player.Pos;
            var region = worldData.RegionAt(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            if (region?.id != miniRegionId && miniMapRoot != null) { BuildMiniMap(miniMapRoot, worldData); return; }
            miniPlayerDot.anchorMin = miniPlayerDot.anchorMax = MiniMapPoint(p + Vector2.one * .5f);
            if (miniViewport != null)
            {
                var view = worldView.VisibleTiles;
                miniViewport.anchorMin = MiniMapPoint(new Vector2(view.xMin, view.yMax));
                miniViewport.anchorMax = MiniMapPoint(new Vector2(view.xMax, view.yMin));
            }
        }

        private Vector2 MiniMapPoint(Vector2 tile) => new Vector2(
            Mathf.Clamp01((tile.x - miniTileBounds.x) / miniTileBounds.width),
            Mathf.Clamp01(1f - (tile.y - miniTileBounds.y) / miniTileBounds.height));

        private Button WuxiaHudButton(RectTransform rect, string label, string iconId, Action click, Color? bgColor = null, Color? textColor = null)
        {
            var fill = rect.gameObject.AddComponent<Image>();
            fill.color = bgColor ?? Panel;
            ModernSurface(rect, bgColor ?? new Color32(12, 18, 24, 238), 13f, new Color32(232, 196, 120, 135), true);

            var hasIcon = !string.IsNullOrEmpty(iconId);
            if (hasIcon)
            {
                var icon = Anchored("Icon", rect, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(16, -20), new Vector2(56, 20)).gameObject.AddComponent<Image>();
                icon.sprite = iconId.StartsWith("ui:") ? UiPixelIcon(iconId.Substring(3)) : ModernUi.Icon(iconId);
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;
            }

            var text = AnchoredText(rect, "Text", Clean(label), ModernUi.SemiBold, 17, textColor ?? HudCream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(hasIcon ? 48 : 10, 0), new Vector2(-10, 0));
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = 17;
            text.raycastTarget = false;
            PixelUiSkin.ApplyTextTreatment(text);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .96f, .82f);
            colors.pressedColor = new Color(.76f, .83f, .79f);
            button.colors = colors;
            if (click != null) button.onClick.AddListener(() => click());
            rect.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        private void BuildMenuColumn(RectTransform root)
        {
            var button = Anchored("WorldMenuButton", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-204, -347), new Vector2(-20, -291));
            WuxiaHudButton(button, "Tiện ích", "compass", OpenWorldMenu);
        }

        private void OpenWorldMenu()
        {
            var card = Modal("TIỆN ÍCH", 1040f, 740f, out var close);
            var area = Anchored("MenuGrid", card, Vector2.zero, Vector2.one, new Vector2(22, 22), new Vector2(-22, -100));
            var viewport = Anchored("Viewport", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 42f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var cells = new GameObject("Items", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            cells.SetParent(viewport, false);
            cells.anchorMin = new Vector2(0, 1);
            cells.anchorMax = new Vector2(1, 1);
            cells.pivot = new Vector2(.5f, 1);
            cells.offsetMin = cells.offsetMax = Vector2.zero;
            var grid = cells.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300f, 76f);
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(8, 8, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;
            cells.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = cells;

            Action run(Action action) => () => { close(); action?.Invoke(); };
            var items = new List<(string icon, string label, Action click)>
            {
                ("road", "Bản đồ lớn", run(() => OpenWorldAtlas(latestState ?? NetworkGameClient.ToGameState(hub)))),
                ("location", "Các châu", run(OpenPlacesModal)),
                ("teleport", "Truyền tống trận", run(() => OpenTeleportScreen(true))),
                ("hanh_trang", "Hành trang", run(() => OpenBagScreen())),
                ("ho_so", "Nhân vật", run(OpenCharacterScreen)),
                ("cong_phap", "Công pháp", run(() => OpenSkillsScreen())),
                ("tong_mon", "Tông môn", run(OpenSectScreen)),
                ("scroll", "Nhiệm vụ", run(OpenBountyScreen)),
                ("power", "Xếp hạng", run(OpenRankScreen)),
                ("mail", hub["inboxUnread"].Int() > 0 ? "Hòm thư · " + hub["inboxUnread"].Int() + " mới" : "Hòm thư", run(OpenInboxScreen)),
                ("sun", "Sự kiện", run(OpenEventsScreen)),
                ("ban_be", "Xã giao", run(OpenSocialScreen)),
                ("", "Phóng to", run(() => worldView?.ZoomBy(1.25f))),
                ("", "Thu nhỏ", run(() => worldView?.ZoomBy(1f / 1.25f))),
            };
            if (offlinePreview) items.Add(("arrowLeft", "Thoát thế giới", run(() => Confirm("Rời thế giới", "Quay lại màn hình đăng nhập?", "Thoát ra", ExitOfflineWorld))));
            foreach (var item in items)
            {
                var rect = new GameObject("Menu_" + item.label, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(cells, false);
                WuxiaHudButton(rect, item.label, string.IsNullOrEmpty(item.icon) ? null : "ui:" + item.icon, item.click);
            }
        }

        internal void WorldTick() { UpdateMiniPlayer(); TickWorld(); }

        // ------------------------------------------------------------------ finding places

        private sealed class WayMark
        {
            public RectTransform Chip;
            public RectTransform Arrow;
            public Text Label;
            public WorldPoi Poi;
            public string Title;
            public int Shown = -1;
        }

        private readonly List<WayMark> wayfinders = new List<WayMark>();
        private RectTransform wayfinderRoot;

        private static string PlaceKind(WorldPoi poi) =>
            poi.kind == "city" ? "Thành" : poi.kind == "zone" ? "Bãi yêu thú" : poi.kind == "dungeon" ? "Cổ động"
            : poi.kind == "province_gate" ? "Cổng châu" : poi.kind == "ascension_gate" ? "Cổng Phi Thăng" : "Danh thắng";

        private static string PlaceIcon(WorldPoi poi) =>
            poi.kind == "city" ? "location" : poi.kind == "zone" ? "swords" : poi.kind == "dungeon" ? "co_dong"
            : poi.kind == "province_gate" || poi.kind == "ascension_gate" ? "teleport" : "scroll";

        private static string Heading(Vector2 delta)
        {
            // tile y grows southwards
            var angle = Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg;
            var index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
            return new[] { "Đông", "Đông Bắc", "Bắc", "Tây Bắc", "Tây", "Tây Nam", "Nam", "Đông Nam" }[index];
        }

        private string PlaceName(WorldPoi poi)
        {
            if (poi.kind == "zone") return Clean(worldData?.Town(poi.townId)?.name ?? poi.label);
            return Clean(poi.label);
        }

        /// <summary>Walks (or flies) to a place picked from the list or an edge pointer.</summary>
        private void GoToPlace(WorldPoi poi)
        {
            if (worldView?.Player == null || poi == null) return;
            if (poi.kind == "zone" || poi.kind == "landmark")
            {
                if (!worldView.WalkTo(new Vector2Int(poi.x, poi.y), SaveWorldTile)) Toast("Không tìm được đường tới " + PlaceName(poi) + ".", true);
                return;
            }
            HandleWorldPoi(poi);
        }

        private void NavigateToRegion(WorldRegionMeta target)
        {
            if (target == null || worldView?.Player == null) return;
            var currentTile = worldView.TileOf(worldView.Player.Pos);
            var here = worldData.RegionAt(currentTile.x, currentTile.y);
            if (here == null) { Toast("Không xác định được châu hiện tại.", true); return; }
            if (here.id == target.id)
            {
                var center = new Vector2Int(target.x + target.w / 2, target.y + target.h / 2);
                if (!worldView.WalkTo(center, SaveWorldTile)) Toast("Không tìm được đường tới khu vực này.", true);
                return;
            }
            var targetCenter = new Vector2(target.x + target.w * .5f, target.y + target.h * .5f);
            var hereCenter = new Vector2(here.x + here.w * .5f, here.y + here.h * .5f);
            var currentDistance = Mathf.Abs(targetCenter.x - hereCenter.x) + Mathf.Abs(targetCenter.y - hereCenter.y);
            WorldPoi nextGate = null;
            var bestDistance = currentDistance;
            foreach (var gate in worldData.pois)
            {
                if (gate?.kind != "province_gate" || gate.regionId != here.id) continue;
                var nextRegion = RegionById(gate.targetMapId);
                if (nextRegion == null) continue;
                var nextCenter = new Vector2(nextRegion.x + nextRegion.w * .5f, nextRegion.y + nextRegion.h * .5f);
                var distance = Mathf.Abs(targetCenter.x - nextCenter.x) + Mathf.Abs(targetCenter.y - nextCenter.y);
                if (distance < bestDistance) { bestDistance = distance; nextGate = gate; }
            }
            if (nextGate != null) { HandleWorldPoi(nextGate); return; }
            Toast("Chưa tìm thấy cổng đi gần hơn tới châu đó.", true);
        }

        private static bool TryProvinceNumber(string id, out int number)
        {
            number = 0;
            return !string.IsNullOrEmpty(id) && id.StartsWith("map_", StringComparison.Ordinal)
                && int.TryParse(id.Substring(4), out number);
        }

        /// <summary>Every châu and nearby place in the current realm; one tap walks across the continuous world.</summary>
        private void OpenPlacesModal()
        {
            if (worldView?.Player == null || worldData == null) return;
            var card = Modal("Địa điểm · " + Clean(worldData.name), 1180f, 800f, out var close);

            // Direct Teleport / World Maps Shortcut at the top of the Places Modal
            var tpBtn = Anchored("TeleportShortcut", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -148), new Vector2(-24, -86));
            var tpFill = tpBtn.gameObject.AddComponent<Image>();
            ModernUi.Fill(tpFill, 16f);
            tpFill.color = new Color32(24, 44, 68, 245);
            var tpEdge = Anchored("Edge", tpBtn, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(tpEdge, 16f, 1.4f);
            tpEdge.color = new Color32(100, 195, 255, 180);
            tpEdge.raycastTarget = false;
            var tpIcon = Anchored("Icon", tpBtn, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(16, -18), new Vector2(52, 18)).gameObject.AddComponent<Image>();
            tpIcon.sprite = UiPixelIcon("teleport");
            tpIcon.preserveAspect = true;
            tpIcon.raycastTarget = false;
            var tpTitle = AnchoredText(tpBtn, "Title", "TRUYỀN TỐNG TRẬN · ĐỔI BẢN ĐỒ / CHÂU KHÁC", ModernUi.Bold, 21, new Color32(230, 245, 255, 255), TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(62, 0), new Vector2(-160, 0));
            tpTitle.raycastTarget = false;
            var tpHint = AnchoredText(tpBtn, "Hint", "Dịch chuyển tức thì", ModernUi.Regular, 18, new Color32(140, 195, 245, 255), TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(-20, 0));
            tpHint.raycastTarget = false;
            var tpAction = tpBtn.gameObject.AddComponent<Button>();
            tpAction.targetGraphic = tpFill;
            tpAction.onClick.AddListener(() => { close(); OpenTeleportScreen(true); });
            tpBtn.gameObject.AddComponent<UiPressScale>();

            var area = Anchored("List", card, Vector2.zero, Vector2.one, new Vector2(22, 22), new Vector2(-22, -156));
            var list = ScrollColumn(area, 8f, 6);
            var me = worldView.Player.Pos;
            var currentTile = worldView.TileOf(me);
            var currentRegion = worldData.RegionAt(currentTile.x, currentTile.y);
            var regions = new List<(float distance, WorldRegionMeta region)>();
            foreach (var region in worldData.regions)
                if (region != null) regions.Add((Vector2.Distance(me, new Vector2(region.x + region.w * .5f, region.y + region.h * .5f)), region));
            regions.Sort((a, b) => a.distance.CompareTo(b.distance));
            foreach (var (distance, region) in regions)
            {
                var target = region;
                var center = new Vector2Int(region.x + region.w / 2, region.y + region.h / 2);
                var where = distance < 3f ? "ngay tại đây" : Mathf.RoundToInt(distance) + " dặm về hướng " + Heading(new Vector2(center.x, center.y) - me);
                var required = region.realmMinName ?? "cảnh giới phù hợp";
                var meetsRealm = (offlinePreview ? offlineProgress.realmIndex : hub["realm"]["index"].Int()) >= region.realmMin;
                var here = currentRegion?.id == region.id;
                Row(list, UiPixelIcon("map"), HudGold, "Châu · " + Clean(region.name), where + " · " + Clean(required), here ? "Đang ở đây" : "Đi qua cổng", meetsRealm ? null : "Cần " + Clean(required), here,
                    () => { close(); NavigateToRegion(target); }, 92f, !meetsRealm);
            }
            var order = new Dictionary<string, int>
            {
                { "province_gate", 0 }, { "ascension_gate", 0 }, { "city", 1 },
                { "zone", 2 }, { "dungeon", 3 }, { "landmark", 4 }
            };
            var entries = new List<(int rank, float distance, WorldPoi poi)>();
            foreach (var poi in worldData.pois)
                if (poi != null && order.TryGetValue(poi.kind ?? "", out var rank) && !string.IsNullOrEmpty(poi.label))
                    entries.Add((rank, Vector2.Distance(me, new Vector2(poi.x, poi.y)), poi));
            entries.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : a.distance.CompareTo(b.distance));
            var accents = new Color[] { new Color32(166, 220, 255, 255), HudGold, new Color32(244, 110, 90, 255), new Color32(186, 132, 255, 255), new Color32(110, 220, 255, 255), new Color32(170, 180, 170, 255) };
            foreach (var (rank, distance, poi) in entries)
            {
                var target = poi;
                var where = distance < 3f ? "ngay tại đây" : Mathf.RoundToInt(distance) + " dặm về hướng " + Heading(new Vector2(poi.x, poi.y) - me);
                Row(list, UiPixelIcon(PlaceIcon(poi)), accents[rank], PlaceKind(poi) + " · " + PlaceName(poi), where, "Đi tới", null, false,
                    () => { close(); GoToPlace(target); }, 92f);
            }
        }

        /// <summary>Edge pointers to the nearest hunting ground and cave while they are off screen.</summary>
        private void BuildWayfinders(RectTransform root)
        {
            wayfinders.Clear();
            wayfinderRoot = root;
            if (worldData == null || worldView?.Player == null) return;
            var townId = hub["town"]["id"].Str();
            WorldPoi Nearest(string kind)
            {
                WorldPoi best = null;
                var bestScore = float.MaxValue;
                foreach (var poi in worldData.pois)
                {
                    if (poi == null || poi.kind != kind) continue;
                    // the current city's own ground / cave first: that is where its monsters and trials are
                    var score = Vector2.Distance(worldView.Player.Pos, new Vector2(poi.x, poi.y)) + (poi.townId == townId ? 0f : 1000f);
                    if (score < bestScore) { bestScore = score; best = poi; }
                }
                return best;
            }
            void Add(WorldPoi poi, string title, Color accent)
            {
                if (poi == null) return;
                var chip = Anchored("Way_" + poi.kind, root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-184, -29), new Vector2(184, 29));
                var fill = chip.gameObject.AddComponent<Image>();
                fill.color = Panel;
                PixelUiSkin.ApplyFrame(chip.gameObject);

                var icon = Anchored("Icon", chip, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -18), new Vector2(50, 18)).gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(PlaceIcon(poi));
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var label = AnchoredText(chip, "Text", title, ModernUi.SemiBold, 18, HudCream, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(58, 0), new Vector2(-46, 0));
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 16;
                label.resizeTextMaxSize = 18;
                label.raycastTarget = false;
                PixelUiSkin.ApplyTextTreatment(label);

                var arrow = Anchored("Arrow", chip, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-42, -15), new Vector2(-12, 15));
                var arrowImage = arrow.gameObject.AddComponent<Image>();
                arrowImage.sprite = ModernUi.Icon("arrowRight");
                arrowImage.color = Gold;
                arrowImage.raycastTarget = false;

                var button = chip.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, .96f, .82f);
                colors.pressedColor = new Color(.76f, .83f, .79f);
                button.colors = colors;
                var target = poi;
                button.onClick.AddListener(() => GoToPlace(target));
                chip.gameObject.AddComponent<UiPressScale>();
                chip.gameObject.SetActive(false);
                wayfinders.Add(new WayMark { Chip = chip, Arrow = arrow, Label = label, Poi = poi, Title = title });
            }
            Add(Nearest("zone"), "Bãi yêu thú", new Color32(244, 110, 90, 255));
            Add(Nearest("dungeon"), "Cổ động", new Color32(186, 132, 255, 255));
            UpdateWayfinders();
        }

        private void UpdateWayfinders()
        {
            if (worldView?.Player == null || wayfinderRoot == null || wayfinders.Count == 0) return;
            var half = wayfinderRoot.rect.size * .5f;
            if (half.x < 10f || half.y < 10f) return;
            var placed = 0;
            foreach (var way in wayfinders)
            {
                if (way.Chip == null) continue;
                var tile = new Vector2(way.Poi.x, way.Poi.y);
                var pos = worldView.LocalToViewport(worldView.TileToLocal(tile));
                var visible = Mathf.Abs(pos.x) < half.x - 80f && Mathf.Abs(pos.y) < half.y - 80f;
                if (way.Chip.gameObject.activeSelf == visible) way.Chip.gameObject.SetActive(!visible);
                if (visible) continue;
                // Keep distant targets in one legible route list below the Places buttons.
                way.Chip.anchoredPosition = new Vector2(-half.x + 224f, half.y - 276f - placed * 74f);
                way.Arrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg);
                var distance = Mathf.RoundToInt(Vector2.Distance(worldView.Player.Pos, tile));
                if (distance != way.Shown)
                {
                    way.Shown = distance;
                    way.Label.text = way.Title + " · " + distance + " dặm";
                }
                placed++;
            }
        }

        /// <summary>Editor captures: take off, move over the nearest rock or forest and hold the pose with its trail.</summary>
        internal void PreviewFlight()
        {
            if (worldView?.Player == null || worldData == null) return;
            ApplyWorldTravel(true);
            var start = worldView.TileOf(worldView.Player.Pos);
            var over = start;
            for (var r = 5; r <= 40 && over == start; r++)
                for (var a = 0; a < 16 && over == start; a++)
                {
                    var x = start.x + Mathf.RoundToInt(Mathf.Cos(a * Mathf.PI / 8f) * r);
                    var y = start.y + Mathf.RoundToInt(Mathf.Sin(a * Mathf.PI / 8f) * r);
                    if (worldData.InBounds(x, y) && worldData.IsBlocked(x, y) && !worldData.IsBlocked(x, y, true) && !worldData.IsWater(x, y)) over = new Vector2Int(x, y);
                }
            worldView.Teleport(worldView.Player, over);
            worldView.SnapTravel(new Vector2(1f, -.35f));
        }

    }

    /// <summary>Drives periodic world refreshes while the HUD exists.</summary>
    internal sealed class WorldHudTicker : MonoBehaviour
    {
        public PrototypeBootstrap Owner;
        private void Update() { if (Owner != null) Owner.WorldTick(); }
    }

    /// <summary>Compact appearance spec ("g=f;hc=#2a2024;...") shared by the creator, server and world sprites.</summary>
    public sealed class LookSpec
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public static LookSpec Parse(string text)
        {
            var spec = new LookSpec();
            if (string.IsNullOrEmpty(text)) return spec;
            foreach (var part in text.Split(';'))
            {
                var i = part.IndexOf('=');
                if (i <= 0) continue;
                spec.values[part.Substring(0, i).Trim()] = part.Substring(i + 1).Trim();
            }
            return spec;
        }

        public bool Has(string key) => values.ContainsKey(key);
        public string Get(string key, string fallback = "") => values.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
        public int Int(string key, int fallback = 0) => int.TryParse(Get(key, null), out var v) ? v : fallback;
        public void Set(string key, string value) => values[key] = value;
        public void Set(string key, int value) => values[key] = value.ToString();

        /// <summary>Adds every key of defaults this look does not have (older saved looks gain new options).</summary>
        public LookSpec Fill(LookSpec defaults)
        {
            if (defaults != null)
                foreach (var pair in defaults.values)
                    if (!values.ContainsKey(pair.Key) || string.IsNullOrEmpty(values[pair.Key])) values[pair.Key] = pair.Value;
            return this;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var pair in values) parts.Add(pair.Key + "=" + pair.Value);
            return string.Join(";", parts);
        }
    }
}
