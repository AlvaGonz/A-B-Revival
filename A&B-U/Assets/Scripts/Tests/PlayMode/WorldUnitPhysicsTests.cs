using NUnit.Framework;
using Unity.U2D.Physics;
using UnityEngine;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Measures the PhysicsCore2D world-unit baseline (ADR 0017): one world unit is one stage pixel,
    /// lengthUnitsPerMeter is 100, and definition values are not scaled a second time by the solver.
    /// Runs in the Tier 2 PlayMode gate only.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class WorldUnitPhysicsTests
    {
        private const float FixedStep = 1f / 60f;
        private const float BodyRadius = 10f;
        private const float DropHeight = 540f;

        private static PhysicsWorld CreateScriptedWorld()
        {
            var definition = PhysicsWorldDefinition.defaultDefinition;
            definition.simulationType = PhysicsWorld.SimulationType.Script;
            return PhysicsWorld.Create(definition);
        }

        [Test]
        public void ProjectSettings_AssignPixelLengthUnits()
        {
            PhysicsWorld world = CreateScriptedWorld();
            try
            {
                Assert.IsTrue(world.isValid, "PhysicsCore2D world could not be created.");
                Assert.AreEqual(100f, PhysicsWorld.lengthUnitsPerMeter, 0.0001f,
                    "PhysicsCoreSettings2D is not assigned, or lengthUnitsPerMeter is not 100.");
            }
            finally
            {
                world.Destroy();
            }
        }

        [Test]
        public void BodyDroppedFrom540Pixels_LandsInAboutOneSecond()
        {
            PhysicsWorld world = CreateScriptedWorld();
            Assert.IsTrue(world.isValid, "PhysicsCore2D world could not be created.");
            try
            {
                PhysicsBody ground = world.CreateBody(new PhysicsBodyDefinition
                {
                    type = PhysicsBody.BodyType.Static,
                    position = new Vector2(0f, -100f),
                    enabled = true,
                });
                ground.CreateShape(PolygonGeometry.CreateBox(new Vector2(4000f, 200f)));

                PhysicsBody body = world.CreateBody(new PhysicsBodyDefinition
                {
                    type = PhysicsBody.BodyType.Dynamic,
                    position = new Vector2(0f, DropHeight),
                    gravityScale = 1f,
                    enabled = true,
                    awake = true,
                });
                body.CreateShape(new CircleGeometry { radius = BodyRadius }, new PhysicsShapeDefinition
                {
                    density = 1f,
                    surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 0.6f },
                });

                float elapsed = 0f;
                while (body.position.y > BodyRadius && elapsed < 3f)
                {
                    world.Simulate(FixedStep);
                    elapsed += FixedStep;
                }

                Assert.Less(elapsed, 3f, "Body never landed: gravity is probably scaled a second time by lengthUnitsPerMeter.");
                Assert.That(elapsed, Is.InRange(0.95f, 1.15f),
                    $"540 px fall at gravity -981 px/s^2 took {elapsed:F3} s; expected about 1.05 s.");
            }
            finally
            {
                world.Destroy();
            }
        }

        [Test]
        public void BodySpawnedAt1000PixelsPerSecond_IsNotClamped()
        {
            PhysicsWorld world = CreateScriptedWorld();
            Assert.IsTrue(world.isValid, "PhysicsCore2D world could not be created.");
            try
            {
                PhysicsBody body = world.CreateBody(new PhysicsBodyDefinition
                {
                    type = PhysicsBody.BodyType.Dynamic,
                    position = Vector2.zero,
                    linearVelocity = new Vector2(1000f, 0f),
                    gravityScale = 0f,
                    enabled = true,
                    awake = true,
                });
                body.CreateShape(new CircleGeometry { radius = BodyRadius }, new PhysicsShapeDefinition
                {
                    density = 1f,
                });

                world.Simulate(FixedStep);

                Assert.AreEqual(1000f, body.linearVelocity.x, 0.01f,
                    "A 1000 px/s body was clamped; maximumLinearSpeed is not scaled to pixels.");
            }
            finally
            {
                world.Destroy();
            }
        }
    }
}
