using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>Per-subject palette + labels (mirrors the HTML mockup's THEMES object).</summary>
    public readonly struct QuizTheme
    {
        public readonly Color Accent;
        public readonly Color Accent2;
        public readonly Color Background;
        public readonly string Name;
        public readonly string ShortName;
        public readonly string IconGlyph;

        public QuizTheme(Color accent, Color accent2, Color background, string name, string shortName, string iconGlyph)
        {
            Accent = accent;
            Accent2 = accent2;
            Background = background;
            Name = name;
            ShortName = shortName;
            IconGlyph = iconGlyph;
        }

        static readonly QuizTheme MathTheme = new QuizTheme(
            new Color(0f, 0.898f, 1f),
            new Color(0.533f, 0.2f, 1f),
            new Color(0.055f, 0f, 0.208f),
            "TOÁN HỌC", "TOÁN", "123");

        // Background was previously (0.043, 0.192, 0.251) — a mid-teal whose luma (~0.15) was
        // roughly 4x brighter than Math's near-black (~0.04), so every neon glow/star/text
        // element had far less contrast headroom and the whole scene read washed out next to
        // Math's rich near-black starfield. Darkened to match Math's luma; hue now carries the
        // whole "this is English" identity instead of overall brightness.
        static readonly QuizTheme EnglishTheme = new QuizTheme(
            new Color(0.176f, 0.831f, 0.749f),
            new Color(1f, 0.478f, 0.42f),
            new Color(0.02f, 0.05f, 0.075f),
            "TIẾNG ANH", "TIẾNG ANH", "Aa");

        public static QuizTheme Get(QuizSubject subject) => subject == QuizSubject.Math ? MathTheme : EnglishTheme;
    }
}
