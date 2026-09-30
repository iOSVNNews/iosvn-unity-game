using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace IOSVN.TuTien.Core
{
    [Serializable] public class ApiResult { public bool ok; public string error; public string message; public string accessToken; public long expiresAt; }
    [Serializable] public class ChoiceInfo { public string id; public string name; }
    [Serializable] public class GameCatalog { public ChoiceInfo[] mon; public ChoiceInfo[] he; }
    [Serializable] public class RealmInfo { public int index; public string name; public int sub; public long experience; }
    [Serializable] public class TownInfo { public string id; public string name; public string mapId; }
    [Serializable] public class PlayerInfo { public string userId; public string name; public string fullName; public string monName; public string heName; public string linhCan; public long stones; public long hp; public long maxHp; }
    [Serializable] public class WorldMonster { public string uid; public string monsterId; public string name; public string townId; public long hp; public long maxHp; public bool isBoss; }
    [Serializable] public class GameState { public bool registered; public bool hasItems; public GameCatalog catalog; public RealmInfo realm; public TownInfo town; public PlayerInfo player; public WorldMonster[] worldMonsters; public string toast; }
    [Serializable] public class MonsterList { public WorldMonster[] list; }
    [Serializable] public class EmailCredentials { public string email; public string password; }
    [Serializable] public class EmptyPayload { }
    [Serializable] public class RegisterChoice { public string name; public string gender; public string mon; public string he; }
    [Serializable] public class HuntChoice { public string monsterUid; }
    [Serializable] public class BattlePlayerView { public string name; public long hp; public long maxHp; public long mp; public long maxMp; }
    [Serializable] public class BattleWarning { public long at; public bool stun; public bool all; }
    [Serializable] public class BattleMonsterView { public string name; public string icon; public long hp; public long maxHp; public BattleWarning warn; }
    [Serializable] public class BattleLogLine { public string text; public long t; }
    [Serializable] public class BattleView { public string id; public bool over; public string result; public BattlePlayerView p; public BattleMonsterView m; public BattleLogLine[] log; }
    [Serializable] public class BattleEnvelope { public BattleView battle; }
    [Serializable] public class BattleAction { public string a; }
    [Serializable] public class BattleActionOutcome { public bool ok; public string msg; }
    [Serializable] public class BattleActionResult { public BattleActionOutcome result; public BattleView battle; public GameState state; }

    /// <summary>
    /// Thin client for the dedicated IPA authoritative game API. The game server decides all
    /// combat, rewards, cultivation, inventory and character-save results.
    /// </summary>
    public sealed class NetworkGameClient : MonoBehaviour
    {
        public static NetworkGameClient Instance { get; private set; }
        private const int RequestTimeoutSeconds = 15;
        private string accessToken;
        private string apiBaseUrl;

        public string AccessToken => accessToken;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            apiBaseUrl = Resources.Load<GameServerConfig>("GameServerConfig")?.apiBaseUrl?.Trim().TrimEnd('/');
        }

        public void SetAccessToken(string token) => accessToken = token;

        public void Login(string email, string password, Action<ApiResult> done) =>
            StartCoroutine(PostJson("/auth/email/login", new EmailCredentials { email = email, password = password }, response =>
            {
                var result = Parse<ApiResult>(response);
                if (response.ok && result != null && !string.IsNullOrEmpty(result.accessToken)) accessToken = result.accessToken;
                done?.Invoke(result ?? new ApiResult { ok = false, error = response.error });
            }, authenticated: false));

        public void SignUp(string email, string password, Action<ApiResult> done) =>
            StartCoroutine(PostJson("/auth/email/register", new EmailCredentials { email = email, password = password }, response =>
            {
                var result = Parse<ApiResult>(response);
                if (response.ok && result != null && !string.IsNullOrEmpty(result.accessToken)) accessToken = result.accessToken;
                done?.Invoke(result ?? new ApiResult { ok = false, error = response.error });
            }, authenticated: false));

        public void Logout(Action<ApiResult> done = null)
        {
            StartCoroutine(PostJson("/auth/logout", new EmptyPayload(), response =>
            {
                accessToken = null;
                done?.Invoke(response.ok ? new ApiResult { ok = true } : new ApiResult { ok = false, error = response.error });
            }));
        }

        public void LoadState(Action<GameState, string> done) => StartCoroutine(GetJson("/state", response =>
        {
            done?.Invoke(response.ok ? Parse<GameState>(response) : null, response.error);
        }));

        public void RegisterCharacter(RegisterChoice choice, Action<GameState, string> done) => StartCoroutine(PostJson("/register", choice, response =>
        {
            done?.Invoke(response.ok ? Parse<GameState>(response) : null, response.error);
        }));

        public void LoadWorldMonsters(Action<WorldMonster[], string> done) => StartCoroutine(GetJson("/world/monsters", response =>
        {
            var list = response.ok ? Parse<MonsterList>(response) : null;
            done?.Invoke(list?.list, response.error);
        }));

        public void StartWorldHunt(string monsterUid, Action<bool, string> done) => StartCoroutine(PostJson("/world/hunt", new HuntChoice { monsterUid = monsterUid }, response =>
        {
            done?.Invoke(response.ok, response.error);
        }));

        public void LoadCurrentBattle(Action<BattleView, string> done) => StartCoroutine(GetJson("/battle/current", response =>
        {
            var envelope = response.ok ? Parse<BattleEnvelope>(response) : null;
            done?.Invoke(envelope?.battle, response.error);
        }));

        public void BattleAct(string action, Action<BattleActionResult, string> done) => StartCoroutine(PostJson("/battle/act", new BattleAction { a = action }, response =>
        {
            done?.Invoke(response.ok ? Parse<BattleActionResult>(response) : null, response.error);
        }));

        private IEnumerator GetJson(string route, Action<Response> done)
        {
            if (!HasServer(done)) yield break;
            using (var request = UnityWebRequest.Get(apiBaseUrl + route))
            {
                request.timeout = RequestTimeoutSeconds;
                SetAuth(request);
                yield return request.SendWebRequest();
                done?.Invoke(ToResponse(request));
            }
        }

        private IEnumerator PostJson(string route, object payload, Action<Response> done, bool authenticated = true)
        {
            if (!HasServer(done)) yield break;
            var json = JsonUtility.ToJson(payload);
            using (var request = new UnityWebRequest(apiBaseUrl + route, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = RequestTimeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                if (authenticated) SetAuth(request);
                yield return request.SendWebRequest();
                done?.Invoke(ToResponse(request));
            }
        }

        private void SetAuth(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(accessToken)) request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        }

        private bool HasServer(Action<Response> done)
        {
            if (!string.IsNullOrWhiteSpace(apiBaseUrl) && Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && (uri.IsLoopback || uri.Host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase))))) return true;
            done?.Invoke(new Response { ok = false, error = "Chưa cấu hình địa chỉ server game IPA trong GameServerConfig." });
            return false;
        }

        private static Response ToResponse(UnityWebRequest request)
        {
            var response = new Response
            {
                ok = request.result == UnityWebRequest.Result.Success,
                body = request.downloadHandler?.text ?? string.Empty,
                error = request.result == UnityWebRequest.Result.Success ? string.Empty : FriendlyError(request.downloadHandler?.text)
            };
            if (string.IsNullOrWhiteSpace(response.error)) response.error = request.error;
            return response;
        }

        private static string FriendlyError(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "Không kết nối được máy chủ game.";
            try
            {
                var parsed = JsonUtility.FromJson<ApiResult>(body);
                if (!string.IsNullOrWhiteSpace(parsed?.error)) return parsed.error;
                if (!string.IsNullOrWhiteSpace(parsed?.message)) return parsed.message;
            }
            catch { }
            return body.Length > 180 ? body.Substring(0, 180) : body;
        }

        private static T Parse<T>(Response response) where T : class
        {
            if (!response.ok || string.IsNullOrWhiteSpace(response.body)) return null;
            try { return JsonUtility.FromJson<T>(response.body); }
            catch (Exception ex) { Debug.LogWarning("Không đọc được phản hồi game: " + ex.Message); return null; }
        }

        private sealed class Response { public bool ok; public string body; public string error; }
    }
}
