using System.Collections;
using System.IO;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Ticket #179 gates for the shared 3x design scale, the Fuel/Reserved bars, and the two
    /// control-mode evidence captures. Runs the real Boot scene and mission HUD.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class CombatHudResolutionPlayModeTests
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        [UnityTest]
        public IEnumerator Hud_FillsAndAnchorsAt1080p720pAnd16By10()
        {
            yield return LoadMission();
            UIDocument document = FindHudDocument();
            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;

            try
            {
                foreach (Vector2Int size in new[]
                         {
                             new Vector2Int(1920, 1080),
                             new Vector2Int(1280, 720),
                             new Vector2Int(1680, 1050)
                         })
                {
                    var target = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
                    target.Create();
                    settings.targetTexture = target;

                    for (int i = 0; i < 6; i++) yield return null;

                    VisualElement root = document.rootVisualElement;
                    VisualElement hud = root.Q("hud-root");
                    VisualElement bars = root.Q(CombatHudPresenter.BarsElement);
                    VisualElement minimap = root.Q(CombatHudPresenter.MinimapElement);
                    VisualElement fuel = root.Q(CombatHudPresenter.FuelFillElement);
                    VisualElement reserved = root.Q(CombatHudPresenter.ReservedFillElement);

                    Assert.IsNotNull(hud, "HUD root is missing at " + size);
                    Assert.IsNotNull(bars, "HUD bars are missing at " + size);
                    Assert.IsNotNull(minimap, "HUD minimap is missing at " + size);
                    Assert.IsNotNull(fuel, "Fuel fill is missing at " + size);
                    Assert.IsNotNull(reserved, "Reserved fill is missing at " + size);

                    Rect surface = hud.worldBound;
                    Assert.Greater(surface.width, 0f, "HUD must fill the root width at " + size);
                    Assert.Greater(surface.height, 0f, "HUD must fill the root height at " + size);
                    Assert.AreEqual(root.worldBound.width, surface.width, Mathf.Max(2f, surface.width * 0.01f),
                        "HUD must fill the root width at " + size);
                    Assert.AreEqual(root.worldBound.height, surface.height, Mathf.Max(2f, surface.height * 0.01f),
                        "HUD must fill the root height at " + size);

                    Assert.LessOrEqual(surface.yMax - bars.worldBound.yMax, 120f,
                        "The bars must stay bottom-anchored at " + size);
                    Assert.AreEqual(surface.xMax, minimap.worldBound.xMax, 2f,
                        "The minimap must stay right-anchored at " + size);
                    Assert.AreEqual(surface.yMax, minimap.worldBound.yMax, 2f,
                        "The minimap must stay bottom-anchored at " + size);
                    Assert.AreEqual(DisplayStyle.Flex, fuel.resolvedStyle.display, "Fuel bar must be visible.");
                    Assert.AreEqual(DisplayStyle.Flex, reserved.resolvedStyle.display, "Reserved bar must be visible.");

                    target.Release();
                    Object.Destroy(target);
                }
            }
            finally
            {
                settings.targetTexture = previous;
            }
        }

        [UnityTest]
        public IEnumerator CombatHud_WritesKeyboardAndMouseModeCaptures()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run PlayMode without -nographics.");

            yield return LoadMission();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow, "Boot must host the game flow.");
            Assert.IsNotNull(flow.Flight, "Boot must host the flight controller.");

            flow.Flight.SetControlMode(PlayerControlMode.Keyboard);
            for (int i = 0; i < 30; i++)
            {
                flow.Flight.SetPointer(new Vector2(1380f, 620f), new Vector2(CaptureWidth, CaptureHeight));
                flow.Tick(GameFlowController.FixedStepSeconds * 4f);
                yield return null;
            }

            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);
            flow.Flight.SetPointer(new Vector2(1380f, 620f), new Vector2(CaptureWidth, CaptureHeight));
            flow.Tick(GameFlowController.FixedStepSeconds * 4f);

            string keyboardPath = Path.Combine(folder, "combat-keyboard-1920x1080.png");
            yield return CaptureCombatFrame(keyboardPath);

            flow.Flight.SetControlMode(PlayerControlMode.Mouse);
            for (int i = 0; i < 20; i++)
            {
                flow.Flight.SetPointer(new Vector2(1500f, 400f), new Vector2(CaptureWidth, CaptureHeight));
                flow.Tick(GameFlowController.FixedStepSeconds * 4f);
                yield return null;
            }

            flow.Flight.SetPointer(new Vector2(1500f, 400f), new Vector2(CaptureWidth, CaptureHeight));
            flow.Tick(GameFlowController.FixedStepSeconds * 4f);
            Assert.Less(flow.Simulation.GetShip(flow.Runner.PlayerShipIndex).rotation, 0f,
                "Mouse mode must turn the live player hull toward the cursor.");

            string mousePath = Path.Combine(folder, "combat-mouse-1920x1080.png");
            yield return CaptureCombatFrame(mousePath);

            Assert.IsTrue(IsUsable(keyboardPath), "Keyboard-mode capture is empty: " + keyboardPath);
            Assert.IsTrue(IsUsable(mousePath), "Mouse-mode capture is empty: " + mousePath);
            Debug.Log("[Combat Capture] keyboard=" + keyboardPath + " mouse=" + mousePath);
        }

        private static IEnumerator LoadMission()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow, "Boot scene must host the game flow.");

            flow.LaunchMission();
            yield return null;
            flow.SkipBriefing();
            for (int i = 0; i < 300 && flow.State != GameFlowState.Mission; i++)
                yield return null;
            Assert.AreEqual(GameFlowState.Mission, flow.State, "The HUD gate needs the live mission HUD.");
        }

        private static UIDocument FindHudDocument()
        {
            CombatHudBehaviour hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud, "Boot scene must host the combat HUD.");
            UIDocument document = hud.GetComponent<UIDocument>();
            Assert.IsNotNull(document, "The HUD behaviour needs its UIDocument.");
            return document;
        }

        /// <summary>
        /// Composites one world frame and the live HUD panel into a single 1920x1080 PNG.
        /// The panel texture has an alpha background, so it layers over the BRG battlefield.
        /// </summary>
        private static IEnumerator CaptureCombatFrame(string path)
        {
            UIDocument document = FindHudDocument();
            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;
            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "The mission camera is required for the capture.");
            RenderTexture previousCameraTarget = camera.targetTexture;

            var worldTarget = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var hudTarget = new RenderTexture(CaptureWidth, CaptureHeight, 0, RenderTextureFormat.ARGB32);
            worldTarget.Create();
            hudTarget.Create();
            var world = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            var hud = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            var composite = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);

            try
            {
                camera.targetTexture = worldTarget;
                camera.Render();
                RenderTexture.active = worldTarget;
                world.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                world.Apply();
                RenderTexture.active = null;
                camera.targetTexture = previousCameraTarget;

                settings.targetTexture = hudTarget;
                CombatHudBehaviour hudBehaviour = document.GetComponent<CombatHudBehaviour>();
                hudBehaviour.enabled = false;
                hudBehaviour.enabled = true;
                for (int i = 0; i < 6; i++) yield return null;

                RenderTexture.active = hudTarget;
                hud.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                hud.Apply();
                RenderTexture.active = null;

                Color32[] battlefield = world.GetPixels32();
                Color32[] overlay = hud.GetPixels32();
                var result = new Color32[battlefield.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    Color32 top = overlay[i];
                    Color32 bottom = battlefield[i];
                    int alpha = top.a;
                    int inverse = 255 - alpha;
                    result[i] = new Color32(
                        (byte)((top.r * alpha + bottom.r * inverse) / 255),
                        (byte)((top.g * alpha + bottom.g * inverse) / 255),
                        (byte)((top.b * alpha + bottom.b * inverse) / 255),
                        255);
                }

                composite.SetPixels32(result);
                composite.Apply();
                File.WriteAllBytes(path, composite.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousCameraTarget;
                settings.targetTexture = previous;
                RenderTexture.active = null;
                worldTarget.Release();
                hudTarget.Release();
                Object.Destroy(worldTarget);
                Object.Destroy(hudTarget);
                Object.Destroy(world);
                Object.Destroy(hud);
                Object.Destroy(composite);
            }
        }

        private static bool IsUsable(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0) return false;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) return false;
                Color32[] pixels = texture.GetPixels32();
                int coloured = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].r > 16 || pixels[i].g > 16 || pixels[i].b > 16) coloured++;
                }

                return coloured > pixels.Length / 200;
            }
            finally
            {
                Object.Destroy(texture);
            }
        }
    }
}
