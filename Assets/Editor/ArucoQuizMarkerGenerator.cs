using System.IO;
using ArucoQuiz;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates marker PNGs for DICT_4X4_50 IDs 0–3 (matches OpenCvArucoAnswerDetector).
/// </summary>
public static class ArucoQuizMarkerGenerator
{
    const string Folder = "Assets/Textures/ArucoMarkers";
    const int PixelSize = 512;

    [MenuItem("Aruco Quiz/Generate Marker Textures (IDs 0–3)", false, 15)]
    public static void GenerateMenu()
    {
        Generate();
        EditorUtility.DisplayDialog("Aruco Quiz",
            "Đã tạo marker_0.png … marker_3.png (DICT_4X4_50).\nA=0, B=1, C=2, D=3.",
            "OK");
    }

    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        var dict = Aruco.getPredefinedDictionary(Aruco.DICT_4X4_50);
        var img = new Mat();
        try
        {
            for (var id = ArucoMarkerConfig.FirstMarkerId;
                 id < ArucoMarkerConfig.FirstMarkerId + ArucoMarkerConfig.AnswerCount;
                 id++)
            {
                Aruco.drawMarker(dict, id, PixelSize, img, 1);
                var path = Path.Combine(Folder, $"marker_{id}.png");
                Imgcodecs.imwrite(path, img);
            }
        }
        finally
        {
            img.Dispose();
        }

        AssetDatabase.Refresh();
        for (var id = ArucoMarkerConfig.FirstMarkerId;
             id < ArucoMarkerConfig.FirstMarkerId + ArucoMarkerConfig.AnswerCount;
             id++)
        {
            var path = $"{Folder}/marker_{id}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
