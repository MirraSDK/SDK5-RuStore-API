#if MIRRA_RUSTORE_REVIEW
using MirraGames.SDK.Common;
using RuStore.Review;
using System;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlatformInteractions))]
    public class RuStorePlatformInteractions : CommonPlatformInteractions {

        private readonly RuStorePlatformInteractions_Configuration configuration;

        public RuStorePlatformInteractions(RuStorePlatformInteractions_Configuration configuration, IEventDispatcher eventDispatcher) : base() {
            this.configuration = configuration;
            eventDispatcher.Start += OnStart;
        }

        private void OnStart() {
            try {
                if (!RuStoreReviewManager.Instance.Init()) {
                    Logger.CreateText(this, "Reviews are off: the review manager starts only on an Android device");
                }
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(OnStart), exception);
            }
            CheckForUpdate();
        }

        // In-app updates have no MirraSDK interface of their own, so this provider, which every RuStore build creates, runs them.
        private void CheckForUpdate() {
            RuStoreUpdateMode updateMode = configuration.UpdateMode;
#if MIRRA_RUSTORE_UPDATE
            if (updateMode != RuStoreUpdateMode.Disabled) {
                new RuStoreAppUpdater(updateMode).CheckOnce();
            }
#endif
        }

        protected override void RateGameImpl() {
            if (!RuStoreReviewManager.Instance.IsInitialized) {
                Logger.CreateWarning(this, nameof(RateGame), "the review manager is not initialized");
                return;
            }
            RuStoreReviewManager.Instance.RequestReviewFlow(
                onFailure: (error) => {
                    Logger.CreateError(this, nameof(RuStoreReviewManager.RequestReviewFlow), RuStoreErrorText.Describe(error));
                },
                onSuccess: () => {
                    Logger.CreateText(this, nameof(RuStoreReviewManager.RequestReviewFlow), "onSuccess");
                    RuStoreReviewManager.Instance.LaunchReviewFlow(
                        onFailure: (error) => {
                            Logger.CreateError(this, nameof(RuStoreReviewManager.LaunchReviewFlow), RuStoreErrorText.Describe(error));
                        },
                        onSuccess: () => {
                            Logger.CreateText(this, nameof(RuStoreReviewManager.LaunchReviewFlow), "onSuccess");
                        }
                    );
                }
            );
        }

        protected override void ShareGameImpl(string messageText) {
            Logger.NotImplementedWarning(this, nameof(ShareGameImpl));
        }

    }

}
#endif
