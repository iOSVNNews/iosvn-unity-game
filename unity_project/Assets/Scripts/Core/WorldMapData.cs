using System;
using System.Collections.Generic;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    [Serializable] public sealed class WorldTownMeta { public string id; public string name; public int x; public int y; public int w; public int h; public bool big; public int[] gate; public int[] spawn; }
    [Serializable] public sealed class WorldPoi { public string kind; public string townId; public string dungeonId; public string label; public int x; public int y; public int[] rect; public bool big; }
    [Serializable] public sealed class WorldZone { public string townId; public string label; public int x; public int y; public int w; public int h; }

    /// <summary>
    /// A painted province: logical grid (collision, water, roads), cities, points of interest and
    /// monster grounds, generated offline by tools/world_gen and shipped in Resources/World.
    /// Tile coordinates have (0,0) at the top-left of the painting.
    /// </summary>
    [Serializable]
    public sealed class WorldMapData
    {
        public string id;
        public string name;
        public string biome;
        public int w;
        public int h;
        public int tile = 16;
        public string block;
        public string water;
        public string road;
        public WorldTownMeta[] towns;
        public WorldPoi[] pois;
        public WorldZone[] zones;

        [NonSerialized] public bool[] Blocked;
        [NonSerialized] public bool[] Water;
        [NonSerialized] public bool[] Road;

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
            data.towns = data.towns ?? Array.Empty<WorldTownMeta>();
            data.pois = data.pois ?? Array.Empty<WorldPoi>();
            data.zones = data.zones ?? Array.Empty<WorldZone>();
            Cache[mapId] = data;
            return data;
        }

        /// <summary>Loads the painting (PNG bytes shipped as a TextAsset) as a point-filtered texture.</summary>
        public static Texture2D LoadPainting(string mapId)
        {
            var bytes = Resources.Load<TextAsset>("World/" + mapId + "_map");
            if (bytes == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "Painting_" + mapId };
            if (!texture.LoadImage(bytes.bytes, true)) { UnityEngine.Object.Destroy(texture); return null; }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Resources.UnloadAsset(bytes);
            return texture;
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

        /// <summary>Nearest walkable tile to (x, y) within a radius (spiral search).</summary>
        public Vector2Int NearestOpen(Vector2Int from, int radius = 8)
        {
            if (!IsBlocked(from.x, from.y)) return from;
            for (var r = 1; r <= radius; r++)
                for (var dy = -r; dy <= r; dy++)
                    for (var dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var x = from.x + dx;
                        var y = from.y + dy;
                        if (!IsBlocked(x, y)) return new Vector2Int(x, y);
                    }
            return from;
        }

        /// <summary>8-way A* over the walkable grid (no corner cutting). Returns tiles after the start.</summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int maxNodes = 20000)
        {
            var result = new List<Vector2Int>();
            if (!InBounds(start.x, start.y) || !InBounds(goal.x, goal.y)) return result;
            goal = NearestOpen(goal);
            if (start == goal) return result;
            var total = w * h;
            var g = new float[total];
            var came = new int[total];
            var closed = new bool[total];
            for (var i = 0; i < total; i++) { g[i] = float.MaxValue; came[i] = -1; }
            var open = new MinHeap(256);
            var s = start.y * w + start.x;
            var t = goal.y * w + goal.x;
            g[s] = 0;
            open.Push(s, Heuristic(start, goal));
            var expanded = 0;
            while (open.Count > 0 && expanded < maxNodes)
            {
                var current = open.Pop();
                if (current == t) break;
                if (closed[current]) continue;
                closed[current] = true;
                expanded++;
                var cx = current % w;
                var cy = current / w;
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var nx = cx + dx;
                        var ny = cy + dy;
                        if (IsBlocked(nx, ny)) continue;
                        if (dx != 0 && dy != 0 && (IsBlocked(cx + dx, cy) || IsBlocked(cx, cy + dy))) continue;
                        var n = ny * w + nx;
                        if (closed[n]) continue;
                        var cost = g[current] + (dx != 0 && dy != 0 ? 1.4142f : 1f) * (Road[n] ? .8f : 1f);
                        if (cost >= g[n]) continue;
                        g[n] = cost;
                        came[n] = current;
                        open.Push(n, cost + Heuristic(new Vector2Int(nx, ny), goal));
                    }
            }
            if (came[t] < 0) return result;
            for (var node = t; node != s && node >= 0; node = came[node]) result.Add(new Vector2Int(node % w, node / w));
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
