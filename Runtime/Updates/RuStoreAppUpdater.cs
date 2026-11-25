#if MIRRA_RUSTORE_UPDATE
using RuStore.AppUpdate;
using System;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore {

    /// <summary>
    /// Checks RuStore for a newer version once per launch and applies it the way the mode says.
    /// Every failure is logged and ignored: an update problem must never stop the game.
    /// </summary>
    internal sealed class RuStoreAppUpdater {

        private readonly RuStoreUpdateMode mode;

        public RuStoreAppUpdater(RuStoreUpdateMode mode) {
            this.mode = mode;
        }

        public void CheckOnce() {
            try {
                RuStoreAppUpdateManager manager = RuStoreAppUpdateManager.Instance;
                if (!manager.IsInitialized && !manager.Init()) {
                    Logger.CreateText(this, "Updates are off: the update manager starts only on an Android device");
                    return;
                }
                manager.GetAppUpdateInfo(
                    onFailure: (error) => {
                        Logger.CreateError(this, nameof(RuStoreAppUpdateManager.GetAppUpdateInfo), RuStoreErrorText.Describe(error));
                    },
                    onSuccess: Apply
                );
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(CheckOnce), exception);
            }
        }

        private void Apply(AppUpdateInfo updateInfo) {
            try {
                RuStoreAppUpdateManager manager = RuStoreAppUpdateManager.Instance;
                bool isUpdateDownloaded = updateInfo.installStatus == AppUpdateInfo.InstallStatus.DOWNLOADED;
                bool isUpdateAvailable = updateInfo.updateAvailability == AppUpdateInfo.UpdateAvailability.UPDATE_AVAILABLE;
                bool isImmediateAllowed = mode == RuStoreUpdateMode.Immediate && isUpdateAvailable && manager.IsImmediateUpdateAllowed();
                RuStoreUpdateAction action = RuStoreUpdatePlan.Decide(mode, isUpdateDownloaded, isUpdateAvailable, isImmediateAllowed);
                Logger.CreateText(this, "availability:", updateInfo.updateAvailability, "install status:", updateInfo.installStatus, "action:", action);
                switch (action) {
                    case RuStoreUpdateAction.CompleteFlexible:
                        CompleteUpdate(manager, UpdateType.FLEXIBLE);
                        break;
                    case RuStoreUpdateAction.CompleteSilent:
                        CompleteUpdate(manager, UpdateType.SILENT);
                        break;
                    case RuStoreUpdateAction.StartFlexible:
                        StartUpdateFlow(manager, UpdateType.FLEXIBLE);
                        break;
                    case RuStoreUpdateAction.StartSilent:
                        StartUpdateFlow(manager, UpdateType.SILENT);
                        break;
                    case RuStoreUpdateAction.StartImmediate:
                        StartUpdateFlow(manager, UpdateType.IMMEDIATE);
                        break;
                }
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(Apply), exception);
            }
        }

        // A flexible or silent download that ends during this session is left alone.
        // The next launch finds it downloaded and installs it before the player is in the game.
        private void StartUpdateFlow(RuStoreAppUpdateManager manager, UpdateType updateType) {
            manager.StartUpdateFlow(
                updateType,
                onFailure: (error) => {
                    Logger.CreateError(this, nameof(RuStoreAppUpdateManager.StartUpdateFlow), updateType, RuStoreErrorText.Describe(error));
                },
                onSuccess: (result) => {
                    Logger.CreateText(this, nameof(RuStoreAppUpdateManager.StartUpdateFlow), updateType, result);
                }
            );
        }

        private void CompleteUpdate(RuStoreAppUpdateManager manager, UpdateType updateType) {
            manager.CompleteUpdate(
                updateType,
                onFailure: (error) => {
                    Logger.CreateError(this, nameof(RuStoreAppUpdateManager.CompleteUpdate), updateType, RuStoreErrorText.Describe(error));
                }
            );
        }

    }

}
#endif
