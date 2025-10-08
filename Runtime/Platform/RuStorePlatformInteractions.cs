using MirraGames.SDK.Common;
using RuStore.Review;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlatformInteractions))]
    public class RuStorePlatformInteractions : CommonPlatformInteractions {

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

        protected override void ShareGameImpl(string messageText) { }

    }

}