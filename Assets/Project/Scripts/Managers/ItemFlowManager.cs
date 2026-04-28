namespace PrisonLife.Managers
{
    public class ItemFlowManager
    {
        readonly PoolManager poolManager;

        public ItemFlowManager(PoolManager _poolManager)
        {
            poolManager = _poolManager;
        }

        // TODO: Source -> Sink 1개 단위 트랜잭션 구현.
        //       광석 투입 / 수갑 수집 / 돈 수집 / 구매 / 광부 흡수 모두 이 매니저를 통한다.
    }
}
