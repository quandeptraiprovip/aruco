using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// HTML "slideUp": translateY(55px)+fade → rest position. Plays on enable, honours a start delay.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiSlideUpFade : MonoBehaviour
    {
        [SerializeField] float delay;
        [SerializeField] float duration = 0.75f;
        [SerializeField] float offsetY = 55f;

        CanvasGroup _group;
        RectTransform _rt;
        Vector2 _restPos;
        bool _captured;
        float _t;

        public void Configure(float startDelay)
        {
            delay = startDelay;
        }

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _rt = transform as RectTransform;
        }

        void OnEnable()
        {
            if (_rt != null && !_captured)
            {
                _restPos = _rt.anchoredPosition;
                _captured = true;
            }

            _t = 0f;
            Apply(0f);
        }

        void OnDisable()
        {
            // Restore rest state so re-captures and layout stay clean.
            if (_rt != null && _captured)
                _rt.anchoredPosition = _restPos;
            if (_group != null)
                _group.alpha = 1f;
        }

        void Update()
        {
            if (_t >= delay + duration)
                return;
            _t += Time.deltaTime;
            var k = Mathf.Clamp01((_t - delay) / duration);
            Apply(k);
        }

        void Apply(float k)
        {
            var e = 1f - (1f - k) * (1f - k) * (1f - k);
            if (_rt != null)
                _rt.anchoredPosition = _restPos + new Vector2(0f, -offsetY * (1f - e));
            if (_group != null)
                _group.alpha = k;
        }
    }
}
