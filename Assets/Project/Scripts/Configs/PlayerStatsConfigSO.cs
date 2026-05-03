using System.Collections.Generic;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Configs
{
    /// <summary>
    /// 무기 단계별 플레이어 스탯 + 무기 prefab 매핑. ScriptableObject 에셋 1개로 관리.
    /// SystemManager 인스펙터에 1개 연결되고, WeaponUpgradePurchaseZone 이 SystemManager.PlayerStats 로 조회.
    /// 0번 인덱스가 시작 단계 (곡괭이) — 게임 시작 시 즉시 적용된다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "PrisonLife/PlayerStatsConfig")]
    public class PlayerStatsConfigSO : ScriptableObject
    {
        [SerializeField] private List<WeaponStageData> stages = new();

        public IReadOnlyList<WeaponStageData> Stages => stages;

        public bool TryGetStage(int _index, out WeaponStageData _stageData)
        {
            if (_index < 0 || _index >= stages.Count)
            {
                _stageData = default;
                return false;
            }
            _stageData = stages[_index];
            return true;
        }
    }
}
