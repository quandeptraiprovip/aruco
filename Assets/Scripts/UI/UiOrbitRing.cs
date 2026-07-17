using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>Continuous Z-rotation for the countdown orbital rings.</summary>
    public class UiOrbitRing : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 90f;

        public void Configure(float speed)
        {
            degreesPerSecond = speed;
        }

        void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
