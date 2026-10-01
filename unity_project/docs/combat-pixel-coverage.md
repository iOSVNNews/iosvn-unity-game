# Bộ pixel chiến đấu

Nguồn định danh là `ipa_core/catalog`. Chạy `node scripts/generate_combat_pixel_art.js` để dựng lại ảnh PNG; tác vụ giữ nguyên năm ảnh quái được vẽ riêng. Chạy `node scripts/audit_combat_pixel_art.js` để kiểm tra thiếu ID và ảnh trùng nội dung.

| Nhóm | ID trong catalog | PNG theo ID | Cách dùng |
| --- | ---: | ---: | --- |
| Quái | 279 | 279 | PvE online và săn quái ngoại tuyến, 8 pose runtime cho mỗi loài |
| Vũ khí | 549 bản ghi / 547 ID duy nhất | 547 | Kho đồ và vũ khí đang trang bị trong trận |
| Kỹ năng người chơi | 245 | 245 | Nút kỹ năng và hiệu ứng 8 frame theo chiêu |
| Chiêu quái theo loài | 279 | 279 | Hiệu ứng đòn đánh của đúng ID quái |
| Vật phẩm khác | 1.068 | 1.068 | Kho đồ, giữ ID của game gốc |

Năm sprite quái vẽ riêng là Cửu Vĩ Ma Hồ (`cuu_vi_ho`), Thanh Long Chân Linh (`thanh_long_anh`), Độc Giác Thanh Giao (`da_lang`), Bạch Hổ Sát Thần (`bach_ho_anh`) và Hỏa Diễm Ma Điệp (`hoa_ho`). Tu sĩ chiến đấu có sprite riêng. Các quái còn lại là hình pixel tạo theo ID với nhiều dáng loài, màu ngũ hành và dấu hiệu khác nhau; chúng **chưa đạt mức minh họa thủ công** của năm quái trên. Đây là giới hạn mỹ thuật cần tiếp tục nâng cấp, nhất là quái cùng họ.

`PixelCreatureArt` tạo các pose đứng, tấn công, trúng đòn từ sprite gốc. `PixelSkillArt` tạo chuyển động 8 frame và nạp hình riêng theo ID. PvE online nhận ID, nguyên tố, sát thương và log từ server; chế độ ngoại tuyến cho phép di chuyển, tấn công và xem phản kích cục bộ. Ảnh kiểm tra từ đúng `PixelCombatPresentation`: [Cửu Vĩ Ma Hồ](pve-cuu-vi-ma-ho.png), [Thanh Long](pve-thanh-long.png), [Hỏa Diễm Ma Điệp](pve-hoa-diem-ma-diep.png).

Bộ này chứa ảnh PNG có thể đóng vào IPA. Ảnh `PixelArt` cũ vẫn ở project để giữ tương thích với các màn chưa chuyển sang `CombatPixel`.
