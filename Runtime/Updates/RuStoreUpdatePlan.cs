namespace MirraGames.SDK.RuStore {

    internal enum RuStoreUpdateAction {
        None,
        CompleteFlexible,
        CompleteSilent,
        StartFlexible,
        StartSilent,
        StartImmediate,
    }

    /// <summary>
    /// Decides what the single update check at startup does. Free of RuStore types, so it can be tested anywhere.
    /// </summary>
    internal static class RuStoreUpdatePlan {

        /// <summary>
        /// A downloaded update is installed first, whatever the mode: this is the launch after the download,
        /// the one moment an install cannot interrupt gameplay. Otherwise an available update starts the flow of the mode.
        /// </summary>
        public static RuStoreUpdateAction Decide(RuStoreUpdateMode mode, bool isUpdateDownloaded, bool isUpdateAvailable, bool isImmediateAllowed) {
            if (mode == RuStoreUpdateMode.Disabled) {
                return RuStoreUpdateAction.None;
            }
            if (isUpdateDownloaded) {
                return mode == RuStoreUpdateMode.Silent ? RuStoreUpdateAction.CompleteSilent : RuStoreUpdateAction.CompleteFlexible;
            }
            if (!isUpdateAvailable) {
                return RuStoreUpdateAction.None;
            }
            switch (mode) {
                case RuStoreUpdateMode.Silent:
                    return RuStoreUpdateAction.StartSilent;
                case RuStoreUpdateMode.Immediate:
                    return isImmediateAllowed ? RuStoreUpdateAction.StartImmediate : RuStoreUpdateAction.StartFlexible;
                default:
                    return RuStoreUpdateAction.StartFlexible;
            }
        }

    }

}
