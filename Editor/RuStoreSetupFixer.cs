using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using Logger = MirraGames.SDK.Common.Logger;
#if MIRRA_RUSTORE_PAY
using PayClientManifestPatcher = RuStore.Editor.Manifest.PayClientManifestPatcher;
using PayClientSettings = RuStore.Editor.PayClientSettings;
using RuStoreModuleSettings = RuStore.Editor.RuStoreModuleSettings;
#endif

namespace MirraGames.SDK.RuStore.Editor {

    /// <summary>
    /// Applies the setup fixes that are safe to make without asking: removing RuStore API 1.x leftovers,
    /// repairing the custom manifest, creating the RuStore Pay settings and patching the manifest from them.
    /// It never touches values only the developer knows (application id, deeplink scheme, API levels, package name).
    /// </summary>
    internal static class RuStoreSetupFixer {

        public const string ManifestBackupPath = "Library/MirraSDK/RuStore/AndroidManifest.backup.xml";

        private const string UnityThemeSelector = "@style/UnityThemeSelector";
        private const string UnityActivityMetaData = "unityplayer.UnityActivity";

        /// <summary>
        /// Runs every fix and returns a description of each change that was made.
        /// </summary>
        public static List<string> Fix() {
            List<string> appliedFixes = new();
            RemoveLegacyActivityFile(appliedFixes);
            SetActivityEntryPoint(appliedFixes);
            RepairManifest(appliedFixes);
            AssetDatabase.Refresh();
            PreparePaySetup(appliedFixes);
            ResolveAndroidDependencies(appliedFixes);
            AssetDatabase.Refresh();

            foreach (string appliedFix in appliedFixes) {
                Logger.CreateText(nameof(RuStoreSetupFixer), appliedFix);
            }
            List<RuStoreSetupProblem> remainingProblems = RuStoreSetupValidator.Validate();
            string report = RuStoreSetupValidator.FormatReport(remainingProblems);
            if (remainingProblems.Count == 0) {
                Logger.CreateText(nameof(RuStoreSetupFixer), report);
            }
            else {
                Logger.CreateWarning(nameof(RuStoreSetupFixer), report);
            }
            return appliedFixes;
        }

        private static void RemoveLegacyActivityFile(List<string> appliedFixes) {
            if (!File.Exists(RuStoreSetupValidator.LegacyActivityPath)) {
                return;
            }
            AssetDatabase.DeleteAsset(RuStoreSetupValidator.LegacyActivityPath);
            appliedFixes.Add($"Deleted {RuStoreSetupValidator.LegacyActivityPath} (RuStore Pay ships its own deeplink activity)");
        }

        private static void SetActivityEntryPoint(List<string> appliedFixes) {
#if UNITY_2023_1_OR_NEWER
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.Activity) {
                PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
                appliedFixes.Add("Set Application Entry Point to Activity (RuStore payments do not work with GameActivity)");
            }
#endif
        }

        private static void RepairManifest(List<string> appliedFixes) {
            if (!File.Exists(RuStoreSetupValidator.ManifestPath)) {
                CreateManifestFromTemplate(appliedFixes);
            }
            XDocument manifest = RuStoreSetupValidator.LoadManifest();
            XElement application = manifest?.Root?.Element("application");
            if (application == null) {
                // Not valid XML, or no application element: nothing here can be repaired safely.
                return;
            }

            List<string> manifestFixes = new();
            RemoveActivities(manifest, RuStoreSetupValidator.LegacyActivityClass, manifestFixes,
                "Removed the RuStore API 1.x activity {0} from the manifest");
            // Unity's own template says to delete the activity of the entry point that is not used.
            RemoveActivities(manifest, RuStoreSetupValidator.GameActivityClass, manifestFixes,
                "Removed {0} from the manifest (the entry point is Activity)");
            if (!RuStoreSetupValidator.HasLauncherActivity(manifest)) {
                AddLauncherFilter(application);
                manifestFixes.Add($"Restored the MAIN/LAUNCHER intent filter on {RuStoreSetupValidator.ActivityClass}");
            }

            if (manifestFixes.Count == 0) {
                return;
            }
            BackUpManifest(manifestFixes);
            manifest.Save(RuStoreSetupValidator.ManifestPath);
            appliedFixes.AddRange(manifestFixes);
        }

        // The same file Unity copies when "Custom Main Manifest" is ticked in Player Settings.
        private static void CreateManifestFromTemplate(List<string> appliedFixes) {
            string playbackEngine = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
            string templatePath = Path.Combine(playbackEngine, "Apk", "UnityManifest.xml");
            if (!File.Exists(templatePath)) {
                Logger.CreateWarning(nameof(RuStoreSetupFixer), "Unity manifest template not found at", templatePath,
                    "- tick Custom Main Manifest in Player Settings > Android > Publishing Settings and run the fix again");
                return;
            }
            Directory.CreateDirectory(RuStoreSetupValidator.AndroidPluginsFolder);
            File.Copy(templatePath, RuStoreSetupValidator.ManifestPath);
            appliedFixes.Add($"Created {RuStoreSetupValidator.ManifestPath} from Unity's template");
        }

