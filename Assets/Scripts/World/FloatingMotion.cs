using UnityEngine;

namespace ArucoQuiz
{
    public class FloatingMotion : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.15f;
        [SerializeField] float speed = 1.2f;
        [SerializeField] float spinSpeed = 8f;

        Vector3 _origin;

        void Start()
        {
            _origin = transform.localPosition;
        }

        void Update()
        {
            var y = Mathf.Sin(Time.time * speed) * amplitude;
            transform.localPosition = _origin + Vector3.up * y;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
