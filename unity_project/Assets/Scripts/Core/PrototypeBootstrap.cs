using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private GameObject content;
        private GameCatalog currentCatalog;
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
            BuildCanvas();
            ShowLogin();
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

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var background = PanelObject("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Ink);
            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeArea.transform.SetParent(background.transform, false);
            Place(safeArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            content = PanelObject("Content", safeArea.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        }

        private void ShowLogin()
        {
            ClearContent();
            Label("IOSVN  •  TU TIÊN", 36, Gold, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.79f), new Vector2(0.98f, 0.9f));
            Label("Đăng nhập để tiếp tục hành trình", 22, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.79f));

            emailInput = Input("Email", "email", new Vector2(0.06f, 0.61f), new Vector2(0.94f, 0.69f), false);
            passwordInput = Input("Mật khẩu", "password", new Vector2(0.06f, 0.51f), new Vector2(0.94f, 0.59f), true);
            Button("ĐĂNG NHẬP", new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.48f), Gold, () => SubmitAuth(false));
            Button("TẠO TÀI KHOẢN EMAIL", new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.38f), Panel, () => SubmitAuth(true));
            Label("Server game xử lý nhân vật, chiến đấu và vật phẩm.", 18, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.25f));
            ShowStatus("Kết nối tới máy chủ game IPA.");
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
                if (!state.registered) ShowCharacterCreation();
                else client.LoadCurrentBattle((battle, _) =>
                {
                    if (battle != null) ShowBattle(battle);
                    else ShowHome(state);
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
                gender,
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
            ClearContent();
            Label("TU TIÊN  •  CỬU CHÂU", 28, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.96f));
            var playerName = string.IsNullOrEmpty(state.player?.fullName) ? state.player?.name : state.player.fullName;
            Label($"{playerName ?? "Đạo hữu"}     •     {state.realm?.name ?? "Sơ nhập"}", 23, Cream, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.80f), new Vector2(0.98f, 0.87f));
            Label($"{state.town?.name ?? "Chưa rõ thành"}     •     {Math.Max(0, state.player?.stones ?? 0):N0} linh thạch", 19, Muted, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.74f), new Vector2(0.98f, 0.80f));
            Label("THIÊN CƠ TRUY TUNG", 22, Gold, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.67f), new Vector2(0.98f, 0.73f));
            Button("LÀM MỚI MỤC TIÊU", new Vector2(0.62f, 0.675f), new Vector2(0.98f, 0.725f), Panel, RefreshMonsters);
            var listRoot = PanelObject("MonsterList", content.transform, new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.66f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            var viewport = PanelObject("Viewport", listRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.015f));
            var mask = viewport.AddComponent<Mask>(); mask.showMaskGraphic = false;
            var scroll = listRoot.AddComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            var scrollContent = PanelObject("Content", viewport.transform, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            scrollContent.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            scroll.content = scrollContent.GetComponent<RectTransform>();
            var layout = scrollContent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12; layout.childControlHeight = false; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            scrollContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (state.worldMonsters != null) AddMonsterCards(state.worldMonsters, scrollContent.transform);
            Button("Đăng xuất", new Vector2(0.65f, 0.04f), new Vector2(0.98f, 0.105f), Panel, () => client.Logout(_ => ShowLogin()));
            ShowStatus("Hồ sơ và mục tiêu đồng bộ với máy chủ game IPA.");
        }

        private void RefreshMonsters()
        {
            ShowStatus("Đang truy tung mục tiêu...");
            client.LoadWorldMonsters((monsters, error) =>
            {
                if (monsters == null) { ShowStatus(error); return; }
                var root = content.transform.Find("MonsterList/Viewport/Content");
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
                var buttonLayout = hunt.AddComponent<LayoutElement>(); buttonLayout.preferredWidth = 190; buttonLayout.preferredHeight = 54;
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
            Label($"{battle.m?.icon}  {battle.m?.name ?? "Yêu thú"}\nHP {Math.Max(0, battle.m?.hp ?? 0):N0} / {Math.Max(0, battle.m?.maxHp ?? 0):N0}\n{warning}", 26, Cream, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.84f));
            Label($"{battle.p?.name ?? "Đạo hữu"}\nKhí huyết {Math.Max(0, battle.p?.hp ?? 0):N0} / {Math.Max(0, battle.p?.maxHp ?? 0):N0}     Linh lực {Math.Max(0, battle.p?.mp ?? 0):N0}", 22, Muted, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.62f));
            var log = battle.log == null ? "" : string.Join("\n", Array.ConvertAll(battle.log, line => line?.text ?? ""));
            Label(log, 18, Cream, TextAnchor.LowerLeft, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.50f));
            if (!battle.over)
            {
                Button("TẤN CÔNG", new Vector2(0.05f, 0.20f), new Vector2(0.48f, 0.28f), Gold, () => SendBattleAction("attack"));
                Button("NÉ ĐÒN", new Vector2(0.52f, 0.20f), new Vector2(0.95f, 0.28f), Panel, () => SendBattleAction("dodge"));
                Button("RÚT LUI", new Vector2(0.20f, 0.09f), new Vector2(0.80f, 0.17f), Panel, () => SendBattleAction("flee"));
            }
            else Button("TRỞ VỀ", new Vector2(0.20f, 0.09f), new Vector2(0.80f, 0.17f), Gold, () => LoadState());
            ShowStatus(battle.over ? $"Trận đã kết thúc: {battle.result}" : "Thao tác được máy chủ xác nhận.");
        }

        private void SendBattleAction(string action)
        {
            ShowStatus("Đang gửi thao tác chiến đấu...");
            client.BattleAct(action, (result, error) =>
            {
                if (result?.battle == null) { ShowStatus(error); return; }
                ShowBattle(result.battle);
                if (!string.IsNullOrWhiteSpace(result.result?.msg)) ShowStatus(result.result.msg);
            });
        }

        private void ShowStatus(string message)
        {
            if (status == null)
                status = Label("", 17, Muted, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.19f));
            status.text = string.IsNullOrEmpty(message) ? "Có lỗi kết nối máy chủ." : message;
        }

        private void ClearContent()
        {
            status = null;
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
