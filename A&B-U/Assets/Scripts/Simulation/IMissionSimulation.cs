namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Minimal simulation surface a <see cref="MissionRunner"/> drives. Keeps the runner
    /// independent of whether the step runs on the pure <see cref="CombatSimulation"/> or on
    /// the Burst-scheduled presentation adapter (ADR 0014).
    /// </summary>
    public interface IMissionSimulation
    {
        bool IsInitialized { get; }
        int MaxShips { get; }
        int ShipCount { get; set; }

        void SetShip(int index, in ShipEntity ship);
        ShipEntity GetShip(int index);
        void Tick(float deltaTime);
    }
}
