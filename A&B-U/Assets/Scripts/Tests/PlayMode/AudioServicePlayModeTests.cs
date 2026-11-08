using System.Collections;
using ABRevival.Core.Audio;
using ABRevival.Presentation.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;

namespace ABRevival.Tests.PlayMode
{
    [TestFixture]
    public class AudioServicePlayModeTests
    {
        [UnityTest]
        public IEnumerator AudioService_RoutesMusicSfxAndUiThroughMixerGroups()
        {
            var host = new GameObject("AudioServiceTest");
            var service = host.AddComponent<AudioService>();
            yield return null;

            Assert.IsNotNull(service.MusicOutputGroup, "Music mixer group missing.");
            Assert.IsNotNull(service.SfxOutputGroup, "SFX mixer group missing.");
            Assert.IsNotNull(service.UiOutputGroup, "UI mixer group missing.");
            Assert.AreEqual("Music", service.MusicOutputGroup.name);
            Assert.AreEqual("SFX", service.SfxOutputGroup.name);
            Assert.AreEqual("UI", service.UiOutputGroup.name);

            AudioSource[] sources = host.GetComponentsInChildren<AudioSource>(true);
            Assert.AreEqual(1, CountWithGroup(sources, service.MusicOutputGroup));
            Assert.AreEqual(16, CountWithGroup(sources, service.SfxOutputGroup));
            Assert.AreEqual(4, CountWithGroup(sources, service.UiOutputGroup));

            service.PlayMusic(MusicTrack.M_Main);
            service.PlaySfx(SoundEffect.S_FireGT);
            service.PlayUi(SoundEffect.S_Start);
            yield return null;

            Assert.IsTrue(HasClipInGroup(sources, service.MusicOutputGroup, "M_Main"));
            Assert.IsTrue(HasClipInGroup(sources, service.SfxOutputGroup, "S_FireGT"));
            Assert.IsTrue(HasClipInGroup(sources, service.UiOutputGroup, "S_Start"));

            Object.Destroy(host);
            yield return null;
        }

        private static int CountWithGroup(AudioSource[] sources, AudioMixerGroup group)
        {
            int count = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].outputAudioMixerGroup == group)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasClipInGroup(AudioSource[] sources, AudioMixerGroup group, string clipName)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source.outputAudioMixerGroup == group && source.clip != null && source.clip.name == clipName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
