using MirraGames.SDK.Common;
using RuStore.PayClient;
using System;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;

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
                    string resultJson = JsonUtility.ToJson(result);
                    Logger.CreateText(this, "GetUserAuthorizationStatus", resultJson);
                    if (result == UserAuthorizationStatus.AUTHORIZED) {
                        isLoggedIn = true;
                    }
                    SetInitialized();
                },
                onFailure: (error) => {
                    string errorJson = JsonUtility.ToJson(error);
                    Logger.CreateError(this, "GetUserAuthorizationStatus", errorJson);
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