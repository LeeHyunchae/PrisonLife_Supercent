using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 다른 PurchaseZone 의 구매 완료 시점에 발동. 구매 체인용.
    /// 예) 광부 일꾼 구매 완료 → 죄수 처리 일꾼 칸 등장.
    /// </summary>
    public class PurchaseCompletedGate : FacilityGateBase
    {
        [SerializeField] PurchaseZone observedPurchaseZone;

        protected override void SubscribeToTrigger()
        {
            if (observedPurchaseZone == null)
            {
                Debug.LogWarning("[PurchaseCompletedGate] observedPurchaseZone 이 비어있어 동작 불가.");
                return;
            }
            observedPurchaseZone.OnPurchaseCompleted += HandleObservedPurchaseCompleted;
        }

        void OnDestroy()
        {
            if (observedPurchaseZone != null)
            {
                observedPurchaseZone.OnPurchaseCompleted -= HandleObservedPurchaseCompleted;
            }
        }

        void HandleObservedPurchaseCompleted()
        {
            TryUnlock();
        }
    }
}
