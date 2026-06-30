using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>Center-top compliance + location. Uses <see cref="PrisonerController.GetCurrentLocationLabel"/> and <see cref="PrisonerController.IsCompliant"/>.</summary>
    public class ComplianceStatusHUD : MonoBehaviour
    {
        [Header("Wiring")]
        public TMP_Text statusText;
        public TMP_Text locationText;
        [Tooltip("Shown during travel grace and enforcement (optional).")]
        public TMP_Text goToText;

        [Header("Copy")]
        public string compliantText = "Status: Clear.";
        public string nonCompliantText = "Status: NON-COMPLIANT.";
        public string graceText = "Status: TRAVEL GRACE ({0}s)";
        public string goToFormat = "GO TO: {0}";

        [Header("Colors (defaults match tactical spec)")]
        public Color compliantColor; // = Concrete
        public Color nonCompliantColor; // = Hazard
        public Color graceColor = new Color(0.85f, 0.75f, 0.45f, 1f);

        [Header("Flashing (non-compliant)")]
        public float nonCompliantFlashHz = 2f;
        [Range(0.5f, 1f)] public float nonCompliantFlashMinAlpha = 0.55f;

        private PrisonerController _prisoner;
        private Image _bgPanel;
        private RectTransform _bgRect;
        private CanvasGroup _goToCanvasGroup;
        private bool _wasGoToActive;

        private void Reset()
        {
            compliantColor = PrisonUITheme.ConcreteGrey;
            nonCompliantColor = PrisonUITheme.HazardRed;
        }

        private void OnValidate()
        {
            if (compliantColor == default) compliantColor = PrisonUITheme.ConcreteGrey;
            if (nonCompliantColor == default) nonCompliantColor = PrisonUITheme.HazardRed;
        }

        private void Start()
        {
            if (compliantColor == default) compliantColor = PrisonUITheme.ConcreteGrey;
            if (nonCompliantColor == default) nonCompliantColor = PrisonUITheme.HazardRed;
            _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            
            // Programmatically inject modern UI background
            if (statusText != null && statusText.transform.parent != null)
            {
                var parent = statusText.transform.parent;
                _bgPanel = parent.GetComponent<Image>();
                if (_bgPanel == null)
                {
                    _bgPanel = parent.gameObject.AddComponent<Image>();
                    _bgPanel.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Sleek dark visor background
                }
                
                _bgRect = parent.GetComponent<RectTransform>();
                if (_bgRect != null)
                {
                    _bgRect.sizeDelta = new Vector2(500, 80);
                }
                
                statusText.fontStyle = TMPro.FontStyles.Bold;
                if (locationText != null) locationText.fontStyle = TMPro.FontStyles.Bold;
            }
            
            if (goToText != null)
            {
                _goToCanvasGroup = goToText.GetComponent<CanvasGroup>();
                if (_goToCanvasGroup == null) _goToCanvasGroup = goToText.gameObject.AddComponent<CanvasGroup>();
                _goToCanvasGroup.alpha = 0f;
                goToText.fontStyle = TMPro.FontStyles.Bold;
            }
        }

        private void Update()
        {
            if (_prisoner == null) _prisoner = FindFirstObjectByType<PrisonerController>(FindObjectsInactive.Exclude);
            if (PrisonTimeManager.Instance == null) return;
            if (_prisoner == null) return;

            var tm = PrisonTimeManager.Instance;
            bool grace = tm.IsMandatoryTravelGraceActive;
            bool enforcement = !grace && PrisonEventRules.IsMandatory(tm.CurrentEvent) && !_prisoner.IsCompliant;
            if (locationText != null)
                locationText.text = "Loc: " + PrisonRoutineLabels.FormatPlayerLocation(_prisoner.GetCurrentLocationLabel());

            string goTo = PrisonRoutineLabels.GetGoToLabel(tm.CurrentEvent, _prisoner.CellIndex);
            if (goToText != null)
            {
                bool showGoTo = grace || enforcement;
                
                if (showGoTo && !_wasGoToActive)
                {
                    // Slide down animation simulation
                    goToText.rectTransform.anchoredPosition = new Vector2(goToText.rectTransform.anchoredPosition.x, -20);
                }
                
                goToText.gameObject.SetActive(true); // Always active, handle visibility via alpha
                
                if (showGoTo)
                {
                    goToText.text = string.Format(goToFormat, goTo);
                    _goToCanvasGroup.alpha = Mathf.Lerp(_goToCanvasGroup.alpha, 1f, Time.deltaTime * 5f);
                    goToText.rectTransform.anchoredPosition = Vector2.Lerp(goToText.rectTransform.anchoredPosition, new Vector2(goToText.rectTransform.anchoredPosition.x, -40), Time.deltaTime * 5f);
                }
                else
                {
                    _goToCanvasGroup.alpha = Mathf.Lerp(_goToCanvasGroup.alpha, 0f, Time.deltaTime * 10f);
                }
                
                _wasGoToActive = showGoTo;
            }

            if (statusText == null) return;
            if (grace && !_prisoner.IsAtRequiredLocation)
            {
                int g = Mathf.CeilToInt(tm.ComplianceGraceSecondsRemaining);
                statusText.text = string.Format(graceText, g);
                statusText.color = graceColor;
                if (_bgPanel != null) _bgPanel.color = Color.Lerp(_bgPanel.color, new Color(0.2f, 0.15f, 0.05f, 0.9f), Time.deltaTime * 2f);
            }
            else if (_prisoner.IsAtRequiredLocation || _prisoner.IsCompliant)
            {
                statusText.text = compliantText;
                statusText.color = compliantColor;
                if (_bgPanel != null) _bgPanel.color = Color.Lerp(_bgPanel.color, new Color(0.05f, 0.05f, 0.08f, 0.85f), Time.deltaTime * 2f);
            }
            else
            {
                statusText.text = nonCompliantText;
                float a = 1f;
                if (nonCompliantFlashHz > 0.01f)
                {
                    a = (Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f) * nonCompliantFlashHz) * 0.5f) + 0.5f;
                    a = Mathf.Lerp(nonCompliantFlashMinAlpha, 1f, a);
                    var c = nonCompliantColor;
                    c.a = a;
                    statusText.color = c;
                }
                else
                    statusText.color = nonCompliantColor;
                    
                if (_bgPanel != null)
                {
                    // Pulse background red aggressively
                    _bgPanel.color = new Color(0.3f * a, 0.05f, 0.05f, 0.9f);
                }
            }
        }
    }
}
