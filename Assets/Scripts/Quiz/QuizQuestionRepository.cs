using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ArucoQuiz
{
    [Serializable]
    public class QuizOptionsJson
    {
        public string A;
        public string B;
        public string C;
        public string D;
    }

    [Serializable]
    public class QuizQuestionJson
    {
        public string id;
        public string question;
        public QuizOptionsJson options;
        public string correct_answer;
    }

    [Serializable]
    class QuizQuestionJsonList
    {
        public QuizQuestionJson[] items;
    }

    /// <summary>Loads per-subject question banks from StreamingAssets JSON (same schema for every subject).</summary>
    public class QuizQuestionRepository : MonoBehaviour
    {
        [SerializeField] string mathStreamingAssetsFileName = "Math.json";
        [SerializeField] string englishStreamingAssetsFileName = "ESL.json";
        [SerializeField] int questionsPerSession = 5;
        [SerializeField] bool shuffleEachSession = true;

        readonly Dictionary<QuizSubject, QuizQuestionJson[]> _banks = new Dictionary<QuizSubject, QuizQuestionJson[]>();
        System.Random _rng = new System.Random();

        public int QuestionsPerSession => questionsPerSession;

        void Awake()
        {
            LoadBank(QuizSubject.Math, mathStreamingAssetsFileName);
            LoadBank(QuizSubject.English, englishStreamingAssetsFileName);
        }

        void LoadBank(QuizSubject subject, string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[QuizQuestionRepository] Missing {path}, using built-in fallback for {subject}.");
                _banks[subject] = Array.Empty<QuizQuestionJson>();
                return;
            }

            var text = File.ReadAllText(path);
            var wrapped = "{\"items\":" + text + "}";
            var list = JsonUtility.FromJson<QuizQuestionJsonList>(wrapped);
            _banks[subject] = list?.items ?? Array.Empty<QuizQuestionJson>();
            Debug.Log($"[QuizQuestionRepository] Loaded {_banks[subject].Length} questions for {subject} from {path}.");
        }

        public QuizQuestion[] PickSessionQuestions(QuizSubject subject)
        {
            if (!_banks.TryGetValue(subject, out var all) || all == null || all.Length == 0)
                return QuizQuestionBank.Default;

            var pool = all.Where(q => q != null && q.options != null).ToList();
            if (pool.Count == 0)
                return QuizQuestionBank.Default;

            if (shuffleEachSession)
                pool = pool.OrderBy(_ => _rng.Next()).ToList();

            var take = Mathf.Min(questionsPerSession, pool.Count);
            var result = new QuizQuestion[take];
            for (var i = 0; i < take; i++)
                result[i] = ToQuizQuestion(pool[i]);
            return result;
        }

        static QuizQuestion ToQuizQuestion(QuizQuestionJson src)
        {
            var answers = new[]
            {
                src.options.A ?? "",
                src.options.B ?? "",
                src.options.C ?? "",
                src.options.D ?? "",
            };
            var correct = LetterToIndex(src.correct_answer);
            return new QuizQuestion(src.id, src.question, answers, correct);
        }

        static int LetterToIndex(string letter)
        {
            if (string.IsNullOrEmpty(letter))
                return 0;
            switch (char.ToUpperInvariant(letter[0]))
            {
                case 'A': return 0;
                case 'B': return 1;
                case 'C': return 2;
                case 'D': return 3;
                default: return 0;
            }
        }
    }
}
