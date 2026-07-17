using System.Collections.Generic;
using System.IO;
using ArucoQuiz;
using OpenCVForUnity.UnityUtils.Helper;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Rebuilds one subject-locked scene (Assets/Scenes/ArucoQuizMath.unity or ArucoQuizEnglish.unity)
/// per the "Game chọn đáp án Aruco" HTML design: prepare → countdown → playing (camera feed behind,
/// 3D answer cubes, cover-the-marker gameplay) → result, with all ambient animations.
/// </summary>
public static class ArucoQuizSceneBuilder
{
    const string MathScenePath = "Assets/Scenes/ArucoQuizMath.unity";
    const string EnglishScenePath = "Assets/Scenes/ArucoQuizEnglish.unity";
    const string MarkerFolder = "Assets/Textures/ArucoMarkers";

    // Palette (matches HTML). Cyan/Purple below are fixed decorative/semantic colors used outside
    // any theme (achievement stars, operator-glyph tinting, etc.) — never recolored per subject.
    static readonly Color BgDeep = new Color(0.016f, 0f, 0.07f, 1f);
    static readonly Color Cyan = new Color(0f, 0.898f, 1f);
    static readonly Color Yellow = new Color(1f, 0.88f, 0.25f);
    static readonly Color Orange = new Color(1f, 0.573f, 0f);
    static readonly Color Green = new Color(0f, 1f, 0.533f);
    static readonly Color Purple = new Color(0.533f, 0.2f, 1f);
    static readonly Color Magenta = new Color(1f, 0.2f, 0.8f);
    static readonly Color RedWrong = new Color(1f, 0.27f, 0.3f);

    // The active build's subject palette (Assets/Scripts/Quiz/QuizTheme.cs), baked once at the top
    // of BuildInternal — each scene is locked to one subject for life, so this never changes again.
    static Color Accent;
    static Color Accent2;
    static Color ThemeBackground;

    // Decorative elements registered here get recolored to Accent/Accent2/ThemeBackground.
    // Semantic colors (correct/wrong, timer urgency, achievement gradients) and the
    // fixed 3D answer-cube palette are set directly with the constants above and never registered.
    static List<(Graphic graphic, float alpha, bool accent2)> _themedGraphics;
    static List<Image> _themedBackgrounds;
    static List<TMP_Text> _themedGradientTexts;

    static void Themed(Graphic g, float alpha, bool accent2 = false)
    {
        _themedGraphics.Add((g, alpha, accent2));
        var c = accent2 ? Accent2 : Accent;
        g.color = new Color(c.r, c.g, c.b, alpha);
    }

    static void ThemedBg(Image img)
    {
        _themedBackgrounds.Add(img);
        img.color = new Color(ThemeBackground.r, ThemeBackground.g, ThemeBackground.b, img.color.a);
    }

    static void ThemedGradient(TMP_Text tmp)
    {
        _themedGradientTexts.Add(tmp);
        SetGradient(tmp, Accent, Accent2);
    }

    [MenuItem("Aruco Quiz/Build Math Scene", false, 10)]
    public static void BuildMathMenu()
    {
        BuildInternal(QuizSubject.Math);
        EditorUtility.DisplayDialog("Aruco Quiz", $"Scene saved to {MathScenePath}", "OK");
    }

    [MenuItem("Aruco Quiz/Build English Scene", false, 11)]
    public static void BuildEnglishMenu()
    {
        BuildInternal(QuizSubject.English);
        EditorUtility.DisplayDialog("Aruco Quiz", $"Scene saved to {EnglishScenePath}", "OK");
    }

    public static void BuildMathFromCommandLine()
    {
        BuildInternal(QuizSubject.Math);
    }

    public static void BuildEnglishFromCommandLine()
    {
        BuildInternal(QuizSubject.English);
    }

