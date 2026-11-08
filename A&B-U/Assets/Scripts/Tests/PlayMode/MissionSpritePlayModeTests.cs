using System;
using System.Collections;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Submits a populated mission frame through the real BRG path and verifies the
    /// zero-GameObject contract, bounded batches, per-part damage tints and part
    /// detachment. Uses the committed UV table so the runtime parser is exercised too.
    /// </summary>
    [TestFixture]
    public class MissionSpritePlayModeTests
    {
        private const float DeltaTime = 1f / 60f;
        private const float FastVelocity = 12000f;

        [Test]
        public void PopulatedMissionFrame_SubmitsBoundedBatches_WithoutPerInstanceGameObjects()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("BatchRendererGroup requires a graphics device; skipped in -nographics runs.");

            SpriteUvTable table = LoadCommittedTable();
            var simulation = new ScheduledCombatSimulation();
            var solver = new ProjectileContactSolver(maxHulls: 4, maxPartsPerHull: 4, maxProjectiles: 8);
            var go = new GameObject("mission-sprite-view-test");
            BatchSpriteRenderer renderer = null;

            try
            {
                simulation.Init(maxShips: 4, maxProjectiles: 8);
                simulation.PlayerIndex = 0;
                simulation.ShipCount = 3;
                simulation.SetShip(0, Ship(-100f, 0f, factionId: 0));
                simulation.SetShip(1, Ship(500f, 40f, factionId: 1));
                simulation.SetShip(2, Ship(-600f, 120f, factionId: 0));

                Span<HullPartDefinition> playerParts = stackalloc HullPartDefinition[2];
                playerParts[0] = Part(partId: 0, health: 1000f, offset: float2.zero, radius: 32f);
                playerParts[1] = Part(partId: 71, health: 100f, offset: new float2(0f, -80f), radius: 20f);
                int playerHull = solver.RegisterHull(new float2(-100f, 0f), 0f, factionId: 0);
                solver.SetHullParts(playerHull, playerParts);

                Span<HullPartDefinition> hostileParts = stackalloc HullPartDefinition[1];
                hostileParts[0] = Part(partId: 0, health: 900f, offset: float2.zero, radius: 32f);
                int hostileHull = solver.RegisterHull(new float2(500f, 40f), 0f, factionId: 1);
                solver.SetHullParts(hostileHull, hostileParts);

                Span<HullPartDefinition> captainParts = stackalloc HullPartDefinition[1];
                captainParts[0] = Part(partId: 1, health: 1200f, offset: float2.zero, radius: 32f);
                int captainHull = solver.RegisterHull(new float2(-600f, 120f), 0f, factionId: 0);
                solver.SetHullParts(captainHull, captainParts);

                simulation.SetProjectile(0, new ProjectileData
                {
                    position = new float2(0f, 300f),
                    velocity = new float2(0f, -900f),
                    lifetime = 2f,
                    damage = 10f,
                    factionId = 0,
                    isAlive = true
                });
                simulation.ProjectileCount = 1;

                var shipToHull = new[] { playerHull, hostileHull, captainHull };

                renderer = go.AddComponent<BatchSpriteRenderer>();
                var view = go.AddComponent<MissionSpriteView>();
                view.Initialize(table, null, capacity: 64);
                Assert.IsTrue(
                    view.BeginMission(simulation, solver, shipToHull, playerIndex: 0, capacity: 64),
                    "The mission sprite view must initialize against the BRG.");

                // Two player parts + hostile body + Captain hull + one projectile.
                Assert.AreEqual(5, view.InstanceCount);
                Assert.AreEqual(view.InstanceCount, renderer.InstanceCount);
                Assert.AreEqual(SpriteBatchPlan.BatchCount(view.InstanceCount), view.BatchCount);
                Assert.LessOrEqual(view.BatchCount, SpriteBatchPlan.BatchCount(64));
                Assert.AreEqual(0, go.transform.childCount, "Instances must not create GameObjects.");
                AssertShipInstancesPristine(renderer, view.InstanceCount - 1);
                Assert.AreNotEqual(SpriteUserValue.Pristine, renderer.GetUserValue(view.InstanceCount - 1),
                    "The live projectile must carry its faction tint.");

                // 25 damage leaves the gun mounted but tints it (unity_RendererUserValue).
                StepProjectile(solver, new float2(-100f, -200f), damage: 25f);
                view.Render();
                Assert.AreEqual(5, view.InstanceCount, "A damaged but mounted part must stay visible.");
                Assert.IsTrue(HasTintedInstance(renderer, view.InstanceCount - 1),
                    "A damaged part must carry a non-pristine tint.");

                // 200 damage detaches the gun: its instance disappears from the same frame.
                StepProjectile(solver, new float2(-100f, -200f), damage: 200f);
                Assert.IsFalse(solver.IsPartMounted(playerHull, 1), "The gun must be detached.");
                Assert.AreEqual(2, solver.GetHullPartCount(playerHull), "Detached parts stay in hull part data.");

                view.Render();
                Assert.AreEqual(4, view.InstanceCount, "A detached part must not submit an instance.");
                Assert.AreEqual(SpriteBatchPlan.BatchCount(view.InstanceCount), view.BatchCount);
                Assert.AreEqual(0, go.transform.childCount);

                // Warm the upload path, then assert the frame loop stays off the managed heap.
                for (int i = 0; i < 5; i++) view.Render();
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 30; i++) view.Render();
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                Assert.AreEqual(0, allocatedBytes,
                    $"Mission sprite rendering allocated {allocatedBytes} bytes over 30 frames.");
            }
            finally
            {
                if (renderer != null) renderer.enabled = false;
                UnityEngine.Object.DestroyImmediate(go);
                solver.Dispose();
                simulation.Dispose();
            }
        }

        [Test]
        public void UvTable_CommittedAssetParsesWithEveryPartId()
        {
            SpriteUvTable table = LoadCommittedTable();
            Assert.AreEqual(271, table.PartCount);
            Assert.AreEqual(223, table.SymbolCount);
            Assert.IsTrue(table.HasWhite);

            for (int partId = 0; partId < 271; partId++)
                Assert.IsTrue(table.TryGetPart(partId, out _), $"Part {partId} is missing from the UV table.");
        }

        [UnityTest]
        public IEnumerator PopulatedFrame_DrawsAtlasPixelsThroughTheUrp2DRenderer()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Rendering requires a graphics device; skipped in -nographics runs.");

            SpriteUvTable table = LoadCommittedTable();
            var simulation = new ScheduledCombatSimulation();
            var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 1, maxProjectiles: 1);
            var viewObject = new GameObject("mission-sprite-render-test");
            var cameraObject = new GameObject("mission-sprite-render-camera");
            var renderTexture = new RenderTexture(96, 96, 24, RenderTextureFormat.ARGB32);
            Texture2D atlas = LoadAtlasTexture();
            Texture2D readback = null;

            try
            {
                simulation.Init(maxShips: 1, maxProjectiles: 1);
                simulation.ShipCount = 1;
                simulation.SetShip(0, Ship(0f, 0f, factionId: 0));

                Span<HullPartDefinition> parts = stackalloc HullPartDefinition[1];
                parts[0] = Part(partId: 0, health: 1000f, offset: float2.zero, radius: 32f);
                int hull = solver.RegisterHull(float2.zero, 0f, factionId: 0);
                solver.SetHullParts(hull, parts);

                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 128f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = renderTexture;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);

                var renderer = viewObject.AddComponent<BatchSpriteRenderer>();
                var view = viewObject.AddComponent<MissionSpriteView>();
                view.Initialize(table, atlas, capacity: 8);
                Assert.IsTrue(view.BeginMission(simulation, solver, new[] { hull }, playerIndex: 0, capacity: 8));
                Assert.Greater(view.InstanceCount, 0);

                readback = new Texture2D(96, 96, TextureFormat.RGBA32, false);

                // URP may not present the first frame in a single tick; poll a bounded number of frames.
                bool drewPixels = false;
                for (int frame = 0; frame < 30 && !drewPixels; frame++)
                {
                    yield return null;

                    RenderTexture.active = renderTexture;
                    readback.ReadPixels(new Rect(0, 0, 96, 96), 0, 0);
                    readback.Apply();
                    RenderTexture.active = null;

                    Color32[] pixels = readback.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        // Clear colour is opaque black; any lit texel proves the sprite pass ran.
                        if (pixels[i].r < 8 && pixels[i].g < 8 && pixels[i].b < 8) continue;
                        drewPixels = true;
                        break;
                    }
                }

                Assert.IsTrue(drewPixels,
                    "The URP 2D renderer produced no pixels for the populated BRG frame; check the shader LightMode tag and atlas.");
            }
            finally
            {
                RenderTexture.active = null;
                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                if (cameraObject.TryGetComponent(out Camera cameraComponent)) cameraComponent.targetTexture = null;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(viewObject);
                if (atlas != null) UnityEngine.Object.DestroyImmediate(atlas);
                solver.Dispose();
                simulation.Dispose();
            }
        }

        private static Texture2D LoadAtlasTexture()
        {
            string path = Path.Combine(Application.dataPath, "Art", "Atlases", "PartsAtlas.png");
            Assert.IsTrue(File.Exists(path), $"Committed parts atlas missing: {path}");

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false),
                "The committed parts atlas could not be decoded.");
            return texture;
        }

        private static SpriteUvTable LoadCommittedTable()
        {
            string path = Path.Combine(Application.dataPath, "Art", "Atlases", "PartsAtlasUv.txt");
            Assert.IsTrue(File.Exists(path), $"Committed UV table missing: {path}");
            return SpriteUvTable.Parse(File.ReadAllText(path));
        }

        private static void StepProjectile(ProjectileContactSolver solver, float2 origin, float damage)
        {
            Span<ProjectileData> projectiles = stackalloc ProjectileData[1];
            projectiles[0] = new ProjectileData
            {
                position = origin,
                velocity = new float2(0f, FastVelocity),
                lifetime = 1f,
                damage = damage,
                factionId = 1,
                isAlive = true
            };
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);
            solver.Step(projectiles, DeltaTime);
        }

        private static void AssertShipInstancesPristine(BatchSpriteRenderer renderer, int shipInstanceCount)
        {
            for (int i = 0; i < shipInstanceCount; i++)
                Assert.AreEqual(SpriteUserValue.Pristine, renderer.GetUserValue(i), $"Ship instance {i} must start pristine.");
        }

        private static bool HasTintedInstance(BatchSpriteRenderer renderer, int count)
        {
            for (int i = 0; i < count; i++)
                if (renderer.GetUserValue(i) != SpriteUserValue.Pristine) return true;
            return false;
        }

        private static ShipEntity Ship(float x, float y, int factionId)
        {
            return new ShipEntity
            {
                position = new float2(x, y),
                velocity = float2.zero,
                rotation = 0f,
                mass = 100f,
                health = 1000f,
                maxHealth = 1000f,
                energy = 500f,
                maxEnergy = 500f,
                thrustPower = 1000f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = factionId
            };
        }

        private static HullPartDefinition Part(int partId, float health, float2 offset, float radius)
        {
            return new HullPartDefinition
            {
                part = new MountedPartData
                {
                    partId = partId,
                    category = PartCategory.Body,
                    health = health,
                    maxHealth = health,
                    localOffset = offset
                },
                hitRadius = radius
            };
        }
    }
}
