using MirraGames.SDK.Common;

namespace MirraGames.SDK.RuStore {

    [Provider(typeof(IPlatformInfo))]
    public class RuStorePlatformInfo : CommonPlatformInfo {

        protected override string GetAppIdImpl() {
            return default;
        }

        protected override PlatformType GetCurrentImpl() {
            return PlatformType.RuStore;
        }

        protected override DeploymentType GetDeploymentImpl() {
            return DeploymentType.Mobile;
        }

    }

}