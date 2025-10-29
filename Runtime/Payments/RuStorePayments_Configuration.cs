using MirraGames.SDK.Common;
using UnityEngine;

namespace MirraGames.SDK.RuStore {

    [ProviderConfiguration(typeof(RuStorePayments))]
    public class RuStorePayments_Configuration : PropertyGroup {

        public override string Name => nameof(RuStorePayments);

        [field: SerializeField] public string ProductsJson { get; private set; } = @"{ ""Values"": [""item1"", ""item2"", ""item3"", ""item4""] }";

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new(
                    nameof(ProductsJson),
                    getter: () => { return ProductsJson; },
                    setter: (value) => { ProductsJson = value; }
                )
            };
        }

    }

}