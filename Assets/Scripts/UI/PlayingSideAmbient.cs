using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    public class PlayingSideAmbient : MonoBehaviour
    {
        [SerializeField] Graphic[] twinkleGraphics;

        float _time;

        void Update()
        {
            if (twinkleGraphics == null)
                return;
            _time += Time.deltaTime;
            for (var i = 0; i < twinkleGraphics.Length; i++)
            {
                var g = twinkleGraphics[i];
                if (g == null)
                    continue;
                var phase = i * 1.1f;
                var a = 0.2f + 0.8f * (0.5f + 0.5f * Mathf.Sin(_time * (2f + i * 0.2f) + phase));
                var c = g.color;
                c.a = a;
                g.color = c;
            }
        }
    }
}
