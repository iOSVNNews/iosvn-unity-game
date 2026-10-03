using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    public sealed class AtlasMapGesture : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        private RectTransform viewport;
        private RectTransform layer;
        private Slider zoomSlider;
        private Vector2 previousPointer;

        public void Initialize(RectTransform view, RectTransform mapLayer) { viewport = view; layer = mapLayer; }
        public void SetZoomSlider(Slider slider) { zoomSlider = slider; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (viewport != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out var local))
                previousPointer = local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (layer == null || viewport == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out var local)) return;
            layer.anchoredPosition += local - previousPointer;
            previousPointer = local;
            ClampPan();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (zoomSlider == null) return;
            zoomSlider.value = Mathf.Clamp(zoomSlider.value + eventData.scrollDelta.y * .12f, zoomSlider.minValue, zoomSlider.maxValue);
        }

        public void ApplyZoom(float zoom)
        {
            if (layer == null) return;
            layer.localScale = Vector3.one * zoom;
            ClampPan();
        }

        public void ResetView()
        {
            if (layer == null) return;
            layer.anchoredPosition = Vector2.zero;
            ClampPan();
        }

        private void ClampPan()
        {
            if (layer == null || viewport == null) return;
            var scaledSize = layer.rect.size * layer.localScale.x;
            var maxX = Mathf.Max(0f, (scaledSize.x - viewport.rect.width) * .5f);
            var maxY = Mathf.Max(0f, (scaledSize.y - viewport.rect.height) * .5f);
            var position = layer.anchoredPosition;
            layer.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -maxX, maxX), Mathf.Clamp(position.y, -maxY, maxY));
        }
    }
}
