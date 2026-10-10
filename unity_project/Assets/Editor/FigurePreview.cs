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
    /// <summary>Import settings for the layered character atlases.</summary>
    public sealed class ModularCharacterImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Characters/Modular/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true;          // the same parts are drawn tiny on the world map
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }

    /// <summary>
    /// Renders the layered figure (motion contact sheets) and the new creator screen into build/figure-captures.
    /// Batch: Unity -batchmode -projectPath unity_project -executeMethod IOSVN.TuTien.Editor.FigurePreview.RenderAll -quit
    /// </summary>
    public static class FigurePreview
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/figure-captures"));

        [MenuItem("iOSVN/Figure/Render all previews")]
        public static void RenderAll()
        {
            var log = new System.Text.StringBuilder();
            Directory.CreateDirectory(OutDir);
            try { RenderMotionSheets(log); } catch (Exception ex) { log.AppendLine("motion: FAIL " + ex); Debug.LogException(ex); }
            try { RenderCreator(log); } catch (Exception ex) { log.AppendLine("creator: FAIL " + ex); Debug.LogException(ex); }
            try { RenderNumberPad(log); } catch (Exception ex) { log.AppendLine("number pad: FAIL " + ex); Debug.LogException(ex); }
            try { RenderBattleHud(log); } catch (Exception ex) { log.AppendLine("battle HUD: FAIL " + ex); Debug.LogException(ex); }
            File.WriteAllText(Path.Combine(OutDir, "log.txt"), log.ToString());
            Debug.Log("FIGURE_PREVIEW_DONE\n" + log);
        }

        [MenuItem("iOSVN/Figure/Render motion sheets")]
        public static void RenderMotionMenu() { var log = new System.Text.StringBuilder(); RenderMotionSheets(log); Debug.Log(log); }

        [MenuItem("iOSVN/Figure/Render creator")]
        public static void RenderCreatorMenu() { var log = new System.Text.StringBuilder(); RenderCreator(log); Debug.Log(log); }

        private static (Canvas canvas, Camera camera, RenderTexture target) Stage(int width, int height, Color background)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            return (canvas, camera, target);
        }

        private static void Save(Camera camera, RenderTexture target, int width, int height, string file)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, file), texture.EncodeToPNG());
            RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static readonly (string name, FighterAction action, bool moving, bool run, bool fly)[] Motions =
        {
            ("idle", FighterAction.Idle, false, false, false), ("walk", FighterAction.Idle, true, false, false),
            ("run", FighterAction.Idle, true, true, false), ("attack", FighterAction.Attack, false, false, false),
            ("cast", FighterAction.Cast, false, false, false), ("hurt", FighterAction.Hurt, false, false, false),
            ("fly", FighterAction.Idle, true, false, true), ("down", FighterAction.Down, false, false, false),
        };

        public static void RenderMotionSheets(System.Text.StringBuilder log)
        {
            if (!CultivatorFigure2D.Available) { log.AppendLine("motion: modular data missing"); return; }
            foreach (var female in new[] { false, true })
            {
                const int cell = 220, frames = 8;
                var width = cell * frames; var height = (int)(cell * 2.0f) * Motions.Length;
                var (canvas, camera, target) = Stage(width, height, new Color(.91f, .89f, .83f));
                var look = AvatarComposer.Default(female);
                foreach (var pair in new[] { ("fa", "1"), ("ey", female ? "4" : "3"), ("br", female ? "1" : "2"), ("no", "1"), ("mo", "1"), ("ha", female ? "3" : "0"),
                                             ("to", "1"), ("hat", female ? "0" : "1"), ("wp", "4"), ("au", "0"), ("ma", female ? "1" : "0") })
                    look.Set(pair.Item1, pair.Item2);
                var rowH = height / (float)Motions.Length;
                for (var m = 0; m < Motions.Length; m++)
                for (var f = 0; f < frames; f++)
                {
                    var holder = new GameObject("Cell", typeof(RectTransform)).GetComponent<RectTransform>();
                    holder.SetParent(canvas.transform, false);
                    holder.anchorMin = holder.anchorMax = new Vector2(0, 1);
                    holder.pivot = new Vector2(0, 1);
                    holder.sizeDelta = new Vector2(cell, rowH - 8);
                    holder.anchoredPosition = new Vector2(f * cell, -m * rowH - 4);
                    var figure = CultivatorFigure2D.Create(holder, look);
                    var mo = Motions[m];
                    figure.Running = mo.run;
                    figure.Airborne = mo.fly;
                    var t = f / (float)frames;
                    var clock = mo.moving ? t * (mo.run ? .57f : .83f) : t * 2.5f;
                    figure.SetMotion(mo.action, t, mo.moving, true, clock);
                    Invoke(figure, "LateUpdate");
                }
                Save(camera, target, width, height, "motion_" + (female ? "female" : "male") + ".png");
                log.AppendLine("motion " + (female ? "female" : "male") + ": OK");
            }
        }

        public static void RenderCreator(System.Text.StringBuilder log)
        {
            var samples = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples/qcbh_state.json"));
            var state = File.Exists(samples) ? File.ReadAllText(samples) : null;
            foreach (var female in new[] { false, true })
            foreach (var category in new[] { "preset", "ey", "ha", "br", "to", "au", "wp", "walk", "run" })
            {
                var name = "creator_" + (female ? "female" : "male") + "_" + category + ".png";
                try
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var controller = new GameObject("FigurePreview").AddComponent<PrototypeBootstrap>();
                    Invoke(controller, "BuildCanvas");
                    var canvas = GameObject.Find("GameCanvas").GetComponent<Canvas>();
                    const int width = 1280, height = 590;
                    var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                    camera.orthographic = true; camera.orthographicSize = height * .5f;
                    camera.transform.position = new Vector3(0, 0, -1000);
                    camera.nearClipPlane = .1f; camera.farClipPlane = 2000f;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                    var target = new RenderTexture(width, height, 24);
                    camera.targetTexture = target;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 100f;
                    Invoke(canvas.GetComponent<CanvasScaler>(), "Handle");
                    if (state != null)
                    {
                        var hub = J.Parse(state);
                        Set(controller, "hub", hub);
                        var typed = NetworkGameClient.ToGameState(hub);
                        Set(controller, "latestState", typed);
                        Set(controller, "currentCatalog", typed.catalog);
                    }
                    Set(controller, "gender", female ? "nu" : "nam");
                    Set(controller, "creatorLook", null);
                    Invoke(controller, "ShowCreator", false);
                    Set(controller, "creatorCategory", category == "walk" || category == "run" ? "ha" : category);
                    if (category == "walk" || category == "run") Set(controller, "creatorMotion", category);
                    var look = (LookSpec)controller.GetType().GetField("creatorLook", Flags).GetValue(controller);
                    if (category == "ha" || category == "br") look.Set("hc", "#dfc9a9");
                    if (category == "au") { look.Set("au", 4); look.Set("auc", "#ff5050"); }
                    Invoke(controller, "RefreshCreator");
                    Canvas.ForceUpdateCanvases();
                    foreach (var figure in canvas.GetComponentsInChildren<CultivatorFigure2D>()) Invoke(figure, "LateUpdate");
                    Save(camera, target, width, height, name);
                    log.AppendLine(name + ": OK");
                }
                catch (Exception ex)
                {
                    log.AppendLine(name + ": FAIL " + (ex.InnerException ?? ex));
                }
            }
        }

        private static void Set(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, Flags);
            if (f != null) { f.SetValue(target, value); return; }
            var p = target.GetType().GetProperty(field, Flags);
            p?.SetValue(target, value);
        }

        private static void RenderNumberPad(System.Text.StringBuilder log)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controller = new GameObject("NumberPadPreview").AddComponent<PrototypeBootstrap>();
            Invoke(controller, "BuildCanvas");
            var canvas = GameObject.Find("GameCanvas").GetComponent<Canvas>();
            const int width = 1280, height = 590;
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = height * .5f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100;
            Invoke(canvas.GetComponent<CanvasScaler>(), "Handle");
            Invoke(controller, "PromptNumber", "Mua Hồi Xuân Đan", "Giá 800 linh thạch mỗi cái. Bạn có 42.000.", 1L, 52L, 7L, "Mua", new Action<long>(_ => { }));
            Canvas.ForceUpdateCanvases();
            var field = canvas.GetComponentInChildren<InputField>();
            var pad = field.GetComponent<NumberPad>();
            var card = field.transform.parent.GetComponent<RectTransform>();
            var original = card.position;
            pad.Open(); pad.Press("3"); pad.Press("0");
            if (field.text != "30" || field.enabled) throw new Exception("Custom keypad input / native keyboard suppression failed");
            Save(camera, target, width, height, "quantity_keyboard.png");
            pad.Press("9");
            if (field.text != "52") throw new Exception("Maximum quantity clamp failed");
            pad.Press("⌫"); pad.Press("0");
            if (field.text != "50") throw new Exception("Backspace failed");
            pad.Press("Xong");
            if (canvas.transform.Find("QuantityKeyboard") != null || Vector3.Distance(card.position, original) > .01f) throw new Exception("Done did not dismiss / restore the modal");
            pad.Open(); pad.Press("0"); pad.Close();
            if (field.text != "1") throw new Exception("Minimum quantity clamp failed");
            pad.Open();
            canvas.transform.Find("QuantityKeyboard").GetComponent<Button>().onClick.Invoke();
            if (canvas.transform.Find("QuantityKeyboard") != null) throw new Exception("Outside tap did not dismiss");
            pad.Open();
            UnityEngine.Object.DestroyImmediate(field.gameObject);
            if (canvas.transform.Find("QuantityKeyboard") != null) throw new Exception("Modal lifetime cleanup failed");
            log.AppendLine("number pad: input, clamp, delete, done, outside tap and cleanup OK");
        }

        private static void RenderBattleHud(System.Text.StringBuilder log)
        {
            foreach (var result in new[] { false, true })
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var controller = new GameObject("BattleHudPreview").AddComponent<PrototypeBootstrap>();
                Invoke(controller, "BuildCanvas");
                var canvas = GameObject.Find("GameCanvas").GetComponent<Canvas>();
                const int width = 1280, height = 590;
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = height * .5f;
                camera.transform.position = new Vector3(0, 0, -1000);
                camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
                var target = new RenderTexture(width, height, 24);
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100;
                Invoke(canvas.GetComponent<CanvasScaler>(), "Handle");
                var samples = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples"));
                var hub = J.Parse(File.ReadAllText(Path.Combine(samples, "qcbh_state.json")));
                Set(controller, "hub", hub);
                var typed = NetworkGameClient.ToGameState(hub);
                Set(controller, "latestState", typed); Set(controller, "currentCatalog", typed.catalog);
                Invoke(controller, "BuildActionBattle", J.Parse(File.ReadAllText(Path.Combine(samples, "qcbh_battle.json")))["battle"]);
                var battle = controller.GetType().GetField("actionBattle", Flags).GetValue(controller);
                if (result) Invoke(battle, "Finish", J.Parse("{\"result\":\"win\",\"summary\":{\"exp\":1234,\"stones\":520,\"drops\":[],\"notes\":[\"Trang bị sau chiến đấu: độ bền giảm nhẹ.\"]}}"));
                else Invoke(battle, "PreviewMoment");
                Canvas.ForceUpdateCanvases();
                foreach (var figure in canvas.GetComponentsInChildren<CultivatorFigure2D>()) Invoke(figure, "LateUpdate");
                Save(camera, target, width, height, result ? "battle_result_large.png" : "battle_hud_large.png");
                log.AppendLine(result ? "battle result: OK" : "battle HUD: OK");
            }
        }

        private static void Invoke(object target, string method, params object[] args)
        {
            foreach (var m in target.GetType().GetMethods(Flags))
                if (m.Name == method && m.GetParameters().Length == args.Length) { m.Invoke(target, args); return; }
            throw new MissingMethodException(method);
        }
    }
}
#endif
