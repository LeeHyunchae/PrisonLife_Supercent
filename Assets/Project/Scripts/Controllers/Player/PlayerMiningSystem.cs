using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Facilities;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.Player
{
    public class PlayerMiningSystem : IDisposable
    {
        readonly PlayerModel playerModel;
        readonly PlayerMiningRange miningRange;
        readonly Transform weaponAnchor;
        readonly Animator characterAnimator;

        readonly List<MineableRock> impactSortBuffer = new();
        float miningSwingAccumulator;

        static readonly int MiningAnimatorBoolHash = Animator.StringToHash("Mining");

        public PlayerMiningSystem(
            PlayerModel _playerModel,
            PlayerMiningRange _miningRange,
            Transform _weaponAnchor,
            Animator _characterAnimator)
        {
            playerModel = _playerModel;
            miningRange = _miningRange;
            weaponAnchor = _weaponAnchor;
            characterAnimator = _characterAnimator;

            if (miningRange != null)
            {
                miningRange.Init(playerModel);
            }
        }

        public void Dispose()
        {
            impactSortBuffer.Clear();
        }

        public void Tick(float _deltaTime)
        {
            if (playerModel == null) return;

            bool hasCandidates = HasAnyAvailableCandidate();
            bool inventoryFull = playerModel.Inventory.IsAtCapacity(ResourceType.Ore);

            if (!hasCandidates || inventoryFull)
            {
                SetMiningAnimation(false);
                miningSwingAccumulator = 0f;
                return;
            }

            SetMiningAnimation(true);

            float swingDuration = playerModel.MiningSwingDurationSeconds.Value;
            miningSwingAccumulator += _deltaTime;
            if (miningSwingAccumulator < swingDuration) return;
            miningSwingAccumulator = 0f;

            ExecuteSwingImpact(playerModel.MiningHitsPerSwing.Value);
        }

        void SetMiningAnimation(bool _isMining)
        {
            if (characterAnimator == null) return;
            characterAnimator.SetBool(MiningAnimatorBoolHash, _isMining);
        }

        bool HasAnyAvailableCandidate()
        {
            if (miningRange == null) return false;

            foreach (var rock in miningRange.CurrentlyOverlappingRocks)
            {
                if (rock != null && rock.IsAvailableForMining.Value) return true;
            }
            return false;
        }

        void ExecuteSwingImpact(int _maxHitsForThisSwing)
        {
            if (_maxHitsForThisSwing <= 0) return;
            if (miningRange == null) return;

            Vector3 impactSourcePosition = weaponAnchor != null
                ? weaponAnchor.position
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
