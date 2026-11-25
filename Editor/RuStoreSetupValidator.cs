using MirraGames.SDK.Common;
using MirraGames.SDK.Editor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using Logger = MirraGames.SDK.Common.Logger;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#if MIRRA_RUSTORE_PAY
using PayClientManifestPatcher = RuStore.Editor.Manifest.PayClientManifestPatcher;
using PayClientSettings = RuStore.Editor.PayClientSettings;
using RuStoreModuleSettings = RuStore.Editor.RuStoreModuleSettings;
#endif

namespace MirraGames.SDK.RuStore.Editor {

    /// <summary>
    /// Finds everything in the project that would make a RuStore build fail or ship without working payments.
    /// Compiles and runs with no RuStore package installed, because that is one of the states it reports.
    /// </summary>
    internal static class RuStoreSetupValidator {

        public const string BuildConfigurationName = "RuStore";
        public const string AndroidPluginsFolder = "Assets/Plugins/Android";
        public const string ManifestPath = AndroidPluginsFolder + "/AndroidManifest.xml";
        public const string LegacyActivityPath = AndroidPluginsFolder + "/RuStoreIntentFilterActivity.java";
        public const string LegacyActivityClass = "ru.rustore.unitysdk.RuStoreIntentFilterActivity";
        public const string ActivityClass = "com.unity3d.player.UnityPlayerActivity";
        public const string GameActivityClass = "com.unity3d.player.UnityPlayerGameActivity";
        public const string MainAction = "android.intent.action.MAIN";
        public const string LauncherCategory = "android.intent.category.LAUNCHER";
        public const int MinimumApiLevel = 24;

        public static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

        private const string LegacyMavenHost = "artifactory-external.vkpartner.ru";
        private const string ResolverTypeName = "GooglePlayServices.PlayServicesResolver";
        private const string ToolkitFix = "Open MirraSDK5 > Open Toolkit, select the package list, choose 'RuStore API for MirraSDK5' and press the install or update button.";
        private const string FixMenuHint = "Run MirraSDK5 > RuStore > Fix setup.";

        /// <summary>
        /// True when the MirraSDK build configuration is RuStore, the only case the build guard acts on.
        /// </summary>
        public static bool IsRuStoreBuildConfiguration() {
            try {
                return PreferencesEditor.CreateEditor().GetBuildConfigurationName() == BuildConfigurationName;
            }
            catch (Exception exception) {
                Logger.CreateWarning(nameof(RuStoreSetupValidator), "Could not read the MirraSDK build configuration:", exception.Message);
                return false;
            }
        }

        public static List<RuStoreSetupProblem> Validate() {
            List<RuStoreSetupProblem> problems = new();
            CheckPackages(problems);
            CheckDependencyManager(problems);
            CheckPlayerSettings(problems);
            CheckLegacyFiles(problems);
            XDocument manifest = CheckManifestFile(problems);
            CheckPaySetup(problems, manifest);
            return problems;
        }

        public static string FormatReport(IReadOnlyList<RuStoreSetupProblem> problems) {
            if (problems.Count == 0) {
                return "RuStore setup is complete.";
            }
            StringBuilder report = new();
            report.Append($"RuStore setup has {problems.Count} problem(s):");
            for (int index = 0; index < problems.Count; index++) {
                report.Append($"\n{index + 1}. {problems[index].Message}\n   Fix: {problems[index].Fix}");
            }
            return report.ToString();
        }

        /// <summary>
        /// Loads the custom main manifest, or returns null when it is missing or not valid XML.
        /// </summary>
        public static XDocument LoadManifest() {
            if (!File.Exists(ManifestPath)) {
                return null;
            }
            try {
                return XDocument.Load(ManifestPath);
            }
            catch (Exception) {
                return null;
            }
        }

        public static IEnumerable<XElement> FindActivities(XDocument manifest, string className) {
            return manifest.Descendants("activity").Where(activity => (string)activity.Attribute(AndroidNamespace + "name") == className);
        }

        public static bool IsLauncherFilter(XElement intentFilter) {
            bool hasMainAction = intentFilter.Elements("action").Any(action => (string)action.Attribute(AndroidNamespace + "name") == MainAction);
            bool hasLauncherCategory = intentFilter.Elements("category").Any(category => (string)category.Attribute(AndroidNamespace + "name") == LauncherCategory);
            return hasMainAction && hasLauncherCategory;
        }

        public static bool HasLauncherActivity(XDocument manifest) {
            return manifest.Descendants("activity").Any(activity => activity.Elements("intent-filter").Any(IsLauncherFilter));
        }

        public static bool IsDependencyManagerInstalled() {
            return FindType(ResolverTypeName) != null;
        }

