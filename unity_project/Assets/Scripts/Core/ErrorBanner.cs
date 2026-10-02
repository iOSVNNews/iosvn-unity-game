using System;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Shows the last unhandled exception as a strip along the top of the screen. A phone has no console:
    /// without this a failure is only a blank screen, with it a screenshot says what went wrong and where.
    /// </summary>
    internal sealed class ErrorBanner : MonoBehaviour
    {
        private static ErrorBanner instance;
        private static bool busy;
        private Text label;
        private GameObject strip;
        private float hideAt;
        private string lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || busy || !Application.isPlaying) return;
            busy = true;
            try
            {
                if (instance == null) instance = Create();
                if (instance != null) instance.Show(condition, stackTrace);
            }
            catch (Exception)
            {
                // the banner must never become a second failure
            }
            finally { busy = false; }
        }

        private static ErrorBanner Create()
        {
            var root = new GameObject("ErrorBanner", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var banner = root.AddComponent<ErrorBanner>();

            var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image), typeof(Button));
            strip.transform.SetParent(root.transform, false);
            var rect = (RectTransform)strip.transform;
            rect.anchorMin = new Vector2(.14f, 1f);
            rect.anchorMax = new Vector2(.86f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(0, 132);
            rect.anchoredPosition = new Vector2(0, -6);
            strip.GetComponent<Image>().color = new Color32(74, 18, 16, 236);
            strip.GetComponent<Button>().onClick.AddListener(() => strip.SetActive(false));

            var text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(strip.transform, false);
            var tr = text.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(22, 8); tr.offsetMax = new Vector2(-22, -8);
            text.font = ModernUi.Regular;
            text.fontSize = 21;
            text.color = new Color32(255, 226, 214, 255);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;

            banner.label = text;
            banner.strip = strip;
            strip.SetActive(false);
            return banner;
        }

        private void Show(string condition, string stackTrace)
        {
            // the first frame of the stack that belongs to the game says where it happened
            var where = "";
            foreach (var line in (stackTrace ?? "").Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (where.Length == 0) where = trimmed;
                if (trimmed.Contains("IOSVN.")) { where = trimmed; break; }
            }
            var cut = where.IndexOf(" (at ", StringComparison.Ordinal);
            if (cut > 0) where = where.Substring(0, cut);
            var message = "Lỗi: " + condition + (where.Length > 0 ? "\n" + where : "") + "\nChụp màn hình này gửi cho người phát triển · chạm để ẩn";
            if (message == lastMessage && strip.activeSelf) { hideAt = Time.unscaledTime + 20f; return; }
            lastMessage = message;
            label.text = message;
            strip.SetActive(true);
            hideAt = Time.unscaledTime + 20f;
        }

        private void Update()
        {
            if (strip != null && strip.activeSelf && Time.unscaledTime >= hideAt) strip.SetActive(false);
        }
    }
}
