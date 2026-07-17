using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates and persists the procedural sprites used by the quiz UI
/// (glows, grids, rounded rects, rings, stars, icons). Persisting them as PNG
/// assets keeps scene references valid after the editor session ends.
/// </summary>
public static class ArucoQuizSpriteAssets
{
    const string Folder = "Assets/Textures/Generated";

    public static Sprite Radial() => Load("radial_glow", GenRadial);
    public static Sprite GridTile() => Load("grid_tile", GenGridTile, tiled: true);
    public static Sprite ScanTile() => Load("scan_tile", GenScanTile, tiled: true);
    public static Sprite Vignette() => Load("vignette", GenVignette);
    public static Sprite VerticalFade() => Load("vertical_fade", GenVerticalFade);
    public static Sprite SideFadeLeft() => Load("side_fade_left", () => GenSideFade(true));
    public static Sprite SideFadeRight() => Load("side_fade_right", () => GenSideFade(false));
    public static Sprite Rounded() => Load("rounded_rect", GenRounded, border: 96, pixelsPerUnit: 400f);
    public static Sprite RoundedBorder() => Load("rounded_border", GenRoundedBorder, border: 96, pixelsPerUnit: 400f);
    public static Sprite Circle() => Load("circle", GenCircle);
    public static Sprite Ring() => Load("ring", GenRing);
    public static Sprite RingArc() => Load("ring_arc", GenRingArc);
    public static Sprite Star() => Load("star5", GenStar);
    public static Sprite Trophy() => Load("trophy", GenTrophy);
    public static Sprite Book() => Load("book", GenBook);
    public static Sprite Cross() => Load("cross", GenCross);
    public static Sprite Clock() => Load("clock", GenClock);
    public static Sprite Rocket() => Load("rocket", GenRocket);
    public static Sprite Restart() => Load("restart", GenRestart);
    public static Sprite HandCover() => Load("hand_cover", GenHandCover);
    public static Sprite CameraIcon() => Load("camera_icon", GenCamera);
    public static Sprite BookOpen() => Load("book_open", GenBookOpen);
    public static Sprite Target() => Load("target", GenTarget);
    public static Sprite Triangle() => Load("triangle", GenTriangle);
    public static Sprite Owl() => Load("owl", GenOwl);

    delegate Texture2D Painter();

