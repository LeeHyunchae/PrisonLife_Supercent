using System;
using Cysharp.Threading.Tasks;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Managers
{
    /// <summary>
    /// 자원 1개의 비행 시각효과. ResourceInputZone / ResourceOutputZone 등이 호출.
    /// model 변화는 caller 가 source 측은 즉시 차감, destination 측은 비행 도착 후 onArrival 콜백에서 증가.
    /// 비행 visual 은 PoolManager.SpawnResourceItem 으로 임시 생성 후 도착 시 Despawn.
    /// </summary>
    public class ItemFlowManager
    {
        private readonly PoolManager poolManager;

        public ItemFlowManager(PoolManager _poolManager)
        {
            poolManager = _poolManager;
        }

        public void Fly(
            ResourceType _type,
            Vector3 _fromPosition,
            Vector3 _toPosition,
            float _durationSeconds,
            float _arcHeight,
            Action _onArrival)
        {
            if (poolManager == null)
            {
                _onArrival?.Invoke();
                return;
            }
            FlyAsync(_type, _fromPosition, _toPosition, _durationSeconds, _arcHeight, _onArrival).Forget();
        }

        private async UniTaskVoid FlyAsync(
            ResourceType _type,
            Vector3 _fromPosition,
            Vector3 _toPosition,
            float _durationSeconds,
            float _arcHeight,
            Action _onArrival)
        {
            // 비행 중 어떤 예외가 나도 onArrival 은 반드시 호출되어야 함.
            // sink 측의 in-flight 카운트가 -1 못 받으면 PrisonerSequencer 가 영구 대기 deadlock 에 빠진다.
            GameObject flyingItem = null;
            try
            {
                if (poolManager == null) return;

                flyingItem = poolManager.SpawnResourceItem(_type, _fromPosition, Quaternion.identity);
                if (flyingItem == null) return;

                float elapsedSeconds = 0f;
                while (elapsedSeconds < _durationSeconds)
                {
                    if (flyingItem == null) return;

                    elapsedSeconds += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedSeconds / _durationSeconds);

                    Vector3 lerpedPosition = Vector3.Lerp(_fromPosition, _toPosition, t);
                    float arcOffsetY = Mathf.Sin(t * Mathf.PI) * _arcHeight;
                    flyingItem.transform.position = lerpedPosition + new Vector3(0f, arcOffsetY, 0f);

                    await UniTask.Yield();
                }
            }
            catch (Exception flightException)
            {
                Debug.LogException(flightException);
            }
            finally
            {
                if (flyingItem != null && poolManager != null) poolManager.Despawn(flyingItem);
                _onArrival?.Invoke();
            }
        }
    }
}
