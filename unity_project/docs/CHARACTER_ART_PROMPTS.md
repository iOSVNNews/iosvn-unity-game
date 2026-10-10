# Prompt vẽ bộ phận nhân vật (dành cho Antigravity / AI vẽ tranh)

Hệ thống nhân vật mới ghép nhiều lớp giống kiểu QCBH: mặt, mắt, mày, mũi, miệng, râu, tóc, y phục, mũ… mỗi thứ là một lớp riêng, game tự nhuộm màu và tự gắn vào xương để đi / chạy / đánh. Muốn ghép khớp, **mỗi bộ phận phải được vẽ đúng chỗ trên khung mẫu cố định**, nền còn lại là xanh lá phẳng để tách nền.

## Khung mẫu (đưa kèm làm ảnh tham chiếu)

Thư mục `unity_project/ArtSource/ModularTemplates/`:

| Tệp | Kích thước | Dùng cho |
| --- | --- | --- |
| `head_male.png`, `head_female.png` | 1024×1024 | mặt, mắt, mày, mũi, miệng, râu, ấn ký, mũ |
| `body_male.png`, `body_female.png` | 1024×2048 | tóc (cả tóc trước và sau), y phục |
| `*_guides.png` | | bản có đường kẻ để người xem; **không** đưa cho AI |

Thông số khung (để kiểm tra): khung đầu 3,6 px/đơn vị, gốc xương đầu tại pixel (512, 780); khung thân 1,5 px/đơn vị, bàn chân tại pixel (512, 1990).
Khung thân dùng **tư thế chữ A**: hai tay dang ra khỏi thân khoảng 22°, để tay áo không đè lên thân áo — game cắt tay áo theo xương tay để cử động.

> Cập nhật sau lượt thử đầu: mặt, mắt, miệng, y phục khớp tốt. **Tóc bị vẽ to hơn đầu khuôn nên mái che mất mắt** → vẽ lại theo quy tắc mới ở mục 8. Mày và mũi vẽ hơi to; công cụ nhập đã tự thu nhỏ, nhưng nên vẽ nhỏ như mô tả.

## Nơi lưu kết quả

`D:\game_iosvn\unity_project\ArtSource\ModularParts\male\` và `...\female\`, tên tệp đúng như bảng dưới (PNG, giữ nguyên kích thước khung). Sau đó chạy `python unity_project/tools/character_art/import_painted.py` (hoặc báo Claude) để tách nền, cắt lớp, đóng atlas cho Unity. Tệp nào thiếu thì game dùng bộ vẽ bằng mã làm dự phòng, nên có thể làm dần từng nhóm.

---

## PROMPT CHUNG — dán trước mọi yêu cầu

```
You are painting ONE layer of a layered 2D character for an original Chinese xianxia RPG
character creator. Use the attached template image as an exact alignment guide.

STYLE: original artwork, refined Chinese ink-wash painting with fine brush linework and soft
watercolor shading, elegant semi-realistic East Asian proportions, the quality of a premium
wuxia game character portrait. Do not copy any existing game's characters or costumes.

LAYOUT RULES (critical):
- Output exactly the same canvas size as the template (1024x1024 or 1024x2048), same framing.
- Paint the requested part exactly where it belongs on the template figure. Do not move,
  scale, rotate or re-pose the figure. The front-facing head and the standing pose stay identical.
- Output ONLY the requested part. Everything else, including the template figure itself, must be
  pure flat chroma green #00FF00 - no gradients, no shadows on the background, no ground, no text.
- Crisp clean edges against the green; no green tint inside the painted part.

