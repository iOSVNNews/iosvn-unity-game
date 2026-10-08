# Đối chiếu thư mục QCBH 1.2.113 — 08/10/2026

Nguồn đọc trực tiếp: `D:\QCBH ver 1.2.113\Mod\modFQA`. Không chạy mã mod, không nhập DLL hoặc sao chép tài nguyên của QCBH vào game iOSVN.

## Những gì xác minh được từ tệp

Thư mục `配置修改教程\配置（只读）Json格式`:

| Tệp | Dữ liệu đọc được | Áp dụng / giới hạn |
| --- | --- | --- |
| WorldTerrainDecorate.json | 818 mẫu; `point` mô tả cụm ô, `weight` chọn mẫu, `openBuildPoint` liên quan điểm công trình | Địa hình phải ghép theo ô logic, tránh đường và điểm tương tác. Không dùng một ảnh toàn thế giới thay cho bố trí địa hình. |
| WorldTerrainType.json | Phân loại công trình / địa hình, `isOverDecorate`, `moveCost` | Phân biệt vật trang trí, công trình và vùng di chuyển. |
| MapPosition.json / MapData.json | Vị trí và loại dữ liệu bản đồ | World, atlas, minimap phải chiếu từ cùng tọa độ; minimap không phải một ảnh nền độc lập. Đây là quyết định của iOSVN, không phải phép đo UI gốc. |
| TownBuilding.json | 18 dòng công trình; `type`, `main`, danh sách `area` | Thành trì là màn hình dịch vụ với điểm công trình riêng. iOSVN có panorama, hit box công trình và danh sách dịch vụ; NPC dùng tọa độ chân trên panorama. |
| DungeonSceneBase.json | 132 mẫu, tách `floor`, `decoration`, `edgeDecoration`, `bg`, âm thanh | Nền combat và vật thể phải được tổ chức riêng. Scene 13 dùng floor 101/102/103 và âm thanh rừng trúc; scene 14 dùng floor 1401/1402/1403 và `qiecuo`. |
| DungeonRoomBase.json | 87 phòng; có 120×60, 48×29, 40×20… | Không có một tỷ lệ phòng duy nhất áp dụng mọi trận. Kích thước này là đơn vị cấu hình, không phải pixel trên điện thoại. |
| DungeonSceneObject.json | 448 nhóm; số cụm, khoảng cách, số vật thể và `barrierType` | Có trang trí và vật cản; không thể coi toàn bộ tranh nền là vùng đi được. |
| BattleAIDefaultValue.json | Khoảng đuổi, dịch chuyển khi đánh, chu kỳ đòn, hướng đánh | AI có nhiều tham số; chưa đủ dữ liệu để suy ra toàn bộ hành vi chỉ từ JSON. |
| BattleSkillAttack.json | Chi phí MP, loại kỹ năng, điều kiện, âm thanh | Giữ kỹ năng, tiêu hao và cooldown tách khỏi hình ảnh hiệu ứng. |
| BattleMissile.json | 1.686 mẫu; tốc độ, tầm, vòng đời, xuyên / phản đạn | Combat QCBH có cấu hình đạn. Hiệu ứng bay đến đối thủ không đồng nghĩa đã có va chạm đạn thật. |

Project ví dụ `资源修改教程\ResBuildABProject_Example` có prefab Map/Unit/206 và Battle/ScenesUnit/Floor/101 (floor kích thước 5×5), cùng ví dụ Barriers. Các prefab UI trong thư mục đối chiếu tài nguyên có nhiều tệp 0 byte: không dùng chúng làm bằng chứng về layout, camera hoặc tỷ lệ nhân vật.

## Bản sửa iOSVN

