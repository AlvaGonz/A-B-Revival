using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Visual-parity evidence for ticket #168: renders the live combat HUD in the Boot scene
    /// and writes a PNG outside the project. Tries <see cref="ScreenCapture"/> first and falls
    /// back to reading a <see cref="PanelSettings.targetTexture"/> back, so a batchmode run still
    /// produces a frame a human can inspect. No HUD behaviour is changed.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class HudCaptureTests
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const float MinimumNonBlackFraction = 0.01f;

        [UnityTest]
        public IEnumerator CombatHud_WritesANonBlackPngOutsideTheProject()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow, "Boot scene must host the game flow.");

            flow.LaunchMission();
            yield return null;
            flow.SkipBriefing();

            for (int i = 0; i < 300 && flow.State != GameFlowState.Mission; i++)
                yield return null;
            Assert.AreEqual(GameFlowState.Mission, flow.State, "The capture needs the live mission HUD.");

            // Let the mission clock advance, then populate the telemetry and the camera badge so
            // the evidence frame shows every HUD element the ticket lists.
            for (int i = 0; i < 30 && flow.Runner.CurrentTick < 60; i++)
            {
                flow.Tick(GameFlowController.FixedStepSeconds * 4f);
                yield return null;
            }

            Assert.GreaterOrEqual(flow.Runner.CurrentTick, 60,
                "The evidence frame must show a running mission clock.");

            CombatHudBehaviour hud = UnityEngine.Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud, "Boot scene must host the combat HUD.");
            hud.State.RecordLostPart(PartCategory.Gun);
            hud.State.RecordLostPart(PartCategory.Gun);
            hud.State.RecordLostPart(PartCategory.Wing);
            hud.CycleCameraDistance();
            yield return null;
            yield return null;

            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);

            // Method 1: the game view back buffer.
            string screenPath = Path.Combine(folder, "hud-capture-screen.png");
            if (File.Exists(screenPath)) File.Delete(screenPath);
            ScreenCapture.CaptureScreenshot(screenPath);
            yield return null;
            yield return null;

            if (IsUsable(screenPath))
            {
                Report("ScreenCapture", screenPath);
                yield break;
            }

            Debug.Log("[HUD CAPTURE] ScreenCapture produced no usable pixels; trying the panel readback.");

            // Method 2: render the live panel into a RenderTexture and read it back.
            UIDocument document = hud.GetComponent<UIDocument>();
            Assert.IsNotNull(document, "The HUD behaviour needs its UIDocument.");

            string panelPath = Path.Combine(folder, "hud-capture-panel.png");
            PanelSettings settings = document.panelSettings;
            RenderTexture previousTarget = settings.targetTexture;
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 0, RenderTextureFormat.ARGB32);
            target.Create();

            try
            {
                settings.targetTexture = target;

                // The document rebuilds its tree when the render target changes; rebind the presenter.
                hud.enabled = false;
                hud.enabled = true;
                for (int i = 0; i < 4; i++)
                    yield return null;

                RenderTexture.active = target;
                var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                image.Apply();
                RenderTexture.active = null;

                File.WriteAllBytes(panelPath, image.EncodeToPNG());
                UnityEngine.Object.Destroy(image);
            }
            finally
            {
                settings.targetTexture = previousTarget;
                hud.enabled = false;
                hud.enabled = true;
                RenderTexture.active = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
            }

            Assert.IsTrue(IsUsable(panelPath),
                $"Neither capture path produced HUD pixels. Screen: {screenPath}");
            Report("PanelSettings.targetTexture", panelPath);
        }

        private static bool IsUsable(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0) return false;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) return false;

                Color32[] pixels = texture.GetPixels32();
                int nonBlack = 0;
                var colors = new HashSet<int>();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) nonBlack++;
                    if (colors.Count < 4096) colors.Add((pixel.r << 16) | (pixel.g << 8) | pixel.b);
                }

                float fraction = pixels.Length > 0 ? (float)nonBlack / pixels.Length : 0f;
                Debug.Log($"[HUD CAPTURE] {Path.GetFileName(path)} size={texture.width}x{texture.height} " +
                          $"nonBlack={fraction:P2} colors={colors.Count}");

                return fraction >= MinimumNonBlackFraction || colors.Count >= 32;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[HUD CAPTURE] {Path.GetFileName(path)} could not be read: {exception.Message}");
                return false;
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static void Report(string method, string path)
        {
            Debug.Log($"[HUD CAPTURE] method={method} path={path} size={new FileInfo(path).Length} bytes");
        }
    }
}
