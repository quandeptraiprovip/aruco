using TMPro;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Raises podium render queue above the webcam RawImage without replacing shaders/textures.
    /// </summary>
    [DefaultExecutionOrder(310)]
    public class PodiumRenderStabilizer : MonoBehaviour
    {
        const int PodiumRenderQueue = 3500;

        Renderer[] _renderers;
        Material[] _instances;

        void OnEnable()
        {
            Apply();
        }

        void Apply()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (_instances == null || _instances.Length != _renderers.Length)
                _instances = new Material[_renderers.Length];

            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null)
                    continue;

                if (r.GetComponent<TextMeshPro>() != null)
                    continue;

                if (r is SpriteRenderer sr)
                    sr.sortingOrder = 200;

                var source = r.sharedMaterial;
                if (source == null)
                    continue;

                // On re-enable, r.sharedMaterial is often already our own instance from a
                // previous Apply() (assigned below) — copying a material's properties into
                // itself throws a native "assign data may not be data from the vector itself".
                if (ReferenceEquals(source, _instances[i]))
                    continue;

                if (_instances[i] == null || _instances[i].shader != source.shader)
                    _instances[i] = new Material(source);
                else
                    _instances[i].CopyPropertiesFromMaterial(source);

                _instances[i].renderQueue = PodiumRenderQueue;
                r.sharedMaterial = _instances[i];
            }

            foreach (var tmp in GetComponentsInChildren<TextMeshPro>(true))
            {
                if (tmp == null)
                    continue;
                var mr = tmp.GetComponent<Renderer>();
                if (mr != null)
                    mr.sortingOrder = 210;
                var mat = tmp.fontMaterial;
                if (mat != null && mat.renderQueue < PodiumRenderQueue)
                    mat.renderQueue = PodiumRenderQueue;
            }
        }

        void OnDestroy()
        {
            if (_instances == null)
                return;
            for (var i = 0; i < _instances.Length; i++)
            {
                if (_instances[i] != null)
                    Destroy(_instances[i]);
            }
        }
    }
}
