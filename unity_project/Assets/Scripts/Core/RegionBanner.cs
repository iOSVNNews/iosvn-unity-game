using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// The big brush title that fades in over the map when the traveller crosses into a new region
    /// (as Quỷ Cốc announces each prefecture), then fades away.
    /// </summary>
    public sealed class RegionBanner : MonoBehaviour
    {
        private CanvasGroup group;
        private float age;
        private const float FadeIn = .6f, Hold = 2.2f, FadeOut = 1.1f;

        public static void Show(RectTransform parent, string title, string subtitle, Font display, Font regular)
        {
            if (parent == null || string.IsNullOrEmpty(title)) return;
            var old = parent.Find("RegionBanner");
            if (old != null) Destroy(old.gameObject);
            var go = new GameObject("RegionBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(RegionBanner));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(.5f, .68f);
            rect.anchorMax = new Vector2(.5f, .68f);
            rect.sizeDelta = new Vector2(760f, 150f);
            var band = new GameObject("Band", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            band.transform.SetParent(rect, false);
            var br = band.rectTransform;
            br.anchorMin = new Vector2(0, .18f); br.anchorMax = new Vector2(1, .82f);
            br.offsetMin = br.offsetMax = Vector2.zero;
            band.sprite = InkUi.Glow;
            band.color = new Color(0f, 0f, 0f, .42f);
            band.raycastTarget = false;
            Text Line(string name, string text, Font font, int size, Color color, float y0, float y1)
            {
                var t = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                t.transform.SetParent(rect, false);
                var r = t.rectTransform;
                r.anchorMin = new Vector2(0, y0); r.anchorMax = new Vector2(1, y1);
                r.offsetMin = r.offsetMax = Vector2.zero;
                t.font = font;
                t.fontSize = size;
                t.color = color;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.text = text;
                t.raycastTarget = false;
                var shadow = t.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .8f);
                shadow.effectDistance = new Vector2(2f, -2f);
                return t;
            }
            Line("Title", title.ToUpperInvariant(), display, 62, new Color32(255, 232, 178, 255), .3f, 1f);
            if (!string.IsNullOrEmpty(subtitle)) Line("Subtitle", subtitle, regular, 20, new Color32(232, 222, 204, 255), 0f, .34f);
            var banner = go.GetComponent<RegionBanner>();
            banner.group = go.GetComponent<CanvasGroup>();
            banner.group.alpha = 0f;
            banner.group.blocksRaycasts = false;
            banner.group.interactable = false;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            float a;
            if (age < FadeIn) a = age / FadeIn;
            else if (age < FadeIn + Hold) a = 1f;
            else a = 1f - (age - FadeIn - Hold) / FadeOut;
            if (group != null) group.alpha = Mathf.Clamp01(a);
            transform.localScale = Vector3.one * (1f + .04f * Mathf.Clamp01(age / (FadeIn + Hold + FadeOut)));
            if (age > FadeIn + Hold + FadeOut) Destroy(gameObject);
        }
    }
}
