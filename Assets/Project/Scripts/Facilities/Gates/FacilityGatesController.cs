using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrisonLife.Game;
using PrisonLife.View;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 씬 단위 조건부 시설 게이트 컨테이너. 인스펙터 config 로 4종 gate 생성.
    /// Awake 에 시설들을 즉시 비활성 → Start 에 POCO gate 생성하고 Initialize.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class FacilityGatesController : MonoBehaviour
    {
        [Header("무기 강화 — 첫 돈 획득")]
        [SerializeField] private GameObject weaponUpgradeFacility;
        [SerializeField] private bool weaponUpgradePlayCinematic = true;

        [Header("광부 일꾼 — 무기 강화 N단계 도달")]
        [SerializeField] private GameObject minerHireFacility;
        [SerializeField, Min(1)] private int minerHireRequiredStage = 1;

        [Header("죄수 처리 일꾼 — 광부 구매 완료")]
        [SerializeField] private GameObject prisonerWorkerHireFacility;
        [SerializeField] private PurchaseZone minerHirePurchaseZone;

        [Header("감옥 확장 — 감옥 가득")]
        [SerializeField] private GameObject prisonExpandFacility;
        [SerializeField] private bool prisonExpandPlayCinematic = true;
        [SerializeField] private Transform prisonFullCinematicFocus;

        private readonly List<FacilityGate> gates = new();

        private void Awake()
        {
            // 등록된 모든 시설을 시작 시 비활성화 (gate 생성 전이지만 Awake 에서 강제로 끔)
            DisableIfPresent(weaponUpgradeFacility);
            DisableIfPresent(minerHireFacility);
            DisableIfPresent(prisonerWorkerHireFacility);
            DisableIfPresent(prisonExpandFacility);
        }

        private void Start()
        {
            if (weaponUpgradeFacility != null)
            {
                gates.Add(new FirstMoneyEarnedGate(weaponUpgradeFacility, weaponUpgradePlayCinematic));
            }

            if (minerHireFacility != null)
            {
                gates.Add(new WeaponStageReachedGate(minerHireFacility, minerHireRequiredStage));
            }

            if (prisonerWorkerHireFacility != null && minerHirePurchaseZone != null)
            {
                gates.Add(new PurchaseCompletedGate(prisonerWorkerHireFacility, minerHirePurchaseZone));
            }

            if (prisonExpandFacility != null)
            {
                gates.Add(new PrisonFullGate(prisonExpandFacility, prisonExpandPlayCinematic, prisonFullCinematicFocus));
            }

            for (int i = 0; i < gates.Count; i++)
            {
                gates[i].Revealed += OnGateRevealed;
                gates[i].Initialize();
            }
        }

        private void OnGateRevealed(FacilityGate _gate)
        {
            if (_gate == null || !_gate.PlayRevealCinematic) return;
            if (_gate.CinematicFocusTarget == null) return;

            CameraDirector director = SystemManager.Instance != null ? SystemManager.Instance.CameraDirector : null;
            if (director == null) return;

            director.PlayFocusOnAsync(_gate.CinematicFocusTarget).Forget();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null) gates[i].Revealed -= OnGateRevealed;
                gates[i]?.Dispose();
            }
            gates.Clear();
        }

        private static void DisableIfPresent(GameObject _go)
        {
            if (_go != null) _go.SetActive(false);
        }
    }
}
