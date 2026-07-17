using System.IO;
using ArucoQuiz;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds 4 small holographic 3D blocks (+ − × ÷) that float and slowly tumble in front of the
/// Math Prepare screen. Reuses the exact same mesh/material language as the Playing-screen answer
/// cubes (ArucoQuizPodium3DBuilder + ArucoQuizCubeAssets) — including their Yellow/Cyan/Green/
/// Magenta accent order, which already matches this app's +/×/−/÷ color convention — so these read
/// as the same "3D toy" family instead of a one-off effect.
/// </summary>
public static class ArucoQuizOperatorFloaters3DBuilder
{
    static readonly string[] Glyphs = { "+", "×", "−", "÷" };

    // Scattered around the hero cluster's edges, clear of every 2D UI block (Title/Hero/StepsRow/
    // StartButton) at that screen height — these float in front of the scene so any spot that
    // shares a UI element's screen position would occlude that element's text. The lower pair used
    // to sit at viewport y=0.35, which lands on top of the StepsRow instruction chips (canvas
    // y≈-162 vs. the chips' -161..-97 band, with the chips nearly spanning the full canvas width);
    // moved up to flank the hero at its vertical center (y=0.50) instead, where the hero's own
    // width (900px) leaves clear margin on both sides.
    static readonly Vector2[] ViewportPositions =
    {
        new Vector2(0.13f, 0.72f),
        new Vector2(0.87f, 0.72f),
        new Vector2(0.15f, 0.50f),
        new Vector2(0.85f, 0.50f),
    };

    static readonly Vector3[] BaseEuler =
    {
        new Vector3(18f, -20f, -3f),
        new Vector3(16f, 18f, 3f),
        new Vector3(15f, -16f, 2f),
        new Vector3(17f, 20f, -2f),
    };

    static readonly float[] Speeds = { 0.85f, 1.0f, 0.78f, 0.95f };
    static readonly float[] Phases = { 0f, 1.4f, 2.7f, 4.1f };

    public static GameObject Build(Camera cam, Transform parent)
    {
        var root = new GameObject("OperatorFloaters3D");
        root.transform.SetParent(parent, false);
        // The World UI canvas scales down to ~0.002 (its 1920x1080 RectTransform maps to ~1.8x1
        // real-world meters) — without this, every child's localScale gets crushed by that same
        // factor, rendering these blocks at roughly 1/500th their intended size (invisible in
        // practice). Same fix ArucoQuizPodium3DBuilder uses for the Playing-screen answer cubes.
        root.AddComponent<PodiumRowScaleNeutralizer>();

        for (var i = 0; i < 4; i++)
        {
            var block = BuildBlock(i);
            block.transform.SetParent(root.transform, false);

            var anchor = block.AddComponent<ViewportAnchor3D>();
            var so = new SerializedObject(anchor);
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("viewportPos").vector2Value = ViewportPositions[i];
            so.FindProperty("faceCamera").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            // OnEnable already fired (with placeholder defaults) before the SerializedObject
            // fields above were set — re-apply now so the block starts at its correct position.
            anchor.Apply();
        }

        return root;
    }

