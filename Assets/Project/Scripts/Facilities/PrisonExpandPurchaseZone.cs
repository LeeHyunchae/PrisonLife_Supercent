using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Game;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 감옥 확장 1회 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// 구매 완료 → PrisonCell.Expand → 시네마 (PrisonCell focus) → GameState.CurrentPhase = Ending → 자기 GameObject 비활성.
    /// 비용은 PurchaseZone 의 initialCostAmount 인스펙터 값으로 설정 (스펙: 50).
    /// 감옥 확장은 게임 종료 트리거 (영상 기준).
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class PrisonExpandPurchaseZone : MonoBehaviour
    {
        private const int CostAmount = 50;

        [Header("Expand")]
        [SerializeField] private PrisonCell prisonCell;
        [SerializeField, Min(1)] private int additionalSlots = 20;

        private PurchaseZone purchaseZone;

        private void Awake()
        {
            purchaseZone = GetComponent<PurchaseZone>();
        }

        private void Start()
        {
            if (purchaseZone == null) return;
            purchaseZone.ResetForNewCost(CostAmount);
            purchaseZone.OnPurchaseCompleted += OnPurchaseCompleted;
        }

        private void OnDestroy()
        {
            if (purchaseZone != null) purchaseZone.OnPurchaseCompleted -= OnPurchaseCompleted;
        }

        private void OnPurchaseCompleted()
        {
            if (prisonCell == null)
            {
                Debug.LogWarning("[PrisonExpandPurchaseZone] prisonCell 미연결.");
                return;
            }

            prisonCell.Expand(additionalSlots);
            PlayCinematicThenEndAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid PlayCinematicThenEndAsync(CancellationToken _cancellationToken)
        {
            SystemManager systemManager = SystemManager.Instance;

            try
            {
                if (systemManager != null && systemManager.CameraDirector != null && prisonCell != null)
                {
                    await systemManager.CameraDirector.PlayFocusOnAsync(prisonCell.transform, _cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (systemManager != null && systemManager.GameState != null)
            {
                systemManager.GameState.CurrentPhase.Value = GamePhase.Ending;
            }

            gameObject.SetActive(false);
        }
    }
}
