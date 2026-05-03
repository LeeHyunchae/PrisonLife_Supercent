using System;
using PrisonLife.Game;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// PlayerModel.WeaponUpgradeStage 가 requiredStage 이상 도달하면 발동.
    /// 핸드드릴(Stage 1) 도달 시 광부 일꾼 칸 등장 등.
    /// </summary>
    public class WeaponStageReachedGate : FacilityGate
    {
        private readonly int requiredStage;
        private IDisposable subscription;

        public WeaponStageReachedGate(GameObject _gatedFacility, int _requiredStage, bool _playRevealCinematic = false)
            : base(_gatedFacility, _playRevealCinematic)
        {
            requiredStage = _requiredStage;
        }

        protected override void SubscribeToTrigger()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;

            subscription = systemManager.PlayerModel.WeaponUpgradeStage.SubscribeOnChange(_currentStage =>
            {
                if (_currentStage >= requiredStage) TryUnlock();
            });
        }

        public override void Dispose()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }
}
