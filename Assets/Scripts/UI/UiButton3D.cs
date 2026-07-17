using UnityEngine;
using UnityEngine.EventSystems;

namespace ArucoQuiz
{
    /// <summary>
    /// Chunky 3D button feel: the face floats above a fixed shadow, lifts on hover
    /// and presses down flush on click (mirrors the HTML hover/active transforms).
    /// </summary>
    public class UiButton3D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform face;
        [SerializeField] float hoverLift = 5f;
        [SerializeField] float pressDrop = 7f;
        [SerializeField] float speed = 22f;

        Vector2 _restPos;
        float _targetOffset;
        bool _hover;
        bool _pressed;

        public void Configure(RectTransform faceRect)
        {
            face = faceRect;
        }

        void OnEnable()
        {
            if (face != null)
                _restPos = face.anchoredPosition;
            _hover = false;
            _pressed = false;
            _targetOffset = 0f;
        }

        void OnDisable()
        {
            if (face != null)
                face.anchoredPosition = _restPos;
        }

        void Update()
        {
            if (face == null)
                return;
            _targetOffset = _pressed ? -pressDrop : (_hover ? hoverLift : 0f);
            var current = face.anchoredPosition;
            var target = _restPos + new Vector2(0f, _targetOffset);
            face.anchoredPosition = Vector2.Lerp(current, target, Time.deltaTime * speed);
        }

        public void OnPointerEnter(PointerEventData eventData) => _hover = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => _pressed = true;

        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
    }
}
