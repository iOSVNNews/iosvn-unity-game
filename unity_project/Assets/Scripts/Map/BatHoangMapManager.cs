using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace QuyCocBatHoang.Map
{
    public enum TileType
    {
        Wilderness,      // Hoang dã (cỏ cây, rừng trúc)
        Mountain,        // Dãy núi Minh Sơn hiểm trở (vật cản không thể đi qua)
        River,           // Sông hồ linh thủy
        TownMajor,       // Đại Thành (Vĩnh Ninh Thành, Hoa Tứ Thành...)
        TownMinor,       // Thôn xóm / dịch trạm
        SectOrthodox,    // Tông môn Chính Đạo
        SectDemonic,     // Tông môn Ma Đạo
        SpiritSpring,    // Linh Khí Chi Địa (Lục Đạo: Thủy, Hỏa, Lôi, Phong, Thổ, Mộc)
        DungeonSecret,   // Thông Thiên Bí Cảnh / Cổ Động
        BossLair,        // Sào huyệt Yêu Thú Sơn Hải Kinh (Đương Khang, Minh Xà, Lục Ngô)
        TeleportArray    // Cổ Truyền Tống Trận (vượt Lôi Trạch, Thập Vạn Đại Sơn)
    }

    [System.Serializable]
    public class MapTile
    {
        public int x;
        public int y;
        public TileType type;
        public string name;
        public bool isExplored; // Đã mở sương mù
        public bool isPassable; // Có thể bước vào hay không
        public int dangerLevel; // Cấp độ nguy hiểm (tương ứng cảnh giới yêu thú)
        public string bossId;   // ID boss nếu là BossLair
    }

    /// <summary>
    /// BatHoangMapManager: Quản lý bản đồ đại lục Bát Hoang dạng sa bàn lưới (Grid/Hex)
    /// Tái hiện 100% cơ chế Quỷ Cốc Bát Hoang:
    /// - Các đại châu: Bạch Nguyên, Vĩnh Ninh Châu, Hoa Tứ Châu, Mộ Tiên Châu...
    /// - Dãy núi Minh Sơn và Lôi Trạch ngăn cách
    /// - Sương mù khám phá (Fog of War) dạng mực loang
    /// - Di chuyển tốn ngày tháng, kích hoạt Nguyệt Báo và sự kiện ngẫu nhiên
    /// </summary>
    public class BatHoangMapManager : MonoBehaviour
    {
        public static BatHoangMapManager Instance { get; private set; }

        [Header("Bát Hoang World Map Dimensions")]
        public int mapWidth = 96;   // Chiều rộng bản đồ toàn cõi
        public int mapHeight = 64;  // Chiều cao bản đồ toàn cõi
        public float tileSize = 1.2f;

        [Header("Lịch Bát Hoang")]
        public int currentYear = 1;
        public int currentMonth = 1;
        public int currentDay = 1;

        [Header("Player Position")]
        public Vector2Int playerCoord = new Vector2Int(12, 14);

        public UnityEvent<int, int, int> OnDateChanged; // Year, Month, Day
        public UnityEvent<string> OnMonthlyBulletin;     // Nguyệt Báo hàng tháng
        public UnityEvent<MapTile> OnPlayerStepOnTile;

        private MapTile[,] mapGrid;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            GenerateBatHoangWorld();
            RevealFogOfWar(playerCoord.x, playerCoord.y, 4);
        }

        public void GenerateBatHoangWorld()
        {
            mapGrid = new MapTile[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    MapTile tile = new MapTile
                    {
                        x = x,
                        y = y,
                        type = TileType.Wilderness,
                        name = "Bát Hoang Hoang Dã",
                        isExplored = false,
                        isPassable = true,
                        dangerLevel = 1
                    };

                    // 1. Phân chia đại lục theo trục X:
                    // x: 0 - 24  -> Bạch Nguyên & Vĩnh Ninh Châu (Luyện Khí, Trúc Cơ)
                    // x: 25 - 28 -> Dãy núi Lôi Trạch (Ranh giới ngăn cách hiểm trở)
                    // x: 29 - 55 -> Hoa Tứ Châu & Thập Vạn Đại Sơn (Kết Tinh, Kim Đan)
                    // x: 56 - 95 -> Mộ Tiên Châu & Xích U Châu (Nguyên Anh -> Đăng Tiên)

                    if (x >= 25 && x <= 28)
                    {
                        // Dải sấm sét Lôi Trạch
                        tile.type = (y == 32) ? TileType.TeleportArray : TileType.Mountain;
                        tile.name = (y == 32) ? "Lôi Trạch Truyền Tống Trận" : "Minh Sơn Lôi Trạch";
                        tile.isPassable = (y == 32);
                        tile.dangerLevel = 3;
                    }
                    else if (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) > 0.68f)
                    {
                        // Dãy núi tự nhiên Minh Sơn
                        tile.type = TileType.Mountain;
                        tile.name = "Minh Sơn Hiểm Trở";
                        tile.isPassable = false;
                    }

                    mapGrid[x, y] = tile;
                }
            }

            // 2. Bố trí Thành Trì & Tông Môn & Bí Cảnh theo chuẩn Quỷ Cốc Bát Hoang:
            // Vĩnh Ninh Châu:
            SetTile(12, 14, TileType.TownMajor, "Vĩnh Ninh Thành", 2);
            SetTile(8, 20, TileType.SectOrthodox, "Vạn Kiếm Tông", 2);
            SetTile(18, 8, TileType.SectDemonic, "Huyết Ma Tông", 2);
            SetTile(16, 22, TileType.BossLair, "Sơn Động Đương Khang", 2, "duong_khang");
            SetTile(22, 12, TileType.DungeonSecret, "Thông Thiên Bí Cảnh (Trúc Cơ)", 2);

            // Bố trí 6 vùng Linh Khí Chi Địa (Thủy, Hỏa, Lôi, Phong, Thổ, Mộc):
            SetTile(6, 6, TileType.SpiritSpring, "Thủy Linh Khí Chi Địa", 2);
            SetTile(19, 19, TileType.SpiritSpring, "Hỏa Linh Khí Chi Địa", 2);
            SetTile(23, 5, TileType.SpiritSpring, "Lôi Linh Khí Chi Địa", 2);
            SetTile(5, 23, TileType.SpiritSpring, "Phong Linh Khí Chi Địa", 2);
            SetTile(10, 10, TileType.SpiritSpring, "Thổ Linh Khí Chi Địa", 2);
            SetTile(14, 25, TileType.SpiritSpring, "Mộc Linh Khí Chi Địa", 2);

            // Hoa Tứ Châu:
            SetTile(42, 30, TileType.TownMajor, "Hoa Tứ Thành", 4);
            SetTile(36, 40, TileType.BossLair, "Đầm Lầy Minh Xà", 4, "minh_xa");
            SetTile(48, 22, TileType.BossLair, "Côn Lôn Lục Ngô Thần Thú", 5, "luc_ngo");
        }

        private void SetTile(int x, int y, TileType type, string name, int danger, string bossId = "")
        {
            if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return;
            mapGrid[x, y].type = type;
            mapGrid[x, y].name = name;
            mapGrid[x, y].dangerLevel = danger;
            mapGrid[x, y].bossId = bossId;
            mapGrid[x, y].isPassable = true;
        }

        /// <summary>
        /// Người chơi di chuyển tới ô mục tiêu. Tiêu hao 1-2 ngày đường, kích hoạt sự kiện.
        /// </summary>
        public bool MovePlayer(Vector2Int target)
        {
            if (target.x < 0 || target.x >= mapWidth || target.y < 0 || target.y >= mapHeight) return false;
            MapTile targetTile = mapGrid[target.x, target.y];
            if (!targetTile.isPassable) return false;

            int dist = Mathf.Abs(playerCoord.x - target.x) + Mathf.Abs(playerCoord.y - target.y);
            playerCoord = target;

            // Mỗi bước đi tốn 1 ngày
            AdvanceDays(Mathf.Max(1, dist));

            // Mở sương mù quanh người chơi
            RevealFogOfWar(playerCoord.x, playerCoord.y, 3);

            OnPlayerStepOnTile?.Invoke(targetTile);
            return true;
        }

        public void RevealFogOfWar(int cx, int cy, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            mapGrid[nx, ny].isExplored = true;
                        }
                    }
                }
            }
        }

        public void AdvanceDays(int days)
        {
            currentDay += days;
            while (currentDay > 30)
            {
                currentDay -= 30;
                currentMonth++;
                TriggerMonthlyBulletin();
            }
            while (currentMonth > 12)
            {
                currentMonth -= 12;
                currentYear++;
                OnMonthlyBulletin?.Invoke($"🌾 Sang Năm Mới: Năm Thứ {currentYear}! Đấu Giá Hội Vĩnh Ninh Thành chính thức khai mạc!");
            }
            OnDateChanged?.Invoke(currentYear, currentMonth, currentDay);
        }

        private void TriggerMonthlyBulletin()
        {
            string bulletin = $"🌙 Nguyệt Báo Tháng {currentMonth}: Địa mạch linh khí biến chuyển. Yêu thú hoang dã xuất hiện rầm rộ tại Lôi Trạch.";
            OnMonthlyBulletin?.Invoke(bulletin);
        }

        public MapTile GetTile(int x, int y)
        {
            if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return null;
            return mapGrid[x, y];
        }
    }
}
