using System;
using System.Collections;
using UnityEngine;

namespace ArucoQuiz
{
    public enum QuizScreen
    {
        Prepare,
        Countdown,
        Playing,
        Result
    }

    public class QuizGameController : MonoBehaviour
    {
        public const int SecondsPerQuestion = 15;

        [SerializeField] QuizQuestion[] questions = QuizQuestionBank.Default;
        [SerializeField] QuizQuestionRepository questionRepository;
        [SerializeField] OpenCvArucoAnswerDetector answerDetector;
        [SerializeField] QuizSubject subject = QuizSubject.Math;

        public event Action StateChanged;

        public QuizScreen Screen { get; private set; } = QuizScreen.Prepare;

        /// <summary>Fixed for the lifetime of the app — baked in by the scene builder. Each subject ships as its own scene.</summary>
        public QuizSubject Subject => subject;
        public int CountdownNum { get; private set; } = 3;
        public int QuestionIndex { get; private set; }
        public int CorrectCount { get; private set; }
        public int TimeLeft { get; private set; } = SecondsPerQuestion;
        public bool ShowFeedback { get; private set; }
        public bool FeedbackCorrect { get; private set; }
        public int? SelectedAnswer { get; private set; }

        public int TotalQuestions => questions != null ? questions.Length : 0;

        public QuizQuestion CurrentQuestion =>
            questions[Mathf.Clamp(QuestionIndex, 0, Mathf.Max(0, TotalQuestions - 1))];

        public bool IsTimeoutFeedback => ShowFeedback && !FeedbackCorrect && SelectedAnswer == null;

        /// <summary>All four ArUco markers visible for the current question (required before cover-to-answer).</summary>
        public bool MatReadyForQuestion { get; private set; }

        public bool CanStartGame =>
            answerDetector != null && answerDetector.IsReadyToStartGame;

        Coroutine _countdownRoutine;
        Coroutine _timerRoutine;
        Coroutine _autoNextRoutine;

        void Awake()
        {
            if (answerDetector == null)
                answerDetector = FindObjectOfType<OpenCvArucoAnswerDetector>();
        }

        void OnDisable()
        {
            StopAllRoutines();
        }

        public void StartGame()
        {
            StopAllRoutines();
            if (questionRepository != null)
                questions = questionRepository.PickSessionQuestions(Subject);
            Screen = QuizScreen.Countdown;
            CountdownNum = 0;
            Notify();
            _countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        public void SetQuestionMatReady(bool ready)
        {
            if (MatReadyForQuestion == ready)
                return;
            MatReadyForQuestion = ready;
            Notify();
        }

        IEnumerator CountdownRoutine()
        {
            while (answerDetector != null && !answerDetector.IsReadyToStartGame)
            {
                CountdownNum = 0;
                Notify();
                yield return new WaitForSeconds(0.1f);
            }

            var n = 3;
            while (n > 0)
            {
                while (answerDetector != null && !answerDetector.IsReadyToStartGame)
                {
                    CountdownNum = 0;
                    Notify();
                    yield return new WaitForSeconds(0.1f);
                }

                CountdownNum = n;
                Notify();
                yield return new WaitForSeconds(1f);
                n--;
            }

            while (answerDetector != null && !answerDetector.IsReadyToStartGame)
            {
                CountdownNum = 0;
                Notify();
                yield return new WaitForSeconds(0.1f);
            }

            BeginPlaying();
        }

        void BeginPlaying()
        {
            QuestionIndex = 0;
            CorrectCount = 0;
            TimeLeft = SecondsPerQuestion;
            SelectedAnswer = null;
            ShowFeedback = false;
            FeedbackCorrect = false;
            MatReadyForQuestion = false;
            Screen = QuizScreen.Playing;
            Notify();
            StartQuestionTimer();
        }

        void StartQuestionTimer()
        {
            if (_timerRoutine != null)
                StopCoroutine(_timerRoutine);
            _timerRoutine = StartCoroutine(TimerRoutine());
        }

        IEnumerator TimerRoutine()
        {
            while (!MatReadyForQuestion)
            {
                yield return new WaitForSeconds(0.1f);
                if (ShowFeedback)
                    yield break;
            }

            while (TimeLeft > 0)
            {
                yield return new WaitForSeconds(1f);
                if (ShowFeedback)
                    yield break;
                TimeLeft--;
                Notify();
            }

            // Hết giờ: nếu đang giữ che ArUco thì không tính timeout — chờ chọn xong hoặc buông tay.
            while (!ShowFeedback)
            {
                if (!IsCoverBlockingTimeout())
                {
                    OnTimeout();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        bool IsCoverBlockingTimeout()
        {
            return answerDetector != null && answerDetector.IsSelectingAnswer;
        }

        void OnTimeout()
        {
            if (ShowFeedback)
                return;
            ShowFeedback = true;
            FeedbackCorrect = false;
            Notify();
            ScheduleNextQuestion(1.7f);
        }

        public void SubmitAnswer(int answerIndex)
        {
            if (Screen != QuizScreen.Playing || ShowFeedback || SelectedAnswer != null)
                return;

            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }

            var correct = answerIndex == CurrentQuestion.correctIndex;
            SelectedAnswer = answerIndex;
            ShowFeedback = true;
            FeedbackCorrect = correct;
            if (correct)
                CorrectCount++;
            Notify();
            ScheduleNextQuestion(1.6f);
        }

        void ScheduleNextQuestion(float delay)
        {
            if (_autoNextRoutine != null)
                StopCoroutine(_autoNextRoutine);
            _autoNextRoutine = StartCoroutine(AutoNextRoutine(delay));
        }

        IEnumerator AutoNextRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            NextQuestion();
        }

        void NextQuestion()
        {
            var next = QuestionIndex + 1;
            if (next >= TotalQuestions)
            {
                Screen = QuizScreen.Result;
                Notify();
                return;
            }

            QuestionIndex = next;
            SelectedAnswer = null;
            ShowFeedback = false;
            FeedbackCorrect = false;
            MatReadyForQuestion = false;
            TimeLeft = SecondsPerQuestion;
            Notify();
            StartQuestionTimer();
        }

        void StopAllRoutines()
        {
            if (_countdownRoutine != null)
                StopCoroutine(_countdownRoutine);
            if (_timerRoutine != null)
                StopCoroutine(_timerRoutine);
            if (_autoNextRoutine != null)
                StopCoroutine(_autoNextRoutine);
            _countdownRoutine = _timerRoutine = _autoNextRoutine = null;
        }

        void Notify() => StateChanged?.Invoke();

        public int StarCount
        {
            get
            {
                if (CorrectCount >= 4) return 3;
                if (CorrectCount >= 3) return 2;
                return 1;
            }
        }

        public string ResultEmoji
        {
            get
            {
                if (CorrectCount >= 4) return "A+";
                if (CorrectCount >= 3) return "OK";
                return "...";
            }
        }

        public string ResultMessage
        {
            get
            {
                if (CorrectCount >= 4) return "Xuất sắc! Bạn rất giỏi!";
                if (CorrectCount >= 3) return "Giỏi lắm! Cố lên nhé!";
                return "Luyện tập thêm nhé!";
            }
        }
    }
}
