using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Facilities;
using PrisonLife.Game;
using PrisonLife.Input;
using PrisonLife.Models;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.View.UI
{
    /// <summary>
    /// 게임 진행 단계별 화살표 힌트 + idle 오버레이.
    /// 시퀀셜 6단계 (StartedGame → OreStocked → OreDeposited → HandcuffReceived → PrisonerProcessed → Done) —
    /// 모델 변화 (player.Ore>0, container.Ore>0, player.Handcuff>0, moneyOutput.Money>0, player.Money>0) 로 자동 advance.
    /// idle 오버레이는 항상 동작 — 조이스틱 입력 없이 N초 경과 시 표시.
    /// 첫 머니 획득 이후엔 시네마가 등장 가이드를 대체하므로 힌트는 모두 hide.
    /// </summary>
    public class HintManager : MonoBehaviour
    {
        private enum HintStep
        {
            StartedGame = 0,
            OreStocked = 1,
            OreDeposited = 2,
            HandcuffReceived = 3,
            PrisonerProcessed = 4,
            Done = 5,
        }

        [Header("Idle Hint")]
        [SerializeField] private GameObject idleOverlay;
        [SerializeField, Min(0.5f)] private float idleThresholdSeconds = 3f;
        [SerializeField] private FloatingJoystick joystick;

        [Header("Game Start — Rock Area Arrow")]
        [SerializeField] private GameObject rockAreaArrow;

        [Header("Ore Stocked — Player → Container Arrow")]
        [SerializeField] private GameObject playerToContainerArrow;
        [SerializeField] private Transform containerArrowTarget;
        // 캐릭터 기준의 수직 offset (Y) + 그 외 미세 보정.
        [SerializeField] private Vector3 playerArrowOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private Vector3 playerArrowRotationEulerOffset = new Vector3(0f, -90f, 0f);
        // 캐릭터에서 컨테이너 방향으로 떨어뜨릴 거리 — 화살표가 캐릭터 주변을 궤도 도는 효과.
        [SerializeField, Min(0f)] private float playerArrowOrbitRadius = 1.5f;

        [Header("Ore Deposited — Handcuff Output Arrow")]
        [SerializeField] private GameObject handcuffOutputArrow;

        [Header("Handcuff Received — Process Zone Arrow")]
        [SerializeField] private GameObject processZoneArrow;

        [Header("Prisoner Processed — Money Output Arrow")]
        [SerializeField] private GameObject moneyOutputArrow;

        [Header("Trigger Sources")]
        [SerializeField] private HandcuffContainer handcuffContainer;
        [SerializeField] private MoneyOutput moneyOutput;

        private HintStep currentStep = HintStep.StartedGame;
        private float idleTimeSeconds;
        private readonly List<IDisposable> subscriptions = new();

        private void Start()
        {
            SubscribeStepTriggers();
            UpdateHintVisuals();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < subscriptions.Count; i++) subscriptions[i]?.Dispose();
            subscriptions.Clear();
        }

        private void Update()
        {
            UpdateIdleHint();
            UpdatePlayerToContainerArrow();
        }

        private void SubscribeStepTriggers()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.PlayerModel == null) return;

            InventoryModel inventory = systemManager.PlayerModel.Inventory;
            if (inventory != null)
            {
                subscriptions.Add(inventory.ObserveCount(ResourceType.Ore).SubscribeOnChange(_currentOre =>
                {
                    if (_currentOre > 0) AdvanceToStep(HintStep.OreStocked);
                }));
                subscriptions.Add(inventory.ObserveCount(ResourceType.Handcuff).SubscribeOnChange(_currentHandcuff =>
                {
                    if (_currentHandcuff > 0) AdvanceToStep(HintStep.HandcuffReceived);
                }));
                subscriptions.Add(inventory.ObserveCount(ResourceType.Money).SubscribeOnChange(_currentMoney =>
                {
                    if (_currentMoney > 0) AdvanceToStep(HintStep.Done);
                }));
            }

            if (handcuffContainer != null && handcuffContainer.OreStockpile != null)
            {
                subscriptions.Add(handcuffContainer.OreStockpile.Count.SubscribeOnChange(_containerOre =>
                {
                    if (_containerOre > 0) AdvanceToStep(HintStep.OreDeposited);
                }));
            }

            if (moneyOutput != null && moneyOutput.MoneyStockpile != null)
            {
                subscriptions.Add(moneyOutput.MoneyStockpile.Count.SubscribeOnChange(_outputMoney =>
                {
                    if (_outputMoney > 0) AdvanceToStep(HintStep.PrisonerProcessed);
                }));
            }
        }

        private void AdvanceToStep(HintStep _newStep)
        {
            // 단계는 단방향 진행 — 더 앞 단계로는 advance 안 함.
            if (_newStep <= currentStep) return;
            currentStep = _newStep;
            UpdateHintVisuals();
        }

        private void UpdateHintVisuals()
        {
            SetActiveSafe(rockAreaArrow, currentStep == HintStep.StartedGame);
            SetActiveSafe(playerToContainerArrow, currentStep == HintStep.OreStocked);
            SetActiveSafe(handcuffOutputArrow, currentStep == HintStep.OreDeposited);
            SetActiveSafe(processZoneArrow, currentStep == HintStep.HandcuffReceived);
            SetActiveSafe(moneyOutputArrow, currentStep == HintStep.PrisonerProcessed);
        }

        private static void SetActiveSafe(GameObject _go, bool _active)
        {
            if (_go == null) return;
            if (_go.activeSelf != _active) _go.SetActive(_active);
        }

        private void UpdateIdleHint()
        {
            if (idleOverlay == null) return;

            Vector2 currentJoystickInput = joystick != null ? joystick.CurrentNormalizedDirection : Vector2.zero;
            if (currentJoystickInput.sqrMagnitude > 0.01f)
            {
                idleTimeSeconds = 0f;
                SetActiveSafe(idleOverlay, false);
                return;
            }

            idleTimeSeconds += Time.deltaTime;
            SetActiveSafe(idleOverlay, idleTimeSeconds >= idleThresholdSeconds);
        }

        private void UpdatePlayerToContainerArrow()
        {
            if (currentStep != HintStep.OreStocked) return;
            if (playerToContainerArrow == null || containerArrowTarget == null) return;

            SystemManager systemManager = SystemManager.Instance;
            Player player = systemManager != null ? systemManager.PlayerEntity : null;
            if (player == null) return;

            Vector3 playerPosition = player.transform.position;

            // 캐릭터→컨테이너 수평 방향 계산.
            Vector3 toTarget = containerArrowTarget.position - playerPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return;

            Vector3 directionToTarget = toTarget.normalized;

            // 위치: 캐릭터 + 방향 * 궤도 반경 + 수직 offset → 캐릭터 주변을 컨테이너 방향에 따라 궤도 운동.
            playerToContainerArrow.transform.position =
                playerPosition + directionToTarget * playerArrowOrbitRadius + playerArrowOffset;

            // 회전: LookRotation 은 +Z 가 forward 가 되도록 정렬 — 화살표 prefab 의 default forward 가 다르면
            // playerArrowRotationEulerOffset 으로 보정 (예: 오른쪽(+X) 향한 화살표 → -90Y 오프셋).
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            playerToContainerArrow.transform.rotation = lookRotation * Quaternion.Euler(playerArrowRotationEulerOffset);
        }
    }
}
