using PrisonLife.Controllers.Player;
using PrisonLife.Core;
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
        [SerializeField] FloatingJoystick floatingJoystick;
        [SerializeField] Animator characterAnimator;

        [Header("Children")]
        [SerializeField] PlayerMiningRange miningRange;
        [SerializeField] Transform weaponAnchor;
        [SerializeField] Transform backStackAnchor;
        [SerializeField] Transform handStackAnchor;

        [Header("Stack Visuals")]
        [SerializeField] GameObject oreStackItemPrefab;
        [SerializeField] Vector3 oreStackOffsetStep = new Vector3(0f, 0.4f, 0f);
        [SerializeField] GameObject handcuffStackItemPrefab;
        [SerializeField] Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);

        [Header("Movement Tuning")]
        [SerializeField] float rotationLerpRate = 15f;

        NavMeshAgent navMeshAgent;
        PlayerModel playerModel;

        NavMeshMover navMeshMover;
        PlayerMovementSystem movementSystem;
        PlayerMiningSystem miningSystem;
        StackVisualizer oreStackVisualizer;
        StackVisualizer handcuffStackVisualizer;

        public InventoryModel Inventory => playerModel?.Inventory;

        void Awake()
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
            miningSystem = new PlayerMiningSystem(playerModel, miningRange, weaponAnchor, characterAnimator);

            oreStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Ore),
                backStackAnchor,
                oreStackItemPrefab,
                oreStackOffsetStep);

            handcuffStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Handcuff),
                handStackAnchor,
                handcuffStackItemPrefab,
                handcuffStackOffsetStep);
        }

        void Update()
        {
            if (playerModel == null) return;

            Vector2 joystickInput = floatingJoystick != null
                ? floatingJoystick.CurrentNormalizedDirection
                : Vector2.zero;

            movementSystem.Tick(joystickInput);
            miningSystem.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            DisposeSubsystems();
        }

        void DisposeSubsystems()
        {
            miningSystem?.Dispose();
            miningSystem = null;
            oreStackVisualizer?.Dispose();
            oreStackVisualizer = null;
            handcuffStackVisualizer?.Dispose();
            handcuffStackVisualizer = null;
            movementSystem = null;
            navMeshMover = null;
        }
    }
}
