using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Four answer indicator lights overlaid on the live camera feed (marker seen + cover charge).
    /// </summary>
    public class CameraFeedAnswerLights : MonoBehaviour
    {
        [SerializeField] Image[] rings;
        [SerializeField] Image[] glows;
        [SerializeField] Image[] chargeFills;
        [SerializeField] Color dimRing = new Color(1f, 1f, 1f, 0.14f);

        static readonly Color[] Accents =
        {
            new Color(1f, 0.88f, 0.25f),
            new Color(0f, 0.898f, 1f),
            new Color(0f, 1f, 0.533f),
            new Color(1f, 0.2f, 0.8f),
        };

        public void SetSlot(int index, bool markerVisible, float coverCharge)
        {
            if (index < 0 || index >= 4)
                return;

            var accent = Accents[index];

            if (rings != null && index < rings.Length && rings[index] != null)
            {
                var ringTarget = markerVisible ? new Color(accent.r, accent.g, accent.b, 0.95f) : dimRing;
                rings[index].color = Color.Lerp(rings[index].color, ringTarget, Time.deltaTime * 10f);
            }

            if (glows != null && index < glows.Length && glows[index] != null)
            {
                var pulse = markerVisible ? 0.45f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * 5f + index)) : 0.04f;
                if (coverCharge > 0.01f)
                    pulse = Mathf.Max(pulse, 0.65f);
                glows[index].color = new Color(accent.r, accent.g, accent.b, pulse);
            }

            if (chargeFills != null && index < chargeFills.Length && chargeFills[index] != null)
                chargeFills[index].fillAmount = coverCharge;
        }
    }
}