        private static void RemoveActivities(XDocument manifest, string className, List<string> manifestFixes, string messageFormat) {
            XElement[] activities = RuStoreSetupValidator.FindActivities(manifest, className).ToArray();
            if (activities.Length == 0) {
                return;
            }
            foreach (XElement activity in activities) {
                activity.Remove();
            }
            manifestFixes.Add(string.Format(messageFormat, className));
        }

        private static void AddLauncherFilter(XElement application) {
            XNamespace android = RuStoreSetupValidator.AndroidNamespace;
            XElement activity = application.Elements("activity")
                .FirstOrDefault(candidate => (string)candidate.Attribute(android + "name") == RuStoreSetupValidator.ActivityClass);
            if (activity == null) {
                activity = new XElement("activity",
                    new XAttribute(android + "name", RuStoreSetupValidator.ActivityClass),
                    new XAttribute(android + "theme", UnityThemeSelector),
                    new XElement("meta-data",
                        new XAttribute(android + "name", UnityActivityMetaData),
                        new XAttribute(android + "value", "true")));
                application.AddFirst(activity);
            }
            // Android 12 and newer refuse to install an app whose launcher activity is not exported.
            activity.SetAttributeValue(android + "exported", "true");
            activity.AddFirst(new XElement("intent-filter",
                new XElement("action", new XAttribute(android + "name", RuStoreSetupValidator.MainAction)),
                new XElement("category", new XAttribute(android + "name", RuStoreSetupValidator.LauncherCategory))));
        }

        // Kept outside Assets so Unity does not import it. One backup: the state before the first repair.
        private static void BackUpManifest(List<string> manifestFixes) {
            if (File.Exists(ManifestBackupPath)) {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestBackupPath));
            File.Copy(RuStoreSetupValidator.ManifestPath, ManifestBackupPath);
            manifestFixes.Insert(0, $"Saved the previous manifest to {ManifestBackupPath}");
        }

        private static void PreparePaySetup(List<string> appliedFixes) {
#if MIRRA_RUSTORE_PAY
            PayClientSettings settings = RuStoreModuleSettings.LoadAsset<PayClientSettings>();
            if (settings == null) {
                // RuStore's own menu action: creates the asset where RuStore looks for it and selects it.
                PayClientSettings.EditPayClientSettings();
                settings = RuStoreModuleSettings.LoadAsset<PayClientSettings>();
                if (settings == null) {
                    Logger.CreateWarning(nameof(RuStoreSetupFixer), "RuStore Pay settings could not be created, open Window > RuStoreSDK > Settings > PayClient");
                    return;
                }
                appliedFixes.Add("Created the RuStore Pay settings asset: fill Console Application Id and Deeplink Scheme");
            }
            if (settings.unityMainActivityClass != RuStoreSetupValidator.ActivityClass) {
                settings.unityMainActivityClass = RuStoreSetupValidator.ActivityClass;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
                appliedFixes.Add("Selected Activity as the Application Entry Point in the RuStore Pay settings");
            }
            if (File.Exists(RuStoreSetupValidator.ManifestPath) && !PayClientManifestPatcher.Instance.VerifyProjectManifest(settings)) {
                PayClientManifestPatcher.Instance.PatchProjectManifest(settings);
                appliedFixes.Add("Patched the manifest with the RuStore Pay deeplink activity and settings entries");
            }
            if (string.IsNullOrWhiteSpace(settings.consoleApplicationId) || string.IsNullOrWhiteSpace(settings.deeplinkScheme)) {
                Selection.activeObject = settings;
            }
#endif
        }

        // Template lines that name the retired Maven host sit in the blocks External Dependency Manager owns.
        // A forced resolve rewrites those blocks from the installed packages.
        private static void ResolveAndroidDependencies(List<string> appliedFixes) {
            bool hasLegacyHost = RuStoreSetupValidator.Validate().Any(problem => problem.Id.StartsWith("legacy-maven-host", StringComparison.Ordinal));
            if (!hasLegacyHost) {
                return;
            }
            Type resolverType = RuStoreSetupValidator.FindType("GooglePlayServices.PlayServicesResolver");
            MethodInfo resolveSync = resolverType?.GetMethod("ResolveSync", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool) }, null);
            if (resolveSync == null) {
                return;
            }
            try {
                resolveSync.Invoke(null, new object[] { true });
                appliedFixes.Add("Asked External Dependency Manager to resolve the Android libraries again");
            }
            catch (Exception exception) {
                Logger.CreateWarning(nameof(RuStoreSetupFixer), "External Dependency Manager could not resolve:", exception.InnerException?.Message ?? exception.Message);
            }
        }

    }

}
