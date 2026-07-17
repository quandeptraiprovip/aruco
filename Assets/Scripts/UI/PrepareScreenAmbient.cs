using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Subtle motion for prepare screen nebula orbs and star twinkles (matches HTML prototype).
    /// </summary>
    public class PrepareScreenAmbient : MonoBehaviour
    {
        [SerializeField] Graphic[] twinkleStars;
        [SerializeField] Image[] nebulaOrbs;
        [SerializeField] Image gridOverlay;

        float _time;

        void Update()
        {
            _time += Time.deltaTime;

            if (twinkleStars != null)
            {
                for (var i = 0; i < twinkleStars.Length; i++)
                {
                    var img = twinkleStars[i];
                    if (img == null)
                        continue;
                    var phase = i * 0.7f;
                    var a = 0.15f + 0.85f * (0.5f + 0.5f * Mathf.Sin(_time * (2.2f + i * 0.15f) + phase));
                    var c = img.color;
                    c.a = a;
                    img.color = c;
                }
            }

            if (nebulaOrbs != null)
            {
                for (var i = 0; i < nebulaOrbs.Length; i++)
                {
                    var img = nebulaOrbs[i];
                    if (img == null)
                        continue;
                    var pulse = 1f + 0.04f * Mathf.Sin(_time * 0.6f + i);
                    img.rectTransform.localScale = Vector3.one * pulse;
                }
            }

            if (gridOverlay != null)
            {
                var c = gridOverlay.color;
                c.a = 0.028f + 0.027f * (0.5f + 0.5f * Mathf.Sin(_time * 0.785f));
                gridOverlay.color = c;
            }
        }
    }
}
