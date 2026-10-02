#if UNITY_EDITOR
using System.IO;
using IOSVN.TuTien.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Editor
{
    public static class CombatPixelValidation
    {
        [MenuItem("iOSVN/Combat/Render PvE pixel previews")]
        public static void RenderPvePreviews()
        {
            Render("cuu_vi_ho", "Cửu Vĩ Ma Hồ", "ma", "pve-cuu-vi-ma-ho.png");
            Render("thanh_long_anh", "Thanh Long Chân Linh", "moc", "pve-thanh-long.png");
            Render("hoa_ho", "Hỏa Diễm Ma Điệp", "hoa", "pve-hoa-diem-ma-diep.png");
            Render("cuu_vi_ho", "Cửu Vĩ Ma Hồ", "ma", "pve-iphone-small.png", 960, 540);
            Render("thanh_long_anh", "Thanh Long Chân Linh", "moc", "pve-ipad-4x3.png", 1024, 768);
        }

        private static void Render(string id, string name, string element, string filename, int width = 1280, int height = 590)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("CombatPreviewCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 295;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            var canvasObject = new GameObject("CombatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            scaler.SendMessage("Handle", SendMessageOptions.DontRequireReceiver);
            var stage = new GameObject("ProductionPixelCombat", typeof(RectTransform), typeof(PixelCombatPresentation));
            stage.transform.SetParent(canvasObject.transform, false);
            var view = stage.GetComponent<PixelCombatPresentation>();
            view.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
            var battle = new BattleView
            {
                id = "preview-" + id,
                battleMap = new BattleMapInfo { id = "preview", name = "U Minh Bí Cảnh" },
                m = new BattleMonsterView { id = id, name = name, element = element, hp = 9000, maxHp = 12000, minionCount = 0 },
                p = new BattlePlayerView { name = "Lăng Vân", hp = 1860, maxHp = 2200, mp = 370, maxMp = 500 },
                skills = new[] {
                    new BattleSkill { id = "kiem_khi_tram", name = "Kiếm Khí Trảm", kind = "atk" },
                    new BattleSkill { id = "ngu_kiem", name = "Ngự Kiếm Thuật", kind = "atk" },
                    new BattleSkill { id = "phi_kiem_lien_tram", name = "Thanh Trúc Kiếm Trận", kind = "multi" },
                    new BattleSkill { id = "kiem_khi_ho_the", name = "Kiếm Khí Ngân Hà", kind = "reflect" },
                    new BattleSkill { id = "kiem_vuc", name = "Vạn Kiếm Quy Tông", kind = "multi" },
                },
                items = new[] {
                    new BattleItem { i = 0, id = "hoi_xuan_dan", uid = "preview-hp", name = "Hồi Xuân Đan", qty = 8 },
                    new BattleItem { i = 1, id = "hoi_linh_dan", uid = "preview-mp", name = "Hồi Linh Đan", qty = 5 },
                    new BattleItem { i = 2, id = "phu_dinh_than", uid = "preview-stun", name = "Phù Định Thân", qty = 2 },
                },
                log = new[] { new BattleLogLine { text = "Yêu thú đã xuất hiện. Chuẩn bị ra chiêu!" } }
            };
            view.BuildPve(battle, null, new AppearanceColors { hair = "#2a2928", outfit = "#497870", eyes = "#e3c676" },
                "Kiếm tu", "moc_kiem", false, null, null, null, null, null, null, null, null, null, null);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, filename), screenshot.EncodeToPNG());
            RenderTexture.active = old;
            Object.DestroyImmediate(screenshot);
            Object.DestroyImmediate(target);
            Debug.Log("PVE_PIXEL_RENDERED: " + filename);
        }
    }
}
#endif
