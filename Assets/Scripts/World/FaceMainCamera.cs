using UnityEngine;

namespace ArucoQuiz
{
    public class FaceMainCamera : MonoBehaviour
    {
        Transform _cam;

        void LateUpdate()
        {
            if (_cam == null)
            {
                var c = Camera.main;
                if (c == null)
                    return;
                _cam = c.transform;
            }

            transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }
    }
}
