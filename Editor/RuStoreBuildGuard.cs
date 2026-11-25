using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore.Editor {

    /// <summary>
    /// Stops an Android build of the RuStore configuration while the setup is incomplete.
    /// Without it such a build succeeds and ships with payments silently replaced by the fallback provider.
    /// </summary>
    internal sealed class RuStoreBuildGuard : IPreprocessBuildWithReport {

        // Before RuStore's own build steps (0 and 10). Those throw a bare NullReferenceException on a
        // settings asset that was never filled in; checked first, the developer gets the report instead.
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) {
            if (report.summary.platform != BuildTarget.Android) {
                return;
            }
            if (!RuStoreSetupValidator.IsRuStoreBuildConfiguration()) {
                return;
            }
            List<RuStoreSetupProblem> problems = RuStoreSetupValidator.Validate();
            if (problems.Count > 0) {
                throw new BuildFailedException(RuStoreSetupValidator.FormatReport(problems));
            }
            Logger.CreateText(nameof(RuStoreBuildGuard), "RuStore setup verified");
        }

    }

}
