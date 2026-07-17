using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Drives the four 3D answer cubes: answer texts per question, cover-charge
    /// effects fed by the ArUco detector, and correct/wrong feedback choreography.
    /// </summary>
    public class PodiumRow3DController : MonoBehaviour
    {
        [SerializeField] QuizGameController game;
        [SerializeField] OpenCvArucoAnswerDetector detector;
        [SerializeField] AnswerCube3D[] cubes;
        [SerializeField] Image[] chargeBars;
        [SerializeField] Image[] markerChips;
        [SerializeField] CameraFeedAnswerLights feedAnswerLights;
        [SerializeField] TMP_Text hintText;

        static readonly Color[] Accents =
        {
            new Color(1f, 0.88f, 0.25f),
            new Color(0f, 0.898f, 1f),
            new Color(0f, 1f, 0.533f),
            new Color(1f, 0.2f, 0.8f),
        };

        int _shownQuestion = -1;
        bool _feedbackShown;
        int _lastCoverSlot = -1;

        public void Configure(QuizGameController gameController, OpenCvArucoAnswerDetector arucoDetector,
            AnswerCube3D[] answerCubes, Image[] bars, Image[] chips, CameraFeedAnswerLights feedLights,
            TMP_Text hint)
        {
            game = gameController;
            detector = arucoDetector;
            cubes = answerCubes;
            chargeBars = bars;
            markerChips = chips;
            feedAnswerLights = feedLights;
            hintText = hint;
        }

        void OnEnable()
        {
            if (GetComponent<PodiumRenderStabilizer>() == null)
                gameObject.AddComponent<PodiumRenderStabilizer>();

            if (game != null)
            {
                game.StateChanged += Refresh;
                _shownQuestion = -1;
                _feedbackShown = false;
                Refresh();
            }
        }

        void OnDisable()
        {
            if (game != null)
                game.StateChanged -= Refresh;
        }

        void Refresh()
        {
            if (game == null || game.Screen != QuizScreen.Playing || game.TotalQuestions == 0)
                return;

            if (_shownQuestion != game.QuestionIndex)
            {
                _shownQuestion = game.QuestionIndex;
                _feedbackShown = false;
                _lastCoverSlot = -1;
                if (cubes != null && cubes.Length > 0)
                {
                    var q = game.CurrentQuestion;
                    for (var i = 0; i < cubes.Length; i++)
                    {
                        if (cubes[i] == null)
                            continue;
                        cubes[i].ResetState();
                        if (q.answers != null && i < q.answers.Length)
                            cubes[i].SetAnswer(q.answers[i]);
                    }
                }
            }

            if (game.ShowFeedback && !_feedbackShown)
            {
                _feedbackShown = true;
                PlayFeedback();
            }
        }

        void PlayFeedback()
        {
            if (cubes == null || cubes.Length == 0)
                return;

            var correctIndex = game.CurrentQuestion.correctIndex;
            var selected = game.SelectedAnswer ?? -1;

            for (var i = 0; i < cubes.Length; i++)
            {
                if (cubes[i] == null)
                    continue;
                if (i == correctIndex)
                    cubes[i].PlayCorrect();
                else if (i == selected)
                    cubes[i].PlayWrong();
                else
                    cubes[i].SetDim(true);
            }
        }

        void Update()
        {
            if (game == null || game.Screen != QuizScreen.Playing)
                return;

            var accepting = !game.ShowFeedback;
            var coverSlot = accepting && detector != null ? detector.CoverCandidate : -1;

            if (coverSlot != _lastCoverSlot)
            {
                if (coverSlot >= 0 && cubes != null && coverSlot < cubes.Length && cubes[coverSlot] != null)
                    cubes[coverSlot].BeginSelection(coverSlot);
                if (_lastCoverSlot >= 0 && _lastCoverSlot != coverSlot && cubes != null &&
                    _lastCoverSlot < cubes.Length && cubes[_lastCoverSlot] != null)
                {
                    cubes[_lastCoverSlot].SetCharge(0f);
                    cubes[_lastCoverSlot].SetDim(false);
                }
                _lastCoverSlot = coverSlot;
            }

            for (var i = 0; i < 4; i++)
            {
                var charge = accepting && detector != null && i == coverSlot ? detector.GetCoverProgress(i) : 0f;

                if (cubes != null && i < cubes.Length && cubes[i] != null && accepting)
                {
                    cubes[i].SetCharge(charge);
                    if (coverSlot >= 0 && i != coverSlot)
                        cubes[i].SetDim(true);
                    else if (coverSlot < 0)
                        cubes[i].SetDim(false);
                }

                if (chargeBars != null && i < chargeBars.Length && chargeBars[i] != null)
                    chargeBars[i].fillAmount = charge;

                if (markerChips != null && i < markerChips.Length && markerChips[i] != null)
                {
                    var visible = detector != null && detector.IsMarkerVisible(i);
                    var accent = cubes != null && i < cubes.Length && cubes[i] != null
                        ? cubes[i].AccentColor
                        : Accents[i];
                    var target = visible ? accent : new Color(1f, 1f, 1f, 0.16f);
                    markerChips[i].color = Color.Lerp(markerChips[i].color, target, Time.deltaTime * 8f);
                }

                if (feedAnswerLights != null)
                {
                    var visible = detector != null && detector.IsMarkerVisible(i);
                    feedAnswerLights.SetSlot(i, visible, charge);
                }
            }

            if (hintText != null)
            {
                if (!accepting)
                    hintText.text = "";
                else if (detector != null && !detector.MatReady)
                    hintText.text = $"Đưa đủ 4 thẻ vào khung ({detector.VisibleMarkerCount}/4)";
                else if (detector != null && detector.CoverCandidate >= 0)
                {
                    var letter = (char)('A' + detector.CoverCandidate);
                    var p = detector.GetCoverProgress(detector.CoverCandidate);
                    var hold = detector.HoldSeconds;
                    hintText.text = $"Giữ che thẻ {letter}… {(p * hold):0.0}s / {hold:0}s";
                }
                else
                    hintText.text = "Che thẻ ArUco của đáp án con chọn (giữ 2 giây)!";
            }
        }
    }
}
