using System;
using UnityEngine;
using UnityEngine.Video;

namespace ABRevival.Presentation.Cinematics
{
    /// <summary>
    /// Plays a supplied sequence of cinematic clips and invokes the end function on
    /// completion or skip, mirroring the source UI movie presenter. Clips are silent;
    /// audio is provided elsewhere. Original movie content: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CinematicPlayer : MonoBehaviour
    {
        [SerializeField] private RenderTexture targetTexture;

        private VideoPlayer _videoPlayer;
        private VideoClip[] _clips;
        private int _index;
        private Action _endFunction;
        private bool _running;

        public VideoClip CurrentClip =>
            _running && _clips != null && _index < _clips.Length ? _clips[_index] : null;

        public bool IsRunning => _running;

        public bool IsPrepared => _videoPlayer != null && _videoPlayer.isPrepared;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = false;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.loopPointReached += OnLoopPointReached;
            _videoPlayer.prepareCompleted += OnPrepareCompleted;
            _videoPlayer.errorReceived += OnErrorReceived;

            if (targetTexture != null)
            {
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                _videoPlayer.targetTexture = targetTexture;
            }
            else
            {
                _videoPlayer.renderMode = VideoRenderMode.APIOnly;
            }
        }

        /// <summary>
        /// Starts sequential playback. The end function runs exactly once, whether the
        /// sequence completes or is skipped.
        /// </summary>
        public void Play(VideoClip[] clips, Action endFunction)
        {
            if (clips == null || clips.Length == 0)
            {
                endFunction?.Invoke();
                return;
            }

            StopCurrent();
            _clips = clips;
            _index = 0;
            _endFunction = endFunction;
            _running = true;
            PrepareCurrent();
        }

        /// <summary>Advances past the current clip as if it had reached its end.</summary>
        public void Skip()
        {
            if (_running)
            {
                Advance();
            }
        }

        private void PrepareCurrent()
        {
            _videoPlayer.Stop();
            _videoPlayer.clip = _clips[_index];
            _videoPlayer.Prepare();
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            if (_running && source.clip == _clips[_index] && source.isPrepared)
            {
                source.Play();
            }
        }

        private void OnLoopPointReached(VideoPlayer source)
        {
            Advance();
        }

        private void OnErrorReceived(VideoPlayer source, string message)
        {
            Debug.LogError($"[CinematicPlayer] Video error: {message}");
            Advance();
        }

        private void Advance()
        {
            if (!_running)
            {
                return;
            }

            _index++;
            if (_index < _clips.Length)
            {
                PrepareCurrent();
                return;
            }

            StopCurrent();
            var endFunction = _endFunction;
            _endFunction = null;
            endFunction?.Invoke();
        }

        private void StopCurrent()
        {
            _running = false;
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.clip = null;
            }
        }

        private void OnDestroy()
        {
            if (_videoPlayer == null)
            {
                return;
            }

            _videoPlayer.loopPointReached -= OnLoopPointReached;
            _videoPlayer.prepareCompleted -= OnPrepareCompleted;
            _videoPlayer.errorReceived -= OnErrorReceived;
        }
    }
}
