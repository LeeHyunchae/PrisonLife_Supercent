using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using PrisonLife.View.World;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 같은 GameObject 의 ResourceOutputZone 과 합쳐서 동작.
    /// Awake 에 stockpile 생성 → Start 에 형제 ResourceOutputZone 에 source 주입 + 시각화.
    /// 별도 자식 GameObject 없음.
    /// </summary>
    [RequireComponent(typeof(ResourceOutputZone))]
    public class MoneyOutput : MonoBehaviour
    {
        // 머니 stockpile 최대치 (won 단위) — 코드 상수.
        private const int InitialMaxStorage = 100;

        [Header("Stack Offset")]
        [SerializeField] private Vector3 moneyStackOffsetStep = new Vector3(0f, 0.1f, 0f);

        public StockpileModel MoneyStockpile { get; private set; }

        private ResourceOutputZone siblingOutputZone;
        private StackVisualizer moneyStockVisualizer;

        private void Awake()
        {
            MoneyStockpile = new StockpileModel(ResourceType.Money, InitialMaxStorage);
            siblingOutputZone = GetComponent<ResourceOutputZone>();
        }

        private void Start()
        {
            if (siblingOutputZone != null) siblingOutputZone.Init(MoneyStockpile.Source);

            SystemManager systemManager = SystemManager.Instance;
            PoolManager pool = systemManager != null ? systemManager.Pool : null;
            moneyStockVisualizer = new StackVisualizer(
                MoneyStockpile.Count,
                transform,
                ResourceType.Money,
                moneyStackOffsetStep,
                pool,
                GameValueConstants.MoneyValuePerItem);
        }

        public void AddMoney(int _amount)
        {
            MoneyStockpile?.TryAdd(_amount);
        }

        private void OnDestroy()
        {
            moneyStockVisualizer?.Dispose();
            moneyStockVisualizer = null;
        }
    }
}
