using PrisonLife.Entities;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 광부 일꾼 1회 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// 구매 완료 → minersPerPurchase (기본 3) 명을 spawnPoints 에 분산 spawn → 자기 GameObject 비활성.
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class MinerHirePurchaseZone : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private MinerWorker minerWorkerPrefab;
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
            if (minerWorkerPrefab == null)
            {
                Debug.LogWarning("[MinerHirePurchaseZone] minerWorkerPrefab 미연결.");
                return;
            }

            if (handcuffContainer == null || handcuffContainer.OreStockpile == null)
            {
                Debug.LogWarning("[MinerHirePurchaseZone] HandcuffContainer / OreStockpile 미초기화.");
                return;
            }

            var rocks = Object.FindObjectsByType<MineableRock>(FindObjectsSortMode.None);
            var oreSink = handcuffContainer.OreStockpile.Sink;

            for (int i = 0; i < minersPerPurchase; i++)
            {
                var spawnPoint = ResolveSpawnPoint(i);
                var instance = Instantiate(minerWorkerPrefab, spawnPoint.position, spawnPoint.rotation);
                instance.Init(rocks, oreSink);
            }
        }

        private Transform ResolveSpawnPoint(int _index)
        {
            if (minerSpawnPoints == null || minerSpawnPoints.Length == 0) return transform;
            var spawnPoint = minerSpawnPoints[_index % minerSpawnPoints.Length];
            return spawnPoint != null ? spawnPoint : transform;
        }
    }
}
