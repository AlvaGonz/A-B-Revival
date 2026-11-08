using ABRevival.Core.Simulation;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Projectile buffer seam owned by the scheduled simulation. Keeps the weapon system
    /// independent of <see cref="ScheduledCombatSimulation"/> and of job/native-array lifetimes.
    /// </summary>
    public interface IProjectileSpawner
    {
        int MaxProjectiles { get; }
        int ProjectileCount { get; set; }
        void SetProjectile(int index, in ProjectileData projectile);
        ProjectileData GetProjectile(int index);
    }

    /// <summary>
    /// Input seam for weapon discharge. The frozen baseline toggles autofire with key 'X';
    /// both manual fire and autofire drive the same fire timer.
    /// </summary>
    public interface ICombatFireInput
    {
        bool FireHeld { get; }
        bool Autofire { get; }
    }

    /// <summary>Receives one notification per destroyed hull, mapped back to its mission ship.</summary>
    public interface ICraftDestroyedSink
    {
        void CraftDestroyed(int shipIndex);
    }
}
