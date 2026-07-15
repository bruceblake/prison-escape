using UnityEngine;

namespace Prison
{
    /// <summary>Recommended palette (hex from design) for UI styling in Inspector / material setup.</summary>
    public static class PrisonUITheme
    {
        public static readonly Color CautionYellow = Hex("#F4D03F");
        public static readonly Color HazardRed = Hex("#C0392B");
        public static readonly Color ConcreteGrey = Hex("#95A5A6");
        public static readonly Color InkGreen = Hex("#5A8A50");
        public static readonly Color CommandStripBackdrop = new Color(0.04f, 0.06f, 0.08f, 0.88f);

        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#') return Color.white;
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return Color.white;
        }
    }
}
