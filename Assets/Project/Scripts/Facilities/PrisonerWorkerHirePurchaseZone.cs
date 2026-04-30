using PrisonLife.Entities;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 죄수 처리 일꾼 1회 구매칸. 같은 GameObject 의 PurchaseZone 과 합쳐 동작.
    /// 구매 완료 → workersPerPurchase 명을 spawn → 자기 GameObject 비활성.
    /// 워커는 컨테이너 출력 zone 과 처리 zone 사이를 왕복하며 IInventoryHolder 로서 자동 transfer.
    /// </summary>
    [RequireComponent(typeof(PurchaseZone))]
    public class PrisonerWorkerHirePurchaseZone : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private PrisonerWorker prisonerWorkerPrefab;
        [SerializeField] private Transform[] workerSpawnPoints;
        [SerializeField, Min(1)] private int workersPerPurchase = 1;

        [Header("Worker Targets")]
        [SerializeField] private HandcuffContainer handcuffContainer;
        [SerializeField] private Transform handcuffOutputZoneTransform;
        [SerializeField] private Transform prisonerProcessZoneTransform;

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
            SpawnWorkers();
            gameObject.SetActive(false);
        }

        private void SpawnWorkers()
        {
            if (prisonerWorkerPrefab == null)
            {
                Debug.LogWarning("[PrisonerWorkerHirePurchaseZone] prisonerWorkerPrefab 미연결.");
                return;
            }

            if (handcuffContainer == null || handcuffContainer.HandcuffStockpile == null)
            {
                Debug.LogWarning("[PrisonerWorkerHirePurchaseZone] HandcuffContainer / HandcuffStockpile 미초기화.");
                return;
            }

            if (handcuffOutputZoneTransform == null || prisonerProcessZoneTransform == null)
            {
                Debug.LogWarning("[PrisonerWorkerHirePurchaseZone] 출력/처리 zone transform 미연결.");
                return;
            }

            var stockpile = handcuffContainer.HandcuffStockpile;

            for (int i = 0; i < workersPerPurchase; i++)
            {
                var spawnPoint = ResolveSpawnPoint(i);
                var instance = Instantiate(prisonerWorkerPrefab, spawnPoint.position, spawnPoint.rotation);
                instance.Init(handcuffOutputZoneTransform, prisonerProcessZoneTransform, stockpile);
            }
        }

        private Transform ResolveSpawnPoint(int _index)
        {
            if (workerSpawnPoints == null || workerSpawnPoints.Length == 0) return transform;
            var spawnPoint = workerSpawnPoints[_index % workerSpawnPoints.Length];
            return spawnPoint != null ? spawnPoint : transform;
        }
    }
}