    static GameObject BuildBlock(int i)
    {
        var root = new GameObject($"Operator_{Glyphs[i]}");

        var floatRoot = new GameObject("FloatRoot");
        floatRoot.transform.SetParent(root.transform, false);
        var motion = floatRoot.AddComponent<Float3D>();
        motion.Configure(0.06f, Speeds[i], Phases[i], BaseEuler[i], new Vector3(10f, 22f, 6f));

        // Shrunk again from 0.24/0.09 per user feedback 2026-07-15 (still too prominent for
        // ambient decor even after the glass-face restyle).
        const float size = 0.18f;
        const float depth = 0.07f;

        // Opaque (depth-writing), same reasoning as before: the Prepare screen's root panel is
        // opaque, so these need to write depth and let ordinary opaque-then-transparent draw order
        // put them in front of the canvas, rather than relying on cross-queue distance sorting.
        // Flat glass-tinted faces (GlassFaceTex), not clones of the Playing-screen cubes' own
        // printed marker-pattern textures (DimmedTex, dropped) — those read as literal toy-block
        // graphics up close, a different visual language from the soft glossy-badge/glow look used
        // everywhere else on this screen (user feedback 2026-07-15: "hoà hợp với nền thiết kế xung
        // quanh"). Per-face brightness fakes simple top-lit shading (top brightest, back/bottom
        // darkest) so it still reads as a solid gem, not a flat decal.
        var accent = ArucoQuizCubeAssets.Accents[i];
        Quad(floatRoot.transform, "Front", new Vector3(0, 0, -depth * 0.5f), Vector3.zero,
            new Vector3(size, size, 1f), Opaque(GlassFaceTex(accent, 0.55f, $"glass{i}_front")));
        Quad(floatRoot.transform, "Back", new Vector3(0, 0, depth * 0.5f), new Vector3(0, 180, 0),
            new Vector3(size, size, 1f), Opaque(GlassFaceTex(accent, 0.18f, $"glass{i}_dark")));
        Quad(floatRoot.transform, "Left", new Vector3(-size * 0.5f, 0, 0), new Vector3(0, 90, 0),
            new Vector3(depth, size, 1f), Opaque(GlassFaceTex(accent, 0.35f, $"glass{i}_side")));
        Quad(floatRoot.transform, "Right", new Vector3(size * 0.5f, 0, 0), new Vector3(0, -90, 0),
            new Vector3(depth, size, 1f), Opaque(GlassFaceTex(accent, 0.35f, $"glass{i}_side")));
        Quad(floatRoot.transform, "Top", new Vector3(0, size * 0.5f, 0), new Vector3(90, 0, 0),
            new Vector3(size, depth, 1f), Opaque(GlassFaceTex(accent, 0.85f, $"glass{i}_top")));
        Quad(floatRoot.transform, "Bottom", new Vector3(0, -size * 0.5f, 0), new Vector3(-90, 0, 0),
            new Vector3(size, depth, 1f), Opaque(GlassFaceTex(accent, 0.18f, $"glass{i}_dark")));

        // A soft radial glow halo — the exact sprite/recipe used for every other glow in this app
        // (nebula blobs, badge halos, twinkle stars) — parented to `root` (not `floatRoot`) so it
        // doesn't tumble with the cube; a steady halo reads as ambient light, a tumbling one would
        // look like a spinning sticker. Sits further from camera than every face (transparent, so it
        // draws after the opaque faces and depth-tests against them: only the halo that pokes past
        // the cube's silhouette shows through), tying these floaters to the same glow language as
        // the hero badges and background nebula instead of standing apart as a one-off effect.
        var glowMat = Glow(ArucoQuizSpriteAssets.Radial().texture, new Color(accent.r, accent.g, accent.b, 0.5f));
        Quad(root.transform, "Glow", new Vector3(0, 0, depth * 2f), Vector3.zero,
            new Vector3(size * 2.6f, size * 2.6f, 1f), glowMat);

        var glyph = NewTmp3D(floatRoot.transform, "Glyph", Glyphs[i], Color.Lerp(accent, Color.white, 0.2f));
        glyph.color = new Color(glyph.color.r, glyph.color.g, glyph.color.b, 0.85f);
        glyph.transform.localPosition = new Vector3(0f, 0f, -depth * 0.5f - 0.01f);
        // Overlay queue so the glyph always draws after the (now opaque, depth-testing) cube face
        // and after the UI canvas, regardless of exact distance — text must win the compare, not
        // just usually win it.
        var glyphMat = new Material(glyph.fontSharedMaterial) { renderQueue = 4000 };
        glyph.fontSharedMaterial = glyphMat;

        return root;
    }

    /// <summary>An opaque (Geometry-queue, depth-writing) material sharing the given texture —
    /// see the comment above BuildBlock's Quad() calls for why this must not be Transparent.</summary>
    static Material Opaque(Texture2D tex)
    {
        return new Material(Shader.Find("Unlit/Texture")) { mainTexture = tex };
    }

    /// <summary>A transparent, non-depth-writing material for the glow halo — see the comment above
    /// the "Glow" Quad() call in BuildBlock for why this one (unlike the cube faces) is safe as
    /// Transparent: it draws after every opaque object regardless of distance, and still depth-tests
    /// against the already-written opaque cube faces, so only the part poking past their silhouette
    /// shows through.</summary>
    static Material Glow(Texture2D tex, Color tint)
    {
        var mat = new Material(Shader.Find("Unlit/Transparent")) { mainTexture = tex, color = tint };
        mat.renderQueue = 3000;
        return mat;
    }

    const string DimFolder = "Assets/Textures/Generated/CubeDim";

    /// <summary>Bakes (and caches on disk) a small flat-colored "glass" face texture — a single
    /// solid color blended between the Prepare screen's deep-space background tone and the
    /// operator's accent color, with `brightness` controlling how much accent shows through (near 0 =
    /// mostly background, reads as a shadowed face; near 1 = full accent, reads as top-lit). Six
    /// faces at different brightness levels still read as a solid shaded gem, without the printed
    /// marker-pattern look of the Playing-screen's own answer-cube textures.</summary>
    static Texture2D GlassFaceTex(Color accent, float brightness, string cacheName)
    {
        Directory.CreateDirectory(DimFolder);
        var path = $"{DimFolder}/{cacheName}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;

        var bg = new Color(0.05f, 0.04f, 0.12f);
        var face = Color.Lerp(bg, accent, brightness);
        if (brightness > 0.7f)
            face = Color.Lerp(face, Color.white, (brightness - 0.7f) * 0.5f);

        const int Size = 8;
        var copy = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var pixels = new Color[Size * Size];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = face;
        copy.SetPixels(pixels);
        copy.Apply();

        File.WriteAllBytes(path, copy.EncodeToPNG());
        Object.DestroyImmediate(copy);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

    static TMP_Text NewTmp3D(Transform parent, string name, string text, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 26f;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        var font = ArucoQuizFontAssets.LoadBaloo();
        if (font != null)
            tmp.font = font;
        tmp.rectTransform.sizeDelta = new Vector2(6f, 3f);
        go.transform.localScale = Vector3.one * 0.1f;
        return tmp;
    }
}
