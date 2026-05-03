using UnityEngine;
using UnityEngine.EventSystems;

namespace PrisonLife.Input
{
    public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private float handleMoveRange = 100f;

        private Vector2 currentNormalizedDirection;
        private bool isCurrentlyActive;

        public Vector2 CurrentNormalizedDirection => currentNormalizedDirection;
        public bool IsCurrentlyActive => isCurrentlyActive;

        private void Awake()
        {
            if (backgroundRect != null) backgroundRect.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData _eventData)
        {
            isCurrentlyActive = true;

            if (backgroundRect != null)
            {
                backgroundRect.gameObject.SetActive(true);

                Vector2 localPointInThisRect;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform as RectTransform,
                    _eventData.position,
                    _eventData.pressEventCamera,
                    out localPointInThisRect);
                backgroundRect.anchoredPosition = localPointInThisRect;
            }

            if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;
            currentNormalizedDirection = Vector2.zero;
        }

        public void OnDrag(PointerEventData _eventData)
        {
            if (!isCurrentlyActive || backgroundRect == null) return;

            Vector2 backgroundScreenPosition = RectTransformUtility.WorldToScreenPoint(
                _eventData.pressEventCamera,
                backgroundRect.position);

            Vector2 deltaPixels = _eventData.position - backgroundScreenPosition;
            Vector2 clampedDelta = Vector2.ClampMagnitude(deltaPixels, handleMoveRange);

            if (handleRect != null) handleRect.anchoredPosition = clampedDelta;
            currentNormalizedDirection = clampedDelta / handleMoveRange;
        }

        public void OnPointerUp(PointerEventData _eventData)
        {
            isCurrentlyActive = false;
            if (backgroundRect != null) backgroundRect.gameObject.SetActive(false);
            if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;
            currentNormalizedDirection = Vector2.zero;
        }
    }
}
