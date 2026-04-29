using PrisonLife.Core;
using PrisonLife.Game;
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
        [Header("Capacity")]
        [SerializeField, Min(1)] int initialMaxStorage = 50;

        [Header("Stack Offset")]
        [SerializeField] Vector3 moneyStackOffsetStep = new Vector3(0f, 0.1f, 0f);

        public StockpileModel MoneyStockpile { get; private set; }

        ResourceOutputZone siblingOutputZone;
        StackVisualizer moneyStockVisualizer;

        void Awake()
        {
            MoneyStockpile = new StockpileModel(ResourceType.Money, initialMaxStorage);
            siblingOutputZone = GetComponent<ResourceOutputZone>();
        }

        void Start()
        {
            if (siblingOutputZone != null) siblingOutputZone.Init(MoneyStockpile.Source);

            var registry = SystemManager.Instance != null ? SystemManager.Instance.ResourceItems : null;
            moneyStockVisualizer = new StackVisualizer(
                MoneyStockpile.Count,
                transform,
                registry != null ? registry.GetPrefab(ResourceType.Money) : null,
                moneyStackOffsetStep);
        }

        public void AddMoney(int _amount)
        {
            MoneyStockpile?.TryAdd(_amount);
        }

        void OnDestroy()
        {
            moneyStockVisualizer?.Dispose();
            moneyStockVisualizer = null;
        }
    }
}
