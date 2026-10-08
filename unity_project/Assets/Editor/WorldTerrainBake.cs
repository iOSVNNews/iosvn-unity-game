using System;
using System.Collections.Generic;
using System.IO;
using IOSVN.TuTien.Core;
using UnityEditor;
using UnityEngine;

namespace IOSVN.TuTien.Editor
{
    // Bake from the SAME masks used by pathfinding. The original overview painting is
    // retained as a fallback; it is not stretched into a substitute collision map.
    public static class WorldTerrainBake
    {
        public const int PixelsPerTile = 6;
        private sealed class Atlas
        {
            public Color32[] pixels;
            public int width, height, columns, rows;
        }

        [MenuItem("iOSVN/World/Bake terrain and review maps")]
        public static void BakeAndReview()
        {
            Bake("world_pham");
            Bake("world_tien");
            AssetDatabase.Refresh();
            QcbhPreview.ReviewMaps();
        }

        public static void ReviewCharactersAndMaps()
        {
            QcbhPreview.ReviewMaps();
            QcbhPreview.ValidateCharacterMotion();
            QcbhPreview.ValidateReportedErrors();
        }

        private static Atlas LoadAtlas(string name, int columns, int rows)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources/World/Art/" + name + ".png"))))
                throw new InvalidDataException(name);
            var result = new Atlas { pixels = texture.GetPixels32(), width = texture.width, height = texture.height, columns = columns, rows = rows };
            UnityEngine.Object.DestroyImmediate(texture);
            return result;
        }

