using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Pins a 3D object to a viewport position at a fixed distance from the camera,
    /// so the podium row lines up on any aspect ratio.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class ViewportAnchor3D : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Vector2 viewportPos = new Vector2(0.5f, 0.2f);
        [SerializeField] float distance = 2.05f;
        [SerializeField] bool faceCamera = true;
        [SerializeField] bool compensateCanvasParentScale = false;

        public void Configure(Camera cam, Vector2 viewport, float dist)
        {
            targetCamera = cam;
            viewportPos = viewport;
            distance = dist;
        }

        // Also applied from OnEnable (not just LateUpdate) so the object sits at its correct
        // anchored position even when nothing has ticked yet — e.g. the editor-time/validator
        // screenshot capture, which poses scenes without ever running LateUpdate. Public because
        // editor builders configure this component via SerializedObject (not Configure()), which
        // doesn't itself re-trigger positioning — callers should invoke this once right after.
        void OnEnable() => Apply();

        void LateUpdate() => Apply();

        public void Apply()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
                return;

            var vp = viewportPos;
            var row = transform.parent;
            if (row != null && row.name == "PodiumRow3D")
            {
                var idx = transform.GetSiblingIndex();
                if (idx >= 0 && idx < 4)
                    vp = PodiumAnswerLayout.ViewportPosition(idx);
            }

            transform.position = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, PodiumAnswerLayout.ResolveCubeDistance(cam)));

            if (faceCamera)
                transform.rotation = cam.transform.rotation;

            if (compensateCanvasParentScale && transform.parent != null)
            {
                var ps = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    ps.x > 1e-6f ? 1f / ps.x : 1f,
                    ps.y > 1e-6f ? 1f / ps.y : 1f,
                    ps.z > 1e-6f ? 1f / ps.z : 1f);
            }
        }
    }
}
