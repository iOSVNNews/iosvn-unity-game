using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// "Trong thành": a painted terraced city (one panorama per province biome) with brush-tag
    /// hotspots for every service — teleport, market, rankings, bounties, healer, crafting, sect...
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        [Serializable] private sealed class CityHotspot { public string id; public string label; public float x; public float y; public float w; public float h; }
        [Serializable] private sealed class CityLayout { public int w = 800; public int h = 360; public CityHotspot[] hotspots; }

        private GameObject cityRoot;
        private static CityLayout cityLayout;
        private static readonly Dictionary<string, Texture2D> CityPaintings = new Dictionary<string, Texture2D>();
        private string cityTownId;

        private static readonly Dictionary<string, string> CityIcons = new Dictionary<string, string>
        {
            { "rank", "power" }, { "lord", "location" }, { "sect", "tong_mon" }, { "bounty", "scroll" }, { "craft", "dan_duoc" },
            { "codex", "thu_cac" }, { "heal", "y_quan" }, { "tavern", "ban_be" }, { "shop", "coin" }, { "teleport", "teleport" },
            { "market", "phuong_thi" }, { "inbox", "mail" }, { "pvp", "swords" }, { "exit", "road" },
        };

        private Texture2D CityPainting(string biome)
        {
            if (CityPaintings.TryGetValue(biome, out var cached) && cached != null) return cached;
            var asset = Resources.Load<TextAsset>("World/city_" + biome) ?? Resources.Load<TextAsset>("World/city_verdant");
            if (asset == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!texture.LoadImage(asset.bytes, true)) return null;
            texture.filterMode = FilterMode.Point;
            CityPaintings[biome] = texture;
            return texture;
        }

        private void ShowCity(string townId)
        {
            if (hub.IsNull) { RefreshHub(() => ShowCity(townId)); return; }
            if (cityLayout == null)
            {
                var asset = Resources.Load<TextAsset>("World/city_layout");
                cityLayout = asset != null ? JsonUtility.FromJson<CityLayout>(asset.text) : new CityLayout { hotspots = Array.Empty<CityHotspot>() };
            }
            var town = hub["town"];
            var mapId = town["mapId"].Str();
            var data = WorldMapData.Load(mapId);
            var biome = data?.biome ?? "verdant";
            var painting = CityPainting(biome);
            ClearContent();
            ClearBattleScene();
            cityTownId = townId;
            // panorama under the safe-area UI
            var root = new GameObject("CityPanorama", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
            root.SetParent(backgroundRoot, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
            root.GetComponent<Image>().color = new Color32(232, 226, 214, 255);
            cityRoot = root.gameObject;
            var view = new GameObject("Painting", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
            view.SetParent(root, false);
            view.anchorMin = view.anchorMax = new Vector2(.5f, .5f);
            view.pivot = new Vector2(.5f, .5f);
            var raw = view.GetComponent<RawImage>();
            raw.texture = painting;
            raw.raycastTarget = false;
            var pan = root.gameObject.AddComponent<CityPan>();
            pan.View = view;
            pan.Aspect = cityLayout.w / (float)Mathf.Max(1, cityLayout.h);
            pan.Refit();
            // drifting mist over the city
            for (var i = 0; i < 6; i++)
            {
                var cloud = InkUi.Simple(view, "Mist" + i, InkUi.Cloud, new Color(1, 1, 1, .22f + i % 3 * .06f), new Vector2(140 + i * 40, 60 + i * 12));
                cloud.rectTransform.anchorMin = cloud.rectTransform.anchorMax = new Vector2((i * .19f) % 1f, .25f + (i % 3) * .2f);
                cloud.gameObject.AddComponent<UiDrift>().speed = 6f + i * 2.5f;
            }
            foreach (var spot in cityLayout.hotspots ?? Array.Empty<CityHotspot>()) AddCityHotspot(view, spot);
            // HUD
            var hud = HudRoot();
            BuildOverlays();
            BuildAvatarCard(hud);
            var title = Anchored("CityTitle", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -140), new Vector2(760, -18));
            var titleBrush = title.gameObject.AddComponent<Image>();
            titleBrush.sprite = InkUi.Brush;
            titleBrush.type = Image.Type.Sliced;
            titleBrush.raycastTarget = false;
            var townName = Clean(town["name"].Str());
            if (townId != town["id"].Str()) townName = Clean(data?.Town(townId)?.name ?? townName);
            var heading = AnchoredText(title, "Name", townName, ModernUi.Display, 46, HudCream, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(46, 0), new Vector2(-30, -14));
            UiGradient.Apply(heading, AuthGoldTop, AuthGoldBottom);
            AnchoredText(title, "Lord", $"{Clean(town["lordTitle"].Str("Thành chủ"))}: {Clean(town["lordName"].Str("—"))} · {Clean(town["lordRealmName"].Str())}   ·   Yêu cầu {Clean(town["realmMinName"].Str("Phàm Nhân"))}",
                ModernUi.Regular, 21, new Color32(220, 212, 196, 255), TextAnchor.LowerLeft, Vector2.zero, Vector2.one, new Vector2(48, 16), new Vector2(-30, 0));
            var leave = Anchored("Leave", hud, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-330, -118), new Vector2(-24, -24));
            PillButton(leave, "Rời thành", "arrowLeft", false, LeaveCity);
            var leaveImage = leave.GetComponent<Image>();
            if (leaveImage != null) leaveImage.color = new Color32(20, 22, 24, 220);
        }

        private void AddCityHotspot(RectTransform view, CityHotspot spot)
        {
            var w = cityLayout.w;
            var h = cityLayout.h;
            var rect = Anchored("Spot_" + spot.id, view,
                new Vector2(spot.x / w, 1f - (spot.y + spot.h) / h), new Vector2((spot.x + spot.w) / w, 1f - spot.y / h), Vector2.zero, Vector2.zero);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            var glow = Anchored("Glow", rect, new Vector2(-.15f, -.1f), new Vector2(1.15f, 1.1f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            glow.sprite = InkUi.Glow;
            glow.color = new Color(1f, .92f, .7f, 0f);
            glow.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var id = spot.id;
            button.onClick.AddListener(() => OpenCityService(id));
            var trigger = rect.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => glow.color = new Color(1f, .92f, .7f, .45f));
            trigger.triggers.Add(down);
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => glow.color = new Color(1f, .92f, .7f, 0f));
            trigger.triggers.Add(up);
            // brush tag above the building
            var tag = InkUi.Tag(rect, spot.label, 22);
            tag.anchorMin = tag.anchorMax = new Vector2(.5f, 1f);
            tag.pivot = new Vector2(.5f, 0f);
            tag.anchoredPosition = new Vector2(0, 6);
            if (CityIcons.TryGetValue(spot.id, out var iconId))
            {
                var icon = Anchored("Icon", tag, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(-14, -22), new Vector2(30, 22)).gameObject.AddComponent<Image>();
                icon.sprite = UiPixelIcon(iconId);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
        }

        private void OpenCityService(string id)
        {
            switch (id)
            {
                case "exit": LeaveCity(); break;
                case "teleport": OpenTeleportScreen(false); break;
                case "market": OpenMarketScreen(); break;
                case "shop": OpenShopScreen(); break;
                case "rank": OpenRankScreen(); break;
                case "bounty": OpenBountyScreen(); break;
                case "heal": OpenHealScreen(); break;
                case "craft": OpenCraftScreen(); break;
                case "codex": OpenCodexScreen(); break;
                case "sect": OpenSectScreen(); break;
                case "tavern": OpenSocialScreen(); break;
                case "inbox": OpenInboxScreen(); break;
                case "pvp": OpenPvpScreen(); break;
                case "lord": OpenLordScreen(); break;
                default: Toast("Khu vực đang được tu sửa.", true); break;
            }
        }

        /// <summary>Back from a city service screen.</summary>
        private void BackToCity()
        {
            if (!string.IsNullOrEmpty(cityTownId)) ShowCity(cityTownId); else ShowWorld();
        }

        private void LeaveCity()
        {
            var data = WorldMapData.Load(hub["town"]["mapId"].Str());
            var town = data?.Town(cityTownId);
            cityTownId = null;
            if (town?.spawn != null) worldReturnTile = new Vector2(town.spawn[0], town.spawn[1]);
            ShowWorld();
        }
    }

    /// <summary>Keeps the city panorama covering the screen height and lets the player pan sideways.</summary>
    internal sealed class CityPan : MonoBehaviour, IDragHandler
    {
        public RectTransform View;
        public float Aspect = 800f / 360f;
        private float offset;
        private Vector2 lastSize;

        public void Refit()
        {
            var root = (RectTransform)transform;
            var size = root.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            var height = size.y;
            var width = height * Aspect;
            if (width < size.x) { width = size.x; height = width / Aspect; }
            View.sizeDelta = new Vector2(width, height);
            var limit = Mathf.Max(0, (width - size.x) * .5f);
            offset = Mathf.Clamp(offset, -limit, limit);
            View.anchoredPosition = new Vector2(offset, 0);
            lastSize = size;
        }

        private void Update()
        {
            var size = ((RectTransform)transform).rect.size;
            if (size != lastSize) Refit();
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = transform.lossyScale.x <= 0 ? 1f : transform.lossyScale.x;
            offset += eventData.delta.x / scale;
            Refit();
        }
    }

    /// <summary>Slow horizontal drift (mist, clouds) within the parent's width.</summary>
    internal sealed class UiDrift : MonoBehaviour
    {
        public float speed = 8f;
        private void Update()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            var p = rect.anchoredPosition;
            p.x += speed * Time.deltaTime;
            if (p.x > parent.rect.width * .6f) p.x = -parent.rect.width * .6f;
            rect.anchoredPosition = p;
        }
    }
}
