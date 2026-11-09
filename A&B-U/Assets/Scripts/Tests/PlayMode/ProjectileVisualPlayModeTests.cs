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
    /// Ticket #184 evidence: the BRG path submits one procedural body plus one tinted trail per
    /// live projectile, the weak/medium/heavy/railgun families differ by frozen btwh/speed,
    /// missiles alone use the extracted Blt art, and the palette follows the base-game faction
    /// hues. The last test writes a human-reviewable PNG outside the project.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ProjectileVisualPlayModeTests
    {
        private const float DeltaTime = 1f / 60f;

        private const int WeakPlayerBullet = 1;     // G.25, btwh 1, spdS 10
        private const int MediumHostileBullet = 14; // G.30, btwh 3, spdS 13.8
        private const int HeavyCaptainBullet = 26;  // G.70, btwh 7.5, spdS 15
        private const int RailgunHostileBullet = 58;// G.L, btwh 6, spdS 80
        private const int MissilePlayerBullet = 32; // G.R, Blt_1 art

        [Test]
        public void Families_SubmitBodiesAndColouredTrails_WithoutLeavingTheCapacityBudget()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("BatchRendererGroup requires a graphics device; skipped in -nographics runs.");

            SpriteUvTable table = LoadCommittedTable();
            var simulation = new ScheduledCombatSimulation();
            var go = new GameObject("projectile-visual-test");

            try
            {
                simulation.Init(maxShips: 1, maxProjectiles: 5);
                simulation.ShipCount = 0;

                SetProjectile(simulation, 0, WeakPlayerBullet, new float2(360f, 0f), ProjectileRole.Player);
                SetProjectile(simulation, 1, MediumHostileBullet, new float2(-468f, 0f), ProjectileRole.Hostile);
                SetProjectile(simulation, 2, HeavyCaptainBullet, new float2(0f, 540f), ProjectileRole.Captain);
                SetProjectile(simulation, 3, RailgunHostileBullet, new float2(2520f, 0f), ProjectileRole.Hostile);
                SetProjectile(simulation, 4, MissilePlayerBullet, new float2(72f, 0f), ProjectileRole.Player);
                simulation.ProjectileCount = 5;

                var renderer = go.AddComponent<BatchSpriteRenderer>();
                var view = go.AddComponent<MissionSpriteView>();
                view.StarfieldEnabled = false;
                view.EffectsEnabled = true;
                view.Initialize(table, null, capacity: 16);
                Assert.IsTrue(view.BeginMission(simulation, null, null, playerIndex: -1, capacity: 16));

                view.Render(DeltaTime);

                Assert.AreEqual(10, view.InstanceCount, "Each live projectile submits one trail and one body.");
                Assert.LessOrEqual(view.InstanceCount, view.Capacity,
                    "Projectile trails must stay inside the render capacity budget.");

                AssertTrailAndBody(renderer, 0, ProjectileRole.Player, table.White);
                AssertTrailAndBody(renderer, 1, ProjectileRole.Hostile, table.White);
                AssertTrailAndBody(renderer, 2, ProjectileRole.Captain, table.White);
                AssertTrailAndBody(renderer, 3, ProjectileRole.Hostile, table.White);

                // Missiles are the only bullets drawn with extracted art.
                Assert.IsTrue(table.TryGetSymbol("bullet_Blt_1.png", out SpriteUvRect missileRect));
                AssertTrailAndBody(renderer, 4, ProjectileRole.Player, missileRect, missileBody: true);

                ProjectileVisualTable visuals = view.ProjectileVisuals;
                Assert.IsNotNull(visuals);
                Assert.IsFalse(visuals.Get(WeakPlayerBullet).isMissile);
                Assert.IsFalse(visuals.Get(HeavyCaptainBullet).isMissile);
                Assert.IsFalse(visuals.Get(RailgunHostileBullet).isMissile);
                Assert.IsTrue(visuals.Get(MissilePlayerBullet).isMissile);
                Assert.Less(visuals.Get(WeakPlayerBullet).thickness, visuals.Get(HeavyCaptainBullet).thickness,
                    "Weak weapons must stay thinner than heavy ones.");

                for (int i = 0; i < 5; i++) view.Render(DeltaTime);
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 30; i++) view.Render(DeltaTime);
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                Assert.AreEqual(0, allocatedBytes,
                    $"Projectile rendering allocated {allocatedBytes} bytes over 30 frames.");
            }
            finally
            {
                if (go.TryGetComponent(out BatchSpriteRenderer rendererComponent)) rendererComponent.enabled = false;
                UnityEngine.Object.DestroyImmediate(go);
                simulation.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Families_RenderToTextureAndWriteAReviewCapture()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Rendering requires a graphics device; skipped in -nographics runs.");

            const int width = 960;
            const int height = 540;

            SpriteUvTable table = LoadCommittedTable();
            Texture2D atlas = LoadAtlasTexture();
            var simulation = new ScheduledCombatSimulation();
            var viewObject = new GameObject("projectile-visual-capture-view");
            var cameraObject = new GameObject("projectile-visual-capture-camera");
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D readback = null;

            try
            {
                simulation.Init(maxShips: 1, maxProjectiles: 5);
                simulation.ShipCount = 0;

                SetProjectile(simulation, 0, WeakPlayerBullet, new float2(360f, 0f), ProjectileRole.Player, 40f, 100f);
                SetProjectile(simulation, 1, MediumHostileBullet, new float2(468f, 0f), ProjectileRole.Hostile, 40f, 55f);
                SetProjectile(simulation, 2, HeavyCaptainBullet, new float2(540f, 0f), ProjectileRole.Captain, 40f, 10f);
                SetProjectile(simulation, 3, RailgunHostileBullet, new float2(2520f, 0f), ProjectileRole.Hostile, 40f, -50f);
                SetProjectile(simulation, 4, MissilePlayerBullet, new float2(72f, 0f), ProjectileRole.Player, 40f, -80f);
                simulation.ProjectileCount = 5;

                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 135f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = renderTexture;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);

                var renderer = viewObject.AddComponent<BatchSpriteRenderer>();
                var view = viewObject.AddComponent<MissionSpriteView>();
                view.StarfieldEnabled = false;
                view.EffectsEnabled = true;
                view.SetCamera(camera);
                view.Initialize(table, atlas, capacity: 16);
                Assert.IsTrue(view.BeginMission(simulation, null, null, playerIndex: -1, capacity: 16));

                readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color32[] pixels = null;
                bool drewPixels = false;
                for (int frame = 0; frame < 30 && !drewPixels; frame++)
                {
                    view.Render(DeltaTime);
                    yield return null;

                    RenderTexture.active = renderTexture;
                    readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    readback.Apply();
                    RenderTexture.active = null;

                    pixels = readback.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (pixels[i].r < 8 && pixels[i].g < 8 && pixels[i].b < 8) continue;
                        drewPixels = true;
                        break;
                    }
                }

                Assert.IsTrue(drewPixels, "The BRG path produced no projectile pixels.");

                int cyan = 0;
                int red = 0;
                int amber = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    if (pixel.b > 150 && pixel.g > 150 && pixel.r < 120) cyan++;
                    if (pixel.r > 150 && pixel.g < 120 && pixel.b < 120) red++;
                    if (pixel.r > 200 && pixel.g > 150 && pixel.b < 80) amber++;
                }

                Assert.Greater(cyan, 100, "Player projectiles must render in the frozen cyan palette.");
                Assert.Greater(red, 100, "Hostile projectiles must render in the frozen red palette.");
                Assert.Greater(amber, 100, "Captain projectiles must render in the HUD's amber palette.");

                string folder = Path.Combine(Path.GetTempPath(), "opencode");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "projectile-families.png");
                File.WriteAllBytes(path, readback.EncodeToPNG());
                Debug.Log($"[PROJECTILE CAPTURE] path={path} cyan={cyan} red={red} amber={amber}");

                if (renderer != null) renderer.enabled = false;
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
                simulation.Dispose();
            }
        }

        private static void AssertTrailAndBody(
            BatchSpriteRenderer renderer, int projectileIndex, ProjectileRole role, SpriteUvRect expectedBodyUv,
            bool missileBody = false)
        {
            int trailIndex = projectileIndex * 2;
            int bodyIndex = trailIndex + 1;

            uint body = renderer.GetUserValue(bodyIndex);
            uint trail = renderer.GetUserValue(trailIndex);

            uint expectedBody = missileBody
                ? ProjectilePalette.MissileBody((int)role)
                : ProjectilePalette.Body((int)role);
            Assert.AreEqual(expectedBody, body, $"Projectile {projectileIndex} body tint.");

            Assert.AreEqual(ProjectilePalette.Body((int)role) & 0x00FFFFFFu, trail & 0x00FFFFFFu,
                $"Projectile {projectileIndex} trail must inherit the faction colour.");
            Assert.AreEqual(ProjectilePalette.TrailAlpha, (byte)(trail >> 24));

            Vector4 uv = renderer.GetUvRect(bodyIndex);
            Assert.AreEqual(expectedBodyUv.UvX, uv.x, 0.00001f);
            Assert.AreEqual(expectedBodyUv.UvY, uv.y, 0.00001f);
            Assert.AreEqual(expectedBodyUv.UvWidth, uv.z, 0.00001f);
            Assert.AreEqual(expectedBodyUv.UvHeight, uv.w, 0.00001f);
        }

        private static void SetProjectile(
            ScheduledCombatSimulation simulation, int index, int bulletId, float2 velocity, ProjectileRole role,
            float x = 0f, float y = 0f)
        {
            simulation.SetProjectile(index, new ProjectileData
            {
                position = new float2(x, y),
                velocity = velocity,
                lifetime = 2f,
                damage = 10f,
                factionId = role == ProjectileRole.Hostile ? 1 : 0,
                projectileType = bulletId,
                paletteRole = (int)role,
                isAlive = true
            });
        }

        private static SpriteUvTable LoadCommittedTable()
        {
            string path = Path.Combine(Application.dataPath, "Art", "Atlases", "PartsAtlasUv.txt");
            Assert.IsTrue(File.Exists(path), $"Committed UV table missing: {path}");
            return SpriteUvTable.Parse(File.ReadAllText(path));
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
    }
}
