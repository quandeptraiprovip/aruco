using TMPro;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Keeps 3D podium answer labels in sync with the current question.
    /// </summary>
    public class PodiumWorldLabelSync : MonoBehaviour
    {
        [SerializeField] QuizGameController game;
        [SerializeField] TextMeshPro[] labels;

        void OnEnable()
        {
            if (game != null)
                game.StateChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (game != null)
                game.StateChanged -= Refresh;
        }

        public void Configure(QuizGameController controller, TextMeshPro[] podiumLabels)
        {
            if (game != null)
                game.StateChanged -= Refresh;
            game = controller;
            labels = podiumLabels;
            if (isActiveAndEnabled && game != null)
                game.StateChanged += Refresh;
            Refresh();
        }

        void Refresh()
        {
            if (game == null || labels == null)
                return;
            var q = game.CurrentQuestion;
            for (var i = 0; i < labels.Length && i < q.answers.Length; i++)
            {
                if (labels[i] != null)
                    labels[i].text = q.answers[i];
            }
        }
    }
}
