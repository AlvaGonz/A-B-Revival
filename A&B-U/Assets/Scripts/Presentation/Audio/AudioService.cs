using ABRevival.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace ABRevival.Presentation.Audio
{
    /// <summary>
    /// Runtime audio service for the frozen sound bank. Clips and mixer groups are
    /// resolved once in Awake; trigger calls only touch preallocated AudioSource voices.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, IAudioService, IMenuAudio, IAssemblyAudio, ICombatAudio, IMissionAudio, ITalkAudio
    {
        private const string MixerResourcePath = "Audio/ABRevivalAudioMixer";
        private const string ClipResourceRoot = "Audio/Original/";
        private const int SfxVoiceCount = 16;
        private const int UiVoiceCount = 4;

        private const float Silence = 0.01f;
        private const float FadeInPerFrame = 0.01f;
        private const float ResumePerFrame = 0.05f;
        private const float DuckVolume = 0.2f;

        private static readonly MusicTrack[] EarlyGameTracks =
        {
            MusicTrack.M_Game1, MusicTrack.M_Game2, MusicTrack.M_Game3, MusicTrack.M_Game4
        };

        private static readonly MusicTrack[] MidGameTracks =
        {
            MusicTrack.M_Game1, MusicTrack.M_Game2, MusicTrack.M_Game3, MusicTrack.M_Game4, MusicTrack.M_Game7
        };

        private static readonly MusicTrack[] AllGameTracks =
        {
            MusicTrack.M_Game1, MusicTrack.M_Game2, MusicTrack.M_Game3, MusicTrack.M_Game4,
            MusicTrack.M_Game5, MusicTrack.M_Game6, MusicTrack.M_Game7
        };

        private static readonly MusicTrack[] Game5ThenSix =
        {
            MusicTrack.M_Game5, MusicTrack.M_Game1, MusicTrack.M_Game2, MusicTrack.M_Game3,
            MusicTrack.M_Game4, MusicTrack.M_Game6, MusicTrack.M_Game7
        };

        public static AudioService Instance { get; private set; }

        public AudioMixerGroup MusicOutputGroup { get; private set; }
        public AudioMixerGroup SfxOutputGroup { get; private set; }
        public AudioMixerGroup UiOutputGroup { get; private set; }

        public bool MusicEnabled => _musicEnabled;
        public bool SfxEnabled => _sfxEnabled;

        private AudioSource _musicSource;
        private AudioSource[] _sfxSources;
        private AudioSource[] _uiSources;
        private AudioClip[] _effectClips;
        private AudioClip[] _musicClips;
        private int _nextSfxVoice;
        private int _nextUiVoice;

        private bool _musicEnabled = true;
        private bool _sfxEnabled = true;
        private float _musicVolume = Silence;
        private float _musicTarget = Silence;
        private float _musicStep = FadeInPerFrame;
        private MusicTrack[] _missionPlaylist;
        private int _missionPlaylistIndex;

        public static AudioService EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var host = new GameObject("ABRevival Audio");
            return host.AddComponent<AudioService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AudioService] Duplicate instance rejected.", this);
                enabled = false;
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadMixer();
            LoadClips();
            BuildSources();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            TickMusicVolume();
            TickMissionPlaylist();
        }

        private void LoadMixer()
        {
            AudioMixer mixer = Resources.Load<AudioMixer>(MixerResourcePath);
            if (mixer == null)
            {
                Debug.LogError($"[AudioService] Missing AudioMixer at Resources/{MixerResourcePath}.");
                return;
            }

            MusicOutputGroup = FindGroup(mixer, "Music");
            SfxOutputGroup = FindGroup(mixer, "SFX");
            UiOutputGroup = FindGroup(mixer, "UI");
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(name);
            if (groups != null && groups.Length > 0)
            {
                return groups[0];
            }

            Debug.LogError($"[AudioService] Mixer group '{name}' is missing.");
            return null;
        }

        private void LoadClips()
        {
            _effectClips = new AudioClip[SoundCatalog.EffectCount];
            for (int i = 0; i < _effectClips.Length; i++)
            {
                string clipName = SoundCatalog.Effects[i].ClipName;
                _effectClips[i] = LoadClip(clipName);
            }

            _musicClips = new AudioClip[SoundCatalog.MusicCount];
            for (int i = 0; i < _musicClips.Length; i++)
            {
                string clipName = SoundCatalog.Music[i].ClipName;
                _musicClips[i] = LoadClip(clipName);
            }
        }

        private static AudioClip LoadClip(string clipName)
        {
            AudioClip clip = Resources.Load<AudioClip>(ClipResourceRoot + clipName);
            if (clip == null)
            {
                Debug.LogError($"[AudioService] Missing clip at Resources/{ClipResourceRoot}{clipName}.");
            }

            return clip;
        }

        private void BuildSources()
        {
            _musicSource = CreateVoice("Music Voice", MusicOutputGroup, true, 0f);
            _sfxSources = CreateVoicePool("SFX Voice", SfxVoiceCount, SfxOutputGroup);
            _uiSources = CreateVoicePool("UI Voice", UiVoiceCount, UiOutputGroup);
        }

        private AudioSource[] CreateVoicePool(string prefix, int count, AudioMixerGroup group)
        {
            var voices = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                voices[i] = CreateVoice($"{prefix} {i}", group, false, 0f);
            }

            return voices;
        }

        private AudioSource CreateVoice(string name, AudioMixerGroup group, bool loop, float spatialBlend)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);

            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = spatialBlend;

            if (group != null)
            {
                source.outputAudioMixerGroup = group;
            }

            return source;
        }

        private void TickMusicVolume()
        {
            if (_musicSource == null)
            {
                return;
            }

            if (_musicVolume != _musicTarget)
            {
                float step = _musicStep * (Time.unscaledDeltaTime * 60f);
                _musicVolume = Mathf.MoveTowards(_musicVolume, _musicTarget, step);
                _musicSource.volume = _musicVolume;
            }
        }

        private void TickMissionPlaylist()
        {
            if (_missionPlaylist == null || _musicSource == null || _musicSource.isPlaying)
            {
                return;
            }

            if (!PlayMusicInternal(_missionPlaylist[_missionPlaylistIndex], 1f, false))
            {
                _missionPlaylist = null;
                return;
            }

            _missionPlaylistIndex++;
            if (_missionPlaylistIndex >= _missionPlaylist.Length)
            {
                _missionPlaylistIndex = 0;
            }
        }

        public void PlayMusic(MusicTrack track, float volume = 1f, bool loop = true)
        {
            _missionPlaylist = null;
            PlayMusicInternal(track, volume, loop);
        }

        private bool PlayMusicInternal(MusicTrack track, float volume, bool loop)
        {
            if (_musicSource == null)
            {
                return false;
            }

            AudioClip clip = _musicClips[(int)track];
            if (clip == null)
            {
                return false;
            }

            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = Silence;

            _musicVolume = Silence;
            _musicTarget = _musicEnabled ? volume : Silence;
            _musicStep = FadeInPerFrame;
            _musicSource.Play();
            return true;
        }

        public void StopMusic()
        {
            _missionPlaylist = null;
            if (_musicSource == null)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
            _musicVolume = Silence;
            _musicTarget = Silence;
        }

        public void PauseMusic()
        {
            _musicTarget = DuckVolume;
            _musicStep = ResumePerFrame;
        }

        public void ResumeMusic()
        {
            _musicTarget = _musicEnabled ? 1f : Silence;
            _musicStep = ResumePerFrame;
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            _musicTarget = enabled ? 1f : Silence;
            _musicStep = enabled ? ResumePerFrame : FadeInPerFrame;
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
        }

        public void PlaySfx(SoundEffect effect, float volume = 1f)
        {
            PlayEffect(_sfxSources, ref _nextSfxVoice, effect, volume);
        }

        public void PlayUi(SoundEffect effect, float volume = 1f)
        {
            PlayEffect(_uiSources, ref _nextUiVoice, effect, volume);
        }

        public void PlayTrigger(AudioTrigger trigger)
        {
            AudioTriggerDefinition definition = SoundCatalog.GetTrigger(trigger);
            if (definition.Bus == AudioBus.Ui)
            {
                PlayUi(definition.Effect, definition.Volume);
            }
            else
            {
                PlaySfx(definition.Effect, definition.Volume);
            }
        }

        private void PlayEffect(AudioSource[] voices, ref int nextVoice, SoundEffect effect, float volume)
        {
            if (!_sfxEnabled || voices == null)
            {
                return;
            }

            int index = (int)effect;
            if (index < 0 || index >= _effectClips.Length)
            {
                return;
            }

            AudioClip clip = _effectClips[index];
            if (clip == null)
            {
                return;
            }

            AudioSource voice = voices[nextVoice];
            nextVoice++;
            if (nextVoice >= voices.Length)
            {
                nextVoice = 0;
            }

            voice.Stop();
            voice.clip = clip;
            voice.PlayOneShot(clip, volume);
        }

        public void PlayMainMenuMusic()
        {
            PlayMusic(MusicTrack.M_Main, 1f);
        }

        public void MenuClicked()
        {
            PlayTrigger(AudioTrigger.MenuClick);
        }

        public void MenuConfirmed()
        {
            PlayTrigger(AudioTrigger.MenuConfirm);
        }

        public void MenuCancelled()
        {
            PlayTrigger(AudioTrigger.MenuCancel);
        }

        public void MenuActionRejected()
        {
            PlayTrigger(AudioTrigger.MenuActionRejected);
        }

        public void PlayAssemblyMusic()
        {
            PlayMusic(MusicTrack.M_Menu, SoundCatalog.GetMusic(MusicTrack.M_Menu).DefaultVolume);
        }

        public void PartAttached()
        {
            PlayTrigger(AudioTrigger.AssemblyPartAttached);
        }

        public void PartRemoved()
        {
            PlayTrigger(AudioTrigger.AssemblyPartRemoved);
        }

        public void ActionRejected()
        {
            PlayTrigger(AudioTrigger.AssemblyActionRejected);
        }

        public void CraftSelected()
        {
            PlayTrigger(AudioTrigger.AssemblyCraftSelected);
        }

        public void CraftCleared()
        {
            PlayTrigger(AudioTrigger.AssemblyCraftCleared);
        }

        public void ZoomedIn()
        {
            PlayTrigger(AudioTrigger.AssemblyZoomIn);
        }

        public void ZoomedOut()
        {
            PlayTrigger(AudioTrigger.AssemblyZoomOut);
        }

        public void WeaponFired(WeaponReport report)
        {
            switch (report)
            {
                case WeaponReport.Autocannon:
                    PlayTrigger(AudioTrigger.CombatWeaponAutocannon);
                    break;
                case WeaponReport.Light:
                    PlayTrigger(AudioTrigger.CombatWeaponLight);
                    break;
                case WeaponReport.Medium:
                    PlayTrigger(AudioTrigger.CombatWeaponMedium);
                    break;
                case WeaponReport.Heavy:
                    PlayTrigger(AudioTrigger.CombatWeaponHeavy);
                    break;
                case WeaponReport.Rocket:
                    PlayTrigger(AudioTrigger.CombatWeaponRocket);
                    break;
            }
        }

        public void Explosion(float intensity)
        {
            PlayTrigger(intensity < 0.5f
                ? AudioTrigger.CombatExplosionSmall
                : AudioTrigger.CombatExplosion);
        }

        public void Booster()
        {
            PlayTrigger(AudioTrigger.CombatBooster);
        }

        public void ComponentLost()
        {
            PlayTrigger(AudioTrigger.CombatComponentLost);
        }

        public void PowerRestored()
        {
            PlayTrigger(AudioTrigger.CombatPowerRestored);
        }

        public void TacticalModeChanged()
        {
            PlayTrigger(AudioTrigger.CombatTacticalModeChanged);
        }

        public void CameraDistanceChanged()
        {
            PlayTrigger(AudioTrigger.CombatCameraDistanceChanged);
        }

        public void PickupCollected(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Health:
                    PlayTrigger(AudioTrigger.CombatPickupHealth);
                    break;
                case PickupKind.Money:
                    PlayTrigger(AudioTrigger.CombatPickupMoney);
                    break;
                case PickupKind.Part:
                    PlayTrigger(AudioTrigger.CombatPickupPart);
                    break;
                case PickupKind.Fuel:
                    PlayTrigger(AudioTrigger.CombatPickupFuel);
                    break;
            }
        }

        public void PlayBriefingMusic()
        {
            PlayMusic(MusicTrack.M_Menu, 0.25f);
        }

        public void PlayMissionMusic(int storyIndex)
        {
            if (storyIndex == 0 || storyIndex == 7)
            {
                PlayMusic(MusicTrack.M_Game3, 1f);
                return;
            }

            if (storyIndex == 8)
            {
                PlayMusic(MusicTrack.M_Game6, 1f);
                return;
            }

            if (storyIndex == 9 || storyIndex == 10)
            {
                PlayMusic(MusicTrack.M_Game7, 1f);
                return;
            }

            if (storyIndex == 49)
            {
                PlayMusic(MusicTrack.M_Creidts, 1f, false);
                return;
            }

            MusicTrack[] playlist;
            if (storyIndex == 34)
            {
                playlist = Game5ThenSix;
            }
            else if (storyIndex == 99)
            {
                playlist = AllGameTracks;
            }
            else if (storyIndex < 9)
            {
                playlist = EarlyGameTracks;
            }
            else
            {
                playlist = MidGameTracks;
            }

            _missionPlaylist = playlist;
            _missionPlaylistIndex = 1;
            if (!PlayMusicInternal(playlist[0], 1f, false))
            {
                _missionPlaylist = null;
            }
        }

        public void DuckMusic()
        {
            PauseMusic();
        }

        public void TalkAdvanced()
        {
            PlayTrigger(AudioTrigger.MissionTalkAdvance);
        }

        public void PlayAdvanceClick()
        {
            TalkAdvanced();
        }

        public void MissionWon()
        {
            PlayMusic(MusicTrack.M_Win2, 1f);
        }

        public void MissionLost()
        {
            StopMusic();
        }

        public void PlayCreditsMusic()
        {
            PlayMusic(MusicTrack.M_Creidts, 1f, false);
        }
    }
}
