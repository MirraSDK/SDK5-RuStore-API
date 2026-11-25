#if MIRRA_RUSTORE_PAY || MIRRA_RUSTORE_REVIEW || MIRRA_RUSTORE_UPDATE || MIRRA_RUSTORE_REMOTECONFIG
using RuStore;

namespace MirraGames.SDK.RuStore {

    internal static class RuStoreErrorText {

        /// <summary>
        /// One readable line for a RuStore error. RuStore models are not serializable,
        /// so JsonUtility prints them as an empty object.
        /// </summary>
        public static string Describe(RuStoreError error) {
            return error == null ? "no details" : $"{error.GetType().Name}: {error.name} {error.description}";
        }

    }

}
#endif
