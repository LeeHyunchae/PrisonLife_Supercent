using System;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using PrisonLife.Reactive;
using PrisonLife.View.World;
using TMPro;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 재사용 구매칸. 같은 GameObject 의 ResourceInputZone(Resource Type=Money) 과 합쳐 동작.
    /// 비용만큼 돈이 누적되면 OnPurchaseCompleted 1회 발사. 외부 시스템(WeaponUpgradePurchaseZone 등)이 후속 처리.
    /// 남은 비용 표시 라벨 (`Cost Remaining Label`) 은 capacity - count 를 표시하고, 0 이 되면 hide.
    /// </summary>
    [RequireComponent(typeof(ResourceInputZone))]
    public class PurchaseZone : MonoBehaviour
    {
        // 초기 cost 는 default 1 won — 각 PurchaseZone 종류 (WeaponUpgrade 등) 가 자기 Start 에서 ResetForNewCost 로 덮어씌운다.
        private const int DefaultInitialCostAmount = 1;

        [Header("Cost Stack Visual")]
        [SerializeField] private Vector3 costStackOffsetStep = new Vector3(0f, 0.05f, 0f);

        [Header("UI")]
        [SerializeField] private TMP_Text costRemainingLabel;

        public StockpileModel CostStockpile { get; private set; }
        public event Action OnPurchaseCompleted;

        private ResourceInputZone siblingMoneyInputZone;
        private StackVisualizer costStockVisualizer;
        private IDisposable costSubscription;
        private IDisposable costLabelCountSubscription;
        private IDisposable costLabelCapacitySubscription;
        private bool hasFiredCompletion;

        private void Awake()
        {
            CostStockpile = new StockpileModel(ResourceType.Money, DefaultInitialCostAmount);
            siblingMoneyInputZone = GetComponent<ResourceInputZone>();
        }

        private void Start()
        {
            if (siblingMoneyInputZone != null) siblingMoneyInputZone.Init(CostStockpile.Sink);

            SystemManager systemManager = SystemManager.Instance;
            PoolManager pool = systemManager != null ? systemManager.Pool : null;
            costStockVisualizer = new StackVisualizer(
                CostStockpile.Count,
                transform,
                ResourceType.Money,
                costStackOffsetStep,
                pool,
                GameValueConstants.MoneyValuePerItem);

            BindCompletionWatcher();
            BindCostRemainingLabel();
        }

        public void ResetForNewCost(int _newCostAmount)
        {
            if (CostStockpile == null) return;
            CostStockpile.Count.Value = 0;
            CostStockpile.SetCapacity(_newCostAmount);
            hasFiredCompletion = false;
        }

        private void BindCompletionWatcher()
        {
            costSubscription?.Dispose();
            costSubscription = CostStockpile.Count.Subscribe(OnCostCountChanged);
        }

        private void BindCostRemainingLabel()
        {
            if (costRemainingLabel == null) return;

            costLabelCountSubscription?.Dispose();
            costLabelCapacitySubscription?.Dispose();

            costLabelCountSubscription = CostStockpile.Count.Subscribe(_ => UpdateCostRemainingLabel());
            costLabelCapacitySubscription = CostStockpile.Capacity.Subscribe(_ => UpdateCostRemainingLabel());
        }

        private void UpdateCostRemainingLabel()
        {
            if (costRemainingLabel == null) return;
            // capacity / count 자체가 won — 그대로 표시.
            int remaining = Mathf.Max(0, CostStockpile.Capacity.Value - CostStockpile.Count.Value);
            costRemainingLabel.text = remaining.ToString();
            costRemainingLabel.gameObject.SetActive(remaining > 0);
        }

        private void OnCostCountChanged(int _currentCount)
        {
            if (hasFiredCompletion) return;
            if (_currentCount < CostStockpile.Capacity.Value) return;

            hasFiredCompletion = true;

            SoundManager sound = SystemManager.Instance != null ? SystemManager.Instance.Sound : null;
            sound?.PlayOneShot(SoundType.PurchaseComplete);

            OnPurchaseCompleted?.Invoke();
        }

        private void OnDestroy()
        {
            costSubscription?.Dispose();
            costSubscription = null;
            costLabelCountSubscription?.Dispose();
            costLabelCountSubscription = null;
            costLabelCapacitySubscription?.Dispose();
            costLabelCapacitySubscription = null;
            costStockVisualizer?.Dispose();
            costStockVisualizer = null;
        }
    }
}
