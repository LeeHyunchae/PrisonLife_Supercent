using System;
using PrisonLife.Game;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 플레이어가 처음으로 돈을 획득하는 시점에 발동. 무기 강화 구매칸 첫 등장용.
    /// </summary>
    public class FirstMoneyEarnedGate : FacilityGate
    {
        IDisposable subscription;

        public FirstMoneyEarnedGate(GameObject _gatedFacility, bool _playRevealCinematic = false)
            : base(_gatedFacility, _playRevealCinematic) { }

        protected override void SubscribeToTrigger()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Wallet == null) return;

            subscription = systemManager.Wallet.Balance.SubscribeOnChange(_balance =>
            {
                if (_balance > 0) TryUnlock();
            });
        }

        public override void Dispose()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }
}
