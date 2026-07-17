using System.Collections;
using TMPro;
using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Holographic answer cube with selection FX while the player covers its ArUco marker.
    /// </summary>
    public class AnswerCube3D : MonoBehaviour
    {
        [SerializeField] TMP_Text answerFront;
        [SerializeField] TMP_Text[] answerDepthLayers;
        [SerializeField] TMP_Text letterText;
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer chargeGlow;
        [SerializeField] SpriteRenderer selectRingA;
        [SerializeField] SpriteRenderer selectRingB;
        [SerializeField] SpriteRenderer selectBeam;
        [SerializeField] SpriteRenderer groundShadow;
        [SerializeField] Float3D floatMotion;
        [SerializeField] Color accentColor = Color.white;

        Vector3 _bodyBaseScale = Vector3.one;
        Color _answerBaseColor = Color.white;
        float _charge;
        float _smoothCharge;
        Coroutine _fx;
        int _selectPulseSeed;
        bool _selecting;

        public Color AccentColor => accentColor;
        public float Charge => _charge;

        public void Configure(TMP_Text front, TMP_Text[] depthLayers, TMP_Text letter, Transform bodyRoot,
            SpriteRenderer glow, SpriteRenderer ringA, SpriteRenderer ringB, SpriteRenderer beam,
            SpriteRenderer shadow, Float3D motion, Color accent)
        {
            answerFront = front;
            answerDepthLayers = depthLayers;
            letterText = letter;
            body = bodyRoot;
            chargeGlow = glow;
            selectRingA = ringA;
            selectRingB = ringB;
            selectBeam = beam;
            groundShadow = shadow;
            floatMotion = motion;
            accentColor = accent;
            if (answerFront != null)
                _answerBaseColor = answerFront.color;
        }

        void Awake()
        {
            if (body != null)
                _bodyBaseScale = body.localScale;
            HideSelectFx();
        }

        void OnEnable()
        {
            ResetState();
        }

        void LateUpdate()
        {
            if (groundShadow != null && floatMotion != null)
            {
                var k = 1f - floatMotion.NormalizedBob * 0.35f;
                groundShadow.transform.localScale = new Vector3(k, k * 0.35f, 1f);
                var c = groundShadow.color;
                c.a = 0.28f + 0.2f * k;
                groundShadow.color = c;
            }

            _smoothCharge = Mathf.Lerp(_smoothCharge, _charge, Time.deltaTime * 10f);
            if (_smoothCharge < 0.005f)
                _smoothCharge = 0f;

            if (!_selecting || _smoothCharge < 0.01f)
                return;

            var cVis = _smoothCharge;
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (4f + cVis * 2f) + _selectPulseSeed);
            var spin = Time.time * (45f + cVis * 35f);

            if (chargeGlow != null)
            {
                var c = chargeGlow.color;
                c.r = accentColor.r;
                c.g = accentColor.g;
                c.b = accentColor.b;
                c.a = Mathf.Lerp(0.08f, 0.55f, cVis) * (0.85f + 0.15f * pulse);
                chargeGlow.color = c;
                var s = 1f + 0.35f * cVis + 0.06f * pulse;
                chargeGlow.transform.localScale = new Vector3(s, s, 1f);
            }

            if (selectRingA != null)
            {
                selectRingA.color = new Color(accentColor.r, accentColor.g, accentColor.b,
                    Mathf.Lerp(0.05f, 0.65f, cVis));
                var rs = 0.92f + 0.18f * cVis + 0.04f * pulse;
                selectRingA.transform.localScale = new Vector3(rs, rs, 1f);
                selectRingA.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
            }

            if (selectRingB != null)
            {
                selectRingB.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.45f, cVis) * (0.9f + 0.1f * pulse));
                var rs = 0.78f + 0.14f * cVis;
                selectRingB.transform.localScale = new Vector3(rs, rs, 1f);
                selectRingB.transform.localRotation = Quaternion.Euler(0f, 0f, -spin * 0.85f);
            }

            if (selectBeam != null)
            {
                var bc = accentColor;
                bc.a = Mathf.Lerp(0f, 0.28f, cVis) * (0.85f + 0.15f * pulse);
                selectBeam.color = bc;
                selectBeam.transform.localScale = new Vector3(0.32f + 0.08f * cVis, 1.35f + 0.25f * cVis, 1f);
            }

            if (body != null && _fx == null)
            {
                var wobble = Mathf.Sin(Time.time * 6f + _selectPulseSeed) * 0.012f * cVis;
                var tilt = Mathf.Sin(Time.time * 5f + _selectPulseSeed * 0.7f) * 5f * cVis;
                body.localPosition = new Vector3(wobble, 0.02f * cVis + wobble * 0.3f, -0.01f * cVis);
                body.localRotation = Quaternion.Euler(tilt * 0.35f, tilt * 0.6f, -tilt * 0.2f);
                body.localScale = _bodyBaseScale * (1f + 0.1f * cVis + 0.03f * pulse * cVis);
            }

            TintAnswerText(Mathf.Lerp(0f, 0.5f, cVis));
        }

        public void SetAnswer(string text)
        {
            if (answerFront != null)
                answerFront.text = text ?? "";
            if (answerDepthLayers != null)
            {
                foreach (var layer in answerDepthLayers)
                {
                    if (layer != null)
                        layer.text = text ?? "";
                }
            }
        }

        public void SetCharge(float charge)
        {
            var prev = _charge;
            _charge = Mathf.Clamp01(charge);
            _selecting = _charge > 0.02f;

            if (floatMotion != null)
                floatMotion.SetIntensity(1f + _charge * 1.15f);

            if (_charge >= 0.98f && prev < 0.98f && _fx == null)
                _fx = StartCoroutine(SelectLockInRoutine());

            if (_charge < 0.05f)
                HideSelectFx();
        }

        public void BeginSelection(int pulseSeed)
        {
            _selectPulseSeed = pulseSeed;
            if (_fx == null)
                _fx = StartCoroutine(SelectKickRoutine());
        }

        public void PlaySelectStart()
        {
            BeginSelection(_selectPulseSeed);
        }

        public void ResetState()
        {
            StopFx();
            _charge = 0f;
            _smoothCharge = 0f;
            _selecting = false;
            if (body != null)
            {
                body.localScale = _bodyBaseScale;
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.identity;
            }
            HideSelectFx();
            SetTextAlpha(1f);
            TintAnswerText(0f);
            if (floatMotion != null)
                floatMotion.SetIntensity(1f);
        }

        public void PlayCorrect()
        {
            StopFx();
            HideSelectFx();
            _fx = StartCoroutine(CorrectRoutine());
        }

        public void PlayWrong()
        {
            StopFx();
            HideSelectFx();
            _fx = StartCoroutine(WrongRoutine());
        }

        public void SetDim(bool dim)
        {
            if (_selecting && dim)
                return;
            SetTextAlpha(dim ? 0.28f : 1f);
            if (body != null && _fx == null && !_selecting)
                body.localScale = _bodyBaseScale * (dim ? 0.82f : 1f);
        }

        void HideSelectFx()
        {
            if (chargeGlow != null)
                chargeGlow.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
            if (selectRingA != null)
                selectRingA.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
            if (selectRingB != null)
                selectRingB.color = new Color(1f, 1f, 1f, 0f);
            if (selectBeam != null)
                selectBeam.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
        }

        void TintAnswerText(float t)
        {
            if (answerFront == null)
                return;
            var hot = Color.Lerp(_answerBaseColor, Color.white, t);
            answerFront.color = hot;
            if (answerDepthLayers != null)
            {
                foreach (var layer in answerDepthLayers)
                {
                    if (layer != null)
                        layer.color = Color.Lerp(layer.color, Color.Lerp(accentColor, Color.black, 0.5f), t * 0.3f);
                }
            }
        }

        void StopFx()
        {
            if (_fx != null)
            {
                StopCoroutine(_fx);
                _fx = null;
            }
        }

        IEnumerator SelectKickRoutine()
        {
            const float dur = 0.42f;
            var t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var ease = SmoothStep(k);
                var pop = 1f + 0.1f * Mathf.Sin(ease * Mathf.PI);
                if (body != null)
                    body.localScale = _bodyBaseScale * pop;
                yield return null;
            }

            _fx = null;
        }

        IEnumerator SelectLockInRoutine()
        {
            const float dur = 0.32f;
            var t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var ease = SmoothStep(k);
                var pop = 1f + 0.12f * Mathf.Sin(ease * Mathf.PI);
                if (body != null)
                    body.localScale = _bodyBaseScale * pop;
                if (selectRingB != null)
                {
                    selectRingB.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.45f, 0.7f, ease));
                    selectRingB.transform.localScale = Vector3.one * (0.92f + ease * 0.08f);
                }
                yield return null;
            }

            _fx = null;
        }

        IEnumerator CorrectRoutine()
        {
            const float dur = 0.9f;
            var t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var jump = Mathf.Sin(k * Mathf.PI) * 0.14f;
                if (body != null)
                {
                    body.localPosition = new Vector3(0f, jump, 0f);
                    body.localRotation = Quaternion.Euler(0f, 360f * EaseOut(k), 0f);
                    body.localScale = _bodyBaseScale * (1f + 0.28f * Mathf.Sin(k * Mathf.PI));
                }
                yield return null;
            }

            if (body != null)
            {
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.identity;
                body.localScale = _bodyBaseScale;
            }
            _fx = null;
        }

        IEnumerator WrongRoutine()
        {
            const float dur = 0.55f;
            var t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var shake = Mathf.Sin(k * Mathf.PI * 7f) * 0.055f * (1f - k);
                if (body != null)
                    body.localPosition = new Vector3(shake, 0f, 0f);
                yield return null;
            }

            if (body != null)
                body.localPosition = Vector3.zero;
            SetTextAlpha(0.45f);
            _fx = null;
        }

        void SetTextAlpha(float a)
        {
            ApplyAlpha(answerFront, a);
            if (answerDepthLayers != null)
            {
                foreach (var layer in answerDepthLayers)
                    ApplyAlpha(layer, a);
            }
            ApplyAlpha(letterText, a);
        }

        static void ApplyAlpha(TMP_Text text, float a)
        {
            if (text == null)
                return;
            var c = text.color;
            c.a = a;
            text.color = c;
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        static float SmoothStep(float t) => t * t * (3f - 2f * t);
    }
}
