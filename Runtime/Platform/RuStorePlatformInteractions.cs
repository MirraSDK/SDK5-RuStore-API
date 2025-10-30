using MirraGames.SDK.Common;
using RuStore.Review;
using System;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlatformInteractions))]
    public class RuStorePlatformInteractions : CommonPlatformInteractions {

        public RuStorePlatformInteractions(IEventDispatcher eventDispatcher) : base() {
            eventDispatcher.Start += OnStart;
        }

        private void OnStart() {
            try {
                RuStoreReviewManager.Instance.Init();
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(OnStart), exception);
            }
        }

        protected override void RateGameImpl() {
            RuStoreReviewManager.Instance.RequestReviewFlow(
                onFailure: (error) => {
                    Logger.CreateError(this, nameof(RateGameImpl), error);
                },
                onSuccess: () => {
                    Logger.CreateText(this, "RequestReviewFlow", "onSuccess");
                    RuStoreReviewManager.Instance.LaunchReviewFlow(
                        onFailure: (error) => {
                            Logger.CreateError(this, nameof(RateGameImpl), error);
                        },
                        onSuccess: () => {
                            Logger.CreateText(this, "LaunchReviewFlow", "onSuccess");
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