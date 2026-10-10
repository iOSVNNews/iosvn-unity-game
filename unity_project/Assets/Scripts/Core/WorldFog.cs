using System;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Fog of war over the world painting, as in Quỷ Cốc Bát Hoang: land the player has not yet travelled
    /// near lies under drifting cloud.  Exploration is stored per realm map in PlayerPrefs, one bit per
    /// 4×4-tile cell, and the same texture also covers the minimap.
    /// </summary>
    public sealed class WorldFog : MonoBehaviour
    {
        public const int Cell = 4;                 // tiles per fog cell
        public const float RevealRadius = 15f;     // tiles cleared around the traveller
        private const string KeyPrefix = "tt_fog_v1_";
        public const float TownRevealPad = 6f;     // tiles of cleared land around every known town

        public Texture2D Texture { get; private set; }
        public int CellsW { get; private set; }
        public int CellsH { get; private set; }

        private string key;
        private bool[] explored;
        private float[] noise;
        private Color32[] pixels;
        private bool dirty, unsaved;
        private float nextApply, nextSave;
        private RawImage image;

        private static readonly Color32 CloudColor = new Color32(236, 238, 241, 255);

        /// <summary>Creates the fog for a realm map and lays it over the map rect (above the actors).</summary>
        public static WorldFog Attach(ProvinceWorld world)
        {
            if (world == null || world.Data == null || world.MapRect == null) return null;
            var go = new GameObject("Fog", typeof(RectTransform), typeof(RawImage), typeof(WorldFog));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(world.MapRect, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fog = go.GetComponent<WorldFog>();
            fog.Init(world.Data.id, world.Data.w, world.Data.h);
            fog.image = go.GetComponent<RawImage>();
            fog.image.texture = fog.Texture;
            fog.image.raycastTarget = false;
            // the labels live in their own layer above the map; keep the fog just under them
            if (world.FxLayer != null) rect.SetSiblingIndex(world.FxLayer.GetSiblingIndex());
            return fog;
        }

        private void Init(string worldId, int tilesW, int tilesH)
        {
            key = KeyPrefix + (worldId ?? "world");
            CellsW = Mathf.Max(1, Mathf.CeilToInt(tilesW / (float)Cell));
            CellsH = Mathf.Max(1, Mathf.CeilToInt(tilesH / (float)Cell));
            explored = new bool[CellsW * CellsH];
            noise = new float[CellsW * CellsH];
            var seed = (worldId ?? "w").GetHashCode();
            for (var y = 0; y < CellsH; y++)
                for (var x = 0; x < CellsW; x++)
                {
                    // two octaves of value noise give the cloud bank some body
                    var n = Mathf.PerlinNoise(x * .11f + seed % 97, y * .11f + seed % 89) * .65f
                          + Mathf.PerlinNoise(x * .37f + 13.1f, y * .37f + 7.7f) * .35f;
                    noise[y * CellsW + x] = n;
                }
            Load();
            pixels = new Color32[CellsW * CellsH];
            Texture = new Texture2D(CellsW, CellsH, TextureFormat.RGBA32, false)
            {
                name = "WorldFog", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            };
            for (var i = 0; i < explored.Length; i++) WritePixel(i);
            Texture.SetPixels32(pixels);
            Texture.Apply(false);
        }

        public bool IsExplored(int tileX, int tileY)
        {
            var cx = tileX / Cell;
            var cy = tileY / Cell;
            if (cx < 0 || cy < 0 || cx >= CellsW || cy >= CellsH) return true;
            return explored[cy * CellsW + cx];
        }

        /// <summary>Clears the fog around a tile position (tile coordinates, y down).</summary>
        public void Reveal(Vector2 tile, float radius = RevealRadius)
        {
            var r = radius / Cell;
            var cx = tile.x / Cell;
            var cy = tile.y / Cell;
            var x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r));
            var x1 = Mathf.Min(CellsW - 1, Mathf.CeilToInt(cx + r));
            var y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r));
            var y1 = Mathf.Min(CellsH - 1, Mathf.CeilToInt(cy + r));
            for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                {
                    var dx = x + .5f - cx;
                    var dy = y + .5f - cy;
                    if (dx * dx + dy * dy > r * r) continue;
                    var i = y * CellsW + x;
                    if (explored[i]) continue;
                    explored[i] = true;
                    WritePixel(i);
                    dirty = unsaved = true;
                }
        }

        private void WritePixel(int i)
        {
            var x = i % CellsW;
            var y = i / CellsW;
            // texture rows run bottom-up while tile rows run top-down
            var p = (CellsH - 1 - y) * CellsW + x;
            if (explored[i]) { pixels[p] = new Color32(CloudColor.r, CloudColor.g, CloudColor.b, 0); return; }
            // a light drifting haze: the land underneath stays readable (rivers, mountains, roads), only
            // washed out, so unexplored country looks distant rather than missing
            var a = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * (.30f + .24f * noise[i])), 0, 255);
            var shade = (byte)Mathf.Clamp(CloudColor.r - Mathf.RoundToInt(30f * (1f - noise[i])), 0, 255);
            pixels[p] = new Color32(shade, (byte)Mathf.Min(255, shade + 3), (byte)Mathf.Min(255, shade + 8), a);
        }

        private void Update()
        {
            if (dirty && Time.unscaledTime >= nextApply)
            {
                dirty = false;
                nextApply = Time.unscaledTime + .2f;
                Texture.SetPixels32(pixels);
                Texture.Apply(false);
            }
            if (unsaved && Time.unscaledTime >= nextSave)
            {
                unsaved = false;
                nextSave = Time.unscaledTime + 5f;
                Save();
            }
        }

        private void OnDestroy()
        {
            if (unsaved) Save();
            if (Texture != null) Destroy(Texture);
        }

        private void Load()
        {
            var stored = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(stored)) return;
            try
            {
                var bytes = Convert.FromBase64String(stored);
                for (var i = 0; i < explored.Length && (i >> 3) < bytes.Length; i++)
                    explored[i] = (bytes[i >> 3] & (1 << (i & 7))) != 0;
            }
            catch (FormatException) { }
        }

        private void Save()
        {
            var bytes = new byte[(explored.Length + 7) / 8];
            for (var i = 0; i < explored.Length; i++)
                if (explored[i]) bytes[i >> 3] |= (byte)(1 << (i & 7));
            PlayerPrefs.SetString(key, Convert.ToBase64String(bytes));
            PlayerPrefs.Save();
        }
    }
}
