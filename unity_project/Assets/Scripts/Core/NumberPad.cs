using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Full-width quantity keypad with an explicit dismissal path on iOS.</summary>
    [ExecuteAlways]
    public sealed class NumberPad : MonoBehaviour
    {
        private InputField field;
        private RectTransform card;
        private Canvas canvas;
        private long min, max;
        private GameObject overlay;
        private Text valueLabel;
        private Vector3 originalPosition, originalScale;
        private bool replaceOnDigit;

        public static NumberPad Attach(InputField field, RectTransform card, Canvas canvas, long min, long max)
        {
            field.DeactivateInputField();
            field.readOnly = true;
            field.enabled = false; // This quantity field must never open the native number pad.
            var pad = field.gameObject.AddComponent<NumberPad>();
            pad.field = field; pad.card = card; pad.canvas = canvas; pad.min = min; pad.max = max;
            var trigger = Node("OpenNumberPad", field.transform, Vector2.zero, Vector2.one);
            var hit = trigger.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = trigger.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.onClick.AddListener(pad.Open);
            return pad;
        }

        public void Open()
        {
            if (overlay != null || canvas == null || card == null) return;
            originalPosition = card.position; originalScale = card.localScale;
            replaceOnDigit = true;
            var root = Node("QuantityKeyboard", canvas.transform, Vector2.zero, Vector2.one);
            overlay = root.gameObject;
            var dismiss = root.gameObject.AddComponent<Image>();
            dismiss.color = Color.clear;
            var outside = root.gameObject.AddComponent<Button>();
            outside.transition = Selectable.Transition.None;
            outside.onClick.AddListener(Close);
            var panel = Node("Keypad", root, Vector2.zero, new Vector2(1, .4f));
            panel.gameObject.AddComponent<Image>().color = new Color32(16, 29, 35, 255);
            var heading = Node("Heading", panel, new Vector2(.02f, .82f), new Vector2(.80f, 1));
            valueLabel = Label(heading, "", 26);
            Key(panel, "Đóng", new Vector2(.82f, .82f), new Vector2(.98f, 1), Close);
            var keys = Node("Keys", panel, new Vector2(0, 0), new Vector2(1, .81f));
            keys.offsetMin = new Vector2(16, 12 + (Screen.height > 0 ? Screen.safeArea.yMin / Screen.height * ((RectTransform)canvas.transform).rect.height : 0));
            keys.offsetMax = new Vector2(-16, -4);
            var labels = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Xóa", "0", "Xong" };
            for (var i = 0; i < labels.Length; i++)
            {
                var key = labels[i];
                var col = i % 3; var row = i / 3;
                Key(keys, key, new Vector2(col / 3f, 1 - (row + 1) / 4f), new Vector2((col + 1) / 3f, 1 - row / 4f), () => Press(key));
            }
            var canvasRect = (RectTransform)canvas.transform;
            card.position = canvasRect.TransformPoint(new Vector3(0, canvasRect.rect.yMin + canvasRect.rect.height * .72f, 0));
            var scale = Mathf.Min(1f, canvasRect.rect.height * .54f / Mathf.Max(1f, card.rect.height));
            card.localScale = originalScale * scale;
            RefreshValue();
        }

        public void Press(string key)
        {
            if (key == "Xong") { Close(); return; }
            if (key == "Xóa" || key == "⌫")
            {
                replaceOnDigit = false;
                field.text = field.text.Length > 0 ? field.text.Substring(0, field.text.Length - 1) : "";
            }
            else if (key.Length == 1 && key[0] >= '0' && key[0] <= '9')
            {
                var text = replaceOnDigit || field.text == "0" ? key : field.text + key;
                replaceOnDigit = false;
                if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    field.text = Math.Min(max, number).ToString(CultureInfo.InvariantCulture);
            }
            RefreshValue();
        }

        private void RefreshValue()
        {
            if (valueLabel != null) valueLabel.text = $"Số lượng: {(field.text.Length == 0 ? "0" : field.text)}  ·  Tối đa {max:N0}";
        }

        public void Close()
        {
            if (overlay == null) return;
            if (field != null)
            {
                if (!long.TryParse(field.text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)) value = min;
                field.text = Math.Max(min, Math.Min(max, value)).ToString(CultureInfo.InvariantCulture);
            }
            overlay.SetActive(false);
            if (Application.isPlaying) Destroy(overlay); else DestroyImmediate(overlay);
            overlay = null;
            if (card != null) { card.position = originalPosition; card.localScale = originalScale; }
        }

        private void OnDisable() => Close();
        private void OnDestroy() => Close();

        private static RectTransform Node(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = min; node.anchorMax = max;
            node.offsetMin = node.offsetMax = Vector2.zero;
            return node;
        }

        private static Text Label(RectTransform rect, string text, int size)
        {
            var label = rect.gameObject.AddComponent<Text>();
            label.font = ModernUi.SemiBold; label.fontSize = size;
            label.color = new Color32(239, 235, 220, 255); label.text = text;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 18; label.resizeTextMaxSize = size;
            return label;
        }

        private static void Key(RectTransform parent, string text, Vector2 min, Vector2 max, Action click)
        {
            var key = Node("Key_" + text, parent, min, max);
            key.offsetMin = new Vector2(4, 4); key.offsetMax = new Vector2(-4, -4);
            var fill = key.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 10);
            fill.color = text == "Xong" ? new Color32(173, 129, 66, 255) : new Color32(39, 58, 65, 255);
            var button = key.gameObject.AddComponent<Button>();
            button.targetGraphic = fill; button.onClick.AddListener(() => click());
            Label(Node("Label", key, Vector2.zero, Vector2.one), text, 32);
        }
    }
}
