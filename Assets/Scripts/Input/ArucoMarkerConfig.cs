namespace ArucoQuiz
{
    /// <summary>
    /// Physical mat uses DICT_4X4_50 markers with IDs 0–3 (answers A–D).
    /// </summary>
    public static class ArucoMarkerConfig
    {
        public const int FirstMarkerId = 0;
        public const int AnswerCount = 4;

        public static int MarkerIdForAnswer(int answerIndex) => FirstMarkerId + answerIndex;

        public static bool TryGetAnswerIndex(int arucoId, out int answerIndex)
        {
            answerIndex = arucoId - FirstMarkerId;
            return arucoId >= FirstMarkerId && arucoId < FirstMarkerId + AnswerCount;
        }

        public static string MarkerTextureFileName(int answerIndex) =>
            $"marker_{MarkerIdForAnswer(answerIndex)}.png";
    }
}
