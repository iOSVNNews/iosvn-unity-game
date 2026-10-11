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
            camera.targetTexture = null;
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

        public static void RenderCombat()
        {
            EnsureBattleTextures();
            var log = new System.Text.StringBuilder();
            RenderBattleHud(log);
            RenderTravel(log);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/figure-captures/combat-log.txt")), log.ToString());
        }

        public static void RenderInterface()
        {
            EnsureBattleTextures();
            Directory.CreateDirectory(OutDir);
            var log = new System.Text.StringBuilder();
            foreach (var screen in new[] { "world", "world_zoom", "city", "boss_preview", "character_female", "character_male" })
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var controller = new GameObject("InterfacePreview").AddComponent<PrototypeBootstrap>();
                Invoke(controller, "BuildCanvas");
                var canvas = GameObject.Find("GameCanvas").GetComponent<Canvas>();
                const int width = 1280, height = 590;
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = height * .5f;
                camera.transform.position = new Vector3(0, 0, -1000);
                camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
                var target = new RenderTexture(width, height, 24); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100;
                Invoke(canvas.GetComponent<CanvasScaler>(), "Handle");
                var hub = J.Parse(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples/qcbh_state.json"))));
                var chosen = AvatarComposer.Default(screen != "character_male"); chosen.Set("wp", 9);
                hub["player"].Set("look", chosen.ToString());
                var worn = LookSpec.Parse(chosen.ToString()); worn.Set("wp", 6);
                hub["player"].Set("lookWorn", worn.ToString());
                Set(controller, "hub", hub); Set(controller, "offlinePreview", true);
                var typed = NetworkGameClient.ToGameState(hub);
                Set(controller, "latestState", typed); Set(controller, "currentCatalog", typed.catalog);
                var displayed = (LookSpec)typeof(PrototypeBootstrap).GetMethod("LookOf", Flags | BindingFlags.Static).Invoke(null, new object[] { hub["player"] });
                if (displayed.Int("wp") != 9) throw new Exception("Equipped weapon replaced the chosen cosmetic weapon");
                if (screen == "city") Invoke(controller, "ShowCity", hub["town"]["id"].Str());
                else if (screen.StartsWith("character")) Invoke(controller, "OpenCharacterScreen");
                else
                {
                    Invoke(controller, "ShowWorld"); Canvas.ForceUpdateCanvases();
                    var world = GameObject.Find("ProvinceWorld").GetComponent<ProvinceWorld>();
                    Invoke(world, "Start");
                    if (screen == "world_zoom") world.SetZoom(world.MaxZoom);
                    if (screen == "world")
                    {
                        var pad = canvas.transform.Find("Background/SafeArea/Content/WorldHud/WorldStick");
                        if (pad == null) pad = Array.Find(canvas.GetComponentsInChildren<RectTransform>(), r => r.name == "WorldStick");
                        if (pad == null) throw new Exception("World joystick is missing");
                        var stick = pad.GetComponent<MonoBehaviour>();
                        foreach (var behaviour in pad.GetComponents<MonoBehaviour>()) if (behaviour.GetType().Name == "BattleStick") stick = behaviour;
                        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { pointerId = 3,
                            pointerPressRaycast = new UnityEngine.EventSystems.RaycastResult { module = canvas.GetComponent<GraphicRaycaster>() } };
                        eventData.position = RectTransformUtility.WorldToScreenPoint(camera, ((RectTransform)pad).TransformPoint(new Vector2(80, 0)));
                        Invoke(stick, "OnPointerDown", eventData);
                        if (world.VirtualStick.x < .4f) throw new Exception("World touch joystick did not deliver movement");
                        var second = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { pointerId = 4 };
                        Invoke(stick, "OnPointerUp", second);
                        if (world.VirtualStick.x < .4f) throw new Exception("Another touch cancelled the active joystick");
                        Invoke(stick, "OnPointerUp", eventData);
                        if (world.VirtualStick != Vector2.zero) throw new Exception("Joystick remained stuck after release");
                        log.AppendLine("touch joystick: movement, independent pointers and release OK");
                        ValidateWorldMovement(world, log);
                        var monster = world.Actors.Find(a => a.Kind == "boss");
                        if (monster != null)
                        {
                            WorldActor tapped = null; world.OnActorTap = a => tapped = a;
                            var bodyPoint = monster.Rect.TransformPoint(new Vector2(0, monster.Rect.rect.height * .6f));
                            world.HandleTap(RectTransformUtility.WorldToScreenPoint(camera, bodyPoint));
                            if (tapped != monster) throw new Exception("Tapping the monster body missed its target");
                            world.SetHp(monster, 1, 2);
                            if (monster.HpFill.type != Image.Type.Filled || Mathf.Abs(monster.HpFill.fillAmount - .5f) > .001f) throw new Exception("Monster HP bar does not reflect actual health");
                            world.SetHp(monster, 1, 1);
                            log.AppendLine("monster body hit target and health bar OK");
                        }
                    }
                    Invoke(world, "LateUpdate");
                    if (screen == "boss_preview")
                    {
                        var actor = world.Actors.Find(a => a.Kind == "boss");
                        if (actor == null) throw new Exception("Missing sample boss");
                        Invoke(controller, "HandleWorldActor", actor);
                        var details = Array.Find(canvas.GetComponentsInChildren<Text>(), t => t.name == "Details");
                        if (details == null || !details.text.Contains("Khí huyết")) throw new Exception("Missing monster preview");
                    }
                }
                for (var pass = 0; pass < 3; pass++)
                {
                    Canvas.ForceUpdateCanvases();
                    foreach (var group in canvas.GetComponentsInChildren<LayoutGroup>()) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
                }
                foreach (var figure in canvas.GetComponentsInChildren<CultivatorFigure2D>()) Invoke(figure, "LateUpdate");
                Save(camera, target, width, height, screen + ".png"); log.AppendLine(screen + ": OK");
            }
            RenderBattleHud(log);
            File.WriteAllText(Path.Combine(OutDir, "interface-log.txt"), log.ToString());
            Debug.Log("INTERFACE_PREVIEW_DONE\n" + log);
        }

        private static void ValidateWorldMovement(ProvinceWorld world, System.Text.StringBuilder log)
        {
            var original = world.Player.Pos;
            var originalData = world.Data;
            world.Data = new WorldMapData { w = 32, h = 32, Blocked = new bool[1024], Border = new bool[1024], Water = new bool[1024], Road = new bool[1024] };
            world.OnPlayerStep = null;
            world.Teleport(world.Player, new Vector2(10, 10));
            world.Player.Path = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(20, 20) };
            Invoke(world, "MovePlayerDirection", Vector2.right, .1f);
            if (world.Player.Pos.x <= 10 || world.Player.Path != null) throw new Exception("Manual movement did not move / cancel the old route");
            world.Teleport(world.Player, new Vector2(10, 10)); world.Data.Blocked[10 * 32 + 11] = true;
            Invoke(world, "MovePlayerDirection", Vector2.right, .25f);
            if (world.Player.Pos.x != 10) throw new Exception("Manual movement crossed blocked terrain");
            Invoke(world, "StepActor", world.Player, .1f, true);
            if (world.Player.Moving) throw new Exception("Blocked character keeps walking in place");
            world.Data.Blocked[10 * 32 + 11] = false;
            var arrived = false;
            if (!world.WalkTo(new Vector2Int(20, 20), () => arrived = true)) throw new Exception("Tap route could not be created");
            for (var i = 0; i < 200 && world.Player.Path != null; i++) Invoke(world, "StepActor", world.Player, .05f, false);
            if (!arrived || world.TileOf(world.Player.Pos) != new Vector2Int(20, 20)) throw new Exception("Tap route failed to reach its destination");
            world.Stop(); world.Data = originalData; world.Teleport(world.Player, original);
            log.AppendLine("world movement: manual, collision, route cancellation and tap destination OK");
        }

        private static void EnsureBattleTextures()
        {
            foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/BattleMaps"), "*.png"))
            {
                var path = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.maxTextureSize >= 2048) continue;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            var snow = Resources.Load<Texture2D>("BattleMaps/battle-pham-pve-bang-lien");
            if (snow == null || snow.width < 1600) throw new Exception("Battle backdrop was still downsampled on import");
            Debug.Log("Battle backdrop native width: " + snow.width);
        }

        public static void RenderTravelMenu()
        {
            Directory.CreateDirectory(OutDir);
            var log = new System.Text.StringBuilder();
            RenderTravel(log);
            File.WriteAllText(Path.Combine(OutDir, "travel-log.txt"), log.ToString());
            Debug.Log(log);
        }

        private static void RenderTravel(System.Text.StringBuilder log)
        {
            foreach (var female in new[] { false, true })
            {
                const int width = 1536, height = 1600;
                var (canvas, camera, target) = Stage(width, height, new Color(.91f, .89f, .83f));
                for (var direction = 0; direction < 8; direction++)
                for (var phase = 0; phase < 4; phase++)
                {
                    var holder = new GameObject("Direction_" + direction + "_" + phase, typeof(RectTransform)).GetComponent<RectTransform>();
                    holder.SetParent(canvas.transform, false);
                    holder.anchorMin = holder.anchorMax = new Vector2(0, 1);
                    holder.pivot = new Vector2(0, 1);
                    holder.sizeDelta = new Vector2(192, 390);
                    holder.anchoredPosition = new Vector2(direction * 192, -phase * 400);
                    var figure = CultivatorFigure2D.Create(holder, AvatarComposer.Default(female));
                    var yaw = direction * Mathf.PI / 4f;
                    var vector = new Vector2(Mathf.Sin(yaw), -Mathf.Cos(yaw));
                    figure.AdvanceTravel(vector, phase * .65f + .0001f, 1f);
                    figure.SetMotion(FighterAction.Idle, 0f, true, vector.x >= 0f, phase * .2f);
                    Invoke(figure, "LateUpdate");
                    if (direction == 4)
                        foreach (var image in figure.GetComponentsInChildren<Image>())
                            if (image.name.StartsWith("eye") && image.enabled) throw new Exception("Walking away still shows front-facing eyes");
                }
                Save(camera, target, width, height, "travel_" + (female ? "female" : "male") + ".png");
                log.AppendLine("travel " + (female ? "female" : "male") + ": eight directions / four stride phases OK");
            }
        }

        private static void RenderBattleHud(System.Text.StringBuilder log)
        {
            foreach (var mode in new[] { "boss", "regular", "snow" })
            foreach (var outcome in new[] { "fight", "win", "loss" })
            {
                var regular = mode != "boss";
                var result = outcome != "fight";
                if (regular && result) continue;
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
                var source = File.ReadAllText(Path.Combine(samples, "qcbh_battle.json"));
                if (regular) source = source.Replace("\"kind\":\"boss\"", "\"kind\":\"pve\"").Replace("\"packSize\":5", "\"packSize\":1").Replace("\"minionCount\":4", "\"minionCount\":0");
                var state = J.Parse(source)["battle"];
                if (mode == "snow") state["battleMap"].Set("visualThemeId", "frost");
                Invoke(controller, "BuildActionBattle", state);
                var battle = controller.GetType().GetField("actionBattle", Flags).GetValue(controller);
                foreach (var bar in canvas.GetComponentsInChildren<RectTransform>())
                    if (bar.name == "Bar" && bar.parent.Find("StatusSurface") is Transform status && bar.GetSiblingIndex() < status.GetSiblingIndex())
                        throw new Exception("Status surface covers the health bar");
                if (result)
                {
                    var finish = outcome == "win"
                        ? J.Parse("{\"result\":\"win\",\"summary\":{\"exp\":1234,\"stones\":520,\"drops\":[],\"notes\":[\"Trang bị sau chiến đấu: độ bền giảm nhẹ.\"]}}")
                        : J.Parse("{\"result\":\"loss\",\"summary\":{\"expLost\":20,\"notes\":[\"Trọng thương, hồi sinh sau 3 phút. Độ bền mất 5–10 điểm tùy phẩm chất.\",\"Trọng thương làm hư hại trang bị: Tân Thủ Kiếm −5 (80/100); Tân Thủ Đạo Bào −5 (80/100); Tân Thủ Ngọc Bội −5 (80/100); Tân Thủ Hộ Phù −5 (80/100); Nhẫn Trữ Đồ Sơ Cấp −5 (80/100). Hãy đến Y Quán hồi phục.\",\"Quái thế giới tạm lùi lại, bạn vẫn có thể tiếp tục khiêu chiến.\"]}}");
                    Invoke(battle, "Finish", finish);
                }
                else Invoke(battle, regular ? "PreviewStanding" : "PreviewMoment");
                Canvas.ForceUpdateCanvases();
                Set(battle, "nextPoll", float.PositiveInfinity);
                Invoke(battle, "Update");
                foreach (var figure in canvas.GetComponentsInChildren<CultivatorFigure2D>()) Invoke(figure, "LateUpdate");
                foreach (var actor in canvas.GetComponentsInChildren<MonoBehaviour>())
                    if (actor.GetType().Name == "MonsterView") Invoke(actor, "Show");
                Save(camera, target, width, height, outcome == "loss" ? "battle_failure_large.png" : result ? "battle_result_large.png" : mode == "snow" ? "battle_snow.png" : regular ? "battle_regular.png" : "battle_hud_large.png");
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
