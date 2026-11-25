#if MIRRA_RUSTORE_REMOTECONFIG
using MirraGames.SDK.Common;
using UnityEngine;

namespace MirraGames.SDK.RuStore {

    [ProviderConfiguration(typeof(RuStoreFlags))]
    public class RuStoreFlags_Configuration : PropertyGroup {

        public override string Name => nameof(RuStoreFlags);

        /// <summary>
        /// Remote Config application id from RuStore Console. Empty turns flags off.
        /// </summary>
        [field: SerializeField] public string AppId { get; private set; } = string.Empty;

        /// <summary>
        /// Longest time game startup waits for remote config before continuing with default values.
        /// </summary>
        [field: SerializeField] public int InitTimeoutSeconds { get; private set; } = 5;

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new(
                    nameof(AppId),
                    getter: () => AppId,
                    setter: (value) => AppId = value
                )
            };
        }

        public override IntProperty[] GetIntProperties() {
            return new IntProperty[] {
                new(
                    nameof(InitTimeoutSeconds),
                    getter: () => InitTimeoutSeconds,
                    setter: (value) => InitTimeoutSeconds = value
                )
            };
        }

    }

}
#endif
