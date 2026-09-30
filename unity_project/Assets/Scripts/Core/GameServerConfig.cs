using UnityEngine;

namespace IOSVN.TuTien.Core
{
    /// <summary>Public URL of the dedicated IPA game server. No credentials belong here.</summary>
    [CreateAssetMenu(menuName = "iOSVN/Online/Game Server Config", fileName = "GameServerConfig")]
    public sealed class GameServerConfig : ScriptableObject
    {
        public string apiBaseUrl = "";
    }
}
