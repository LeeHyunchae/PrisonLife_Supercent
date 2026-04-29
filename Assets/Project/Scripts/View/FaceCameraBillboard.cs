using UnityEngine;

namespace PrisonLife.View
{
    /// <summary>
    /// LateUpdate 마다 transform.rotation 을 카메라 회전과 동기화.
    /// World-space UI Canvas 나 일반 텍스트 렌더가 카메라를 향하도록 한다.
    /// 아이소메트릭 고정 카메라에서도 안전 (회전이 안 바뀌어도 무해함).
    /// </summary>
    public class FaceCameraBillboard : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;

        Transform cachedCameraTransform;

        void Start()
        {
            EnsureCameraTransform();
        }

        void LateUpdate()
        {
            if (cachedCameraTransform == null)
            {
                EnsureCameraTransform();
                if (cachedCameraTransform == null) return;
            }

            transform.rotation = cachedCameraTransform.rotation;
        }

        void EnsureCameraTransform()
        {
            if (targetCamera != null)
            {
                cachedCameraTransform = targetCamera.transform;
                return;
            }
            if (Camera.main != null)
            {
                cachedCameraTransform = Camera.main.transform;
            }
        }
    }
}
