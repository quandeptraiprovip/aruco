using UnityEngine;

namespace ArucoQuiz
{
    public class UiPulseScale : MonoBehaviour
    {
        [SerializeField] float amount = 0.06f;
        [SerializeField] float speed = 4f;

        bool _pulsing = true;

        public void SetPulsing(bool pulsing)
        {
            _pulsing = pulsing;
            if (!_pulsing)
                transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (!_pulsing)
                return;
            var s = 1f + amount * (0.5f + 0.5f * Mathf.Sin(Time.time * speed));
            transform.localScale = Vector3.one * s;
        }
    }
}
