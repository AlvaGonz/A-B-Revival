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

        private const string PartsOutput = "Assets/Art/Sprites/Parts";
        private const string MountsOutput = "Assets/Art/Sprites/PartMounts";
        private const string BulletsOutput = "Assets/Art/Sprites/Bullets";
        private const string AtlasOutput = "Assets/Art/Atlases";
        private const string PartsAtlasPath = AtlasOutput + "/PartsAtlas.spriteatlasv2";
        private const int AtlasMaxSize = 2048;

        private static readonly Regex SymbolClassLine = new Regex("^(\\d+);\"(.*)\"$", RegexOptions.Compiled);
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
                AssertFilesExist(partBin, fireBin, bulletBin, partSwf);

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

                int exportedMounts = ExportMountArt(
                    ffdec, renderSwf, symbolIds, canonicalBySymbol, bulletBin, projectRoot, tempRoot);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ApplyImportSettings(PartsOutput);
                ApplyImportSettings(MountsOutput);
                ApplyImportSettings(BulletsOutput);
                RebuildAtlas(PartsAtlasPath, PartsOutput);

                if (!PartsAtlasBuilder.Build())
                {
                    throw new InvalidOperationException("The BRG parts atlas could not be rebuilt.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    $"[SpriteExportPipeline] Exported {exportedParts} part thumbnails and {exportedMounts} mount sprites " +
                    $"({canonicalBySymbol.Count} symbols, {records.Count} catalog ids). Atlases: {PartsAtlasPath}.");
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

        /// <summary>
        /// Exports the in-game frame-0 mount art at <see cref="SpriteNaming.MountZoom"/> and
        /// rotates it a quarter turn counter-clockwise. The frozen wrapper symbol draws its
        /// SHOW frame nose-east (the 0-radian frame of <c>MC2BMPD</c>); Unity headings are
        /// nose-up, so the same pixels are stored unrotated by 90 degrees. Bullet art uses the
        /// same rule; ids whose frozen symbol is the procedural <c>normal</c> disc use a baked
        /// copy of that original disc rather than a synthetic white cell.
        /// </summary>
        private static int ExportMountArt(
            string ffdec,
            string renderSwf,
            Dictionary<string, int> symbolIds,
            Dictionary<string, int> canonicalBySymbol,
            string bulletBin,
            string projectRoot,
            string tempRoot)
        {
            List<BulletArtRecord> bullets = BulletCatalog.ParseFromFile(bulletBin);
            var bulletSymbols = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < bullets.Count; i++)
            {
                BulletArtRecord bullet = bullets[i];
                if (bullet.mcName == "normal" || bulletSymbols.ContainsKey(bullet.mcName)) continue;
                if (!symbolIds.TryGetValue(bullet.mcName, out int bulletId))
                    throw new InvalidOperationException($"Bullet symbol '{bullet.mcName}' was not exported by FFDec.");
                bulletSymbols.Add(bullet.mcName, bulletId);
            }

            var ids = new List<string>(canonicalBySymbol.Count + bulletSymbols.Count);
            foreach (var symbol in canonicalBySymbol)
                ids.Add(symbolIds[symbol.Key].ToString(CultureInfo.InvariantCulture));
            foreach (var symbol in bulletSymbols)
                ids.Add(symbol.Value.ToString(CultureInfo.InvariantCulture));
            ids.Sort(StringComparer.Ordinal);

            string mountsTemp = Path.Combine(tempRoot, "mounts");
            Directory.CreateDirectory(mountsTemp);
            RunFfdec(
                ffdec,
                "-selectid " + string.Join(",", ids.ToArray()) +
                " -format sprite:png -zoom " + SpriteNaming.MountZoom +
                " -ignorebackground -export sprite " + Quote(mountsTemp) + " " + Quote(renderSwf));

            var expectedMounts = new HashSet<string>(StringComparer.Ordinal);
            int exported = 0;
            foreach (var symbol in canonicalBySymbol)
            {
                string fileName = SpriteNaming.PartMountFile(symbol.Value, symbol.Key);
                expectedMounts.Add(fileName);

                string sourcePng = Path.Combine(FindSpriteFolder(mountsTemp, symbolIds[symbol.Key]), "1.png");
                if (!File.Exists(sourcePng))
                    throw new InvalidOperationException($"FFDec did not render mount art for symbol '{symbol.Key}'.");
                CopyRotatedCounterClockwise(sourcePng, Path.Combine(projectRoot, MountsOutput, fileName));
                exported++;
            }
            RemoveStalePngs(Path.Combine(projectRoot, MountsOutput), expectedMounts);

            var expectedBullets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var symbol in bulletSymbols)
            {
                string fileName = SpriteNaming.BulletSpriteFile(symbol.Key);
                expectedBullets.Add(fileName);

                string sourcePng = Path.Combine(FindSpriteFolder(mountsTemp, symbol.Value), "1.png");
                if (!File.Exists(sourcePng))
                    throw new InvalidOperationException($"FFDec did not render bullet art for symbol '{symbol.Key}'.");
                CopyRotatedCounterClockwise(sourcePng, Path.Combine(projectRoot, BulletsOutput, fileName));
            }

            string normalFile = SpriteNaming.BulletSpriteFile("normal");
            expectedBullets.Add(normalFile);
            WriteBytesIfChanged(
                Path.Combine(projectRoot, BulletsOutput, normalFile),
                BakeNormalBulletDisc());

            RemoveStalePngs(Path.Combine(projectRoot, BulletsOutput), expectedBullets);
            return exported;
        }

        private static void CopyRotatedCounterClockwise(string source, string destination)
        {
            byte[] bytes = File.ReadAllBytes(source);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false))
                    throw new InvalidDataException("Could not decode exported sprite " + source);

                int width = texture.width;
                int height = texture.height;
                Color32[] sourcePixels = texture.GetPixels32();
                int destWidth = height;
                int destHeight = width;
                var destPixels = new Color32[destWidth * destHeight];

                for (int y = 0; y < destHeight; y++)
                {
                    for (int x = 0; x < destWidth; x++)
                    {
                        // Visual top-left rotation: dst(x, y) = src(width - 1 - y, x).
                        int sourceX = width - 1 - y;
                        int sourceY = x;
                        destPixels[((destHeight - 1 - y) * destWidth) + x] =
                            sourcePixels[((height - 1 - sourceY) * width) + sourceX];
                    }
                }

                var rotated = new Texture2D(destWidth, destHeight, TextureFormat.RGBA32, false, false);
                try
                {
                    rotated.SetPixels32(destPixels);
                    rotated.Apply(false, false);
                    WriteBytesIfChanged(destination, rotated.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(rotated);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Frozen <c>GData.packBulletDisc("normal", 8, 0x00CCFF)</c>, baked at the mount-art density.</summary>
        private static byte[] BakeNormalBulletDisc()
        {
            const int size = 32;
            const float falloff = 12f;
            const float center = size * 0.5f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            try
            {
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - center;
                        float dy = y + 0.5f - center;
                        float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                        float alpha = distance <= falloff ? 1f - (distance / falloff) : 0f;
                        pixels[(y * size) + x] = new Color32(0, 204, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WriteBytesIfChanged(string destination, byte[] bytes)
        {
            string directory = Path.GetDirectoryName(destination);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (File.Exists(destination))
            {
                byte[] existing = File.ReadAllBytes(destination);
                if (existing.Length == bytes.Length)
                {
                    bool equal = true;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (existing[i] == bytes[i]) continue;
                        equal = false;
                        break;
                    }
                    if (equal) return;
                }
            }
            File.WriteAllBytes(destination, bytes);
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

        internal static Dictionary<string, int> ReadSymbolIds(string csvPath)
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

        internal static void RunFfdec(string executable, string arguments)
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
