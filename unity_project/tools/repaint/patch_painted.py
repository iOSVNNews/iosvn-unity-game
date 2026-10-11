"""Wires the painted skill icons and monster action poses into the game, editing files in place.
usage: python patch_painted.py <Assets/Scripts/Core dir>"""
import sys, os
root = sys.argv[1]


def patch(name, pairs):
    p = os.path.join(root, name)
    s = open(p, encoding='utf-8').read()
    crlf = '\r\n' in s
    s = s.replace('\r\n', '\n')
    for old, new in pairs:
        marker = next((l for l in new.split('\n') if l.strip() and l not in old), None)
        if marker and marker in s:
            continue          # already applied
        assert s.count(old) == 1, (name, old[:80], s.count(old))
        s = s.replace(old, new)
    if crlf: s = s.replace('\n', '\r\n')
    open(p, 'w', encoding='utf-8', newline='').write(s)
    print('patched', name)


patch('PrototypeBootstrap.Battle.cs', [
    ("""        internal Sprite BattleSkillIcon(J skill)
        {
            var name = skill["name"].Str().ToLowerInvariant();""",
     """        internal Sprite BattleSkillIcon(J skill)
        {
            // every player skill has its own painted icon now; the generic glyphs stay as a fallback
            var paintedSkill = PaintedArt.Skill(skill["id"].Str());
            if (paintedSkill != null) return paintedSkill;
            var name = skill["name"].Str().ToLowerInvariant();"""),
    ("""            monsterLungeScale = melee ? 1f : .25f;""",
     """            monsterLungeScale = melee ? 1f : .25f;
            monster.Strike(monsterLungeTime, melee);"""),
    ("""                minionRushEnds[i] = minionRushStarts[i] + .58f;""",
     """                minionRushEnds[i] = minionRushStarts[i] + .58f;
                minions[i].Strike(.58f, true, i * .11f);"""),
    ("""            monster.Rect.anchoredPosition = monsterPos + mLunge + shakeOffset;""",
     """            monster.Rect.anchoredPosition = monsterPos + mLunge + shakeOffset;
            monster.Step(monsterPos);"""),
    ("""                minions[i].Rect.anchoredPosition = minionPositions[i] + shakeOffset;
                minions[i].FaceRight = target.x > minionPositions[i].x;""",
     """                minions[i].Rect.anchoredPosition = minionPositions[i] + shakeOffset;
                minions[i].Step(minionPositions[i]);
                minions[i].FaceRight = target.x > minionPositions[i].x;"""),
])

patch('PrototypeBootstrap.Systems.cs', [
    ("""                var painted = BattleInkIcons.Skill(name, BattleFx.ElementOfSkill(name, skill["element"].Str("kim")));""",
     """                var painted = PaintedArt.Skill(skill["id"].Str()) ?? BattleInkIcons.Skill(name, BattleFx.ElementOfSkill(name, skill["element"].Str("kim")));"""),
])

patch('ArtSprites.cs', [
    ("""                if (Resources.Load<Texture2D>("CombatPixel/Skills/" + id) != null) return UiIcon("scroll");""",
     """                var skillArt = PaintedArt.Skill(id);
                if (skillArt != null) return skillArt;
                if (Resources.Load<Texture2D>("CombatPixel/Skills/" + id) != null) return UiIcon("scroll");"""),
])

patch('BattleFx.cs', [
    ("""        private float time, flashUntil, dieAt = -1f, phase;
        public bool FaceRight;
        public float Height => Rect != null ? Rect.sizeDelta.y : 300f;""",
     """        private float time, flashUntil, dieAt = -1f, phase;
        public bool FaceRight;
        public float Height => Rect != null ? Rect.sizeDelta.y : 300f;
        // painted action poses of this species (idle, run, attack, pounce) and what the beast is doing
        private Sprite[] poses;
        private bool posesLoaded;
        private Vector2 lastGround;
        private bool hasGround;
        private float speed, stride, strikeFrom, strikeUntil = -1f;
        private bool strikePounce;

        /// <summary>The beast strikes: a pounce leaps in then bites, otherwise it rears up and claws / bites.</summary>
        public void Strike(float seconds, bool pounce, float delay = 0f)
        {
            strikeFrom = Time.unscaledTime + Mathf.Max(0f, delay);
            strikeUntil = strikeFrom + Mathf.Max(.12f, seconds);
            strikePounce = pounce;
        }

        /// <summary>Its ground position this frame (without lunges and screen shake): drives the run cycle.</summary>
        public void Step(Vector2 ground)
        {
            var dt = Mathf.Max(1e-3f, Time.unscaledDeltaTime);
            if (hasGround)
            {
                var moved = (ground - lastGround).magnitude;
                speed = Mathf.Lerp(speed, moved / dt, 1f - Mathf.Exp(-dt * 10f));
                stride += moved;
            }
            lastGround = ground;
            hasGround = true;
        }

        /// <summary>The pose to show now, or null to keep the breathing body frames.</summary>
        private Sprite PoseNow(out float lift)
        {
            lift = 0f;
            if (!posesLoaded) { poses = PaintedArt.MonsterPoses(monsterId); posesLoaded = true; }
            if (poses == null || dieAt >= 0) return null;
            var now = Time.unscaledTime;
            if (now >= strikeFrom && now < strikeUntil)
            {
                var k = (now - strikeFrom) / Mathf.Max(.01f, strikeUntil - strikeFrom);
                if (strikePounce && k < .55f)
                {
                    lift = Mathf.Sin(k / .55f * Mathf.PI) * .16f;
                    return poses[PaintedArt.PosePounce];
                }
                return poses[PaintedArt.PoseAttack];
            }
            if (speed > 40f)
            {
                // a gait: the stride pose and the standing pose alternate with the distance covered
                var gait = Mathf.Max(26f, Rect != null ? Rect.sizeDelta.x * .16f : 40f);
                return ((int)(stride / gait) & 1) == 0 ? poses[PaintedArt.PoseRun] : poses[PaintedArt.PoseIdle];
            }
            return null;
        }"""),
    ("""            body.enabled = bodySprite != null;""",
     """            var pose = PoseNow(out var poseLift);
            if (pose != null)
            {
                bodySprite = pose;
                body.sprite = pose;
                bodyNext.enabled = false;
            }
            body.rectTransform.anchoredPosition = new Vector2(0f, poseLift * (Rect != null ? Rect.sizeDelta.y : 0f));
            flash.rectTransform.anchoredPosition = body.rectTransform.anchoredPosition;
            body.enabled = bodySprite != null;"""),
])

patch('ProvinceWorld.cs', [
    ("""                actor.Body.sprite = actor.Frames[(int)(actor.AnimTime * (actor.Moving ? 7f : 4f)) % actor.Frames.Length];
                var rightFacingArt = actor.Data is J m && m["monsterId"].Str() == "da_lang";""",
     """                // a roaming beast with painted action poses strides (run / stand) instead of only breathing
                var monsterPoses = actor.Moving && actor.Data is J pm ? PaintedArt.MonsterPoses(pm["monsterId"].Str()) : null;
                actor.Body.sprite = monsterPoses != null
                    ? monsterPoses[((int)(actor.AnimTime * 6f) & 1) == 0 ? PaintedArt.PoseRun : PaintedArt.PoseIdle]
                    : actor.Frames[(int)(actor.AnimTime * (actor.Moving ? 7f : 4f)) % actor.Frames.Length];
                var rightFacingArt = actor.Data is J m && m["monsterId"].Str() == "da_lang";"""),
])
