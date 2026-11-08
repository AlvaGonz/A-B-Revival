using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using ABRevival.Core.Rendering;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// PlayMode smoke coverage for the presentation seams: the frozen-layout HUD with live
    /// values and original sprites, the mouse reticle inverse mapping, camera preset
    /// switching, and bounded BatchRendererGroup submission.
    /// </summary>
    [TestFixture]
    public class PresentationPlayModeTests
    {
        [Test]
        public void HudPresenter_RendersTelemetryCaptainCodeAndCameraBadge()
        {
            VisualElement root = CreateHudRoot();
            var hullValue = root.Q<Label>(CombatHudPresenter.HullValueElement);
            var hullFill = root.Q<VisualElement>(CombatHudPresenter.HullFillElement);
            var lostParts = root.Q<Label>(CombatHudPresenter.LostPartsElement);
            var captainMode = root.Q<Label>(CombatHudPresenter.CaptainModeElement);
            var captainTile = root.Q<VisualElement>(CombatHudPresenter.CaptainTileElement);
            var modeBadge = root.Q<Label>(CombatHudPresenter.ModeBadgeTextElement);
            var cameraBadge = root.Q<VisualElement>(CombatHudPresenter.CameraBadgeElement);
            var cameraBadgeText = root.Q<Label>(CombatHudPresenter.CameraBadgeTextElement);

            var state = new CombatHudState();
            var presenter = new CombatHudPresenter(root);

            state.SetHullHealth(750f, 1000f);
            state.RecordLostPart(PartCategory.Gun);
            state.RecordLostPart(PartCategory.Gun);
            state.RecordLostPart(PartCategory.Wing);
            state.SetCaptainPresence(true);
            state.SetCaptainMode(CaptainTacticalMode.FireSupport);
            state.CycleCameraDistance(); // Short -> Medium, shows the ephemeral badge

            presenter.Render(state.Snapshot());

            Assert.AreEqual("750 / 1000", hullValue.text);
            Assert.AreEqual(LengthUnit.Percent, hullFill.style.width.value.unit);
            Assert.AreEqual(75f, hullFill.style.width.value.value, 0.001f);
            Assert.AreEqual("LOST: [Gun x2] [Wing x1]", lostParts.text);
            Assert.AreEqual("FS", captainMode.text);
            Assert.IsTrue(captainTile.visible);
            Assert.AreEqual("TACTICAL FS", modeBadge.text);
            Assert.AreEqual("[ CAM: MEDIUM (1.3x) ]", cameraBadgeText.text);
            Assert.IsTrue(cameraBadge.visible);
            Assert.AreEqual(1f, cameraBadge.style.opacity.value, 0.001f);

            state.Tick(1.2f);
            presenter.Render(state.Snapshot());
            Assert.IsTrue(cameraBadge.visible);
            Assert.AreEqual(0.6f, cameraBadge.style.opacity.value, 0.001f);

            state.Tick(0.4f);
            presenter.Render(state.Snapshot());
            Assert.IsFalse(cameraBadge.visible);
        }

        [Test]
        public void HudPresenter_HidesCaptainCodeAndTelemetryWhenAbsent()
        {
            VisualElement root = CreateHudRoot();
            var lostParts = root.Q<Label>(CombatHudPresenter.LostPartsElement);
            var captainMode = root.Q<Label>(CombatHudPresenter.CaptainModeElement);
            var captainTile = root.Q<VisualElement>(CombatHudPresenter.CaptainTileElement);
            var modeBadge = root.Q<Label>(CombatHudPresenter.ModeBadgeTextElement);
            var state = new CombatHudState();
            var presenter = new CombatHudPresenter(root);

            presenter.Render(state.Snapshot());
            Assert.AreEqual(string.Empty, lostParts.text);
            Assert.IsFalse(captainTile.visible);
            Assert.AreEqual("TACTICAL --", modeBadge.text);

            state.SetCaptainPresence(true);
            state.SetCaptainMode(CaptainTacticalMode.Evade);
            presenter.Render(state.Snapshot());
            Assert.AreEqual("EV", captainMode.text);
            Assert.IsTrue(captainTile.visible);
            Assert.AreEqual("TACTICAL EV", modeBadge.text);

            state.SetCaptainPresence(false);
            presenter.Render(state.Snapshot());
            Assert.IsFalse(captainTile.visible);
        }

        [Test]
        public void HudPresenter_RendersLiveFrozenReadouts()
        {
            VisualElement root = CreateHudRoot();
            var state = new CombatHudState();
            var presenter = new CombatHudPresenter(root);

            state.SetMoney(1234);
            state.SetMissionSeconds(92.4f);
            state.SetAiCount(3);
            state.SetEnemyDistance(812.6f);
            state.SetEnergyFraction(0.25f);
            state.SetShieldFraction(0.75f);
            state.SetPilotName("Michael");
            state.SetRadarBearing(-90f);
            state.SetReticlePosition(480f, 270f);
            state.SetCaptainHealth(400f, 1000f);
            state.SetCaptainPresence(true);
            presenter.Render(state.Snapshot());

            Assert.AreEqual("1,234 G", root.Q<Label>(CombatHudPresenter.MoneyElement).text);
            Assert.AreEqual("01:32", root.Q<Label>(CombatHudPresenter.MissionTimerElement).text);
            Assert.AreEqual("AI x 3", root.Q<Label>(CombatHudPresenter.AiCountElement).text);
            Assert.AreEqual("813", root.Q<Label>(CombatHudPresenter.DistanceElement).text);
            Assert.AreEqual("Michael", root.Q<Label>(CombatHudPresenter.PilotNameElement).text);

            Assert.AreEqual(25f, root.Q<VisualElement>(CombatHudPresenter.EnergyFillElement).style.width.value.value, 0.01f);
            Assert.AreEqual(75f, root.Q<VisualElement>(CombatHudPresenter.ShieldFillElement).style.width.value.value, 0.01f);
            Assert.AreEqual(40f, root.Q<VisualElement>(CombatHudPresenter.CaptainHealthElement).style.width.value.value, 0.01f);

            VisualElement reticle = root.Q<VisualElement>(CombatHudPresenter.ReticleElement);
            Assert.AreEqual(480f, reticle.style.left.value.value, 0.01f);
            Assert.AreEqual(270f, reticle.style.top.value.value, 0.01f);

            VisualElement arrow = root.Q<VisualElement>(CombatHudPresenter.RadarArrowElement);
            Assert.AreEqual(-90f, arrow.style.rotate.value.angle.value, 0.01f);
        }

        [Test]
        public void HudPresenter_AppliesTheOriginalSpritesFromTheGeneratedLibrary()
        {
            HudSpriteLibrary library = Resources.Load<HudSpriteLibrary>(HudSpriteLibrary.ResourcePath);
            Assert.IsNotNull(library, "HUD sprite library must be generated under Resources/UI.");

            Assert.IsNotNull(library.MoneyPanel, "hud_money_panel sprite");
            Assert.IsNotNull(library.PauseButton, "hud_button_pause sprite");
            Assert.IsNotNull(library.HelpButton, "hud_button_help sprite");
            Assert.IsNotNull(library.Radar, "hud_radar_ring sprite");
            Assert.IsNotNull(library.RadarArrow, "hud_radar_arrow sprite");
            Assert.IsNotNull(library.AiCount, "AI count plate sprite");
            Assert.IsNotNull(library.Minimap, "hud_minimap sprite");
            Assert.IsNotNull(library.PilotPortrait, "hud_pilot_portrait_frame sprite");
            Assert.IsNotNull(library.CaptainPortrait, "hud_captain_portrait_frame sprite");
            Assert.IsNotNull(library.PortraitBacking, "hud_portrait_backing sprite");
            Assert.IsNotNull(library.HealthBar, "hud_health_bar sprite");
            Assert.IsNotNull(library.ShieldBar, "hud_shield_bar sprite");
            Assert.IsNotNull(library.EnergyBar, "hud_energy_bar sprite");
            Assert.IsNotNull(library.Reticle, "hud_reticle sprite");
            Assert.IsNotNull(library.ModeBadge, "hud_mode_badge sprite");
            Assert.IsNotNull(library.CameraBadge, "hud_camera_badge sprite");
            Assert.IsNotNull(library.DamageTelemetryRow, "hud_damage_telemetry_row sprite");
            Assert.IsNotNull(library.PortraitHealthPip, "hud_portrait_health_pip sprite");

            VisualElement root = CreateHudRoot();
            var presenter = new CombatHudPresenter(root, library);
            presenter.Render(new CombatHudState().Snapshot());

            Assert.AreEqual(library.MoneyPanel, root.Q<Image>(CombatHudPresenter.MoneyPanelElement).sprite);
            Assert.AreEqual(library.Radar, root.Q<Image>(CombatHudPresenter.RadarElement).sprite);
            Assert.AreEqual(library.RadarArrow, root.Q<Image>(CombatHudPresenter.RadarArrowElement).sprite);
            Assert.AreEqual(library.AiCount, root.Q<Image>(CombatHudPresenter.AiCountPanelElement).sprite);
            Assert.AreEqual(library.Minimap, root.Q<Image>(CombatHudPresenter.MinimapElement).sprite);
            Assert.AreEqual(library.PilotPortrait, root.Q<Image>(CombatHudPresenter.PilotPortraitElement).sprite);
            Assert.AreEqual(library.CaptainPortrait, root.Q<Image>(CombatHudPresenter.CaptainPortraitElement).sprite);
            Assert.AreEqual(library.HealthBar, root.Q<Image>(CombatHudPresenter.HullFillElement).sprite);
            Assert.AreEqual(library.ShieldBar, root.Q<Image>(CombatHudPresenter.ShieldFillElement).sprite);
            Assert.AreEqual(library.EnergyBar, root.Q<Image>(CombatHudPresenter.EnergyFillElement).sprite);
            Assert.AreEqual(library.Reticle, root.Q<Image>(CombatHudPresenter.ReticleElement).sprite);
            Assert.AreEqual(library.ModeBadge, root.Q<Image>(CombatHudPresenter.ModeBadgeElement).sprite);
            Assert.AreEqual(library.CameraBadge, root.Q<Image>(CombatHudPresenter.CameraBadgeElement).sprite);
        }

        [Test]
        public void Reticle_FollowsThePointerThroughTheCameraInverseMapping()
        {
            var go = new GameObject("hud-pointer-test");
            try
            {
                Camera camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                var rig = go.AddComponent<CombatCameraRig>();
                rig.SetCamera(camera);
                var hud = go.AddComponent<CombatHudBehaviour>();

                var screen = new Vector2(1920f, 1080f);
                var panel = new Vector2(960f, 540f);

                hud.SetPointer(new Vector2(480f, 270f), screen, panel);
                Assert.AreEqual(240f, hud.State.ReticleX, 0.01f);
                Assert.AreEqual(405f, hud.State.ReticleY, 0.01f);

                hud.SetPointer(Vector2.zero, screen, panel);
                Assert.AreEqual(0f, hud.State.ReticleX, 0.01f);
                Assert.AreEqual(540f, hud.State.ReticleY, 0.01f);

                // The world aim grows with the preset while the reticle stays under the cursor.
                rig.ApplyPreset(CameraDistancePreset.Medium);
                Vector2 centreMedium = rig.ScreenToViewport(new Vector2(960f, 540f), screen);
                hud.SetPointer(new Vector2(480f, 270f), screen, panel);
                Assert.AreEqual(240f, hud.State.ReticleX, 0.01f);
                Assert.AreEqual(405f, hud.State.ReticleY, 0.01f);

                Vector2 centreFar = rig.ScreenToViewport(new Vector2(960f, 540f), screen);
                Assert.AreEqual(1248f, centreMedium.x, 0.01f);
                Assert.AreEqual(702f, centreMedium.y, 0.01f);
                rig.ApplyPreset(CameraDistancePreset.Far);
                centreFar = rig.ScreenToViewport(new Vector2(960f, 540f), screen);
                Assert.AreEqual(1440f, centreFar.x, 0.01f);
                Assert.AreEqual(810f, centreFar.y, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator BootScene_HudDocumentBindsTheFrozenLayout()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return null;
            yield return null;

            CombatHudBehaviour hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud, "Boot scene must host the combat HUD.");

            UIDocument document = hud.GetComponent<UIDocument>();
            Assert.IsNotNull(document, "The HUD behaviour needs its UIDocument.");
            VisualElement root = document.rootVisualElement;
            Assert.IsNotNull(root, "The HUD document has no live root.");

            foreach (string element in new[]
                     {
                         CombatHudPresenter.MoneyPanelElement, CombatHudPresenter.MoneyElement,
                         CombatHudPresenter.PauseButtonElement, CombatHudPresenter.HelpButtonElement,
                         CombatHudPresenter.RadarElement, CombatHudPresenter.DistanceElement,
                         CombatHudPresenter.MissionTimerElement, CombatHudPresenter.AiCountElement,
                         CombatHudPresenter.TelemetryElement, CombatHudPresenter.LostPartsElement,
                         CombatHudPresenter.HullFillElement, CombatHudPresenter.ShieldFillElement,
                         CombatHudPresenter.EnergyFillElement, CombatHudPresenter.PilotPortraitElement,
                         CombatHudPresenter.PilotNameElement, CombatHudPresenter.MinimapElement,
                         CombatHudPresenter.CaptainPortraitElement, CombatHudPresenter.CaptainModeElement,
                         CombatHudPresenter.ModeBadgeElement, CombatHudPresenter.CameraBadgeElement,
                         CombatHudPresenter.ReticleElement
                     })
            {
                Assert.IsNotNull(root.Q(element), "Boot HUD document is missing: " + element);
            }

            Assert.IsNotNull(hud.Sprites, "The HUD must load Resources/UI/HudSpriteLibrary.");
            Assert.IsNotNull(root.Q<Image>(CombatHudPresenter.ReticleElement).sprite,
                "The HUD images must receive the original sprites.");
        }

        [Test]
        public void CameraRig_AppliesPresetsAndInverseMouseAimMapping()
        {
            var go = new GameObject("camera-rig-test");
            try
            {
                Camera camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                go.transform.position = Vector3.zero;

                var rig = go.AddComponent<CombatCameraRig>();
                rig.SetCamera(camera);

                rig.ApplyPreset(CameraDistancePreset.Medium);
                Assert.AreEqual(CameraDistancePreset.Medium, rig.Preset);
                Assert.AreEqual(468f, camera.orthographicSize, 0.001f);
                Assert.AreEqual(1664f, rig.ViewportWidth, 0.001f);

                Vector2 viewport = rig.ScreenToViewport(new Vector2(960f, 540f), new Vector2(1920f, 1080f));
                Assert.AreEqual(832f, viewport.x, 0.001f);
                Assert.AreEqual(468f, viewport.y, 0.001f);

                rig.CyclePreset();
                Assert.AreEqual(CameraDistancePreset.Far, rig.Preset);
                Assert.AreEqual(540f, camera.orthographicSize, 0.001f);

                Vector2 roundTrip = rig.ViewportToScreen(new Vector2(1920f, 1080f), new Vector2(1920f, 1080f));
                Assert.AreEqual(1920f, roundTrip.x, 0.001f);
                Assert.AreEqual(1080f, roundTrip.y, 0.001f);

                rig.CyclePreset();
                Assert.AreEqual(CameraDistancePreset.Short, rig.Preset);
                Assert.AreEqual(360f, camera.orthographicSize, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BatchSpriteRenderer_SubmitsBoundedBatchesWithoutPerPartGameObjects()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("BatchRendererGroup requires a graphics device; skipped in -nographics runs.");

            var go = new GameObject("batch-sprite-test");
            BatchSpriteRenderer renderer = null;
            try
            {
                renderer = go.AddComponent<BatchSpriteRenderer>();
                renderer.Configure(5000, null);

                var instance = new SpriteInstance
                {
                    X = 0f,
                    Y = 0f,
                    Width = 32f,
                    Height = 32f,
                    RotationRadians = 0f,
                    UvX = 0f,
                    UvY = 0f,
                    UvWidth = 1f,
                    UvHeight = 1f,
                    UserValue = SpriteUserValue.Pristine
                };

                for (int i = 0; i < 5000; i++)
                {
                    instance.X = i;
                    renderer.AddInstance(in instance);
                }

                renderer.Submit();

                Assert.AreEqual(5000, renderer.InstanceCount);
                Assert.AreEqual(2, renderer.BatchCount);
                Assert.AreEqual(2, renderer.DrawCommandCount);
                Assert.AreEqual(0, go.transform.childCount);
            }
            finally
            {
                if (renderer != null)
                    renderer.enabled = false;
                Object.DestroyImmediate(go);
            }
        }

        private static VisualElement CreateHudRoot()
        {
            var root = new VisualElement();
            root.Add(new Image { name = CombatHudPresenter.MoneyPanelElement });
            root.Add(new Label { name = CombatHudPresenter.MoneyElement });
            root.Add(new Image { name = CombatHudPresenter.PauseButtonElement });
            root.Add(new Image { name = CombatHudPresenter.HelpButtonElement });
            root.Add(new Image { name = CombatHudPresenter.RadarElement });
            root.Add(new Image { name = CombatHudPresenter.RadarArrowElement });
            root.Add(new Label { name = CombatHudPresenter.DistanceElement });
            root.Add(new Label { name = CombatHudPresenter.MissionTimerElement });
            root.Add(new Image { name = CombatHudPresenter.AiCountPanelElement });
            root.Add(new Label { name = CombatHudPresenter.AiCountElement });
            root.Add(new Image { name = CombatHudPresenter.TelemetryElement });
            root.Add(new Label { name = CombatHudPresenter.LostPartsElement });
            root.Add(new Label { name = CombatHudPresenter.HullValueElement });
            root.Add(new Image { name = CombatHudPresenter.HullFillElement });
            root.Add(new Image { name = CombatHudPresenter.ShieldFillElement });
            root.Add(new Image { name = CombatHudPresenter.EnergyFillElement });
            root.Add(new Image { name = CombatHudPresenter.PilotPortraitElement });
            root.Add(new Label { name = CombatHudPresenter.PilotNameElement });
            root.Add(new Image { name = CombatHudPresenter.MinimapElement });
            root.Add(new VisualElement { name = CombatHudPresenter.CaptainTileElement });
            root.Add(new Image { name = CombatHudPresenter.CaptainPortraitElement });
            root.Add(new VisualElement { name = CombatHudPresenter.CaptainHealthElement });
            root.Add(new Label { name = CombatHudPresenter.CaptainModeElement });
            root.Add(new Image { name = CombatHudPresenter.ModeBadgeElement });
            root.Add(new Label { name = CombatHudPresenter.ModeBadgeTextElement });
            root.Add(new Image { name = CombatHudPresenter.CameraBadgeElement });
            root.Add(new Label { name = CombatHudPresenter.CameraBadgeTextElement });
            root.Add(new Image { name = CombatHudPresenter.ReticleElement });
            return root;
        }
    }
}
