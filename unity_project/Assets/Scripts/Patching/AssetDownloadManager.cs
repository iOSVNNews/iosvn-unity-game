using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace QuyCocBatHoang.Patching
{
    /// <summary>
    /// Downloads versioned AssetBundles after startup so the initial iOS install
    /// stays small. The CDN is optional until a content host is configured.
    /// </summary>
    public sealed class AssetDownloadManager : MonoBehaviour
    {
        private static readonly Regex SafeBundleName = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.Compiled);

        public static AssetDownloadManager Instance { get; private set; }

        [Header("CDN Configuration")]
        [SerializeField] private string cdnBaseUrl = "";
        [SerializeField] private string versionManifestFile = "version_manifest.json";

        [Header("UI Events")]
        public UnityEvent<float, string> OnDownloadProgress = new UnityEvent<float, string>();
        public UnityEvent<string> OnStatusMessage = new UnityEvent<string>();
        public UnityEvent OnDownloadComplete = new UnityEvent();
        public UnityEvent<string> OnDownloadFailed = new UnityEvent<string>();

        [Serializable]
        public sealed class AssetManifest
        {
            public int version;
            public long totalBytes;
            public List<BundleInfo> bundles = new List<BundleInfo>();
        }

        [Serializable]
        public sealed class BundleInfo
        {
            public string bundleName;
            public string sha256;
            public long size;
            public bool isRequired;
        }

        private readonly Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private string localSavePath;
        private bool isDownloading;
        private bool patchCheckStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            localSavePath = Path.Combine(Application.persistentDataPath, "AssetBundles");
            Directory.CreateDirectory(localSavePath);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var bundle in loadedBundles.Values)
                if (bundle != null) bundle.Unload(false);
            loadedBundles.Clear();
        }

        public void Configure(string assetCdnBaseUrl)
        {
            cdnBaseUrl = string.IsNullOrWhiteSpace(assetCdnBaseUrl) ? "" : assetCdnBaseUrl.Trim().TrimEnd('/') + "/";
        }

        public void StartPatchCheck(Action<bool, string> finished = null)
        {
            if (patchCheckStarted || isDownloading)
            {
                finished?.Invoke(false, "Đang kiểm tra tài nguyên.");
                return;
            }
            patchCheckStarted = true;
            StartCoroutine(CheckVersionAndDownloadRoutine(finished));
        }

        private IEnumerator CheckVersionAndDownloadRoutine(Action<bool, string> finished)
        {
            if (string.IsNullOrWhiteSpace(cdnBaseUrl))
            {
                const string noCdnMessage = "Máy chủ tài nguyên chưa được cấu hình; tiếp tục với nội dung trong bản cài.";
                OnStatusMessage.Invoke(noCdnMessage);
                OnDownloadProgress.Invoke(1f, "Bản thử nghiệm");
                OnDownloadComplete.Invoke();
                finished?.Invoke(true, noCdnMessage);
                yield break;
            }

            if (!Uri.TryCreate(cdnBaseUrl, UriKind.Absolute, out var cdnUri) || cdnUri.Scheme != Uri.UriSchemeHttps)
            {
                const string invalidUrlMessage = "Địa chỉ máy chủ tài nguyên phải dùng HTTPS.";
                OnDownloadFailed.Invoke(invalidUrlMessage);
                finished?.Invoke(false, invalidUrlMessage);
                yield break;
            }

            OnStatusMessage.Invoke("Đang kiểm tra phiên bản tài nguyên...");
            AssetManifest manifest;
            var manifestUrl = BuildUrl(cdnUri, versionManifestFile) + "?t=" + DateTime.UtcNow.Ticks;
            using (var request = UnityWebRequest.Get(manifestUrl))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    var message = "Không thể kết nối máy chủ tài nguyên: " + request.error;
                    OnDownloadFailed.Invoke(message);
                    finished?.Invoke(false, message);
                    yield break;
                }

                try { manifest = JsonUtility.FromJson<AssetManifest>(request.downloadHandler.text); }
                catch (Exception exception)
                {
                    var message = "Manifest tài nguyên không hợp lệ: " + exception.Message;
                    OnDownloadFailed.Invoke(message);
                    finished?.Invoke(false, message);
                    yield break;
                }
            }

            if (!ValidateManifest(manifest, out var validationError))
            {
                OnDownloadFailed.Invoke(validationError);
                finished?.Invoke(false, validationError);
                yield break;
            }

            var queue = new List<BundleInfo>();
            long totalBytes = 0;
            foreach (var bundle in manifest.bundles)
            {
                var localFile = Path.Combine(localSavePath, bundle.bundleName);
                if (File.Exists(localFile) && new FileInfo(localFile).Length == bundle.size &&
                    string.Equals(ComputeSha256(localFile), bundle.sha256, StringComparison.OrdinalIgnoreCase))
                    continue;
                queue.Add(bundle);
                totalBytes += bundle.size;
            }

            if (queue.Count == 0)
            {
                PruneObsoleteBundles(manifest);
                const string upToDateMessage = "Tài nguyên đã là bản mới nhất.";
                OnStatusMessage.Invoke(upToDateMessage);
                OnDownloadProgress.Invoke(1f, "Đã cập nhật");
                OnDownloadComplete.Invoke();
                finished?.Invoke(true, upToDateMessage);
                yield break;
            }

            isDownloading = true;
            long completedBytes = 0;
            var startedAt = Time.realtimeSinceStartup;
            OnStatusMessage.Invoke($"Đang tải tài nguyên bản {manifest.version} ({totalBytes / (1024f * 1024f):F1} MB)...");

            foreach (var bundle in queue)
            {
                var destination = Path.Combine(localSavePath, bundle.bundleName);
                var temporary = destination + ".tmp";
                var bundleUrl = BuildUrl(cdnUri, bundle.bundleName);
                var attempt = 0;
                var bundleReady = false;

                while (!bundleReady && attempt < 2)
                {
                    var existingBytes = File.Exists(temporary) ? new FileInfo(temporary).Length : 0;
                    if (existingBytes >= bundle.size)
                    {
                        File.Delete(temporary);
                        existingBytes = 0;
                    }

                    using (var request = UnityWebRequest.Get(bundleUrl))
                    {
                        request.timeout = 120;
                        request.downloadHandler = new DownloadHandlerFile(temporary, existingBytes > 0);
                        if (existingBytes > 0) request.SetRequestHeader("Range", "bytes=" + existingBytes + "-");
                        var operation = request.SendWebRequest();

                        while (!operation.isDone)
                        {
                            var currentPart = existingBytes + (long)(request.downloadProgress * Math.Max(0, bundle.size - existingBytes));
                            var currentTotal = completedBytes + currentPart;
                            var elapsed = Mathf.Max(0.1f, Time.realtimeSinceStartup - startedAt);
                            var speed = currentTotal / (1024f * 1024f) / elapsed;
                            var progress = totalBytes == 0 ? 1f : Mathf.Clamp01((float)currentTotal / totalBytes);
                            OnDownloadProgress.Invoke(progress, $"{currentTotal / (1024 * 1024)} / {totalBytes / (1024 * 1024)} MB ({speed:F1} MB/s)");
                            yield return null;
                        }

                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            isDownloading = false;
                            var message = $"Tải gói {bundle.bundleName} thất bại: {request.error}";
                            OnDownloadFailed.Invoke(message);
                            finished?.Invoke(false, message);
                            yield break;
                        }

                        // Some CDNs ignore Range and return the complete file (200). Restart once
                        // without append so a resumed partial file cannot become corrupted.
                        if (existingBytes > 0 && request.responseCode != 206)
                        {
                            File.Delete(temporary);
                            attempt++;
                            continue;
                        }
                    }

                    if (!File.Exists(temporary) || new FileInfo(temporary).Length != bundle.size ||
                        !string.Equals(ComputeSha256(temporary), bundle.sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(temporary);
                        isDownloading = false;
                        var message = $"Gói {bundle.bundleName} sai kích thước hoặc SHA-256; đã xóa bản tải lỗi.";
                        OnDownloadFailed.Invoke(message);
                        finished?.Invoke(false, message);
                        yield break;
                    }

                    if (File.Exists(destination)) File.Delete(destination);
                    File.Move(temporary, destination);
                    completedBytes += bundle.size;
                    bundleReady = true;
                }

                if (!bundleReady)
                {
                    isDownloading = false;
                    const string rangeError = "Máy chủ không hỗ trợ tiếp tục tải gói tài nguyên.";
                    OnDownloadFailed.Invoke(rangeError);
                    finished?.Invoke(false, rangeError);
                    yield break;
                }
            }

            isDownloading = false;
            PruneObsoleteBundles(manifest);
            const string completeMessage = "Đã tải và xác thực tài nguyên.";
            OnStatusMessage.Invoke(completeMessage);
            OnDownloadProgress.Invoke(1f, "100% Hoàn tất");
            OnDownloadComplete.Invoke();
            finished?.Invoke(true, completeMessage);
        }

        public void LoadBundleAsync(string bundleName, Action<AssetBundle, string> finished)
        {
            StartCoroutine(LoadBundleRoutine(bundleName, finished));
        }

        public void LoadAssetAsync<T>(string bundleName, string assetName, Action<T, string> finished) where T : UnityEngine.Object
        {
            StartCoroutine(LoadAssetRoutine<T>(bundleName, assetName, finished));
        }

        private IEnumerator LoadBundleRoutine(string bundleName, Action<AssetBundle, string> finished)
        {
            if (!IsSafeBundleName(bundleName))
            {
                finished?.Invoke(null, "Tên gói tài nguyên không hợp lệ.");
                yield break;
            }
            if (loadedBundles.TryGetValue(bundleName, out var loaded) && loaded != null)
            {
                finished?.Invoke(loaded, null);
                yield break;
            }

            var path = Path.Combine(localSavePath, bundleName);
            if (!File.Exists(path))
            {
                finished?.Invoke(null, "Chưa có gói tài nguyên " + bundleName + ".");
                yield break;
            }
            var request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            if (request.assetBundle == null)
            {
                finished?.Invoke(null, "Không thể mở gói tài nguyên " + bundleName + ".");
                yield break;
            }
            loadedBundles[bundleName] = request.assetBundle;
            finished?.Invoke(request.assetBundle, null);
        }

        private IEnumerator LoadAssetRoutine<T>(string bundleName, string assetName, Action<T, string> finished) where T : UnityEngine.Object
        {
            AssetBundle bundle = null;
            string error = null;
            yield return LoadBundleRoutine(bundleName, (loaded, message) => { bundle = loaded; error = message; });
            if (bundle == null)
            {
                finished?.Invoke(null, error);
                yield break;
            }
            var request = bundle.LoadAssetAsync<T>(assetName);
            yield return request;
            var asset = request.asset as T;
            finished?.Invoke(asset, asset == null ? "Không tìm thấy tài nguyên " + assetName + "." : null);
        }

        private static bool ValidateManifest(AssetManifest manifest, out string error)
        {
            error = null;
            if (manifest == null || manifest.version < 1 || manifest.bundles == null)
            {
                error = "Manifest thiếu phiên bản hoặc danh sách gói.";
                return false;
            }
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var bundle in manifest.bundles)
            {
                if (bundle == null || !IsSafeBundleName(bundle.bundleName) || bundle.size < 0 ||
                    string.IsNullOrWhiteSpace(bundle.sha256) || bundle.sha256.Length != 64 || !names.Add(bundle.bundleName))
                {
                    error = "Manifest chứa tên gói, kích thước, SHA-256 không hợp lệ hoặc gói trùng tên.";
                    return false;
                }
            }
            return true;
        }

        private static bool IsSafeBundleName(string value) =>
            !string.IsNullOrWhiteSpace(value) && SafeBundleName.IsMatch(value) && value != "." && value != "..";

        private static string BuildUrl(Uri baseUri, string relativePath) =>
            new Uri(baseUri, Uri.EscapeDataString(relativePath)).AbsoluteUri;

        private static string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var hash = sha.ComputeHash(stream);
                var result = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) result.Append(value.ToString("x2"));
                return result.ToString();
            }
        }

        private void PruneObsoleteBundles(AssetManifest manifest)
        {
            var currentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var bundle in manifest.bundles) currentNames.Add(bundle.bundleName);
            foreach (var path in Directory.GetFiles(localSavePath))
            {
                var name = Path.GetFileName(path);
                if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) || !currentNames.Contains(name))
                    File.Delete(path);
            }
        }
    }
}
