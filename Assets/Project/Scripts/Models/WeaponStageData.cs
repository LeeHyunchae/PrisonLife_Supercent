using UnityEngine;

namespace PrisonLife.Models
{
    /// <summary>
    /// 무기 단계별 플레이어 스탯 + 무기 visual prefab. SystemManager 인스펙터에 List 로 직렬화.
    /// 0번 인덱스가 시작 단계 (예: 곡괭이) — 게임 시작 시 즉시 적용된다.
    /// </summary>
    [System.Serializable]
    public struct WeaponStageData
    {
        public string stageName;
        public GameObject weaponVisualPrefab;
        public int upgradeCost;
        public float swingDurationSeconds;
        public int hitsPerSwing;
        public float miningRangeWidth;
        public float miningRangeDepth;
        public int oreCapacity;
    }
}
