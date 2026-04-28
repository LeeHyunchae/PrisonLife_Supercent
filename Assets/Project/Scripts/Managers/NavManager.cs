using Unity.AI.Navigation;
using UnityEngine;

namespace PrisonLife.Managers
{
    public class NavManager : MonoBehaviour
    {
        [SerializeField] NavMeshSurface navMeshSurface;

        public void RebuildNavMesh()
        {
            if (navMeshSurface == null)
            {
                Debug.LogWarning("[NavManager] NavMeshSurface 가 인스펙터에 연결되지 않았습니다.");
                return;
            }
            navMeshSurface.BuildNavMesh();
        }
    }
}
