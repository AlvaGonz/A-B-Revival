using System.IO;
using UnityEditor;
using UnityEngine;
using ABRevival.Core.Data;

namespace ABRevival.Editor
{
    /// <summary>
    /// Decodes the frozen level scripting payload (src/assets/binaryData/10_all.AllData_levels.bin)
    /// and writes one deterministic LevelDefinitionSO asset per story level into Assets/Data/Levels/.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class LevelDataImporter
    {
        private const string LevelBinRelativePath = "../src/assets/binaryData/10_all.AllData_levels.bin";
        private const string OutputDirectory = "Assets/Data/Levels";

        [MenuItem("Tools/A&B Revival/Reimport Level Data")]
        public static void ReimportLevelData()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string levelPath = Path.GetFullPath(Path.Combine(projectRoot, LevelBinRelativePath));

            if (!File.Exists(levelPath))
            {
                Debug.LogError($"[LevelDataImporter] Source level payload not found at expected path: {levelPath}");
                return;
            }

            var catalog = LevelDataParser.ParseFromFile(levelPath);
            if (catalog.LevelCount == 0)
            {
                Debug.LogWarning("[LevelDataImporter] No levels decoded from the binaryData payload.");
                return;
            }

            EnsureFolder();

            int createdCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < catalog.levels.Count; i++)
                {
                    var level = catalog.levels[i];
                    var levelSO = ScriptableObject.CreateInstance<LevelDefinitionSO>();
                    levelSO.Populate(level);

                    string safeName = MakeSafeFilename(level.background);
                    string assetPath = $"{OutputDirectory}/Level_{level.storyKey}_{safeName}.asset";
                    AssetDatabase.CreateAsset(levelSO, assetPath);
                    createdCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[LevelDataImporter] Imported {createdCount} LevelDefinitionSO assets into {OutputDirectory} " +
                      $"({catalog.EventCount} events, {catalog.SpawnCount} spawn acts).");
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputDirectory)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            AssetDatabase.CreateFolder("Assets/Data", "Levels");
        }

        private static string MakeSafeFilename(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                name = name.Replace(invalidChars[i], '_');
            return name;
        }
    }
}