    static Sprite Load(string name, Painter painter, bool tiled = false, int border = 0, float pixelsPerUnit = 100f)
    {
        Directory.CreateDirectory(Folder);
        var path = $"{Folder}/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        var tex = painter();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        if (border > 0)
            importer.spriteBorder = new Vector4(border, border, border, border);
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Texture2D NewTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color(1f, 1f, 1f, 0f);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);
        return tex;
    }

    static Texture2D GenRadial()
    {
        const int n = 512;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                var a = Mathf.Clamp01(1f - d);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenGridTile()
    {
        const int n = 80;
        var tex = NewTex(n, n);
        for (var i = 0; i < n; i++)
        {
            tex.SetPixel(i, n - 1, new Color(1f, 1f, 1f, 0.38f));
            tex.SetPixel(n - 1, i, new Color(1f, 1f, 1f, 0.38f));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenScanTile()
    {
        var tex = NewTex(4, 6);
        for (var x = 0; x < 4; x++)
            tex.SetPixel(x, 0, Color.white);
        tex.Apply();
        return tex;
    }

    static Texture2D GenVignette()
    {
        const int n = 512;
        var tex = NewTex(n, n);
        var cx = n * 0.5f;
        var cy = n * 0.45f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = (x - cx) / (n * 0.45f);
                var dy = (y - cy) / (n * 0.42f);
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.Clamp01((d - 0.2f) / 0.85f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenVerticalFade()
    {
        const int w = 4;
        const int h = 128;
        var tex = NewTex(w, h);
        for (var y = 0; y < h; y++)
        {
            var t = y / (float)(h - 1);
            var a = Mathf.Lerp(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.85f));
            for (var x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenSideFade(bool leftEdge)
    {
        const int w = 128;
        const int h = 4;
        var tex = NewTex(w, h);
        for (var x = 0; x < w; x++)
        {
            var t = x / (float)(w - 1);
            var a = leftEdge ? 1f - t : t;
            a *= a;
            for (var y = 0; y < h; y++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    static float RoundedRectAlpha(int x, int y, int n, float radius, float inset = 0f)
    {
        var half = n * 0.5f;
        var px = Mathf.Abs(x + 0.5f - half) - (half - radius - inset);
        var py = Mathf.Abs(y + 0.5f - half) - (half - radius - inset);
        var dx = Mathf.Max(px, 0f);
        var dy = Mathf.Max(py, 0f);
        var dist = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
        return Mathf.Clamp01(0.5f - dist);
    }

    static Texture2D GenRounded()
    {
        // 4x the old 64px canvas (with a matching 4x spritePixelsPerUnit in Load()) — same
        // rendered corner size, much more texel detail so corners stay crisp on large screens.
        const int n = 256;
        var tex = NewTex(n, n);
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, RoundedRectAlpha(x, y, n, 80f)));
        tex.Apply();
        return tex;
    }

    static Texture2D GenRoundedBorder()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var outer = RoundedRectAlpha(x, y, n, 80f);
                var inner = RoundedRectAlpha(x, y, n, 68f, 12f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner)));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenCircle()
    {
        const int n = 512;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(c - 1f - d)));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenRing()
    {
        const int n = 512;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        const float thickness = 6f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                var a = Mathf.Clamp01(thickness * 0.5f + 1f - Mathf.Abs(d - (c - 8f)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenRingArc()
    {
        // Ring with a bright 120° arc fading out — for orbital spinners.
        const int n = 512;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        const float thickness = 10f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = x + 0.5f - c;
                var dy = y + 0.5f - c;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var ring = Mathf.Clamp01(thickness * 0.5f + 1f - Mathf.Abs(d - (c - 10f)));
                if (ring <= 0f)
                    continue;
                var ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (ang < 0)
                    ang += 360f;
                var arc = ang <= 130f ? Mathf.Lerp(1f, 0.15f, ang / 130f) : 0.14f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, ring * arc));
            }
        }
        tex.Apply();
        return tex;
    }

    static bool InsideStar(float px, float py, float outer, float inner, int points = 5)
    {
        var ang = Mathf.Atan2(py, px) - Mathf.PI * 0.5f;
        var seg = Mathf.PI / points;
        var a = Mathf.Repeat(ang, 2f * seg);
        var t = Mathf.Abs(a - seg) / seg; // 1 at spike tip direction, 0 at valley
        var r = Mathf.Lerp(inner, outer, t);
        return Mathf.Sqrt(px * px + py * py) <= r;
    }

    static Texture2D GenStar()
    {
        const int n = 512;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                if (InsideStar(x + 0.5f - c, y + 0.5f - c, c - 12f, (c - 12f) * 0.5f))
                    tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return tex;
    }

    static void FillRect(Texture2D tex, float x0, float y0, float x1, float y1, Color c)
    {
        for (var y = Mathf.Max(0, (int)y0); y < Mathf.Min(tex.height, (int)y1); y++)
            for (var x = Mathf.Max(0, (int)x0); x < Mathf.Min(tex.width, (int)x1); x++)
                tex.SetPixel(x, y, c);
    }

    static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color c, bool onlyTopHalf = false, bool onlyBottomHalf = false)
    {
        for (var y = 0; y < tex.height; y++)
        {
            if (onlyTopHalf && y < cy) continue;
            if (onlyBottomHalf && y > cy) continue;
            for (var x = 0; x < tex.width; x++)
            {
                var dx = (x + 0.5f - cx) / rx;
                var dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    tex.SetPixel(x, y, c);
            }
        }
    }

    static void CutEllipse(Texture2D tex, float cx, float cy, float rx, float ry)
    {
        for (var y = 0; y < tex.height; y++)
        {
            for (var x = 0; x < tex.width; x++)
            {
                var dx = (x + 0.5f - cx) / rx;
                var dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    tex.SetPixel(x, y, Color.clear);
            }
        }
    }

    static Texture2D GenTrophy()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        // Cup bowl (upper half dome, open top)
        FillEllipse(tex, 128, 150, 62, 66, w, onlyBottomHalf: true);
        FillRect(tex, 66, 150, 190, 208, w);
        // Handles
        FillEllipse(tex, 52, 170, 26, 30, w);
        CutEllipse(tex, 52, 170, 14, 18);
        FillEllipse(tex, 204, 170, 26, 30, w);
        CutEllipse(tex, 204, 170, 14, 18);
        // Re-fill bowl over handle inner cuts overlap
        FillRect(tex, 66, 150, 190, 208, w);
        FillEllipse(tex, 128, 150, 62, 66, w, onlyBottomHalf: true);
        // Stem
        FillRect(tex, 116, 62, 140, 96, w);
        FillEllipse(tex, 128, 96, 34, 16, w);
        // Base
        FillRect(tex, 84, 40, 172, 60, w);
        FillRect(tex, 74, 30, 182, 44, w);
        tex.Apply();
        return tex;
    }

    static Texture2D GenBook()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        // Stack of 3 books
        FillRect(tex, 40, 40, 216, 84, w);
        FillRect(tex, 56, 96, 200, 140, w);
        FillRect(tex, 48, 152, 208, 196, w);
        // Spine notches (transparent gaps)
        FillRect(tex, 40, 84, 216, 96, Color.clear);
        FillRect(tex, 56, 140, 200, 152, Color.clear);
        tex.Apply();
        return tex;
    }

    static Texture2D GenCross()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        const float half = 88f;
        const float thick = 26f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = x + 0.5f - c;
                var dy = y + 0.5f - c;
                var u = (dx + dy) * 0.70710678f;
                var v = (dx - dy) * 0.70710678f;
                var inA = Mathf.Abs(u) <= thick && Mathf.Abs(v) <= half;
                var inB = Mathf.Abs(v) <= thick && Mathf.Abs(u) <= half;
                if (inA || inB)
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenClock()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        // Face ring
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                var a = Mathf.Clamp01(9f - Mathf.Abs(d - 96f));
                if (a > 0f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Min(1f, a)));
            }
        }
        // Hands: 12 o'clock + ~4 o'clock
        FillRect(tex, c - 7f, c, c + 7f, c + 66f, Color.white);
        for (var t = 0f; t <= 1f; t += 0.01f)
        {
            var x = c + t * 48f;
            var y = c - t * 34f;
            FillRect(tex, x - 7f, y - 7f, x + 7f, y + 7f, Color.white);
        }
        // Top bells
        FillRect(tex, c - 12f, c + 100f, c + 12f, c + 118f, Color.white);
        tex.Apply();
        return tex;
    }

    static Texture2D GenRocket()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        // Body (vertical capsule)
        FillEllipse(tex, 128, 150, 34, 76, w);
        // Nose window hole
        CutEllipse(tex, 128, 176, 13, 13);
        // Fins
        for (var t = 0f; t <= 1f; t += 0.01f)
        {
            var yy = 80f + t * 56f;
            var spread = 12f + (1f - t) * 30f;
            FillRect(tex, 128 - 34 - spread * (1f - t), yy, 128 - 20, yy + 4f, w);
            FillRect(tex, 128 + 20, yy, 128 + 34 + spread * (1f - t), yy + 4f, w);
        }
        // Flame
        FillEllipse(tex, 128, 58, 16, 26, w);
        tex.Apply();
        return tex;
    }

    static Texture2D GenRestart()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        // 300° arc ring
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = x + 0.5f - c;
                var dy = y + 0.5f - c;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                if (Mathf.Abs(d - 78f) > 14f)
                    continue;
                var ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (ang < 0)
                    ang += 360f;
                if (ang > 20f && ang < 80f)
                    continue; // gap for arrow
                tex.SetPixel(x, y, Color.white);
            }
        }
        // Arrow head at gap end
        for (var t = 0f; t <= 1f; t += 0.005f)
        {
            var size = 34f * (1f - t);
            var bx = c + Mathf.Cos(15f * Mathf.Deg2Rad) * 78f + t * 30f;
            var by = c + Mathf.Sin(15f * Mathf.Deg2Rad) * 78f + t * 16f;
            FillRect(tex, bx - size * 0.5f, by - size * 0.5f, bx + size * 0.5f, by + size * 0.5f, Color.white);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D GenHandCover()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        // Card (rotated square outline)
        for (var y = 40; y < 130; y++)
        {
            for (var x = 60; x < 196; x++)
            {
                var edge = x < 68 || x >= 188 || y < 48 || y >= 122;
                if (edge)
                    tex.SetPixel(x, y, w);
            }
        }
        // Palm covering
        FillEllipse(tex, 128, 150, 58, 44, w);
        // Fingers
        for (var f = 0; f < 4; f++)
        {
            var fx = 86f + f * 28f;
            FillEllipse(tex, fx, 196, 12, 34, w);
        }
        // Thumb
        FillEllipse(tex, 188, 158, 30, 13, w);
        tex.Apply();
        return tex;
    }

    static Texture2D GenCamera()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        FillRect(tex, 34, 70, 222, 186, w);
        FillRect(tex, 92, 186, 164, 210, w); // top hump
        CutEllipse(tex, 128, 128, 46, 46);
        FillEllipse(tex, 128, 128, 32, 32, w); // lens
        FillRect(tex, 188, 158, 210, 172, Color.clear); // flash dot cut
        tex.Apply();
        return tex;
    }

    static Texture2D GenBookOpen()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        for (var x = 0; x < 100; x++)
        {
            var t = x / 100f;
            var top = 170f - 26f * Mathf.Sin(t * Mathf.PI * 0.5f);
            FillRect(tex, 128 - 4 - x, 74, 128 - 2 - x, top, w);
            FillRect(tex, 128 + 2 + x, 74, 128 + 4 + x, top, w);
        }
        FillRect(tex, 124, 70, 132, 176, w); // spine
        tex.Apply();
        return tex;
    }

    static Texture2D GenTarget()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var c = n * 0.5f;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                var ring1 = Mathf.Abs(d - 100f) <= 10f;
                var ring2 = Mathf.Abs(d - 62f) <= 10f;
                var dot = d <= 26f;
                if (ring1 || ring2 || dot)
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return tex;
    }

    static float TriangleSign(Vector2 p1, Vector2 p2, Vector2 p3) =>
        (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    static bool PointInTriangle(Vector2 pt, Vector2 a, Vector2 b, Vector2 c)
    {
        var d1 = TriangleSign(pt, a, b);
        var d2 = TriangleSign(pt, b, c);
        var d3 = TriangleSign(pt, c, a);
        var hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        var hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    /// <summary>A simple upward-pointing triangle — geometry motif for Math, and doubles as a
    /// speech-bubble tail (rotated) for English.</summary>
    static Texture2D GenTriangle()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var apex = new Vector2(128, 214);
        var baseL = new Vector2(46, 46);
        var baseR = new Vector2(210, 46);
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                if (PointInTriangle(new Vector2(x + 0.5f, y + 0.5f), apex, baseL, baseR))
                    tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return tex;
    }

    /// <summary>A friendly reading-owl mascot — round body, ear tufts, and cutout eyes (so the dark
    /// scene background shows through as pupils when placed directly on it, no badge backing).</summary>
    static Texture2D GenOwl()
    {
        const int n = 256;
        var tex = NewTex(n, n);
        var w = Color.white;
        FillEllipse(tex, 86, 200, 20, 26, w); // left ear tuft
        FillEllipse(tex, 170, 200, 20, 26, w); // right ear tuft
        FillEllipse(tex, 128, 138, 82, 78, w); // body/head
        FillEllipse(tex, 108, 34, 15, 11, w); // left foot
        FillEllipse(tex, 148, 34, 15, 11, w); // right foot
        CutEllipse(tex, 96, 150, 24, 24); // left eye
        CutEllipse(tex, 160, 150, 24, 24); // right eye
        FillEllipse(tex, 128, 124, 11, 15, w); // beak
        tex.Apply();
        return tex;
    }
}
