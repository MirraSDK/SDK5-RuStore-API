#if MIRRA_RUSTORE_PAY
using RuStore.PayClient;
using System;
using System.Collections.Generic;

namespace MirraGames.SDK.RuStore {

    /// <summary>
    /// Turns RuStore records into the values MirraSDK payments work with.
    /// </summary>
    internal static class RuStorePurchaseRules {

        // RuStore reports prices in minor units, for example kopecks.
        private const float MinorUnitsPerUnit = 100f;

        /// <summary>
        /// Builds the MirraSDK purchase list: one product id per bought unit, repeats included.
        /// MirraSDK compares the count of each id with the supply counter in the save, so a wiped save
        /// gets every listed purchase back. Only paid-in-full product purchases count: a held, refunded,
        /// cancelled or unfinished record is not owned, and subscriptions are not products.
        /// </summary>
        public static string[] CollectProductTags(IEnumerable<IPurchase> storePurchases) {
            List<string> productTags = new();
            if (storePurchases == null) {
                return productTags.ToArray();
            }
            foreach (IPurchase storePurchase in storePurchases) {
                if (storePurchase is not ProductPurchase productPurchase) {
                    continue;
                }
                if (productPurchase.status != ProductPurchaseStatus.CONFIRMED) {
                    continue;
                }
                string productTag = productPurchase.productId?.value;
                if (string.IsNullOrEmpty(productTag)) {
                    continue;
                }
                int quantity = Math.Max(1, productPurchase.quantity?.value ?? 1);
                for (int unit = 0; unit < quantity; unit++) {
                    productTags.Add(productTag);
                }
            }
            return productTags.ToArray();
        }

        /// <summary>
        /// Converts a RuStore price to whole currency units. A product without a price costs 0.
        /// </summary>
        public static float ToPriceFloat(Price price) {
            return price == null ? 0f : price.value / MinorUnitsPerUnit;
        }

    }

}
#endif
