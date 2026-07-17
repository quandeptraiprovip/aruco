using TMPro;
using UnityEngine;

namespace ArucoQuiz
{
    [DisallowMultipleComponent]
    public class QuizTmpFontTag : MonoBehaviour
    {
        [SerializeField] bool useMonospace;
        TMP_Text _text;

        public bool UseMonospace => useMonospace;
        public TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        public void Configure(bool monospace)
        {
            useMonospace = monospace;
        }
    }
}
