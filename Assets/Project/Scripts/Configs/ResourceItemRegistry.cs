using System.Collections.Generic;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Configs
{
    /// <summary>
    /// ResourceType → 시각용 prefab 매핑. ScriptableObject 에셋으로 1개 생성하고
    /// SystemManager 인스펙터에 연결해 전역 조회 (SystemManager.Instance.ResourceItems) 한다.
    /// 이후 PoolManager 도 같은 registry 를 참조해 풀링 단위를 결정한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceItemRegistry", menuName = "PrisonLife/ResourceItemRegistry")]
    public class ResourceItemRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public ResourceType type;
            public GameObject prefab;
        }

        [SerializeField] Entry[] entries;

        Dictionary<ResourceType, GameObject> prefabByType;

        void EnsureLookup()
        {
            if (prefabByType != null) return;
            prefabByType = new Dictionary<ResourceType, GameObject>();
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                prefabByType[entries[i].type] = entries[i].prefab;
            }
        }

        public GameObject GetPrefab(ResourceType _type)
        {
            EnsureLookup();
            if (!prefabByType.TryGetValue(_type, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"[ResourceItemRegistry] {_type} 의 prefab 등록 안 됨.");
                return null;
            }
            return prefab;
        }
    }
}
