using TMPro;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Applies Baloo / Share Tech Mono after play mode starts (avoids TMP console spam during Build Scene).
    /// Runs as early as possible (before any other script's Awake) so TMP never gets a chance to
    /// parse text with the default Liberation Sans font first.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class QuizUIFontBootstrap : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset balooFont;
        [SerializeField] TMP_FontAsset monoFont;

        void Awake()
        {
            QuizUIFonts.Register(balooFont, monoFont);

            foreach (var tag in GetComponentsInChildren<QuizTmpFontTag>(true))
            {
                var font = tag.UseMonospace ? monoFont : balooFont;
                if (font == null)
                    font = balooFont;
                if (font != null && tag.Text != null)
                    tag.Text.font = font;
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<QuizTmpFontTag>() != null)
                    continue;
                if (balooFont != null)
                    text.font = balooFont;
            }
        }
    }
}
