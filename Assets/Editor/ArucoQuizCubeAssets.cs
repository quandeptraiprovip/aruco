using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Baked textures + materials for the four holographic 3D answer cubes
/// (colors match the HTML design: yellow / cyan / green / magenta).
/// </summary>
public static class ArucoQuizCubeAssets
{
    const string TexFolder = "Assets/Textures/Generated/Cube";
    const string MatFolder = "Assets/Materials/Generated";

    public static readonly Color[] Accents =
    {
        new Color(1f, 0.88f, 0.25f),   // A yellow
        new Color(0f, 0.898f, 1f),     // B cyan
        new Color(0f, 1f, 0.533f),     // C green
        new Color(1f, 0.2f, 0.8f),     // D magenta
    };

    public static readonly string[] Letters = { "A", "B", "C", "D" };

    public static Material FrontMaterial(int i) => MatFor($"cube{i}_front", () => GenFront(Accents[i]));
    public static Material SideMaterial(int i) => MatFor($"cube{i}_side", () => GenSide(Accents[i]));
    public static Material TopMaterial(int i) => MatFor($"cube{i}_top", () => GenTop(Accents[i]));
    public static Material DarkMaterial(int i) => MatFor($"cube{i}_dark", () => GenDark(Accents[i]));

    static Material MatFor(string name, System.Func<Texture2D> painter)
    {
        Directory.CreateDirectory(MatFolder);
        var matPath = $"{MatFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null)
            return existing;

        var tex = SaveTexture(name, painter);
        var mat = new Material(Shader.Find("Unlit/Transparent"))
        {
            mainTexture = tex,
        };
        AssetDatabase.CreateAsset(mat, matPath);
        return AssetDatabase.LoadAssetAtPath<Material>(matPath);
    }

    static Texture2D SaveTexture(string name, System.Func<Texture2D> painter)
    {
        Directory.CreateDirectory(TexFolder);
        var path = $"{TexFolder}/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;

        var tex = painter();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
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

    static Texture2D NewTex(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        return tex;
    }

    static Texture2D GenFront(Color accent)
    {
        const int n = 256;
        var tex = NewTex(n);
        var darkA = new Color(accent.r * 0.14f, accent.g * 0.14f, accent.b * 0.14f, 0.88f);
        var darkB = new Color(0.03f, 0.02f, 0.08f, 0.85f);

        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                // 155° diagonal gradient background
                var t = Mathf.Clamp01((x * 0.35f + (n - y) * 0.65f) / n);
                var c = Color.Lerp(darkA, darkB, t);

                // Inner glow near edges
                var edge = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                if (edge < 26)
                {
                    var g = 1f - edge / 26f;
                    c = Color.Lerp(c, new Color(accent.r, accent.g, accent.b, 0.9f), g * g * 0.28f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        // Neon border
        var border = new Color(accent.r, accent.g, accent.b, 0.95f);
        for (var i = 0; i < n; i++)
        {
            for (var w = 0; w < 4; w++)
            {
                tex.SetPixel(i, w, border);
                tex.SetPixel(i, n - 1 - w, border);
                tex.SetPixel(w, i, border);
                tex.SetPixel(n - 1 - w, i, border);
            }
        }

        // Corner brackets (thicker inner marks)
        void Bracket(int cx, int cy, int dx, int dy)
        {
            for (var k = 0; k < 22; k++)
            {
                for (var w = 0; w < 5; w++)
                {
                    tex.SetPixel(cx + dx * k, cy + dy * w, Color.white);
                    tex.SetPixel(cx + dx * w, cy + dy * k, Color.white);
                }
            }
        }

        Bracket(6, 6, 1, 1);
        Bracket(n - 7, 6, -1, 1);
        Bracket(6, n - 7, 1, -1);
        Bracket(n - 7, n - 7, -1, -1);

        tex.Apply();
        return tex;
    }

    static Texture2D GenTop(Color accent)
    {
        const int n = 128;
        var tex = NewTex(n);
        var light = Color.Lerp(accent, Color.white, 0.45f);
        for (var y = 0; y < n; y++)
        {
            var t = y / (float)(n - 1);
            var c = Color.Lerp(
                new Color(light.r, light.g, light.b, 0.62f),
                new Color(accent.r, accent.g, accent.b, 0.34f), t);
            for (var x = 0; x < n; x++)
                tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenSide(Color accent)
    {
        const int n = 128;
        var tex = NewTex(n);
        var mid = Color.Lerp(accent, Color.black, 0.35f);
        var deep = Color.Lerp(accent, Color.black, 0.65f);
        for (var x = 0; x < n; x++)
        {
            var t = x / (float)(n - 1);
            var c = Color.Lerp(
                new Color(mid.r, mid.g, mid.b, 0.5f),
                new Color(deep.r, deep.g, deep.b, 0.28f), t);
            for (var y = 0; y < n; y++)
                tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenDark(Color accent)
    {
        const int n = 64;
        var tex = NewTex(n);
        var c = Color.Lerp(accent, Color.black, 0.8f);
        c.a = 0.45f;
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                tex.SetPixel(x, y, c);
        tex.Apply();
        return tex;
    }
}
