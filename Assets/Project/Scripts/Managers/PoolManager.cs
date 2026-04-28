using UnityEngine;

namespace PrisonLife.Managers
{
    public class PoolManager : MonoBehaviour
    {
        // TODO: 풀링 본 구현은 자원/이펙트 prefab 도입 단계에서 작성.
        //       지금은 SystemManager 와 ItemFlowManager 의 의존성 자리만 잡아둔다.

        public T SpawnFromPrefab<T>(T _prefab, Vector3 _spawnPosition, Quaternion _spawnRotation) where T : Component
        {
            return Instantiate(_prefab, _spawnPosition, _spawnRotation);
        }

        public void Despawn<T>(T _instance) where T : Component
        {
            if (_instance == null) return;
            Destroy(_instance.gameObject);
        }
    }
}
