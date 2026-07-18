using System.IO;
using ArucoQuiz;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates BuiltIn WAV clips and wires them onto QuizAudioController in the open scene.
/// Replace clips under Assets/Audio/BuiltIn/ with your own files (keep filenames) anytime.
/// </summary>
public static class ArucoQuizAudioInstaller
{
    const string AudioRoot = "Assets/Audio/BuiltIn";
    const string GeneratorScript = "Tools/generate_quiz_audio.py";

    /// <summary>Called from scene builder after QuizAudio is created.</summary>
    public static void EnsureAndWire(QuizAudioController controller)
    {
        if (!Directory.Exists(AudioRoot) || !File.Exists($"{AudioRoot}/music_prepare.wav"))
            GenerateWavs();
        AssetDatabase.Refresh();
        ConfigureImportSettings();
        if (controller != null)
            WireController(controller);
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

        Assign(so, "sfxArucoAllReady", "sfx_aruco_all_ready.wav");
        Assign(so, "sfxCountdownTick", "sfx_countdown_tick.wav");
        Assign(so, "sfxCountdownGo", "sfx_countdown_go.wav");

        Assign(so, "sfxQuestionNew", "sfx_question_new.wav");
        Assign(so, "sfxMatReady", "sfx_mat_ready.wav");

        Assign(so, "sfxTimerTickWarning", "sfx_timer_tick_warning.wav");

        Assign(so, "sfxCoverSelectStart", "sfx_cover_select_start.wav");
        Assign(so, "sfxCoverChargeMid", "sfx_cover_charge_mid.wav");
        Assign(so, "sfxCoverLockIn", "sfx_cover_lock_in.wav");

        Assign(so, "sfxAnswerCorrect", "sfx_answer_correct.wav");
        Assign(so, "sfxAnswerWrong", "sfx_answer_wrong.wav");
        Assign(so, "sfxTimeout", "sfx_timeout.wav");

        Assign(so, "sfxResultFanfare", "sfx_result_fanfare.wav");
        Assign(so, "sfxStarPop", "sfx_star_pop.wav");

        so.ApplyModifiedPropertiesWithoutUndo();
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
