using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 다른 PurchaseZone 의 구매 완료 시점에 발동. 구매 체인용.
    /// 예) 광부 일꾼 구매 완료 → 죄수 처리 일꾼 칸 등장.
    /// </summary>
    public class PurchaseCompletedGate : FacilityGate
    {
        readonly PurchaseZone observedPurchaseZone;

        public PurchaseCompletedGate(GameObject _gatedFacility, PurchaseZone _observedPurchaseZone, bool _playRevealCinematic = false)
            : base(_gatedFacility, _playRevealCinematic)
        {
            observedPurchaseZone = _observedPurchaseZone;
        }

        protected override void SubscribeToTrigger()
        {
            if (observedPurchaseZone == null) return;
            observedPurchaseZone.OnPurchaseCompleted += HandleObservedPurchaseCompleted;
        }

        void HandleObservedPurchaseCompleted()
        {
            TryUnlock();
        }

        public override void Dispose()
        {
            if (observedPurchaseZone != null)
            {
                observedPurchaseZone.OnPurchaseCompleted -= HandleObservedPurchaseCompleted;
            }
        }
    }
}
