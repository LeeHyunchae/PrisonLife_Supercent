using PrisonLife.Controllers.Player;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Input;
using PrisonLife.Models;
using PrisonLife.Movement;
using PrisonLife.View.World;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Entities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Player : MonoBehaviour, IInventoryHolder
    {
        [Header("Input / Animation")]
        [SerializeField] private FloatingJoystick floatingJoystick;
        [SerializeField] private Animator characterAnimator;

        [Header("Children")]
        [SerializeField] private PlayerMiningRange miningRange;
        [SerializeField] private Transform weaponAnchor;
        [SerializeField] private Transform backStackAnchor;
        [SerializeField] private Transform handStackAnchor;
        [SerializeField] private Transform moneyStackAnchor;

        [Header("Stack Offset (per-context)")]
        [SerializeField] private Vector3 oreStackOffsetStep = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);
        [SerializeField] private Vector3 moneyStackOffsetStep = new Vector3(0f, 0.1f, 0f);

        [Header("Movement Tuning")]
        [SerializeField] private float rotationLerpRate = 15f;

        private NavMeshAgent navMeshAgent;
        private PlayerModel playerModel;

        private NavMeshMover navMeshMover;
        private PlayerMovementSystem movementSystem;
        private PlayerMiningSystem miningSystem;
        private PlayerWeaponSystem weaponSystem;
        private StackVisualizer oreStackVisualizer;
        private StackVisualizer handcuffStackVisualizer;
        private StackVisualizer moneyStackVisualizer;

        private GameObject currentWeaponVisualInstance;
        private bool isWeaponCurrentlyVisible;

        public InventoryModel Inventory => playerModel?.Inventory;
        public Transform WeaponAnchor => weaponAnchor;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;
        }

        public void Init(PlayerModel _playerModel)
        {
            playerModel = _playerModel;

            DisposeSubsystems();

            navMeshMover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);
            movementSystem = new PlayerMovementSystem(playerModel, navMeshMover);
            miningSystem = new PlayerMiningSystem(playerModel, miningRange, this, characterAnimator);

            var systemManager = SystemManager.Instance;
            var registry = systemManager != null ? systemManager.ResourceItems : null;
            var statsConfig = systemManager != null ? systemManager.PlayerStats : null;

            // PlayerWeaponSystem 은 Subscribe 즉시 fire 로 stage 0 데이터(곡괭이) 를 즉시 적용한다.
            weaponSystem = new PlayerWeaponSystem(playerModel, this, statsConfig);

            oreStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Ore),
                backStackAnchor,
                registry != null ? registry.GetPrefab(ResourceType.Ore) : null,
                oreStackOffsetStep);

            handcuffStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Handcuff),
                handStackAnchor,
                registry != null ? registry.GetPrefab(ResourceType.Handcuff) : null,
                handcuffStackOffsetStep);

            moneyStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Money),
                moneyStackAnchor,
                registry != null ? registry.GetPrefab(ResourceType.Money) : null,
                moneyStackOffsetStep);
        }

        private void Update()
        {
            if (playerModel == null) return;

            Vector2 joystickInput = floatingJoystick != null
                ? floatingJoystick.CurrentNormalizedDirection
                : Vector2.zero;

            movementSystem.Tick(joystickInput);
            miningSystem.Tick(Time.deltaTime);
        }

        public void SetWeaponVisual(GameObject _weaponPrefab)
        {
            if (currentWeaponVisualInstance != null)
            {
                Destroy(currentWeaponVisualInstance);
                currentWeaponVisualInstance = null;
            }

            if (_weaponPrefab == null || weaponAnchor == null) return;

            currentWeaponVisualInstance = Instantiate(_weaponPrefab, weaponAnchor);
            currentWeaponVisualInstance.transform.localPosition = Vector3.zero;
            currentWeaponVisualInstance.transform.localRotation = Quaternion.identity;
            currentWeaponVisualInstance.SetActive(isWeaponCurrentlyVisible);
        }

        public void SetWeaponVisible(bool _visible)
        {
            isWeaponCurrentlyVisible = _visible;
            if (currentWeaponVisualInstance != null)
            {
                currentWeaponVisualInstance.SetActive(_visible);
            }
        }

        private void OnDestroy()
        {
            DisposeSubsystems();
        }

        private void DisposeSubsystems()
        {
            miningSystem?.Dispose();
            miningSystem = null;
            weaponSystem?.Dispose();
            weaponSystem = null;
            oreStackVisualizer?.Dispose();
            oreStackVisualizer = null;
            handcuffStackVisualizer?.Dispose();
            handcuffStackVisualizer = null;
            moneyStackVisualizer?.Dispose();
            moneyStackVisualizer = null;
            movementSystem = null;
            navMeshMover = null;
        }
    }
}
