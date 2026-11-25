#if MIRRA_RUSTORE_REVIEW
using MirraGames.SDK.Common;
using RuStore.Review;
using System;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlatformInteractions))]
    public class RuStorePlatformInteractions : CommonPlatformInteractions {

        public RuStorePlatformInteractions(IEventDispatcher eventDispatcher) : base() {
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
