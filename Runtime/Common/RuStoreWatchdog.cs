using MirraGames.SDK.Common;
using System;
using System.Collections;
using UnityEngine;

namespace MirraGames.SDK.RuStore {

    /// <summary>
    /// Runs an action exactly once: when <see cref="Complete"/> is called or when the time limit passes,
    /// whichever comes first. Keeps provider startup from waiting forever on a store request that never answers.
    /// </summary>
    internal sealed class RuStoreWatchdog {

        private readonly Action onCompleted;

        public RuStoreWatchdog(IEventDispatcher eventDispatcher, float timeoutSeconds, Action onCompleted, Action onTimeout = null) {
            this.onCompleted = onCompleted;
            eventDispatcher.StartCoroutine(WaitForTimeout(timeoutSeconds, onTimeout));
        }

        public bool IsCompleted { get; private set; }

        public void Complete() {
            if (IsCompleted) {
                return;
            }
            IsCompleted = true;
            onCompleted?.Invoke();
        }

        private IEnumerator WaitForTimeout(float timeoutSeconds, Action onTimeout) {
            // Realtime, so a paused game (time scale 0) still times out.
            yield return new WaitForSecondsRealtime(timeoutSeconds);
            if (IsCompleted) {
                yield break;
            }
            onTimeout?.Invoke();
            Complete();
        }

    }

}