- Thêm công cụ Unity `WorldTerrainBake`: dựng nền đất/nước thủy mặc, ghép cụm rừng/núi theo mask bản đồ; chừa công trình, đường, nước và điểm tương tác. Không tô đường thành dải màu thẳng hoặc tô ô blocked thành ô vuông. Bờ nước dùng coverage làm mềm để không lộ góc vuông; vùng collision vẫn giữ nguyên. Tệp xuất dùng 6 pixel/ô thay vì ảnh tổng khoảng 2 pixel/ô.
- Tạo atlas `grounded_towns_v1` thay các thành có đế đảo đá/thác nước. Thành được ghép một lần vào terrain theo vị trí cổng, nên đất quanh chân thành hòa vào nền và minimap/atlas nhìn thấy cùng công trình. Không thêm một hình thành thứ hai lên trên terrain. Các tranh v2 và atlas cũ vẫn là dự phòng.
- World player, NPC trong thành và nhân vật chính PvE/PvP dùng `FullBodyActorsV2`, bộ toàn thân có cả chân/giày và khuôn mặt/trang phục khớp chân dung V3. Cả bộ toàn thân và afterimage biến dạng qua lưới liên tục. Avatar HUD lấy đầu từ V3. Không dùng ảnh chân dung bị cắt ở đùi để thay sprite toàn thân. Các sprite cũ chỉ còn dự phòng nếu thiếu tài nguyên mới.
- World player dùng khung 64×72 đơn vị bản đồ, thành nhỏ/lớn dùng atlas trong khung 24×24 / 28×28 ô. Đây là tỷ lệ iOSVN chọn, không phải số đo nhân vật QCBH. Rig toàn thân dùng lưới khác với chân dung lớn để giữ đủ chi tiết chỉnh mặt ở màn hình chân dung.
- Zoom mặc định `.78`, giới hạn `.42–1.25`; đưa tên thành lên trên mái. Cổng thành gần nhất tại map_1 cách nhau 77 ô: không đổi tọa độ thành hoặc save. Tăng zoom giảm số thành thấy cùng lúc, không giả định QCBH dùng cùng khoảng cách hay zoom.
- Giới hạn nhân vật PvE theo kích thước sân và kích thước cơ thể, thay vì trừ gần nửa màn hình ở mỗi mép. Né dùng cùng giới hạn. PvP bỏ giới hạn tọa độ cố định của màn hình mẫu và lấy kích thước sân hiện tại.
- Kiểm tra Unity: độ phân giải terrain, tỷ lệ ảnh, minimap chuyển tỉnh / đánh dấu người / viewport, zoom giữ người, full-body V2 world player và V3 avatar HUD, chuyển động chân khi di chuyển, tỷ lệ người/thành, tỷ lệ người PvE/PvP và đi tới vùng gần mép sân. Kiểm tra nam/nữ với 6 trạng thái rig, cùng regression hủy path / vào thành / mở chân dung.

## Phần chưa thể gọi là giống QCBH

Màn hình thành trì của iOSVN dùng tranh panorama và điểm dịch vụ, chưa có layout UI gốc trích xuất được để đo so sánh. Combat iOSVN đang gửi `/battle/act`; server xử lý sát thương và cửa sổ né trong `ipa_core/engine.js`. Vị trí nhân vật hiện chủ yếu nằm ở client. Chưa có mô phỏng server đầy đủ cho đường đạn, vật cản, khoảng cách trúng và AI phòng như QCBH. Các tranh combat hiện có cũng chưa thay thế bằng hệ thống phòng / vật cản độc lập.

QCBH đã mở được menu; save cũ báo lỗi ở lần kiểm tra trước. Lần mở ô mới bị treo / công cụ UI timeout. Vì vậy không tuyên bố đã trực tiếp kiểm chứng minimap, thành trì và combat trong một ván QCBH đang chơi. Các kết luận trên giới hạn ở những tệp thực sự đọc được và mã iOSVN hiện tại.

Tăng độ phân giải terrain tăng bộ nhớ texture. Kiểm tra Editor không thay thế kiểm tra hiệu năng trên iPhone; chưa xuất IPA ở lần sửa này.

## Tài nguyên hình ảnh mới và prompt

Dùng built-in imagegen; không dùng CLI/API key. Các file được lưu trong project:

- `D:\game_iosvn\unity_project\Assets\Resources\World\Art\ink_ground_v1.png`
- `D:\game_iosvn\unity_project\Assets\Resources\World\Art\grounded_towns_v1.png`
- `D:\game_iosvn\unity_project\Assets\Resources\World\Art\ink_water_v1.png`
- `D:\game_iosvn\unity_project\Assets\Resources\Characters\FullBodyActorsV2.png`

Tệp terrain được dựng bằng mã Unity, không phải phóng lớn ảnh AI toàn thế giới: `world_pham_terrain.bytes` (4608×2880), `world_tien_terrain.bytes` (6144×2880). Prompt chính xác cho các ảnh nguồn:

### ink_ground_v1

Use case: stylized-concept. Create a production-ready seamless terrain ground texture for an original Chinese ink-wash cultivation exploration game. The supplied painting is a STYLE REFERENCE ONLY; do not reproduce its geography. Output a large square 2048x2048 or larger opaque texture of gently varied muted sage-green meadow, warm pale earth, moss and fine dry-brush grass texture on rice paper. Seen obliquely from above, extremely shallow relief, consistent diffuse light. Landscape should feel carefully hand-painted in delicate traditional Chinese shan shui linework and watercolor washes, cohesive with the reference. Subtle small sparse grass and tiny stones, generous quiet ground for characters and separately placed towns/mountains. All four edges must tile seamlessly; no central focal point, no horizon, no shadows from off-frame objects. No river, road, mountain, hill, cliff, buildings, characters, icons, text, grid, UI, framing, watermark. Not pixel art; no noise checkerboards, no plastic cartoon rendering, no bright lime green. Texture rather than a scenic vista.

### grounded_towns_v1

