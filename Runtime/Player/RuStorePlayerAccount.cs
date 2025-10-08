using MirraGames.SDK.Common;
using RuStore.PayClient;
using System;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlayerAccount))]
    public class RuStorePlayerAccount : CommonPlayerAccount {

        private bool isLoggedIn = false;

        public RuStorePlayerAccount(IEventDispatcher eventDispatcher) : base() {
            eventDispatcher.Start += Start;
        }

        private void Start() {
            RuStorePayClient.Instance.GetUserAuthorizationStatus(
                onSuccess: (result) => {
                    Logger.CreateText(this, "GetUserAuthorizationStatus", result);
                    if (result == UserAuthorizationStatus.AUTHORIZED) {
                        isLoggedIn = true;
                    }
                    SetInitialized();
                },
                onFailure: (error) => {
                    Logger.CreateError(this, "GetUserAuthorizationStatus", error);
                    SetInitialized();
                }
            );
        }

        protected override string GetDisplayNameImpl() {
            return default;
        }

        protected override string GetFirstNameImpl() {
            return default;
        }

        protected override string GetLastNameImpl() {
            return default;
        }

        protected override string GetUniqueIdImpl() {
            return default;
        }

        protected override string GetUsernameImpl() {
            return default;
        }

        protected override void InvokeLoginImpl(Action onLoginSuccess = null, Action onLoginError = null) {
            Logger.NotImplementedWarning(this, nameof(InvokeLoginImpl));
        }

        protected override bool IsLoggedInImpl() {
            return isLoggedIn;
        }

    }

}