using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ABRevival.Editor
{
    /// <summary>
    /// Builds the BRG part atlas consumed by <c>MissionSpriteView</c>: one deterministic
    /// 2048x2048 texture composed from the exported in-game mount PNGs, the extracted bullet
    /// art and the matching UV table. It also regenerates the part mount manifest (mount art
    /// vs shop thumbnails) and the bullet art table from the frozen SWF markers, so every
    /// generated file has one command and one source. Original game IP: Vargames (2011).
    /// </summary>
    public static class PartsAtlasBuilder
    {
        private const string PartBin = "../src/assets/binaryData/8_all.AllData_defPart.bin";
        private const string FireBin = "../src/assets/binaryData/2_all.AllData_defFire.bin";
        private const string BulletBin = "../src/assets/binaryData/11_all.AllData_defBullet.bin";
        private const string PartSwf = "../src/assets/binaryData/4_all.AllData_data.bin";

        public const string MountsFolder = "Assets/Art/Sprites/PartMounts";
        public const string BulletsFolder = "Assets/Art/Sprites/Bullets";
        public const string AtlasPath = "Assets/Art/Atlases/PartsAtlas.png";
        public const string UvTablePath = "Assets/Art/Atlases/PartsAtlasUv.txt";
        public const string MountTablePath = "Assets/Resources/Data/PartMounts.txt";
        public const string BulletTablePath = "Assets/Resources/Data/BulletSprites.txt";
        public const string ManifestPath = "docs/assets/part-sprites.md";

        [MenuItem("Tools/A&B Revival/Rebuild Parts Atlas UV Table")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>
        /// Batchmode entry point:
        /// Unity.exe -batchmode -quit -projectPath "A&amp;B-U" -executeMethod ABRevival.Editor.PartsAtlasBuilder.BuildBatch
        /// </summary>
        public static void BuildBatch()
        {
            if (!Build()) EditorApplication.Exit(1);
        }

        public static bool Build()
        {
            try
            {
                string projectRoot = ProjectRoot();
                var records = PartCatalogParser.ParseCatalogFromFiles(
                    Resolve(projectRoot, PartBin),
                    Resolve(projectRoot, FireBin),
                    Resolve(projectRoot, BulletBin));
                if (records.Count == 0) throw new InvalidOperationException("Part catalog is empty.");

                var bullets = BulletCatalog.ParseFromFile(Resolve(projectRoot, BulletBin));
                if (bullets.Count == 0) throw new InvalidOperationException("Bullet catalog is empty.");

                var mappings = PartSpriteMap.Build(records);
                Dictionary<string, SpritePixelSize> sizes = ReadSpriteSizes(projectRoot, out Dictionary<string, string> symbolPaths);
                List<string> extraSymbols = ReadBulletSymbols(projectRoot);

                var extractor = PartMountExtractor.ExtractFromSwf(
                    SpriteExportPipeline.ResolveFfdec(),
                    Resolve(projectRoot, PartSwf),
                    records,
                    bullets,
                    ReadFolderSizes(projectRoot, MountsFolder),
                    ReadFolderSizes(projectRoot, BulletsFolder));

                WriteText(projectRoot, MountTablePath, extractor.MountTableText);
                WriteText(projectRoot, BulletTablePath, extractor.BulletTableText);
                WriteText(Path.GetFullPath(Path.Combine(projectRoot, "..")), ManifestPath, extractor.ManifestMarkdown);

                string uvText = SpriteUvTableBuilder.Build(mappings, sizes, extraSymbols: extraSymbols);
                WriteText(projectRoot, UvTablePath, uvText);

                WriteAtlasTexture(projectRoot, mappings, sizes, symbolPaths, extraSymbols);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureAtlasImporter();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    $"[PartsAtlasBuilder] Wrote {AtlasPath}, {UvTablePath}, {MountTablePath}, " +
                    $"{BulletTablePath} and {ManifestPath} ({mappings.Count} part ids, " +
                    $"{extractor.SymbolCount} mount symbols, {extraSymbols.Count} bullet symbols).");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[PartsAtlasBuilder] " + exception);
                return false;
            }
        }

        private static Dictionary<string, SpritePixelSize> ReadSpriteSizes(
            string projectRoot,
            out Dictionary<string, string> symbolPaths)
        {
            var sizes = new Dictionary<string, SpritePixelSize>(StringComparer.Ordinal);
            symbolPaths = new Dictionary<string, string>(StringComparer.Ordinal);

            string mounts = Resolve(projectRoot, MountsFolder);
            string bullets = Resolve(projectRoot, BulletsFolder);
            AddFolder(sizes, symbolPaths, mounts);
            AddFolder(sizes, symbolPaths, bullets);
            return sizes;
        }

        private static void AddFolder(
            Dictionary<string, SpritePixelSize> sizes,
            Dictionary<string, string> paths,
            string folder)
        {
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException("Exported sprite folder missing: " + folder);

            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                string name = Path.GetFileName(file);
                sizes[name] = PngSize.Read(file);
                paths[name] = file;
            }
        }

        private static Dictionary<string, SpritePixelSize> ReadFolderSizes(string projectRoot, string folder)
        {
            string absolute = Resolve(projectRoot, folder);
            if (!Directory.Exists(absolute))
                throw new DirectoryNotFoundException("Exported sprite folder missing: " + absolute);
            return PngSize.ReadFolder(absolute);
        }

        private static List<string> ReadBulletSymbols(string projectRoot)
        {
            string folder = Resolve(projectRoot, BulletsFolder);
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException("Exported bullet folder missing: " + folder);

            var files = new List<string>(Directory.GetFiles(folder, "*.png"));
            var names = new List<string>(files.Count);
            for (int i = 0; i < files.Count; i++) names.Add(Path.GetFileName(files[i]));
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private static void WriteAtlasTexture(
            string projectRoot,
            IReadOnlyList<PartSpriteMapping> mappings,
            IReadOnlyDictionary<string, SpritePixelSize> sizes,
            IReadOnlyDictionary<string, string> symbolPaths,
            IReadOnlyList<string> extraSymbols)
        {
            const int width = SpriteAtlasLayout.DefaultWidth;
            const int height = SpriteAtlasLayout.DefaultHeight;
            List<SpriteSymbolRect> layout = SpriteUvTableBuilder.BuildLayout(mappings, sizes, width, height, extraSymbols);

            var atlas = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var scratch = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                atlas.SetPixels32(new Color32[width * height]);

                for (int i = 0; i < layout.Count; i++)
                {
                    SpriteSymbolRect rect = layout[i];
                    Color32[] pixels = rect.Symbol == SpriteUvTableBuilder.WhiteSymbol
                        ? WhiteBlock(rect.Width * rect.Height)
                        : ReadSprite(scratch, symbolPaths[rect.Symbol], rect);

                    // The table stores top-left rows; Texture2D pixels start at the bottom-left.
                    atlas.SetPixels32(rect.X, height - rect.Y - rect.Height, rect.Width, rect.Height, pixels);
                }

                atlas.Apply(false, false);
                File.WriteAllBytes(Resolve(projectRoot, AtlasPath), atlas.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scratch);
                UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        private static Color32[] ReadSprite(Texture2D scratch, string path, in SpriteSymbolRect rect)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Exported sprite missing.", path);
            if (!ImageConversion.LoadImage(scratch, File.ReadAllBytes(path), false))
                throw new InvalidDataException("Could not decode " + path);

            Color32[] pixels = scratch.GetPixels32();
            if (pixels.Length != rect.Width * rect.Height)
                throw new InvalidDataException($"Decoded size of {path} does not match its PNG header.");
            return pixels;
        }

        private static Color32[] WhiteBlock(int count)
        {
            var pixels = new Color32[count];
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = white;
            return pixels;
        }

        private static void ConfigureAtlasImporter()
        {
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("TextureImporter missing for " + AtlasPath);

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = SpriteAtlasLayout.DefaultWidth;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static void WriteText(string projectRoot, string relative, string content)
        {
            string absolute = Resolve(projectRoot, relative);
            string directory = Path.GetDirectoryName(absolute);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(absolute, content, new UTF8Encoding(false));
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string Resolve(string projectRoot, string relative)
        {
            return Path.GetFullPath(Path.Combine(projectRoot, relative));
        }
    }
}
