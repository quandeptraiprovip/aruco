using UnityEngine;

namespace ArucoQuiz
{
    public class UiFloatMotion : MonoBehaviour
    {
        [SerializeField] float amplitude = 14f;
        [SerializeField] float speed = 1.1f;
        [SerializeField] float phase;

        Vector2 _base;

        void OnEnable()
        {
            var rt = transform as RectTransform;
            if (rt != null)
                _base = rt.anchoredPosition;
        }

        void Update()
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;
            var y = Mathf.Sin(Time.time * speed + phase) * amplitude;
            rt.anchoredPosition = _base + new Vector2(0, y);
        }
    }
}
