using System.Collections;
using ABRevival.Presentation.Cinematics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;

namespace ABRevival.Tests.PlayMode
{
    public sealed class CinematicPlayerTests
    {
        [UnityTest]
        public IEnumerator ClipReportsDurationAndSkipCompletes()
        {
            VideoClip clip = Resources.Load<VideoClip>("Cinematics/Mov1");
            Assert.IsNotNull(clip, "Mov1.mp4 must be committed under Assets/Resources/Cinematics.");
            Assert.Greater(clip.length, 0f, "The imported clip must report a duration.");

            var playerObject = new GameObject("CinematicPlayerTest");
            var player = playerObject.AddComponent<CinematicPlayer>();
            int completedCount = 0;
            player.Play(new[] { clip }, () => completedCount++);

            yield return null;
            player.Skip();
            yield return null;

            Assert.AreEqual(1, completedCount, "Skip must invoke the end function exactly once.");
            Assert.IsFalse(player.IsRunning, "The player must stop after the last clip is skipped.");

            Object.Destroy(playerObject);
        }
    }
}
