using System;
using UnityEngine;

namespace ArucoQuiz
{
    [Serializable]
    public struct QuizQuestion
    {
        public string id;
        public string text;
        public string[] answers;
        public int correctIndex;

        public QuizQuestion(string text, string[] answers, int correctIndex)
            : this(null, text, answers, correctIndex)
        {
        }

        public QuizQuestion(string id, string text, string[] answers, int correctIndex)
        {
            this.id = id;
            this.text = text;
            this.answers = answers;
            this.correctIndex = correctIndex;
        }
    }

    public static class QuizQuestionBank
    {
        public static readonly QuizQuestion[] Default = new[]
        {
            new QuizQuestion("Tính: 0,6 + 0,25 = ?", new[] { "0,9", "0,85", "0,75", "0,95" }, 0),
            new QuizQuestion("Tính: 3 × 4 = ?", new[] { "10", "12", "14", "16" }, 1),
            new QuizQuestion("Tính: 15 − 7 = ?", new[] { "6", "9", "8", "7" }, 2),
            new QuizQuestion("24 ÷ 6 = ?", new[] { "3", "5", "4", "6" }, 2),
            new QuizQuestion("Tính: 0,5 + 0,3 = ?", new[] { "0,7", "0,9", "0,8", "0,6" }, 2),
        };
    }
}
