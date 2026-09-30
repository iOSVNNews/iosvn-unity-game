using System;
using System.Collections;
using System.Collections.Generic;
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
        private enum WorldPointKind { Town, Dungeon, MonsterZone }
        private sealed class WorldMapPoint
        {
            public WorldPointKind kind;
            public string title;
            public TownInfo town;
            public DungeonInfo dungeon;
            public Vector2Int cell;
        }

        private static readonly Color Ink = new Color32(13, 18, 27, 255);
        private static readonly Color Panel = new Color32(25, 33, 43, 255);
        private static readonly Color Gold = new Color32(225, 185, 104, 255);
        private static readonly Color Cream = new Color32(239, 228, 203, 255);
        private static readonly Color Muted = new Color32(158, 171, 184, 255);

        private NetworkGameClient client;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private Transform backgroundRoot;
        private GameObject atlasMapRoot;
        private GameObject explorationMapRoot;
        private RectTransform explorationViewport;
        private RectTransform explorationLayer;
        private RectTransform explorationMapRect;
        private RawImage explorationMiniMap;
        private RectTransform explorationMiniPlayer;
        private Image explorationPlayerMarker;
        private Text explorationLocationText;
        private Text explorationPoiText;
        private RectTransform explorationMiniPlayerRect;
        private WorldMapPoint explorationSelectedPoint;
        private MapInfo explorationMap;
        private Texture2D explorationTexture;
        private bool[,] explorationBlocked;
        private readonly List<WorldMapPoint> explorationPoints = new List<WorldMapPoint>();
        private Vector2Int explorationCell;
        private Vector2Int explorationMoveTarget;
        private Coroutine explorationMovement;
        private float explorationZoom = 1f;
        private bool atlasFromExploration;
        private const int ExplorationMapWidth = 144;
        private const int ExplorationMapHeight = 64;
        private const int ExplorationTilePixels = 12;
        private const float ExplorationDefaultZoom = 1.12f;
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
        private InputField verificationCodeInput;
        private string pendingVerificationEmail;
        private bool offlinePreview;
        private GameState offlinePreviewState;
        private bool offlineCreationPreview;
        private string[] sectNames;
        private string[] elementNames;
        private string gender = "nam";
        private int sectIndex;
        private int elementIndex;
        private int appearanceIndex;
        private Text genderChoice;
        private Text sectChoice;
        private Text elementChoice;
        private Text appearanceTitle;
        private Text appearanceDetails;
        private Image characterPortrait;
        private static Sprite[,] characterCreationSprites;
        private readonly HashSet<string> selectedTalents = new HashSet<string>();
        private static readonly string[] CreationTalentIds =
        {
            "dao_the", "kiem_tam", "tu_linh", "son_nhac", "phong_hanh", "than_thuc", "phuong_hoang", "van_thu", "ky_duyen"
        };
        private static readonly string[] CreationTalentNames =
        {
            "Thiên sinh đạo thể", "Kiếm tâm thông minh", "Tụ linh kỳ tài", "Bất động như sơn", "Tật phong bộ", "Thần thức vượt trội", "Phượng hoàng niết bàn", "Vạn thú thân hòa", "Tán tu kỳ duyên"
        };
        private bool atlasRealmInitialized;
        private bool atlasImmortalRealm;
        private bool atlasShowTowns = true;
        private bool atlasShowDungeons = true;
        private bool atlasShowMonsterZones = true;
        private float atlasZoom = 1f;
        private Transform atlasLayer;
        private AtlasMapGesture atlasMapGesture;
        private TownInfo atlasSelectedTown;
        private DungeonInfo atlasSelectedDungeon;
        private int atlasSelectedMonsterField;
        private int atlasSelectedMonsterFieldCount = 1;
        private int atlasSelectedMonsterFieldLabel;
        private string atlasSelectionKind = "town";
        private bool atlasInfoExpanded;
        private int buttonFontSize = 20;
        private Slider atlasZoomSlider;
        private static Sprite atlasTownSprite;
        private static Sprite atlasDungeonSprite;
        private static Sprite atlasMonsterSprite;
        private string pendingBattleVisualAction;
        private string pendingBattleVisualSkillId;
        private string pendingBattleVisualSkillName;
        private string pendingBattleVisualSkillKind;

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
            canvasScaler = scaler;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var background = PanelObject("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Ink);
            backgroundRoot = background.transform;
            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeArea.transform.SetParent(background.transform, false);
            Place(safeArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            content = PanelObject("Content", safeArea.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        }

        private void ShowLogin(string patchMessage = null)
        {
            ClearContent();
            statusMin = new Vector2(0.02f, 0.005f); statusMax = new Vector2(0.98f, 0.035f);
            GameLogo(new Vector2(0.12f, 0.71f), new Vector2(0.88f, 0.96f));
            Label("Đăng nhập để tiếp tục hành trình", 22, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.70f));

            emailInput = Input("email", "Email", new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.60f), false);
            passwordInput = Input("password", "Mật khẩu", new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.50f), true);
            Button("ĐĂNG NHẬP", new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.40f), Gold, () => SubmitAuth(false));
            Button("TẠO TÀI KHOẢN EMAIL", new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.30f), Panel, () => SubmitAuth(true));
            Button("XEM BẢN ĐỒ NGOẠI TUYẾN", new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.21f), Panel, EnterOfflinePreview);
            Button("XEM THỬ TẠO NHÂN VẬT", new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.115f), Panel, EnterOfflineCharacterCreationPreview);
            ShowStatus(string.IsNullOrEmpty(patchMessage) ? "Kết nối tới máy chủ game IPA." : patchMessage);
        }

        private void SubmitAuth(bool createAccount)
        {
            if (string.IsNullOrWhiteSpace(emailInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                ShowStatus("Nhập email và mật khẩu trước.");
                return;
            }
            var email = emailInput.text.Trim();
            ShowStatus(createAccount ? "Đang tạo tài khoản..." : "Đang đăng nhập...");
            Action<ApiResult> finish = result =>
            {
                if (result?.verificationRequired == true)
                {
                    ShowEmailVerification(result.email ?? email, result.message ?? result.error);
                    return;
                }
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
            if (createAccount) client.SignUp(email, passwordInput.text, finish);
            else client.Login(email, passwordInput.text, finish);
        }

        private void ShowEmailVerification(string email, string message = null)
        {
            pendingVerificationEmail = email?.Trim();
            statusMin = new Vector2(0.02f, 0.015f); statusMax = new Vector2(0.98f, 0.075f);
            ClearContent();
            GameLogo(new Vector2(0.12f, 0.75f), new Vector2(0.88f, 0.98f));
            Label("XÁC MINH EMAIL", 27, Gold, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.67f), new Vector2(0.96f, 0.73f));
            Label("Nhập mã 6 số đã gửi tới\n" + pendingVerificationEmail, 18, Muted, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.57f), new Vector2(0.94f, 0.66f));
            verificationCodeInput = Input("Mã xác minh", "6 chữ số", new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.53f), false);
            verificationCodeInput.contentType = InputField.ContentType.IntegerNumber;
            verificationCodeInput.characterLimit = 6;
            verificationCodeInput.keyboardType = TouchScreenKeyboardType.NumberPad;
            Button("XÁC MINH VÀO GAME", new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.42f), Gold, SubmitEmailVerification);
            Button("GỬI LẠI MÃ", new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.31f), Panel, ResendEmailVerification);
            Button("QUAY LẠI ĐĂNG NHẬP", new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.20f), Panel, () => ShowLogin());
            ShowStatus(string.IsNullOrWhiteSpace(message) ? "Mã có hiệu lực trong 10 phút." : message);
        }

        private void SubmitEmailVerification()
        {
            if (string.IsNullOrWhiteSpace(pendingVerificationEmail) || string.IsNullOrWhiteSpace(verificationCodeInput?.text))
            {
                ShowStatus("Nhập mã 6 số trong email trước.");
                return;
            }
            ShowStatus("Đang xác minh email...");
            client.VerifyEmail(pendingVerificationEmail, verificationCodeInput.text.Trim(), result =>
            {
                if (result == null || !result.ok) { ShowStatus(result?.error ?? "Không xác minh được email."); return; }
                pendingVerificationEmail = null;
                LoadState();
            });
        }

        private void ResendEmailVerification()
        {
            if (string.IsNullOrWhiteSpace(pendingVerificationEmail)) { ShowLogin(); return; }
            ShowStatus("Đang gửi lại mã xác minh...");
            client.ResendEmailVerification(pendingVerificationEmail, result =>
            {
                ShowStatus(result?.message ?? result?.error ?? "Đã yêu cầu gửi lại mã.");
            });
        }

        private void EnterOfflinePreview()
        {
            var catalogAsset = Resources.Load<TextAsset>("MapCatalog");
            if (catalogAsset == null)
            {
                ShowStatus("Thiếu danh mục bản đồ ngoại tuyến trong bản cài.");
                return;
            }
            mapCatalog = JsonUtility.FromJson<MapCatalog>(catalogAsset.text);
            var town = FirstTownInAtlas(false);
            if (mapCatalog?.maps == null || mapCatalog.maps.Length == 0 || town == null)
            {
                ShowStatus("Danh mục bản đồ ngoại tuyến không hợp lệ.");
                return;
            }
            offlinePreview = true;
            offlinePreviewState = new GameState
            {
                registered = true,
                town = town,
                realm = new RealmInfo { index = 0, name = town.realmMinName ?? "Phàm Nhân" },
                player = new PlayerInfo { userId = "offline-preview", name = "Đạo hữu", fullName = "Đạo hữu · Ngoại tuyến", ascended = false },
                worldMonsters = Array.Empty<WorldMonster>()
            };
            latestState = offlinePreviewState;
            atlasRealmInitialized = true;
            atlasImmortalRealm = false;
            atlasSelectedTown = town;
            atlasSelectedDungeon = null;
            atlasSelectionKind = "town";
            atlasInfoExpanded = false;
            SetRealmMusic(offlinePreviewState);
            SetAtlasOrientation(true);
            ShowMap(offlinePreviewState);
            ShowStatus("Đang xem bản đồ offline. Đăng nhập, di chuyển và chiến đấu cần máy chủ online.");
        }

        private void ReturnFromWorldAtlas(GameState state)
        {
            if (atlasFromExploration)
            {
                atlasFromExploration = false;
                RenderExplorationMap(state);
                return;
            }
            if (!offlinePreview) { LoadState(); return; }
            offlinePreview = false;
            offlinePreviewState = null;
            pendingVerificationEmail = null;
            SetAtlasOrientation(false);
            ShowLogin("Bản xem ngoại tuyến chỉ để duyệt bản đồ.");
        }

        private void LoadState()
        {
            SetAtlasOrientation(false);
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
            SetAtlasOrientation(true);
            offlineCreationPreview = false;
            ShowCharacterCreationForm(resetSelection: true);
        }

        private void EnterOfflineCharacterCreationPreview()
        {
            var catalogAsset = Resources.Load<TextAsset>("CharacterCatalog");
            if (catalogAsset == null)
            {
                ShowStatus("Thiếu danh mục nhân vật ngoại tuyến trong bản cài.");
                return;
            }
            currentCatalog = JsonUtility.FromJson<GameCatalog>(catalogAsset.text);
            if (currentCatalog?.mon == null || currentCatalog.he == null || currentCatalog.mon.Length == 0 || currentCatalog.he.Length == 0)
            {
                ShowStatus("Danh mục nhân vật ngoại tuyến không hợp lệ.");
                return;
            }
            offlineCreationPreview = true;
            SetAtlasOrientation(true);
            ShowCharacterCreationForm(resetSelection: true);
            ShowStatus("Bản thử ngoại tuyến: lựa chọn chỉ lưu trên thiết bị.");
        }

        private void ShowCharacterCreationForm(bool resetSelection)
        {
            var savedName = resetSelection ? "" : nameInput?.text;
            var savedSectIndex = resetSelection ? 0 : sectIndex;
            var savedElementIndex = resetSelection ? 0 : elementIndex;
            if (resetSelection)
            {
                gender = "nam";
                appearanceIndex = 0;
                selectedTalents.Clear();
            }
            ClearContent();
            statusMin = new Vector2(0.02f, 0.002f); statusMax = new Vector2(0.98f, 0.035f);
            PanelObject("CreationScroll", content.transform, new Vector2(0.005f, 0.035f), new Vector2(0.995f, 0.97f), Vector2.zero, Vector2.zero, new Color32(90, 68, 43, 255));
            PanelObject("CreationParchment", content.transform, new Vector2(0.012f, 0.045f), new Vector2(0.988f, 0.96f), Vector2.zero, Vector2.zero, new Color32(220, 207, 178, 255));
            Label(offlineCreationPreview ? "KHAI MỞ ĐẠO ĐỒ  ·  BẢN THỬ OFFLINE" : "KHAI MỞ ĐẠO ĐỒ", 27, new Color32(77, 52, 28, 255), TextAnchor.MiddleCenter, new Vector2(0.02f, 0.89f), new Vector2(0.98f, 0.96f));
            PanelObject("PortraitFrame", content.transform, new Vector2(0.025f, 0.33f), new Vector2(0.275f, 0.87f), Vector2.zero, Vector2.zero, new Color32(82, 66, 49, 255));
            var portrait = PanelObject("CharacterPortrait", content.transform, new Vector2(0.034f, 0.36f), new Vector2(0.266f, 0.84f), Vector2.zero, Vector2.zero, Cream);
            characterPortrait = portrait.GetComponent<Image>();
            characterPortrait.preserveAspect = true;
            Label("ĐẠO ĐỒ CỦA BẠN", 16, new Color32(77, 52, 28, 255), TextAnchor.MiddleCenter, new Vector2(0.035f, 0.325f), new Vector2(0.265f, 0.36f));

            Label("ĐẠO HIỆU", 15, new Color32(77, 52, 28, 255), TextAnchor.MiddleLeft, new Vector2(0.30f, 0.82f), new Vector2(0.63f, 0.87f));
            nameInput = Input("Đạo hiệu", "Tên nhân vật (2–24 ký tự)", new Vector2(0.30f, 0.75f), new Vector2(0.63f, 0.82f), false);
            nameInput.characterLimit = 24;
            nameInput.text = savedName ?? "";
            Label("GIỚI TÍNH", 15, new Color32(77, 52, 28, 255), TextAnchor.MiddleLeft, new Vector2(0.30f, 0.70f), new Vector2(0.63f, 0.75f));
            Button("NAM", new Vector2(0.30f, 0.63f), new Vector2(0.46f, 0.70f), gender == "nam" ? Gold : Panel, () => SetCreationGender("nam"));
            Button("NỮ", new Vector2(0.47f, 0.63f), new Vector2(0.63f, 0.70f), gender == "nu" ? Gold : Panel, () => SetCreationGender("nu"));
            Label("TÓC · TRANG PHỤC · MÀU MẮT", 15, new Color32(77, 52, 28, 255), TextAnchor.MiddleLeft, new Vector2(0.30f, 0.58f), new Vector2(0.63f, 0.63f));
            Button("‹  ĐỔI DIỆN MẠO  ›", new Vector2(0.30f, 0.51f), new Vector2(0.63f, 0.58f), Panel, CycleAppearance);
            appearanceTitle = Label("", 17, new Color32(77, 52, 28, 255), TextAnchor.MiddleLeft, new Vector2(0.30f, 0.46f), new Vector2(0.63f, 0.51f));
            appearanceDetails = Label("", 14, new Color32(94, 78, 58, 255), TextAnchor.MiddleLeft, new Vector2(0.30f, 0.415f), new Vector2(0.63f, 0.46f));

            sectNames = Names(currentCatalog?.mon); elementNames = Names(currentCatalog?.he);
            sectIndex = Mathf.Clamp(savedSectIndex, 0, Math.Max(0, sectNames.Length - 1));
            elementIndex = Mathf.Clamp(savedElementIndex, 0, Math.Max(0, elementNames.Length - 1));
            sectChoice = ChoiceSelector("Môn phái", sectNames, new Vector2(0.30f, 0.335f), new Vector2(0.63f, 0.405f), value => sectIndex = value, sectIndex);
            elementChoice = ChoiceSelector("Ngũ hành", elementNames, new Vector2(0.30f, 0.255f), new Vector2(0.63f, 0.325f), value => elementIndex = value, elementIndex);

            PanelObject("TalentPanel", content.transform, new Vector2(0.65f, 0.32f), new Vector2(0.975f, 0.87f), Vector2.zero, Vector2.zero, new Color32(51, 48, 43, 255));
            Label("TIÊN THIÊN KHÍ VẬN", 18, Gold, TextAnchor.MiddleCenter, new Vector2(0.66f, 0.80f), new Vector2(0.965f, 0.86f));
            Label("Chọn đúng 3 khí vận", 14, Cream, TextAnchor.MiddleCenter, new Vector2(0.66f, 0.76f), new Vector2(0.965f, 0.80f));
            if (resetSelection) selectedTalents.Clear();
            for (var i = 0; i < CreationTalentIds.Length; i++)
            {
                var column = i % 2;
                var row = i / 2;
                var x0 = column == 0 ? 0.665f : 0.815f;
                var x1 = column == 0 ? 0.810f : 0.960f;
                var y1 = 0.75f - row * 0.083f;
                var y0 = y1 - 0.074f;
                AddCreationTalentButton(i, new Vector2(x0, y0), new Vector2(x1, y1));
            }

            PanelObject("StartingStats", content.transform, new Vector2(0.025f, 0.12f), new Vector2(0.975f, 0.29f), Vector2.zero, Vector2.zero, new Color32(55, 49, 41, 255));
            Label("CHỈ SỐ CĂN BẢN  ·  LINH CĂN SẼ ĐƯỢC XÁC ĐỊNH KHI VÀO GAME", 15, Gold, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.245f), new Vector2(0.96f, 0.285f));
            var stats = new[] { "THỂ CHẤT   500", "LINH LỰC   200", "CÔNG KÍCH   50", "PHÒNG NGỰ   30", "TỐC ĐỘ   10", "THẦN THỨC   10" };
            for (var i = 0; i < stats.Length; i++)
            {
                var x0 = 0.035f + i * 0.156f;
                Label(stats[i], 15, Cream, TextAnchor.MiddleCenter, new Vector2(x0, 0.175f), new Vector2(x0 + 0.15f, 0.235f));
            }
            Label("Phàm Nhân  ·  30.000 linh thạch  ·  3 kỹ năng và trang bị nhập môn", 14, Cream, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.125f), new Vector2(0.96f, 0.17f));
            Button("QUAY LẠI", new Vector2(0.025f, 0.045f), new Vector2(0.20f, 0.11f), Panel, BackFromCharacterCreation);
            Button(offlineCreationPreview ? "LƯU BẢN DEMO OFFLINE" : "BẮT ĐẦU TU LUYỆN",
                new Vector2(0.70f, 0.045f), new Vector2(0.975f, 0.11f), Gold, CreateCharacter);
            RefreshCharacterPreview();
            ShowStatus(offlineCreationPreview ? "Bản demo chỉ lưu trên thiết bị; vào game đầy đủ cần máy chủ online." : "Thông tin nhân vật và 3 khí vận sẽ được lưu trên máy chủ.");
        }

        private void CreateCharacter()
        {
            if (currentCatalog?.mon == null || currentCatalog.he == null || currentCatalog.mon.Length == 0 || currentCatalog.he.Length == 0)
            {
                ShowStatus("Server chưa trả danh sách môn phái và ngũ hành.");
                return;
            }
            if (string.IsNullOrWhiteSpace(nameInput?.text) || nameInput.text.Trim().Length < 2)
            {
                ShowStatus("Đạo hiệu cần có ít nhất 2 ký tự.");
                return;
            }
            if (selectedTalents.Count != 3)
            {
                ShowStatus("Hãy chọn đúng 3 tiên thiên khí vận.");
                return;
            }
            var choice = new RegisterChoice
            {
                name = nameInput.text.Trim(),
                gender = gender,
                mon = currentCatalog.mon[Mathf.Clamp(sectIndex, 0, currentCatalog.mon.Length - 1)].id,
                he = currentCatalog.he[Mathf.Clamp(elementIndex, 0, currentCatalog.he.Length - 1)].id,
                appearance = AppearanceId(),
                talents = new List<string>(selectedTalents).ToArray()
            };
            if (offlineCreationPreview)
            {
                PlayerPrefs.SetString("tutien_offline_character_demo", JsonUtility.ToJson(choice));
                PlayerPrefs.Save();
                ShowStatus($"Đã lưu bản demo ngoại tuyến cho {choice.name}. Dữ liệu này chưa đồng bộ lên server.");
                return;
            }
            ShowStatus("Đang tạo nhân vật trên máy chủ...");
            client.RegisterCharacter(choice, (state, error) =>
            {
                if (state == null) { ShowStatus(error); return; }
                offlineCreationPreview = false;
                SetAtlasOrientation(false);
                ShowHome(state);
            });
        }

        private void BackFromCharacterCreation()
        {
            if (offlineCreationPreview)
            {
                offlineCreationPreview = false;
                SetAtlasOrientation(false);
                ShowLogin();
                return;
            }
            client.Logout(_ =>
            {
                SetAtlasOrientation(false);
                ShowLogin("Đã quay lại màn hình đăng nhập.");
            });
        }

        private void SetCreationGender(string value)
        {
            gender = value == "nu" ? "nu" : "nam";
            ShowCharacterCreationForm(resetSelection: false);
        }

        private void CycleAppearance()
        {
            appearanceIndex = (appearanceIndex + 1) % 2;
            RefreshCharacterPreview();
        }

        private string AppearanceId()
        {
            if (gender == "nu") return appearanceIndex == 0 ? "thanh_ngoc" : "xich_lien";
            return appearanceIndex == 0 ? "thanh_ngoc" : "bach_van";
        }

        private void RefreshCharacterPreview()
        {
            if (characterPortrait == null) return;
            EnsureCharacterCreationSprites();
            var row = gender == "nu" ? 1 : 0;
            characterPortrait.sprite = characterCreationSprites[appearanceIndex, row];
            var selected = appearanceIndex == 0
                ? new[] { "Tóc đen", "Áo xanh ngọc", "Mắt lục" }
                : gender == "nu" ? new[] { "Tóc bạc", "Áo xích hắc", "Mắt hổ phách" } : new[] { "Tóc nâu", "Áo bạch lam", "Mắt lam" };
            if (appearanceTitle != null) appearanceTitle.text = selected[0] + "  ·  " + selected[1];
            if (appearanceDetails != null) appearanceDetails.text = selected[2] + "  ·  Diện mạo " + (appearanceIndex + 1);
        }

        private static void EnsureCharacterCreationSprites()
        {
            if (characterCreationSprites != null) return;
            characterCreationSprites = new Sprite[2, 2];
            var texture = Resources.Load<Texture2D>("Characters/CharacterCreationAtlas");
            if (texture == null) { characterCreationSprites = null; return; }
            var width = texture.width / 2;
            var height = texture.height / 2;
            for (var row = 0; row < 2; row++)
            for (var column = 0; column < 2; column++)
            {
                var y = row == 0 ? height : 0;
                characterCreationSprites[column, row] = Sprite.Create(texture, new Rect(column * width, y, width, height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        private void AddCreationTalentButton(int index, Vector2 min, Vector2 max)
        {
            var id = CreationTalentIds[index];
            var selected = selectedTalents.Contains(id);
            var color = selected ? Gold : new Color(84f / 255f, 74f / 255f, 59f / 255f, 1f);
            var button = Button((selected ? "✓ " : "") + CreationTalentNames[index], min, max, color, () =>
            {
                if (selectedTalents.Contains(id)) selectedTalents.Remove(id);
                else if (selectedTalents.Count >= 3) { ShowStatus("Chỉ chọn tối đa 3 khí vận."); return; }
                else selectedTalents.Add(id);
                ShowCharacterCreationForm(resetSelection: false);
            });
            var label = button.GetComponentInChildren<Text>();
            if (label != null) { label.fontSize = 13; label.color = selected ? Ink : Cream; }
        }

        private void ShowHome(GameState state)
        {
            latestState = state;
            atlasImmortalRealm = IsImmortalRealm(state);
            atlasRealmInitialized = true;
            atlasSelectedTown = state.town;
            atlasSelectedDungeon = null;
            atlasSelectionKind = "town";
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
            SetAtlasOrientation(true);
            atlasFromExploration = false;
            RenderExplorationMap(state);
        }

        private void OpenWorldAtlas(GameState state)
        {
            StopExplorationMovement(savePosition: true);
            if (!atlasRealmInitialized)
            {
                atlasImmortalRealm = IsImmortalRealm(state);
                atlasRealmInitialized = true;
            }
            if (explorationMapRoot != null) Destroy(explorationMapRoot);
            explorationMapRoot = null;
            if (explorationTexture != null) Destroy(explorationTexture);
            explorationTexture = null;
            explorationLayer = null;
            explorationViewport = null;
            if (atlasSelectedTown == null || !IsTownInAtlas(atlasSelectedTown, atlasImmortalRealm))
            {
                atlasSelectedTown = IsTownInAtlas(state.town, atlasImmortalRealm) ? state.town : FirstTownInAtlas(atlasImmortalRealm);
                atlasSelectedDungeon = null;
                atlasSelectionKind = "town";
            }
            atlasFromExploration = true;
            RenderWorldAtlas(state);
        }

        private void RenderExplorationMap(GameState state)
        {
            latestState = state;
            StopExplorationMovement(savePosition: true);
            if (atlasMapRoot != null) { Destroy(atlasMapRoot); atlasMapRoot = null; atlasLayer = null; }
            if (explorationMapRoot != null) Destroy(explorationMapRoot);
            if (explorationTexture != null) Destroy(explorationTexture);
            explorationPoints.Clear();
            explorationSelectedPoint = null;
            explorationMiniPlayerRect = null;
            explorationPlayerMarker = null;
            explorationMovement = null;
            ClearContent();
            statusMin = new Vector2(.36f, .005f); statusMax = new Vector2(.64f, .042f);
            explorationZoom = ExplorationDefaultZoom;

            explorationMap = FindMap(state?.town?.mapId);
            if (explorationMap == null)
                foreach (var candidate in mapCatalog?.maps ?? Array.Empty<MapInfo>())
                    if (candidate != null && candidate.ascensionRequired == (state?.player?.ascended ?? false)) { explorationMap = candidate; break; }
            if (explorationMap == null)
            {
                ShowStatus("Không tìm thấy châu hiện tại trong danh mục bản đồ.");
                return;
            }

            explorationMapRoot = PanelObject("ExplorationMapRoot", backgroundRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            explorationMapRoot.transform.SetAsFirstSibling();
            explorationMapRoot.GetComponent<Image>().raycastTarget = false;
            var viewportObject = PanelObject("ExplorationViewport", explorationMapRoot.transform,
                new Vector2(0f, .095f), new Vector2(1f, .915f), Vector2.zero, Vector2.zero, new Color32(20, 24, 27, 255));
            explorationViewport = viewportObject.GetComponent<RectTransform>();
            viewportObject.AddComponent<RectMask2D>();
            var touch = viewportObject.AddComponent<ExplorationMapTouch>();
            touch.Initialize(HandleExplorationTap, PanExplorationMap);
            var mapObject = new GameObject("PixelProvinceMap", typeof(RectTransform), typeof(RawImage));
            mapObject.transform.SetParent(viewportObject.transform, false);
            explorationMapRect = mapObject.GetComponent<RectTransform>();
            explorationMapRect.anchorMin = explorationMapRect.anchorMax = new Vector2(.5f, .5f);
            explorationMapRect.pivot = new Vector2(.5f, .5f);
            explorationMapRect.sizeDelta = new Vector2(ExplorationMapWidth * ExplorationTilePixels, ExplorationMapHeight * ExplorationTilePixels);
            explorationMapRect.localScale = Vector3.one * explorationZoom;
            explorationMapRect.anchoredPosition = Vector2.zero;
            var mapImage = mapObject.GetComponent<RawImage>();
            mapImage.texture = BuildExplorationTexture(explorationMap);
            mapImage.raycastTarget = false;
            BuildExplorationPoints(state);
            foreach (var point in explorationPoints) AddExplorationMarker(point);

            var top = PanelObject("ExplorationTopBar", content.transform, new Vector2(.008f, .915f), new Vector2(.992f, .99f), Vector2.zero, Vector2.zero, new Color32(15, 20, 27, 230));
            Button("‹ THIÊN HẠ", new Vector2(.008f, .06f), new Vector2(.13f, .94f), Panel, () => OpenWorldAtlas(state), top.transform);
            var mapName = string.IsNullOrWhiteSpace(explorationMap.provinceName) ? explorationMap.name : explorationMap.provinceName;
            Label($"{(explorationMap.ascensionRequired ? "TIÊN GIỚI" : "PHÀM GIỚI")}  ·  {mapName}", 23, Gold, TextAnchor.MiddleLeft,
                new Vector2(.15f, .08f), new Vector2(.56f, .92f), top.transform);
            explorationLocationText = Label("", 17, Cream, TextAnchor.MiddleRight, new Vector2(.57f, .08f), new Vector2(.83f, .92f), top.transform);
            Button("VỀ GAME", new Vector2(.85f, .06f), new Vector2(.992f, .94f), Panel,
                () => { if (offlinePreview) ShowStatus("Đang ở chế độ xem bản đồ offline."); else LoadState(); }, top.transform);

            var miniFrame = PanelObject("MinimapFrame", content.transform, new Vector2(.80f, .755f), new Vector2(.98f, .89f), Vector2.zero, Vector2.zero, new Color32(17, 22, 28, 235));
            var miniObject = new GameObject("Minimap", typeof(RectTransform), typeof(RawImage));
            miniObject.transform.SetParent(miniFrame.transform, false);
            Place(miniObject.GetComponent<RectTransform>(), new Vector2(.035f, .08f), new Vector2(.965f, .92f));
            explorationMiniMap = miniObject.GetComponent<RawImage>(); explorationMiniMap.texture = explorationTexture; explorationMiniMap.raycastTarget = false;
            var miniPlayer = new GameObject("MinimapPlayer", typeof(RectTransform), typeof(Image));
            miniPlayer.transform.SetParent(miniFrame.transform, false);
            explorationMiniPlayerRect = miniPlayer.GetComponent<RectTransform>();
            explorationMiniPlayerRect.anchorMin = explorationMiniPlayerRect.anchorMax = new Vector2(.5f, .5f);
            explorationMiniPlayerRect.sizeDelta = new Vector2(8, 8);
            miniPlayer.GetComponent<Image>().color = new Color32(255, 84, 68, 255);

            var poiPanel = PanelObject("ExplorationPoiPanel", content.transform, new Vector2(.015f, .755f), new Vector2(.77f, .89f), Vector2.zero, Vector2.zero, new Color32(15, 20, 27, 220));
            explorationPoiText = ChildText(poiPanel.transform, "SelectedPoint", 17, Cream, TextAnchor.MiddleLeft, new Vector2(.035f, .06f), new Vector2(.97f, .94f));
            explorationPoiText.text = "Chạm một ô trên bản đồ để nhân vật đi tới; chạm biểu tượng để xem thành trấn, cổ động hoặc bãi yêu.";

            Button("↑", new Vector2(.075f, .135f), new Vector2(.125f, .195f), Panel, () => MoveExplorationBy(Vector2Int.up));
            Button("←", new Vector2(.025f, .075f), new Vector2(.075f, .135f), Panel, () => MoveExplorationBy(Vector2Int.left));
            Button("↓", new Vector2(.075f, .075f), new Vector2(.125f, .135f), Panel, () => MoveExplorationBy(Vector2Int.down));
            Button("→", new Vector2(.125f, .075f), new Vector2(.175f, .135f), Panel, () => MoveExplorationBy(Vector2Int.right));
            Button("THÀNH / ĐIỂM ĐẾN", new Vector2(.77f, .075f), new Vector2(.98f, .145f), Gold, () => ActivateSelectedExplorationPoint(state));
            Button("−", new Vector2(.38f, .075f), new Vector2(.425f, .13f), Panel, () => SetExplorationZoom(explorationZoom - .15f));
            var zoomTrack = PanelObject("ExplorationZoomTrack", content.transform, new Vector2(.435f, .091f), new Vector2(.625f, .112f), Vector2.zero, Vector2.zero, new Color32(38, 42, 47, 245));
            var zoomHandle = PanelObject("ExplorationZoomHandle", zoomTrack.transform, new Vector2(0f, -1f), new Vector2(.12f, 2f), Vector2.zero, Vector2.zero, Gold);
            var zoomSlider = zoomTrack.AddComponent<Slider>(); zoomSlider.minValue = 1f; zoomSlider.maxValue = 2f; zoomSlider.value = explorationZoom;
            zoomSlider.direction = Slider.Direction.LeftToRight; zoomSlider.targetGraphic = zoomHandle.GetComponent<Image>(); zoomSlider.handleRect = zoomHandle.GetComponent<RectTransform>();
            zoomSlider.onValueChanged.AddListener(SetExplorationZoom);
            Button("+", new Vector2(.635f, .075f), new Vector2(.68f, .13f), Panel, () => SetExplorationZoom(explorationZoom + .15f));

            var start = FindPointForTown(state?.town?.id);
            explorationCell = start != null ? start.cell : new Vector2Int(ExplorationMapWidth / 2, ExplorationMapHeight / 2);
            if (!offlinePreview && state?.player?.worldPosition != null && state.player.worldPosition.mapId == explorationMap.id)
                explorationCell = new Vector2Int(Mathf.Clamp(state.player.worldPosition.x, 2, ExplorationMapWidth - 3), Mathf.Clamp(state.player.worldPosition.y, 2, ExplorationMapHeight - 3));
            else
            {
                var saved = PlayerPrefs.GetString(ExplorationSaveKey(state, explorationMap), "");
                var values = saved.Split(',');
                if (values.Length == 2 && int.TryParse(values[0], out var savedX) && int.TryParse(values[1], out var savedY))
                    explorationCell = new Vector2Int(Mathf.Clamp(savedX, 2, ExplorationMapWidth - 3), Mathf.Clamp(savedY, 2, ExplorationMapHeight - 3));
            }
            explorationSelectedPoint = start;
            explorationPlayerMarker = AddExplorationPlayerMarker(state);
            UpdateExplorationPlayerPosition();
            UpdateExplorationLabels();
            ShowStatus(offlinePreview ? "Bản đồ đi lại ngoại tuyến · vị trí được lưu trên máy." : "Bản đồ đi lại · vị trí nhân vật được đồng bộ với hồ sơ online.");
        }

        private Texture2D BuildExplorationTexture(MapInfo map)
        {
            const int w = ExplorationMapWidth, h = ExplorationMapHeight, tile = ExplorationTilePixels;
            var kinds = new byte[w, h];
            explorationBlocked = new bool[w, h];
            var ordinal = AtlasMapOrdinal(map);
            var seed = ordinal * 9137 + (map.ascensionRequired ? 517 : 31);
            var oldRandom = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            var land = ordinal >= 9 ? new Color32(83, 111, 102, 255) : ordinal == 4 ? new Color32(91, 125, 119, 255) : new Color32(104, 119, 77, 255);
            var mountain = ordinal >= 9 ? new Color32(107, 115, 122, 255) : ordinal == 6 ? new Color32(110, 72, 57, 255) : new Color32(104, 96, 80, 255);
            var water = ordinal == 4 ? new Color32(57, 106, 124, 255) : ordinal >= 9 ? new Color32(71, 124, 139, 255) : new Color32(70, 110, 117, 255);
            var shore = new Color32(162, 147, 105, 255);
            var mountainNoise = new float[w, h]; var forestNoise = new float[w, h];
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                var noise = Mathf.PerlinNoise((x + seed) * .083f, (y - seed) * .079f);
                var ridge = Mathf.PerlinNoise((x + seed) * .044f, (y + seed) * .13f);
                mountainNoise[x, y] = noise * .62f + ridge * .38f;
                forestNoise[x, y] = Mathf.PerlinNoise((x - seed) * .15f, (y + seed) * .16f);
                var edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                var rim = edge < 2 || (edge == 2 && noise > .32f);
                var longRidge = Mathf.Abs(y - (int)(h * .62f + Mathf.Sin((x + seed) * .08f) * 6f)) <= 1 && x > 10 && x < w - 10 && ridge > .52f;
                var innerRidge = Mathf.Abs((int)(y - (h * .28f + x * .24f + Mathf.Sin(x * .12f) * 4f))) <= 1 && noise > .73f && x > 15 && x < w - 12;
                if (rim || longRidge || innerRidge || (noise > .82f && x > 5 && x < w - 5 && y > 5 && y < h - 5)) { kinds[x, y] = 2; explorationBlocked[x, y] = true; }
                else if ((ordinal == 4 || ordinal == 10 || ordinal == 17) && (Mathf.Abs(x - (w * .49f + Mathf.Sin(y * .11f) * 8f)) < 1.6f || (noise > .87f && forestNoise[x, y] > .5f))) { kinds[x, y] = 3; explorationBlocked[x, y] = true; }
                else if (forestNoise[x, y] > .70f && noise < .69f) kinds[x, y] = 1;
                else kinds[x, y] = noise < .19f ? (byte)5 : (byte)0;
            }

            BuildExplorationPointsData(map);
            var startTown = explorationMap != null ? FindPointForTown(latestState?.town?.id) : null;
            var start = startTown?.cell ?? new Vector2Int(w / 2, h / 2);
            foreach (var point in explorationPoints)
            {
                CarveExplorationRoad(kinds, start, point.cell);
                explorationBlocked[point.cell.x, point.cell.y] = false;
            }

            var pixels = new Color32[w * tile * h * tile];
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                var k = kinds[x, y];
                var variation = .86f + mountainNoise[x, y] * .27f;
                var baseColor = k == 1 ? Color32.Lerp(land, new Color32(36, 70, 53, 255), .45f) :
                    k == 2 ? Color32.Lerp(mountain, new Color32(169, 159, 139, 255), mountainNoise[x, y] > .67f ? .56f : .1f) :
                    k == 3 ? Color32.Lerp(water, new Color32(119, 159, 161, 255), mountainNoise[x, y] * .3f) :
                    k == 4 ? new Color32(156, 130, 83, 255) : k == 5 ? Color32.Lerp(land, shore, .55f) : land;
                baseColor = ScalePixel(baseColor, variation);
                FillExplorationTile(pixels, w * tile, x * tile, y * tile, k, baseColor, land, ordinal, x, y);
            }
            UnityEngine.Random.state = oldRandom;
            explorationTexture = new Texture2D(w * tile, h * tile, TextureFormat.RGBA32, false)
            { name = "PixelProvince_" + map.id, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            explorationTexture.SetPixels32(pixels); explorationTexture.Apply(false, true);
            return explorationTexture;
        }

        private void BuildExplorationPoints(MapInfo map)
        {
            explorationPoints.Clear();
            var towns = new List<TownInfo>();
            foreach (var town in mapCatalog?.towns ?? Array.Empty<TownInfo>()) if (town != null && town.mapId == map.id) towns.Add(town);
            if (towns.Count == 0) return;
            var minX = int.MaxValue; var maxX = int.MinValue; var minY = int.MaxValue; var maxY = int.MinValue;
            foreach (var town in towns) { minX = Mathf.Min(minX, town.x); maxX = Mathf.Max(maxX, town.x); minY = Mathf.Min(minY, town.y); maxY = Mathf.Max(maxY, town.y); }
            var used = new HashSet<int>();
            foreach (var town in towns)
            {
                var nx = maxX == minX ? .5f : Mathf.InverseLerp(minX, maxX, town.x);
                var ny = maxY == minY ? .5f : Mathf.InverseLerp(minY, maxY, town.y);
                var cell = new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(14, ExplorationMapWidth - 15, nx)), Mathf.RoundToInt(Mathf.Lerp(10, ExplorationMapHeight - 11, ny)));
                cell = FindOpenExplorationCell(cell, used); used.Add(cell.y * ExplorationMapWidth + cell.x);
                explorationPoints.Add(new WorldMapPoint { kind = WorldPointKind.Town, title = town.name, town = town, cell = cell });
                var caveIndex = 0;
                foreach (var dungeon in mapCatalog.dungeons ?? Array.Empty<DungeonInfo>())
                {
                    if (dungeon == null || dungeon.townId != town.id) continue;
                    var dir = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }[caveIndex % 4];
                    var caveCell = FindOpenExplorationCell(cell + dir * (4 + caveIndex / 4 * 3), used); used.Add(caveCell.y * ExplorationMapWidth + caveCell.x);
                    explorationPoints.Add(new WorldMapPoint { kind = WorldPointKind.Dungeon, title = dungeon.name, town = town, dungeon = dungeon, cell = caveCell });
                    caveIndex++;
                }
                if (town.monsterPool != null && town.monsterPool.Length > 0)
                {
                    var zoneCell = FindOpenExplorationCell(cell + new Vector2Int(-4, -3), used); used.Add(zoneCell.y * ExplorationMapWidth + zoneCell.x);
                    explorationPoints.Add(new WorldMapPoint { kind = WorldPointKind.MonsterZone, title = "Bãi tiểu yêu · " + town.name, town = town, cell = zoneCell });
                }
            }
        }

        private void BuildExplorationPointsData(MapInfo map) => BuildExplorationPoints(map);
        private void BuildExplorationPoints(GameState state) => BuildExplorationPoints(explorationMap);

        private Vector2Int FindOpenExplorationCell(Vector2Int desired, HashSet<int> used)
        {
            for (var radius = 0; radius < 12; radius++)
                for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius) continue;
                    var cell = new Vector2Int(Mathf.Clamp(desired.x + x, 4, ExplorationMapWidth - 5), Mathf.Clamp(desired.y + y, 4, ExplorationMapHeight - 5));
                    var key = cell.y * ExplorationMapWidth + cell.x;
                    if (!used.Contains(key)) return cell;
                }
            return new Vector2Int(Mathf.Clamp(desired.x, 4, ExplorationMapWidth - 5), Mathf.Clamp(desired.y, 4, ExplorationMapHeight - 5));
        }

        private void CarveExplorationRoad(byte[,] tiles, Vector2Int from, Vector2Int to)
        {
            var dx = Mathf.Abs(to.x - from.x); var sx = from.x < to.x ? 1 : -1;
            var dy = -Mathf.Abs(to.y - from.y); var sy = from.y < to.y ? 1 : -1;
            var error = dx + dy; var x = from.x; var y = from.y;
            while (true)
            {
                if (x >= 2 && y >= 2 && x < ExplorationMapWidth - 2 && y < ExplorationMapHeight - 2)
                {
                    tiles[x, y] = 4;
                    explorationBlocked[x, y] = false;
                }
                if (x == to.x && y == to.y) break;
                var twice = error * 2;
                if (twice >= dy) { error += dy; x += sx; }
                if (twice <= dx) { error += dx; y += sy; }
            }
        }

        private static Color32 ScalePixel(Color32 color, float scale) => new Color32((byte)Mathf.Clamp(color.r * scale, 0, 255), (byte)Mathf.Clamp(color.g * scale, 0, 255), (byte)Mathf.Clamp(color.b * scale, 0, 255), 255);

        private static void FillExplorationTile(Color32[] pixels, int textureWidth, int ox, int oy, byte kind, Color32 color, Color32 land, int ordinal, int cellX, int cellY)
        {
            var tile = ExplorationTilePixels;
            for (var y = 0; y < tile; y++) for (var x = 0; x < tile; x++)
            {
                var pixel = color;
                var hash = (cellX * 73856093) ^ (cellY * 19349663) ^ (x * 83492791) ^ (y * 297121507);
                var speckle = (hash & 15) == 0;
                if (kind == 2)
                {
                    var peak = 5 + Mathf.Abs((cellX * 3 + cellY * 7) % 5);
                    if (y > peak + x / 2 && y > peak + (tile - x) / 2) pixel = ScalePixel(color, .50f);
                    else if ((x + y + cellX) % 5 == 0 || y == peak + x / 2 || y == peak + (tile - x) / 2) pixel = new Color32(201, 190, 164, 255);
                    if (ordinal >= 9 && y >= peak + 2) pixel = new Color32(204, 214, 213, 255);
                }
                else if (kind == 1)
                {
                    if ((x >= 3 && x <= 9 && y >= 2 && y <= 8) || (x >= 1 && x <= 10 && y >= 5 && y <= 10)) pixel = new Color32((byte)Mathf.Clamp(color.r * .77f, 0, 255), (byte)Mathf.Clamp(color.g * 1.12f, 0, 255), (byte)Mathf.Clamp(color.b * .82f, 0, 255), 255);
                    if (x == 6 && y >= 8 && y <= 11) pixel = new Color32(112, 83, 55, 255);
                }
                else if (kind == 3)
                {
                    if ((y + cellY) % 5 == 2 && x > 1 && x < 10) pixel = new Color32(120, 164, 159, 255);
                }
                else if (kind == 4)
                {
                    if (x == 0 || x == tile - 1 || y == 0 || y == tile - 1) pixel = Color32.Lerp(color, land, .42f);
                    else if (speckle) pixel = new Color32(194, 164, 104, 255);
                }
                else if (kind == 0 && speckle) pixel = ScalePixel(color, (hash & 16) == 0 ? .90f : 1.08f);
                if (x == 0 || y == 0) pixel = new Color32((byte)(pixel.r * .70f), (byte)(pixel.g * .70f), (byte)(pixel.b * .70f), 255);
                pixels[(oy + y) * textureWidth + ox + x] = pixel;
            }
        }

        private void AddExplorationMarker(WorldMapPoint point)
        {
            var size = point.kind == WorldPointKind.Town ? 24f : 19f;
            var root = new GameObject("WorldPoint_" + point.kind + "_" + point.title, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(explorationMapRect, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = ExplorationCellPosition(point.cell);
            var image = root.GetComponent<Image>();
            image.sprite = AtlasPixelSprite(point.kind == WorldPointKind.Town ? "T" : point.kind == WorldPointKind.Dungeon ? "D" : "Y");
            image.color = point.kind == WorldPointKind.Town ? new Color32(255, 227, 165, 255) : Color.white;
            image.preserveAspect = true;
            root.GetComponent<Button>().onClick.AddListener(() => SelectExplorationPoint(point));
            var labelObject = new GameObject("Name", typeof(RectTransform), typeof(Text)); labelObject.transform.SetParent(root.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>(); Place(labelRect, new Vector2(-.8f, -1.8f), new Vector2(5f, -.72f));
            var label = labelObject.GetComponent<Text>(); label.font = BuiltinFont(); label.fontSize = point.kind == WorldPointKind.Town ? 13 : 11;
            label.color = point.kind == WorldPointKind.Town ? new Color32(250, 236, 198, 255) : new Color32(255, 211, 142, 255);
            label.alignment = TextAnchor.MiddleLeft; label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow; label.text = point.title;
            label.raycastTarget = false; label.enabled = point.kind == WorldPointKind.Town;
        }

        private Image AddExplorationPlayerMarker(GameState state)
        {
            var marker = new GameObject("Cultivator", typeof(RectTransform), typeof(Image)); marker.transform.SetParent(explorationMapRect, false);
            var rect = marker.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(25, 36);
            var image = marker.GetComponent<Image>(); image.sprite = CreateCultivatorSprite(state?.player?.appearanceColors); image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        private static Sprite CreateCultivatorSprite(AppearanceColors colors)
        {
            const int width = 16, height = 24;
            var pixels = new Color32[width * height]; var clear = new Color32(0, 0, 0, 0);
            for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;
            var hair = PixelColor(colors?.hair, new Color32(48, 35, 32, 255));
            var robe = PixelColor(colors?.outfit, new Color32(64, 115, 117, 255));
            var eyes = PixelColor(colors?.eyes, new Color32(61, 149, 135, 255));
            Action<int, int, int, int, Color32> rect = (x, y, rw, rh, c) => { for (var py = y; py < y + rh; py++) for (var px = x; px < x + rw; px++) if (px >= 0 && px < width && py >= 0 && py < height) pixels[py * width + px] = c; };
            rect(5, 18, 6, 4, hair); rect(4, 15, 8, 4, new Color32(220, 175, 137, 255)); rect(5, 16, 1, 1, eyes); rect(10, 16, 1, 1, eyes);
            rect(3, 7, 10, 8, robe); rect(1, 2, 14, 5, robe); rect(6, 7, 4, 7, new Color32(220, 193, 133, 255));
            rect(4, 3, 8, 2, new Color32(197, 166, 100, 255)); rect(6, 1, 4, 2, new Color32(42, 44, 46, 255)); rect(13, 10, 1, 12, new Color32(195, 197, 191, 255));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "PlayerPixelSprite", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .1f), width);
        }

        private static Color32 PixelColor(string value, Color32 fallback)
        {
            if (!string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value.StartsWith("#") ? value : "#" + value, out var parsed)) return parsed;
            return fallback;
        }

        private Vector2 ExplorationCellPosition(Vector2Int cell) => new Vector2(cell.x * ExplorationTilePixels - explorationMapRect.sizeDelta.x * .5f + ExplorationTilePixels * .5f,
            cell.y * ExplorationTilePixels - explorationMapRect.sizeDelta.y * .5f + ExplorationTilePixels * .5f);

        private void HandleExplorationTap(Vector2 screenPoint)
        {
            if (explorationMapRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(explorationMapRect, screenPoint, null, out var local)) return;
            var cell = new Vector2Int(Mathf.FloorToInt((local.x + explorationMapRect.sizeDelta.x * .5f) / ExplorationTilePixels),
                Mathf.FloorToInt((local.y + explorationMapRect.sizeDelta.y * .5f) / ExplorationTilePixels));
            if (cell.x < 0 || cell.y < 0 || cell.x >= ExplorationMapWidth || cell.y >= ExplorationMapHeight) return;
            explorationSelectedPoint = ClosestExplorationPoint(cell, 2);
            if (explorationSelectedPoint != null) UpdateExplorationLabels();
            MoveExplorationTo(cell);
        }

        private void SelectExplorationPoint(WorldMapPoint point)
        {
            explorationSelectedPoint = point;
            UpdateExplorationLabels();
            MoveExplorationTo(point.cell);
        }

        private void MoveExplorationBy(Vector2Int delta) => MoveExplorationTo(explorationCell + delta);

        private void MoveExplorationTo(Vector2Int destination)
        {
            if (explorationMovement != null) StopCoroutine(explorationMovement);
            destination.x = Mathf.Clamp(destination.x, 3, ExplorationMapWidth - 4); destination.y = Mathf.Clamp(destination.y, 3, ExplorationMapHeight - 4);
            var path = FindExplorationPath(explorationCell, destination);
            if (path == null || path.Count == 0) { ShowStatus("Dãy núi hoặc vực nước chặn lối đi. Hãy chọn đường mòn khác."); return; }
            explorationMovement = StartCoroutine(WalkExplorationPath(path));
        }

        private List<Vector2Int> FindExplorationPath(Vector2Int start, Vector2Int goal)
        {
            var total = ExplorationMapWidth * ExplorationMapHeight;
            var previous = new int[total]; for (var i = 0; i < total; i++) previous[i] = -2;
            var queue = new int[total]; var head = 0; var tail = 0;
            var startIndex = start.y * ExplorationMapWidth + start.x; var goalIndex = goal.y * ExplorationMapWidth + goal.x;
            previous[startIndex] = -1; queue[tail++] = startIndex;
            var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (head < tail && previous[goalIndex] == -2)
            {
                var current = queue[head++]; var cell = new Vector2Int(current % ExplorationMapWidth, current / ExplorationMapWidth);
                foreach (var dir in dirs)
                {
                    var next = cell + dir;
                    if (next.x < 0 || next.y < 0 || next.x >= ExplorationMapWidth || next.y >= ExplorationMapHeight || explorationBlocked[next.x, next.y]) continue;
                    var index = next.y * ExplorationMapWidth + next.x;
                    if (previous[index] != -2) continue;
                    previous[index] = current; queue[tail++] = index;
                }
            }
            if (previous[goalIndex] == -2) return null;
            var path = new List<Vector2Int>();
            for (var index = goalIndex; index != startIndex; index = previous[index]) path.Add(new Vector2Int(index % ExplorationMapWidth, index / ExplorationMapWidth));
            path.Reverse(); return path;
        }

        private IEnumerator WalkExplorationPath(List<Vector2Int> path)
        {
            foreach (var cell in path)
            {
                explorationCell = cell; UpdateExplorationPlayerPosition();
                yield return new WaitForSeconds(.075f);
            }
            explorationMovement = null;
            SaveExplorationPosition();
            var point = ClosestExplorationPoint(explorationCell, 2);
            if (point != null) { explorationSelectedPoint = point; UpdateExplorationLabels(); }
        }

        private void SaveExplorationPosition()
        {
            if (offlinePreview)
            {
                PlayerPrefs.SetString(ExplorationSaveKey(latestState, explorationMap), explorationCell.x + "," + explorationCell.y); PlayerPrefs.Save();
                return;
            }
            if (latestState?.player == null || explorationMap == null) return;
            var savedCell = explorationCell;
            var savedMapId = explorationMap.id;
            PlayerPrefs.SetString(ExplorationSaveKey(latestState, explorationMap), savedCell.x + "," + savedCell.y); PlayerPrefs.Save();
            client.SaveWorldPosition(savedMapId, savedCell.x, savedCell.y, (success, error) =>
            {
                if (success && latestState?.player != null) { if (latestState.player.worldPosition == null) latestState.player.worldPosition = new WorldMapPosition(); latestState.player.worldPosition.mapId = savedMapId; latestState.player.worldPosition.x = savedCell.x; latestState.player.worldPosition.y = savedCell.y; }
                else ShowStatus("Vị trí đã đi được lưu trên máy; chưa đồng bộ được máy chủ: " + error);
            });
        }

        private void StopExplorationMovement(bool savePosition)
        {
            if (explorationMovement == null) return;
            StopCoroutine(explorationMovement);
            explorationMovement = null;
            if (savePosition) SaveExplorationPosition();
        }

        private static string ExplorationSaveKey(GameState state, MapInfo map) => "tutien.world." + (state?.player?.userId ?? "guest") + "." + (map?.id ?? "unknown");

        private void UpdateExplorationPlayerPosition()
        {
            if (explorationPlayerMarker != null)
                explorationPlayerMarker.rectTransform.anchoredPosition = ExplorationCellPosition(explorationCell) + new Vector2(0, 7);
            if (explorationMiniPlayerRect != null)
            {
                var miniSize = explorationMiniMap == null ? new Vector2(300, 110) : explorationMiniMap.rectTransform.rect.size;
                explorationMiniPlayerRect.anchoredPosition = new Vector2((explorationCell.x / (float)ExplorationMapWidth - .5f) * miniSize.x,
                    (explorationCell.y / (float)ExplorationMapHeight - .5f) * miniSize.y);
            }
            if (explorationLocationText != null) explorationLocationText.text = $"Ô {explorationCell.x + 1} : {explorationCell.y + 1}  ·  {latestState?.realm?.name ?? "Phàm Nhân"}";
        }

        private void UpdateExplorationLabels()
        {
            if (explorationPoiText == null) return;
            if (explorationSelectedPoint == null) { explorationPoiText.text = "Đường núi · Chạm bản đồ để đi từng ô."; return; }
            var point = explorationSelectedPoint;
            var kind = point.kind == WorldPointKind.Town ? "THÀNH TRẤN" : point.kind == WorldPointKind.Dungeon ? "CỔ ĐỘNG" : "BÃI TIỂU YÊU";
            explorationPoiText.text = $"{kind}  ·  {point.title}     ({point.cell.x + 1}, {point.cell.y + 1})\n{point.town?.realmMinName ?? explorationMap?.realmMinName ?? "Địa vực tu luyện"}  ·  {(point.town?.desc ?? point.dungeon?.desc ?? "Điểm thám hiểm trên bản đồ")}";
        }

        private void PanExplorationMap(Vector2 delta)
        {
            if (explorationMapRect == null) return;
            var scale = canvasScaler == null ? 1f : canvasScaler.scaleFactor;
            explorationMapRect.anchoredPosition += delta / Mathf.Max(.01f, scale);
            ClampExplorationPan();
        }

        private void SetExplorationZoom(float value)
        {
            explorationZoom = Mathf.Clamp(value, 1f, 2f);
            if (explorationMapRect == null) return;
            explorationMapRect.localScale = Vector3.one * explorationZoom; ClampExplorationPan();
        }

        private void ClampExplorationPan()
        {
            if (explorationMapRect == null || explorationViewport == null) return;
            var mapSize = explorationMapRect.sizeDelta * explorationZoom; var viewSize = explorationViewport.rect.size;
            var limit = new Vector2(Mathf.Max(0, (mapSize.x - viewSize.x) * .5f), Mathf.Max(0, (mapSize.y - viewSize.y) * .5f));
            explorationMapRect.anchoredPosition = new Vector2(Mathf.Clamp(explorationMapRect.anchoredPosition.x, -limit.x, limit.x), Mathf.Clamp(explorationMapRect.anchoredPosition.y, -limit.y, limit.y));
        }

        private WorldMapPoint FindPointForTown(string townId)
        {
            foreach (var point in explorationPoints) if (point.kind == WorldPointKind.Town && point.town?.id == townId) return point;
            return null;
        }

        private WorldMapPoint ClosestExplorationPoint(Vector2Int cell, int range)
        {
            WorldMapPoint closest = null; var best = int.MaxValue;
            foreach (var point in explorationPoints)
            {
                var d = Mathf.Abs(point.cell.x - cell.x) + Mathf.Abs(point.cell.y - cell.y);
                if (d <= range && d < best) { best = d; closest = point; }
            }
            return closest;
        }

        private void ActivateSelectedExplorationPoint(GameState state)
        {
            var point = explorationSelectedPoint;
            if (point == null) { ShowStatus("Hãy chọn một thành trấn, cổ động hoặc bãi yêu trước."); return; }
            if (explorationCell != point.cell) { MoveExplorationTo(point.cell); return; }
            if (offlinePreview) { ShowStatus("Bản đồ đã sẵn sàng offline; chiến đấu và ngự kiếm cần máy chủ online."); return; }
            if (point.kind == WorldPointKind.Town)
            {
                if (state.town?.id == point.town.id) ShowHome(state); else TravelTo(point.town);
            }
            else if (state.town?.id != point.town?.id) TravelTo(point.town);
            else if (point.kind == WorldPointKind.Dungeon) { SetAtlasOrientation(false); EnterDungeon(point.dungeon.id); }
            else { SetAtlasOrientation(false); ShowPveTown(state, point.town); }
        }

        private void RenderWorldAtlas(GameState state)
        {
            if (atlasMapRoot != null) Destroy(atlasMapRoot);
            ClearContent();
            statusMin = new Vector2(0.40f, 0.01f); statusMax = new Vector2(0.60f, 0.045f);

            atlasMapRoot = PanelObject("AtlasFullscreenRoot", backgroundRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            atlasMapRoot.transform.SetAsFirstSibling();
            atlasMapRoot.GetComponent<Image>().raycastTarget = false;
            var viewport = PanelObject("AtlasViewport", atlasMapRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.AddComponent<RectMask2D>();
            var layer = PanelObject("AtlasLayer", viewport.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            layer.GetComponent<Image>().enabled = false;
            atlasLayer = layer.transform;
            atlasLayer.localScale = Vector3.one * atlasZoom;
            atlasMapGesture = viewport.AddComponent<AtlasMapGesture>();
            atlasMapGesture.Initialize(viewport.GetComponent<RectTransform>(), layer.GetComponent<RectTransform>());
            atlasMapGesture.ApplyZoom(atlasZoom);

            var art = new GameObject("AtlasPainting", typeof(RectTransform), typeof(RawImage));
            art.transform.SetParent(atlasLayer, false);
            Place(art.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var atlas = art.GetComponent<RawImage>();
            atlas.texture = Resources.Load<Texture2D>(atlasImmortalRealm ? "Maps/TienGioi_Atlas_v4" : "Maps/PhamGioi_Atlas_v4");
            atlas.color = atlas.texture == null ? new Color32(154, 126, 82, 255) : Color.white;
            atlas.raycastTarget = false;

            var maps = AtlasMaps(atlasImmortalRealm);
            var towns = AtlasTowns(atlasImmortalRealm);
            foreach (var map in maps) AddAtlasRegionLabel(map, towns);
            AddAtlasLandmark("THIÊN SƠN", new Vector2(0.50f, 0.88f));

            foreach (var town in towns)
            {
                var point = AtlasPosition(town, towns);
                if (atlasShowTowns)
                {
                    var current = state.town?.id == town.id;
                    AddAtlasMarker("Town_" + town.id, point, "T", current ? Gold : Color.white,
                        current ? 42 : 34, () => SelectAtlasTown(state, town, "town", null), town.name,
                        current || (atlasSelectedTown?.id == town.id && atlasSelectionKind == "town"));
                }
                if (atlasShowDungeons)
                {
                    var caveIndex = 0;
                    foreach (var dungeon in mapCatalog.dungeons ?? Array.Empty<DungeonInfo>())
                    {
                        if (dungeon.townId != town.id) continue;
                        var caveAngle = caveIndex * Mathf.PI * 2f / 5f - Mathf.PI * .5f;
                        var offset = new Vector2(Mathf.Cos(caveAngle) * .046f, Mathf.Sin(caveAngle) * .044f);
                        AddAtlasMarker("Cave_" + dungeon.id, ClampAtlasPosition(point + offset), "D", Color.white, 32,
                            () => SelectAtlasTown(state, town, "dungeon", dungeon), dungeon.name,
                            atlasSelectedDungeon?.id == dungeon.id && atlasSelectionKind == "dungeon");
                        caveIndex++;
                    }
                }
            }

            if (atlasShowMonsterZones)
            {
                foreach (var map in maps)
                {
                    var regionTowns = new System.Collections.Generic.List<TownInfo>();
                    foreach (var town in towns)
                        if (town.mapId == map.id && town.monsterPool != null && town.monsterPool.Length > 0) regionTowns.Add(town);
                    if (regionTowns.Count == 0) continue;
                    var fieldCount = AtlasMonsterFieldCount(regionTowns);
                    for (var field = 0; field < fieldCount; field++)
                    {
                        var townIndex = field % regionTowns.Count;
                        var localField = field / regionTowns.Count;
                        var localFieldCount = (fieldCount + regionTowns.Count - 1 - townIndex) / regionTowns.Count;
                        var town = regionTowns[townIndex];
                        var point = AtlasMonsterFieldPosition(map, field, fieldCount);
                        var fieldNumber = field + 1;
                        AddAtlasMarker("MonsterField_" + map.id + "_" + field, point, "Y", Color.white, 34,
                            () => SelectAtlasMonsterField(state, town, localField, localFieldCount, fieldNumber), "Bãi quái " + fieldNumber,
                            atlasSelectionKind == "monsters" && atlasSelectedTown?.mapId == map.id && atlasSelectedMonsterFieldLabel == fieldNumber);
                    }
                }
            }

            if (atlasInfoExpanded && atlasSelectedTown != null)
            {
                var info = PanelObject("AtlasSelectionPanel", content.transform, new Vector2(0.015f, 0.055f), new Vector2(0.34f, 0.30f), Vector2.zero, Vector2.zero, new Color32(15, 20, 27, 224));
                Label(AtlasSelectionText(state), 17, Cream, TextAnchor.UpperLeft, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.91f), info.transform);
                Button("×", new Vector2(0.86f, 0.82f), new Vector2(0.97f, 0.98f), Panel, () => { atlasInfoExpanded = false; RenderWorldAtlas(state); }, info.transform);
                if (atlasSelectionKind == "dungeon" && atlasSelectedDungeon != null)
                {
                    Button("TỚI THÀNH", new Vector2(0.04f, 0.07f), new Vector2(0.48f, 0.25f), Gold,
                        () => { if (offlinePreview) ShowStatus("Bản xem offline không di chuyển thành trấn."); else TravelTo(atlasSelectedTown); }, info.transform);
                    Button("VÀO ĐỘNG", new Vector2(0.52f, 0.07f), new Vector2(0.96f, 0.25f), Panel,
                        () => { if (offlinePreview) ShowStatus("Vào cổ động cần máy chủ online."); else if (state.town?.id != atlasSelectedTown?.id) ShowStatus("Hãy tới thành trấn gắn với cổ động này trước."); else { SetAtlasOrientation(false); EnterDungeon(atlasSelectedDungeon.id); } }, info.transform);
                }
                else if (atlasSelectionKind == "monsters")
                {
                    Button(state.town?.id == atlasSelectedTown?.id ? "SĂN TIỂU YÊU" : "TỚI BÃI QUÁI", new Vector2(0.04f, 0.07f), new Vector2(0.55f, 0.25f), Gold,
                        () => { if (offlinePreview) ShowStatus("Săn yêu thú cần máy chủ online."); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPveTown(state, atlasSelectedTown); } else TravelTo(atlasSelectedTown); }, info.transform);
                    Button("PVP", new Vector2(0.59f, 0.07f), new Vector2(0.96f, 0.25f), Panel,
                        () => { if (offlinePreview) ShowStatus("PVP cần máy chủ online."); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPvp(state); } else ShowStatus("PVP chỉ mở tại thành trấn hiện tại."); }, info.transform);
                }
                else
                {
                    Button(state.town?.id == atlasSelectedTown?.id ? "PVE" : "NGỰ KIẾM TỚI", new Vector2(0.04f, 0.07f), new Vector2(0.48f, 0.25f), Gold,
                        () => { if (offlinePreview) ShowStatus("PVE và di chuyển cần máy chủ online."); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPveTown(state, atlasSelectedTown); } else TravelTo(atlasSelectedTown); }, info.transform);
                    Button("PVP", new Vector2(0.52f, 0.07f), new Vector2(0.96f, 0.25f), Panel,
                        () => { if (offlinePreview) ShowStatus("PVP cần máy chủ online."); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPvp(state); } else ShowStatus("PVP chỉ mở tại thành trấn hiện tại."); }, info.transform);
                }
            }

            var townCount = towns.Length;
            var caveCount = CountAtlasDungeons(towns);
            var monsterZoneCount = CountAtlasMonsterZones(towns);
            var top = PanelObject("AtlasTopBar", content.transform, new Vector2(0.008f, 0.91f), new Vector2(0.992f, 0.99f), Vector2.zero, Vector2.zero, new Color32(15, 20, 27, 208));
            Button("×", new Vector2(0.008f, 0.08f), new Vector2(0.065f, 0.92f), Panel, () => ReturnFromWorldAtlas(state), top.transform);
            Label("THIÊN HẠ", 20, Gold, TextAnchor.MiddleCenter, new Vector2(0.07f, 0.08f), new Vector2(0.17f, 0.92f), top.transform);
            Button((atlasShowTowns ? "● " : "○ ") + "Thành " + townCount, new Vector2(0.18f, 0.08f), new Vector2(0.31f, 0.92f), atlasShowTowns ? Panel : Ink,
                () => { atlasShowTowns = !atlasShowTowns; RenderWorldAtlas(state); }, top.transform);
            Button((atlasShowDungeons ? "● " : "○ ") + "Động " + caveCount, new Vector2(0.32f, 0.08f), new Vector2(0.44f, 0.92f), atlasShowDungeons ? Panel : Ink,
                () => { atlasShowDungeons = !atlasShowDungeons; RenderWorldAtlas(state); }, top.transform);
            Button((atlasShowMonsterZones ? "● " : "○ ") + "Tiểu yêu " + monsterZoneCount, new Vector2(0.45f, 0.08f), new Vector2(0.61f, 0.92f), atlasShowMonsterZones ? Panel : Ink,
                () => { atlasShowMonsterZones = !atlasShowMonsterZones; RenderWorldAtlas(state); }, top.transform);
            Button("PHÀM", new Vector2(0.63f, 0.08f), new Vector2(0.72f, 0.92f), atlasImmortalRealm ? Panel : Gold,
                () => SetAtlasRealm(state, false), top.transform);
            Button("TIÊN", new Vector2(0.73f, 0.08f), new Vector2(0.81f, 0.92f), atlasImmortalRealm ? Gold : Panel,
                () => SetAtlasRealm(state, true), top.transform);
            Button("PVP / PVE", new Vector2(0.82f, 0.08f), new Vector2(0.96f, 0.92f), Panel,
                () => { SetAtlasOrientation(false); ShowBattleMapSet(state, atlasImmortalRealm); }, top.transform);
            Button("−", new Vector2(0.91f, 0.045f), new Vector2(0.955f, 0.105f), Panel,
                () => { if (atlasZoomSlider != null) atlasZoomSlider.value = Mathf.Max(1f, atlasZoomSlider.value - 0.15f); });
            var zoomTrack = PanelObject("AtlasZoomTrack", content.transform, new Vector2(0.76f, 0.062f), new Vector2(0.90f, 0.082f), Vector2.zero, Vector2.zero, new Color32(38, 32, 24, 220));
            var zoomHandle = PanelObject("AtlasZoomHandle", zoomTrack.transform, new Vector2(0f, -1f), new Vector2(0.12f, 2f), Vector2.zero, Vector2.zero, Gold);
            atlasZoomSlider = zoomTrack.AddComponent<Slider>();
            atlasZoomSlider.minValue = 1f; atlasZoomSlider.maxValue = 3f; atlasZoomSlider.value = atlasZoom; atlasZoomSlider.direction = Slider.Direction.LeftToRight;
            atlasZoomSlider.targetGraphic = zoomHandle.GetComponent<Image>(); atlasZoomSlider.handleRect = zoomHandle.GetComponent<RectTransform>();
            atlasZoomSlider.onValueChanged.AddListener(value =>
            {
                atlasZoom = value;
                if (atlasMapGesture != null) atlasMapGesture.ApplyZoom(value);
                else if (atlasLayer != null) atlasLayer.localScale = Vector3.one * value;
            });
            if (atlasMapGesture != null) atlasMapGesture.SetZoomSlider(atlasZoomSlider);
            Button("+", new Vector2(0.955f, 0.045f), new Vector2(0.995f, 0.105f), Panel,
                () => { if (atlasZoomSlider != null) atlasZoomSlider.value = Mathf.Min(3f, atlasZoomSlider.value + 0.15f); });

            ShowStatus(atlas.texture == null ? "Thiếu tranh bản đồ trong Resources/Maps." : $"{maps.Length} châu · {townCount} thành · {caveCount} cổ động · {monsterZoneCount} bãi tiểu yêu.");
        }

        private void SelectAtlasTown(GameState state, TownInfo town, string kind, DungeonInfo dungeon)
        {
            atlasSelectedTown = town;
            atlasSelectedDungeon = dungeon;
            atlasSelectedMonsterField = 0;
            atlasSelectedMonsterFieldCount = 1;
            atlasSelectedMonsterFieldLabel = 0;
            atlasSelectionKind = kind;
            atlasInfoExpanded = true;
            RenderWorldAtlas(state);
        }

        private void SelectAtlasMonsterField(GameState state, TownInfo town, int fieldIndex, int fieldCount, int fieldNumber)
        {
            atlasSelectedTown = town;
            atlasSelectedDungeon = null;
            atlasSelectedMonsterField = fieldIndex;
            atlasSelectedMonsterFieldCount = Mathf.Max(1, fieldCount);
            atlasSelectedMonsterFieldLabel = fieldNumber;
            atlasSelectionKind = "monsters";
            atlasInfoExpanded = true;
            RenderWorldAtlas(state);
        }

        private void SetAtlasRealm(GameState state, bool immortal)
        {
            atlasImmortalRealm = immortal;
            atlasSelectedTown = IsTownInAtlas(state.town, immortal) ? state.town : FirstTownInAtlas(immortal);
            atlasSelectedDungeon = null;
            atlasSelectedMonsterField = 0;
            atlasSelectedMonsterFieldCount = 1;
            atlasSelectedMonsterFieldLabel = 0;
            atlasSelectionKind = "town";
            RenderWorldAtlas(state);
            if (immortal && state.player?.ascended != true)
                ShowStatus("Đang xem trước Tiên Giới; cần Phi Thăng mới có thể tới các thành trấn.");
        }

        private string AtlasSelectionText(GameState state)
        {
            var town = atlasSelectedTown;
            if (town == null) return "Chưa có dữ liệu địa danh trong giới này.";
            var map = FindMap(town.mapId);
            var current = state.town?.id == town.id ? "\n📍 Bạn đang ở đây" : "";
            if (atlasSelectionKind == "dungeon" && atlasSelectedDungeon != null)
                return $"{atlasSelectedDungeon.icon} {atlasSelectedDungeon.name}\n{map?.provinceName ?? map?.name}\nCảnh giới: {atlasSelectedDungeon.realmMin}\n\n{atlasSelectedDungeon.desc}\n\nGắn với: {town.name}{current}";
            if (atlasSelectionKind == "monsters")
                return $"🐾 BÃI QUÁI {atlasSelectedMonsterFieldLabel}\n{town.name} · {map?.provinceName ?? map?.name}\n\n{AtlasMonsterNames(town, atlasSelectedMonsterField, atlasSelectedMonsterFieldCount)}\n\nNhóm {atlasSelectedMonsterField + 1}/{atlasSelectedMonsterFieldCount} · {town.monsterPool?.Length ?? 0} loài trong khu vực{current}";
            var dungeonCount = CountTownDungeons(town.id);
            return $"{town.icon} {town.name}\n{map?.provinceName ?? map?.name}\nCảnh giới: {town.realmMinName ?? RealmLabel(town.realmMin)}\n\n{town.desc}\n\n{town.monsterPool?.Length ?? 0} loài yêu thú\n{dungeonCount} cổ động{current}";
        }

        private string AtlasMonsterNames(TownInfo town, int groupIndex, int groupCount)
        {
            var names = new System.Collections.Generic.List<string>();
            var pool = town?.monsterPool ?? Array.Empty<string>();
            var groups = Mathf.Max(1, groupCount);
            var start = Mathf.FloorToInt(pool.Length * Mathf.Clamp01((float)groupIndex / groups));
            var end = Mathf.FloorToInt(pool.Length * Mathf.Clamp01((float)(groupIndex + 1) / groups));
            if (end <= start && start < pool.Length) end = start + 1;
            for (var index = start; index < end; index++)
            {
                var id = pool[index];
                foreach (var monster in mapCatalog?.monsters ?? Array.Empty<MonsterInfo>())
                {
                    if (monster.id != id) continue;
                    names.Add(monster.icon + " " + monster.name);
                    break;
                }
                if (names.Count >= 7) break;
            }
            var extra = Math.Max(0, end - start - names.Count);
            if (extra > 0) names.Add($"… và {extra} loài khác");
            return string.Join("\n", names);
        }

        private void AddAtlasRegionLabel(MapInfo map, TownInfo[] towns)
        {
            var hasTowns = false;
            foreach (var town in towns) if (town.mapId == map.id) { hasTowns = true; break; }
            if (!hasTowns) return;
            var regionTowns = new System.Collections.Generic.List<TownInfo>();
            foreach (var town in towns) if (town.mapId == map.id && town.monsterPool != null && town.monsterPool.Length > 0) regionTowns.Add(town);
            var fieldCount = AtlasMonsterFieldCount(regionTowns);
            var caveCount = 0;
            foreach (var town in towns) if (town.mapId == map.id) caveCount += CountTownDungeons(town.id);
            var townCount = 0;
            foreach (var town in towns) if (town.mapId == map.id) townCount++;

            var point = AtlasRegionAnchor(map);
            point.y += point.y < .50f ? .105f : -.105f;
            const float width = .17f;
            const float halfHeight = .031f;
            var tag = PanelObject("Region_" + map.id, atlasLayer, point - new Vector2(width * .5f, halfHeight), point + new Vector2(width * .5f, halfHeight), Vector2.zero, Vector2.zero, new Color32(22, 25, 31, 218));
            tag.AddComponent<PixelMapMarkerMotion>();
            tag.GetComponent<Image>().raycastTarget = false;
            var label = ChildText(tag.transform, "ProvinceName", 13, Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            label.text = $"{AtlasRealmNumber(map):00} · {map.provinceName ?? map.name}\n{townCount} thành · {fieldCount} bãi quái · {caveCount} cổ động";
            label.raycastTarget = false;
        }

        private void AddAtlasLandmark(string name, Vector2 point)
        {
            var marker = PanelObject("HeavenlyMountainLabel", atlasLayer, point - new Vector2(.075f, .019f), point + new Vector2(.075f, .019f), Vector2.zero, Vector2.zero, new Color32(37, 41, 44, 196));
            marker.AddComponent<PixelMapMarkerMotion>();
            marker.GetComponent<Image>().raycastTarget = false;
            var text = ChildText(marker.transform, "Name", 18, Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.text = atlasImmortalRealm ? "CỬU TIÊU THIÊN SƠN" : name;
            text.raycastTarget = false;
        }

        private void AddAtlasMarker(string name, Vector2 point, string glyph, Color color, float size, Action click, string caption = null, bool showCaption = false)
        {
            var offset = new Vector2(size * .5f, size * .5f);
            var marker = PanelObject(name, atlasLayer, point, point, -offset, offset, Color.clear);
            marker.AddComponent<PixelMapMarkerMotion>();
            var image = marker.GetComponent<Image>();
            image.sprite = AtlasPixelSprite(glyph); image.type = Image.Type.Simple; image.preserveAspect = true; image.color = color; image.raycastTarget = true;
            var button = marker.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors; colors.normalColor = color; colors.highlightedColor = Color.white; colors.pressedColor = Gold; button.colors = colors;
            button.onClick.AddListener(() => click?.Invoke());
            if (!string.IsNullOrWhiteSpace(caption))
            {
                var labelObject = new GameObject("LocationCaption", typeof(RectTransform), typeof(Text), typeof(Outline));
                labelObject.transform.SetParent(marker.transform, false);
                var rect = labelObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f); rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -size * .42f); rect.sizeDelta = new Vector2(220f, 26f);
                var label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); label.fontSize = 12; label.color = Cream;
                label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
                label.text = caption; label.raycastTarget = false;
                var outline = labelObject.GetComponent<Outline>(); outline.effectColor = new Color(0f, 0f, 0f, .94f); outline.effectDistance = new Vector2(1.2f, -1.2f);
                labelObject.SetActive(showCaption);
            }
        }

        private static Sprite AtlasPixelSprite(string glyph)
        {
            if (glyph == "T" && atlasTownSprite != null) return atlasTownSprite;
            if (glyph == "D" && atlasDungeonSprite != null) return atlasDungeonSprite;
            if (glyph == "Y" && atlasMonsterSprite != null) return atlasMonsterSprite;

            const int size = 16;
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;
            Action<int, int, int, int, Color32> rect = (x, y, width, height, color) =>
            {
                for (var py = Mathf.Max(0, y); py < Mathf.Min(size, y + height); py++)
                    for (var px = Mathf.Max(0, x); px < Mathf.Min(size, x + width); px++) pixels[py * size + px] = color;
            };

            var dark = new Color32(47, 35, 28, 255);
            var stone = new Color32(127, 113, 91, 255);
            var lightStone = new Color32(197, 172, 123, 255);
            var gold = new Color32(245, 202, 91, 255);
            if (glyph == "T")
            {
                rect(3, 2, 10, 2, dark); rect(4, 4, 8, 5, new Color32(228, 211, 165, 255));
                rect(2, 9, 12, 2, dark); rect(4, 11, 8, 2, new Color32(189, 104, 54, 255));
                rect(5, 13, 6, 1, gold); rect(7, 2, 2, 4, dark);
                rect(7, 2, 2, 3, new Color32(116, 70, 45, 255));
            }
            else if (glyph == "D")
            {
                rect(4, 2, 8, 2, stone); rect(2, 4, 3, 5, stone); rect(11, 4, 3, 5, stone);
                rect(3, 9, 10, 3, lightStone); rect(5, 4, 6, 7, dark);
                rect(7, 4, 2, 5, new Color32(22, 19, 18, 255)); rect(10, 5, 1, 1, gold);
            }
            else
            {
                rect(3, 10, 3, 3, dark); rect(10, 10, 3, 3, dark);
                rect(4, 6, 8, 6, new Color32(154, 62, 74, 255));
                rect(5, 4, 6, 7, new Color32(191, 78, 86, 255));
                rect(4, 3, 2, 3, dark); rect(10, 3, 2, 3, dark);
                rect(6, 8, 1, 1, gold); rect(9, 8, 1, 1, gold);
                rect(2, 12, 3, 2, new Color32(209, 131, 68, 255)); rect(11, 12, 3, 2, new Color32(209, 131, 68, 255));
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "AtlasPixel" + glyph, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            if (glyph == "T") atlasTownSprite = sprite;
            else if (glyph == "D") atlasDungeonSprite = sprite;
            else if (glyph == "Y") atlasMonsterSprite = sprite;
            return sprite;
        }

        private void SetAtlasOrientation(bool landscape)
        {
            if (!landscape)
            {
                StopExplorationMovement(savePosition: true);
                if (explorationMapRoot != null) { Destroy(explorationMapRoot); explorationMapRoot = null; }
                if (explorationTexture != null) { Destroy(explorationTexture); explorationTexture = null; }
                explorationMapRect = null; explorationViewport = null; explorationMiniMap = null;
            }
            if (!landscape && atlasMapRoot != null)
            {
                Destroy(atlasMapRoot);
                atlasMapRoot = null;
                atlasLayer = null;
            }
            Screen.orientation = landscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
            if (canvasScaler != null) canvasScaler.referenceResolution = landscape ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
            buttonFontSize = landscape ? 20 : 20;
        }

        private MapInfo[] AtlasMaps(bool immortal)
        {
            var result = new System.Collections.Generic.List<MapInfo>();
            foreach (var map in mapCatalog?.maps ?? Array.Empty<MapInfo>()) if (map != null && map.ascensionRequired == immortal) result.Add(map);
            return result.ToArray();
        }

        private TownInfo[] AtlasTowns(bool immortal)
        {
            var result = new System.Collections.Generic.List<TownInfo>();
            foreach (var town in mapCatalog?.towns ?? Array.Empty<TownInfo>()) if (IsTownInAtlas(town, immortal)) result.Add(town);
            result.Sort((a, b) => string.CompareOrdinal(a.mapId, b.mapId));
            return result.ToArray();
        }

        private bool IsTownInAtlas(TownInfo town, bool immortal)
        {
            if (town == null) return false;
            var map = FindMap(town.mapId);
            return map != null && map.ascensionRequired == immortal;
        }

        private TownInfo FirstTownInAtlas(bool immortal)
        {
            foreach (var town in mapCatalog?.towns ?? Array.Empty<TownInfo>()) if (IsTownInAtlas(town, immortal)) return town;
            return null;
        }

        private MapInfo FindMap(string id)
        {
            foreach (var map in mapCatalog?.maps ?? Array.Empty<MapInfo>()) if (map.id == id) return map;
            return null;
        }

        private int CountTownDungeons(string townId)
        {
            var count = 0;
            foreach (var dungeon in mapCatalog?.dungeons ?? Array.Empty<DungeonInfo>()) if (dungeon.townId == townId) count++;
            return count;
        }

        private int CountAtlasDungeons(TownInfo[] towns)
        {
            var count = 0;
            foreach (var town in towns) count += CountTownDungeons(town.id);
            return count;
        }

        private int CountAtlasMonsterZones(TownInfo[] towns)
        {
            var count = 0;
            var countedMaps = new System.Collections.Generic.HashSet<string>();
            foreach (var town in towns)
            {
                if (!countedMaps.Add(town.mapId)) continue;
                var region = new System.Collections.Generic.List<TownInfo>();
                foreach (var candidate in towns)
                    if (candidate.mapId == town.mapId && candidate.monsterPool != null && candidate.monsterPool.Length > 0) region.Add(candidate);
                count += AtlasMonsterFieldCount(region);
            }
            return count;
        }

        private static int AtlasMonsterFieldCount(System.Collections.Generic.List<TownInfo> towns)
        {
            var speciesCount = 0;
            foreach (var town in towns) speciesCount += town?.monsterPool?.Length ?? 0;
            return speciesCount == 0 ? 0 : Mathf.Clamp(Mathf.Max(towns.Count, Mathf.CeilToInt(speciesCount / 8f)), 3, 6);
        }

        private Vector2 AtlasMonsterFieldPosition(MapInfo map, int index, int count)
        {
            var anchor = AtlasRegionAnchor(map);
            var angle = -Mathf.PI * .5f + index * Mathf.PI * 2f / Mathf.Max(1, count);
            var outer = index % 2 == 0 ? .102f : .113f;
            return ClampAtlasPosition(anchor + new Vector2(Mathf.Cos(angle) * outer, Mathf.Sin(angle) * outer * .90f));
        }

        private Vector2 AtlasRegionAnchor(MapInfo map)
        {
            var mortal = new[]
            {
                new Vector2(.20f, .20f), new Vector2(.40f, .20f), new Vector2(.60f, .22f), new Vector2(.81f, .24f),
                new Vector2(.81f, .72f), new Vector2(.61f, .79f), new Vector2(.37f, .79f), new Vector2(.50f, .50f)
            };
            var immortal = new[]
            {
                new Vector2(.18f, .18f), new Vector2(.39f, .18f), new Vector2(.61f, .18f), new Vector2(.82f, .18f),
                new Vector2(.82f, .50f), new Vector2(.82f, .82f), new Vector2(.61f, .82f), new Vector2(.39f, .82f),
                new Vector2(.18f, .82f), new Vector2(.18f, .51f), new Vector2(.50f, .50f)
            };
            var index = AtlasMapOrdinal(map) - (map != null && map.ascensionRequired ? 9 : 1);
            if (map != null && map.ascensionRequired) return immortal[Mathf.Clamp(index, 0, immortal.Length - 1)];
            return mortal[Mathf.Clamp(index, 0, mortal.Length - 1)];
        }

        private static int AtlasMapOrdinal(MapInfo map)
        {
            if (map == null || string.IsNullOrEmpty(map.id)) return 1;
            var underscore = map.id.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(map.id.Substring(underscore + 1), out var value) ? value : 1;
        }

        private static int AtlasRealmNumber(MapInfo map) => map != null && map.ascensionRequired ? AtlasMapOrdinal(map) - 8 : AtlasMapOrdinal(map);

        private Vector2 AtlasPosition(TownInfo town, TownInfo[] towns)
        {
            var map = FindMap(town?.mapId);
            if (town == null || map == null) return new Vector2(.5f, .5f);
            var minX = float.MaxValue; var maxX = float.MinValue; var minY = float.MaxValue; var maxY = float.MinValue;
            foreach (var item in towns)
            {
                if (item.mapId != town.mapId) continue;
                minX = Mathf.Min(minX, item.x); maxX = Mathf.Max(maxX, item.x);
                minY = Mathf.Min(minY, item.y); maxY = Mathf.Max(maxY, item.y);
            }
            var x = maxX == minX ? .5f : Mathf.InverseLerp(minX, maxX, town.x);
            var y = maxY == minY ? .5f : Mathf.InverseLerp(minY, maxY, town.y);
            var anchor = AtlasRegionAnchor(map);
            return ClampAtlasPosition(anchor + new Vector2((x - .5f) * .105f, (y - .5f) * .09f));
        }

        private static Vector2 ClampAtlasPosition(Vector2 point) => new Vector2(Mathf.Clamp(point.x, .035f, .965f), Mathf.Clamp(point.y, .075f, .90f));

        private bool IsImmortalRealm(GameState state)
        {
            var mapId = state?.town?.mapId;
            foreach (var map in mapCatalog?.maps ?? Array.Empty<MapInfo>())
                if (map != null && map.id == mapId) return map.ascensionRequired;
            return state?.player?.ascended == true;
        }

        private BattleMapRealmSet FindBattleMapSet(bool immortal)
        {
            foreach (var set in mapCatalog?.battleMapSets ?? Array.Empty<BattleMapRealmSet>())
                if (set != null && set.requiresAscension == immortal) return set;
            return null;
        }

        private static BattleMapMode FindBattleMapMode(BattleMapRealmSet set, string modeId)
        {
            foreach (var mode in set?.modes ?? Array.Empty<BattleMapMode>())
                if (mode != null && mode.id == modeId) return mode;
            return null;
        }

        private static BattleMapInfo ActiveBattleMap(BattleMapMode mode)
        {
            foreach (var map in mode?.maps ?? Array.Empty<BattleMapInfo>())
                if (map != null && (map.isActive || map.id == mode.activeMapId)) return map;
            return mode?.maps != null && mode.maps.Length > 0 ? mode.maps[0] : null;
        }

        private void AddModeMapOverview(Transform parent, GameState state, bool pvp)
        {
            var set = FindBattleMapSet(IsImmortalRealm(state));
            if (set == null) return;
            var modeIds = pvp
                ? new[] { "pvp_ranked", "pvp_duel", "pvp_sat_phat", "pvp_sect" }
                : new[] { "pve_small_monster", "pve_elite_boss", "pve_world_boss", "pve_ancient_cave" };
            var lines = new System.Collections.Generic.List<string> { $"BỘ MAP {set.name.ToUpperInvariant()} · {(pvp ? "PVP" : "PVE")}" };
            foreach (var id in modeIds)
            {
                var mode = FindBattleMapMode(set, id);
                var active = ActiveBattleMap(mode);
                if (mode == null || active == null) continue;
                var suffix = mode.rotation?.strategy == "random_cycle"
                    ? $" · {mode.maps?.Length ?? 0} map, xoay ngẫu nhiên mỗi {Math.Max(1, mode.rotation.periodSeconds / 60)} phút"
                    : "";
                lines.Add($"{mode.name}: {active.name}{suffix}");
            }
            var card = PanelObject("ModeMapOverview", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Panel);
            card.AddComponent<LayoutElement>().preferredHeight = 118;
            var label = ChildText(card.transform, "Text", 14, Muted, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            label.text = string.Join("\n", lines);
        }

        private void ShowBattleMapSet(GameState state, bool immortal)
        {
            SetAtlasOrientation(false);
            if (offlinePreview && mapCatalog != null)
            {
                RenderBattleMapSet(state, immortal);
                ShowStatus("Bản xem offline · các hoạt động chiến đấu cần máy chủ online.");
                return;
            }
            ShowStatus("Đang đồng bộ bộ map và map Cổ Động đang mở...");
            client.LoadMapCatalog((catalog, error) =>
            {
                if (catalog == null) { ShowStatus(error); return; }
                mapCatalog = catalog;
                RenderBattleMapSet(state, immortal);
            });
        }

        private void RenderBattleMapSet(GameState state, bool immortal)
        {
            var set = FindBattleMapSet(immortal);
            if (set == null) { ShowStatus("Máy chủ chưa gửi bộ bản đồ chiến đấu."); return; }
            latestState = state;
            ClearContent();
            Label("BỘ MAP PVP / PVE", 27, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            Label("Mỗi chế độ có chiến trường riêng; Cổ Động có 5 map xoay vòng ngẫu nhiên.", 16, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.88f));
            var currentImmortal = IsImmortalRealm(state);
            Button("PHÀM GIỚI", new Vector2(0.02f, 0.755f), new Vector2(0.48f, 0.81f), immortal ? Panel : Gold, () => ShowBattleMapSet(state, false));
            Button("TIÊN GIỚI", new Vector2(0.52f, 0.755f), new Vector2(0.98f, 0.81f), immortal ? Gold : Panel, () => ShowBattleMapSet(state, true));
            Label($"{set.name} · {set.modes?.Length ?? 0} chế độ · 12 map", 17, Cream, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.70f), new Vector2(0.98f, 0.75f));
            var rows = CreateScrollList("BattleMapSet", 0.21f, 0.685f);
            foreach (var mode in set.modes ?? Array.Empty<BattleMapMode>())
            {
                if (mode == null) continue;
                var rotating = mode.rotation?.strategy == "random_cycle";
                var header = PanelObject("Mode_" + mode.id, rows, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(39, 48, 60, 255));
                header.AddComponent<LayoutElement>().preferredHeight = 62;
                var rotationText = rotating
                    ? $"Xoay ngẫu nhiên · {mode.maps?.Length ?? 0} map · chu kỳ {Math.Max(1, mode.rotation.periodSeconds / 60)} phút"
                    : "Bản đồ riêng của chế độ";
                var headerText = ChildText(header.transform, "ModeInfo", 16, Gold, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
                headerText.text = $"{mode.name} · {mode.maps?.Length ?? 0} map\n{rotationText}";
                foreach (var map in mode.maps ?? Array.Empty<BattleMapInfo>())
                {
                    if (map == null) continue;
                    var activeMap = map != null && (map.isActive || map.id == mode.activeMapId);
                    var card = PanelObject("BattleMap_" + map?.id, rows, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                        activeMap ? new Color32(55, 49, 34, 255) : Panel);
                    card.AddComponent<LayoutElement>().preferredHeight = 104;
                    var detail = ChildText(card.transform, "MapInfo", 13, activeMap ? Gold : Cream, TextAnchor.MiddleLeft, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
                    var palette = map.palette == null ? "" : string.Join(" · ", map.palette);
                    detail.text = $"{(activeMap ? "✦ ĐANG MỞ  " : "")}{map.name}\n{map.description}\n{map.terrain} · {map.weather} · {palette}";
                }
            }
            Button("VỀ BẢN ĐỒ", new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.105f), Panel, () => ShowMap(state));
            Button("CẬP NHẬT MAP", new Vector2(0.02f, 0.04f), new Vector2(0.48f, 0.105f), Panel, () => ShowBattleMapSet(state, immortal));
            ShowStatus(immortal && !currentImmortal
                ? "Đang xem trước chiến trường Tiên Giới; cần Phi Thăng mới tham chiến được."
                : $"Bộ {set.name}: 4 map PvP, 3 map PvE cố định và 5 Cổ Động xoay vòng.");
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
            if (offlinePreview) { ShowStatus("Di chuyển cần máy chủ online."); return; }
            if (town == null) { ShowStatus("Chưa chọn thành trấn hợp lệ."); return; }
            if (latestState?.town?.id == town.id) { ShowStatus("Đạo hữu đang ở thành này."); return; }
            var targetMap = FindMap(town.mapId);
            if (targetMap != null && IsImmortalRealm(latestState) != targetMap.ascensionRequired)
            {
                ShowStatus("Không thể đi thẳng giữa Phàm Giới và Tiên Giới. Hãy hoàn thành điều kiện Phi Thăng.");
                return;
            }
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
            if (town == null || state?.town?.id != town.id)
            {
                ShowStatus("Hãy tới đúng thành trấn để tham gia nội dung PvE.");
                return;
            }
            ShowStatus("Đang đồng bộ map PvE và Cổ Động đang mở...");
            client.LoadMapCatalog((catalog, error) =>
            {
                if (catalog != null) mapCatalog = catalog;
                if (mapCatalog == null) { ShowStatus(error); return; }
                RenderPveTown(state, town);
            });
        }

        private void RenderPveTown(GameState state, TownInfo town)
        {
            latestState = state;
            ClearContent();
            Label("PVE  •  " + town.name, 27, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            Label($"Săn yêu thú tại thành và thám hiểm bí cảnh · cảnh giới {town.realmMinName ?? ""}", 17, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.88f));
            Button("PVP Ở THÀNH NÀY", new Vector2(0.02f, 0.755f), new Vector2(0.48f, 0.81f), Gold, () => ShowPvp(state));
            Button("BẢN ĐỒ", new Vector2(0.52f, 0.755f), new Vector2(0.98f, 0.81f), Panel, () => ShowMap(state));
            var rows = CreateScrollList("PveList", 0.20f, 0.74f);
            AddModeMapOverview(rows, state, pvp: false);
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
            ShowStatus("Đang đồng bộ map PvP...");
            client.LoadMapCatalog((catalog, error) =>
            {
                if (catalog != null) mapCatalog = catalog;
                if (mapCatalog == null) { ShowStatus(error); return; }
                ContinueShowPvp(state);
            });
        }

        private void ContinueShowPvp(GameState state)
        {
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
                AddModeMapOverview(rows, state, pvp: true);
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

        private PixelCombatPresentation CreatePixelCombatPresentation()
        {
            var go = new GameObject("PixelCombatPresentation", typeof(RectTransform), typeof(PixelCombatPresentation));
            go.transform.SetParent(content.transform, false);
            Place(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            return go.GetComponent<PixelCombatPresentation>();
        }
        private void ShowPvpBattle(PvpBattle battle)
        {
            SetAtlasOrientation(true);
            ClearContent();
            statusMin = new Vector2(0.25f, 0.245f); statusMax = new Vector2(0.75f, 0.28f);
            var view = CreatePixelCombatPresentation();
            view.BuildPvp(battle, client, latestState?.player?.appearanceColors, IsImmortalRealm(latestState), pendingBattleVisualAction,
                pendingBattleVisualSkillId, pendingBattleVisualSkillName, pendingBattleVisualSkillKind,
                (action, skillId, skillName, skillKind) => SendPvpAction(battle, action, skillId, skillName, skillKind),
                RefreshPvpBattle, LoadState, ShowPvpBattle);
            ClearPendingBattleVisual();
        }
        private void SendPvpAction(PvpBattle battle, string action, string skillId, string skillName, string skillKind)
        {
            pendingBattleVisualAction = action;
            pendingBattleVisualSkillId = skillId;
            pendingBattleVisualSkillName = skillName;
            pendingBattleVisualSkillKind = skillKind;
            GameAudioController.Instance?.PlaySkillEffect();
            client.PvpAct(battle.id, action, skillId, (updated, error) =>
            {
                if (updated == null) { ClearPendingBattleVisual(); ShowStatus(error); return; }
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
            SetAtlasOrientation(true);
            ClearContent();
            statusMin = new Vector2(0.25f, 0.245f); statusMax = new Vector2(0.75f, 0.28f);
            var view = CreatePixelCombatPresentation();
            view.BuildPve(battle, client, latestState?.player?.appearanceColors, IsImmortalRealm(latestState), pendingBattleVisualAction,
                pendingBattleVisualSkillId, pendingBattleVisualSkillName, pendingBattleVisualSkillKind,
                SendBattleAction, SendBattleSkill, NextDungeonStage, LoadState, ShowBattle);
            ClearPendingBattleVisual();
        }
        private void SendBattleAction(string action)
        {
            pendingBattleVisualAction = action;
            pendingBattleVisualSkillId = pendingBattleVisualSkillName = pendingBattleVisualSkillKind = null;
            ShowStatus("Đang gửi thao tác chiến đấu...");
            GameAudioController.Instance?.PlaySkillEffect();
            client.BattleAct(action, (result, error) =>
            {
                if (result?.battle == null) { ClearPendingBattleVisual(); ShowStatus(error); return; }
                ShowBattle(result.battle);
                if (!string.IsNullOrWhiteSpace(result.result?.msg)) ShowStatus(result.result.msg);
            });
        }

        private void SendBattleSkill(BattleSkill skill)
        {
            pendingBattleVisualAction = "skill";
            pendingBattleVisualSkillId = skill?.id;
            pendingBattleVisualSkillName = skill?.name;
            pendingBattleVisualSkillKind = skill?.kind;
            ShowStatus("Đang thi triển kỹ năng...");
            GameAudioController.Instance?.PlaySkillEffect();
            client.BattleAct("skill", skill?.i ?? 0, (result, error) =>
            {
                if (result?.battle == null) { ClearPendingBattleVisual(); ShowStatus(error); return; }
                ShowBattle(result.battle);
                if (!string.IsNullOrWhiteSpace(result.result?.msg)) ShowStatus(result.result.msg);
            });
        }

        private void ClearPendingBattleVisual()
        {
            pendingBattleVisualAction = null;
            pendingBattleVisualSkillId = pendingBattleVisualSkillName = pendingBattleVisualSkillKind = null;
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

        private void GameLogo(Vector2 min, Vector2 max)
        {
            var texture = Resources.Load<Texture2D>("Brand/TuTienGioi_Logo");
            if (texture == null)
            {
                Label("TU TIÊN GIỚI", 36, Gold, TextAnchor.MiddleCenter, min, max);
                return;
            }

            var obj = new GameObject("TuTienGioiLogo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            obj.transform.SetParent(content.transform, false);
            Place(obj.GetComponent<RectTransform>(), min, max);
            var image = obj.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            var aspect = obj.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)texture.width / texture.height;
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

        private Text ChoiceSelector(string label, string[] choices, Vector2 min, Vector2 max, Action<int> selected, int initialIndex = 0)
        {
            var root = PanelObject(label, content.transform, min, max, Vector2.zero, Vector2.zero, Panel);
            var button = root.AddComponent<Button>();
            var text = ChildText(root.transform, "Selected", 20, Cream, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            var choiceIndex = choices == null || choices.Length == 0 ? 0 : Mathf.Clamp(initialIndex, 0, choices.Length - 1);
            text.text = choices != null && choices.Length > 0 ? $"{label}:  {choices[0]}   ‹ Chọn ›" : $"{label}: chưa có dữ liệu";
            if (choices != null && choices.Length > 0) text.text = $"{label}:  {choices[choiceIndex]}   ‹ Chọn ›";
            button.onClick.AddListener(() =>
            {
                if (choices == null || choices.Length == 0) return;
                choiceIndex = (choiceIndex + 1) % choices.Length;
                text.text = $"{label}:  {choices[choiceIndex]}   ‹ Chọn ›";
                selected?.Invoke(choiceIndex);
            });
            return text;
        }

        private Button Button(string label, Vector2 min, Vector2 max, Color color, Action click, Transform parent = null)
        {
            var root = PanelObject("Button_" + label, parent ?? content.transform, min, max, Vector2.zero, Vector2.zero, color);
            var button = root.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = color; colors.highlightedColor = new Color(1f, 0.9f, 0.68f); colors.pressedColor = Gold; button.colors = colors;
            var text = ChildText(root.transform, "Text", buttonFontSize, color == Gold ? Ink : Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
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

    internal sealed class ExplorationMapTouch : MonoBehaviour, IPointerClickHandler, IDragHandler
    {
        private Action<Vector2> onTap;
        private Action<Vector2> onDrag;

        public void Initialize(Action<Vector2> tap, Action<Vector2> drag)
        {
            onTap = tap;
            onDrag = drag;
            var image = GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onTap?.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            onDrag?.Invoke(eventData.delta);
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
