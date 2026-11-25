namespace MirraGames.SDK.RuStore.Editor {

    /// <summary>
    /// One thing that keeps a RuStore build from working, with the way to fix it.
    /// </summary>
    internal sealed class RuStoreSetupProblem {

        public RuStoreSetupProblem(string id, string message, string fix) {
            Id = id;
            Message = message;
            Fix = fix;
        }

        /// <summary>
        /// Stable identifier, for example "settings-missing" or "package-missing:ru.rustore.pay".
        /// </summary>
        public string Id { get; }

        public string Message { get; }

        public string Fix { get; }

    }

}
