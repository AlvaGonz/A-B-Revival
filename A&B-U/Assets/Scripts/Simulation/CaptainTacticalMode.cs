namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Tactical AI modes for Captain Tom and fleet flagships established in ADR 0006:
    /// Anchor = 0, Escort = 1, Evade = 2, SeekAndDestroy = 3, FireSupport = 4, Shield = 5.
    /// </summary>
    public enum CaptainTacticalMode
    {
        Anchor = 0,
        Escort = 1,
        Evade = 2,
        SeekAndDestroy = 3,
        FireSupport = 4,
        Shield = 5
    }
}
