using System.Collections.Generic;
using PrisonLife.Configs;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Facilities;
using PrisonLife.Managers;
using PrisonLife.Models;
using PrisonLife.View;
using UnityEngine;

namespace PrisonLife.Game
{
    [DefaultExecutionOrder(-1000)]
    public class SystemManager : MonoBehaviour
    {
        public static SystemManager Instance { get; private set; }

        [Header("Configs (Inspector)")]
        [SerializeField] private ResourceItemRegistry resourceItemRegistry;
        [SerializeField] private PlayerStatsConfigSO playerStatsConfig;

        [Header("MonoBehaviour Managers (Inspector)")]
        [SerializeField] private PoolManager poolManager;
        [SerializeField] private NavManager navManager;
        [SerializeField] private SoundManager soundManager;

        [Header("Scene Player (Inspector)")]
        [SerializeField] private Player playerEntity;

        [Header("Scene Facilities (Inspector)")]
        [SerializeField] private RockArea rockArea;

        [Header("Scene Camera (Inspector)")]
        [SerializeField] private CameraDirector cameraDirector;

        // 플레이어 초기 스탯 (won / item / m/s) — 코드 상수로 고정.
        private const int InitialMoneyCapacity = 100;
        private const int InitialHandcuffCapacity = 20;
        private const float InitialPlayerMoveSpeed = 5f;

        public PoolManager Pool => poolManager;
        public NavManager Nav => navManager;
        public SoundManager Sound => soundManager;
        public ItemFlowManager ItemFlow { get; private set; }
        public ResourceItemRegistry ResourceItems => resourceItemRegistry;
        public PlayerStatsConfigSO PlayerStats => playerStatsConfig;
        public Player PlayerEntity => playerEntity;
        public RockArea RockArea => rockArea;
        public CameraDirector CameraDirector => cameraDirector;

        public PrisonStateModel Prison { get; private set; }
        public GameStateModel GameState { get; private set; }
        public PlayerModel PlayerModel { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Prison = new PrisonStateModel();
            GameState = new GameStateModel();
            ItemFlow = new ItemFlowManager(poolManager);

            PlayerModel = CreatePlayerModel();
        }

        private void Start()
        {
            // 의존성 주입은 Start 에서. Awake 단계에서 호출하면
            // 대상 엔티티들의 Awake (예: Player 의 NavMeshAgent 캐싱) 가 아직 안 돈 상태여서
            // 내부 참조가 null 인 채로 서브시스템이 생성된다.
            InjectPlayer();

            GameState.CurrentPhase.Value = GamePhase.Playing;
        }

        private PlayerModel CreatePlayerModel()
        {
            // Ore 한도/스윙/타격/박스 등 무기 단계 의존 스탯은 Stage 0 데이터에서 가져온다.
            int initialOreCapacity = 10;
            float initialSwingDuration = 0.7f;
            int initialHitsPerSwing = 1;
            float initialRangeWidth = 1.0f;
            float initialRangeDepth = 1.0f;

            if (playerStatsConfig != null && playerStatsConfig.TryGetStage(0, out WeaponStageData stage0))
            {
                initialOreCapacity = stage0.oreCapacity;
                initialSwingDuration = stage0.swingDurationSeconds;
                initialHitsPerSwing = stage0.hitsPerSwing;
                initialRangeWidth = stage0.miningRangeWidth;
                initialRangeDepth = stage0.miningRangeDepth;
            }

            Dictionary<ResourceType, int> initialCapacities = new Dictionary<ResourceType, int>
            {
                { ResourceType.Ore, initialOreCapacity },
                { ResourceType.Money, InitialMoneyCapacity },
                { ResourceType.Handcuff, InitialHandcuffCapacity },
            };

            InventoryModel inventory = new InventoryModel(initialCapacities);
            PlayerModel model = new PlayerModel(inventory);
            model.MoveSpeed.Value = InitialPlayerMoveSpeed;
            model.MiningSwingDurationSeconds.Value = initialSwingDuration;
            model.MiningHitsPerSwing.Value = initialHitsPerSwing;
            model.MiningRangeWidth.Value = initialRangeWidth;
            model.MiningRangeDepth.Value = initialRangeDepth;
            return model;
        }

        private void InjectPlayer()
        {
            if (playerEntity == null)
            {
                Debug.LogWarning("[SystemManager] playerEntity 가 인스펙터에 연결되지 않았습니다.");
                return;
            }
            playerEntity.Init(PlayerModel);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
