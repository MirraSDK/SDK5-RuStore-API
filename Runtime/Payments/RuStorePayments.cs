using MirraGames.SDK.Common;
using RuStore;
using RuStore.PayClient;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPayments))]
    public class RuStorePayments : CommonPayments {

        private readonly RuStorePayments_Configuration configuration;

        public override bool IsPaymentsAvailable {
            get => isPaymentsAvailable;
        }

        private Dictionary<string, ProductData> Products { get; } = new();

        private HashSet<string> PendingTasks { get; } = new() {
            nameof(RuStorePayClient.Instance.GetPurchaseAvailability),
            nameof(RuStorePayClient.Instance.GetProducts),
            nameof(RuStorePayClient.Instance.GetPurchases)
        };

        private bool isPaymentsAvailable = false;

        public RuStorePayments(RuStorePayments_Configuration configuration, IEventDispatcher eventDispatcher, IData data) : base(data) {
            this.configuration = configuration;
            eventDispatcher.Start += Start;
        }

        private void OnTaskCompleted(string taskName) {
            PendingTasks.Remove(taskName);
            if (PendingTasks.Count == 0) {
                SetInitialized();
            }
        }

        private void OnPaymentsAvailable() {
            isPaymentsAvailable = true;

            string[] productTags = ParseProductTags();

            List<ProductId> productsIds = new();
            foreach (string productTag in productTags) {
                productsIds.Add(new ProductId(productTag));
            }

            RuStorePayClient.Instance.GetProducts(
                productsId: productsIds.ToArray(),
                onFailure: (exception) => {
                    string exceptionJson = JsonUtility.ToJson(exception);
                    Logger.CreateError(this, "GetProducts", exceptionJson);
                    OnTaskCompleted(nameof(RuStorePayClient.Instance.GetProducts));
                },
                onSuccess: (products) => {
                    string productsJson = JsonUtility.ToJson(products);
                    Logger.CreateText(this, "GetProducts", "onSuccess", products);
                    foreach (Product product in products) {
                        string productIdJson = JsonUtility.ToJson(product.productId);
                        Logger.CreateText(this, "GetProducts", "product", productIdJson);
                        string productTag = product.productId.value;
                        int productPrice = (product.price != null) ? product.price.value : -1;
                        string productCurrency = product.currency.value;
                        ProductData productData = new(productTag, productPrice, productCurrency);
                        Products.Add(productTag, productData);
                        Logger.CreateText(this, "GetProducts", productData);
                    }
                    OnTaskCompleted(nameof(RuStorePayClient.Instance.GetProducts));
                }
            );

            GetPurchases((purchases) => {
                Purchases = purchases;
                OnTaskCompleted(nameof(RuStorePayClient.Instance.GetPurchases));
            });
        }

        private void GetPurchases(Action<string[]> onSuccess) {
            RuStorePayClient.Instance.GetPurchases(
                onFailure: (error) => {
                    Logger.CreateError(this, "GetPurchases", error);
                },
                onSuccess: (result) => {
                    List<string> purchases = new();
                    result.ForEach(purchase => {
                        if (purchase is ProductPurchase productPurchase) {
                            purchases.Add(productPurchase.productId.value);
                        }
                        if (purchase is SubscriptionPurchase subscriptionPurchase) {
                            // Subscriptions are not supported
                        }
                    });
                    onSuccess?.Invoke(purchases.ToArray());
                }
            );
        }

        private void OnPaymentsNotAvailable() {
            isPaymentsAvailable = false;
            SetInitialized();
        }

        private void Start() {
            RuStorePayClient.Instance.GetPurchaseAvailability(
                onFailure: (error) => {
                    Logger.CreateError(this, "Payments not available", error);
                    OnPaymentsNotAvailable();
                },
                onSuccess: (result) => {
                    if (result.isAvailable) {
                        Logger.CreateText(this, "Payments available");
                        OnTaskCompleted(nameof(RuStorePayClient.Instance.GetPurchaseAvailability));
                        OnPaymentsAvailable();
                    }
                    else {
                        Logger.CreateError(this, "Payments not available", result.cause);
                        OnPaymentsNotAvailable();
                    }
                }
            );
        }

        private string[] ParseProductTags() {
            string productTags = configuration.ProductTags;
            Logger.CreateText(this, nameof(ParseProductTags), $"'{productTags}'");
            try {
                HashSet<string> splitSet = new(productTags.Split(','));
                HashSet<string> productsSet = new();
                foreach (string value in splitSet) {
                    string productTag = value.Trim();
                    if (!string.IsNullOrEmpty(productTag)) {
                        productsSet.Add(value);
                        Logger.CreateText(this, $"Add product '{productTag}'");
                    }
                    else {
                        Logger.CreateError(this, "Invalid product tag found");
                    }
                }
                return productsSet.ToArray();
            }
            catch (Exception exception) {
                Logger.CreateError(this, exception);
            }
            return Array.Empty<string>();
        }

        protected override ProductData GetProductDataImpl(string productTag) {
            return Products[productTag];
        }

        protected override bool IsAlreadyPurchasedImpl(string productTag) {
            return Purchases.Contains(productTag);
        }

        protected override void PurchaseImpl(string productTag, Action onSuccess, Action onError = null) {
            ProductPurchaseParams parameters = new(
                productId: new ProductId(productTag)
            );
            void onPaymentSuccess(ProductPurchaseResult result) {
                onSuccess?.Invoke();
            }
            void onPaymentError(RuStoreError error) {
                Logger.CreateError(this, nameof(onPaymentError), productTag, error);
                onError?.Invoke();
            }
            RuStorePayClient.Instance.Purchase(parameters, PreferredPurchaseType.ONE_STEP, onPaymentError, onPaymentSuccess);
        }

        protected override void RestorePurchasesImpl(Action<IRestoreData> onRestoreData) {
            GetPurchases((purchases) => {
                Purchases = purchases;
                RestoreData restoreData = new(this, purchases);
                onRestoreData?.Invoke(restoreData);
            });
        }

    }

}