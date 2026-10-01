using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>Full-bleed pixel scenery with inexpensive ambient UI particles.</summary>
    public sealed class LoginBackdrop : MonoBehaviour
    {
        private RectTransform root;
        private RectTransform scenery;
        private float aspect;
        private readonly RectTransform[] motes = new RectTransform[28];
        private readonly Image[] moteImages = new Image[28];
        private Vector2 lastSize;

        public static GameObject Create(Transform parent, Texture2D texture)
        {
            var obj = new GameObject("LoginBackdrop", typeof(RectTransform), typeof(LoginBackdrop));
            obj.transform.SetParent(parent, false);
            obj.transform.SetAsFirstSibling();
            var view = obj.GetComponent<LoginBackdrop>();
            view.root = obj.GetComponent<RectTransform>();
            Stretch(view.root);
            if (texture != null)
            {
                var art = new GameObject("PixelLandscape", typeof(RectTransform), typeof(RawImage));
                art.transform.SetParent(obj.transform, false);
                view.scenery = art.GetComponent<RectTransform>();
                view.scenery.anchorMin = view.scenery.anchorMax = new Vector2(.5f, .5f);
                var image = art.GetComponent<RawImage>();
                image.texture = texture;
                image.raycastTarget = false;
                view.aspect = (float)texture.width / texture.height;
            }
            var shade = new GameObject("AtmosphereShade", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(obj.transform, false);
            Stretch(shade.GetComponent<RectTransform>());
            shade.GetComponent<Image>().color = new Color32(7, 16, 27, 65);
            shade.GetComponent<Image>().raycastTarget = false;
            for (var i = 0; i < view.motes.Length; i++)
            {
                var mote = new GameObject("LanternMote_" + i, typeof(RectTransform), typeof(Image));
                mote.transform.SetParent(obj.transform, false);
                var rect = mote.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = Vector2.one * (i % 4 == 0 ? 4 : 2);
                view.motes[i] = rect;
                view.moteImages[i] = mote.GetComponent<Image>();
                view.moteImages[i].raycastTarget = false;
            }
            return obj;
        }

        private void Update()
        {
            if (root == null) return;
            var size = root.rect.size;
            if (size != lastSize && scenery != null)
            {
                var height = Mathf.Max(size.y, size.x / aspect);
                scenery.sizeDelta = new Vector2(height * aspect, height);
                lastSize = size;
            }
            var time = Time.unscaledTime;
            for (var i = 0; i < motes.Length; i++)
            {
                // Keep the central form quiet; tiny lantern lights drift at the sides.
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * size.x * (.27f + (i * .037f) % .2f);
                var y = (Mathf.Repeat(i * .137f + time * (.007f + i % 3 * .001f), 1f) - .5f) * size.y;
                motes[i].anchoredPosition = new Vector2(Mathf.Round(x + Mathf.Sin(time * .25f + i) * 12f), Mathf.Round(y));
                var alpha = .1f + (.5f + .5f * Mathf.Sin(time * .7f + i * 1.4f)) * .38f;
                moteImages[i].color = new Color(1f, .77f, .35f, alpha);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
