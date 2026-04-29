using PrisonLife.Game;
using PrisonLife.Reactive;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 감옥에 빈 슬롯이 없어진 시점에 발동. 감옥 확장 칸 등장용.
    /// </summary>
    public class PrisonFullGate : FacilityGateBase
    {
        protected override void SubscribeToTrigger()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Prison == null) return;

            systemManager.Prison.CurrentInmateCount
                .SubscribeOnChange(_currentInmateCount =>
                {
                    if (!systemManager.Prison.HasFreeSlot) TryUnlock();
                })
                .AddTo(this);
        }
    }
}
