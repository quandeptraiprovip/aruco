using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Optional ScriptableObject to share clip assignments across scenes.
    /// Drag clips here, then assign this asset on QuizAudioController (future) or copy references in Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "QuizAudioClipCatalog", menuName = "Aruco Quiz/Audio Clip Catalog")]
    public class QuizAudioClipCatalog : ScriptableObject
    {
        [Header("Music")]
        public AudioClip musicPrepare;
        public AudioClip musicCountdown;
        public AudioClip musicGameplay;
        public AudioClip musicResult;

        [Header("UI")]
        public AudioClip sfxButtonClick;
        public AudioClip sfxButtonHover;
        public AudioClip sfxScreenWhoosh;

        [Header("Gameplay")]
        public AudioClip sfxArucoAllReady;
        public AudioClip sfxCountdownTick;
        public AudioClip sfxCountdownGo;
        public AudioClip sfxQuestionNew;
        public AudioClip sfxMatReady;
        public AudioClip sfxTimerTick;
        public AudioClip sfxTimerTickWarning;
        public AudioClip sfxTimerUrgentLoop;
        public AudioClip sfxCoverSelectStart;
        public AudioClip sfxCoverChargeMid;
        public AudioClip sfxCoverLockIn;
        public AudioClip sfxAnswerCorrect;
        public AudioClip sfxAnswerWrong;
        public AudioClip sfxTimeout;
        public AudioClip sfxResultFanfare;
        public AudioClip sfxStarPop;
    }
}
