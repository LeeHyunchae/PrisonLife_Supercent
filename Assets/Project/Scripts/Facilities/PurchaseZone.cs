using System;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Models;
using PrisonLife.Reactive;
using PrisonLife.View.World;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 재사용 구매칸. 같은 GameObject 의 ResourceInputZone(Resource Type=Money) 과 합쳐 동작.
    /// 비용만큼 돈이 누적되면 OnPurchaseCompleted 1회 발사. 외부 시스템(WeaponUpgradePurchaseZone 등)이 후속 처리.
    /// </summary>
    [RequireComponent(typeof(ResourceInputZone))]
    public class PurchaseZone : MonoBehaviour
    {
        [Header("Cost")]
        [SerializeField, Min(1)] int initialCostAmount = 100;

        [Header("Cost Stack Visual")]
        [SerializeField] Vector3 costStackOffsetStep = new Vector3(0f, 0.05f, 0f);

        public StockpileModel CostStockpile { get; private set; }
        public event Action OnPurchaseCompleted;

        ResourceInputZone siblingMoneyInputZone;
        StackVisualizer costStockVisualizer;
        IDisposable costSubscription;
        bool hasFiredCompletion;

        void Awake()
        {
            CostStockpile = new StockpileModel(ResourceType.Money, initialCostAmount);
            siblingMoneyInputZone = GetComponent<ResourceInputZone>();
        }

        void Start()
        {
            if (siblingMoneyInputZone != null) siblingMoneyInputZone.Init(CostStockpile.Sink);

            var registry = SystemManager.Instance != null ? SystemManager.Instance.ResourceItems : null;
            costStockVisualizer = new StackVisualizer(
                CostStockpile.Count,
                transform,
                registry != null ? registry.GetPrefab(ResourceType.Money) : null,
                costStackOffsetStep);

            BindCompletionWatcher();
        }

        public void ResetForNewCost(int _newCostAmount)
        {
            if (CostStockpile == null) return;
            CostStockpile.Count.Value = 0;
            CostStockpile.SetCapacity(_newCostAmount);
            hasFiredCompletion = false;
        }

        void BindCompletionWatcher()
        {
            costSubscription?.Dispose();
            costSubscription = CostStockpile.Count.Subscribe(OnCostCountChanged);
        }

        void OnCostCountChanged(int _currentCount)
        {
            if (hasFiredCompletion) return;
            if (_currentCount < CostStockpile.Capacity.Value) return;

            hasFiredCompletion = true;
            OnPurchaseCompleted?.Invoke();
        }

        void OnDestroy()
        {
            costSubscription?.Dispose();
            costSubscription = null;
            costStockVisualizer?.Dispose();
            costStockVisualizer = null;
        }
    }
}
