namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Frozen <c>OpOneCore.playerCtrlMode</c> user choice, persisted for the Options screen.
    /// Keyboard is the revival default (ticket #179): WASD hull, mouse turrets.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public enum PlayerControlMode
    {
        Keyboard = 0,
        Mouse = 1
    }
}
