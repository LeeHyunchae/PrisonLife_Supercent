using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Facilities;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.Player
{
    /// <summary>
    /// 채굴 상태 머신 + 스윙 임팩트.
    /// - Idle 상태: 후보 광석이 진입하고 인벤 여유가 있으면 StartMining (애니/무기 ON, 스윙 누적 시작)
    /// - Active 상태: 매 프레임 스윙 누적. 스윙 끝 시점에 후보+여유 재평가.
    ///     - 둘 다 만족 → ExecuteSwingImpact + 다음 스윙 계속
    ///     - 하나라도 false → StopMining (애니/무기 OFF). 즉, 스윙은 끝까지 완주됨.
    /// </summary>
    public class PlayerMiningSystem : IDisposable
    {
        private readonly PlayerModel playerModel;
        private readonly PlayerMiningRange miningRange;
        private readonly Entities.Player playerEntity;
        private readonly Animator characterAnimator;

        private readonly List<MineableRock> impactSortBuffer = new();

        private bool isMiningActive;
        private float miningSwingAccumulator;

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
            // 종료 시점에 무기/애니가 켜져있을 수 있으니 명시적으로 끔
            if (isMiningActive) StopMining();
        }

        public void Tick(float _deltaTime)
        {
            if (playerModel == null) return;

            bool hasCandidates = HasAnyAvailableCandidate();
            bool inventoryFull = playerModel.Inventory.IsAtCapacity(ResourceType.Ore);

            if (!isMiningActive)
            {
                // Idle — 광석 진입 + 인벤 여유 있을 때만 시작
                if (hasCandidates && !inventoryFull)
                {
                    StartMining();
                }
                return;
            }

            // Active — 스윙 누적, 끝까지 완주
            float swingDuration = playerModel.MiningSwingDurationSeconds.Value;
            miningSwingAccumulator += _deltaTime;
            if (miningSwingAccumulator < swingDuration) return;

            miningSwingAccumulator = 0f;

            // 스윙 종료 시점에 재평가
            if (!hasCandidates || inventoryFull)
            {
                StopMining();
                return;
            }

            ExecuteSwingImpact(playerModel.MiningHitsPerSwing.Value);
        }

        private void StartMining()
        {
            isMiningActive = true;
            miningSwingAccumulator = 0f;
            SetMiningAnimation(true);
            if (playerEntity != null) playerEntity.SetWeaponVisible(true);
        }

        private void StopMining()
        {
            isMiningActive = false;
            miningSwingAccumulator = 0f;
            SetMiningAnimation(false);
            if (playerEntity != null) playerEntity.SetWeaponVisible(false);
        }

        private void SetMiningAnimation(bool _isMining)
        {
            if (characterAnimator == null) return;
            characterAnimator.SetBool(MiningAnimatorBoolHash, _isMining);
        }

        private bool HasAnyAvailableCandidate()
        {
            if (miningRange == null) return false;

            foreach (var rock in miningRange.CurrentlyOverlappingRocks)
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
            foreach (var rock in miningRange.CurrentlyOverlappingRocks)
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
                }
            }
        }
    }
}
