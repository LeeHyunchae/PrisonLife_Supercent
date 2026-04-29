using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Game;
using UnityEngine;

namespace PrisonLife.Facilities
{
    public class PrisonerQueueManager : MonoBehaviour
    {
        [Header("Spawn / Layout")]
        [SerializeField] Prisoner prisonerPrefab;
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform[] queueSlots;
        [SerializeField] Transform prisonCellPoint;

        [Header("Process Zone (자식 prefab — Resource Type=Handcuff)")]
        [SerializeField] ResourceInputZone handcuffDeliveryZone;

        [Header("Prisoner Config")]
        [SerializeField, Min(1)] int minRequiredHandcuffs = 1;
        [SerializeField, Min(1)] int maxRequiredHandcuffs = 4;

        [Header("Money Output")]
        [SerializeField] MoneyOutput moneyOutput;
        [SerializeField, Min(1)] int moneyPerProcessedPrisoner = 1;

        Prisoner[] prisonersInSlots;
        IResourceSink headPrisonerHandcuffSink;

        public Prisoner GetFrontPrisoner()
        {
            if (prisonersInSlots == null || prisonersInSlots.Length == 0) return null;
            return prisonersInSlots[0];
        }

        void Awake()
        {
            headPrisonerHandcuffSink = new HeadPrisonerHandcuffSink(this);
        }

        void Start()
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

        void SpawnPrisonerForSlot(int _slotIndex)
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

        void HandlePrisonerFullyProcessed(Prisoner _prisoner)
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

        void HandlePrisonerEnteredCell(Prisoner _prisoner)
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

        sealed class HeadPrisonerHandcuffSink : IResourceSink
        {
            readonly PrisonerQueueManager owner;
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
