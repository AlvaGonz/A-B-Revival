using System;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;
using Unity.Collections;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// One mounted part and its circular hitbox, in hull-local space.
    /// Registered once per hull; the solver copies it into persistent native state.
    /// </summary>
    public struct HullPartDefinition
    {
        public MountedPartData part;
        public float hitRadius;
    }

    /// <summary>
    /// Damage outcome of one projectile contact. Reported in projectile order.
    /// </summary>
    public struct ProjectileHit
    {
        public int projectileIndex;
        public int hullIndex;
        public int partIndex;
        public float damage;
        public float fraction;
        public float2 point;
        public float2 normal;
        public bool partDetached;
        public bool hullDestroyed;
    }

    /// <summary>
    /// Explicit hull-destruction result for one contact step.
    /// </summary>
    public struct HullDestroyedResult
    {
        public int hullIndex;
        public int factionId;
        public float2 position;
    }

    /// <summary>
    /// Aggregate outcome of one contact step.
    /// </summary>
    public struct ContactStepResult
    {
        public int hitCount;
        public int detachedPartCount;
        public int destroyedHullCount;
    }

    /// <summary>
    /// PhysicsCore2D contact layer for projectile flight (ADR 0012, ADR 0014).
    /// <para>
    /// Each projectile is swept as a world ray from its previous integrated position to its
    /// current one, so a 4000 px/s shot cannot tunnel through a 40 px hull at 60 Hz. Candidate
    /// hits are ordered nearest-first, filtered by faction and detachment, then applied to
    /// <see cref="MountedPartData"/> health. A part at zero health is detached (its shape stops
    /// participating in queries). Hull destruction is emitted as an explicit
    /// <see cref="HullDestroyedResult"/>.
    /// </para>
    /// <para>
    /// The solver never mutates <see cref="ProjectileData.lifetime"/> or
    /// <see cref="ProjectileData.distanceTraveled"/>: range and lifetime remain owned by
    /// <see cref="Ballistics.EffectiveRange"/> and <see cref="ProjectileMoveJob.Integrate"/>.
    /// All step state lives in persistent native buffers, so a step performs no managed allocation.
    /// </para>
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class ProjectileContactSolver : IDisposable
    {
        /// <summary>ADR 0017 / ticket #123 world standard: 100 px = 1 m.</summary>
        public const float ProjectLengthUnitsPerMeter = 100f;

        private readonly int _maxHulls;
        private readonly int _maxPartsPerHull;
        private readonly int _maxParts;
        private readonly int _maxProjectiles;

        private readonly PhysicsWorld _world;
        private readonly int _ownerKey;

        private NativeArray<PhysicsBody> _hullBodies;
        private NativeArray<int> _hullFactions;
        private NativeArray<float> _hullHealth;
        private NativeArray<int> _hullPartCounts;
        private NativeArray<bool> _hullDestroyed;

        private NativeArray<MountedPartData> _parts;
        private NativeArray<bool> _partMounted;
        private NativeArray<PhysicsShape> _partShapes;

        private NativeArray<BallisticHit> _candidates;
        private NativeArray<ProjectileHit> _hits;
        private NativeArray<HullDestroyedResult> _destroyedHulls;

        private int _hullCount;
        private int _hitCount;
        private int _destroyedHullCount;
        private bool _isDisposed;

        public int HullCount => _hullCount;
        public int HitCount => _hitCount;
        public int DestroyedHullCount => _destroyedHullCount;
        public ReadOnlySpan<ProjectileHit> Hits => _hits.AsSpan().Slice(0, _hitCount);
        public ReadOnlySpan<HullDestroyedResult> DestroyedHulls => _destroyedHulls.AsSpan().Slice(0, _destroyedHullCount);

        /// <summary>
        /// Length units per metre currently active in the world. Ticket #123 owns assigning the
        /// 100 px = 1 m settings asset; this layer only reports what the solver is running under.
        /// </summary>
        public float LengthUnitsPerMeter => PhysicsWorld.lengthUnitsPerMeter;

        /// <summary>False while the ticket #123 settings asset is missing and Unity defaults are active.</summary>
        public bool UsesProjectWorldScale => math.abs(LengthUnitsPerMeter - ProjectLengthUnitsPerMeter) < 0.001f;

        public ProjectileContactSolver(int maxHulls, int maxPartsPerHull, int maxProjectiles)
        {
            if (maxHulls <= 0) throw new ArgumentOutOfRangeException(nameof(maxHulls), "maxHulls must be greater than 0.");
            if (maxPartsPerHull <= 0) throw new ArgumentOutOfRangeException(nameof(maxPartsPerHull), "maxPartsPerHull must be greater than 0.");
            if (maxProjectiles <= 0) throw new ArgumentOutOfRangeException(nameof(maxProjectiles), "maxProjectiles must be greater than 0.");

            _maxHulls = maxHulls;
            _maxPartsPerHull = maxPartsPerHull;
            _maxParts = maxHulls * maxPartsPerHull;
            _maxProjectiles = maxProjectiles;

            _world = PhysicsWorld.Create();
            _ownerKey = _world.SetOwner(null);

            _hullBodies = new NativeArray<PhysicsBody>(maxHulls, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _hullFactions = new NativeArray<int>(maxHulls, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _hullHealth = new NativeArray<float>(maxHulls, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _hullPartCounts = new NativeArray<int>(maxHulls, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _hullDestroyed = new NativeArray<bool>(maxHulls, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _parts = new NativeArray<MountedPartData>(_maxParts, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _partMounted = new NativeArray<bool>(_maxParts, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _partShapes = new NativeArray<PhysicsShape>(_maxParts, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _candidates = new NativeArray<BallisticHit>(_maxParts, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _hits = new NativeArray<ProjectileHit>(maxProjectiles, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _destroyedHulls = new NativeArray<HullDestroyedResult>(maxHulls, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }

        /// <summary>
        /// Creates a static hull body. Parts and hitboxes are added with <see cref="SetHullParts"/>.
        /// </summary>
        public int RegisterHull(float2 position, float rotation, int factionId)
        {
            ThrowIfDisposed();
            if (_hullCount >= _maxHulls)
                throw new InvalidOperationException($"ProjectileContactSolver supports at most {_maxHulls} hulls.");
            if (factionId < 0 || factionId > 30)
                throw new ArgumentOutOfRangeException(nameof(factionId), "factionId must be between 0 and 30.");

            var definition = PhysicsBodyDefinition.defaultDefinition;
            definition.type = PhysicsBody.BodyType.Static;
            definition.position = new Vector2(position.x, position.y);
            definition.rotation = PhysicsRotate.FromRadians(rotation);
            definition.gravityScale = 0f;

            int hullIndex = _hullCount++;
            _hullBodies[hullIndex] = _world.CreateBody(definition);
            _hullFactions[hullIndex] = factionId;
            return hullIndex;
        }

        /// <summary>Moves a hull before a contact step. Main-thread call, like all physics object writes.</summary>
        public void SetHullPose(int hullIndex, float2 position, float rotation)
        {
            ThrowIfDisposed();
            ThrowIfHullOutOfRange(hullIndex);

            var body = _hullBodies[hullIndex];
            body.position = new Vector2(position.x, position.y);
            body.rotation = PhysicsRotate.FromRadians(rotation);
        }

        /// <summary>
        /// Copies the hull's mounted parts into native state and creates one circular hitbox per part.
        /// Called once per hull; registration is not a hot path.
        /// </summary>
        public void SetHullParts(int hullIndex, ReadOnlySpan<HullPartDefinition> parts)
        {
            ThrowIfDisposed();
            ThrowIfHullOutOfRange(hullIndex);

            if (_hullPartCounts[hullIndex] != 0)
                throw new InvalidOperationException("Hull parts are already configured.");
            if (parts.Length == 0)
                throw new ArgumentException("A hull must have at least one part.", nameof(parts));
            if (parts.Length > _maxPartsPerHull)
                throw new ArgumentException($"A hull supports at most {_maxPartsPerHull} parts.", nameof(parts));

            var body = _hullBodies[hullIndex];
            var shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            shapeDefinition.contactFilter = new PhysicsShape.ContactFilter(FactionCategory(_hullFactions[hullIndex]), PhysicsMask.All, 0);

            float totalHealth = 0f;
            for (int partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                var definition = parts[partIndex];
                if (definition.hitRadius <= 0f)
                    throw new ArgumentException("Every part must have a positive hitRadius.", nameof(parts));
                if (definition.part.health <= 0f)
                    throw new ArgumentException("Every part must have positive health.", nameof(parts));

                int globalPartIndex = ToGlobalPartIndex(hullIndex, partIndex);
                _parts[globalPartIndex] = definition.part;
                _partMounted[globalPartIndex] = true;
                totalHealth += definition.part.health;

                var geometry = CircleGeometry.Create(definition.hitRadius, new Vector2(definition.part.localOffset.x, definition.part.localOffset.y));
                var shape = body.CreateShape(geometry, shapeDefinition);
                var userData = shape.userData;
                userData.intValue = globalPartIndex;
                shape.userData = userData;

                _partShapes[globalPartIndex] = shape;
            }

            _hullHealth[hullIndex] = totalHealth;
            _hullPartCounts[hullIndex] = parts.Length;
        }

        /// <summary>
        /// Resolves one simulation step of projectile contacts.
        /// The projectile buffer must already have been integrated for <paramref name="deltaTime"/>.
        /// </summary>
        public ContactStepResult Step(Span<ProjectileData> projectiles, float deltaTime)
        {
            ThrowIfDisposed();
            if (projectiles.Length > _maxProjectiles)
                throw new ArgumentException($"ProjectileContactSolver supports at most {_maxProjectiles} projectiles per step.", nameof(projectiles));

            _hitCount = 0;
            _destroyedHullCount = 0;
            int detachedPartCount = 0;
            int destroyedHullCount = 0;

            for (int projectileIndex = 0; projectileIndex < projectiles.Length; projectileIndex++)
            {
                ref var projectile = ref projectiles[projectileIndex];
                if (!projectile.isAlive) continue;

                Ballistics.SweepSegment(in projectile, deltaTime, out float2 from, out float2 to);
                if (math.lengthsq(to - from) <= 1e-8f) continue;

                var input = PhysicsQuery.CastRayInput.FromTo(new Vector2(from.x, from.y), new Vector2(to.x, to.y));
                var results = _world.CastRay(input, PhysicsQuery.QueryFilter.Everything, PhysicsQuery.WorldCastMode.All, Allocator.Temp);

                int candidateCount = 0;
                for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
                {
                    var result = results[resultIndex];
                    if (!result.shape.isValid) continue;

                    int globalPartIndex = result.shape.userData.intValue;
                    if (globalPartIndex < 0 || globalPartIndex >= _maxParts) continue;
                    if (!_partMounted[globalPartIndex]) continue;

                    int hullIndex = globalPartIndex / _maxPartsPerHull;
                    if (hullIndex >= _hullCount) continue;
                    if (_hullFactions[hullIndex] == projectile.factionId) continue;

                    _candidates[candidateCount++] = new BallisticHit
                    {
                        targetIndex = globalPartIndex,
                        fraction = result.fraction,
                        point = new float2(result.point.x, result.point.y),
                        normal = new float2(result.normal.x, result.normal.y)
                    };
                }

                if (results.IsCreated) results.Dispose();
                if (candidateCount == 0) continue;

                Ballistics.OrderByFraction(_candidates.AsSpan().Slice(0, candidateCount));
                var nearest = _candidates[0];
                int nearestHull = nearest.targetIndex / _maxPartsPerHull;

                bool hullDestroyed = ApplyDamage(nearest.targetIndex, projectile.damage, out bool partDetached);
                if (partDetached) detachedPartCount++;
                if (hullDestroyed) destroyedHullCount++;

                _hits[_hitCount++] = new ProjectileHit
                {
                    projectileIndex = projectileIndex,
                    hullIndex = nearestHull,
                    partIndex = nearest.targetIndex - (nearestHull * _maxPartsPerHull),
                    damage = projectile.damage,
                    fraction = nearest.fraction,
                    point = nearest.point,
                    normal = nearest.normal,
                    partDetached = partDetached,
                    hullDestroyed = hullDestroyed
                };

                // A hit consumes the projectile; lifetime and distanceTraveled stay untouched.
                projectile.isAlive = false;
            }

            return new ContactStepResult
            {
                hitCount = _hitCount,
                detachedPartCount = detachedPartCount,
                destroyedHullCount = destroyedHullCount
            };
        }

        public MountedPartData GetPart(int hullIndex, int partIndex)
        {
            ThrowIfDisposed();
            ThrowIfHullOutOfRange(hullIndex);
            ThrowIfPartOutOfRange(partIndex);

            int globalPartIndex = ToGlobalPartIndex(hullIndex, partIndex);
            if (partIndex >= _hullPartCounts[hullIndex])
                throw new ArgumentOutOfRangeException(nameof(partIndex));

            return _parts[globalPartIndex];
        }

        public bool IsPartMounted(int hullIndex, int partIndex)
        {
            ThrowIfDisposed();
            ThrowIfHullOutOfRange(hullIndex);
            ThrowIfPartOutOfRange(partIndex);

            int globalPartIndex = ToGlobalPartIndex(hullIndex, partIndex);
            return partIndex < _hullPartCounts[hullIndex] && _partMounted[globalPartIndex];
        }

        public float GetHullHealth(int hullIndex)
        {
            ThrowIfDisposed();
            ThrowIfHullOutOfRange(hullIndex);
            return _hullHealth[hullIndex];
        }

        public bool IsHullDestroyed(int hullIndex)
        {
            ThrowIfDisposed();
            ThrowIfHullOutOfRange(hullIndex);
            return _hullDestroyed[hullIndex];
        }

        /// <summary>
        /// Applies damage to one mounted part. Returns true when this hit destroyed the hull.
        /// </summary>
        private bool ApplyDamage(int globalPartIndex, float damage, out bool partDetached)
        {
            int hullIndex = globalPartIndex / _maxPartsPerHull;
            var part = _parts[globalPartIndex];
            float remainingBefore = part.health;
            float appliedDamage = math.min(math.max(0f, damage), remainingBefore);

            part.health = remainingBefore - appliedDamage;
            _parts[globalPartIndex] = part;

            // Hull health is the sum of its mounted parts' remaining health, so every hit
            // reduces it, not only the hit that finishes a part.
            _hullHealth[hullIndex] = math.max(0f, _hullHealth[hullIndex] - appliedDamage);

            partDetached = false;
            if (part.health > 0f) return false;

            partDetached = true;
            _partMounted[globalPartIndex] = false;
            DisableShape(globalPartIndex);

            if (_hullHealth[hullIndex] > 0f || _hullDestroyed[hullIndex])
                return false;

            _hullDestroyed[hullIndex] = true;
            DisableHull(hullIndex);

            var body = _hullBodies[hullIndex];
            _destroyedHulls[_destroyedHullCount++] = new HullDestroyedResult
            {
                hullIndex = hullIndex,
                factionId = _hullFactions[hullIndex],
                position = new float2(body.position.x, body.position.y)
            };
            return true;
        }

        private void DisableHull(int hullIndex)
        {
            int firstPart = ToGlobalPartIndex(hullIndex, 0);
            int partCount = _hullPartCounts[hullIndex];
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                int globalPartIndex = firstPart + partIndex;
                if (!_partMounted[globalPartIndex]) continue;

                _partMounted[globalPartIndex] = false;
                DisableShape(globalPartIndex);
            }
        }

        private void DisableShape(int globalPartIndex)
        {
            var shape = _partShapes[globalPartIndex];
            if (!shape.isValid) return;

            var userData = shape.userData;
            userData.intValue = -1;
            shape.userData = userData;

            var contactFilter = shape.contactFilter;
            contactFilter.categories = PhysicsMask.None;
            shape.contactFilter = contactFilter;
        }

        private static PhysicsMask FactionCategory(int factionId)
        {
            return (PhysicsMask)(1UL << (factionId + 1));
        }

        private int ToGlobalPartIndex(int hullIndex, int partIndex)
        {
            return (hullIndex * _maxPartsPerHull) + partIndex;
        }

        private void ThrowIfHullOutOfRange(int hullIndex)
        {
            if (hullIndex < 0 || hullIndex >= _hullCount)
                throw new ArgumentOutOfRangeException(nameof(hullIndex));
        }

        private void ThrowIfPartOutOfRange(int partIndex)
        {
            if (partIndex < 0 || partIndex >= _maxPartsPerHull)
                throw new ArgumentOutOfRangeException(nameof(partIndex));
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ProjectileContactSolver));
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            if (_world.isValid) _world.Destroy(_ownerKey);

            if (_hullBodies.IsCreated) _hullBodies.Dispose();
            if (_hullFactions.IsCreated) _hullFactions.Dispose();
            if (_hullHealth.IsCreated) _hullHealth.Dispose();
            if (_hullPartCounts.IsCreated) _hullPartCounts.Dispose();
            if (_hullDestroyed.IsCreated) _hullDestroyed.Dispose();

            if (_parts.IsCreated) _parts.Dispose();
            if (_partMounted.IsCreated) _partMounted.Dispose();
            if (_partShapes.IsCreated) _partShapes.Dispose();

            if (_candidates.IsCreated) _candidates.Dispose();
            if (_hits.IsCreated) _hits.Dispose();
            if (_destroyedHulls.IsCreated) _destroyedHulls.Dispose();

            _isDisposed = true;
        }
    }
}
