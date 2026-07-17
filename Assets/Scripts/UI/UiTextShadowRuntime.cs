using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Adds UI Shadow at runtime (Shadow on TMP during editor scene build breaks on NewScene unload).
    /// </summary>
    public class UiTextShadowRuntime : MonoBehaviour
    {
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] Vector2 distance = new Vector2(2f, -2f);

        void Start()
        {
            if (GetComponent<Shadow>() != null)
                return;
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = distance;
        }
    }
}
