using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    [Serializable] public sealed class WorldTownMeta { public string id; public string name; public int x; public int y; public int w; public int h; public bool big; public int[] gate; public int[] spawn; public string regionId; }
    [Serializable] public sealed class WorldPoi { public string kind; public string townId; public string dungeonId; public string label; public int x; public int y; public int[] rect; public bool big; public string regionId; public string gateId; public string targetMapId; }
    [Serializable] public sealed class WorldZone { public string townId; public string label; public int x; public int y; public int w; public int h; public string regionId; }
    [Serializable] public sealed class WorldRegionMeta { public string id; public string name; public int x; public int y; public int w; public int h; public int realmMin; public string realmMinName; }

    /// <summary>
    /// Province source data or one merged realm world: logical grid (collision, water, roads),
    /// cities, points of interest and monster grounds shipped in Resources/World.
    /// Tile coordinates have (0,0) at the top-left of the painting.
    /// </summary>
    [Serializable]
    public sealed class WorldMapData
    {
        public string id;
        [NonSerialized] public string worldId;
        public string name;
        public string biome;
        public int w;
        public int h;
        public int tile = 16;
        public string block;
        public string water;
        public string road;
        public string border;
        public WorldTownMeta[] towns;
        public WorldPoi[] pois;
        public WorldZone[] zones;
        public WorldRegionMeta[] regions;

        [NonSerialized] public bool[] Blocked;
        [NonSerialized] public bool[] Water;
        [NonSerialized] public bool[] Road;
        /// <summary>Uncrossable world-edge terrain for flying travel; province joins are opened in the merged world.</summary>
        [NonSerialized] public bool[] Border;
        [NonSerialized] private float[] pathCost;
        [NonSerialized] private int[] pathCame;
        [NonSerialized] private int[] pathSeen;
        [NonSerialized] private int[] pathClosed;
        [NonSerialized] private int pathStamp;

        private static readonly Dictionary<string, WorldMapData> Cache = new Dictionary<string, WorldMapData>();

        public static WorldMapData Load(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;
            if (Cache.TryGetValue(mapId, out var cached)) return cached;
            var asset = Resources.Load<TextAsset>("World/" + mapId);
            if (asset == null) return null;
            WorldMapData data;
            try { data = JsonUtility.FromJson<WorldMapData>(asset.text); }
            catch (Exception ex) { Debug.LogWarning("World map " + mapId + " unreadable: " + ex.Message); return null; }
            if (data == null || data.w <= 0 || data.h <= 0) return null;
            data.Blocked = Bits(data.block, data.w * data.h);
            data.Water = Bits(data.water, data.w * data.h);
            data.Road = Bits(data.road, data.w * data.h);
            data.Border = string.IsNullOrEmpty(data.border) ? EdgeRing(data.w, data.h, 3) : Bits(data.border, data.w * data.h);
            data.towns = data.towns ?? Array.Empty<WorldTownMeta>();
            data.pois = data.pois ?? Array.Empty<WorldPoi>();
            data.zones = data.zones ?? Array.Empty<WorldZone>();
            data.regions = data.regions ?? Array.Empty<WorldRegionMeta>();
            Cache[mapId] = data;
            return data;
        }

        /// <summary>Builds one continuous walkable world from the province data belonging to a realm.</summary>
        public static WorldMapData LoadWorldForProvince(string provinceId)
        {
            if (string.IsNullOrEmpty(provinceId) || !provinceId.StartsWith("map_", StringComparison.Ordinal)
                || !int.TryParse(provinceId.Substring(4), out var provinceNumber)) return null;
            return LoadWorld(provinceNumber >= 9 ? "world_tien" : "world_pham");
        }

        public static WorldMapData LoadWorld(string worldId)
        {
            var immortal = worldId == "world_tien";
            if (!immortal && worldId != "world_pham") return null;
            if (Cache.TryGetValue(worldId, out var cached)) return cached;

            const int provinceWidth = 256;
            const int provinceHeight = 160;
            var columns = immortal ? 4 : 3;
            var rows = 3;
            var firstProvince = immortal ? 9 : 1;
            var provinceCount = immortal ? 11 : 8;
            var regions = new List<WorldRegionMeta>(provinceCount);
            var towns = new List<WorldTownMeta>();
            var pois = new List<WorldPoi>();
            var zones = new List<WorldZone>();
            var data = new WorldMapData
            {
                id = worldId,
                worldId = worldId,
                name = immortal ? "Tiên Giới" : "Phàm Giới",
                biome = immortal ? "tiên cảnh" : "phàm giới",
                w = columns * provinceWidth,
                h = rows * provinceHeight,
                tile = 16,
            };
            var total = data.w * data.h;
            data.Blocked = new bool[total];
            data.Water = new bool[total];
            data.Road = new bool[total];
            data.Border = new bool[total];

            for (var index = 0; index < provinceCount; index++)
            {
                var source = Load("map_" + (firstProvince + index));
                if (source == null || source.w != provinceWidth || source.h != provinceHeight) return null;
                var column = index % columns;
                var row = index / columns;
                var offsetX = column * provinceWidth;
                var offsetY = row * provinceHeight;
                regions.Add(new WorldRegionMeta { id = source.id, name = source.name, x = offsetX, y = offsetY, w = source.w, h = source.h });

                for (var y = 0; y < source.h; y++)
                {
                    var target = (offsetY + y) * data.w + offsetX;
                    var origin = y * source.w;
                    Array.Copy(source.Blocked, origin, data.Blocked, target, source.w);
                    Array.Copy(source.Water, origin, data.Water, target, source.w);
                    Array.Copy(source.Road, origin, data.Road, target, source.w);
                    Array.Copy(source.Border, origin, data.Border, target, source.w);
                }

                foreach (var town in source.towns)
                {
                    if (town == null) continue;
                    towns.Add(new WorldTownMeta
                    {
                        id = town.id, name = town.name, x = town.x + offsetX, y = town.y + offsetY,
                        w = town.w, h = town.h, big = town.big,
                        gate = Offset(town.gate, offsetX, offsetY), spawn = Offset(town.spawn, offsetX, offsetY), regionId = source.id
                    });
                }
                foreach (var poi in source.pois)
                {
                    if (poi == null || poi.kind == "portal") continue;
                    pois.Add(new WorldPoi
                    {
                        kind = poi.kind, townId = poi.townId, dungeonId = poi.dungeonId, label = poi.label,
                        x = poi.x + offsetX, y = poi.y + offsetY, rect = Offset(poi.rect, offsetX, offsetY), big = poi.big,
                        regionId = source.id
                    });
                }
                foreach (var zone in source.zones)
                {
                    if (zone == null) continue;
                    zones.Add(new WorldZone
                    {
                        townId = zone.townId, label = zone.label, x = zone.x + offsetX, y = zone.y + offsetY,
                        w = zone.w, h = zone.h, regionId = source.id
                    });
                }
            }

            // Generate paired free gates only between adjacent provinces in this realm.
            // Their IDs and coordinates match WORLD_PROVINCE_GATES in the game server.
            for (var index = 0; index < provinceCount; index++)
            {
                var column = index % columns;
                if (column + 1 < columns && index + 1 < provinceCount)
                    AddProvinceGate(pois, regions[index], regions[index + 1], true);
                if (index + columns < provinceCount)
                    AddProvinceGate(pois, regions[index], regions[index + columns], false);
            }

            var ascensionMapId = immortal ? "map_9" : "map_8";
            var ascensionPosition = immortal ? new Vector2Int(14, 80) : new Vector2Int(502, 400);
            pois.Add(new WorldPoi
            {
                kind = "ascension_gate", townId = immortal ? "tien_gioi_khoi_nguyen" : "man_hoang_thien_dia",
                label = "Cổng Phi Thăng", x = ascensionPosition.x, y = ascensionPosition.y,
                rect = new[] { ascensionPosition.x - 3, ascensionPosition.y - 3, 7, 7 }, regionId = ascensionMapId
            });

            data.regions = regions.ToArray();
            data.towns = towns.ToArray();
            data.pois = pois.ToArray();
            data.zones = zones.ToArray();

            // Province edge walls separate regions by cultivation level. Walkable paths lead to
            // nearby towns, landmarks and the paired free gates, while the new painting stays seamless.
            foreach (var region in data.regions)
            {
                var center = new Vector2Int(region.x + region.w / 2, region.y + region.h / 2);
                foreach (var town in data.towns)
                    if (town.regionId == region.id)
                    {
                        if (town.gate != null && town.gate.Length >= 2) CarvePath(data, center, new Vector2Int(town.gate[0], town.gate[1]));
                        if (town.spawn != null && town.spawn.Length >= 2) CarvePath(data, center, new Vector2Int(town.spawn[0], town.spawn[1]));
                    }
                foreach (var poi in data.pois)
                    if (poi.regionId == region.id) CarvePath(data, center, new Vector2Int(poi.x, poi.y));
                foreach (var zone in data.zones)
                    if (zone.regionId == region.id)
                        CarvePath(data, center, new Vector2Int(zone.x + zone.w / 2, zone.y + zone.h / 2));
            }

            Cache[worldId] = data;
            return data;
        }

        private static int[] Offset(int[] source, int x, int y)
        {
            if (source == null || source.Length < 2) return source;
            var result = (int[])source.Clone();
            result[0] += x; result[1] += y;
            return result;
        }

        private static void AddProvinceGate(List<WorldPoi> pois, WorldRegionMeta from, WorldRegionMeta to, bool horizontal)
        {
            var x = horizontal ? from.x + from.w - 5 : from.x + from.w / 2;
            var y = horizontal ? from.y + from.h / 2 : from.y + from.h - 5;
            AddEndpoint(from, to, x, y);
            x = horizontal ? to.x + 4 : to.x + to.w / 2;
            y = horizontal ? to.y + to.h / 2 : to.y + 4;
            AddEndpoint(to, from, x, y);

            void AddEndpoint(WorldRegionMeta source, WorldRegionMeta target, int gateX, int gateY)
            {
                pois.Add(new WorldPoi
                {
                    kind = "province_gate", label = "Cổng sang " + target.name,
                    x = gateX, y = gateY, rect = new[] { gateX - 3, gateY - 3, 7, 7 },
                    regionId = source.id, gateId = "gate_" + from.id + "_" + to.id, targetMapId = target.id
                });
            }
        }

        private static void CarvePath(WorldMapData data, Vector2Int from, Vector2Int to)
        {
            var x = from.x;
            var y = from.y;
            var stepX = to.x >= x ? 1 : -1;
            while (x != to.x) { CarveBrush(data, x, y); x += stepX; }
            var stepY = to.y >= y ? 1 : -1;
            while (y != to.y) { CarveBrush(data, x, y); y += stepY; }
            CarveBrush(data, x, y);
        }

        private static void CarveBrush(WorldMapData data, int x, int y)
        {
            for (var oy = -2; oy <= 2; oy++)
                for (var ox = -2; ox <= 2; ox++) Open(data, x + ox, y + oy);
        }

        private static void Open(WorldMapData data, int x, int y)
        {
            if (!data.InBounds(x, y)) return;
            var index = y * data.w + x;
            data.Blocked[index] = false;
            data.Border[index] = false;
            data.Water[index] = false;
            data.Road[index] = true;
        }

        /// <summary>Loads detailed and realm-scale painted maps from PNG bytes in Resources.</summary>
        public static Texture2D LoadPainting(string mapId)
        {
            var bytes = Resources.Load<TextAsset>("World/" + mapId + "_map");
            if (bytes == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "Painting_" + mapId };
            if (!texture.LoadImage(bytes.bytes, true)) { UnityEngine.Object.Destroy(texture); return null; }
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Resources.UnloadAsset(bytes);
            return texture;
        }

        private static bool[] EdgeRing(int w, int h, int width)
        {
            var result = new bool[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    result[y * w + x] = x < width || y < width || x >= w - width || y >= h - width;
            return result;
        }

        private static bool[] Bits(string base64, int count)
        {
            var result = new bool[count];
            if (string.IsNullOrEmpty(base64)) return result;
            var bytes = Convert.FromBase64String(base64);
            for (var i = 0; i < count; i++)
            {
                var b = i >> 3;
                if (b < bytes.Length) result[i] = (bytes[b] & (1 << (i & 7))) != 0;
            }
            return result;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;
        public bool IsBlocked(int x, int y) => !InBounds(x, y) || Blocked[y * w + x];
        public bool IsWater(int x, int y) => InBounds(x, y) && Water[y * w + x];
        /// <summary>On a flying sword or a mount, mountains, forest and water pass underneath; only the border wall stops the traveller.</summary>
        public bool IsBlocked(int x, int y, bool flying) => flying ? !InBounds(x, y) || Border[y * w + x] : IsBlocked(x, y);

        public WorldTownMeta Town(string townId)
        {
            foreach (var town in towns) if (town != null && town.id == townId) return town;
            return null;
        }

        public WorldZone ZoneFor(string townId)
        {
            foreach (var zone in zones) if (zone != null && zone.townId == townId) return zone;
            return zones.Length > 0 ? zones[0] : null;
        }

        public WorldRegionMeta RegionAt(int x, int y)
        {
            foreach (var region in regions)
                if (region != null && x >= region.x && x < region.x + region.w && y >= region.y && y < region.y + region.h) return region;
            return null;
        }

        /// <summary>Nearest tile open to the traveller within a radius (spiral search).</summary>
        public Vector2Int NearestOpen(Vector2Int from, int radius = 16, bool flying = false)
        {
            if (!IsBlocked(from.x, from.y, flying)) return from;
            for (var r = 1; r <= radius; r++)
                for (var dy = -r; dy <= r; dy++)
                    for (var dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var x = from.x + dx;
                        var y = from.y + dy;
                        if (!IsBlocked(x, y, flying)) return new Vector2Int(x, y);
                    }
            return from;
        }

        /// <summary>
        /// 8-way A* (no corner cutting). Returns the tiles after the start; when the goal cannot be reached
        /// within maxNodes the path leads to the reachable tile closest to it. The search buffers are reused,
        /// so roaming monsters do not allocate a map-sized array on every step.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int maxNodes = 60000, bool flying = false)
        {
            var result = new List<Vector2Int>();
            if (!InBounds(start.x, start.y) || !InBounds(goal.x, goal.y)) return result;
            goal = NearestOpen(goal, 16, flying);
            if (start == goal) return result;
            var total = w * h;
            if (pathCost == null || pathCost.Length != total)
            {
                pathCost = new float[total];
                pathCame = new int[total];
                pathSeen = new int[total];
                pathClosed = new int[total];
                pathStamp = 0;
            }
            var stamp = ++pathStamp;
            var open = new MinHeap(256);
            var s = start.y * w + start.x;
            var t = goal.y * w + goal.x;
            pathCost[s] = 0;
            pathCame[s] = -1;
            pathSeen[s] = stamp;
            open.Push(s, Heuristic(start, goal));
            var expanded = 0;
            var bestNode = s;
            var bestDist = Heuristic(start, goal);
            var reached = false;
            while (open.Count > 0 && expanded < maxNodes)
            {
                var current = open.Pop();
                if (current == t) { reached = true; break; }
                if (pathClosed[current] == stamp) continue;
                pathClosed[current] = stamp;
                expanded++;
                var cx = current % w;
                var cy = current / w;
                var toGoal = Heuristic(new Vector2Int(cx, cy), goal);
                if (toGoal < bestDist) { bestDist = toGoal; bestNode = current; }
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var nx = cx + dx;
                        var ny = cy + dy;
                        if (IsBlocked(nx, ny, flying)) continue;
                        if (dx != 0 && dy != 0 && (IsBlocked(cx + dx, cy, flying) || IsBlocked(cx, cy + dy, flying))) continue;
                        var n = ny * w + nx;
                        if (pathClosed[n] == stamp) continue;
                        var cost = pathCost[current] + (dx != 0 && dy != 0 ? 1.4142f : 1f) * (!flying && Road[n] ? .8f : 1f);
                        if (pathSeen[n] == stamp && cost >= pathCost[n]) continue;
                        pathCost[n] = cost;
                        pathCame[n] = current;
                        pathSeen[n] = stamp;
                        open.Push(n, cost + Heuristic(new Vector2Int(nx, ny), goal));
                    }
            }
            var target = reached ? t : (bestNode != s ? bestNode : -1);
            if (target < 0) return result;
            for (var node = target; node != s && node >= 0; node = pathCame[node]) result.Add(new Vector2Int(node % w, node / w));
            result.Reverse();
            return result;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }

        private sealed class MinHeap
        {
            private int[] items;
            private float[] keys;
            public int Count { get; private set; }
            public MinHeap(int capacity) { items = new int[capacity]; keys = new float[capacity]; }

            public void Push(int item, float key)
            {
                if (Count == items.Length) { Array.Resize(ref items, Count * 2); Array.Resize(ref keys, Count * 2); }
                var i = Count++;
                items[i] = item; keys[i] = key;
                while (i > 0)
                {
                    var p = (i - 1) / 2;
                    if (keys[p] <= keys[i]) break;
                    Swap(i, p);
                    i = p;
                }
            }

            public int Pop()
            {
                var top = items[0];
                Count--;
                items[0] = items[Count]; keys[0] = keys[Count];
                var i = 0;
                while (true)
                {
                    var l = i * 2 + 1;
                    var r = l + 1;
                    var m = i;
                    if (l < Count && keys[l] < keys[m]) m = l;
                    if (r < Count && keys[r] < keys[m]) m = r;
                    if (m == i) break;
                    Swap(i, m);
                    i = m;
                }
                return top;
            }

            private void Swap(int a, int b)
            {
                var ti = items[a]; items[a] = items[b]; items[b] = ti;
                var tk = keys[a]; keys[a] = keys[b]; keys[b] = tk;
            }
        }
    }
}
