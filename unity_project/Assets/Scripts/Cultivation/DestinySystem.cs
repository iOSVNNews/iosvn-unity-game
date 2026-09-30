using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace QuyCocBatHoang.Cultivation
{
    [System.Serializable]
    public class DestinyPerk
    {
        public string id;
        public string name;
        public string description;
        public int rarity; // 1: Lam, 2: Tím, 3: Cam, 4: Đỏ (Thần Cấp)
        public Sprite icon;
    }

    /// <summary>
    /// DestinySystem: Hệ thống Đột Phá Cảnh Giới & Nghịch Thiên Cải Mệnh
    /// Chuẩn Quỷ Cốc Bát Hoang:
    /// - 3 Lối Đột Phá: Thiên Đạo (cần 6 Lục Khí + Thiên Tài Địa Bảo), Địa Đạo, Nhân Đạo
    /// - Khi đột phá đại cảnh giới, hệ thống bốc ngẫu nhiên 3 thiên phú Nghịch Thiên Cải Mệnh
    /// </summary>
    public class DestinySystem : MonoBehaviour
    {
        public static DestinySystem Instance { get; private set; }

        public static readonly string[] REALMS = {
            "Luyện Khí", "Trúc Cơ", "Kết Tinh", "Kim Đan", "Cụ Linh",
            "Nguyên Anh", "Hóa Thần", "Ngộ Đạo", "Vũ Hóa", "Đăng Tiên"
        };

        [Header("Destiny Perk Library")]
        public List<DestinyPerk> allPerks = new List<DestinyPerk>();

        [Header("Active Player Destinies")]
        public List<DestinyPerk> acquiredPerks = new List<DestinyPerk>();

        public UnityEvent<List<DestinyPerk>> OnDrawDestinyCards;
        public UnityEvent<string, DestinyPerk> OnBreakthroughSuccess;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePerkDatabase();
        }

        private void InitializePerkDatabase()
        {
            if (allPerks.Count > 0) return;

            allPerks.Add(new DestinyPerk { id = "kiem_linh", name = "Kiếm Linh Tinh Thông", rarity = 4, description = "Triệu hồi kiếm linh phi hành hộ thể, tự động công kích kẻ địch trong tầm nhìn." });
            allPerks.Add(new DestinyPerk { id = "vo_than", name = "Võ Thần Tàn Quyết", rarity = 4, description = "Vào trận đem toàn bộ Khí Huyết biến thành Hộ Thuẫn khổng lồ, bạo phát lực công kích cực hạn." });
            allPerks.Add(new DestinyPerk { id = "huyet_ma", name = "Huyết Ma Đao Pháp", rarity = 3, description = "Đánh trúng kẻ địch cận chiến hút 15% sát thương thành sinh lực hồi phục." });
            allPerks.Add(new DestinyPerk { id = "tat_phong", name = "Tật Phong Kình Thảo", rarity = 3, description = "Tốc độ di chuyển tăng 25%, thời gian hồi chiêu của Thân Pháp giảm 35%." });
            allPerks.Add(new DestinyPerk { id = "bat_hoang_don", name = "Bát Hoang Độn Thuật", rarity = 4, description = "Tử vong lập tức kích hoạt độn thuật hồi sinh với 35% Khí Huyết (1 lần mỗi trận)." });
            allPerks.Add(new DestinyPerk { id = "thuy_luc", name = "Thủy Lực Tứ Xạ", rarity = 3, description = "Đánh thường bắn thêm 3 đạo thủy tiễn áp súc xuyên thấu vạn vật." });
        }

        /// <summary>
        /// Rút 3 quẻ Nghịch Thiên Cải Mệnh khi đủ điều kiện đột phá
        /// </summary>
        public List<DestinyPerk> DrawDestinyCards()
        {
            List<DestinyPerk> available = allPerks.FindAll(p => !acquiredPerks.Exists(a => a.id == p.id));
            List<DestinyPerk> picked = new List<DestinyPerk>();

            // Trộn ngẫu nhiên
            for (int i = 0; i < available.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, available.Count);
                var temp = available[i];
                available[i] = available[r];
                available[r] = temp;
            }

            int count = Mathf.Min(3, available.Count);
            for (int i = 0; i < count; i++)
            {
                picked.Add(available[i]);
            }

            OnDrawDestinyCards?.Invoke(picked);
            return picked;
        }

        /// <summary>
        /// Người chơi chọn 1 trong 3 thiên phú
        /// </summary>
        public void SelectDestinyPerk(DestinyPerk perk, int nextRealmIndex)
        {
            acquiredPerks.Add(perk);
            string realmName = REALMS[Mathf.Clamp(nextRealmIndex, 0, REALMS.Length - 1)];
            OnBreakthroughSuccess?.Invoke(realmName, perk);
        }
    }
}
