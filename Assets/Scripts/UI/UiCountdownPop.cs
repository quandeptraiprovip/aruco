using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// HTML "cdAnim": scale 2.6 (fade in) → 1 → hold → shrink out. Re-triggered each countdown tick.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiCountdownPop : MonoBehaviour
    {
        [SerializeField] float duration = 1f;

        CanvasGroup _group;
        float _t;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            Play();
        }

        public void Play()
        {
            _t = 0f;
            Apply(0f);
        }

        void Update()
        {
            if (_t >= duration)
                return;
            _t += Time.deltaTime;
            Apply(Mathf.Clamp01(_t / duration));
        }

        void Apply(float t)
        {
            float s, a;
            if (t < 0.25f)
            {
                var k = EaseOut(t / 0.25f);
                s = Mathf.Lerp(2.6f, 1f, k);
                a = k;
            }
            else if (t < 0.8f)
            {
                s = 1f;
                a = 1f;
            }
            else
            {
                var k = (t - 0.8f) / 0.2f;
                s = Mathf.Lerp(1f, 0.2f, k * k);
                a = 1f - k;
            }

            transform.localScale = Vector3.one * s;
            if (_group != null)
                _group.alpha = a;
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
