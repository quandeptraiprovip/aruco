using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class ArucoQuizFontAssets
{
    const string MonoTtfPath = "Assets/Fonts/ShareTechMono-Regular.ttf";
    const string MonoSdfPath = "Assets/Fonts/ShareTechMono-Regular SDF.asset";

    static readonly string[] BalooTtfCandidates =
    {
        "Assets/Fonts/Baloo2-ExtraBold.ttf",
        "Assets/Fonts/Baloo2-VariableFont_wght.ttf",
        "Assets/Fonts/Baloo2-Variable.ttf",
    };

    static readonly string[] BalooSdfCandidates =
    {
        "Assets/Fonts/Baloo2-VariableFont_wght SDF.asset",
        "Assets/Fonts/Baloo2-Variable SDF.asset",
        "Assets/Fonts/Baloo2-ExtraBold SDF.asset",
    };

    /// <summary>
    /// Baloo: câu hỏi / UI tiếng Việt. Deliberately excludes ✦◈ and emoji — confirmed absent
    /// from the Baloo 2 TTF itself (not just the baked atlas), so declaring them here would only
    /// add a harmless-but-noisy "missing characters, ignored" warning at generation time. All
    /// icons/decorative glyphs are procedural sprites (ArucoQuizSpriteAssets) for this reason.
    /// </summary>
    const string BalooCharacterSet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        " .,!?-+*/=():\"'\n\r_…·—’" +
        "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ" +
        "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ";

    /// <summary>Share Tech Mono: Latin only (timer, điểm, badge — không có glyph tiếng Việt trong font gốc).</summary>
    const string MonoCharacterSet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        " .,!?-+*/=():\"'\n\r:/_…·";

    [MenuItem("Aruco Quiz/Generate Font Assets", false, 11)]
    public static void GenerateMenu()
    {
        EnsureTmpEssentials();
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog(
                "Aruco Quiz — Fonts",
                "Chưa có TMP Settings.\n\nVào Window → TextMesh Pro → Import TMP Essential Resources, rồi thử lại.",
                "OK");
            return;
        }

        DeleteBrokenSdfAssets();

        var balooTtf = ResolveBalooTtfPath();
        var balooOk = !string.IsNullOrEmpty(balooTtf) &&
                      GenerateStaticSdf(balooTtf, SdfPathForTtf(balooTtf), BalooCharacterSet);
        var monoOk = GenerateStaticSdf(MonoTtfPath, MonoSdfPath, MonoCharacterSet);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var anyUsable = LoadBaloo() != null || LoadMono() != null;
        EditorUtility.DisplayDialog(
            "Aruco Quiz — Fonts",
            anyUsable
                ? "Font SDF đã sẵn sàng trong Assets/Fonts/.\n\nTiếp theo: Aruco Quiz → Build Scene."
                : "Không tạo được font SDF (xem Console).\n\nVẫn có thể Build Scene — UI dùng Liberation Sans mặc định.",
            "OK");

        if (!balooOk)
            Debug.LogWarning(
                "[Aruco Quiz] Baloo: nếu variable font không load được, thêm Baloo2-ExtraBold.ttf (static) vào Assets/Fonts/ rồi Generate lại.");
        if (!monoOk)
            Debug.LogWarning($"[Aruco Quiz] Share Tech Mono: kiểm tra file {MonoTtfPath} và Include Font Data.");
    }

    /// <summary>
    /// Called automatically from the scene builder (unlike GenerateMenu, which is a manual,
    /// easy-to-forget step). Silently (re)generates whichever SDF font is missing or no longer
    /// covers every character in BalooCharacterSet/MonoCharacterSet — e.g. after that set gains
    /// new Vietnamese letters — so text never silently falls back to Liberation Sans and shows
    /// tofu boxes for glyphs the fallback font doesn't have either.
    /// </summary>
    public static void EnsureFonts()
    {
        EnsureTmpEssentials();
        if (TMP_Settings.instance == null)
            return;

        DeleteBrokenSdfAssets();

        var balooTtf = ResolveBalooTtfPath();
        if (!string.IsNullOrEmpty(balooTtf) && NeedsRegeneration(SdfPathForTtf(balooTtf), BalooCharacterSet))
            GenerateStaticSdf(balooTtf, SdfPathForTtf(balooTtf), BalooCharacterSet);

        if (NeedsRegeneration(MonoSdfPath, MonoCharacterSet))
            GenerateStaticSdf(MonoTtfPath, MonoSdfPath, MonoCharacterSet);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static bool NeedsRegeneration(string sdfPath, string requiredCharacters)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
        if (!IsFontAssignable(font))
            return true;

        foreach (var c in requiredCharacters)
        {
            if (!font.HasCharacter(c))
                return true;
        }

        return false;
    }

    public static void EnsureTmpEssentials()
    {
        if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            return;
        TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh();
    }

    public static TMP_FontAsset LoadBaloo()
    {
        foreach (var path in BalooSdfCandidates)
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (IsFontAssignable(f))
                return f;
        }

        return null;
    }

    public static TMP_FontAsset LoadMono()
    {
        var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoSdfPath);
        return IsFontAssignable(f) ? f : null;
    }

    public static bool IsFontAssignable(TMP_FontAsset font)
    {
        if (font == null || font.material == null)
            return false;

        try
        {
            var tex = font.atlasTexture;
            if (tex != null && tex.width > 0 && tex.height > 0)
                return true;

            return font.characterTable != null && font.characterTable.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    static string ResolveBalooTtfPath()
    {
        foreach (var path in BalooTtfCandidates)
        {
            if (AssetDatabase.LoadAssetAtPath<Font>(path) != null)
                return path;
        }

        Debug.LogWarning("[Aruco Quiz] Không tìm thấy font Baloo trong Assets/Fonts/ (Baloo2-VariableFont_wght.ttf hoặc Baloo2-Variable.ttf).");
        return null;
    }

    static string SdfPathForTtf(string ttfPath)
    {
        var dir = Path.GetDirectoryName(ttfPath)?.Replace('\\', '/');
        var name = Path.GetFileNameWithoutExtension(ttfPath);
        return $"{dir}/{name} SDF.asset";
    }

    static bool GenerateStaticSdf(string ttfPath, string sdfAssetPath, string characterSet)
    {
        EnsureFontImportIncludesData(ttfPath);
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogWarning($"[Aruco Quiz] Không tìm thấy font: {ttfPath}");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfAssetPath) != null)
            AssetDatabase.DeleteAsset(sdfAssetPath);

        const int samplingPointSize = 90;
        const int atlasPadding = 9;
        const int atlasSize = 2048;

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            samplingPointSize,
            atlasPadding,
            GlyphRenderMode.SDFAA,
            atlasSize,
            atlasSize,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            Debug.LogError($"[Aruco Quiz] TMP không load được font face: {ttfPath} (bật Include Font Data; variable font có thể cần bản static ExtraBold).");
            return false;
        }

        fontAsset.TryAddCharacters(characterSet, out var missing);

        if (!string.IsNullOrEmpty(missing))
            Debug.LogWarning($"[Aruco Quiz] Font {ttfPath} không có một số ký tự (bỏ qua): {missing}");

        var texBeforeSave = fontAsset.atlasTexture;
        if (texBeforeSave == null || texBeforeSave.width <= 0 || texBeforeSave.height <= 0)
        {
            Debug.LogError(
                $"[Aruco Quiz] Không nạp được glyph vào atlas cho {ttfPath} (kích thước {texBeforeSave?.width}x{texBeforeSave?.height}).");
            Object.DestroyImmediate(fontAsset);
            return false;
        }

        if (fontAsset.characterTable == null || fontAsset.characterTable.Count == 0)
        {
            Debug.LogError($"[Aruco Quiz] Bảng ký tự trống sau TryAddCharacters: {ttfPath}");
            Object.DestroyImmediate(fontAsset);
            return false;
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.creationSettings = new FontAssetCreationSettings
        {
            sourceFontFileGUID = AssetDatabase.AssetPathToGUID(ttfPath),
            pointSize = samplingPointSize,
            pointSizeSamplingMode = 0,
            padding = atlasPadding,
            packingMode = 0,
            atlasWidth = atlasSize,
            atlasHeight = atlasSize,
            characterSetSelectionMode = 7,
            characterSequence = characterSet,
            renderMode = (int)GlyphRenderMode.SDFAA,
        };

        AssetDatabase.CreateAsset(fontAsset, sdfAssetPath);

        var tex = fontAsset.atlasTexture;
        if (tex != null)
        {
            tex.name = Path.GetFileNameWithoutExtension(sdfAssetPath) + " Atlas";
            AssetDatabase.AddObjectToAsset(tex, fontAsset);
        }

        var mat = fontAsset.material;
        if (mat != null)
        {
            mat.name = (tex != null ? tex.name : "Font") + " Material";
            AssetDatabase.AddObjectToAsset(mat, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();

        var saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfAssetPath);
        if (!IsFontAssignable(saved))
        {
            Debug.LogError($"[Aruco Quiz] Font SDF tạo xong nhưng atlas trống: {sdfAssetPath}");
            return false;
        }

        Debug.Log($"[Aruco Quiz] Đã tạo font SDF: {sdfAssetPath}");
        return true;
    }

    static void EnsureFontImportIncludesData(string ttfPath)
    {
        var importer = AssetImporter.GetAtPath(ttfPath) as TrueTypeFontImporter;
        if (importer == null)
            return;
        if (importer.includeFontData)
            return;
        importer.includeFontData = true;
        importer.SaveAndReimport();
    }

    static void DeleteBrokenSdfAssets()
    {
        foreach (var path in BalooSdfCandidates)
            TryDeleteBrokenSdf(path);
        TryDeleteBrokenSdf(MonoSdfPath);
    }

    static void TryDeleteBrokenSdf(string assetPath)
    {
        var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (f == null)
            return;
        if (IsFontAssignable(f))
            return;
        AssetDatabase.DeleteAsset(assetPath);
        Debug.Log($"[Aruco Quiz] Đã xóa font SDF hỏng: {assetPath}");
    }
}
