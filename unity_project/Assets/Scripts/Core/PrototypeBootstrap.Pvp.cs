using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Lôi đài: the PvP duel staged with the same animated pixel figures as PvE. Both duelists wear
    /// what they have equipped, burn with their own aura, swing / cast / recoil, and every skill
    /// either side uses is staged with its own effect. The server resolves every action.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private PvpArena pvpArena;
        private bool pvpArenaFailed;

        /// <summary>Removes battle scenes left under the UI (PvE and PvP) before another full-screen scene is built.</summary>
        private void ClearBattleScene()
        {
            actionBattle = null;
            pvpArena = null;
            if (backgroundRoot == null) return;
            for (var i = backgroundRoot.childCount - 1; i >= 0; i--)
            {
                var child = backgroundRoot.GetChild(i);
                if (child.name != "ActionBattle" && child.name != "PvpArena") continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        /// <summary>Opens the animated arena for the current duel; false means the caller should use the classic view.</summary>
        private bool TryShowPvpArena(Action fallback)
        {
            if (pvpArenaFailed || !AvatarComposer.Available || !hub.IsObject) return false;
            ShowBusy(true);
            client.Get("/pvp/battle", (data, error) =>
            {
                ShowBusy(false);
                var battle = data["battle"];
                if (error != null || !battle.IsObject || string.IsNullOrEmpty(battle["id"].Str())) { pvpArenaFailed = true; fallback?.Invoke(); pvpArenaFailed = false; return; }
                try { BuildPvpArena(battle); }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    pvpArenaFailed = true;
                    ClearBattleScene();
                    fallback?.Invoke();
                }
            });
            return true;
        }

        private void BuildPvpArena(J battle)
        {
            SetAtlasOrientation(false);
            ClearContent();
            ClearBattleScene();
            if (worldView != null) { Destroy(worldView.gameObject); worldView = null; }
            if (cityRoot != null) { Destroy(cityRoot); cityRoot = null; }
            var root = new GameObject("PvpArena", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            root.SetParent(backgroundRoot, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
            root.GetComponent<Image>().color = new Color32(26, 28, 34, 255);
            pvpArena = root.gameObject.AddComponent<PvpArena>();
            var hud = HudRoot();
            BuildOverlays();
            // PvP is deliberately one consistent stone ring regardless of queue or realm.
            var painting = PixelCombatPresentation.PvpGround();
            pvpArena.Init(this, client, root, hud, battle, painting, new Rect(0, 0, 1, 1));
        }

        internal LookSpec PvpLook(J side)
        {
            var text = side["look"].Str();
            var female = side["gender"].Str() == "nu";
            if (string.IsNullOrEmpty(text)) return HeroSprites.RandomLook(side["name"].Str("?"), female);
            var look = LookSpec.Parse(text);
            return look.Fill(AvatarComposer.Default(look.Get("g", female ? "f" : "m") == "f"));
        }

        internal void PvpFinished()
        {
            pvpArena = null;
            RefreshHub(SafeShowWorld);
        }
    }

    /// <summary>The duel scene and its controls.</summary>
    internal sealed class PvpArena : MonoBehaviour
    {
        private PrototypeBootstrap owner;
        private NetworkGameClient client;
        private RectTransform arena, hud;
        private J battle;
        private string battleId;
        private bool over, pending, applied;
        private int lastStamp;
        private Vector2 lastFloat;
        private float lastFloatTime = -9f;
        private float nextPoll;

        private FighterView me, foe;
        private Vector2 mePos = new Vector2(310, -150);
        private readonly Vector2 foePos = new Vector2(-310, -150);
        private Vector2 moveInput;
        private float meLungeUntil, foeLungeUntil;
        private double lastMeHp = -1, lastFoeHp = -1;
        private int lastFoeSeq = -1, lastMeSeq = -1;
        private string meElement = "kim", foeElement = "kim";

        private Image meHp, meMp, foeHp, foeMp, meTrail, foeTrail;
        private Text meHpText, meMpText, foeHpText, foeMpText, logText, roundText, statusText;
        private readonly List<(RectTransform rect, Image cooldown, Text seconds, Image icon, string id)> skillButtons = new List<(RectTransform, Image, Text, Image, string)>();
        private Image attackCooldown, dodgeCooldown;
        private RectTransform takeover, pass;
        private Vector2 shakeOffset;
        private float shakeUntil, shakeAmp;
        private Image dimImage;
        private float dimFrom, dimUntil;
        private bool previewHold;
        private RectTransform backLayer, fighterLayer, fxLayer;
        private float appliedAt;
        private static readonly Color Cream = new Color32(244, 236, 220, 255);
        private static readonly Color Gold = new Color32(232, 196, 120, 255);

        public void Init(PrototypeBootstrap owner, NetworkGameClient client, RectTransform root, RectTransform hud, J battle, Texture painting, Rect uv)
        {
            this.owner = owner;
            this.client = client;
            this.hud = hud;
            arena = root;
            battleId = battle["id"].Str();
            if (painting != null)
            {
                var bg = new GameObject("Scenery", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                bg.transform.SetParent(root, false);
                var r = bg.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                bg.texture = painting;
                var fit = ProvinceWorld.TextureUv(painting, root.rect.width / root.rect.height);
                bg.uvRect = new Rect(uv.x + fit.x * uv.width, uv.y + fit.y * uv.height,
                    uv.width * fit.width, uv.height * fit.height);
                bg.raycastTarget = false;
            }
            var tint = InkUi.Simple(root, "Tint", InkUi.White, new Color(0, 0, 0, .16f), Vector2.zero);
            tint.rectTransform.anchorMin = Vector2.zero; tint.rectTransform.anchorMax = Vector2.one;
            tint.rectTransform.offsetMin = tint.rectTransform.offsetMax = Vector2.zero;
            var veil = InkUi.Simple(root, "Vignette", InkUi.Vignette, new Color(.04f, .02f, .06f, .42f), Vector2.zero);
            veil.rectTransform.anchorMin = Vector2.zero; veil.rectTransform.anchorMax = Vector2.one;
            veil.rectTransform.offsetMin = veil.rectTransform.offsetMax = Vector2.zero;
            dimImage = InkUi.Simple(root, "Dim", InkUi.White, new Color(.02f, .01f, .05f, 0f), Vector2.zero);
            dimImage.rectTransform.anchorMin = Vector2.zero; dimImage.rectTransform.anchorMax = Vector2.one;
            dimImage.rectTransform.offsetMin = dimImage.rectTransform.offsetMax = Vector2.zero;
            var mine = battle["me"];
            var theirs = battle["opponent"];
            meElement = mine["element"].Str("kim");
            foeElement = theirs["element"].Str("kim");
            backLayer = BattleFx.Layer(root, "BackEffects");
            fighterLayer = BattleFx.Layer(root, "Fighters");
            fxLayer = BattleFx.Layer(root, "Effects");
            foe = FighterView.Create(fighterLayer, "Opponent", owner.PvpLook(theirs), FighterView.BattleScale, AvatarComposer.AuraStrength(theirs["realm"].Int()));
            me = FighterView.Create(fighterLayer, "Me", owner.PvpLook(mine), FighterView.BattleScale, AvatarComposer.AuraStrength(mine["realm"].Int()));
            foe.FaceRight = true;
            me.FaceRight = false;
            foe.Rect.anchoredPosition = foePos;
            me.Rect.anchoredPosition = mePos;
            BuildHud(battle);
            Apply(battle);
            nextPoll = Time.time + 1.2f;
        }

        private StageContext StageOf(bool mine) => new StageContext
        {
            Host = this, Arena = fxLayer, Back = backLayer,
            Caster = () => mine ? mePos : foePos, Target = () => mine ? foePos : mePos,
            CasterHeight = me.Height * .8f, TargetHeight = me.Height * .8f, Element = mine ? meElement : foeElement, Shake = Shake, Dim = Dim,
        };

        private void Dim(float seconds)
        {
            dimFrom = Time.unscaledTime;
            dimUntil = Mathf.Max(dimUntil, Time.unscaledTime + seconds);
        }

        private void Shake(float strength)
        {
            shakeAmp = Mathf.Max(shakeAmp, 24f * Mathf.Clamp01(strength));
            shakeUntil = Time.unscaledTime + .3f;
        }

        // ------------------------------------------------------------------ HUD

        private void BuildHud(J b)
        {
            var A = (Func<string, Transform, Vector2, Vector2, Vector2, Vector2, RectTransform>)owner.BattleAnchored;
            var foePlate = A("FoeBar", hud, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-300, -86), new Vector2(300, -18));
            var foeBrush = foePlate.gameObject.AddComponent<Image>();
            foeBrush.sprite = InkUi.Brush; foeBrush.type = Image.Type.Sliced; foeBrush.raycastTarget = false;
            owner.BattleText(foePlate, "Name", owner.BattleClean(b["opponent"]["name"].Str("Đối thủ")), ModernUi.SemiBold, 21, Gold, TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(14, 34), new Vector2(-14, -4));
            foeHp = Bar(foePlate, new Vector2(14, 10), new Vector2(-14, 31), new Color32(196, 62, 54, 255), out foeHpText, out foeTrail);
            foeMp = null; foeMpText = null;

            var mePlate = A("MeBar", hud, new Vector2(.18f, 0), new Vector2(.53f, 0), new Vector2(0, 82), new Vector2(0, 180));
            var meBrush = mePlate.gameObject.AddComponent<Image>();
            meBrush.sprite = InkUi.Brush; meBrush.type = Image.Type.Sliced; meBrush.raycastTarget = false;
            owner.BattleText(mePlate, "Name", owner.BattleClean(b["me"]["name"].Str("Đạo hữu")), ModernUi.SemiBold, 25, Gold, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(34, 0), new Vector2(-30, -8));
            meHp = Bar(mePlate, new Vector2(34, 55), new Vector2(-30, 81), new Color32(196, 62, 54, 255), out meHpText, out meTrail);
            meMp = Bar(mePlate, new Vector2(34, 22), new Vector2(-30, 46), new Color32(72, 140, 214, 255), out meMpText, out _);
            roundText = owner.BattleText(hud, "Round", "", ModernUi.Display, 24, Gold, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -82), new Vector2(176, -52));
            roundText.gameObject.AddComponent<Outline>().effectColor = new Color(.25f, .08f, 0, .9f);
            statusText = owner.BattleText(hud, "Status", "", ModernUi.SemiBold, 14, new Color32(255, 190, 150, 255), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -118), new Vector2(300, -86));
            statusText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            logText = owner.BattleText(hud, "Log", "", ModernUi.Regular, 13, Cream, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -215), new Vector2(325, -122));
            logText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
            // controls (bottom right): attack, the equipped skills on an arc, dodge
            var attack = A("Attack", hud, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-158, 44), new Vector2(-28, 174));
            RoundButton(attack, "swords", "Đánh", new Color32(82, 42, 39, 225), () => Send("attack", null, J.Null), 20);
            attackCooldown = Cooldown(attack);
            var skillCenters = new[]
            {
                new Vector2(-330f, 136f), new Vector2(-295f, 236f), new Vector2(-205f, 294f),
                new Vector2(-106f, 307f), new Vector2(-405f, 253f)
            };
            var skills = b["me"]["skills"];
            for (var i = 0; i < Mathf.Min(5, skills.Count); i++)
            {
                var pos = skillCenters[i];
                var rect = A("Skill" + i, hud, new Vector2(1, 0), new Vector2(1, 0), pos - new Vector2(47, 47), pos + new Vector2(47, 47));
                var skill = skills[i];
                var id = skill["id"].Str();
                var skillElement = BattleFx.ElementOfSkill(skill["name"].Str(), meElement);
                RoundButton(rect, null, null, new Color32(24, 30, 34, 225), () => Send("skill", id, skill), 20, Color.Lerp(BattleFx.ElementColor(skillElement), Color.white, .15f));
                var icon = InkUi.Simple(rect, "Icon", owner.BattleSkillIcon(skill), Color.white, Vector2.zero);
                icon.rectTransform.anchorMin = new Vector2(.2f, .2f); icon.rectTransform.anchorMax = new Vector2(.8f, .8f);
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
                var label = owner.BattleText(rect, "Name", owner.BattleClean(skill["name"].Str()) + (skill["mp"].Int() > 0 ? "\n<color=#8cc8ff>" + skill["mp"].Int() + " LL</color>" : ""),
                    ModernUi.SemiBold, 13, Cream, TextAnchor.UpperCenter, new Vector2(-.45f, 0), new Vector2(1.45f, 0), new Vector2(0, -40), new Vector2(0, -2));
                label.supportRichText = true;
                label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
                var cd = Cooldown(rect);
                var seconds = owner.BattleText(rect, "Seconds", "", ModernUi.Bold, 34, Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                seconds.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
                seconds.raycastTarget = false;
                skillButtons.Add((rect, cd, seconds, icon, id));
            }
            var dpos = new Vector2(-245f, 83f);
            var dodge = A("Dodge", hud, new Vector2(1, 0), new Vector2(1, 0), dpos - new Vector2(42, 42), dpos + new Vector2(42, 42));
            RoundButton(dodge, "spd", "Né", new Color32(40, 70, 90, 225), () => Send("dodge", null, J.Null), 16);
            dodgeCooldown = Cooldown(dodge);
            // bottom left: circular analog joystick for movement in PvP arena
            var stickBase = A("PvpStick", hud, Vector2.zero, Vector2.zero, new Vector2(38, 36), new Vector2(204, 202));
            var baseImage = stickBase.gameObject.AddComponent<Image>();
            ModernUi.Fill(baseImage, 84f);
            baseImage.color = new Color(0, 0, 0, .28f);
            var baseRing = InkUi.Simple(stickBase, "Ring", InkUi.Ring, new Color(1, 1, 1, .45f), Vector2.zero);
            baseRing.rectTransform.anchorMin = Vector2.zero; baseRing.rectTransform.anchorMax = Vector2.one;
            baseRing.rectTransform.offsetMin = baseRing.rectTransform.offsetMax = Vector2.zero;
            var knob = InkUi.Simple(stickBase, "Knob", InkUi.Glow, new Color32(232, 214, 170, 230), new Vector2(70, 70));
            var stick = stickBase.gameObject.AddComponent<BattleStick>();
            stick.Knob = knob.rectTransform;
            stick.OnMove = v => moveInput = v;

            // skip a stunned turn, or let the guardian bot stand in for an absent opponent
            pass = A("Pass", hud, Vector2.zero, Vector2.zero, new Vector2(70, 410), new Vector2(340, 490));
            Pill(pass, "Bỏ lượt (bị khống chế)", new Color32(60, 64, 72, 235), () => Send("pass", null, J.Null));
            takeover = A("Takeover", hud, Vector2.zero, Vector2.zero, new Vector2(70, 505), new Vector2(440, 585));
            Pill(takeover, "Gọi Bot Hộ Đạo đấu thay", new Color32(120, 84, 30, 235), () =>
            {
                if (pending || over) return;
                pending = true;
                client.Post("/pvp/bot-takeover", new Dictionary<string, object> { { "battleId", battleId } }, (result, error) =>
                {
                    pending = false;
                    if (this == null) return;
                    if (error != null) { owner.BattleToast(error, true); return; }
                    Refresh();
                });
            });
            pass.gameObject.SetActive(false);
            takeover.gameObject.SetActive(false);
        }

        private void Pill(RectTransform rect, string label, Color color, Action click)
        {
            var fill = rect.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 24f);
            fill.color = color;
            owner.BattleText(rect, "Text", label, ModernUi.SemiBold, 24, Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => click());
            rect.gameObject.AddComponent<UiPressScale>();
        }

        private Image Bar(RectTransform parent, Vector2 offMin, Vector2 offMax, Color color, out Text text, out Image trail)
        {
            var track = owner.BattleAnchored("Bar", parent, new Vector2(0, 0), new Vector2(1, 0), offMin, offMax);
            var t = track.gameObject.AddComponent<Image>();
            ModernUi.Fill(t, 12f);
            t.color = new Color(0, 0, 0, .6f);
            t.raycastTarget = false;
            trail = owner.BattleAnchored("Trail", track, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2)).gameObject.AddComponent<Image>();
            trail.sprite = InkUi.White; trail.type = Image.Type.Filled; trail.fillMethod = Image.FillMethod.Horizontal;
            trail.color = new Color(1f, .92f, .78f, .85f); trail.raycastTarget = false;
            var fill = owner.BattleAnchored("Fill", track, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2)).gameObject.AddComponent<Image>();
            fill.sprite = InkUi.White; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = color; fill.raycastTarget = false;
            text = owner.BattleText(track, "Text", "", ModernUi.SemiBold, 18, Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return fill;
        }

        private void RoundButton(RectTransform rect, string iconId, string label, Color color, Action click, int fontSize = 20, Color? ringColor = null)
        {
            var disc = rect.gameObject.AddComponent<Image>();
            disc.sprite = InkUi.Glow;
            disc.color = color;
            var ring = InkUi.Simple(rect, "Ring", InkUi.Ring, ringColor ?? (Color)new Color32(232, 196, 120, 200), Vector2.zero);
            ring.rectTransform.anchorMin = new Vector2(.04f, .04f); ring.rectTransform.anchorMax = new Vector2(.96f, .96f);
            ring.rectTransform.offsetMin = ring.rectTransform.offsetMax = Vector2.zero;
            if (!string.IsNullOrEmpty(iconId))
            {
                var icon = InkUi.Simple(rect, "Icon", owner.BattleUiIcon(iconId), Color.white, Vector2.zero);
                icon.rectTransform.anchorMin = new Vector2(.28f, .34f); icon.rectTransform.anchorMax = new Vector2(.72f, .8f);
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
            }
            if (!string.IsNullOrEmpty(label))
            {
                var text = owner.BattleText(rect, "Label", label, ModernUi.Bold, fontSize, Cream, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, new Vector2(0, 14), Vector2.zero);
                text.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            }
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            button.onClick.AddListener(() => click());
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

        private static string Vn(double v) => Math.Round(v).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');

        private static void SetBar(Image fill, Text text, double value, double max)
        {
            if (fill == null || text == null) return;
            fill.fillAmount = Mathf.Clamp01((float)(value / Math.Max(1, max)));
            text.text = Vn(value) + " / " + Vn(max);
        }

        private void Apply(J b)
        {
            var mine = b["me"];
            var theirs = b["opponent"];
            // A poll that left before the last action can be answered after it. Each side's move counter only
            // grows, so a lower total is an older picture: drop it instead of replaying the previous blow.
            var stamp = (mine["lastAct"].IsObject ? mine["lastAct"]["seq"].Int() : 0) + (theirs["lastAct"].IsObject ? theirs["lastAct"]["seq"].Int() : 0);
            if (applied && !b["over"].Bool() && stamp < lastStamp) return;
            lastStamp = stamp;
            battle = b;
            appliedAt = Time.time;
            SetBar(meHp, meHpText, mine["hp"].Num(), mine["maxHp"].Num(1));
            SetBar(meMp, meMpText, mine["mp"].Num(), mine["maxMp"].Num(1));
            SetBar(foeHp, foeHpText, theirs["hp"].Num(), theirs["maxHp"].Num(1));
            SetBar(foeMp, foeMpText, theirs["mp"].Num(), theirs["maxMp"].Num(1));
            roundText.text = "HIỆP " + Mathf.Max(1, b["round"].Int(1));
            var status = new List<string>();
            if (mine["stunTurns"].Int() > 0) status.Add("Bạn đang bị khống chế");
            if (mine["bindTurns"].Int() > 0) status.Add("Bạn đang bị trói");
            if (mine["dodging"].Bool()) status.Add("Thân pháp đang hộ thể");
            if (theirs["stunTurns"].Int() > 0) status.Add("Đối thủ bị khống chế");
            if (theirs["dodging"].Bool()) status.Add("Đối thủ đang né");
            if (!b["isBot"].Bool() && b["botTakeoverLeftSec"].Int() > 0) status.Add("Đối thủ vắng mặt: Bot Hộ Đạo sau " + b["botTakeoverLeftSec"].Int() + " giây");
            statusText.text = string.Join("  ·  ", status);
            pass.gameObject.SetActive(!b["over"].Bool() && mine["stunTurns"].Int() > 0);
            takeover.gameObject.SetActive(!b["over"].Bool() && b["canBotTakeover"].Bool());
            var lines = new List<string>();
            var log = b["log"];
            for (var i = Mathf.Max(0, log.Count - 3); i < log.Count; i++) lines.Add(owner.BattleClean(log[i]["text"].Str(log[i].Str())));
            logText.text = string.Join("\n", lines);

            // stage what each side did since the last update: my action first, the answer after it
            var first = !applied;
            var delay = 0f;
            var myAct = mine["lastAct"];
            if (myAct.IsObject && myAct["seq"].Int() != lastMeSeq)
            {
                lastMeSeq = myAct["seq"].Int();
                if (!first) delay = StageAct(true, myAct, 0f);
            }
            var foeAct = theirs["lastAct"];
            var foeDelay = 0f;
            if (foeAct.IsObject && foeAct["seq"].Int() != lastFoeSeq)
            {
                lastFoeSeq = foeAct["seq"].Int();
                if (!first) foeDelay = StageAct(false, foeAct, delay > 0 ? delay + .35f : 0f);
            }
            var foeHpNow = theirs["hp"].Num();
            if (lastFoeHp >= 0 && foeHpNow < lastFoeHp - .5) Land(false, lastFoeHp - foeHpNow, delay);
            lastFoeHp = foeHpNow;
            var meHpNow = mine["hp"].Num();
            if (lastMeHp >= 0 && meHpNow < lastMeHp - .5) Land(true, lastMeHp - meHpNow, foeDelay);
            lastMeHp = meHpNow;
            applied = true;
            if (b["over"].Bool() && !over) BattleFx.After(this, Mathf.Max(delay, foeDelay) + .5f, () => { if (this != null && !over) Finish(b); });
        }

        /// <summary>Plays one side's action; returns the time from now until its blow lands.</summary>
        private float StageAct(bool mine, J act, float wait)
        {
            var kind = act["act"].Str();
            var skillKind = act["kind"].Str();
            void Go()
            {
                if (this == null) return;
                var actor = mine ? me : foe;
                var from = mine ? mePos : foePos;
                var ctx = StageOf(mine);
                if (kind == "dodge") { SkillStage.Player(ctx, "dodge", "", "escape", false); return; }
                if (kind == "skill")
                {
                    var name = owner.BattleClean(act["name"].Str());
                    actor.Play(skillKind == "atk" || skillKind == "multi" ? FighterAction.Attack : FighterAction.Cast);
                    Float(from + new Vector2(0, actor.Height * .95f), "« " + name + " »", BattleFx.ElementColor(BattleFx.ElementOfSkill(name, ctx.Element)), 30);
                    SkillStage.Player(ctx, act["skillId"].Str(), act["name"].Str(), skillKind, act["big"].Bool());
                    return;
                }
                actor.Play(FighterAction.Attack);
                if (mine) meLungeUntil = Time.time + .28f; else foeLungeUntil = Time.time + .28f;
                var trail = BattleFx.ElementColor(ctx.Element);
                BattleFx.After(this, .05f, () => { if (actor != null) actor.Ghost(trail); });
                BattleFx.After(this, .11f, () => { if (actor != null) actor.Ghost(trail); });
                SkillStage.Player(ctx, "attack", "", "attack", false);
            }
            if (wait > 0f) BattleFx.After(this, wait, Go); else Go();
            return wait + (kind == "skill" ? .4f : kind == "dodge" ? .1f : .22f);
        }

        private void Land(bool onMe, double lost, float delay)
        {
            void Go()
            {
                if (this == null) return;
                var victim = onMe ? me : foe;
                victim.Play(FighterAction.Hurt);
                Float((onMe ? mePos : foePos) + new Vector2(UnityEngine.Random.Range(-30f, 30f), victim.Height * .84f), "−" + Vn(lost),
                    onMe ? new Color32(255, 110, 90, 255) : new Color32(255, 240, 200, 255), 40);
                Shake(.35f);
            }
            if (delay > 0f) BattleFx.After(this, delay, Go); else Go();
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

        // ------------------------------------------------------------------ actions

        private void Send(string action, string skillId, J skill)
        {
            if (over || pending) return;
            var mine = battle["me"];
            if (action == "skill")
            {
                J current = J.Null;
                foreach (var s in mine["skills"].Items) if (s["id"].Str() == skillId) current = s;
                if (current.IsObject && current["cdLeft"].Int() > 0 && Time.time - appliedAt < current["cdLeft"].Int()) { Float(mePos + new Vector2(0, me.Height * .9f), "Chiêu chưa hồi", new Color32(220, 220, 220, 255), 26); return; }
                if (current.IsObject && current["mp"].Num() > mine["mp"].Num()) { Float(mePos + new Vector2(0, me.Height * .9f), "Không đủ linh lực", new Color32(140, 200, 255, 255), 26); return; }
            }
            pending = true;
            var body = new Dictionary<string, object> { { "battleId", battleId }, { "act", action } };
            if (!string.IsNullOrEmpty(skillId)) body["skillId"] = skillId;
            client.Post("/pvp/action", body, (result, error) =>
            {
                pending = false;
                if (this == null) return;
                if (error != null) { owner.BattleToast(error, true); return; }
                if (result["state"].IsObject) owner.BattleRefreshHub(result["state"]);
                if (result["battle"].IsObject) Apply(result["battle"]);
                nextPoll = Time.time + 1.2f;
            });
        }

        private void Refresh()
        {
            client.Get("/pvp/battle", (data, error) =>
            {
                if (this == null || over) return;
                var b = data["battle"];
                if (error == null && b.IsObject && !string.IsNullOrEmpty(b["id"].Str())) Apply(b);
                else if (error == null && !over) { over = true; owner.PvpFinished(); }
            });
        }

        private void Finish(J b)
        {
            over = true;
            var won = b["isWin"].Bool();
            (won ? foe : me).Play(FighterAction.Down);
            pass.gameObject.SetActive(false);
            takeover.gameObject.SetActive(false);
            var panel = owner.BattleAnchored("Result", hud, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-520, -280), new Vector2(520, 280));
            var bg = panel.gameObject.AddComponent<Image>();
            ModernUi.Fill(bg, 30f);
            bg.color = new Color32(16, 20, 24, 240);
            var title = owner.BattleText(panel, "Title", won ? "ĐẠI THẮNG" : "THẤT BẠI", ModernUi.Display, 60, won ? Gold : new Color32(220, 120, 110, 255),
                TextAnchor.UpperCenter, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, -30));
            UiGradient.Apply(title, won ? new Color32(255, 230, 160, 255) : new Color32(240, 150, 140, 255), won ? new Color32(206, 150, 60, 255) : new Color32(170, 70, 60, 255));
            var text = owner.BattleClean(b["result"].Str()) + "\n";
            var log = b["log"];
            for (var i = Mathf.Max(0, log.Count - 5); i < log.Count; i++) text += owner.BattleClean(log[i]["text"].Str(log[i].Str())) + "\n";
            owner.BattleText(panel, "Body", text.Trim(), ModernUi.Regular, 24, Cream, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(46, 130), new Vector2(-46, -120));
            var go = owner.BattleAnchored("Continue", panel, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-220, 24), new Vector2(220, 112));
            var fill = go.gameObject.AddComponent<Image>();
            ModernUi.Fill(fill, 26f);
            fill.color = new Color32(196, 150, 70, 255);
            owner.BattleText(go, "Text", "Rời lôi đài", ModernUi.SemiBold, 30, new Color32(30, 22, 14, 255), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => owner.PvpFinished());
            UiIntro.Play(panel, new Vector2(0, -30), .3f);
        }

        /// <summary>Previews (edit mode has no coroutines): hold a mid-duel moment with effects on screen.</summary>
        internal void PreviewMoment()
        {
            me.Freeze(FighterAction.Attack, .6f);
            foe.Freeze(FighterAction.Cast, .9f);
            FxPlayer Hold(FxPlayer fx, int frame) { if (fx != null) { fx.Frozen = true; fx.StartFrame = frame; } return fx; }
            dimImage.color = new Color(.02f, .01f, .05f, .5f);
            previewHold = true;
            foreach (var layer in BattleFx.Wheel(fxLayer, foeElement, foePos + new Vector2(0, foe.Height * .52f), 1.3f, 1f, backLayer)) if (layer != null) layer.Frozen = true;
            BattleFx.Behind(Hold(BattleFx.OnGround(fxLayer, "cast", foeElement, foePos + new Vector2(0, 14), 1.35f), 3), backLayer);
            Hold(BattleFx.Spawn(fxLayer, "slash", meElement, foePos + new Vector2(50, foe.Height * .4f), 1.3f, false), 4);
            Hold(BattleFx.Spawn(fxLayer, "dragon", "loi", (mePos + foePos) * .5f + new Vector2(60, 230), 1.25f, true), 2);
            Hold(BattleFx.OnGround(fxLayer, "pillar", "loi", mePos + new Vector2(130, 14), 1f), 6);
            Float(foePos + new Vector2(0, foe.Height * .86f), "−486", new Color32(255, 240, 200, 255), 44);
        }

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (battle.IsNull) return;
            if (Time.unscaledTime < shakeUntil) shakeOffset = UnityEngine.Random.insideUnitCircle * shakeAmp * ((shakeUntil - Time.unscaledTime) / .3f);
            else { shakeOffset = Vector2.zero; shakeAmp = 0f; }
            if (dimImage != null && !previewHold)
            {
                var k = Time.unscaledTime < dimUntil ? Mathf.Clamp01((Time.unscaledTime - dimFrom) / .15f) * Mathf.Clamp01((dimUntil - Time.unscaledTime) / .3f) : 0f;
                dimImage.color = new Color(.02f, .01f, .05f, .56f * k);
            }
            var dt = Mathf.Min(Time.deltaTime, .05f);
            if (moveInput.sqrMagnitude > .01f && !over)
            {
                var moveSpeed = owner.BattleMoveSpeed();
                mePos += moveInput * moveSpeed * dt;
                mePos = ActionBattle.ClampToBattleGround(mePos, arena.rect.size, me.Height);
                if (Mathf.Abs(moveInput.x) > .2f) me.FaceRight = moveInput.x > 0;
            }
            me.Moving = moveInput.sqrMagnitude > .01f && !over;
            var meLunge = Time.time < meLungeUntil ? new Vector2(-170f, 0) * Mathf.Sin((meLungeUntil - Time.time) / .28f * Mathf.PI) : Vector2.zero;
            var foeLunge = Time.time < foeLungeUntil ? new Vector2(170f, 0) * Mathf.Sin((foeLungeUntil - Time.time) / .28f * Mathf.PI) : Vector2.zero;
            me.Rect.anchoredPosition = mePos + meLunge + shakeOffset;
            foe.Rect.anchoredPosition = foePos + foeLunge + shakeOffset;
            meTrail.fillAmount = meTrail.fillAmount < meHp.fillAmount ? meHp.fillAmount : Mathf.MoveTowards(meTrail.fillAmount, meHp.fillAmount, dt * .35f);
            foeTrail.fillAmount = foeTrail.fillAmount < foeHp.fillAmount ? foeHp.fillAmount : Mathf.MoveTowards(foeTrail.fillAmount, foeHp.fillAmount, dt * .35f);
            // cooldowns count down locally from the last server answer
            var mine = battle["me"];
            var since = Time.time - appliedAt;
            SetCooldown(attackCooldown, mine["attackCdLeft"].Int() - since, 1f);
            SetCooldown(dodgeCooldown, mine["dodgeCdLeft"].Int() - since, 6f);
            var mp = mine["mp"].Num();
            foreach (var (rect, cd, seconds, icon, id) in skillButtons)
            {
                J skill = J.Null;
                foreach (var s in mine["skills"].Items) if (s["id"].Str() == id) skill = s;
                var left = skill["cdLeft"].Int() - since;
                SetCooldown(cd, left, Mathf.Max(1f, skill["cd"].Int(5)));
                seconds.text = left > .4f ? Mathf.CeilToInt(left).ToString() : "";
                icon.color = skill["mp"].Num() > mp ? new Color(.45f, .5f, .7f, .8f) : Color.white;
            }
            if (!over && !pending && Time.time >= nextPoll)
            {
                nextPoll = Time.time + 1.2f;
                Refresh();
            }
        }

        private static void SetCooldown(Image image, float left, float total)
        {
            if (image == null) return;
            image.fillAmount = left <= 0 ? 0f : Mathf.Clamp01(left / total);
        }
    }
}
