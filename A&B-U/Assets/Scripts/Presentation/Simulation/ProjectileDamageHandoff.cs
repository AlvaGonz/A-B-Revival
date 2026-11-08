using System;
using ABRevival.Core.Hud;
using ABRevival.Core.Simulation;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Translates one <see cref="ProjectileContactSolver"/> step into gameplay consequences:
    /// lost-part telemetry for the player hull and exactly one destruction notification per
    /// hull, mapped back to its mission ship. All buffers are owned by the caller and sized
    /// once, so <see cref="Apply"/> performs no managed allocation.
    /// </summary>
    public sealed class ProjectileDamageHandoff
    {
        private readonly int[] _hullToShip;
        private readonly bool[] _reportedDestroyed;
        private readonly int _playerHull;
        private readonly float _playerHullMax;

        /// <param name="hullToShip">
        /// Hull index -> simulation ship index map. The array is shared with the caller so hulls
        /// registered after construction are visible; entries without a ship stay -1.
        /// </param>
        public ProjectileDamageHandoff(int[] hullToShip, int playerHull, float playerHullMax)
        {
            _hullToShip = hullToShip ?? throw new ArgumentNullException(nameof(hullToShip));
            _reportedDestroyed = new bool[hullToShip.Length];
            _playerHull = playerHull;
            _playerHullMax = playerHullMax;
        }

        public int PlayerHull => _playerHull;

        /// <summary>
        /// Applies contact results and returns the number of newly destroyed hulls reported.
        /// Replaying the same step is idempotent.
        /// </summary>
        public int Apply(
            ProjectileContactSolver solver,
            CombatHudState playerHud,
            ICraftDestroyedSink sink)
        {
            if (solver == null) return 0;

            if (playerHud != null && _playerHull >= 0 && _playerHull < solver.HullCount)
            {
                var hits = solver.Hits;
                for (int i = 0; i < hits.Length; i++)
                {
                    ProjectileHit hit = hits[i];
                    if (hit.hullIndex != _playerHull || !hit.partDetached) continue;
                    playerHud.RecordLostPart(solver.GetPart(hit.hullIndex, hit.partIndex).category);
                }

                playerHud.SetHullHealth(solver.GetHullHealth(_playerHull), _playerHullMax);
            }

            int reported = 0;
            var destroyed = solver.DestroyedHulls;
            for (int i = 0; i < destroyed.Length; i++)
            {
                int hullIndex = destroyed[i].hullIndex;
                if (hullIndex < 0 || hullIndex >= _reportedDestroyed.Length) continue;
                if (_reportedDestroyed[hullIndex]) continue;

                _reportedDestroyed[hullIndex] = true;
                reported++;

                if (hullIndex >= _hullToShip.Length) continue;
                int shipIndex = _hullToShip[hullIndex];
                if (shipIndex < 0) continue;
                sink?.CraftDestroyed(shipIndex);
            }

            return reported;
        }
    }
}
