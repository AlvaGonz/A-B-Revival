using System;
using System.IO;
using ABRevival.Core.Audio;
using NUnit.Framework;

namespace ABRevival.Tests
{
    [TestFixture]
    public class AudioCatalogTests
    {
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null
                   && !Directory.Exists(Path.Combine(dir.FullName, ".git"))
                   && !File.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new InvalidOperationException("Repository root containing .git directory could not be located.");
            }

            return dir.FullName;
        }

        [Test]
        public void EffectCatalog_UsesOriginalSoundArrayIds()
        {
            Assert.AreEqual(22, SoundCatalog.EffectCount);
            Assert.AreEqual(22, SoundCatalog.Effects.Length);
            Assert.AreEqual(0, (int)SoundEffect.S_Fire100);
            Assert.AreEqual(20, (int)SoundEffect.S_Eng);
            Assert.AreEqual(21, (int)SoundEffect.S_Click);

            for (int i = 0; i < SoundCatalog.Effects.Length; i++)
            {
                var definition = SoundCatalog.Effects[i];
                Assert.AreEqual(((SoundEffect)i).ToString(), definition.ClipName, $"Effect id {i} drifted.");
                Assert.IsNotEmpty(definition.ClipName);
                Assert.Greater(definition.DefaultVolume, 0f);
            }
        }

        [Test]
        public void MusicCatalog_UsesOriginalMusicArrayIds()
        {
            Assert.AreEqual(11, SoundCatalog.MusicCount);
            Assert.AreEqual(11, SoundCatalog.Music.Length);
            Assert.AreEqual(0, (int)MusicTrack.M_Main);
            Assert.AreEqual(1, (int)MusicTrack.M_Menu);
            Assert.AreEqual(10, (int)MusicTrack.M_Win2);

            for (int i = 0; i < SoundCatalog.Music.Length; i++)
            {
                var definition = SoundCatalog.Music[i];
                Assert.AreEqual(((MusicTrack)i).ToString(), definition.ClipName, $"Music id {i} drifted.");
                Assert.AreEqual(AudioBus.Music, definition.Bus);
                Assert.Greater(definition.DefaultVolume, 0f);
            }
        }

        [Test]
        public void TriggerTable_CoversEveryTriggerWithValidClipBusAndVolume()
        {
            Assert.AreEqual(SoundCatalog.TriggerCount, Enum.GetValues(typeof(AudioTrigger)).Length);
            Assert.AreEqual(SoundCatalog.TriggerCount, SoundCatalog.Triggers.Length);

            for (int i = 0; i < SoundCatalog.Triggers.Length; i++)
            {
                var definition = SoundCatalog.Triggers[i];
                Assert.GreaterOrEqual((int)definition.Effect, 0, $"Trigger id {i} has an invalid effect.");
                Assert.Less((int)definition.Effect, SoundCatalog.EffectCount, $"Trigger id {i} has an invalid effect.");
                Assert.AreEqual(
                    definition.Effect.ToString(),
                    SoundCatalog.ClipName(definition.Effect),
                    $"Trigger id {i} points at a clip whose name does not match its id.");
                Assert.IsTrue(definition.Bus == AudioBus.Ui || definition.Bus == AudioBus.Sfx);
                Assert.Greater(definition.Volume, 0f, $"Trigger id {i} has a non-positive volume.");
                Assert.LessOrEqual(definition.Volume, 1.5f, $"Trigger id {i} exceeds the original 1.5 max gain.");
            }
        }

        [Test]
        public void MenuAssemblyAndMissionTriggers_RouteToUiBus()
        {
            Assert.AreEqual(AudioBus.Ui, SoundCatalog.GetTrigger(AudioTrigger.MenuClick).Bus);
            Assert.AreEqual(AudioBus.Ui, SoundCatalog.GetTrigger(AudioTrigger.MenuConfirm).Bus);
            Assert.AreEqual(AudioBus.Ui, SoundCatalog.GetTrigger(AudioTrigger.AssemblyPartAttached).Bus);
            Assert.AreEqual(AudioBus.Ui, SoundCatalog.GetTrigger(AudioTrigger.AssemblyZoomIn).Bus);
            Assert.AreEqual(AudioBus.Ui, SoundCatalog.GetTrigger(AudioTrigger.MissionTalkAdvance).Bus);
        }

        [Test]
        public void CombatTriggers_RouteToSfxBus_AndKeepOriginalVolumes()
        {
            Assert.AreEqual(AudioBus.Sfx, SoundCatalog.GetTrigger(AudioTrigger.CombatWeaponHeavy).Bus);
            Assert.AreEqual(1.5f, SoundCatalog.GetTrigger(AudioTrigger.CombatWeaponHeavy).Volume);
            Assert.AreEqual(1.5f, SoundCatalog.GetTrigger(AudioTrigger.CombatExplosion).Volume);
            Assert.AreEqual(0.4f, SoundCatalog.GetTrigger(AudioTrigger.CombatExplosionSmall).Volume);
            Assert.AreEqual(0.35f, SoundCatalog.GetTrigger(AudioTrigger.CombatComponentLost).Volume);
            Assert.AreEqual(1.15f, SoundCatalog.GetTrigger(AudioTrigger.CombatPowerRestored).Volume);
        }

        [Test]
        public void TriggerLookup_DoesNotAllocateManagedMemory()
        {
            SoundCatalog.GetTrigger(AudioTrigger.MenuClick);
            SoundCatalog.GetTrigger(AudioTrigger.CombatWeaponHeavy);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int checksum = 0;
            for (int i = 0; i < 10000; i++)
            {
                var definition = SoundCatalog.GetTrigger((AudioTrigger)(i % SoundCatalog.TriggerCount));
                checksum += (int)definition.Effect + (int)definition.Bus;
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, allocated, "Trigger lookup must not allocate managed memory.");
            Assert.Greater(checksum, 0);
        }

        [Test]
        public void EveryCatalogEntry_HasCommittedAudioClip()
        {
            string root = FindRepoRoot();
            string clipRoot = Path.Combine(root, "A&B-U", "Assets", "Resources", "Audio", "Original");

            foreach (var music in SoundCatalog.Music)
            {
                AssertClipExists(clipRoot, music.ClipName);
            }

            foreach (var effect in SoundCatalog.Effects)
            {
                AssertClipExists(clipRoot, effect.ClipName);
            }
        }

        private static void AssertClipExists(string clipRoot, string clipName)
        {
            string path = Path.Combine(clipRoot, clipName + ".mp3");
            Assert.IsTrue(File.Exists(path), $"Missing committed clip: {path}");
            Assert.Greater(new FileInfo(path).Length, 0L, $"Empty committed clip: {path}");
        }
    }
}
