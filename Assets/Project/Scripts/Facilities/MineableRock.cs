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
        [SerializeField, Min(0.1f)] float respawnDelaySeconds = 4f;

        [Header("Visual")]
        [SerializeField] GameObject visualRoot;

        public ReactiveProperty<bool> IsAvailableForMining { get; } = new(true);
        public Vector3 OreSpawnPosition => transform.position;

        void Awake()
        {
            ResetRockToFullState();
        }

        /// <summary>
        /// 한 번의 타격 시도. 무기 위력 개념 없이, 가용 상태면 즉시 파괴되고 ore 1개 획득.
        /// </summary>
        public bool TryDeplete()
        {
            if (!IsAvailableForMining.Value) return false;
            DepleteAndRespawnAsync(destroyCancellationToken).Forget();
            return true;
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
            IsAvailableForMining.Value = true;
            if (visualRoot != null) visualRoot.SetActive(true);
        }
    }
}
