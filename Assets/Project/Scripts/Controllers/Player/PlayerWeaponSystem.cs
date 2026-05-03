using System;
using PrisonLife.Configs;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Models;

namespace PrisonLife.Controllers.Player
{
    /// <summary>
    /// PlayerModel.WeaponUpgradeStage 변화를 구독하고 PlayerStatsConfig 의 단계 데이터를
    /// PlayerModel/Inventory/Player visual 에 적용한다. Player 가 owning, Init 시 생성.
    /// 즉시 fire 로 stage 0 (곡괭이) 가 게임 시작 시점부터 적용됨.
    /// </summary>
    public class PlayerWeaponSystem : IDisposable
    {
        private readonly PlayerModel playerModel;
        private readonly Entities.Player playerEntity;
        private readonly PlayerStatsConfigSO statsConfig;

        private IDisposable stageSubscription;

        public PlayerWeaponSystem(
            PlayerModel _playerModel,
            Entities.Player _playerEntity,
            PlayerStatsConfigSO _statsConfig)
        {
            playerModel = _playerModel;
            playerEntity = _playerEntity;
            statsConfig = _statsConfig;

            if (playerModel != null)
            {
                stageSubscription = playerModel.WeaponUpgradeStage.Subscribe(ApplyStageData);
            }
        }

        public void Dispose()
        {
            stageSubscription?.Dispose();
            stageSubscription = null;
        }

        private void ApplyStageData(int _stageIndex)
        {
            if (statsConfig == null) return;
            if (!statsConfig.TryGetStage(_stageIndex, out WeaponStageData stageData)) return;

            playerModel.MiningSwingDurationSeconds.Value = stageData.swingDurationSeconds;
            playerModel.MiningHitsPerSwing.Value = stageData.hitsPerSwing;
            playerModel.MiningRangeWidth.Value = stageData.miningRangeWidth;
            playerModel.MiningRangeDepth.Value = stageData.miningRangeDepth;

            if (playerModel.Inventory != null)
            {
                playerModel.Inventory.SetCapacity(ResourceType.Ore, stageData.oreCapacity);
            }

            if (playerEntity != null)
            {
                playerEntity.SetWeaponVisual(stageData.weaponVisualPrefab);
            }
        }
    }
}
