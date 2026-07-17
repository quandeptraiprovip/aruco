using System.Collections;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Quiz audio with a calm default mix: mostly background music, few clear one-shot cues.
    /// </summary>
    public class QuizAudioController : MonoBehaviour
    {
        enum SfxPriority
        {
            Low,
            Normal,
            Important,
        }

        [Header("References")]
        [SerializeField] QuizGameController game;
        [SerializeField] OpenCvArucoAnswerDetector detector;

        [Header("Audio sources (auto-created if empty)")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        [Header("Mix")]
        [SerializeField] bool minimalAudioMix = true;
        [SerializeField] [Range(0f, 1f)] float musicVolume = 0.5f;
        [SerializeField] [Range(0f, 1f)] float sfxVolume = 0.72f;
        [SerializeField] float musicCrossfadeSeconds = 1.25f;
        [SerializeField] float minGapBetweenSfx = 0.28f;
        [SerializeField] float musicDuckScale = 0.38f;
        [SerializeField] float musicDuckSeconds = 0.45f;

        [Header("Music — nhạc nền")]
        [SerializeField] AudioClip musicPrepare;
        [SerializeField] AudioClip musicCountdown;
        [SerializeField] AudioClip musicGameplay;
        [SerializeField] AudioClip musicResult;

        [Header("UI")]
        [SerializeField] AudioClip sfxButtonClick;
        [SerializeField] AudioClip sfxButtonHover;

        [Header("Countdown")]
        [SerializeField] AudioClip sfxCountdownTick;
        [SerializeField] AudioClip sfxCountdownGo;

        [Header("Gameplay (optional if minimal mix off)")]
        [SerializeField] AudioClip sfxArucoAllReady;
        [SerializeField] AudioClip sfxQuestionNew;
        [SerializeField] AudioClip sfxMatReady;
        [SerializeField] AudioClip sfxTimerTickWarning;
        [SerializeField] AudioClip sfxCoverSelectStart;

        [Header("Kết quả câu")]
        [SerializeField] AudioClip sfxAnswerCorrect;
        [SerializeField] AudioClip sfxAnswerWrong;
        [SerializeField] AudioClip sfxTimeout;

        [Header("Màn kết quả")]
        [SerializeField] AudioClip sfxResultFanfare;
        [SerializeField] AudioClip sfxStarPop;

        // Legacy slots — kept so existing scenes don't lose serialized refs
        [SerializeField] AudioClip sfxScreenWhoosh;
        [SerializeField] AudioClip sfxCountdownWaitPulse;
        [SerializeField] AudioClip sfxTimerTick;
        [SerializeField] AudioClip sfxTimerUrgentLoop;
        [SerializeField] AudioClip sfxCoverChargeMid;
        [SerializeField] AudioClip sfxCoverLockIn;
        [SerializeField] AudioClip sfxPodiumCorrect;
        [SerializeField] AudioClip sfxPodiumWrong;
        [SerializeField] AudioClip sfxCountdownPopAnim;
        [SerializeField] AudioClip sfxFeedbackPanelIn;

        QuizScreen _screen = (QuizScreen)(-1);
        int _countdown = -1;
        int _timeLeft = -1;
        int _questionIndex = -1;
        bool _matReady;
        bool _showFeedback;
        bool _canStart;
        int _coverSlot = -1;

        Coroutine _musicFadeRoutine;
        Coroutine _duckRoutine;
        Coroutine _syncRoutine;
        bool _bound;

        float _lastSfxTime = -999f;
        float _targetMusicVol;

        public static QuizAudioController Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureListener();
            EnsureSources();
            _targetMusicVol = musicVolume;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Unbind();
        }

        void Start()
        {
            if (game == null)
                game = FindObjectOfType<QuizGameController>();
            if (detector == null)
                detector = FindObjectOfType<OpenCvArucoAnswerDetector>();
            Bind();
            ApplyScreenMusic(game != null ? game.Screen : QuizScreen.Prepare, true);
        }

        public void Bind(QuizGameController gameController, OpenCvArucoAnswerDetector arucoDetector = null)
        {
            Unbind();
            game = gameController;
            if (arucoDetector != null)
                detector = arucoDetector;
            Bind();
            RequestSync(true);
        }

        void Bind()
        {
            if (game == null || _bound)
                return;
            _bound = true;
            game.StateChanged += OnGameStateChanged;
        }

        void Unbind()
        {
            if (!_bound || game == null)
                return;
            game.StateChanged -= OnGameStateChanged;
            _bound = false;
        }

        void OnGameStateChanged() => RequestSync(false);

        void RequestSync(bool forceMusic)
        {
            if (_syncRoutine != null)
                return;
            _syncRoutine = StartCoroutine(SyncNextFrame(forceMusic));
        }

        IEnumerator SyncNextFrame(bool forceMusic)
        {
            yield return null;
            _syncRoutine = null;
            if (game == null)
                yield break;
            SyncState(forceMusic);
        }

        void SyncState(bool forceMusic)
        {
            if (forceMusic || _screen != game.Screen)
            {
                OnScreenChanged(_screen, game.Screen);
                _screen = game.Screen;
            }

            if (_countdown != game.CountdownNum)
            {
                OnCountdownChanged(_countdown, game.CountdownNum);
                _countdown = game.CountdownNum;
            }

            if (_questionIndex != game.QuestionIndex)
            {
                OnQuestionIndexChanged(_questionIndex, game.QuestionIndex);
                _questionIndex = game.QuestionIndex;
            }

            if (_matReady != game.MatReadyForQuestion)
            {
                OnMatReadyChanged(_matReady, game.MatReadyForQuestion);
                _matReady = game.MatReadyForQuestion;
            }

            if (_timeLeft != game.TimeLeft)
            {
                OnTimeLeftChanged(_timeLeft, game.TimeLeft);
                _timeLeft = game.TimeLeft;
            }

            if (_showFeedback != game.ShowFeedback)
            {
                OnFeedbackChanged(_showFeedback, game.ShowFeedback);
                _showFeedback = game.ShowFeedback;
            }

            if (!minimalAudioMix)
                PollCoverSelectionLegacy();
            else if (_coverSlot >= 0)
                _coverSlot = -1;
        }

        void OnScreenChanged(QuizScreen from, QuizScreen to)
        {
            switch (to)
            {
                case QuizScreen.Prepare:
                    ApplyScreenMusic(QuizScreen.Prepare, from == to);
                    _canStart = game.CanStartGame;
                    break;
                case QuizScreen.Countdown:
                    ApplyScreenMusic(QuizScreen.Countdown, from == to);
                    break;
                case QuizScreen.Playing:
                    ApplyScreenMusic(QuizScreen.Playing, from == to);
                    if (!minimalAudioMix && from == QuizScreen.Countdown)
                        PlaySfx(sfxCountdownGo, SfxPriority.Normal, 0.85f);
                    break;
                case QuizScreen.Result:
                    ApplyScreenMusic(QuizScreen.Result, from == to);
                    if (minimalAudioMix)
                        PlaySfx(sfxResultFanfare, SfxPriority.Important, 0.88f);
                    else
                    {
                        PlaySfx(sfxResultFanfare, SfxPriority.Important, 0.9f);
                        StartCoroutine(StarPopsDelayed());
                    }
                    break;
            }
        }

        void ApplyScreenMusic(QuizScreen screen, bool sameScreen)
        {
            AudioClip clip = screen switch
            {
                QuizScreen.Prepare => musicPrepare,
                QuizScreen.Countdown => musicPrepare != null ? musicPrepare : musicCountdown,
                QuizScreen.Playing => musicGameplay,
                QuizScreen.Result => musicResult,
                _ => musicPrepare
            };
            if (clip == null)
                return;
            if (sameScreen && musicSource != null && musicSource.clip == clip && musicSource.isPlaying)
                return;
            PlayMusic(clip);
        }

        void OnCountdownChanged(int prev, int next)
        {
            if (game.Screen != QuizScreen.Countdown || next <= 0 || next == prev)
                return;
            PlaySfx(sfxCountdownTick, SfxPriority.Normal, 0.7f);
        }

        void OnQuestionIndexChanged(int prev, int next)
        {
            if (game.Screen != QuizScreen.Playing || next <= prev)
                return;
            _coverSlot = -1;
            if (minimalAudioMix || prev < 0)
                return;
            PlaySfx(sfxQuestionNew, SfxPriority.Low, 0.65f);
        }

        void OnMatReadyChanged(bool was, bool now)
        {
            if (minimalAudioMix || game.Screen != QuizScreen.Playing || was || !now)
                return;
            PlaySfx(sfxMatReady, SfxPriority.Low, 0.6f);
        }

        void OnTimeLeftChanged(int prev, int next)
        {
            if (game.Screen != QuizScreen.Playing || !game.MatReadyForQuestion || game.ShowFeedback)
                return;
            if (prev <= 0 || next >= prev)
                return;

            if (minimalAudioMix)
            {
                // Chỉ 3 tiếng nhắc: 3s, 2s, 1s — không tick mỗi giây
                if (next == 3 || next == 2 || next == 1)
                    PlaySfx(sfxTimerTickWarning, SfxPriority.Normal, 0.5f);
                return;
            }

            if (next <= 10)
                PlaySfx(sfxTimerTickWarning, SfxPriority.Low, 0.45f);
        }

        void OnFeedbackChanged(bool was, bool now)
        {
            if (!now || was)
                return;

            if (game.FeedbackCorrect)
                PlaySfx(sfxAnswerCorrect, SfxPriority.Important, 0.92f);
            else if (game.IsTimeoutFeedback)
                PlaySfx(sfxTimeout, SfxPriority.Important, 0.85f);
            else
                PlaySfx(sfxAnswerWrong, SfxPriority.Important, 0.85f);

            _coverSlot = -1;
        }

        void PollCoverSelectionLegacy()
        {
            if (game.Screen != QuizScreen.Playing || game.ShowFeedback || detector == null)
            {
                _coverSlot = -1;
                return;
            }

            if (!detector.MatReady)
            {
                _coverSlot = -1;
                return;
            }

            var slot = detector.CoverCandidate;
            if (slot != _coverSlot && slot >= 0)
                PlaySfx(sfxCoverSelectStart, SfxPriority.Low, 0.5f);
            _coverSlot = slot;
        }

        void Update()
        {
            if (minimalAudioMix || game == null || game.Screen != QuizScreen.Prepare || detector == null)
                return;

            var ready = game.CanStartGame;
            if (ready && !_canStart)
                PlaySfx(sfxArucoAllReady, SfxPriority.Normal, 0.7f);
            _canStart = ready;
        }

        IEnumerator StarPopsDelayed()
        {
            var count = game != null ? game.StarCount : 0;
            for (var i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(0.35f + i * 0.25f);
                PlaySfx(sfxStarPop, SfxPriority.Low, 0.45f);
            }
        }

        public void PlayButtonClick() => PlaySfx(sfxButtonClick, SfxPriority.Normal, 0.75f);

        public void PlayButtonHover()
        {
            if (minimalAudioMix)
                return;
            PlaySfx(sfxButtonHover, SfxPriority.Low, 0.35f);
        }

        public void PlayUiPop() { }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f) =>
            PlaySfx(clip, SfxPriority.Normal, volumeScale);

        void PlaySfx(AudioClip clip, SfxPriority priority, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null)
                return;

            var now = Time.unscaledTime;
            var gap = now - _lastSfxTime;
            if (priority == SfxPriority.Low && gap < minGapBetweenSfx * 1.5f)
                return;
            if (priority == SfxPriority.Normal && gap < minGapBetweenSfx)
                return;

            if (priority == SfxPriority.Important)
                DuckMusicBriefly();

            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
            _lastSfxTime = now;
        }

        void DuckMusicBriefly()
        {
            if (musicSource == null || !musicSource.isPlaying)
                return;
            if (_duckRoutine != null)
                StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(DuckMusicRoutine());
        }

        IEnumerator DuckMusicRoutine()
        {
            var from = musicSource.volume;
            var duck = _targetMusicVol * musicDuckScale;
            musicSource.volume = duck;
            yield return new WaitForSecondsRealtime(musicDuckSeconds);
            if (musicSource != null)
                musicSource.volume = _targetMusicVol;
            _duckRoutine = null;
        }

        void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            if (_musicFadeRoutine != null)
                StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(CrossfadeMusic(clip));
        }

        IEnumerator CrossfadeMusic(AudioClip next)
        {
            var fade = Mathf.Max(0.05f, musicCrossfadeSeconds);
            if (musicSource.isPlaying && musicSource.volume > 0.02f)
            {
                var fromVol = musicSource.volume;
                for (var t = 0f; t < fade; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(fromVol, 0f, t / fade);
                    yield return null;
                }
            }

            musicSource.clip = next;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();
            _targetMusicVol = musicVolume;

            for (var t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fade);
                yield return null;
            }

            musicSource.volume = musicVolume;
            _musicFadeRoutine = null;
        }

        void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.priority = 0;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.priority = 128;
            }

            ConfigureSource2D(musicSource);
            ConfigureSource2D(sfxSource);
            musicSource.volume = musicVolume;
        }

        static void EnsureListener()
        {
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners != null && listeners.Length > 0)
                return;

            var cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam != null && cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }

        static void ConfigureSource2D(AudioSource source)
        {
            if (source == null)
                return;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.reverbZoneMix = 0f;
        }
    }
}
