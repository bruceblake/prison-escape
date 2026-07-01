namespace Prison.Visuals
{
    /// <summary>
    /// Shared sizing for low-poly characters relative to the prison environment.
    /// Base rig is ~2m; VisualScale bumps characters to feel proportional to doors/props.
    /// </summary>
    public static class CharacterVisualConstants
    {
        public const float VisualScale = 1.3f;
        public const float BaseHeight = 2f;

        public const float ColliderHeight = BaseHeight * VisualScale;
        public const float ColliderRadius = 0.5f * VisualScale;
        public const float ColliderCenterY = ColliderHeight * 0.5f;
        /// <summary>First-person camera / guard eye line from the character root (feet).</summary>
        public const float EyeHeight = ColliderHeight * 0.83f;
        public const float CameraForwardOffset = 0.5f * VisualScale;
        public const float NameLabelHeight = 2.15f * VisualScale;
        public const float SocialLabelHeight = 1.75f * VisualScale;
    }
}
