#if MIRRA_RUSTORE_PAY
using MirraGames.SDK.Common;
using RuStore;
using RuStore.PayClient;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPayments))]
    public class RuStorePayments : CommonPayments {

        // Startup asks the store three things. If it stays silent this long, the game starts without them.
        private const float StartupTimeoutSeconds = 10f;

        private readonly RuStorePayments_Configuration configuration;
        private readonly IEventDispatcher eventDispatcher;
        private readonly Dictionary<string, ProductData> products = new();

        private RuStoreWatchdog startup;
        private int pendingStartupRequests;
        private bool isPaymentsAvailable = false;

        public override bool IsPaymentsAvailable {
            get => isPaymentsAvailable;
        }

        public RuStorePayments(RuStorePayments_Configuration configuration, IEventDispatcher eventDispatcher, IData data) : base(data) {
            this.configuration = configuration;
            this.eventDispatcher = eventDispatcher;
            Purchases = Array.Empty<string>();
            eventDispatcher.Start += Start;
        }

        private void Start() {
            startup = new RuStoreWatchdog(eventDispatcher, StartupTimeoutSeconds, SetInitialized, onTimeout: () => {
                Logger.CreateWarning(this, "RuStore did not answer in time, starting with what is loaded so far");
            });
            try {
                RuStorePayClient.Instance.GetPurchaseAvailability(
                    onFailure: (error) => {
                        Logger.CreateError(this, "Payments not available", Describe(error));
                        startup.Complete();
                    },
                    onSuccess: (result) => {
                        if (!result.isAvailable) {
                            Logger.CreateWarning(this, "Payments not available", Describe(result.cause));
                            startup.Complete();
                            return;
                        }
                        Logger.CreateText(this, "Payments available");
                        isPaymentsAvailable = true;
                        LoadProductsAndPurchases();
                    }
                );
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(Start), exception);
                startup.Complete();
            }
        }

        // Both requests end startup together. Each one reports back on failure too, so neither can hold it up.
        private void LoadProductsAndPurchases() {
            pendingStartupRequests = 2;
            RequestProducts(CompleteStartupRequest);
            RequestPurchases((isRefreshed) => CompleteStartupRequest());
        }

        private void CompleteStartupRequest() {
            pendingStartupRequests--;
            if (pendingStartupRequests <= 0) {
                startup.Complete();
            }
        }

        private void RequestProducts(Action onCompleted) {
            ProductId[] productIds = ReadProductIds();
            if (productIds.Length == 0) {
                Logger.CreateWarning(this, "No product ids in", nameof(RuStorePayments_Configuration.ProductsJson));
                onCompleted?.Invoke();
                return;
            }
            RuStorePayClient.Instance.GetProducts(
                productsId: productIds,
                onFailure: (error) => {
                    Logger.CreateError(this, nameof(RuStorePayClient.GetProducts), Describe(error));
                    onCompleted?.Invoke();
                },
                onSuccess: (loadedProducts) => {
                    foreach (Product product in loadedProducts) {
                        string productTag = product.productId?.value;
                        if (string.IsNullOrEmpty(productTag)) {
                            continue;
                        }
                        float price = RuStorePurchaseRules.ToPriceFloat(product.price);
                        string currency = product.currency?.value ?? string.Empty;
                        ProductData productData = new(productTag, price, currency);
                        products[productTag] = productData;
                        Logger.CreateText(this, nameof(RuStorePayClient.GetProducts), productData);
                    }
                    onCompleted?.Invoke();
                }
            );
        }

        // Replaces the purchase list with what the store lists now. Reports whether the store answered.
        private void RequestPurchases(Action<bool> onCompleted) {
            RuStorePayClient.Instance.GetPurchases(
                onFailure: (error) => {
                    Logger.CreateError(this, nameof(RuStorePayClient.GetPurchases), Describe(error));
                    onCompleted?.Invoke(false);
                },
                onSuccess: (storePurchases) => {
                    Purchases = RuStorePurchaseRules.CollectProductTags(storePurchases);
                    Logger.CreateText(this, nameof(RuStorePayClient.GetPurchases), "confirmed units:", Purchases.Length);
                    onCompleted?.Invoke(true);
                }
            );
        }

        private ProductId[] ReadProductIds() {
            List<ProductId> productIds = new();
            try {
                StringArray productsArray = JsonUtility.FromJson<StringArray>(configuration.ProductsJson);
                if (productsArray?.Values == null) {
                    return productIds.ToArray();
                }
                HashSet<string> seenTags = new();
                foreach (string value in productsArray.Values) {
                    string productTag = value?.Trim();
                    if (!string.IsNullOrEmpty(productTag) && seenTags.Add(productTag)) {
                        productIds.Add(new ProductId(productTag));
                    }
                }
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(RuStorePayments_Configuration.ProductsJson), "is not valid JSON:", exception.Message);
            }
            return productIds.ToArray();
        }

        private void AddPurchasedUnits(string productTag, int quantity) {
            string[] updatedPurchases = new string[Purchases.Length + quantity];
            Array.Copy(Purchases, updatedPurchases, Purchases.Length);
            for (int index = Purchases.Length; index < updatedPurchases.Length; index++) {
                updatedPurchases[index] = productTag;
            }
            Purchases = updatedPurchases;
        }

        private static string Describe(RuStoreError error) {
            return error == null ? "no details" : $"{error.GetType().Name}: {error.name} {error.description}";
        }

        protected override ProductData GetProductDataImpl(string productTag) {
            if (products.TryGetValue(productTag, out ProductData productData)) {
                return productData;
            }
            // Never null: games read the price straight from the result.
            Logger.CreateWarning(this, nameof(GetProductData), "unknown product", productTag);
            return new ProductData(productTag, 0, string.Empty);
        }

        protected override bool IsAlreadyPurchasedImpl(string productTag) {
            return Array.IndexOf(Purchases, productTag) >= 0;
        }

        protected override void PurchaseImpl(string productTag, Action onSuccess, Action onError = null) {
            ProductPurchaseParams parameters = new(
                productId: new ProductId(productTag)
            );
            RuStorePayClient.Instance.Purchase(
                parameters: parameters,
                preferredPurchaseType: PreferredPurchaseType.ONE_STEP,
                onFailure: (error) => {
                    if (error is RuStorePaymentException.ProductPurchaseCancelled) {
                        Logger.CreateText(this, nameof(Purchase), "cancelled by the player", productTag);
                    }
                    else {
                        Logger.CreateError(this, nameof(Purchase), productTag, Describe(error));
                    }
                    onError?.Invoke();
                },
                onSuccess: (result) => {
                    Logger.CreateText(this, nameof(Purchase), productTag, result);
                    // Listed at once, so the product counts as bought before the next store refresh.
                    AddPurchasedUnits(productTag, Math.Max(1, result.quantity?.value ?? 1));
                    onSuccess?.Invoke();
                }
            );
        }

        protected override void RestorePurchasesImpl(Action<IRestoreData> onRestoreData) {
            RequestPurchases((isRefreshed) => {
                if (!isRefreshed) {
                    Logger.CreateWarning(this, nameof(RestorePurchases), "store list not refreshed, using the last known purchases");
                }
                RestoreData restoreData = new(this, Purchases);
                onRestoreData?.Invoke(restoreData);
            });
        }

    }

}
#endif
