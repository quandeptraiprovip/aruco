using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// HTML "bounceIn": scale .1 → 1.18 → .96 → 1 with fade-in. Plays on enable.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiBounceIn : MonoBehaviour
    {
        [SerializeField] float duration = 0.5f;

        CanvasGroup _group;
        float _t;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        void OnEnable()
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
            float s;
            if (t < 0.55f)
                s = Mathf.Lerp(0.1f, 1.18f, EaseOut(t / 0.55f));
            else if (t < 0.8f)
                s = Mathf.Lerp(1.18f, 0.96f, (t - 0.55f) / 0.25f);
            else
                s = Mathf.Lerp(0.96f, 1f, (t - 0.8f) / 0.2f);
            transform.localScale = Vector3.one * s;
            if (_group != null)
                _group.alpha = Mathf.Clamp01(t / 0.4f);
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
