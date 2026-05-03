using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Game;
using PrisonLife.Managers;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 광부 일꾼 1회 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// 구매 완료 → minersPerPurchase (기본 3) 명을 spawnPoints 에 분산 spawn → 자기 GameObject 비활성.
    /// MinerWorker prefab 은 PoolManager 가 소유 (호출자는 type 으로만 요청).
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class MinerHirePurchaseZone : MonoBehaviour
    {
        private const int CostAmount = 50;

        [Header("Spawn")]
        [SerializeField] private Transform[] minerSpawnPoints;
        [SerializeField, Min(1)] private int minersPerPurchase = 3;

        [Header("Sink")]
        [SerializeField] private HandcuffContainer handcuffContainer;

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
            SpawnMiners();
            gameObject.SetActive(false);
        }

        private void SpawnMiners()
        {
            if (handcuffContainer == null || handcuffContainer.OreStockpile == null)
            {
                Debug.LogWarning("[MinerHirePurchaseZone] HandcuffContainer / OreStockpile 미초기화.");
                return;
            }

            SystemManager systemManager = SystemManager.Instance;
            PoolManager pool = systemManager != null ? systemManager.Pool : null;
            RockArea rockArea = systemManager != null ? systemManager.RockArea : null;
            if (pool == null)
            {
                Debug.LogError("[MinerHirePurchaseZone] PoolManager 미초기화.");
                return;
            }
            if (rockArea == null)
            {
                Debug.LogError("[MinerHirePurchaseZone] RockArea 미초기화.");
                return;
            }

            IReadOnlyList<MineableRock> rocks = rockArea.Rocks;
            IResourceSink oreSink = handcuffContainer.OreStockpile.Sink;

            for (int i = 0; i < minersPerPurchase; i++)
            {
                Transform spawnPoint = ResolveSpawnPoint(i);
                MinerWorker instance = pool.Spawn<MinerWorker>(spawnPoint.position, spawnPoint.rotation);
                if (instance == null) continue;
                instance.Init(rocks, oreSink);
            }
        }

        private Transform ResolveSpawnPoint(int _index)
        {
            if (minerSpawnPoints == null || minerSpawnPoints.Length == 0) return transform;
            Transform spawnPoint = minerSpawnPoints[_index % minerSpawnPoints.Length];
            return spawnPoint != null ? spawnPoint : transform;
        }
    }
}
