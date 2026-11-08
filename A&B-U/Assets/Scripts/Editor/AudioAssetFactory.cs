using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace ABRevival.Editor
{
    /// <summary>
    /// Reproducible editor utility for the audio assets: creates the Music/SFX/UI
    /// AudioMixer and applies import settings to the extracted original clips.
    /// The mixer editor API is internal to Unity, so it is reached by reflection.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class AudioAssetFactory
    {
        private const string AudioFolder = "Assets/Resources/Audio";
        private const string ClipFolder = AudioFolder + "/Original";
        private const string MixerPath = AudioFolder + "/ABRevivalAudioMixer.mixer";

        [MenuItem("Tools/A&B Revival/Audio/Rebuild Audio Assets")]
        public static void RebuildAudioAssets()
        {
            EnsureFolder(AudioFolder);
            CreateMixer();
            ConfigureClipImportSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/A&B Revival/Audio/Create Mixer")]
        public static void CreateMixer()
        {
            EnsureFolder(AudioFolder);

            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null)
            {
                Debug.Log($"[AudioAssetFactory] Mixer already exists at {MixerPath}.");
                return;
            }

            Type controllerType = FindMixerControllerType();
            if (controllerType == null)
            {
                Debug.LogError("[AudioAssetFactory] UnityEditor.Audio.AudioMixerController was not found.");
                return;
            }

            MethodInfo create = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.Static);
            MethodInfo createGroup = controllerType.GetMethod(
                "CreateNewGroup",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(bool) },
                null);
            MethodInfo addChild = controllerType.GetMethod(
                "AddChildToParent",
                BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo masterGroup = controllerType.GetProperty("masterGroup");

            if (create == null || createGroup == null || addChild == null || masterGroup == null)
            {
                Debug.LogError("[AudioAssetFactory] AudioMixerController reflection surface changed.");
                return;
            }

            object controller = create.Invoke(null, new object[] { MixerPath });
            if (controller == null)
            {
                Debug.LogError($"[AudioAssetFactory] Could not create mixer at {MixerPath}.");
                return;
            }

            object master = masterGroup.GetValue(controller);
            CreateGroup(controller, master, createGroup, addChild, "Music");
            CreateGroup(controller, master, createGroup, addChild, "SFX");
            CreateGroup(controller, master, createGroup, addChild, "UI");

            EditorUtility.SetDirty((UnityEngine.Object)controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AudioAssetFactory] Created {MixerPath} with Music, SFX and UI groups.");
        }

        [MenuItem("Tools/A&B Revival/Audio/Configure Clip Import Settings")]
        public static void ConfigureClipImportSettings()
        {
            if (!AssetDatabase.IsValidFolder(ClipFolder))
            {
                Debug.LogWarning($"[AudioAssetFactory] Clip folder not found: {ClipFolder}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { ClipFolder });
            int configured = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                bool music = Path.GetFileNameWithoutExtension(path).StartsWith("M_", StringComparison.Ordinal);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = !music;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                configured++;
            }

            Debug.Log($"[AudioAssetFactory] Configured import settings for {configured} clips in {ClipFolder}.");
        }

        private static void CreateGroup(
            object controller,
            object master,
            MethodInfo createGroup,
            MethodInfo addChild,
            string name)
        {
            object group = createGroup.Invoke(controller, new object[] { name, false });
            if (group != null)
            {
                addChild.Invoke(controller, new[] { group, master });
            }
        }

        private static Type FindMixerControllerType()
        {
            return Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor.CoreModule");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
