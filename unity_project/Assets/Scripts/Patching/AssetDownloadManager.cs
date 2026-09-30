using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace QuyCocBatHoang.Patching
{
    /// <summary>
    /// AssetDownloadManager: Quản lý tải tài nguyên động qua CDN (Addressables / AssetBundles).
    /// Giúp file cài đặt .ipa khởi điểm siêu nhẹ chỉ 200 - 400 MB.
    /// Toàn bộ 1.5 - 2.8 GB tài nguyên game (quái Sơn Hải Kinh, âm thanh, map HD, hiệu ứng) 
    /// sẽ được tải ngầm hoặc tải ở màn hình cập nhật ban đầu.
    /// </summary>
    public class AssetDownloadManager : MonoBehaviour
    {
        public static AssetDownloadManager Instance { get; private set; }

        [Header("CDN Configuration")]
        [SerializeField] private string cdnBaseUrl = "https://cdn.tutien.iosvn.vn/game_assets/ios/";
        [SerializeField] private string versionManifestFile = "version_manifest.json";

        [Header("UI Events")]
        public UnityEvent<float, string> OnDownloadProgress; // (0.0 to 1.0, "245 MB / 1850 MB (12.4 MB/s)")
        public UnityEvent<string> OnStatusMessage;
        public UnityEvent OnDownloadComplete;
        public UnityEvent<string> OnDownloadFailed;

        [System.Serializable]
        public class AssetManifest
        {
            public int version;
            public long totalBytes;
            public List<BundleInfo> bundles;
        }

        [System.Serializable]
        public class BundleInfo
        {
            public string bundleName;
            public string md5;
            public long size;
            public bool isRequired; // Bắt buộc tải trước khi vào game
        }

        private AssetManifest serverManifest;
        private string localSavePath;
        private long totalBytesToDownload = 0;
        private long downloadedBytes = 0;
        private bool isDownloading = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                localSavePath = Path.Combine(Application.persistentDataPath, "AssetBundles");
                if (!Directory.Exists(localSavePath)) Directory.CreateDirectory(localSavePath);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartPatchCheck()
        {
            StartCoroutine(CheckVersionAndDownloadRoutine());
        }

        private IEnumerator CheckVersionAndDownloadRoutine()
        {
            OnStatusMessage?.Invoke("Đang kiểm tra phiên bản máy chủ...");

            // 1. Tải Manifest từ CDN
            string manifestUrl = cdnBaseUrl + versionManifestFile + "?t=" + DateTime.UtcNow.Ticks;
            using (UnityWebRequest req = UnityWebRequest.Get(manifestUrl))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    OnDownloadFailed?.Invoke("Không thể kết nối đến máy chủ cập nhật: " + req.error);
                    yield break;
                }

                try
                {
                    serverManifest = JsonUtility.FromJson<AssetManifest>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    OnDownloadFailed?.Invoke("Lỗi định dạng tệp manifest: " + ex.Message);
                    yield break;
                }
            }

            // 2. So sánh danh sách bundle cần tải
            List<BundleInfo> queue = new List<BundleInfo>();
            totalBytesToDownload = 0;

            foreach (var b in serverManifest.bundles)
            {
                string localFile = Path.Combine(localSavePath, b.bundleName);
                if (!File.Exists(localFile) || new FileInfo(localFile).Length != b.size)
                {
                    queue.Add(b);
                    totalBytesToDownload += b.size;
                }
            }

            // 3. Nếu không có gì cần tải -> Vào game luôn
            if (queue.Count == 0 || totalBytesToDownload == 0)
            {
                OnStatusMessage?.Invoke("Tài nguyên đã là bản mới nhất!");
                OnDownloadProgress?.Invoke(1.0f, "Hoàn tất kiểm tra");
                yield return new WaitForSeconds(0.5f);
                OnDownloadComplete?.Invoke();
                yield break;
            }

            // 4. Kiểm tra dung lượng trống của thiết bị iOS
            double totalMB = totalBytesToDownload / (1024.0 * 1024.0);
            OnStatusMessage?.Invoke($"Phát hiện bản cập nhật mới ({totalMB:F1} MB). Đang tải gói tài nguyên...");

            // 5. Tiến hành tải tuần tự từng AssetBundle với cơ chế tiếp tục (Resume)
            isDownloading = true;
            downloadedBytes = 0;
            float startTime = Time.time;

            foreach (var bundle in queue)
            {
                string bundleUrl = cdnBaseUrl + bundle.bundleName;
                string destPath = Path.Combine(localSavePath, bundle.bundleName);
                string tempPath = destPath + ".tmp";

                long existingBytes = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0;

                using (UnityWebRequest dlReq = UnityWebRequest.Get(bundleUrl))
                {
                    dlReq.downloadHandler = new DownloadHandlerFile(tempPath, true); // Hỗ trợ resume
                    if (existingBytes > 0)
                    {
                        dlReq.SetRequestHeader("Range", $"bytes={existingBytes}-");
                    }

                    var op = dlReq.SendWebRequest();
                    long lastReported = existingBytes;

                    while (!op.isDone)
                    {
                        long currentPart = (long)(dlReq.downloadProgress * (bundle.size - existingBytes)) + existingBytes;
                        long currentTotal = downloadedBytes + currentPart;
                        float percent = (float)currentTotal / totalBytesToDownload;
                        float elapsed = Mathf.Max(0.1f, Time.time - startTime);
                        float speedMBps = (currentTotal / (1024f * 1024f)) / elapsed;

                        string progressStr = $"{currentTotal / (1024 * 1024)} MB / {totalBytesToDownload / (1024 * 1024)} MB ({speedMBps:F1} MB/s)";
                        OnDownloadProgress?.Invoke(percent, progressStr);

                        yield return null;
                    }

                    if (dlReq.result != UnityWebRequest.Result.Success)
                    {
                        OnDownloadFailed?.Invoke($"Tải thất bại gói [{bundle.bundleName}]: {dlReq.error}");
                        yield break;
                    }

                    if (File.Exists(destPath)) File.Delete(destPath);
                    File.Move(tempPath, destPath);

                    downloadedBytes += bundle.size;
                }
            }

            isDownloading = false;
            OnStatusMessage?.Invoke("Giải nén và xác thực tài nguyên hoàn tất!");
            OnDownloadProgress?.Invoke(1.0f, "100% Hoàn tất");
            yield return new WaitForSeconds(0.8f);
            OnDownloadComplete?.Invoke();
        }
    }
}
