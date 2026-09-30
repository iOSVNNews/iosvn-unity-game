# Hướng thiết kế game Unity online

Đây là yêu cầu sản phẩm của chủ game ngày 30/09/2026. Bản AWS đang chạy là nguồn luật chơi, nội dung, chỉ số và tiến trình; client Unity viết lại giao diện và cách trình bày. Lõi AWS được giữ riêng trong `D:\game_iosvn\ipa_core` để server IPA dùng độc lập với Telegram.

## Thế giới và di chuyển

- Có hai bản đồ thế giới độc lập: Phàm Giới và Tiên Giới. Nút mở cổng chuyển giữa hai bản đồ khi nhân vật đáp ứng điều kiện vào giới tương ứng.
- Các châu, thành, tông môn, điểm săn, bí cảnh và điểm dịch chuyển nằm trên bản đồ rộng mở. Mỗi vùng có cảnh giới tối thiểu/tối đa, cấp nguy hiểm, lối nối và trạng thái khám phá.
- Server quyết định quyền đi vào dựa trên cảnh giới, lực chiến khi luật vùng yêu cầu, và việc người chơi tới đúng điểm dịch chuyển.
- Thời gian hành trình được máy chủ xác nhận và không quá 30 phút cho một lần đi hết bản đồ. Có thể rút ngắn theo quãng đường/cơ chế game; client không tự sửa thời điểm đến.
- Trạng thái giới, bản đồ, tọa độ, hành trình, cổng mở và điểm dịch chuyển phải nằm trong dữ liệu save của server.

## Nhân vật và pixel art

- Khi tạo profile, người chơi chọn màu tóc, màu trang phục, màu mắt, giới tính/kiểu nhân vật và hệ nguyên tố. Profile hiển thị bảng chỉ số cạnh hình nhân vật.
- Pixel art được vẽ mới cho Unity. Không trích xuất hoặc tái sử dụng sprite từ Mini App cũ.
- Mỗi loại nội dung cần silhouette và sprite riêng: kiếm có hình kiếm, quái có hình quái, áo giáp có hình áo, nhẫn có hình nhẫn. Không nhân bản cùng một sprite cho các vật phẩm khác nhau.
- Sprite nhân vật cần các lớp có thể ghép màu (thân, tóc, mắt, trang phục, trang bị). Hiệu ứng đánh, nhận sát thương và kỹ năng dùng animation/VFX riêng; trang bị và quái có bộ nhận dạng riêng.
- Mỗi asset phát hành có ID, nguồn tạo, kích thước pixel, palette và liên kết catalog. Tên/ID item từ `ipa_core/catalog.js` là khóa nối giữa gameplay và hình ảnh.

## Người chơi trong cùng thành

- Người cùng thành có thể xem profile công khai: nhân vật, cảnh giới, lực chiến, trang bị đang đeo và hoạt động cho phép hiển thị.
- Có lời mời kết bạn, chấp nhận/từ chối, danh sách bạn và trạng thái online gần nhất.
- Server kiểm tra danh tính, thành hiện tại, quyền xem profile, lời mời và quan hệ bạn bè; client không gửi user ID để tự nhận danh tính.
- Dữ liệu xã hội lưu cùng save server riêng IPA và không phụ thuộc danh tính Telegram.

## Lộ trình triển khai

1. Đồng bộ lõi luật chơi từ AWS vào `ipa_core/` và giữ API/tài khoản/save riêng cho IPA.
2. Hoàn thành prototype Unity có đăng nhập, hồ sơ, trận đánh và cấu hình server.
3. Đưa các vùng AWS thành dữ liệu map có cổng, yêu cầu cảnh giới/lực chiến, đường đi và thời lượng hành trình tối đa 30 phút; sau đó nối với UI bản đồ 2D.
4. Chốt schema profile ngoại hình và bộ tiêu chuẩn pixel art; tạo sprite gốc theo từng nhân vật, quái, vũ khí, giáp, trang sức và hiệu ứng.
5. Mở profile cùng thành và hệ thống kết bạn qua API server.
6. Dùng GitHub Actions trên runner macOS để ký và xuất IPA. Chỉ chạy bước xuất IPA khi Unity license, Apple signing certificate, provisioning profile, bundle ID và team ID được cấu hình trong GitHub Secrets/Variables.

Các tính năng map, tùy biến ngoại hình, pixel art và xã hội ở trên là mục tiêu thiết kế; chúng chưa được triển khai trong prototype hiện tại.
