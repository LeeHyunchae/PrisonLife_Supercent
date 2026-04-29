using PrisonLife.Game;
using PrisonLife.Reactive;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 플레이어가 처음으로 돈을 획득하는 시점에 발동. 무기 강화 구매칸 첫 등장용.
    /// </summary>
    public class FirstMoneyEarnedGate : FacilityGateBase
    {
        protected override void SubscribeToTrigger()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Wallet == null) return;

            systemManager.Wallet.Balance
                .SubscribeOnChange(_balance =>
                {
                    if (_balance > 0) TryUnlock();
                })
                .AddTo(this);
        }
    }
}
