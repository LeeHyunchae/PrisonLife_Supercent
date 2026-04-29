using UnityEngine;

namespace PrisonLife.View
{
    /// <summary>
    /// 카메라가 target 을 약하게 따라가도록 한다. 회전은 고정 (인스펙터에서 설정한 값 유지).
    /// LateUpdate 에서 적용 — 플레이어 이동(Update) 후에 카메라 보정.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 worldOffsetFromTarget = new Vector3(0f, 12f, -8f);
        [SerializeField, Min(0.01f)] float smoothTimeSeconds = 0.25f;
        [SerializeField] bool snapToTargetOnStart = true;

        Vector3 currentSmoothVelocity;

        void Start()
        {
            if (target == null) return;
            if (snapToTargetOnStart)
            {
                transform.position = target.position + worldOffsetFromTarget;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + worldOffsetFromTarget;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentSmoothVelocity,
                smoothTimeSeconds);
        }
    }
}
