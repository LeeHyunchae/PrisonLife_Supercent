using UnityEngine;

namespace PrisonLife.Core
{
    public interface IResourceSink
    {
        ResourceType InputType { get; }
        bool CanAcceptOne();
        bool TryAcceptOne();

        /// <summary>
        /// 자원이 도착하는 위치 — 비행 시각효과의 to 기준점.
        /// null 이면 caller 가 자기 zone 의 transform 등 fallback 위치를 사용.
        /// </summary>
        Transform AnchorTransform { get; }
    }
}
