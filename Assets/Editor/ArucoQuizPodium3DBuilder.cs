using ArucoQuiz;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the four floating holographic 3D answer cubes (saved as prefabs in
/// Assets/Prefabs/) and assembles the PodiumRow3D shown during gameplay.
/// </summary>
public static class ArucoQuizPodium3DBuilder
{
    const string PrefabFolder = "Assets/Prefabs";

    // Float choreography per cube, mirroring HTML floatA–D.
    static readonly Vector3[] BaseEuler =
    {
        new Vector3(17f, -15f, -2f),
        new Vector3(15f, 12f, 2f),
        new Vector3(16f, -12f, 2f),
        new Vector3(18f, 14f, -2f),
    };

    static readonly float[] Speeds = { 1.25f, 1.12f, 1.0f, 1.18f };
    static readonly float[] Phases = { 0f, 1.6f, 3.1f, 4.5f };

    public static GameObject BuildRow(Camera cam, Transform parent)
    {
        var row = new GameObject("PodiumRow3D");
        if (parent != null)
            row.transform.SetParent(parent, false);
        row.AddComponent<PodiumRowScaleNeutralizer>();
        row.AddComponent<PodiumRenderStabilizer>();

        for (var i = 0; i < 4; i++)
        {
            var prefab = BuildCubePrefab(i);
            var cube = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            cube.name = $"AnswerCube_{ArucoQuizCubeAssets.Letters[i]}";
            cube.transform.SetParent(row.transform, false);

            var vp = PodiumAnswerLayout.ViewportPosition(i);
            var anchor = cube.GetComponent<ViewportAnchor3D>();
            var so = new SerializedObject(anchor);
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("viewportPos").vector2Value = vp;
            so.FindProperty("distance").floatValue = PodiumAnswerLayout.CubePlaneDistance;
            so.FindProperty("compensateCanvasParentScale").boolValue = false;
            so.FindProperty("faceCamera").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            // OnEnable already fired (with placeholder defaults) before the SerializedObject
            // fields above were set — re-apply now so the cube starts at its correct position
            // (the validator's PosePodium also repositions these explicitly for its screenshot).
            anchor.Apply();
        }

        return row;
    }

    public static AnswerCube3D[] GetCubes(GameObject row)
    {
        var cubes = new AnswerCube3D[4];
        for (var i = 0; i < 4; i++)
            cubes[i] = row.transform.GetChild(i).GetComponent<AnswerCube3D>();
        return cubes;
    }

    static GameObject BuildCubePrefab(int i)
    {
        System.IO.Directory.CreateDirectory(PrefabFolder);
        var path = $"{PrefabFolder}/AnswerCube_{ArucoQuizCubeAssets.Letters[i]}.prefab";

        var accent = ArucoQuizCubeAssets.Accents[i];
        var root = new GameObject($"AnswerCube_{ArucoQuizCubeAssets.Letters[i]}");
        root.AddComponent<ViewportAnchor3D>();
        var view = root.AddComponent<AnswerCube3D>();

        var floatRoot = new GameObject("FloatRoot");
        floatRoot.transform.SetParent(root.transform, false);
        var motion = floatRoot.AddComponent<Float3D>();
        motion.Configure(0.05f, Speeds[i], Phases[i], BaseEuler[i], new Vector3(2.5f, 9f, 2f));

        var body = new GameObject("Body");
        body.transform.SetParent(floatRoot.transform, false);

        BuildCubeMesh(body.transform, i);

        // Letter badge above the cube
        var badge = new GameObject("LetterBadge");
        badge.transform.SetParent(body.transform, false);
        badge.transform.localPosition = new Vector3(0f, 0.335f, -0.02f);

        var badgeRing = NewSprite(badge.transform, "Ring", ArucoQuizSpriteAssets.Circle(), Color.white, 0.165f);
        badgeRing.transform.localPosition = new Vector3(0f, 0f, 0.001f);
        var badgeFill = NewSprite(badge.transform, "Fill", ArucoQuizSpriteAssets.Circle(), accent, 0.148f);
        var badgeGlow = NewSprite(badge.transform, "Glow", ArucoQuizSpriteAssets.Radial(),
            new Color(accent.r, accent.g, accent.b, 0.5f), 0.30f);
        badgeGlow.transform.localPosition = new Vector3(0f, 0f, 0.01f);

        var letter = NewTmp3D(badge.transform, "Letter", ArucoQuizCubeAssets.Letters[i], 0.95f,
            Color.Lerp(accent, Color.black, 0.75f));
        letter.transform.localPosition = new Vector3(0f, 0f, -0.002f);

        // Answer value: bright front + darker stacked layers for a chunky 3D extrusion.
        // wrapAndFit=true so long English answers (ESL.json) wrap and auto-shrink to stay
        // inside the cube face instead of overflowing past its edges — short math answers
        // (digits/fractions) are well under the max size and render unchanged.
        var answer = NewTmp3D(body.transform, "AnswerText", "0", 1.55f, Color.Lerp(accent, Color.white, 0.25f),
            wrapAndFit: true);
        answer.transform.localPosition = new Vector3(0f, 0f, -0.075f);
        answer.fontStyle = FontStyles.Bold;

        var depth = new TMP_Text[3];
        for (var d = 0; d < depth.Length; d++)
        {
            var shade = Color.Lerp(accent, Color.black, 0.55f + d * 0.14f);
            var layer = NewTmp3D(body.transform, $"AnswerDepth{d}", "0", 1.55f, shade, wrapAndFit: true);
            layer.transform.localPosition = new Vector3(0.004f * (d + 1), -0.005f * (d + 1), -0.075f + 0.006f * (d + 1));
            layer.fontStyle = FontStyles.Bold;
            depth[d] = layer;
        }

        // Charge glow (lights up while the child covers this answer's marker)
        var glow = NewSprite(body.transform, "ChargeGlow", ArucoQuizSpriteAssets.Radial(),
            new Color(accent.r, accent.g, accent.b, 0f), 0.62f);
        glow.transform.localPosition = new Vector3(0f, 0f, 0.09f);

        var ringA = NewSprite(body.transform, "SelectRingA", ArucoQuizSpriteAssets.RingArc(),
            new Color(accent.r, accent.g, accent.b, 0f), 1.02f);
        ringA.transform.localPosition = new Vector3(0f, 0f, 0.11f);

        var ringB = NewSprite(body.transform, "SelectRingB", ArucoQuizSpriteAssets.Ring(),
            new Color(1f, 1f, 1f, 0f), 0.82f);
        ringB.transform.localPosition = new Vector3(0f, 0f, 0.115f);

        var beam = NewSprite(body.transform, "SelectBeam", ArucoQuizSpriteAssets.Radial(),
            new Color(accent.r, accent.g, accent.b, 0f), 0.42f);
        beam.transform.localPosition = new Vector3(0f, 0.55f, 0.08f);
        beam.transform.localScale = new Vector3(0.25f, 1.2f, 1f);

        // Ground shadow
        var shadow = NewSprite(root.transform, "GroundShadow", ArucoQuizSpriteAssets.Radial(),
            new Color(accent.r * 0.4f, accent.g * 0.4f, accent.b * 0.4f, 0.45f), 0.5f);
        shadow.transform.localPosition = new Vector3(0f, -0.33f, 0.05f);
        shadow.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

        view.Configure(answer, depth, letter, body.transform, glow, ringA, ringB, beam, shadow, motion, accent);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void BuildCubeMesh(Transform parent, int i)
    {
        const float size = 0.42f;
        const float depth = 0.115f;

        var cube = new GameObject("Cube");
        cube.transform.SetParent(parent, false);

        Quad(cube.transform, "Front", new Vector3(0, 0, -depth * 0.5f), Vector3.zero,
            new Vector3(size, size, 1f), ArucoQuizCubeAssets.FrontMaterial(i));
        Quad(cube.transform, "Back", new Vector3(0, 0, depth * 0.5f), new Vector3(0, 180, 0),
            new Vector3(size, size, 1f), ArucoQuizCubeAssets.DarkMaterial(i));
        Quad(cube.transform, "Left", new Vector3(-size * 0.5f, 0, 0), new Vector3(0, 90, 0),
            new Vector3(depth, size, 1f), ArucoQuizCubeAssets.SideMaterial(i));
        Quad(cube.transform, "Right", new Vector3(size * 0.5f, 0, 0), new Vector3(0, -90, 0),
            new Vector3(depth, size, 1f), ArucoQuizCubeAssets.SideMaterial(i));
        Quad(cube.transform, "Top", new Vector3(0, size * 0.5f, 0), new Vector3(90, 0, 0),
            new Vector3(size, depth, 1f), ArucoQuizCubeAssets.TopMaterial(i));
        Quad(cube.transform, "Bottom", new Vector3(0, -size * 0.5f, 0), new Vector3(-90, 0, 0),
            new Vector3(size, depth, 1f), ArucoQuizCubeAssets.DarkMaterial(i));
    }

    static void Quad(Transform parent, string name, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = pos;
        quad.transform.localRotation = Quaternion.Euler(euler);
        quad.transform.localScale = scale;
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, Color color, float diameter)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        var world = sprite.bounds.size.x;
        var s = diameter / Mathf.Max(0.0001f, world);
        go.transform.localScale = new Vector3(s, s, 1f);
        return sr;
    }

    // Cube front face is 0.42 world units square; a wrapAndFit text box must stay a little
    // inside that (in the same local units, scaled by the 0.1 GameObject scale below) or
    // wrapped lines would render past the cube's edges.
    static readonly Vector2 AnswerBoxSize = new Vector2(3.6f, 3.4f);
    const float AnswerFontSizeMinRatio = 0.28f;

    static TMP_Text NewTmp3D(Transform parent, string name, string text, float fontSize, Color color,
        bool wrapAndFit = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize * 10f; // TMP world text: 10 pt ≈ 1 unit at scale 0.1
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var font = ArucoQuizFontAssets.LoadBaloo();
        if (font != null)
            tmp.font = font;
        var rt = tmp.rectTransform;

        if (wrapAndFit)
        {
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = tmp.fontSize * AnswerFontSizeMinRatio;
            rt.sizeDelta = AnswerBoxSize;
        }
        else
        {
            tmp.enableWordWrapping = false;
            rt.sizeDelta = new Vector2(6f, 3f);
        }

        go.transform.localScale = Vector3.one * 0.1f;
        return tmp;
    }
}
