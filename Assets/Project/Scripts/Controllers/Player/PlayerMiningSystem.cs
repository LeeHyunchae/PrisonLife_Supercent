using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Facilities;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.Player
{
    /// <summary>
    /// 채굴 상태 머신 + 스윙 임팩트.
    /// - 무기 가시성: isInRockArea OR isMiningActive (영역 안 또는 스윙 중에 ON).
    /// - Idle: Area + 후보 + 인벤 여유 만족 시 StartMining (애니/state ON).
    /// - 스윙 임팩트 / cycle 종료:
    ///     stage 0 (곡괭이, 애니 준비됨): Pickaxe 클립의 Animation Event 가 OnAnimationSwingImpact / OnAnimationSwingCycleEnd 호출.
    ///     stage 1+ (드릴/파쇄차, 애니 미준비): Tick 의 시간 누적기로 swing 종료 + impact 처리 (legacy fallback).
    /// - cycle 종료 재평가 (Area 이탈 / 후보 없음 / 인벤 가득) 시 StopMining → 무기 hide. 스윙 도중엔 무기 유지됨.
    /// </summary>
    public class PlayerMiningSystem : IDisposable
    {
        private readonly PlayerModel playerModel;
        private readonly PlayerMiningRange miningRange;
        private readonly Entities.Player playerEntity;
        private readonly Animator characterAnimator;

        private readonly List<MineableRock> impactSortBuffer = new();

        private bool isInRockArea;
        private bool isMiningActive;
        private float miningSwingAccumulator;
        private float blockedFullAccumulator;

        public event Action OnAttemptedMiningWhileFull;

        private static readonly int MiningAnimatorBoolHash = Animator.StringToHash("Mining");

        public PlayerMiningSystem(
            PlayerModel _playerModel,
            PlayerMiningRange _miningRange,
            Entities.Player _playerEntity,
            Animator _characterAnimator)
        {
            playerModel = _playerModel;
            miningRange = _miningRange;
            playerEntity = _playerEntity;
            characterAnimator = _characterAnimator;

            if (miningRange != null)
            {
                miningRange.Init(playerModel);
            }
        }

        public void Dispose()
        {
            impactSortBuffer.Clear();
            if (isMiningActive) StopMining();
            isInRockArea = false;
            UpdateWeaponVisibility();
        }

        public void SetInRockArea(bool _inArea)
        {
            if (isInRockArea == _inArea) return;
            isInRockArea = _inArea;
            UpdateWeaponVisibility();
        }

        public void Tick(float _deltaTime)
        {
            if (playerModel == null) return;

            if (!isMiningActive)
            {
                bool hasCandidates = HasAnyAvailableCandidate();
                bool inventoryFull = playerModel.Inventory.IsAtCapacity(ResourceType.Ore);

                if (isInRockArea && hasCandidates && !inventoryFull)
                {
                    StartMining();
                    blockedFullAccumulator = 0f;
                }
                else if (isInRockArea && hasCandidates && inventoryFull)
                {
                    // 인벤 가득 — swingDuration 마다 MAX 알림 트리거.
                    blockedFullAccumulator += _deltaTime;
                    float swingDuration = playerModel.MiningSwingDurationSeconds.Value;
                    if (blockedFullAccumulator >= swingDuration)
                    {
                        blockedFullAccumulator = 0f;
                        OnAttemptedMiningWhileFull?.Invoke();
                    }
                }
                else
                {
                    blockedFullAccumulator = 0f;
                }
                return;
            }

            // 애니메이션 클립이 준비된 단계는 Animation Event 가 cycle 종료를 처리.
            if (IsAnimatedSwingStage()) return;

            // 미준비 단계 — 시간 누적기로 cycle 시뮬레이션.
            miningSwingAccumulator += _deltaTime;
            if (miningSwingAccumulator < playerModel.MiningSwingDurationSeconds.Value) return;
            miningSwingAccumulator = 0f;

            ApplyImpactAndEvaluateCycleEnd();
        }

        public void OnAnimationSwingImpact()
        {
            if (!isMiningActive) return;
            if (!IsAnimatedSwingStage()) return;
            PlaySound(SoundType.PickaxeImpact);
            ExecuteSwingImpact(playerModel.MiningHitsPerSwing.Value);
        }

        public void OnAnimationSwingCycleEnd()
        {
            if (!isMiningActive) return;
            if (!IsAnimatedSwingStage()) return;

            if (!isInRockArea
                || !HasAnyAvailableCandidate()
                || playerModel.Inventory.IsAtCapacity(ResourceType.Ore))
            {
                StopMining();
            }
        }

        private void ApplyImpactAndEvaluateCycleEnd()
        {
            if (!isInRockArea
                || !HasAnyAvailableCandidate()
                || playerModel.Inventory.IsAtCapacity(ResourceType.Ore))
            {
                StopMining();
                return;
            }
            ExecuteSwingImpact(playerModel.MiningHitsPerSwing.Value);
        }

        private bool IsAnimatedSwingStage()
        {
            return playerModel != null && playerModel.WeaponUpgradeStage.Value == 0;
        }

        private void StartMining()
        {
            isMiningActive = true;
            miningSwingAccumulator = 0f;
            SetMiningAnimation(true);
            UpdateWeaponVisibility();
        }

        private void StopMining()
        {
            isMiningActive = false;
            miningSwingAccumulator = 0f;
            SetMiningAnimation(false);
            UpdateWeaponVisibility();
        }

        private void UpdateWeaponVisibility()
        {
            if (playerEntity == null) return;
            bool shouldShow = isInRockArea || isMiningActive;
            playerEntity.SetWeaponVisible(shouldShow);
        }

        private void SetMiningAnimation(bool _isMining)
        {
            if (characterAnimator == null) return;
            // 곡괭이(stage 0) 만 스윙 애니메이션 — 드릴/파쇄차는 클립 미준비라 Idle/Run 유지.
            bool shouldPlaySwingAnimation = _isMining && IsAnimatedSwingStage();
            characterAnimator.SetBool(MiningAnimatorBoolHash, shouldPlaySwingAnimation);
        }

        private bool HasAnyAvailableCandidate()
        {
            if (miningRange == null) return false;

            foreach (MineableRock rock in miningRange.CurrentlyOverlappingRocks)
            {
                if (rock != null && rock.IsAvailableForMining.Value) return true;
            }
            return false;
        }

        private void ExecuteSwingImpact(int _maxHitsForThisSwing)
        {
            if (_maxHitsForThisSwing <= 0) return;
            if (miningRange == null) return;

            Vector3 impactSourcePosition = playerEntity != null && playerEntity.WeaponAnchor != null
                ? playerEntity.WeaponAnchor.position
                : miningRange.transform.position;

            impactSortBuffer.Clear();
            foreach (MineableRock rock in miningRange.CurrentlyOverlappingRocks)
            {
                if (rock == null) continue;
                if (!rock.IsAvailableForMining.Value) continue;
                impactSortBuffer.Add(rock);
            }

            impactSortBuffer.Sort((_a, _b) =>
            {
                float squaredDistanceA = (_a.transform.position - impactSourcePosition).sqrMagnitude;
                float squaredDistanceB = (_b.transform.position - impactSourcePosition).sqrMagnitude;
                return squaredDistanceA.CompareTo(squaredDistanceB);
            });

            int hitsToApply = Mathf.Min(_maxHitsForThisSwing, impactSortBuffer.Count);

            for (int i = 0; i < hitsToApply; i++)
            {
                if (playerModel.Inventory.IsAtCapacity(ResourceType.Ore)) break;

                if (impactSortBuffer[i].TryDeplete())
                {
                    playerModel.Inventory.TryAdd(ResourceType.Ore, 1);
                    PlaySound(SoundType.OreCollect);
                }
            }
        }

        private void PlaySound(SoundType _type)
        {
            if (_type == SoundType.None) return;
            SoundManager sound = SystemManager.Instance != null ? SystemManager.Instance.Sound : null;
            sound?.PlayOneShot(_type);
        }
    }
}
