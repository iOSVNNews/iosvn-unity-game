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
        private static bool validateFontFamily;

        public static void ReviewCreatorAndFonts()
        {
            validateFontFamily = true;
            var state = File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples/qcbh_state.json")));
            renderLog = new System.Text.StringBuilder();
            foreach (var female in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 590), new Vector2Int(1024, 768) })
            {
                Render(c =>
                {
                    var hub = J.Parse(state);
                    Set(c, "hub", hub);
                    var typed = NetworkGameClient.ToGameState(hub);
                    Set(c, "latestState", typed);
                    Set(c, "currentCatalog", typed.catalog);
                    Set(c, "gender", female ? "nu" : "nam");
                    Set(c, "creatorLook", AvatarComposer.Default(female));
                    Call(c, "ShowCreator", false);
                    var name = (InputField)typeof(PrototypeBootstrap).GetField("nameInput", Flags).GetValue(c);
                    name.text = "Đạo Hữu Kiểm Thử";
                    Call(c, "SetCreatorGender", female ? "nam" : "nu");
                    if (name.text != "Đạo Hữu Kiểm Thử") throw new Exception("Gender change lost the typed name");
                    Call(c, "SetCreatorGender", female ? "nu" : "nam");
                    var rig = (Component)typeof(PrototypeBootstrap).GetField("creatorRig", Flags).GetValue(c);
                    var body = rig.transform.Find("Body").GetComponent<Image>();
                    if (!body.sprite.name.Contains("FullBodyActorsV3")) throw new Exception("Creator does not show the current ink portrait");
                    if (!(bool)body.GetType().GetProperty("PreservePaintedShape").GetValue(body)) throw new Exception("Creator distorts the painted face");
                    var fullBody = (Component)typeof(PrototypeBootstrap).GetField("creatorActor", Flags).GetValue(c);
                    if (!fullBody.transform.Find("ContinuousBody").GetComponent<Image>().sprite.name.Contains("FullBodyActorsV3")) throw new Exception("Missing full-body preview");
                    using (var mesh = new VertexHelper())
                    {
                        body.GetType().GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null).Invoke(body, new object[] { mesh });
                        if (mesh.currentVertCount != 4) throw new Exception("Creator still warps the painted face/body mesh");
                    }
                    Set(c, "creatorFullBodyPreview", true);
                    Call(c, "RefreshCreator");
                    if (rig.gameObject.activeSelf || !fullBody.gameObject.activeSelf) throw new Exception("Full-body preview tab did not switch");
                    Set(c, "creatorFullBodyPreview", false);
                    Call(c, "RefreshCreator");
                    var motion = rig.GetType().GetMethod("SetMotion");
                    string Fingerprint()
                    {
                        motion.Invoke(rig, new object[] { FighterAction.Idle, 0f, false, false, 0f });
                        var text = new System.Text.StringBuilder();
                        foreach (var image in rig.GetComponentsInChildren<Image>())
                            text.Append(image.enabled).Append(image.sprite?.name).Append(image.sprite?.rect.ToString()).Append(image.color.ToString());
                        foreach (var property in new[] { "_HairTint", "_SkinTint", "_RobeTint", "_EyeTint" }) text.Append(body.material.GetColor(property).ToString());
                        return text.ToString();
                    }
                    foreach (var key in new[] { "hc", "sk", "oc", "hat", "wp", "au" })
                    {
                        Set(c, "creatorCategory", key);
                        var before = Fingerprint();
                        // Exercise the actual next-button listener, not just SetLook.
                        rig.transform.root.Find("Background/SafeArea/Content/Creator/Inner/Custom/Stepper/Next")
                            .GetComponent<Button>().onClick.Invoke();
                        if (before == Fingerprint()) throw new Exception("Creator option has no visual effect: " + key);
                    }
                    var look = (LookSpec)typeof(PrototypeBootstrap).GetField("creatorLook", Flags).GetValue(c);
                    var expectedHair = body.material.GetColor("_HairTint");
                    var expectedRobe = body.material.GetColor("_RobeTint");
                    Call(c, "ApplyOfflineCharacterChoice", new RegisterChoice { name = name.text, gender = female ? "nu" : "nam", look = look.ToString() });
                    var prefKeys = new[] { "tutien_offline_character_demo", "tutien_offline_look" };
                    var prefExists = Array.ConvertAll(prefKeys, PlayerPrefs.HasKey);
                    var prefValues = Array.ConvertAll(prefKeys, key => PlayerPrefs.GetString(key));
                    try
                    {
                        Call(c, "PersistOfflineAppearance", look.ToString());
                        var restored = (RegisterChoice)typeof(PrototypeBootstrap).GetMethod("ReadOfflineCharacterChoice", Flags | BindingFlags.Static).Invoke(null, null);
                        if (restored.look != look.ToString() || restored.gender != (female ? "nu" : "nam"))
                            throw new Exception("Offline appearance is lost after reload");
                    }
                    finally
                    {
                        for (var i = 0; i < prefKeys.Length; i++)
                            if (prefExists[i]) PlayerPrefs.SetString(prefKeys[i], prefValues[i]); else PlayerPrefs.DeleteKey(prefKeys[i]);
                        PlayerPrefs.Save();
                    }
                    if (hub["player"]["look"].Str() != look.ToString() || hub["player"]["lookWorn"].Str() != look.ToString())
                        throw new Exception("Created appearance was not retained by the player");
                    Call(c, "ShowWorld");
                    var world = GameObject.Find("ProvinceWorld").GetComponent<ProvinceWorld>();
                    var worldBody = world.Player.Rect.Find("SkinnedActor/ContinuousBody").GetComponent<Image>();
                    if (worldBody.material.GetColor("_HairTint") != expectedHair || worldBody.material.GetColor("_RobeTint") != expectedRobe)
                        throw new Exception("World discarded the creator appearance");
                    Call(c, "BuildActionBattle", J.Parse(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples/qcbh_battle.json"))))["battle"]);
                    var battleBody = GameObject.Find("Player/SkinnedActor/ContinuousBody").GetComponent<Image>();
                    if (battleBody.material.GetColor("_HairTint") != expectedHair || battleBody.material.GetColor("_RobeTint") != expectedRobe)
                        throw new Exception("Combat discarded the creator appearance");
                    Call(c, "ShowCreator", true);
                    if (((LookSpec)typeof(PrototypeBootstrap).GetField("creatorLook", Flags).GetValue(c)).ToString() != look.ToString())
                        throw new Exception("Reopening appearance lost saved choices");
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in GameObject.Find("GameCanvas").GetComponentsInChildren<Text>())
                        if (text.font == null || !text.font.name.StartsWith("OpenSans")) throw new Exception("Old font: " + text.name);
                    foreach (var weight in new[] { "Regular", "SemiBold", "Bold" })
                    {
                        var font = Resources.Load<Font>("Fonts/OpenSans-" + weight);
                        font.RequestCharactersInTexture("Tiếng Việt: Đạo hữu, khí vận, khuôn mặt, căn cơ", 24);
                        foreach (var ch in "ĐđăâêôơưĂÂÊÔƠƯáàảãạấầẩẫậắằẳẵặếềểễệốồổỗộớờởỡợứừửữự")
                            if (!font.HasCharacter(ch)) throw new Exception("Open Sans lacks Vietnamese glyph: " + ch);
                    }
                    renderLog.AppendLine("creator options, name retention, saved look, world/combat appearance, reopen, Open Sans Vietnamese: ok " + female + " " + size);
                }, "creator-final-" + (female ? "female-" : "male-") + size.x + ".png", size.x, size.y);
            }
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/qcbh-captures/creator-review.txt")), renderLog.ToString());
            Debug.Log("CREATOR_APPEARANCE_REVIEW_DONE\n" + renderLog);
            RenderAll();
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            ValidateCharacterMotion();
            ValidateMapPresentation();
            ValidateReportedErrors();
        }

        [MenuItem("iOSVN/Preview/Render creator figures")]
        public static void RenderCreatorFigures()
        {
            var state = File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples/qcbh_state.json")));
            renderLog = new System.Text.StringBuilder();
            foreach (var female in new[] { false, true })
            {
                Render(c =>
                {
                    var hub = J.Parse(state);
                    Set(c, "hub", hub);
                    var typed = NetworkGameClient.ToGameState(hub);
                    Set(c, "latestState", typed);
                    Set(c, "currentCatalog", typed?.catalog);
                    Set(c, "gender", female ? "nu" : "nam");
                    Set(c, "creatorLook", AvatarComposer.Default(female));
                    Call(c, "ShowCreator", false);
                }, female ? "qcbh-creator-nu-review.png" : "qcbh-creator-nam-review.png", 1280, 590);
            }
            Debug.Log("QCBH_CREATOR_REVIEW\n" + renderLog);
        }

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
                Set(c, "gender", "nam");
                Step("creator-face-edit", () =>
                {
                    Call(c, "ShowCreator", false);
                    Set(c, "creatorCategory", "fa");
                    Call(c, "RefreshCreator");
                });
            }, "qcbh-creator-face-edit.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Step("opening-intro", () => { Call(c, "BeginOpeningIntro"); Call(c, "UpdateOpeningIntroVisuals", 3.4f); });
            }, "qcbh-opening-intro.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "gender", "nu");
                Set(c, "creatorLook", AvatarComposer.Default(true));
                Step("creator-f", () => Call(c, "ShowCreator", false));
            }, "qcbh-creator-nu.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "gender", "nam");
                var look = AvatarComposer.Default(false);
                look.Set("bo", 0);
                look.Set("sk", "#9a6a4a");
                Set(c, "creatorLook", look);
                Step("creator-m-slim-dark", () => { Call(c, "ShowCreator", false); Set(c, "creatorCategory", "bo"); Call(c, "RefreshCreator"); });
            }, "qcbh-creator-nam-body-dark.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "gender", "nu");
                var look = AvatarComposer.Default(true);
                look.Set("bo", 3);
                look.Set("sk", "#c08860");
                Set(c, "creatorLook", look);
                Step("creator-f-curvy-dark", () => { Call(c, "ShowCreator", false); Set(c, "creatorCategory", "bo"); Call(c, "RefreshCreator"); });
            }, "qcbh-creator-nu-body-dark.png", 1280, 590);
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
            Render(c => { Prepare(c); Step("world-zoom", () => { Call(c, "ShowWorld"); var w = GameObject.Find("ProvinceWorld")?.GetComponent<ProvinceWorld>(); if (w != null) w.SetZoom(w.MaxZoom); }); }, "qcbh-world-zoom.png", 1280, 590);
            // the whole province from high up: how far the cities lie from each other
            Render(c => { Prepare(c); Step("world-wide", () => { Call(c, "ShowWorld"); var w = GameObject.Find("ProvinceWorld")?.GetComponent<ProvinceWorld>(); if (w != null) w.SetZoom(w.MinZoom); }); }, "qcbh-world-wide.png", 1280, 590);
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
            Render(c =>
            {
                Prepare(c);
                Set(c, "offlinePreview", true);
                Set(c, "cityNearbyPlayers", new[]
                {
                    new PvpOpponent { userId = "preview-player-01", name = "Vân Kiếm", realmName = "Nguyên Anh", power = 2480, points = 1120 },
                    new PvpOpponent { userId = "preview-player-02", name = "Mộng Dao", realmName = "Kim Đan", power = 1840, points = 1040 },
                    new PvpOpponent { userId = "preview-player-03", name = "Huyền Tâm", realmName = "Trúc Cơ", power = 930, points = 1010 },
                });
                Step("city", () => Call(c, "ShowCity", J.Parse(stateText)["town"]["id"].Str()));
            }, "qcbh-city.png", 1280, 590);
            Render(c =>
            {
                Prepare(c);
                Set(c, "offlinePreview", true);
                Set(c, "cityNearbyPlayers", new[]
                {
                    new PvpOpponent { userId = "preview-player-01", name = "Vân Kiếm", realmName = "Nguyên Anh", power = 2480, points = 1120 },
                    new PvpOpponent { userId = "preview-player-02", name = "Mộng Dao", realmName = "Kim Đan", power = 1840, points = 1040 },
                    new PvpOpponent { userId = "preview-player-03", name = "Huyền Tâm", realmName = "Trúc Cơ", power = 930, points = 1010 },
                });
                Step("city-roster-expanded", () =>
                {
                    Call(c, "ShowCity", J.Parse(stateText)["town"]["id"].Str());
                    Call(c, "ToggleCityPresence");
                    Call(c, "SelectCityPresenceCategory", 1);
                });
            }, "qcbh-city-roster.png", 1280, 590);
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
            foreach (var (filename, logName, id, name, theme, layout) in new[]
            {
                ("qcbh-pve-forest.png", "pve-forest", "pham_pve_truc_lam", "Rừng Trúc Thanh Vân", "battle/pham/small-monster", "winding_forest_path"),
                ("qcbh-pve-wasteland.png", "pve-wasteland", "pham_pve_hoang_nguyen", "Hoang Nguyên Tàn Mộc", "battle/pham/pve-hoang-nguyen", "barren_ash_field"),
                ("qcbh-pve-frost.png", "pve-frost", "pham_pve_bang_lien", "Băng Liên Tuyết Cốc", "battle/pham/pve-bang-lien", "snowy_icefield"),
            })
            {
                Render(c =>
                {
                    Prepare(c);
                    var variant = J.Parse(battleText)["battle"];
                    variant["battleMap"].Set("id", id);
                    variant["battleMap"].Set("name", name);
                    variant["battleMap"].Set("visualThemeId", theme);
                    variant["battleMap"].Set("layout", layout);
                    Step(logName, () => { Call(c, "BuildActionBattle", variant); Moment(c, "actionBattle"); });
                }, filename, 1280, 590);
            }
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

        [MenuItem("iOSVN/Preview/Validate character motion")]
        public static void ValidateCharacterMotion()
        {
            renderLog = new System.Text.StringBuilder();
            foreach (var female in new[] { false, true })
            foreach (var pose in new[] { FighterAction.Idle, FighterAction.Walk, FighterAction.Attack, FighterAction.Cast, FighterAction.Hurt, FighterAction.Down })
            {
                Render(c =>
                {
                    var parent = new GameObject("MotionReview", typeof(RectTransform)).GetComponent<RectTransform>();
                    parent.SetParent(GameObject.Find("GameCanvas").transform, false);
                    parent.anchorMin = parent.anchorMax = new Vector2(.5f, .5f);
                    parent.sizeDelta = new Vector2(320f, 480f);
                    var rigType = typeof(AvatarComposer).Assembly.GetType("IOSVN.TuTien.Core.QcbhSkinnedActor2D", true);
                    var rig = (Component)rigType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { parent, AvatarComposer.Default(female) });
                    void Motion(FighterAction state, float progress, bool moving, bool right, float time) => rigType.GetMethod("SetMotion").Invoke(rig, new object[] { state, progress, moving, right, time });
                    Motion(pose, .5f, pose == FighterAction.Walk, false, .17f);
                    for (var i = 0; i < 120; i++) Motion(pose, .5f, pose == FighterAction.Walk, false, .17f);
                    var mesh = rig.transform.Find("ContinuousBody").GetComponent<Image>();
                    var map = mesh.GetType().GetMethod("MapPoint");
                    Vector2 Point(float x, float y) => (Vector2)map.Invoke(mesh, new object[] { new Vector2(x, y) });
                    var before = Point(.43f, .12f);
                    Motion(pose, .5f, pose == FighterAction.Walk, true, .50f);
                    rig.gameObject.SendMessage("LateUpdate");
                    if (rig.transform.localScale.x >= 0f) throw new Exception("Facing did not mirror");
                    if (pose == FighterAction.Walk && Vector2.Distance(before, Point(.43f, .12f)) < .001f)
                        throw new Exception("Walk mesh did not advance");
                    for (var y = .1f; y < .95f; y += .1f)
                    for (var x = .1f; x < .95f; x += .1f)
                    {
                        var point = Point(x, y);
                        if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y))
                            throw new Exception("Invalid skinning weights");
                    }
                    rigType.GetMethod("SetHit").Invoke(rig, new object[] { true });
                    rigType.GetMethod("SetHit").Invoke(rig, new object[] { false });
                    foreach (var image in rig.GetComponentsInChildren<Image>())
                        if (image.enabled && image.sprite == null) throw new Exception("Missing sprite: " + image.name);
                    Debug.Log("CHARACTER_MOTION_OK: " + (female ? "female" : "male") + " " + pose);
                }, "motion-" + (female ? "female-" : "male-") + pose.ToString().ToLowerInvariant() + ".png", 720, 590);
            }
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/qcbh-captures/motion-review.txt")), renderLog.ToString());
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            Debug.Log("CHARACTER_MOTION_REVIEW_DONE");
        }

        [MenuItem("iOSVN/Preview/Render skinned walk cycle")]
        public static void RenderWalkCycle()
        {
            renderLog = new System.Text.StringBuilder();
            for (var frame = 0; frame < 24; frame++)
            {
                var time = frame / 24f * (Mathf.PI * 2f / 9f);
                Render(c =>
                {
                    foreach (var female in new[] { false, true })
                    {
                        var parent = new GameObject("WalkingActor", typeof(RectTransform)).GetComponent<RectTransform>();
                        parent.SetParent(GameObject.Find("GameCanvas").transform, false);
                        parent.anchorMin = parent.anchorMax = new Vector2(female ? .68f : .32f, .5f);
                        parent.sizeDelta = new Vector2(400f, 650f);
                        var actorType = typeof(AvatarComposer).Assembly.GetType("IOSVN.TuTien.Core.QcbhSkinnedActor2D", true);
                        var look = AvatarComposer.Default(female); look.Set("wp", 0);
                        var actor = (Component)actorType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { parent, look });
                        actorType.GetMethod("SetMotion").Invoke(actor, new object[] { FighterAction.Walk, 0f, true, false, time });
                    }
                }, "walk-cycle-" + frame.ToString("000") + ".png", 900, 590);
            }
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            Debug.Log("WALK_CYCLE_RENDERED");
        }

        public static void ReviewSkinnedCharacters()
        {
            ValidateCharacterMotion();
            RenderAll();
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
        }

        public static void ValidateMapPresentation()
        {
            var samples = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples"));
            var state = File.ReadAllText(Path.Combine(samples, "qcbh_state.json"));
            renderLog = new System.Text.StringBuilder();
            void PrepareMap(PrototypeBootstrap c)
            {
                var hub = J.Parse(state);
                Set(c, "hub", hub);
                var typed = NetworkGameClient.ToGameState(hub);
                Set(c, "latestState", typed);
                Set(c, "currentCatalog", typed?.catalog);
                Call(c, "ShowWorld");
                Canvas.ForceUpdateCanvases();
                Tick(3);
            }
            void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
            foreach (var size in new[] { new Vector2Int(1280, 590), new Vector2Int(1024, 768) })
            {
                Render(c =>
                {
                    PrepareMap(c);
                    var world = GameObject.Find("ProvinceWorld").GetComponent<ProvinceWorld>();
                    Check(!world.Player.Body.enabled, "World still shows the old composed sprite");
                    var worldPuppet = world.Player.Rect.Find("SkinnedActor");
                    Check(worldPuppet != null && worldPuppet.Find("ContinuousBody").GetComponent<Image>().sprite.name.Contains("FullBodyActorsV3"),
                        "World player must use the new complete full-body asset");
                    var worldRig = worldPuppet.GetComponent<MonoBehaviour>();
                    var worldBody = worldPuppet.Find("ContinuousBody").GetComponent<Image>();
                    var mapPoint = worldBody.GetType().GetMethod("MapPoint");
                    var legBefore = (Vector2)mapPoint.Invoke(worldBody, new object[] { new Vector2(.43f, .12f) });
                    world.Player.Moving = true;
                    world.GetType().GetMethod("StepActor", Flags).Invoke(world, new object[] { world.Player, .05f, true });
                    var legAfter = (Vector2)mapPoint.Invoke(worldBody, new object[] { new Vector2(.43f, .12f) });
                    Check((bool)worldRig.GetType().GetField("moving", Flags).GetValue(worldRig) && Vector2.Distance(legBefore, legAfter) > .001f,
                        "Moving player does not animate the new body");
                    world.GetType().GetMethod("StepActor", Flags).Invoke(world, new object[] { world.Player, .05f, false });
                    var avatar = GameObject.Find("PortraitMask/Portrait");
                    Check(avatar != null && avatar.GetComponent<Image>() != null && avatar.GetComponent<Image>().sprite.name.Contains("V3"),
                        "HUD avatar still uses the old face");
                    Check(world.Player.Rect.sizeDelta.y <= 24f * ProvinceWorld.T * .25f,
                        "World player too large compared with a small town footprint");
                    world.SetZoom(world.MinZoom);
                    var wide = world.VisibleTiles;
                    world.SetZoom(world.MaxZoom);
                    var close = world.VisibleTiles;
                    Check(close.width < wide.width && close.height < wide.height, "Zoom must change the camera footprint");
                    Check(close.Contains(world.Player.Pos), "Following camera lost the player");
                    var region = world.Data.regions[world.Data.regions.Length - 1];
                    var target = new Vector2(region.x + region.w * .5f, region.y + region.h * .5f);
                    world.Teleport(world.Player, target);
                    Call(c, "UpdateMiniPlayer");
                    var bounds = (Rect)typeof(PrototypeBootstrap).GetField("miniTileBounds", Flags).GetValue(c);
                    var dot = (RectTransform)typeof(PrototypeBootstrap).GetField("miniPlayerDot", Flags).GetValue(c);
                    var map = (RectTransform)typeof(PrototypeBootstrap).GetField("miniMapRect", Flags).GetValue(c);
                    Check(bounds.Contains(target), "Minimap did not switch province after teleport");
                    Check(Vector2.Distance(dot.anchorMin, new Vector2((target.x + .5f - bounds.x) / bounds.width,
                        1f - (target.y + .5f - bounds.y) / bounds.height)) < .001f, "Player marker uses the wrong coordinate space");
                    var uv = map.GetComponent<RawImage>().uvRect;
                    var texture = map.GetComponent<RawImage>().texture;
                    Check(texture.width >= world.Data.w * 6 && texture.height >= world.Data.h * 6,
                        "World still magnifies the low resolution overview instead of the terrain bake");
                    Check(Mathf.Abs(texture.width * uv.width / (texture.height * uv.height)
                        - bounds.width / bounds.height) < .001f, "Minimap painting is stretched");
                    renderLog.AppendLine("FullBodyActorsV3 world player, matching ink avatar, human/town scale, zoom, camera, province transition, player marker, UV aspect: ok " + size);
                }, "qcbh-minimap-transition-" + size.x + ".png", size.x, size.y);
            }
            float pveHeight = 0;
            Render(c =>
            {
                PrepareMap(c);
                Call(c, "BuildActionBattle", J.Parse(File.ReadAllText(Path.Combine(samples, "qcbh_battle.json")))["battle"]);
                pveHeight = GameObject.Find("Player").GetComponent<RectTransform>().sizeDelta.y;
                Check(GameObject.Find("Player/SkinnedActor/ContinuousBody").GetComponent<Image>().sprite.name.Contains("FullBodyActorsV3"),
                    "PvE body uses the old character asset");
                var ground = GameObject.Find("Battlefield/Scenery").GetComponent<RawImage>();
                var world = ground.transform.parent.GetComponent<RectTransform>();
                var clamp = typeof(PrototypeBootstrap).Assembly.GetType("IOSVN.TuTien.Core.ActionBattle")
                    .GetMethod("ClampToBattleGround", BindingFlags.Static | BindingFlags.NonPublic);
                var inside = (Vector2)clamp.Invoke(null, new object[] { new Vector2(world.rect.width * .4f, 0f), world.rect.size, pveHeight });
                Check(inside.x > world.rect.width * .35f, "Camera size incorrectly restricts traversal of the battlefield");
                var edge = (Vector2)clamp.Invoke(null, new object[] { new Vector2(100000f, 100000f), world.rect.size, pveHeight });
                Check(edge.x + pveHeight * .35f <= world.rect.width * .5f + .01f
                    && edge.y + pveHeight <= world.rect.height * .5f + .01f, "Fighter leaves the battlefield bounds");
                Check(Mathf.Abs(world.rect.width / world.rect.height
                    - ground.texture.width * ground.uvRect.width / (ground.texture.height * ground.uvRect.height)) < .001f,
                    "PvE ground is distorted");
            }, "qcbh-battle-scale-check.png", 1280, 590);
            Render(c =>
            {
                PrepareMap(c);
                Call(c, "BuildPvpArena", J.Parse(File.ReadAllText(Path.Combine(samples, "qcbh_pvp.json")))["battle"]);
                Check(Mathf.Abs(GameObject.Find("Me").GetComponent<RectTransform>().sizeDelta.y - pveHeight) < .001f,
                    "PvP and PvE character proportions differ");
                renderLog.AppendLine("PvE ground aspect and PvE/PvP human height: ok (" + pveHeight + ")");
            }, "qcbh-pvp-scale-check.png", 1280, 590);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/qcbh-captures/map-review.txt")), renderLog.ToString());
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            Debug.Log("MAP_PRESENTATION_REVIEW_DONE\n" + renderLog);
        }

        public static void ReviewMaps()
        {
            RenderAll();
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            ValidateMapPresentation();
        }

        public static void ReviewReportedErrors()
        {
            RenderAll();
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            ValidateReportedErrors();
        }

        public static void ValidateReportedErrors()
        {
            var samples = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/samples"));
            renderLog = new System.Text.StringBuilder();
            foreach (var female in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 590), new Vector2Int(1024, 768) })
            {
                Render(c =>
                {
                    var hub = J.Parse(File.ReadAllText(Path.Combine(samples, "qcbh_state.json")));
                    hub["player"].Set("look", female ? "g=f;ey=4;br=3;no=2;mo=4" : "g=m;ey=0;br=0;no=0;mo=0");
                    hub["player"].Set("lookWorn", hub["player"]["look"].Str());
                    Set(c, "hub", hub);
                    Set(c, "latestState", NetworkGameClient.ToGameState(hub));
                    Set(c, "offlinePreview", true);
                    Call(c, "ShowWorld");
                    Canvas.ForceUpdateCanvases();
                    var world = GameObject.Find("ProvinceWorld").GetComponent<ProvinceWorld>();
                    var actor = world.Player;
                    void StepWith(Action callback)
                    {
                        actor.Path = new System.Collections.Generic.List<Vector2Int> { Vector2Int.RoundToInt(actor.Pos) };
                        actor.PathIndex = 0;
                        actor.Speed = 100;
                        world.OnPlayerStep = callback;
                        world.GetType().GetMethod("StepActor", Flags).Invoke(world, new object[] { actor, .05f, false });
                    }
                    StepWith(() => actor.Path = null);
                    StepWith(() => Call(c, "ShowCity", hub["town"]["id"].Str()));
                    if (world.gameObject.activeSelf) throw new Exception("Retired world still active after town entry");
                    var city = (GameObject)typeof(PrototypeBootstrap).GetField("cityRoot", Flags).GetValue(c);
                    if (city == null || !city.activeSelf) throw new Exception("Town did not open");
                    Call(c, "OpenCharacterScreen");
                    if (city.activeSelf) throw new Exception("City panorama leaks behind profile");
                    var puppet = GameObject.Find("CultivatorPuppet");
                    if (puppet == null) throw new Exception("Missing repaired portrait");
                    var body = puppet.transform.Find("Body").GetComponent<Image>();
                    if (!body.sprite.name.Contains("V3")) throw new Exception("Profile uses the old faceless asset");
                    foreach (var feature in new[] { "LeftEye", "RightEye", "LeftBrow", "RightBrow", "Nose", "Mouth" })
                        if (puppet.transform.Find(feature).GetComponent<Image>().enabled) throw new Exception("Loose face patch visible: " + feature);
                    renderLog.AppendLine("path cancellation, town transition, city/profile isolation, integrated face: ok " + female + " " + size);
                }, "fixed-profile-" + (female ? "female-" : "male-") + size.x + ".png", size.x, size.y);
            }
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/qcbh-captures/reported-errors-review.txt")), renderLog.ToString());
            if (renderLog.ToString().Contains("FAIL")) throw new Exception(renderLog.ToString());
            Debug.Log("REPORTED_ERRORS_REVIEW_DONE\n" + renderLog);
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
            if (validateFontFamily)
                foreach (var label in canvas.GetComponentsInChildren<Text>())
                    if (label.font == null || !label.font.name.StartsWith("OpenSans"))
                        throw new Exception("Non-Open Sans font in " + filename + ": " + label.name);
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
