#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using IOSVN.TuTien.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Editor
{
    public static class PaintedArtAudit
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("iOSVN/Art/Validate painted catalog and actions")]
        public static void Run()
        {
            var manifest = J.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "../tools/repaint/manifest.json")));
            var skills = 0; var weapons = 0; var monsters = 0;
            foreach (var sheet in manifest.Items)
                foreach (var item in sheet["items"].Items)
                {
                    var id = item["id"].Str();
                    switch (sheet["kind"].Str())
                    {
                        case "skill":
                            var skill = PaintedArt.Skill(id);
                            Require(skill != null && skill.texture.width == 256 && skill.texture.height == 256, "skill " + id);
                            skills++;
                            Release(skill);
                            break;
                        case "weapon":
                            var weapon = ArtSprites.ForLegacyPath("PixelArt/Items/" + id);
                            Require(weapon != null && weapon.texture.width == 256 && weapon.texture.height == 256, "weapon " + id);
                            weapons++;
                            Release(weapon);
                            break;
                        case "monster":
                            var poses = PaintedArt.MonsterPoses(id);
                            Require(poses != null && poses.Length == 4, "poses " + id);
                            for (var p = 0; p < 4; p++) Require(poses[p].rect.width == 256 && poses[p].rect.height == 256, "pose cell " + id);
                            var body = ArtSprites.MonsterFrames(id);
                            Require(body != null && body.Body.Length == 4, "idle body " + id);
                            monsters++;
                            Release(poses[0]);
                            Release(body.Body[0]);
                            break;
                    }
                }
            Require(skills == 245 && weapons == 547 && monsters == 279, "Incomplete catalog");
            Debug.Log($"PAINTED_CATALOG_OK skills={skills} equipment={weapons} monsters={monsters}");
            RenderActions();
            FigurePreview.RenderInterface();
            Debug.Log("PAINTED_ART_AUDIT_DONE");
        }

        private static void Release(Sprite sprite)
        {
            UnityEngine.Object.DestroyImmediate(sprite.texture);
            UnityEngine.Object.DestroyImmediate(sprite);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Painted art validation failed: " + message);
        }

        private static void RenderActions()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("PaintedActions", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 470;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(28, 35, 39, 255);
            var target = new RenderTexture(1280, 940, 24); camera.targetTexture = target;
            root.renderMode = RenderMode.ScreenSpaceCamera; root.worldCamera = camera; root.planeDistance = 100;
            Canvas.ForceUpdateCanvases();
            var type = typeof(PaintedArt).Assembly.GetType("IOSVN.TuTien.Core.MonsterView");
            var create = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            var show = type.GetMethod("Show", Flags);
            var ids = new[] { "da_lang", "thien_ma", "thai_at_kim_long", "tiengioi_boss_65" };
            var labels = new[] { "Đứng", "Chạy", "Cắn / vồ", "Nhảy chồm" };
            for (var col = 0; col < 4; col++) Label((RectTransform)root.transform, labels[col], new Vector2((col - 1.5f) * 310, -26), 18);
            for (var row = 0; row < ids.Length; row++)
            {
                var id = ids[row];
                var bodyFrames = ArtSprites.MonsterFrames(id).Body;
                for (var col = 0; col < 4; col++)
                {
                    var view = (MonoBehaviour)create.Invoke(null, new object[] { root.transform, id + col, id, bodyFrames[0], new Vector2(188, 188), null });
                    var rect = (RectTransform)view.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                    rect.anchoredPosition = new Vector2((col - 1.5f) * 310, -245 - row * 215);
                    if (col == 1)
                    {
                        type.GetMethod("Step").Invoke(view, new object[] { Vector2.zero });
                        type.GetMethod("Step").Invoke(view, new object[] { new Vector2(10, 0) });
                    }
                    if (col >= 2) type.GetMethod("Strike").Invoke(view, new object[] { 1f, col == 3, 0f });
                    if (col == 3)
                    {
                        type.GetField("strikeFrom", Flags).SetValue(view, Time.unscaledTime - .2f);
                        type.GetField("strikeUntil", Flags).SetValue(view, Time.unscaledTime + .8f);
                    }
                    show.Invoke(view, null);
                    var image = (Image)type.GetField("body", Flags).GetValue(view);
                    if (col > 0)
                    {
                        Require(image.sprite.name == id + "_pose" + col, "action switch " + id + " " + col);
                        var expectedScale = PaintedArt.PoseCanvasScale(bodyFrames[0], rect.sizeDelta);
                        Require(Mathf.Abs(Mathf.Abs(image.rectTransform.localScale.x) - expectedScale) < .01f, "consistent action size " + id);
                    }
                    if (col == 3)
                    {
                        Require(image.rectTransform.anchoredPosition.y > PaintedArt.PoseFeetOffset(bodyFrames[0], rect.sizeDelta), "pounce lift " + id);
                        type.GetField("strikeFrom", Flags).SetValue(view, Time.unscaledTime - .7f);
                        type.GetField("strikeUntil", Flags).SetValue(view, Time.unscaledTime + .3f);
                        show.Invoke(view, null);
                        Require(image.sprite.name == id + "_pose2", "pounce followed by bite " + id);
                        type.GetField("strikeFrom", Flags).SetValue(view, Time.unscaledTime - .2f);
                        type.GetField("strikeUntil", Flags).SetValue(view, Time.unscaledTime + .8f);
                        show.Invoke(view, null);
                    }
                }
                Label((RectTransform)root.transform, id, new Vector2(0, -257 - row * 215), 18);
            }
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1280, 940, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 940), 0, 0); texture.Apply();
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/figure-captures/monster_actions.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("PAINTED_ACTIONS_OK: run, attack, pounce, bite after pounce and consistent sprite scale");
        }

        private static void Label(RectTransform parent, string value, Vector2 at, int size)
        {
            var text = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, 1);
            text.rectTransform.sizeDelta = new Vector2(900, 36); text.rectTransform.anchoredPosition = at;
            text.font = Resources.Load<Font>("Fonts/OpenSans-Regular"); text.text = value; text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = new Color32(205, 220, 218, 255);
        }
    }
}
#endif
