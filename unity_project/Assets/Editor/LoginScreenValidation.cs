#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using IOSVN.TuTien.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Editor
{
    public static class LoginScreenValidation
    {
        [MenuItem("iOSVN/Login/Render auth previews")]
        public static void RenderAuthScreens()
        {
            // 1280x590 matches modern iPhone landscape; 1024x768 checks the iPad fit.
            RenderForm("ShowAccountForm", new object[] { false }, "login.png", 1280, 590);
            RenderForm("ShowAccountForm", new object[] { true }, "register.png", 1280, 590);
            RenderForm("ShowEmailVerification", new object[] { "daohuu@iosvn.com.vn", null }, "verify.png", 1280, 590);
            RenderForm("ShowAccountForm", new object[] { false }, "login-ipad.png", 1024, 768);
            RenderForm("ShowAccountForm", new object[] { true }, "register-ipad.png", 1024, 768);
            const BindingFlags methodFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            RenderScreen(controller => typeof(PrototypeBootstrap).GetMethod("ShowAccountForm", methodFlags).Invoke(controller, new object[] { false }), "login-keyboard.png", 1280, 590, true);
        }

        [MenuItem("iOSVN/Login/Render HUD previews")]
        public static void RenderHudScreens()
        {
            var catalog = JsonUtility.FromJson<MapCatalog>(Resources.Load<TextAsset>("MapCatalog").text);
            var town = catalog.towns[0];
            var state = new GameState
            {
                registered = true,
                realm = new RealmInfo { index = 3, name = "Trúc Cơ Sơ Kỳ" },
                town = town,
                allTowns = catalog.towns,
                allMaps = catalog.maps,
                player = new PlayerInfo { userId = "preview", name = "Lăng Vân", fullName = "Lăng Vân", monName = "Kiếm Tông", heName = "Hỏa linh căn", stones = 128500, hp = 1840, maxHp = 2400 },
                worldMonsters = new[]
                {
                    new WorldMonster { uid = "m1", monsterId = catalog.monsters[0].id, name = catalog.monsters[0].name, townId = town.id, hp = 300, maxHp = 300 },
                    new WorldMonster { uid = "m2", monsterId = catalog.monsters[1].id, name = catalog.monsters[1].name, townId = town.id, hp = 420, maxHp = 600 },
                },
            };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            RenderScreen(controller => typeof(PrototypeBootstrap).GetMethod("ShowHome", flags).Invoke(controller, new object[] { state }), "hud-home.png", 1280, 590);
            RenderScreen(controller =>
            {
                typeof(PrototypeBootstrap).GetField("mapCatalog", flags).SetValue(controller, catalog);
                typeof(PrototypeBootstrap).GetMethod("RenderPveTown", flags).Invoke(controller, new object[] { state, town });
            }, "hud-pve-town.png", 1280, 590);
        }

        [MenuItem("iOSVN/Login/Replay new account flow")]
        public static void ReplayNewAccountFlow()
        {
            // Replays the exact JSON the live IPA server returns for a brand-new account
            // (build/samples/flow_*.json) through the same parse + screen code the app uses.
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples"));
            var log = new System.Text.StringBuilder();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            GameState Load(string name)
            {
                try
                {
                    var parsed = JsonUtility.FromJson<GameState>(File.ReadAllText(Path.Combine(directory, name)));
                    log.AppendLine(name + ": registered=" + parsed?.registered + " realm=" + parsed?.realm?.name + " town=" + parsed?.town?.name + " player=" + parsed?.player?.name + " monsters=" + (parsed?.worldMonsters?.Length ?? -1));
                    return parsed;
                }
                catch (System.Exception ex) { log.AppendLine(name + ": PARSE FAIL " + ex); return null; }
            }
            void Call(PrototypeBootstrap controller, string method, params object[] arguments)
            {
                try { typeof(PrototypeBootstrap).GetMethod(method, flags).Invoke(controller, arguments); log.AppendLine(method + ": ok"); }
                catch (System.Exception ex) { log.AppendLine(method + ": FAIL " + (ex.InnerException ?? ex)); }
            }
            var fresh = Load("flow_state_new.json");
            var registered = Load("flow_register.json");
            var relogin = Load("flow_state_registered.json");
            RenderScreen(controller =>
            {
                Call(controller, "ShowAccountForm", true);
                typeof(PrototypeBootstrap).GetField("currentCatalog", flags).SetValue(controller, fresh?.catalog);
                typeof(PrototypeBootstrap).GetField("latestState", flags).SetValue(controller, fresh);
                Call(controller, "ShowCharacterCreation");
            }, "flow-1-creation.png", 1280, 590);
            RenderScreen(controller =>
            {
                Call(controller, "ShowAccountForm", true);
                typeof(PrototypeBootstrap).GetField("currentCatalog", flags).SetValue(controller, fresh?.catalog);
                Call(controller, "ShowCharacterCreation");
                Call(controller, "SetAtlasOrientation", false);
                Call(controller, "ShowHome", registered);
            }, "flow-2-home.png", 1280, 590);
            RenderScreen(controller =>
            {
                Call(controller, "ShowAccountForm", false);
                Call(controller, "ShowHome", relogin);
            }, "flow-3-relogin-home.png", 1280, 590);
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/login-layout-captures/flow-log.txt"));
            File.WriteAllText(output, log.ToString());
            Debug.Log("FLOW_REPLAY_DONE\n" + log);
        }

        private static void RenderForm(string method, object[] arguments, string filename, int width, int height)
        {
            const BindingFlags methodFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            RenderScreen(controller => typeof(PrototypeBootstrap).GetMethod(method, methodFlags).Invoke(controller, arguments), filename, width, height);
        }

        private static void RenderScreen(System.Action<PrototypeBootstrap> show, string filename, int width, int height, bool keyboardPreview = false)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controller = new GameObject("LoginLayoutPreview").AddComponent<PrototypeBootstrap>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PrototypeBootstrap).GetMethod("BuildCanvas", flags).Invoke(controller, null);
            var canvas = GameObject.Find("GameCanvas").GetComponent<Canvas>();
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;
            canvas.GetComponent<CanvasScaler>().SendMessage("Handle", SendMessageOptions.DontRequireReceiver);
            show(controller);
            if (keyboardPreview)
            {
                var authRoot = GameObject.Find("AuthRoot")?.GetComponent<RectTransform>();
                if (authRoot != null) authRoot.anchoredPosition += new Vector2(0f, 210f);
                AddKeyboardPreview(canvas.transform as RectTransform);
            }
            // Edit-mode captures do not tick the layout loop; settle nested layout groups explicitly.
            for (var pass = 0; pass < 3; pass++)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
                foreach (var group in canvas.GetComponentsInChildren<LayoutGroup>(true))
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }
            Canvas.ForceUpdateCanvases();
            var backdrop = GameObject.Find("LoginBackdrop");
            if (backdrop != null) backdrop.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/login-layout-captures"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, filename), texture.EncodeToPNG());
            RenderTexture.active = old;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            Debug.Log("LOGIN_LAYOUT_RENDERED: " + filename);
        }

        private static void AddKeyboardPreview(RectTransform canvas)
        {
            if (canvas == null) return;
            var panel = new GameObject("SimulatedIosKeyboard", typeof(RectTransform), typeof(Image));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvas, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(1f, 0.47f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            var background = panel.GetComponent<Image>();
            background.color = new Color32(28, 28, 30, 255);
            background.raycastTarget = false;

            AddKeyboardKey(panelRect, "QWERTYUIOP", 0.018f, 0.964f, 0.79f, 0.19f, new Color32(70, 70, 73, 255));
            AddKeyboardKey(panelRect, "ASDFGHJKL", 0.062f, 0.876f, 0.55f, 0.19f, new Color32(70, 70, 73, 255));
            AddKeyboardKeycap(panelRect, "shift", 0.03f, 0.13f, 0.31f, 0.19f, new Color32(112, 112, 116, 255), 16);
            AddKeyboardKey(panelRect, "ZXCVBNM", 0.175f, 0.65f, 0.31f, 0.19f, new Color32(70, 70, 73, 255));
            AddKeyboardKeycap(panelRect, "delete", 0.84f, 0.13f, 0.31f, 0.19f, new Color32(112, 112, 116, 255), 16);
            AddKeyboardBottomRow(panelRect);
        }

        private static void AddKeyboardKey(RectTransform parent, string labels, float start, float rowWidth, float centerY, float height, Color keyColor)
        {
            var gap = 0.009f;
            var keyWidth = (rowWidth - gap * (labels.Length - 1)) / labels.Length;
            for (var i = 0; i < labels.Length; i++)
            {
                AddKeyboardKeycap(parent, labels[i].ToString(), start + i * (keyWidth + gap), keyWidth, centerY, height, keyColor, 32);
            }
        }

        private static void AddKeyboardBottomRow(RectTransform parent)
        {
            const float gap = 0.012f;
            var labels = new[] { "123", "EN", "space", ".", "return" };
            var widths = new[] { 0.13f, 0.09f, 0.39f, 0.08f, 0.16f };
            var colors = new[]
            {
                new Color32(57, 57, 60, 255), new Color32(57, 57, 60, 255),
                new Color32(70, 70, 73, 255), new Color32(57, 57, 60, 255),
                new Color32(32, 113, 184, 255)
            };
            var total = gap * (labels.Length - 1);
            foreach (var width in widths) total += width;
            var x = (1f - total) * 0.5f;
            for (var i = 0; i < labels.Length; i++)
            {
                AddKeyboardKeycap(parent, labels[i], x, widths[i], 0.12f, 0.17f, colors[i], labels[i] == "space" ? 22 : 25);
                x += widths[i] + gap;
            }
        }

        private static void AddKeyboardKeycap(RectTransform parent, string label, float x, float width, float centerY, float height, Color color, int fontSize)
        {
            var key = new GameObject("Key_" + label, typeof(RectTransform), typeof(Image));
            var rect = key.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x, centerY - height * 0.5f);
            rect.anchorMax = new Vector2(x + width, centerY + height * 0.5f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
            var image = key.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = label;
        }

    }
}
#endif
