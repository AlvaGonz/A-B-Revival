using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using ABRevival.Core.Rendering;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// PlayMode smoke coverage for the presentation seams: HUD state transitions,
    /// camera preset switching, and bounded BatchRendererGroup submission.
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
            var cameraBadge = root.Q<Label>(CombatHudPresenter.CameraBadgeElement);

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
            Assert.IsTrue(captainMode.visible);
            Assert.AreEqual("[ CAM: MEDIUM (1.3x) ]", cameraBadge.text);
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
            var state = new CombatHudState();
            var presenter = new CombatHudPresenter(root);

            presenter.Render(state.Snapshot());
            Assert.AreEqual(string.Empty, lostParts.text);
            Assert.IsFalse(captainMode.visible);

            state.SetCaptainPresence(true);
            state.SetCaptainMode(CaptainTacticalMode.Evade);
            presenter.Render(state.Snapshot());
            Assert.AreEqual("EV", captainMode.text);
            Assert.IsTrue(captainMode.visible);

            state.SetCaptainPresence(false);
            presenter.Render(state.Snapshot());
            Assert.IsFalse(captainMode.visible);
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
                Assert.AreEqual(702f, camera.orthographicSize, 0.001f);
                Assert.AreEqual(2496f, rig.ViewportWidth, 0.001f);

                Vector2 viewport = rig.ScreenToViewport(new Vector2(960f, 540f), new Vector2(1920f, 1080f));
                Assert.AreEqual(1248f, viewport.x, 0.001f);
                Assert.AreEqual(702f, viewport.y, 0.001f);

                Vector2 worldAim = rig.ScreenToWorldAim(new Vector2(1920f, 1080f), new Vector2(1920f, 1080f));
                Assert.AreEqual(1248f, worldAim.x, 0.001f);
                Assert.AreEqual(702f, worldAim.y, 0.001f);

                rig.CyclePreset();
                Assert.AreEqual(CameraDistancePreset.Far, rig.Preset);
                Assert.AreEqual(810f, camera.orthographicSize, 0.001f);

                Vector2 roundTrip = rig.ViewportToScreen(new Vector2(2880f, 1620f), new Vector2(1920f, 1080f));
                Assert.AreEqual(1920f, roundTrip.x, 0.001f);
                Assert.AreEqual(1080f, roundTrip.y, 0.001f);

                rig.CyclePreset();
                Assert.AreEqual(CameraDistancePreset.Short, rig.Preset);
                Assert.AreEqual(540f, camera.orthographicSize, 0.001f);
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
            root.Add(new Label { name = CombatHudPresenter.HullValueElement });
            root.Add(new VisualElement { name = CombatHudPresenter.HullFillElement });
            root.Add(new Label { name = CombatHudPresenter.LostPartsElement });
            root.Add(new Label { name = CombatHudPresenter.CaptainModeElement });
            root.Add(new Label { name = CombatHudPresenter.CameraBadgeElement });
            return root;
        }
    }
}
