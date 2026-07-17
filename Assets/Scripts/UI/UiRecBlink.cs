using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    public class UiRecBlink : MonoBehaviour
    {
        [SerializeField] Graphic dot;

        void Update()
        {
            if (dot == null)
                return;
            var a = 0.15f + 0.85f * (0.5f + 0.5f * Mathf.Sin(Time.time * 6.28f));
            var c = dot.color;
            c.a = a;
            dot.color = c;
        }
    }
}
