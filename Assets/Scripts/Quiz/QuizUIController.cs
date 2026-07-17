using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    public class QuizUIController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] GameObject prepareScreen;
        [SerializeField] GameObject countdownScreen;
        [SerializeField] GameObject playingScreen;
        [SerializeField] GameObject resultScreen;

        [Header("Prepare")]
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text prepareArucoStatusText;
        [SerializeField] TMP_Text startButtonLabel;

        [Header("Countdown")]
        [SerializeField] TMP_Text countdownText;
        [SerializeField] UiCountdownPop countdownPop;
        [SerializeField] TMP_Text countdownArucoHintText;
        [SerializeField] TMP_Text countdownSubjectLabel;

        [Header("Playing HUD")]
        [SerializeField] TMP_Text questionNumText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text questionLabelText;
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] UiPulseScale timerPulse;
        [SerializeField] Color timerOrangeColor = new Color(1f, 0.722f, 0f);
        [SerializeField] Color timerRedColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] TMP_Text[] answerLabels;
        [SerializeField] TMP_Text recSubjectText;

        [Header("Feedback")]
        [SerializeField] GameObject feedbackCorrectPanel;
        [SerializeField] GameObject feedbackWrongPanel;
        [SerializeField] TMP_Text feedbackWrongTitle;
        [SerializeField] TMP_Text feedbackWrongSubtitle;
        [SerializeField] Image feedbackWrongIcon;
        [SerializeField] Sprite wrongCrossSprite;
        [SerializeField] Sprite timeoutClockSprite;

        [Header("Result")]
        [SerializeField] TMP_Text resultEmojiText;
        [SerializeField] TMP_Text resultScoreText;
        [SerializeField] TMP_Text resultMessageText;
        [SerializeField] GameObject[] resultStars;
        [SerializeField] Button playAgainButton;
        [SerializeField] TMP_Text resultArucoStatusText;
        [SerializeField] Image resultIconImage;
        [SerializeField] Sprite resultTrophySprite;
        [SerializeField] Sprite resultStarSprite;
        [SerializeField] Sprite resultBookSprite;
        [SerializeField] TMP_Text resultSubjectLabel;

        [Header("World")]
        [SerializeField] GameObject worldPlayingRoot;

        [SerializeField] OpenCvArucoAnswerDetector arucoDetector;
        [SerializeField] QuizThemeApplier themeApplier;

        QuizGameController _game;
        bool _bound;
        int _lastCountdownShown = -1;

        void Start()
        {
            EnsureResultArucoStatusLabel();
            if (_bound)
                return;
            var game = FindObjectOfType<QuizGameController>();
            if (game != null)
                Bind(game);
        }

        public void Bind(QuizGameController game)
        {
            if (game == null || _bound)
                return;
            _bound = true;
            _game = game;
            _game.StateChanged += Refresh;
            if (startButton != null)
                startButton.onClick.AddListener(_game.StartGame);
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(_game.StartGame);
            Refresh();
        }

        void EnsureResultArucoStatusLabel()
        {
            if (resultArucoStatusText != null || resultScreen == null)
                return;

            var existing = resultScreen.transform.Find("ResultArucoStatus");
            if (existing != null)
            {
                resultArucoStatusText = existing.GetComponent<TMP_Text>();
                return;
            }

            if (prepareArucoStatusText == null)
                return;

            var go = Instantiate(prepareArucoStatusText.gameObject, resultScreen.transform);
            go.name = "ResultArucoStatus";
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(0, -438);
            resultArucoStatusText = go.GetComponent<TMP_Text>();
            if (resultArucoStatusText != null)
                resultArucoStatusText.text = "Đặt đủ 4 thẻ ArUco (0–3) vào khung camera";
        }

        void Update()
        {
            RefreshPrepareArucoGate();
        }

        void RefreshPrepareArucoGate()
        {
            if (_game == null)
                return;

            if (arucoDetector == null)
                arucoDetector = FindObjectOfType<OpenCvArucoAnswerDetector>();

            var ready = _game.CanStartGame;
            var menuArmed = arucoDetector != null && arucoDetector.MenuMatArmed;

            if (_game.Screen == QuizScreen.Prepare && startButton != null)
                startButton.interactable = false;

            if (_game.Screen == QuizScreen.Result && playAgainButton != null)
                playAgainButton.interactable = false;

            if (prepareArucoStatusText != null)
            {
                if (_game.Screen == QuizScreen.Prepare)
                {
                    if (arucoDetector == null)
                        prepareArucoStatusText.text = "Đang khởi động camera…";
                    else if (menuArmed)
                    {
                        var p = arucoDetector.GetMenuCoverProgress();
                        prepareArucoStatusText.text = p > 0.01f
                            ? $"Giữ che thẻ… {(p * arucoDetector.HoldSeconds):0.0}s / {arucoDetector.HoldSeconds:0}s → BẮT ĐẦU!"
                            : "Che 1 thẻ bất kỳ, giữ 2 giây để bắt đầu!";
                    }
                    else
                        prepareArucoStatusText.text =
                            $"Đặt đủ 4 thẻ vào khung ({arucoDetector.VisibleMarkerCount}/4)";
                }
                else if (_game.Screen == QuizScreen.Countdown && _game.CountdownNum == 0)
                {
                    prepareArucoStatusText.text = arucoDetector != null && ready
                        ? "Sẵn sàng!"
                        : $"Chờ đủ 4 thẻ ArUco… ({arucoDetector?.VisibleMarkerCount ?? 0}/4)";
                }
            }

            if (_game.Screen == QuizScreen.Result)
            {
                if (resultArucoStatusText != null)
                {
                    if (arucoDetector == null)
                        resultArucoStatusText.text = "Đang khởi động camera…";
                    else if (menuArmed)
                    {
                        var p = arucoDetector.GetMenuCoverProgress();
                        resultArucoStatusText.text = p > 0.01f
                            ? $"Giữ che thẻ… {(p * arucoDetector.HoldSeconds):0.0}s / {arucoDetector.HoldSeconds:0}s → CHƠI LẠI!"
                            : "Che 1 thẻ bất kỳ, giữ 2 giây để chơi lại!";
                    }
                    else
                        resultArucoStatusText.text =
                            $"Đặt đủ 4 thẻ vào khung ({arucoDetector.VisibleMarkerCount}/4)";
                }

                var progress = arucoDetector != null ? arucoDetector.GetMenuCoverProgress() : 0f;
                SetButtonHighlight(playAgainButton, menuArmed, progress);
            }

            if (countdownArucoHintText != null && _game.Screen == QuizScreen.Countdown)
            {
                if (_game.CountdownNum == 0 && !ready)
                    countdownArucoHintText.text =
                        $"Đặt đủ 4 thẻ ArUco (0–3) vào khung ({arucoDetector?.VisibleMarkerCount ?? 0}/4)";
                else if (_game.CountdownNum == 0)
                    countdownArucoHintText.text = "SẴN SÀNG — CHUẨN BỊ!";
                else
                    countdownArucoHintText.text = "CÂU HỎI SẮP BẮT ĐẦU";
            }
        }

        static void SetButtonHighlight(Button button, bool isTarget, float progress)
        {
            if (button == null)
                return;
            var t = button.transform;
            var targetScale = isTarget ? 1f + 0.08f * progress : 1f;
            t.localScale = Vector3.Lerp(t.localScale, Vector3.one * targetScale, Time.deltaTime * 10f);
        }

        void OnDestroy()
        {
            if (_game != null)
                _game.StateChanged -= Refresh;
        }

        void Refresh()
        {
            if (_game == null || prepareScreen == null)
                return;

            prepareScreen.SetActive(_game.Screen == QuizScreen.Prepare);
            if (countdownScreen != null)
                countdownScreen.SetActive(_game.Screen == QuizScreen.Countdown);
            if (playingScreen != null)
                playingScreen.SetActive(_game.Screen == QuizScreen.Playing);
            if (resultScreen != null)
                resultScreen.SetActive(_game.Screen == QuizScreen.Result);
            if (worldPlayingRoot != null)
                worldPlayingRoot.SetActive(_game.Screen == QuizScreen.Playing);

            if (countdownText != null)
            {
                if (_game.Screen == QuizScreen.Countdown && _game.CountdownNum == 0)
                    countdownText.text = "…";
                else
                    countdownText.text = _game.CountdownNum.ToString();
            }
            if (_game.Screen == QuizScreen.Countdown && _game.CountdownNum != _lastCountdownShown)
            {
                _lastCountdownShown = _game.CountdownNum;
                if (countdownPop != null && countdownPop.isActiveAndEnabled)
                    countdownPop.Play();
            }
            else if (_game.Screen != QuizScreen.Countdown)
            {
                _lastCountdownShown = -1;
            }

            themeApplier?.ApplyTheme(_game.Subject);
            var theme = QuizTheme.Get(_game.Subject);
            if (startButtonLabel != null)
                startButtonLabel.text = $"BẮT ĐẦU MÔN {theme.ShortName}!";
            if (countdownSubjectLabel != null)
                countdownSubjectLabel.text = $"{theme.Name} · CHUẨN BỊ...";
            if (recSubjectText != null)
                recSubjectText.text = $"REC · {theme.Name}";
            if (resultSubjectLabel != null)
                resultSubjectLabel.text = theme.Name;

            if (_game.Screen != QuizScreen.Playing && _game.Screen != QuizScreen.Result)
            {
                if (feedbackCorrectPanel != null)
                    feedbackCorrectPanel.SetActive(false);
                if (feedbackWrongPanel != null)
                    feedbackWrongPanel.SetActive(false);
                return;
            }

            if (_game.TotalQuestions == 0)
                return;

            var q = _game.CurrentQuestion;

            if (questionNumText != null)
                questionNumText.text = $"{_game.QuestionIndex + 1}/{_game.TotalQuestions}";
            if (scoreText != null)
                scoreText.text = $"{_game.CorrectCount}/{_game.TotalQuestions}";
            if (questionLabelText != null)
                questionLabelText.text = $"· CÂU HỎI {_game.QuestionIndex + 1} ·";
            if (questionText != null)
                questionText.text = q.text ?? "";

            if (timerText != null)
            {
                if (arucoDetector == null)
                    arucoDetector = FindObjectOfType<OpenCvArucoAnswerDetector>();

                // "--:--" only until the mat is seen fully for the FIRST time this question —
                // not every time MatReadyForQuestion dips false, which also happens while
                // covering a marker to answer (only one of four visible then, by design).
                var everReadyThisQuestion = arucoDetector != null && arucoDetector.MatReady;

                if (_game.Screen == QuizScreen.Playing && !everReadyThisQuestion && !_game.ShowFeedback)
                {
                    // Not "CHỜ": this field stays on the mono font for stable digit width,
                    // and Share Tech Mono's fallback doesn't cover the ơ-horn vowel in "Chờ".
                    timerText.text = "--:--";
                    timerText.fontSize = 52;
                    timerText.color = theme.Accent;
                    if (timerPulse != null)
                        timerPulse.SetPulsing(false);
                }
                else
                {
                var t = _game.TimeLeft;
                var timeStr = $"0:{t:D2}";
                if (t > 10)
                {
                    timerText.fontSize = 70;
                    timerText.color = theme.Accent;
                }
                else if (t > 5)
                {
                    timerText.fontSize = 74;
                    timerText.color = timerOrangeColor;
                }
                else
                {
                    timerText.fontSize = 78;
                    timerText.color = timerRedColor;
                }

                timerText.text = timeStr;
                if (timerPulse != null)
                    timerPulse.SetPulsing(t <= 10);
                }
            }

            if (answerLabels != null)
            {
                for (var i = 0; i < answerLabels.Length && i < q.answers.Length; i++)
                {
                    if (answerLabels[i] != null)
                        answerLabels[i].text = q.answers[i] ?? "";
                }
            }

            if (feedbackCorrectPanel != null)
                feedbackCorrectPanel.SetActive(_game.ShowFeedback && _game.FeedbackCorrect);
            if (feedbackWrongPanel != null)
                feedbackWrongPanel.SetActive(_game.ShowFeedback && !_game.FeedbackCorrect);
            if (_game.ShowFeedback && !_game.FeedbackCorrect)
            {
                if (_game.IsTimeoutFeedback)
                {
                    if (feedbackWrongTitle != null)
                        feedbackWrongTitle.text = "HẾT GIỜ!";
                    if (feedbackWrongSubtitle != null)
                        feedbackWrongSubtitle.text = "LẦN SAU NHANH HƠN NHÉ!";
                    if (feedbackWrongIcon != null && timeoutClockSprite != null)
                        feedbackWrongIcon.sprite = timeoutClockSprite;
                }
                else
                {
                    if (feedbackWrongTitle != null)
                        feedbackWrongTitle.text = "SAI RỒI!";
                    if (feedbackWrongSubtitle != null)
                        feedbackWrongSubtitle.text = "CỐ LÊN NHÉ!";
                    if (feedbackWrongIcon != null && wrongCrossSprite != null)
                        feedbackWrongIcon.sprite = wrongCrossSprite;
                }
            }

            if (_game.Screen != QuizScreen.Result)
                return;

            if (resultEmojiText != null)
                resultEmojiText.text = _game.ResultEmoji;
            if (resultIconImage != null)
            {
                if (_game.CorrectCount >= 4)
                {
                    resultIconImage.sprite = resultTrophySprite;
                    resultIconImage.color = new Color(1f, 0.78f, 0.1f);
                }
                else if (_game.CorrectCount >= 3)
                {
                    resultIconImage.sprite = resultStarSprite;
                    resultIconImage.color = new Color(1f, 0.85f, 0.2f);
                }
                else
                {
                    resultIconImage.sprite = resultBookSprite;
                    resultIconImage.color = new Color(0f, 0.898f, 1f);
                }
            }
            if (resultScoreText != null)
                resultScoreText.text = $"{_game.CorrectCount}/{_game.TotalQuestions}";
            if (resultMessageText != null)
                resultMessageText.text = _game.ResultMessage;
            if (resultStars != null)
            {
                var starCount = _game.StarCount;
                for (var i = 0; i < resultStars.Length; i++)
                {
                    if (resultStars[i] != null)
                        resultStars[i].SetActive(i < starCount);
                }
            }
        }
    }
}
