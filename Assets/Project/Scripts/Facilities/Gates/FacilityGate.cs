using System;
using UnityEngine;

namespace PrisonLife.Facilities.Gates
{
    /// <summary>
    /// 조건부 시설 등장 추상 — POCO. FacilityGatesController 가 인스턴스화하고 Initialize/Dispose 한다.
    /// Initialize 즉시 gatedFacility 비활성. 서브클래스가 자기 트리거 만족 시 TryUnlock 호출.
    /// </summary>
    public abstract class FacilityGate : IDisposable
    {
        private readonly GameObject gatedFacility;
        private readonly bool playRevealCinematic;
        private readonly Transform cinematicFocusOverride;
        private bool hasUnlocked;

        public event Action<FacilityGate> Revealed;

        public GameObject GatedFacility => gatedFacility;
        public bool PlayRevealCinematic => playRevealCinematic;
        public bool HasUnlocked => hasUnlocked;

        /// <summary>
        /// 시네마 카메라가 focus 할 대상. override 가 지정되면 그것을, 없으면 gatedFacility 자체를 사용.
        /// 예: PrisonFullGate 는 gated facility (확장 구매칸) 가 아닌 감옥 본체를 보여줘야 함.
        /// </summary>
        public Transform CinematicFocusTarget => cinematicFocusOverride != null
            ? cinematicFocusOverride
            : (gatedFacility != null ? gatedFacility.transform : null);

        protected FacilityGate(GameObject _gatedFacility, bool _playRevealCinematic, Transform _cinematicFocusOverride = null)
        {
            gatedFacility = _gatedFacility;
            playRevealCinematic = _playRevealCinematic;
            cinematicFocusOverride = _cinematicFocusOverride;
        }

        public void Initialize()
        {
            if (gatedFacility != null) gatedFacility.SetActive(false);
            SubscribeToTrigger();
        }

        protected abstract void SubscribeToTrigger();

        protected void TryUnlock()
        {
            if (hasUnlocked) return;
            hasUnlocked = true;

            if (gatedFacility != null) gatedFacility.SetActive(true);

            Revealed?.Invoke(this);
        }

        public virtual void Dispose() { }
    }
}
