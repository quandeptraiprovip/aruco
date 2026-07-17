using System.Collections;
using OpenCVForUnity.UnityUtils.Helper;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Picks a webcam and starts OpenCV capture, retrying with lower resolutions if the first open times out (common on macOS).
    /// </summary>
    [DefaultExecutionOrder(0)]
    public class ArQuizOpenCvBootstrap : MonoBehaviour
    {
        [Tooltip("Prefer rear/external camera when available; otherwise front (FaceTime on Mac).")]
        [SerializeField] bool preferRearCamera = true;

        WebCamTextureToMatHelper _helper;
        string _deviceName;
        bool _deviceFrontFacing;
        int _profileIndex;
        bool _finished;

        static readonly (int width, int height, float fps)[] Profiles =
        {
            (640, 480, 15f),
            (640, 480, 30f),
            (1280, 720, 24f),
            (0, 0, 15f),
        };

        void Awake()
        {
            _helper = GetComponent<WebCamTextureToMatHelper>();
            if (_helper == null)
                return;

            _helper.onErrorOccurred.AddListener(OnWebCamError);
            _helper.onInitialized.AddListener(OnWebCamReady);
            _helper.timeoutFrameCount = Mathf.Max(_helper.timeoutFrameCount, 900);

            LogDevices();
            if (!PickDevice(out _deviceName, out _deviceFrontFacing))
                return;

            _helper.requestedIsFrontFacing = _deviceFrontFacing;
        }

        void Start()
        {
            if (_helper == null || string.IsNullOrEmpty(_deviceName) || _helper.IsInitialized())
                return;

            BeginProfile(0);
        }

        void OnDestroy()
        {
            if (_helper == null)
                return;
            _helper.onErrorOccurred.RemoveListener(OnWebCamError);
            _helper.onInitialized.RemoveListener(OnWebCamReady);
        }

        void BeginProfile(int index)
        {
            _profileIndex = index;
            var p = Profiles[index];
            Debug.Log(
                $"[ArucoQuiz] Mở camera \"{_deviceName}\" {FormatSize(p.width, p.height)} @ {p.fps:0.#}fps (thử {index + 1}/{Profiles.Length})");
            _helper.Initialize(_deviceName, p.width, p.height, _deviceFrontFacing, p.fps);
        }

        static string FormatSize(int w, int h) => w <= 0 || h <= 0 ? "native" : $"{w}×{h}";

        void OnWebCamReady()
        {
            _finished = true;
            var wct = _helper.GetWebCamTexture();
            var w = wct != null ? wct.width : 0;
            var h = wct != null ? wct.height : 0;
            Debug.Log($"[ArucoQuiz] Webcam sẵn sàng \"{_deviceName}\" ({w}×{h}).");
        }

        void OnWebCamError(WebCamTextureToMatHelper.ErrorCode code)
        {
            if (_finished || _helper.IsInitialized())
                return;

            if (code == WebCamTextureToMatHelper.ErrorCode.TIMEOUT && _profileIndex + 1 < Profiles.Length)
            {
                Debug.LogWarning("[ArucoQuiz] Camera timeout — thử cấu hình thấp hơn…");
                StartCoroutine(RetryNextProfile());
                return;
            }

            switch (code)
            {
                case WebCamTextureToMatHelper.ErrorCode.CAMERA_DEVICE_NOT_EXIST:
                    Debug.LogError("[ArucoQuiz] Không tìm thấy thiết bị camera.");
                    break;
                case WebCamTextureToMatHelper.ErrorCode.TIMEOUT:
                    Debug.LogError(
                        "[ArucoQuiz] Camera timeout sau mọi cấu hình. Đóng Zoom/Teams/OBS, khởi động lại Unity, kiểm tra quyền Camera (macOS), thử camera FaceTime vật lý.");
                    break;
                case WebCamTextureToMatHelper.ErrorCode.CAMERA_PERMISSION_DENIED:
                    Debug.LogError("[ArucoQuiz] Quyền camera bị từ chối.");
                    break;
                default:
                    Debug.LogError("[ArucoQuiz] WebCam lỗi: " + code);
                    break;
            }
        }

        IEnumerator RetryNextProfile()
        {
            yield return new WaitForSeconds(0.35f);
            if (_helper.IsInitialized())
                yield break;
            BeginProfile(_profileIndex + 1);
        }

        static void LogDevices()
        {
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogWarning("[ArucoQuiz] WebCamTexture.devices rỗng — cấp quyền Camera cho Unity Editor (macOS).");
                return;
            }

            for (var i = 0; i < devices.Length; i++)
                Debug.Log($"[ArucoQuiz] Camera [{i}] \"{devices[i].name}\" front={devices[i].isFrontFacing}");
        }

        bool PickDevice(out string deviceName, out bool isFront)
        {
            deviceName = null;
            isFront = true;
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
                return false;

            if (preferRearCamera)
            {
                foreach (var d in devices)
                {
                    if (!d.isFrontFacing)
                    {
                        deviceName = d.name;
                        isFront = false;
                        Debug.Log("[ArucoQuiz] Chọn camera sau/ngoài: " + deviceName);
                        return true;
                    }
                }
            }

            foreach (var d in devices)
            {
                if (d.isFrontFacing)
                {
                    deviceName = d.name;
                    isFront = true;
                    Debug.Log("[ArucoQuiz] Chọn camera trước: " + deviceName);
                    return true;
                }
            }

            deviceName = devices[0].name;
            isFront = devices[0].isFrontFacing;
            Debug.Log("[ArucoQuiz] Chọn camera mặc định: " + deviceName);
            return true;
        }
    }
}
