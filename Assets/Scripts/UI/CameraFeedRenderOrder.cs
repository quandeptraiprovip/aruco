using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Forces the live webcam RawImage to render before podium overlays (lower render queue).
    /// </summary>
    [DefaultExecutionOrder(320)]
    [RequireComponent(typeof(RawImage))]
    public class CameraFeedRenderOrder : MonoBehaviour
    {
        const int FeedRenderQueue = 2900;

        RawImage _image;
        Material _feedMat;

        void Awake()
        {
            _image = GetComponent<RawImage>();
            EnsureMaterial();
        }

        void LateUpdate()
        {
            EnsureMaterial();
        }

        void EnsureMaterial()
        {
            if (_image == null)
                return;

            if (_feedMat == null)
            {
                var baseMat = _image.material != null ? _image.material : _image.defaultMaterial;
                _feedMat = new Material(baseMat);
                _feedMat.renderQueue = FeedRenderQueue;
                _image.material = _feedMat;
            }
            else if (_feedMat.renderQueue != FeedRenderQueue)
            {
                _feedMat.renderQueue = FeedRenderQueue;
            }
        }

        void OnDestroy()
        {
            if (_feedMat != null)
                Destroy(_feedMat);
        }
    }
}
