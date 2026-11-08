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
        private const string FlyBinRelativePath = "../src/assets/binaryData/3_all.AllData_defFly.bin";
        private const string OutputDirectory = "Assets/Data/Parts";
        private const string ResourcesDirectory = "Assets/Resources/Data";

        private static readonly string[] RuntimeSourceNames =
        {
            "8_all.AllData_defPart",
            "2_all.AllData_defFire",
            "11_all.AllData_defBullet",
            "3_all.AllData_defFly"
        };

        [MenuItem("Tools/A&B Revival/Reimport Part Catalog")]
        public static void ReimportPartCatalog()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string partPath = Path.GetFullPath(Path.Combine(projectRoot, PartBinRelativePath));
            string firePath = Path.GetFullPath(Path.Combine(projectRoot, FireBinRelativePath));
            string bulletPath = Path.GetFullPath(Path.Combine(projectRoot, BulletBinRelativePath));
            string flyPath = Path.GetFullPath(Path.Combine(projectRoot, FlyBinRelativePath));

            if (!File.Exists(partPath) || !File.Exists(firePath) || !File.Exists(bulletPath) || !File.Exists(flyPath))
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

            CopyRuntimeSources(projectRoot);

            int createdCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var record in records)
                {
                    string safeName = MakeSafeFilename(record.partName);
                    string assetPath = $"{OutputDirectory}/Part_{record.partId}_{safeName}.asset";

                    var partSO = AssetDatabase.LoadAssetAtPath<PartDefinitionSO>(assetPath);
                    if (partSO == null)
                    {
                        partSO = ScriptableObject.CreateInstance<PartDefinitionSO>();
                        AssetDatabase.CreateAsset(partSO, assetPath);
                    }

                    record.PopulateScriptableObject(partSO);
                    UnityEditor.EditorUtility.SetDirty(partSO);
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

        /// <summary>
        /// Copies the frozen binaries the runtime parser needs into Resources/Data. Deterministic
        /// binary copies keep the shipped mission and the generated catalog on the same source.
        /// </summary>
        private static void CopyRuntimeSources(string projectRoot)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(ResourcesDirectory))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Data");
            }

            string sourceDirectory = Path.GetFullPath(Path.Combine(projectRoot, "../src/assets/binaryData"));
            for (int i = 0; i < RuntimeSourceNames.Length; i++)
            {
                string name = RuntimeSourceNames[i];
                string source = Path.Combine(sourceDirectory, name + ".bin");
                string destination = Path.GetFullPath(Path.Combine(projectRoot, ResourcesDirectory, name + ".bytes"));
                File.Copy(source, destination, overwrite: true);
                AssetDatabase.ImportAsset(
                    $"{ResourcesDirectory}/{name}.bytes",
                    ImportAssetOptions.ForceSynchronousImport);
            }
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
