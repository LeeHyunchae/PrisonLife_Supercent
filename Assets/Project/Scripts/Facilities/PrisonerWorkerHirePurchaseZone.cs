using PrisonLife.Entities;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 죄수 처리 일꾼 1회 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// 구매 완료 → workersPerPurchase 명 spawn → 자기 GameObject 비활성.
    /// 워커는 컨테이너 출력 → InputZone (deposit) → ProcessZone (drain 감시) 루프.
    /// PrisonerWorker prefab 은 PoolManager 가 소유 (호출자는 type 으로만 요청).
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class PrisonerWorkerHirePurchaseZone : MonoBehaviour
    {
        private const int CostAmount = 50;

        [Header("Spawn")]
        [SerializeField] private Transform[] workerSpawnPoints;
        [SerializeField, Min(1)] private int workersPerPurchase = 1;

        [Header("Worker Targets")]
        [SerializeField] private HandcuffContainer handcuffContainer;
        [SerializeField] private Transform containerOutputZoneTransform;
        [SerializeField] private PrisonerQueueManager prisonerQueueManager;

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
            SpawnWorkers();
            gameObject.SetActive(false);
        }

        private void SpawnWorkers()
        {
            if (handcuffContainer == null || handcuffContainer.HandcuffStockpile == null)
            {
                Debug.LogWarning("[PrisonerWorkerHirePurchaseZone] HandcuffContainer / HandcuffStockpile 미초기화.");
                return;
            }

            if (containerOutputZoneTransform == null || prisonerQueueManager == null)
            {
                Debug.LogWarning("[PrisonerWorkerHirePurchaseZone] containerOutput / queueManager 미연결.");
                return;
            }

            HandcuffInputZone inputZone = prisonerQueueManager.HandcuffInputZone;
            HandcuffProcessZone processZone = prisonerQueueManager.HandcuffProcessZone;
            StockpileModel bufferStockpile = prisonerQueueManager.HandcuffBufferStockpile;

            if (inputZone == null || processZone == null)
            {
                Debug.LogWarning("[PrisonerWorkerHirePurchaseZone] queueManager 의 InputZone/ProcessZone 미설정.");
                return;
            }

            PoolManager pool = SystemManager.Instance != null ? SystemManager.Instance.Pool : null;
            if (pool == null)
            {
                Debug.LogError("[PrisonerWorkerHirePurchaseZone] PoolManager 미초기화.");
                return;
            }

            StockpileModel containerStockpile = handcuffContainer.HandcuffStockpile;

            for (int i = 0; i < workersPerPurchase; i++)
            {
                Transform spawnPoint = ResolveSpawnPoint(i);
                PrisonerWorker instance = pool.Spawn<PrisonerWorker>(spawnPoint.position, spawnPoint.rotation);
                if (instance == null) continue;
                instance.Init(
                    containerOutputZoneTransform,
                    containerStockpile,
                    inputZone.transform,
                    bufferStockpile,
                    processZone.transform);
            }
        }

        private Transform ResolveSpawnPoint(int _index)
        {
            if (workerSpawnPoints == null || workerSpawnPoints.Length == 0) return transform;
            Transform spawnPoint = workerSpawnPoints[_index % workerSpawnPoints.Length];
            return spawnPoint != null ? spawnPoint : transform;
        }
    }
}
