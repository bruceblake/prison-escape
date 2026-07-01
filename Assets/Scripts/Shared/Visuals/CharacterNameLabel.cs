using TMPro;
using UnityEngine;

namespace Prison.Visuals
{
    /// <summary>
    /// World-space name tag that always faces the camera and stays readable.
    /// </summary>
    public class CharacterNameLabel : MonoBehaviour
    {
        [SerializeField] private string displayName = "Character";
        [SerializeField] private Vector3 localOffset = new Vector3(0f, CharacterVisualConstants.NameLabelHeight, 0f);
        [SerializeField] private float fontSize = 5f;
        [SerializeField] private float uniformScale = 0.06f;
        [SerializeField] private Color textColor = Color.white;

        private Transform _labelRoot;
        private TextMeshPro _text;

        public string DisplayName => displayName;

        public void SetDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            displayName = name.Trim();
            RefreshText();
        }

        public void ApplyScaledLayout()
        {
            localOffset = new Vector3(0f, CharacterVisualConstants.NameLabelHeight, 0f);
            if (_labelRoot != null)
                _labelRoot.localPosition = localOffset;
        }

        private void Awake()
        {
            EnsureLabel();
            RefreshText();
        }

        private void LateUpdate()
        {
            if (_labelRoot == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 toCamera = _labelRoot.position - cam.transform.position;
            if (toCamera.sqrMagnitude < 0.0001f)
                return;

            _labelRoot.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }

        private void EnsureLabel()
        {
            if (_text != null)
                return;

            _labelRoot = new GameObject("NameLabel").transform;
            _labelRoot.SetParent(transform, false);
            _labelRoot.localPosition = localOffset;
            _labelRoot.localRotation = Quaternion.identity;
            _labelRoot.localScale = Vector3.one * uniformScale;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_labelRoot, false);
            textGo.transform.localPosition = Vector3.zero;
            textGo.transform.localRotation = Quaternion.identity;
            textGo.transform.localScale = Vector3.one;

            _text = textGo.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _text.verticalAlignment = VerticalAlignmentOptions.Middle;
            _text.fontSize = fontSize;
            _text.fontStyle = FontStyles.Bold;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.raycastTarget = false;
            _text.outlineWidth = 0.25f;
            _text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        }

        private void RefreshText()
        {
            EnsureLabel();
            if (_text == null)
                return;

            _text.color = textColor;
            _text.text = displayName;
        }
    }
}
