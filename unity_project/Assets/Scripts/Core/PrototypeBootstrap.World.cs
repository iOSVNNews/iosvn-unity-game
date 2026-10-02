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
        private WorldHudTicker hudTicker;
        private bool hudActionAuto;
        private WorldPoi promptPoi;
        private TravelMode worldTravel;
        private Text travelLabel;
        /// <summary>Bumped when the province maps are redrawn at another size: saved tiles of an older layout are dropped once.</summary>
        private const int WorldLayoutRev = 2;

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
                        if (key != worldMapId && PaintingCache[key] != null) { Destroy(PaintingCache[key]); PaintingCache.Remove(key); break; }
                }
                PaintingCache[mapId] = texture;
            }
            return texture;
        }

        /// <summary>Main game screen. Loads the player's province and places the character in it.</summary>
        private void ShowWorld()
        {
            if (hub.IsNull) { RefreshHub(ShowWorld); return; }
            var town = hub["town"];
            var mapId = town["mapId"].Str();
            var data = WorldMapData.Load(mapId);
            var painting = data == null ? null : GetPainting(mapId);
            if (data == null || painting == null)
            {
                if (latestState != null) ShowHome(latestState);
                Toast("Thiếu dữ liệu bản đồ " + mapId + " trong bản cài.", true);
                return;
            }
            SetAtlasOrientation(false);
            ClearContent();
            ClearBattleScene();
            worldData = data;
            worldMapId = mapId;
            worldMonsterActors.Clear();
            if (worldView != null) { Destroy(worldView.gameObject); worldView = null; }
            worldView = ProvinceWorld.Build(backgroundRoot, data, painting);
            worldView.transform.SetAsFirstSibling();
            var player = hub["player"];
            var spawn = ResolveWorldSpawn(data, player, town["id"].Str());
            var me = worldView.AddActor("me", "player", spawn, HeroFramesFor(player), null, HeroSize,
                Clean(player["name"].Str("Đạo hữu")), new Color32(255, 240, 200, 255));
            worldView.WalkSpeed = 3.2f;
            me.Speed = worldView.WalkSpeed;
            var myRealm = hub["realm"]["index"].Int();
            worldView.SetAura(me, LookOf(player), AvatarComposer.AuraStrength(myRealm));
            me.Pressure = myRealm >= 6;
            me.PressureColor = HeroSprites.ParseColor(LookOf(player).Get("auc", "#8fe0ff"), new Color32(140, 220, 255, 255));
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
            BuildWorldLabels(data);
            SyncWorldMonsters(hub["worldMonsters"]);
            // cultivators (NPCs) are met inside the cities, not out on the map
            var phase = hub["timePhase"]["phase"].Str();
            worldView.SetNight(phase == "night" ? 1f : phase == "evening" ? .55f : phase == "dawn" ? .25f : 0f);
            promptPoi = null;
            BuildWorldHud(data);
            nextMonsterRefresh = Time.time + 15f;
            worldReturnTile = null;
            if (worldTravel != TravelMode.Walk) ApplyWorldTravel(true);
            UpdatePlacePrompt(worldView.TileOf(me.Pos));
        }

        private Vector2 ResolveWorldSpawn(WorldMapData data, J player, string townId)
        {
            if (worldReturnTile.HasValue)
            {
                var t = worldReturnTile.Value;
                var open = data.NearestOpen(new Vector2Int(Mathf.RoundToInt(t.x), Mathf.RoundToInt(t.y)), 40);
                if (!data.IsBlocked(open.x, open.y)) return open;
            }
            var pos = player["worldPosition"];
            if (pos.IsObject && pos["mapId"].Str() == data.id && PlayerPrefs.GetInt("tt_world_layout", 1) == WorldLayoutRev)
            {
                var cell = new Vector2Int(pos["x"].Int(), pos["y"].Int());
                if (data.InBounds(cell.x, cell.y) && !data.IsBlocked(cell.x, cell.y)) return cell;
            }
            var town = data.Town(townId);
            if (town?.spawn != null && town.spawn.Length >= 2) return data.NearestOpen(new Vector2Int(town.spawn[0], town.spawn[1]));
            return data.NearestOpen(new Vector2Int(data.w / 2, data.h / 2), 20);
        }

        /// <summary>Figures are small against the land: a city wall is several times taller than a cultivator.</summary>
        private static readonly Vector2 HeroSize = new Vector2(HeroSprites.FrameW * .40f, HeroSprites.FrameH * .40f);

        /// <summary>The player's layered look (creator look, or one derived from the legacy appearance).</summary>
        private static LookSpec LookOf(J player)
        {
            // lookWorn = the creator look with the equipped weapon / armour applied by the server
            var text = player["lookWorn"].Str(player["look"].Str());
            if (!string.IsNullOrEmpty(text))
            {
                var look = LookSpec.Parse(text);
                return look.Fill(AvatarComposer.Default(look.Get("g", player["gender"].Str() == "nu" ? "f" : "m") == "f"));
            }
            var colors = player["appearanceColors"];
            return HeroSprites.LegacyLook(player["gender"].Str() == "nu", colors["hair"].Str(), colors["outfit"].Str(), colors["eyes"].Str());
        }

        private Sprite[] HeroFramesFor(J player) => HeroSprites.Get(LookOf(player));

        // ------------------------------------------------------------------ labels

        private void BuildWorldLabels(WorldMapData data)
        {
            foreach (var poi in data.pois)
            {
                if (poi == null) continue;
                switch (poi.kind)
                {
                    case "city":
                        if (poi.rect == null) break;
                        var banner = InkUi.VerticalBanner(worldView.LabelLayer, poi.label, poi.big ? 24 : 21);
                        worldView.AddLabel(banner, new Vector2(poi.rect[0] - .2f, poi.rect[1] + 1.5f), new Vector2(0, 0));
                        break;
                    case "dungeon":
                        worldView.AddLabel(PlaceTag("Cổ động · " + poi.label, "co_dong", 21, new Color32(226, 206, 255, 255)), new Vector2(poi.x, poi.y - 3.6f), Vector2.zero);
                        break;
                    case "portal":
                        worldView.AddLabel(InkUi.Tag(worldView.LabelLayer, poi.label, 19, new Color32(206, 190, 255, 255)), new Vector2(poi.x + .5f, poi.y - 4f), Vector2.zero);
                        break;
                    case "landmark":
                        if (!string.IsNullOrEmpty(poi.label)) worldView.AddLabel(InkUi.Tag(worldView.LabelLayer, poi.label, 17), new Vector2(poi.x, poi.y - 3.2f), Vector2.zero);
                        break;
                }
            }
            foreach (var zone in data.zones)
            {
                if (zone == null) continue;
                var zoneTown = data.Town(zone.townId);
                var tag = PlaceTag("Bãi yêu thú" + (zoneTown != null ? " · " + Clean(zoneTown.name) : ""), "swords", 21, new Color32(255, 204, 180, 255));
                worldView.AddLabel(tag, new Vector2(zone.x + zone.w * .5f, zone.y - .4f), Vector2.zero);
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
                var bossPrefix = m["isWorldBoss"].Bool() ? "[Thế Giới] " : "[Đại Boss] ";
                var displayName = (boss ? bossPrefix : "") + name + " · " + Clean(m["realmName"].Str());
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
                case "city":
                    ShowHudAction("Vào thành · " + poi.label, () => WalkToPoi(poi, () => EnterCityFromWorld(poi.townId)));
                    WalkToPoi(poi, () => EnterCityFromWorld(poi.townId));
                    break;
                case "dungeon":
                    WalkToPoi(poi, () => OpenDungeonDialog(poi));
                    break;
                case "portal":
                    WalkToPoi(poi, () => OpenTeleportScreen(true));
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
                ShowHudAction("Khiêu chiến · " + Clean(m["name"].Str()), () => worldView.Approach(actor, 1.4f, () => StartWorldBattle(uid, m)));
                worldView.Approach(actor, 1.4f, () => StartWorldBattle(uid, m));
            }
            else if (actor.Kind == "npc")
            {
                var npc = actor.Data is J j ? j : J.Null;
                worldView.Approach(actor, 1.6f, () => OpenNpcDialog(npc));
            }
        }

        private void StartWorldBattle(string uid, J monster)
        {
            if (IsBossMonster(monster) && monster["requiredPartySize"].Int(1) > 1 && !monster["partyOk"].Bool(true))
            {
                Confirm("Boss cần tổ đội", $"{Clean(monster["name"].Str())} yêu cầu tổ đội tối thiểu {monster["requiredPartySize"].Int()} người. Vẫn thử khiêu chiến?",
                    "Khiêu chiến", () => Hunt(uid));
                return;
            }
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
            if (string.IsNullOrEmpty(townId)) return;
            worldReturnTile = worldView != null ? worldView.Player.Pos : (Vector2?)null;
            var gate = worldData?.Town(townId)?.gate;
            if (gate != null && gate.Length >= 2) worldReturnTile = new Vector2(gate[0], gate[1] + 1);
            worldTravel = TravelMode.Walk;
            void Open()
            {
                try { ShowCity(townId); }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
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
                if (poi == null || (poi.kind != "city" && poi.kind != "dungeon" && poi.kind != "portal")) continue;
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
            else ShowHudAction("Cổng Ngự Kiếm", () => OpenTeleportScreen(true));
            hudActionAuto = true;
        }

        /// <summary>Walking into another city of the same province makes it the player's current town.</summary>
        private void EnterTownThen(string townId, Action then)
        {
            var tile = worldView != null ? worldView.TileOf(worldView.Player.Pos) : new Vector2Int(-1, -1);
            ShowBusy(true);
            client.Post("/world/enter-town", Body("townId", townId, "mapId", worldMapId, "x", tile.x, "y", tile.y), (result, error) =>
            {
                ShowBusy(false);
                if (error != null) { Toast(error, true); return; }
                if (result["state"].IsObject) AcceptState(result["state"]);
                then?.Invoke();
            });
        }

        // ------------------------------------------------------------------ position sync

        private void OnWorldStep()
        {
            if (worldView?.Player == null) return;
            var tile = worldView.TileOf(worldView.Player.Pos);
            UpdateMiniPlayer();
            UpdatePlacePrompt(tile);
            if (Time.time >= nextWorldSave && tile != lastSavedTile)
            {
                nextWorldSave = Time.time + 6f;
                SaveWorldTile();
            }
            UpdateHudLocation(tile);
        }

        private void SaveWorldTile()
        {
            if (worldView?.Player == null || offlinePreview) return;
            var tile = worldView.TileOf(worldView.Player.Pos);
            // in the air the saved tile is the ground below (or beside) the rider, so a later login lands somewhere walkable
            if (worldData != null && worldData.IsBlocked(tile.x, tile.y))
            {
                tile = worldData.NearestOpen(tile, 30);
                if (worldData.IsBlocked(tile.x, tile.y)) return;
            }
            if (tile == lastSavedTile) return;
            lastSavedTile = tile;
            client.SaveWorldPosition(worldMapId, tile.x, tile.y, (ok, error) =>
            {
                if (ok) PlayerPrefs.SetInt("tt_world_layout", WorldLayoutRev);
                else Debug.LogWarning("world/move: " + error);
            });
        }

        private void TickWorld()
        {
            if (worldView == null || offlinePreview) return;
            UpdateWayfinders();
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
            // The painted map is bright: shade the edges the HUD sits on, so panels, icons and names stand out.
            void Scrim(string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color from, Color to, bool horizontal)
            {
                var image = Anchored(name, root, min, max, offsetMin, offsetMax).gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                UiGradient.Apply(image, from, to, horizontal);
            }
            var clear = new Color(0f, 0f, 0f, 0f);
            Scrim("ScrimRight", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-460, 0), Vector2.zero, clear, new Color(.02f, .03f, .05f, .52f), true);
            Scrim("ScrimTop", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -190), Vector2.zero, new Color(.02f, .03f, .05f, .42f), clear, false);
            Scrim("ScrimBottom", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 250), clear, new Color(.02f, .03f, .05f, .40f), false);
            BuildAvatarCard(root);
            BuildMiniMap(root, data);
            BuildVirtualDpad(root);
            BuildTravelButton(root);
            BuildWayfinders(root);
            var places = Anchored("Places", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -196), new Vector2(284, -124));
            PillButton(places, "Địa điểm", "ui:location", false, OpenPlacesModal);
            var placesFill = places.GetComponent<Image>();
            if (placesFill != null) placesFill.color = new Color32(16, 18, 22, 244);
            BuildMenuColumn(root);
            // location banner (top-left)
            var loc = Anchored("Location", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -112), new Vector2(720, -20));
            var brush = loc.gameObject.AddComponent<Image>();
            brush.sprite = InkUi.Brush;
            brush.type = Image.Type.Sliced;
            brush.raycastTarget = false;
            hudLocation = AnchoredText(loc, "Name", "", ModernUi.Display, 34, HudCream, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(40, 18), new Vector2(-30, -6));
            hudPhase = AnchoredText(loc, "Phase", "", ModernUi.Regular, 20, new Color32(214, 206, 190, 255), TextAnchor.LowerLeft, Vector2.zero, Vector2.one, new Vector2(42, 6), new Vector2(-30, -50));
            UpdateHudLocation(worldView.TileOf(worldView.Player.Pos));
            // context action (bottom-right, above the menu)
            hudActionRect = Anchored("Action", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-640, 30), new Vector2(-160, 126));
            var actionFill = hudActionRect.gameObject.AddComponent<Image>();
            ModernUi.Fill(actionFill, 30f);
            UiGradient.Apply(actionFill, AuthGoldTop, AuthGoldBottom);
            var actionButton = hudActionRect.gameObject.AddComponent<Button>();
            actionButton.targetGraphic = actionFill;
            actionButton.onClick.AddListener(() => hudActionCallback?.Invoke());
            hudActionRect.gameObject.AddComponent<UiPressScale>();
            hudActionLabel = AnchoredText(hudActionRect, "Text", "", ModernUi.SemiBold, 28, AuthInkOnGold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
            hudActionLabel.resizeTextForBestFit = true; hudActionLabel.resizeTextMinSize = 18; hudActionLabel.resizeTextMaxSize = 28;
            HideHudAction();
            // zoom buttons (bottom-right corner)
            var plus = IconButton(root, "ui:search", new Vector2(1, 0), new Vector2(-136, 140), 76f, () => worldView?.ZoomBy(1.25f));
            var minus = IconButton(root, "arrowLeft", new Vector2(1, 0), new Vector2(-136, 60), 76f, () => worldView?.ZoomBy(1f / 1.25f));
            var minusLabel = AnchoredText(minus.transform, "Minus", "–", ModernUi.Bold, 46, HudCream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            minus.transform.Find("Icon")?.gameObject.SetActive(false);
            minusLabel.raycastTarget = false;
            // solid discs: the default glass button vanishes on the bright painting
            foreach (var zoom in new[] { minus, plus })
            {
                var fill = zoom.GetComponent<Image>();
                if (fill != null) fill.color = new Color32(16, 18, 22, 244);
            }
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
            hudLocation.text = Clean(worldData.name) + (place != null ? "  ·  " + Clean(place) : "");
            var phase = hub["timePhase"];
            hudPhase.text = Clean(phase["name"].Str("Ban ngày")) + "  ·  " + Clean(hub["realm"]["name"].Str()) + " " + Clean(hub["realm"]["sub"].Str());
        }

        private void BuildAvatarCard(RectTransform root)
        {
            var player = hub["player"];
            var card = Anchored("Avatar", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 18), new Vector2(720, 220));
            var back = card.gameObject.AddComponent<Image>();
            back.sprite = InkUi.Brush;
            back.type = Image.Type.Sliced;
            back.color = Color.white;
            back.raycastTarget = false;
            // portrait disc
            var disc = Anchored("Disc", card, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -100), new Vector2(200, 100));
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
            portrait.texture = AvatarComposer.Available ? AvatarComposer.Compose(LookOf(player)) : null;
            portrait.uvRect = new Rect(.28f, .69f, .44f, .2625f);   // head and shoulders of the front-view portrait
            portrait.raycastTarget = false;
            var discButton = disc.gameObject.AddComponent<Button>();
            discButton.onClick.AddListener(OpenCharacterScreen);
            disc.gameObject.AddComponent<UiPressScale>();
            AnchoredText(card, "Name", Clean(player["name"].Str("Đạo hữu")), ModernUi.SemiBold, 30, HudCream, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(212, -64), new Vector2(-30, -14));
            AnchoredText(card, "Realm", Clean(hub["realm"]["name"].Str()) + " · " + Clean(player["monName"].Str()), ModernUi.Regular, 20, HudGold, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(214, -96), new Vector2(-30, -62));
            HudBar(card, "Khí huyết", player["hp"].Num(), player["maxHp"].Num(1), new Color32(196, 62, 54, 255), 112);
            var stamina = player["stamina"].Num();
            HudBar(card, "Thể lực", stamina, player["staminaMax"].Num(1), new Color32(92, 170, 110, 255), 78);
            var coin = Anchored("Stones", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(212, 10), new Vector2(-30, 52));
            var coinIcon = Anchored("Icon", coin, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -16), new Vector2(32, 16)).gameObject.AddComponent<Image>();
            coinIcon.sprite = UiPixelIcon("coin");
            coinIcon.preserveAspect = true;
            coinIcon.raycastTarget = false;
            AnchoredText(coin, "Value", Vn(player["stones"]) + " linh thạch", ModernUi.SemiBold, 22, HudCream, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(40, 0), Vector2.zero);
        }

        private void HudBar(RectTransform card, string label, double value, double max, Color color, float y)
        {
            var bar = Anchored("Bar_" + label, card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(212, y), new Vector2(-34, y + 26));
            var track = bar.gameObject.AddComponent<Image>();
            ModernUi.Fill(track, 12f);
            track.color = new Color(0, 0, 0, .55f);
            track.raycastTarget = false;
            var fill = Anchored("Fill", bar, Vector2.zero, new Vector2(Mathf.Clamp01((float)(value / Math.Max(1, max))), 1), new Vector2(2, 2), new Vector2(-2, -2)).gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 10f);
            fill.color = color;
            fill.raycastTarget = false;
            AnchoredText(bar, "Text", $"{label}  {Vn(value)}/{Vn(max)}", ModernUi.SemiBold, 17, HudCream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void BuildMiniMap(RectTransform root, WorldMapData data)
        {
            var frame = Anchored("MiniMap", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-400, -272), new Vector2(-20, -20));
            var bg = frame.gameObject.AddComponent<Image>();
            ModernUi.Fill(bg, 18f);
            bg.color = HudInk;

            var border = Anchored("Border", frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            ModernUi.Ring(border, 18f, 1.8f);
            border.color = HudGold;
            border.raycastTarget = false;

            var map = Anchored("Map", frame, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            var raw = map.gameObject.AddComponent<RawImage>();
            raw.texture = worldView.Painting.texture;
            raw.raycastTarget = false;
            miniMapRect = map;
            foreach (var town in data.towns)
            {
                if (town?.gate == null) continue;
                var dot = Anchored("Town", map, new Vector2((town.gate[0] + .5f) / data.w, 1f - (town.gate[1] + .5f) / data.h), new Vector2((town.gate[0] + .5f) / data.w, 1f - (town.gate[1] + .5f) / data.h), new Vector2(-7, -7), new Vector2(7, 7)).gameObject.AddComponent<Image>();
                dot.sprite = InkUi.Glow;
                dot.color = HudGold;
                dot.raycastTarget = false;
            }
            // caves, hunting grounds and the sword gate, so the small map answers "where is it"
            foreach (var poi in data.pois)
            {
                if (poi == null || (poi.kind != "dungeon" && poi.kind != "zone" && poi.kind != "portal")) continue;
                var at = new Vector2((poi.x + .5f) / data.w, 1f - (poi.y + .5f) / data.h);
                var half = poi.kind == "zone" ? 7f : 6f;
                var mark = Anchored("Mark_" + poi.kind, map, at, at, new Vector2(-half, -half), new Vector2(half, half)).gameObject.AddComponent<Image>();
                mark.sprite = InkUi.Glow;
                mark.color = poi.kind == "zone" ? new Color32(244, 84, 64, 255) : poi.kind == "dungeon" ? new Color32(186, 132, 255, 255) : new Color32(110, 220, 255, 255);
                mark.raycastTarget = false;
            }
            miniPlayerDot = Anchored("Me", map, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-9, -9), new Vector2(9, 9));
            var me = miniPlayerDot.gameObject.AddComponent<Image>();
            me.sprite = InkUi.Glow;
            me.color = new Color32(255, 80, 60, 255);
            me.raycastTarget = false;
            UpdateMiniPlayer();
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => OpenTeleportScreen(true));

            // North badge
            var northBadge = Anchored("North", frame, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-26, -18), new Vector2(26, 6));
            var nimg = northBadge.gameObject.AddComponent<Image>();
            ModernUi.Fill(nimg, 10f);
            nimg.color = new Color32(18, 20, 24, 220);
            nimg.raycastTarget = false;
            var nText = AnchoredText(northBadge, "N", "BẮC", ModernUi.Bold, 13, HudGold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            nText.raycastTarget = false;

            AnchoredText(frame, "Hint", "Thiên hạ · " + Clean(data.name), ModernUi.SemiBold, 17, HudCream, TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-16, 0));
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
            var u = (p.x + .5f) / worldData.w;
            var v = 1f - (p.y + .5f) / worldData.h;
            miniPlayerDot.anchorMin = miniPlayerDot.anchorMax = new Vector2(u, v);
        }

        private void BuildMenuColumn(RectTransform root)
        {
            var items = new (string icon, string label, Action click)[]
            {
                ("hanh_trang", "Hành trang", () => OpenBagScreen()),
                ("ho_so", "Nhân vật", OpenCharacterScreen),
                ("cong_phap", "Công pháp", () => OpenSkillsScreen()),
                ("tong_mon", "Tông môn", () => OpenSectScreen()),
                ("ban_be", "Xã giao", () => OpenSocialScreen()),
                ("scroll", "Nhiệm vụ", () => OpenBountyScreen()),
                ("power", "Xếp hạng", () => OpenRankScreen()),
                ("mail", "Hòm thư", () => OpenInboxScreen()),
                ("sun", "Sự kiện", () => OpenEventsScreen()),
            };
            var x = -110f;
            var y = -300f;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var size = 92f;
                var rect = Anchored("Menu_" + item.label, root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(x - size / 2, y - size), new Vector2(x + size / 2, y));
                var disc = rect.gameObject.AddComponent<Image>();
                disc.sprite = InkUi.Glow;
                disc.color = new Color32(16, 18, 22, 250);
                var rim = Anchored("Rim", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                rim.sprite = InkUi.Ring;
                rim.color = new Color32(232, 196, 120, 210);
                rim.raycastTarget = false;
                var icon = Anchored("Icon", rect, Vector2.zero, Vector2.one, new Vector2(22, 26), new Vector2(-22, -18)).gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(item.icon);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = AnchoredText(rect, "Label", item.label, ModernUi.SemiBold, 18, Color.white, TextAnchor.LowerCenter, new Vector2(-.3f, 0), new Vector2(1.3f, 0), new Vector2(0, -24), new Vector2(0, 2));
                label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = disc;
                var click = item.click;
                button.onClick.AddListener(() => click());
                rect.gameObject.AddComponent<UiPressScale>();
                if (item.icon == "mail" && hub["inboxUnread"].Int() > 0)
                {
                    var badge = Anchored("Badge", rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(4, 4)).gameObject.AddComponent<Image>();
                    badge.sprite = InkUi.Glow;
                    badge.color = new Color32(230, 60, 48, 255);
                    AnchoredText(badge.transform, "N", hub["inboxUnread"].Int().ToString(), ModernUi.Bold, 16, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                y -= 118f;
                if (i == 4) { x -= 128f; y = -300f; }
            }
        }

        internal void WorldTick() => TickWorld();

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
            poi.kind == "city" ? "Thành" : poi.kind == "zone" ? "Bãi yêu thú" : poi.kind == "dungeon" ? "Cổ động" : poi.kind == "portal" ? "Cổng" : "Danh thắng";

        private static string PlaceIcon(WorldPoi poi) =>
            poi.kind == "city" ? "location" : poi.kind == "zone" ? "swords" : poi.kind == "dungeon" ? "co_dong" : poi.kind == "portal" ? "teleport" : "scroll";

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

        /// <summary>Every city, hunting ground, cave and gate of the province with its distance; one tap walks there.</summary>
        private void OpenPlacesModal()
        {
            if (worldView?.Player == null || worldData == null) return;
            var card = Modal("Địa điểm · " + Clean(worldData.name), 1180f, 800f, out var close);
            var area = Anchored("List", card, Vector2.zero, Vector2.one, new Vector2(22, 22), new Vector2(-22, -98));
            var list = ScrollColumn(area, 8f, 6);
            var me = worldView.Player.Pos;
            var order = new Dictionary<string, int> { { "city", 0 }, { "zone", 1 }, { "dungeon", 2 }, { "portal", 3 }, { "landmark", 4 } };
            var entries = new List<(int rank, float distance, WorldPoi poi)>();
            foreach (var poi in worldData.pois)
                if (poi != null && order.TryGetValue(poi.kind ?? "", out var rank) && !string.IsNullOrEmpty(poi.label))
                    entries.Add((rank, Vector2.Distance(me, new Vector2(poi.x, poi.y)), poi));
            entries.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : a.distance.CompareTo(b.distance));
            var accents = new Color[] { HudGold, new Color32(244, 110, 90, 255), new Color32(186, 132, 255, 255), new Color32(110, 220, 255, 255), new Color32(170, 180, 170, 255) };
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
                var chip = Anchored("Way_" + poi.kind, root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-150, -30), new Vector2(150, 30));
                var fill = chip.gameObject.AddComponent<Image>();
                ModernUi.Fill(fill, 28f);
                fill.color = new Color32(16, 18, 22, 240);
                var edge = Anchored("Edge", chip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                ModernUi.Ring(edge, 28f, 1.6f);
                edge.color = accent;
                edge.raycastTarget = false;
                var icon = Anchored("Icon", chip, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -19), new Vector2(52, 19)).gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(PlaceIcon(poi));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = AnchoredText(chip, "Text", title, ModernUi.SemiBold, 20, HudCream, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(60, 0), new Vector2(-50, 0));
                label.raycastTarget = false;
                var arrow = Anchored("Arrow", chip, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-46, -16), new Vector2(-14, 16));
                var arrowImage = arrow.gameObject.AddComponent<Image>();
                arrowImage.sprite = ModernUi.Icon("arrowRight");
                arrowImage.color = accent;
                arrowImage.raycastTarget = false;
                var button = chip.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
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
                // pinned to the edge of a box that keeps clear of the menu column, the mini map and the avatar card
                var boxX = half.x - 520f;
                var boxTop = half.y - 250f;
                var boxBottom = half.y - 300f;
                var scale = Mathf.Min(boxX / Mathf.Max(1f, Mathf.Abs(pos.x)), (pos.y >= 0 ? boxTop : boxBottom) / Mathf.Max(1f, Mathf.Abs(pos.y)));
                var at = pos * Mathf.Min(1f, scale);
                at.y -= placed * 70f;
                way.Chip.anchoredPosition = at;
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
