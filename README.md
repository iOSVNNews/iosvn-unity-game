# Tu Tiên Giới

Project Unity online cho iOS. Lõi luật chơi được đồng bộ từ bản AWS sang `ipa_core/`; server IPA dùng tài khoản email và dữ liệu lưu riêng. Server IPA chưa được deploy.

## Build trên GitHub

Mở **Actions** → **Tu Tiên Giới iOS build**. Workflow biên dịch project bằng Unity, sau đó dùng runner macOS và Xcode để tạo bản iOS.

Để xuất bản thử nghiệm chưa ký từ Xcode export có sẵn, chạy workflow thủ công với:

- `use_prebuilt_xcode=true`
- `xcode_export_tag=iosvn-xcode-bootstrap`
- `export_ipa=true`
- `publish_release=true`
- `release_tag=iosvn-unsigned-test`
- `publish_container=true` nếu muốn phát hành server lên GitHub Packages

IPA unsigned được lưu thành Actions artifact và đính kèm vào Release. Bản này kiểm tra được gói build; cần ký bằng chứng chỉ và provisioning profile của Apple trước khi cài lên iPhone. Khi đã có license Unity cho CI, đặt `use_prebuilt_xcode=false` để build lại trực tiếp từ `unity_project/`.

### Cấu hình bắt buộc

- Secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` để bật Unity Personal build. `UNITY_LICENSE` là nội dung tệp `.ulf` do Unity Hub kích hoạt cấp; không commit hoặc đính kèm nó vào Release.
- App dùng tên **Tu Tiên Giới** và Bundle ID mặc định `com.iosvn.tutiengioi`; chỉ đặt variable `IOS_BUNDLE_ID` nếu cần ghi đè. Mỗi người ký bằng chứng chỉ riêng phải dùng provisioning profile cho phép Bundle ID này. Profile wildcard tương thích cũng có thể cho phép app mà không cần đăng ký ID tường minh.
- Variable `IPA_SERVER_URL` is the HTTPS base URL of the dedicated IPA API and ends in `/api`.
- Biến `ASSET_CDN_URL` là URL HTTPS chứa manifest và AssetBundles; có thể để trống trong bản thử nghiệm.
- Để xuất IPA: secrets `IOS_TEAM_ID`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD`, `IOS_PROFILE_BASE64`.
- Gmail sender for the dedicated server: environment values `GMAIL_SMTP_USER` and `GMAIL_SMTP_APP_PASSWORD`.

Thiếu Unity license thì workflow ghi thông báo và bỏ qua build. Runner macOS của repo private dùng quota phút GitHub Actions và có thể bị tính phí theo gói.

Trên Windows, vào **Unity Hub > Preferences > Licenses > Add > Get a free personal license** để Hub kích hoạt license. Với Personal license, tạo `.ulf` qua Hub; quy trình manual activation `.alf` của Unity chỉ hỗ trợ license ngoài Personal. Khi đã có `.ulf`, thêm nội dung file vào GitHub Actions secret `UNITY_LICENSE`, rồi đặt `UNITY_EMAIL` và `UNITY_PASSWORD` làm secrets riêng. Không gửi mật khẩu qua chat và không đưa file `.ulf` vào Release hoặc Packages.

## GitHub Packages: IPA server

Workflow có thể đóng gói server email riêng trong container và đẩy lên `ghcr.io/iosvnnews/iosvn-ipa-server:unsigned-test`. Container lắng nghe cổng `8788`; gắn volume vào `/data` để giữ tài khoản và nhân vật qua lần khởi động lại. Ví dụ chạy:

```sh
docker run --name iosvn-ipa-server --env-file ipa_server.env -p 8788:8788 -v iosvn-ipa-data:/data ghcr.io/iosvnnews/iosvn-ipa-server:unsigned-test
```

Server IPA này dùng API và tài khoản email riêng. AWS mini app hiện xác thực bằng Telegram; các route `/api/auth/email/*` và `/api/map/catalog` chưa có trên AWS mini app nên không thể dùng URL đó làm backend IPA. Đăng ký email gửi mã xác minh 6 số qua Gmail; cần tạo Google App Password cho mailbox gửi thư rồi đặt vào `GMAIL_SMTP_APP_PASSWORD` trên máy chủ. Không nhúng mật khẩu Gmail vào IPA hoặc Git.

Khi chưa chọn máy chủ, màn đăng nhập có nút **XEM BẢN ĐỒ NGOẠI TUYẾN**. Nút này mở dữ liệu bản đồ đóng gói trong app để xem 8 châu Phàm Giới, 11 vùng Tiên Giới, 65 thành, 65 cổ động và bãi tiểu yêu. Di chuyển, đăng nhập và chiến đấu cần máy chủ game online.

## Hiện trạng

- Unity project: `unity_project/` (ghim Unity `6000.6.3f1`; Editor và iOS Build Support đã cài trên ổ D của máy phát triển).
- Server IPA: `ipa_server.js`; chưa có domain HTTPS hoặc nhà cung cấp máy chủ.
- Atlas Phàm Giới/Tiên Giới có thể mở ngoại tuyến để duyệt; đăng nhập email, xác minh Gmail, lưu nhân vật và gameplay online cần máy chủ HTTPS riêng.
- Dữ liệu tài khoản và nhân vật tách riêng khỏi Mini App Telegram; workflow có thể build image server nhưng chưa triển khai máy chủ.
- Hướng tích hợp và ghi nhận nguồn AWS nằm trong `unity_project/docs/` và `ipa_core/PROVENANCE.md`.

Để build trên GitHub Actions cần cấu hình Unity Personal secrets. Xuất IPA cài trên iPhone còn cần Apple signing secrets và bundle ID khớp provisioning profile.

### Tải tài nguyên sau khi cài

Game kiểm tra `version_manifest.json` khi mở. Gói tải được xác thực bằng SHA-256, lưu trong vùng dữ liệu của ứng dụng và chỉ nạp AssetBundle khi màn chơi yêu cầu. Đặt tên AssetBundle cho nội dung trong Unity rồi chạy `iOSVN > Assets > Build iOS downloadable bundles`; tải toàn bộ thư mục `build/AssetBundles/iOS` lên HTTPS CDN và đặt URL thư mục đó ở biến `ASSET_CDN_URL`. Bản cài hiện chưa có CDN nên sẽ vào nội dung thử nghiệm mà không chờ tải.