        public static Type FindType(string fullName) {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type type = assembly.GetType(fullName, false);
                if (type != null) {
                    return type;
                }
            }
            return null;
        }

        private static void CheckPackages(List<RuStoreSetupProblem> problems) {
            Dictionary<string, string> installedVersions = new();
            foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages()) {
                installedVersions[package.name] = package.version;
            }

            if (installedVersions.TryGetValue(RuStorePins.MirraSdkPackage, out string mirraSdkVersion)
                && CompareVersions(mirraSdkVersion, RuStorePins.MirraSdkVersion) < 0) {
                problems.Add(new RuStoreSetupProblem(
                    "mirrasdk-outdated",
                    $"MirraSDK5 {mirraSdkVersion} is installed, RuStore API needs {RuStorePins.MirraSdkVersion} or newer.",
                    "Update MirraSDK5 in MirraSDK5 > Open Toolkit, then install the RuStore packages from the same list."));
            }

            foreach ((string name, string version) in RuStorePins.RequiredPackages) {
                if (!installedVersions.TryGetValue(name, out string installedVersion)) {
                    problems.Add(new RuStoreSetupProblem($"package-missing:{name}", $"Package {name} {version} is not installed.", ToolkitFix));
                }
                else if (CompareVersions(installedVersion, version) < 0) {
                    problems.Add(new RuStoreSetupProblem($"package-outdated:{name}",
                        $"Package {name} {installedVersion} is installed, {version} or newer is needed.", ToolkitFix));
                }
            }

            foreach ((string name, string version) in RuStorePins.OptionalPackages) {
                if (installedVersions.TryGetValue(name, out string installedVersion) && CompareVersions(installedVersion, version) < 0) {
                    problems.Add(new RuStoreSetupProblem($"package-outdated:{name}",
                        $"Package {name} {installedVersion} is installed, {version} or newer is needed.", ToolkitFix));
                }
            }
        }

        // RuStore libraries are Maven artifacts. Without External Dependency Manager nothing adds them to the build.
        private static void CheckDependencyManager(List<RuStoreSetupProblem> problems) {
            if (!IsDependencyManagerInstalled()) {
                problems.Add(new RuStoreSetupProblem(
                    "edm4u-missing",
                    "External Dependency Manager for Unity is not installed, so the RuStore Android libraries are not added to the build.",
                    "Install com.google.external-dependency-manager (for example from OpenUPM), then run Assets > External Dependency Manager > Android Resolver > Force Resolve."));
            }
        }

        private static void CheckPlayerSettings(List<RuStoreSetupProblem> problems) {
            int minimumApiLevel = (int)PlayerSettings.Android.minSdkVersion;
            if (minimumApiLevel < MinimumApiLevel) {
                problems.Add(new RuStoreSetupProblem(
                    "min-api-level",
                    $"Minimum API Level is {minimumApiLevel}, RuStore needs {MinimumApiLevel} or higher.",
                    $"Set Player Settings > Android > Other Settings > Minimum API Level to {MinimumApiLevel} or higher."));
            }
#if UNITY_2023_1_OR_NEWER
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.Activity) {
                problems.Add(new RuStoreSetupProblem(
                    "entry-point-game-activity",
                    $"Application Entry Point is {PlayerSettings.Android.applicationEntry}. RuStore payments work only with Activity.",
                    $"{FixMenuHint} It sets Player Settings > Android > Other Settings > Application Entry Point to Activity."));
            }
