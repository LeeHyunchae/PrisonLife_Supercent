using System;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 조건부로 등장하는 시설을 게이팅하는 추상 컴포넌트.
    /// Awake 에 gatedFacility 비활성화 → 서브클래스가 자기 트리거 만족 시 TryUnlock 호출 → 활성화 + OnRevealed 발사.
    /// SystemManager 보다 늦게, 일반 Mono 보다 빠르게 실행되도록 -500 으로 ordering.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public abstract class FacilityGateBase : MonoBehaviour
    {
        [SerializeField] GameObject gatedFacility;
        [SerializeField] bool playRevealCinematic = false;

        public event Action<FacilityGateBase> Revealed;

        public GameObject GatedFacility => gatedFacility;
        public bool PlayRevealCinematic => playRevealCinematic;
        public bool HasUnlocked { get; private set; }

        void Awake()
        {
            if (gatedFacility != null) gatedFacility.SetActive(false);
        }

        void Start()
        {
            SubscribeToTrigger();
        }

        /// <summary>
        /// 서브클래스에서 자기 트리거(ReactiveProperty / event 등) 를 구독하고,
        /// 조건 만족 시 TryUnlock 을 호출한다.
        /// </summary>
        protected abstract void SubscribeToTrigger();

        protected void TryUnlock()
        {
            if (HasUnlocked) return;
            HasUnlocked = true;

            if (gatedFacility != null) gatedFacility.SetActive(true);

            Revealed?.Invoke(this);
        }
    }
}
