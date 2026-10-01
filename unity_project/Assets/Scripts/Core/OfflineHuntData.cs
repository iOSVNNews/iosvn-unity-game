using System;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    [Serializable]
    public sealed class OfflineHuntCatalogData
    {
        public string sourceMapId;
        public string sourceMapName;
        public string sourceName;
        public string[] realmNames;
        public OfflineClassData[] classes;
        public OfflineElementData[] elements;
        public OfflineSkillData[] skills;
        public OfflineMonsterData[] monsters;
        public OfflineItemData[] items;

        public static OfflineHuntCatalogData Load()
        {
            var asset = Resources.Load<TextAsset>("OfflineHuntCatalog");
            return asset == null ? null : JsonUtility.FromJson<OfflineHuntCatalogData>(asset.text);
        }
    }

    [Serializable]
    public sealed class OfflineClassData
    {
        public string id;
        public string name;
        public string weapon;
        public string weaponName;
        public string statName;
    }

    [Serializable]
    public sealed class OfflineElementData
    {
        public string id;
        public string name;
        public string beats;
        public string effect;
    }

    [Serializable]
    public sealed class OfflineSkillData
    {
        public string id;
        public string mon;
        public string name;
        public string icon;
        public string kind;
        public int realm;
        public int mp;
        public int cd;
        public string desc;
    }

    [Serializable]
    public sealed class OfflineMonsterData
    {
        public string id;
        public string name;
        public int realm;
        public string element;
        public int hp;
        public int atk;
        public int def;
        public int spd;
        public bool small;
        public bool worldBoss;
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
