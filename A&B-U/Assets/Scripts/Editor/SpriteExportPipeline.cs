using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ABRevival.Core.Data;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ABRevival.Editor
{
    /// <summary>
    /// Regenerates the modular part and UI sprites from the frozen SWF symbol
    /// libraries, applies the PPU 1 import contract, and rebuilds the BC7/ASTC
    /// sprite atlases. Original game IP: Vargames (2011).
    /// </summary>
    public static class SpriteExportPipeline
    {
        private const string PartBin = "../src/assets/binaryData/8_all.AllData_defPart.bin";
        private const string FireBin = "../src/assets/binaryData/2_all.AllData_defFire.bin";
        private const string BulletBin = "../src/assets/binaryData/11_all.AllData_defBullet.bin";
        private const string PartSwf = "../src/assets/binaryData/4_all.AllData_data.bin";
        private const string UiSwf = "../src/assets/binaryData/9_all.AllData_ui.bin";

        private const string PartsOutput = "Assets/Art/Sprites/Parts";
        private const string UiOutput = "Assets/Art/Sprites/UI";
        private const string AtlasOutput = "Assets/Art/Atlases";
        private const string PartsAtlasPath = AtlasOutput + "/PartsAtlas.spriteatlasv2";
        private const string UiAtlasPath = AtlasOutput + "/UiAtlas.spriteatlasv2";
        private const int AtlasMaxSize = 2048;

        private static readonly Regex SymbolClassLine = new Regex("^(\\d+);\"(.*)\"$", RegexOptions.Compiled);
        private static readonly Regex ImageFileName = new Regex("^(\\d+)$", RegexOptions.Compiled);
        private static readonly Regex HiddenLayerTag = new Regex(
            "(?s)<item type=\"PlaceObject2Tag\"[^>]*name=\"[LHPF][^\"]*\"[^>]*>.*?</item>",
            RegexOptions.Compiled);

        [MenuItem("Tools/A&B Revival/Regenerate Sprites and Atlases")]
        public static void RegenerateFromMenu()
        {
            Run();
        }

        /// <summary>
        /// Batchmode entry point:
        /// Unity.exe -batchmode -quit -projectPath "A&amp;B-U" -executeMethod ABRevival.Editor.SpriteExportPipeline.RegenerateBatch
        /// </summary>
        public static void RegenerateBatch()
        {
            if (!Run()) EditorApplication.Exit(1);
        }

        private static bool Run()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string tempRoot = Path.Combine(Path.GetTempPath(), "ab-revival-sprite-export");

            try
            {
                string partBin = Resolve(projectRoot, PartBin);
                string fireBin = Resolve(projectRoot, FireBin);
                string bulletBin = Resolve(projectRoot, BulletBin);
                string partSwf = Resolve(projectRoot, PartSwf);
                string uiSwf = Resolve(projectRoot, UiSwf);
                AssertFilesExist(partBin, fireBin, bulletBin, partSwf, uiSwf);

                string ffdec = ResolveFfdec();
                Debug.Log($"[SpriteExportPipeline] FFDec: {ffdec}");

                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
                Directory.CreateDirectory(tempRoot);

                var records = PartCatalogParser.ParseCatalogFromFiles(partBin, fireBin, bulletBin);
                if (records.Count == 0) throw new InvalidOperationException("Part catalog is empty.");

                var mappings = PartSpriteMap.Build(records);
                var canonicalBySymbol = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var mapping in mappings)
                {
                    if (!canonicalBySymbol.ContainsKey(mapping.mcName))
                    {
                        canonicalBySymbol[mapping.mcName] = mapping.partId;
                    }
                }

                string symbolDir = Path.Combine(tempRoot, "symbols");
                Directory.CreateDirectory(symbolDir);
                string renderSwf = PrepareRenderSwf(ffdec, partSwf, tempRoot);
                RunFfdec(ffdec, "-export symbolClass " + Quote(symbolDir) + " " + Quote(renderSwf));
                string symbolsCsv = Path.Combine(symbolDir, "symbols.csv");
                if (!File.Exists(symbolsCsv))
                {
                    throw new InvalidOperationException("FFDec did not write symbols.csv for " + renderSwf);
                }

                var symbolIds = ReadSymbolIds(symbolsCsv);
                var ids = new List<string>(canonicalBySymbol.Count);
                foreach (var symbol in canonicalBySymbol)
                {
                    if (!symbolIds.TryGetValue(symbol.Key, out int characterId))
                    {
                        throw new InvalidOperationException(
                            $"SWF symbol '{symbol.Key}' for part {symbol.Value} was not exported by FFDec.");
                    }
                    ids.Add(characterId.ToString(CultureInfo.InvariantCulture));
                }
                ids.Sort(StringComparer.Ordinal);

                string partsTemp = Path.Combine(tempRoot, "parts");
                Directory.CreateDirectory(partsTemp);
                RunFfdec(
                    ffdec,
                    "-selectid " + string.Join(",", ids.ToArray()) +
                    " -format sprite:png -zoom " + SpriteNaming.ExportZoom +
                    " -ignorebackground -export sprite " + Quote(partsTemp) + " " + Quote(renderSwf));

                int exportedParts = 0;
                var expectedParts = new HashSet<string>(StringComparer.Ordinal);
                foreach (var symbol in canonicalBySymbol)
                {
                    string fileName = SpriteNaming.PartSpriteFile(symbol.Value, symbol.Key);
                    expectedParts.Add(fileName);

                    int characterId = symbolIds[symbol.Key];
                    string sourceFolder = FindSpriteFolder(partsTemp, characterId);
                    string sourcePng = Path.Combine(sourceFolder, "1.png");
                    if (!File.Exists(sourcePng))
                    {
                        throw new InvalidOperationException(
                            $"FFDec did not render frame 1 for symbol '{symbol.Key}' ({sourcePng}).");
                    }

                    CopyIfChanged(sourcePng, Path.Combine(projectRoot, PartsOutput, fileName));
                    exportedParts++;
                }
                RemoveStalePngs(Path.Combine(projectRoot, PartsOutput), expectedParts);

                string uiTemp = Path.Combine(tempRoot, "ui");
                Directory.CreateDirectory(uiTemp);
                RunFfdec(ffdec, "-format image:png -export image " + Quote(uiTemp) + " " + Quote(uiSwf));

                var expectedUi = new HashSet<string>(StringComparer.Ordinal);
                int exportedUi = 0;
                foreach (string image in Directory.GetFiles(uiTemp, "*.png"))
                {
                    string id = Path.GetFileNameWithoutExtension(image);
                    Match match = ImageFileName.Match(id);
                    if (!match.Success)
                    {
                        throw new InvalidOperationException("Unexpected FFDec image name: " + id);
                    }

                    string fileName = SpriteNaming.UiImageFile(
                        int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                    expectedUi.Add(fileName);
                    CopyIfChanged(image, Path.Combine(projectRoot, UiOutput, fileName));
                    exportedUi++;
                }
                RemoveStalePngs(Path.Combine(projectRoot, UiOutput), expectedUi);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ApplyImportSettings(PartsOutput);
                ApplyImportSettings(UiOutput);
                RebuildAtlas(PartsAtlasPath, PartsOutput);
                RebuildAtlas(UiAtlasPath, UiOutput);

                if (!PartsAtlasBuilder.Build())
                {
                    throw new InvalidOperationException("The BRG parts atlas could not be rebuilt.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    $"[SpriteExportPipeline] Exported {exportedParts} part sprites " +
                    $"({canonicalBySymbol.Count} symbols, {records.Count} catalog ids) and " +
                    $"{exportedUi} UI images. Atlases: {PartsAtlasPath}, {UiAtlasPath}.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[SpriteExportPipeline] " + exception);
                return false;
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }

        internal static void ApplyImportSettings(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;
                dirty |= Set(importer.textureType != TextureImporterType.Sprite,
                    () => importer.textureType = TextureImporterType.Sprite);
                dirty |= Set(importer.spriteImportMode != SpriteImportMode.Single,
                    () => importer.spriteImportMode = SpriteImportMode.Single);
                dirty |= Set(!Mathf.Approximately(importer.spritePixelsPerUnit, 1f),
                    () => importer.spritePixelsPerUnit = 1f);
                dirty |= Set(importer.filterMode != FilterMode.Bilinear,
                    () => importer.filterMode = FilterMode.Bilinear);
                dirty |= Set(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
                dirty |= Set(importer.wrapMode != TextureWrapMode.Clamp,
                    () => importer.wrapMode = TextureWrapMode.Clamp);
                dirty |= Set(!importer.alphaIsTransparency,
                    () => importer.alphaIsTransparency = true);
                dirty |= Set(importer.textureCompression != TextureImporterCompression.Uncompressed,
                    () => importer.textureCompression = TextureImporterCompression.Uncompressed);
                dirty |= Set(importer.crunchedCompression,
                    () => importer.crunchedCompression = false);
                dirty |= Set(importer.maxTextureSize != AtlasMaxSize,
                    () => importer.maxTextureSize = AtlasMaxSize);
                dirty |= Set(!importer.sRGBTexture, () => importer.sRGBTexture = true);
                dirty |= Set(importer.isReadable, () => importer.isReadable = false);
                dirty |= Set(importer.npotScale != TextureImporterNPOTScale.None,
                    () => importer.npotScale = TextureImporterNPOTScale.None);

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool pivotDirty =
                    settings.spriteAlignment != (int)SpriteAlignment.Custom ||
                    settings.spritePivot != new Vector2(0.5f, 0.5f) ||
                    settings.spriteMeshType != SpriteMeshType.FullRect ||
                    settings.spriteExtrude != 0 ||
                    settings.spriteGenerateFallbackPhysicsShape;
                if (pivotDirty)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteExtrude = 0;
                    settings.spriteGenerateFallbackPhysicsShape = false;
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                if (dirty) importer.SaveAndReimport();
            }
        }

        private static bool Set(bool changed, Action apply)
        {
            if (!changed) return false;
            apply();
            return true;
        }

        internal static void RebuildAtlas(string atlasPath, string packableFolder)
        {
            if (AssetDatabase.IsValidFolder(AtlasOutput) == false)
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art", "Atlases"));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            // Recreated every run so the packable list and importer settings are exact.
            AssetDatabase.DeleteAsset(atlasPath);
            var atlas = new SpriteAtlasAsset();
            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);

            atlas = SpriteAtlasAsset.Load(atlasPath);
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(packableFolder);
            if (folder == null) throw new InvalidOperationException("Missing packable folder: " + packableFolder);
            atlas.Add(new[] { folder });
            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("SpriteAtlasImporter missing for " + atlasPath);
            }

            SpriteAtlasTextureSettings texture = importer.textureSettings;
            texture.filterMode = FilterMode.Bilinear;
            texture.generateMipMaps = false;
            texture.readable = false;
            importer.textureSettings = texture;

            SpriteAtlasPackingSettings packing = importer.packingSettings;
            packing.padding = 4;
            packing.blockOffset = 1;
            packing.enableRotation = false;
            packing.enableTightPacking = true;
            packing.enableAlphaDilation = true;
            importer.packingSettings = packing;

            importer.includeInBuild = true;
            importer.SetPlatformSettings(PlatformSettings("Standalone", TextureImporterFormat.BC7));
            importer.SetPlatformSettings(PlatformSettings("Android", TextureImporterFormat.ASTC_6x6));
            importer.SetPlatformSettings(PlatformSettings("iPhone", TextureImporterFormat.ASTC_6x6));
            importer.SaveAndReimport();

            Debug.Log($"[SpriteExportPipeline] Atlas rebuilt: {atlasPath}");
        }

        private static TextureImporterPlatformSettings PlatformSettings(string platform, TextureImporterFormat format)
        {
            return new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                format = format,
                maxTextureSize = AtlasMaxSize,
                textureCompression = TextureImporterCompression.CompressedHQ,
                crunchedCompression = false,
                allowsAlphaSplitting = false
            };
        }

        private static string PrepareRenderSwf(string ffdec, string sourceSwf, string tempRoot)
        {
            string sourceXml = Path.Combine(tempRoot, "symbols.xml");
            string cleanXml = Path.Combine(tempRoot, "symbols-clean.xml");
            string cleanSwf = Path.Combine(tempRoot, "symbols-clean.swf");

            RunFfdec(ffdec, "-swf2xml " + Quote(sourceSwf) + " " + Quote(sourceXml));

            string xml = File.ReadAllText(sourceXml);
            MatchCollection hiddenLayers = HiddenLayerTag.Matches(xml);
            xml = HiddenLayerTag.Replace(xml, string.Empty);
            File.WriteAllText(cleanXml, xml, new UTF8Encoding(false));

            RunFfdec(ffdec, "-xml2swf " + Quote(cleanXml) + " " + Quote(cleanSwf));
            Debug.Log($"[SpriteExportPipeline] Removed {hiddenLayers.Count} hidden L/H/F/P marker layers.");
            return cleanSwf;
        }

        private static Dictionary<string, int> ReadSymbolIds(string csvPath)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(csvPath))
            {
                Match match = SymbolClassLine.Match(line.Trim());
                if (!match.Success) continue;
                int id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                result[match.Groups[2].Value] = id;
            }
            return result;
        }

        private static string FindSpriteFolder(string root, int characterId)
        {
            string[] matches = Directory.GetDirectories(root, "DefineSprite_" + characterId + "_*");
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one FFDec sprite folder for character {characterId}, found {matches.Length}.");
            }
            return matches[0];
        }

        private static void RemoveStalePngs(string folder, HashSet<string> expected)
        {
            string absolute = Path.GetFullPath(folder);
            if (!Directory.Exists(absolute)) Directory.CreateDirectory(absolute);
            foreach (string file in Directory.GetFiles(absolute, "*.png"))
            {
                if (!expected.Contains(Path.GetFileName(file))) File.Delete(file);
            }
        }

        internal static void CopyIfChanged(string source, string destination)
        {
            string directory = Path.GetDirectoryName(destination);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (File.Exists(destination) && FilesEqual(source, destination)) return;
            File.Copy(source, destination, true);
        }

        private static bool FilesEqual(string left, string right)
        {
            var leftInfo = new FileInfo(left);
            var rightInfo = new FileInfo(right);
            if (leftInfo.Length != rightInfo.Length) return false;

            using (var leftStream = File.OpenRead(left))
            using (var rightStream = File.OpenRead(right))
            {
                int leftByte;
                while ((leftByte = leftStream.ReadByte()) != -1)
                {
                    if (leftByte != rightStream.ReadByte()) return false;
                }
            }
            return true;
        }

        private static string Resolve(string projectRoot, string relative)
        {
            return Path.GetFullPath(Path.Combine(projectRoot, relative));
        }

        private static void AssertFilesExist(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Required source asset missing.", path);
            }
        }

        internal static string ResolveFfdec()
        {
            string fromEnvironment = Environment.GetEnvironmentVariable("FFDEC_CLI");
            if (!string.IsNullOrEmpty(fromEnvironment) && File.Exists(fromEnvironment)) return fromEnvironment;

            string[] candidates =
            {
                @"C:\Program Files (x86)\FFDec\ffdec-cli.exe",
                @"C:\Program Files\FFDec\ffdec-cli.exe"
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            return "ffdec-cli";
        }

        private static void RunFfdec(string executable, string arguments)
        {
            var startInfo = new ProcessStartInfo(executable, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) throw new InvalidOperationException("Could not start " + executable);
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"{executable} exited with {process.ExitCode}.\n{stdout}\n{stderr}");
                }
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
