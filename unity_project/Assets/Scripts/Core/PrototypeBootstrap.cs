using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QuyCocBatHoang.Patching;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// First mobile vertical slice: email entry, character creation, profile and
    /// server-authoritative world-hunt list. Visual assets can replace these UI
    /// primitives without changing the network flow.
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(13, 18, 27, 255);
        private static readonly Color Panel = new Color32(25, 33, 43, 255);
        private static readonly Color Gold = new Color32(225, 185, 104, 255);
        private static readonly Color Cream = new Color32(239, 228, 203, 255);
        private static readonly Color Muted = new Color32(158, 171, 184, 255);

        private NetworkGameClient client;
        private Canvas canvas;
        private Text status;
        private Text patchStatus;
        private GameObject content;
        private Vector2 statusMin = new Vector2(0.02f, 0.12f);
        private Vector2 statusMax = new Vector2(0.98f, 0.19f);
        private GameCatalog currentCatalog;
        private MapCatalog mapCatalog;
        private GameState latestState;
        private InputField nameInput;
        private InputField emailInput;
        private InputField passwordInput;
        private string[] sectNames;
        private string[] elementNames;
        private string gender = "nam";
        private int sectIndex;
        private int elementIndex;
        private Text genderChoice;
        private Text sectChoice;
        private Text elementChoice;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            client = NetworkGameClient.Instance;
            if (client == null) client = new GameObject("NetworkGameClient").AddComponent<NetworkGameClient>();
            if (GameAudioController.Instance == null) new GameObject("GameAudioController").AddComponent<GameAudioController>();
            BuildCanvas();
            StartStartupPatchCheck();
        }

        private void StartStartupPatchCheck()
        {
            patchStatus = Label("Đang chuẩn bị tài nguyên...", 18, Muted, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.45f), new Vector2(0.96f, 0.55f));
            var patcher = AssetDownloadManager.Instance;
            if (patcher == null) patcher = new GameObject("AssetDownloadManager").AddComponent<AssetDownloadManager>();
            var config = Resources.Load<GameServerConfig>("GameServerConfig");
            patcher.Configure(config?.assetCdnBaseUrl);
            patcher.OnStatusMessage.AddListener(message =>
            {
                if (patchStatus != null) patchStatus.text = message;
            });
            patcher.OnDownloadProgress.AddListener((_, progress) =>
            {
                if (patchStatus != null && !string.IsNullOrEmpty(progress)) patchStatus.text = progress;
            });
            patcher.StartPatchCheck((success, message) =>
            {
                ShowLogin(success ? message : "Không cập nhật được tài nguyên: " + message);
            });
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var background = PanelObject("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Ink);
            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeArea.transform.SetParent(background.transform, false);
            Place(safeArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            content = PanelObject("Content", safeArea.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        }

        private void ShowLogin(string patchMessage = null)
        {
            ClearContent();
            Label("IOSVN  •  TU TIÊN", 36, Gold, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.79f), new Vector2(0.98f, 0.9f));
            Label("Đăng nhập để tiếp tục hành trình", 22, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.79f));

            emailInput = Input("Email", "email", new Vector2(0.06f, 0.61f), new Vector2(0.94f, 0.69f), false);
            passwordInput = Input("Mật khẩu", "password", new Vector2(0.06f, 0.51f), new Vector2(0.94f, 0.59f), true);
            Button("ĐĂNG NHẬP", new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.48f), Gold, () => SubmitAuth(false));
            Button("TẠO TÀI KHOẢN EMAIL", new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.38f), Panel, () => SubmitAuth(true));
            Label("Server game xử lý nhân vật, chiến đấu và vật phẩm.", 18, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.25f));
            ShowStatus(string.IsNullOrEmpty(patchMessage) ? "Kết nối tới máy chủ game IPA." : patchMessage);
        }

        private void SubmitAuth(bool createAccount)
        {
            if (string.IsNullOrWhiteSpace(emailInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                ShowStatus("Nhập email và mật khẩu trước.");
                return;
            }
            ShowStatus(createAccount ? "Đang tạo tài khoản..." : "Đang đăng nhập...");
            Action<ApiResult> finish = result =>
            {
                if (result == null || !result.ok)
                {
                    var detail = result?.error;
                    if (string.IsNullOrWhiteSpace(detail) || detail.Contains("Not found"))
                        detail = "Server game IPA chưa bật API đăng nhập email.";
                    ShowStatus(detail);
                    return;
                }
                LoadState();
            };
            if (createAccount) client.SignUp(emailInput.text.Trim(), passwordInput.text, finish);
            else client.Login(emailInput.text.Trim(), passwordInput.text, finish);
        }

        private void LoadState()
        {
            ShowStatus("Đang tải hồ sơ từ máy chủ...");
            client.LoadState((state, error) =>
            {
                if (state == null) { ShowStatus(error); return; }
                currentCatalog = state.catalog;
                latestState = state;
                SetRealmMusic(state);
                if (!state.registered) ShowCharacterCreation();
                else client.LoadCurrentBattle((battle, _) =>
                {
                    if (battle != null) ShowBattle(battle);
                    else client.LoadPvpBattle((pvpBattle, __) =>
                    {
                        if (pvpBattle != null && !pvpBattle.none && !pvpBattle.over) ShowPvpBattle(pvpBattle);
                        else ShowHome(state);
                    });
                });
            });
        }

        private void ShowCharacterCreation()
        {
            ClearContent();
            Label("KHAI MỞ ĐẠO ĐỒ", 34, Gold, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.84f), new Vector2(0.98f, 0.92f));
            nameInput = Input("Đạo hiệu", "Tên nhân vật", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.80f), false);
            gender = "nam"; sectIndex = 0; elementIndex = 0;
            sectNames = Names(currentCatalog?.mon); elementNames = Names(currentCatalog?.he);
            genderChoice = ChoiceSelector("Giới tính", new[] { "Nam", "Nữ" }, new Vector2(0.06f, 0.61f), new Vector2(0.94f, 0.69f), value => { gender = value == 1 ? "nu" : "nam"; });
            sectChoice = ChoiceSelector("Môn võ học", sectNames, new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.58f), value => sectIndex = value);
            elementChoice = ChoiceSelector("Ngũ hành", elementNames, new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.47f), value => elementIndex = value);
            Button("BẮT ĐẦU TU LUYỆN", new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.34f), Gold, CreateCharacter);
            ShowStatus("Lựa chọn sẽ được lưu trên server.");
        }

        private void CreateCharacter()
        {
            if (currentCatalog?.mon == null || currentCatalog.he == null || currentCatalog.mon.Length == 0 || currentCatalog.he.Length == 0)
            {
                ShowStatus("Server chưa trả danh sách môn phái và ngũ hành.");
                return;
            }
            var choice = new RegisterChoice
            {
                name = nameInput.text.Trim(),
                gender = gender,
                mon = currentCatalog.mon[Mathf.Clamp(sectIndex, 0, currentCatalog.mon.Length - 1)].id,
                he = currentCatalog.he[Mathf.Clamp(elementIndex, 0, currentCatalog.he.Length - 1)].id
            };
            client.RegisterCharacter(choice, (state, error) =>
            {
                if (state == null) { ShowStatus(error); return; }
                ShowHome(state);
            });
        }

        private void ShowHome(GameState state)
        {
            latestState = state;
            SetRealmMusic(state);
            ClearContent();
            Label("TU TIÊN  •  CỬU CHÂU", 28, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            var playerName = string.IsNullOrEmpty(state.player?.fullName) ? state.player?.name : state.player.fullName;
            Label($"{playerName ?? "Đạo hữu"}     •     {state.realm?.name ?? "Sơ nhập"}", 23, Cream, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.80f), new Vector2(0.98f, 0.87f));
            Label($"{state.town?.name ?? "Chưa rõ thành"}     •     {Math.Max(0, state.player?.stones ?? 0):N0} linh thạch", 19, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.75f), new Vector2(0.98f, 0.80f));
            Button(ActiveTitleNames(state.player?.titles), new Vector2(0.02f, 0.715f), new Vector2(0.98f, 0.75f), Panel, () => ShowTitles(state));
            Button("BẢN ĐỒ", new Vector2(0.02f, 0.655f), new Vector2(0.32f, 0.71f), Panel, () => ShowMap(state));
            Button("PVP", new Vector2(0.34f, 0.655f), new Vector2(0.63f, 0.71f), Gold, () => ShowPvp(state));
            Button("LÀM MỚI PVE", new Vector2(0.65f, 0.655f), new Vector2(0.98f, 0.71f), Panel, RefreshMonsters);
            Label("PVE  •  MỤC TIÊU TẠI THÀNH", 20, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.615f), new Vector2(0.98f, 0.65f));
            var scrollContent = CreateScrollList("MonsterList", 0.20f, 0.61f);
            if (state.worldMonsters != null) AddMonsterCards(state.worldMonsters, scrollContent);
            Button("BÍ CẢNH", new Vector2(0.02f, 0.04f), new Vector2(0.48f, 0.105f), Panel, () => ShowPveTown(state, state.town));
            Button("ĐĂNG XUẤT", new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.105f), Panel, () => client.Logout(_ => ShowLogin()));
            ShowStatus("Hồ sơ và mục tiêu đồng bộ với máy chủ game IPA.");
        }

        private void ShowMap(GameState state)
        {
            latestState = state;
            if (mapCatalog == null)
            {
                ShowStatus("Đang tải bản đồ, thành trấn và bí cảnh...");
                client.LoadMapCatalog((catalog, error) =>
                {
                    if (catalog == null) { ShowStatus(error); return; }
                    mapCatalog = catalog;
                    ShowMap(state);
                });
                return;
            }

            ClearContent();
            var here = state.town?.name ?? "Chưa rõ thành";
            Label("BẢN ĐỒ CỬU CHÂU", 29, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            Label($"Đang ở: {here}  •  {state.realm?.name ?? "Sơ nhập"}", 18, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.88f));
            Button("PVE THÀNH NÀY", new Vector2(0.02f, 0.755f), new Vector2(0.48f, 0.81f), Panel, () => ShowPveTown(state, state.town));
            Button("PVP THÀNH NÀY", new Vector2(0.52f, 0.755f), new Vector2(0.98f, 0.81f), Gold, () => ShowPvp(state));
            var rows = CreateScrollList("WorldMap", 0.20f, 0.74f);
            foreach (var map in mapCatalog.maps ?? Array.Empty<MapInfo>())
            {
                var townCount = CountTowns(map.id);
                var header = PanelObject("Map_" + map.id, rows, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(39, 48, 60, 255));
                header.AddComponent<LayoutElement>().preferredHeight = 68;
                var heading = header.AddComponent<HorizontalLayoutGroup>();
                heading.padding = new RectOffset(14, 12, 5, 5); heading.childAlignment = TextAnchor.MiddleLeft; heading.childControlWidth = true; heading.childControlHeight = true; heading.childForceExpandWidth = true; heading.childForceExpandHeight = true;
                var text = ChildText(header.transform, "MapName", 19, Cream, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one);
                text.text = $"{(map.ascensionRequired ? "✨ " : "🌏 ")}{map.name}\n{map.realmMinName ?? ""}  •  {townCount} thành";
                foreach (var town in mapCatalog.towns ?? Array.Empty<TownInfo>())
                {
                    if (town.mapId != map.id) continue;
                    AddTownMapRow(rows, town, map);
                }
            }
            Button("VỀ GAME", new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.105f), Panel, LoadState);
            ShowStatus("19 map • 65 thành • mỗi thành có yêu thú và bí cảnh PvE. PvP ghép người chơi đang ở cùng thành.");
        }

        private void AddTownMapRow(Transform parent, TownInfo town, MapInfo map)
        {
            var current = latestState?.town?.id == town.id;
            var row = PanelObject("Town_" + town.id, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, current ? new Color32(48, 54, 49, 255) : Panel);
            row.AddComponent<LayoutElement>().preferredHeight = 98;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 8, 7, 7); layout.spacing = 6; layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            var labelObject = new GameObject("TownInfo", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            labelObject.transform.SetParent(row.transform, false);
            var label = labelObject.GetComponent<Text>(); label.font = BuiltinFont(); label.fontSize = 16; label.color = Cream; label.alignment = TextAnchor.MiddleLeft; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            var dungeonCount = 0;
            foreach (var dungeon in mapCatalog.dungeons ?? Array.Empty<DungeonInfo>()) if (dungeon.townId == town.id) dungeonCount++;
            label.text = $"{town.icon} {town.name}\n{town.realmMinName ?? ""} · {(town.monsterPool?.Length ?? 0)} yêu thú · {dungeonCount} bí cảnh";
            labelObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            var travel = Button(current ? "Ở đây" : "Đi", Vector2.zero, Vector2.one, current ? Gold : Panel, () => TravelTo(town));
            travel.transform.SetParent(row.transform, false); travel.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;
            var pve = Button("PVE", Vector2.zero, Vector2.one, Panel, () => { if (current) ShowPveTown(latestState, town); else ShowStatus("Hãy ngự kiếm tới thành này để săn yêu thú và vào bí cảnh."); });
            pve.transform.SetParent(row.transform, false); pve.gameObject.AddComponent<LayoutElement>().preferredWidth = 92;
            var pvp = Button("PVP", Vector2.zero, Vector2.one, Panel, () => { if (current) ShowPvp(latestState); else ShowStatus("PvP diễn ra tại cùng thành. Hãy tới thành này trước."); });
            pvp.transform.SetParent(row.transform, false); pvp.gameObject.AddComponent<LayoutElement>().preferredWidth = 92;
        }

        private void TravelTo(TownInfo town)
        {
            if (latestState?.town?.id == town.id) { ShowStatus("Đạo hữu đang ở thành này."); return; }
            if ((latestState?.realm?.index ?? 0) < town.realmMin)
            {
                ShowStatus($"Cần đạt {town.realmMinName ?? RealmLabel(town.realmMin)} mới được tới {town.name}.");
                return;
            }
            ShowStatus("Đang gửi lệnh ngự kiếm lên máy chủ...");
            client.TravelTo(town.id, (result, error) =>
            {
                if (result == null) { ShowStatus(error); return; }
                latestState = result.state;
                var minutes = Math.Max(1, Mathf.CeilToInt(result.travel.durationSec / 60f));
                ShowMap(result.state);
                ShowStatus($"Đang bay tới {result.travel.toTownName}; dự kiến {minutes} phút. PvP/PvE mở khi tới nơi.");
            });
        }

        private void ShowPveTown(GameState state, TownInfo town)
        {
            if (mapCatalog == null)
            {
                ShowStatus("Đang tải danh sách bí cảnh...");
                client.LoadMapCatalog((catalog, error) =>
                {
                    if (catalog == null) { ShowStatus(error); return; }
                    mapCatalog = catalog;
                    ShowPveTown(state, town);
                });
                return;
            }
            if (town == null || state?.town?.id != town.id)
            {
                ShowStatus("Hãy tới đúng thành trấn để tham gia nội dung PvE.");
                return;
            }
            latestState = state;
            ClearContent();
            Label("PVE  •  " + town.name, 27, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            Label($"Săn yêu thú tại thành và thám hiểm bí cảnh · cảnh giới {town.realmMinName ?? ""}", 17, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.88f));
            Button("PVP Ở THÀNH NÀY", new Vector2(0.02f, 0.755f), new Vector2(0.48f, 0.81f), Gold, () => ShowPvp(state));
            Button("BẢN ĐỒ", new Vector2(0.52f, 0.755f), new Vector2(0.98f, 0.81f), Panel, () => ShowMap(state));
            var rows = CreateScrollList("PveList", 0.20f, 0.74f);
            var targets = state.worldMonsters ?? Array.Empty<WorldMonster>();
            if (targets.Length == 0) AddInfoRow(rows, "🌫️ Chưa có yêu thú xuất hiện tại thành này. Làm mới danh sách sau.");
            else AddMonsterCards(targets, rows);
            AddInfoRow(rows, "🏔️ BÍ CẢNH CỦA THÀNH");
            var count = 0;
            foreach (var dungeon in mapCatalog?.dungeons ?? Array.Empty<DungeonInfo>())
            {
                if (dungeon.townId != town.id) continue;
                count++;
                AddDungeonRow(rows, dungeon);
            }
            if (count == 0) AddInfoRow(rows, "Thành này chưa có bí cảnh trong catalog.");
            Button("LÀM MỚI", new Vector2(0.02f, 0.04f), new Vector2(0.48f, 0.105f), Panel, RefreshPve);
            Button("VỀ GAME", new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.105f), Panel, LoadState);
            ShowStatus($"PvE của {town.name}: {town.monsterPool?.Length ?? 0} loài yêu thú và {count} bí cảnh.");
        }

        private void AddDungeonRow(Transform parent, DungeonInfo dungeon)
        {
            var row = PanelObject("Dungeon_" + dungeon.id, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Panel);
            row.AddComponent<LayoutElement>().preferredHeight = 98;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 8, 7, 7); layout.spacing = 8; layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            var textObject = new GameObject("DungeonInfo", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(row.transform, false);
            var text = textObject.GetComponent<Text>(); text.font = BuiltinFont(); text.fontSize = 16; text.color = Cream; text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = $"{dungeon.icon} {dungeon.name}\n{dungeon.stamina} thể lực · yêu cầu {RealmLabel(dungeon.realmMin)}";
            textObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            var enter = Button("VÀO", Vector2.zero, Vector2.one, Gold, () => EnterDungeon(dungeon.id));
            enter.transform.SetParent(row.transform, false); enter.gameObject.AddComponent<LayoutElement>().preferredWidth = 115;
        }

        private void EnterDungeon(string dungeonId)
        {
            ShowStatus("Đang mở bí cảnh PvE...");
            client.EnterDungeon(dungeonId, (result, error) =>
            {
                if (result?.battle == null) { ShowStatus(error); return; }
                ShowBattle(result.battle);
            });
        }

        private void RefreshPve()
        {
            client.LoadState((state, error) =>
            {
                if (state == null) { ShowStatus(error); return; }
                var town = state.town;
                ShowPveTown(state, town);
            });
        }

        private void ShowPvp(GameState state)
        {
            latestState = state;
            ShowStatus("Đang tìm người chơi cùng thành...");
            client.LoadPvpBattle((battle, _) =>
            {
                if (battle != null && !battle.none && !battle.over) { ShowPvpBattle(battle); return; }
                LoadPvpList(state);
            });
        }

        private void LoadPvpList(GameState state)
        {
            client.LoadPvp((pvp, error) =>
            {
                if (pvp == null) { ShowStatus(error); return; }
                ClearContent();
                Label("PVP  •  LÔI ĐÀI THÀNH TRẤN", 27, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
                Label($"{pvp.me?.townName ?? state.town?.name} · {pvp.me?.points ?? 1000} điểm · {pvp.me?.dailyPvpRemaining ?? 0} lượt hôm nay", 18, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.88f));
                Button("PVE THÀNH NÀY", new Vector2(0.02f, 0.755f), new Vector2(0.48f, 0.81f), Panel, () => ShowPveTown(state, state.town));
                Button("LÀM MỚI PVP", new Vector2(0.52f, 0.755f), new Vector2(0.98f, 0.81f), Panel, () => ShowPvp(state));
                var rows = CreateScrollList("PvpList", 0.20f, 0.74f);
                var opponents = pvp.sameTownPlayers ?? Array.Empty<PvpOpponent>();
                if (opponents.Length == 0) AddInfoRow(rows, "Chưa có người chơi PvP đang hoạt động ở thành này. Hãy thử lại sau khi người chơi khác đăng nhập.");
                for (var i = 0; i < opponents.Length; i++) AddPvpRow(rows, opponents[i]);
                AddInfoRow(rows, $"Chiến thư nhận: {pvp.challenges?.received?.Length ?? 0} · đã gửi: {pvp.challenges?.sent?.Length ?? 0}");
                Button("VỀ GAME", new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.105f), Panel, LoadState);
                ShowStatus("PvP dùng nhân vật online đang đứng cùng thành; trận đấu và điểm do server quản lý.");
            });
        }

        private void AddPvpRow(Transform parent, PvpOpponent opponent)
        {
            var row = PanelObject("Opponent_" + opponent.userId, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Panel);
            row.AddComponent<LayoutElement>().preferredHeight = 92;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 8, 7, 7); layout.spacing = 8; layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            var labelObject = new GameObject("OpponentInfo", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            labelObject.transform.SetParent(row.transform, false);
            var label = labelObject.GetComponent<Text>(); label.font = BuiltinFont(); label.fontSize = 16; label.color = Cream; label.alignment = TextAnchor.MiddleLeft; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.text = $"{(opponent.isDemon ? "☯️ " : "⚔️ ")}{opponent.fullName ?? opponent.name}\n{opponent.realmName} · {opponent.power:N0} chiến lực · {opponent.points} điểm";
            labelObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            var fight = Button("GIAO CHIẾN", Vector2.zero, Vector2.one, Gold, () => StartPvp(opponent.userId));
            fight.transform.SetParent(row.transform, false); fight.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
        }

        private void StartPvp(string targetId)
        {
            ShowStatus("Đang mở trận PvP với người chơi này...");
            client.StartPvp(targetId, (result, error) =>
            {
                if (result?.battle == null) { ShowStatus(error); return; }
                ShowPvpBattle(result.battle);
            });
        }

        private void ShowPvpBattle(PvpBattle battle)
        {
            ClearContent();
            Label("PVP  •  GIAO CHIẾN", 29, Gold, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            Label($"{battle.opponent?.name ?? "Đối thủ"}\nHP {Math.Max(0, battle.opponent?.hp ?? 0):N0}/{Math.Max(0, battle.opponent?.maxHp ?? 0):N0}  ·  Chiến lực {battle.opponent?.power ?? 0:N0}", 22, Cream, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.69f), new Vector2(0.96f, 0.85f));
            Label($"{battle.me?.name ?? "Đạo hữu"}\nHP {Math.Max(0, battle.me?.hp ?? 0):N0}/{Math.Max(0, battle.me?.maxHp ?? 0):N0}  ·  MP {Math.Max(0, battle.me?.mp ?? 0):N0}/{Math.Max(0, battle.me?.maxMp ?? 0):N0}", 20, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.59f), new Vector2(0.96f, 0.69f));
            var log = battle.log == null ? "" : string.Join("\n", Array.ConvertAll(battle.log, line => line?.text ?? ""));
            Label(log, 17, Cream, TextAnchor.LowerLeft, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.56f));
            statusMin = new Vector2(0.02f, 0.34f); statusMax = new Vector2(0.98f, 0.37f);
            if (!battle.over)
            {
                Button("TẤN CÔNG", new Vector2(0.05f, 0.24f), new Vector2(0.48f, 0.32f), Gold, () => SendPvpAction(battle, "attack", null));
                Button("NÉ ĐÒN", new Vector2(0.52f, 0.24f), new Vector2(0.95f, 0.32f), Panel, () => SendPvpAction(battle, "dodge", null));
                Button("LÀM MỚI TRẬN", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.22f), Panel, RefreshPvpBattle);
                if (battle.me?.skills != null && battle.me.skills.Length > 0 && battle.me.skills[0].canUse)
                    Button("KỸ NĂNG: " + battle.me.skills[0].name, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.13f), Panel, () => SendPvpAction(battle, "skill", battle.me.skills[0].id));
            }
            else Button("TRỞ VỀ", new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.16f), Gold, LoadState);
            ShowStatus(battle.over ? (battle.isWin ? "Đạo hữu đã thắng trận PvP." : "Trận PvP đã kết thúc.") : "Đòn đánh PvP được gửi trực tiếp tới server.");
        }

        private void SendPvpAction(PvpBattle battle, string action, string skillId)
        {
            GameAudioController.Instance?.PlaySkillEffect();
            client.PvpAct(battle.id, action, skillId, (updated, error) =>
            {
                if (updated == null) { ShowStatus(error); return; }
                ShowPvpBattle(updated);
            });
        }

        private void RefreshPvpBattle() => client.LoadPvpBattle((battle, error) =>
        {
            if (battle == null || battle.none) { ShowStatus(string.IsNullOrEmpty(error) ? "Không còn trận PvP đang diễn ra." : error); return; }
            ShowPvpBattle(battle);
        });

        private int CountTowns(string mapId)
        {
            var count = 0;
            foreach (var town in mapCatalog?.towns ?? Array.Empty<TownInfo>()) if (town.mapId == mapId) count++;
            return count;
        }

        private static string RealmLabel(int realmIndex) => realmIndex <= 0 ? "Phàm Nhân" : "cảnh giới " + realmIndex;

        private static string ActiveTitleNames(PlayerTitle[] titles)
        {
            if (titles == null) return "Chưa có danh hiệu buff";
            var active = new System.Collections.Generic.List<string>();
            foreach (var title in titles) if (title != null && title.active) active.Add(title.name);
            return active.Count == 0 ? "Chưa có danh hiệu buff" : "Danh hiệu: " + string.Join(" · ", active.ToArray());
        }

        private void ShowTitles(GameState state)
        {
            ClearContent();
            Label("DANH HIỆU & BUFF", 29, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            Label("Danh hiệu tự mất cùng buff khi không còn giữ điều kiện.", 17, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.88f));
            var rows = CreateScrollList("TitleList", 0.20f, 0.80f);
            foreach (var title in state.player?.titles ?? Array.Empty<PlayerTitle>())
            {
                var card = PanelObject("Title_" + title.id, rows, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, title.active ? new Color32(57, 49, 31, 255) : Panel);
                card.AddComponent<LayoutElement>().preferredHeight = 176;
                var text = ChildText(card.transform, "Details", 17, title.active ? Gold : Cream, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
                text.text = $"{(title.active ? "✦ ĐANG GIỮ" : "◇ CHƯA ĐẠT")}  {title.name}\nĐiều kiện: {title.requirement}\nDuy trì: {title.maintain}\nBuff: {title.buff}";
            }
            Button("VỀ GAME", new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.105f), Panel, () => ShowHome(state));
            ShowStatus("Danh hiệu và chỉ số buff được máy chủ tính lại khi chơi.");
        }

        private void AddInfoRow(Transform parent, string value)
        {
            var card = PanelObject("Info", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Panel);
            card.AddComponent<LayoutElement>().preferredHeight = 72;
            var label = ChildText(card.transform, "Text", 17, Muted, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            label.text = value;
        }

        private Transform CreateScrollList(string name, float bottom, float top)
        {
            var listRoot = PanelObject(name, content.transform, new Vector2(0.02f, bottom), new Vector2(0.98f, top), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            var viewport = PanelObject("Viewport", listRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.015f));
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = listRoot.AddComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            var scrollContent = PanelObject("Rows", viewport.transform, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            scrollContent.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            scroll.content = scrollContent.GetComponent<RectTransform>();
            var layout = scrollContent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.childControlHeight = false; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            scrollContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return scrollContent.transform;
        }

        private void RefreshMonsters()
        {
            ShowStatus("Đang truy tung mục tiêu...");
            client.LoadWorldMonsters((monsters, error) =>
            {
                if (monsters == null) { ShowStatus(error); return; }
                var root = content.transform.Find("MonsterList/Viewport/Rows");
                if (root == null) return;
                for (var i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
                AddMonsterCards(monsters, root);
                ShowStatus($"Tìm thấy {monsters.Length} mục tiêu.");
            });
        }

        private void AddMonsterCards(WorldMonster[] monsters, Transform parent)
        {
            if (monsters.Length == 0)
            {
                Label("Chưa có mục tiêu đang xuất hiện.", 19, Muted, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f), parent);
                return;
            }
            var max = Mathf.Min(monsters.Length, 12);
            for (var i = 0; i < max; i++)
            {
                var monster = monsters[i];
                var card = PanelObject("Target_" + monster.uid, parent, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, 88), Panel);
                var row = card.AddComponent<HorizontalLayoutGroup>();
                var cardSize = card.AddComponent<LayoutElement>(); cardSize.preferredHeight = 106;
                row.padding = new RectOffset(16, 12, 8, 8); row.spacing = 10;
                row.childAlignment = TextAnchor.MiddleLeft; row.childControlWidth = true; row.childControlHeight = true;
                row.childForceExpandWidth = false; row.childForceExpandHeight = true;
                var labelObject = new GameObject("TargetLabel", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                labelObject.transform.SetParent(card.transform, false);
                var label = labelObject.GetComponent<Text>(); label.font = BuiltinFont(); label.fontSize = 18; label.color = Cream; label.alignment = TextAnchor.MiddleLeft;
                label.text = $"{monster.name}\n{monster.townId}  •  HP {Math.Max(0, monster.hp):N0}";
                labelObject.GetComponent<LayoutElement>().flexibleWidth = 1;
                var hunt = Button("Khiêu chiến", Vector2.zero, Vector2.one, Gold, () => Hunt(monster.uid));
                hunt.transform.SetParent(card.transform, false);
                var buttonLayout = hunt.gameObject.AddComponent<LayoutElement>(); buttonLayout.preferredWidth = 190; buttonLayout.preferredHeight = 54;
            }
        }

        private void Hunt(string uid)
        {
            ShowStatus("Gửi yêu cầu trận đấu lên server...");
            client.StartWorldHunt(uid, (started, error) =>
            {
                if (!started) { ShowStatus(error); return; }
                ShowStatus("Server đã mở trận đấu.");
                client.LoadCurrentBattle((battle, battleError) =>
                {
                    if (battle == null) { ShowStatus(battleError); return; }
                    ShowBattle(battle);
                });
            });
        }

        private void ShowBattle(BattleView battle)
        {
            ClearContent();
            Label("GIAO CHIẾN", 32, Gold, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            var warning = battle.m?.warn != null ? "⚠ Boss sắp tung chiêu lớn!" : "";
            Label($"{battle.m?.icon}  {battle.m?.name ?? "Yêu thú"}\nHP {Math.Max(0, battle.m?.hp ?? 0):N0} / {Math.Max(0, battle.m?.maxHp ?? 0):N0}\n{warning}", 24, Cream, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.84f));
            Label($"{battle.p?.name ?? "Đạo hữu"}\nKhí huyết {Math.Max(0, battle.p?.hp ?? 0):N0} / {Math.Max(0, battle.p?.maxHp ?? 0):N0}     Linh lực {Math.Max(0, battle.p?.mp ?? 0):N0}", 20, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.59f), new Vector2(0.96f, 0.67f));
            var log = battle.log == null ? "" : string.Join("\n", Array.ConvertAll(battle.log, line => line?.text ?? ""));
            Label(log, 17, Cream, TextAnchor.LowerLeft, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.57f));
            statusMin = new Vector2(0.02f, 0.34f); statusMax = new Vector2(0.98f, 0.39f);
            if (!battle.over)
            {
                Button("TẤN CÔNG", new Vector2(0.05f, 0.25f), new Vector2(0.48f, 0.32f), Gold, () => SendBattleAction("attack"));
                Button("NÉ ĐÒN", new Vector2(0.52f, 0.25f), new Vector2(0.95f, 0.32f), Panel, () => SendBattleAction("dodge"));
                var availableSkills = 0;
                foreach (var skill in battle.skills ?? Array.Empty<BattleSkill>())
                {
                    if (skill == null || skill.locked || string.IsNullOrEmpty(skill.id) || availableSkills >= 2) continue;
                    var index = skill.i;
                    var y = availableSkills == 0 ? 0.17f : 0.09f;
                    Button((skill.icon ?? "✨") + " " + skill.name, new Vector2(0.05f, y), new Vector2(0.95f, y + 0.065f), Panel, () => SendBattleSkill(index));
                    availableSkills++;
                }
                Button("RÚT LUI", new Vector2(0.26f, 0.025f), new Vector2(0.74f, 0.085f), Panel, () => SendBattleAction("flee"));
            }
            else if (!string.IsNullOrEmpty(battle.dungeonLeaderId) && (battle.result == "win" || battle.result == "win_down"))
                Button("MỞ ẢI TIẾP THEO", new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.20f), Gold, NextDungeonStage);
            else Button("TRỞ VỀ", new Vector2(0.20f, 0.12f), new Vector2(0.80f, 0.20f), Gold, LoadState);
            ShowStatus(battle.over ? $"Trận đã kết thúc: {battle.result}" : "Thao tác được máy chủ xác nhận.");
        }

        private void SendBattleAction(string action)
        {
            ShowStatus("Đang gửi thao tác chiến đấu...");
            GameAudioController.Instance?.PlaySkillEffect();
            client.BattleAct(action, (result, error) =>
            {
                if (result?.battle == null) { ShowStatus(error); return; }
                ShowBattle(result.battle);
                if (!string.IsNullOrWhiteSpace(result.result?.msg)) ShowStatus(result.result.msg);
            });
        }

        private void SendBattleSkill(int slot)
        {
            ShowStatus("Đang thi triển kỹ năng...");
            GameAudioController.Instance?.PlaySkillEffect();
            client.BattleAct("skill", slot, (result, error) =>
            {
                if (result?.battle == null) { ShowStatus(error); return; }
                ShowBattle(result.battle);
                if (!string.IsNullOrWhiteSpace(result.result?.msg)) ShowStatus(result.result.msg);
            });
        }

        private void NextDungeonStage()
        {
            ShowStatus("Đang mở ải tiếp theo...");
            client.NextDungeonStage((result, error) =>
            {
                if (result?.completed == true)
                {
                    client.LoadState((state, _) => { if (state != null) ShowPveTown(state, state.town); });
                    return;
                }
                if (result?.battle == null) { ShowStatus(error); return; }
                ShowBattle(result.battle);
            });
        }

        private void SetRealmMusic(GameState state)
        {
            var immortal = state.player?.ascended ?? false;
            foreach (var map in state.allMaps ?? Array.Empty<MapInfo>())
                if (map.id == state.town?.mapId) { immortal = map.ascensionRequired; break; }
            GameAudioController.Instance?.SetRealm(immortal);
        }

        private void ShowStatus(string message)
        {
            if (status == null)
                status = Label("", 17, Muted, TextAnchor.MiddleCenter, statusMin, statusMax);
            status.text = string.IsNullOrEmpty(message) ? "Có lỗi kết nối máy chủ." : message;
        }

        private void ClearContent()
        {
            status = null;
            statusMin = new Vector2(0.02f, 0.12f); statusMax = new Vector2(0.98f, 0.19f);
            if (content == null) return;
            for (var i = content.transform.childCount - 1; i >= 0; i--) Destroy(content.transform.GetChild(i).gameObject);
        }

        private Text Label(string value, int size, Color color, TextAnchor alignment, Vector2 min, Vector2 max, Transform parent = null)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent ?? content.transform, false);
            Place(obj.GetComponent<RectTransform>(), min, max);
            var text = obj.GetComponent<Text>();
            text.font = BuiltinFont(); text.fontSize = size; text.color = color; text.alignment = alignment;
            text.text = value; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private InputField Input(string label, string placeholder, Vector2 min, Vector2 max, bool secret)
        {
            var root = PanelObject(label, content.transform, min, max, Vector2.zero, Vector2.zero, Panel);
            var valueText = ChildText(root.transform, "Value", 22, Cream, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            var hint = ChildText(root.transform, "Placeholder", 21, Muted, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            hint.text = placeholder;
            var input = root.AddComponent<InputField>();
            input.textComponent = valueText; input.placeholder = hint; input.contentType = secret ? InputField.ContentType.Password : (label == "email" ? InputField.ContentType.EmailAddress : InputField.ContentType.Standard);
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private Text ChoiceSelector(string label, string[] choices, Vector2 min, Vector2 max, Action<int> selected)
        {
            var root = PanelObject(label, content.transform, min, max, Vector2.zero, Vector2.zero, Panel);
            var button = root.AddComponent<Button>();
            var text = ChildText(root.transform, "Selected", 20, Cream, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            var choiceIndex = 0;
            text.text = choices != null && choices.Length > 0 ? $"{label}:  {choices[0]}   ‹ Chọn ›" : $"{label}: chưa có dữ liệu";
            button.onClick.AddListener(() =>
            {
                if (choices == null || choices.Length == 0) return;
                choiceIndex = (choiceIndex + 1) % choices.Length;
                text.text = $"{label}:  {choices[choiceIndex]}   ‹ Chọn ›";
                selected?.Invoke(choiceIndex);
            });
            return text;
        }

        private Button Button(string label, Vector2 min, Vector2 max, Color color, Action click)
        {
            var root = PanelObject("Button_" + label, content.transform, min, max, Vector2.zero, Vector2.zero, color);
            var button = root.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = color; colors.highlightedColor = new Color(1f, 0.9f, 0.68f); colors.pressedColor = Gold; button.colors = colors;
            var text = ChildText(root.transform, "Text", 20, color == Gold ? Ink : Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.text = label; button.onClick.AddListener(() => click?.Invoke());
            return button;
        }

        private GameObject PanelObject(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax);
            obj.GetComponent<Image>().color = color;
            return obj;
        }

        private Text ChildText(Transform parent, string name, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), min, max);
            var text = obj.GetComponent<Text>(); text.font = BuiltinFont(); text.fontSize = size; text.color = color; text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Font BuiltinFont() => Resources.GetBuiltinResource<Font>("Arial.ttf");
        private static string[] Names(ChoiceInfo[] choices)
        {
            if (choices == null) return Array.Empty<string>();
            var names = new string[choices.Length];
            for (var i = 0; i < choices.Length; i++) names[i] = choices[i]?.name ?? choices[i]?.id ?? "?";
            return names;
        }
        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }
    }

    internal sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;

        private void Awake() => Apply();
        private void Update()
        {
            if (lastSafeArea != Screen.safeArea) Apply();
        }

        private void Apply()
        {
            lastSafeArea = Screen.safeArea;
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            rect.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
