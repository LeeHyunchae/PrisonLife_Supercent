using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Models;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 무기 강화 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// SystemManager.PlayerStats (PlayerStatsConfigSO) 를 기준으로 PlayerModel.WeaponUpgradeStage 변화 → 단계 데이터 적용.
    /// 구매 완료 → 다음 단계로 stage 증가 → 다음 단계 비용으로 PurchaseZone 리셋. 더 이상 단계가 없으면 GameObject 비활성.
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class WeaponUpgradePurchaseZone : MonoBehaviour
    {
        PurchaseZone purchaseZone;

        void Awake()
        {
            purchaseZone = GetComponent<PurchaseZone>();
        }

        void Start()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;

            if (systemManager.PlayerStats == null || systemManager.PlayerStats.Stages.Count == 0)
            {
                Debug.LogError("[WeaponUpgradePurchaseZone] SystemManager.PlayerStats 가 비어있어 동작 불가.");
                return;
            }

            systemManager.PlayerModel.WeaponUpgradeStage
                .Subscribe(ApplyStageData)
                .AddTo(this);

            purchaseZone.OnPurchaseCompleted += OnPurchaseCompleted;

            UpdateZoneForNextStage();
        }

        void OnDestroy()
        {
            if (purchaseZone != null) purchaseZone.OnPurchaseCompleted -= OnPurchaseCompleted;
        }

        void OnPurchaseCompleted()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;
            if (systemManager.PlayerStats == null) return;

            int nextStageIndex = systemManager.PlayerModel.WeaponUpgradeStage.Value + 1;
            if (nextStageIndex >= systemManager.PlayerStats.Stages.Count) return;

            systemManager.PlayerModel.WeaponUpgradeStage.Value = nextStageIndex;
            UpdateZoneForNextStage();
        }

        void UpdateZoneForNextStage()
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;
            if (systemManager.PlayerStats == null) return;

            int currentStage = systemManager.PlayerModel.WeaponUpgradeStage.Value;
            int nextStageIndex = currentStage + 1;

            if (!systemManager.PlayerStats.TryGetStage(nextStageIndex, out var nextStageData))
            {
                // 더 이상 강화 단계 없음 — 구매칸 자체 비활성
                gameObject.SetActive(false);
                return;
            }

            purchaseZone.ResetForNewCost(nextStageData.upgradeCost);
        }

        void ApplyStageData(int _stageIndex)
        {
            var systemManager = SystemManager.Instance;
            if (systemManager == null) return;
            if (systemManager.PlayerStats == null) return;

            if (!systemManager.PlayerStats.TryGetStage(_stageIndex, out var stageData)) return;

            var playerModel = systemManager.PlayerModel;
            if (playerModel != null)
            {
                playerModel.MiningSwingDurationSeconds.Value = stageData.swingDurationSeconds;
                playerModel.MiningHitsPerSwing.Value = stageData.hitsPerSwing;
                playerModel.MiningRangeWidth.Value = stageData.miningRangeWidth;
                playerModel.MiningRangeDepth.Value = stageData.miningRangeDepth;
                if (playerModel.Inventory != null)
                {
                    playerModel.Inventory.SetCapacity(ResourceType.Ore, stageData.oreCapacity);
                }
            }

            var playerEntity = systemManager.PlayerEntity;
            if (playerEntity != null)
            {
                playerEntity.SetWeaponVisual(stageData.weaponVisualPrefab);
            }
        }
    }
}