    static void BuildInternal(QuizSubject subject)
    {
        var theme = QuizTheme.Get(subject);
        Accent = theme.Accent;
        Accent2 = theme.Accent2;
        ThemeBackground = theme.Background;
        var scenePath = subject == QuizSubject.Math ? MathScenePath : EnglishScenePath;

        ArucoQuizFontAssets.EnsureFonts();

        EnsureMarkerImportSettings();
        if (!System.IO.File.Exists($"{MarkerFolder}/marker_0.png"))
            ArucoQuizMarkerGenerator.Generate();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        _themedGraphics = new List<(Graphic, float, bool)>();
        _themedBackgrounds = new List<Image>();
        _themedGradientTexts = new List<TMP_Text>();

        // ── Camera + AR video background ──
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Depth;
        cam.backgroundColor = new Color(0.01f, 0f, 0.05f, 1f);
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 50f;
        camGo.tag = "MainCamera";
        if (camGo.GetComponent<AudioListener>() == null)
            camGo.AddComponent<AudioListener>();

        var videoBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        videoBg.name = "VideoBackground";
        videoBg.transform.SetParent(camGo.transform, false);
        Object.DestroyImmediate(videoBg.GetComponent<Collider>());
        const string bgMatPath = "Assets/Materials/Generated/video_background.mat";
        var bgMat = AssetDatabase.LoadAssetAtPath<Material>(bgMatPath);
        if (bgMat == null)
        {
            Directory.CreateDirectory("Assets/Materials/Generated");
            bgMat = new Material(Shader.Find("Unlit/Texture"));
            AssetDatabase.CreateAsset(bgMat, bgMatPath);
            bgMat = AssetDatabase.LoadAssetAtPath<Material>(bgMatPath);
        }
        videoBg.GetComponent<Renderer>().sharedMaterial = bgMat;

        var arSystem = new GameObject("ARSystem");
        var webCamHelper = arSystem.AddComponent<WebCamTextureToMatHelper>();
        webCamHelper.requestedWidth = 640;
        webCamHelper.requestedHeight = 480;
        webCamHelper.requestedFPS = 15;
        webCamHelper.timeoutFrameCount = 900;
        arSystem.AddComponent<ArQuizOpenCvBootstrap>();
        var arBackground = arSystem.AddComponent<ArCameraVideoBackground>();
        SetPrivate(arBackground, "targetRenderer", videoBg.GetComponent<Renderer>());
        SetPrivate(arBackground, "planeDistance", 8f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.85f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        // ── World-space UI canvas pinned to the camera ──
        var canvasGo = new GameObject("WorldUI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var follow = canvasGo.AddComponent<WorldCanvasFollowCamera>();
        SetPrivate(follow, "targetCamera", cam);
        SetPrivate(follow, "distance", PodiumAnswerLayout.CanvasPlaneDistance);
        var fontBootstrap = canvasGo.AddComponent<QuizUIFontBootstrap>();
        SetPrivate(fontBootstrap, "balooFont", ArucoQuizFontAssets.LoadBaloo());
        SetPrivate(fontBootstrap, "monoFont", ArucoQuizFontAssets.LoadMono());
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);

        // ── Game logic ──
        var gameGo = new GameObject("QuizGame");
        var repo = gameGo.AddComponent<QuizQuestionRepository>();
        var game = gameGo.AddComponent<QuizGameController>();
        SetPrivate(game, "questionRepository", repo);
        SetPrivate(game, "subject", subject);

        var detector = arSystem.AddComponent<OpenCvArucoAnswerDetector>();
        SetPrivate(detector, "game", game);
        SetPrivate(detector, "holdSeconds", 2f);
        SetPrivate(detector, "logDetectionToConsole", true);
        SetPrivate(game, "answerDetector", detector);

        // ── Screens ──
        var prepare = CreatePrepareScreen(canvasGo.transform, game, subject, cam);
        var countdown = CreateCountdownScreen(canvasGo.transform);
        var playing = CreatePlayingScreen(canvasGo.transform, game, cam, subject, out var podiumRow);
        var result = CreateResultScreen(canvasGo.transform, out var stars);

        SetPrivate(arBackground, "uiFeedImage", playingCameraFeedRef);

        var rowController = podiumRow.GetComponent<PodiumRow3DController>();
        if (rowController == null)
            rowController = podiumRow.AddComponent<PodiumRow3DController>();
        rowController.Configure(game, detector, ArucoQuizPodium3DBuilder.GetCubes(podiumRow),
            null, null, feedAnswerLightsRef, hintTextRef);
        EditorUtility.SetDirty(rowController);

        var audioGo = new GameObject("QuizAudio");
        var quizAudio = audioGo.AddComponent<QuizAudioController>();
        SetPrivate(quizAudio, "game", game);
        SetPrivate(quizAudio, "detector", detector);
        AttachQuizButtonSound(prepareButton, quizAudio);
        AttachQuizButtonSound(playAgainBtn, quizAudio);
        ArucoQuizAudioInstaller.EnsureAndWire(quizAudio);

        // ── UI controller wiring ──
        var uiGo = new GameObject("QuizUI");
        uiGo.transform.SetParent(canvasGo.transform, false);
        var ui = uiGo.AddComponent<QuizUIController>();

        SetPrivate(ui, "prepareScreen", prepare);
        SetPrivate(ui, "countdownScreen", countdown);
        SetPrivate(ui, "playingScreen", playing);
        SetPrivate(ui, "resultScreen", result);
        SetPrivate(ui, "worldPlayingRoot", null);
        WireUi(ui, stars, detector);

        var themeApplier = uiGo.AddComponent<QuizThemeApplier>();
        themeApplier.Configure(_themedGraphics, _themedBackgrounds, _themedGradientTexts);
        SetPrivate(ui, "themeApplier", themeApplier);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuildSettings(scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void WireUi(QuizUIController ui, GameObject[] stars, OpenCvArucoAnswerDetector detector)
    {
        SetPrivate(ui, "startButton", prepareButton);
        SetPrivate(ui, "countdownText", countdownLabel);
        SetPrivate(ui, "countdownPop", countdownPopRef);
        SetPrivate(ui, "questionNumText", questionNumLabel);
        SetPrivate(ui, "scoreText", scoreLabel);
        SetPrivate(ui, "questionLabelText", questionHeaderLabel);
        SetPrivate(ui, "questionText", questionBodyLabel);
        SetPrivate(ui, "timerText", timerLabel);
        SetPrivate(ui, "timerPulse", timerPulseScale);
        SetPrivate(ui, "answerLabels", null);
        SetPrivate(ui, "feedbackCorrectPanel", feedbackOk);
        SetPrivate(ui, "feedbackWrongPanel", feedbackBad);
        SetPrivate(ui, "feedbackWrongTitle", feedbackBadTitle);
        SetPrivate(ui, "feedbackWrongSubtitle", feedbackBadSub);
        SetPrivate(ui, "feedbackWrongIcon", feedbackWrongIconRef);
        SetPrivate(ui, "wrongCrossSprite", ArucoQuizSpriteAssets.Cross());
        SetPrivate(ui, "timeoutClockSprite", ArucoQuizSpriteAssets.Clock());
        SetPrivate(ui, "resultScoreText", resultScore);
        SetPrivate(ui, "resultMessageText", resultMsg);
        SetPrivate(ui, "resultStars", stars);
        SetPrivate(ui, "playAgainButton", playAgainBtn);
        SetPrivate(ui, "resultArucoStatusText", resultArucoStatusRef);
        SetPrivate(ui, "resultIconImage", resultIconRef);
        SetPrivate(ui, "resultTrophySprite", ArucoQuizSpriteAssets.Trophy());
        SetPrivate(ui, "resultStarSprite", ArucoQuizSpriteAssets.Star());
        SetPrivate(ui, "resultBookSprite", ArucoQuizSpriteAssets.Book());
        SetPrivate(ui, "prepareArucoStatusText", prepareArucoStatusRef);
        SetPrivate(ui, "countdownArucoHintText", countdownArucoHintRef);
        SetPrivate(ui, "arucoDetector", detector);

        SetPrivate(ui, "startButtonLabel", startButtonLabelRef);
        SetPrivate(ui, "countdownSubjectLabel", countdownSubjectLabelRef);
        SetPrivate(ui, "recSubjectText", recSubjectTextRef);
        SetPrivate(ui, "resultSubjectLabel", resultSubjectLabelRef);
    }

    // Captured while building screens
    static Button prepareButton;
    static TMP_Text prepareArucoStatusRef;
    static TMP_Text countdownArucoHintRef;
    static TMP_Text countdownLabel;
    static UiCountdownPop countdownPopRef;
    static TMP_Text questionNumLabel;
    static TMP_Text scoreLabel;
    static TMP_Text questionHeaderLabel;
    static TMP_Text questionBodyLabel;
    static TMP_Text timerLabel;
    static UiPulseScale timerPulseScale;
    static GameObject feedbackOk;
    static GameObject feedbackBad;
    static TMP_Text feedbackBadTitle;
    static TMP_Text feedbackBadSub;
    static Image feedbackWrongIconRef;
    static TMP_Text resultScore;
    static TMP_Text resultMsg;
    static Button playAgainBtn;
    static TMP_Text resultArucoStatusRef;
    static Image resultIconRef;
    static TMP_Text hintTextRef;
    static RawImage playingCameraFeedRef;
    static CameraFeedAnswerLights feedAnswerLightsRef;

    // Subject theme labels
    static TMP_Text startButtonLabelRef;
    static TMP_Text countdownSubjectLabelRef;
    static TMP_Text recSubjectTextRef;
    static TMP_Text resultSubjectLabelRef;

    // Playing layout (1920×1080 design — matches HTML podium height 370px)
    const float PodiumBarHeight = PodiumAnswerLayout.PodiumBarHeight;
    const float FeedInsetLeft = PodiumAnswerLayout.FeedInsetLeft;
    const float FeedInsetRight = PodiumAnswerLayout.FeedInsetRight;
    const float FeedInsetTop = 248f;
    const float FeedInsetBottom = PodiumBarHeight;

    static GameObject CreatePrepareScreen(Transform parent, QuizGameController game, QuizSubject subject, Camera cam)
    {
        var root = CreatePanel("PrepareScreen", parent, BgDeep);
        ThemedBg(root.GetComponent<Image>());
        Stretch(root);

        // Math gets 4 holographic 3D blocks (+ − × ÷) floating in front of the scene — a real
        // depth effect 2D UI can't fake, reusing the Playing screen's answer-cube mesh/material look.
        if (subject == QuizSubject.Math)
            ArucoQuizOperatorFloaters3DBuilder.Build(cam, root.transform);

        var nebulaTl = CreateRadialGlow(root.transform, "NebulaTL", new Vector2(-660, 380), new Vector2(700, 700),
            new Color(0.43f, 0f, 1f, 0.2f));
        Themed(nebulaTl, 0.2f, true);
        var nebulaBr = CreateRadialGlow(root.transform, "NebulaBR", new Vector2(710, -390), new Vector2(650, 650),
            new Color(0f, 0.47f, 1f, 0.14f));
        Themed(nebulaBr, 0.14f);
        var nebulaCenter = CreateRadialGlow(root.transform, "NebulaCenter", new Vector2(0, 40), new Vector2(1000, 600),
            new Color(0.31f, 0f, 0.78f, 0.1f));
        Themed(nebulaCenter, 0.1f, true);
        var nebulas = new List<Image> { nebulaTl, nebulaBr, nebulaCenter };

        var grid = CreateTiledGrid(root.transform);

        // Both subjects keep the twinkling stars, plus a matching "rain" layer that continuously
        // rises up the screen and loops — Math's rain is the 4 operator glyphs (so every operation
        // is represented, not just the ones on the hero badges), English's is short words/phrases.
        // Same UiRiseLoop mechanism for both — kept as separate arrays (not shared helper data)
        // since the glyph sets, sizing and cadence are tuned per subject.
        List<Graphic> stars;
        if (subject == QuizSubject.Math)
        {
            stars = new List<Graphic>
            {
                CreateTwinkleStar(root.transform, new Vector2(-700, 440), 28),
                CreateTwinkleStar(root.transform, new Vector2(-870, 80), 20),
                CreateTwinkleStar(root.transform, new Vector2(740, 410), 24),
                CreateTwinkleStar(root.transform, new Vector2(870, -40), 18),
                CreateTwinkleStar(root.transform, new Vector2(-430, -450), 22),
                CreateTwinkleStar(root.transform, new Vector2(500, -430), 20),
            };
            // Not added to `stars`: UiRiseLoop already owns their alpha (fade in/out at the top and
            // bottom of the loop) — PrepareScreenAmbient's twinkle sine wave would fight over the
            // same alpha channel every frame if these were twinkled too.
            // x values grouped into two side banks (|x| ≥ 500), leaving a clean central corridor so
            // the rising glyphs frame the title/hero/button instead of scrolling across them. Only x
            // changed here — sizes/speeds/offsets (which stagger the vertical timing) are untouched,
            // and consecutive glyphs alternate left/right so both banks stay populated over the cycle.
            var operators = new (string op, float x, float size, float speed, float offset, bool accent2)[]
            {
                ("+", -920, 30, 26f, 0f, false),
                ("×", 500, 26, 33f, 124f, true),
                ("−", -800, 34, 29f, 248f, false),
                ("÷", 580, 28, 24f, 372f, true),
                ("+", -680, 30, 31f, 496f, false),
                ("×", 680, 26, 27f, 620f, true),
                ("−", -580, 34, 35f, 744f, false),
                ("÷", 800, 28, 23f, 868f, true),
                ("+", -500, 30, 30f, 992f, false),
                ("×", 920, 26, 26f, 1116f, true),
            };
            foreach (var o in operators)
                CreateRisingWord(root.transform, o.op, o.x, o.size, o.speed, o.offset, o.accent2);
        }
        else
        {
            // English words continuously rise up the background instead of just bobbing in
            // place — "câu từ tiếng anh bay lơ lửng lên liên tục". Each word's cycle starts at
            // vertical center (rangeBottom 0, half of Math's -620..620) rather than climbing all
            // the way up from below the canvas, then loops back to center once it exits the top —
            // "xuất hiện ngay ở giữa, không phải bắt đầu từ phía dưới".
            // 14 words (up from 8, per user feedback 2026-07-15 asking twice for denser/wider) —
            // spread across the nearly the full 1920 canvas width and the 620 loop range. At this
            // count a word behind the title at any given moment is the norm rather than the
            // exception; the title's own bold glow/gradient still reads over a faint 0.2-alpha word
            // passing behind it, so this leans fully into "dense" per the explicit ask.
            // x values grouped into two side banks (|x| ≥ 500), leaving a clean central corridor so
            // the words rise in the wings and frame the title/hero/button instead of scrolling across
            // them (the old spread put a moving word behind the title at nearly all times). Only x
            // changed — sizes/speeds/offsets are untouched, and consecutive words alternate left/right
            // so both banks stay populated across the loop.
            var words = new (string word, float x, float size, float speed, float offset, bool accent2)[]
            {
                ("HELLO", -950, 22, 13f, 0f, false),
                ("YES!", 500, 19, 16f, 44f, true),
                ("GOOD", -870, 22, 12f, 89f, false),
                ("HAPPY", 560, 19, 17f, 133f, true),
                ("LEARN", -790, 21, 14f, 177f, false),
                ("SMART", 610, 19, 19f, 221f, true),
                ("GREAT", -700, 21, 11f, 266f, false),
                ("FUN!", 700, 23, 15f, 310f, true),
                ("ABC", -610, 20, 18f, 354f, false),
                ("STAR", 790, 19, 13f, 398f, true),
                ("COOL", -560, 21, 16f, 443f, false),
                ("NICE", 870, 19, 12f, 487f, true),
                ("SUPER", -500, 22, 17f, 531f, false),
                ("WOW!", 950, 20, 14f, 575f, true),
            };
            // Not added to `stars`: UiRiseLoop already owns their alpha (fade in/out at the top and
            // bottom of the loop) — PrepareScreenAmbient's twinkle sine wave would fight over the
            // same alpha channel every frame if these were twinkled too.
            stars = new List<Graphic>();
            foreach (var w in words)
                CreateRisingWord(root.transform, w.word, w.x, w.size, w.speed, w.offset, w.accent2, 0f);
        }

        // Title block (slides up first) — vertical rhythm below is centered as one
        // ~886px-tall block within the 1080px canvas (≈97px margin top & bottom).
        var titleGroup = CreateGroup("TitleGroup", root.transform, new Vector2(0, 353), new Vector2(1400, 180));
        AddSlideUp(titleGroup, 0f);

        var subtitleText = subject == QuizSubject.Math
            ? "· ARUCO GAME SYSTEM · TOÁN HỌC ·"
            : "· ARUCO GAME SYSTEM · TIẾNG ANH ·";
        var subtitle = CreateTmp("Subtitle", titleGroup.transform, subtitleText, 16,
            new Vector2(0, 68), new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f), mono: true);
        Themed(subtitle, 0.55f);
        subtitle.characterSpacing = 10;

        var title1Text = "CHỌN ĐÁP ÁN ĐÚNG";
        var title1 = CreateTmp("Title1", titleGroup.transform, title1Text, 62, new Vector2(0, 6), Yellow);
        SetGradient(title1, new Color(1f, 0.88f, 0.25f), new Color(1f, 0.573f, 0f));
        StyleTitleGlow(title1, new Color(1f, 0.7f, 0f, 0.45f));

        var title2Text = subject == QuizSubject.Math ? "MÔN TOÁN" : "MÔN TIẾNG ANH";
        var title2 = CreateTmp("Title2", titleGroup.transform, title2Text, 54, new Vector2(0, -60), Cyan);
        ThemedGradient(title2);
        StyleTitleGlow(title2, new Color(0.2f, 0.5f, 1f, 0.45f));

        CreateTitleShimmer(titleGroup.transform);

        // Hero (slides up second) — subject-specific centerpiece instead of the old subject picker.
        var heroGroup = CreateGroup("HeroGroup", root.transform, new Vector2(0, 88), new Vector2(900, 290));
        AddSlideUp(heroGroup, 0.15f);
        if (subject == QuizSubject.Math)
            CreateMathPrepareHero(heroGroup.transform);
        else
            CreateEnglishPrepareHero(heroGroup.transform);

        // Compact step chips (slides up third)
        var stepsRow = CreateGroup("StepsRow", root.transform, new Vector2(0, -129), new Vector2(1780, 64));
        AddSlideUp(stepsRow, 0.28f);
        var steps = new (Sprite icon, string text)[]
        {
            (ArucoQuizSpriteAssets.CameraIcon(), "Đặt thẻ Aruco trước camera"),
            (ArucoQuizSpriteAssets.BookOpen(), "Đọc câu hỏi trên màn hình"),
            (ArucoQuizSpriteAssets.HandCover(), "Che thẻ đúng để trả lời"),
            (ArucoQuizSpriteAssets.Clock(), "15 giây mỗi câu"),
        };
        const float chipWidth = 420f;
        const float chipGap = 20f;
        var stepsTotalWidth = steps.Length * chipWidth + (steps.Length - 1) * chipGap;
        var stepStartX = -stepsTotalWidth * 0.5f + chipWidth * 0.5f;
        for (var i = 0; i < steps.Length; i++)
        {
            var x = stepStartX + i * (chipWidth + chipGap);
            CreateStepChip(stepsRow.transform, steps[i].icon, steps[i].text, x, chipWidth);
        }

        // Start button (slides up fourth)
        prepareButton = Create3DButton(root.transform, "StartButton", "BẮT ĐẦU NGAY!", new Vector2(0, -267),
            new Vector2(520, 92), 44, ArucoQuizSpriteAssets.Rocket(), out var startWrap);
        AddSlideUp(startWrap, 0.4f);
        prepareButton.interactable = false;
        startButtonLabelRef = prepareButton.transform.Find("Label").GetComponent<TMP_Text>();

        // A couple of tiny twinkling sparkles right at the button — draws the eye without a new asset.
        stars.Add(CreateTwinkleStar(root.transform, new Vector2(-292, -226), 15));
        stars.Add(CreateTwinkleStar(root.transform, new Vector2(298, -308), 13));

        // Not mono: its runtime status strings ("Giữ che thẻ…", "khởi động"...) use ư/ơ-horn
        // vowels that Share Tech Mono's fallback chain can't cover (see PlayHint/ResultArucoStatus).
        prepareArucoStatusRef = CreateTmp("ArucoStartStatus", root.transform,
            "Đặt đủ 4 thẻ ArUco (0–3) vào khung camera", 20, new Vector2(0, -375),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f));
        Themed(prepareArucoStatusRef, 0.75f);
        prepareArucoStatusRef.rectTransform.sizeDelta = new Vector2(1100, 36);

        var footerGroup = CreateGroup("FooterGroup", root.transform, new Vector2(0, -428), new Vector2(1200, 44));
        AddSlideUp(footerGroup, 0.5f);
        var footer = CreateTmp("Footer", footerGroup.transform, "· 5 CÂU HỎI · 15 GIÂY / CÂU · CHÚC MAY MẮN ·", 18,
            Vector2.zero, new Color(Cyan.r, Cyan.g, Cyan.b, 0.38f), mono: true);
        Themed(footer, 0.38f);
        footer.characterSpacing = 2;

        var ambient = root.AddComponent<PrepareScreenAmbient>();
        SetPrivate(ambient, "twinkleStars", stars.ToArray());
        SetPrivate(ambient, "nebulaOrbs", nebulas.ToArray());
        SetPrivate(ambient, "gridOverlay", grid);

        return root;
    }

    static readonly Color BadgeTextDark = new Color(0.06f, 0.05f, 0.09f);

    /// <summary>Glossy circular "chip" badge (glow + drop shadow + gradient-ish fill + rim + shine)
    /// — same visual language as the 3D answer-cube letter badges, reused for 2D UI icons.</summary>
    static GameObject CreateGlossyBadge(Transform parent, string name, Vector2 pos, float size, Color color)
    {
        var wrap = new GameObject(name, typeof(RectTransform));
        wrap.transform.SetParent(parent, false);
        var wrapRt = wrap.GetComponent<RectTransform>();
        wrapRt.sizeDelta = new Vector2(size, size);
        wrapRt.anchoredPosition = pos;

        CreateRadialGlow(wrap.transform, "Glow", Vector2.zero, new Vector2(size * 2.2f, size * 2.2f),
            new Color(color.r, color.g, color.b, 0.32f));

        CreateIcon(wrap.transform, "Shadow", ArucoQuizSpriteAssets.Circle(), new Vector2(0, -size * 0.1f),
            new Vector2(size, size), new Color(color.r * 0.3f, color.g * 0.25f, color.b * 0.25f, 0.85f));

        CreateIcon(wrap.transform, "Fill", ArucoQuizSpriteAssets.Circle(), Vector2.zero,
            new Vector2(size * 0.88f, size * 0.88f), color);

        CreateIcon(wrap.transform, "Rim", ArucoQuizSpriteAssets.Ring(), Vector2.zero,
            new Vector2(size * 0.88f, size * 0.88f), new Color(1f, 1f, 1f, 0.55f));

        CreateIcon(wrap.transform, "Shine", ArucoQuizSpriteAssets.Circle(), new Vector2(-size * 0.16f, size * 0.18f),
            new Vector2(size * 0.4f, size * 0.4f), new Color(1f, 1f, 1f, 0.38f));

        return wrap;
    }

    /// <summary>Math Prepare hero: a big "123" badge with operator badges truly orbiting it (not just
    /// bobbing), plus a rocket weaving past two ringed "number planets" on a wider outer orbit —
    /// leans fully into the space/orbit motif now that it's this app's exclusive identity.</summary>
    static void CreateMathPrepareHero(Transform heroGroup)
    {
        var center = new Vector2(0, 6);

        var ringA = CreateOrbitRing(heroGroup, "HeroRingA", 300, ArucoQuizSpriteAssets.RingArc(),
            new Color(Accent.r, Accent.g, Accent.b, 0.5f), 42f);
        Themed(ringA, 0.5f);
        var ringB = CreateOrbitRing(heroGroup, "HeroRingB", 250, ArucoQuizSpriteAssets.Ring(),
            new Color(Accent2.r, Accent2.g, Accent2.b, 0.22f), -30f);
        Themed(ringB, 0.22f, true);

        // Hero centerpiece: the rocket mascot "saying" a launch callout — same grounded-mascot +
        // speech-bubble structure as the English owl, for a matched pair instead of a bare equation.
        // Sized up from the original 190×96/92px icon so the mascot reads as the one clear focal
        // point instead of one of ten similarly-sized floating objects.
        CreateSpeechBubble(heroGroup, "GO!", new Vector2(center.x, center.y + 68), new Vector2(210, 104), Accent,
            BadgeTextDark, 44f);

        var rocketWrap = new GameObject("RocketMascot", typeof(RectTransform));
        rocketWrap.transform.SetParent(heroGroup, false);
        var rocketRt = rocketWrap.GetComponent<RectTransform>();
        rocketRt.sizeDelta = new Vector2(150, 150);
        rocketRt.anchoredPosition = new Vector2(center.x, center.y - 60);
        StyleImageGlow(rocketWrap.transform, Vector2.zero, Yellow);
        CreateIcon(rocketWrap.transform, "Icon", ArucoQuizSpriteAssets.Rocket(), Vector2.zero, new Vector2(132, 132),
            Color.white);
        var rocketMotion = rocketWrap.AddComponent<UiFloatMotion>();
        SetPrivate(rocketMotion, "phase", 0.6f);
        SetPrivate(rocketMotion, "speed", 0.9f);
        SetPrivate(rocketMotion, "amplitude", 8f);

        // All 4 operations orbit the mascot as one clean set — replaces the old mix of 4 geometric
        // shapes + 2 "number planet" orbs, which doubled up on signaling "math" without adding
        // clarity. Diagonal phases (not cardinal 0/90/180/270) so no badge lands on the vertical
        // bubble+rocket stack — cardinal phases put the top/bottom badges directly on top of it.
        // Radius/phase mirror the English hero's 4-badge ring exactly, so the two heroes read as
        // one deliberate family instead of unrelated templates.
        var operators = new (string glyph, Color color, float phaseDeg)[]
        {
            ("+", Yellow, 45f),
            ("×", Cyan, 135f),
            ("−", Green, 225f),
            ("÷", Magenta, 315f),
        };
        foreach (var op in operators)
        {
            var badge = CreateGlossyBadge(heroGroup, $"Op_{op.glyph}", Vector2.zero, 76, op.color);
            var g = CreateTmp("Glyph", badge.transform, op.glyph, 32, Vector2.zero, BadgeTextDark);
            g.fontStyle = FontStyles.Bold;
            g.rectTransform.sizeDelta = new Vector2(68, 68);
            badge.AddComponent<UiOrbitPoint>().Configure(center, 300, 110, 20f, op.phaseDeg);
        }
    }

    /// <summary>English Prepare hero: a reading owl "saying" a speech bubble at the center — a much
    /// stronger language/communication signal than a plain letter badge — with book/target/letter
    /// badges orbiting in the gaps around it, and a spinning globe further out.</summary>
    static void CreateEnglishPrepareHero(Transform heroGroup)
    {
        var center = new Vector2(0, 6);

        var ringA = CreateOrbitRing(heroGroup, "HeroRingA", 300, ArucoQuizSpriteAssets.RingArc(),
            new Color(Accent.r, Accent.g, Accent.b, 0.5f), 42f);
        Themed(ringA, 0.5f);
        var ringB = CreateOrbitRing(heroGroup, "HeroRingB", 250, ArucoQuizSpriteAssets.Ring(),
            new Color(Accent2.r, Accent2.g, Accent2.b, 0.22f), -30f);
        Themed(ringB, 0.22f, true);

        var coral = new Color(1f, 0.478f, 0.42f);

        // Sized up from the original 190×96/96px icon to match the Math hero's enlarged rocket —
        // one clear focal mascot instead of one of nine similarly-sized floating objects.
        CreateSpeechBubble(heroGroup, "Hi!", new Vector2(center.x, center.y + 68), new Vector2(210, 104), Accent,
            BadgeTextDark, 44f);

        var owlWrap = new GameObject("OwlMascot", typeof(RectTransform));
        owlWrap.transform.SetParent(heroGroup, false);
        var owlRt = owlWrap.GetComponent<RectTransform>();
        owlRt.sizeDelta = new Vector2(150, 150);
        owlRt.anchoredPosition = new Vector2(center.x, center.y - 60);
        StyleImageGlow(owlWrap.transform, Vector2.zero, coral);
        CreateIcon(owlWrap.transform, "Icon", ArucoQuizSpriteAssets.Owl(), Vector2.zero, new Vector2(132, 132), coral);
        var owlMotion = owlWrap.AddComponent<UiFloatMotion>();
        SetPrivate(owlMotion, "phase", 0.6f);
        SetPrivate(owlMotion, "speed", 0.9f);
        SetPrivate(owlMotion, "amplitude", 8f);

        // A/B/C/D orbit the owl as one clean set — these are literally the 4 answer letters the
        // child will see on the Playing screen, so the hero previews the real format instead of
        // showing book/target icons and "?/!" punctuation alongside a globe (7 extra objects that
        // doubled up on signaling "language" without adding clarity). Diagonal phases (not cardinal
        // 0/90/180/270) so no badge lands on the vertical bubble+owl stack. Radius/phase mirror the
        // Math hero's 4-badge ring exactly, so the two heroes read as one deliberate family.
        var letters = new (string glyph, Color color, float phaseDeg)[]
        {
            ("A", Yellow, 45f),
            ("B", Cyan, 135f),
            ("C", coral, 225f),
            ("D", Magenta, 315f),
        };
        foreach (var l in letters)
        {
            var badge = CreateGlossyBadge(heroGroup, $"Letter_{l.glyph}", Vector2.zero, 76, l.color);
            var g = CreateTmp("Glyph", badge.transform, l.glyph, 32, Vector2.zero, BadgeTextDark);
            g.fontStyle = FontStyles.Bold;
            g.rectTransform.sizeDelta = new Vector2(68, 68);
            badge.AddComponent<UiOrbitPoint>().Configure(center, 300, 110, 20f, l.phaseDeg);
        }

        // A second, smaller speech bubble — kept static (not orbiting, unlike the old version)
        // so it can't sweep through the A/B/C/D ring; parked in the left gap outside the ring,
        // mirroring "Hi!" as a two-line conversation instead of one word. Lowered from y=150 to 110
        // so its top clears the title line above (it previously nudged into "MÔN TIẾNG ANH").
        CreateSpeechBubble(heroGroup, "How are you?", new Vector2(-400, 110), new Vector2(250, 68), coral,
            BadgeTextDark, 22f);
    }

    /// <summary>A comic-style speech bubble (rounded tile + a small downward tail, built from the
    /// existing rounded-panel and Triangle sprite) — for the Prepare heroes' callouts.</summary>
    static GameObject CreateSpeechBubble(Transform parent, string text, Vector2 pos, Vector2 size, Color fill,
        Color textColor, float textSize)
    {
        var bubble = CreateRoundedPanel("SpeechBubble", parent, fill);
        var rt = bubble.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        CreateRoundedBorder(bubble.transform, new Color(1f, 1f, 1f, 0.5f));

        var label = CreateTmp("Label", bubble.transform, text, textSize, Vector2.zero, textColor);
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.sizeDelta = size;

        var tail = new GameObject("Tail", typeof(RectTransform));
        tail.transform.SetParent(bubble.transform, false);
        var tailImg = tail.AddComponent<Image>();
        tailImg.sprite = ArucoQuizSpriteAssets.Triangle();
        tailImg.color = fill;
        tailImg.raycastTarget = false;
        var tailRt = tail.GetComponent<RectTransform>();
        tailRt.sizeDelta = new Vector2(30, 26);
        tailRt.anchoredPosition = new Vector2(-size.x * 0.22f, -size.y * 0.5f - 8f);
        tailRt.localRotation = Quaternion.Euler(0f, 0f, 180f);

        return bubble;
    }

    /// <summary>A soft glow that periodically sweeps across the title text, pausing between passes —
    /// built from the existing Radial glow sprite, no new art needed.</summary>
    static void CreateTitleShimmer(Transform titleGroup)
    {
        var bar = CreatePanel("ShimmerBar", titleGroup, new Color(1f, 1f, 1f, 0.16f));
        var barImg = bar.GetComponent<Image>();
        barImg.sprite = ArucoQuizSpriteAssets.Radial();
        barImg.raycastTarget = false;
        var barRt = bar.GetComponent<RectTransform>();
        barRt.sizeDelta = new Vector2(170, 260);
        barRt.anchoredPosition = new Vector2(-760, -27);

        bar.AddComponent<UiShimmerSweep>().Configure(1600f, 1.5f, 3.6f, 1f);
    }

    /// <summary>Background decoration: a short word/glyph that continuously rises up the screen from
    /// `rangeBottom` to the top edge and loops back, fading in/out at the ends. Math's operators use
    /// the full-height range (rangeBottom -620, i.e. starting below the visible canvas); English's
    /// words use rangeBottom 0 so each word's cycle starts at vertical center instead of climbing all
    /// the way up from off-screen — "xuất hiện ngay ở giữa, không phải bắt đầu từ phía dưới".</summary>
    static Graphic CreateRisingWord(Transform parent, string word, float x, float size, float speed,
        float startOffset, bool accent2, float rangeBottom = -620f)
    {
        var tmp = CreateTmp("RisingWord", parent, word, size, new Vector2(x, rangeBottom), Color.white);
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 1;
        tmp.enableWordWrapping = false;
        Themed(tmp, 0.2f, accent2);
        tmp.gameObject.AddComponent<UiRiseLoop>().Configure(speed, rangeBottom, 620f, startOffset);
        return tmp;
    }

    static void CreateStepChip(Transform parent, Sprite icon, string text, float x, float width)
    {
        var chip = CreateRoundedPanel("Chip", parent, new Color(1f, 1f, 1f, 0.05f));
        var rt = chip.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 60);
        rt.anchoredPosition = new Vector2(x, 0);

        CreateRoundedBorder(chip.transform, new Color(1f, 1f, 1f, 0.2f));
        Themed(chip.transform.Find("Border").GetComponent<Image>(), 0.3f);

        CreateIcon(chip.transform, "Icon", icon, new Vector2(-width * 0.5f + 42, 0), new Vector2(30, 30), Color.white);

        var label = CreateTmp("Label", chip.transform, text, 16, new Vector2(16, 0), new Color(1f, 1f, 1f, 0.82f));
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.sizeDelta = new Vector2(width - 76, 40);
    }

