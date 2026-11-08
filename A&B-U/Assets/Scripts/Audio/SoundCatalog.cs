namespace ABRevival.Core.Audio
{
    /// <summary>
    /// Mixer bus for a clip. Mirrors the three groups in ABRevivalAudioMixer.
    /// </summary>
    public enum AudioBus
    {
        Music = 0,
        Sfx = 1,
        Ui = 2
    }

    /// <summary>
    /// Effect ids are the original SoundData.soundArray indices, extended with S_Click
    /// (present in the frozen bank but absent from the AS3 array). Do not reorder.
    /// </summary>
    public enum SoundEffect
    {
        S_Fire100 = 0,
        S_FireGT = 1,
        S_FireL1 = 2,
        S_FireL2 = 3,
        S_Bomb = 4,
        S_Hit = 5,
        S_Pow = 6,
        S_Rocket = 7,
        S_Fuel = 8,
        S_Money = 9,
        S_Repair = 10,
        S_Weapon = 11,
        S_Zin = 12,
        S_Cant = 13,
        S_Check = 14,
        S_Word = 15,
        S_Zout = 16,
        S_Yes = 17,
        S_No = 18,
        S_Start = 19,
        S_Eng = 20,
        S_Click = 21
    }

    /// <summary>
    /// Music ids are the original SoundData.musicArray indices. Do not reorder.
    /// </summary>
    public enum MusicTrack
    {
        M_Main = 0,
        M_Menu = 1,
        M_Game1 = 2,
        M_Game2 = 3,
        M_Game3 = 4,
        M_Game4 = 5,
        M_Game5 = 6,
        M_Game6 = 7,
        M_Game7 = 8,
        M_Creidts = 9,
        M_Win2 = 10
    }

    /// <summary>
    /// Semantic trigger names for the original playback sites. Presenters and domain
    /// adapters call these names instead of hard-coding clip ids or volumes.
    /// </summary>
    public enum AudioTrigger
    {
        MenuClick = 0,
        MenuConfirm = 1,
        MenuCancel = 2,
        MenuActionRejected = 3,
        AssemblyPartAttached = 4,
        AssemblyPartRemoved = 5,
        AssemblyActionRejected = 6,
        AssemblyCraftCleared = 7,
        AssemblyZoomIn = 8,
        AssemblyZoomOut = 9,
        AssemblyCraftSelected = 10,
        CombatWeaponAutocannon = 11,
        CombatWeaponLight = 12,
        CombatWeaponMedium = 13,
        CombatWeaponHeavy = 14,
        CombatWeaponRocket = 15,
        CombatExplosion = 16,
        CombatExplosionSmall = 17,
        CombatBooster = 18,
        CombatComponentLost = 19,
        CombatPowerRestored = 20,
        CombatTacticalModeChanged = 21,
        CombatCameraDistanceChanged = 22,
        CombatPickupHealth = 23,
        CombatPickupMoney = 24,
        CombatPickupPart = 25,
        MissionTalkAdvance = 26
    }

    public readonly struct SoundDefinition
    {
        public readonly string ClipName;
        public readonly AudioBus Bus;
        public readonly float DefaultVolume;

        public SoundDefinition(string clipName, AudioBus bus, float defaultVolume)
        {
            ClipName = clipName;
            Bus = bus;
            DefaultVolume = defaultVolume;
        }
    }

    public readonly struct AudioTriggerDefinition
    {
        public readonly SoundEffect Effect;
        public readonly AudioBus Bus;
        public readonly float Volume;

        public AudioTriggerDefinition(SoundEffect effect, AudioBus bus, float volume)
        {
            Effect = effect;
            Bus = bus;
            Volume = volume;
        }
    }

    /// <summary>
    /// Pure C# catalogue of the frozen sound bank (6_all.AllData_sound.bin).
    /// No Unity API is used, so the mapping is covered by the Tier 1 test gate.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class SoundCatalog
    {
        public const int EffectCount = 22;
        public const int MusicCount = 11;
        public const int TriggerCount = 27;

        public static readonly SoundDefinition[] Effects =
        {
            new SoundDefinition("S_Fire100", AudioBus.Sfx, 1.5f),
            new SoundDefinition("S_FireGT", AudioBus.Sfx, 1f),
            new SoundDefinition("S_FireL1", AudioBus.Sfx, 0.7f),
            new SoundDefinition("S_FireL2", AudioBus.Sfx, 0.8f),
            new SoundDefinition("S_Bomb", AudioBus.Sfx, 1f),
            new SoundDefinition("S_Hit", AudioBus.Sfx, 1f),
            new SoundDefinition("S_Pow", AudioBus.Sfx, 1f),
            new SoundDefinition("S_Rocket", AudioBus.Sfx, 0.2f),
            new SoundDefinition("S_Fuel", AudioBus.Sfx, 1f),
            new SoundDefinition("S_Money", AudioBus.Sfx, 0.5f),
            new SoundDefinition("S_Repair", AudioBus.Sfx, 1.4f),
            new SoundDefinition("S_Weapon", AudioBus.Sfx, 1f),
            new SoundDefinition("S_Zin", AudioBus.Ui, 1f),
            new SoundDefinition("S_Cant", AudioBus.Ui, 1f),
            new SoundDefinition("S_Check", AudioBus.Ui, 1f),
            new SoundDefinition("S_Word", AudioBus.Ui, 1f),
            new SoundDefinition("S_Zout", AudioBus.Ui, 1f),
            new SoundDefinition("S_Yes", AudioBus.Ui, 1f),
            new SoundDefinition("S_No", AudioBus.Ui, 1f),
            new SoundDefinition("S_Start", AudioBus.Ui, 1f),
            new SoundDefinition("S_Eng", AudioBus.Sfx, 1.15f),
            new SoundDefinition("S_Click", AudioBus.Ui, 1f)
        };

        public static readonly SoundDefinition[] Music =
        {
            new SoundDefinition("M_Main", AudioBus.Music, 1f),
            new SoundDefinition("M_Menu", AudioBus.Music, 1f),
            new SoundDefinition("M_Game1", AudioBus.Music, 1f),
            new SoundDefinition("M_Game2", AudioBus.Music, 1f),
            new SoundDefinition("M_Game3", AudioBus.Music, 1f),
            new SoundDefinition("M_Game4", AudioBus.Music, 1f),
            new SoundDefinition("M_Game5", AudioBus.Music, 1f),
            new SoundDefinition("M_Game6", AudioBus.Music, 1f),
            new SoundDefinition("M_Game7", AudioBus.Music, 1f),
            new SoundDefinition("M_Creidts", AudioBus.Music, 1f),
            new SoundDefinition("M_Win2", AudioBus.Music, 1f)
        };

        public static readonly AudioTriggerDefinition[] Triggers =
        {
            new AudioTriggerDefinition(SoundEffect.S_Start, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Yes, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_No, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Cant, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Check, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_No, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Cant, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_No, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Zin, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Zout, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Check, AudioBus.Ui, 1f),
            new AudioTriggerDefinition(SoundEffect.S_FireGT, AudioBus.Sfx, 1f),
            new AudioTriggerDefinition(SoundEffect.S_FireL1, AudioBus.Sfx, 0.7f),
            new AudioTriggerDefinition(SoundEffect.S_FireL2, AudioBus.Sfx, 0.8f),
            new AudioTriggerDefinition(SoundEffect.S_Fire100, AudioBus.Sfx, 1.5f),
            new AudioTriggerDefinition(SoundEffect.S_Rocket, AudioBus.Sfx, 0.2f),
            new AudioTriggerDefinition(SoundEffect.S_Bomb, AudioBus.Sfx, 1.5f),
            new AudioTriggerDefinition(SoundEffect.S_Bomb, AudioBus.Sfx, 0.4f),
            new AudioTriggerDefinition(SoundEffect.S_Pow, AudioBus.Sfx, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Cant, AudioBus.Sfx, 0.35f),
            new AudioTriggerDefinition(SoundEffect.S_Eng, AudioBus.Sfx, 1.15f),
            new AudioTriggerDefinition(SoundEffect.S_Check, AudioBus.Sfx, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Check, AudioBus.Sfx, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Repair, AudioBus.Sfx, 1.4f),
            new AudioTriggerDefinition(SoundEffect.S_Money, AudioBus.Sfx, 0.5f),
            new AudioTriggerDefinition(SoundEffect.S_Weapon, AudioBus.Sfx, 1f),
            new AudioTriggerDefinition(SoundEffect.S_Word, AudioBus.Ui, 1f)
        };

        public static SoundDefinition GetEffect(SoundEffect effect)
        {
            return Effects[(int)effect];
        }

        public static SoundDefinition GetMusic(MusicTrack track)
        {
            return Music[(int)track];
        }

        public static AudioTriggerDefinition GetTrigger(AudioTrigger trigger)
        {
            return Triggers[(int)trigger];
        }

        public static string ClipName(SoundEffect effect)
        {
            return Effects[(int)effect].ClipName;
        }

        public static string ClipName(MusicTrack track)
        {
            return Music[(int)track].ClipName;
        }
    }
}
