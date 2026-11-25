using MirraGames.SDK.Common;
using System.Collections.Generic;
using UnityEditor;
using Logger = MirraGames.SDK.Common.Logger;

namespace MirraGames.SDK.RuStore.Editor {

    internal static class RuStoreMenu {

        private const string MenuRoot = Naming.MirraSDK5 + "/RuStore/";

        // Prints what the build guard would report, without starting a build.
        [MenuItem(MenuRoot + "Validate setup")]
        private static void ValidateSetup() {
            List<RuStoreSetupProblem> problems = RuStoreSetupValidator.Validate();
            string report = RuStoreSetupValidator.FormatReport(problems);
            if (problems.Count == 0) {
                Logger.CreateText(nameof(RuStoreMenu), report);
            }
            else {
                Logger.CreateWarning(nameof(RuStoreMenu), report);
            }
        }

        [MenuItem(MenuRoot + "Fix setup")]
        private static void FixSetup() {
            RuStoreSetupFixer.Fix();
        }

    }

}
