using System.IO;
using ArucoQuiz;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates BuiltIn WAV clips and wires them onto QuizAudioController in the open scene.
/// Replace clips under Assets/Audio/BuiltIn/ with your own files (keep filenames) anytime.
/// </summary>
public static class ArucoQuizAudioInstaller
{
    const string AudioRoot = "Assets/Audio/BuiltIn";
    const string GeneratorScript = "Tools/generate_quiz_audio.py";

    [MenuItem("Aruco Quiz/Fix Audio (Listener + Reimport Clips)", false, 26)]
    public static void FixAudioMenu()
    {
        ConfigureImportSettings();
        AssetDatabase.Refresh();

        var cam = Camera.main;
        if (cam == null)
            cam = Object.FindObjectOfType<Camera>();
        if (cam != null && cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        var wired = WireAllInScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Aruco Quiz Audio",
            "Đã bật import 2D + preload, thêm AudioListener trên camera (nếu thiếu), " +
            $"gắn lại {wired} QuizAudioController.\n\nKiểm tra Game view không bật icon Mute.",
            "OK");
    }

    [MenuItem("Aruco Quiz/Install Built-in Audio (Generate + Wire)", false, 25)]
    public static void InstallMenu()
    {
        if (!GenerateWavs())
        {
            EditorUtility.DisplayDialog("Aruco Quiz Audio",
                "Không chạy được script sinh WAV.\nChạy tay: python3 Tools/generate_quiz_audio.py",
                "OK");
            return;
        }

        AssetDatabase.Refresh();
        ConfigureImportSettings();
        var wired = WireAllInScene();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Aruco Quiz Audio",
            wired > 0
                ? $"Đã gắn clip vào {wired} QuizAudioController trong scene."
                : "Clip đã tạo tại Assets/Audio/BuiltIn/. Thêm object QuizAudio (Build Scene) rồi chạy lại menu.",
            "OK");
    }

    /// <summary>Called from scene builder after QuizAudio is created.</summary>
    public static void EnsureAndWire(QuizAudioController controller)
    {
        if (!Directory.Exists(AudioRoot) || !File.Exists($"{AudioRoot}/music_prepare.wav"))
            GenerateWavs();
        AssetDatabase.Refresh();
        ConfigureImportSettings();
        if (controller != null)
            WireController(controller);
        EnsureClipCatalogAsset();
    }

    static bool GenerateWavs()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return false;
        var script = Path.Combine(projectRoot, GeneratorScript);
        if (!File.Exists(script))
            return Directory.Exists(AudioRoot);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{script}\"",
            WorkingDirectory = projectRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null)
                return false;
            p.WaitForExit(15000);
            return p.ExitCode == 0;
        }
        catch
        {
            return Directory.Exists(AudioRoot);
        }
    }

    static void ConfigureImportSettings()
    {
        if (!Directory.Exists(AudioRoot))
            return;

        foreach (var path in Directory.GetFiles(AudioRoot, "*.wav"))
        {
            var asset = path.Replace('\\', '/');
            if (!asset.StartsWith("Assets/"))
            {
                var idx = asset.IndexOf("Assets/");
                if (idx >= 0)
                    asset = asset.Substring(idx);
                else
                    continue;
            }

            var importer = AssetImporter.GetAtPath(asset) as AudioImporter;
            if (importer == null)
                continue;

            var isMusic = Path.GetFileName(asset).StartsWith("music_");

            var settings = importer.defaultSampleSettings;
            settings.loadType = isMusic ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.ambisonic = false;

            var serialized = new SerializedObject(importer);
            var threeD = serialized.FindProperty("3D");
            if (threeD != null)
                threeD.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            importer.SaveAndReimport();
        }
    }

    static int WireAllInScene()
    {
        var count = 0;
        foreach (var audio in Object.FindObjectsOfType<QuizAudioController>(true))
        {
            WireController(audio);
            EditorUtility.SetDirty(audio);
            count++;
        }

        return count;
    }

    public static void WireController(QuizAudioController controller)
    {
        if (controller == null)
            return;

        var so = new SerializedObject(controller);
        Assign(so, "musicPrepare", "music_prepare.wav");
        Assign(so, "musicCountdown", "music_countdown.wav");
        Assign(so, "musicGameplay", "music_gameplay.wav");
        Assign(so, "musicResult", "music_result.wav");

        Assign(so, "sfxButtonClick", "sfx_button_click.wav");
        Assign(so, "sfxButtonHover", "sfx_button_hover.wav");
        Assign(so, "sfxScreenWhoosh", "sfx_screen_whoosh.wav");

        Assign(so, "sfxArucoAllReady", "sfx_aruco_all_ready.wav");
        Assign(so, "sfxCountdownTick", "sfx_countdown_tick.wav");
        Assign(so, "sfxCountdownWaitPulse", "sfx_countdown_wait_pulse.wav");
        Assign(so, "sfxCountdownGo", "sfx_countdown_go.wav");

        Assign(so, "sfxQuestionNew", "sfx_question_new.wav");
        Assign(so, "sfxMatReady", "sfx_mat_ready.wav");

        Assign(so, "sfxTimerTick", "sfx_timer_tick.wav");
        Assign(so, "sfxTimerTickWarning", "sfx_timer_tick_warning.wav");
        Assign(so, "sfxTimerUrgentLoop", "sfx_timer_urgent_loop.wav");

        Assign(so, "sfxCoverSelectStart", "sfx_cover_select_start.wav");
        Assign(so, "sfxCoverChargeMid", "sfx_cover_charge_mid.wav");
        Assign(so, "sfxCoverLockIn", "sfx_cover_lock_in.wav");

        Assign(so, "sfxAnswerCorrect", "sfx_answer_correct.wav");
        Assign(so, "sfxAnswerWrong", "sfx_answer_wrong.wav");
        Assign(so, "sfxTimeout", "sfx_timeout.wav");
        Assign(so, "sfxPodiumCorrect", "sfx_podium_correct.wav");
        Assign(so, "sfxPodiumWrong", "sfx_podium_wrong.wav");

        Assign(so, "sfxResultFanfare", "sfx_result_fanfare.wav");
        Assign(so, "sfxStarPop", "sfx_star_pop.wav");
        Assign(so, "sfxCountdownPopAnim", "sfx_countdown_pop_anim.wav");
        Assign(so, "sfxFeedbackPanelIn", "sfx_feedback_panel_in.wav");

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureClipCatalogAsset()
    {
        const string path = "Assets/Audio/QuizAudioClipCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<QuizAudioClipCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<QuizAudioClipCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }

        catalog.musicPrepare = Load("music_prepare.wav");
        catalog.musicCountdown = Load("music_countdown.wav");
        catalog.musicGameplay = Load("music_gameplay.wav");
        catalog.musicResult = Load("music_result.wav");
        catalog.sfxButtonClick = Load("sfx_button_click.wav");
        catalog.sfxButtonHover = Load("sfx_button_hover.wav");
        catalog.sfxScreenWhoosh = Load("sfx_screen_whoosh.wav");
        catalog.sfxArucoAllReady = Load("sfx_aruco_all_ready.wav");
        catalog.sfxCountdownTick = Load("sfx_countdown_tick.wav");
        catalog.sfxCountdownGo = Load("sfx_countdown_go.wav");
        catalog.sfxQuestionNew = Load("sfx_question_new.wav");
        catalog.sfxMatReady = Load("sfx_mat_ready.wav");
        catalog.sfxTimerTick = Load("sfx_timer_tick.wav");
        catalog.sfxTimerTickWarning = Load("sfx_timer_tick_warning.wav");
        catalog.sfxTimerUrgentLoop = Load("sfx_timer_urgent_loop.wav");
        catalog.sfxCoverSelectStart = Load("sfx_cover_select_start.wav");
        catalog.sfxCoverChargeMid = Load("sfx_cover_charge_mid.wav");
        catalog.sfxCoverLockIn = Load("sfx_cover_lock_in.wav");
        catalog.sfxAnswerCorrect = Load("sfx_answer_correct.wav");
        catalog.sfxAnswerWrong = Load("sfx_answer_wrong.wav");
        catalog.sfxTimeout = Load("sfx_timeout.wav");
        catalog.sfxResultFanfare = Load("sfx_result_fanfare.wav");
        catalog.sfxStarPop = Load("sfx_star_pop.wav");
        EditorUtility.SetDirty(catalog);
    }

    static AudioClip Load(string fileName) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/{fileName}");

    static void Assign(SerializedObject so, string field, string fileName)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
            return;
        prop.objectReferenceValue = Load(fileName);
    }
}
