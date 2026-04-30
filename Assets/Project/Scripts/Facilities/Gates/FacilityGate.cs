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
        readonly GameObject gatedFacility;
        readonly bool playRevealCinematic;
        bool hasUnlocked;

        public event Action<FacilityGate> Revealed;

        public GameObject GatedFacility => gatedFacility;
        public bool PlayRevealCinematic => playRevealCinematic;
        public bool HasUnlocked => hasUnlocked;

        protected FacilityGate(GameObject _gatedFacility, bool _playRevealCinematic)
        {
            gatedFacility = _gatedFacility;
            playRevealCinematic = _playRevealCinematic;
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
