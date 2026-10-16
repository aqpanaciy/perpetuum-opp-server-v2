using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using System.ComponentModel;

namespace Perpetuum
{
    public class GlobalConfiguration
    {
        public string ListenerIP { get; set; }
        public int ListenerPort { get; set; }

        public string GameRoot { get; set; }
        public string WebServiceIP { get; set; }
        public string PersonalConfig { get; set; }
        public string ConnectionString { get; set; }
        public string RelayName => "relay";

        public bool EnableUpnp { get; set; }

        public int SteamAppID { get; set; }
        public byte[] SteamKey { get; set; }

        public string ResourceServerURL { get; set; }

        public bool EnableDev { get; set; }

        public CorporationConfiguration Corporation { get; set; }

        public bool StartServerInAdminOnlyMode { get; set; }

        // Default NIC value for new player.
        [DefaultValue(500000), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int StartCredit { get; set; }

        // Default NIC per level value for new player.
        [DefaultValue(125000), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int LevelCredit { get; set; }

        // Default EP value for new player.
        [DefaultValue(40000), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int StartEP { get; set; }

        // Default camouflage bonus value.
        [DefaultValue(5), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int CamouflageBonus { get; set; }

        public string WebHookId { get; set; }

        public string WebHookOAuth { get; set; }

        public string DiscordBotToken { get; set; }

        public string OpHelpChannelId { get; set; }

        // Default roaming mode.
        [DefaultValue(RoamingState.RoamingMode.Default)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        public RoamingState.RoamingMode RoamingMode { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent pathfinding operations allowed.
        /// </summary>
        [DefaultValue(0), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ConcurrentPathFinds { get; set; }

        /// <summary>
        /// URL for transmitting logger messages (http://localhost:4100/loki/api/v1/push)
        /// </summary>
        [DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string LokiURL { get; set; }

        /// <summary>
        /// File system path for internal logger messages (C:\\PerpetuumServer\\data\\log\\loki.log)
        /// </summary>
        [DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string LokiFallbackPath { get; set; }

        /// <summary>
        /// File system path for layer resource (C:\\PerpetuumResoruce\\lang0000\\layers\\GAMMA_LAYERS_NEW)
        /// </summary>
        [DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string ResourcePath { get; set; }
    }
}
