# Tu Tiên iOSVN

Project Unity online cho iOS. Lõi luật chơi được đồng bộ từ bản AWS sang `ipa_core/`; server IPA dùng tài khoản email và dữ liệu lưu riêng. Server IPA chưa được deploy.

## Build trên GitHub

Mở tab **Actions** → **Unity iOS build**. Build dùng Unity Builder và runner macOS của GitHub để xuất, biên dịch project iOS bằng Xcode. Kết quả Xcode được lưu thành artifact; chọn `export_ipa=true` để ký và xuất IPA.

### Cấu hình bắt buộc

- Secret `UNITY_LICENSE` để bật Unity build.
- Variables `IOS_BUNDLE_ID` và `IPA_SERVER_URL`.
- Để xuất IPA: secrets `IOS_TEAM_ID`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD`, `IOS_PROFILE_BASE64`.

Thiếu Unity license thì workflow ghi thông báo và bỏ qua build. Runner macOS của repo private dùng quota phút GitHub Actions và có thể bị tính phí theo gói.

## Hiện trạng

- Unity project: `unity_project/` (đang ghim Unity `6000.3.13f1`).
- Server IPA: `ipa_server.js`; chưa có domain HTTPS hoặc nhà cung cấp máy chủ.
- Bản đồ Phàm Giới/Tiên Giới, profile tùy biến, pixel art mới và kết bạn vẫn là yêu cầu thiết kế, chưa được dựng thành game.
- Hướng tích hợp và ghi nhận nguồn AWS nằm trong `unity_project/docs/` và `ipa_core/PROVENANCE.md`.
