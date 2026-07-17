using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    public class UiGlowPulse : MonoBehaviour
    {
        [SerializeField] Image glowImage;
        [SerializeField] float speed = 1.2f;
        [SerializeField] float minAlpha = 0.12f;
        [SerializeField] float maxAlpha = 0.35f;

        void Update()
        {
            if (glowImage == null)
                return;
            var t = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
            var c = glowImage.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            glowImage.color = c;
        }
    }
}
