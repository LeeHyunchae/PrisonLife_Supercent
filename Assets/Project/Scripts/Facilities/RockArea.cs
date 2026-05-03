using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Entities;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 광석 영역. cols × rows 그리드 위치에 MineableRock 을 instantiate 하고, 파괴되면 일정 시간 후 재활성.
    /// 자기 BoxCollider 가 trigger 로 영역 entry/exit 을 감지 — Player 진입 시 무기 ON, 이탈 시 OFF
    /// (스윙 중이면 PlayerMiningSystem 이 swing 끝까지 완주 후 OFF).
    /// 외부 (MinerHirePurchaseZone, AI 등) 는 SystemManager.Instance.RockArea.Rocks 로 조회.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RockArea : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private MineableRock rockPrefab;
        [SerializeField, Min(1)] private int gridCols = 4;
        [SerializeField, Min(1)] private int gridRows = 4;
        [SerializeField] private Vector2 cellSpacing = new Vector2(2f, 2f);

        [Header("Respawn")]
        private float respawnDelaySeconds = 5f;

        private readonly List<MineableRock> rocks = new();
        private readonly Dictionary<Collider, Player> colliderToPlayerCache = new();

        private BoxCollider areaTrigger;

        public IReadOnlyList<MineableRock> Rocks => rocks;

        private void Awake()
        {
            areaTrigger = GetComponent<BoxCollider>();
            areaTrigger.isTrigger = true;

            if (rockPrefab == null)
            {
                Debug.LogError("[RockArea] rockPrefab 미연결.");
                return;
            }
            SpawnGrid();
        }

        private void OnTriggerEnter(Collider _other)
        {
            Player player = ResolvePlayer(_other);
            if (player == null) return;
            player.SetInRockArea(true);
        }

        private void OnTriggerExit(Collider _other)
        {
            if (!colliderToPlayerCache.TryGetValue(_other, out Player player)) return;
            player.SetInRockArea(false);
        }

        private Player ResolvePlayer(Collider _collider)
        {
            if (_collider == null) return null;
            if (colliderToPlayerCache.TryGetValue(_collider, out Player cached)) return cached;

            Player direct = _collider.GetComponent<Player>();
            Player resolved = direct != null ? direct : _collider.GetComponentInParent<Player>();
            colliderToPlayerCache[_collider] = resolved;
            return resolved;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < rocks.Count; i++)
            {
                MineableRock rock = rocks[i];
                if (rock != null) rock.OnDepleted -= HandleRockDepleted;
            }
        }

        private void SpawnGrid()
        {
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    float localX = (col - (gridCols - 1) * 0.5f) * cellSpacing.x;
                    float localZ = (row - (gridRows - 1) * 0.5f) * cellSpacing.y;
                    Vector3 worldPosition = transform.TransformPoint(new Vector3(localX, 0f, localZ));

                    MineableRock rock = Instantiate(rockPrefab, worldPosition, transform.rotation, transform);
                    rock.OnDepleted += HandleRockDepleted;
                    rocks.Add(rock);
                }
            }
        }

        private void HandleRockDepleted(MineableRock _rock)
        {
            if (_rock == null) return;
            ScheduleRespawn(_rock, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid ScheduleRespawn(MineableRock _rock, CancellationToken _cancellationToken)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(respawnDelaySeconds),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken: _cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_rock != null) _rock.ResetToAvailable();
        }
    }
}
