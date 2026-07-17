using System.IO;
using ArucoQuiz;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Opens the built scene, poses each screen (prepare / countdown / playing / result)
/// and renders the main camera to PNGs so the layout can be reviewed headlessly.
/// Output folder comes from -screenshotDir on the command line (defaults to Temp/QuizShots).
/// </summary>
public static class ArucoQuizSceneValidator
{
    const string MathScenePath = "Assets/Scenes/ArucoQuizMath.unity";
    const string EnglishScenePath = "Assets/Scenes/ArucoQuizEnglish.unity";

    [MenuItem("Aruco Quiz/Capture Math Screenshots", false, 12)]
    public static void CaptureMathMenu()
    {
        Capture(MathScenePath, "Screenshots", exitAfter: false);
        EditorUtility.RevealInFinder("Screenshots");
    }

    [MenuItem("Aruco Quiz/Capture English Screenshots", false, 13)]
    public static void CaptureEnglishMenu()
    {
        Capture(EnglishScenePath, "Screenshots", exitAfter: false);
        EditorUtility.RevealInFinder("Screenshots");
    }

    public static void CaptureFromCommandLine()
    {
        var outDir = "Temp/QuizShots";
        var scenePath = MathScenePath;
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-screenshotDir")
                outDir = args[i + 1];
            if (args[i] == "-scenePath")
                scenePath = args[i + 1];
        }

        Capture(scenePath, outDir, exitAfter: true);
    }

    static void Capture(string scenePath, string outDir, bool exitAfter)
    {
        Directory.CreateDirectory(outDir);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var cam = Camera.main;
        PoseCanvas(cam);
        ApplyFonts();
        PosePodium(cam);

        var prepare = GameObject.Find("WorldUI/PrepareScreen");
        var countdown = FindInactive("CountdownScreen");
        var playing = FindInactive("PlayingScreen");
        var result = FindInactive("ResultScreen");
        var podium = FindPodiumRow();

        Shot(cam, outDir, "1_prepare", prepare, null, null);
        Shot(cam, outDir, "2_countdown", countdown, prepare, null);
        Shot(cam, outDir, "3_playing", playing, countdown, podium);
        if (playing != null)
        {
            var fb = playing.transform.Find("HUD/FeedbackCorrect");
            if (fb != null)
            {
                fb.gameObject.SetActive(true);
                Shot(cam, outDir, "4_feedback_correct", playing, null, podium);
                fb.gameObject.SetActive(false);
            }
        }
        Shot(cam, outDir, "5_result", result, playing, null);

        Debug.Log($"[Validator] Screenshots written to {outDir}");
        if (exitAfter)
            EditorApplication.Exit(0);
    }

    static void Shot(Camera cam, string dir, string name, GameObject show, GameObject hide, GameObject podium)
    {
        if (show == null)
        {
            Debug.LogWarning($"[Validator] Screen missing for {name}");
            return;
        }

        if (hide != null)
            hide.SetActive(false);
        show.SetActive(true);
        Canvas.ForceUpdateCanvases();

        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prevTarget;

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;

        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
    }

    static void PoseCanvas(Camera cam)
    {
        var canvas = GameObject.Find("WorldUI");
        if (canvas == null || cam == null)
            return;
        var follow = canvas.GetComponent<WorldCanvasFollowCamera>();
        if (follow != null)
            follow.Reposition(cam); // LateUpdate never runs outside Play mode — pose it directly.
    }

    static void PosePodium(Camera cam)
    {
        var podium = FindPodiumRow();
        if (podium == null || cam == null)
            return;
        for (var i = 0; i < podium.transform.childCount && i < 4; i++)
        {
            var cube = podium.transform.GetChild(i);
            var vp = PodiumAnswerLayout.ViewportPosition(i);
            cube.position = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, 1.72f));
            // Mimic Float3D rest pose
            var floatRoot = cube.Find("FloatRoot");
            if (floatRoot != null)
            {
                var f = floatRoot.GetComponent<Float3D>();
                if (f != null)
                {
                    var so = new SerializedObject(f);
                    floatRoot.localRotation = Quaternion.Euler(so.FindProperty("baseEuler").vector3Value);
                }
            }
        }
    }

    static void ApplyFonts()
    {
        var baloo = ArucoQuizFontAssets.LoadBaloo();
        var mono = ArucoQuizFontAssets.LoadMono();
        if (baloo == null)
            return;
        foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (!text.gameObject.scene.IsValid())
                continue;
            var tag = text.GetComponent<QuizTmpFontTag>();
            text.font = tag != null && tag.UseMonospace && mono != null ? mono : baloo;
        }
    }

    static GameObject FindInactive(string name)
    {
        var canvas = GameObject.Find("WorldUI");
        if (canvas == null)
            return null;
        var t = canvas.transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    static GameObject FindPodiumRow()
    {
        var playing = FindInactive("PlayingScreen");
        if (playing == null)
            return null;
        var t = playing.transform.Find("PodiumRow3D");
        if (t == null)
            t = playing.transform.Find("HUD/PodiumRow3D");
        return t != null ? t.gameObject : null;
    }
}
