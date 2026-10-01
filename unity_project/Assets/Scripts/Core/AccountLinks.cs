using System;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    public sealed partial class PrototypeBootstrap
    {
        private bool accountLinksOpen;
        private bool accountProfileLoading;
        private AccountProfileResult accountProfile;
        private Text accountSummary;
        private InputField linkedEmailInput;
        private InputField linkedEmailCode;
        private Button accountSendEmail;
        private Button accountVerifyEmail;
        private Button accountGoogle;
        private Button accountFacebook;

        private void ShowAccountLinks(GameState state)
        {
            PrepareAccountScreen();
            accountLinksOpen = true;
            accountProfile = null;
            accountProfileLoading = false;
            var card = PanelObject("AccountLinksCard", content.transform, new Vector2(.29f, .12f), new Vector2(.71f, .93f), Vector2.zero, Vector2.zero, new Color32(14, 27, 35, 242));
            ChildText(card.transform, "AccountTitle", 32, Gold, TextAnchor.MiddleCenter, new Vector2(.07f, .88f), new Vector2(.93f, .96f)).text = "LIÊN KẾT TÀI KHOẢN";
            accountSummary = ChildText(card.transform, "AccountSummary", 21, Cream, TextAnchor.MiddleCenter, new Vector2(.085f, .76f), new Vector2(.915f, .87f));
            accountSummary.text = "Đang tải thông tin tài khoản...";
            linkedEmailInput = Input("email", "Email muốn liên kết", new Vector2(.085f, .65f), new Vector2(.915f, .745f), false, card.transform);
            linkedEmailInput.characterLimit = 254;
            AuthControl(linkedEmailInput);
            linkedEmailCode = Input("LinkVerification", "Mã 6 số", new Vector2(.085f, .525f), new Vector2(.49f, .62f), false, card.transform);
            linkedEmailCode.contentType = InputField.ContentType.IntegerNumber;
            linkedEmailCode.characterLimit = 6;
            AuthControl(linkedEmailCode);
            accountSendEmail = Button("GỬI MÃ", new Vector2(.51f, .525f), new Vector2(.915f, .62f), Panel, SendAccountEmailCode, card.transform);
            accountVerifyEmail = Button("XÁC MINH VÀ LIÊN KẾT EMAIL", new Vector2(.085f, .405f), new Vector2(.915f, .50f), Gold, VerifyAccountEmail, card.transform);
            accountGoogle = Button("LIÊN KẾT GOOGLE", new Vector2(.085f, .15f), new Vector2(.49f, .255f), Panel, () => LinkAccountProvider("google"), card.transform);
            accountFacebook = Button("LIÊN KẾT FACEBOOK", new Vector2(.51f, .15f), new Vector2(.915f, .255f), Panel, () => LinkAccountProvider("facebook"), card.transform);
            foreach (var button in new[] { accountSendEmail, accountVerifyEmail, accountGoogle, accountFacebook }) AuthControl(button);
            status = ChildText(card.transform, "AuthFeedback", 21, Cream, TextAnchor.MiddleCenter, new Vector2(.085f, .275f), new Vector2(.915f, .395f));
            Button("LÀM MỚI LIÊN KẾT", new Vector2(.085f, .035f), new Vector2(.915f, .115f), Panel, RefreshAccountProfile, card.transform);
            Button("VỀ GAME", new Vector2(.40f, .015f), new Vector2(.60f, .095f), Gold, () => ShowHome(state));
            ApplyAccountCapabilities();
            RefreshAccountProfile();
        }

        private void RefreshAccountProfile()
        {
            if (!accountLinksOpen || accountProfileLoading || authRequestPending) return;
            accountProfileLoading = true;
            var version = authScreenVersion;
            client.LoadAccountProfile(result =>
            {
                if (version != authScreenVersion) return;
                accountProfileLoading = false;
                if (result?.ok != true) { AuthFeedback(result?.error ?? "Không tải được thông tin tài khoản.", true); return; }
                accountProfile = result;
                accountSummary.text = (string.IsNullOrEmpty(result.username) ? "Tài khoản email" : "Tài khoản: " + result.username) + "\n" +
                    (result.emailVerified ? "Email: " + result.email : "Chưa liên kết email");
                if (result.emailVerified) linkedEmailInput.text = result.email;
                ApplyAccountCapabilities();
                AuthFeedback(result.emailAvailable || result.googleAvailable || result.facebookAvailable
                    ? "Liên kết dịch vụ với cùng hồ sơ tu luyện của bạn." : "Dịch vụ liên kết email, Google và Facebook chưa mở. Bạn vẫn dùng tài khoản và mật khẩu bình thường.");
            });
        }

        private void ApplyAccountCapabilities()
        {
            if (!accountLinksOpen) return;
            accountSendEmail.interactable = !authRequestPending && accountProfile?.emailAvailable == true;
            accountVerifyEmail.interactable = accountSendEmail.interactable;
            accountGoogle.interactable = !authRequestPending && accountProfile?.googleAvailable == true && accountProfile?.googleLinked != true;
            accountFacebook.interactable = !authRequestPending && accountProfile?.facebookAvailable == true && accountProfile?.facebookLinked != true;
            accountGoogle.GetComponentInChildren<Text>().text = accountProfile?.googleLinked == true ? "GOOGLE · ĐÃ LIÊN KẾT" : accountProfile?.googleAvailable == true ? "LIÊN KẾT GOOGLE" : "GOOGLE · CHƯA MỞ";
            accountFacebook.GetComponentInChildren<Text>().text = accountProfile?.facebookLinked == true ? "FACEBOOK · ĐÃ LIÊN KẾT" : accountProfile?.facebookAvailable == true ? "LIÊN KẾT FACEBOOK" : "FACEBOOK · CHƯA MỞ";
        }

        private void SendAccountEmailCode()
        {
            if (authRequestPending) return;
            var email = linkedEmailInput.text.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")) { AuthFeedback("Nhập email hợp lệ trước.", true); return; }
            var version = authScreenVersion;
            SetAuthBusy(true);
            AuthFeedback("Đang gửi mã xác minh...");
            client.SendEmailLink(email, result =>
            {
                if (version != authScreenVersion) return;
                SetAuthBusy(false); ApplyAccountCapabilities();
                AuthFeedback(result?.ok == true ? result.message : result?.error ?? "Không gửi được mã.", result?.ok != true);
            });
        }

        private void VerifyAccountEmail()
        {
            if (authRequestPending) return;
            if (!System.Text.RegularExpressions.Regex.IsMatch(linkedEmailCode.text, @"^\d{6}$")) { AuthFeedback("Nhập mã xác minh 6 số trước.", true); return; }
            var version = authScreenVersion;
            SetAuthBusy(true);
            AuthFeedback("Đang liên kết email...");
            client.ConfirmEmailLink(linkedEmailInput.text.Trim(), linkedEmailCode.text, result =>
            {
                if (version != authScreenVersion) return;
                SetAuthBusy(false); ApplyAccountCapabilities();
                if (result?.ok != true) { AuthFeedback(result?.error ?? "Không liên kết được email.", true); return; }
                linkedEmailCode.text = "";
                RefreshAccountProfile();
            });
        }

        private void LinkAccountProvider(string provider)
        {
            if (authRequestPending) return;
            var version = authScreenVersion;
            SetAuthBusy(true);
            client.StartProviderLink(provider, result =>
            {
                if (version != authScreenVersion) return;
                SetAuthBusy(false); ApplyAccountCapabilities();
                if (result?.ok != true) { AuthFeedback(result?.error ?? "Không mở được dịch vụ liên kết.", true); return; }
                if (!Uri.TryCreate(result.url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                    (uri.Host != "accounts.google.com" && uri.Host != "www.facebook.com")) { AuthFeedback("Địa chỉ liên kết không hợp lệ.", true); return; }
                AuthFeedback("Xác nhận trong trình duyệt rồi quay lại game để tiếp tục.");
                Application.OpenURL(result.url);
            });
        }

        private void OnApplicationFocus(bool focus) { if (focus && accountLinksOpen) RefreshAccountProfile(); }
        private void OnApplicationPause(bool paused) { if (!paused && accountLinksOpen) RefreshAccountProfile(); }
    }
}