    // ════════════════════════════ SCREEN 2 · COUNTDOWN ════════════════════════════

    static GameObject CreateCountdownScreen(Transform parent)
    {
        var root = CreatePanel("CountdownScreen", parent, BgDeep);
        ThemedBg(root.GetComponent<Image>());
        Stretch(root);
        root.SetActive(false);

        var nebula = CreateRadialGlow(root.transform, "CdNebula", Vector2.zero, new Vector2(900, 640),
            new Color(0.31f, 0f, 0.78f, 0.12f));
        Themed(nebula, 0.12f, true);
        CreateTiledGrid(root.transform);

        countdownSubjectLabelRef = CreateTmp("PrepLabel", root.transform, "CHUẨN BỊ...", 30, new Vector2(0, 320),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f), mono: true);
        Themed(countdownSubjectLabelRef, 0.6f);
        countdownSubjectLabelRef.characterSpacing = 8;

        // Orbital rings around the number
        var orbitHost = CreateGroup("Orbits", root.transform, new Vector2(0, -10), new Vector2(420, 420));
        var ringA = CreateOrbitRing(orbitHost.transform, "RingA", 420, ArucoQuizSpriteAssets.RingArc(),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f), 90f);
        Themed(ringA, 0.7f);
        var ringB = CreateOrbitRing(orbitHost.transform, "RingB", 384, ArucoQuizSpriteAssets.RingArc(),
            new Color(Purple.r, Purple.g, Purple.b, 0.7f), -144f);
        Themed(ringB, 0.7f, true);
        var ringC = CreateOrbitRing(orbitHost.transform, "RingC", 344, ArucoQuizSpriteAssets.Ring(),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f), 60f);
        Themed(ringC, 0.12f);
        var ringD = CreateOrbitRing(orbitHost.transform, "RingD", 300, ArucoQuizSpriteAssets.RingArc(),
            new Color(Magenta.r, Magenta.g, Magenta.b, 0.4f), -45f);
        Themed(ringD, 0.4f, true);

        var numWrap = CreateGroup("CountdownNumWrap", root.transform, new Vector2(0, -10), new Vector2(420, 420));
        numWrap.AddComponent<CanvasGroup>();
        countdownPopRef = numWrap.AddComponent<UiCountdownPop>();
        countdownLabel = CreateTmp("CountdownNum", numWrap.transform, "3", 240, Vector2.zero, Yellow);
        SetGradient(countdownLabel, new Color(1f, 0.92f, 0.4f), Orange);
        StyleTitleGlow(countdownLabel, new Color(1f, 0.88f, 0.25f, 0.5f));

        var hint = CreateTmp("Hint", root.transform, "CÂU HỎI SẮP BẮT ĐẦU", 24, new Vector2(0, -320),
            new Color(1f, 1f, 1f, 0.3f), mono: true);
        hint.characterSpacing = 4;
        countdownArucoHintRef = hint;

        return root;
    }

    static Image CreateOrbitRing(Transform parent, string name, float size, Sprite sprite, Color color, float speed)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        go.AddComponent<UiOrbitRing>().Configure(speed);
        return img;
    }

    // ════════════════════════════ SCREEN 3 · PLAYING ════════════════════════════

    static GameObject CreatePlayingScreen(Transform parent, QuizGameController game, Camera cam,
        QuizSubject subject, out GameObject podiumRow)
    {
        var root = CreatePanel("PlayingScreen", parent, Color.clear);
        Stretch(root);
        root.GetComponent<Image>().raycastTarget = false;
        root.SetActive(false);

        var hud = CreatePanel("HUD", root.transform, Color.clear);
        Stretch(hud);
        hud.GetComponent<Image>().raycastTarget = false;

        playingCameraFeedRef = CreatePlayingCameraFeed(hud.transform);
        CreateEdgeDimOutsideFeed(hud.transform);
        CreateFineScanlinesOnFeed(hud.transform);
        var feedTransform = hud.transform.Find("CameraFeedLayer/LiveWebcam");
        if (feedTransform != null)
            feedAnswerLightsRef = CreateFeedAnswerLights(feedTransform);

        CreateSideHoloPanels(hud.transform, subject);
        CreateRecBadge(hud.transform);

        questionNumLabel = CreateBadgeValue(hud.transform, "QuestionBadge", "CÂU HỎI", "1/5",
            new Vector2(28, -28), Yellow, true);
        scoreLabel = CreateBadgeValue(hud.transform, "ScoreBadge", "ĐÃ ĐÚNG", "0/5",
            new Vector2(-28, -28), Green, false);

        CreateQuestionPanel(hud.transform);

        // Timer
        var timerWrap = new GameObject("TimerWrap", typeof(RectTransform));
        timerWrap.transform.SetParent(hud.transform, false);
        SetAnchoredRect(timerWrap.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0, -375), new Vector2(520, 110));
        timerPulseScale = timerWrap.AddComponent<UiPulseScale>();
        timerLabel = CreateTmp("Timer", timerWrap.transform, "0:15", 70, Vector2.zero, Cyan, mono: true);
        timerLabel.characterSpacing = 6;
        timerLabel.rectTransform.sizeDelta = new Vector2(520, 110);
        StyleTitleGlow(timerLabel, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f));

        // Bottom vignette only (answers are 3D cubes — no static UI columns)
        var bottomFade = CreateVerticalGradientPanel("PodiumBottomFade", hud.transform);
        var bottomRt = bottomFade.GetComponent<RectTransform>();
        SetAnchoredRect(bottomRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            Vector2.zero, new Vector2(0, PodiumBarHeight));
        bottomFade.GetComponent<Image>().color = new Color(0f, 0f, 0.12f, 0.32f);
        bottomFade.GetComponent<Image>().raycastTarget = false;

        // 3D cubes under PlayingScreen (World UI). ViewportAnchor3D cancels canvas scale shrink.
        podiumRow = ArucoQuizPodium3DBuilder.BuildRow(cam, root.transform);
        podiumRow.transform.SetAsFirstSibling();

        // Not mono: PodiumRow3DController swaps this to sentences containing "giữ"/"Giữ" —
        // Share Tech Mono's fallback doesn't cover the ư/ơ-horn vowel family.
        hintTextRef = CreateTmp("PlayHint", hud.transform, "Che thẻ ArUco của đáp án con chọn!", 22,
            Vector2.zero, new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f));
        var hintRt = hintTextRef.rectTransform;
        SetAnchoredRect(hintRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, PodiumBarHeight + 6), new Vector2(1100, 36));
        StyleTitleGlow(hintTextRef, new Color(0f, 0f, 0.1f, 0.55f));

        // Feedback overlays
        feedbackOk = CreateFeedbackPanel(hud.transform, true);
        feedbackBad = CreateFeedbackPanel(hud.transform, false);
        feedbackBadTitle = feedbackBad.transform.Find("Card/Title").GetComponent<TMP_Text>();
        feedbackBadSub = feedbackBad.transform.Find("Card/Sub").GetComponent<TMP_Text>();
        feedbackWrongIconRef = feedbackBad.transform.Find("Card/Icon").GetComponent<Image>();
        feedbackOk.SetActive(false);
        feedbackBad.SetActive(false);

        return root;
    }

    static void CreateQuestionPanel(Transform hud)
    {
        var qPanel = CreateRoundedPanel("QuestionPanel", hud, new Color(0f, 0.02f, 0.12f, 0.82f));
        var qPanelRt = qPanel.GetComponent<RectTransform>();
        SetAnchoredRect(qPanelRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -128), new Vector2(1500, 235));
        CreateRoundedBorder(qPanel.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f));
        Themed(qPanel.transform.Find("Border").GetComponent<Image>(), 0.45f);

        var qGlow = CreateRadialGlow(qPanel.transform, "QGlow", Vector2.zero, new Vector2(1600, 280),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f));
        Themed(qGlow, 0.12f);

        CreateScanLine(qPanel.transform, Cyan, 280f, 230f);
        Themed(qPanel.transform.Find("ScanHost/ScanLine").GetComponent<Image>(), 0.65f);
        CreateCornerBrackets(qPanel.transform, Cyan, new Vector2(738, 105), themed: true);

        questionHeaderLabel = CreateTmp("QHeader", qPanel.transform, "· CÂU HỎI 1 ·", 16,
            new Vector2(0, 72), new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f), mono: true);
        Themed(questionHeaderLabel, 0.55f);
        questionHeaderLabel.characterSpacing = 4;
        questionHeaderLabel.rectTransform.sizeDelta = new Vector2(1400, 36);

        questionBodyLabel = CreateTmp("QBody", qPanel.transform, "Câu hỏi", 62, new Vector2(0, -12), Color.white);
        questionBodyLabel.enableWordWrapping = true;
        questionBodyLabel.overflowMode = TextOverflowModes.Overflow;
        questionBodyLabel.enableAutoSizing = true;
        questionBodyLabel.fontSizeMin = 36;
        questionBodyLabel.fontSizeMax = 62;
        questionBodyLabel.lineSpacing = -4;
        questionBodyLabel.rectTransform.sizeDelta = new Vector2(1380, 130);
        StyleTitleGlow(questionBodyLabel, new Color(1f, 1f, 1f, 0.22f));
    }

    static RawImage CreatePlayingCameraFeed(Transform hud)
    {
        var layer = new GameObject("CameraFeedLayer", typeof(RectTransform));
        layer.transform.SetParent(hud, false);
        layer.transform.SetAsFirstSibling();
        Stretch(layer);

        var feedGo = new GameObject("LiveWebcam", typeof(RectTransform));
        feedGo.transform.SetParent(layer.transform, false);
        var feedRt = feedGo.GetComponent<RectTransform>();
        feedRt.anchorMin = Vector2.zero;
        feedRt.anchorMax = Vector2.one;
        feedRt.offsetMin = new Vector2(FeedInsetLeft, FeedInsetBottom);
        feedRt.offsetMax = new Vector2(-FeedInsetRight, -FeedInsetTop);

        var videoGo = new GameObject("Video", typeof(RectTransform));
        videoGo.transform.SetParent(feedGo.transform, false);
        Stretch(videoGo);
        var raw = videoGo.AddComponent<RawImage>();
        raw.color = Color.white;
        raw.raycastTarget = false;
        videoGo.AddComponent<CameraFeedRenderOrder>();
        var fitter = videoGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        var frame = CreatePanel("FeedFrame", feedGo.transform, Color.clear);
        Stretch(frame);
        frame.GetComponent<Image>().raycastTarget = false;
        CreateRoundedBorder(frame.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));
        Themed(frame.transform.Find("Border").GetComponent<Image>(), 0.35f);

        var c = new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f);
        var corner1 = CreateCameraCorner(frame.transform, new Vector2(0, 1), new Vector2(0, 0), true, true, c);
        var corner2 = CreateCameraCorner(frame.transform, new Vector2(1, 1), new Vector2(0, 0), false, true, c);
        var corner3 = CreateCameraCorner(frame.transform, new Vector2(0, 0), new Vector2(0, 0), true, false, c);
        var corner4 = CreateCameraCorner(frame.transform, new Vector2(1, 0), new Vector2(0, 0), false, false, c);
        foreach (var corner in new[] { corner1, corner2, corner3, corner4 })
        {
            Themed(corner.h, 0.55f);
            Themed(corner.v, 0.55f);
        }

        var label = CreateTmp("FeedHint", feedGo.transform, "ĐẶT THẢM ARUCO TRONG KHUNG · CHE THẺ ĐỂ CHỌN ĐÁP ÁN", 15,
            new Vector2(0, 12), new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f), mono: true);
        Themed(label, 0.45f);
        var labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0.5f, 0f);
        labelRt.anchorMax = new Vector2(0.5f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0, 10);
        labelRt.sizeDelta = new Vector2(1200, 28);
        label.characterSpacing = 2;

        return raw;
    }

    static CameraFeedAnswerLights CreateFeedAnswerLights(Transform feedParent)
    {
        var bar = new GameObject("AnswerLightsOnFeed", typeof(RectTransform));
        bar.transform.SetParent(feedParent, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0, 0);
        barRt.anchorMax = new Vector2(1, 0);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0, 48);
        barRt.sizeDelta = new Vector2(-32, 100);

        var rings = new Image[4];
        var glows = new Image[4];
        var fills = new Image[4];
        var markers = LoadMarkerTextures();

        for (var i = 0; i < 4; i++)
        {
            var accent = ArucoQuizCubeAssets.Accents[i];
            var xAnchor = 0.125f + i * 0.25f;

            var slot = new GameObject($"FeedLight_{ArucoQuizCubeAssets.Letters[i]}", typeof(RectTransform));
            slot.transform.SetParent(bar.transform, false);
            var slotRt = slot.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(xAnchor, 0.5f);
            slotRt.anchorMax = new Vector2(xAnchor, 0.5f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.sizeDelta = new Vector2(120, 110);
            slotRt.anchoredPosition = Vector2.zero;

            glows[i] = CreateRadialGlow(slot.transform, "Glow", new Vector2(0, 18), new Vector2(100, 100),
                new Color(accent.r, accent.g, accent.b, 0.08f));

            var ring = CreateRoundedPanel("Ring", slot.transform, new Color(accent.r, accent.g, accent.b, 0.12f));
            var ringRt = ring.GetComponent<RectTransform>();
            ringRt.sizeDelta = new Vector2(64, 64);
            ringRt.anchoredPosition = new Vector2(0, 22);
            CreateRoundedBorder(ring.transform, new Color(accent.r, accent.g, accent.b, 0.55f));
            rings[i] = ring.GetComponent<Image>();

            var markerGo = new GameObject("MarkerThumb", typeof(RectTransform));
            markerGo.transform.SetParent(ring.transform, false);
            markerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
            var raw = markerGo.AddComponent<RawImage>();
            raw.raycastTarget = false;
            if (markers[i] != null)
                raw.texture = markers[i];

            CreateTmp("Letter", slot.transform, ArucoQuizCubeAssets.Letters[i], 22, new Vector2(0, -8), accent);

            var barBg = CreateRoundedPanel("ChargeBg", slot.transform, new Color(0f, 0f, 0.08f, 0.65f));
            var barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.sizeDelta = new Vector2(96, 10);
            barBgRt.anchoredPosition = new Vector2(0, -42);
            barBg.GetComponent<Image>().raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = ArucoQuizSpriteAssets.Rounded();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            fill.color = accent;
            fill.raycastTarget = false;
            fills[i] = fill;
        }

        var lights = bar.AddComponent<CameraFeedAnswerLights>();
        SetPrivate(lights, "rings", rings);
        SetPrivate(lights, "glows", glows);
        SetPrivate(lights, "chargeFills", fills);
        return lights;
    }

    static void CreateEdgeDimOutsideFeed(Transform hud)
    {
        var dim = new Color(0.01f, 0f, 0.05f, 0.88f);

        var top = CreatePanel("DimTop", hud, dim);
        var topRt = top.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0, 1);
        topRt.anchorMax = Vector2.one;
        topRt.offsetMin = new Vector2(0, -FeedInsetTop);
        topRt.offsetMax = Vector2.zero;
        top.GetComponent<Image>().raycastTarget = false;

        var left = CreatePanel("DimLeft", hud, dim);
        var leftRt = left.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0, 0);
        leftRt.anchorMax = new Vector2(0, 1);
        leftRt.pivot = new Vector2(0, 0.5f);
        leftRt.offsetMin = new Vector2(0, PodiumBarHeight);
        leftRt.offsetMax = new Vector2(FeedInsetLeft, -FeedInsetTop);
        left.GetComponent<Image>().raycastTarget = false;

        var right = CreatePanel("DimRight", hud, dim);
        var rightRt = right.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(1, 0);
        rightRt.anchorMax = new Vector2(1, 1);
        rightRt.pivot = new Vector2(1, 0.5f);
        rightRt.offsetMin = new Vector2(-FeedInsetRight, PodiumBarHeight);
        rightRt.offsetMax = new Vector2(0, -FeedInsetTop);
        right.GetComponent<Image>().raycastTarget = false;
    }

    static void CreateFineScanlinesOnFeed(Transform hud)
    {
        var layer = hud.transform.Find("CameraFeedLayer/LiveWebcam");
        if (layer == null)
            return;
        var go = CreatePanel("FineScanlines", layer, Color.white);
        Stretch(go);
        var img = go.GetComponent<Image>();
        img.sprite = ArucoQuizSpriteAssets.ScanTile();
        img.type = Image.Type.Tiled;
        Themed(img, 0.022f);
        img.raycastTarget = false;
    }

    static (Image h, Image v) CreateCameraCorner(Transform hud, Vector2 anchor, Vector2 pos, bool left, bool top,
        Color color)
    {
        var corner = new GameObject("CamCorner", typeof(RectTransform));
        corner.transform.SetParent(hud, false);
        var rt = corner.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(left ? 0 : 1, top ? 1 : 0);
        rt.sizeDelta = new Vector2(32, 32);
        rt.anchoredPosition = pos;
        return CreateBracket(corner.transform, Vector2.zero, new Vector2(32, 32), left, top, color);
    }

    static void CreateSideHoloPanels(Transform hud, QuizSubject subject)
    {
        var left = CreateSideGradientPanel("LeftHolo", hud, true);
        SetAnchoredRect(left.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0.5f), new Vector2(92, 0), new Vector2(185, 0));

        if (subject == QuizSubject.Math)
        {
            var leftSymbols = new (string glyph, Color color, float phase)[]
            {
                ("+", Yellow, 0f),
                ("×", Cyan, 1.5f),
                ("−", Green, 0.8f),
                ("÷", Magenta, 2f),
            };
            for (var i = 0; i < leftSymbols.Length; i++)
            {
                var y = 150 - i * 96;
                var badge = CreateGlossyBadge(left.transform, $"SymL{i}", new Vector2(0, y), 72, leftSymbols[i].color);
                var g = CreateTmp("Glyph", badge.transform, leftSymbols[i].glyph, 30, Vector2.zero, BadgeTextDark);
                g.fontStyle = FontStyles.Bold;
                g.rectTransform.sizeDelta = new Vector2(64, 64);
                var motion = badge.AddComponent<UiFloatMotion>();
                SetPrivate(motion, "phase", leftSymbols[i].phase);
                SetPrivate(motion, "speed", 1.1f + i * 0.08f);
            }
        }
        else
        {
            // A/B/C/D instead of generic book/hand/target/camera icons — these are literally the
            // answer letters on the markers below, so the rail previews the real format the same
            // way Math's rail shows the 4 operators, instead of unrelated step-icon glyphs.
            var englishSymbols = new (string glyph, Color color, float phase)[]
            {
                ("A", new Color(0.176f, 0.831f, 0.749f), 0f),
                ("B", new Color(1f, 0.478f, 0.42f), 1.5f),
                ("C", Yellow, 0.8f),
                ("D", Cyan, 2f),
            };
            for (var i = 0; i < englishSymbols.Length; i++)
            {
                var y = 150 - i * 96;
                var badge = CreateGlossyBadge(left.transform, $"SymEn{i}", new Vector2(0, y), 72,
                    englishSymbols[i].color);
                var g = CreateTmp("Glyph", badge.transform, englishSymbols[i].glyph, 30, Vector2.zero, BadgeTextDark);
                g.fontStyle = FontStyles.Bold;
                g.rectTransform.sizeDelta = new Vector2(64, 64);
                var motion = badge.AddComponent<UiFloatMotion>();
                SetPrivate(motion, "phase", englishSymbols[i].phase);
                SetPrivate(motion, "speed", 1.1f + i * 0.08f);
            }
        }

        var right = CreateSideGradientPanel("RightHolo", hud, false);
        SetAnchoredRect(right.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(1, 0.5f), new Vector2(-92, 0), new Vector2(185, 0));

        var twinkles = new List<Graphic>();
        var rightItems = new (Sprite icon, float size, Color tint, float y, float phase, bool twinkle)[]
        {
            (ArucoQuizSpriteAssets.Trophy(), 76, new Color(1f, 0.71f, 0f), 160, 0.5f, false),
            (ArucoQuizSpriteAssets.Star(), 52, new Color(1f, 0.86f, 0f), 58, 0f, true),
            (ArucoQuizSpriteAssets.Star(), 40, Cyan, -30, 1f, true),
            (ArucoQuizSpriteAssets.Star(), 64, Purple, -128, 2.2f, false),
        };
        for (var i = 0; i < rightItems.Length; i++)
        {
            var item = rightItems[i];
            var img = CreateIcon(right.transform, $"SymR{i}", item.icon, new Vector2(0, item.y),
                new Vector2(item.size, item.size), item.tint);
            if (item.twinkle)
                twinkles.Add(img);
            else
            {
                var motion = img.gameObject.AddComponent<UiFloatMotion>();
                SetPrivate(motion, "phase", item.phase);
                SetPrivate(motion, "speed", 0.95f + i * 0.05f);
            }
        }

        var sideFx = right.AddComponent<PlayingSideAmbient>();
        SetPrivate(sideFx, "twinkleGraphics", twinkles.ToArray());
    }

    static GameObject CreateSideGradientPanel(string name, Transform parent, bool leftEdge)
    {
        var go = CreatePanel(name, parent, Color.white);
        var img = go.GetComponent<Image>();
        img.sprite = leftEdge ? ArucoQuizSpriteAssets.SideFadeLeft() : ArucoQuizSpriteAssets.SideFadeRight();
        img.color = new Color(0f, 0.04f, 0.2f, 0.85f);
        img.raycastTarget = false;

        var edgeLine = CreatePanel("EdgeLine", go.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.08f));
        var lineRt = edgeLine.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(leftEdge ? 1 : 0, 0);
        lineRt.anchorMax = new Vector2(leftEdge ? 1 : 0, 1);
        lineRt.sizeDelta = new Vector2(1, 0);
        lineRt.anchoredPosition = Vector2.zero;
        var edgeLineImg = edgeLine.GetComponent<Image>();
        edgeLineImg.raycastTarget = false;
        Themed(edgeLineImg, 0.08f);

        return go;
    }

    static GameObject CreateVerticalGradientPanel(string name, Transform parent)
    {
        var go = CreatePanel(name, parent, Color.white);
        var img = go.GetComponent<Image>();
        img.sprite = ArucoQuizSpriteAssets.VerticalFade();
        img.color = new Color(0f, 0f, 0.12f, 0.58f);
        img.raycastTarget = false;
        return go;
    }

    static void CreateRecBadge(Transform hud)
    {
        var rec = CreateRoundedPanel("RecBadge", hud, new Color(0f, 0f, 0.08f, 0.6f));
        SetAnchoredRect(rec.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(640, 38));
        CreateRoundedBorder(rec.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f));
        Themed(rec.transform.Find("Border").GetComponent<Image>(), 0.25f);

        var dot = CreatePanel("RecDot", rec.transform, new Color(1f, 0.13f, 0.13f, 1f));
        var dotRt = dot.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0, 0.5f);
        dotRt.anchorMax = new Vector2(0, 0.5f);
        dotRt.sizeDelta = new Vector2(9, 9);
        dotRt.anchoredPosition = new Vector2(20, 0);
        dot.GetComponent<Image>().sprite = ArucoQuizSpriteAssets.Circle();
        dot.GetComponent<Image>().raycastTarget = false;
        var blink = rec.AddComponent<UiRecBlink>();
        SetPrivate(blink, "dot", dot.GetComponent<Image>());

        recSubjectTextRef = CreateTmp("RecText", rec.transform, "REC · TOÁN HỌC", 14, new Vector2(12, 0),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f), mono: true);
        Themed(recSubjectTextRef, 0.75f);
        recSubjectTextRef.characterSpacing = 2;
        recSubjectTextRef.rectTransform.sizeDelta = new Vector2(600, 34);
    }

    static TMP_Text CreateBadgeValue(Transform hud, string name, string caption, string value, Vector2 insetTop,
        Color accent, bool left)
    {
        var wrap = new GameObject(name, typeof(RectTransform));
        wrap.transform.SetParent(hud, false);
        var wrapRt = wrap.GetComponent<RectTransform>();
        var anchor = left ? new Vector2(0, 1) : new Vector2(1, 1);
        SetAnchoredRect(wrapRt, anchor, anchor, anchor, insetTop, new Vector2(220, 120));

        var shadow = CreateRoundedPanel("BadgeShadow", wrap.transform,
            new Color(accent.r * 0.35f, accent.g * 0.28f, accent.b * 0.1f, 0.55f));
        var shadowRt = shadow.GetComponent<RectTransform>();
        shadowRt.anchorMin = Vector2.zero;
        shadowRt.anchorMax = Vector2.one;
        shadowRt.offsetMin = new Vector2(0, -8);
        shadowRt.offsetMax = new Vector2(0, -8);
        shadow.GetComponent<Image>().raycastTarget = false;

        var badge = CreateRoundedPanel("Face", wrap.transform, new Color(accent.r, accent.g, accent.b, 0.14f));
        Stretch(badge);
        CreateRoundedBorder(badge.transform, new Color(accent.r, accent.g, accent.b, 0.75f));

        CreateRadialGlow(badge.transform, "BadgeGlow", Vector2.zero, new Vector2(260, 140),
            new Color(accent.r, accent.g, accent.b, 0.18f));

        CreateBracket(badge.transform, new Vector2(-98, 52), new Vector2(12, 12), true, true, accent);
        CreateBracket(badge.transform, new Vector2(98, 52), new Vector2(12, 12), false, true, accent);

        CreateTmp("Caption", badge.transform, caption, 13, new Vector2(0, 32),
            new Color(accent.r, accent.g, accent.b, 0.75f), mono: true).rectTransform.sizeDelta = new Vector2(200, 28);
        var val = CreateTmp("Value", badge.transform, value, 50, new Vector2(0, -16), accent);
        val.rectTransform.sizeDelta = new Vector2(200, 70);
        StyleTitleGlow(val, new Color(accent.r, accent.g, accent.b, 0.45f));
        return val;
    }

    static GameObject CreateFeedbackPanel(Transform parent, bool correct)
    {
        var accent = correct ? Green : RedWrong;
        var overlay = CreatePanel(correct ? "FeedbackCorrect" : "FeedbackWrong", parent,
            correct ? new Color(0f, 0.12f, 0.04f, 0.65f) : new Color(0.12f, 0f, 0.03f, 0.65f));
        Stretch(overlay);
        overlay.GetComponent<Image>().raycastTarget = true;

        var card = CreateRoundedPanel("Card", overlay.transform, new Color(accent.r, accent.g, accent.b, 0.14f));
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(720, 430);
        card.AddComponent<CanvasGroup>();
        card.AddComponent<UiBounceIn>();
        CreateRoundedBorder(card.transform, new Color(accent.r, accent.g, accent.b, 0.75f));
        CreateRadialGlow(card.transform, "CardGlow", Vector2.zero, new Vector2(860, 560),
            new Color(accent.r, accent.g, accent.b, 0.22f));
        CreateCornerBrackets(card.transform, accent, new Vector2(348, 203));

        var icon = CreateIcon(card.transform, "Icon", correct ? ArucoQuizSpriteAssets.Star() : ArucoQuizSpriteAssets.Cross(),
            new Vector2(0, 118), new Vector2(120, 120), correct ? new Color(1f, 0.86f, 0f) : accent);
        icon.name = "Icon";

        var title = CreateTmp("Title", card.transform, correct ? "CHÍNH XÁC!" : "SAI RỒI!", 78, new Vector2(0, -6), accent);
        StyleTitleGlow(title, new Color(accent.r, accent.g, accent.b, 0.5f));
        var sub = CreateTmp("Sub", card.transform, correct ? "+1 ĐIỂM" : "CỐ LÊN NHÉ!", 32, new Vector2(0, -120),
            new Color(1f, 1f, 1f, 0.75f), mono: true);
        sub.characterSpacing = 2;

        return overlay;
    }

    // ════════════════════════════ SCREEN 4 · RESULT ════════════════════════════

    static GameObject CreateResultScreen(Transform parent, out GameObject[] stars)
    {
        var root = CreatePanel("ResultScreen", parent, BgDeep);
        ThemedBg(root.GetComponent<Image>());
        Stretch(root);
        root.SetActive(false);

        var resultNebula = CreateRadialGlow(root.transform, "ResultNebula", Vector2.zero, new Vector2(900, 900),
            new Color(0.39f, 0f, 1f, 0.1f));
        Themed(resultNebula, 0.1f, true);
        CreateTiledGrid(root.transform);

        var twinkles = new List<Graphic>
        {
            CreateTwinkleStar(root.transform, new Vector2(-780, 450), 26),
            CreateTwinkleStar(root.transform, new Vector2(780, 420), 22),
            CreateTwinkleStar(root.transform, new Vector2(-580, -450), 24),
            CreateTwinkleStar(root.transform, new Vector2(600, -460), 20),
        };
        var ambient = root.AddComponent<PlayingSideAmbient>();
        SetPrivate(ambient, "twinkleGraphics", twinkles.ToArray());

        resultSubjectLabelRef = CreateTmp("SubjectLabel", root.transform, "TOÁN HỌC", 24, new Vector2(0, 424),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f), mono: true);
        Themed(resultSubjectLabelRef, 0.7f);
        resultSubjectLabelRef.characterSpacing = 5;

        // Big result icon (trophy / star / book — swapped by controller)
        var iconWrap = CreateGroup("ResultIconWrap", root.transform, new Vector2(0, 322), new Vector2(170, 170));
        iconWrap.AddComponent<CanvasGroup>();
        iconWrap.AddComponent<UiBounceIn>();
        CreateRadialGlow(iconWrap.transform, "IconGlow", Vector2.zero, new Vector2(330, 330),
            new Color(1f, 0.71f, 0f, 0.25f));
        resultIconRef = CreateIcon(iconWrap.transform, "Icon", ArucoQuizSpriteAssets.Trophy(),
            Vector2.zero, new Vector2(155, 155), new Color(1f, 0.78f, 0.1f));

        // Holographic score panel
        var panelWrap = CreateGroup("ScorePanelWrap", root.transform, new Vector2(0, 55), new Vector2(780, 340));
        panelWrap.AddComponent<CanvasGroup>();
        AddSlideUp(panelWrap, 0.1f);

        var panel = CreateRoundedPanel("ScorePanel", panelWrap.transform, new Color(0f, 0.02f, 0.12f, 0.75f));
        Stretch(panel);
        CreateRoundedBorder(panel.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f));
        Themed(panel.transform.Find("Border").GetComponent<Image>(), 0.4f);
        CreateScanLine(panel.transform, Cyan, 220f, 320f);
        Themed(panel.transform.Find("ScanHost/ScanLine").GetComponent<Image>(), 0.65f);
        var scoreBracket1 = CreateBracket(panel.transform, new Vector2(-378, 158), new Vector2(18, 18), true, true, Cyan);
        var scoreBracket2 = CreateBracket(panel.transform, new Vector2(378, 158), new Vector2(18, 18), false, true, Cyan);
        foreach (var b in new[] { scoreBracket1, scoreBracket2 })
        {
            Themed(b.h, 0.95f);
            Themed(b.v, 0.95f);
        }

        var caption = CreateTmp("Caption", panel.transform, "KẾT QUẢ", 22, new Vector2(0, 122),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f), mono: true);
        Themed(caption, 0.55f);
        caption.characterSpacing = 6;

        resultScore = CreateTmp("Score", panel.transform, "0/5", 128, new Vector2(0, 8), Yellow);
        SetGradient(resultScore, new Color(1f, 0.92f, 0.4f), Orange);
        StyleTitleGlow(resultScore, new Color(1f, 0.7f, 0f, 0.4f));
        resultScore.rectTransform.sizeDelta = new Vector2(700, 150);

        resultMsg = CreateTmp("Message", panel.transform, "Kết quả", 38, new Vector2(0, -112), Color.white);
        resultMsg.fontStyle = FontStyles.Bold;
        resultMsg.rectTransform.sizeDelta = new Vector2(720, 60);

        // Star rating
        var starsRow = CreateGroup("StarsRow", root.transform, new Vector2(0, -205), new Vector2(400, 110));
        stars = new GameObject[3];
        for (var i = 0; i < 3; i++)
        {
            var starWrap = CreateGroup($"Star{i}", starsRow.transform, new Vector2(-118 + i * 118, 0), new Vector2(100, 100));
            CreateRadialGlow(starWrap.transform, "StarGlow", Vector2.zero, new Vector2(170, 170),
                new Color(1f, 0.84f, 0f, 0.3f));
            CreateIcon(starWrap.transform, "Star", ArucoQuizSpriteAssets.Star(), Vector2.zero,
                new Vector2(94, 94), new Color(1f, 0.84f, 0.1f));
            var pop = starWrap.AddComponent<UiStarPop>();
            pop.Configure(0.3f + i * 0.2f);
            stars[i] = starWrap;
        }

        // Play again — one chunky 3D button, centered (no more "change subject" — one subject per app).
        playAgainBtn = Create3DButton(root.transform, "PlayAgainButton", "CHƠI LẠI", new Vector2(0, -350),
            new Vector2(360, 86), 34, ArucoQuizSpriteAssets.Restart(), out var againWrap);
        AddSlideUp(againWrap, 0.3f);
        playAgainBtn.interactable = false;

        // Not mono — same horn-vowel reason as ArucoStartStatus/PlayHint above.
        resultArucoStatusRef = CreateTmp("ResultArucoStatus", root.transform,
            "Đặt đủ 4 thẻ ArUco (0–3) vào khung camera", 20, new Vector2(0, -450),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f));
        Themed(resultArucoStatusRef, 0.75f);
        resultArucoStatusRef.rectTransform.sizeDelta = new Vector2(900, 36);

        return root;
    }

    // ════════════════════════════ SHARED BUILDERS ════════════════════════════

    static Button Create3DButton(Transform parent, string name, string label, Vector2 anchored, Vector2 size,
        float fontSize, Sprite icon, out GameObject wrap)
    {
        wrap = new GameObject(name, typeof(RectTransform));
        wrap.transform.SetParent(parent, false);
        var wrapRt = wrap.GetComponent<RectTransform>();
        wrapRt.sizeDelta = size + new Vector2(80, 40);
        wrapRt.anchoredPosition = anchored;
        wrap.AddComponent<CanvasGroup>();

        var glow = CreateRadialGlow(wrap.transform, "BtnGlow", Vector2.zero, size + new Vector2(60, 60),
            new Color(1f, 0.7f, 0f, 0.22f));
        var pulse = wrap.AddComponent<UiGlowPulse>();
        SetPrivate(pulse, "glowImage", glow);

        var shadow = CreateRoundedPanel("BtnShadow", wrap.transform, new Color(0.63f, 0.24f, 0f, 0.7f));
        var shadowRt = shadow.GetComponent<RectTransform>();
        shadowRt.sizeDelta = size;
        shadowRt.anchoredPosition = new Vector2(0, -9);
        shadow.GetComponent<Image>().raycastTarget = false;

        var face = CreateRoundedPanel("Face", wrap.transform, Yellow);
        var faceRt = face.GetComponent<RectTransform>();
        faceRt.sizeDelta = size;
        faceRt.anchoredPosition = Vector2.zero;

        var shine = CreateRoundedPanel("Shine", face.transform, new Color(1f, 0.96f, 0.62f, 0.45f));
        var shineRt = shine.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0.04f, 0.52f);
        shineRt.anchorMax = new Vector2(0.96f, 0.92f);
        shineRt.offsetMin = Vector2.zero;
        shineRt.offsetMax = Vector2.zero;
        shine.GetComponent<Image>().raycastTarget = false;

        var btn = face.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.94f, 0.55f);
        colors.pressedColor = new Color(1f, 0.78f, 0.2f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        var btn3d = face.AddComponent<UiButton3D>();
        btn3d.Configure(faceRt);

        // Icon fixed near the left edge; label gets the remaining width as a fixed box with
        // auto-sizing text — label content varies per subject at runtime (e.g. the much longer
        // "BẮT ĐẦU MÔN TIẾNG ANH!"), so it must shrink to fit rather than spill past the button.
        var hasIcon = icon != null;
        var iconSize = fontSize + 8f;
        var sidePadding = size.x * 0.06f;
        var iconX = -size.x * 0.5f + sidePadding + iconSize * 0.5f;

        if (hasIcon)
        {
            var iconImg = CreateIcon(face.transform, "BtnIcon", icon, new Vector2(iconX, 0),
                new Vector2(iconSize, iconSize), new Color(0.12f, 0.02f, 0f));
            iconImg.raycastTarget = false;
        }

        var labelLeft = hasIcon ? iconX + iconSize * 0.5f + 12f : -size.x * 0.5f + sidePadding;
        var labelRight = size.x * 0.5f - sidePadding;
        var labelWidth = Mathf.Max(40f, labelRight - labelLeft);
        var labelCenterX = (labelLeft + labelRight) * 0.5f;

        var labelTmp = CreateTmp("Label", face.transform, label, fontSize, new Vector2(labelCenterX, 2),
            new Color(0.09f, 0f, 0.01f));
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.characterSpacing = 2;
        labelTmp.enableWordWrapping = false;
        labelTmp.overflowMode = TextOverflowModes.Overflow;
        labelTmp.enableAutoSizing = true;
        labelTmp.fontSizeMin = fontSize * 0.45f;
        labelTmp.fontSizeMax = fontSize;
        labelTmp.rectTransform.sizeDelta = new Vector2(labelWidth, size.y * 0.82f);

        return btn;
    }

    static void CreateScanLine(Transform panel, Color color, float speed, float travel)
    {
        var scanHost = CreatePanel("ScanHost", panel, Color.clear);
        Stretch(scanHost);
        scanHost.GetComponent<Image>().raycastTarget = false;
        scanHost.AddComponent<RectMask2D>();
        var scanLine = CreatePanel("ScanLine", scanHost.transform, new Color(color.r, color.g, color.b, 0.65f));
        var scanRt = scanLine.GetComponent<RectTransform>();
        scanRt.anchorMin = new Vector2(0, 1);
        scanRt.anchorMax = new Vector2(1, 1);
        scanRt.sizeDelta = new Vector2(0, 2);
        scanRt.anchoredPosition = Vector2.zero;
        scanLine.GetComponent<Image>().raycastTarget = false;
        var scanFx = scanHost.AddComponent<UiScanLine>();
        SetPrivate(scanFx, "line", scanRt);
        SetPrivate(scanFx, "speed", speed);
        SetPrivate(scanFx, "travel", travel);
    }

    static void CreateCornerBrackets(Transform card, Color accent, Vector2 halfSize, bool themed = false)
    {
        var bright = new Color(accent.r, accent.g, accent.b, 0.95f);
        var dim = new Color(accent.r, accent.g, accent.b, 0.4f);
        var b1 = CreateBracket(card, new Vector2(-halfSize.x, halfSize.y), new Vector2(14, 14), true, true, bright);
        var b2 = CreateBracket(card, new Vector2(halfSize.x, halfSize.y), new Vector2(14, 14), false, true, bright);
        var b3 = CreateBracket(card, new Vector2(-halfSize.x, -halfSize.y), new Vector2(14, 14), true, false, dim);
        var b4 = CreateBracket(card, new Vector2(halfSize.x, -halfSize.y), new Vector2(14, 14), false, false, dim);
        if (!themed)
            return;
        foreach (var b in new[] { b1, b2 })
        {
            Themed(b.h, 0.95f);
            Themed(b.v, 0.95f);
        }
        foreach (var b in new[] { b3, b4 })
        {
            Themed(b.h, 0.4f);
            Themed(b.v, 0.4f);
        }
    }

    static (Image h, Image v) CreateBracket(Transform parent, Vector2 pos, Vector2 size, bool left, bool top, Color color)
    {
        var h = CreatePanel("BracketH", parent, color);
        var hRt = h.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(size.x, 2.5f);
        hRt.anchoredPosition = pos + new Vector2(left ? -size.x * 0.35f : size.x * 0.35f, top ? size.y * 0.35f : -size.y * 0.35f);
        var hImg = h.GetComponent<Image>();
        hImg.raycastTarget = false;

        var v = CreatePanel("BracketV", parent, color);
        var vRt = v.GetComponent<RectTransform>();
        vRt.sizeDelta = new Vector2(2.5f, size.y);
        vRt.anchoredPosition = pos + new Vector2(left ? -size.x * 0.35f : size.x * 0.35f, top ? size.y * 0.35f : -size.y * 0.35f);
        var vImg = v.GetComponent<Image>();
        vImg.raycastTarget = false;

        return (hImg, vImg);
    }

    static Image CreateRadialGlow(Transform parent, string name, Vector2 anchored, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = ArucoQuizSpriteAssets.Radial();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchored;
        return img;
    }

    static Graphic CreateTwinkleStar(Transform parent, Vector2 anchored, float size)
    {
        var img = CreateIcon(parent, "Star", ArucoQuizSpriteAssets.Star(), anchored, new Vector2(size, size),
            new Color(1f, 1f, 1f, 0.85f));
        return img;
    }

    static Image CreateTiledGrid(Transform parent)
    {
        var go = CreatePanel("TechGrid", parent, Color.white);
        Stretch(go);
        var img = go.GetComponent<Image>();
        img.sprite = ArucoQuizSpriteAssets.GridTile();
        img.type = Image.Type.Tiled;
        Themed(img, 0.04f);
        img.raycastTarget = false;
        return img;
    }

    static Image CreateIcon(Transform parent, string name, Sprite sprite, Vector2 anchored, Vector2 size, Color tint)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = tint;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchored;
        return img;
    }

    static void StyleImageGlow(Transform parent, Vector2 pos, Color accent)
    {
        var glow = CreateRadialGlow(parent, "IconGlow", pos, new Vector2(130, 130),
            new Color(accent.r, accent.g, accent.b, 0.28f));
        glow.transform.SetSiblingIndex(Mathf.Max(0, glow.transform.GetSiblingIndex() - 1));
    }

    static GameObject CreateGroup(string name, Transform parent, Vector2 anchored, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchored;
        return go;
    }

    static void AddSlideUp(GameObject go, float delay)
    {
        if (go.GetComponent<CanvasGroup>() == null)
            go.AddComponent<CanvasGroup>();
        go.AddComponent<UiSlideUpFade>().Configure(delay);
    }

    static void SetGradient(TMP_Text tmp, Color top, Color bottom)
    {
        tmp.enableVertexGradient = true;
        tmp.colorGradient = new VertexGradient(top, top, bottom, bottom);
        tmp.color = Color.white;
    }

    static void StyleTitleGlow(TMP_Text tmp, Color underlay)
    {
        if (tmp == null)
            return;
        tmp.fontStyle = FontStyles.Bold;
        if (tmp.GetComponent<UiTextShadowRuntime>() != null)
            return;
        var fx = tmp.gameObject.AddComponent<UiTextShadowRuntime>();
        SetPrivate(fx, "shadowColor", underlay);
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject CreateRoundedPanel(string name, Transform parent, Color color)
    {
        var go = CreatePanel(name, parent, color);
        var img = go.GetComponent<Image>();
        img.sprite = ArucoQuizSpriteAssets.Rounded();
        img.type = Image.Type.Sliced;
        return go;
    }

    static void CreateRoundedBorder(Transform parent, Color color)
    {
        var go = new GameObject("Border", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = ArucoQuizSpriteAssets.RoundedBorder();
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        Stretch(go);
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    static TMP_Text CreateTmp(string name, Transform parent, string text, float size, Vector2 anchored, Color color,
        bool mono = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (mono)
            go.AddComponent<QuizTmpFontTag>().Configure(true);

        var rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(900, 140);
        rt.anchoredPosition = anchored;
        return tmp;
    }

    static Texture2D[] LoadMarkerTextures()
    {
        var list = new Texture2D[4];
        for (var i = 0; i < 4; i++)
            list[i] = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MarkerFolder}/{ArucoMarkerConfig.MarkerTextureFileName(i)}");
        return list;
    }

    static void EnsureMarkerImportSettings()
    {
        for (var i = 0; i < 4; i++)
        {
            var path = $"{MarkerFolder}/{ArucoMarkerConfig.MarkerTextureFileName(i)}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            if (importer.isReadable && importer.textureCompression == TextureImporterCompression.Uncompressed)
                continue;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
                return;
        }

        var list = new EditorBuildSettingsScene[scenes.Length + 1];
        for (var i = 0; i < scenes.Length; i++)
            list[i] = scenes[i];
        list[list.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = list;
    }

    internal static void SetPrivate(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"Field not found: {target.name}.{field}");
            return;
        }

        if (value == null)
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                prop.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            return;
        }

        switch (value)
        {
            case Object obj when prop.propertyType == SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = obj;
                break;
            case int iv when prop.isArray == false && prop.propertyType == SerializedPropertyType.Integer:
                prop.intValue = iv;
                break;
            case float fv when prop.propertyType == SerializedPropertyType.Float:
                prop.floatValue = fv;
                break;
            case Color col when prop.propertyType == SerializedPropertyType.Color:
                prop.colorValue = col;
                break;
            case bool bv when prop.propertyType == SerializedPropertyType.Boolean:
                prop.boolValue = bv;
                break;
            // Boxed enums (e.g. QuizSubject) never match `case int` above — pattern matching checks
            // the boxed type, not the enum's underlying type — so this was silently falling through
            // to the "Unsupported assign" warning below and leaving the field at its default value.
            // Confirmed in practice: QuizGameController.subject was baked as 0 (Math) in BOTH scenes,
            // including the English one, which meant English was pulling Math's question set and
            // theme colors at runtime, not just showing the wrong Prepare-screen button label.
            case System.Enum e when prop.propertyType == SerializedPropertyType.Enum:
                prop.enumValueIndex = System.Convert.ToInt32(e);
                break;
            default:
                if (value is TMP_Text[] texts && prop.isArray)
                {
                    prop.arraySize = texts.Length;
                    for (var i = 0; i < texts.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
                }
                else if (value is GameObject[] gos && prop.isArray)
                {
                    prop.arraySize = gos.Length;
                    for (var i = 0; i < gos.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = gos[i];
                }
                else if (value is Graphic[] graphics && prop.isArray)
                {
                    prop.arraySize = graphics.Length;
                    for (var i = 0; i < graphics.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = graphics[i];
                }
                else if (value is Object[] arr && prop.isArray)
                {
                    prop.arraySize = arr.Length;
                    for (var i = 0; i < arr.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                }
                else if (value is GameObject go)
                    prop.objectReferenceValue = go;
                else if (value is Component c)
                    prop.objectReferenceValue = c;
                else
                    Debug.LogWarning($"Unsupported assign {field} type {value?.GetType().Name}");
                break;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AttachQuizButtonSound(Button button, QuizAudioController audio)
    {
        if (button == null || audio == null)
            return;
        var sound = button.gameObject.GetComponent<UiQuizButtonSound>();
        if (sound == null)
            sound = button.gameObject.AddComponent<UiQuizButtonSound>();
        SetPrivate(sound, "audio", audio);
    }
}
