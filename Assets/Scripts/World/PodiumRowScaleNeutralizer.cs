using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// World UI canvas scales to ~0.002; neutralize on this row so 3D cube children keep real-world size.
    /// </summary>
    [DefaultExecutionOrder(180)]
    public class PodiumRowScaleNeutralizer : MonoBehaviour
    {
        void LateUpdate()
        {
            var parent = transform.parent;
            if (parent == null)
                return;

            var ps = parent.lossyScale;
            var inv = new Vector3(
                ps.x > 1e-6f ? 1f / ps.x : 1f,
                ps.y > 1e-6f ? 1f / ps.y : 1f,
                ps.z > 1e-6f ? 1f / ps.z : 1f);
            transform.localScale = inv;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
