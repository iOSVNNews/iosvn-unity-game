using System;
using QuyCocBatHoang.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    public sealed partial class PrototypeBootstrap
    {
        private const float OpeningIntroDuration = 7.2f;
        private GameObject openingIntroRoot;
        private CanvasGroup openingIntroGroup;
        private CanvasGroup openingBrandGroup;
        private RectTransform openingDragonRect;
        private Image openingLeftEye;
        private Image openingRightEye;
        private Image[] openingMists = Array.Empty<Image>();
        private Vector2[] openingMistOrigins = Array.Empty<Vector2>();
        private Image[] openingMotes = Array.Empty<Image>();
        private float[] openingMotePhases = Array.Empty<float>();
        private bool startupIntroFinished;
        private bool startupPatchFinished;
        private bool startupPatchSucceeded;
        private string startupPatchMessage;
        private AssetDownloadManager.MajorUpdateInfo startupMajorUpdate;
        private float openingIntroStartedAt;

        private void BeginOpeningIntro()
        {
            startupIntroFinished = false;
            var painting = Resources.Load<Texture2D>("Brand/OpeningDragonInk");
            if (painting == null)
            {
                startupIntroFinished = true;
                Debug.LogWarning("OpeningDragonInk is missing; continuing to startup checks.");
                return;
            }

            openingIntroRoot = new GameObject("OpeningCinematic", typeof(RectTransform), typeof(CanvasGroup));
            openingIntroRoot.transform.SetParent(canvas.transform, false);
            var root = openingIntroRoot.GetComponent<RectTransform>();
            Place(root, Vector2.zero, Vector2.one);
            openingIntroGroup = openingIntroRoot.GetComponent<CanvasGroup>();
            openingIntroGroup.alpha = 0f;

            // Full-screen tap target lets players skip the short opening.
            var hitArea = new GameObject("SkipOnTap", typeof(RectTransform), typeof(Image), typeof(Button));
            hitArea.transform.SetParent(openingIntroRoot.transform, false);
            Place(hitArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var hitImage = hitArea.GetComponent<Image>();
            hitImage.color = new Color(0f, 0f, 0f, 0f);
            hitImage.raycastTarget = true;
            var hitButton = hitArea.GetComponent<Button>();
            hitButton.transition = Selectable.Transition.None;
            hitButton.targetGraphic = hitImage;
            hitButton.onClick.AddListener(SkipOpeningIntro);

            var art = new GameObject("DragonPainting", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            art.transform.SetParent(openingIntroRoot.transform, false);
            openingDragonRect = art.GetComponent<RectTransform>();
            openingDragonRect.anchorMin = openingDragonRect.anchorMax = new Vector2(.5f, .5f);
            openingDragonRect.pivot = new Vector2(.5f, .5f);
            openingDragonRect.sizeDelta = Vector2.zero;
            var artImage = art.GetComponent<RawImage>();
            artImage.texture = painting;
            artImage.raycastTarget = false;
            var aspect = art.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = (float)painting.width / painting.height;

            var vignette = OpeningImage("Vignette", openingIntroRoot.transform, InkUi.Vignette, Vector2.zero, Vector2.one,
                new Color(.10f, .11f, .13f, .43f));
            vignette.raycastTarget = false;

            openingMists = new Image[3];
            openingMistOrigins = new Vector2[openingMists.Length];
            for (var i = 0; i < openingMists.Length; i++)
            {
                var minY = .015f + i * .055f;
                var maxY = .34f + i * .045f;
                var mist = OpeningImage("Mist" + i, openingIntroRoot.transform, InkUi.Cloud,
                    new Vector2(-.08f, minY), new Vector2(1.08f, maxY), new Color32(227, 232, 233, (byte)(54 + i * 12)));
                mist.raycastTarget = false;
                mist.preserveAspect = false;
                mist.rectTransform.localScale = new Vector3(1.08f + i * .04f, 1f, 1f);
                openingMists[i] = mist;
                openingMistOrigins[i] = new Vector2((i - 1) * 28f, 0f);
            }

            openingLeftEye = OpeningGlow("EyeGlowLeft", openingIntroRoot.transform, new Vector2(.624f, .505f), new Vector2(84f, 62f));
            openingRightEye = OpeningGlow("EyeGlowRight", openingIntroRoot.transform, new Vector2(.667f, .51f), new Vector2(84f, 62f));

            openingMotes = new Image[12];
            openingMotePhases = new float[openingMotes.Length];
            var random = new System.Random(745319);
            for (var i = 0; i < openingMotes.Length; i++)
            {
                var x = (float)random.NextDouble() * .94f + .03f;
                var y = (float)random.NextDouble() * .74f + .08f;
                var size = 7f + (float)random.NextDouble() * 11f;
                var mote = OpeningGlow("Mote" + i, openingIntroRoot.transform, new Vector2(x, y), new Vector2(size, size));
                mote.color = new Color32(255, 214, 139, 0);
                openingMotes[i] = mote;
                openingMotePhases[i] = (float)random.NextDouble() * Mathf.PI * 2f;
            }

            var brand = new GameObject("OpeningBrand", typeof(RectTransform), typeof(CanvasGroup));
            brand.transform.SetParent(openingIntroRoot.transform, false);
            var brandRect = brand.GetComponent<RectTransform>();
            Place(brandRect, Vector2.zero, Vector2.one);
            openingBrandGroup = brand.GetComponent<CanvasGroup>();
            openingBrandGroup.alpha = 0f;

            var logo = Resources.Load<Texture2D>("Brand/TuTienGioi_Logo");
            if (logo != null)
            {
                var slot = Anchored("LogoSlot", brandRect, new Vector2(.015f, .025f), new Vector2(.215f, .355f), Vector2.zero, Vector2.zero);
                var logoObject = new GameObject("Logo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                logoObject.transform.SetParent(slot, false);
                Place(logoObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
                var logoImage = logoObject.GetComponent<RawImage>();
                logoImage.texture = logo;
                logoImage.raycastTarget = false;
                var logoAspect = logoObject.GetComponent<AspectRatioFitter>();
                logoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                logoAspect.aspectRatio = (float)logo.width / logo.height;
            }

            var subtitle = AnchoredText(brandRect, "Subtitle", "HÀNH TRÌNH TU TIÊN BẮT ĐẦU", ModernUi.SemiBold, 25,
                new Color32(247, 234, 205, 255), TextAnchor.MiddleCenter,
                new Vector2(.29f, .045f), new Vector2(.73f, .11f), Vector2.zero, Vector2.zero);
            subtitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, .8f);
            var outline = subtitle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.12f, .10f, .08f, .85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var skip = Anchored("Skip", openingIntroRoot.transform, new Vector2(.86f, .89f), new Vector2(.975f, .965f), Vector2.zero, Vector2.zero);
            var skipImage = skip.gameObject.AddComponent<Image>();
            ModernUi.Fill(skipImage, 22f);
            skipImage.color = new Color32(17, 22, 27, 188);
            var skipLabel = AnchoredText(skip, "Label", "BỎ QUA  ›", ModernUi.SemiBold, 20,
                new Color32(246, 231, 198, 255), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            skipLabel.resizeTextForBestFit = true;
            skipLabel.resizeTextMinSize = 15;
            skipLabel.resizeTextMaxSize = 20;
            var skipButton = skip.gameObject.AddComponent<Button>();
            skipButton.transition = Selectable.Transition.None;
            skipButton.targetGraphic = skipImage;
            skipButton.onClick.AddListener(SkipOpeningIntro);

            openingIntroStartedAt = Time.unscaledTime;
            UpdateOpeningIntroVisuals(0f);
        }

        private Image OpeningImage(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max, Color color)
        {
            var rect = Anchored(name, parent, min, max, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Image OpeningGlow(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var rect = Anchored(name, parent, anchor, anchor, -size * .5f, size * .5f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = InkUi.Glow;
            image.color = Color.clear;
            image.raycastTarget = false;
            return image;
        }

        private void UpdateOpeningIntro()
        {
            if (startupIntroFinished || openingIntroRoot == null) return;
            var elapsed = Time.unscaledTime - openingIntroStartedAt;
            UpdateOpeningIntroVisuals(elapsed);
            if (elapsed >= OpeningIntroDuration) FinishOpeningIntro();
        }

        private void UpdateOpeningIntroVisuals(float elapsed)
        {
            if (openingIntroGroup == null) return;
            var fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .9f));
            var fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((OpeningIntroDuration - elapsed) / .75f));
            openingIntroGroup.alpha = Mathf.Min(fadeIn, fadeOut);

            var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / OpeningIntroDuration));
            if (openingDragonRect != null)
            {
                openingDragonRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.105f, progress);
                openingDragonRect.anchoredPosition = new Vector2(Mathf.Lerp(-34f, 26f, progress), Mathf.Sin(elapsed * .28f) * 5f);
            }

            for (var i = 0; i < openingMists.Length; i++)
            {
                var mist = openingMists[i];
                if (mist == null) continue;
                var origin = openingMistOrigins[i];
                mist.rectTransform.anchoredPosition = origin + new Vector2(Mathf.Sin(elapsed * (.17f + i * .025f) + i) * 68f, Mathf.Cos(elapsed * .19f + i) * 5f);
                var c = mist.color;
                c.a = (.18f + i * .035f) * (0.82f + Mathf.Sin(elapsed * .65f + i) * .18f);
                mist.color = c;
            }

            var eyePulse = .18f + .18f * (0.5f + 0.5f * Mathf.Sin(elapsed * 2.2f));
            if (openingLeftEye != null) openingLeftEye.color = new Color(1f, .48f, .08f, eyePulse);
            if (openingRightEye != null) openingRightEye.color = new Color(1f, .48f, .08f, eyePulse * .9f);

            for (var i = 0; i < openingMotes.Length; i++)
            {
                var mote = openingMotes[i];
                if (mote == null) continue;
                var phase = openingMotePhases[i];
                mote.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(elapsed * .31f + phase) * 18f, Mathf.Sin(elapsed * .52f + phase) * 24f);
                var c = mote.color;
                c.a = (.12f + .24f * Mathf.Max(0f, Mathf.Sin(elapsed * 1.15f + phase))) * openingIntroGroup.alpha;
                mote.color = c;
            }

            if (openingBrandGroup != null)
            {
                var brandIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 1.15f) / .9f));
                openingBrandGroup.alpha = brandIn * openingIntroGroup.alpha;
            }
        }

        private void SkipOpeningIntro() => FinishOpeningIntro();

        private void FinishOpeningIntro()
        {
            if (startupIntroFinished) return;
            startupIntroFinished = true;
            if (openingIntroRoot != null) Destroy(openingIntroRoot);
            openingIntroRoot = null;
            openingIntroGroup = null;
            openingBrandGroup = null;
            openingDragonRect = null;
            openingLeftEye = null;
            openingRightEye = null;
            openingMists = Array.Empty<Image>();
            openingMistOrigins = Array.Empty<Vector2>();
            openingMotes = Array.Empty<Image>();
            openingMotePhases = Array.Empty<float>();
            TryFinishStartupSequence();
        }

        private void TryFinishStartupSequence()
        {
            if (!startupIntroFinished) return;
            if (startupMajorUpdate != null)
            {
                if (!majorUpdateShowing)
                {
                    majorUpdateShowing = true;
                    ShowMajorUpdateDialog(startupMajorUpdate);
                }
                return;
            }
            if (!startupPatchFinished) return;
            ShowLogin(startupPatchSucceeded ? startupPatchMessage : "Không cập nhật được tài nguyên: " + startupPatchMessage);
        }

        private void ReceiveStartupMajorUpdate(AssetDownloadManager.MajorUpdateInfo info)
        {
            startupMajorUpdate = info;
            TryFinishStartupSequence();
        }

        private void CompleteStartupPatchCheck(bool success, string message)
        {
            startupPatchFinished = true;
            startupPatchSucceeded = success;
            startupPatchMessage = message;
            TryFinishStartupSequence();
        }
    }
}