Use case: stylized-concept, game sprite atlas. The attached images are style references, NOT a layout to copy. Make a polished transparent PNG sprite sheet, a STRICT 4 columns by 4 rows uniform grid, exactly 16 isolated traditional Chinese settlements in equal square cells. Each cell has 12% clear transparent margins; no touching other cells. Each is a distinct realistic small settlement seen at the same elevated oblique angle in restrained, finely detailed Chinese shan shui ink linework and watercolor, subdued roof slate blue / moss green / warm gray and ochre earth, as in the landscape reference. Varieties: large walled provincial capital, small gated town, village of courtyard homes, monastery compound, market town, riverside port, mountain pass fort, bamboo village; sixteen varied arrangements. Low compact architecture clusters with tiled roofs and a discernible south/front gate opening at the bottom center. Grounded buildings on flat natural terrain: a thin soft irregular wash of pale earth and a few perimeter shrubs blending smoothly to TRUE alpha transparency. Towns should feel part of a painted landscape, not inventory icons. Remove the raised circular rock pedestals, sheer cliffs, waterfalls, huge staircases, isolated islands and tall mountain backdrop seen in the fortress reference. No snow, lava, bright blossoms or magic effects. No decorative outline, drop-shadow oval, hard base cutout, colored halo, floating platform, labels, gridlines, UI, text, watermark. Consistent scale and lighting across all 16. Output square 2048x2048 or larger with actual transparent background. Each sprite fills about 76% of its cell width and 62% height, front gate at 80% down each cell.

### ink_water_v1

Use case: stylized-concept. Production-ready seamless square water surface texture for a Chinese ink-wash landscape game, 2048x2048 or larger. View from above at very shallow oblique angle, no perspective horizon. ONLY calm pale muted jade / gray turquoise river water, handmade soft watercolor washes and extremely delicate curving ink ripple lines, fine paper texture, a few gentle short ivory ripple strokes. Cohesive with traditional Chinese shan shui landscape painting: restrained, airy, natural, understated. Large areas of quiet surface, modest variation, subtle details. All edges must tile seamlessly. No shoreline, grass, bank, rocks, plants, buildings, characters, objects, text, icons, grid, frames or watermark. No bright blue/cyan, no pixel art, no straight stripe pattern, no ocean surf, no glitter or photorealism. Opaque texture.

### FullBodyActorsV2 — tạo toàn thân

Use case: identity-preserve / compositing, production game character sprite sheet. Image 1 is the FULL-BODY LAYOUT reference: two equal vertical cells, male left, female right, both standing entirely head-to-toe. Images 2 and 3 are the NEW FACE AND COSTUME references, male and female respectively. Create an updated full-body sprite sheet, transparent PNG, wide 3:2 aspect ratio 1536x1024 or larger. Exactly two complete figures in two equal half-width cells, no clothing-only pieces or extra figures. Both characters must show the entire head/topknot, robe down to ankles, both legs and both boots/shoes; NEVER crop at waist or knees. Extend the new portrait costumes into coherent full-length garments. Male: same youthful face, long dark hair/topknot, dark jade/teal and ivory robe, intricate restrained silver shoulder armor and embroidery as image 2. Female: same youthful face and silver-white hair with gold hair ornament, flowing pale ivory/lavender dress with fine gold details as image 3, appropriate closed ankle boots below a long modest skirt. Both faces, hair and clothing look like the new portraits, rendered as clear painterly ink/watercolor xianxia characters for an action game. Both facing to the LEFT in the same slight three-quarter angle as image 1. Natural neutral standing bind pose, hands relaxed with separation from torso, two distinct feet on the same baseline; light asymmetric cloth and hair suitable for skeletal animation. Each figure centered exactly at the midpoint of its half-cell. Bottom of both boots at 94% down the canvas, hair at 5% down; leave clean alpha margins around head, hands, trailing fabric and feet. Crisp readable silhouette, stable natural human anatomy. Truly transparent background, including spaces between arms and torso, no black or gray backdrop, no glowing haze or gradient surround, no shadows or ground pedestal. No weapons, text, labels, grid, watermark. The output MUST be FULL BODY and fit both characters inside the frame with no body part cut off.

### FullBodyActorsV2 — chỉnh khoảng cách hai ô sprite

Precise edit of this full-body character sheet. Preserve BOTH characters' new faces, hair colors, ornate costumes, neutral standing pose, full legs and visible shoes, painterly style, canvas aspect ratio and true alpha transparency. Correct only the sprite-cell separation and cloak width: the sheet must have two strictly separate equal half-width cells. Male entirely within x=8% to 42% of the full canvas; female entirely within x=58% to 92%. Center male at x=25%, female at x=75%. Keep both at the same tall height, hair at 5% and boots at 94% down. Tuck or narrow the billowing side cloth and trailing hair to fit these exact safe bounds without changing anatomy. Make the wide vertical gutter from x=42% to 58% fully transparent from top to bottom, and both outer 8% margins fully transparent. No robe fragment or hair from one character may leak into the other half, no overlap, no element may be cropped. Both complete head-to-toe figures with both feet visible. Do not add weapons, shadows, glow, pedestal, background or text. Output transparent PNG, 1536x1024 or larger, ratio 3:2.
