using UnityEngine;
using UnityEngine.EventSystems;

namespace ArucoQuiz
{
    public class PodiumUiClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] int answerIndex;
        [SerializeField] QuizGameController game;

        public void Configure(int index, QuizGameController controller)
        {
            answerIndex = index;
            game = controller;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (game != null)
                game.SubmitAnswer(answerIndex);
        }
    }
}
