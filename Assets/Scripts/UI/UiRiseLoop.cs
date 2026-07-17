using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>Continuously scrolls a UI element upward through a vertical range, looping back to
    /// the bottom and fading in/out near the ends so it never pops — used for English's
    /// "words rising" Prepare-screen background.</summary>
    public class UiRiseLoop : MonoBehaviour
    {
        [SerializeField] float speed = 40f;
        [SerializeField] float bottomY = -620f;
        [SerializeField] float topY = 620f;
        [SerializeField] float startOffset;

        RectTransform _rt;
        Graphic _graphic;
        float _baseAlpha;
        float _t;

        // Applies immediately rather than waiting for OnEnable/Update — empirically, OnEnable does
        // not reliably fire before this Configure() call when the component is added from an editor
        // script (AddComponent().Configure() in the same statement), so relying on it left objects
        // stuck at their pre-Configure creation position (e.g. the validator's screenshot capture,
        // which never ticks Update either).
        public void Configure(float riseSpeed, float rangeBottomY, float rangeTopY, float offset)
        {
            speed = riseSpeed;
            bottomY = rangeBottomY;
            topY = rangeTopY;
            startOffset = offset;
            CacheRefs();
            _t = startOffset;
            Apply();
        }

        void OnEnable()
        {
            CacheRefs();
            Apply();
        }

        void CacheRefs()
        {
            if (_rt == null)
                _rt = transform as RectTransform;
            if (_graphic == null)
            {
                _graphic = GetComponent<Graphic>();
                _baseAlpha = _graphic != null ? _graphic.color.a : 1f;
            }
        }

        void Update()
        {
            _t += Time.deltaTime * speed;
            Apply();
        }

        void Apply()
        {
            if (_rt == null)
                return;

            var range = Mathf.Max(1f, topY - bottomY);
            var localT = Mathf.Repeat(_t, range);
            var pos = _rt.anchoredPosition;
            pos.y = bottomY + localT;
            _rt.anchoredPosition = pos;

            if (_graphic == null)
                return;

            var fade = range * 0.18f;
            var mul = 1f;
            if (localT < fade)
                mul = localT / fade;
            else if (localT > range - fade)
                mul = (range - localT) / fade;

            var c = _graphic.color;
            c.a = _baseAlpha * Mathf.Clamp01(mul);
            _graphic.color = c;
        }
    }
}
