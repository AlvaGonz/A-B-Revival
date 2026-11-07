using System.IO;
using UnityEditor;
using UnityEngine;
using ABRevival.Core.Data;

namespace ABRevival.Editor
{
    /// <summary>
    /// Unity Editor utility that parses raw XML part definitions from the frozen AS3 baseline
    /// (src/assets/binaryData/8_all.AllData_defPart.bin, 2_all.AllData_defFire.bin, 11_all.AllData_defBullet.bin)
    /// and generates deterministic PartDefinitionSO asset files in Assets/Data/Parts/.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class PartCatalogImporter
    {
        private const string PartBinRelativePath = "../src/assets/binaryData/8_all.AllData_defPart.bin";
        private const string FireBinRelativePath = "../src/assets/binaryData/2_all.AllData_defFire.bin";
        private const string BulletBinRelativePath = "../src/assets/binaryData/11_all.AllData_defBullet.bin";
        private const string OutputDirectory = "Assets/Data/Parts";

        [MenuItem("Tools/A&B Revival/Reimport Part Catalog")]
        public static void ReimportPartCatalog()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string partPath = Path.GetFullPath(Path.Combine(projectRoot, PartBinRelativePath));
            string firePath = Path.GetFullPath(Path.Combine(projectRoot, FireBinRelativePath));
            string bulletPath = Path.GetFullPath(Path.Combine(projectRoot, BulletBinRelativePath));

            if (!File.Exists(partPath) || !File.Exists(firePath) || !File.Exists(bulletPath))
            {
                Debug.LogError($"[PartCatalogImporter] Source binaryData files not found at expected paths relative to project root: {partPath}");
                return;
            }

            var records = PartCatalogParser.ParseCatalogFromFiles(partPath, firePath, bulletPath);
            if (records == null || records.Count == 0)
            {
                Debug.LogWarning("[PartCatalogImporter] No parts parsed from binaryData XML files.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputDirectory))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                {
                    AssetDatabase.CreateFolder("Assets", "Data");
                }
                AssetDatabase.CreateFolder("Assets/Data", "Parts");
            }

            int createdCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var record in records)
                {
                    var partSO = ScriptableObject.CreateInstance<PartDefinitionSO>();
                    record.PopulateScriptableObject(partSO);

                    string safeName = MakeSafeFilename(record.partName);
                    string assetPath = $"{OutputDirectory}/Part_{record.partId}_{safeName}.asset";

                    AssetDatabase.CreateAsset(partSO, assetPath);
                    createdCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[PartCatalogImporter] Successfully imported {createdCount} PartDefinitionSO assets into {OutputDirectory}.");
        }

        private static string MakeSafeFilename(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
