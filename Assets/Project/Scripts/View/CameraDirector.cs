using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PrisonLife.View
{
    /// <summary>
    /// 시네마틱 카메라 시퀀스 매니저. PlayFocusOnAsync 로 특정 target 으로 카메라를 이동→홀드→복귀.
    /// 시네마 동안 Time.timeScale = 0 으로 게임 전체 정지 (NPC, 조이스틱 이동, 비행, 채굴, 수갑 생산 등 모두 멈춤).
    /// 시네마 자신은 unscaled 시간을 사용해 정상 진행. 종료 시 timeScale 복원 + CameraFollow 재활성.
    /// 호출자: FacilityGatesController (gate reveal), PrisonExpandPurchaseZone (확장 구매 후 엔딩 전).
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private CameraFollow cameraFollow;

        [Header("Cinematic Defaults")]
        [SerializeField] private Vector3 cinematicWorldOffset = new Vector3(0f, 8f, -6f);
        [SerializeField, Min(0.05f)] private float moveDurationSeconds = 1.0f;
        [SerializeField, Min(0f)] private float holdDurationSeconds = 1.5f;
        [SerializeField, Min(0.05f)] private float returnDurationSeconds = 1.0f;

        private bool isCinematicActive;

        public bool IsCinematicActive => isCinematicActive;

        public async UniTask PlayFocusOnAsync(Transform _target, CancellationToken _cancellationToken = default)
        {
            if (_target == null || cameraFollow == null) return;
            if (isCinematicActive) return;

            isCinematicActive = true;
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            try
            {
                Transform cameraTransform = cameraFollow.transform;
                Quaternion originalRotation = cameraTransform.rotation;

                cameraFollow.enabled = false;

                Vector3 cinematicPosition = _target.position + cinematicWorldOffset;
                Vector3 lookDirection = _target.position - cinematicPosition;
                Quaternion cinematicRotation = lookDirection.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                    : originalRotation;

                Vector3 moveStartPosition = cameraTransform.position;
                await LerpAsync(cameraTransform, moveStartPosition, cinematicPosition, originalRotation, cinematicRotation, moveDurationSeconds, _cancellationToken);

                if (holdDurationSeconds > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(holdDurationSeconds),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        _cancellationToken);
                }

                Vector3 returnTargetPosition = cameraFollow.Target != null
                    ? cameraFollow.Target.position + cameraFollow.WorldOffsetFromTarget
                    : moveStartPosition;
                await LerpAsync(cameraTransform, cameraTransform.position, returnTargetPosition, cameraTransform.rotation, originalRotation, returnDurationSeconds, _cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 정상 취소
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                if (cameraFollow != null) cameraFollow.enabled = true;
                isCinematicActive = false;
            }
        }

        private async UniTask LerpAsync(
            Transform _cameraTransform,
            Vector3 _fromPosition,
            Vector3 _toPosition,
            Quaternion _fromRotation,
            Quaternion _toRotation,
            float _durationSeconds,
            CancellationToken _cancellationToken)
        {
            float elapsedSeconds = 0f;
            while (elapsedSeconds < _durationSeconds)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                elapsedSeconds += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedSeconds / _durationSeconds);
                _cameraTransform.position = Vector3.Lerp(_fromPosition, _toPosition, t);
                _cameraTransform.rotation = Quaternion.Slerp(_fromRotation, _toRotation, t);
                await UniTask.Yield();
            }
            _cameraTransform.position = _toPosition;
            _cameraTransform.rotation = _toRotation;
        }
    }
}