#endif
        }

        // Files that RuStore API 1.x asked projects to import. They point at a Maven host that no longer exists
        // and at an activity class that RuStore Pay replaced.
        private static void CheckLegacyFiles(List<RuStoreSetupProblem> problems) {
            if (File.Exists(LegacyActivityPath)) {
                problems.Add(new RuStoreSetupProblem(
                    "legacy-activity-file",
                    $"{LegacyActivityPath} is left from RuStore API 1.x and does not compile against RuStore Pay 11.",
                    FixMenuHint));
            }
            if (!Directory.Exists(AndroidPluginsFolder)) {
                return;
            }
            foreach (string path in Directory.GetFiles(AndroidPluginsFolder)) {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".gradle" && extension != ".properties" && extension != ".xml") {
                    continue;
                }
                if (File.ReadAllText(path).Contains(LegacyMavenHost)) {
                    problems.Add(new RuStoreSetupProblem(
                        $"legacy-maven-host:{Path.GetFileName(path)}",
                        $"{path.Replace('\\', '/')} still lists the Maven host {LegacyMavenHost}, which no longer serves RuStore libraries.",
                        $"{FixMenuHint} It asks External Dependency Manager to resolve again. If the line stays, delete it by hand."));
                }
            }
        }

        private static XDocument CheckManifestFile(List<RuStoreSetupProblem> problems) {
            if (!File.Exists(ManifestPath)) {
                problems.Add(new RuStoreSetupProblem(
                    "manifest-missing",
                    $"{ManifestPath} does not exist. RuStore Pay writes its activity and settings into the custom main manifest.",
                    $"{FixMenuHint} It creates the manifest from Unity's template."));
                return null;
            }
            XDocument manifest = LoadManifest();
            if (manifest == null) {
                problems.Add(new RuStoreSetupProblem("manifest-invalid", $"{ManifestPath} is not valid XML.", "Repair the file by hand."));
                return null;
            }
            if (FindActivities(manifest, LegacyActivityClass).Any()) {
                problems.Add(new RuStoreSetupProblem(
                    "legacy-activity-manifest",
                    $"{ManifestPath} still declares {LegacyActivityClass} from RuStore API 1.x.",
                    FixMenuHint));
            }
            if (!HasLauncherActivity(manifest)) {
                problems.Add(new RuStoreSetupProblem(
                    "manifest-no-launcher",
                    $"{ManifestPath} has no activity with the MAIN action and LAUNCHER category, so the game would have no icon to start from.",
                    $"{FixMenuHint} It restores the launcher filter on {ActivityClass}."));
            }
            return manifest;
        }

        private static void CheckPaySetup(List<RuStoreSetupProblem> problems, XDocument manifest) {
#if MIRRA_RUSTORE_PAY
            PayClientSettings settings = RuStoreModuleSettings.LoadAsset<PayClientSettings>();
            if (settings == null) {
                problems.Add(new RuStoreSetupProblem(
                    "settings-missing",
                    "RuStore Pay settings do not exist yet.",
                    $"{FixMenuHint} It creates the settings asset. Then fill Console Application Id and Deeplink Scheme."));
                return;
            }
            if (string.IsNullOrWhiteSpace(settings.consoleApplicationId)) {
                problems.Add(new RuStoreSetupProblem(
                    "settings-app-id-empty",
                    "Console Application Id is empty in the RuStore Pay settings.",
                    "Open Window > RuStoreSDK > Settings > PayClient and enter the application id from RuStore Console (the number in the app page address)."));
            }
            if (string.IsNullOrWhiteSpace(settings.deeplinkScheme)) {
                problems.Add(new RuStoreSetupProblem(
                    "settings-deeplink-empty",
                    "Deeplink Scheme is empty in the RuStore Pay settings.",
                    "Open Window > RuStoreSDK > Settings > PayClient and enter a scheme unique to the game, lowercase letters and digits only."));
            }
            if (settings.unityMainActivityClass != ActivityClass) {
                problems.Add(new RuStoreSetupProblem(
                    "settings-main-activity",
                    $"RuStore Pay settings name '{settings.unityMainActivityClass}' as the game activity. Payments work only with {ActivityClass}.",
                    $"{FixMenuHint} It selects Activity as the Application Entry Point in the RuStore Pay settings."));
            }
            if (manifest != null && !PayClientManifestPatcher.Instance.VerifyProjectManifest(settings)) {
                problems.Add(new RuStoreSetupProblem(
                    "manifest-not-patched",
                    $"{ManifestPath} lacks the RuStore Pay deeplink activity or its settings entries. The Console has the details.",
                    $"{FixMenuHint} It runs the RuStore manifest patch."));
            }
#endif
        }

        // Major.minor.patch compare. A prerelease such as 10.5.2-ci counts as lower than 10.5.2.
        private static int CompareVersions(string left, string right) {
            ReadVersion(left, out long[] leftNumbers, out bool isLeftPrerelease);
            ReadVersion(right, out long[] rightNumbers, out bool isRightPrerelease);
            for (int index = 0; index < leftNumbers.Length; index++) {
                int comparison = leftNumbers[index].CompareTo(rightNumbers[index]);
                if (comparison != 0) {
                    return comparison;
                }
            }
            return isRightPrerelease.CompareTo(isLeftPrerelease);
        }

        private static void ReadVersion(string text, out long[] numbers, out bool isPrerelease) {
            string version = (text ?? string.Empty).Trim();
            int metadataIndex = version.IndexOf('+');
            if (metadataIndex >= 0) {
                version = version.Substring(0, metadataIndex);
            }
            int prereleaseIndex = version.IndexOf('-');
            isPrerelease = prereleaseIndex >= 0;
            if (isPrerelease) {
                version = version.Substring(0, prereleaseIndex);
            }
            numbers = new long[3];
            string[] parts = version.Split('.');
            for (int index = 0; index < numbers.Length && index < parts.Length; index++) {
                long.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index]);
            }
        }

    }

}
