#if MIRRA_RUSTORE_REMOTECONFIG
using MirraGames.SDK.Common;
using RuStore.RemoteConfig;
using System;
using System.Collections.Generic;
using System.Globalization;
using Logger = MirraGames.SDK.Common.Logger;
using RuStoreRemoteConfig = RuStore.RemoteConfig.RemoteConfig;

namespace MirraGames.SDK.RuStore {

    /// <summary>
    /// MirraSDK flags backed by RuStore Remote Config.
    /// </summary>
    [Provider(typeof(IFlags))]
    public class RuStoreFlags : IFlags {

        private const int MinimumTimeoutSeconds = 1;

        private readonly RuStoreFlags_Configuration configuration;
        private readonly IEventDispatcher eventDispatcher;
        private readonly Stack<Action> waiters = new();

        // Every read crosses into Java, so each key is fetched once and kept as text.
        private readonly Dictionary<string, string> values = new();
        private readonly HashSet<string> missingKeys = new();

        private RuStoreRemoteConfig remoteConfig;
        private RuStoreWatchdog startup;

        public RuStoreFlags(RuStoreFlags_Configuration configuration, IEventDispatcher eventDispatcher) {
            this.configuration = configuration;
            this.eventDispatcher = eventDispatcher;
            eventDispatcher.Start += OnStart;
        }

        public bool IsFlagsInitialized { get; private set; } = false;
        public bool IsFlagsAvailable { get; private set; } = false;

        public void WaitForFlags(Action onInitialized) {
            if (!IsFlagsInitialized) {
                waiters.Push(onInitialized);
                return;
            }
            onInitialized?.Invoke();
        }

        private void OnStart() {
            string appId = configuration.AppId?.Trim();
            if (string.IsNullOrEmpty(appId)) {
                Logger.CreateText(this, "Flags are off:", nameof(RuStoreFlags_Configuration.AppId), "is empty");
                SetInitialized();
                return;
            }
            int timeoutSeconds = Math.Max(MinimumTimeoutSeconds, configuration.InitTimeoutSeconds);
            startup = new RuStoreWatchdog(eventDispatcher, timeoutSeconds, SetInitialized, onTimeout: () => {
                Logger.CreateWarning(this, "Remote config did not arrive in", timeoutSeconds, "s, starting with default values");
            });
            try {
                RuStoreRemoteConfigClient client = RuStoreRemoteConfigClient.Instance;
                RuStoreRemoteConfigClientSettings settings = new() {
                    appId = appId
                };
                if (!client.IsInitialized && !client.Init(settings)) {
                    Logger.CreateText(this, "Flags are off: remote config starts only on an Android device");
                    startup.Complete();
                    return;
                }
                client.GetRemoteConfig(
                    onFailure: (error) => {
                        Logger.CreateError(this, nameof(RuStoreRemoteConfigClient.GetRemoteConfig), RuStoreErrorText.Describe(error));
                        startup.Complete();
                    },
                    onSuccess: (loadedConfig) => {
                        // Also taken after the timeout: later reads then return the real values.
                        remoteConfig = loadedConfig;
                        IsFlagsAvailable = loadedConfig != null;
                        Logger.CreateText(this, nameof(RuStoreRemoteConfigClient.GetRemoteConfig), "onSuccess");
                        startup.Complete();
                    }
                );
            }
            catch (Exception exception) {
                Logger.CreateError(this, nameof(OnStart), exception);
                startup.Complete();
            }
        }

        private void SetInitialized() {
            IsFlagsInitialized = true;
            waiters.PopInvokeAll();
        }

        // RuStore's own typed getters parse with the device culture, which misreads "1.5" on a Russian device.
        // Values are therefore read as text and parsed here, culture independent.
        private bool TryGetText(string key, out string text) {
            text = null;
            if (remoteConfig == null || string.IsNullOrEmpty(key)) {
                return false;
            }
            if (values.TryGetValue(key, out text)) {
                return true;
            }
            if (missingKeys.Contains(key)) {
                return false;
            }
            try {
                if (remoteConfig.ContainsKey(key)) {
                    text = remoteConfig.GetString(key);
                }
            }
            catch (Exception exception) {
                Logger.CreateError(this, "Failed to read flag", key, exception.Message);
            }
            if (text == null) {
                missingKeys.Add(key);
                return false;
            }
            values[key] = text;
            return true;
        }

        public bool GetBool(string key, bool defaultValue = false) {
            if (!TryGetText(key, out string text)) {
                return defaultValue;
            }
            text = text.Trim();
            if (bool.TryParse(text, out bool result)) {
                return result;
            }
            if (text == "1") {
                return true;
            }
            return text == "0" ? false : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0) {
            return TryGetText(key, out string text)
                && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0.0f) {
            return TryGetText(key, out string text)
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        public string GetString(string key, string defaultValue = "") {
            return TryGetText(key, out string text) ? text : defaultValue;
        }

        public bool HasKey(string key) {
            return TryGetText(key, out _);
        }

    }

}
#endif
