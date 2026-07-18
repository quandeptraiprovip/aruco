using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Plays quiz UI sounds on button click / hover. Wire QuizAudioController or leave empty to use Instance.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UiQuizButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] QuizAudioController audioController;
        [SerializeField] bool playClick = true;
        [SerializeField] bool playHover = true;

        Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (audioController == null)
                audioController = QuizAudioController.Instance;

            if (_button != null && playClick)
                _button.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        void OnClick()
        {
            ResolveAudio()?.PlayButtonClick();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!playHover || _button == null || !_button.interactable)
                return;
            ResolveAudio()?.PlayButtonHover();
        }

        QuizAudioController ResolveAudio()
        {
            if (audioController != null)
                return audioController;
            return QuizAudioController.Instance;
        }
    }
}
