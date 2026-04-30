using System.Collections.Generic;
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
        [SerializeField] GameObject weaponUpgradeFacility;
        [SerializeField] bool weaponUpgradePlayCinematic = true;

        [Header("광부 일꾼 — 무기 강화 N단계 도달")]
        [SerializeField] GameObject minerHireFacility;
        [SerializeField, Min(1)] int minerHireRequiredStage = 1;

        [Header("죄수 처리 일꾼 — 광부 구매 완료")]
        [SerializeField] GameObject prisonerWorkerHireFacility;
        [SerializeField] PurchaseZone minerHirePurchaseZone;

        [Header("감옥 확장 — 감옥 가득")]
        [SerializeField] GameObject prisonExpandFacility;
        [SerializeField] bool prisonExpandPlayCinematic = false;

        readonly List<FacilityGate> gates = new();

        void Awake()
        {
            // 등록된 모든 시설을 시작 시 비활성화 (gate 생성 전이지만 Awake 에서 강제로 끔)
            DisableIfPresent(weaponUpgradeFacility);
            DisableIfPresent(minerHireFacility);
            DisableIfPresent(prisonerWorkerHireFacility);
            DisableIfPresent(prisonExpandFacility);
        }

        void Start()
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
                gates.Add(new PrisonFullGate(prisonExpandFacility, prisonExpandPlayCinematic));
            }

            for (int i = 0; i < gates.Count; i++)
            {
                gates[i].Initialize();
                // 추후 CameraDirector 가 이 이벤트를 구독해 PlayRevealCinematic == true 인 게이트에 시네마 적용 예정
                // gates[i].Revealed += OnGateRevealed;
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < gates.Count; i++)
            {
                gates[i]?.Dispose();
            }
            gates.Clear();
        }

        static void DisableIfPresent(GameObject _go)
        {
            if (_go != null) _go.SetActive(false);
        }
    }
}
