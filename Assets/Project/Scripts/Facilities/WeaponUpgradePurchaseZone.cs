using PrisonLife.Game;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 무기 강화 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// 책임: 다음 단계 비용을 PurchaseZone 에 세팅 + 구매 완료 시 PlayerModel.WeaponUpgradeStage 증가.
    /// 단계 데이터 적용 (스탯/visual) 은 PlayerWeaponSystem 이 stage RP 구독으로 처리한다.
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class WeaponUpgradePurchaseZone : MonoBehaviour
    {
        private const int CostStage1 = 20;  // stage 0 → 1 (핸드드릴) 강화 비용
        private const int CostStage2 = 50;  // stage 1 → 2 (파쇄차) 강화 비용

        private PurchaseZone purchaseZone;

        private void Awake()
        {
            purchaseZone = GetComponent<PurchaseZone>();
        }

        private void Start()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null)
            {
                Debug.LogError("[WeaponUpgradePurchaseZone] SystemManager / PlayerModel 미초기화 상태로 동작 불가.");
                return;
            }

            if (systemManager.PlayerStats == null || systemManager.PlayerStats.Stages.Count == 0)
            {
                Debug.LogError("[WeaponUpgradePurchaseZone] SystemManager.PlayerStats 가 비어있어 동작 불가.");
                return;
            }

            purchaseZone.OnPurchaseCompleted += OnPurchaseCompleted;
            UpdateZoneForNextStage();
        }

        private void OnDestroy()
        {
            if (purchaseZone != null) purchaseZone.OnPurchaseCompleted -= OnPurchaseCompleted;
        }

        private void OnPurchaseCompleted()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;
            if (systemManager.PlayerStats == null) return;

            int nextStageIndex = systemManager.PlayerModel.WeaponUpgradeStage.Value + 1;
            if (nextStageIndex >= systemManager.PlayerStats.Stages.Count) return;

            // RP 변경 → PlayerWeaponSystem 이 자동으로 단계 데이터 적용
            systemManager.PlayerModel.WeaponUpgradeStage.Value = nextStageIndex;

            UpdateZoneForNextStage();
        }

        private void UpdateZoneForNextStage()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;
            if (systemManager.PlayerStats == null) return;

            int currentStage = systemManager.PlayerModel.WeaponUpgradeStage.Value;
            int nextStageIndex = currentStage + 1;

            if (!systemManager.PlayerStats.TryGetStage(nextStageIndex, out WeaponStageData nextStageData))
            {
                // 더 이상 강화 단계 없음 — 구매칸 자체 비활성
                gameObject.SetActive(false);
                return;
            }

            int costForNextStage = nextStageIndex == 1 ? CostStage1 : CostStage2;
            purchaseZone.ResetForNewCost(costForNextStage);
        }
    }
}
