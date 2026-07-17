using UnityEngine;
using UnityEngine.EventSystems;

namespace ArucoQuiz
{
    public class PodiumUiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] float hoverScale = 1.04f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}
