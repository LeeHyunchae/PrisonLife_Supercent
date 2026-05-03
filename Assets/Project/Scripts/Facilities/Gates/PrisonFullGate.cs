using System;
using PrisonLife.Game;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 감옥에 빈 슬롯이 없어진 시점에 발동. 감옥 확장 칸 등장용.
    /// </summary>
    public class PrisonFullGate : FacilityGate
    {
        private IDisposable subscription;

        public PrisonFullGate(GameObject _gatedFacility, bool _playRevealCinematic = false, Transform _cinematicFocusOverride = null)
            : base(_gatedFacility, _playRevealCinematic, _cinematicFocusOverride) { }

        protected override void SubscribeToTrigger()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Prison == null) return;

            subscription = systemManager.Prison.CurrentInmateCount.SubscribeOnChange(_currentInmateCount =>
            {
                if (!systemManager.Prison.HasFreeSlot) TryUnlock();
            });
        }

        public override void Dispose()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }
}
