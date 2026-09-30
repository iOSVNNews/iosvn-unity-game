# Tu Tiên iOSVN

Project Unity online cho iOS. Lõi luật chơi được đồng bộ từ bản AWS sang `ipa_core/`; server IPA dùng tài khoản email và dữ liệu lưu riêng. Server IPA chưa được deploy.

## Build trên GitHub

Mở tab **Actions** → **Unity iOS build**. Build dùng Unity Builder và runner macOS của GitHub để xuất, biên dịch project iOS bằng Xcode. Kết quả Xcode được lưu thành artifact; chọn `export_ipa=true` để ký và xuất IPA.

### Cấu hình bắt buộc

- Secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` để bật Unity Personal build (workflow `Unity license activation` tạo tệp `.alf` một lần).
- Variables `IOS_BUNDLE_ID` và `IPA_SERVER_URL`.
- Biến `ASSET_CDN_URL` là URL HTTPS chứa manifest và AssetBundles; có thể để trống trong bản thử nghiệm.
- Để xuất IPA: secrets `IOS_TEAM_ID`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD`, `IOS_PROFILE_BASE64`.

Thiếu Unity license thì workflow ghi thông báo và bỏ qua build. Runner macOS của repo private dùng quota phút GitHub Actions và có thể bị tính phí theo gói.

## Hiện trạng

- Unity project: `unity_project/` (ghim Unity `6000.6.3f1`; Editor và iOS Build Support đã cài trên ổ D của máy phát triển).
- Server IPA: `ipa_server.js`; chưa có domain HTTPS hoặc nhà cung cấp máy chủ.
- Bản đồ Phàm Giới/Tiên Giới, profile tùy biến, pixel art mới và kết bạn vẫn là yêu cầu thiết kế, chưa được dựng thành game.
- Hướng tích hợp và ghi nhận nguồn AWS nằm trong `unity_project/docs/` và `ipa_core/PROVENANCE.md`.

Để build trên GitHub Actions cần cấu hình Unity Personal secrets. Xuất IPA cài trên iPhone còn cần Apple signing secrets và bundle ID khớp provisioning profile.

### Tải tài nguyên sau khi cài

Game kiểm tra `version_manifest.json` khi mở. Gói tải được xác thực bằng SHA-256, lưu trong vùng dữ liệu của ứng dụng và chỉ nạp AssetBundle khi màn chơi yêu cầu. Đặt tên AssetBundle cho nội dung trong Unity rồi chạy `iOSVN > Assets > Build iOS downloadable bundles`; tải toàn bộ thư mục `build/AssetBundles/iOS` lên HTTPS CDN và đặt URL thư mục đó ở biến `ASSET_CDN_URL`. Bản cài hiện chưa có CDN nên sẽ vào nội dung thử nghiệm mà không chờ tải.
