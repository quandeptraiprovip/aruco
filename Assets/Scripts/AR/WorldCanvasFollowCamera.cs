using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Keeps a world-space canvas anchored in front of the AR camera, scaled with a "cover" fit
    /// (like CSS background-size:cover) so it always fully fills the viewport on both axes and
    /// never exposes the camera background behind it, regardless of the display's aspect ratio.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class WorldCanvasFollowCamera : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] float distance = 1.85f;
        [SerializeField] Vector3 localOffset;
        [SerializeField] Vector2 designResolution = new Vector2(1920, 1080);
        [SerializeField] [Range(1f, 1.1f)] float overscan = 1.02f;

        public float PlaneDistance => distance;

        /// <summary>Full camera-viewport width/height (world units) at <see cref="PlaneDistance"/>.</summary>
        public float WorldWidth { get; private set; }
        public float WorldHeight { get; private set; }

        /// <summary>Uniform scale actually applied to the canvas this frame.</summary>
        public float AppliedScale { get; private set; }

        RectTransform _rt;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (_rt != null)
                _rt.sizeDelta = designResolution;
        }

        void LateUpdate()
        {
            Reposition(targetCamera != null ? targetCamera : Camera.main);
        }

        /// <summary>Repositions/rescales the canvas against a given camera. Public so editor
        /// tooling (screenshot capture) can pose it outside Play mode, where LateUpdate never runs.</summary>
        public void Reposition(Camera cam)
        {
            if (cam == null)
                return;

            transform.position = cam.transform.position + cam.transform.forward * distance +
                               cam.transform.TransformVector(localOffset);
            transform.rotation = cam.transform.rotation;

            var halfHeight = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var halfWidth = halfHeight * cam.aspect;
            WorldWidth = halfWidth * 2f;
            WorldHeight = halfHeight * 2f;

            AppliedScale = Mathf.Max(WorldWidth / designResolution.x, WorldHeight / designResolution.y) * overscan;
            transform.localScale = Vector3.one * AppliedScale;
        }
    }
}
