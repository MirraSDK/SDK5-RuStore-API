using MirraGames.SDK.Common;
using UnityEngine;

namespace MirraGames.SDK.RuStore {

    [ProviderConfiguration(typeof(RuStorePayments))]
    public class RuStorePayments_Configuration : PropertyGroup {

        public override string Name => nameof(RuStorePayments);

        [field: SerializeField] public string ProductTags { get; private set; } = "";

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new(
                    nameof(ProductTags),
                    getter: () => { return ProductTags; },
                    setter: (value) => { ProductTags = value; }
                )
            };
        }

    }

}