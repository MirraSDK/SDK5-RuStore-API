using MirraGames.SDK.Common;
using RuStore.Review;
using System;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;

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
                    string errorJson = JsonUtility.ToJson(error);
                    Logger.CreateError(this, nameof(RateGameImpl), errorJson);
                },
                onSuccess: () => {
                    Logger.CreateText(this, "RequestReviewFlow", "onSuccess");
                    RuStoreReviewManager.Instance.LaunchReviewFlow(
                        onFailure: (error) => {
                            string errorJson = JsonUtility.ToJson(error);
                            Logger.CreateError(this, nameof(RateGameImpl), errorJson);
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