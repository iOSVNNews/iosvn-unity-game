using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Small hand-built pixel animations keyed by each skill's id, name, element and combat role.</summary>
    public static class PixelSkillArt
    {
        private enum Shape { Sword, Fire, Lightning, Ice, Wind, Water, Earth, Leaf, Shadow, Talisman, Poison, Spirit, Fist, Beast, Wheel }
        private sealed class Palette { public Color32 shade, body, light, core; }
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        public static Sprite[] Frames(string id, string name, string kind, bool immortal)
        {
            var key = (id ?? "") + "|" + (name ?? "") + "|" + (kind ?? "") + "|" + immortal;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var normalized = Normalize((id ?? "") + " " + (name ?? ""));
            var role = Normalize(kind ?? "atk");
            var seed = StableHash(key);
            var shape = ResolveShape(normalized, role);
            var palette = ResolvePalette(shape, immortal, seed);
            const int size = 48;
            var frames = new Sprite[8];
            for (var frame = 0; frame < frames.Length; frame++)
            {
                var pixels = new Color32[size * size];
                DrawShape(pixels, size, frame, shape, palette, seed);
                DrawRoleOverlay(pixels, size, frame, role, palette, seed);
                DrawSignature(pixels, size, frame, palette, seed);
                frames[frame] = MakeSprite(pixels, size, "PixelSkill_" + SafeName(id) + "_" + frame);
            }
            Cache[key] = frames;
            return frames;
        }

        public static void Animate(Image image, string id, string name, string kind, bool immortal, float phase = 0f)
        {
            if (image == null) return;
            var animator = image.GetComponent<PixelSkillSpriteAnimator>();
            if (animator == null) animator = image.gameObject.AddComponent<PixelSkillSpriteAnimator>();
            animator.SetFrames(Frames(id, name, kind, immortal), 13f, phase);
        }

        public static Color Accent(string id, string name, string kind, bool immortal)
        {
            var p = ResolvePalette(ResolveShape(Normalize((id ?? "") + " " + (name ?? "")), Normalize(kind ?? "atk")), immortal, StableHash(id ?? name ?? ""));
            return p.light;
        }

        private static Shape ResolveShape(string text, string kind)
        {
            if (ContainsAny(text, "kiem", "trieu_kiem", "kiem_khi", "kiem_vu", "triet_tien", "van_kiem", "kiem_vuc", "tram", "binh_quy", "bach_binh", "than_binh")) return Shape.Sword;
            if (ContainsAny(text, "loi", "thien_loi", "tich_ta", "sam", "dien", "thunder", "lightning")) return Shape.Lightning;
            if (ContainsAny(text, "bang", "bang_son", "ngu_quy", "lanh", "ice", "tuyet")) return Shape.Ice;
            if (ContainsAny(text, "hoa", "diem", "hoa_cau", "thien_hoa", "xich_diem", "hoa_huyet", "fire", "viem")) return Shape.Fire;
            if (ContainsAny(text, "phong_hanh", "phu_don", "thuan_di", "than_hanh", "gio", "wind", "toan_phong", "co_than_toan_phong")) return Shape.Wind;
            if (text.Contains("phong") && !text.Contains("phong_an")) return Shape.Wind;
            if (ContainsAny(text, "thuy", "hai", "phuc_hai", "son_ha", "wave", "water")) return Shape.Water;
            if (ContainsAny(text, "moc", "thao", "xuan", "linh_cham", "la", "plant", "leaf", "duong_sinh")) return Shape.Leaf;
            if (ContainsAny(text, "trieu_hoan", "ngu_thu", "thu_an", "long_tuong", "linh_thu", "beast", "summon")) return Shape.Beast;
            if (ContainsAny(text, "ngu_hanh", "luan_chuyen", "van_khi_tran", "five_element", "dao_luan")) return Shape.Wheel;
            if (ContainsAny(text, "cuu_u", "u_minh", "phe_hon", "nghich_chi", "thac_thien", "chan_ma", "dark", "soul")) return Shape.Shadow;
            if (ContainsAny(text, "doc", "bach_doc", "huyet", "poison")) return Shape.Poison;
            if (ContainsAny(text, "phu", "an_", "chu", "tran", "phong_an", "dinh_than", "seal", "bat_quai")) return Shape.Talisman;
            if (ContainsAny(text, "son", "nui", "tho", "dia", "thach", "rock", "mountain", "bat_cuc")) return Shape.Earth;
            if (ContainsAny(text, "quyen", "chuong", "cuong", "pha_gioi", "chan_ma", "sat_luc", "punch", "fist")) return Shape.Fist;
            if (kind == "heal") return Shape.Leaf;
            if (kind == "dot") return Shape.Poison;
            if (kind == "stun") return Shape.Talisman;
            if (kind == "escape") return Shape.Wind;
            if (kind == "shield" || kind == "reflect" || kind == "buff" || kind == "mana") return Shape.Spirit;
            return Shape.Spirit;
        }

        private static Palette ResolvePalette(Shape shape, bool immortal, uint seed)
        {
            Color32 shade, body, light, core;
            switch (shape)
            {
                case Shape.Sword: shade = new Color32(34, 77, 98, 255); body = new Color32(55, 179, 207, 255); light = new Color32(135, 238, 245, 255); core = new Color32(245, 255, 241, 255); break;
                case Shape.Fire: shade = new Color32(133, 42, 31, 255); body = new Color32(237, 89, 39, 255); light = new Color32(255, 181, 55, 255); core = new Color32(255, 245, 171, 255); break;
                case Shape.Lightning: shade = new Color32(91, 66, 166, 255); body = new Color32(105, 163, 255, 255); light = new Color32(191, 221, 255, 255); core = new Color32(255, 255, 239, 255); break;
                case Shape.Ice: shade = new Color32(55, 118, 155, 255); body = new Color32(87, 198, 224, 255); light = new Color32(177, 242, 250, 255); core = new Color32(246, 255, 255, 255); break;
                case Shape.Wind: shade = new Color32(55, 121, 111, 255); body = new Color32(79, 202, 164, 255); light = new Color32(167, 246, 196, 255); core = new Color32(237, 255, 227, 255); break;
                case Shape.Water: shade = new Color32(36, 72, 143, 255); body = new Color32(48, 144, 222, 255); light = new Color32(108, 220, 255, 255); core = new Color32(222, 255, 255, 255); break;
                case Shape.Earth: shade = new Color32(101, 62, 45, 255); body = new Color32(174, 115, 64, 255); light = new Color32(238, 190, 108, 255); core = new Color32(255, 239, 180, 255); break;
                case Shape.Leaf: shade = new Color32(47, 105, 61, 255); body = new Color32(85, 184, 85, 255); light = new Color32(177, 239, 112, 255); core = new Color32(243, 255, 196, 255); break;
                case Shape.Shadow: shade = new Color32(53, 38, 78, 255); body = new Color32(121, 73, 156, 255); light = new Color32(202, 92, 137, 255); core = new Color32(255, 174, 169, 255); break;
                case Shape.Talisman: shade = new Color32(122, 66, 35, 255); body = new Color32(216, 156, 73, 255); light = new Color32(255, 218, 111, 255); core = new Color32(255, 250, 208, 255); break;
                case Shape.Poison: shade = new Color32(55, 77, 63, 255); body = new Color32(103, 174, 81, 255); light = new Color32(182, 226, 83, 255); core = new Color32(242, 255, 153, 255); break;
                case Shape.Fist: shade = new Color32(117, 49, 43, 255); body = new Color32(212, 88, 57, 255); light = new Color32(255, 164, 83, 255); core = new Color32(255, 238, 182, 255); break;
                case Shape.Beast: shade = new Color32(81, 61, 48, 255); body = new Color32(174, 124, 68, 255); light = new Color32(241, 191, 88, 255); core = new Color32(255, 241, 183, 255); break;
                case Shape.Wheel: shade = new Color32(62, 73, 122, 255); body = new Color32(117, 146, 220, 255); light = new Color32(220, 193, 112, 255); core = new Color32(255, 248, 211, 255); break;
                default: shade = new Color32(60, 47, 123, 255); body = new Color32(103, 100, 220, 255); light = new Color32(175, 199, 255, 255); core = new Color32(250, 245, 255, 255); break;
            }
            if (immortal)
            {
                body = Color32.Lerp(body, new Color32(137, 156, 255, 255), .15f);
                light = Color32.Lerp(light, new Color32(237, 222, 255, 255), .30f);
            }
            if ((seed & 3u) == 1u) { var swap = shade; shade = body; body = swap; }
            return new Palette { shade = shade, body = body, light = light, core = core };
        }

        private static void DrawShape(Color32[] p, int n, int frame, Shape shape, Palette c, uint seed)
        {
            switch (shape)
            {
                case Shape.Sword: DrawSword(p, n, frame, c); break;
                case Shape.Fire: DrawFire(p, n, frame, c); break;
                case Shape.Lightning: DrawLightning(p, n, frame, c, seed); break;
                case Shape.Ice: DrawIce(p, n, frame, c); break;
                case Shape.Wind: DrawWind(p, n, frame, c); break;
                case Shape.Water: DrawWater(p, n, frame, c); break;
                case Shape.Earth: DrawEarth(p, n, frame, c); break;
                case Shape.Leaf: DrawLeaf(p, n, frame, c); break;
                case Shape.Shadow: DrawShadow(p, n, frame, c); break;
                case Shape.Talisman: DrawTalisman(p, n, frame, c, seed); break;
                case Shape.Poison: DrawPoison(p, n, frame, c); break;
                case Shape.Fist: DrawFist(p, n, frame, c); break;
                case Shape.Beast: DrawBeast(p, n, frame, c); break;
                case Shape.Wheel: DrawWheel(p, n, frame, c); break;
                default: DrawSpirit(p, n, frame, c, seed); break;
            }
            for (var i = 0; i < 8; i++)
            {
                var angle = (i * Mathf.PI / 4f) + frame * .30f + (seed % 23u) * .01f;
                var radius = 15 + (i % 3) * 3;
                var x = 24 + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
                var y = 24 + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
                var color = i % 3 == 0 ? c.core : i % 2 == 0 ? c.light : c.body;
                if ((frame + i) % 2 == 0) Pixel(p, n, x, y, color);
            }
        }

        private static void DrawRoleOverlay(Color32[] p, int n, int frame, string kind, Palette c, uint seed)
        {
            if (kind == "shield" || kind == "reflect" || kind == "buff" || kind == "mana" || kind == "heal")
            {
                var radius = 19 + frame % 3;
                for (var degree = frame * 9; degree < 360 + frame * 9; degree += 5)
                {
                    if (degree % 20 < 7) continue;
                    var angle = degree * Mathf.Deg2Rad;
                    Pixel(p, n, 24 + Mathf.RoundToInt(Mathf.Cos(angle) * radius), 24 + Mathf.RoundToInt(Mathf.Sin(angle) * radius), degree % 3 == 0 ? c.core : c.light);
                }
                for (var ray = 0; ray < 8; ray++)
                {
                    var angle = ray * Mathf.PI / 4f + frame * .05f;
                    var x = 24 + Mathf.RoundToInt(Mathf.Cos(angle) * 21);
                    var y = 24 + Mathf.RoundToInt(Mathf.Sin(angle) * 21);
                    Pixel(p, n, x, y, ray % 2 == 0 ? c.core : c.light);
                }
            }
            if (kind == "stun")
            {
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * Mathf.PI / 3f + frame * .12f;
                    var x = 24 + Mathf.RoundToInt(Mathf.Cos(angle) * 20);
                    var y = 24 + Mathf.RoundToInt(Mathf.Sin(angle) * 20);
                    DrawRect(p, n, x - 1, y - 1, 3, 3, i % 2 == 0 ? c.core : c.light);
                }
            }
            if (kind == "multi" || kind == "dot")
            {
                var random = new System.Random(unchecked((int)(seed + (uint)frame * 977u)));
                var count = kind == "multi" ? 12 : 8;
                for (var i = 0; i < count; i++)
                {
                    var angle = random.NextDouble() * Math.PI * 2;
                    var radius = 10 + random.Next(7, 16);
                    Pixel(p, n, 24 + Mathf.RoundToInt((float)Math.Cos(angle) * radius), 24 + Mathf.RoundToInt((float)Math.Sin(angle) * radius), i % 3 == 0 ? c.core : c.light);
                }
            }
        }

        private static void DrawSword(Color32[] p, int n, int f, Palette c)
        {
            for (var step = 0; step <= 40; step++)
            {
                var t = step / 40f;
                var x = 5 + Mathf.RoundToInt(t * 38);
                var y = 41 - Mathf.RoundToInt(t * 35) + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * (3 + (f % 3)));
                Pixel(p, n, x, y, c.shade); Pixel(p, n, x, y - 1, c.shade); Pixel(p, n, x + 1, y, c.body);
                Pixel(p, n, x + 1, y - 1, c.light); if (step % 3 == 0) Pixel(p, n, x + 2, y - 1, c.core);
            }
            for (var i = 0; i < 5; i++) Pixel(p, n, 7 + i * 2, 36 - i * 2 + f % 2, i % 2 == 0 ? c.core : c.light);
        }

        private static void DrawFire(Color32[] p, int n, int f, Palette c)
        {
            for (var y = 7; y < 44; y++)
            {
                var h = (y - 7) / 37f;
                var width = Mathf.RoundToInt(3 + h * 14 + Mathf.Sin(y * .48f + f * .7f) * (1 + h * 3));
                var center = 24 + Mathf.RoundToInt(Mathf.Sin(y * .23f - f * .52f) * 3);
                for (var x = center - width; x <= center + width; x++)
                {
                    var d = Mathf.Abs(x - center);
                    Pixel(p, n, x, y, d > width - 3 ? c.shade : d > width - 7 ? c.body : d > width - 11 ? c.light : c.core);
                }
            }
            for (var i = 0; i < 5; i++) Pixel(p, n, 14 + i * 5 + f % 3, 6 + (i * 3 + f) % 7, i % 2 == 0 ? c.light : c.core);
        }

        private static void DrawLightning(Color32[] p, int n, int f, Palette c, uint seed)
        {
            var random = new System.Random(unchecked((int)seed)); var previousX = 9 + random.Next(4); var previousY = 2;
            for (var segment = 0; segment < 8; segment++)
            {
                var nextX = Mathf.Clamp(previousX + random.Next(-8, 9) + (f % 3 - 1), 5, 43);
                var nextY = previousY + 6;
                DrawLine(p, n, previousX, previousY, nextX, nextY, c.shade, 5);
                DrawLine(p, n, previousX, previousY, nextX, nextY, c.body, 3);
                DrawLine(p, n, previousX, previousY, nextX, nextY, c.core, 1);
                if (segment == 2 || segment == 5)
                    DrawLine(p, n, nextX, nextY, Mathf.Clamp(nextX + (segment % 2 == 0 ? 9 : -8), 2, 45), nextY + 6, c.light, 2);
                previousX = nextX; previousY = nextY;
            }
        }

        private static void DrawIce(Color32[] p, int n, int f, Palette c)
        {
            var pulse = f % 2;
            for (var y = 7; y <= 41; y++)
            for (var x = 9; x <= 39; x++)
            {
                var dx = Mathf.Abs(x - 24); var dy = Mathf.Abs(y - 24);
                if (dx * 1.05f + dy * .82f > 17 + pulse) continue;
                Pixel(p, n, x, y, dx > 10 ? c.shade : dx > 6 ? c.body : dx > 3 ? c.light : c.core);
            }
            DrawLine(p, n, 24, 7, 24, 41, c.core, 2); DrawLine(p, n, 10, 17, 38, 31, c.light, 1);
            DrawLine(p, n, 12, 32, 35, 15, c.body, 1);
            DrawLine(p, n, 10, 14, 5, 7 + f % 3, c.light, 2); DrawLine(p, n, 37, 34, 43, 41 - f % 3, c.core, 2);
        }

        private static void DrawWind(Color32[] p, int n, int f, Palette c)
        {
            for (var a = 0; a < 310; a += 3)
            {
                var t = a / 310f; var angle = (a + f * 42) * Mathf.Deg2Rad;
                var radius = 4 + t * 17;
                var x = 24 + Mathf.RoundToInt(Mathf.Cos(angle) * radius); var y = 24 + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
                if (a % 9 == 0) Pixel(p, n, x, y, a % 18 == 0 ? c.core : c.light);
            }
            for (var i = 0; i < 4; i++)
            {
                var x = 5 + i * 5 + f % 4; var y = 11 + i * 7;
                DrawLine(p, n, x, y, x + 8, y - 3, c.body, 2); Pixel(p, n, x + 8, y - 3, c.core);
            }
        }

        private static void DrawWater(Color32[] p, int n, int f, Palette c)
        {
            for (var layer = 0; layer < 3; layer++)
            {
                var baseY = 17 + layer * 7;
                for (var x = 3; x < 46; x++)
                {
                    var y = baseY + Mathf.RoundToInt(Mathf.Sin(x * .31f + f * .72f + layer) * (3 + layer));
                    DrawRect(p, n, x, y, 2, 3 + layer, layer == 0 ? c.light : layer == 1 ? c.body : c.shade);
                    if ((x + f + layer) % 9 < 3) Pixel(p, n, x, y - 1, c.core);
                }
            }
            for (var i = 0; i < 6; i++) Pixel(p, n, 8 + i * 6, 37 - ((i + f) % 3) * 2, c.core);
        }

        private static void DrawEarth(Color32[] p, int n, int f, Palette c)
        {
            DrawRect(p, n, 9, 28 + f % 2, 12, 13, c.shade); DrawRect(p, n, 14, 20, 13, 20, c.body);
            DrawRect(p, n, 20, 13 - f % 2, 11, 29, c.shade); DrawRect(p, n, 22, 17, 8, 24, c.light);
            DrawRect(p, n, 31, 24, 8, 17, c.body); DrawRect(p, n, 5, 34, 14, 7, c.shade);
            DrawLine(p, n, 18, 29, 22, 34, c.core, 1); DrawLine(p, n, 29, 23, 26, 30, c.core, 1);
            for (var i = 0; i < 7; i++) Pixel(p, n, 8 + ((i * 11 + f * 3) % 33), 9 + (i * 7) % 12, i % 2 == 0 ? c.light : c.core);
        }

        private static void DrawLeaf(Color32[] p, int n, int f, Palette c)
        {
            var cx = 24; var cy = 24;
            for (var petal = 0; petal < 8; petal++)
            {
                var angle = petal * Mathf.PI / 4f + f * .10f;
                var x = cx + Mathf.RoundToInt(Mathf.Cos(angle) * 9); var y = cy + Mathf.RoundToInt(Mathf.Sin(angle) * 9);
                DrawEllipse(p, n, x, y, 6, 10, petal % 2 == 0 ? c.body : c.light, angle);
                Pixel(p, n, x, y, c.core);
            }
            DrawEllipse(p, n, cx, cy, 6, 6, c.shade, 0); DrawEllipse(p, n, cx, cy, 3, 3, c.core, 0);
            DrawLine(p, n, 24, 28, 24 + f % 3 - 1, 43, c.light, 2);
            DrawLine(p, n, 24, 37, 15, 33 + f % 2, c.body, 2); DrawLine(p, n, 24, 39, 33, 34 - f % 2, c.body, 2);
        }

        private static void DrawShadow(Color32[] p, int n, int f, Palette c)
        {
            for (var y = 7; y < 44; y++)
            {
                var width = Mathf.RoundToInt(5 + Mathf.Sin(y * .42f + f * .6f) * 3 + (y - 7) * .27f);
                var center = 24 + Mathf.RoundToInt(Mathf.Sin(y * .19f + f) * 4);
                for (var x = center - width; x <= center + width; x++)
                    Pixel(p, n, x, y, Mathf.Abs(x - center) > width - 4 ? c.shade : Mathf.Abs(x - center) > width - 8 ? c.body : c.light);
            }
            DrawEllipse(p, n, 20, 23, 3, 5, c.core, 0); DrawEllipse(p, n, 28, 23, 3, 5, c.core, 0);
            for (var i = 0; i < 6; i++) Pixel(p, n, 8 + i * 6, 5 + (i * 5 + f * 2) % 13, i % 2 == 0 ? c.light : c.body);
        }

        private static void DrawTalisman(Color32[] p, int n, int f, Palette c, uint seed)
        {
            DrawRect(p, n, 17, 5, 15, 38, c.shade); DrawRect(p, n, 19, 7, 11, 34, c.body); DrawRect(p, n, 21, 9, 7, 30, c.light);
            DrawLine(p, n, 24, 11, 24 + ((int)(seed % 7u) - 3), 34, c.shade, 2);
            for (var row = 0; row < 4; row++)
            {
                var x = 22 + (row % 2) * 5 + f % 2; var y = 14 + row * 5;
                Pixel(p, n, x, y, c.core); Pixel(p, n, x + 1, y, c.core); Pixel(p, n, x, y + 1, c.light);
                Pixel(p, n, x - 2, y + 2, c.core); Pixel(p, n, x + 2, y + 2, c.light);
            }
            Pixel(p, n, 17, 5, c.core); Pixel(p, n, 30, 5, c.core); Pixel(p, n, 17, 42, c.core); Pixel(p, n, 30, 42, c.core);
        }

        private static void DrawPoison(Color32[] p, int n, int f, Palette c)
        {
            var blobs = new[] { new Vector2(15, 26), new Vector2(22, 20), new Vector2(29, 26), new Vector2(19, 30), new Vector2(28, 32) };
            for (var i = 0; i < blobs.Length; i++)
            {
                var b = blobs[i]; var shift = (f + i) % 3 - 1;
                DrawEllipse(p, n, Mathf.RoundToInt(b.x + shift), Mathf.RoundToInt(b.y - shift), 7 + i % 2, 7 + i % 3, i % 2 == 0 ? c.body : c.shade, 0);
                Pixel(p, n, Mathf.RoundToInt(b.x + shift - 2), Mathf.RoundToInt(b.y + 1), c.light);
                Pixel(p, n, Mathf.RoundToInt(b.x + shift + 1), Mathf.RoundToInt(b.y - 2), c.core);
            }
            for (var i = 0; i < 8; i++) Pixel(p, n, 8 + ((i * 13 + f * 4) % 32), 7 + (i * 9) % 15, i % 2 == 0 ? c.light : c.core);
        }

        private static void DrawFist(Color32[] p, int n, int f, Palette c)
        {
            var swell = f % 3;
            DrawEllipse(p, n, 24 + swell, 25, 15, 12, c.shade, -.2f);
            DrawEllipse(p, n, 24 + swell, 23, 11, 10, c.body, -.2f);
            for (var knuckle = 0; knuckle < 4; knuckle++)
                DrawRect(p, n, 15 + knuckle * 5 + swell, 14, 4, 6, knuckle % 2 == 0 ? c.light : c.body);
            DrawLine(p, n, 11, 19, 5, 15 - swell, c.core, 2); DrawLine(p, n, 12, 29, 5, 34 + swell, c.light, 2);
            for (var i = 0; i < 8; i++) Pixel(p, n, 8 + i * 4, 8 + (i * 7 + f * 3) % 31, i % 2 == 0 ? c.core : c.light);
        }

        private static void DrawBeast(Color32[] p, int n, int f, Palette c)
        {
            var stride = f % 2 == 0 ? -2 : 2;
            DrawEllipse(p, n, 24, 27, 13, 10, c.shade, 0);
            DrawEllipse(p, n, 24, 24, 10, 9, c.body, 0);
            DrawRect(p, n, 17 + stride, 31, 5, 9, c.shade); DrawRect(p, n, 27 - stride, 31, 5, 9, c.shade);
            DrawLine(p, n, 15, 18, 9, 11 + f % 3, c.light, 3); DrawLine(p, n, 33, 18, 39, 11 - f % 3, c.light, 3);
            DrawEllipse(p, n, 20, 23, 2, 3, c.core, 0); DrawEllipse(p, n, 28, 23, 2, 3, c.core, 0);
            DrawLine(p, n, 20, 29, 28, 29, c.light, 2);
            for (var i = 0; i < 5; i++) Pixel(p, n, 12 + i * 6 + stride, 8 + (i % 2) * 4, i % 2 == 0 ? c.core : c.light);
        }

        private static void DrawWheel(Color32[] p, int n, int f, Palette c)
        {
            var elements = new[]
            {
                new Color32(85, 194, 105, 255), new Color32(82, 167, 238, 255), new Color32(244, 91, 58, 255),
                new Color32(224, 195, 112, 255), new Color32(211, 227, 242, 255)
            };
            for (var spoke = 0; spoke < 10; spoke++)
            {
                var angle = spoke * Mathf.PI / 5f + f * .14f;
                var distance = spoke % 2 == 0 ? 20 : 15;
                var x = 24 + Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                var y = 24 + Mathf.RoundToInt(Mathf.Sin(angle) * distance);
                DrawLine(p, n, 24, 24, x, y, elements[spoke % elements.Length], spoke % 2 == 0 ? 2 : 1);
                DrawRect(p, n, x - 2, y - 2, 5, 5, spoke % 2 == 0 ? c.light : c.body);
                Pixel(p, n, x, y, c.core);
            }
            for (var radius = 7; radius <= 14; radius += 7)
                for (var angle = 0; angle < 360; angle += 4)
                {
                    var a = (angle + f * 6) * Mathf.Deg2Rad;
                    Pixel(p, n, 24 + Mathf.RoundToInt(Mathf.Cos(a) * radius), 24 + Mathf.RoundToInt(Mathf.Sin(a) * radius),
                        elements[(angle / 72 + f) % elements.Length]);
                }
            DrawEllipse(p, n, 24, 24, 5, 5, c.shade, 0); DrawEllipse(p, n, 24, 24, 3, 3, c.core, 0);
        }

        private static void DrawSpirit(Color32[] p, int n, int f, Palette c, uint seed)
        {
            for (var radius = 15; radius <= 20; radius += 5)
                for (var deg = 0; deg < 360; deg += radius == 15 ? 9 : 13)
                {
                    var a = (deg + f * (radius == 15 ? 8 : -5) + (int)(seed % 31u)) * Mathf.Deg2Rad;
                    if ((deg / 7 + f) % 4 == 0) continue;
                    Pixel(p, n, 24 + Mathf.RoundToInt(Mathf.Cos(a) * radius), 24 + Mathf.RoundToInt(Mathf.Sin(a) * radius), radius == 15 ? c.light : c.body);
                }
            for (var ray = 0; ray < 12; ray++)
            {
                var a = ray * Mathf.PI / 6f + f * .18f; var len = 7 + (ray % 3) * 2;
                DrawLine(p, n, 24, 24, 24 + Mathf.RoundToInt(Mathf.Cos(a) * len), 24 + Mathf.RoundToInt(Mathf.Sin(a) * len), ray % 2 == 0 ? c.core : c.light, 1);
            }
            DrawEllipse(p, n, 24, 24, 5 + f % 2, 5 + f % 2, c.body, 0); Pixel(p, n, 24, 24, c.core);
        }

        private static void DrawSignature(Color32[] p, int n, int f, Palette c, uint seed)
        {
            for (var i = 0; i < 6; i++)
            {
                var x = 4 + (int)((seed >> (i * 3)) % 40u);
                var y = 4 + (int)((seed >> (i * 4 + 1)) % 40u);
                if ((x + y + f) % 3 == 0) Pixel(p, n, x, y, c.core);
                else if ((x + f) % 2 == 0) Pixel(p, n, x, y, c.light);
            }
            if (f % 2 == 0)
            {
                Pixel(p, n, 24, 3, c.core); Pixel(p, n, 44, 24, c.light);
                Pixel(p, n, 24, 44, c.core); Pixel(p, n, 3, 24, c.light);
            }
        }

        private static void DrawLine(Color32[] p, int n, int x0, int y0, int x1, int y1, Color32 color, int width)
        {
            var dx = Math.Abs(x1 - x0); var sx = x0 < x1 ? 1 : -1; var dy = -Math.Abs(y1 - y0); var sy = y0 < y1 ? 1 : -1; var err = dx + dy;
            while (true)
            {
                DrawRect(p, n, x0 - width / 2, y0 - width / 2, width, width, color);
                if (x0 == x1 && y0 == y1) break;
                var e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static void DrawEllipse(Color32[] p, int n, int cx, int cy, int rx, int ry, Color32 color, float angle)
        {
            var cos = Mathf.Cos(angle); var sin = Mathf.Sin(angle);
            for (var y = cy - ry - rx; y <= cy + ry + rx; y++)
            for (var x = cx - rx - ry; x <= cx + rx + ry; x++)
            {
                var dx = x - cx; var dy = y - cy;
                var u = dx * cos + dy * sin; var v = -dx * sin + dy * cos;
                if (u * u / (rx * rx) + v * v / (ry * ry) <= 1f) Pixel(p, n, x, y, color);
            }
        }

        private static void DrawRect(Color32[] p, int n, int x, int y, int width, int height, Color32 color)
        {
            for (var py = y; py < y + height; py++) for (var px = x; px < x + width; px++) Pixel(p, n, px, py, color);
        }
        private static void Pixel(Color32[] p, int n, int x, int y, Color32 color) { if (x >= 0 && x < n && y >= 0 && y < n) p[y * n + x] = color; }

        private static Sprite MakeSprite(Color32[] pixels, int size, string name)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        private static string Normalize(string value)
        {
            value = (value ?? "").ToLowerInvariant().Replace('đ', 'd');
            var decomposed = value.Normalize(NormalizationForm.FormD); var result = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed) if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) result.Append(ch);
            return result.ToString().Normalize(NormalizationForm.FormC);
        }
        private static bool ContainsAny(string value, params string[] words) { foreach (var word in words) if (value.Contains(word)) return true; return false; }
        private static uint StableHash(string value) { unchecked { uint hash = 2166136261u; foreach (var ch in value) { hash ^= ch; hash *= 16777619u; } return hash; } }
        private static string SafeName(string value) { if (string.IsNullOrEmpty(value)) return "generic"; var b = new StringBuilder(); foreach (var c in value) if (char.IsLetterOrDigit(c) || c == '_') b.Append(c); return b.Length > 32 ? b.ToString(0, 32) : b.ToString(); }
    }
}
