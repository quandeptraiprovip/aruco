using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>Sweeps a soft glow horizontally across a title, pausing between passes —
    /// a periodic "shine" accent for the Prepare-screen title text.</summary>
    public class UiShimmerSweep : MonoBehaviour
    {
        [SerializeField] float travel = 1400f;
        [SerializeField] float sweepDuration = 1.6f;
        [SerializeField] float pauseDuration = 3.2f;
        [SerializeField] float startDelay;

        RectTransform _rt;
        Graphic _graphic;
        float _baseAlpha;
        float _t;

        // Applies immediately rather than waiting for OnEnable/Update — empirically, OnEnable does
        // not reliably fire before this Configure() call when the component is added from an editor
        // script (AddComponent().Configure() in the same statement), so relying on it left the bar
        // stuck at its pre-Configure creation state (e.g. the validator's screenshot capture, which
        // never ticks Update either).
        public void Configure(float travelDistance, float sweep, float pause, float delay)
        {
            travel = travelDistance;
            sweepDuration = sweep;
            pauseDuration = pause;
            startDelay = delay;
            CacheRefs();
            _t = -startDelay;
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
                _baseAlpha = _graphic != null ? _graphic.color.a : 0f;
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            Apply();
        }

        // Also called from OnEnable so the bar starts hidden (mid-pause) instead of frozen
        // mid-sweep at full alpha when nothing has ticked yet — e.g. the validator screenshot.
        void Apply()
        {
            if (_rt == null)
                return;

            var cycle = sweepDuration + pauseDuration;
            var local = _t % cycle;
            if (local < 0f)
                local += cycle;

            if (local <= sweepDuration)
            {
                var p = local / sweepDuration;
                var x = Mathf.Lerp(-travel * 0.5f, travel * 0.5f, p);
                _rt.anchoredPosition = new Vector2(x, _rt.anchoredPosition.y);
                SetAlpha(_baseAlpha * Mathf.Sin(p * Mathf.PI));
            }
            else
            {
                SetAlpha(0f);
            }
        }

        void SetAlpha(float a)
        {
            if (_graphic == null)
                return;
            var c = _graphic.color;
            c.a = a;
            _graphic.color = c;
        }
    }
}
