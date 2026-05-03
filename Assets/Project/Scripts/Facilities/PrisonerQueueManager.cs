using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Facilities
{
    public class PrisonerQueueManager : MonoBehaviour
    {
        [Header("Spawn / Layout")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] queueSlots;

        [Header("Handcuff Zones (자식)")]
        [SerializeField] private HandcuffInputZone handcuffInputZone;
        [SerializeField] private HandcuffProcessZone handcuffProcessZone;

        [Header("Prison Cell")]
        [SerializeField] private PrisonCell prisonCell;

        [Header("Handcuff Buffer")]
        [SerializeField, Min(1)] private int handcuffBufferCapacity = 8;

        [Header("Prisoner Config")]
        [SerializeField, Min(1)] private int minRequiredHandcuffs = 1;
        [SerializeField, Min(1)] private int maxRequiredHandcuffs = 4;

        [Header("Money Output")]
        [SerializeField] private MoneyOutput moneyOutput;

        // 죄수 1명 처리당 보상 지폐 수 — Random.Range(min, max+1) 로 4~6장 지급.
        private const int MinBillsPerProcessedPrisoner = 4;
        private const int MaxBillsPerProcessedPrisoner = 6;

        private Prisoner[] prisonersInSlots;
        private IResourceSink headPrisonerHandcuffSink;
        private StockpileModel handcuffBufferStockpile;

        public Prisoner GetFrontPrisoner()
        {
            if (prisonersInSlots == null || prisonersInSlots.Length == 0) return null;
            return prisonersInSlots[0];
        }

        public StockpileModel HandcuffBufferStockpile => handcuffBufferStockpile;
        public HandcuffInputZone HandcuffInputZone => handcuffInputZone;
        public HandcuffProcessZone HandcuffProcessZone => handcuffProcessZone;

        private void Awake()
        {
            handcuffBufferStockpile = new StockpileModel(ResourceType.Handcuff, handcuffBufferCapacity);
            headPrisonerHandcuffSink = new HeadPrisonerHandcuffSink(this);
        }

        private void Start()
        {
            if (queueSlots == null || queueSlots.Length == 0)
            {
                Debug.LogWarning("[PrisonerQueueManager] queueSlots 가 비어있어 큐 시작 불가.");
                return;
            }

            prisonersInSlots = new Prisoner[queueSlots.Length];
            // 한번에 전 슬롯을 채우지 않고, 현재 비어있는 가장 앞 슬롯에 1마리씩 순차 spawn.
            // 다음 spawn 은 직전 spawn 의 prisoner 가 슬롯에 도착했을 때 (FirstArrivedAtQueue) 트리거.
            SpawnNextEmptySlot();

            // 양 zone 에 buffer 와 sink 동시 주입 — buffer 는 queueManager 가 단일 소유.
            // ProcessZone 은 buffer 시각 위치 (= InputZone transform) 도 같이 받아 비행 from 위치를 정확히 산출.
            if (handcuffInputZone != null)
            {
                handcuffInputZone.Init(handcuffBufferStockpile, headPrisonerHandcuffSink);
            }
            if (handcuffProcessZone != null)
            {
                Transform bufferAnchor = handcuffInputZone != null ? handcuffInputZone.BufferStackAnchor : null;
                handcuffProcessZone.Init(handcuffBufferStockpile, headPrisonerHandcuffSink, bufferAnchor);
            }
        }

        private void SpawnNextEmptySlot()
        {
            if (prisonersInSlots == null) return;
            for (int i = 0; i < prisonersInSlots.Length; i++)
            {
                if (prisonersInSlots[i] == null)
                {
                    SpawnPrisonerForSlot(i);
                    return;
                }
            }
        }

        private void SpawnPrisonerForSlot(int _slotIndex)
        {
            if (spawnPoint == null) return;
            if (_slotIndex < 0 || _slotIndex >= queueSlots.Length) return;
            if (prisonCell == null)
            {
                Debug.LogWarning("[PrisonerQueueManager] prisonCell 미연결 — 죄수 spawn 불가.");
                return;
            }

            PoolManager pool = SystemManager.Instance != null ? SystemManager.Instance.Pool : null;
            if (pool == null)
            {
                Debug.LogError("[PrisonerQueueManager] PoolManager 미초기화.");
                return;
            }

            Prisoner instance = pool.Spawn<Prisoner>(spawnPoint.position, spawnPoint.rotation);
            if (instance == null) return;

            int requiredHandcuffs = Random.Range(minRequiredHandcuffs, maxRequiredHandcuffs + 1);
            instance.Init(requiredHandcuffs, queueSlots[_slotIndex], prisonCell.CellPathWaypoints);

            instance.FullyProcessed += HandlePrisonerFullyProcessed;
            instance.EnteredCell += HandlePrisonerEnteredCell;
            instance.FirstArrivedAtQueue += HandlePrisonerFirstArrivedAtQueue;

            prisonersInSlots[_slotIndex] = instance;
        }

        private void HandlePrisonerFirstArrivedAtQueue(Prisoner _prisoner)
        {
            if (_prisoner != null) _prisoner.FirstArrivedAtQueue -= HandlePrisonerFirstArrivedAtQueue;
            // 도착 시 다음 빈 슬롯에 1마리 spawn — 슬롯이 모두 차 있으면 no-op.
            SpawnNextEmptySlot();
        }

        private void HandlePrisonerFullyProcessed(Prisoner _prisoner)
        {
            if (prisonersInSlots == null) return;

            int slotIndex = System.Array.IndexOf(prisonersInSlots, _prisoner);
            if (slotIndex < 0) return;

            for (int i = slotIndex; i < prisonersInSlots.Length - 1; i++)
            {
                prisonersInSlots[i] = prisonersInSlots[i + 1];
                if (prisonersInSlots[i] != null)
                {
                    prisonersInSlots[i].AssignQueueSlot(queueSlots[i]);
                }
            }
            prisonersInSlots[prisonersInSlots.Length - 1] = null;

            // 이전엔 무조건 마지막 슬롯에 spawn 했지만, sequential 모드에서는 가장 앞쪽 빈 슬롯에 spawn.
            // (큐가 아직 다 안 채워진 상태에서 head 가 처리되면 중간이 빌 수 있음.)
            SpawnNextEmptySlot();

            if (moneyOutput != null)
            {
                int billsToReward = Random.Range(MinBillsPerProcessedPrisoner, MaxBillsPerProcessedPrisoner + 1);
                // stockpile 은 won 단위 — 지폐 1장 = MoneyValuePerItem 원.
                moneyOutput.AddMoney(billsToReward * GameValueConstants.MoneyValuePerItem);
            }
        }

        private void HandlePrisonerEnteredCell(Prisoner _prisoner)
        {
            if (_prisoner == null || prisonCell == null) return;

            _prisoner.FullyProcessed -= HandlePrisonerFullyProcessed;
            _prisoner.EnteredCell -= HandlePrisonerEnteredCell;

            if (!prisonCell.AdmitPrisoner(_prisoner))
            {
                // sink 체크 이후 in-flight 중첩으로 정원 초과 — 풀로 반환.
                PoolManager pool = SystemManager.Instance != null ? SystemManager.Instance.Pool : null;
                if (pool != null) pool.Despawn(_prisoner);
                else Destroy(_prisoner.gameObject);
            }
        }

        private sealed class HeadPrisonerHandcuffSink : IResourceSink
        {
            private readonly PrisonerQueueManager owner;

            public HeadPrisonerHandcuffSink(PrisonerQueueManager _owner) { owner = _owner; }

            public ResourceType InputType => ResourceType.Handcuff;

            public bool CanAcceptOne()
            {
                if (!HasPrisonFreeSlot()) return false;
                Prisoner head = owner.GetFrontPrisoner();
                return head != null && head.CanReceiveHandcuff;
            }

            public bool TryAcceptOne()
            {
                if (!HasPrisonFreeSlot()) return false;
                Prisoner head = owner.GetFrontPrisoner();
                if (head == null || !head.CanReceiveHandcuff) return false;
                head.ReceiveOneHandcuff();

                SoundManager sound = SystemManager.Instance != null ? SystemManager.Instance.Sound : null;
                sound?.PlayOneShot(SoundType.HandcuffGiveToPrisoner);

                return true;
            }

            public Transform AnchorTransform
            {
                get
                {
                    Prisoner head = owner.GetFrontPrisoner();
                    return head != null ? head.transform : null;
                }
            }

            private bool HasPrisonFreeSlot()
            {
                // 감옥이 가득차면 더 이상 수갑을 줘서 죄수를 cell 로 보내지 않음. 확장 후 재개.
                SystemManager systemManager = SystemManager.Instance;
                if (systemManager == null || systemManager.Prison == null) return true;
                return systemManager.Prison.HasFreeSlot;
            }
        }
    }
}
