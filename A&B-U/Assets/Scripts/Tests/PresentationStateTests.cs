using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using ABRevival.Core.Rendering;
using ABRevival.Core.Simulation;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 verification of the pure HUD/camera/BRG layout model. No Unity API
    /// and no engine startup, preserving the fast pre-commit gate (ADR 0016).
    /// </summary>
    [TestFixture]
    public class PresentationStateTests
    {
        [Test]
        public void CameraPresets_MatchAdr0005ScaleAndViewport()
        {
            Assert.AreEqual(1.0f, CameraDistancePresets.ViewScale(CameraDistancePreset.Short), 0.0001f);
            Assert.AreEqual(1.3f, CameraDistancePresets.ViewScale(CameraDistancePreset.Medium), 0.0001f);
            Assert.AreEqual(1.5f, CameraDistancePresets.ViewScale(CameraDistancePreset.Far), 0.0001f);

            Assert.AreEqual(1280f, CameraDistancePresets.ViewportWidth(CameraDistancePreset.Short), 0.001f);
            Assert.AreEqual(720f, CameraDistancePresets.ViewportHeight(CameraDistancePreset.Short), 0.001f);
            Assert.AreEqual(1664f, CameraDistancePresets.ViewportWidth(CameraDistancePreset.Medium), 0.001f);
            Assert.AreEqual(936f, CameraDistancePresets.ViewportHeight(CameraDistancePreset.Medium), 0.001f);
            Assert.AreEqual(1920f, CameraDistancePresets.ViewportWidth(CameraDistancePreset.Far), 0.001f);
            Assert.AreEqual(1080f, CameraDistancePresets.ViewportHeight(CameraDistancePreset.Far), 0.001f);

            Assert.AreEqual("[ CAM: SHORT (1.0x) ]", CameraDistancePresets.BadgeText(CameraDistancePreset.Short));
            Assert.AreEqual("[ CAM: MEDIUM (1.3x) ]", CameraDistancePresets.BadgeText(CameraDistancePreset.Medium));
            Assert.AreEqual("[ CAM: FAR (1.5x) ]", CameraDistancePresets.BadgeText(CameraDistancePreset.Far));
        }

        [Test]
        public void CameraPresets_CycleWrapsShortMediumFar()
        {
            Assert.AreEqual(CameraDistancePreset.Medium, CameraDistancePresets.Next(CameraDistancePreset.Short));
            Assert.AreEqual(CameraDistancePreset.Far, CameraDistancePresets.Next(CameraDistancePreset.Medium));
            Assert.AreEqual(CameraDistancePreset.Short, CameraDistancePresets.Next(CameraDistancePreset.Far));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => CameraDistancePresets.ViewScale((CameraDistancePreset)3));
        }

        [Test]
        public void CameraViewport_MapsMouseIntoTheScaledDesignSpace()
        {
            var screenSize = new ViewportPoint(1920f, 1080f);
            var screenCentre = new ViewportPoint(960f, 540f);

            ViewportPoint shortCentre = CameraDistancePresets.ScreenToViewport(
                screenCentre, screenSize, CameraDistancePreset.Short);
            Assert.AreEqual(640f, shortCentre.X, 0.001f);
            Assert.AreEqual(360f, shortCentre.Y, 0.001f);

            ViewportPoint farCentre = CameraDistancePresets.ScreenToViewport(
                screenCentre, screenSize, CameraDistancePreset.Far);
            Assert.AreEqual(960f, farCentre.X, 0.001f);
            Assert.AreEqual(540f, farCentre.Y, 0.001f);

            ViewportPoint farCorner = CameraDistancePresets.ScreenToViewport(
                new ViewportPoint(1920f, 1080f), screenSize, CameraDistancePreset.Far);
            Assert.AreEqual(1920f, farCorner.X, 0.001f);
            Assert.AreEqual(1080f, farCorner.Y, 0.001f);

            ViewportPoint worldCorner = CameraDistancePresets.ViewportToWorldCentered(
                farCorner, CameraDistancePreset.Far);
            Assert.AreEqual(960f, worldCorner.X, 0.001f);
            Assert.AreEqual(540f, worldCorner.Y, 0.001f);
        }

        [Test]
        public void CameraViewport_InverseRoundTripsAtEveryPreset()
        {
            var screenSize = new ViewportPoint(1920f, 1080f);
            var screen = new ViewportPoint(1234f, 567f);

            foreach (CameraDistancePreset preset in new[]
                     { CameraDistancePreset.Short, CameraDistancePreset.Medium, CameraDistancePreset.Far })
            {
                ViewportPoint viewport = CameraDistancePresets.ScreenToViewport(screen, screenSize, preset);
                ViewportPoint roundTrip = CameraDistancePresets.ViewportToScreen(viewport, screenSize, preset);

                Assert.AreEqual(screen.X, roundTrip.X, 0.001f, preset.ToString());
                Assert.AreEqual(screen.Y, roundTrip.Y, 0.001f, preset.ToString());
            }
        }

        [Test]
        public void HudState_HullHealthClampsToValidRange()
        {
            var state = new CombatHudState();
            Assert.AreEqual(1f, state.HullFraction, 0.0001f);

            state.SetHullHealth(1250f, 1000f);
            Assert.AreEqual(1000f, state.HullCurrent, 0.0001f);
            Assert.AreEqual(1f, state.HullFraction, 0.0001f);

            state.SetHullHealth(-10f, 1000f);
            Assert.AreEqual(0f, state.HullCurrent, 0.0001f);
            Assert.AreEqual(0f, state.HullFraction, 0.0001f);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.SetHullHealth(1f, 0f));
        }

        [Test]
        public void HudState_RecordsLostPartsByCategoryAndIgnoresTheBody()
        {
            var state = new CombatHudState();
            state.RecordLostPart(PartCategory.Body);

            Assert.AreEqual(0, state.TotalLostParts);

            state.RecordLostPart(PartCategory.Gun);
            state.RecordLostPart(PartCategory.Gun);
            state.RecordLostPart(PartCategory.Wing);
            state.RecordLostPart(PartCategory.Engine);
            state.RecordLostPart(BattleDamageCategory.Module);
            state.RecordLostPart((PartCategory)9); // legacy type >= 4 is a module

            Assert.AreEqual(2, state.LostGuns);
            Assert.AreEqual(1, state.LostWings);
            Assert.AreEqual(1, state.LostEngines);
            Assert.AreEqual(2, state.LostModules);
            Assert.AreEqual(6, state.TotalLostParts);
        }

        [Test]
        public void HudState_ExposesCaptainPresenceAndModeCode()
        {
            var state = new CombatHudState();
            state.SetCaptainPresence(true);
            state.SetCaptainMode(CaptainTacticalMode.SeekAndDestroy);

            CombatHudSnapshot snapshot = state.Snapshot();
            Assert.IsTrue(snapshot.CaptainPresent);
            Assert.AreEqual(CaptainTacticalMode.SeekAndDestroy, snapshot.CaptainMode);
            Assert.AreEqual("SD", snapshot.CaptainModeCode);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => state.SetCaptainMode((CaptainTacticalMode)6));
        }

        [Test]
        public void HudState_CameraBadgeFadesOutAfterOnePointFiveSeconds()
        {
            var state = new CombatHudState();
            state.SetCameraDistance(CameraDistancePreset.Medium);
            Assert.IsFalse(state.CameraBadgeVisible);
            Assert.AreEqual("[ CAM: MEDIUM (1.3x) ]", state.Snapshot().CameraBadgeText);

            CameraDistancePreset next = state.CycleCameraDistance();
            Assert.AreEqual(CameraDistancePreset.Far, next);
            Assert.IsTrue(state.CameraBadgeVisible);
            Assert.AreEqual(1f, state.CameraBadgeAlpha, 0.0001f);

            state.Tick(1.0f);
            Assert.AreEqual(1f, state.CameraBadgeAlpha, 0.0001f);

            state.Tick(0.25f);
            Assert.AreEqual(0.5f, state.CameraBadgeAlpha, 0.0001f);

            state.Tick(0.25f);
            Assert.IsFalse(state.CameraBadgeVisible);
            Assert.AreEqual(0f, state.CameraBadgeAlpha, 0.0001f);

            state.ShowCameraDistanceBadge();
            state.Tick(-1f);
            Assert.IsTrue(state.CameraBadgeVisible);
        }

        [Test]
        public void HudTextFormatter_MatchesTheFrozenBaselineWording()
        {
            var state = new CombatHudState();
            state.RecordLostPart(PartCategory.Gun);
            state.RecordLostPart(PartCategory.Gun);
            state.RecordLostPart(PartCategory.Wing);
            state.RecordLostPart(PartCategory.Engine);
            state.RecordLostPart(BattleDamageCategory.Module);

            Assert.AreEqual(
                "LOST: [Gun x2] [Wing x1] [Engine x1] [Module x1]",
                HudTextFormatter.FormatLostParts(state.Snapshot()));

            Assert.AreEqual("LOST: [Wing x1]", FormatSingleWing());
            Assert.AreEqual(string.Empty, HudTextFormatter.FormatLostParts(new CombatHudState().Snapshot()));
            Assert.AreEqual("750 / 1000", HudTextFormatter.FormatHull(750f, 1000f));
        }

        [Test]
        public void HudState_ExposesTheLiveFrozenReadouts()
        {
            var state = new CombatHudState();
            state.SetMoney(1234);
            state.SetMissionSeconds(92.4f);
            state.SetAiCount(3);
            state.SetEnemyDistance(812.6f);
            state.SetShieldFraction(0.25f);
            state.SetPilotName("Michael");
            state.SetRadarBearing(-90f);
            state.SetReticlePosition(480f, 270f);
            state.SetCaptainHealth(250f, 1000f);

            CombatHudSnapshot snapshot = state.Snapshot();
            Assert.AreEqual(1234, snapshot.Money);
            Assert.AreEqual(92.4f, snapshot.MissionSeconds, 0.0001f);
            Assert.AreEqual(3, snapshot.AiCount);
            Assert.AreEqual(812.6f, snapshot.EnemyDistance, 0.0001f);
            Assert.AreEqual(0.25f, snapshot.ShieldFraction, 0.0001f);
            Assert.AreEqual("Michael", snapshot.PilotName);
            Assert.AreEqual(-90f, snapshot.RadarBearingDegrees, 0.0001f);
            Assert.AreEqual(480f, snapshot.ReticleX, 0.0001f);
            Assert.AreEqual(270f, snapshot.ReticleY, 0.0001f);
            Assert.AreEqual(0.25f, snapshot.CaptainHealthFraction, 0.0001f);

            Assert.AreEqual("1,234 G", HudTextFormatter.FormatMoney(1234));
            Assert.AreEqual("01:32", HudTextFormatter.FormatMissionTime(92.4f));
            Assert.AreEqual("AI x 3", HudTextFormatter.FormatAiCount(3));
            Assert.AreEqual("813", HudTextFormatter.FormatDistance(812.6f));
            Assert.AreEqual("TACTICAL --", HudTextFormatter.FormatModeBadge(false, CaptainTacticalMode.Anchor));
            Assert.AreEqual("TACTICAL FS", HudTextFormatter.FormatModeBadge(true, CaptainTacticalMode.FireSupport));
            Assert.AreEqual("[ CAM: SHORT (1.0x) ]", state.Snapshot().CameraBadgeText);
        }

        [Test]
        public void HudState_ClampsOutOfRangeLiveValues()
        {
            var state = new CombatHudState();

            state.SetMoney(-5);
            state.SetMissionSeconds(-1f);
            state.SetAiCount(-2);
            state.SetEnemyDistance(-3f);
            state.SetShieldFraction(-1f);

            Assert.AreEqual(0, state.Money);
            Assert.AreEqual(0f, state.MissionSeconds, 0.0001f);
            Assert.AreEqual(0, state.AiCount);
            Assert.AreEqual(0f, state.EnemyDistance, 0.0001f);
            Assert.AreEqual(0f, state.ShieldFraction, 0.0001f);

            Assert.AreEqual("0 G", HudTextFormatter.FormatMoney(0));
            Assert.AreEqual("00:00", HudTextFormatter.FormatMissionTime(-1f));
            Assert.Throws<ArgumentException>(() => state.SetMissionSeconds(float.NaN));
            Assert.Throws<ArgumentException>(() => state.SetShieldFraction(float.NaN));
        }

        [Test]
        public void SpriteBatchPlan_BoundsBatchesAt4096Instances()
        {
            Assert.AreEqual(0, SpriteBatchPlan.BatchCount(0));
            Assert.AreEqual(1, SpriteBatchPlan.BatchCount(1));
            Assert.AreEqual(1, SpriteBatchPlan.BatchCount(4096));
            Assert.AreEqual(2, SpriteBatchPlan.BatchCount(4097));
            Assert.AreEqual(3, SpriteBatchPlan.BatchCount(10000));

            Assert.AreEqual(0, SpriteBatchPlan.BatchStart(0));
            Assert.AreEqual(4096, SpriteBatchPlan.BatchStart(1));
            Assert.AreEqual(4096, SpriteBatchPlan.BatchInstanceCount(10000, 0));
            Assert.AreEqual(4096, SpriteBatchPlan.BatchInstanceCount(10000, 1));
            Assert.AreEqual(1808, SpriteBatchPlan.BatchInstanceCount(10000, 2));
            Assert.AreEqual(0, SpriteBatchPlan.BatchInstanceCount(10000, 3));
        }

        [Test]
        public void SpriteBatchPlan_BufferLayoutIsAlignedAndDisjoint()
        {
            const int capacity = 5000;
            int userOffset = SpriteBatchPlan.UserValueOffset(capacity);
            int uvOffset = SpriteBatchPlan.UvRectOffset(capacity);
            int totalBytes = SpriteBatchPlan.BufferBytes(capacity);

            Assert.AreEqual(0, SpriteBatchPlan.ObjectToWorldOffset(capacity));
            Assert.AreEqual(0, userOffset % 16);
            Assert.GreaterOrEqual(userOffset, SpriteBatchPlan.PackedTransformBytes * capacity);
            Assert.AreEqual(0, uvOffset % 16);
            Assert.GreaterOrEqual(uvOffset, userOffset + SpriteBatchPlan.UserValueBytes * capacity);
            Assert.AreEqual(0, totalBytes % 16);
            Assert.GreaterOrEqual(totalBytes, uvOffset + SpriteBatchPlan.UvRectBytes * capacity);
            Assert.AreEqual(0x80000080u, SpriteBatchPlan.MetadataValue(128));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpriteBatchPlan.MetadataValue(-1));
        }

        [Test]
        public void SpriteUserValue_PacksPristineAndDamageTints()
        {
            Assert.AreEqual(0xFFFFFFFFu, SpriteUserValue.DamageTint(0f));
            Assert.AreEqual(0xFF0000FFu, SpriteUserValue.DamageTint(1f));
            Assert.AreEqual(0xFF8080FFu, SpriteUserValue.DamageTint(0.5f));
            Assert.AreEqual(0xFFFFFFFFu, SpriteUserValue.DamageTint(-5f));
            Assert.AreEqual(0xFF0000FFu, SpriteUserValue.DamageTint(5f));
            Assert.AreEqual(0x04030201u, SpriteUserValue.PackTint(1, 2, 3, 4));
        }

        [Test]
        public void PackedTransform_MapsTheUnitQuadToSpriteExtents()
        {
            PackedTransform transform = PackedTransform.FromTrs(100f, 200f, 40f, 20f, 0f);

            Assert.AreEqual(40f, transform.c0x, 0.001f);
            Assert.AreEqual(0f, transform.c0y, 0.001f);
            Assert.AreEqual(0f, transform.c1x, 0.001f);
            Assert.AreEqual(20f, transform.c1y, 0.001f);
            Assert.AreEqual(100f, transform.c3x, 0.001f);
            Assert.AreEqual(200f, transform.c3y, 0.001f);

            PackedTransform rotated = PackedTransform.FromTrs(0f, 0f, 40f, 20f, MathF.PI * 0.5f);
            Assert.AreEqual(0f, rotated.c0x, 0.001f);
            Assert.AreEqual(40f, rotated.c0y, 0.001f);
            Assert.AreEqual(-20f, rotated.c1x, 0.001f);
            Assert.AreEqual(0f, rotated.c1y, 0.001f);
        }

        private static string FormatSingleWing()
        {
            var state = new CombatHudState();
            state.RecordLostPart(PartCategory.Wing);
            return HudTextFormatter.FormatLostParts(state.Snapshot());
        }
    }
}
