using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Real-time PvE battle with action-RPG controls: movement stick on the left, attack +
    /// skill cluster and dodge on the right, quick consumables along the bottom. The server
    /// resolves every hit; movement lets the player step out of telegraphed big attacks
    /// (which triggers the server dodge window). Fighters are animated pixel figures: the hero
    /// swings, casts and recoils, the monster's body moves and its aura burns, every skill and
    /// each of the monster's five moves has its own staged effect.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private ActionBattle actionBattle;
        private bool actionBattleFailed;

        /// <summary>Opens the action battle for the current server battle (falls back to the classic view).</summary>
        private void ShowActionBattle(Action<bool> fallback = null)
        {
            ShowBusy(true);
            client.Get("/battle/current", (data, error) =>
            {
                ShowBusy(false);
                var battle = data["battle"];
                if (error != null || !battle.IsObject) { fallback?.Invoke(false); if (error != null) Toast(error, true); return; }
                try { BuildActionBattle(battle); }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    actionBattleFailed = true;
                    fallback?.Invoke(false);
                }
            });
        }

        private void BuildActionBattle(J battle)
        {
            SetAtlasOrientation(false);
            ClearContent();
            ClearBattleScene();
            if (worldView != null) { Destroy(worldView.gameObject); worldView = null; }
            if (cityRoot != null) { Destroy(cityRoot); cityRoot = null; }
            var root = new GameObject("ActionBattle", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            root.SetParent(backgroundRoot, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
            root.GetComponent<Image>().color = new Color32(30, 34, 36, 255);
            actionBattle = root.gameObject.AddComponent<ActionBattle>();
            var hud = HudRoot();
            BuildOverlays();
            var player = hub.IsObject && hub["player"].IsObject ? hub["player"] : (battle.IsObject && battle["p"].IsObject ? battle["p"] : J.Null);
            var mapId = hub.IsObject && hub["town"].IsObject ? hub["town"]["mapId"].Str("map_1") : "map_1";
            var realmIdx = hub.IsObject && hub["realm"].IsObject ? hub["realm"]["index"].Int() : (battle.IsObject && battle["p"].IsObject ? battle["p"]["realmIndex"].Int() : 0);
            Texture painting = null;
            Rect uv = new Rect(0, 0, 1, 1);
            var data = WorldMapData.Load(mapId);
            var battleMap = BattleMapFromJson(battle["battleMap"]);
            if (battleMap != null)
            {
                var immortal = hub.IsObject && hub["player"].IsObject && hub["player"]["ascended"].Bool();
                painting = PixelCombatPresentation.GroundFor(battleMap, immortal);
            }
            else if (data != null)
            {
                painting = GetPainting(mapId);
                var tile = worldReturnTile ?? (Vector2?)null;
                var cx = tile.HasValue ? tile.Value.x : data.w * .5f;
                var cy = tile.HasValue ? tile.Value.y : data.h * .5f;
                var w = 30f / data.w;
                var h = 16.9f / data.h;
                var u = Mathf.Clamp(cx / data.w - w / 2, 0, 1 - w);
                var v = Mathf.Clamp(1f - cy / data.h - h / 2, 0, 1 - h);
                uv = new Rect(u, v, w, h);
            }
            actionBattle.Init(this, client, root, hud, battle, painting, uv, LookOf(player), AvatarComposer.AuraStrength(realmIdx), realmIdx);
        }

        internal static BattleMapInfo BattleMapFromJson(J source)
        {
            if (!source.IsObject || string.IsNullOrEmpty(source["id"].Str())) return null;
            var palette = new List<string>();
            foreach (var color in source["palette"].Items) palette.Add(color.Str());
            return new BattleMapInfo
            {
                id = source["id"].Str(),
                name = source["name"].Str(),
                description = source["description"].Str(),
                terrain = source["terrain"].Str(),
                layout = source["layout"].Str(),
                palette = palette.ToArray(),
                weather = source["weather"].Str(),
                visualThemeId = source["visualThemeId"].Str(),
                isActive = source["isActive"].Bool(),
            };
        }

        // ---- callbacks used by the battle component (keeps the partial's private helpers in reach)

        internal Sprite BattleSkillIcon(J skill) => BattleInkIcons.Skill(skill["name"].Str(), BattleFx.ElementOfSkill(skill["name"].Str(), skill["element"].Str("kim")));
        internal Sprite BattleItemIcon(J item) => ArtSprites.Item(item["id"].Str()) ?? BattleInkIcons.Potion;
        internal Sprite BattleMonsterSprite(string id) => ArtSprites.Monster(id) ?? ArtSprites.Monster("da_lang");
        internal Sprite BattleUiIcon(string id) => BattleInkIcons.Ui(id);
        internal void BattleToast(string message, bool error) => Toast(message, error);
        internal RectTransform BattleAnchored(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax) => Anchored(name, parent, min, max, offMin, offMax);
        internal Text BattleText(Transform parent, string name, string value, Font font, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
            => AnchoredText(parent, name, value, font, size, color, anchor, min, max, offMin, offMax);
        internal string BattleClean(string value) => Clean(value);

        internal void BattleFinished(J battle)
        {
            actionBattle = null;
            var won = battle["result"].Str().StartsWith("win");
            if (won && !string.IsNullOrEmpty(battle["dungeonLeaderId"].Str()))
            {
                ShowBusy(true);
                client.Post("/dungeon/next-stage", Body(), (result, error) =>
                {
                    ShowBusy(false);
                    if (error != null) { Toast(error, true); RefreshHub(SafeShowWorld); return; }
                    if (result["completed"].Bool()) { Toast("Bí cảnh đã được chinh phục!"); RefreshHub(SafeShowWorld); return; }
                    if (result["battle"].IsObject) BuildActionBattle(result["battle"]);
                    else RefreshHub(SafeShowWorld);
                });
                return;
            }
            RefreshHub(SafeShowWorld);
        }

        internal J HubPlayer() => hub["player"];

        internal void BattleRefreshHub(J state)
        {
            if (state.IsObject) AcceptState(state);
        }
    }

    /// <summary>The action battle scene and its controls.</summary>
    internal sealed class ActionBattle : MonoBehaviour
    {
        private PrototypeBootstrap owner;
        private NetworkGameClient client;
        private RectTransform arena;
        private RectTransform hud;
        private J battle;
        private double serverOffset;          // server ms - local ms
        private bool over;
        private bool applied;
        private float nextPoll;
        private bool attackHeld;
        private float nextAttackLocalTime;
        private bool attackInFlight;
        private float dodgeCooldownUntil;
        private readonly Queue<double> pendingDamageQueue = new Queue<double>();

        private FighterView hero;
        private Vector2 playerPos = new Vector2(260, -120);
        private Vector2 moveInput;
        private float playerLungeUntil, playerDashUntil;
        private double lastPlayerHp = -1;

        private MonsterView monster;
        private Vector2 monsterPos = new Vector2(-300, -60);
        private float monsterLungeUntil, monsterLungeTime = .35f;
        private readonly List<MonsterView> minions = new List<MonsterView>();
        private readonly List<Vector2> minionPositions = new List<Vector2>();
        private readonly List<float> minionRushStarts = new List<float>();
        private readonly List<float> minionRushEnds = new List<float>();
        private readonly List<RectTransform> fighterDepthOrder = new List<RectTransform>();
        private int lastMoveSeq = -1;
        private string element = "kim";

        private Image monsterHp, monsterHpTrail, playerHp, playerMp;
        private Text monsterHpText, playerHpText, playerMpText, logText, monsterName, movesText, warnLabel, comboText;
        private double lastStamp;
        private Vector2 lastFloat;
        private float lastFloatTime = -9f;
        private RectTransform warnCircle;
        private Vector2 warnCenter;
        private bool warnActive;
        private bool warnDodged;
        private const float WarnRadius = 230f;
        private int combo;
        private float comboUntil;

        private readonly List<(RectTransform rect, Image cooldown, Text seconds, Image icon, int index)> skillButtons = new List<(RectTransform, Image, Text, Image, int)>();
        private readonly List<(RectTransform rect, Text qty, int index)> itemButtons = new List<(RectTransform, Text, int)>();
        private Image attackCooldown;
        private Image dodgeCooldown;
        private RectTransform resultPanel;
        private Vector2 shakeOffset;
        private float shakeUntil, shakeAmp;
        private Image dimImage;
        private float dimFrom, dimUntil;
        private RectTransform worldLayer, backLayer, fighterLayer, fxLayer;
        private Vector2 cameraOffset, cameraVelocity;
        private const float ArenaWorldScale = 1.35f;
        private static readonly Color Cream = new Color32(244, 236, 220, 255);
        private static readonly Color Gold = new Color32(232, 196, 120, 255);

        private double ServerNow => (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + serverOffset);

        public void Init(PrototypeBootstrap owner, NetworkGameClient client, RectTransform root, RectTransform hud, J battle, Texture painting, Rect uv, LookSpec look, float auraStrength, int realmIndex)
        {
            this.owner = owner;
            this.client = client;
            this.hud = hud;
            arena = root;
            element = battle["p"]["element"].Str("kim");
            // The battlefield is a real world larger than the phone viewport. The phone
            // camera shows a section of it and pans as the player moves toward the edge.
            worldLayer = new GameObject("Battlefield", typeof(RectTransform)).GetComponent<RectTransform>();
            worldLayer.SetParent(root, false);
            worldLayer.anchorMin = worldLayer.anchorMax = new Vector2(.5f, .5f);
            var groundAspect = painting == null ? root.rect.width / root.rect.height
                : painting.width * uv.width / (painting.height * uv.height);
            var worldHeight = Mathf.Max(root.rect.height, root.rect.width / groundAspect) * ArenaWorldScale;
            worldLayer.sizeDelta = new Vector2(worldHeight * groundAspect, worldHeight);
            worldLayer.anchoredPosition = Vector2.zero;
            worldLayer.localScale = Vector3.one;
            // The painted ground spans the whole large world, including its distant perimeter.
            if (painting != null)
            {
                var bg = new GameObject("Scenery", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                bg.transform.SetParent(worldLayer, false);
                var r = bg.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
                bg.texture = painting;
                bg.uvRect = uv;
                bg.raycastTarget = false;
            }
            var shade = InkUi.Simple(root, "Vignette", InkUi.Cloud, new Color(.05f, .06f, .08f, .12f), Vector2.zero);
            shade.rectTransform.anchorMin = new Vector2(-.3f, -.4f); shade.rectTransform.anchorMax = new Vector2(1.3f, 1.4f);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            var tint = InkUi.Simple(root, "Tint", InkUi.White, new Color(0, 0, 0, .06f), Vector2.zero);
            tint.rectTransform.anchorMin = Vector2.zero; tint.rectTransform.anchorMax = Vector2.one;
            tint.rectTransform.offsetMin = tint.rectTransform.offsetMax = Vector2.zero;
            // great techniques darken the field under the fighters
            dimImage = InkUi.Simple(root, "Dim", InkUi.White, new Color(.02f, .01f, .05f, 0f), Vector2.zero);
            dimImage.rectTransform.anchorMin = Vector2.zero; dimImage.rectTransform.anchorMax = Vector2.one;
            dimImage.rectTransform.offsetMin = dimImage.rectTransform.offsetMax = Vector2.zero;
            // telegraph circle
            warnCircle = InkUi.Simple(root, "Telegraph", InkUi.Glow, new Color(1f, .15f, .1f, 0f), new Vector2(WarnRadius * 2, WarnRadius * 1.1f)).rectTransform;
            warnCircle.anchorMin = warnCircle.anchorMax = new Vector2(.5f, .5f);
            var ring = InkUi.Simple(warnCircle, "Ring", InkUi.Ring, new Color(1f, .3f, .2f, .9f), Vector2.zero);
            ring.rectTransform.anchorMin = Vector2.zero; ring.rectTransform.anchorMax = Vector2.one;
            ring.rectTransform.offsetMin = ring.rectTransform.offsetMax = Vector2.zero;
            warnCircle.gameObject.SetActive(false);
            warnCircle.SetParent(worldLayer, false);
            // draw order: effects behind the fighters, the fighters, effects and numbers over them
            backLayer = BattleFx.Layer(worldLayer, "BackEffects");
            fighterLayer = BattleFx.Layer(worldLayer, "Fighters");
            fxLayer = BattleFx.Layer(worldLayer, "Effects");
            BuildFighters(battle, look, auraStrength);
            BuildHud(battle);
            Apply(battle);
            nextPoll = Time.time + .6f;
            var m = battle["m"];
            var kind = battle["kind"].Str();
            var boss = kind == "boss" || kind == "worldBoss" || m["packSize"].Int(1) > 1;
            var monsterRealm = m["realm"].Int(m["realmIndex"].Int(-1));
            if (boss || monsterRealm > realmIndex) StartCoroutine(PressureIntro(owner.BattleClean(m["realmName"].Str()), owner.BattleClean(m["name"].Str())));
        }

        /// <summary>Uy áp: a powerful foe's pressure darkens the field, shakes it and announces its realm.</summary>
        private System.Collections.IEnumerator PressureIntro(string realm, string name)
        {
            var veil = InkUi.Simple(arena, "Pressure", InkUi.Vignette, new Color(.25f, 0f, .03f, 0f), Vector2.zero);
            veil.rectTransform.anchorMin = Vector2.zero; veil.rectTransform.anchorMax = Vector2.one;
            veil.rectTransform.offsetMin = veil.rectTransform.offsetMax = Vector2.zero;
            var title = owner.BattleText(hud, "UyAp", "UY ÁP", ModernUi.Display, 96, new Color32(255, 214, 160, 0), TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-600, 20), new Vector2(600, 160));
            title.gameObject.AddComponent<Outline>().effectColor = new Color(.3f, 0, 0, .9f);
            var sub = owner.BattleText(hud, "UyApSub", name + (string.IsNullOrEmpty(realm) ? "" : "  ·  " + realm), ModernUi.SemiBold, 34, new Color32(244, 236, 220, 0), TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-600, -40), new Vector2(600, 20));
            sub.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            var ring = InkUi.Simple(worldLayer != null ? worldLayer : arena, "Shock", InkUi.Ring, new Color(1f, .35f, .25f, .9f), new Vector2(200, 90));
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(.5f, .5f);
            ring.rectTransform.anchoredPosition = monsterPos;
            var t = 0f;
            while (t < 1.8f && this != null)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / .25f) * Mathf.Clamp01((1.8f - t) / .5f);
                veil.color = new Color(.25f, 0f, .03f, .7f * k);
                title.color = new Color(1f, .84f, .63f, k);
                title.rectTransform.localScale = Vector3.one * (1.25f - .25f * Mathf.Clamp01(t / .3f));
                sub.color = new Color(.96f, .93f, .86f, k);
                var u = Mathf.Clamp01(t / 1.1f);
                ring.rectTransform.localScale = Vector3.one * (.3f + u * 7f);
                ring.color = new Color(1f, .35f, .25f, (1 - u) * .9f);
                if (t < .8f) Shake(1f - t / .8f);
                yield return null;
            }
            if (veil != null) Destroy(veil.gameObject);
            if (title != null) Destroy(title.gameObject);
            if (sub != null) Destroy(sub.gameObject);
            if (ring != null) Destroy(ring.gameObject);
        }

        // ------------------------------------------------------------------ scene

        private void BuildFighters(J b, LookSpec look, float auraStrength)
        {
            var m = b["m"];
            var id = m["id"].Str();
            var boss = m["packSize"].Int(1) > 1 || b["kind"].Str() == "boss" || b["kind"].Str() == "worldBoss";
            var still = owner.BattleMonsterSprite(id);
            var minionCount = Mathf.Clamp(m["minionCount"].Int(), 0, 4);
            var towardPlayer = (playerPos - monsterPos).normalized;
            var cross = new Vector2(-towardPlayer.y, towardPlayer.x);
            for (var i = 0; i < minionCount; i++)
            {
                var minion = MonsterView.Create(fighterLayer, "Minion" + i, id, still, new Vector2(64, 64));
                minions.Add(minion);
                var progress = (i + 1f) / (minionCount + 1f);
                var lane = (i - (minionCount - 1) * .5f) * 80f;
                var initial = Vector2.Lerp(monsterPos, playerPos, progress) + cross * lane;
                minionPositions.Add(initial);
                minion.Rect.anchoredPosition = initial;
                minionRushStarts.Add(float.PositiveInfinity);
                minionRushEnds.Add(float.NegativeInfinity);
            }
            var size = boss ? 124f : m["small"].Bool() ? 76f : 96f;
            monster = MonsterView.Create(fighterLayer, "Monster", id, still, new Vector2(size, size), m["element"].Str("kim"));
            hero = FighterView.Create(fighterLayer, "Player", look, FighterView.BattleScale, auraStrength);
            fighterDepthOrder.Clear();
            foreach (var minion in minions) fighterDepthOrder.Add(minion.Rect);
            fighterDepthOrder.Add(monster.Rect);
            fighterDepthOrder.Add(hero.Rect);
        }

        private StageContext HeroStage() => new StageContext
        {
            Host = this, Arena = fxLayer, Caster = () => playerPos, Target = () => monsterPos,
            CasterHeight = hero.Height * .8f, TargetHeight = monster.Height * .8f, Element = element, Shake = Shake, Dim = Dim, Back = backLayer,
        };

        private StageContext MonsterStage() => new StageContext
        {
            Host = this, Arena = fxLayer, Caster = () => monsterPos, Target = () => playerPos,
            CasterHeight = monster.Height * .8f, TargetHeight = hero.Height * .8f, Element = battle["m"]["element"].Str("kim"), Shake = Shake, Dim = Dim, Back = backLayer,
        };

        private void Dim(float seconds)
        {
            dimFrom = Time.unscaledTime;
            dimUntil = Mathf.Max(dimUntil, Time.unscaledTime + seconds);
        }

        private void Shake(float strength)
        {
            shakeAmp = Mathf.Max(shakeAmp, 26f * Mathf.Clamp01(strength));
            shakeUntil = Time.unscaledTime + .3f;
        }

        // ------------------------------------------------------------------ HUD

        private void BuildHud(J b)
        {
            var A = (Func<string, Transform, Vector2, Vector2, Vector2, Vector2, RectTransform>)owner.BattleAnchored;
            // Keep the opponent status compact and centered at the top of the screen.
            var enemy = A("EnemyBar", hud, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-430, -88), new Vector2(430, -20));
            monsterName = owner.BattleText(enemy, "Name", "", ModernUi.SemiBold, 21, Cream, TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(16, 36), new Vector2(-16, -2));
            monsterName.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
            monsterHp = Bar(enemy, new Vector2(0, 4), new Vector2(0, 36), new Color32(206, 58, 48, 255), out monsterHpText, out monsterHpTrail);
            movesText = owner.BattleText(hud, "Moves", "", ModernUi.Regular, 19, new Color32(236, 214, 170, 255), TextAnchor.UpperCenter,
                new Vector2(.12f, 1), new Vector2(.88f, 1), new Vector2(0, -162), new Vector2(0, -130));
            movesText.supportRichText = true;
            movesText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            movesText.gameObject.SetActive(false);
            // Player status sits at the bottom centre as in Quỷ Cốc: name, a long framed health bar, a slimmer
            // energy bar and the five quick item slots right under them.
            var me = A("MeBar", hud, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-310, 96), new Vector2(310, 200));
            var nameText = owner.BattleText(me, "Name", owner.BattleClean(b["p"]["name"].Str("Đạo hữu")), ModernUi.SemiBold, 19, Gold, TextAnchor.LowerLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -30), new Vector2(-26, 0));
            nameText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
            playerHp = Bar(me, new Vector2(0, 32), new Vector2(0, 68), new Color32(204, 58, 50, 255), out playerHpText, out _);
            playerMp = Bar(me, new Vector2(16, 4), new Vector2(-16, 30), new Color32(70, 142, 220, 255), out playerMpText, out _);
            logText = owner.BattleText(hud, "Log", "", ModernUi.Regular, 13, Cream, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -190), new Vector2(318, -108));
            logText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
            comboText = owner.BattleText(hud, "Combo", "", ModernUi.Bold, 24, new Color32(255, 214, 110, 255), TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -228), new Vector2(260, -190));
            // the pop scales from the left edge, so the number never leaves the screen
            comboText.rectTransform.pivot = new Vector2(0, .5f);
            comboText.rectTransform.anchoredPosition = new Vector2(24, -209);
            comboText.gameObject.AddComponent<Outline>().effectColor = new Color(.35f, .08f, 0, .9f);
            warnLabel = owner.BattleText(hud, "Warn", "", ModernUi.Bold, 36, new Color32(255, 120, 96, 255), TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-640, 150), new Vector2(640, 214));
            warnLabel.gameObject.AddComponent<Outline>().effectColor = new Color(.25f, 0, 0, .95f);
            warnLabel.gameObject.SetActive(false);
            // flee (top right)
            var flee = A("Flee", hud, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-122, -122), new Vector2(-34, -34));
            RoundButton(flee, "road", "Rút lui", new Color32(26, 30, 32, 210), () => Send("flee", -1), 15, null, BattleHudArt.SkillRing);
            // joystick (bottom left): painted bronze ring on a soft dark disc, jade knob
            var stickBase = A("Stick", hud, Vector2.zero, Vector2.zero, new Vector2(44, 34), new Vector2(244, 234));
            var baseImage = stickBase.gameObject.AddComponent<Image>();
            baseImage.sprite = InkUi.Glow;
            baseImage.color = new Color(0, 0, 0, .28f);
            var ringSprite = BattleHudArt.StickBase;
            var baseRing = InkUi.Simple(stickBase, "Ring", ringSprite != null ? ringSprite : InkUi.Ring, ringSprite != null ? new Color(1, 1, 1, .92f) : new Color(1, 1, 1, .55f), Vector2.zero);
            baseRing.rectTransform.anchorMin = new Vector2(-.04f, -.04f); baseRing.rectTransform.anchorMax = new Vector2(1.04f, 1.04f);
            baseRing.rectTransform.offsetMin = baseRing.rectTransform.offsetMax = Vector2.zero;
            baseRing.raycastTarget = false;
            var knobSprite = BattleHudArt.StickKnob;
            var knob = InkUi.Simple(stickBase, "Knob", knobSprite != null ? knobSprite : InkUi.Glow, knobSprite != null ? Color.white : (Color)new Color32(232, 214, 170, 230), new Vector2(88, 88));
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(.5f, .5f);
            knob.raycastTarget = false;
            var stick = stickBase.gameObject.AddComponent<BattleStick>();
            stick.Knob = knob.rectTransform;
            stick.OnMove = v => moveInput = v;
            // attack + skills (bottom right): slim painted rings, the icon fills the opening, a small name underneath
            var attack = A("Attack", hud, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-176, 54), new Vector2(-60, 170));
            RoundButton(attack, "swords", null, new Color32(70, 34, 30, 235), TryAttack, 18, null, BattleHudArt.AttackRing);
            attackCooldown = Cooldown(attack);
            var hold = attack.gameObject.AddComponent<BattleHold>();
            hold.OnDown = () => { attackHeld = true; TryAttack(); };
            hold.OnUp = () => attackHeld = false;
            var skillCenters = new[]
            {
                // an inner arc of three around the attack button and two further out, ~120 units apart
                new Vector2(-297f, 128f), new Vector2(-245f, 239f), new Vector2(-134f, 291f),
                new Vector2(-228f, 372f), new Vector2(-366f, 256f)
            };
            var skills = b["skills"];
            for (var i = 0; i < 5; i++)
            {
                var pos = skillCenters[i];
                var rect = A("Skill" + i, hud, new Vector2(1, 0), new Vector2(1, 0), pos - new Vector2(40, 40), pos + new Vector2(40, 40));
                var skill = skills[i];
                var index = i;
                var locked = skill["locked"].Bool() || string.IsNullOrEmpty(skill["id"].Str());
                var skillElement = BattleFx.ElementOfSkill(skill["name"].Str(), element);
                RoundButton(rect, null, null, new Color32(20, 26, 30, 230), locked ? (Action)null : () => PerformSkill(index), 18,
                    locked ? (Color?)new Color(.7f, .7f, .7f, .7f) : Color.Lerp(BattleFx.ElementColor(skillElement), Color.white, .55f), BattleHudArt.SkillRing);
                var icon = InkUi.Simple(rect, "Icon", locked ? owner.BattleUiIcon("lock") : owner.BattleSkillIcon(skill), locked ? new Color(1, 1, 1, .4f) : Color.white, Vector2.zero);
                icon.rectTransform.anchorMin = new Vector2(.2f, .2f); icon.rectTransform.anchorMax = new Vector2(.8f, .8f);
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.transform.SetSiblingIndex(1);
                var label = owner.BattleText(rect, "Name", locked ? "" : owner.BattleClean(skill["name"].Str()) + (skill["mp"].Int() > 0 ? "  <color=#8cc8ff>" + skill["mp"].Int() + "</color>" : ""),
                    ModernUi.SemiBold, 12, Cream, TextAnchor.UpperCenter, new Vector2(-.6f, 0), new Vector2(1.6f, 0), new Vector2(0, -34), new Vector2(0, -14));
                label.supportRichText = true;
                label.raycastTarget = false;
                label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
                var cd = Cooldown(rect);
                var seconds = owner.BattleText(rect, "Seconds", "", ModernUi.Bold, 30, Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                seconds.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
                seconds.raycastTarget = false;
                skillButtons.Add((rect, cd, seconds, icon, i));
            }
            // dodge sits low between the attack button and the skills
            var dpos = new Vector2(-232f, 44f);
            var dodge = A("Dodge", hud, new Vector2(1, 0), new Vector2(1, 0), dpos - new Vector2(30, 30), dpos + new Vector2(30, 30));
            RoundButton(dodge, "spd", null, new Color32(30, 56, 72, 230), PerformDodge, 14, null, BattleHudArt.SkillRing);
            dodgeCooldown = Cooldown(dodge);
            // five quick item slots under the status bars (empty slots stay as frames)
            for (var i = 0; i < 5; i++)
            {
                var x = (i - 2f) * 80f;
                var rect = A("Item" + i, hud, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(x - 34, 14), new Vector2(x + 34, 82));
                var index = i;
                var fill = rect.gameObject.AddComponent<Image>();
                fill.sprite = InkUi.White;
                fill.color = new Color32(14, 18, 20, 200);
                var slotSprite = BattleHudArt.ItemSlot;
                if (slotSprite != null)
                {
                    var frame = InkUi.Simple(rect, "Frame", slotSprite, Color.white, Vector2.zero);
                    frame.rectTransform.anchorMin = new Vector2(-.04f, -.04f); frame.rectTransform.anchorMax = new Vector2(1.04f, 1.04f);
                    frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
                    frame.raycastTarget = false;
                    // the dark bed sits inside the frame's opening; the slot itself stays an invisible hit area
                    fill.color = new Color(0, 0, 0, 0);
                    var bed = InkUi.Simple(rect, "Bed", InkUi.White, new Color32(14, 18, 20, 210), Vector2.zero);
                    bed.rectTransform.anchorMin = new Vector2(.08f, .08f); bed.rectTransform.anchorMax = new Vector2(.92f, .92f);
                    bed.rectTransform.offsetMin = bed.rectTransform.offsetMax = Vector2.zero;
                    bed.raycastTarget = false;
                    bed.transform.SetAsFirstSibling();
                }
                else ModernUi.Fill(fill, 14f);
                var icon = InkUi.Simple(rect, "Icon", InkUi.White, new Color(1, 1, 1, 0), Vector2.zero);
                icon.rectTransform.anchorMin = new Vector2(.17f, .17f); icon.rectTransform.anchorMax = new Vector2(.83f, .83f);
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var key = owner.BattleText(rect, "Key", (i + 1).ToString(), ModernUi.SemiBold, 12, new Color32(236, 214, 170, 200), TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(9, 0), new Vector2(0, -6));
                key.raycastTarget = false;
                var qty = owner.BattleText(rect, "Qty", "", ModernUi.Bold, 17, Cream, TextAnchor.LowerRight, Vector2.zero, Vector2.one, new Vector2(4, 6), new Vector2(-9, -4));
                qty.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
                qty.raycastTarget = false;
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.onClick.AddListener(() => PerformItem(index));
                rect.gameObject.AddComponent<UiPressScale>();
                itemButtons.Add((rect, qty, i));
            }
        }

        private Image Bar(RectTransform parent, Vector2 offMin, Vector2 offMax, Color color, out Text text, out Image trail)
        {
            var track = owner.BattleAnchored("Bar", parent, new Vector2(0, 0), new Vector2(1, 0), offMin, offMax);
            var frameSprite = BattleHudArt.BarFrame;
            var h = Mathf.Max(8f, offMax.y - offMin.y);
            // the painted frame's end caps are ~88 texels wide and its rim ~24 texels: inset the fill to its opening
            var k = BattleHudArt.BarTexHeight / h;
            var inX = frameSprite != null ? 90f / k : 2f;
            var inY = frameSprite != null ? 25f / k : 2f;
            var bed = owner.BattleAnchored("Bed", track, Vector2.zero, Vector2.one, new Vector2(inX - 2, inY - 1), new Vector2(-inX + 2, -inY + 1)).gameObject.AddComponent<Image>();
            bed.sprite = InkUi.White;
            bed.color = new Color(.03f, .04f, .05f, .82f);
            bed.raycastTarget = false;
            // the pale trail shows how much the last blows took before it drains away
            trail = owner.BattleAnchored("Trail", track, Vector2.zero, Vector2.one, new Vector2(inX, inY), new Vector2(-inX, -inY)).gameObject.AddComponent<Image>();
            trail.sprite = InkUi.White;
            trail.type = Image.Type.Filled;
            trail.fillMethod = Image.FillMethod.Horizontal;
            trail.color = new Color(1f, .92f, .78f, .85f);
            trail.raycastTarget = false;
            var fill = owner.BattleAnchored("Fill", track, Vector2.zero, Vector2.one, new Vector2(inX, inY), new Vector2(-inX, -inY)).gameObject.AddComponent<Image>();
            fill.sprite = InkUi.White;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = color;
            fill.raycastTarget = false;
            // a soft highlight along the top half of the fill
            var sheen = owner.BattleAnchored("Sheen", fill.rectTransform, new Vector2(0, .55f), Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            sheen.sprite = InkUi.White;
            sheen.color = new Color(1, 1, 1, .16f);
            sheen.raycastTarget = false;
            if (frameSprite != null)
            {
                var frame = owner.BattleAnchored("Frame", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                frame.sprite = frameSprite;
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = k;
                frame.raycastTarget = false;
            }
            else
            {
                var t = track.gameObject.AddComponent<Image>();
                ModernUi.Fill(t, 12f);
                t.color = new Color(0, 0, 0, .6f);
                t.raycastTarget = false;
                t.transform.SetAsFirstSibling();
            }
            text = owner.BattleText(track, "Text", "", ModernUi.SemiBold, Mathf.Clamp(Mathf.RoundToInt(h * .5f), 12, 18), Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
            return fill;
        }

        private void RoundButton(RectTransform rect, string iconId, string label, Color color, Action click, int fontSize = 20, Color? ringColor = null, Sprite ringSprite = null)
        {
            var disc = rect.gameObject.AddComponent<Image>();
            disc.sprite = InkUi.Glow;
            disc.color = color;
            if (ringSprite != null)
            {
                // the painted ring is wider than the button: its opening (half the sprite) frames the button face
                var glow = InkUi.Simple(rect, "Tint", InkUi.Glow, ringColor.HasValue ? new Color(ringColor.Value.r, ringColor.Value.g, ringColor.Value.b, .22f) : new Color(1, .9f, .6f, .12f), Vector2.zero);
                glow.rectTransform.anchorMin = new Vector2(.08f, .08f); glow.rectTransform.anchorMax = new Vector2(.92f, .92f);
                glow.rectTransform.offsetMin = glow.rectTransform.offsetMax = Vector2.zero;
                glow.raycastTarget = false;
                var painted = InkUi.Simple(rect, "Ring", ringSprite, ringColor.HasValue && ringColor.Value.a < .8f ? new Color(.75f, .75f, .75f, 1) : Color.white, Vector2.zero);
                painted.rectTransform.anchorMin = new Vector2(-.2f, -.2f); painted.rectTransform.anchorMax = new Vector2(1.2f, 1.2f);
                painted.rectTransform.offsetMin = painted.rectTransform.offsetMax = Vector2.zero;
                painted.raycastTarget = false;
            }
            else
            {
                var ring = InkUi.Simple(rect, "Ring", InkUi.Ring, ringColor ?? (Color)new Color32(232, 196, 120, 200), Vector2.zero);
                ring.rectTransform.anchorMin = new Vector2(.04f, .04f); ring.rectTransform.anchorMax = new Vector2(.96f, .96f);
                ring.rectTransform.offsetMin = ring.rectTransform.offsetMax = Vector2.zero;
            }
            if (!string.IsNullOrEmpty(iconId))
            {
                var icon = InkUi.Simple(rect, "Icon", owner.BattleUiIcon(iconId), Color.white, Vector2.zero);
                if (ringSprite != null) { icon.rectTransform.anchorMin = new Vector2(.22f, .22f); icon.rectTransform.anchorMax = new Vector2(.78f, .78f); }
                else { icon.rectTransform.anchorMin = new Vector2(.28f, .34f); icon.rectTransform.anchorMax = new Vector2(.72f, .8f); }
                icon.raycastTarget = false;
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
            }
            if (!string.IsNullOrEmpty(label))
            {
                var text = ringSprite != null
                    ? owner.BattleText(rect, "Label", label, ModernUi.SemiBold, fontSize, Cream, TextAnchor.UpperCenter, new Vector2(-.6f, 0), new Vector2(1.6f, 0), new Vector2(0, -34), new Vector2(0, -14))
                    : owner.BattleText(rect, "Label", label, ModernUi.Bold, fontSize, Cream, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, new Vector2(0, 14), Vector2.zero);
                text.raycastTarget = false;
                text.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            }
            if (click != null)
            {
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = disc;
                button.onClick.AddListener(() => click());
            }
            rect.gameObject.AddComponent<UiPressScale>();
        }

        private Image Cooldown(RectTransform rect)
        {
            var cd = InkUi.Simple(rect, "Cooldown", InkUi.Glow, new Color(0, 0, 0, .65f), Vector2.zero);
            cd.rectTransform.anchorMin = Vector2.zero; cd.rectTransform.anchorMax = Vector2.one;
            cd.rectTransform.offsetMin = cd.rectTransform.offsetMax = Vector2.zero;
            cd.type = Image.Type.Filled;
            cd.fillMethod = Image.FillMethod.Radial360;
            cd.fillOrigin = (int)Image.Origin360.Top;
            cd.fillClockwise = false;
            cd.fillAmount = 0;
            return cd;
        }

        // ------------------------------------------------------------------ state

        private void Apply(J b)
        {
            // A poll that left before the last action can be answered after it. Never step back in time:
            // the bars would jump backwards and the same blow would be staged twice.
            var stamp = b["now"].Num();
            var sameFight = !battle.IsNull && battle["id"].Str() == b["id"].Str();
            if (sameFight && stamp > 0 && stamp < lastStamp) return;
            lastStamp = stamp;
            battle = b;
            serverOffset = b["now"].Num(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var p = b["p"];
            var m = b["m"];
            monsterName.text = owner.BattleClean(m["name"].Str()) + "  ·  " + owner.BattleClean(m["realmName"].Str());
            SetBar(monsterHp, monsterHpText, m["hp"].Num(), m["maxHp"].Num(1));
            SetBar(playerHp, playerHpText, p["hp"].Num(), p["maxHp"].Num(1));
            SetBar(playerMp, playerMpText, p["mp"].Num(), p["maxMp"].Num(1));
            // the monster's move since the last update, staged with its own effect
            var move = m["move"];
            var staged = 0f;
            var bigMove = false;
            if (move.IsObject && move["seq"].Int() != lastMoveSeq)
            {
                var first = lastMoveSeq < 0 && !applied;
                lastMoveSeq = move["seq"].Int();
                if (!first && !over)
                {
                    bigMove = move["big"].Bool();
                    staged = StageMonsterMove(move);
                }
            }
            var hp = p["hp"].Num();
            if (lastPlayerHp >= 0 && hp < lastPlayerHp - .5)
            {
                var lost = lastPlayerHp - hp;
                var kick = bigMove ? .9f : .3f;
                void Land()
                {
                    if (this == null || hero == null) return;
                    hero.Play(FighterAction.Hurt);
                    Float(playerPos + new Vector2(0, hero.Height * .86f), "−" + Vn(lost), new Color32(255, 110, 90, 255), bigMove ? 44 : 34);
                    Shake(kick);
                }
                if (staged > 0f) BattleFx.After(this, staged, Land); else Land();
            }
            lastPlayerHp = hp;
            var log = b["log"];
            logText.text = log.Count > 0 ? owner.BattleClean(log[log.Count - 1]["text"].Str(log[log.Count - 1].Str())) : "";
            var items = b["items"];
            foreach (var (rect, qty, index) in itemButtons)
            {
                var item = items[index];
                var icon = rect.Find("Icon").GetComponent<Image>();
                var has = !string.IsNullOrEmpty(item["uid"].Str());
                icon.sprite = has ? owner.BattleItemIcon(LookupItem(item)) : owner.BattleUiIcon("quick_slot");
                icon.color = has ? Color.white : new Color(1, 1, 1, .35f);
                qty.text = has && item["qty"].Int(1) > 1 ? item["qty"].Int().ToString() : "";
            }
            var warn = m["warn"];
            if (warn.IsObject && !warnActive)
            {
                warnActive = true;
                warnDodged = false;
                warnCenter = playerPos;
                warnCircle.gameObject.SetActive(true);
                var name = owner.BattleClean(warn["name"].Str());
                warnLabel.text = string.IsNullOrEmpty(name) ? "!! CHIÊU LỚN — RỜI KHỎI VÒNG ĐỎ !!" : "!! " + name.ToUpperInvariant() + " !!";
                warnLabel.gameObject.SetActive(true);
            }
            else if (!warn.IsObject && warnActive)
            {
                warnActive = false;
                warnCircle.gameObject.SetActive(false);
                warnLabel.gameObject.SetActive(false);
            }
            applied = true;
            if (b["over"].Bool() && !over) Finish(b);
        }

        /// <summary>The monster lunges, its move is named above it and staged; returns when the blow lands.</summary>
        private float StageMonsterMove(J move)
        {
            var big = move["big"].Bool();
            var name = owner.BattleClean(move["name"].Str());
            var cut = name.IndexOf(" · ", StringComparison.Ordinal);
            if (cut >= 0) name = name.Substring(cut + 3);
            if (!string.IsNullOrEmpty(name))
                Float(monsterPos + new Vector2(0, monster.Height * .92f), "« " + name + " »", big ? new Color32(255, 120, 90, 255) : new Color32(255, 214, 120, 255), big ? 40 : 30);
            var fx = move["fx"].Str("slash");
            var melee = fx == "claw" || fx == "talon" || fx == "bite" || fx == "charge" || fx == "dive" || fx == "tail" || fx == "slash" || fx == "palm";
            monsterLungeTime = melee ? .4f : .25f;
            monsterLungeUntil = Time.time + monsterLungeTime;
            monsterLungeScale = melee ? 1f : .25f;
            StageMinionRush();
            return SkillStage.Monster(MonsterStage(), fx, move["v"].Int(), big, battle["m"]["element"].Str("kim"));
        }

        /// <summary>When the server reports the pack's turn, its lesser beasts rush the player in a staggered wave.</summary>
        private void StageMinionRush()
        {
            var now = Time.unscaledTime;
            for (var i = 0; i < minions.Count; i++)
            {
                minionRushStarts[i] = now + i * .11f;
                minionRushEnds[i] = minionRushStarts[i] + .58f;
            }
        }

        private float monsterLungeScale = 1f;

        private J LookupItem(J quick)
        {
            var uid = quick["uid"].Str();
            foreach (var item in owner.HubPlayer()["bag"].Items) if (item["uid"].Str() == uid) return item;
            return quick;
        }

        private static string Vn(double v) => Math.Round(v).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');

        private static void SetBar(Image fill, Text text, double value, double max)
        {
            fill.fillAmount = Mathf.Clamp01((float)(value / Math.Max(1, max)));
            text.text = Vn(value) + " / " + Vn(max);
        }

        // ------------------------------------------------------------------ actions

        private void TryAttack()
        {
            if (over) return;
            if (Time.time < nextAttackLocalTime) return;
            nextAttackLocalTime = Time.time + 0.32f;
            FireLocalAttack();
        }

        private void FireLocalAttack()
        {
            if (over || hero == null || monster == null) return;
            hero.Play(FighterAction.Attack);
            playerLungeUntil = Time.time + 0.20f;
            var right = monsterPos.x > playerPos.x;
            hero.FaceRight = right;
            var trail = BattleFx.ElementColor(element);
            hero.Ghost(trail);
            BattleFx.After(this, 0.06f, () => { if (hero != null) hero.Ghost(trail); });
            GameAudioController.Instance?.PlaySkillEffect();

            var from = playerPos + new Vector2(right ? 40f : -40f, hero.Height * 0.45f);
            var to = monsterPos + new Vector2(UnityEngine.Random.Range(-25f, 25f), monster.Height * UnityEngine.Random.Range(0.35f, 0.65f));
            var arc = UnityEngine.Random.Range(-18f, 18f);
            var fxName = "sword";
            BattleFx.Projectile(fxLayer, fxName, element, from, to, 0.13f, 1.25f, 0f, arc, () =>
            {
                if (this == null || monster == null || over) return;
                monster.Hit(0.14f);
                BattleFx.Spawn(fxLayer, "hit", element, to, 1.05f);
                Shake(0.12f);
                combo = Time.time < comboUntil ? combo + 1 : 1;
                comboUntil = Time.time + 2.5f;

                if (pendingDamageQueue.Count > 0)
                {
                    var dmg = pendingDamageQueue.Dequeue();
                    Float(to, "−" + Vn(dmg), new Color32(255, 246, 230, 255), 36);
                }
            }, true);

            if (!attackInFlight)
            {
                attackInFlight = true;
                client.Post("/battle/act", new Dictionary<string, object> { { "a", "attack" } }, (result, error) =>
                {
                    attackInFlight = false;
                    if (this == null || over || error != null) return;
                    var r = result["result"];
                    if (r.IsObject && r["ok"].Bool())
                    {
                        var dmg = r["dmg"].Num();
                        if (dmg > 0) pendingDamageQueue.Enqueue(dmg);
                        if (r["crit"].Bool())
                        {
                    Float(monsterPos + new Vector2(UnityEngine.Random.Range(-30f, 30f), monster.Height * 0.82f), "CHÍ MẠNG −" + Vn(dmg), new Color32(255, 214, 90, 255), 28);
                            Shake(0.35f);
                        }
                    }
                    if (result["state"].IsObject) owner.BattleRefreshHub(result["state"]);
                    if (result["battle"].IsObject) Apply(result["battle"]);
                });
            }
        }

        private void PerformDodge()
        {
            if (over || hero == null) return;
            if (Time.time < dodgeCooldownUntil)
            {
                Float(playerPos + new Vector2(0, hero.Height * 0.9f), "Thân pháp chưa hồi", new Color32(220, 220, 220, 255), 26);
                return;
            }
            dodgeCooldownUntil = Time.time + 4.0f;
            playerDashUntil = Time.time + 0.30f;

            var bounds = worldLayer != null ? worldLayer.rect : arena.rect;
            var dir = moveInput.sqrMagnitude > 0.05f ? moveInput.normalized : (monsterPos.x > playerPos.x ? Vector2.left : Vector2.right);
            playerPos += dir * 260f;
            playerPos = ClampToBattleGround(playerPos, bounds.size, hero.Height);

            hero.Ghost(new Color(0.4f, 0.95f, 1f, 0.95f));
            BattleFx.After(this, 0.05f, () => { if (hero != null) hero.Ghost(new Color(0.4f, 0.95f, 1f, 0.7f)); });
            BattleFx.After(this, 0.11f, () => { if (hero != null) hero.Ghost(new Color(0.4f, 0.95f, 1f, 0.5f)); });
            BattleFx.Spawn(fxLayer, "burst", "phong", playerPos, 1.25f);
            GameAudioController.Instance?.PlaySkillEffect();

            if (warnActive && !warnDodged)
            {
                warnDodged = true;
                Float(playerPos + new Vector2(0, hero.Height * 0.9f), "NÉ ĐÒN THÀNH CÔNG!", new Color32(100, 240, 255, 255), 38);
            }
            else
            {
                Float(playerPos + new Vector2(0, hero.Height * 0.9f), "Thân Pháp!", new Color32(140, 230, 255, 255), 30);
            }

            client.Post("/battle/act", new Dictionary<string, object> { { "a", "dodge" } }, (result, error) =>
            {
                if (this == null || over || error != null) return;
                if (result["state"].IsObject) owner.BattleRefreshHub(result["state"]);
                if (result["battle"].IsObject) Apply(result["battle"]);
            });
        }

        private void PerformSkill(int index)
        {
            if (over || battle.IsNull) return;
            var skills = battle["skills"];
            if (index < 0 || index >= skills.Count) return;
            var skill = skills[index];
            if (skill["locked"].Bool() || string.IsNullOrEmpty(skill["id"].Str())) return;

            if (ServerNow < skill["readyAt"].Num())
            {
                Float(playerPos + new Vector2(0, hero.Height * .9f), "Chiêu chưa hồi", new Color32(220, 220, 220, 255), 26);
                return;
            }
            if (skill["mp"].Num() > battle["p"]["mp"].Num())
            {
                Float(playerPos + new Vector2(0, hero.Height * .9f), "Không đủ linh lực", new Color32(140, 200, 255, 255), 26);
                return;
            }

            var kind = skill["kind"].Str();
            hero.Play(kind == "atk" || kind == "multi" ? FighterAction.Attack : FighterAction.Cast);
            GameAudioController.Instance?.PlaySkillEffect();

            var skillName = owner.BattleClean(skill["name"].Str());
            var skillEl = BattleFx.ElementOfSkill(skillName, element);
            Float(playerPos + new Vector2(0, hero.Height * 0.95f), "« " + skillName + " »", BattleFx.ElementColor(skillEl), 34);

            var stage = HeroStage();
            var delay = SkillStage.Player(stage, skill["id"].Str(), skillName, kind, skill["big"].Bool());
            var big = skill["big"].Bool();

            BattleFx.After(this, delay, () =>
            {
                if (this == null || monster == null || over) return;
                monster.Hit(big ? 0.32f : 0.20f);
                Shake(big ? 0.75f : 0.35f);
                combo = Time.time < comboUntil ? combo + 2 : 2;
                comboUntil = Time.time + 3.0f;
            });

            client.Post("/battle/act", new Dictionary<string, object> { { "a", "skill" }, { "i", index } }, (result, error) =>
            {
                if (this == null || over) return;
                if (error != null) { owner.BattleToast(error, true); return; }
                var r = result["result"];
                if (!r["ok"].Bool())
                {
                    if (!string.IsNullOrEmpty(r["msg"].Str()))
                        Float(playerPos + new Vector2(0, hero.Height * .9f), owner.BattleClean(r["msg"].Str()), new Color32(220, 220, 220, 255), 26);
                }
                else
                {
                    var msg = owner.BattleClean(r["msg"].Str());
                    var crit = r["crit"].Bool();
                    Float(monsterPos + new Vector2(UnityEngine.Random.Range(-40f, 40f), monster.Height * .8f), msg, crit ? new Color32(255, 214, 90, 255) : new Color32(255, 246, 230, 255), crit || big ? 30 : 24);
                }
                if (result["state"].IsObject) owner.BattleRefreshHub(result["state"]);
                if (result["battle"].IsObject) Apply(result["battle"]);
            });
        }

        private void PerformItem(int index)
        {
            if (over || battle.IsNull) return;
            if (string.IsNullOrEmpty(battle["items"][index]["uid"].Str())) return;   // an empty quick slot
            BattleFx.OnGround(fxLayer, "heal", "moc", playerPos + new Vector2(0, 14), 1.1f, false, 0, .8f);
            GameAudioController.Instance?.PlaySkillEffect();
            client.Post("/battle/act", new Dictionary<string, object> { { "a", "item" }, { "i", index } }, (result, error) =>
            {
                if (this == null || over) return;
                if (error != null) { owner.BattleToast(error, true); return; }
                var r = result["result"];
                if (r.IsObject && r["ok"].Bool())
                {
                    var msg = owner.BattleClean(r["msg"].Str());
                    Float(playerPos + new Vector2(0, hero.Height * .9f), msg, new Color32(140, 230, 160, 255), 32);
                }
                if (result["state"].IsObject) owner.BattleRefreshHub(result["state"]);
                if (result["battle"].IsObject) Apply(result["battle"]);
            });
        }

        private void Send(string action, int index)
        {
            if (action == "attack") { TryAttack(); return; }
            if (action == "dodge") { PerformDodge(); return; }
            if (action == "skill") { PerformSkill(index); return; }
            if (action == "item") { PerformItem(index); return; }
            var body = new Dictionary<string, object> { { "a", action } };
            if (index >= 0) body["i"] = index;
            client.Post("/battle/act", body, (result, error) =>
            {
                if (this == null || over) return;
                if (error != null) { owner.BattleToast(error, true); return; }
                if (result["state"].IsObject) owner.BattleRefreshHub(result["state"]);
                if (result["battle"].IsObject) Apply(result["battle"]);
            });
        }

        private void Float(Vector2 at, string text, Color color, int size = 34)
        {
            if (string.IsNullOrEmpty(text)) return;
            at = BattleFx.FloatSpot(fxLayer, at, ref lastFloat, ref lastFloatTime);
            var label = owner.BattleText(fxLayer, "Float", text, ModernUi.Bold, size, color, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), at - new Vector2(320, 34), at + new Vector2(320, 34));
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            label.gameObject.AddComponent<FloatUp>();
        }

        private void Finish(J b)
        {
            over = true;
            var result = b["result"].Str();
            var won = result.StartsWith("win");
            if (won)
            {
                monster.Die();
                foreach (var minion in minions) minion.Die();
                BattleFx.Spawn(fxLayer, "burst", battle["m"]["element"].Str("kim"), monsterPos + new Vector2(0, monster.Height * .4f), 1.6f);
                BattleFx.ScreenFlash(fxLayer, new Color(1f, .96f, .88f, .3f), .3f);
            }
            else if (result != "fled") hero.Play(FighterAction.Down);
            warnCircle.gameObject.SetActive(false);
            warnLabel.gameObject.SetActive(false);
            resultPanel = owner.BattleAnchored("Result", hud, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-520, -300), new Vector2(520, 300));
            var bg = resultPanel.gameObject.AddComponent<Image>();
            ModernUi.Fill(bg, 30f);
            bg.color = new Color32(16, 20, 24, 240);
            var title = owner.BattleText(resultPanel, "Title", won ? "ĐẠI THẮNG" : result == "fled" ? "RÚT LUI" : result == "timeout" ? "HẾT GIỜ" : "THẤT BẠI",
                ModernUi.Display, 60, won ? Gold : new Color32(220, 120, 110, 255), TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, -30));
            UiGradient.Apply(title, won ? new Color32(255, 230, 160, 255) : new Color32(240, 150, 140, 255), won ? new Color32(206, 150, 60, 255) : new Color32(170, 70, 60, 255));
            var summary = b["summary"];
            var text = "";
            if (summary.IsObject)
                foreach (var pair in summary.Pairs)
                    if (!pair.Value.IsObject && !pair.Value.IsArray && !string.IsNullOrEmpty(pair.Value.Str())) text += owner.BattleClean(pair.Value.Str()) + "\n";
            var log = b["log"];
            for (var i = Mathf.Max(0, log.Count - 5); i < log.Count; i++) text += owner.BattleClean(log[i]["text"].Str(log[i].Str())) + "\n";
            owner.BattleText(resultPanel, "Body", text.Trim(), ModernUi.Regular, 24, Cream, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(46, 130), new Vector2(-46, -120));
            var go = owner.BattleAnchored("Continue", resultPanel, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-220, 24), new Vector2(220, 112));
            var fill = go.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 26f);
            fill.color = new Color32(196, 150, 70, 255);
            owner.BattleText(go, "Text", won && !string.IsNullOrEmpty(b["dungeonLeaderId"].Str()) ? "Ải tiếp theo" : "Trở về", ModernUi.SemiBold, 30, new Color32(30, 22, 14, 255),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => owner.BattleFinished(b));
            UiIntro.Play(resultPanel, new Vector2(0, -30), .3f);
        }

        /// <summary>Previews (edit mode has no coroutines): hold a mid-fight moment with effects on screen.</summary>
        internal void PreviewMoment()
        {
            hero.Freeze(FighterAction.Cast, .9f);
            var mEl = battle["m"]["element"].Str("kim");
            var towardHero = (playerPos - monsterPos).normalized;
            var cross = new Vector2(-towardHero.y, towardHero.x);
            for (var i = 0; i < minions.Count; i++)
            {
                var progress = (i + 1f) / (minions.Count + 1f);
                var lane = (i - (minions.Count - 1) * .5f) * 54f;
                minionPositions[i] = Vector2.Lerp(monsterPos, playerPos, progress) + cross * lane;
                minions[i].Rect.anchoredPosition = minionPositions[i];
                minions[i].FaceRight = playerPos.x > minionPositions[i].x;
            }
            FxPlayer Hold(FxPlayer fx, int frame) { if (fx != null) { fx.Frozen = true; fx.StartFrame = frame; } return fx; }
            dimImage.color = new Color(.02f, .01f, .05f, .5f);
            foreach (var layer in BattleFx.Wheel(fxLayer, element, playerPos + new Vector2(0, hero.Height * .52f), 1.3f, 1f, backLayer)) if (layer != null) layer.Frozen = true;
            BattleFx.Behind(Hold(BattleFx.OnGround(fxLayer, "cast", element, playerPos + new Vector2(0, 14), 1.35f), 2), backLayer);
            Hold(BattleFx.OnGround(fxLayer, "giantsword", element, monsterPos + new Vector2(0, 14), 1.25f), 6);
            Hold(BattleFx.Spawn(fxLayer, "slash", element, monsterPos + new Vector2(monsterPos.x > playerPos.x ? -50 : 50, monster.Height * .4f), 1.3f, monsterPos.x > playerPos.x), 4);
            Hold(BattleFx.Spawn(fxLayer, "claw", mEl, playerPos + new Vector2(0, hero.Height * .4f), 1.1f, playerPos.x > monsterPos.x), 4);
            Float(monsterPos + new Vector2(0, monster.Height * .85f), "Chí mạng −1.280", new Color32(255, 214, 90, 255), 28);
            var skills = battle["m"]["skills"];
            if (skills.Count > 0) Float(monsterPos + new Vector2(0, monster.Height * 1.0f), "« " + owner.BattleClean(skills[0]["name"].Str()) + " »", new Color32(255, 214, 120, 255), 30);
            combo = 3;
            comboUntil = Time.time + 99f;
            previewHold = true;
        }

        private bool previewHold;

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (battle.IsNull) return;
            var dt = Mathf.Min(Time.deltaTime, .05f);
            // movement
            var bounds = worldLayer != null ? worldLayer.rect : arena.rect;
            var moving = moveInput.sqrMagnitude > .01f && !over;
            if (moving)
            {
                var moveSpeed = owner.BattleMoveSpeed();
                playerPos += moveInput * moveSpeed * dt;
                playerPos = ClampToBattleGround(playerPos, bounds.size, hero.Height);
                if (Mathf.Abs(moveInput.x) > .2f) hero.FaceRight = moveInput.x > 0;
            }
            if (!moving || hero.Busy) hero.FaceRight = monsterPos.x > playerPos.x;
            hero.Moving = moving;
            if (Time.unscaledTime < shakeUntil) shakeOffset = UnityEngine.Random.insideUnitCircle * shakeAmp * ((shakeUntil - Time.unscaledTime) / .3f);
            else { shakeOffset = Vector2.zero; shakeAmp = 0f; }
            if (dimImage != null && !previewHold)
            {
                var k = Time.unscaledTime < dimUntil ? Mathf.Clamp01((Time.unscaledTime - dimFrom) / .15f) * Mathf.Clamp01((dimUntil - Time.unscaledTime) / .3f) : 0f;
                dimImage.color = new Color(.02f, .01f, .05f, .56f * k);
            }
            var toMonster = (monsterPos - playerPos).normalized;
            var lunge = Time.time < playerLungeUntil ? toMonster * 70f * Mathf.Sin((playerLungeUntil - Time.time) / .25f * Mathf.PI) : Vector2.zero;
            if (Time.time < playerDashUntil) lunge -= toMonster * 150f * Mathf.Sin((playerDashUntil - Time.time) / .28f * Mathf.PI);
            hero.Rect.anchoredPosition = playerPos + lunge + shakeOffset;
            // monster keeps a fighting distance and lunges with its melee moves
            if (!over && !previewHold)
            {
                var target = playerPos + new Vector2(monsterPos.x < playerPos.x ? -300 : 300, 20);
                monsterPos = Vector2.MoveTowards(monsterPos, target, 140f * dt);
            }
            var mLunge = Time.time < monsterLungeUntil
                ? -toMonster * 150f * monsterLungeScale * Mathf.Sin((monsterLungeUntil - Time.time) / monsterLungeTime * Mathf.PI)
                : Vector2.zero;
            monster.Rect.anchoredPosition = monsterPos + mLunge + shakeOffset;
            monster.FaceRight = monsterPos.x < playerPos.x;
            var midpoint = Vector2.Lerp(monsterPos, playerPos, .46f);
            var side = new Vector2(-toMonster.y, toMonster.x);
            for (var i = 0; i < minions.Count; i++)
            {
                if (previewHold)
                {
                    minions[i].Rect.anchoredPosition = minionPositions[i] + shakeOffset;
                    continue;
                }
                var phase = Time.time * (.72f + i * .035f) + i * Mathf.PI * .5f;
                var lane = 205f + (i % 2) * 90f;
                var patrol = midpoint + new Vector2(Mathf.Cos(phase) * lane, Mathf.Sin(phase * 1.25f) * (72f + (i % 2) * 26f) - 26f);
                var rushDuration = Mathf.Max(.01f, minionRushEnds[i] - minionRushStarts[i]);
                var rushProgress = Mathf.Clamp01((Time.unscaledTime - minionRushStarts[i]) / rushDuration);
                var target = patrol;
                if (Time.unscaledTime >= minionRushStarts[i] && Time.unscaledTime <= minionRushEnds[i])
                {
                    var formationLane = (i - (minions.Count - 1) * .5f) * 62f;
                    var attackPoint = playerPos + toMonster * 42f + side * formationLane;
                    target = Vector2.Lerp(patrol, attackPoint, Mathf.SmoothStep(0f, 1f, rushProgress));
                }
                target.x = Mathf.Clamp(target.x, -bounds.width * .42f, bounds.width * .42f);
                target.y = Mathf.Clamp(target.y, -bounds.height * .34f, bounds.height * .10f);
                minionPositions[i] = Vector2.MoveTowards(minionPositions[i], target, 310f * dt);
                minions[i].Rect.anchoredPosition = minionPositions[i] + shakeOffset;
                minions[i].FaceRight = target.x > minionPositions[i].x;
            }
            UpdateCamera(bounds, dt);
            // Sort the whole pack and player together: lower figures overlap the terrain and actors in front.
            for (var i = 1; i < fighterDepthOrder.Count; i++)
            {
                var actor = fighterDepthOrder[i];
                var y = actor.anchoredPosition.y;
                var j = i - 1;
                while (j >= 0 && fighterDepthOrder[j].anchoredPosition.y < y)
                {
                    fighterDepthOrder[j + 1] = fighterDepthOrder[j];
                    j--;
                }
                fighterDepthOrder[j + 1] = actor;
            }
            for (var i = 0; i < fighterDepthOrder.Count; i++) fighterDepthOrder[i].SetSiblingIndex(i);
            // the pale trail on the enemy bar drains toward the real value
            if (monsterHpTrail != null)
                monsterHpTrail.fillAmount = monsterHpTrail.fillAmount < monsterHp.fillAmount ? monsterHp.fillAmount : Mathf.MoveTowards(monsterHpTrail.fillAmount, monsterHp.fillAmount, dt * .35f);
            if (comboText != null)
            {
                var show = combo >= 2 && Time.time < comboUntil;
                comboText.text = show ? combo + " LIÊN KÍCH" : "";
                if (show) comboText.rectTransform.localScale = Vector3.one * (1f + .25f * Mathf.Clamp01((comboUntil - 2.35f - Time.time) / .25f));
            }
            // telegraph: step out of the circle to dodge
            if (warnActive)
            {
                var warn = battle["m"]["warn"];
                var left = warn["at"].Num() - ServerNow;
                var t = Mathf.Clamp01(1f - (float)left / 1500f);
                warnCircle.anchoredPosition = warnCenter;
                warnCircle.GetComponent<Image>().color = new Color(1f, .15f, .1f, .15f + .35f * t);
                warnCircle.localScale = Vector3.one * (.6f + .4f * t);
                warnLabel.color = new Color(1f, .47f, .38f, .65f + .35f * Mathf.Sin(Time.time * 14f));
                if (!warnDodged && Vector2.Distance(playerPos, warnCenter) > WarnRadius * .9f && Time.time >= dodgeCooldownUntil)
                {
                    PerformDodge();
                }
            }
            // cooldowns
            var now = ServerNow;
            var atkLeft = (nextAttackLocalTime - Time.time) * 1000f;
            SetCooldown(attackCooldown, atkLeft, 320);
            var dodgeLeft = (dodgeCooldownUntil - Time.time) * 1000f;
            SetCooldown(dodgeCooldown, dodgeLeft, 4000);
            var skills = battle["skills"];
            var mp = battle["p"]["mp"].Num();
            foreach (var (rect, cd, seconds, icon, index) in skillButtons)
            {
                var left = skills[index]["readyAt"].Num() - now;
                SetCooldown(cd, left, 10000);
                seconds.text = left > 400 ? Mathf.CeilToInt((float)(left / 1000)).ToString() : "";
                if (!skills[index]["locked"].Bool() && !string.IsNullOrEmpty(skills[index]["id"].Str()))
                    icon.color = skills[index]["mp"].Num() > mp ? new Color(.45f, .5f, .7f, .8f) : Color.white;
            }
            if (attackHeld) TryAttack();
            // polling
            if (!over && Time.time >= nextPoll && !attackInFlight)
            {
                nextPoll = Time.time + .6f;
                client.Get("/battle/current", (data, error) =>
                {
                    if (this == null || over) return;
                    var b = data["battle"];
                    if (error == null && b.IsObject) Apply(b);
                    else if (error == null && b.IsNull) { over = true; owner.BattleFinished(battle); }
                });
            }
        }

        internal static Vector2 ClampToBattleGround(Vector2 position, Vector2 size, float actorHeight)
        {
            var halfWidth = Mathf.Max(0f, size.x * .5f - actorHeight * .35f);
            var bottom = -size.y * .5f + actorHeight * .18f;
            var top = Mathf.Max(bottom, size.y * .5f - actorHeight);
            return new Vector2(Mathf.Clamp(position.x, -halfWidth, halfWidth), Mathf.Clamp(position.y, bottom, top));
        }

        private void UpdateCamera(Rect bounds, float dt)
        {
            if (worldLayer == null) return;
            // Keep a broad safe area inside the phone view. Once the player crosses it,
            // scroll the world smoothly and stop at the actual map boundary.
            var view = arena.rect;
            var deadZone = new Vector2(Mathf.Max(1f, view.width * .30f), Mathf.Max(1f, view.height * .12f));
            var screenPosition = playerPos;
            var target = new Vector2(
                screenPosition.x - Mathf.Clamp(screenPosition.x, -deadZone.x, deadZone.x),
                screenPosition.y - Mathf.Clamp(screenPosition.y, -deadZone.y, deadZone.y));
            var maxPan = new Vector2(Mathf.Max(0f, (bounds.width - view.width) * .5f), Mathf.Max(0f, (bounds.height - view.height) * .5f));
            target.x = Mathf.Clamp(target.x, -maxPan.x, maxPan.x);
            target.y = Mathf.Clamp(target.y, -maxPan.y, maxPan.y);
            cameraOffset = Vector2.SmoothDamp(cameraOffset, target, ref cameraVelocity, .24f, Mathf.Infinity, dt);
            worldLayer.anchoredPosition = -cameraOffset;
        }

        private static void SetCooldown(Image image, double leftMs, double totalMs)
        {
            if (image == null) return;
            image.fillAmount = leftMs <= 0 ? 0f : Mathf.Clamp01((float)(leftMs / totalMs));
        }
    }

    /// <summary>Virtual analog stick.</summary>
    internal sealed class BattleStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public Action<Vector2> OnMove;

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local)) return;
            var radius = rect.rect.width * .5f;
            var v = Vector2.ClampMagnitude(local / radius, 1f);
            Knob.anchoredPosition = v * radius * .6f;
            OnMove?.Invoke(v.magnitude < .15f ? Vector2.zero : v);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Knob.anchoredPosition = Vector2.zero;
            OnMove?.Invoke(Vector2.zero);
        }

        private void Update()
        {
            // keyboard support in the editor
            var k = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (k.sqrMagnitude > .01f) { OnMove?.Invoke(k.normalized); Knob.anchoredPosition = k.normalized * ((RectTransform)transform).rect.width * .3f; }
        }
    }

    /// <summary>Press-and-hold helper (continuous attacks).</summary>
    internal sealed class BattleHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnDown;
        public Action OnUp;
        public void OnPointerDown(PointerEventData eventData) => OnDown?.Invoke();
        public void OnPointerUp(PointerEventData eventData) => OnUp?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnUp?.Invoke();
    }

    /// <summary>Damage number: pops in large, settles, then drifts up and fades.</summary>
    internal sealed class FloatUp : MonoBehaviour
    {
        private float born;
        private Text text;
        private Vector2 origin;
        private float drift;
        private void Start()
        {
            born = Time.time;
            text = GetComponent<Text>();
            origin = ((RectTransform)transform).anchoredPosition;
            drift = UnityEngine.Random.Range(-26f, 26f);
        }
        private void Update()
        {
            var age = Time.time - born;
            var rise = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / 1.3f), 2.4f);
            ((RectTransform)transform).anchoredPosition = origin + new Vector2(drift * rise, 110f * rise);
            var pop = age < .1f ? Mathf.Lerp(1.7f, .94f, age / .1f) : Mathf.Lerp(.94f, 1f, Mathf.Clamp01((age - .1f) / .08f));
            transform.localScale = new Vector3(pop, pop, 1f);
            if (text != null) { var c = text.color; c.a = Mathf.Clamp01((1.3f - age) / .35f); text.color = c; }
            if (age > 1.3f) Destroy(gameObject);
        }
    }
}
