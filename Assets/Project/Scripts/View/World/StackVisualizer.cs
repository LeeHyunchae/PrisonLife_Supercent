using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Managers;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.View.World
{
    /// <summary>
    /// stockpile count 변화에 따라 anchor 아래에 시각용 아이템을 쌓고 부순다.
    /// 시각용 prefab 은 PoolManager 가 ResourceItemRegistry 로 해석. 호출자는 prefab 을 들지 않는다.
    /// `valuePerVisualItem` 으로 count 단위 → 시각 단위 환산. 예: 돈은 1원 단위 count 지만 5원당 1지폐 visual
    /// 이라 valuePerVisualItem=5 로 설정. 일반 자원은 1:1 (default).
    /// </summary>
    public class StackVisualizer : IDisposable
    {
        private readonly ReactiveProperty<int> trackedCount;
        private readonly Transform stackAnchor;
        private readonly ResourceType resourceType;
        private readonly Vector3 stackOffsetStep;
        private readonly PoolManager pool;
        private readonly int valuePerVisualItem;

        private readonly List<GameObject> spawnedVisualItems = new();
        private IDisposable countSubscription;

        public StackVisualizer(
            ReactiveProperty<int> _trackedCount,
            Transform _stackAnchor,
            ResourceType _resourceType,
            Vector3 _stackOffsetStep,
            PoolManager _pool,
            int _valuePerVisualItem = 1)
        {
            trackedCount = _trackedCount;
            stackAnchor = _stackAnchor;
            resourceType = _resourceType;
            stackOffsetStep = _stackOffsetStep;
            pool = _pool;
            valuePerVisualItem = _valuePerVisualItem > 0 ? _valuePerVisualItem : 1;

            if (stackAnchor == null || pool == null)
            {
                Debug.LogWarning(
                    $"[StackVisualizer] 시각화 비활성: " +
                    $"stackAnchor={(stackAnchor != null)}, pool={(pool != null)}.");
            }

            if (trackedCount != null)
            {
                countSubscription = trackedCount.Subscribe(OnTrackedCountChanged);
            }
        }

        public void Dispose()
        {
            countSubscription?.Dispose();
            countSubscription = null;

            for (int i = 0; i < spawnedVisualItems.Count; i++)
            {
                GameObject visualItem = spawnedVisualItems[i];
                if (visualItem != null) pool?.Despawn(visualItem);
            }
            spawnedVisualItems.Clear();
        }

        private void OnTrackedCountChanged(int _newCount)
        {
            if (stackAnchor == null || pool == null) return;

            int targetVisualCount = _newCount / valuePerVisualItem;
            while (spawnedVisualItems.Count < targetVisualCount) SpawnOneVisualItem();
            while (spawnedVisualItems.Count > targetVisualCount) DespawnLastVisualItem();
        }

        private void SpawnOneVisualItem()
        {
            GameObject instance = pool.SpawnResourceItem(resourceType, stackAnchor.position, stackAnchor.rotation);
            if (instance == null) return;

            instance.transform.SetParent(stackAnchor, worldPositionStays: false);
            instance.transform.localPosition = stackOffsetStep * spawnedVisualItems.Count;
            instance.transform.localRotation = Quaternion.identity;
            spawnedVisualItems.Add(instance);
        }

        private void DespawnLastVisualItem()
        {
            int lastIndex = spawnedVisualItems.Count - 1;
            if (lastIndex < 0) return;

            GameObject instance = spawnedVisualItems[lastIndex];
            spawnedVisualItems.RemoveAt(lastIndex);
            if (instance != null) pool.Despawn(instance);
        }
    }
}
