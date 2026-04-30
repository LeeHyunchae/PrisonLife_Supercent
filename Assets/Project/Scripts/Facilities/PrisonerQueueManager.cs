using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Game;
using UnityEngine;

namespace PrisonLife.Facilities
{
    public class PrisonerQueueManager : MonoBehaviour
    {
        [Header("Spawn / Layout")]
        [SerializeField] private Prisoner prisonerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform[] queueSlots;
        [SerializeField] private Transform prisonCellPoint;

        [Header("Process Zone (자식 — PrisonerProcessZone 컴포넌트, 버퍼 포함)")]
        [SerializeField] private PrisonerProcessZone handcuffDeliveryZone;

        [Header("Prisoner Config")]
        [SerializeField, Min(1)] private int minRequiredHandcuffs = 1;
        [SerializeField, Min(1)] private int maxRequiredHandcuffs = 4;

        [Header("Money Output")]
        [SerializeField] private MoneyOutput moneyOutput;
        [SerializeField, Min(1)] private int moneyPerProcessedPrisoner = 1;

        private Prisoner[] prisonersInSlots;
        private IResourceSink headPrisonerHandcuffSink;

        public Prisoner GetFrontPrisoner()
        {
            if (prisonersInSlots == null || prisonersInSlots.Length == 0) return null;
            return prisonersInSlots[0];
        }

        private void Awake()
        {
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
            for (int i = 0; i < queueSlots.Length; i++)
            {
                SpawnPrisonerForSlot(i);
            }

            if (handcuffDeliveryZone != null)
            {
                handcuffDeliveryZone.Init(headPrisonerHandcuffSink);
            }
        }

        private void SpawnPrisonerForSlot(int _slotIndex)
        {
            if (prisonerPrefab == null || spawnPoint == null) return;
            if (_slotIndex < 0 || _slotIndex >= queueSlots.Length) return;

            var instance = Instantiate(prisonerPrefab, spawnPoint.position, spawnPoint.rotation);
            int requiredHandcuffs = Random.Range(minRequiredHandcuffs, maxRequiredHandcuffs + 1);
            instance.Init(requiredHandcuffs, queueSlots[_slotIndex], prisonCellPoint);

            instance.FullyProcessed += HandlePrisonerFullyProcessed;
            instance.EnteredCell += HandlePrisonerEnteredCell;

            prisonersInSlots[_slotIndex] = instance;
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

            SpawnPrisonerForSlot(prisonersInSlots.Length - 1);

            if (moneyOutput != null)
            {
                moneyOutput.AddMoney(moneyPerProcessedPrisoner);
            }
        }

        private void HandlePrisonerEnteredCell(Prisoner _prisoner)
        {
            if (_prisoner == null) return;

            _prisoner.FullyProcessed -= HandlePrisonerFullyProcessed;
            _prisoner.EnteredCell -= HandlePrisonerEnteredCell;

            if (SystemManager.Instance != null && SystemManager.Instance.Prison != null)
            {
                SystemManager.Instance.Prison.TryAdmitOne();
            }

            Destroy(_prisoner.gameObject);
        }

        private sealed class HeadPrisonerHandcuffSink : IResourceSink
        {
            private readonly PrisonerQueueManager owner;

            public HeadPrisonerHandcuffSink(PrisonerQueueManager _owner) { owner = _owner; }

            public ResourceType InputType => ResourceType.Handcuff;

            public bool CanAcceptOne()
            {
                var head = owner.GetFrontPrisoner();
                return head != null && head.CanReceiveHandcuff;
            }

            public bool TryAcceptOne()
            {
                var head = owner.GetFrontPrisoner();
                if (head == null || !head.CanReceiveHandcuff) return false;
                head.ReceiveOneHandcuff();
                return true;
            }
        }
    }
}
