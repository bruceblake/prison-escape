using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Prison;

public class PrisonSocialRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text affinityText;
    [SerializeField] private Image affinityFill;
    [SerializeField] private TMP_Text snitchHintText;

    public void SetRow(string displayName, float affinity, NPCPersonalityData personality = null)
    {
        if (nameText == null) nameText = GetComponentInChildren<TMP_Text>();
        if (nameText != null) nameText.text = displayName;
        if (affinityText != null) affinityText.text = affinity.ToString("F0");
        if (affinityFill != null) affinityFill.fillAmount = Mathf.Clamp01((affinity + 100f) / 200f);
        if (snitchHintText != null)
            snitchHintText.text = personality != null ? $"Snitch risk if below {personality.snitchThreshold:0}" : "";
    }
}
