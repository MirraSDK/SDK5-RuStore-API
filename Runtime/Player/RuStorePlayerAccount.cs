#if MIRRA_RUSTORE_PAY
using MirraGames.SDK.Common;
using RuStore.CoreClient;
using RuStore.PayClient;
using System;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlayerAccount))]
    public class RuStorePlayerAccount : CommonPlayerAccount {

        private const float StartupTimeoutSeconds = 10f;

        private readonly IEventDispatcher eventDispatcher;

        private RuStoreWatchdog startup;
        private bool isLoggedIn = false;

        // A login leaves the game for the RuStore app. Its result is read when the game is back in focus.
        private bool isLoginPending = false;
        private bool hasLeftGameForLogin = false;
        private Action pendingLoginSuccess;
        private Action pendingLoginError;

        public RuStorePlayerAccount(IEventDispatcher eventDispatcher) : base() {
            this.eventDispatcher = eventDispatcher;
            eventDispatcher.Start += Start;
            eventDispatcher.OnApplicationFocus += OnApplicationFocus;
        }

        private void Start() {
            startup = new RuStoreWatchdog(eventDispatcher, StartupTimeoutSeconds, SetInitialized, onTimeout: () => {
                Logger.CreateWarning(this, "RuStore did not answer in time, starting as not logged in");
            });
            RefreshAuthorizationStatus((isAuthorized) => startup.Complete());
        }

        // Reports the fresh status, or the last known one when the store does not answer.
        private void RefreshAuthorizationStatus(Action<bool> onCompleted) {
            try {
                RuStorePayClient.Instance.GetUserAuthorizationStatus(
                    onFailure: (error) => {
                        Logger.CreateError(this, nameof(RuStorePayClient.GetUserAuthorizationStatus), RuStoreErrorText.Describe(error));
                        onCompleted?.Invoke(isLoggedIn);
                    },
                    onSuccess: (status) => {
                        Logger.CreateText(this, nameof(RuStorePayClient.GetUserAuthorizationStatus), status);
                        isLoggedIn = status == UserAuthorizationStatus.AUTHORIZED;
                        onCompleted?.Invoke(isLoggedIn);
                    }
                );
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(RefreshAuthorizationStatus), exception);
                onCompleted?.Invoke(isLoggedIn);
            }
        }

        private void OnApplicationFocus(bool hasFocus) {
            if (!isLoginPending) {
                return;
            }
            if (!hasFocus) {
                hasLeftGameForLogin = true;
                return;
            }
            if (!hasLeftGameForLogin) {
                return;
            }
            Action onLoginSuccess = pendingLoginSuccess;
            Action onLoginError = pendingLoginError;
            ClearPendingLogin();
            RefreshAuthorizationStatus((isAuthorized) => {
                if (isAuthorized) {
                    onLoginSuccess?.Invoke();
                }
                else {
                    onLoginError?.Invoke();
                }
            });
        }

        private void ClearPendingLogin() {
            isLoginPending = false;
            hasLeftGameForLogin = false;
            pendingLoginSuccess = null;
            pendingLoginError = null;
        }

        // RuStore exposes only whether the player is signed in, no profile.
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
            if (isLoggedIn) {
                onLoginSuccess?.Invoke();
                return;
            }
            if (!RuStoreCoreClient.Instance.IsRuStoreInstalled()) {
                Logger.CreateWarning(this, nameof(InvokeLogin), "RuStore is not installed on this device");
                onLoginError?.Invoke();
                return;
            }
            if (isLoginPending) {
                // The newer request owns the result; the older one ends as failed.
                Action replacedLoginError = pendingLoginError;
                ClearPendingLogin();
                replacedLoginError?.Invoke();
            }
            isLoginPending = true;
            pendingLoginSuccess = onLoginSuccess;
            pendingLoginError = onLoginError;
            RuStoreCoreClient.Instance.openRuStoreAuthorization();
        }

        protected override bool IsLoggedInImpl() {
            return isLoggedIn;
        }

    }

}
#endif
