using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Configs;
using PrisonLife.Controllers.Player;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Input;
using PrisonLife.Managers;
using PrisonLife.Models;
using PrisonLife.Movement;
using PrisonLife.View.World;
using TMPro;
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
        [SerializeField] private Transform pickaxeWeaponAnchor;
        [SerializeField] private Transform centerWeaponAnchor;
        [SerializeField] private Transform backStackAnchor;
        [SerializeField] private Transform handStackAnchor;
        [SerializeField] private Transform moneyStackAnchor;

        [Header("Ore Max Popup")]
        [SerializeField] private TMP_Text oreFullLabelText;
        [SerializeField, Min(0.1f)] private float oreFullLabelDurationSeconds = 2f;
        [SerializeField] private float oreFullLabelRiseAmount = 2f;

        [Header("Stack Offset (per-context)")]
        [SerializeField] private Vector3 oreStackOffsetStep = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);
        [SerializeField] private Vector3 moneyStackOffsetStep = new Vector3(0f, 0.1f, 0f);

        [Header("Movement Tuning")]
        [SerializeField] private float rotationLerpRate = 15f;

        [Header("Animation Tuning")]
        [SerializeField, Min(0f)] private float animatorSpeedDampSeconds = 0.1f;

        private static readonly int SpeedAnimatorFloatHash = Animator.StringToHash("Speed");

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
        private Transform currentWeaponAnchor;
        private bool isWeaponCurrentlyVisible;

        private Vector3 oreFullLabelOriginLocalPosition;
        private bool isOreFullLabelAnimating;

        public InventoryModel Inventory => playerModel?.Inventory;
        public bool IsPlayerControlled => true;
        public Transform Transform => transform;
        public Transform WeaponAnchor => currentWeaponAnchor != null ? currentWeaponAnchor : pickaxeWeaponAnchor;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;

            if (oreFullLabelText != null)
            {
                oreFullLabelOriginLocalPosition = oreFullLabelText.transform.localPosition;
                oreFullLabelText.gameObject.SetActive(false);
            }
        }

        public void Init(PlayerModel _playerModel)
        {
            playerModel = _playerModel;

            DisposeSubsystems();

            navMeshMover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);
            movementSystem = new PlayerMovementSystem(playerModel, navMeshMover);
            miningSystem = new PlayerMiningSystem(playerModel, miningRange, this, characterAnimator);
            miningSystem.OnAttemptedMiningWhileFull += TriggerOreFullLabelPopup;

            SystemManager systemManager = SystemManager.Instance;
            PlayerStatsConfigSO statsConfig = systemManager != null ? systemManager.PlayerStats : null;
            PoolManager pool = systemManager != null ? systemManager.Pool : null;

            // PlayerWeaponSystem 은 Subscribe 즉시 fire 로 stage 0 데이터(곡괭이) 를 즉시 적용한다.
            weaponSystem = new PlayerWeaponSystem(playerModel, this, statsConfig);

            oreStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Ore),
                backStackAnchor,
                ResourceType.Ore,
                oreStackOffsetStep,
                pool);

            handcuffStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Handcuff),
                handStackAnchor,
                ResourceType.Handcuff,
                handcuffStackOffsetStep,
                pool);

            moneyStackVisualizer = new StackVisualizer(
                playerModel.Inventory.ObserveCount(ResourceType.Money),
                moneyStackAnchor,
                ResourceType.Money,
                moneyStackOffsetStep,
                pool,
                GameValueConstants.MoneyValuePerItem);
        }

        private void TriggerOreFullLabelPopup()
        {
            if (isOreFullLabelAnimating) return;
            if (oreFullLabelText == null) return;
            AnimateOreFullLabelAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid AnimateOreFullLabelAsync(CancellationToken _cancellationToken)
        {
            isOreFullLabelAnimating = true;
            try
            {
                Transform labelTransform = oreFullLabelText.transform;
                labelTransform.localPosition = oreFullLabelOriginLocalPosition;
                oreFullLabelText.alpha = 1f;
                oreFullLabelText.gameObject.SetActive(true);

                float elapsedSeconds = 0f;
                while (elapsedSeconds < oreFullLabelDurationSeconds)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    elapsedSeconds += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedSeconds / oreFullLabelDurationSeconds);

                    Vector3 nextPosition = oreFullLabelOriginLocalPosition;
                    nextPosition.y += oreFullLabelRiseAmount * t;
                    labelTransform.localPosition = nextPosition;

                    oreFullLabelText.alpha = 1f - t;

                    await UniTask.Yield();
                }

                oreFullLabelText.gameObject.SetActive(false);
                oreFullLabelText.alpha = 1f;
                labelTransform.localPosition = oreFullLabelOriginLocalPosition;
            }
            catch (OperationCanceledException)
            {
                // 정상 취소
            }
            finally
            {
                isOreFullLabelAnimating = false;
            }
        }

        private void Update()
        {
            if (playerModel == null) return;

            Vector2 joystickInput = floatingJoystick != null
                ? floatingJoystick.CurrentNormalizedDirection
                : Vector2.zero;

            movementSystem.Tick(joystickInput);
            miningSystem.Tick(Time.deltaTime);
            UpdateLocomotionAnimation(joystickInput.magnitude);
        }

        private void UpdateLocomotionAnimation(float _normalizedSpeed)
        {
            if (characterAnimator == null) return;
            characterAnimator.SetFloat(SpeedAnimatorFloatHash, _normalizedSpeed, animatorSpeedDampSeconds, Time.deltaTime);
        }

        public void SetWeaponVisual(GameObject _weaponPrefab)
        {
            if (currentWeaponVisualInstance != null)
            {
                Destroy(currentWeaponVisualInstance);
                currentWeaponVisualInstance = null;
            }

            currentWeaponAnchor = ResolveWeaponAnchor();
            if (_weaponPrefab == null || currentWeaponAnchor == null) return;

            currentWeaponVisualInstance = Instantiate(_weaponPrefab, currentWeaponAnchor);
            currentWeaponVisualInstance.transform.localPosition = Vector3.zero;
            currentWeaponVisualInstance.transform.localRotation = Quaternion.identity;
            currentWeaponVisualInstance.SetActive(isWeaponCurrentlyVisible);
        }

        private Transform ResolveWeaponAnchor()
        {
            // 곡괭이 (stage 0) 만 손 위치, 그 외 (드릴/파쇄차) 는 캐릭터 중앙.
            bool isPickaxe = playerModel != null && playerModel.WeaponUpgradeStage.Value == 0;
            return isPickaxe ? pickaxeWeaponAnchor : centerWeaponAnchor;
        }

        public void SetWeaponVisible(bool _visible)
        {
            isWeaponCurrentlyVisible = _visible;
            if (currentWeaponVisualInstance != null)
            {
                currentWeaponVisualInstance.SetActive(_visible);
            }
        }

        public void SetInRockArea(bool _inArea)
        {
            miningSystem?.SetInRockArea(_inArea);
        }

        // ----- Animation Event entry points -----
        // Pickaxe AnimationClip 의 Animation Event 가 호출 — Player 가 Animator 와 같은 GameObject 일 때.
        // Animator 가 자식 rig 에 있으면 PlayerAnimationEventForwarder 를 통해 호출.
        public void OnMiningSwingImpact()
        {
            miningSystem?.OnAnimationSwingImpact();
        }

        public void OnMiningSwingCycleEnd()
        {
            miningSystem?.OnAnimationSwingCycleEnd();
        }

        private void OnDestroy()
        {
            DisposeSubsystems();
        }

        private void DisposeSubsystems()
        {
            if (miningSystem != null)
            {
                miningSystem.OnAttemptedMiningWhileFull -= TriggerOreFullLabelPopup;
                miningSystem.Dispose();
                miningSystem = null;
            }
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
