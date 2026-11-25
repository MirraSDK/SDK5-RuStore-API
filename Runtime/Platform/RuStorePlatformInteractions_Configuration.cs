#if MIRRA_RUSTORE_REVIEW
using MirraGames.SDK.Common;
using UnityEngine;

namespace MirraGames.SDK.RuStore {

    [ProviderConfiguration(typeof(RuStorePlatformInteractions))]
    public class RuStorePlatformInteractions_Configuration : PropertyGroup {

        public override string Name => nameof(RuStorePlatformInteractions);

        /// <summary>
        /// How RuStore in-app updates are applied. Has an effect only when the ru.rustore.update package is installed.
        /// </summary>
        [field: SerializeField] public string UpdateModeName { get; private set; } = RuStoreUpdateMode.Flexible.ToString();

        public RuStoreUpdateMode UpdateMode {
            get => UpdateModeName.ToEnumOrDefault(RuStoreUpdateMode.Flexible);
        }

        public override EnumProperty[] GetEnumProperties() {
            return new EnumProperty[] {
                new(
                    nameof(UpdateMode),
                    () => UpdateMode,
                    (value) => UpdateModeName = value.ToString()
                )
            };
        }

    }

}
#endif
