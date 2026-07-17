using TMPro;
using UnityEngine;

namespace ArucoQuiz
{
    public static class QuizUIFonts
    {
        public static TMP_FontAsset Baloo { get; private set; }
        public static TMP_FontAsset ShareTechMono { get; private set; }

        public static void Register(TMP_FontAsset baloo, TMP_FontAsset mono)
        {
            Baloo = baloo;
            ShareTechMono = mono;
        }

        public static void Apply(TMP_Text text, bool monospace = false)
        {
            if (text == null)
                return;
            var font = monospace ? ShareTechMono : Baloo;
            if (font != null)
                text.font = font;
        }
    }
}
