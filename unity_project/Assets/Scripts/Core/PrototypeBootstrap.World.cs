using System;
using System.Collections.Generic;
using UnityEngine;
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
            worldView = ProvinceWorld.Build(backgroundRoot, data, painting);
            worldView.transform.SetAsFirstSibling();
            var player = hub["player"];
            var spawn = ResolveWorldSpawn(data, player, town["id"].Str());
            var me = worldView.AddActor("me", "player", spawn, HeroFramesFor(player), null, HeroSize,
                Clean(player["name"].Str("Đạo hữu")), new Color32(255, 240, 200, 255));
            me.Speed = 4.6f;
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
            SpawnWorldNpcs(data, town["id"].Str());
            var phase = hub["timePhase"]["phase"].Str();
            worldView.SetNight(phase == "night" ? 1f : phase == "evening" ? .55f : phase == "dawn" ? .25f : 0f);
            BuildWorldHud(data);
            nextMonsterRefresh = Time.time + 15f;
            worldReturnTile = null;
        }

        private Vector2 ResolveWorldSpawn(WorldMapData data, J player, string townId)
        {
            if (worldReturnTile.HasValue)
            {
                var t = worldReturnTile.Value;
                var open = data.NearestOpen(new Vector2Int(Mathf.RoundToInt(t.x), Mathf.RoundToInt(t.y)));
                return open;
            }
            var pos = player["worldPosition"];
            if (pos.IsObject && pos["mapId"].Str() == data.id)
            {
                var cell = new Vector2Int(pos["x"].Int(), pos["y"].Int());
                if (data.InBounds(cell.x, cell.y) && !data.IsBlocked(cell.x, cell.y)) return cell;
            }
            var town = data.Town(townId);
            if (town?.spawn != null && town.spawn.Length >= 2) return data.NearestOpen(new Vector2Int(town.spawn[0], town.spawn[1]));
            return data.NearestOpen(new Vector2Int(data.w / 2, data.h / 2), 20);
        }

        private static readonly Vector2 HeroSize = new Vector2(HeroSprites.FrameW * .62f, HeroSprites.FrameH * .62f);

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
                        worldView.AddLabel(InkUi.Tag(worldView.LabelLayer, poi.label, 19), new Vector2(poi.x, poi.y - 3.6f), Vector2.zero);
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
                var tag = InkUi.Tag(worldView.LabelLayer, "Bãi Yêu Thú", 17, new Color32(255, 196, 170, 255));
                tag.GetComponent<Image>().color = new Color(1, 1, 1, .8f);
                worldView.AddLabel(tag, new Vector2(zone.x + zone.w * .5f, zone.y - .4f), Vector2.zero);
            }
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
                    range = boss ? new RectInt(Mathf.Max(2, zone.x - 22), Mathf.Max(2, zone.y - 16), zone.w + 44, zone.h + 32) : new RectInt(zone.x, zone.y, zone.w, zone.h);
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
                var size = boss ? new Vector2(78, 78) : new Vector2(46, 46);
                var name = Clean(m["name"].Str());
                if (boss) name = (m["isWorldBoss"].Bool() ? "Boss Thế Giới · " : "Đại Boss · ") + name;
                Color tagColor = boss ? (Color)new Color32(255, 120, 96, 255) : (Color)new Color32(250, 232, 210, 255);
                var actor = worldView.AddActor(uid, boss ? "boss" : "monster", tile, null, sprite, size, name + "  " + Clean(m["realmName"].Str()), tagColor, aura: boss);
                actor.Data = m;
                actor.Pressure = boss;
                actor.PressureColor = m["isWorldBoss"].Bool() ? new Color(1f, .78f, .3f, 1f) : new Color(1f, .3f, .22f, 1f);
                actor.Roams = true;
                actor.Range = range;
                actor.Speed = boss ? 1.6f : 1.9f;
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

        private void SpawnWorldNpcs(WorldMapData data, string townId)
        {
            var town = data.Town(townId);
            if (town?.gate == null) return;
            var index = 0;
            foreach (var npc in hub["npcs"].Items)
            {
                if (index >= 3) break;
                var npcLook = HeroSprites.RandomLook(npc["id"].Str(), npc["gender"].Str() == "nu");
                var frames = HeroSprites.Get(npcLook);
                var tile = data.NearestOpen(new Vector2Int(town.gate[0] + (index - 1) * 6 + (index == 1 ? 3 : 0), town.gate[1] + 5 + index % 2 * 2));
                var actor = worldView.AddActor("npc_" + npc["id"].Str(), "npc", tile, frames, null, HeroSize,
                    Clean(npc["name"].Str()) + " · " + Clean(npc["realmName"].Str()), new Color32(170, 220, 255, 255));
                var npcRealm = npc["realmIndex"].Int(npc["realm"].Int());
                worldView.SetAura(actor, npcLook, AvatarComposer.AuraStrength(npcRealm));
                actor.Pressure = npcRealm >= 8;
                actor.PressureColor = HeroSprites.ParseColor(npcLook.Get("auc"), new Color32(140, 220, 255, 255));
                actor.Data = npc;
                actor.Roams = true;
                actor.Speed = 1.4f;
                actor.Range = new RectInt(tile.x - 3, tile.y - 1, 7, 4);
                index++;
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
            if (townId == hub["town"]["id"].Str()) { ShowCity(townId); return; }
            EnterTownThen(townId, () => ShowCity(townId));
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
            if (tile == lastSavedTile) return;
            lastSavedTile = tile;
            client.SaveWorldPosition(worldMapId, tile.x, tile.y, (ok, error) => { if (!ok) Debug.LogWarning("world/move: " + error); });
        }

        private void TickWorld()
        {
            if (worldView == null || offlinePreview) return;
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
            IconButton(root, "ui:search", new Vector2(1, 0), new Vector2(-136, 140), 76f, () => worldView?.ZoomBy(1.25f));
            var minus = IconButton(root, "arrowLeft", new Vector2(1, 0), new Vector2(-136, 60), 76f, () => worldView?.ZoomBy(1f / 1.25f));
            var minusLabel = AnchoredText(minus.transform, "Minus", "–", ModernUi.Bold, 46, HudCream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            minus.transform.Find("Icon")?.gameObject.SetActive(false);
            minusLabel.raycastTarget = false;
        }

        private void ShowHudAction(string label, Action click)
        {
            if (hudActionRect == null) return;
            hudActionLabel.text = Clean(label);
            hudActionCallback = click;
            hudActionRect.gameObject.SetActive(true);
        }

        private void HideHudAction()
        {
            if (hudActionRect != null) hudActionRect.gameObject.SetActive(false);
            hudActionCallback = null;
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
            back.color = new Color(1, 1, 1, .92f);
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
            portrait.uvRect = new Rect(.30f, .69f, .44f, .2625f);   // head and shoulders
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
            miniPlayerDot = Anchored("Me", map, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-9, -9), new Vector2(9, 9));
            var me = miniPlayerDot.gameObject.AddComponent<Image>();
            me.sprite = InkUi.Glow;
            me.color = new Color32(255, 80, 60, 255);
            me.raycastTarget = false;
            UpdateMiniPlayer();
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => OpenTeleportScreen(true));
            AnchoredText(frame, "Hint", "Thiên hạ", ModernUi.SemiBold, 18, HudCream, TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(10, 12), new Vector2(-16, 0));
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
                disc.color = new Color32(26, 28, 30, 235);
                var icon = Anchored("Icon", rect, Vector2.zero, Vector2.one, new Vector2(22, 26), new Vector2(-22, -18)).gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(item.icon);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = AnchoredText(rect, "Label", item.label, ModernUi.SemiBold, 17, HudCream, TextAnchor.LowerCenter, new Vector2(-.3f, 0), new Vector2(1.3f, 0), new Vector2(0, -22), new Vector2(0, 2));
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
