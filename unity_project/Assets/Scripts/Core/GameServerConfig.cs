using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>Public endpoints for the IPA game. No credentials belong here.</summary>
    [CreateAssetMenu(menuName = "iOSVN/Online/Game Server Config", fileName = "GameServerConfig")]
    public sealed class GameServerConfig : ScriptableObject
    {
        public string apiBaseUrl = "";
        public string assetCdnBaseUrl = "";
    }
}
