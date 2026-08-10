using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Lerps a numeric TMP display and/or Image fill toward a target; punch-scales
    /// (~1.15 → 1 over 150ms) on change. Unscaled time — safe behind non-blocking pause.</summary>
    public sealed class ValueAnimator : MonoBehaviour
    {
        [SerializeField] TMP_Text _label;
        [SerializeField, OptionalBinding] Image _fill;
        [SerializeField] RectTransform _punchTarget;
        [SerializeField] float _lerpSpeed = 8f;
        [SerializeField] float _punchScale = 1.15f;
        [SerializeField] float _punchSettle = 0.15f;
        [SerializeField] string _format = "0";
        [SerializeField] string _prefix = "";
        [SerializeField] string _suffix = "";
        [SerializeField] bool _writeValueOverMax;

        float _display;
        float _target;
        float _max = 1f;
        float _punchT = 1f;
        bool _hasValue;
        bool _settled;

        void Awake()
        {
            if (_label == null) _label = GetComponent<TMP_Text>();
            if (_punchTarget == null) _punchTarget = transform as RectTransform;
        }

        void OnEnable()
        {
            _punchT = 1f;
            ApplyScale(1f);
        }

        /// <summary>Snap display and target without punch.</summary>
        public void SetImmediate(float value)
        {
            _target = value;
            _display = value;
            _hasValue = true;
            _punchT = 1f;
            _settled = false;
            ApplyScale(1f);
            WriteVisuals();
        }

        /// <summary>Animate toward value; punch-scale when the target changes.
        /// Early-out (M35 criterion 27): an unchanged target does no per-frame work.</summary>
        public void SetValue(float value)
        {
            bool changed = !_hasValue || !Mathf.Approximately(_target, value);
            _target = value;
            if (!_hasValue)
            {
                _display = value;
                _hasValue = true;
            }
            if (changed)
            {
                _punchT = 0f;
                _settled = false;
            }
        }

        /// <summary>Bar lerp: fillAmount = value/max, optional "v / m" label.</summary>
        public void SetBar(float value, float max)
        {
            _max = max > 0f ? max : 1f;
            _writeValueOverMax = _label != null;
            SetValue(Mathf.Clamp(value, 0f, _max));
        }

        public void SetBarImmediate(float value, float max)
        {
            _max = max > 0f ? max : 1f;
            _writeValueOverMax = _label != null;
            SetImmediate(Mathf.Clamp(value, 0f, _max));
        }

        public void SetFillColor(Color color)
        {
            if (_fill != null) _fill.color = color;
        }

        /// <summary>Punch without changing the numeric target (string-driven readouts).</summary>
        public void Punch()
        {
            _punchT = 0f;
            _settled = false;
        }

        void Update()
        {
            // Early-out when the display already equals the target and the punch is done —
            // a steady HUD value costs nothing per frame (M35 criterion 27).
            if (_settled && _punchT >= 1f) return;
            if (!_hasValue && _punchT >= 1f) return;

            float dt = Time.unscaledDeltaTime;
            if (_hasValue)
            {
                _display = Mathf.Lerp(_display, _target, 1f - Mathf.Exp(-_lerpSpeed * dt));
                if (Mathf.Abs(_display - _target) < 0.01f)
                    _display = _target;
            }

            if (_punchT < 1f)
            {
                _punchT = _punchSettle <= 0f
                    ? 1f
                    : Mathf.Clamp01(_punchT + dt / _punchSettle);
                float s = Mathf.Lerp(_punchScale, 1f, EaseOutCubic(_punchT));
                ApplyScale(s);
            }

            if (_hasValue)
            {
                WriteVisuals();
                if (_display == _target && _punchT >= 1f)
                    _settled = true;
            }
        }

        void WriteVisuals()
        {
            if (_fill != null)
                _fill.fillAmount = Mathf.Clamp01(_display / _max);

            if (_label == null) return;
            if (_writeValueOverMax)
                _label.SetText("{0} / {1}", Mathf.Round(_display), Mathf.Round(_max)); // TMP buffer, no string alloc
            else
                _label.text = _prefix + _display.ToString(_format) + _suffix; // runs only while animating (early-out above)
        }

        void ApplyScale(float s)
        {
            if (_punchTarget != null)
                _punchTarget.localScale = new Vector3(s, s, 1f);
        }

        static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }
    }
}
