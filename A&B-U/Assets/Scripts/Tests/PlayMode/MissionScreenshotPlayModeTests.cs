using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ABRevival.Core.Camera;
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
    /// Renders the frozen Captain composition (defIndex 76: body + wings + engines + guns)
    /// through the real BRG path into a RenderTexture and proves the frame is the original art
    /// and not a placeholder ring. When a committed golden exists it is compared within
    /// tolerance; otherwise the test asserts structural ship pixels and always writes the
    /// actual frame as a build artifact.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class MissionScreenshotPlayModeTests
    {
        private const int Size = 512;
        private static readonly float CameraSize = 256f * CameraDistancePresets.MountArtScale;
        private const string GoldenRelativePath = "Tests/Golden/mission_captain_defIndex76.png";

        [UnityTest]
        public IEnumerator CaptainComposition_RendersExtractedMountArtAndSavesArtifact()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Rendering requires a graphics device; skipped in -nographics runs.");

            CraftStatRecord craft = LoadCraft(76);
            PartMountTable mounts = LoadMountTable();
            Assert.IsNotNull(craft, "Captain defIndex 76 must exist in the frozen craft catalog.");
            Assert.IsNotNull(mounts, "The committed part mount table is required for composition.");

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[32];
            int placementCount = CraftComposition.Build(craft, mounts, placements);
            Assert.Greater(placementCount, 5, "The Captain composition must include hull, wings, engines and guns.");

            SpriteUvTable table = LoadCommittedUvTable();
            Texture2D atlas = LoadAtlasTexture();
            var simulation = new ScheduledCombatSimulation();
            var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 32, maxProjectiles: 1);
            var viewObject = new GameObject("mission-screenshot-view");
            var cameraObject = new GameObject("mission-screenshot-camera");
            var renderTexture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            Texture2D readback = null;

            try
            {
                simulation.Init(maxShips: 1, maxProjectiles: 1);
                simulation.ShipCount = 1;
                simulation.SetShip(0, Ship());

                Span<HullPartDefinition> parts = stackalloc HullPartDefinition[32];
                for (int i = 0; i < placementCount; i++)
                {
                    parts[i] = new HullPartDefinition
                    {
                        part = new MountedPartData
                        {
                            partId = placements[i].partId,
                            category = placements[i].category,
                            health = 1000f,
                            maxHealth = 1000f,
                            localOffset = placements[i].localOffset * CameraDistancePresets.MountArtScale,
                            localRotation = placements[i].localRotation,
                            mirrored = placements[i].mirrored
                        },
                        hitRadius = 12f
                    };
                }

                int hull = solver.RegisterHull(float2.zero, 0f, factionId: 0);
                solver.SetHullParts(hull, parts.Slice(0, placementCount));

                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = CameraSize;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = renderTexture;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);

                var view = viewObject.AddComponent<MissionSpriteView>();
                view.StarfieldEnabled = false;
                view.EffectsEnabled = false;
                view.SetCamera(camera);
                view.Initialize(table, atlas, capacity: 128);
                Assert.IsTrue(view.BeginMission(simulation, solver, new[] { hull }, playerIndex: 0, capacity: 128));
                view.Render(0f);
                Assert.AreEqual(placementCount, view.InstanceCount, "Every mounted part must submit exactly one instance.");

                // Engine mounts first, then hull, wings, and guns/modules in front (ADR 0019).
                int previousLayer = -1;
                for (int instance = 0; instance < view.InstanceCount; instance++)
                {
                    Vector4 uv = view.Renderer.GetUvRect(instance);
                    int layer = -1;
                    for (int i = 0; i < placementCount; i++)
                    {
                        Assert.IsTrue(table.TryGetPart(placements[i].partId, out SpriteUvRect rect));
                        if (Mathf.Abs(rect.UvX - uv.x) > 0.00001f) continue;
                        if (Mathf.Abs(rect.UvY - uv.y) > 0.00001f) continue;
                        if (Mathf.Abs(rect.UvWidth - uv.z) > 0.00001f) continue;
                        if (Mathf.Abs(rect.UvHeight - uv.w) > 0.00001f) continue;
                        layer = CraftComposition.Layer(placements[i].category);
                        break;
                    }

                    Assert.GreaterOrEqual(layer, previousLayer,
                        $"Instance {instance} broke the engine/hull/wing/gun layer order.");
                    previousLayer = layer;
                }

                readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                Color32[] pixels = null;
                bool drewPixels = false;
                for (int frame = 0; frame < 40 && !drewPixels; frame++)
                {
                    yield return null;
                    RenderTexture.active = renderTexture;
                    readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
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

                Assert.IsTrue(drewPixels, "The BRG path produced no pixels for the Captain composition.");

                int nonBlack = 0;
                int distinct = 0;
                var colors = new HashSet<int>();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].r < 8 && pixels[i].g < 8 && pixels[i].b < 8) continue;
                    nonBlack++;
                    if (colors.Add((pixels[i].r << 16) | (pixels[i].g << 8) | pixels[i].b)) distinct++;
                }

                Assert.Greater(nonBlack, 1500, "A composed Captain must cover substantially more than a ring placeholder.");
                Assert.Greater(distinct, 12, "The frame must contain the original ship art palette, not a single flat colour.");

                int centreColumn = Size / 2;
                int minY = Size;
                int maxY = -1;
                for (int y = 0; y < Size; y++)
                {
                    Color32 pixel = pixels[(y * Size) + centreColumn];
                    if (pixel.r < 8 && pixel.g < 8 && pixel.b < 8) continue;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
                Assert.Greater(maxY - minY, 100, "The hull must be recognisable at the corrected world scale, not a tiny ring.");

                Color32 centre = pixels[((Size / 2) * Size) + (Size / 2)];
                Assert.IsFalse(centre.r > 250 && centre.g > 250 && centre.b > 250,
                    "The centre pixel must be ship art, not the synthetic white projectile cell.");

                string artifact = SaveArtifact(readback);
                Debug.Log($"[MissionScreenshot] Frame artifact: {artifact}");

                string goldenPath = Path.Combine(Application.dataPath, GoldenRelativePath);
                if (Environment.GetEnvironmentVariable("AB_REVIVAL_CAPTURE_GOLDEN") == "1")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(goldenPath));
                    File.WriteAllBytes(goldenPath, readback.EncodeToPNG());
                    Debug.Log($"[MissionScreenshot] Wrote golden: {goldenPath}");
                    yield break;
                }

                Assert.IsTrue(File.Exists(goldenPath),
                    $"No committed golden at {goldenPath}; a graphics device must not silently skip parity.");
                AssertGolden(goldenPath, pixels);
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

        private static void AssertGolden(string path, Color32[] actual)
        {
            var golden = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(golden, File.ReadAllBytes(path), false),
                    "The committed golden could not be decoded.");
                Assert.AreEqual(Size, golden.width, "Golden width changed.");
                Assert.AreEqual(Size, golden.height, "Golden height changed.");

                Color32[] expected = golden.GetPixels32();
                Assert.AreEqual(expected.Length, actual.Length);

                long totalDelta = 0;
                int outsideTolerance = 0;
                for (int i = 0; i < expected.Length; i++)
                {
                    int delta = Math.Abs(expected[i].r - actual[i].r) +
                                Math.Abs(expected[i].g - actual[i].g) +
                                Math.Abs(expected[i].b - actual[i].b);
                    totalDelta += delta;
                    if (delta > 120) outsideTolerance++;
                }

                double meanDelta = totalDelta / (double)(expected.Length * 3);
                double outsideFraction = outsideTolerance / (double)expected.Length;
                Assert.Less(meanDelta, 16.0, "The rendered Captain drifted from the committed golden.");
                Assert.Less(outsideFraction, 0.03, "More than 3% of pixels differ strongly from the committed golden.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(golden);
            }
        }

        private static string SaveArtifact(Texture2D texture)
        {
            string directory = Environment.GetEnvironmentVariable("AB_REVIVAL_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(directory))
                directory = Path.Combine(Application.persistentDataPath, "screenshots");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, "mission_captain_defIndex76.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return path;
        }

        private static CraftStatRecord LoadCraft(int defIndex)
        {
            TextAsset defFly = Resources.Load<TextAsset>("Data/3_all.AllData_defFly");
            TextAsset defPart = Resources.Load<TextAsset>("Data/8_all.AllData_defPart");
            TextAsset defFire = Resources.Load<TextAsset>("Data/2_all.AllData_defFire");
            TextAsset defBullet = Resources.Load<TextAsset>("Data/11_all.AllData_defBullet");
            Assert.IsNotNull(defFly, "Resources/Data/3_all.AllData_defFly is required.");
            Assert.IsNotNull(defPart, "Resources/Data/8_all.AllData_defPart is required.");
            Assert.IsNotNull(defFire, "Resources/Data/2_all.AllData_defFire is required.");
            Assert.IsNotNull(defBullet, "Resources/Data/11_all.AllData_defBullet is required.");

            CraftCatalog catalog = CraftCatalogParser.Parse(defFly.bytes, defPart.text, defFire.text, defBullet.text);
            return catalog.Find(defIndex);
        }

        private static PartMountTable LoadMountTable()
        {
            TextAsset table = Resources.Load<TextAsset>("Data/PartMounts");
            return table != null ? PartMountTable.Parse(table.text) : null;
        }

        private static SpriteUvTable LoadCommittedUvTable()
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

        private static ShipEntity Ship()
        {
            return new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f,
                mass = 100f,
                health = 1000f,
                maxHealth = 1000f,
                boosterFuel = 500f,
                maxBoosterFuel = 500f,
                thrustPower = 1000f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = 0
            };
        }
    }
}
