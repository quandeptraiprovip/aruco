using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Pushes webcam frames from OpenCV onto a world-space quad and optional UI RawImage (playing-screen viewport).
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    [DefaultExecutionOrder(-50)]
    public class ArCameraVideoBackground : MonoBehaviour
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] RawImage uiFeedImage;
        [SerializeField] float planeDistance = 8f;

        WebCamTextureToMatHelper _webCam;
        Texture2D _texture;
        Material _planeMaterial;

        void Awake()
        {
            _webCam = GetComponent<WebCamTextureToMatHelper>();
            _webCam.onInitialized.AddListener(OnCamReady);
            _webCam.onErrorOccurred.AddListener(OnCamError);

            if (uiFeedImage != null)
            {
                uiFeedImage.color = new Color(0.1f, 0.1f, 0.12f, 1f);
                if (uiFeedImage.GetComponent<CameraFeedRenderOrder>() == null)
                    uiFeedImage.gameObject.AddComponent<CameraFeedRenderOrder>();
            }
        }

        void Start()
        {
            if (_webCam.IsInitialized())
                OnCamReady();
        }

        void OnDestroy()
        {
            if (_webCam != null)
            {
                _webCam.onInitialized.RemoveListener(OnCamReady);
                _webCam.onErrorOccurred.RemoveListener(OnCamError);
            }

            if (_planeMaterial != null)
                Destroy(_planeMaterial);
        }

        void OnCamReady()
        {
            var mat = _webCam.GetMat();
            if (mat == null || mat.empty())
            {
                Debug.LogWarning("[ArucoQuiz] Camera init xong nhưng Mat rỗng — dùng WebCamTexture fallback.");
                ApplyWebCamFallback();
                return;
            }

            _texture = new Texture2D(mat.cols(), mat.rows(), TextureFormat.RGBA32, false);
            ApplyTexture(_texture, mat);
            FitPlaneToCamera();
            Debug.Log($"[ArucoQuiz] Camera feed OK {mat.cols()}×{mat.rows()} — {_webCam.GetWebCamDevice().name}");
        }

        void OnCamError(WebCamTextureToMatHelper.ErrorCode code)
        {
            if (uiFeedImage != null)
                uiFeedImage.color = new Color(0.25f, 0.08f, 0.08f, 1f);
        }

        void ApplyTexture(Texture2D tex, Mat mat)
        {
            if (targetRenderer != null)
            {
                if (_planeMaterial == null)
                    _planeMaterial = targetRenderer.material;
                _planeMaterial.mainTexture = tex;
            }

            if (uiFeedImage == null)
                return;

            uiFeedImage.texture = tex;
            uiFeedImage.color = Color.white;
            var fitter = uiFeedImage.GetComponent<AspectRatioFitter>();
            if (fitter != null && mat != null && mat.cols() > 0)
                fitter.aspectRatio = (float)mat.cols() / mat.rows();
        }

        void ApplyWebCamFallback()
        {
            var wct = _webCam.GetWebCamTexture();
            if (wct == null || uiFeedImage == null)
                return;
            uiFeedImage.texture = wct;
            uiFeedImage.color = Color.white;
            if (targetRenderer != null && _planeMaterial == null)
                _planeMaterial = targetRenderer.material;
            if (_planeMaterial != null)
                _planeMaterial.mainTexture = wct;
        }

        int _lastAspectCols;

        void LateUpdate()
        {
            if (_webCam == null || !_webCam.IsInitialized())
                return;

            if (_texture != null && _webCam.IsPlaying())
            {
                var mat = _webCam.GetMat();
                if (mat == null || mat.empty())
                    return;
                Utils.matToTexture2D(mat, _texture);
                if (uiFeedImage != null)
                {
                    var fitter = uiFeedImage.GetComponent<AspectRatioFitter>();
                    if (fitter != null && mat.cols() != _lastAspectCols)
                    {
                        fitter.aspectRatio = (float)mat.cols() / mat.rows();
                        _lastAspectCols = mat.cols();
                    }
                }

                return;
            }

            ApplyWebCamFallback();
        }

        void FitPlaneToCamera()
        {
            if (targetRenderer == null)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;

            var t = targetRenderer.transform;
            t.localPosition = new Vector3(0f, 0f, planeDistance);
            if (cam.orthographic)
            {
                var h = cam.orthographicSize * 2f;
                var w = h * cam.aspect;
                t.localScale = new Vector3(w, h, 1f);
            }
            else
            {
                var h = 2f * planeDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                var w = h * cam.aspect;
                t.localScale = new Vector3(w, h, 1f);
            }
        }
    }
}
