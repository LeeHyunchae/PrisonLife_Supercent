using PrisonLife.Game;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// PlayerModel.WeaponUpgradeStage 가 requiredStage 이상 도달하면 발동.
    /// 핸드드릴(Stage 1) 도달 시 광부 일꾼 칸 등장 등.
    /// </summary>
    public class WeaponStageReachedGate : FacilityGateBase
    {
        [SerializeField, Min(1)] int requiredStage = 1;

        protected override void SubscribeToTrigger()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;

            systemManager.PlayerModel.WeaponUpgradeStage
                .SubscribeOnChange(_currentStage =>
                {
                    if (_currentStage >= requiredStage) TryUnlock();
                })
                .AddTo(this);
        }
    }
}
