using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Gives atlas markers a small pixel-scale bob and breathing pulse over the still atlas art.</summary>
    public sealed class PixelMapMarkerMotion : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 origin;
        private float phase;
        private Image image;
        private Color baseColor;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            origin = rect != null ? rect.anchoredPosition : Vector2.zero;
            phase = (Mathf.Abs(gameObject.name.GetHashCode()) % 1000) * .006f;
            image = GetComponent<Image>();
            if (image != null) baseColor = image.color;
        }

        private void Update()
        {
            if (rect == null) return;
            var time = Time.unscaledTime * 1.45f + phase;
            var pulse = .96f + Mathf.Sin(time) * .045f;
            rect.localScale = new Vector3(pulse, pulse, 1f);
            rect.anchoredPosition = origin + new Vector2(0f, Mathf.Sin(time * 1.7f) * 2.0f);
            if (image != null && baseColor.a > .8f)
            {
                var color = baseColor;
                color.a *= .88f + (Mathf.Sin(time * 1.2f) + 1f) * .06f;
                image.color = color;
            }
        }
    }
}
