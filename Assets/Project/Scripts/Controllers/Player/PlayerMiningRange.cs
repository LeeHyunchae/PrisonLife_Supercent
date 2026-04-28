using System.Collections.Generic;
using PrisonLife.Facilities;
using PrisonLife.Models;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.Controllers.Player
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMiningRange : MonoBehaviour
    {
        [SerializeField] float boxHeight = 1.0f;

        BoxCollider triggerBoxCollider;
        Rigidbody kinematicRigidbody;
        PlayerModel playerModel;

        readonly HashSet<MineableRock> currentlyOverlappingRocks = new();
        readonly Dictionary<Collider, MineableRock> colliderToRockCache = new();

        public IReadOnlyCollection<MineableRock> CurrentlyOverlappingRocks => currentlyOverlappingRocks;

        void Awake()
        {
            triggerBoxCollider = GetComponent<BoxCollider>();
            triggerBoxCollider.isTrigger = true;

            kinematicRigidbody = GetComponent<Rigidbody>();
            kinematicRigidbody.isKinematic = true;
            kinematicRigidbody.useGravity = false;
        }

        public void Init(PlayerModel _playerModel)
        {
            playerModel = _playerModel;

            playerModel.MiningRangeWidth
                .Subscribe(_ => RecalculateTriggerBox())
                .AddTo(this);

            playerModel.MiningRangeDepth
                .Subscribe(_ => RecalculateTriggerBox())
                .AddTo(this);
        }

        void RecalculateTriggerBox()
        {
            if (playerModel == null || triggerBoxCollider == null) return;

            float currentWidth = playerModel.MiningRangeWidth.Value;
            float currentDepth = playerModel.MiningRangeDepth.Value;

            triggerBoxCollider.size = new Vector3(currentWidth, boxHeight, currentDepth);
            triggerBoxCollider.center = new Vector3(0f, boxHeight * 0.5f, currentDepth * 0.5f);
        }

        void FixedUpdate()
        {
            // 매 물리 스텝 시작에 비운다. 같은 스텝 안에서 OnTriggerStay 가
            // 실제 overlap 중인 collider 마다 호출되며 다시 채운다.
            currentlyOverlappingRocks.Clear();
        }

        void OnTriggerStay(Collider _other)
        {
            // collider -> MineableRock 매핑은 처음 만났을 때 한 번만 풀고 캐싱.
            // 같은 collider 의 후속 Stay 호출은 Dictionary 룩업 (O(1)) 만으로 끝.
            if (!colliderToRockCache.TryGetValue(_other, out var rock))
            {
                rock = FindMineableRockOnOrAbove(_other);
                colliderToRockCache[_other] = rock;
            }
            if (rock == null) return;
            currentlyOverlappingRocks.Add(rock);
        }

        void OnTriggerExit(Collider _other)
        {
            // 물리적으로 영역을 벗어난 collider 의 캐시는 제거해 누적을 방지.
            // (Exit 가 안 불리는 경우엔 캐시가 남지만 메모리적으로 무해 — 다음 만남에 그대로 재사용.)
            colliderToRockCache.Remove(_other);
        }

        static MineableRock FindMineableRockOnOrAbove(Collider _collider)
        {
            if (_collider == null) return null;
            var direct = _collider.GetComponent<MineableRock>();
            if (direct != null) return direct;
            return _collider.GetComponentInParent<MineableRock>();
        }
    }
}
