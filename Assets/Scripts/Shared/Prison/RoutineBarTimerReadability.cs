using TMPro;
using UnityEngine;

namespace Prison
{
    /// <summary>
    /// Keeps the ghost timer on the progress bar stark white with a heavy dark outline so it reads on any fill color.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class RoutineBarTimerReadability : MonoBehaviour
    {
        [Header("Appearance")]
        public Color timerColor = Color.white;
        public Color outlineColor = new Color(0f, 0f, 0f, 0.92f);
        public Vector2 outlineOffset = new Vector2(2f, -2f);
        [Range(0f, 1f)] public float faceDilate = 0.35f;

        private TMP_Text _text;
        private Material _materialInstance;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            Apply();
        }

        private void OnEnable() => Apply();

        private void LateUpdate()
        {
            if (_text != null && _text.color != timerColor)
                Apply();
        }

        public void Apply()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();

            _text.color = timerColor;
            _text.fontStyle = FontStyles.Bold;
            _text.outlineWidth = 0.35f;
            _text.outlineColor = outlineColor;

            if (_text.fontMaterial == null)
                return;

            if (_materialInstance == null || _materialInstance.shader != _text.fontMaterial.shader)
                _materialInstance = new Material(_text.fontMaterial);

            _materialInstance.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            _materialInstance.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.35f);
            _materialInstance.SetFloat(ShaderUtilities.ID_FaceDilate, faceDilate);
            _text.fontMaterial = _materialInstance;
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
                Destroy(_materialInstance);
        }
    }
}
