using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Cover-to-answer detection. Physical mat: ArUco IDs 0–3 (DICT_4X4_50) → A–D.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class OpenCvArucoAnswerDetector : MonoBehaviour
    {
        [SerializeField] QuizGameController game;
        [SerializeField] float holdSeconds = 2f;
        [SerializeField] float visibleGraceSeconds = 0.35f;
        [SerializeField] float startStableSeconds = 0.45f;
        [SerializeField] bool enableKeyboardFallback = true;
        [SerializeField] bool logDetectionToConsole;
        [SerializeField] float logIntervalSeconds = 2f;

        WebCamTextureToMatHelper _webCam;
        Dictionary _dictionary;
        DetectorParameters _detectorParams;
        Mat _gray;
        Mat _ids;
        List<Mat> _corners;
        List<Mat> _rejected;

        readonly float[] _lastSeen =
        {
            float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity
        };

        readonly List<int> _lastFrameDetectedIds = new List<int>(8);
        bool _armed;
        int _armedQuestion = -1;
        int _coverCandidate = -1;
        float _coverStart = -1f;
        float _nextLogTime;
        float _allFourSince = -1f;
        bool _cameraMatOk;

        bool _menuArmed;
        int _menuCoverCandidate = -1;
        float _menuCoverStart = -1f;
        QuizScreen _menuUnlockScreen;
        float _menuActionCooldownUntil;

        public bool MatReady => _armed;
        public int CoverCandidate => _coverCandidate;
        public int MenuCoverCandidate => _menuCoverCandidate;
        public float HoldSeconds => holdSeconds;
        public int VisibleMarkerCount { get; private set; }
        public bool AllFourMarkersInView { get; private set; }
        public bool IsReadyToStartGame { get; private set; }

        /// <summary>Prepare/Result: saw 4 stable markers; stays true while exactly one is covered.</summary>
        public bool MenuMatArmed => _menuArmed;

        public bool IsSelectingAnswer =>
            game != null && game.Screen == QuizScreen.Playing && !game.ShowFeedback && _armed &&
            _coverCandidate >= 0 && _coverStart >= 0f;

        public bool CameraFeedReady => _webCam != null && _webCam.IsPlaying();
        public IReadOnlyList<int> LastFrameDetectedIds => _lastFrameDetectedIds;

        public bool IsMarkerVisible(int answerIndex)
        {
            if (answerIndex < 0 || answerIndex >= ArucoMarkerConfig.AnswerCount)
                return false;
            return Time.time - _lastSeen[answerIndex] <= visibleGraceSeconds;
        }

        public float GetCoverProgress(int answerIndex)
        {
            if (answerIndex != _coverCandidate || _coverStart < 0f)
                return 0f;
            return Mathf.Clamp01((Time.time - _coverStart) / holdSeconds);
        }

        public float GetMenuCoverProgress()
        {
            if (_menuCoverCandidate < 0 || _menuCoverStart < 0f)
                return 0f;
            return Mathf.Clamp01((Time.time - _menuCoverStart) / holdSeconds);
        }

        void Start()
        {
            _webCam = GetComponent<WebCamTextureToMatHelper>();
            _dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_4X4_50);
            _detectorParams = DetectorParameters.create();
            _gray = new Mat();
            _ids = new Mat();
            _corners = new List<Mat>();
            _rejected = new List<Mat>();
        }

        void OnDestroy()
        {
            _gray?.Dispose();
            _ids?.Dispose();
        }

        void Update()
        {
            _lastFrameDetectedIds.Clear();
            _cameraMatOk = false;

            DetectMarkers();
            UpdateStartReadiness();

            var screen = game != null ? game.Screen : QuizScreen.Prepare;
            if (screen == QuizScreen.Prepare || screen == QuizScreen.Result)
                TryMenuCoverAction(screen);
            else
                ResetMenuCoverState();

            if (logDetectionToConsole && Time.time >= _nextLogTime)
            {
                _nextLogTime = Time.time + logIntervalSeconds;
                LogDetectionStatus();
            }

            if (game == null || game.Screen != QuizScreen.Playing || game.ShowFeedback)
            {
                ClearCandidate();
                if (game == null || game.Screen != QuizScreen.Playing)
                    _armedQuestion = -1;
                return;
            }

            if (enableKeyboardFallback)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) game.SubmitAnswer(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) game.SubmitAnswer(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) game.SubmitAnswer(2);
                if (Input.GetKeyDown(KeyCode.Alpha4)) game.SubmitAnswer(3);
            }

            if (_armedQuestion != game.QuestionIndex)
            {
                _armedQuestion = game.QuestionIndex;
                _armed = false;
                game.SetQuestionMatReady(false);
                ClearCandidate();
            }

            var visibleCount = 0;
            var missingIndex = -1;
            for (var i = 0; i < ArucoMarkerConfig.AnswerCount; i++)
            {
                if (IsMarkerVisible(i))
                    visibleCount++;
                else
                    missingIndex = i;
            }

            if (visibleCount == ArucoMarkerConfig.AnswerCount)
            {
                _armed = true;
                game.SetQuestionMatReady(true);
                ClearCandidate();
                return;
            }

            game.SetQuestionMatReady(false);

            if (!_armed)
                return;

            if (visibleCount == ArucoMarkerConfig.AnswerCount - 1)
            {
                if (_coverCandidate != missingIndex)
                {
                    _coverCandidate = missingIndex;
                    _coverStart = Time.time;
                    return;
                }

                if (Time.time - _coverStart >= holdSeconds)
                {
                    var answer = _coverCandidate;
                    ClearCandidate();
                    game.SubmitAnswer(answer);
                }
            }
            else
            {
                ClearCandidate();
            }
        }

        void UpdateStartReadiness()
        {
            var count = 0;
            for (var i = 0; i < ArucoMarkerConfig.AnswerCount; i++)
            {
                if (IsMarkerVisible(i))
                    count++;
            }

            VisibleMarkerCount = count;
            AllFourMarkersInView = count == ArucoMarkerConfig.AnswerCount;

            if (AllFourMarkersInView)
            {
                if (_allFourSince < 0f)
                    _allFourSince = Time.time;
            }
            else
            {
                _allFourSince = -1f;
            }

            IsReadyToStartGame = AllFourMarkersInView &&
                                 _allFourSince > 0f &&
                                 Time.time - _allFourSince >= startStableSeconds;
        }

        void TryMenuCoverAction(QuizScreen screen)
        {
            if (game == null || Time.time < _menuActionCooldownUntil)
                return;

            if (_menuUnlockScreen != screen)
            {
                _menuUnlockScreen = screen;
                _menuArmed = false;
                ClearMenuCover();
            }

            var visibleCount = 0;
            var missingIndex = -1;
            for (var i = 0; i < ArucoMarkerConfig.AnswerCount; i++)
            {
                if (IsMarkerVisible(i))
                    visibleCount++;
                else
                    missingIndex = i;
            }

            if (visibleCount < ArucoMarkerConfig.AnswerCount - 1)
            {
                _menuArmed = false;
                ClearMenuCover();
                return;
            }

            if (IsReadyToStartGame)
                _menuArmed = true;

            if (!_menuArmed)
                return;

            if (visibleCount == ArucoMarkerConfig.AnswerCount)
            {
                ClearMenuCover();
                return;
            }

            if (_menuCoverCandidate != missingIndex)
            {
                _menuCoverCandidate = missingIndex;
                _menuCoverStart = Time.time;
                return;
            }

            if (Time.time - _menuCoverStart < holdSeconds)
                return;

            ClearMenuCover();
            _menuArmed = false;
            _menuActionCooldownUntil = Time.time + 1.2f;

            // One subject per app now — covering any single marker just (re)starts the game,
            // on both Prepare and Result.
            game.StartGame();
        }

        void ResetMenuCoverState()
        {
            _menuArmed = false;
            ClearMenuCover();
        }

        void ClearMenuCover()
        {
            _menuCoverCandidate = -1;
            _menuCoverStart = -1f;
        }

        void LogDetectionStatus()
        {
            var sb = new StringBuilder("[ArucoQuiz] Detect: cam=");
            sb.Append(CameraFeedReady ? "OK" : "OFF");
            sb.Append(" mat=").Append(_cameraMatOk ? "OK" : "—");
            sb.Append(" visible=").Append(VisibleMarkerCount).Append("/4");
            sb.Append(" startReady=").Append(IsReadyToStartGame);
            sb.Append(" | IDs: ");
            if (_lastFrameDetectedIds.Count == 0)
                sb.Append("(không)");
            else
                sb.Append(string.Join(", ", _lastFrameDetectedIds));
            Debug.Log(sb.ToString());
        }

        void ClearCandidate()
        {
            _coverCandidate = -1;
            _coverStart = -1f;
        }

        void DetectMarkers()
        {
            if (_webCam == null || !_webCam.IsPlaying())
                return;

            var rgba = _webCam.GetMat();
            if (rgba == null || rgba.empty())
                return;

            _cameraMatOk = true;
            Imgproc.cvtColor(rgba, _gray, Imgproc.COLOR_RGBA2GRAY);
            _corners.Clear();
            _rejected.Clear();
            Aruco.detectMarkers(_gray, _dictionary, _corners, _ids, _detectorParams, _rejected);

            if (_ids == null || _ids.empty())
                return;

            var now = Time.time;
            for (var i = 0; i < _ids.total(); i++)
            {
                var idArr = new int[1];
                _ids.get(i, 0, idArr);
                var id = idArr[0];
                _lastFrameDetectedIds.Add(id);
                if (ArucoMarkerConfig.TryGetAnswerIndex(id, out var answerIndex))
                    _lastSeen[answerIndex] = now;
            }
        }
    }
}
