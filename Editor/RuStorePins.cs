namespace MirraGames.SDK.RuStore.Editor {

    /// <summary>
    /// Versions this package is built and tested against. Keep equal to dependencies.json
    /// and to the versionDefines of the assembly definitions.
    /// </summary>
    internal static class RuStorePins {

        public const string MirraSdkPackage = "com.romanlee17.mirrasdk5";
        // 5.2.6 fixes MirraSDK.WaitForProviders, which could wait forever when the player account finished last.
        public const string MirraSdkVersion = "5.2.6";

        public static readonly (string Name, string Version)[] RequiredPackages = {
            ("ru.rustore.core", "10.5.1"),
            ("ru.rustore.pay", "11.2.0"),
            ("ru.rustore.review", "10.5.2"),
        };

        // Checked only when installed.
        public static readonly (string Name, string Version)[] OptionalPackages = {
            ("ru.rustore.update", "10.5.2"),
            ("ru.rustore.remoteconfig", "10.5.2"),
        };

    }

}
