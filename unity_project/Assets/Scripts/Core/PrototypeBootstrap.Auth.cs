using System;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Landscape account screens: one glass card split into a brand column (title,
    /// offline entry points) and a form column (tabs, fields, primary action).
    /// Sizes are chosen for legibility on iPhone landscape (about 2.5 canvas units per point); the card
    /// grows to fill a phone screen, so fields and type end up larger than the nominal sizes below.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private const float AuthCardWidth = 1300f;
        private const float AuthCardHeight = 740f;
        private const float AuthRegisterHeight = 812f;
        private float authCardHeight = AuthCardHeight;
        private const float AuthCardRadius = 36f;
        private const float AuthBrandWidth = 460f;
        private const float AuthBrandInset = 14f;
        private const float AuthBrandTextX = 56f;
        private const float AuthBrandTextWidth = 364f;
        private const float AuthFormX = 520f;
        private const float AuthFormWidth = 720f;
        private const float AuthFieldHeight = 100f;

        private static readonly Color AuthTextPrimary = new Color32(240, 231, 210, 255);
        private static readonly Color AuthTextSecondary = new Color32(184, 193, 202, 255);
        private static readonly Color AuthTextTertiary = new Color32(132, 145, 157, 255);
        private static readonly Color AuthGoldTop = new Color32(249, 224, 164, 255);
        private static readonly Color AuthGoldBottom = new Color32(206, 152, 68, 255);
        private static readonly Color AuthGoldAccent = new Color32(228, 188, 108, 255);
        private static readonly Color AuthInkOnGold = new Color32(43, 28, 9, 255);
        private static readonly Color AuthError = new Color32(255, 146, 130, 255);

        public const string PrefKeySavedAccount = "tutien_saved_auth_identity";
        public const string PrefKeySavedPassword = "tutien_saved_auth_password";

        private RectTransform authCardRoot;
        private Image authPrimarySpinner;
        private Image authPrimaryArrow;
        private bool authTabSwitch;

        private void ShowAccountForm(bool createAccount)
        {
            var savedAccount = PlayerPrefs.GetString(PrefKeySavedAccount, "");
            var savedPassword = PlayerPrefs.GetString(PrefKeySavedPassword, "");
            var previousIdentity = emailInput != null ? emailInput.text : (!createAccount ? savedAccount : string.Empty);
            var previousPassword = passwordInput != null ? passwordInput.text : (!createAccount ? savedPassword : string.Empty);
            var tabSwitch = authTabSwitch;
            authTabSwitch = false;
            PrepareAccountScreen();
            passwordConfirmationInput = null;

            var card = BuildAuthCard(!tabSwitch, createAccount ? AuthRegisterHeight : (string.IsNullOrEmpty(savedAccount) ? AuthCardHeight : AuthCardHeight + 50f));
            var grow = authCardHeight - AuthCardHeight;
            AuthBrandHeader(card,
                createAccount ? "KHỞI ĐẦU\nTIÊN LỘ" : "CHÀO MỪNG\nĐẠO HỮU",
                createAccount ? "Tạo tài khoản để lưu hành trình tu luyện trên mọi thiết bị." : "Đăng nhập để tiếp tục hành trình tu luyện của bạn.");
            // Official game server info on the brand panel
            var badgeY = 460f + grow;
            var infoBox = AuthNode("ServerInfoBox", card, AuthBrandTextX, badgeY, AuthBrandTextWidth, 190f);
            var infoFill = infoBox.gameObject.AddComponent<Image>();
            ModernUi.Fill(infoFill, 18f);
            infoFill.color = new Color32(2, 7, 12, 130);
            var infoEdge = AuthImage(infoBox, "Edge", 0f, 0f, AuthBrandTextWidth, 190f);
            ModernUi.Ring(infoEdge, 18f, 1.2f);
            infoEdge.color = new Color32(240, 228, 204, 38);

            AuthText(infoBox, "ServerLabel", "MÁY CHỦ CHÍNH THỨC", ModernUi.Bold, 20, AuthGoldAccent, TextAnchor.MiddleLeft, 22f, 16f, AuthBrandTextWidth - 44f, 26f);
            AuthText(infoBox, "ServerLine1", "• Thế giới mở Quỷ Cốc Bát Hoang", ModernUi.Regular, 19, AuthTextSecondary, TextAnchor.MiddleLeft, 22f, 52f, AuthBrandTextWidth - 44f, 26f);
            AuthText(infoBox, "ServerLine2", "• Tu tiên độ kiếp · Trảm yêu trừ ma", ModernUi.Regular, 19, AuthTextSecondary, TextAnchor.MiddleLeft, 22f, 84f, AuthBrandTextWidth - 44f, 26f);
            AuthText(infoBox, "ServerLine3", "• Đấu pháp liên server & Bang phái", ModernUi.Regular, 19, AuthTextSecondary, TextAnchor.MiddleLeft, 22f, 116f, AuthBrandTextWidth - 44f, 26f);
            AuthText(infoBox, "ServerLine4", "• Trực tuyến: tutien.iosvn.com.vn", ModernUi.Medium, 17, AuthTextTertiary, TextAnchor.MiddleLeft, 22f, 150f, AuthBrandTextWidth - 44f, 24f);

            // Creating an account stacks three full-width fields, so a long password is readable while typing.
            var top = createAccount ? 44f : 60f;
            var row = createAccount ? 154f : 162f;
            var first = createAccount ? 148f : 184f;
            AuthTabs(card, AuthFormX, top, AuthFormWidth, createAccount ? 84f : 88f, createAccount, tabSwitch);

            emailInput = AuthField(card, "account", "Tài khoản hoặc email",
                createAccount ? "3–24 ký tự không dấu, hoặc email" : "Nhập tên tài khoản hoặc email",
                "user", AuthFormX, first, AuthFormWidth, false);
            emailInput.text = previousIdentity;
            emailInput.characterLimit = 254;
            AuthControl(emailInput);

            if (createAccount)
            {
                passwordInput = AuthField(card, "password", "Mật khẩu", "Từ 10 ký tự trở lên", "lock", AuthFormX, first + row, AuthFormWidth, true);
                passwordConfirmationInput = AuthField(card, "passwordConfirmation", "Nhập lại mật khẩu", "Nhập lại mật khẩu vừa đặt", "lock", AuthFormX, first + row * 2f, AuthFormWidth, true);
                passwordConfirmationInput.characterLimit = 128;
                AuthControl(passwordConfirmationInput);
            }
            else
            {
                passwordInput = AuthField(card, "password", "Mật khẩu", "Nhập mật khẩu", "lock", AuthFormX, first + row, AuthFormWidth, true);
                if (!string.IsNullOrEmpty(previousPassword)) passwordInput.text = previousPassword;
            }
            passwordInput.characterLimit = 128;
            AuthControl(passwordInput);

            // A phone keyboard closes on Return; opening the next field at once makes it drop and rise again,
            // so only a hardware keyboard walks from field to field.
            var chain = !TouchScreenKeyboard.isSupported;
            emailInput.onSubmit.AddListener(_ => { if (chain && passwordInput != null) passwordInput.ActivateInputField(); });
            passwordInput.onSubmit.AddListener(_ =>
            {
                if (!createAccount) SubmitAuth(false);
                else if (chain && passwordConfirmationInput != null) passwordConfirmationInput.ActivateInputField();
            });
            if (passwordConfirmationInput != null) passwordConfirmationInput.onSubmit.AddListener(_ => SubmitAuth(true));

            status = AuthText(card, "AuthFeedback",
                createAccount ? "Tạo tài khoản xong sẽ tự động lưu và đăng nhập vào game." : (!string.IsNullOrEmpty(savedAccount) ? "Tài khoản đã lưu trên thiết bị. Bấm Đăng nhập nhanh để vào game." : "Hồ sơ và nhân vật của bạn được lưu trên máy chủ game."),
                ModernUi.Regular, 24, AuthTextSecondary, TextAnchor.MiddleLeft, AuthFormX + 4f, createAccount ? 606f : 500f, AuthFormWidth - 8f, createAccount ? 48f : 64f);

            var hasSaved = !createAccount && !string.IsNullOrEmpty(emailInput.text) && !string.IsNullOrEmpty(passwordInput.text);
            var primaryLabel = createAccount ? "TẠO TÀI KHOẢN & VÀO GAME" : (hasSaved ? "ĐĂNG NHẬP NHANH" : "VÀO GAME");
            authPrimaryButton = AuthPrimary(card, primaryLabel, AuthFormX, createAccount ? 662f : 576f, AuthFormWidth, () => SubmitAuth(createAccount));
            AuthControl(authPrimaryButton);

            if (!createAccount && hasSaved)
            {
                var changeAccount = AuthGhost(card, "Đổi tài khoản khác", "user", AuthFormX, 692f, AuthFormWidth, 72f, ClearSavedAccountFields);
                AuthControl(changeAccount);
            }
        }

        private void ClearSavedAccountFields()
        {
            PlayerPrefs.DeleteKey(PrefKeySavedAccount);
            PlayerPrefs.DeleteKey(PrefKeySavedPassword);
            PlayerPrefs.Save();
            if (emailInput != null) emailInput.text = string.Empty;
            if (passwordInput != null) passwordInput.text = string.Empty;
            ShowAccountForm(false);
        }

        private void ShowEmailVerification(string email, string message = null)
        {
            pendingVerificationEmail = email?.Trim();
            PrepareAccountScreen();
            var card = BuildAuthCard(true);
            AuthBrandHeader(card, "XÁC MINH\nEMAIL", "Nhập mã 6 số vừa được gửi tới hộp thư của bạn để hoàn tất đăng ký.");
            AuthText(card, "VerificationTip", "Không thấy thư? Hãy kiểm tra mục Spam hoặc gửi lại mã sau ít phút.",
                ModernUi.Regular, 22, AuthTextTertiary, TextAnchor.LowerLeft, AuthBrandTextX, 560f, AuthBrandTextWidth, 120f);

            var chip = AuthNode("VerificationTarget", card, AuthFormX, 60f, AuthFormWidth, 88f);
            var chipFill = chip.gameObject.AddComponent<Image>();
            ModernUi.Fill(chipFill, 22f);
            chipFill.color = new Color32(255, 255, 255, 12);
            chipFill.raycastTarget = false;
            var chipRing = AuthImage(chip, "Ring", 0f, 0f, AuthFormWidth, 88f);
            ModernUi.Ring(chipRing, 22f, 1.2f);
            chipRing.color = new Color32(255, 255, 255, 22);
            var chipIcon = AuthImage(chip, "Icon", 28f, 25f, 38f, 38f);
            chipIcon.sprite = ModernUi.Icon("mail");
            chipIcon.color = AuthGoldAccent;
            var chipText = AuthText(chip, "Email", pendingVerificationEmail, ModernUi.SemiBold, 28, AuthTextPrimary, TextAnchor.MiddleLeft, 88f, 0f, AuthFormWidth - 116f, 88f);
            chipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            chipText.verticalOverflow = VerticalWrapMode.Truncate;
            chipText.resizeTextForBestFit = true;
            chipText.resizeTextMinSize = 18;
            chipText.resizeTextMaxSize = 28;

            verificationCodeInput = AuthField(card, "verificationCode", "Mã xác minh", "6 chữ số", "lock", AuthFormX, 190f, AuthFormWidth, false);
            verificationCodeInput.contentType = InputField.ContentType.IntegerNumber;
            verificationCodeInput.characterLimit = 6;
            verificationCodeInput.keyboardType = TouchScreenKeyboardType.NumberPad;
            verificationCodeInput.textComponent.fontSize = 36;
            verificationCodeInput.onSubmit.AddListener(_ => SubmitEmailVerification());
            AuthControl(verificationCodeInput);

            status = AuthText(card, "AuthFeedback", "", ModernUi.Regular, 24, AuthTextSecondary, TextAnchor.MiddleLeft, AuthFormX + 4f, 346f, AuthFormWidth - 8f, 64f);
            authPrimaryButton = AuthPrimary(card, "XÁC MINH VÀO GAME", AuthFormX, 426f, AuthFormWidth, SubmitEmailVerification);
            AuthControl(authPrimaryButton);
            var half = (AuthFormWidth - 20f) * .5f;
            AuthControl(AuthGhost(card, "Gửi lại mã", "refresh", AuthFormX, 560f, half, 80f, ResendEmailVerification));
            AuthControl(AuthGhost(card, "Về đăng nhập", "arrowLeft", AuthFormX + half + 20f, 560f, half, 80f, () => ShowLogin()));
            AuthFeedback(string.IsNullOrWhiteSpace(message) ? "Mã có hiệu lực trong 10 phút." : message);
        }

        private static void FlagAuthInput(InputField field)
        {
            if (field == null) return;
            var focus = field.GetComponent<UiInputFocus>();
            if (focus != null) focus.Flag();
        }

        // ---------------------------------------------------------------- card

        private RectTransform BuildAuthCard(bool intro, float height = AuthCardHeight)
        {
            authCardHeight = height;
            // The card lives to the right of the brand logo; it grows to use a phone screen and shrinks on a narrow one.
            var viewport = AuthStretch("AuthViewport", content.transform);
            viewport.anchorMin = new Vector2(.13f, 0f);
            viewport.anchorMax = Vector2.one;

            var root = new GameObject("AuthRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(viewport, false);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = new Vector2(AuthCardWidth, height);
            root.anchoredPosition = Vector2.zero;
            authCardRoot = root;

            var fit = viewport.gameObject.AddComponent<UiFitScale>();
            fit.target = root;
            fit.size = root.sizeDelta;
            fit.maxScale = 1.3f;
            fit.Apply();

            var card = AuthStretch("AuthGlass", root);
            var fill = card.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, AuthCardRadius);
            UiGradient.Apply(fill, new Color32(17, 28, 37, 240), new Color32(8, 14, 20, 238));
            ModernUi.Soft(card, 240f, new Color(0f, 0f, 0f, .30f), Vector2.zero);
            ModernUi.Soft(card, AuthCardRadius, new Color(0f, 0f, 0f, .55f), new Vector2(0f, -18f));

            var border = AuthImage(card, "Edge", 0f, 0f, AuthCardWidth, height);
            ModernUi.Ring(border, AuthCardRadius, 1.6f);
            UiGradient.Apply(border, new Color32(242, 208, 136, 150), new Color32(225, 185, 104, 34));

            // Brand column: an inset warm panel so the two halves read as one object.
            var brand = AuthImage(card, "BrandColumn", AuthBrandInset, AuthBrandInset, AuthBrandWidth - AuthBrandInset, height - AuthBrandInset * 2f);
            ModernUi.Fill(brand, AuthCardRadius - 10f);
            UiGradient.Apply(brand, new Color32(66, 60, 42, 92), new Color32(18, 27, 33, 36));
            var brandEdge = AuthImage(card, "BrandEdge", AuthBrandInset, AuthBrandInset, AuthBrandWidth - AuthBrandInset, height - AuthBrandInset * 2f);
            ModernUi.Ring(brandEdge, AuthCardRadius - 10f, 1.2f);
            brandEdge.color = new Color32(255, 240, 210, 16);
            var glowAnchor = AuthNode("BrandGlow", card, 40f, 40f, 260f, 150f);
            ModernUi.Soft(glowAnchor, 70f, new Color(1f, .78f, .38f, .10f), Vector2.zero);

            if (intro) UiIntro.Play(root, new Vector2(0f, -28f));
            card.gameObject.AddComponent<UiAuthKeyboardShift>();
            return card;
        }

        private void AuthBrandHeader(RectTransform card, string title, string subtitle)
        {
            var bar = AuthImage(card, "EyebrowBar", AuthBrandTextX, 72f, 34f, 4f);
            ModernUi.Fill(bar, 2f);
            bar.color = AuthGoldAccent;
            AuthText(card, "Eyebrow", "TU TIÊN GIỚI", ModernUi.SemiBold, 21, AuthGoldAccent, TextAnchor.MiddleLeft, AuthBrandTextX + 48f, 60f, AuthBrandTextWidth - 48f, 28f);

            var heading = AuthText(card, "AuthTitle", title, ModernUi.Display, 54, Color.white, TextAnchor.UpperLeft, AuthBrandTextX - 2f, 104f, AuthBrandTextWidth + 20f, 150f);
            heading.horizontalOverflow = HorizontalWrapMode.Overflow;
            heading.lineSpacing = .92f;
            UiGradient.Apply(heading, AuthGoldTop, AuthGoldBottom);
            var shadow = heading.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .45f);
            shadow.effectDistance = new Vector2(0f, -3f);

            var copy = AuthText(card, "AuthSubtitle", subtitle, ModernUi.Regular, 26, AuthTextSecondary, TextAnchor.UpperLeft, AuthBrandTextX, 266f, AuthBrandTextWidth, 110f);
            copy.lineSpacing = 1.12f;
        }

        // ---------------------------------------------------------------- controls

        private void AuthTabs(RectTransform card, float x, float y, float w, float h, bool createAccount, bool animate)
        {
            var track = AuthNode("AuthTabs", card, x, y, w, h);
            var trackFill = track.gameObject.AddComponent<Image>();
            ModernUi.Fill(trackFill, 24f);
            trackFill.color = new Color32(2, 7, 12, 150);
            trackFill.raycastTarget = false;
            var trackEdge = AuthImage(track, "Edge", 0f, 0f, w, h);
            ModernUi.Ring(trackEdge, 24f, 1.2f);
            trackEdge.color = new Color32(255, 255, 255, 20);

            const float inset = 7f;
            var segment = (w - inset * 2f) * .5f;
            var indicator = AuthImage(track, "ActiveTab", inset + (createAccount ? segment : 0f), inset, segment, h - inset * 2f);
            ModernUi.Fill(indicator, 18f);
            UiGradient.Apply(indicator, AuthGoldTop, AuthGoldBottom);
            var glow = ModernUi.Soft(indicator.rectTransform, 18f, new Color(.9f, .62f, .2f, .30f), new Vector2(0f, -5f), -12f);
            if (animate)
            {
                var offset = new Vector2(createAccount ? -segment : segment, 0f);
                UiSlide.Play(indicator.rectTransform, offset);
                UiSlide.Play(glow.rectTransform, offset);
            }

            for (var i = 0; i < 2; i++)
            {
                var register = i == 1;
                var active = register == createAccount;
                var node = AuthNode(register ? "Tab_Register" : "Tab_Login", track, inset + segment * i, inset, segment, h - inset * 2f);
                var hit = node.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                var button = node.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                AuthText(node, "Text", register ? "Tạo tài khoản" : "Đăng nhập", ModernUi.SemiBold, 28,
                    active ? AuthInkOnGold : AuthTextSecondary, TextAnchor.MiddleCenter, 0f, 0f, segment, h - inset * 2f);
                if (!active)
                {
                    button.onClick.AddListener(() =>
                    {
                        if (authRequestPending) return;
                        authTabSwitch = true;
                        ShowAccountForm(register);
                    });
                }
                AuthControl(button);
            }
        }

        private InputField AuthField(RectTransform card, string name, string label, string placeholder, string icon, float x, float y, float w, bool secret)
        {
            const float radius = 20f;
            AuthText(card, name + "Label", label, ModernUi.Medium, 24, AuthTextSecondary, TextAnchor.MiddleLeft, x + 4f, y, w - 8f, 30f);
            var root = AuthNode(name, card, x, y + 38f, w, AuthFieldHeight);
            var glow = ModernUi.Soft(root, radius, Color.clear, Vector2.zero, -8f);
            var fill = root.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, radius);
            fill.color = new Color32(2, 7, 12, 168);
            var border = AuthImage(root, "Edge", 0f, 0f, w, AuthFieldHeight);
            ModernUi.Ring(border, radius, 1.6f);
            var glyph = AuthImage(root, "Icon", 28f, (AuthFieldHeight - 36f) * .5f, 36f, 36f);
            glyph.sprite = ModernUi.Icon(icon);

            const float textX = 84f;
            var textRight = secret ? 82f : 28f;
            var value = AuthText(root, "Value", "", ModernUi.Medium, 30, AuthTextPrimary, TextAnchor.MiddleLeft, textX, 0f, w - textX - textRight, AuthFieldHeight);
            value.supportRichText = false;
            value.verticalOverflow = VerticalWrapMode.Truncate;
            var hint = AuthText(root, "Placeholder", placeholder, ModernUi.Regular, 28, AuthTextTertiary, TextAnchor.MiddleLeft, textX, 0f, w - textX - textRight, AuthFieldHeight);
            hint.verticalOverflow = VerticalWrapMode.Truncate;

            var input = root.gameObject.AddComponent<InputField>();
            input.transition = Selectable.Transition.None;
            input.targetGraphic = fill;
            input.textComponent = value;
            input.placeholder = hint;
            input.contentType = secret ? InputField.ContentType.Password : (name == "email" ? InputField.ContentType.EmailAddress : InputField.ContentType.Standard);
            input.lineType = InputField.LineType.SingleLine;
            input.asteriskChar = '•';
            input.caretWidth = 3;
            input.customCaretColor = true;
            input.caretColor = AuthGoldTop;
            input.selectionColor = new Color32(225, 185, 104, 96);
            input.shouldHideMobileInput = true;

            var focus = root.gameObject.AddComponent<UiInputFocus>();
            focus.input = input;
            focus.border = border;
            focus.glow = glow;
            focus.icon = glyph;
            focus.borderIdle = new Color32(225, 185, 104, 58);
            focus.borderFocus = new Color32(244, 206, 130, 235);
            focus.borderError = AuthError;
            focus.iconIdle = new Color32(150, 162, 174, 255);
            focus.iconFocus = AuthGoldAccent;
            focus.Refresh(0f);

            if (secret)
            {
                var reveal = AuthNode("Reveal", root, w - 76f, (AuthFieldHeight - 64f) * .5f, 64f, 64f);
                var hit = reveal.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                var eye = AuthImage(reveal, "Icon", 14f, 14f, 36f, 36f);
                eye.sprite = ModernUi.Icon("eye");
                eye.color = new Color32(150, 162, 174, 255);
                var toggle = reveal.gameObject.AddComponent<Button>();
                toggle.transition = Selectable.Transition.None;
                toggle.targetGraphic = hit;
                toggle.onClick.AddListener(() =>
                {
                    var hidden = input.contentType == InputField.ContentType.Password;
                    input.contentType = hidden ? InputField.ContentType.Standard : InputField.ContentType.Password;
                    eye.sprite = ModernUi.Icon(hidden ? "eyeOff" : "eye");
                    eye.color = hidden ? AuthGoldAccent : new Color32(150, 162, 174, 255);
                    input.ForceLabelUpdate();
                });
            }
            return input;
        }

        private Button AuthPrimary(RectTransform card, string label, float x, float y, float w, Action click)
        {
            const float h = 104f;
            const float radius = 24f;
            var root = AuthNode("PrimaryAction", card, x, y, w, h);
            ModernUi.Soft(root, radius, new Color(.92f, .62f, .2f, .30f), new Vector2(0f, -10f), -10f);
            var fill = root.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, radius);
            UiGradient.Apply(fill, AuthGoldTop, AuthGoldBottom);
            var sheen = AuthImage(root, "Sheen", radius, 3f, w - radius * 2f, 2f);
            sheen.color = new Color(1f, 1f, 1f, .75f);
            UiGradient.Apply(sheen, new Color(1f, 1f, 1f, 0f), Color.white, true, true);

            var text = AuthText(root, "Text", label, ModernUi.Bold, 31, AuthInkOnGold, TextAnchor.MiddleCenter, 0f, 0f, w, h);
            text.raycastTarget = false;
            authPrimaryArrow = AuthImage(root, "Arrow", w - 82f, (h - 40f) * .5f, 40f, 40f);
            authPrimaryArrow.sprite = ModernUi.Icon("arrowRight");
            authPrimaryArrow.color = new Color(AuthInkOnGold.r, AuthInkOnGold.g, AuthInkOnGold.b, .82f);
            authPrimarySpinner = AuthImage(root, "Spinner", w - 82f, (h - 40f) * .5f, 40f, 40f);
            authPrimarySpinner.sprite = ModernUi.Icon("spinner");
            authPrimarySpinner.color = AuthInkOnGold;
            authPrimarySpinner.gameObject.AddComponent<UiSpinner>();
            authPrimarySpinner.gameObject.SetActive(false);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .97f, .9f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.88f, .83f, .76f);
            colors.disabledColor = new Color(.78f, .76f, .72f, .9f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.onClick.AddListener(() => click?.Invoke());
            root.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        private Button AuthGhost(RectTransform parent, string label, string icon, float x, float y, float w, float h, Action click)
        {
            const float radius = 20f;
            var root = AuthNode("Ghost_" + label, parent, x, y, w, h);
            var fill = root.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, radius);
            fill.color = new Color32(255, 255, 255, 13);
            var edge = AuthImage(root, "Edge", 0f, 0f, w, h);
            ModernUi.Ring(edge, radius, 1.3f);
            edge.color = new Color32(240, 228, 204, 52);

            var text = AuthText(root, "Text", label, ModernUi.SemiBold, 27, AuthTextPrimary, TextAnchor.MiddleLeft, 0f, 0f, w, h);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            const float iconSize = 34f;
            const float gap = 14f;
            var group = iconSize + gap + text.preferredWidth;
            var start = Mathf.Max(18f, (w - group) * .5f);
            var glyph = AuthImage(root, "Icon", start, (h - iconSize) * .5f, iconSize, iconSize);
            glyph.sprite = ModernUi.Icon(icon);
            glyph.color = AuthGoldAccent;
            var textRect = text.rectTransform;
            textRect.sizeDelta = new Vector2(w - start - iconSize - gap, h);
            textRect.anchoredPosition = new Vector2(start + iconSize + gap + textRect.sizeDelta.x * .5f, -h * .5f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.7f, .7f, .7f, 1f);
            colors.disabledColor = new Color(.6f, .6f, .6f, .6f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.onClick.AddListener(() => click?.Invoke());
            root.gameObject.AddComponent<UiPressScale>();
            return button;
        }

        // ---------------------------------------------------------------- layout helpers (top-left origin, y down)

        private static RectTransform AuthNode(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x + w * .5f, -(y + h * .5f));
            return rect;
        }

        private static RectTransform AuthStretch(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image AuthImage(Transform parent, string name, float x, float y, float w, float h)
        {
            var image = AuthNode(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text AuthText(Transform parent, string name, string value, Font font, int size, Color color, TextAnchor anchor, float x, float y, float w, float h)
        {
            var text = AuthNode(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value ?? string.Empty;
            return text;
        }
    }

    /// <summary>Smoothly moves the auth card up and creates a full-width bottom curtain when the mobile on-screen keyboard is visible.</summary>
    internal sealed class UiAuthKeyboardShift : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 basePos;
        private bool initialized;
        private RectTransform curtain;

        private void Awake()
        {
            rect = (RectTransform)transform;
        }

        private void Start()
        {
            CreateCurtain();
        }

        private void CreateCurtain()
        {
            if (curtain != null) return;
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform.parent;
            var curtainObj = new GameObject("KeyboardCurtain", typeof(RectTransform), typeof(Image));
            curtain = curtainObj.GetComponent<RectTransform>();
            curtain.SetParent(parent, false);
            curtain.SetSiblingIndex(Mathf.Max(0, transform.GetSiblingIndex() - 1));
            // Full width across the entire bottom of the screen
            curtain.anchorMin = new Vector2(0f, 0f);
            curtain.anchorMax = new Vector2(1f, 0f);
            curtain.pivot = new Vector2(0.5f, 0f);
            curtain.sizeDelta = new Vector2(0f, 0f);
            curtain.anchoredPosition = Vector2.zero;

            var img = curtainObj.GetComponent<Image>();
            // Apple standard iOS dark keyboard background color (#1C1C1E)
            img.color = new Color32(28, 28, 30, 255);
            img.raycastTarget = false;
            curtainObj.SetActive(false);
        }

        private void Update()
        {
            if (!initialized)
            {
                basePos = rect.anchoredPosition;
                initialized = true;
            }
            if (TouchScreenKeyboard.visible)
            {
                TouchScreenKeyboard.hideInput = true;
            }
            var isKeyboard = TouchScreenKeyboard.visible;
            if (curtain != null)
            {
                if (isKeyboard)
                {
                    if (!curtain.gameObject.activeSelf) curtain.gameObject.SetActive(true);
                    var canvas = GetComponentInParent<Canvas>();
                    var canvasHeight = canvas != null ? ((RectTransform)canvas.transform).rect.height : 1080f;
                    float kh = canvasHeight * 0.55f;
                    if (TouchScreenKeyboard.area.height > 0 && Screen.height > 0)
                    {
                        var ratio = TouchScreenKeyboard.area.height / (float)Screen.height;
                        if (ratio > 0.1f && ratio < 0.9f) kh = canvasHeight * ratio;
                    }
                    curtain.sizeDelta = new Vector2(0f, kh);
                }
                else
                {
                    if (curtain.gameObject.activeSelf) curtain.gameObject.SetActive(false);
                }
            }

            var targetY = isKeyboard ? basePos.y + 160f : basePos.y;
            var p = rect.anchoredPosition;
            if (Mathf.Abs(p.y - targetY) > 0.5f)
            {
                p.y = Mathf.MoveTowards(p.y, targetY, Time.unscaledDeltaTime * 750f);
                rect.anchoredPosition = p;
            }
        }

        private void OnDestroy()
        {
            if (curtain != null) Destroy(curtain.gameObject);
        }
    }
}
