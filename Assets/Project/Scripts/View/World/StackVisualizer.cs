using System;
using System.Collections.Generic;
using PrisonLife.Reactive;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PrisonLife.View.World
{
    public class StackVisualizer : IDisposable
    {
        readonly ReactiveProperty<int> trackedCount;
        readonly Transform stackAnchor;
        readonly GameObject itemVisualPrefab;
        readonly Vector3 stackOffsetStep;

        readonly List<GameObject> spawnedVisualItems = new();
        IDisposable countSubscription;

        public StackVisualizer(
            ReactiveProperty<int> _trackedCount,
            Transform _stackAnchor,
            GameObject _itemVisualPrefab,
            Vector3 _stackOffsetStep)
        {
            trackedCount = _trackedCount;
            stackAnchor = _stackAnchor;
            itemVisualPrefab = _itemVisualPrefab;
            stackOffsetStep = _stackOffsetStep;

            if (itemVisualPrefab == null || stackAnchor == null)
            {
                Debug.LogWarning(
                    $"[StackVisualizer] 시각화 비활성: " +
                    $"itemVisualPrefab={(itemVisualPrefab != null)}, stackAnchor={(stackAnchor != null)}.");
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
                if (spawnedVisualItems[i] != null) Object.Destroy(spawnedVisualItems[i]);
            }
            spawnedVisualItems.Clear();
        }

        void OnTrackedCountChanged(int _newCount)
        {
            if (itemVisualPrefab == null || stackAnchor == null) return;

            while (spawnedVisualItems.Count < _newCount) SpawnOneVisualItem();
            while (spawnedVisualItems.Count > _newCount) DespawnLastVisualItem();
        }

        void SpawnOneVisualItem()
        {
            var instance = Object.Instantiate(itemVisualPrefab, stackAnchor);
            instance.transform.localPosition = stackOffsetStep * spawnedVisualItems.Count;
            instance.transform.localRotation = Quaternion.identity;
            spawnedVisualItems.Add(instance);
        }

        void DespawnLastVisualItem()
        {
            int lastIndex = spawnedVisualItems.Count - 1;
            if (lastIndex < 0) return;
            var instance = spawnedVisualItems[lastIndex];
            spawnedVisualItems.RemoveAt(lastIndex);
            if (instance != null) Object.Destroy(instance);
        }
    }
}
