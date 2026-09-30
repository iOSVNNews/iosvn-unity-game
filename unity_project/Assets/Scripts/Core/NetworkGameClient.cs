using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace IOSVN.TuTien.Core
{
    [Serializable] public class ApiResult { public bool ok; public string error; public string message; public string accessToken; public long expiresAt; public bool verificationRequired; public string email; public string code; }
    [Serializable] public class ChoiceInfo { public string id; public string name; }
    [Serializable] public class GameCatalog { public ChoiceInfo[] mon; public ChoiceInfo[] he; }
    [Serializable] public class RealmInfo { public int index; public string name; public int sub; public long experience; }
    [Serializable] public class MapInfo { public string id; public string name; public string provinceName; public string desc; public int realmMin; public int realmMax; public bool ascensionRequired; public string realmMinName; public string realmMaxName; public string[] townIds; }
    [Serializable] public class TownInfo { public string id; public string name; public string mapId; public string icon; public string desc; public int realmMin; public string realmMinName; public int x; public int y; public string[] monsterPool; }
    [Serializable] public class DungeonInfo { public string id; public string name; public string icon; public string townId; public int realmMin; public int stamina; public string desc; }
    [Serializable] public class MonsterInfo { public string id; public string name; public string icon; public int realm; public string element; }
    [Serializable] public class BattleMapInfo { public string id; public string name; public string description; public string terrain; public string layout; public string[] palette; public string weather; public string visualThemeId; public bool isActive; }
    [Serializable] public class BattleMapRotation { public string strategy; public int periodSeconds; public long slot; public long nextRotationAt; }
    [Serializable] public class BattleMapMode { public string id; public string name; public string activityType; public BattleMapRotation rotation; public string activeMapId; public BattleMapInfo[] maps; }
    [Serializable] public class BattleMapRealmSet { public string id; public string name; public bool requiresAscension; public int realmMin; public int realmMax; public BattleMapMode[] modes; }
    [Serializable] public class MapCatalog { public MapInfo[] maps; public TownInfo[] towns; public DungeonInfo[] dungeons; public MonsterInfo[] monsters; public BattleMapRealmSet[] battleMapSets; }
    [Serializable] public class PlayerTitle { public string id; public string name; public bool active; public string requirement; public string maintain; public string buff; public int rank; }
    [Serializable] public class WorldMapPosition { public string mapId; public int x; public int y; }
    [Serializable] public class WorldMoveChoice { public string mapId; public int x; public int y; }
    [Serializable] public class PlayerInfo { public string userId; public string name; public string fullName; public string monName; public string heName; public string linhCan; public long stones; public long hp; public long maxHp; public bool ascended; public string appearanceId; public AppearanceColors appearanceColors; public string[] talents; public PlayerTitle[] titles; public WorldMapPosition worldPosition; }
    [Serializable] public class AppearanceColors { public string hair; public string outfit; public string eyes; }
    [Serializable] public class WorldMonster { public string uid; public string monsterId; public string name; public string townId; public long hp; public long maxHp; public bool isBoss; }
    [Serializable] public class GameState { public bool registered; public bool hasItems; public GameCatalog catalog; public RealmInfo realm; public TownInfo town; public TownInfo[] allTowns; public MapInfo[] allMaps; public PlayerInfo player; public WorldMonster[] worldMonsters; public string toast; }
    [Serializable] public class TravelChoice { public string toTownId; }
    [Serializable] public class TravelResult { public string fromTownName; public string toTownName; public int durationSec; public long arriveAt; }
    [Serializable] public class TravelEnvelope { public TravelResult travel; public GameState state; }
    [Serializable] public class MapCatalogEnvelope { public MapCatalog catalog; }
    [Serializable] public class MonsterList { public WorldMonster[] list; }
    [Serializable] public class EmailCredentials { public string email; public string password; }
    [Serializable] public class EmailVerificationChoice { public string email; public string code; }
    [Serializable] public class EmptyPayload { }
    [Serializable] public class RegisterChoice { public string name; public string gender; public string mon; public string he; public string appearance; public string[] talents; }
    [Serializable] public class HuntChoice { public string monsterUid; }
    [Serializable] public class BattlePlayerView { public string name; public long hp; public long maxHp; public long mp; public long maxMp; }
    [Serializable] public class BattleWarning { public long at; public bool stun; public bool all; }
    [Serializable] public class BattleMonsterView { public string name; public string icon; public long hp; public long maxHp; public BattleWarning warn; public int packSize; public int minionCount; }
    [Serializable] public class BattleLogLine { public string text; public long t; }
    [Serializable] public class BattleSkill { public int i; public string id; public string name; public string icon; public string kind; public bool locked; public long readyAt; public long mp; }
    [Serializable] public class BattleView { public string id; public bool over; public string result; public string dungeonLeaderId; public BattleMapInfo battleMap; public BattlePlayerView p; public BattleMonsterView m; public BattleSkill[] skills; public BattleLogLine[] log; }
    [Serializable] public class BattleEnvelope { public BattleView battle; }
    [Serializable] public class BattleAction { public string a; public int i; }
    [Serializable] public class BattleActionOutcome { public bool ok; public string msg; }
    [Serializable] public class BattleActionResult { public BattleActionOutcome result; public BattleView battle; public GameState state; }
    [Serializable] public class DungeonChoice { public string dungeonId; }
    [Serializable] public class DungeonEnvelope { public bool success; public bool completed; public BattleView battle; public string message; }
    [Serializable] public class PvpOpponent { public string userId; public string name; public string fullName; public string realmName; public string town; public string townName; public long power; public int points; public int wins; public int losses; public bool isSameTown; public bool isDemon; }
    [Serializable] public class PvpChallenge { public string id; public string fromId; public string fromName; public string toName; public long challengedAt; }
    [Serializable] public class PvpChallengeGroup { public PvpChallenge[] received; public PvpChallenge[] sent; }
    [Serializable] public class PvpMe { public int points; public int wins; public int losses; public int dailyPvpRemaining; public string townName; }
    [Serializable] public class PvpList { public PvpMe me; public PvpOpponent[] sameTownPlayers; public PvpOpponent[] opponents; public PvpChallengeGroup challenges; }
    [Serializable] public class PvpSide { public string name; public long hp; public long maxHp; public long mp; public long maxMp; public long power; public PvpSkill[] skills; }
    [Serializable] public class PvpSkill { public string id; public string name; public string icon; public string kind; public int mp; public int cdLeft; public bool canUse; }
    [Serializable] public class PvpLogLine { public string text; public long t; }
    [Serializable] public class PvpBattle { public string id; public bool none; public bool over; public bool isWin; public bool myTurn; public int round; public BattleMapInfo battleMap; public PvpSide me; public PvpSide opponent; public PvpLogLine[] log; public string result; }
    [Serializable] public class PvpBattleEnvelope { public PvpBattle battle; }
    [Serializable] public class PvpFightChoice { public string targetId; }
    [Serializable] public class PvpAction { public string battleId; public string act; public string skillId; }
    [Serializable] public class PvpFightResult { public bool success; public PvpBattle battle; public string message; }

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

        public void VerifyEmail(string email, string code, Action<ApiResult> done) =>
            StartCoroutine(PostJson("/auth/email/verify", new EmailVerificationChoice { email = email, code = code }, response =>
            {
                var result = Parse<ApiResult>(response);
                if (response.ok && result != null && !string.IsNullOrEmpty(result.accessToken)) accessToken = result.accessToken;
                done?.Invoke(result ?? new ApiResult { ok = false, error = response.error });
            }, authenticated: false));

        public void ResendEmailVerification(string email, Action<ApiResult> done) =>
            StartCoroutine(PostJson("/auth/email/resend", new EmailCredentials { email = email }, response =>
            {
                var result = Parse<ApiResult>(response);
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

        public void LoadMapCatalog(Action<MapCatalog, string> done) => StartCoroutine(GetJson("/map/catalog", response =>
        {
            var envelope = response.ok ? Parse<MapCatalog>(response) : null;
            done?.Invoke(envelope, response.error);
        }));

        public void TravelTo(string townId, Action<TravelEnvelope, string> done) => StartCoroutine(PostJson("/travel", new TravelChoice { toTownId = townId }, response =>
        {
            done?.Invoke(response.ok ? Parse<TravelEnvelope>(response) : null, response.error);
        }));

        public void SaveWorldPosition(string mapId, int x, int y, Action<bool, string> done) => StartCoroutine(PostJson("/world/move", new WorldMoveChoice { mapId = mapId, x = x, y = y }, response =>
        {
            done?.Invoke(response.ok, response.error);
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

        public void BattleAct(string action, Action<BattleActionResult, string> done) => BattleAct(action, -1, done);

        public void BattleAct(string action, int slot, Action<BattleActionResult, string> done) => StartCoroutine(PostJson("/battle/act", new BattleAction { a = action, i = slot }, response =>
        {
            done?.Invoke(response.ok ? Parse<BattleActionResult>(response) : null, response.error);
        }));

        public void EnterDungeon(string dungeonId, Action<DungeonEnvelope, string> done) => StartCoroutine(PostJson("/dungeon/enter", new DungeonChoice { dungeonId = dungeonId }, response =>
        {
            done?.Invoke(response.ok ? Parse<DungeonEnvelope>(response) : null, response.error);
        }));

        public void NextDungeonStage(Action<DungeonEnvelope, string> done) => StartCoroutine(PostJson("/dungeon/next-stage", new EmptyPayload(), response =>
        {
            done?.Invoke(response.ok ? Parse<DungeonEnvelope>(response) : null, response.error);
        }));

        public void LoadPvp(Action<PvpList, string> done) => StartCoroutine(GetJson("/pvp", response =>
        {
            done?.Invoke(response.ok ? Parse<PvpList>(response) : null, response.error);
        }));

        public void StartPvp(string targetId, Action<PvpFightResult, string> done) => StartCoroutine(PostJson("/pvp/fight", new PvpFightChoice { targetId = targetId }, response =>
        {
            done?.Invoke(response.ok ? Parse<PvpFightResult>(response) : null, response.error);
        }));

        public void LoadPvpBattle(Action<PvpBattle, string> done) => StartCoroutine(GetJson("/pvp/battle", response =>
        {
            var envelope = response.ok ? Parse<PvpBattleEnvelope>(response) : null;
            done?.Invoke(envelope?.battle, response.error);
        }));

        public void PvpAct(string battleId, string action, string skillId, Action<PvpBattle, string> done) => StartCoroutine(PostJson("/pvp/action", new PvpAction { battleId = battleId, act = action, skillId = skillId }, response =>
        {
            var result = response.ok ? Parse<PvpActionResult>(response) : null;
            done?.Invoke(result?.battle, response.error);
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
            if (string.IsNullOrWhiteSpace(response.body)) return null;
            try { return JsonUtility.FromJson<T>(response.body); }
            catch (Exception ex) { Debug.LogWarning("Không đọc được phản hồi game: " + ex.Message); return null; }
        }

        private sealed class Response { public bool ok; public string body; public string error; }
    }

    [Serializable] internal class PvpActionResult { public PvpBattle battle; }
}
