using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>Moves a RectTransform along an ellipse around a fixed center — unlike UiOrbitRing
    /// (which just spins a ring texture in place), this actually carries the object around a path,
    /// e.g. operator/letter badges circling a Prepare-screen hero badge.</summary>
    public class UiOrbitPoint : MonoBehaviour
    {
        [SerializeField] Vector2 center;
        [SerializeField] float radiusX = 260f;
        [SerializeField] float radiusY = 90f;
        [SerializeField] float degreesPerSecond = 24f;
        [SerializeField] float phaseDeg;

        RectTransform _rt;
        float _angleDeg;

        // Applies immediately (not just on the next Update) so the object sits at its correct
        // starting position even when nothing has ticked yet — e.g. the editor-time/validator
        // screenshot capture, which poses scenes without ever running Update.
        public void Configure(Vector2 orbitCenter, float rx, float ry, float speedDegPerSec, float startPhaseDeg)
        {
            center = orbitCenter;
            radiusX = rx;
            radiusY = ry;
            degreesPerSecond = speedDegPerSec;
            phaseDeg = startPhaseDeg;
            _angleDeg = startPhaseDeg;
            ApplyPosition();
        }

        void OnEnable()
        {
            _rt = transform as RectTransform;
            _angleDeg = phaseDeg;
            ApplyPosition();
        }

        void Update()
        {
            _angleDeg += degreesPerSecond * Time.deltaTime;
            ApplyPosition();
        }

        void ApplyPosition()
        {
            if (_rt == null)
                _rt = transform as RectTransform;
            if (_rt == null)
                return;
            var rad = _angleDeg * Mathf.Deg2Rad;
            _rt.anchoredPosition = center + new Vector2(Mathf.Cos(rad) * radiusX, Mathf.Sin(rad) * radiusY);
        }
    }
}
