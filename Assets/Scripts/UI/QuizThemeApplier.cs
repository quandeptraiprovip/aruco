using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArucoQuiz
{
    /// <summary>
    /// Recolors registered decorative graphics (borders, glows, backgrounds, labels) to match the
    /// active subject's theme. Semantic colors (correct/wrong, timer urgency, achievement gradients)
    /// and the fixed 3D answer-cube palette are never registered here.
    /// </summary>
    public class QuizThemeApplier : MonoBehaviour
    {
        [Serializable]
        struct Entry
        {
            public Graphic graphic;
            public float alpha;
            public bool accent2;
        }

        [SerializeField] Entry[] entries = Array.Empty<Entry>();
        [SerializeField] Image[] backgroundPanels = Array.Empty<Image>();
        [SerializeField] TMP_Text[] gradientTexts = Array.Empty<TMP_Text>();

        QuizSubject? _applied;

        public void Configure(List<(Graphic graphic, float alpha, bool accent2)> registrations, List<Image> backgrounds,
            List<TMP_Text> gradients)
        {
            entries = new Entry[registrations.Count];
            for (var i = 0; i < registrations.Count; i++)
            {
                entries[i] = new Entry
                {
                    graphic = registrations[i].graphic,
                    alpha = registrations[i].alpha,
                    accent2 = registrations[i].accent2,
                };
            }

            backgroundPanels = backgrounds.ToArray();
            gradientTexts = gradients.ToArray();
        }

        public void ApplyTheme(QuizSubject subject)
        {
            if (_applied == subject)
                return;
            _applied = subject;

            var theme = QuizTheme.Get(subject);

            foreach (var e in entries)
            {
                if (e.graphic == null)
                    continue;
                var c = e.accent2 ? theme.Accent2 : theme.Accent;
                e.graphic.color = new Color(c.r, c.g, c.b, e.alpha);
            }

            foreach (var bg in backgroundPanels)
            {
                if (bg == null)
                    continue;
                var alpha = bg.color.a;
                bg.color = new Color(theme.Background.r, theme.Background.g, theme.Background.b, alpha);
            }

            foreach (var tmp in gradientTexts)
            {
                if (tmp == null)
                    continue;
                tmp.colorGradient = new VertexGradient(theme.Accent, theme.Accent, theme.Accent2, theme.Accent2);
            }
        }
    }
}
