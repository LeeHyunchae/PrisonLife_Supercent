namespace PrisonLife.Managers
{
    /// <summary>
    /// 풀에서 spawn / despawn 될 때 호출되는 라이프사이클 후크.
    /// reuse 가능한 인스턴스에서 reactive subscription 정리·NavMeshAgent enable 복원 등을 수행.
    /// 한 GameObject 의 모든 IPoolable 컴포넌트가 호출됨.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}
