using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using PrisonLife.View.World;
using UnityEngine;

namespace PrisonLife.Facilities
{
    public class HandcuffContainer : MonoBehaviour
    {
        
        private int initialMaxOreStorage = 50;
        private int initialMaxHandcuffStorage = 20;

        private float initialProductionPeriodSeconds = 0.25f;

        [Header("Zones (자식 prefab — stack 시각도 zone 위치에 쌓임)")]
        [SerializeField] private ResourceInputZone oreInputZone;
        [SerializeField] private ResourceOutputZone handcuffOutputZone;

        [Header("Stack Offset")]
        [SerializeField] private Vector3 oreStackOffsetStep = new Vector3(0f, 0.25f, 0f);
        [SerializeField] private Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);

        public StockpileModel OreStockpile { get; private set; }
        public StockpileModel HandcuffStockpile { get; private set; }

        private HandcuffProductionSystem productionSystem;
        private StackVisualizer oreStockVisualizer;
        private StackVisualizer handcuffStockVisualizer;

        private void Awake()
        {
            OreStockpile = new StockpileModel(ResourceType.Ore, initialMaxOreStorage);
            HandcuffStockpile = new StockpileModel(ResourceType.Handcuff, initialMaxHandcuffStorage);
        }

        private void Start()
        {
            if (oreInputZone != null) oreInputZone.Init(OreStockpile.Sink);
            if (handcuffOutputZone != null) handcuffOutputZone.Init(HandcuffStockpile.Source);

            SystemManager systemManager = SystemManager.Instance;
            PoolManager pool = systemManager != null ? systemManager.Pool : null;

            if (oreInputZone != null)
            {
                oreStockVisualizer = new StackVisualizer(
                    OreStockpile.Count,
                    oreInputZone.transform,
                    ResourceType.Ore,
                    oreStackOffsetStep,
                    pool);
            }

            if (handcuffOutputZone != null)
            {
                handcuffStockVisualizer = new StackVisualizer(
                    HandcuffStockpile.Count,
                    handcuffOutputZone.transform,
                    ResourceType.Handcuff,
                    handcuffStackOffsetStep,
                    pool);
            }

            productionSystem = new HandcuffProductionSystem(
                OreStockpile,
                HandcuffStockpile,
                initialProductionPeriodSeconds,
                destroyCancellationToken);
            productionSystem.Start();
        }

        private void OnDestroy()
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
