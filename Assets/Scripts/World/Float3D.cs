using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// HTML floatA–D for real 3D objects: gentle Y bob combined with X/Y/Z rotation sway.
    /// </summary>
    public class Float3D : MonoBehaviour
    {
        [SerializeField] float bobAmplitude = 0.03f;
        [SerializeField] float speed = 1.1f;
        [SerializeField] float phase;
        [SerializeField] Vector3 baseEuler = new Vector3(16f, -22f, -2f);
        [SerializeField] Vector3 swayEuler = new Vector3(3f, 12f, 2f);

        Vector3 _basePos;
        float _intensity = 1f;

        public void Configure(float bob, float spd, float ph, Vector3 baseRot, Vector3 sway)
        {
            bobAmplitude = bob;
            speed = spd;
            phase = ph;
            baseEuler = baseRot;
            swayEuler = sway;
        }

        public void SetIntensity(float mult) => _intensity = Mathf.Max(0.1f, mult);

        void OnEnable()
        {
            _basePos = transform.localPosition;
            Apply(0f);
        }

        void Update()
        {
            Apply(Time.time * speed + phase);
        }

        void Apply(float t)
        {
            var s = Mathf.Sin(t);
            var c = Mathf.Cos(t * 0.8f);
            transform.localPosition = _basePos + new Vector3(0f, (s * 0.5f + 0.5f) * bobAmplitude * _intensity, 0f);
            var sway = swayEuler * _intensity;
            transform.localRotation = Quaternion.Euler(
                baseEuler.x + sway.x * s,
                baseEuler.y + sway.y * c,
                baseEuler.z + sway.z * s);
        }

        public float NormalizedBob => Mathf.Sin(Time.time * speed + phase) * 0.5f + 0.5f;
    }
}
