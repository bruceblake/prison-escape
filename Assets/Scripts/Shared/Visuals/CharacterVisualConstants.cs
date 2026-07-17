namespace Prison.Visuals
{
    /// <summary>
    /// Shared sizing for characters relative to the prison environment.
    /// Cell doors are ~1.2 m wide — radius must stay under half that with margin.
    /// </summary>
    public static class CharacterVisualConstants
    {
        /// <summary>Was 1.3 (2.6 m tall / ~1.3 m wide) — too fat for 1.2 m barred doors.</summary>
        public const float VisualScale = 1.0f;
        public const float BaseHeight = 2f;

        public const float ColliderHeight = BaseHeight * VisualScale;
        /// <summary>~0.76 m diameter — clears 1.2 m cell doorways with margin.</summary>
        public const float ColliderRadius = 0.38f;
        public const float ColliderCenterY = ColliderHeight * 0.5f;
        /// <summary>First-person camera / guard eye line from the character root (feet).</summary>
        public const float EyeHeight = ColliderHeight * 0.83f;
        public const float CameraForwardOffset = 0.35f;
        public const float NameLabelHeight = 2.15f * VisualScale;
        public const float SocialLabelHeight = 1.75f * VisualScale;
    }
}
