using System;
using System.Collections.Generic;
using PrisonLife.Configs;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Managers
{
    /// <summary>
    /// 모든 prefab 의 단일 소유자. 호출자는 type(엔티티) 또는 ResourceType(아이템) 으로만 spawn 요청한다.
    /// 호출자가 prefab 을 들고 다니지 않게 하기 위한 SRP 분리.
    /// IPoolable 구현 인스턴스에는 spawn / despawn 시 OnSpawn / OnDespawn 후크 호출.
    /// Awake 에 entry 별 prewarmCount 만큼 미리 instantiate 해서 첫 spawn 비용을 분산.
    /// 비활성 인스턴스는 poolRoot 아래 모임 (Hierarchy 정리).
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [Serializable]
        public struct EntityPrefabConfig
        {
            public GameObject prefab;
            [Min(0)] public int prewarmCount;
        }

        [Header("Entity Prefabs")]
        [SerializeField] private List<EntityPrefabConfig> entityPrefabs;

        [Header("Resource Item Prefabs")]
        [SerializeField] private ResourceItemRegistry resourceItemRegistry;
        [SerializeField, Min(0)] private int resourceItemPrewarmCountEach = 32;

        [Header("Hierarchy")]
        [SerializeField] private Transform poolRoot;

        private readonly Dictionary<Type, GameObject> entityPrefabByType = new();
        private readonly Dictionary<GameObject, Stack<GameObject>> inactivePoolsByPrefab = new();
        private readonly Dictionary<GameObject, GameObject> sourcePrefabByInstance = new();

        private void Awake()
        {
            BuildEntityPrefabRegistry();
            Prewarm();
        }

        public T Spawn<T>(Vector3 _spawnPosition, Quaternion _spawnRotation) where T : Component
        {
            if (!entityPrefabByType.TryGetValue(typeof(T), out GameObject prefab))
            {
                Debug.LogError($"[PoolManager] {typeof(T).Name} 프리팹이 등록되지 않음. PoolManager.entityPrefabs 확인.");
                return null;
            }

            GameObject instance = SpawnInternal(prefab, _spawnPosition, _spawnRotation);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public GameObject SpawnResourceItem(ResourceType _type, Vector3 _spawnPosition, Quaternion _spawnRotation)
        {
            if (resourceItemRegistry == null)
            {
                Debug.LogError("[PoolManager] resourceItemRegistry 미연결.");
                return null;
            }
            GameObject prefab = resourceItemRegistry.GetPrefab(_type);
            if (prefab == null) return null;
            return SpawnInternal(prefab, _spawnPosition, _spawnRotation);
        }

        public void Despawn<T>(T _instance) where T : Component
        {
            if (_instance == null) return;
            Despawn(_instance.gameObject);
        }

        public void Despawn(GameObject _instance)
        {
            if (_instance == null) return;

            if (!sourcePrefabByInstance.TryGetValue(_instance, out GameObject prefab))
            {
                // 풀 외부에서 만든 인스턴스 — 안전하게 destroy.
                Destroy(_instance);
                return;
            }

            IPoolable[] poolables = _instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawn();

            _instance.SetActive(false);
            if (poolRoot != null) _instance.transform.SetParent(poolRoot, worldPositionStays: false);

            PushToInactiveStack(prefab, _instance);
        }

        private GameObject SpawnInternal(GameObject _prefab, Vector3 _spawnPosition, Quaternion _spawnRotation)
        {
            if (_prefab == null) return null;

            GameObject instance;
            if (inactivePoolsByPrefab.TryGetValue(_prefab, out Stack<GameObject> stack) && stack.Count > 0)
            {
                instance = stack.Pop();
                instance.transform.SetParent(null, worldPositionStays: false);
                instance.transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(_prefab, _spawnPosition, _spawnRotation);
                sourcePrefabByInstance[instance] = _prefab;
            }

            IPoolable[] poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawn();

            return instance;
        }

        private void BuildEntityPrefabRegistry()
        {
            if (entityPrefabs == null) return;

            for (int i = 0; i < entityPrefabs.Count; i++)
            {
                GameObject prefab = entityPrefabs[i].prefab;
                if (prefab == null) continue;

                Component[] components = prefab.GetComponents<Component>();
                for (int j = 0; j < components.Length; j++)
                {
                    Component component = components[j];
                    if (component == null) continue;

                    Type componentType = component.GetType();
                    // PrisonLife.* 네임스페이스 컴포넌트만 등록 — Unity 빌트인 (Transform 등) 제외.
                    if (componentType.Namespace == null) continue;
                    if (!componentType.Namespace.StartsWith("PrisonLife")) continue;

                    entityPrefabByType[componentType] = prefab;
                }
            }
        }

        private void Prewarm()
        {
            if (entityPrefabs != null)
            {
                for (int i = 0; i < entityPrefabs.Count; i++)
                {
                    EntityPrefabConfig config = entityPrefabs[i];
                    PrewarmPrefab(config.prefab, config.prewarmCount);
                }
            }

            if (resourceItemRegistry != null && resourceItemPrewarmCountEach > 0)
            {
                foreach (KeyValuePair<ResourceType, GameObject> entry in resourceItemRegistry.AllPrefabs)
                {
                    PrewarmPrefab(entry.Value, resourceItemPrewarmCountEach);
                }
            }
        }

        private void PrewarmPrefab(GameObject _prefab, int _count)
        {
            if (_prefab == null || _count <= 0) return;
            for (int i = 0; i < _count; i++)
            {
                GameObject instance = Instantiate(_prefab);
                sourcePrefabByInstance[instance] = _prefab;
                Despawn(instance); 
            }
        }

        private void PushToInactiveStack(GameObject _prefab, GameObject _instance)
        {
            if (!inactivePoolsByPrefab.TryGetValue(_prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                inactivePoolsByPrefab[_prefab] = stack;
            }
            stack.Push(_instance);
        }
    }
}
