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

        [Header("Zones (자식 prefab)")]
        [SerializeField] ResourceInputZone oreInputZone;
        [SerializeField] ResourceOutputZone handcuffOutputZone;

        [Header("Stack Anchors (자식)")]
        [SerializeField] Transform oreStackAnchor;
        [SerializeField] Transform handcuffStackAnchor;

        [Header("Stack Prefabs")]
        [SerializeField] GameObject oreStackItemPrefab;
        [SerializeField] Vector3 oreStackOffsetStep = new Vector3(0f, 0.25f, 0f);
        [SerializeField] GameObject handcuffStackItemPrefab;
        [SerializeField] Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);

        public HandcuffContainerModel Model { get; private set; }

        HandcuffProductionSystem productionSystem;
        StackVisualizer oreStockVisualizer;
        StackVisualizer handcuffStockVisualizer;

        void Awake()
        {
            Model = new HandcuffContainerModel();
            Model.MaxOreStorage.Value = initialMaxOreStorage;
            Model.MaxHandcuffStorage.Value = initialMaxHandcuffStorage;
            Model.ProductionPeriodSeconds.Value = initialProductionPeriodSeconds;
        }

        void Start()
        {
            if (oreInputZone != null) oreInputZone.Init(Model.OreSink);
            if (handcuffOutputZone != null) handcuffOutputZone.Init(Model.HandcuffSource);

            oreStockVisualizer = new StackVisualizer(
                Model.StoredOreCount,
                oreStackAnchor,
                oreStackItemPrefab,
                oreStackOffsetStep);

            handcuffStockVisualizer = new StackVisualizer(
                Model.ProducedHandcuffCount,
                handcuffStackAnchor,
                handcuffStackItemPrefab,
                handcuffStackOffsetStep);

            productionSystem = new HandcuffProductionSystem(Model, destroyCancellationToken);
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
