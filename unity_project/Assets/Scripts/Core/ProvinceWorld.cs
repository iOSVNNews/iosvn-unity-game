using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>A character, monster, boss or NPC standing on the province map.</summary>
    public sealed class WorldActor
    {
        public string Id;
        public string Kind;                // player | monster | boss | npc | other
        public RectTransform Rect;
        public Image Body;
        public Image Aura;
        public Sprite[] Frames;            // 12 hero frames (8 walk + 4 idle, facing left) or null for a static sprite
        public bool FaceRight;             // hero sheets face left; mirrored when walking right
        public AuraAnimator AuraFx;        // cultivator aura (khí tức) behind / in front of the hero
        public bool Pressure;              // emits uy áp ripples (bosses, powerful cultivators)
        public Color PressureColor = new Color(1f, .3f, .22f, 1f);
        public float NextRipple;
        public Vector2 Pos;                // tile coordinates (float), feet position
        public List<Vector2Int> Path;
        public int PathIndex;
        public float Speed = 4.2f;
        public int Facing;                 // 0 down 1 left 2 right 3 up
        public float AnimTime;
        public bool Moving;
        public RectTransform Tag;
        public Text TagText;
        public Image HpFill;
        public object Data;
        public RectInt Range;              // wander area (tiles)
        public bool Roams;
        public float NextWander;
        public float TagHeight = 40f;      // map px above the feet
        public Action OnArrive;
        public bool Hidden;
    }

    /// <summary>
    /// The painted province map: camera that follows the player, pinch/scroll zoom, tap-to-move with
    /// 8-way pathfinding, roaming monsters and bosses, name tags, drifting mist and day/night tint.
    /// </summary>
    public sealed class ProvinceWorld : MonoBehaviour
    {
        public const float T = 16f;          // painting pixels per tile
        public WorldMapData Data;
        public RectTransform Viewport;
        public RectTransform MapRect;
        public RectTransform ActorLayer;
        public RectTransform FxLayer;
        public RectTransform LabelLayer;
        public RawImage Painting;
        public Image NightTint;
        public float Zoom = 3.2f;
        public float MinZoom = 1.2f;
        public float MaxZoom = 5.5f;
        public WorldActor Player;
        public readonly List<WorldActor> Actors = new List<WorldActor>();
        public Action<WorldPoi> OnPoiTap;
        public Action<WorldActor> OnActorTap;
        public Action<Vector2Int> OnGroundTap;
        public Action OnPlayerStep;

        private Vector2 focus;              // camera focus in map-local pixels
        private bool following = true;
        private float lastManualInput = -10f;
        private float lastPinchDistance;
        private readonly List<(RectTransform rect, float speed)> clouds = new List<(RectTransform, float)>();
        private readonly List<(RectTransform rect, Vector2 local)> labels = new List<(RectTransform, Vector2)>();
        private Image tapRing;
        private Image pressureTint;
        private float pressure;
        private Vector2 shake;
        private readonly List<(Image image, float born, float size)> ripples = new List<(Image, float, float)>();
        private float tapRingTime = 99f;
        private WorldActor chaseTarget;
        private float chaseRange;
        private Action chaseArrive;
        private float nextRepath;

        public static ProvinceWorld Build(Transform parent, WorldMapData data, Texture2D painting)
        {
            var root = new GameObject("ProvinceWorld", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            var bg = root.GetComponent<Image>();
            bg.color = new Color32(228, 224, 214, 255);
            var world = root.gameObject.AddComponent<ProvinceWorld>();
            world.Data = data;
            world.Viewport = root;
            world.MapRect = Child("Map", root);
            world.MapRect.anchorMin = world.MapRect.anchorMax = new Vector2(.5f, .5f);
            world.MapRect.pivot = Vector2.zero;
            world.MapRect.sizeDelta = new Vector2(data.w * T, data.h * T);
            var paint = new GameObject("Painting", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            paint.transform.SetParent(world.MapRect, false);
            Stretch(paint.rectTransform);
            paint.texture = painting;
            paint.raycastTarget = false;
            world.Painting = paint;
            world.ActorLayer = Child("Actors", world.MapRect);
            Stretch(world.ActorLayer);
            world.FxLayer = Child("Fx", world.MapRect);
            Stretch(world.FxLayer);
            var night = new GameObject("NightTint", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            night.transform.SetParent(root, false);
            Stretch(night.rectTransform);
            night.color = new Color(0, 0, 0, 0);
            night.raycastTarget = false;
            world.NightTint = night;
            var tint = new GameObject("Pressure", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            tint.transform.SetParent(root, false);
            Stretch(tint.rectTransform);
            tint.sprite = InkUi.Vignette;
            tint.color = new Color(.3f, 0f, .04f, 0f);
            tint.raycastTarget = false;
            world.pressureTint = tint;
            world.LabelLayer = Child("Labels", root);
            world.LabelLayer.anchorMin = world.LabelLayer.anchorMax = new Vector2(.5f, .5f);
            world.LabelLayer.sizeDelta = Vector2.zero;
            var touch = root.gameObject.AddComponent<WorldTouch>();
            touch.World = world;
            world.tapRing = InkUi.Simple(world.FxLayer, "TapRing", InkUi.Ring, new Color(1, 1, 1, 0), new Vector2(T * 1.6f, T * .9f));
            world.BuildClouds();
            return world;
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.pivot = new Vector2(.5f, .5f);
        }

        // ------------------------------------------------------------------ coordinates

        public Vector2 TileToLocal(Vector2 tile) => new Vector2((tile.x + .5f) * T, (Data.h - tile.y - .5f) * T);
        public Vector2 LocalToTile(Vector2 local) => new Vector2(local.x / T - .5f, Data.h - local.y / T - .5f);
        public Vector2Int TileOf(Vector2 tile) => new Vector2Int(Mathf.RoundToInt(tile.x), Mathf.RoundToInt(tile.y));
        public Vector2 LocalToViewport(Vector2 local) => MapRect.anchoredPosition + local * Zoom;

        public bool ScreenToTile(Vector2 screen, out Vector2 tile)
        {
            tile = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(MapRect, screen, null, out var local)) return false;
            tile = LocalToTile(local);
            return true;
        }

        // ------------------------------------------------------------------ actors

        public WorldActor AddActor(string id, string kind, Vector2 tile, Sprite[] frames, Sprite still, Vector2 size, string name, Color tagColor, bool aura = false)
        {
            var actor = new WorldActor { Id = id, Kind = kind, Pos = tile, Frames = frames };
            actor.Rect = Child("Actor_" + kind + "_" + id, ActorLayer);
            actor.Rect.anchorMin = actor.Rect.anchorMax = Vector2.zero;
            actor.Rect.pivot = new Vector2(.5f, 0f);
            actor.Rect.sizeDelta = size;
            // figure sheets are wide (room for a weapon at full reach): the figure itself is about half the frame
            var figure = frames != null && frames.Length >= HeroSprites.Total;
            var foot = figure ? size.x * .5f : size.x * .9f;
            var shadow = InkUi.Simple(actor.Rect, "Shadow", InkUi.Shadow, Color.white, new Vector2(foot, foot * .36f));
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(.5f, 0f);
            shadow.rectTransform.anchoredPosition = new Vector2(0, 2);
            if (aura)
            {
                actor.Aura = InkUi.Simple(actor.Rect, "Aura", InkUi.Glow, new Color(1f, .25f, .2f, .55f), size * 1.6f);
                actor.Aura.rectTransform.anchorMin = actor.Aura.rectTransform.anchorMax = new Vector2(.5f, .45f);
            }
            actor.Body = new GameObject("Body", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            actor.Body.transform.SetParent(actor.Rect, false);
            var br = actor.Body.rectTransform;
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            actor.Body.sprite = frames != null && frames.Length > 0 ? frames[0] : still;
            actor.Body.preserveAspect = true;
            actor.Body.raycastTarget = false;
            actor.TagHeight = figure ? size.y * .875f : size.y + 4f;
            if (!string.IsNullOrEmpty(name))
            {
                actor.Tag = Child("Tag_" + id, LabelLayer);
                actor.Tag.sizeDelta = new Vector2(10, 10);
                var text = new GameObject("Name", typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
                text.transform.SetParent(actor.Tag, false);
                text.rectTransform.sizeDelta = new Vector2(420, 40);
                text.rectTransform.anchoredPosition = new Vector2(0, 14);
                text.font = ModernUi.SemiBold;
                text.fontSize = kind == "boss" ? 26 : 22;
                text.color = tagColor;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.raycastTarget = false;
                text.text = name;
                var outline = text.GetComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, .8f);
                outline.effectDistance = new Vector2(1.6f, -1.6f);
                actor.TagText = text;
                if (kind == "boss" || kind == "monster")
                {
                    var track = new GameObject("Hp", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    track.transform.SetParent(actor.Tag, false);
                    track.rectTransform.sizeDelta = new Vector2(kind == "boss" ? 120 : 76, 8);
                    track.rectTransform.anchoredPosition = new Vector2(0, -6);
                    track.color = new Color(0, 0, 0, .6f);
                    track.raycastTarget = false;
                    actor.HpFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    actor.HpFill.transform.SetParent(track.transform, false);
                    var fr = actor.HpFill.rectTransform;
                    fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = new Vector2(1, 1); fr.offsetMax = new Vector2(-1, -1);
                    actor.HpFill.sprite = InkUi.White;
                    actor.HpFill.color = kind == "boss" ? new Color32(226, 60, 48, 255) : new Color32(214, 120, 70, 255);
                    actor.HpFill.type = Image.Type.Filled;
                    actor.HpFill.fillMethod = Image.FillMethod.Horizontal;
                    actor.HpFill.raycastTarget = false;
                }
            }
            Actors.Add(actor);
            Place(actor);
            return actor;
        }

        /// <summary>Gives a hero-sheet actor its cultivator aura (frames from the look, strength from the realm).</summary>
        public void SetAura(WorldActor actor, LookSpec look, float strength)
        {
            if (actor?.Rect == null || look == null) return;
            if (actor.AuraFx == null)
            {
                Image Layer(string name, int sibling)
                {
                    var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(actor.Rect, false);
                    var r = image.rectTransform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.transform.SetSiblingIndex(sibling);
                    return image;
                }
                var bodyIndex = actor.Body.transform.GetSiblingIndex();
                var back = Layer("AuraBack", bodyIndex);
                var front = Layer("AuraFront", actor.Body.transform.GetSiblingIndex() + 1);
                actor.AuraFx = actor.Rect.gameObject.AddComponent<AuraAnimator>();
                actor.AuraFx.Back = back;
                actor.AuraFx.Front = front;
            }
            actor.AuraFx.Set(look, true, strength);
        }

        public void RemoveActor(WorldActor actor)
        {
            if (actor == null) return;
            Actors.Remove(actor);
            if (actor.Rect != null) Destroy(actor.Rect.gameObject);
            if (actor.Tag != null) Destroy(actor.Tag.gameObject);
            if (chaseTarget == actor) chaseTarget = null;
        }

        public void SetHp(WorldActor actor, double hp, double maxHp)
        {
            if (actor?.HpFill != null) actor.HpFill.fillAmount = maxHp <= 0 ? 0f : Mathf.Clamp01((float)(hp / maxHp));
        }

        private void Place(WorldActor actor)
        {
            var local = TileToLocal(actor.Pos);
            actor.Rect.anchoredPosition = new Vector2(local.x, local.y - T * .45f);
        }

        // ------------------------------------------------------------------ movement

        public bool WalkTo(Vector2Int goal, Action onArrive)
        {
            chaseTarget = null;
            return WalkPlayer(goal, onArrive);
        }

        private bool WalkPlayer(Vector2Int goal, Action onArrive)
        {
            if (Player == null) return false;
            var start = Data.NearestOpen(TileOf(Player.Pos), 3);
            goal = Data.NearestOpen(goal, 6);
            var path = Data.FindPath(start, goal);
            if (path.Count == 0)
            {
                if (start == goal) { onArrive?.Invoke(); return true; }
                return false;
            }
            Player.Path = path;
            Player.PathIndex = 0;
            Player.OnArrive = onArrive;
            following = true;
            ShowTapRing(goal);
            return true;
        }

        /// <summary>Walks to a (possibly moving) actor and calls onArrive once within range tiles.</summary>
        public void Approach(WorldActor target, float range, Action onArrive)
        {
            if (Player == null || target == null) return;
            chaseTarget = target;
            chaseRange = range;
            chaseArrive = onArrive;
            nextRepath = 0f;
            if (Vector2.Distance(Player.Pos, target.Pos) <= range) { chaseTarget = null; onArrive?.Invoke(); return; }
            WalkPlayer(TileOf(target.Pos), null);
            chaseTarget = target;
        }

        public void Stop()
        {
            chaseTarget = null;
            if (Player != null) { Player.Path = null; Player.Moving = false; }
        }

        public void Teleport(WorldActor actor, Vector2 tile)
        {
            actor.Pos = tile;
            actor.Path = null;
            Place(actor);
            if (actor == Player) { focus = TileToLocal(tile); ApplyCamera(); }
        }

        private void StepActor(WorldActor actor, float dt)
        {
            actor.Moving = false;
            if (actor.Path != null && actor.PathIndex < actor.Path.Count)
            {
                var next = (Vector2)actor.Path[actor.PathIndex];
                var delta = next - actor.Pos;
                var dist = delta.magnitude;
                var step = actor.Speed * dt;
                if (dist <= step)
                {
                    actor.Pos = next;
                    actor.PathIndex++;
                    if (actor == Player) OnPlayerStep?.Invoke();
                }
                else actor.Pos += delta / dist * step;
                if (dist > .001f)
                {
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.1f) actor.Facing = delta.x < 0 ? 1 : 2;
                    else actor.Facing = delta.y < 0 ? 3 : 0;
                    if (Mathf.Abs(delta.x) > dist * .25f) actor.FaceRight = delta.x > 0;
                }
                actor.Moving = true;
                if (actor.PathIndex >= actor.Path.Count)
                {
                    actor.Path = null;
                    var arrive = actor.OnArrive;
                    actor.OnArrive = null;
                    arrive?.Invoke();
                }
            }
            actor.AnimTime += dt;
            if (actor.Frames != null && actor.Frames.Length >= HeroSprites.Total)
            {
                actor.Body.sprite = actor.Frames[HeroSprites.FrameIndex(actor.Moving, actor.AnimTime)];
                actor.Body.rectTransform.localScale = new Vector3(actor.FaceRight ? -1f : 1f, 1f, 1f);
                if (actor.AuraFx != null) actor.AuraFx.Flip = actor.FaceRight;
            }
            else if (actor.Body != null)
            {
                // static sprites bob gently and flip to face their walking direction
                var bob = Mathf.Sin(actor.AnimTime * (actor.Moving ? 9f : 2.4f)) * (actor.Moving ? 1.6f : .8f);
                actor.Body.rectTransform.anchoredPosition = new Vector2(0, bob);
                actor.Body.rectTransform.localScale = new Vector3(actor.Facing == 2 ? -1f : 1f, 1f, 1f);
            }
            if (actor.Aura != null) actor.Aura.color = new Color(1f, .25f, .2f, .35f + Mathf.Sin(actor.AnimTime * 3f) * .15f);
            Place(actor);
        }

        private void Wander(WorldActor actor)
        {
            if (!actor.Roams || actor.Path != null || Time.time < actor.NextWander) return;
            actor.NextWander = Time.time + UnityEngine.Random.Range(1.8f, 5f);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var r = actor.Range;
                var goal = new Vector2Int(UnityEngine.Random.Range(r.xMin, r.xMax), UnityEngine.Random.Range(r.yMin, r.yMax));
                if (Data.IsBlocked(goal.x, goal.y)) continue;
                var path = Data.FindPath(TileOf(actor.Pos), goal, 2500);
                if (path.Count == 0 || path.Count > 40) continue;
                actor.Path = path;
                actor.PathIndex = 0;
                return;
            }
        }

        // ------------------------------------------------------------------ camera & input

        public void Pan(Vector2 screenDelta)
        {
            var scale = Viewport.lossyScale.x <= 0 ? 1f : Viewport.lossyScale.x;
            focus -= screenDelta / scale / Zoom;
            following = false;
            lastManualInput = Time.time;
            ApplyCamera();
        }

        public void ZoomBy(float factor, Vector2? screenPivot = null)
        {
            var old = Zoom;
            Zoom = Mathf.Clamp(Zoom * factor, MinZoom, MaxZoom);
            if (!following && screenPivot.HasValue && RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport, screenPivot.Value, null, out var pivot))
            {
                // keep the map point under the pivot fixed
                var under = (pivot - MapRect.anchoredPosition) / old;
                focus = under - pivot / Zoom;
            }
            lastManualInput = Time.time;
            ApplyCamera();
        }

        public void SetZoom(float value)
        {
            Zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            ApplyCamera();
        }

        public void Recenter() { following = true; }

        private void ApplyCamera()
        {
            MapRect.localScale = new Vector3(Zoom, Zoom, 1f);
            var view = Viewport.rect.size;
            var map = MapRect.sizeDelta * Zoom;
            var pos = -focus * Zoom;
            if (map.x > view.x) pos.x = Mathf.Clamp(pos.x, view.x * .5f - map.x, -view.x * .5f); else pos.x = -map.x * .5f;
            if (map.y > view.y) pos.y = Mathf.Clamp(pos.y, view.y * .5f - map.y, -view.y * .5f); else pos.y = -map.y * .5f;
            MapRect.anchoredPosition = pos + shake;
            focus = -pos / Zoom;
        }

        /// <summary>Uy áp: powerful actors pulse pressure rings; near them the screen darkens and trembles.</summary>
        private void UpdatePressure(float dt)
        {
            WorldActor nearest = null;
            var best = 99f;
            foreach (var actor in Actors)
            {
                if (!actor.Pressure || actor.Hidden || actor.Rect == null) continue;
                if (Time.time >= actor.NextRipple)
                {
                    actor.NextRipple = Time.time + (actor.Kind == "boss" ? 1.6f : 2.4f);
                    var ring = InkUi.Simple(FxLayer, "Ripple", InkUi.Ring, actor.PressureColor, new Vector2(T * 2f, T * 1f));
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = Vector2.zero;
                    var local = TileToLocal(actor.Pos);
                    ring.rectTransform.anchoredPosition = new Vector2(local.x, local.y - T * .45f);
                    ripples.Add((ring, Time.time, actor.Kind == "boss" ? 7f : 4.5f));
                }
                if (Player != null && actor != Player)
                {
                    var d = Vector2.Distance(actor.Pos, Player.Pos);
                    if (d < best) { best = d; nearest = actor; }
                }
            }
            for (var i = ripples.Count - 1; i >= 0; i--)
            {
                var (image, born, size) = ripples[i];
                var t = (Time.time - born) / 1.5f;
                if (image == null || t >= 1f)
                {
                    if (image != null) Destroy(image.gameObject);
                    ripples.RemoveAt(i);
                    continue;
                }
                image.rectTransform.localScale = Vector3.one * (.4f + t * size);
                var c = image.color;
                c.a = (1f - t) * .75f;
                image.color = c;
            }
            var target = nearest != null && best < 8f ? Mathf.Lerp(.42f, 0f, best / 8f) : 0f;
            pressure = Mathf.MoveTowards(pressure, target, dt * .5f);
            pressureTint.color = new Color(.3f, 0f, .04f, pressure);
            shake = pressure > .12f ? UnityEngine.Random.insideUnitCircle * pressure * 4f : Vector2.zero;
        }

        public void HandleTap(Vector2 screen)
        {
            if (!ScreenToTile(screen, out var tile)) return;
            // actors first (monsters and bosses are generous targets)
            WorldActor best = null;
            var bestDist = float.MaxValue;
            foreach (var actor in Actors)
            {
                if (actor == Player || actor.Hidden) continue;
                var reach = actor.Kind == "boss" ? 1.9f : 1.25f;
                var d = Vector2.Distance(actor.Pos + new Vector2(0, -.6f), tile);
                if (d < reach && d < bestDist) { best = actor; bestDist = d; }
            }
            if (best != null) { OnActorTap?.Invoke(best); return; }
            var cell = TileOf(tile);
            foreach (var poi in Data.pois)
            {
                if (poi?.rect == null || poi.rect.Length < 4 || poi.kind == "npcspot" || poi.kind == "zone") continue;
                if (cell.x >= poi.rect[0] && cell.x < poi.rect[0] + poi.rect[2] && cell.y >= poi.rect[1] - 1 && cell.y <= poi.rect[1] + poi.rect[3])
                {
                    OnPoiTap?.Invoke(poi);
                    return;
                }
            }
            OnGroundTap?.Invoke(cell);
        }

        public void ShowTapRing(Vector2Int tile)
        {
            tapRingTime = 0f;
            var local = TileToLocal(tile);
            tapRing.rectTransform.anchorMin = tapRing.rectTransform.anchorMax = Vector2.zero;
            tapRing.rectTransform.anchoredPosition = new Vector2(local.x, local.y - T * .45f);
        }

        // ------------------------------------------------------------------ labels & fx

        public RectTransform AddLabel(RectTransform label, Vector2 tile, Vector2 offsetPx)
        {
            label.SetParent(LabelLayer, false);
            labels.Add((label, TileToLocal(tile) + offsetPx));
            return label;
        }

        private void BuildClouds()
        {
            var rng = new System.Random(Data.id.GetHashCode());
            var count = Mathf.Clamp(Data.w * Data.h / 900, 6, 16);
            for (var i = 0; i < count; i++)
            {
                var w = 260f + (float)rng.NextDouble() * 520f;
                var image = InkUi.Simple(FxLayer, "Mist" + i, InkUi.Cloud, new Color(1, 1, 1, .18f + (float)rng.NextDouble() * .2f), new Vector2(w, w * .42f));
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.zero;
                image.rectTransform.anchoredPosition = new Vector2((float)rng.NextDouble() * Data.w * T, (float)rng.NextDouble() * Data.h * T);
                clouds.Add((image.rectTransform, 5f + (float)rng.NextDouble() * 9f));
            }
        }

        public void SetNight(float amount)
        {
            NightTint.color = new Color(.05f, .08f, .2f, Mathf.Clamp01(amount) * .42f);
        }

        // ------------------------------------------------------------------ loop

        private void Start()
        {
            if (Player != null) focus = TileToLocal(Player.Pos);
            ApplyCamera();
        }

        private void Update()
        {
            var dt = Mathf.Min(Time.deltaTime, .05f);
            HandlePinch();
            if (chaseTarget != null && Player != null)
            {
                if (Vector2.Distance(Player.Pos, chaseTarget.Pos) <= chaseRange)
                {
                    var arrive = chaseArrive;
                    chaseTarget = null;
                    Player.Path = null;
                    arrive?.Invoke();
                }
                else if (Time.time >= nextRepath)
                {
                    nextRepath = Time.time + .5f;
                    var target = chaseTarget;
                    WalkPlayer(TileOf(target.Pos), null);
                    chaseTarget = target;
                }
            }
            foreach (var actor in Actors.ToArray())
            {
                if (this == null || !isActiveAndEnabled) return;
                if (actor.Rect == null) continue;
                if (actor != Player) Wander(actor);
                StepActor(actor, dt);
            }
            if (this == null || !isActiveAndEnabled) return;
            if (Player != null && (following || Time.time - lastManualInput > 4f))
            {
                following = true;
                var target = TileToLocal(Player.Pos);
                focus = Vector2.Lerp(focus, target, 1f - Mathf.Exp(-dt * 6f));
            }
            UpdatePressure(dt);
            ApplyCamera();
            foreach (var (rect, speed) in clouds)
            {
                var p = rect.anchoredPosition;
                p.x += speed * dt;
                if (p.x > Data.w * T + 400) p.x = -400;
                rect.anchoredPosition = p;
            }
            if (tapRingTime < 1f)
            {
                tapRingTime += dt;
                var t = tapRingTime;
                tapRing.color = new Color(.95f, .86f, .6f, Mathf.Clamp01(1 - t) * .9f);
                tapRing.rectTransform.localScale = Vector3.one * (.6f + t * .7f);
            }
        }

        private void LateUpdate()
        {
            // depth sort: north (smaller y) first
            Actors.RemoveAll(a => a.Rect == null);
            Actors.Sort((a, b) => a.Pos.y.CompareTo(b.Pos.y));
            for (var i = 0; i < Actors.Count; i++) Actors[i].Rect.SetSiblingIndex(i);
            foreach (var actor in Actors)
            {
                if (actor.Tag == null) continue;
                var local = TileToLocal(actor.Pos);
                actor.Tag.anchoredPosition = LocalToViewport(new Vector2(local.x, local.y - T * .45f + actor.TagHeight));
                actor.Tag.gameObject.SetActive(!actor.Hidden);
            }
            foreach (var (rect, local) in labels)
                if (rect != null) rect.anchoredPosition = LocalToViewport(local);
        }

        private void HandlePinch()
        {
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0).position;
                var b = Input.GetTouch(1).position;
                var d = Vector2.Distance(a, b);
                if (lastPinchDistance > 0f && d > 0f) ZoomBy(d / lastPinchDistance, (a + b) * .5f);
                lastPinchDistance = d;
                WorldTouch.SuppressTap = true;
            }
            else lastPinchDistance = 0f;
        }
    }

    /// <summary>Tap / drag / scroll input for the province map.</summary>
    internal sealed class WorldTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public ProvinceWorld World;
        public static bool SuppressTap;
        private Vector2 downPosition;
        private bool dragged;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Input.touchCount <= 1) SuppressTap = false;
            downPosition = eventData.position;
            dragged = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Input.touchCount >= 2) return;
            if (!dragged && Vector2.Distance(eventData.position, downPosition) < 18f) return;
            dragged = true;
            World.Pan(eventData.delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (dragged || SuppressTap) return;
            World.HandleTap(eventData.position);
        }

        public void OnScroll(PointerEventData eventData)
        {
            World.ZoomBy(eventData.scrollDelta.y > 0 ? 1.12f : 1f / 1.12f, eventData.position);
        }
    }
}
