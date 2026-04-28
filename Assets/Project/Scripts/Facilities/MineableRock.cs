using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.Facilities
{
    public class MineableRock : MonoBehaviour
    {
        [Header("Mining")]
        private int maxHitPoints = 3;
        private float respawnDelaySeconds = 4f;

        [Header("Visual")]
        [SerializeField] GameObject visualRoot;

        int currentHitPoints;

        public ReactiveProperty<bool> IsAvailableForMining { get; } = new(true);
        public Vector3 OreSpawnPosition => transform.position;

        void Awake()
        {
            ResetRockToFullState();
        }

        public bool TryApplyMiningDamage(int _damageAmount)
        {
            if (!IsAvailableForMining.Value) return false;
            if (_damageAmount <= 0) return false;

            currentHitPoints -= _damageAmount;
            if (currentHitPoints <= 0)
            {
                DepleteAndRespawnAsync(destroyCancellationToken).Forget();
                return true;
            }
            return false;
        }

        async UniTaskVoid DepleteAndRespawnAsync(CancellationToken _cancellationToken)
        {
            IsAvailableForMining.Value = false;
            if (visualRoot != null) visualRoot.SetActive(false);

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(respawnDelaySeconds),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken: _cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            ResetRockToFullState();
        }

        void ResetRockToFullState()
        {
            currentHitPoints = maxHitPoints;
            IsAvailableForMining.Value = true;
            if (visualRoot != null) visualRoot.SetActive(true);
        }
    }
}
