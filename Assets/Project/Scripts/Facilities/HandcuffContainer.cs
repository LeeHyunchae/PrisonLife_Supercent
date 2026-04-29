using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Models;
using PrisonLife.View.World;
using UnityEngine;

namespace PrisonLife.Facilities
{
    public class HandcuffContainer : MonoBehaviour
    {
        [Header("Capacities")]
        [SerializeField, Min(1)] int initialMaxOreStorage = 20;
        [SerializeField, Min(1)] int initialMaxHandcuffStorage = 8;

        [Header("Production")]
        [SerializeField, Min(0.05f)] float initialProductionPeriodSeconds = 1.0f;

        [Header("Zones (자식 prefab — stack 시각도 zone 위치에 쌓임)")]
        [SerializeField] ResourceInputZone oreInputZone;
        [SerializeField] ResourceOutputZone handcuffOutputZone;

        [Header("Stack Offset")]
        [SerializeField] Vector3 oreStackOffsetStep = new Vector3(0f, 0.25f, 0f);
        [SerializeField] Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);

        public StockpileModel OreStockpile { get; private set; }
        public StockpileModel HandcuffStockpile { get; private set; }

        HandcuffProductionSystem productionSystem;
        StackVisualizer oreStockVisualizer;
        StackVisualizer handcuffStockVisualizer;

        void Awake()
        {
            OreStockpile = new StockpileModel(ResourceType.Ore, initialMaxOreStorage);
            HandcuffStockpile = new StockpileModel(ResourceType.Handcuff, initialMaxHandcuffStorage);
        }

        void Start()
        {
            if (oreInputZone != null) oreInputZone.Init(OreStockpile.Sink);
            if (handcuffOutputZone != null) handcuffOutputZone.Init(HandcuffStockpile.Source);

            var registry = SystemManager.Instance != null ? SystemManager.Instance.ResourceItems : null;

            if (oreInputZone != null)
            {
                oreStockVisualizer = new StackVisualizer(
                    OreStockpile.Count,
                    oreInputZone.transform,
                    registry != null ? registry.GetPrefab(ResourceType.Ore) : null,
                    oreStackOffsetStep);
            }

            if (handcuffOutputZone != null)
            {
                handcuffStockVisualizer = new StackVisualizer(
                    HandcuffStockpile.Count,
                    handcuffOutputZone.transform,
                    registry != null ? registry.GetPrefab(ResourceType.Handcuff) : null,
                    handcuffStackOffsetStep);
            }

            productionSystem = new HandcuffProductionSystem(
                OreStockpile,
                HandcuffStockpile,
                initialProductionPeriodSeconds,
                destroyCancellationToken);
            productionSystem.Start();
        }

        void OnDestroy()
        {
            productionSystem?.Dispose();
            productionSystem = null;
            oreStockVisualizer?.Dispose();
            oreStockVisualizer = null;
            handcuffStockVisualizer?.Dispose();
            handcuffStockVisualizer = null;
        }
    }
}
