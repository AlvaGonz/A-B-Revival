namespace ABRevival.Core.Hud
{
    /// <summary>
    /// Canonical cumulative lost-part buckets shown by the combat HUD (ADR 0004).
    /// The ship chassis is intentionally absent: hull loss destroys the craft.
    /// </summary>
    public enum BattleDamageCategory
    {
        Gun = 0,
        Wing = 1,
        Engine = 2,
        Module = 3
    }
}
