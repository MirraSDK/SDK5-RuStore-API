namespace MirraGames.SDK.RuStore {

    /// <summary>
    /// How a newer version found in RuStore is applied.
    /// </summary>
    public enum RuStoreUpdateMode {

        /// <summary>
        /// Never check for updates.
        /// </summary>
        Disabled,

        /// <summary>
        /// RuStore asks the player, downloads in the background, and the update is installed on the next launch.
        /// </summary>
        Flexible,

        /// <summary>
        /// RuStore shows a full screen update the player has to finish. Falls back to Flexible when RuStore does not allow it.
        /// </summary>
        Immediate,

        /// <summary>
        /// The update downloads without any RuStore dialog and is installed on the next launch.
        /// </summary>
        Silent,

    }

}
