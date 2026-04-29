using System.Collections.Generic;
using PrisonLife.Configs;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Managers;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Game
{
    [DefaultExecutionOrder(-1000)]
    public class SystemManager : MonoBehaviour
    {
        public static SystemManager Instance { get; private set; }

        [Header("Configs (Inspector)")]
        [SerializeField] ResourceItemRegistry resourceItemRegistry;

        [Header("MonoBehaviour Managers (Inspector)")]
        [SerializeField] PoolManager poolManager;
        [SerializeField] NavManager navManager;

        [Header("Scene Player (Inspector)")]
        [SerializeField] Player playerEntity;

        [Header("Player Initial Stats")]
        [SerializeField] int initialOreCapacity = 10;
        [SerializeField] int initialMoneyCapacity = 30;
        [SerializeField] int initialHandcuffCapacity = 6;
        [SerializeField] float initialPlayerMoveSpeed = 5f;

        [Header("Player Initial Mining Stats (Stage 0 — 곡괭이)")]
        private int initialMiningPower = 1;
        private float initialMiningSwingDurationSeconds = 0.7f;
        private int initialMiningHitsPerSwing = 1;
        private float initialMiningRangeWidth = 1.0f;
        private float initialMiningRangeDepth = 1.0f;

        public PoolManager Pool => poolManager;
        public NavManager Nav => navManager;
        public ItemFlowManager ItemFlow { get; private set; }
        public ResourceItemRegistry ResourceItems => resourceItemRegistry;

        public WalletModel Wallet { get; private set; }
        public PrisonStateModel Prison { get; private set; }
        public GameStateModel GameState { get; private set; }
        public PlayerModel PlayerModel { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Wallet = new WalletModel();
            Prison = new PrisonStateModel();
            GameState = new GameStateModel();
            ItemFlow = new ItemFlowManager(poolManager);

            PlayerModel = CreatePlayerModel();
        }

        void Start()
        {
            // 의존성 주입은 Start 에서. Awake 단계에서 호출하면
            // 대상 엔티티들의 Awake (예: Player 의 NavMeshAgent 캐싱) 가 아직 안 돈 상태여서
            // 내부 참조가 null 인 채로 서브시스템이 생성된다.
            InjectPlayer();
        }

        PlayerModel CreatePlayerModel()
        {
            var initialCapacities = new Dictionary<ResourceType, int>
            {
                { ResourceType.Ore, initialOreCapacity },
                { ResourceType.Money, initialMoneyCapacity },
                { ResourceType.Handcuff, initialHandcuffCapacity },
            };

            var inventory = new InventoryModel(initialCapacities);
            var model = new PlayerModel(inventory);
            model.MoveSpeed.Value = initialPlayerMoveSpeed;
            model.MiningPower.Value = initialMiningPower;
            model.MiningSwingDurationSeconds.Value = initialMiningSwingDurationSeconds;
            model.MiningHitsPerSwing.Value = initialMiningHitsPerSwing;
            model.MiningRangeWidth.Value = initialMiningRangeWidth;
            model.MiningRangeDepth.Value = initialMiningRangeDepth;
            return model;
        }

        void InjectPlayer()
        {
            if (playerEntity == null)
            {
                Debug.LogWarning("[SystemManager] playerEntity 가 인스펙터에 연결되지 않았습니다.");
                return;
            }
            playerEntity.Init(PlayerModel);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
