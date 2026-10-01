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

        private static void RenderForm(string method, object[] arguments, string filename, int width, int height)
        {
            const BindingFlags methodFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            RenderScreen(controller => typeof(PrototypeBootstrap).GetMethod(method, methodFlags).Invoke(controller, arguments), filename, width, height);
        }

        private static void RenderScreen(System.Action<PrototypeBootstrap> show, string filename, int width, int height)
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

    }
}
#endif
