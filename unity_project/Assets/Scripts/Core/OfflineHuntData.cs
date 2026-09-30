using System;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    [Serializable]
    public sealed class OfflineHuntCatalogData
    {
        public string sourceMapId;
        public string sourceMapName;
        public OfflineMonsterData[] monsters;
        public OfflineItemData[] items;

        public static OfflineHuntCatalogData Load()
        {
            var asset = Resources.Load<TextAsset>("OfflineHuntCatalog");
            return asset == null ? null : JsonUtility.FromJson<OfflineHuntCatalogData>(asset.text);
        }
    }

    [Serializable]
    public sealed class OfflineMonsterData
    {
        public string id;
        public string name;
        public int realm;
        public string element;
        public int hp;
        public OfflineDropData[] drops;
    }

    [Serializable]
    public sealed class OfflineDropData
    {
        public string kind;
        public string id;
        public float rate;
        public int qty;
        public int min;
        public int max;
    }

    [Serializable]
    public sealed class OfflineItemData
    {
        public string id;
        public string name;
        public string kind;
        public string tier;
        public string desc;
    }
}
