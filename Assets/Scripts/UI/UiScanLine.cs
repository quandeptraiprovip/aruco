using UnityEngine;

namespace ArucoQuiz
{
    public class UiScanLine : MonoBehaviour
    {
        [SerializeField] RectTransform line;
        [SerializeField] float speed = 280f;
        [SerializeField] float travel = 420f;

        float _y;

        void Reset()
        {
            line = transform as RectTransform;
        }

        void OnEnable()
        {
            _y = travel * 0.5f;
        }

        void Update()
        {
            if (line == null)
                return;
            _y -= speed * Time.deltaTime;
            if (_y < -travel)
                _y = travel;
            line.anchoredPosition = new Vector2(0, _y);
        }
    }
}
