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
        public int Hop = 7;                // tiles covered by one stroll; bosses stride much further
        public float TagHeight = 40f;      // map px above the feet
        public Action OnArrive;
        public bool Hidden;
        public Image Shadow;
        public float Lift;                 // map px above the ground (flying sword, mount)
    }

    /// <summary>How the player crosses the province: on foot, on a flying sword or on a mount.</summary>
    public enum TravelMode { Walk, Sword, Mount }

    /// <summary>
    /// The painted realm world: camera that follows the player, pinch/scroll zoom, tap-to-move with
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
        public float MinZoom = .85f;
        public float MaxZoom = 5.5f;
        /// <summary>Tiles per second on foot; a sword or a mount multiplies it.</summary>
        public float WalkSpeed = 3.2f;
        public TravelMode Travel { get; private set; }
        /// <summary>In the air nothing on the ground is in the way; the outer world edge still bounds travel.</summary>
        public bool Flying => Travel != TravelMode.Walk;
        public WorldActor Player;
        public readonly List<WorldActor> Actors = new List<WorldActor>();
        public Action<WorldPoi> OnPoiTap;
        public Action<WorldActor> OnActorTap;
        public Action<Vector2Int> OnGroundTap;
        public Action OnPlayerStep;
        public Vector2 VirtualStick;
        private Vector2Int lastStepTile = new Vector2Int(-999, -999);

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
        private float lift, liftTarget;
        private Image vehicle, vehicleGlow;
        private Sprite[] vehicleFrames;
        private Color travelColor = Color.white;
        private float nextTrail;
        private Vector2 lastTrailPos;
        private readonly List<(Image image, float born, float life)> trail = new List<(Image, float, float)>();
        private readonly List<(WorldActor actor, Vector2 basePos)> tagScratch = new List<(WorldActor, Vector2)>();
        private readonly List<Vector2> tagResolved = new List<Vector2>();
        private static Sprite swordSprite;

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
            if (painting != null && painting.width > 0 && painting.height > 0)
            {
                var imageAspect = (float)painting.width / painting.height;
                var mapAspect = (float)data.w / data.h;
                if (imageAspect > mapAspect)
                {
                    var visibleWidth = mapAspect / imageAspect;
                    paint.uvRect = new Rect((1f - visibleWidth) * .5f, 0f, visibleWidth, 1f);
                }
                else if (imageAspect < mapAspect)
                {
                    var visibleHeight = imageAspect / mapAspect;
                    paint.uvRect = new Rect(0f, (1f - visibleHeight) * .5f, 1f, visibleHeight);
                }
            }
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
            actor.Shadow = shadow;
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

                var pill = new GameObject("Pill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                pill.transform.SetParent(actor.Tag, false);
                pill.raycastTarget = false;

                Color32 bgCol;
                Color32 borderCol;
                int fontSize;
                if (kind == "boss")
                {
                    bgCol = new Color32(36, 12, 16, 225);
                    borderCol = new Color32(245, 76, 60, 235);
                    fontSize = 21;
                }
                else if (kind == "player")
                {
                    bgCol = new Color32(26, 22, 16, 220);
                    borderCol = new Color32(240, 204, 110, 230);
                    fontSize = 20;
                }
                else if (kind == "npc")
                {
                    bgCol = new Color32(14, 20, 32, 215);
                    borderCol = new Color32(80, 165, 245, 220);
                    fontSize = 19;
                }
                else
                {
                    bgCol = new Color32(20, 16, 14, 200);
                    borderCol = new Color32(175, 125, 75, 180);
                    fontSize = 18;
                }

                ModernUi.Fill(pill, 14f);
                pill.color = bgCol;

                var border = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                border.transform.SetParent(pill.transform, false);
                Stretch(border.rectTransform);
                ModernUi.Ring(border, 14f, 1.2f);
                border.color = borderCol;
                border.raycastTarget = false;

                var text = new GameObject("Name", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                text.transform.SetParent(pill.transform, false);
                text.font = ModernUi.SemiBold;
                text.fontSize = fontSize;
                text.color = tagColor;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 14;
                text.resizeTextMaxSize = fontSize;
                text.raycastTarget = false;
                text.text = name;
                PixelUiSkin.ApplyTextTreatment(text);

                var textWidth = Mathf.Clamp(name.Length * (fontSize * 0.56f) + 22f, 80f, kind == "boss" ? 240f : kind == "monster" ? 190f : 230f);
                var pillHeight = (kind == "boss" || kind == "monster") ? 38f : 30f;
                pill.rectTransform.sizeDelta = new Vector2(textWidth, pillHeight);
                pill.rectTransform.anchoredPosition = new Vector2(0, 14);
                var textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 2f);
                textRect.offsetMax = new Vector2(-8f, -2f);

                actor.TagText = text;
                if (kind == "boss" || kind == "monster")
                {
                    var track = new GameObject("Hp", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    track.transform.SetParent(actor.Tag, false);
                    track.rectTransform.sizeDelta = new Vector2(Mathf.Min(textWidth - 8f, kind == "boss" ? 120f : 76f), 6f);
                    track.rectTransform.anchoredPosition = new Vector2(0, -6f);
                    track.color = new Color(0, 0, 0, .75f);
                    track.raycastTarget = false;
                    ModernUi.Fill(track, 3f);

                    actor.HpFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    actor.HpFill.transform.SetParent(track.transform, false);
                    var fr = actor.HpFill.rectTransform;
                    fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = new Vector2(1, 1); fr.offsetMax = new Vector2(-1, -1);
                    actor.HpFill.sprite = InkUi.White;
                    actor.HpFill.color = kind == "boss" ? new Color32(236, 68, 54, 255) : new Color32(224, 130, 70, 255);
                    actor.HpFill.type = Image.Type.Filled;
                    actor.HpFill.fillMethod = Image.FillMethod.Horizontal;
                    actor.HpFill.raycastTarget = false;
                    ModernUi.Fill(actor.HpFill, 2f);
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
            actor.Rect.anchoredPosition = new Vector2(local.x, local.y - T * .45f + actor.Lift);
            if (actor.Shadow != null && (actor.Lift != 0f || actor == Player))
            {
                // the shadow stays on the ground and thins out as the rider climbs
                var height = Mathf.Max(0f, actor.Lift);
                actor.Shadow.rectTransform.anchoredPosition = new Vector2(0, 2 - actor.Lift);
                var k = 1f / (1f + height * .035f);
                actor.Shadow.rectTransform.localScale = new Vector3(k, k, 1f);
                actor.Shadow.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, .5f, Mathf.Clamp01(height / 24f)));
            }
        }

        // ------------------------------------------------------------------ flying sword & mount

        /// <summary>
        /// Switches how the player travels. A sword or a mount lifts the figure off the ground, multiplies the
        /// speed and lets it cross mountains, forest and water; the wall between provinces stays closed.
        /// </summary>
        public void SetTravel(TravelMode mode, Sprite[] mountFrames, Color color, float speedFactor)
        {
            if (Player == null) return;
            var changed = Travel != mode;
            Travel = mode;
            travelColor = color;
            vehicleFrames = mode == TravelMode.Mount ? mountFrames : null;
            Player.Speed = WalkSpeed * (mode == TravelMode.Walk ? 1f : Mathf.Max(1.2f, speedFactor));
            var tall = Player.Rect.sizeDelta.y;
            liftTarget = mode == TravelMode.Sword ? tall * .34f : mode == TravelMode.Mount ? tall * .86f : 0f;
            Player.Path = null;
            Player.OnArrive = null;
            chaseTarget = null;
            BuildVehicle();
            if (changed) TravelBurst();
        }

        /// <summary>Editor captures have no Update loop: put the rider at full height with a trail behind.</summary>
        public void SnapTravel(Vector2 direction)
        {
            if (Player == null) return;
            lift = liftTarget;
            Player.Lift = lift;
            Player.FaceRight = direction.x > 0;
            UpdateVehicle(0f);
            Place(Player);
            if (!Flying) return;
            var step = direction.normalized * -.55f;
            for (var i = 1; i <= 7; i++) SpawnTrail(Player.Pos + step * i, direction, 1f - i / 8f);
        }

        /// <summary>The nearest tile a walker can stand on, for coming down from the air.</summary>
        public bool LandingSpot(out Vector2Int tile)
        {
            tile = Player != null ? Data.NearestOpen(TileOf(Player.Pos), 30) : default;
            return Player != null && !Data.IsBlocked(tile.x, tile.y);
        }

        private void BuildVehicle()
        {
            if (vehicle != null) Destroy(vehicle.gameObject);
            if (vehicleGlow != null) Destroy(vehicleGlow.gameObject);
            vehicle = null;
            vehicleGlow = null;
            if (!Flying || Player?.Rect == null) return;
            var size = Player.Rect.sizeDelta;
            var bodyIndex = Player.Body.transform.GetSiblingIndex();
            vehicleGlow = InkUi.Simple(Player.Rect, "TravelGlow", InkUi.Glow, new Color(travelColor.r, travelColor.g, travelColor.b, .55f),
                Travel == TravelMode.Sword ? new Vector2(size.y * 1.5f, size.y * .42f) : new Vector2(size.y * 1.7f, size.y * .7f));
            vehicleGlow.rectTransform.anchorMin = vehicleGlow.rectTransform.anchorMax = new Vector2(.5f, 0f);
            vehicleGlow.rectTransform.anchoredPosition = new Vector2(0, Travel == TravelMode.Sword ? size.y * .04f : -size.y * .2f);
            vehicleGlow.transform.SetSiblingIndex(bodyIndex);
            vehicle = new GameObject("Vehicle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            vehicle.transform.SetParent(Player.Rect, false);
            vehicle.raycastTarget = false;
            vehicle.preserveAspect = true;
            var rect = vehicle.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            if (Travel == TravelMode.Sword)
            {
                vehicle.sprite = SwordSprite();
                vehicle.color = Color.Lerp(Color.white, travelColor, .35f);
                rect.sizeDelta = new Vector2(size.y * 1.25f, size.y * .3f);
                rect.anchoredPosition = new Vector2(0, -size.y * .1f);
            }
            else
            {
                vehicle.sprite = vehicleFrames != null && vehicleFrames.Length > 0 ? vehicleFrames[0] : null;
                vehicle.color = vehicle.sprite != null ? Color.white : new Color(1, 1, 1, 0);
                rect.sizeDelta = new Vector2(size.y * 1.55f, size.y * 1.55f);
                rect.anchoredPosition = new Vector2(0, -size.y * .78f);
            }
            // the blade passes in front of the feet; a mount is drawn behind the rider, who stands on its back
            if (Travel == TravelMode.Sword) vehicle.transform.SetSiblingIndex(Player.Body.transform.GetSiblingIndex() + 1);
            else vehicle.transform.SetSiblingIndex(vehicleGlow.transform.GetSiblingIndex() + 1);
        }

        private void UpdateVehicle(float dt)
        {
            if (Player == null) return;
            lift = Mathf.MoveTowards(lift, liftTarget, dt * 46f);
            var bob = lift > .5f ? Mathf.Sin(Time.time * 3.1f) * 1.1f * Mathf.Clamp01(lift / 6f) : 0f;
            Player.Lift = lift + bob;
            if (vehicle == null) return;
            if (Travel == TravelMode.Mount && vehicleFrames != null && vehicleFrames.Length > 0)
                vehicle.sprite = vehicleFrames[(int)(Time.time * (Player.Moving ? 9f : 5f)) % vehicleFrames.Length];
            // the sword sheet points left like the hero sheets; monster art (mounts) faces right
            var face = Travel == TravelMode.Mount ? (Player.FaceRight ? 1f : -1f) : (Player.FaceRight ? -1f : 1f);
            vehicle.rectTransform.localScale = new Vector3(face, 1f, 1f);
            if (vehicleGlow != null)
            {
                var c = vehicleGlow.color;
                c.a = .42f + Mathf.Sin(Time.time * 5f) * .12f;
                vehicleGlow.color = c;
            }
        }

        private void TravelBurst()
        {
            if (Player == null || FxLayer == null) return;
            var local = TileToLocal(Player.Pos);
            for (var i = 0; i < 2; i++)
            {
                var ring = InkUi.Simple(FxLayer, "TravelRing", InkUi.Ring, travelColor, new Vector2(T * 2f, T * 1f));
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = Vector2.zero;
                ring.rectTransform.anchoredPosition = new Vector2(local.x, local.y - T * .45f);
                ripples.Add((ring, Time.time - i * .18f, 3.2f + i * 1.6f));
            }
        }

        private void SpawnTrail(Vector2 tile, Vector2 direction, float strength)
        {
            if (FxLayer == null) return;
            var sword = Travel == TravelMode.Sword;
            var tall = Player.Rect.sizeDelta.y;
            var size = sword ? new Vector2(tall * .95f, tall * .13f) : new Vector2(tall * .6f, tall * .34f);
            var color = sword ? new Color(travelColor.r, travelColor.g, travelColor.b, .7f * strength) : new Color(1f, 1f, 1f, .42f * strength);
            var image = InkUi.Simple(FxLayer, "Trail", sword ? InkUi.Glow : InkUi.Cloud, color, size);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.zero;
            var local = TileToLocal(tile);
            image.rectTransform.anchoredPosition = new Vector2(local.x, local.y - T * .45f + lift + tall * (sword ? .06f : .2f));
            if (sword && direction.sqrMagnitude > .0001f)
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg);
            trail.Add((image, Time.time, sword ? .42f : .7f));
        }

        private void UpdateTrail(float dt)
        {
            if (Flying && Player != null && Player.Moving && Time.time >= nextTrail)
            {
                nextTrail = Time.time + (Travel == TravelMode.Sword ? .035f : .07f);
                var direction = Player.Pos - lastTrailPos;
                if (direction.sqrMagnitude > .0004f) SpawnTrail(Player.Pos, direction, 1f);
            }
            if (Player != null) lastTrailPos = Player.Pos;
            for (var i = trail.Count - 1; i >= 0; i--)
            {
                var (image, born, life) = trail[i];
                var t = (Time.time - born) / life;
                if (image == null || t >= 1f)
                {
                    if (image != null) Destroy(image.gameObject);
                    trail.RemoveAt(i);
                    continue;
                }
                var c = image.color;
                c.a *= 1f - Mathf.Clamp01(dt / Mathf.Max(.01f, life * (1f - t)));
                image.color = c;
                image.rectTransform.localScale = new Vector3(1f + t * .5f, 1f - t * .45f, 1f);
            }
        }

        /// <summary>A slim blade pointing left, drawn once: the flying sword under the rider's feet.</summary>
        private static Sprite SwordSprite()
        {
            if (swordSprite != null) return swordSprite;
            const int w = 96, h = 24;
            var pixels = new Color32[w * h];
            var edge = new Color32(250, 252, 255, 255);
            var steel = new Color32(196, 212, 232, 255);
            var shade = new Color32(120, 140, 172, 255);
            var gold = new Color32(232, 190, 96, 255);
            var dark = new Color32(60, 44, 40, 255);
            for (var x = 0; x < w; x++)
            {
                // blade: x 2..66 (tip at the left), guard 67..71, grip 72..90, pommel 91..94
                if (x >= 2 && x <= 66)
                {
                    var half = x < 16 ? Mathf.Max(0, (x - 2) / 4) : 3;
                    for (var dy = -half; dy <= half; dy++)
                        pixels[(12 + dy) * w + x] = dy == half || dy == -half ? shade : dy == 0 ? edge : steel;
                }
                else if (x >= 67 && x <= 71)
                    for (var dy = -7; dy <= 7; dy++) pixels[(12 + dy) * w + x] = Mathf.Abs(dy) > 5 || x == 67 || x == 71 ? dark : gold;
                else if (x >= 72 && x <= 90)
                    for (var dy = -2; dy <= 2; dy++) pixels[(12 + dy) * w + x] = (x / 3) % 2 == 0 ? dark : gold;
                else if (x >= 91 && x <= 94)
                    for (var dy = -3; dy <= 3; dy++) pixels[(12 + dy) * w + x] = gold;
            }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "FlyingSword", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            swordSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 16f);
            return swordSprite;
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
            var start = Data.NearestOpen(TileOf(Player.Pos), 8, Flying);
            goal = Data.NearestOpen(goal, 16, Flying);
            var path = Data.FindPath(start, goal, 60000, Flying);
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

        private void StepActor(WorldActor actor, float dt, bool manualMoving = false)
        {
            if (!manualMoving) actor.Moving = false;
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
                actor.Body.sprite = actor.Frames[HeroSprites.FrameIndex(actor.Moving && !(actor == Player && Flying), actor.AnimTime)];
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
            var here = TileOf(actor.Pos);
            var hop = Mathf.Max(3, actor.Hop);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                // one stroll at a time inside the roaming range: a wide range is crossed over many strolls
                var r = actor.Range;
                var goal = new Vector2Int(
                    Mathf.Clamp(here.x + UnityEngine.Random.Range(-hop, hop + 1), r.xMin, Mathf.Max(r.xMin, r.xMax - 1)),
                    Mathf.Clamp(here.y + UnityEngine.Random.Range(-hop, hop + 1), r.yMin, Mathf.Max(r.yMin, r.yMax - 1)));
                if (goal == here || Data.IsBlocked(goal.x, goal.y)) continue;
                var path = Data.FindPath(here, goal, hop > 12 ? 5000 : 2500);
                if (path.Count == 0 || path.Count > hop * 3) continue;
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

            // Direct keyboard (WASD / Arrow Keys) & virtual joystick movement
            float moveX = 0f;
            float moveY = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveY += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveY -= 1f;
            if (VirtualStick.sqrMagnitude > 0.01f)
            {
                moveX += VirtualStick.x;
                moveY += VirtualStick.y;
            }

            bool manualMoving = (Mathf.Abs(moveX) > 0.05f || Mathf.Abs(moveY) > 0.05f) && Player != null;
            if (manualMoving)
            {
                chaseTarget = null;
                Player.Path = null;
                var inputDir = new Vector2(moveX, -moveY).normalized;
                var step = Player.Speed * dt;
                var desired = Player.Pos + inputDir * step;

                // 8-way directional sliding against collision grid
                var targetTile = new Vector2Int(Mathf.RoundToInt(desired.x), Mathf.RoundToInt(desired.y));
                if (!Data.IsBlocked(targetTile.x, targetTile.y, Flying))
                {
                    Player.Pos = desired;
                }
                else
                {
                    // Try slide X
                    var slideX = new Vector2(desired.x, Player.Pos.y);
                    var tileX = new Vector2Int(Mathf.RoundToInt(slideX.x), Mathf.RoundToInt(slideX.y));
                    if (!Data.IsBlocked(tileX.x, tileX.y, Flying))
                    {
                        Player.Pos = slideX;
                    }
                    else
                    {
                        // Try slide Y
                        var slideY = new Vector2(Player.Pos.x, desired.y);
                        var tileY = new Vector2Int(Mathf.RoundToInt(slideY.x), Mathf.RoundToInt(slideY.y));
                        if (!Data.IsBlocked(tileY.x, tileY.y, Flying))
                        {
                            Player.Pos = slideY;
                        }
                    }
                }

                Player.Moving = true;
                if (Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.y) * 0.75f)
                {
                    Player.Facing = inputDir.x < 0 ? 1 : 2;
                    Player.FaceRight = inputDir.x > 0;
                }
                else
                {
                    Player.Facing = inputDir.y < 0 ? 3 : 0;
                }

                var curTile = TileOf(Player.Pos);
                if (curTile != lastStepTile)
                {
                    lastStepTile = curTile;
                    OnPlayerStep?.Invoke();
                }

                following = true;
                Place(Player);
            }

            UpdateVehicle(dt);
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
                StepActor(actor, dt, actor == Player && manualMoving);
            }
            if (this == null || !isActiveAndEnabled) return;
            if (Player != null && (following || Time.time - lastManualInput > 4f))
            {
                following = true;
                var target = TileToLocal(Player.Pos);
                focus = Vector2.Lerp(focus, target, 1f - Mathf.Exp(-dt * 6f));
            }
            UpdatePressure(dt);
            UpdateTrail(dt);
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

            // name tags follow their actor; tags that would overlap are stacked upwards
            tagScratch.Clear();
            tagResolved.Clear();
            foreach (var actor in Actors)
            {
                if (actor.Tag == null) continue;
                actor.Tag.gameObject.SetActive(!actor.Hidden);
                if (actor.Hidden) continue;
                var local = TileToLocal(actor.Pos);
                tagScratch.Add((actor, LocalToViewport(new Vector2(local.x, local.y - T * .45f + actor.TagHeight + actor.Lift))));
            }
            tagScratch.Sort((a, b) => a.basePos.y.CompareTo(b.basePos.y));
            for (var i = 0; i < tagScratch.Count; i++)
            {
                var cur = tagScratch[i].basePos;
                for (var j = 0; j < i; j++)
                {
                    var prev = tagResolved[j];
                    if (Mathf.Abs(cur.x - prev.x) < 130f && Mathf.Abs(cur.y - prev.y) < 32f) cur.y = prev.y + 34f;
                }
                tagResolved.Add(cur);
                tagScratch[i].actor.Tag.anchoredPosition = cur;
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
            if (!dragged && Vector2.Distance(eventData.position, downPosition) < 28f) return;
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
