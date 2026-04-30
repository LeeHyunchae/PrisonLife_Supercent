using System;
using PrisonLife.Core;
using PrisonLife.Game;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 플레이어가 처음으로 돈을 인벤토리에 적재하는 시점에 발동. 무기 강화 구매칸 첫 등장용.
    /// 게임 내 돈의 진실원은 PlayerModel.Inventory[Money] (별도 Wallet 모델 없음).
    /// </summary>
    public class FirstMoneyEarnedGate : FacilityGate
    {
        private IDisposable subscription;

        public FirstMoneyEarnedGate(GameObject _gatedFacility, bool _playRevealCinematic = false)
            : base(_gatedFacility, _playRevealCinematic) { }

        protected override void SubscribeToTrigger()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;

            var inventory = systemManager.PlayerModel.Inventory;
            if (inventory == null) return;

            subscription = inventory.ObserveCount(ResourceType.Money).SubscribeOnChange(_currentMoney =>
            {
                if (_currentMoney > 0) TryUnlock();
            });
        }

        public override void Dispose()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }
}
