using UnityEngine;

namespace Prison.Social
{
    /// <summary>Band colors shared by nameplates, the Talk Menu, and the notebook dossier.</summary>
    public static class StandingBandUI
    {
        public static readonly Color Enemy = new Color(0.85f, 0.18f, 0.14f);
        public static readonly Color Hostile = new Color(0.93f, 0.53f, 0.18f);
        public static readonly Color Neutral = Color.white;
        public static readonly Color Friendly = new Color(0.62f, 0.85f, 0.5f);
        public static readonly Color Ally = new Color(0.35f, 0.82f, 0.35f);
        public static readonly Color Confidant = new Color(0.2f, 0.95f, 0.45f);

        public static Color ColorOf(StandingBand band)
        {
            switch (band)
            {
                case StandingBand.Enemy: return Enemy;
                case StandingBand.Hostile: return Hostile;
                case StandingBand.Friendly: return Friendly;
                case StandingBand.Ally: return Ally;
                case StandingBand.Confidant: return Confidant;
                default: return Neutral;
            }
        }

        public static string Label(StandingBand band) => band.ToString();
    }
}
