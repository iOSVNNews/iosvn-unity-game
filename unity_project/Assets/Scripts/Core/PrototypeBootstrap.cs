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
    public sealed partial class PrototypeBootstrap : MonoBehaviour
    {
        private enum WorldPointKind { Town, Dungeon, MonsterZone }
        private sealed class WorldMapPoint
        {
            public WorldPointKind kind;
            public string title;
            public TownInfo town;
            public DungeonInfo dungeon;
            public string monsterId;
            public Vector2Int cell;
        }

        private sealed class RoamingMonsterActor
        {
            public WorldMapPoint point;
            public Vector2Int homeCell;
            public RectTransform marker;
            public Text nameplate;
            public Vector2 moveStart;
            public Vector2 moveEnd;
            public float moveStartedAt;
            public float moveDuration;
            public float nextMoveAt;
            public float respawnAt;
            public bool defeated;
        }

        [Serializable] private sealed class OfflineInventoryStack { public string id; public int quantity; }
        [Serializable] private sealed class OfflineProgressSave { public int hp = 240; public int kills; public int stones = 30000; public int realmIndex; public int experience; public string currentTownId; public string monClass = "kiem"; public List<OfflineInventoryStack> items = new List<OfflineInventoryStack>(); }

        private static readonly Color Ink = new Color32(13, 18, 27, 255);
        private static readonly Color Panel = new Color32(25, 33, 43, 255);
        private static readonly Color Gold = new Color32(225, 185, 104, 255);
        private static readonly Color Cream = new Color32(239, 228, 203, 255);
        private static readonly Color Muted = new Color32(158, 171, 184, 255);
        private const string OfflineProgressKey = "tutien.offline.progress.v1";
        private static readonly Dictionary<string, Sprite> PixelIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> CultivatorSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private NetworkGameClient client;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private Transform backgroundRoot;
        private GameObject atlasMapRoot;
        private GameObject explorationMapRoot;
        private RectTransform explorationViewport;
        private RectTransform explorationMapRect;
        private RawImage explorationMiniMap;
        private RectTransform explorationMiniPlayer;
        private Image explorationPlayerMarker;
        private Text explorationLocationText;
        private Text explorationPoiText;
        private Button explorationActionButton;
        private RectTransform explorationMiniPlayerRect;
        private WorldMapPoint explorationSelectedPoint;
        private MapInfo explorationMap;
        private Texture2D explorationTexture;
        private bool[,] explorationBlocked;
        private readonly List<WorldMapPoint> explorationPoints = new List<WorldMapPoint>();
        private readonly List<RoamingMonsterActor> roamingMonsters = new List<RoamingMonsterActor>();
        private RoamingMonsterActor selectedRoamingMonster;
        private RoamingMonsterActor activeRoamingMonster;
        private Vector2Int explorationCell;
        private Vector2Int explorationMoveTarget;
        private Coroutine explorationMovement;
        private float explorationZoom = 1f;
        private bool atlasFromExploration;
        private const int ExplorationMapWidth = 144;
        private const int ExplorationMapHeight = 64;
        private const int ExplorationTilePixels = 16;
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
        private InputField passwordConfirmationInput;
        private GameObject authBackdrop;
        private readonly List<Selectable> authControls = new List<Selectable>();
        private Button authPrimaryButton;
        private bool authRequestPending;
        private int authScreenVersion;
        private InputField verificationCodeInput;
        private string pendingVerificationEmail;
        private bool offlinePreview;
        private GameState offlinePreviewState;
        private bool offlineCreationPreview;
        private OfflineHuntCatalogData offlineHuntCatalog;
        private OfflineProgressSave offlineProgress = new OfflineProgressSave();
        private OfflineMonsterData activeOfflineMonster;
        private OfflineSkillData activeOfflineSkill;
        private GameObject offlineBattleRoot;
        private Texture2D offlineBattleTexture;
        private GameObject offlineInventoryRoot;
        private RectTransform offlinePlayerFighter;
        private RectTransform offlineMonsterFighter;
        private Image offlineMonsterImage;
        private Image offlinePlayerHealthFill;
        private Image offlineMonsterHealthFill;
        private Text offlineBattleMessage;
        private Text offlineBattleTitle;
        private bool offlineActionRunning;
        private bool offlineProgressLoaded;
        private Button offlineSkillButton;
        private Image offlineSkillIcon;
        private Button offlineSkillCycleButton;
        private readonly List<OfflineSkillData> offlineBattleSkills = new List<OfflineSkillData>();
        private int offlineBattleSkillIndex;
        private Vector2 offlineBattleMoveInput;
        private Vector2 offlinePlayerBattlePosition;
        private float offlineNextEnemyAttackTime;
        private float offlineBattleMotionTime;
        private float offlineEnemyStunnedUntil;
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
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            client = NetworkGameClient.Instance;
            if (client == null) client = new GameObject("NetworkGameClient").AddComponent<NetworkGameClient>();
            if (GameAudioController.Instance == null) new GameObject("GameAudioController").AddComponent<GameAudioController>();
            BuildCanvas();
            StartStartupPatchCheck();
        }

        private void Update()
        {
            if (offlinePreview && offlineBattleRoot == null) UpdateRoamingMonsters();
            if (offlineBattleRoot == null || offlinePlayerFighter == null || offlineMonsterFighter == null || offlineBattleOver) return;
            var bounds = offlineBattleRoot.GetComponent<RectTransform>().rect;
            var playerPosition = offlinePlayerBattlePosition;
            if (offlineBattleMoveInput.sqrMagnitude > .01f && !offlineActionRunning)
            {
                playerPosition += offlineBattleMoveInput.normalized * (330f * Time.deltaTime);
                playerPosition.x = Mathf.Clamp(playerPosition.x, -bounds.width * .45f, bounds.width * .45f);
                playerPosition.y = Mathf.Clamp(playerPosition.y, -bounds.height * .31f, bounds.height * .32f);
                offlinePlayerBattlePosition = playerPosition;
            }
            offlinePlayerFighter.localPosition = playerPosition + Vector2.up * Mathf.Sin(offlineBattleMotionTime * 7f) * 3f;

            offlineBattleMotionTime += Time.deltaTime;
            var enemyPosition = offlineMonsterFighter.localPosition;
            var distance = Vector2.Distance(playerPosition, enemyPosition);
            if (!offlineActionRunning)
            {
                var orbit = new Vector2(Mathf.Sin(offlineBattleMotionTime * 2.1f), Mathf.Cos(offlineBattleMotionTime * 1.7f)) * 30f;
                var target = distance > 205f ? playerPosition : playerPosition + orbit;
                var speed = distance > 205f ? 125f : 86f;
                enemyPosition = Vector2.MoveTowards(enemyPosition, target, speed * Time.deltaTime);
                enemyPosition.x = Mathf.Clamp(enemyPosition.x, -bounds.width * .45f, bounds.width * .45f);
                enemyPosition.y = Mathf.Clamp(enemyPosition.y, -bounds.height * .31f, bounds.height * .32f);
                offlineMonsterFighter.localPosition = enemyPosition;

                if (distance < 178f && Time.time >= offlineNextEnemyAttackTime && Time.time >= offlineEnemyStunnedUntil)
                {
                    offlineNextEnemyAttackTime = Time.time + 1.15f;
                        var damage = OfflineMonsterAttackDamage(activeOfflineMonster);
                    offlineProgress.hp = Mathf.Max(0, offlineProgress.hp - damage);
                    offlineMonsterImage.color = new Color32(255, 137, 112, 255);
                    StartCoroutine(ResetMonsterHitFlash());
                    offlineBattleMessage.text = activeOfflineMonster.name + " áp sát phản kích · mất " + damage + " khí huyết.";
                    if (offlineProgress.hp <= 0)
                    {
                        offlineProgress.hp = OfflinePlayerMaxHp() / 2;
                        offlineBattleOver = true;
                        offlineBattleTitle.text = "TRỌNG THƯƠNG  ·  ĐƯỢC CỨU VỀ THÀNH";
                        offlineBattleMessage.text = "Chưa nhận được chiến lợi phẩm. Khí huyết đã hồi một nửa.";
                        SetOfflineButtonLabel(offlineLeaveButton, "HỒI THÀNH  ·  VỀ MAP");
                    }
                    SaveOfflineProgress();
                    ShowOfflineBattleVitals();
                }
            }

        }

        private IEnumerator ResetMonsterHitFlash()
        {
            yield return new WaitForSeconds(.12f);
            if (offlineMonsterImage != null) offlineMonsterImage.color = Color.white;
        }

        private void SetOfflineBattleMove(Vector2 direction) => offlineBattleMoveInput = direction;

        private Button BattleMoveButton(string label, Vector2 min, Vector2 max, Vector2 direction)
        {
            var button = Button(label, min, max, new Color32(27, 35, 42, 232), () => { }, offlineBattleRoot.transform);
            var trigger = button.gameObject.AddComponent<EventTrigger>();
            AddBattlePointerEvent(trigger, EventTriggerType.PointerDown, () => SetOfflineBattleMove(direction));
            AddBattlePointerEvent(trigger, EventTriggerType.PointerUp, () => SetOfflineBattleMove(Vector2.zero));
            AddBattlePointerEvent(trigger, EventTriggerType.PointerExit, () => SetOfflineBattleMove(Vector2.zero));
            return button;
        }

        private static void AddBattlePointerEvent(EventTrigger trigger, EventTriggerType eventType, Action callback)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(_ => callback?.Invoke());
            trigger.triggers.Add(entry);
        }

        private bool majorUpdateShowing;

        private void StartStartupPatchCheck()
        {
            patchStatus = Label("Đang chuẩn bị tài nguyên...", 18, Muted, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.45f), new Vector2(0.96f, 0.55f));
            var patcher = AssetDownloadManager.Instance;
            if (patcher == null) patcher = new GameObject("AssetDownloadManager").AddComponent<AssetDownloadManager>();
            var config = Resources.Load<GameServerConfig>("GameServerConfig");
            patcher.Configure(config?.assetCdnBaseUrl, config?.apiBaseUrl);
            patcher.OnStatusMessage.AddListener(message =>
            {
                if (patchStatus != null) patchStatus.text = message;
            });
            patcher.OnDownloadProgress.AddListener((_, progress) =>
            {
                if (patchStatus != null && !string.IsNullOrEmpty(progress)) patchStatus.text = "Tự động tải cập nhật: " + progress;
            });
            patcher.OnMajorUpdateRequired.AddListener(info =>
            {
                ShowMajorUpdateDialog(info);
            });
            patcher.StartPatchCheck((success, message) =>
            {
                if (!majorUpdateShowing)
                    ShowLogin(success ? message : "Không cập nhật được tài nguyên: " + message);
            });
        }

        private void ShowMajorUpdateDialog(AssetDownloadManager.MajorUpdateInfo info)
        {
            majorUpdateShowing = true;
            ClearContent();
            var dialog = PanelObject("MajorUpdateDialog", content.transform, new Vector2(0.20f, 0.12f), new Vector2(0.80f, 0.88f), Vector2.zero, Vector2.zero, new Color32(18, 24, 32, 252));
            ModernUi.Fill(dialog.GetComponent<Image>(), 24f);

            var title = ChildText(dialog.transform, "Title", 34, Gold, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));
            title.text = string.IsNullOrEmpty(info.updateTitle) ? "PHÁT HIỆN BẢN CẬP NHẬT MỚI" : info.updateTitle;
            title.fontStyle = FontStyle.Bold;

            var ver = ChildText(dialog.transform, "Version", 22, new Color32(140, 200, 255, 255), TextAnchor.MiddleCenter, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.86f));
            ver.text = $"Phiên bản mới: {info.appVersion}  ·  Hiện tại: {Application.version}";

            var scrollPanel = PanelObject("UpdateNotesScroll", dialog.transform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.76f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.45f));
            var notesText = ChildText(scrollPanel.transform, "Notes", 21, Cream, TextAnchor.UpperLeft, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            notesText.text = string.IsNullOrEmpty(info.updateNotes) ? "Đã có bản cài đặt mới. Vui lòng tải về để tiếp tục." : info.updateNotes;

            var downloadBtn = Button("TẢI BẢN MỚI NGAY", new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.24f), Gold, () =>
            {
                if (!string.IsNullOrEmpty(info.packageUrl))
                {
                    Application.OpenURL(info.packageUrl);
                }
            }, parent: dialog.transform);

            if (!info.forceUpdate)
            {
                Button("Để sau", new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f), Panel, () =>
                {
                    majorUpdateShowing = false;
                    Destroy(dialog);
                    ShowLogin();
                }, parent: dialog.transform);
            }
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler = scaler;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var background = PanelObject("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Ink);
            backgroundRoot = background.transform;
            var bgImg = background.GetComponent<Image>();
            if (bgImg != null) bgImg.raycastTarget = false;
            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeArea.transform.SetParent(background.transform, false);
            Place(safeArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            content = PanelObject("Content", safeArea.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            var contentImg = content.GetComponent<Image>();
            if (contentImg != null) contentImg.raycastTarget = false;
        }

        private void ShowLogin(string patchMessage = null)
        {
            ShowAccountForm(false);
        }

        private void PrepareAccountScreen()
        {
            ClearContent();
            authBackdrop = LoginBackdrop.Create(backgroundRoot, Resources.Load<Texture2D>("Brand/LoginLandscapePixel"));
            GameLogo(new Vector2(.005f, .77f), new Vector2(.125f, .98f));
            var tagline = Label("PHÀM GIỚI  ·  TIÊN GIỚI", 17, new Color32(240, 228, 204, 220), TextAnchor.MiddleCenter, new Vector2(.005f, .725f), new Vector2(.125f, .765f));
            tagline.font = ModernUi.SemiBold;
            tagline.horizontalOverflow = HorizontalWrapMode.Overflow;
            var taglineOutline = tagline.GetComponent<Outline>();
            if (taglineOutline != null) taglineOutline.enabled = false;
            var taglineShadow = tagline.gameObject.AddComponent<Shadow>();
            taglineShadow.effectColor = new Color(0f, 0f, 0f, .65f);
            taglineShadow.effectDistance = new Vector2(0f, -2f);
        }

        private void AuthControl(Selectable control) => authControls.Add(control);

        private void SetAuthBusy(bool busy)
        {
            authRequestPending = busy;
            foreach (var control in authControls) if (control != null) control.interactable = !busy;
            if (authPrimarySpinner != null) authPrimarySpinner.gameObject.SetActive(busy);
            if (authPrimaryArrow != null) authPrimaryArrow.gameObject.SetActive(!busy);
        }

        private void AuthFeedback(string message, bool error = false)
        {
            ShowStatus(message);
            if (status != null) status.color = error ? AuthError : AuthTextSecondary;
            if (error) UiShake.Play(authCardRoot);
        }

        /// <summary>Puts the caret in a field after a failed check. On a phone this would reopen the keyboard that just closed, so the red frame alone marks the field there.</summary>
        private static void FocusAuthInput(InputField field)
        {
            if (field != null && !TouchScreenKeyboard.isSupported) field.ActivateInputField();
        }

        private void SubmitAuth(bool createAccount)
        {
            if (authRequestPending) return;
            var email = emailInput.text.Trim();
            var validIdentity = email.Contains("@")
                ? email.Length <= 254 && System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")
                : System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9][a-zA-Z0-9_.]{2,23}$");
            if (!validIdentity)
            {
                FlagAuthInput(emailInput);
                AuthFeedback("Nhập tên tài khoản 3–24 ký tự không dấu hoặc email hợp lệ.", true);
                FocusAuthInput(emailInput);
                return;
            }
            if (passwordInput.text.Length < 10 || passwordInput.text.Length > 128)
            {
                FlagAuthInput(passwordInput);
                AuthFeedback("Mật khẩu cần có từ 10 đến 128 ký tự.", true);
                FocusAuthInput(passwordInput);
                return;
            }
            if (createAccount && passwordConfirmationInput.text != passwordInput.text)
            {
                FlagAuthInput(passwordConfirmationInput);
                AuthFeedback("Mật khẩu nhập lại chưa khớp.", true);
                FocusAuthInput(passwordConfirmationInput);
                return;
            }
            var screenVersion = authScreenVersion;
            SetAuthBusy(true);
            AuthFeedback(createAccount ? "Đang tạo tài khoản..." : "Đang đăng nhập...");
            Action<ApiResult> finish = result =>
            {
                if (screenVersion != authScreenVersion) return;
                if (result?.verificationRequired == true)
                {
                    ShowEmailVerification(result.email ?? email, result.message ?? result.error);
                    return;
                }
                if (result == null || !result.ok)
                {
                    SetAuthBusy(false);
                    var detail = result?.error;
                    if (string.IsNullOrWhiteSpace(detail) || detail.Contains("Not found"))
                        detail = "Server game IPA chưa bật API đăng nhập email.";
                    AuthFeedback(detail, true);
                    return;
                }
                if (string.IsNullOrWhiteSpace(result.accessToken))
                {
                    SetAuthBusy(false);
                    AuthFeedback("Máy chủ chưa trả phiên đăng nhập. Hãy thử lại.", true);
                    return;
                }
                LoadState();
            };
            if (createAccount) client.SignUp(email, passwordInput.text, finish);
            else client.Login(email, passwordInput.text, finish);
        }

        private void SubmitEmailVerification()
        {
            if (authRequestPending) return;
            if (string.IsNullOrWhiteSpace(pendingVerificationEmail) || !System.Text.RegularExpressions.Regex.IsMatch(verificationCodeInput?.text ?? "", @"^\d{6}$"))
            {
                FlagAuthInput(verificationCodeInput);
                AuthFeedback("Nhập đầy đủ mã 6 số trong email.", true);
                return;
            }
            var screenVersion = authScreenVersion;
            SetAuthBusy(true);
            AuthFeedback("Đang xác minh email...");
            client.VerifyEmail(pendingVerificationEmail, verificationCodeInput.text.Trim(), result =>
            {
                if (screenVersion != authScreenVersion) return;
                if (result == null || !result.ok) { SetAuthBusy(false); AuthFeedback(result?.error ?? "Không xác minh được email.", true); return; }
                pendingVerificationEmail = null;
                LoadState();
            });
        }

        private void ResendEmailVerification()
        {
            if (authRequestPending) return;
            if (string.IsNullOrWhiteSpace(pendingVerificationEmail)) { ShowLogin(); return; }
            var screenVersion = authScreenVersion;
            SetAuthBusy(true);
            AuthFeedback("Đang gửi lại mã xác minh...");
            client.ResendEmailVerification(pendingVerificationEmail, result =>
            {
                if (screenVersion != authScreenVersion) return;
                SetAuthBusy(false);
                AuthFeedback(result?.message ?? result?.error ?? "Không gửi được mã. Hãy thử lại.", result?.ok != true);
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
            offlineHuntCatalog = OfflineHuntCatalogData.Load();
            if (offlineHuntCatalog == null || offlineHuntCatalog.monsters == null || offlineHuntCatalog.monsters.Length == 0)
            {
                ShowStatus("Thiếu dữ liệu quái và vật phẩm ngoại tuyến trong bản cài.");
                return;
            }
            LoadOfflineProgress();
            var town = FindOfflineTown(offlineProgress.currentTownId) ?? FirstTownInAtlas(false);
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
                realm = new RealmInfo { index = offlineProgress.realmIndex, name = OfflineRealmName(offlineProgress.realmIndex) },
                player = new PlayerInfo { userId = "offline-preview", name = "Đạo hữu", fullName = "Đạo hữu · Ngoại tuyến", ascended = offlineProgress.realmIndex >= 11 },
                worldMonsters = Array.Empty<WorldMonster>()
            };
            offlineProgress.currentTownId = town.id;
            SaveOfflineProgress();
            latestState = offlinePreviewState;
            atlasRealmInitialized = true;
            atlasImmortalRealm = offlinePreviewState.player.ascended;
            atlasSelectedTown = town;
            atlasSelectedDungeon = null;
            atlasSelectionKind = "town";
            atlasInfoExpanded = false;
            SetRealmMusic(offlinePreviewState);
            SetAtlasOrientation(true);
            ShowMap(offlinePreviewState);
            ShowStatus($"Chơi thử ngoại tuyến · {offlineHuntCatalog.sourceMapName} · {offlineProgress.kills} trận thắng · túi đồ lưu trên máy.");
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
            ShowLoadingVeil("Đang tải hồ sơ từ máy chủ...");
            client.LoadStateBoth((state, raw, error) =>
            {
                if (state == null) { ShowLoadError(error ?? "Không đọc được hồ sơ.", LoadState); return; }
                hub = raw;
                currentCatalog = state.catalog;
                latestState = state;
                SetRealmMusic(state);
                if (!state.registered) { ShowCharacterCreation(); return; }
                client.LoadCurrentBattle((battle, _) =>
                {
                    // JsonUtility turns "battle": null into an empty object: only a battle with an id is a fight in progress
                    if (battle != null && !string.IsNullOrEmpty(battle.id) && !battle.over)
                    {
                        try { ShowBattle(battle); return; }
                        catch (Exception ex) { Debug.LogException(ex); }
                    }
                    client.LoadPvpBattle((pvpBattle, __) =>
                    {
                        if (pvpBattle != null && !string.IsNullOrEmpty(pvpBattle.id) && !pvpBattle.none && !pvpBattle.over)
                        {
                            try { ShowPvpBattle(pvpBattle); return; }
                            catch (Exception ex) { Debug.LogException(ex); }
                        }
                        SafeShowWorld();
                    });
                });
            });
        }

        /// <summary>Never leave the player on a blank screen: fall back to the classic home on any error.</summary>
        private void SafeShowWorld()
        {
            try { ShowWorld(); }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ClearBattleScene();
                if (latestState != null) ShowHome(latestState);
                ShowStatus("Không mở được bản đồ: " + ex.Message);
            }
        }

        private void ShowLoadingVeil(string message)
        {
            if (content == null) return;
            var veil = new GameObject("LoadingVeil", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            veil.SetParent(content.transform, false);
            veil.anchorMin = new Vector2(-.1f, -.1f); veil.anchorMax = new Vector2(1.1f, 1.1f); veil.offsetMin = veil.offsetMax = Vector2.zero;
            veil.GetComponent<Image>().color = new Color(0, 0, 0, .45f);
            var spinner = new GameObject("Spinner", typeof(RectTransform), typeof(Image), typeof(UiSpinner)).GetComponent<RectTransform>();
            spinner.SetParent(veil, false);
            spinner.sizeDelta = new Vector2(72, 72);
            var image = spinner.GetComponent<Image>();
            image.sprite = ModernUi.Icon("spinner");
            image.color = AuthGoldAccent;
            image.raycastTarget = false;
        }

        private void ShowLoadError(string message, Action retry)
        {
            ClearContent();
            Label(message, 26, Cream, TextAnchor.MiddleCenter, new Vector2(.1f, .5f), new Vector2(.9f, .65f));
            Button("THỬ LẠI", new Vector2(.35f, .36f), new Vector2(.65f, .46f), Gold, () => retry?.Invoke());
            Button("ĐĂNG XUẤT", new Vector2(.35f, .24f), new Vector2(.65f, .33f), Panel, () => client.Logout(_ => ShowLogin()));
        }

        private void ShowCharacterCreation()
        {
            SetAtlasOrientation(false);
            offlineCreationPreview = false;
            ShowCreator();
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
            if (creatorLook != null) gender = creatorLook.Get("g", "m") == "f" ? "nu" : "nam";
            var choice = new RegisterChoice
            {
                name = nameInput.text.Trim(),
                gender = gender,
                mon = currentCatalog.mon[Mathf.Clamp(sectIndex, 0, currentCatalog.mon.Length - 1)].id,
                he = currentCatalog.he[Mathf.Clamp(elementIndex, 0, currentCatalog.he.Length - 1)].id,
                appearance = AppearanceId(),
                talents = new List<string>(selectedTalents).ToArray(),
                look = creatorLook?.ToString()
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
                if (state == null) { SetAuthBusy(false); ShowStatus(error); return; }
                offlineCreationPreview = false;
                SetAtlasOrientation(false);
                latestState = state;
                RefreshHub(SafeShowWorld);
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
            var playerName = string.IsNullOrEmpty(state.player?.fullName) ? state.player?.name : state.player.fullName;
            statusMin = new Vector2(.34f, .115f); statusMax = new Vector2(.98f, .15f);

            var header = PanelObject("HomeHeader", content.transform, new Vector2(.02f, .855f), new Vector2(.98f, .98f), Vector2.zero, Vector2.zero, new Color32(18, 25, 33, 248));
            ChildText(header.transform, "GameMark", 15, Gold, TextAnchor.MiddleLeft, new Vector2(.025f, .55f), new Vector2(.27f, .94f)).text = "TU TIÊN GIỚI  ·  CỬU CHÂU";
            ChildText(header.transform, "Welcome", 23, Cream, TextAnchor.MiddleLeft, new Vector2(.025f, .06f), new Vector2(.56f, .60f)).text = playerName ?? "Đạo hữu";
            ChildText(header.transform, "HeaderResources", 16, Gold, TextAnchor.MiddleRight, new Vector2(.58f, .12f), new Vector2(.975f, .88f)).text =
                $"{state.realm?.name ?? "Sơ nhập"}     ·     {state.town?.name ?? "Chưa rõ thành"}     ·     {Math.Max(0, state.player?.stones ?? 0):N0} LINH THẠCH";
            ((RectTransform)header.transform.Find("HeaderResources")).anchorMax = new Vector2(.94f, .88f);
            PlacePixelIcon(header.transform, "coin", new Vector2(.945f, .22f), new Vector2(.975f, .78f));

            var profile = PanelObject("HomeProfileCard", content.transform, new Vector2(.02f, .185f), new Vector2(.315f, .835f), Vector2.zero, Vector2.zero, new Color32(23, 29, 36, 255));
            PanelObject("HomePortraitFrame", profile.transform, new Vector2(.25f, .40f), new Vector2(.75f, .94f), Vector2.zero, Vector2.zero, new Color32(71, 57, 40, 255));
            var portrait = new GameObject("HomePortrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(profile.transform, false);
            Place(portrait.GetComponent<RectTransform>(), new Vector2(.275f, .425f), new Vector2(.725f, .915f));
            var portraitImage = portrait.GetComponent<Image>();
            portraitImage.sprite = CreateCultivatorSprite(state.player?.appearanceColors);
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            ChildText(profile.transform, "ProfileName", 20, Cream, TextAnchor.MiddleCenter, new Vector2(.05f, .32f), new Vector2(.95f, .41f)).text =
                string.IsNullOrWhiteSpace(state.player?.name) ? "Đạo hữu" : state.player.name;
            ChildText(profile.transform, "ProfileSect", 15, Muted, TextAnchor.MiddleCenter, new Vector2(.05f, .25f), new Vector2(.95f, .33f)).text =
                $"{state.player?.monName ?? "Tán tu"}  ·  {state.player?.heName ?? "Linh căn chưa rõ"}";
            var hp = Math.Max(0L, state.player?.hp ?? 0L);
            var maxHp = Math.Max(0L, state.player?.maxHp ?? 0L);
            var hpTrack = PanelObject("HomeHealthTrack", profile.transform, new Vector2(.08f, .18f), new Vector2(.92f, .23f), Vector2.zero, Vector2.zero, new Color32(37, 39, 40, 255));
            var hpFill = PanelObject("HomeHealthFill", hpTrack.transform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2), new Color32(79, 178, 104, 255)).GetComponent<Image>();
            hpFill.type = Image.Type.Filled; hpFill.fillMethod = Image.FillMethod.Horizontal; hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.fillAmount = maxHp == 0 ? 0f : Mathf.Clamp01((float)hp / maxHp);
            ChildText(profile.transform, "ProfileVitals", 14, Cream, TextAnchor.MiddleCenter, new Vector2(.05f, .12f), new Vector2(.95f, .18f)).text =
                maxHp == 0 ? "KHÍ HUYẾT  ·  CHƯA CÓ DỮ LIỆU" : $"KHÍ HUYẾT  ·  {hp:N0} / {maxHp:N0}";
            PlacePixelIcon(profile.transform, "heart", new Vector2(.02f, .17f), new Vector2(.075f, .24f));
            AddButtonPixelIcon(Button("DANH HIỆU", new Vector2(.07f, .035f), new Vector2(.93f, .105f), Panel, () => ShowTitles(state), profile.transform), "power");

            AddButtonPixelIcon(Button("BẢN ĐỒ", new Vector2(.34f, .785f), new Vector2(.55f, .84f), Panel, () => ShowMap(state)), "road");
            AddButtonPixelIcon(Button("LÔI ĐÀI  ·  PVP", new Vector2(.565f, .785f), new Vector2(.765f, .84f), Gold, () => ShowPvp(state)), "swords");
            AddButtonPixelIcon(Button("TRUY TUNG  ·  PVE", new Vector2(.78f, .785f), new Vector2(.98f, .84f), Panel, RefreshMonsters), "san_yeu");
            Label("YÊU THÚ QUANH THÀNH", 19, Gold, TextAnchor.MiddleLeft, new Vector2(.35f, .735f), new Vector2(.77f, .78f));
            Button("LÀM MỚI", new Vector2(.82f, .735f), new Vector2(.98f, .78f), Panel, RefreshMonsters);
            var scrollContent = CreateScrollList("MonsterList", 0.175f, 0.725f, .34f);
            if (state.worldMonsters != null) AddMonsterCards(state.worldMonsters, scrollContent);
            Button("BÍ CẢNH", new Vector2(.34f, .035f), new Vector2(.60f, .105f), Panel, () => ShowPveTown(state, state.town));
            AddButtonPixelIcon(Button("TÀI KHOẢN", new Vector2(.62f, .035f), new Vector2(.80f, .105f), Panel, () => ShowAccountLinks(state)), "ho_so");
            Button("ĐĂNG XUẤT", new Vector2(.82f, .035f), new Vector2(.98f, .105f), Panel, () => client.Logout(_ => ShowLogin()));
            ShowStatus("Hồ sơ và mục tiêu được đồng bộ với máy chủ.");
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
            roamingMonsters.Clear();
            selectedRoamingMonster = null;
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
            explorationLocationText = Label("", 16, Cream, TextAnchor.MiddleRight, new Vector2(.55f, .08f), new Vector2(.70f, .92f), top.transform);
            if (offlinePreview)
                Button("TÚI ĐỒ  " + OfflineInventoryCount(), new Vector2(.71f, .06f), new Vector2(.84f, .94f), new Color32(45, 61, 56, 255), ShowOfflineInventory, top.transform);
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
            explorationPoiText.text = offlinePreview
                ? $"{offlineProgress.hp}/{OfflinePlayerMaxHp()} KHÍ HUYẾT  ·  CẢNH GIỚI {offlineProgress.realmIndex} · {offlineProgress.experience:N0} LINH LỰC  ·  {offlineProgress.stones:N0} LINH THẠCH\n{offlineProgress.kills} trận thắng · Yêu thú đang tuần quanh bãi săn."
                : "Chạm bản đồ để nhân vật đi theo đường mòn; chọn thành trấn, cổ động hoặc bãi yêu để xem hoạt động.";

            Button("↑", new Vector2(.075f, .135f), new Vector2(.125f, .195f), Panel, () => MoveExplorationBy(Vector2Int.up));
            Button("←", new Vector2(.025f, .075f), new Vector2(.075f, .135f), Panel, () => MoveExplorationBy(Vector2Int.left));
            Button("↓", new Vector2(.075f, .075f), new Vector2(.125f, .135f), Panel, () => MoveExplorationBy(Vector2Int.down));
            Button("→", new Vector2(.125f, .075f), new Vector2(.175f, .135f), Panel, () => MoveExplorationBy(Vector2Int.right));
            explorationActionButton = Button(offlinePreview ? "CHỌN MỤC TIÊU" : "THÀNH / ĐIỂM ĐẾN", new Vector2(.77f, .075f), new Vector2(.98f, .145f), Gold, () => ActivateSelectedExplorationPoint(state));
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
            if (offlinePreview) BuildRoamingMonsters();
            UpdateExplorationLabels();
            ShowStatus(offlinePreview ? "Thế giới ngoại tuyến · yêu thú tuần tra và áp sát · tiến độ lưu trên máy." : "Bản đồ đi lại · vị trí nhân vật được đồng bộ với hồ sơ online.");
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
            // The province is an ink-and-paper map, not a tiled green minimap.
            // Keep the water-like regions in muted paper gray to match the game's scroll atlas.
            var land = ordinal >= 9 ? new Color32(190, 194, 185, 255) : ordinal == 6 ? new Color32(179, 149, 117, 255) : new Color32(197, 180, 145, 255);
            var mountain = ordinal >= 9 ? new Color32(113, 123, 124, 255) : ordinal == 6 ? new Color32(105, 74, 57, 255) : new Color32(117, 96, 72, 255);
            var water = ordinal == 4 ? new Color32(157, 159, 151, 255) : ordinal >= 9 ? new Color32(162, 174, 173, 255) : new Color32(166, 159, 145, 255);
            var shore = new Color32(221, 207, 174, 255);
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
                var variation = .95f + mountainNoise[x, y] * .10f;
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
                    var monsterId = town.monsterPool[(town.id.GetHashCode() & 0x7fffffff) % town.monsterPool.Length];
                    var monsterName = FindOfflineMonster(monsterId)?.name;
                    if (string.IsNullOrEmpty(monsterName))
                        foreach (var candidate in mapCatalog.monsters ?? Array.Empty<MonsterInfo>())
                            if (candidate != null && candidate.id == monsterId) { monsterName = candidate.name; break; }
                    explorationPoints.Add(new WorldMapPoint { kind = WorldPointKind.MonsterZone, title = "Bãi tiểu yêu · " + (monsterName ?? town.name), town = town, monsterId = monsterId, cell = zoneCell });
                }
            }
        }

        private void BuildRoamingMonsters()
        {
            roamingMonsters.Clear();
            var occupied = new HashSet<int>();
            foreach (var point in explorationPoints)
                if (point != null) occupied.Add(point.cell.y * ExplorationMapWidth + point.cell.x);
            var serial = 0;
            foreach (var zone in explorationPoints)
            {
                if (zone.kind != WorldPointKind.MonsterZone || zone.town?.monsterPool == null) continue;
                var candidates = new List<OfflineMonsterData>();
                foreach (var id in zone.town.monsterPool)
                {
                    var monster = FindOfflineMonster(id);
                    if (monster == null) continue;
                    if (monster.realm <= offlineProgress.realmIndex + 1) candidates.Add(monster);
                }
                if (candidates.Count == 0)
                    foreach (var id in zone.town.monsterPool)
                    {
                        var monster = FindOfflineMonster(id);
                        if (monster != null) candidates.Add(monster);
                    }
                if (candidates.Count == 0) continue;
                var count = Mathf.Min(3, candidates.Count);
                var offset = (int)(Math.Abs((long)zone.town.id.GetHashCode()) % candidates.Count);
                for (var i = 0; i < count; i++)
                {
                    var monster = candidates[(offset + i) % candidates.Count];
                    var cell = FindOpenRoamingCell(zone.cell, occupied);
                    occupied.Add(cell.y * ExplorationMapWidth + cell.x);
                    var actorPoint = new WorldMapPoint
                    {
                        kind = WorldPointKind.MonsterZone, title = monster.name,
                        town = zone.town, monsterId = monster.id, cell = cell,
                    };
                    var actor = new RoamingMonsterActor { point = actorPoint, homeCell = cell, nextMoveAt = Time.time + 1.5f + i * .45f };
                    var marker = new GameObject("RoamingMonster_" + monster.id + "_" + serial++, typeof(RectTransform), typeof(Image), typeof(Button));
                    marker.transform.SetParent(explorationMapRect, false);
                    actor.marker = marker.GetComponent<RectTransform>();
                    actor.marker.anchorMin = actor.marker.anchorMax = new Vector2(.5f, .5f);
                    actor.marker.sizeDelta = monster.worldBoss ? new Vector2(58, 58) : new Vector2(42, 42);
                    actor.marker.anchoredPosition = ExplorationCellPosition(cell) + new Vector2(0f, 7f);
                    var image = marker.GetComponent<Image>();
                    image.sprite = LoadPixelIcon("PixelArt/Monsters/" + monster.id) ?? AtlasPixelSprite("Y");
                    image.preserveAspect = true;
                    var button = marker.GetComponent<Button>();
                    button.transition = Selectable.Transition.ColorTint;
                    button.onClick.AddListener(() => SelectRoamingMonster(actor));
                    var nameObject = new GameObject("Nameplate", typeof(RectTransform), typeof(Text), typeof(Outline));
                    nameObject.transform.SetParent(marker.transform, false);
                    var nameRect = nameObject.GetComponent<RectTransform>();
                    nameRect.anchorMin = new Vector2(-.75f, 1f); nameRect.anchorMax = new Vector2(1.75f, 1f);
                    nameRect.pivot = new Vector2(.5f, 0f); nameRect.anchoredPosition = new Vector2(0f, 1f); nameRect.sizeDelta = new Vector2(92f, 20f);
                    actor.nameplate = nameObject.GetComponent<Text>();
                    actor.nameplate.font = BuiltinFont(); actor.nameplate.fontSize = 11;
                    actor.nameplate.color = monster.worldBoss ? Gold : Cream;
                    actor.nameplate.alignment = TextAnchor.MiddleCenter;
                    actor.nameplate.horizontalOverflow = HorizontalWrapMode.Wrap;
                    actor.nameplate.text = monster.name;
                    var outline = nameObject.GetComponent<Outline>(); outline.effectColor = Ink; outline.effectDistance = new Vector2(1f, -1f);
                    actor.nameplate.raycastTarget = false;
                    nameObject.SetActive(false);
                    roamingMonsters.Add(actor);
                }
            }
        }

        private Vector2Int FindOpenRoamingCell(Vector2Int origin, HashSet<int> occupied)
        {
            for (var radius = 0; radius <= 8; radius++)
                for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius) continue;
                    var cell = origin + new Vector2Int(x, y);
                    if (cell.x < 3 || cell.y < 3 || cell.x >= ExplorationMapWidth - 3 || cell.y >= ExplorationMapHeight - 3) continue;
                    if (explorationBlocked != null && explorationBlocked[cell.x, cell.y]) continue;
                    var key = cell.y * ExplorationMapWidth + cell.x;
                    if (occupied.Contains(key) || cell == explorationCell) continue;
                    return cell;
                }
            return origin;
        }

        private void UpdateRoamingMonsters()
        {
            if (explorationMapRoot == null || explorationMapRect == null) return;
            foreach (var actor in roamingMonsters)
            {
                if (actor == null || actor.marker == null || actor.point == null) continue;
                if (actor.moveDuration > 0f)
                {
                    var progress = Mathf.Clamp01((Time.time - actor.moveStartedAt) / actor.moveDuration);
                    actor.marker.anchoredPosition = Vector2.Lerp(actor.moveStart, actor.moveEnd, progress * progress * (3f - 2f * progress));
                    if (progress >= 1f) actor.moveDuration = 0f;
                }
                if (actor.defeated)
                {
                    if (Time.time < actor.respawnAt) continue;
                    actor.defeated = false;
                    actor.marker.gameObject.SetActive(true);
                    var occupiedOnRespawn = new HashSet<int>();
                    foreach (var other in roamingMonsters)
                        if (other != actor && other != null && !other.defeated && other.point != null)
                            occupiedOnRespawn.Add(other.point.cell.y * ExplorationMapWidth + other.point.cell.x);
                    actor.point.cell = FindOpenRoamingCell(actor.homeCell, occupiedOnRespawn);
                    actor.marker.anchoredPosition = ExplorationCellPosition(actor.point.cell) + new Vector2(0f, 7f);
                    actor.moveDuration = 0f;
                    actor.nextMoveAt = Time.time + 2f;
                }
                if (actor == selectedRoamingMonster && explorationMovement != null) continue;
                if (Time.time < actor.nextMoveAt) continue;
                actor.nextMoveAt = Time.time + UnityEngine.Random.Range(1.2f, 2.7f);
                var distance = Mathf.Abs(actor.point.cell.x - explorationCell.x) + Mathf.Abs(actor.point.cell.y - explorationCell.y);
                if (distance <= 1) continue;
                var neighbors = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                var valid = new List<Vector2Int>();
                var occupied = new HashSet<int>();
                foreach (var other in roamingMonsters)
                    if (other != actor && other != null && !other.defeated && other.point != null)
                        occupied.Add(other.point.cell.y * ExplorationMapWidth + other.point.cell.x);
                foreach (var direction in neighbors)
                {
                    var next = actor.point.cell + direction;
                    if (next.x < 3 || next.y < 3 || next.x >= ExplorationMapWidth - 3 || next.y >= ExplorationMapHeight - 3) continue;
                    if (explorationBlocked != null && explorationBlocked[next.x, next.y]) continue;
                    if (occupied.Contains(next.y * ExplorationMapWidth + next.x)) continue;
                    valid.Add(next);
                }
                if (valid.Count == 0) continue;
                Vector2Int chosen = valid[UnityEngine.Random.Range(0, valid.Count)];
                if (distance <= 8)
                {
                    var best = int.MaxValue;
                    foreach (var candidate in valid)
                    {
                        var d = Mathf.Abs(candidate.x - explorationCell.x) + Mathf.Abs(candidate.y - explorationCell.y);
                        if (d < best) { best = d; chosen = candidate; }
                    }
                }
                actor.point.cell = chosen;
                actor.moveStart = actor.marker.anchoredPosition;
                actor.moveEnd = ExplorationCellPosition(chosen) + new Vector2(0f, 7f);
                actor.moveStartedAt = Time.time;
                actor.moveDuration = .32f;
                if (actor == selectedRoamingMonster) UpdateExplorationLabels();
            }
        }

        private void SelectRoamingMonster(RoamingMonsterActor actor)
        {
            if (actor == null || actor.defeated) return;
            ClearSelectedRoamingMonster();
            selectedRoamingMonster = actor;
            explorationSelectedPoint = actor.point;
            if (actor.nameplate != null) actor.nameplate.gameObject.SetActive(true);
            UpdateExplorationLabels();
            MoveExplorationTo(actor.point.cell);
        }

        private void ClearSelectedRoamingMonster()
        {
            if (selectedRoamingMonster?.nameplate != null) selectedRoamingMonster.nameplate.gameObject.SetActive(false);
            selectedRoamingMonster = null;
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
                var detail = hash & 31;
                if (kind == 2)
                {
                    var peak = 3 + Mathf.Abs((cellX * 3 + cellY * 7) % 6);
                    var ridge = peak + Mathf.Abs(x - tile / 2) / 2;
                    if (y > ridge + 2) pixel = ScalePixel(color, .47f);
                    else if (y >= ridge && y <= ridge + 2) pixel = ScalePixel(color, .70f);
                    else if ((x + y + cellX) % 7 == 0 || y == ridge - 1) pixel = new Color32(202, 192, 169, 255);
                    if (ordinal >= 9 && y >= ridge - 1) pixel = new Color32(206, 218, 218, 255);
                    if (detail == 2 && y > ridge + 1) pixel = ScalePixel(pixel, .72f);
                }
                else if (kind == 1)
                {
                    var canopyCenter = tile / 2 + ((cellX + cellY) % 3 - 1);
                    var canopyWidth = y < 5 ? 2 : y < 11 ? 5 : 4;
                    var inCanopy = y >= 2 && y <= 13 && Mathf.Abs(x - canopyCenter) <= canopyWidth;
                    if (inCanopy) pixel = (y < 5 || detail < 5)
                        ? new Color32(99, 139, 69, 255)
                        : new Color32((byte)Mathf.Clamp(color.r * .67f, 0, 255), (byte)Mathf.Clamp(color.g * 1.10f, 0, 255), (byte)Mathf.Clamp(color.b * .72f, 0, 255), 255);
                    if (x == canopyCenter && y >= 10 && y <= 15) pixel = new Color32(112, 83, 55, 255);
                    if (inCanopy && detail == 9) pixel = new Color32(157, 176, 91, 255);
                }
                else if (kind == 3)
                {
                    if ((x + cellX * 3 + y) % 13 < 2) pixel = new Color32(120, 164, 159, 255);
                    if (detail == 0) pixel = new Color32(50, 91, 101, 255);
                }
                else if (kind == 4)
                {
                    if (detail == 1 || detail == 12) pixel = new Color32(199, 166, 112, 255);
                    else if (detail == 6) pixel = new Color32(128, 103, 68, 255);
                    else if (detail == 21) pixel = Color32.Lerp(color, land, .35f);
                }
                else if (kind == 0)
                {
                    if (detail == 3 || detail == 19) pixel = ScalePixel(color, .82f);
                    else if (detail == 7) pixel = ScalePixel(color, 1.19f);
                    if (detail == 11 && ((x + cellX) % 3 == 0)) pixel = new Color32(150, 162, 91, 255);
                }
                else if (kind == 5 && detail == 7) pixel = ScalePixel(color, .94f);
                pixels[(oy + y) * textureWidth + ox + x] = pixel;
            }
        }

        private void AddExplorationMarker(WorldMapPoint point)
        {
            var size = point.kind == WorldPointKind.Town ? 32f : point.kind == WorldPointKind.MonsterZone ? 36f : 28f;
            var root = new GameObject("WorldPoint_" + point.kind + "_" + point.title, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(explorationMapRect, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = ExplorationCellPosition(point.cell);
            var image = root.GetComponent<Image>();
            image.sprite = point.kind == WorldPointKind.MonsterZone
                ? LoadPixelIcon("PixelArt/Monsters/" + point.monsterId)
                : AtlasPixelSprite(point.kind == WorldPointKind.Town ? "T" : "D");
            if (image.sprite == null) image.sprite = AtlasPixelSprite(point.kind == WorldPointKind.MonsterZone ? "Y" : point.kind == WorldPointKind.Town ? "T" : "D");
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
            var cacheKey = (colors?.hair ?? "") + "|" + (colors?.outfit ?? "") + "|" + (colors?.eyes ?? "");
            if (CultivatorSpriteCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;
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
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .1f), width);
            CultivatorSpriteCache[cacheKey] = sprite;
            return sprite;
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
            ClearSelectedRoamingMonster();
            explorationSelectedPoint = ClosestExplorationPoint(cell, 2);
            if (explorationSelectedPoint != null) UpdateExplorationLabels();
            MoveExplorationTo(cell);
        }

        private void SelectExplorationPoint(WorldMapPoint point)
        {
            ClearSelectedRoamingMonster();
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
                if (offlinePreview)
                {
                    RoamingMonsterActor encountered = null;
                    foreach (var actor in roamingMonsters)
                    {
                        if (actor == null || actor.defeated || actor.point == null) continue;
                        var distance = Mathf.Abs(actor.point.cell.x - explorationCell.x) + Mathf.Abs(actor.point.cell.y - explorationCell.y);
                        if (distance <= 1) { encountered = actor; break; }
                    }
                    if (encountered != null)
                    {
                        ClearSelectedRoamingMonster();
                        selectedRoamingMonster = encountered;
                        explorationSelectedPoint = encountered.point;
                        if (encountered.nameplate != null) encountered.nameplate.gameObject.SetActive(true);
                        explorationMovement = null;
                        SaveExplorationPosition();
                        UpdateExplorationLabels();
                        yield break;
                    }
                }
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
            if (offlinePreview && selectedRoamingMonster != null && !selectedRoamingMonster.defeated)
            {
                var target = FindOfflineMonster(selectedRoamingMonster.point.monsterId);
                explorationPoiText.text = $"MỤC TIÊU ĐANG DI CHUYỂN  ·  {target?.name ?? selectedRoamingMonster.point.title}\nCảnh giới yêu thú {target?.realm ?? 0} · {selectedRoamingMonster.point.town?.name} · {Mathf.Abs(explorationCell.x - selectedRoamingMonster.point.cell.x) + Mathf.Abs(explorationCell.y - selectedRoamingMonster.point.cell.y)} ô\nTiến sát mục tiêu rồi chạm GIAO CHIẾN.";
                if (explorationActionButton != null) SetOfflineButtonLabel(explorationActionButton, "GIAO CHIẾN");
                return;
            }
            if (explorationActionButton != null) SetOfflineButtonLabel(explorationActionButton, offlinePreview ? "KHIÊU CHIẾN / VÀO ĐIỂM" : "THÀNH / ĐIỂM ĐẾN");
            if (explorationSelectedPoint == null) { explorationPoiText.text = "Đường núi · Chạm bản đồ để đi từng ô."; return; }
            var point = explorationSelectedPoint;
            var kind = point.kind == WorldPointKind.Town ? "THÀNH TRẤN" : point.kind == WorldPointKind.Dungeon ? "CỔ ĐỘNG" : "BÃI TIỂU YÊU";
            explorationPoiText.text = $"{kind}  ·  {point.title}     ({point.cell.x + 1}, {point.cell.y + 1})\n{point.town?.realmMinName ?? explorationMap?.realmMinName ?? "Địa vực tu luyện"}  ·  {(offlinePreview && point.kind == WorldPointKind.MonsterZone ? "Chạm KHIÊU CHIẾN để mở trận và nhận chiến lợi phẩm." : point.town?.desc ?? point.dungeon?.desc ?? "Điểm thám hiểm trên bản đồ")}";
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
            if (offlinePreview && selectedRoamingMonster != null && !selectedRoamingMonster.defeated)
            {
                if (explorationCell != selectedRoamingMonster.point.cell) { MoveExplorationTo(selectedRoamingMonster.point.cell); return; }
                activeRoamingMonster = selectedRoamingMonster;
                StartOfflineHunt(selectedRoamingMonster.point);
                return;
            }
            var point = explorationSelectedPoint;
            if (point == null) { ShowStatus("Hãy chọn một thành trấn, cổ động hoặc bãi yêu trước."); return; }
            if (explorationCell != point.cell) { MoveExplorationTo(point.cell); return; }
            if (offlinePreview)
            {
                if (point.kind == WorldPointKind.Town) { OfflineTravelTo(point.town); return; }
                var encounter = point;
                if (point.kind == WorldPointKind.Dungeon && string.IsNullOrEmpty(point.monsterId) && point.town?.monsterPool?.Length > 0)
                    encounter = new WorldMapPoint { kind = point.kind, title = point.title, town = point.town, dungeon = point.dungeon, monsterId = point.town.monsterPool[point.town.monsterPool.Length - 1], cell = point.cell };
                StartOfflineHunt(encounter);
                return;
            }
            if (point.kind == WorldPointKind.Town)
            {
                if (state.town?.id == point.town.id) ShowHome(state); else TravelTo(point.town);
            }
            else if (state.town?.id != point.town?.id) TravelTo(point.town);
            else if (point.kind == WorldPointKind.Dungeon) { SetAtlasOrientation(false); EnterDungeon(point.dungeon.id); }
            else { SetAtlasOrientation(false); ShowPveTown(state, point.town); }
        }

        private void OfflineTravelTo(TownInfo town)
        {
            if (town == null)
            {
                ShowStatus("Không tìm thấy thành trấn này trong dữ liệu thế giới.");
                return;
            }
            if (offlineProgress.realmIndex < town.realmMin)
            {
                ShowStatus($"Cần đạt {town.realmMinName ?? RealmLabel(town.realmMin)} để ngự kiếm tới {town.name}. Hãy săn yêu thú để tích lũy linh lực.");
                return;
            }
            var targetMap = FindMap(town.mapId);
            if (targetMap == null) { ShowStatus("Thiếu dữ liệu bản đồ của thành trấn."); return; }
            if (targetMap.ascensionRequired && offlineProgress.realmIndex < 11)
            {
                ShowStatus("Cần hoàn thành Phi Thăng ở cảnh giới 11 trước khi vào Tiên Giới.");
                return;
            }
            offlinePreviewState.player.ascended = offlineProgress.realmIndex >= 11;
            offlinePreviewState.realm.index = offlineProgress.realmIndex;
            offlinePreviewState.realm.name = OfflineRealmName(offlineProgress.realmIndex);
            offlinePreviewState.town = town;
            latestState = offlinePreviewState;
            offlineProgress.currentTownId = town.id;
            PlayerPrefs.DeleteKey(ExplorationSaveKey(offlinePreviewState, targetMap));
            SaveOfflineProgress();
            atlasImmortalRealm = targetMap.ascensionRequired;
            RenderExplorationMap(offlinePreviewState);
            ShowStatus("Đã ngự kiếm tới " + town.name + " · yêu thú, chiến lợi phẩm và tiến độ được lưu trên máy.");
        }

        private TownInfo FindOfflineTown(string townId)
        {
            if (string.IsNullOrEmpty(townId)) return null;
            foreach (var town in mapCatalog?.towns ?? Array.Empty<TownInfo>())
                if (town != null && town.id == townId && town.realmMin <= offlineProgress.realmIndex) return town;
            return null;
        }

        private void EnterOfflineMonsterField(GameState state)
        {
            if (atlasSelectedTown == null) { ShowStatus("Chọn một thành trấn có bãi yêu thú trước."); return; }
            if (state?.town?.id != atlasSelectedTown.id) { OfflineTravelTo(atlasSelectedTown); return; }
            atlasFromExploration = false;
            RenderExplorationMap(state);
            foreach (var point in explorationPoints)
            {
                if (point.kind != WorldPointKind.MonsterZone || point.town?.id != atlasSelectedTown.id) continue;
                explorationSelectedPoint = point;
                UpdateExplorationLabels();
                MoveExplorationTo(point.cell);
                return;
            }
            ShowStatus("Khu vực này chưa có bãi quái trong catalog.");
        }

        private void EnterOfflineAtlasDungeon(GameState state)
        {
            if (atlasSelectedTown == null || atlasSelectedDungeon == null) { ShowStatus("Chọn cổ động cần thám hiểm trước."); return; }
            if (state?.town?.id != atlasSelectedTown.id) { OfflineTravelTo(atlasSelectedTown); return; }
            atlasFromExploration = false;
            RenderExplorationMap(state);
            foreach (var point in explorationPoints)
            {
                if (point.kind != WorldPointKind.Dungeon || point.dungeon?.id != atlasSelectedDungeon.id) continue;
                explorationSelectedPoint = point;
                UpdateExplorationLabels();
                MoveExplorationTo(point.cell);
                return;
            }
            ShowStatus("Cổ động chưa có đường vào trên bản đồ khu vực.");
        }

        private void LoadOfflineProgress()
        {
            if (offlineProgressLoaded) return;
            var saved = PlayerPrefs.GetString(OfflineProgressKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                try { offlineProgress = JsonUtility.FromJson<OfflineProgressSave>(saved); }
                catch { offlineProgress = new OfflineProgressSave(); }
            }
            if (offlineProgress == null) offlineProgress = new OfflineProgressSave();
            if (offlineProgress.items == null) offlineProgress.items = new List<OfflineInventoryStack>();
            if (offlineProgress.items.Count == 0)
            {
                AddOfflineInventory("moc_kiem", 1);
                AddOfflineInventory("bo_y", 1);
                AddOfflineInventory("hoi_xuan_dan", 3);
            }
            offlineProgress.realmIndex = Mathf.Clamp(offlineProgress.realmIndex, 0, 65);
            offlineProgress.experience = Mathf.Max(0, offlineProgress.experience);
            offlineProgress.hp = Mathf.Clamp(offlineProgress.hp, 1, OfflinePlayerMaxHp());
            offlineProgressLoaded = true;
            SaveOfflineProgress();
        }

        private const int OfflineMaxHp = 240;

        private int OfflinePlayerMaxHp() => OfflineMaxHp + Mathf.Max(0, offlineProgress.realmIndex) * 65;

        private int OfflineExperienceToNextRealm() => 300 + Mathf.Max(0, offlineProgress.realmIndex) * 55;

        private string OfflineRealmName(int index)
        {
            var best = "Cảnh giới " + index;
            foreach (var map in mapCatalog?.maps ?? Array.Empty<MapInfo>())
            {
                if (map == null || index < map.realmMin || index > map.realmMax) continue;
                best = map.realmMinName ?? best;
                if (index > map.realmMin && !string.IsNullOrEmpty(map.realmMaxName)) best = map.realmMaxName;
                break;
            }
            return best;
        }

        private void GrantOfflineExperience(OfflineMonsterData monster)
        {
            var oldRealm = offlineProgress.realmIndex;
            var gained = 120 + Mathf.Max(0, monster.realm) * 45;
            offlineProgress.experience += gained;
            while (offlineProgress.realmIndex < 65 && offlineProgress.experience >= OfflineExperienceToNextRealm())
            {
                offlineProgress.experience -= OfflineExperienceToNextRealm();
                offlineProgress.realmIndex++;
            }
            var realmUps = offlineProgress.realmIndex - oldRealm;
            if (realmUps > 0)
            {
                offlineProgress.hp = Mathf.Min(OfflinePlayerMaxHp(), offlineProgress.hp + realmUps * 65);
                offlinePreviewState.realm.index = offlineProgress.realmIndex;
                offlinePreviewState.realm.name = OfflineRealmName(offlineProgress.realmIndex);
                offlinePreviewState.player.ascended = offlineProgress.realmIndex >= 11;
                offlineBattleMessage.text = $"Đột phá {realmUps} cảnh giới · {offlinePreviewState.realm.name}! +{gained:N0} linh lực.";
            }
            else offlineBattleMessage.text = $"Đánh bại {monster.name} · +{gained:N0} linh lực.";
        }

        private void SaveOfflineProgress()
        {
            PlayerPrefs.SetString(OfflineProgressKey, JsonUtility.ToJson(offlineProgress));
            PlayerPrefs.Save();
        }

        private int OfflineInventoryCount()
        {
            var total = 0;
            foreach (var item in offlineProgress?.items ?? new List<OfflineInventoryStack>()) total += Mathf.Max(0, item?.quantity ?? 0);
            return total;
        }

        private OfflineMonsterData FindOfflineMonster(string id)
        {
            foreach (var monster in offlineHuntCatalog?.monsters ?? Array.Empty<OfflineMonsterData>())
                if (monster != null && monster.id == id) return monster;
            return null;
        }

        private OfflineItemData FindOfflineItem(string id)
        {
            foreach (var item in offlineHuntCatalog?.items ?? Array.Empty<OfflineItemData>())
                if (item != null && item.id == id) return item;
            return null;
        }

        private void PrepareOfflineBattleSkills()
        {
            offlineBattleSkills.Clear();
            foreach (var skill in offlineHuntCatalog?.skills ?? Array.Empty<OfflineSkillData>())
            {
                if (skill == null || skill.mon != offlineProgress.monClass || skill.realm > offlineProgress.realmIndex) continue;
                if (skill.kind == "escape" || skill.kind == "mana") continue;
                offlineBattleSkills.Add(skill);
            }
            if (offlineBattleSkills.Count == 0)
                offlineBattleSkills.Add(new OfflineSkillData { id = "kiem_khi_tram", mon = "kiem", name = "Kiếm Khí Trảm", kind = "atk", realm = 0, mp = 14 });
            offlineBattleSkills.Sort((left, right) => left.realm.CompareTo(right.realm));
            offlineBattleSkillIndex = offlineBattleSkills.Count - 1;
            activeOfflineSkill = offlineBattleSkills[offlineBattleSkillIndex];
        }

        private void CycleOfflineSkill()
        {
            if (offlineBattleSkills.Count == 0) return;
            offlineBattleSkillIndex = (offlineBattleSkillIndex + offlineBattleSkills.Count - 1) % offlineBattleSkills.Count;
            activeOfflineSkill = offlineBattleSkills[offlineBattleSkillIndex];
            UpdateOfflineSkillPresentation();
            if (offlineBattleMessage != null) offlineBattleMessage.text = activeOfflineSkill.desc ?? "Đã chọn kỹ năng.";
        }

        private void UpdateOfflineSkillPresentation()
        {
            if (offlineSkillButton == null) return;
            SetOfflineButtonLabel(offlineSkillButton, (activeOfflineSkill?.name ?? "Kỹ năng") + " · " + offlineBattleEnergy);
            if (offlineSkillIcon == null) return;
            var sprite = LoadPixelIcon("PixelArt/Items/" + (activeOfflineSkill?.id ?? "kiem_khi_tram"));
            offlineSkillIcon.sprite = sprite;
            offlineSkillIcon.enabled = sprite != null;
        }

        private static Sprite LoadPixelIcon(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            // Unity destroys unreferenced runtime sprites when unused assets are unloaded; rebuild those.
            if (PixelIconCache.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            // The redrawn monsters and item icons replace the original icons wherever they exist.
            var redrawn = ArtSprites.ForLegacyPath(resourcePath);
            if (redrawn != null) { PixelIconCache[resourcePath] = redrawn; return redrawn; }
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 64);
            sprite.name = resourcePath;
            PixelIconCache[resourcePath] = sprite;
            return sprite;
        }

        private void AddOfflineInventory(string itemId, int quantity)
        {
            if (quantity <= 0 || offlineProgress?.items == null) return;
            foreach (var stack in offlineProgress.items)
                if (stack.id == itemId) { stack.quantity += quantity; return; }
            offlineProgress.items.Add(new OfflineInventoryStack { id = itemId, quantity = quantity });
        }

        private bool RemoveOfflineInventory(string itemId, int quantity)
        {
            foreach (var stack in offlineProgress?.items ?? new List<OfflineInventoryStack>())
            {
                if (stack.id != itemId || stack.quantity < quantity) continue;
                stack.quantity -= quantity;
                if (stack.quantity <= 0) offlineProgress.items.Remove(stack);
                return true;
            }
            return false;
        }

        private void ShowOfflineInventory()
        {
            if (offlineInventoryRoot != null) { Destroy(offlineInventoryRoot); offlineInventoryRoot = null; return; }
            offlineInventoryRoot = PanelObject("OfflineInventoryOverlay", content.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, .76f));
            var card = PanelObject("InventoryCard", offlineInventoryRoot.transform, new Vector2(.20f, .12f), new Vector2(.80f, .88f), Vector2.zero, Vector2.zero, new Color32(23, 29, 36, 255));
            ChildText(card.transform, "InventoryTitle", 26, Gold, TextAnchor.MiddleLeft, new Vector2(.05f, .87f), new Vector2(.78f, .98f)).text = "TÚI ĐỒ NGOẠI TUYẾN";
            ChildText(card.transform, "InventoryCount", 16, Muted, TextAnchor.MiddleRight, new Vector2(.66f, .87f), new Vector2(.94f, .98f)).text = $"{OfflineInventoryCount()} vật phẩm";
            var shown = 0;
            foreach (var stack in offlineProgress.items)
            {
                if (shown >= 7) break;
                var item = FindOfflineItem(stack.id);
                var y1 = .82f - shown * .105f;
                var row = PanelObject("InventoryItem_" + stack.id, card.transform, new Vector2(.04f, y1 - .09f), new Vector2(.96f, y1), Vector2.zero, Vector2.zero, new Color32(36, 43, 50, 255));
                var icon = new GameObject("ItemPixel", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(row.transform, false);
                Place(icon.GetComponent<RectTransform>(), new Vector2(.025f, .10f), new Vector2(.13f, .90f));
                var sprite = LoadPixelIcon("PixelArt/Items/" + stack.id);
                icon.GetComponent<Image>().sprite = sprite; icon.GetComponent<Image>().preserveAspect = true;
                ChildText(row.transform, "ItemName", 17, Cream, TextAnchor.MiddleLeft, new Vector2(.16f, .12f), new Vector2(.76f, .88f)).text = item?.name ?? stack.id;
                ChildText(row.transform, "ItemQuantity", 17, Gold, TextAnchor.MiddleRight, new Vector2(.77f, .12f), new Vector2(.96f, .88f)).text = "× " + stack.quantity;
                shown++;
            }
            if (shown == 0) ChildText(card.transform, "EmptyInventory", 18, Muted, TextAnchor.MiddleCenter, new Vector2(.08f, .40f), new Vector2(.92f, .60f)).text = "Túi đồ đang trống.";
            Button("ĐÓNG", new Vector2(.35f, .04f), new Vector2(.65f, .13f), Gold, ShowOfflineInventory, card.transform);
        }

        private void StartOfflineHunt(WorldMapPoint point)
        {
            if (offlineBattleRoot != null || offlineActionRunning) return;
            activeRoamingMonster = selectedRoamingMonster != null && selectedRoamingMonster.point == point ? selectedRoamingMonster : null;
            activeOfflineMonster = FindOfflineMonster(point?.monsterId);
            if (activeOfflineMonster == null)
            {
                ShowStatus("Bãi này chưa có sprite hoặc dữ liệu quái trong gói offline.");
                return;
            }
            if (offlineProgress.hp <= 0) offlineProgress.hp = OfflinePlayerMaxHp() / 2;
            offlineActionRunning = false;
            offlineBattleOver = false;
            offlineBattleMonsterHp = OfflineMonsterBattleMaxHp(activeOfflineMonster);
            offlineBattleEnergy = 3;
            offlineEnemyStunnedUntil = 0f;
            PrepareOfflineBattleSkills();
            if (explorationMapRoot != null) explorationMapRoot.SetActive(false);
            if (offlineInventoryRoot != null) { Destroy(offlineInventoryRoot); offlineInventoryRoot = null; }
            ClearContent();

            offlineBattleRoot = PanelObject("OfflinePixelBattle", content.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            offlineBattleRoot.GetComponent<Image>().raycastTarget = false;
            var ground = new GameObject("PixelArena", typeof(RectTransform), typeof(RawImage)); ground.transform.SetParent(offlineBattleRoot.transform, false);
            Place(ground.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            offlineBattleTexture = BuildOfflineBattleGround(activeOfflineMonster);
            ground.GetComponent<RawImage>().texture = offlineBattleTexture;
            ground.GetComponent<RawImage>().raycastTarget = false;
            PanelObject("BattleShade", offlineBattleRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, .20f));

            var header = PanelObject("BattleHud", offlineBattleRoot.transform, new Vector2(.025f, .815f), new Vector2(.975f, .98f), Vector2.zero, Vector2.zero, new Color32(15, 20, 24, 235));
            ChildText(header.transform, "PlayerVitals", 17, Cream, TextAnchor.MiddleLeft, new Vector2(.035f, .53f), new Vector2(.37f, .94f)).text = $"ĐẠO HỮU  ·  {offlineProgress.hp}/{OfflinePlayerMaxHp()} KHÍ HUYẾT";
            ChildText(header.transform, "BattleLocation", 15, Gold, TextAnchor.MiddleCenter, new Vector2(.38f, .53f), new Vector2(.60f, .94f)).text = "PVE  ·  " + (point.town?.name ?? offlineHuntCatalog.sourceMapName);
            ChildText(header.transform, "EnemyName", 17, Cream, TextAnchor.MiddleRight, new Vector2(.61f, .53f), new Vector2(.80f, .94f)).text = activeOfflineMonster.name;
            offlinePlayerHealthFill = MakeBattleHealthBar(header.transform, new Vector2(.035f, .15f), new Vector2(.37f, .43f), new Color32(73, 190, 111, 255));
            offlineMonsterHealthFill = MakeBattleHealthBar(header.transform, new Vector2(.61f, .15f), new Vector2(.80f, .43f), new Color32(206, 72, 63, 255));

            offlinePlayerFighter = MakeBattleFighter("PixelCultivator", CreateCultivatorSprite(offlinePreviewState.player.appearanceColors), new Vector2(.28f, .48f), new Vector2(114, 172));
            offlineMonsterImage = MakeBattleFighterImage("PixelMonster", LoadPixelIcon("PixelArt/Monsters/" + activeOfflineMonster.id), new Vector2(.70f, .49f), new Vector2(190, 190), out offlineMonsterFighter);
            if (offlineMonsterImage.sprite == null) offlineMonsterImage.sprite = AtlasPixelSprite("Y");
            offlineBattleTitle = ChildText(offlineBattleRoot.transform, "EnemyCaption", 18, Cream, TextAnchor.MiddleCenter, new Vector2(.54f, .35f), new Vector2(.86f, .42f));
            offlineBattleTitle.text = activeOfflineMonster.name;
            offlineBattleMessage = ChildText(offlineBattleRoot.transform, "CombatLog", 17, new Color32(255, 228, 169, 255), TextAnchor.MiddleCenter, new Vector2(.29f, .27f), new Vector2(.71f, .34f));
            offlineBattleMessage.text = "Yêu thú phát hiện đạo hữu!";

            BattleMoveButton("↑", new Vector2(.105f, .135f), new Vector2(.175f, .205f), Vector2.up);
            BattleMoveButton("←", new Vector2(.035f, .055f), new Vector2(.105f, .125f), Vector2.left);
            BattleMoveButton("↓", new Vector2(.105f, .055f), new Vector2(.175f, .125f), Vector2.down);
            BattleMoveButton("→", new Vector2(.175f, .055f), new Vector2(.245f, .125f), Vector2.right);
            Button("ĐÁNH", new Vector2(.64f, .045f), new Vector2(.75f, .19f), Gold, () => OfflineBattleAction(false), offlineBattleRoot.transform);
            offlineSkillButton = Button((activeOfflineSkill?.name ?? "Kỹ năng") + " · " + offlineBattleEnergy,
                new Vector2(.69f, .045f), new Vector2(.84f, .19f), new Color32(52, 75, 96, 255), () => OfflineBattleAction(true), offlineBattleRoot.transform);
            var skillLabel = offlineSkillButton.GetComponentInChildren<Text>()?.GetComponent<RectTransform>();
            if (skillLabel != null)
            {
                skillLabel.anchorMin = new Vector2(.27f, 0f);
                skillLabel.anchorMax = new Vector2(.98f, 1f);
                skillLabel.offsetMin = Vector2.zero;
                skillLabel.offsetMax = Vector2.zero;
            }
            var skillIconObject = new GameObject("SkillPixelArt", typeof(RectTransform), typeof(Image));
            skillIconObject.transform.SetParent(offlineSkillButton.transform, false);
            var skillIconRect = skillIconObject.GetComponent<RectTransform>();
            skillIconRect.anchorMin = new Vector2(.025f, .10f);
            skillIconRect.anchorMax = new Vector2(.265f, .90f);
            skillIconRect.offsetMin = Vector2.zero;
            skillIconRect.offsetMax = Vector2.zero;
            offlineSkillIcon = skillIconObject.GetComponent<Image>();
            offlineSkillIcon.preserveAspect = true;
            offlineSkillIcon.raycastTarget = false;
            UpdateOfflineSkillPresentation();
            offlineSkillCycleButton = Button("ĐỔI PHÁP", new Vector2(.845f, .045f), new Vector2(.91f, .19f), Panel, CycleOfflineSkill, offlineBattleRoot.transform);
            Button("HỒI ĐAN", new Vector2(.915f, .045f), new Vector2(.99f, .19f), new Color32(57, 90, 70, 255), UseOfflineHealingPill, offlineBattleRoot.transform);
            offlineLeaveButton = Button("RÚT LUI", new Vector2(.82f, .85f), new Vector2(.97f, .95f), Panel, FinishOfflineBattle, header.transform);
            offlineNextEnemyAttackTime = Time.time + 1.1f;
            offlineBattleMotionTime = 0f;
            offlineBattleMoveInput = Vector2.zero;
            offlinePlayerBattlePosition = offlinePlayerFighter.localPosition;
            ShowOfflineBattleVitals();
        }

        private bool offlineBattleOver;
        private int offlineBattleMonsterHp;
        private int offlineBattleEnergy;
        private Button offlineLeaveButton;

        private Image MakeBattleHealthBar(Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var frame = PanelObject("HealthTrack", parent, min, max, Vector2.zero, Vector2.zero, new Color32(36, 39, 39, 255));
            var fill = PanelObject("HealthFill", frame.transform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2), color).GetComponent<Image>();
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            return fill;
        }

        private RectTransform MakeBattleFighter(string name, Sprite sprite, Vector2 anchor, Vector2 size)
        {
            var rect = MakeBattleFighterImage(name, sprite, anchor, size, out _).rectTransform;
            return rect;
        }

        private Image MakeBattleFighterImage(string name, Sprite sprite, Vector2 anchor, Vector2 size, out RectTransform rect)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(offlineBattleRoot.transform, false);
            rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
            var image = obj.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        private Texture2D BuildOfflineBattleGround(OfflineMonsterData monster)
        {
            const int width = 320, height = 180;
            var pixels = new Color32[width * height];
            var ground = monster.element == "hoa" ? new Color32(76, 70, 56, 255) : monster.element == "thuy" ? new Color32(57, 82, 79, 255) : new Color32(70, 91, 58, 255);
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var n = ((x * 92837111) ^ (y * 689287499) ^ (monster.id.GetHashCode() * 283923481)) & 31;
                var color = n < 3 ? ScalePixel(ground, .73f) : n > 27 ? ScalePixel(ground, 1.25f) : ground;
                var roadCenter = width * .51f + Mathf.Sin(y * .035f + (monster.realm * .6f)) * 39f;
                if (Mathf.Abs(x - roadCenter) < 8 + (int)(Mathf.Sin(y * .08f) * 2)) color = n % 4 == 0 ? new Color32(147, 118, 77, 255) : new Color32(126, 103, 72, 255);
                pixels[y * width + x] = color;
            }
            var oldRandom = UnityEngine.Random.state;
            UnityEngine.Random.InitState(monster.id.GetHashCode());
            for (var i = 0; i < 42; i++)
            {
                var x = UnityEngine.Random.Range(8, width - 8); var y = UnityEngine.Random.Range(8, height - 8);
                var leaf = UnityEngine.Random.value > .42f;
                var c = leaf ? new Color32(48, 79, 52, 255) : new Color32(107, 102, 89, 255);
                DrawPixelRect(pixels, width, height, x - 2, y, 5, 2, c);
                DrawPixelRect(pixels, width, height, x - 1, y - 2, 3, 2, ScalePixel(c, 1.22f));
                if (!leaf) DrawPixelRect(pixels, width, height, x, y + 2, 2, 2, new Color32(61, 54, 45, 255));
            }
            for (var i = 0; i < 160; i++)
            {
                var x = UnityEngine.Random.Range(2, width - 2); var y = UnityEngine.Random.Range(2, height - 2);
                if (UnityEngine.Random.value > .5f) DrawPixelRect(pixels, width, height, x, y, 1, UnityEngine.Random.Range(1, 4), new Color32(126, 149, 88, 255));
            }
            UnityEngine.Random.state = oldRandom;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "AWS_OfflinePixelArena", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false, true); return texture;
        }

        private static void DrawPixelRect(Color32[] pixels, int width, int height, int x, int y, int drawWidth, int drawHeight, Color32 color)
        {
            for (var py = Mathf.Max(0, y); py < Mathf.Min(height, y + drawHeight); py++)
                for (var px = Mathf.Max(0, x); px < Mathf.Min(width, x + drawWidth); px++) pixels[py * width + px] = color;
        }

        private void ShowOfflineBattleVitals()
        {
            if (offlinePlayerHealthFill != null) offlinePlayerHealthFill.fillAmount = Mathf.Clamp01(offlineProgress.hp / (float)OfflinePlayerMaxHp());
            if (offlineMonsterHealthFill != null) offlineMonsterHealthFill.fillAmount = Mathf.Clamp01(offlineBattleMonsterHp / (float)OfflineMonsterBattleMaxHp(activeOfflineMonster));
        }

        private int OfflineMonsterBattleMaxHp(OfflineMonsterData monster)
        {
            var health = Mathf.Max(110f, monster == null ? 110f : monster.hp * .42f);
            if (monster?.worldBoss == true) health *= 2f;
            return Mathf.Clamp(Mathf.RoundToInt(health), 110, 18000);
        }

        private int OfflineMonsterAttackDamage(OfflineMonsterData monster)
        {
            var damage = 13 + Mathf.Clamp(Mathf.RoundToInt((monster?.atk ?? 25) * .08f), 0, 110);
            return damage + UnityEngine.Random.Range(0, 7);
        }

        private void OfflineBattleAction(bool skill)
        {
            if (offlineActionRunning || offlineBattleOver) return;
            if (skill && offlineBattleEnergy <= 0) { offlineBattleMessage.text = "Linh lực đã cạn · đánh thường để tiếp tục."; return; }
            var range = Vector2.Distance(offlinePlayerFighter.localPosition, offlineMonsterFighter.localPosition);
            if (range > 310f) { offlineBattleMessage.text = "Yêu thú đang ở xa · dùng phím hướng để áp sát."; return; }
            offlineActionRunning = true;
            StartCoroutine(ResolveOfflineBattleTurn(skill));
        }

        private IEnumerator ResolveOfflineBattleTurn(bool skill)
        {
            if (skill) offlineBattleEnergy--;
            UpdateOfflineSkillPresentation();
            StartCoroutine(AnimateOfflineSwordEffect(skill));
            var playerHome = offlinePlayerFighter.anchoredPosition;
            var enemyHome = offlineMonsterFighter.anchoredPosition;
            var direction = ((Vector2)offlineMonsterFighter.localPosition - (Vector2)offlinePlayerFighter.localPosition).normalized * (skill ? 92 : 72);
            var duration = .16f;
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                offlinePlayerFighter.anchoredPosition = playerHome + direction * Mathf.Sin(time / duration * Mathf.PI * .5f);
                yield return null;
            }
            offlinePlayerFighter.anchoredPosition = playerHome + direction;
            if (skill) offlineMonsterImage.color = new Color32(255, 184, 102, 255);
            var rawDamage = (skill ? 88 : 51) + offlineProgress.realmIndex * (skill ? 15 : 9);
            if (skill)
            {
                var kind = activeOfflineSkill?.kind ?? "atk";
                var multiplier = kind == "multi" ? 2.1f : kind == "dot" ? 1.7f : kind == "stun" ? 1.45f : kind == "buff" ? 1.65f : kind == "shield" ? 1.3f : 1.6f;
                rawDamage = Mathf.RoundToInt(rawDamage * multiplier);
                if (kind == "stun") offlineEnemyStunnedUntil = Time.time + 1.8f;
                if (kind == "heal") offlineProgress.hp = Mathf.Min(OfflinePlayerMaxHp(), offlineProgress.hp + Mathf.Max(28, rawDamage / 2));
            }
            var damage = Mathf.Max(18, rawDamage - Mathf.Clamp(activeOfflineMonster.def / 12, 0, 180));
            if (skill && activeOfflineSkill?.kind == "dot") damage += Mathf.Max(8, damage / 5);
            offlineBattleMonsterHp = Mathf.Max(0, offlineBattleMonsterHp - damage);
            for (var n = 0; n < 5; n++)
            {
                offlineMonsterFighter.anchoredPosition = enemyHome + new Vector2(UnityEngine.Random.Range(-9, 10), UnityEngine.Random.Range(-6, 7));
                yield return new WaitForSeconds(.035f);
            }
            offlineMonsterFighter.anchoredPosition = enemyHome;
            offlinePlayerFighter.anchoredPosition = playerHome;
            offlineMonsterImage.color = Color.white;
            ShowOfflineBattleVitals();

            if (offlineBattleMonsterHp <= 0)
            {
                offlineBattleOver = true;
                offlineProgress.kills++;
                offlineProgress.stones += 300 + activeOfflineMonster.realm * 45;
                GrantOfflineExperience(activeOfflineMonster);
                if (activeRoamingMonster != null)
                {
                    activeRoamingMonster.defeated = true;
                    activeRoamingMonster.respawnAt = Time.time + 22f;
                    activeRoamingMonster.marker.gameObject.SetActive(false);
                }
                var drops = RollOfflineDrops(activeOfflineMonster);
                var description = new List<string>();
                foreach (var drop in drops)
                {
                    AddOfflineInventory(drop.Key, drop.Value);
                    var item = FindOfflineItem(drop.Key);
                    description.Add((item?.name ?? drop.Key) + " ×" + drop.Value);
                    var icon = new GameObject("LootPixel_" + drop.Key, typeof(RectTransform), typeof(Image)); icon.transform.SetParent(offlineBattleRoot.transform, false);
                    Place(icon.GetComponent<RectTransform>(), new Vector2(.40f + (description.Count - 1) * .065f, .39f), new Vector2(.45f + (description.Count - 1) * .065f, .48f));
                    icon.GetComponent<Image>().sprite = LoadPixelIcon("PixelArt/Items/" + drop.Key); icon.GetComponent<Image>().preserveAspect = true;
                }
                offlineBattleTitle.text = "CHIẾN THẮNG  ·  " + activeOfflineMonster.name;
                offlineBattleMessage.text += "\nRƠI ĐỒ  ·  " + string.Join("   ·   ", description);
                SetOfflineButtonLabel(offlineLeaveButton, "NHẶT ĐỒ  ·  VỀ MAP");
                SaveOfflineProgress();
                ShowOfflineBattleVitals();
            }
            else
            {
                offlineBattleMessage.text = skill
                    ? (activeOfflineSkill?.name ?? "Kỹ năng") + " · gây " + damage + " sát thương."
                    : "Một đòn đánh trúng yêu thú · " + damage + " sát thương.";
                if (skill && activeOfflineSkill?.kind == "stun")
                {
                    offlineBattleMessage.text += " Yêu thú bị định thân.";
                    SaveOfflineProgress(); ShowOfflineBattleVitals();
                    offlineActionRunning = false;
                    yield break;
                }
                yield return new WaitForSeconds(.38f);
                var incoming = OfflineMonsterAttackDamage(activeOfflineMonster);
                offlineProgress.hp = Mathf.Max(0, offlineProgress.hp - incoming);
                offlineBattleMessage.text = activeOfflineMonster.name + " phản kích · mất " + incoming + " khí huyết.";
                if (offlineProgress.hp <= 0)
                {
                    offlineProgress.hp = OfflinePlayerMaxHp() / 2;
                    offlineBattleOver = true;
                    offlineBattleTitle.text = "TRỌNG THƯƠNG  ·  ĐƯỢC CỨU VỀ THÀNH";
                    offlineBattleMessage.text = "Chưa nhận được chiến lợi phẩm. Khí huyết đã hồi một nửa.";
                    SetOfflineButtonLabel(offlineLeaveButton, "HỒI THÀNH  ·  VỀ MAP");
                }
                SaveOfflineProgress();
                ShowOfflineBattleVitals();
            }
            offlineActionRunning = false;
        }

        private IEnumerator AnimateOfflineSwordEffect(bool skill)
        {
            var rootRect = offlineBattleRoot.GetComponent<RectTransform>();
            var effectObject = new GameObject("AnimatedPixelSwordQi", typeof(RectTransform), typeof(Image));
            effectObject.transform.SetParent(offlineBattleRoot.transform, false);
            var rect = effectObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .49f);
            rect.sizeDelta = new Vector2(skill ? 112 : 84, skill ? 112 : 84);
            var image = effectObject.GetComponent<Image>();
            image.raycastTarget = false;
            var artId = skill ? activeOfflineSkill?.id ?? "kiem_khi_tram" : "kiem_phap_co_ban";
            var artName = skill ? activeOfflineSkill?.name ?? "Kiếm Khí Trảm" : "Đánh thường";
            var combatRole = skill ? activeOfflineSkill?.kind ?? offlineProgress.monClass : offlineProgress.monClass;
            var detailedSkillSprite = skill ? LoadPixelIcon("PixelArt/Items/" + artId) : null;
            if (detailedSkillSprite != null) image.sprite = detailedSkillSprite;
            else
            {
                image.sprite = PixelSkillArt.Frames(artId, artName, combatRole, offlineProgress.realmIndex >= 11)[0];
                PixelSkillArt.Animate(image, artId, artName, combatRole, offlineProgress.realmIndex >= 11);
            }
            var canvasSize = rootRect.rect.size;
            var start = new Vector2(-canvasSize.x * .10f, 0);
            var end = new Vector2(canvasSize.x * .12f, canvasSize.y * .015f);
            var duration = skill ? .29f : .20f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var progress = t / duration;
                rect.anchoredPosition = Vector2.Lerp(start, end, progress);
                rect.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-28, 24, progress));
                image.color = new Color(1, 1, 1, Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }
            Destroy(effectObject);
        }

        private Dictionary<string, int> RollOfflineDrops(OfflineMonsterData monster)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var drop in monster.drops ?? Array.Empty<OfflineDropData>())
            {
                if (drop == null || UnityEngine.Random.value > Mathf.Clamp01(drop.rate)) continue;
                var count = drop.qty > 0 ? drop.qty : UnityEngine.Random.Range(Mathf.Max(1, drop.min), Mathf.Max(Mathf.Max(1, drop.min) + 1, drop.max + 1));
                if (result.ContainsKey(drop.id)) result[drop.id] += count; else result[drop.id] = count;
            }
            if (result.Count == 0) result["mat_yeu_dan"] = 1;
            return result;
        }

        private void UseOfflineHealingPill()
        {
            if (offlineActionRunning || offlineBattleOver) return;
            if (!RemoveOfflineInventory("hoi_xuan_dan", 1)) { offlineBattleMessage.text = "Túi không còn Hồi Xuân Đan."; return; }
            if (offlineProgress.hp >= OfflinePlayerMaxHp()) { AddOfflineInventory("hoi_xuan_dan", 1); offlineBattleMessage.text = "Khí huyết đã đầy."; return; }
            offlineProgress.hp = Mathf.Min(OfflinePlayerMaxHp(), offlineProgress.hp + 82 + offlineProgress.realmIndex * 10);
            SaveOfflineProgress(); ShowOfflineBattleVitals();
            OfflineBattleAction(false);
        }

        private void FinishOfflineBattle()
        {
            if (offlineActionRunning) return;
            if (offlineBattleRoot != null) Destroy(offlineBattleRoot);
            if (offlineBattleTexture != null) Destroy(offlineBattleTexture);
            offlineBattleTexture = null;
            offlineBattleRoot = null; activeOfflineMonster = null; offlineLeaveButton = null;
            activeRoamingMonster = null;
            offlineBattleOver = false; offlineActionRunning = false; offlineSkillButton = null; offlineSkillIcon = null;
            offlineBattleMoveInput = Vector2.zero;
            SaveOfflineProgress();
            RenderExplorationMap(offlinePreviewState);
            ShowStatus($"Ngoại tuyến · {offlineProgress.kills} trận thắng · {OfflineInventoryCount()} vật phẩm trong túi.");
        }

        private static void SetOfflineButtonLabel(Button button, string value)
        {
            var label = button == null ? null : button.GetComponentInChildren<Text>();
            if (label != null) label.text = value;
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
                        () => { if (offlinePreview) OfflineTravelTo(atlasSelectedTown); else TravelTo(atlasSelectedTown); }, info.transform);
                    Button("VÀO ĐỘNG", new Vector2(0.52f, 0.07f), new Vector2(0.96f, 0.25f), Panel,
                        () => { if (offlinePreview) EnterOfflineAtlasDungeon(state); else if (state.town?.id != atlasSelectedTown?.id) ShowStatus("Hãy tới thành trấn gắn với cổ động này trước."); else { SetAtlasOrientation(false); EnterDungeon(atlasSelectedDungeon.id); } }, info.transform);
                }
                else if (atlasSelectionKind == "monsters")
                {
                    Button(state.town?.id == atlasSelectedTown?.id ? "SĂN TIỂU YÊU" : "TỚI BÃI QUÁI", new Vector2(0.04f, 0.07f), new Vector2(0.55f, 0.25f), Gold,
                        () => { if (offlinePreview) EnterOfflineMonsterField(state); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPveTown(state, atlasSelectedTown); } else TravelTo(atlasSelectedTown); }, info.transform);
                    Button("PVP", new Vector2(0.59f, 0.07f), new Vector2(0.96f, 0.25f), Panel,
                        () => { if (offlinePreview) ShowStatus("PVP với người chơi thật cần máy chủ IPA online."); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPvp(state); } else ShowStatus("PVP chỉ mở tại thành trấn hiện tại."); }, info.transform);
                }
                else
                {
                    Button(state.town?.id == atlasSelectedTown?.id ? "PVE" : "NGỰ KIẾM TỚI", new Vector2(0.04f, 0.07f), new Vector2(0.48f, 0.25f), Gold,
                        () => { if (offlinePreview) OfflineTravelTo(atlasSelectedTown); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPveTown(state, atlasSelectedTown); } else TravelTo(atlasSelectedTown); }, info.transform);
                    Button("PVP", new Vector2(0.52f, 0.07f), new Vector2(0.96f, 0.25f), Panel,
                        () => { if (offlinePreview) ShowStatus("PVP với người chơi thật cần máy chủ IPA online."); else if (state.town?.id == atlasSelectedTown?.id) { SetAtlasOrientation(false); ShowPvp(state); } else ShowStatus("PVP chỉ mở tại thành trấn hiện tại."); }, info.transform);
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
            var current = state.town?.id == town.id ? "\n• Bạn đang ở đây" : "";
            if (atlasSelectionKind == "dungeon" && atlasSelectedDungeon != null)
                return $"{atlasSelectedDungeon.name}\n{map?.provinceName ?? map?.name}\nCảnh giới: {atlasSelectedDungeon.realmMin}\n\n{atlasSelectedDungeon.desc}\n\nGắn với: {town.name}{current}";
            if (atlasSelectionKind == "monsters")
                return $"BÃI QUÁI {atlasSelectedMonsterFieldLabel}\n{town.name} · {map?.provinceName ?? map?.name}\n\n{AtlasMonsterNames(town, atlasSelectedMonsterField, atlasSelectedMonsterFieldCount)}\n\nNhóm {atlasSelectedMonsterField + 1}/{atlasSelectedMonsterFieldCount} · {town.monsterPool?.Length ?? 0} loài trong khu vực{current}";
            var dungeonCount = CountTownDungeons(town.id);
            return $"{town.name}\n{map?.provinceName ?? map?.name}\nCảnh giới: {town.realmMinName ?? RealmLabel(town.realmMin)}\n\n{town.desc}\n\n{town.monsterPool?.Length ?? 0} loài yêu thú\n{dungeonCount} cổ động{current}";
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
                    names.Add(monster.name);
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
                label.font = BuiltinFont(); label.fontSize = 12; label.color = Cream;
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
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            if (canvasScaler != null) canvasScaler.referenceResolution = new Vector2(1920, 1080);
            buttonFontSize = 20;
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
            label.text = $"{town.name}\n{town.realmMinName ?? ""} · {(town.monsterPool?.Length ?? 0)} yêu thú · {dungeonCount} bí cảnh";
            AddRowPixelIcon(row, UiPixelIcon(PixelIconForEmoji(town.icon)), 48f);
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
            text.text = $"{dungeon.name}\n{dungeon.stamina} thể lực · yêu cầu {RealmLabel(dungeon.realmMin)}";
            AddRowPixelIcon(row, UiPixelIcon(PixelIconForEmoji(dungeon.icon, "co_dong")), 48f);
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
            label.text = $"{StripEmoji(opponent.fullName ?? opponent.name)}\n{opponent.realmName} · {opponent.power:N0} chiến lực · {opponent.points} điểm";
            AddRowPixelIcon(row, UiPixelIcon(opponent.isDemon ? "bat_quai" : "swords"), 44f);
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
            // the animated arena (same figures and effects as PvE); the classic view stays as the fallback
            if (battle != null && !battle.none && !battle.over && TryShowPvpArena(() => ShowPvpBattle(battle))) return;
            ClearBattleScene();
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
            var icon = LeadingPixelIcon(value);
            var label = ChildText(card.transform, "Text", 17, Muted, TextAnchor.MiddleLeft, new Vector2(icon != null ? 0.085f : 0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            label.text = StripEmoji(value);
            if (icon != null) PlacePixelIcon(card.transform, icon, new Vector2(0.02f, 0.2f), new Vector2(0.07f, 0.8f));
        }

        private Transform CreateScrollList(string name, float bottom, float top, float left = 0.02f, float right = 0.98f)
        {
            var listRoot = PanelObject(name, content.transform, new Vector2(left, bottom), new Vector2(right, top), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
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
                AddRowPixelIcon(card, LoadPixelIcon("PixelArt/Monsters/" + monster.monsterId), 76f);
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
            // Real-time action battle (joystick / skills / quick items); the classic view stays as a fallback.
            if (battle != null && !battle.over && !actionBattleFailed && AvatarComposer.Available && hub.IsObject)
            {
                ShowActionBattle(_ => ShowClassicBattle(battle));
                return;
            }
            ShowClassicBattle(battle);
        }

        private void ShowClassicBattle(BattleView battle)
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
            authScreenVersion++;
            accountLinksOpen = false;
            authRequestPending = false;
            authControls.Clear();
            authPrimaryButton = null;
            authPrimarySpinner = null;
            authPrimaryArrow = null;
            authCardRoot = null;
            if (authBackdrop != null) { authBackdrop.SetActive(false); Destroy(authBackdrop); authBackdrop = null; }
            status = null;
            statusMin = new Vector2(0.02f, 0.12f); statusMax = new Vector2(0.98f, 0.19f);
            if (content == null) return;
            for (var i = content.transform.childCount - 1; i >= 0; i--)
            {
                var child = content.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private Text Label(string value, int size, Color color, TextAnchor alignment, Vector2 min, Vector2 max, Transform parent = null)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent ?? content.transform, false);
            Place(obj.GetComponent<RectTransform>(), min, max);
            var text = obj.GetComponent<Text>();
            text.font = BuiltinFont(); text.fontSize = size; text.color = color; text.alignment = alignment;
            text.text = value; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            PixelUiSkin.ApplyTextTreatment(text);
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

            // FitInParent changes its own anchors: constrain it inside a fixed slot.
            var slot = new GameObject("TuTienGioiLogoSlot", typeof(RectTransform));
            slot.transform.SetParent(content.transform, false);
            Place(slot.GetComponent<RectTransform>(), min, max);
            var obj = new GameObject("TuTienGioiLogo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            obj.transform.SetParent(slot.transform, false);
            Place(obj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var image = obj.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            var aspect = obj.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)texture.width / texture.height;
        }

        private InputField Input(string label, string placeholder, Vector2 min, Vector2 max, bool secret, Transform parent = null)
        {
            var root = PanelObject(label, parent ?? content.transform, min, max, Vector2.zero, Vector2.zero, Panel);
            PixelUiSkin.ApplyFrame(root);
            var valueText = ChildText(root.transform, "Value", 22, Cream, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            var hint = ChildText(root.transform, "Placeholder", 21, Muted, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            hint.text = placeholder;
            var input = root.AddComponent<InputField>();
            input.targetGraphic = root.GetComponent<Image>();
            input.textComponent = valueText; input.placeholder = hint; input.contentType = secret ? InputField.ContentType.Password : (label == "email" ? InputField.ContentType.EmailAddress : InputField.ContentType.Standard);
            input.lineType = InputField.LineType.SingleLine;
            input.caretWidth = 2;
            input.selectionColor = new Color32(83, 129, 118, 180);
            return input;
        }

        private Text ChoiceSelector(string label, string[] choices, Vector2 min, Vector2 max, Action<int> selected, int initialIndex = 0)
        {
            var root = PanelObject(label, content.transform, min, max, Vector2.zero, Vector2.zero, Panel);
            PixelUiSkin.ApplyFrame(root);
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
            button.targetGraphic = root.GetComponent<Image>();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, .96f, .82f); colors.pressedColor = new Color(.76f, .83f, .79f); colors.disabledColor = new Color(.55f, .55f, .55f, .75f); button.colors = colors;
            var text = ChildText(root.transform, "Text", buttonFontSize, color == Gold ? Ink : Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.text = label; text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = buttonFontSize;
            PixelUiSkin.ApplyFrame(root);
            button.onClick.AddListener(() => click?.Invoke());
            return button;
        }

        private GameObject PanelObject(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax);
            var img = obj.GetComponent<Image>();
            img.color = color;
            if (color.a == 0f) img.raycastTarget = false;
            if (PixelUiSkin.NeedsFrame(name)) PixelUiSkin.ApplyFrame(obj);
            return obj;
        }

        private Text ChildText(Transform parent, string name, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), min, max);
            var text = obj.GetComponent<Text>(); text.font = BuiltinFont(); text.fontSize = size; text.color = color; text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            PixelUiSkin.ApplyTextTreatment(text);
            return text;
        }

        private static Font BuiltinFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
