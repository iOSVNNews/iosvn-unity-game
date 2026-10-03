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
    /// <summary>
    /// Renders the QCBH-style screens (creator, province map, character sheet, bag, PvE and PvP battles) from the
    /// real server JSON in build/samples/qcbh_*.json into build/qcbh-captures, so layouts and the new
    /// layered figures can be checked without a device.
    /// </summary>
    public static class QcbhPreview
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [MenuItem("iOSVN/Preview/Render QCBH screens")]
        public static void RenderAll()
        {
            var samples = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples"));
            var stateText = File.ReadAllText(Path.Combine(samples, "qcbh_state.json"));
            var battleText = File.ReadAllText(Path.Combine(samples, "qcbh_battle.json"));
            var log = new System.Text.StringBuilder();
            renderLog = log;
            void Prepare(PrototypeBootstrap c)
            {
                var hub = J.Parse(stateText);
                Set(c, "hub", hub);
                var typed = NetworkGameClient.ToGameState(hub);
                Set(c, "latestState", typed);
                Set(c, "currentCatalog", typed?.catalog);
            }
            void Step(string name, Action action)
            {
                try { action(); log.AppendLine(name + ": ok"); }
                catch (Exception ex) { log.AppendLine(name + ": FAIL " + (ex.InnerException ?? ex)); }
            }
            Render(c => { Prepare(c); Set(c, "gender", "nam"); Step("creator-m", () => Call(c, "ShowCreator", false)); }, "qcbh-creator-nam.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "gender", "nu");
                Set(c, "creatorLook", AvatarComposer.Default(true));
                Step("creator-f", () => Call(c, "ShowCreator", false));
            }, "qcbh-creator-nu.png", 1280, 590);
            Render(c => { Prepare(c); Step("world", () => Call(c, "ShowWorld")); }, "qcbh-world.png", 1280, 590);
            Render(c => { Prepare(c); Step("world-routes", () => { Call(c, "ShowWorld"); var w = GameObject.Find("ProvinceWorld")?.GetComponent<ProvinceWorld>(); if (w != null) w.SetZoom(.85f); }); }, "qcbh-world-routes.png", 1280, 590);
            Render(c => { Prepare(c); Step("home-profile", () => Call(c, "ShowHome", NetworkGameClient.ToGameState(J.Parse(stateText)))); }, "qcbh-home-profile.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "offlinePreview", true);
                Step("world-atlas", () => Call(c, "ShowMap", NetworkGameClient.ToGameState(J.Parse(stateText))));
            }, "qcbh-world-atlas.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "offlinePreview", true);
                Set(c, "atlasImmortalRealm", true);
                Set(c, "atlasRealmInitialized", true);
                Step("world-atlas-tien", () => Call(c, "ShowMap", NetworkGameClient.ToGameState(J.Parse(stateText))));
            }, "qcbh-world-atlas-tien.png", 1280, 590);
            Render(c => { Prepare(c); Step("teleport", () => Call(c, "OpenTeleportScreen", true)); }, "qcbh-teleport.png", 1280, 590);
            Render(c => { Prepare(c); Step("world-zoom", () => { Call(c, "ShowWorld"); var w = GameObject.Find("ProvinceWorld")?.GetComponent<ProvinceWorld>(); if (w != null) w.SetZoom(4.6f); }); }, "qcbh-world-zoom.png", 1280, 590);
            // the whole province from high up: how far the cities lie from each other
            Render(c => { Prepare(c); Step("world-wide", () => { Call(c, "ShowWorld"); var w = GameObject.Find("ProvinceWorld")?.GetComponent<ProvinceWorld>(); if (w != null) w.SetZoom(.85f); }); }, "qcbh-world-wide.png", 1280, 590);
            // flying sword and mount: the rider crosses rock and forest, with the trail behind
            foreach (var (file, item) in new[]
            {
                ("qcbh-fly-sword.png", "{\"uid\":\"preview-sword\",\"kind\":\"equip\",\"id\":\"phi_kiem_thanh_phong_phi_kiem\",\"name\":\"Thanh Phong Phi Kiếm\",\"slot\":\"phi_kiem\",\"tier\":\"huyen\",\"qualityRank\":2,\"mount\":false,\"flySpeed\":0.2}"),
                ("qcbh-fly-mount.png", "{\"uid\":\"preview-mount\",\"kind\":\"equip\",\"id\":\"toa_ky_bach_van\",\"name\":\"Bạch Vân Linh Hạc\",\"slot\":\"phi_kiem\",\"tier\":\"hoang\",\"qualityRank\":1,\"mount\":true,\"flySpeed\":0.2}"),
            })
            {
                var flying = stateText.Replace("\"phiKiem\":null", "\"phiKiem\":" + item);
                var name = Path.GetFileNameWithoutExtension(file);
                Render(c =>
                {
                    var hub = J.Parse(flying);
                    Set(c, "hub", hub);
                    var typed = NetworkGameClient.ToGameState(hub);
                    Set(c, "latestState", typed);
                    Set(c, "currentCatalog", typed?.catalog);
                    Step(name, () =>
                    {
                        if (flying == stateText) throw new InvalidOperationException("sample state has no empty phiKiem slot");
                        Call(c, "ShowWorld");
                        Call(c, "PreviewFlight");
                    });
                }, file, 1280, 590);
            }
            // an account made by an older client (no stored look): the world must still open
            var legacyPath = Path.Combine(samples, "qcbh_state_legacy.json");
            if (File.Exists(legacyPath))
            {
                var legacyText = File.ReadAllText(legacyPath);
                Render(c =>
                {
                    var hub = J.Parse(legacyText);
                    Set(c, "hub", hub);
                    var typed = NetworkGameClient.ToGameState(hub);
                    Set(c, "latestState", typed);
                    Set(c, "currentCatalog", typed?.catalog);
                    Step("world-legacy", () => Call(c, "ShowWorld"));
                }, "qcbh-world-legacy.png", 1280, 590);
            }
            Render(c => { Step("register", () => Call(c, "ShowAccountForm", true)); }, "qcbh-register.png", 1280, 590);
            Render(c => { Step("login", () => Call(c, "ShowAccountForm", false)); }, "qcbh-login.png", 1280, 590);
            Render(c => { Step("register-ipad", () => Call(c, "ShowAccountForm", true)); }, "qcbh-register-ipad.png", 1024, 768);
            Render(c => { Prepare(c); Step("character", () => Call(c, "OpenCharacterScreen")); }, "qcbh-character.png", 1280, 590);
            Render(c => { Prepare(c); Step("city", () => Call(c, "ShowCity", J.Parse(stateText)["town"]["id"].Str())); }, "qcbh-city.png", 1280, 590);
            Render(c => { Prepare(c); Step("bag", () => Call(c, "OpenBagScreen")); }, "qcbh-bag.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                // an item selected, so the sheet with the comparison against the equipped piece is visible
                var bag = J.Parse(stateText)["player"]["bag"];
                var pick = bag.Count > 0 ? bag[bag.Count - 1]["uid"].Str() : null;
                foreach (var item in bag.Items) if (item["kind"].Str() == "equip") pick = item["uid"].Str();
                Set(c, "bagSelected", pick);
                Step("bag-item", () => Call(c, "OpenBagScreen"));
            }, "qcbh-bag-item.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Step("battle", () => Call(c, "BuildActionBattle", J.Parse(battleText)["battle"]));
            }, "qcbh-battle.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Step("battle-fx", () => { Call(c, "BuildActionBattle", J.Parse(battleText)["battle"]); Moment(c, "actionBattle"); });
            }, "qcbh-battle-fx.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Step("battle-ipad", () => { Call(c, "BuildActionBattle", J.Parse(battleText)["battle"]); Moment(c, "actionBattle"); });
            }, "qcbh-battle-ipad.png", 1024, 768);
            var pvpPath = Path.Combine(samples, "qcbh_pvp.json");
            if (File.Exists(pvpPath))
            {
                var pvpText = File.ReadAllText(pvpPath);
                Render(c =>
                {
                    Prepare(c);
                    Step("pvp", () => { Call(c, "BuildPvpArena", J.Parse(pvpText)["battle"]); Moment(c, "pvpArena"); });
                }, "qcbh-pvp.png", 1280, 590);
            }
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/qcbh-captures/log.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, log.ToString());
            Debug.Log("QCBH_PREVIEW_DONE\n" + log);
        }

        private static void Set(object target, string field, object value)
        {
            var f = typeof(PrototypeBootstrap).GetField(field, Flags);
            if (f == null) throw new MissingFieldException(field);
            f.SetValue(target, value);
        }

        /// <summary>Asks a battle scene (field on the controller) to hold a mid-fight moment with effects showing.</summary>
        private static void Moment(PrototypeBootstrap controller, string field)
        {
            var f = typeof(PrototypeBootstrap).GetField(field, Flags);
            var scene = f?.GetValue(controller);
            if (scene == null) throw new MissingFieldException(field);
            var m = scene.GetType().GetMethod("PreviewMoment", Flags);
            if (m == null) throw new MissingMethodException("PreviewMoment");
            m.Invoke(scene, null);
        }

        private static void Call(object target, string method, params object[] args)
        {
            var m = typeof(PrototypeBootstrap).GetMethod(method, Flags);
            if (m == null) throw new MissingMethodException(method);
            m.Invoke(target, args);
        }

        /// <summary>Edit mode does not tick MonoBehaviours: run Start/Update/LateUpdate by hand so maps centre and auras animate.</summary>
        private static void Tick(int frames)
        {
            // Only the game's own components: Unity's UI classes have overloads and editor-only paths.
            bool Ours(MonoBehaviour mb) => mb != null && !(mb is PrototypeBootstrap) && (mb.GetType().Namespace ?? "").StartsWith("IOSVN");
            void Invoke(MonoBehaviour mb, string name)
            {
                try { mb.GetType().GetMethod(name, Flags, null, Type.EmptyTypes, null)?.Invoke(mb, null); }
                catch (Exception ex) { Debug.LogWarning("QCBH_TICK " + mb.GetType().Name + "." + name + ": " + (ex.InnerException ?? ex).Message); }
            }
            foreach (var mb in SceneBehaviours())
                if (Ours(mb)) Invoke(mb, "Start");
            for (var i = 0; i < frames; i++)
                foreach (var mb in SceneBehaviours())
                {
                    if (!Ours(mb)) continue;
                    Invoke(mb, "Update");
                    Invoke(mb, "LateUpdate");
                }
        }

        private static MonoBehaviour[] SceneBehaviours()
        {
            var list = new System.Collections.Generic.List<MonoBehaviour>();
            foreach (var mb in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
                if (mb != null && mb.gameObject.scene.IsValid() && mb.gameObject.activeInHierarchy) list.Add(mb);
            return list.ToArray();
        }

        private static System.Text.StringBuilder renderLog;

        private static void Render(Action<PrototypeBootstrap> show, string filename, int width, int height)
        {
            try { RenderUnsafe(show, filename, width, height); }
            catch (Exception ex) { renderLog?.AppendLine("render " + filename + ": FAIL " + (ex.InnerException ?? ex)); Debug.LogException(ex); }
        }

        private static void RenderUnsafe(Action<PrototypeBootstrap> show, string filename, int width, int height)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controller = new GameObject("QcbhPreview").AddComponent<PrototypeBootstrap>();
            Call(controller, "BuildCanvas");
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
            for (var pass = 0; pass < 3; pass++)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
                foreach (var group in canvas.GetComponentsInChildren<LayoutGroup>(true))
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }
            Tick(3);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/qcbh-captures"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, filename), texture.EncodeToPNG());
            RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("QCBH_RENDERED: " + filename);
        }
    }
}
#endif
