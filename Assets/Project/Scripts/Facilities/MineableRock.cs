using System;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 단일 광석 노드. 채굴 가능 상태 + 채굴 1회 처리만 담당.
    /// respawn 타이밍과 그리드 배치는 RockArea 가 관리 (이 클래스는 자기 상태만 관여).
    /// 파괴 시 GameObject 자체를 SetActive(false) — collider 도 함께 비활성되어 PlayerMiningRange 의 OnTriggerStay 갱신에서 자동 제외.
    /// </summary>
    public class MineableRock : MonoBehaviour
    {
        public ReactiveProperty<bool> IsAvailableForMining { get; } = new(true);
        public Vector3 OreSpawnPosition => transform.position;

        public event Action<MineableRock> OnDepleted;

        public bool TryDeplete()
        {
            if (!IsAvailableForMining.Value) return false;

            IsAvailableForMining.Value = false;
            OnDepleted?.Invoke(this);
            gameObject.SetActive(false);
            return true;
        }

        public void ResetToAvailable()
        {
            gameObject.SetActive(true);
            IsAvailableForMining.Value = true;
        }
    }
}
