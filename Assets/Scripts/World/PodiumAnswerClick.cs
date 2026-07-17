using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Click a 3D podium in the Scene/Game view to submit an answer (testing without markers).
    /// </summary>
    public class PodiumAnswerClick : MonoBehaviour
    {
        [SerializeField] int answerIndex;
        [SerializeField] QuizGameController game;

        public void Configure(int index, QuizGameController controller)
        {
            answerIndex = index;
            game = controller;
        }

        void OnMouseDown()
        {
            if (game != null)
                game.SubmitAnswer(answerIndex);
        }
    }
}