        public static void Bake(string worldId)
        {
            var data = WorldMapData.LoadWorld(worldId);
            if (data == null) throw new InvalidDataException(worldId);
            var width = data.w * PixelsPerTile;
            var height = data.h * PixelsPerTile;
            var pixels = new Color32[width * height];
            var ground = LoadAtlas("ink_ground_v1", 1, 1);
            var waterArt = LoadAtlas("ink_water_v1", 1, 1);
            var waterCoverage = new float[data.w * data.h];
            var kernel = new[] { 1f, 4f, 6f, 4f, 1f };
            for (var y = 0; y < data.h; y++)
                for (var x = 0; x < data.w; x++)
                {
                    float sum = 0;
                    for (var dy = -2; dy <= 2; dy++)
                        for (var dx = -2; dx <= 2; dx++)
                            if (data.Water[Mathf.Clamp(y + dy, 0, data.h - 1) * data.w + Mathf.Clamp(x + dx, 0, data.w - 1)])
                                sum += kernel[dx + 2] * kernel[dy + 2];
                    waterCoverage[y * data.w + x] = sum / 256f;
                }
            var reserved = new bool[data.w * data.h];
            foreach (var town in data.towns)
                Reserve(town.x - 10, town.y - 14, town.w + 20, town.h + 24);
            foreach (var poi in data.pois) Reserve(poi.x - 5, poi.y - 5, 11, 11);
            void Reserve(int x, int y, int w, int h)
            {
                for (var ty = Mathf.Max(0, y); ty < Mathf.Min(data.h, y + h); ty++)
                    for (var tx = Mathf.Max(0, x); tx < Mathf.Min(data.w, x + w); tx++) reserved[ty * data.w + tx] = true;
            }
            // Smooth ink wash colour; tile masks still define the shoreline and roads.
            var biome = new Color[data.w * data.h];
            var provinceColumns = data.w / 256;
            var provinceRows = data.h / 160;
            var provinceTints = new Color[provinceColumns * provinceRows];
            for (var i = 0; i < provinceTints.Length; i++) provinceTints[i] = GroundTint("misty");
            foreach (var region in data.regions)
            {
                provinceTints[region.y / 160 * provinceColumns + region.x / 256] = GroundTint(region.biome);
            }
            Color ProvinceTint(int x, int y) => provinceTints[Mathf.Clamp(y, 0, provinceRows - 1) * provinceColumns + Mathf.Clamp(x, 0, provinceColumns - 1)];
            for (var y = 0; y < data.h; y++)
                for (var x = 0; x < data.w; x++)
                {
                    var gx = (x + .5f) / 256f - .5f; var gy = (y + .5f) / 160f - .5f;
                    var bx = Mathf.FloorToInt(gx); var by = Mathf.FloorToInt(gy);
                    var u = Mathf.SmoothStep(0, 1, gx - bx); var v = Mathf.SmoothStep(0, 1, gy - by);
                    biome[y * data.w + x] = Color.Lerp(Color.Lerp(ProvinceTint(bx, by), ProvinceTint(bx + 1, by), u),
                        Color.Lerp(ProvinceTint(bx, by + 1), ProvinceTint(bx + 1, by + 1), u), v);
                }
            for (var py = 0; py < height; py++)
                for (var px = 0; px < width; px++)
                {
                    var x = px / PixelsPerTile;
                    var y = data.h - 1 - py / PixelsPerTile;
                    var i = y * data.w + x;
                    var wash = Mathf.PerlinNoise(px / 115f, py / 115f) * .12f + Mathf.PerlinNoise(px / 17f, py / 17f) * .025f;
                    var regionTint = biome[i].a == 0 ? GroundTint("misty") : biome[i];
                    var tint = (Color)ground.pixels[(py % ground.height) * ground.width + px % ground.width];
                    var gray = tint.grayscale;
                    tint = Color.Lerp(tint, new Color(gray, gray, gray), .12f);
                    tint.r *= regionTint.r / .73f;
                    tint.g *= regionTint.g / .80f;
                    tint.b *= regionTint.b / .63f;
                    var water = SampleMask(waterCoverage, (px + .5f) / PixelsPerTile - .5f, (height - py - .5f) / PixelsPerTile - .5f);
                    if (water > .01f)
                    {
                        var river = (Color)waterArt.pixels[(py % waterArt.height) * waterArt.width + px % waterArt.width];
                        var coverage = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.10f, .80f, water));
                        tint = Color.Lerp(tint, river, coverage);
                        // A narrow, soft brush at the shoreline; no tile-wide blue bands.
                        var bank = Mathf.Pow(1f - Mathf.Abs(water - .40f) / .40f, 4f);
                        if (water < .8f) tint = Color.Lerp(tint, new Color(.32f, .40f, .28f), Mathf.Clamp01(bank) * .18f);
                    }
                    tint.a = 1;
                    pixels[py * width + px] = tint;
                }

            float SampleMask(float[] mask, float x, float y)
            {
                var x0 = Mathf.FloorToInt(x); var y0 = Mathf.FloorToInt(y);
                float At(int tx, int ty) => mask[Mathf.Clamp(ty, 0, data.h - 1) * data.w + Mathf.Clamp(tx, 0, data.w - 1)];
                return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), x - x0),
                    Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), x - x0), y - y0);
            }

            var mountains = LoadAtlas("mountain_ridges", 4, 4);
            var trees = LoadAtlas("ink_trees", 4, 4);
            var occupied = new bool[reserved.Length];
            var decorations = 0;
            // Back to front, deterministic footprints. Never place decoration bases on a
            // pass, road, water, city entrance or an interaction point.
            for (var y = 8; y < data.h - 8; y += 4)
                for (var x = 8; x < data.w - 8; x += 4)
                {
                    var hash = Hash(x, y);
                    var tx = x + (int)(hash % 3);
                    var ty = y + (int)((hash >> 3) % 3);
                    var i = ty * data.w + tx;
                    if (!data.Blocked[i] || data.Water[i] || data.Road[i] || reserved[i] || occupied[i]) continue;
                    var mountain = Mathf.PerlinNoise(tx / 35f, ty / 35f) > .55f;
                    var w = mountain ? 16 : 7;
                    var h = mountain ? 13 : 9;
                    var clear = true;
                    for (var dy = -h + 1; dy <= 2 && clear; dy++)
                        for (var dx = -w / 2; dx <= w / 2; dx++)
                        {
                            var at = (ty + dy) * data.w + tx + dx;
                            if (at < 0 || at >= reserved.Length || reserved[at] || data.Road[at] || data.Water[at]) { clear = false; break; }
                        }
                    if (!clear) continue;
                    Stamp(mountain ? mountains : trees, (int)((hash >> 8) % 16), tx, ty, w, h, -1f, mountain);
                    decorations++;
                    for (var dy = -1; dy <= 2; dy++)
                        for (var dx = -w / 3; dx <= w / 3; dx++) occupied[(ty + dy) * data.w + tx + dx] = true;
                }

            var towns = LoadAtlas("grounded_towns_v1", 4, 4);
            var landCells = new[] { 0, 1, 2, 4, 5, 6, 7, 9, 10, 11, 12, 14, 15 };
            for (var i = 0; i < data.towns.Length; i++)
            {
                var town = data.towns[i];
                if (town?.gate == null || town.gate.Length < 2) continue;
                var size = town.big ? 28 : 24;
                Stamp(towns, landCells[i % landCells.Length], town.gate[0], town.gate[1], size, size, .14f, false);
            }

            void Stamp(Atlas atlas, int cell, int tx, int ty, int tileWidth, int tileHeight, float baseFraction = -1f, bool fog = false)
            {
                var sw = atlas.width / atlas.columns;
                var sh = atlas.height / atlas.rows;
                var sx = cell % atlas.columns * sw;
                var sy = (atlas.rows - 1 - cell / atlas.columns) * sh;
                var dw = tileWidth * PixelsPerTile;
                var dh = tileHeight * PixelsPerTile;
                var left = tx * PixelsPerTile - dw / 2;
                var bottom = baseFraction < 0 ? (data.h - ty - 2) * PixelsPerTile
                    : Mathf.RoundToInt((data.h - ty - .5f) * PixelsPerTile - dh * baseFraction);
                for (var dy = 0; dy < dh; dy++)
                    for (var dx = 0; dx < dw; dx++)
                    {
                        var px = left + dx; var py = bottom + dy;
                        if (px < 0 || py < 0 || px >= width || py >= height) continue;
                        var source = atlas.pixels[(sy + dy * sh / dh) * atlas.width + sx + dx * sw / dw];
                        // Softly feather cell borders to avoid chopped atlas neighbours.
                        var edge = Mathf.Min(Mathf.Min(dx, dw - 1 - dx), Mathf.Min(dy, dh - 1 - dy));
                        var alpha = source.a / 255f * Mathf.Clamp01(edge / 3f);
                        // Ink mountain atlases contain pale fog at their feet. Let the
                        // paper/grass show through instead of creating white cutout bases.
                        var pale = Mathf.Min(source.r, Mathf.Min(source.g, source.b)) / 255f;
                        if (fog) alpha *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.70f, .98f, pale)) * .75f;
                        if (alpha < .01f) continue;
                        var at = py * width + px;
                        pixels[at] = Color.Lerp(pixels[at], source, alpha);
                    }
            }
            var output = new Texture2D(width, height, TextureFormat.RGB24, false);
            output.SetPixels32(pixels);
            output.Apply(false);
            File.WriteAllBytes(Path.Combine(Application.dataPath, "Resources/World/" + worldId + "_terrain.bytes"), output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            Debug.Log($"Terrain bake {worldId}: {width}x{height}, {decorations} decorations; collision masks unchanged.");
        }

        private static uint Hash(int x, int y)
        {
            unchecked { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177; return h ^ (h >> 16); }
        }
        private static Color GroundTint(string biome)
        {
            switch (biome)
            {
                case "snow": case "frost": return new Color(.84f, .89f, .86f);
                case "desert": case "wasteland": return new Color(.80f, .73f, .58f);
                case "wetland": return new Color(.65f, .77f, .67f);
                case "misty": return new Color(.75f, .82f, .75f);
                case "highland": return new Color(.79f, .79f, .67f);
                case "coast": return new Color(.78f, .83f, .70f);
                case "storm": return new Color(.68f, .75f, .75f);
                case "volcanic": return new Color(.81f, .70f, .57f);
                case "canyon": return new Color(.83f, .76f, .62f);
                case "jungle": return new Color(.64f, .77f, .61f);
                case "celestial": return new Color(.77f, .84f, .78f);
                default: return new Color(.73f, .80f, .63f);
            }
        }
    }
}