COLOR RULE: paint in neutral GRAYSCALE only (no hue at all) unless the request says
"in natural color". The game recolors the gray. Use a full value range: lit areas near white,
shadows mid gray, ink lines dark gray to black.
```

---

## Yêu cầu từng bộ phận (thay {GIỚI TÍNH} = male / female, dùng khung tương ứng)

### 1. Khuôn mặt — khung `head_*.png`, tên `face_0.png` … `face_3.png`

```
Paint ONLY the bare head: face skin, both ears and the neck down to the collar line,
bald (no hair, no eyebrows, no eyes, no nose, no mouth - leave a smooth blank face, those are separate layers).
Grayscale skin with gentle shading, light from the upper left, thin warm-gray ink contour along the jaw.
Face shape: {SHAPE}.
```
`face_0` = oval face with a tapering chin · `face_1` = balanced, slim jaw · `face_2` = broad face, square strong jaw · `face_3` = round face, soft full cheeks.
Nữ: thêm "delicate feminine face".

### 2. Mắt — khung `head_*.png`, tên `eyes_0.png` … `eyes_7.png`

```
Paint ONLY a pair of eyes in natural color, exactly on the template's eye positions:
upper eyelid ink line, double eyelid crease, white of the eye, dark brown iris with a small
highlight, faint lower lid line. No eyebrows, no skin, no other features. Eye type: {TYPE}.
```
0 narrow sharp eyes · 1 small eyes · 2 almond eyes · 3 balanced eyes · 4 bright clear eyes · 5 large eyes · 6 round eyes · 7 phoenix eyes, long and strongly upturned at the outer corner.
Nữ: thêm "soft feminine eyes with fine upper lashes".

### 3. Lông mày — `brows_0.png` … `brows_4.png` (xám)

```
Paint ONLY a pair of eyebrows in grayscale, individual brush hairs visible, exactly on the
template's eyebrow positions. Each brow is about as long as the eye below it, and both brows stay
well inside the outline of the face. Brow type: {TYPE}.
```
0 thin straight brows · 1 natural brows · 2 sword brows, rising sharply toward the temples · 3 high arched brows · 4 thick bold brows.

### 4. Mũi — `nose_0.png` … `nose_3.png` (xám)
```
Paint ONLY the nose in grayscale: soft bridge shading, nose tip and nostrils, no hard outline.
Keep it small and delicate: the nose is narrower than the gap between the two eyes and its tip sits
exactly where the template's nose tip is. Nose type: {TYPE}.
```
0 small nose · 1 straight nose · 2 high-bridged nose · 3 broad nose.

### 5. Miệng — `mouth_0.png` … `mouth_4.png` (màu tự nhiên)
```
Paint ONLY the lips in natural color, exactly on the template's mouth position. Mouth type: {TYPE}.
```
0 calm closed mouth · 1 gentle smile · 2 thin stern mouth · 3 full lips · 4 slight one-sided smirk.

### 6. Râu (chỉ nam) — `beard_1.png` … `beard_4.png` (xám)
```
Paint ONLY facial hair in grayscale, growing from the correct places on the template face,
with fine brush strands. Leave the lips visible. Style: {TYPE}.
```
1 thin drooping moustache · 2 moustache and small goatee · 3 long flowing sage beard reaching the chest · 4 short full beard along the jaw.

### 7. Ấn ký trán — `mark_1.png` … `mark_5.png` (xám)
```
Paint ONLY a small forehead mark in the center of the forehead, solid light gray with a thin darker edge: {TYPE}.
```
1 flame · 2 vertical third-eye · 3 three-petal lotus · 4 crescent moon · 5 round dot.

### 8. Tóc — khung **`body_*.png`**, tên `hair_0.png` … `hair_9.png` (xám)
```
Paint ONLY the hair in grayscale on the template figure: the whole hairstyle including the part
over the forehead, the sides, and any hair falling behind the head and shoulders. Flowing brush
strands with soft sheen.
SIZE AND PLACEMENT (critical): the hair must fit the template's bald head exactly - the same skull
size, the hairline on the template's forehead, the sides just outside the template's temples and ears.
Bangs or fringe must end ABOVE the template's eyebrows: the eyes, eyebrows and the whole face below the
hairline stay completely uncovered (green). Hair falling down the back may spread wide behind the
shoulders. Hairstyle: {TYPE}.
```
Nam: 0 topknot with long hair down the back · 1 high ponytail · 2 short messy hair · 3 long loose hair parted in the middle · 4 half-up bun with long hair · 5 neat bun, short at the back · 6 long hair with side-swept bangs · 7 low tied tail with one lock in front of the ear · 8 wild long untamed hair · 9 shaved head (only a faint shadow of stubble).
Nữ: 0 long straight hair with curtain bangs · 1 high bun with a hairpin · 2 twin buns with straight bangs · 3 high ponytail · 4 half-up bun with a hairpin, long hair · 5 long hair with blunt bangs · 6 side ponytail over the shoulder · 7 shoulder-length bob with bangs · 8 one long braid · 9 low bun with a hairpin.

### 9. Y phục — khung `body_*.png`, tên `outfit_0.png` … `outfit_5.png` (xám)
```
Paint ONLY the clothing in grayscale on the template figure, from the collar to the shoes,
including sleeves, belt or sash, trousers and boots. The template stands in an A-pose: each sleeve
runs along the template's arm, angled away from the body, with a clear green gap between the
sleeve and the torso, and the sleeve opening ends at the template's wrist. Paint the robe's chest and
sides completely, as if the arms were lifted away. The two boots stand apart exactly on the
template's feet, soles on the template's ground line.
Use lighter gray (near white) for trims, inner collar and hems, mid gray for the main robe,
darker gray for trousers and boots. No hands, no head, no hair. Outfit: {TYPE}.
```
0 short martial-arts tunic to the knees with trousers and boots · 1 long Taoist robe to the ankles with wide sleeves · 2 swordsman robe to mid-calf, front slit, leather bracers · 3 light armor: lamellar chest piece and shoulder guards over a short robe · 4 scholar robe with very wide sleeves and a broad collar · 5 long robe with a cape hanging from the shoulders.
Nữ: thêm "feminine cut, flowing layered skirt".

### 9b. Bàn tay — khung `body_*.png`, tên `hands.png` (xám)
```
Paint ONLY the two bare hands in grayscale, relaxed and slightly curled, exactly at the template's
hands (the wrists start where the template's sleeves end). Elegant slender fingers, soft shading.
```

### 10. Mũ / trang sức — khung `head_*.png`, tên `hat_1.png` … `hat_5.png` (xám)
```
Paint ONLY the headwear in grayscale, sitting correctly on the template head (assume a topknot on top). Type: {TYPE}.
```
1 small jade crown over the topknot with a hairpin · 2 wide conical bamboo hat · 3 lotus-petal crown · 4 cloth headband with ribbon tails · 5 ornate gold crown with a long hairpin.

---

## Kiểm tra nhanh trước khi giao

- Mở ảnh chồng lên khung mẫu (độ mờ 50%): bộ phận phải trùng vị trí mặt/thân của khung.
- Nền chỉ một màu #00FF00; không có bóng đổ trên nền.
- Ảnh xám thật sự (không ngả màu) trừ mắt và môi.

---

## VÒNG 2 — vẽ lại các bộ phận chưa đạt (09/10/2026)

**Khuôn thân đã đổi sang dáng người lớn chuẩn Quỷ Cốc Bát Hoang (QCBH)**: Thân hình cao ~7 đầu, **khung người to, vai rộng vạm vỡ** (vai nam ~2.5 - 2.6 lần đầu ~450-480px, vai nữ ~2.1 - 2.2 lần đầu ~340-365px, ngực nở, eo lưng vững chãi, tay áo thụng bồng bềnh, dáng đứng A-pose dang tay 22° tạo khoảng cách xanh rõ ràng giữa tay áo và thân; khung thân 1,5 px/đơn vị, bàn chân ở pixel (512, 1990)). Mọi thứ vẽ trên khung thân (tóc, y phục, bàn tay) phải vẽ theo `body_male.png` / `body_female.png` MỚI; ảnh cũ không còn khớp. Khuôn đầu không đổi, nên mặt, mắt, mày, miệng, râu, ấn ký, mũ vẫn dùng được.

Kiểm tra sau khi ghép thấy các lỗi do **cách vẽ**, không phải do vị trí:

1. **Tóc:** chỗ mặt bị khoét bằng một khung chữ nhật / hình mặt, nên trán có đường cắt thẳng và tóc hai bên thái dương bị mất.
2. **Y phục:** nhiều mảng trắng lớn và hoa văn trắng, nên khi game nhuộm màu áo bị loang lổ. Phần vạt áo ngắn bị tô tối như quần, nên game nhầm thành quần.
3. **Mũi:** có nơi vẽ kèm cả một mảng da quanh mũi, thành mảng vuông trên mặt.
4. **Bàn tay:** tô xám quá đậm, nên sau khi nhuộm da tay bị xỉn.
5. **Mũ (`hat_1`, `hat_5`):** nằm lơ lửng trên đỉnh đầu.
6. **`male/eyes_3`:** vẽ ở lượt đầu, khác hẳn bộ mắt còn lại.

### Quy tắc độ sáng (bắt buộc cho mọi ảnh xám)

Game nhuộm màu bằng cách nhân với độ sáng và tự tách vùng theo độ sáng, nên phải giữ đúng các khoảng sau:

| Vùng | Độ sáng (0 = đen, 100 = trắng) |
| --- | --- |
| Vải chính của áo / váy / tay áo | 45–75, đổ bóng mềm; nếp gấp xuống tối nhất 35 |
| Viền, lót cổ, đường viền gấu, đai | 85–95, **chỉ** ở mép / dải hẹp, không loang thành mảng lớn |
| Quần và giày / ủng | 15–35 |
| Da (mặt, tay) | vùng sáng 90–100, bóng 65–80 |
| Tóc | 35–80, sợi sáng tối 25 |
| Nét mực viền | 5–20 |

Hoa văn thêu trên áo: chỉ dùng tông trung (55–70), mảnh, không dùng trắng.

---

### Đoạn dán cho Antigravity

```
Đọc D:\game_iosvn\unity_project\docs\CHARACTER_ART_PROMPTS.md, gồm PROMPT CHUNG và mục "VÒNG 2".
Dùng ảnh khuôn MỚI trong D:\game_iosvn\unity_project\ArtSource\ModularTemplates\ (không dùng *_guides.png):
body_male.png / body_female.png đã đổi sang dáng người lớn cao ~7 đầu, vai rộng — y phục, tóc và tay
phải khớp đúng dáng mới này (không dùng lại ảnh cũ).
Ghi đè đúng tên file cũ trong ArtSource\ModularParts\male\ và ...\female\.
Tuyệt đối KHÔNG khoét mặt bằng mặt nạ / hình chữ nhật và KHÔNG xử lý hậu kỳ bằng code: vẽ trực tiếp,
mép tóc và mép áo là nét vẽ tự nhiên trên nền #00FF00.

VẼ LẠI:
A. Tất cả 20 kiểu tóc (male\hair_0..9, female\hair_0..9).
   Thêm vào prompt tóc: "Draw a natural painted hairline across the forehead and down both temples,
   following the template head's shape; hair covers the top and sides of the skull and the ears' upper
   edge like real hair. Never leave straight cut edges. Only the face below the hairline stays green.
   Bangs end above the eyebrows."
B. Tất cả 12 bộ y phục (male\outfit_0..5, female\outfit_0..5).
   Thêm vào prompt y phục: "Keep the gray values strictly: main fabric 45-75% brightness with soft
   shading; trims, collar lining, hems and sash only as narrow bands at 85-95%; trousers and boots
   15-35%; embroidery mid gray 55-70%, never white patches. A short tunic's skirt is main fabric
   (45-75%), clearly lighter than the trousers below it. Wide sleeves hang from the template arm and
   must not overlap the skirt or the torso: keep a green gap."
C. male\nose_0..3 và female\nose_0..3: "only the nose's soft shadow lines, tip and nostrils; no skin
   patch around it - everything else green".
D. male\hands.png và female\hands.png: skin 90-100% in the light, 65-80% in shadow.
E. male\hat_1, male\hat_5, female\hat_1, female\hat_5: "the crown sits directly on the top of the
   template skull (on the hair), touching the head, not floating above it".
F. male\eyes_3: vẽ lại cùng phong cách với male\eyes_0..7 còn lại.

Mỗi nhóm A–F xong thì lưu ngay để Claude kiểm tra dần.
```
