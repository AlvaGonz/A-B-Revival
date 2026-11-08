using ABRevival.Core.Audio;

namespace ABRevival.Presentation.Audio
{
    /// <summary>
    /// Original weapon report families from FireMode and EvtCtrl.
    /// </summary>
    public enum WeaponReport
    {
        Autocannon = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3,
        Rocket = 4
    }

    public enum PickupKind
    {
        Health = 0,
        Money = 1,
        Part = 2
    }

    /// <summary>
    /// Low-level playback surface. Everything is preloaded, so these calls do not
    /// allocate managed memory per trigger.
    /// </summary>
    public interface IAudioService
    {
        bool MusicEnabled { get; }
        bool SfxEnabled { get; }

        void PlayMusic(MusicTrack track, float volume = 1f, bool loop = true);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);

        void PlaySfx(SoundEffect effect, float volume = 1f);
        void PlayUi(SoundEffect effect, float volume = 1f);
        void PlayTrigger(AudioTrigger trigger);
    }

    /// <summary>Main-menu and options trigger points.</summary>
    public interface IMenuAudio
    {
        void PlayMainMenuMusic();
        void MenuClicked();
        void MenuConfirmed();
        void MenuCancelled();
        void MenuActionRejected();
    }

    /// <summary>Hangar assembly trigger points.</summary>
    public interface IAssemblyAudio
    {
        void PlayAssemblyMusic();
        void PartAttached();
        void PartRemoved();
        void ActionRejected();
        void CraftSelected();
        void CraftCleared();
        void ZoomedIn();
        void ZoomedOut();
    }

    /// <summary>Combat trigger points.</summary>
    public interface ICombatAudio
    {
        void WeaponFired(WeaponReport report);
        void Explosion(float intensity);
        void Booster();
        void ComponentLost();
        void PowerRestored();
        void TacticalModeChanged();
        void CameraDistanceChanged();
        void PickupCollected(PickupKind kind);
    }

    /// <summary>Mission, dialogue and end-of-mission trigger points.</summary>
    public interface IMissionAudio
    {
        void PlayBriefingMusic();
        void PlayMissionMusic(int storyIndex);
        void DuckMusic();
        void ResumeMusic();
        void TalkAdvanced();
        void MissionWon();
        void MissionLost();
        void PlayCreditsMusic();
    }
}
