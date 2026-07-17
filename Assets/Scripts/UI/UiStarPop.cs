using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// HTML "starPop": scale 0 rot -30° → 1.3 rot 10° → 1 rot 0. Plays on enable with delay.
    /// </summary>
    public class UiStarPop : MonoBehaviour
    {
        [SerializeField] float delay = 0.3f;
        [SerializeField] float duration = 0.5f;

        float _t;

        public void Configure(float startDelay)
        {
            delay = startDelay;
        }

        void OnEnable()
        {
            _t = 0f;
            Apply(0f);
        }

        void Update()
        {
            if (_t >= delay + duration)
                return;
            _t += Time.deltaTime;
            Apply(Mathf.Clamp01((_t - delay) / duration));
        }

        void Apply(float t)
        {
            float s, rot;
            if (t <= 0f)
            {
                s = 0f;
                rot = -30f;
            }
            else if (t < 0.7f)
            {
                var k = EaseOut(t / 0.7f);
                s = Mathf.Lerp(0f, 1.3f, k);
                rot = Mathf.Lerp(-30f, 10f, k);
            }
            else
            {
                var k = (t - 0.7f) / 0.3f;
                s = Mathf.Lerp(1.3f, 1f, k);
                rot = Mathf.Lerp(10f, 0f, k);
            }

            transform.localScale = Vector3.one * s;
            transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
