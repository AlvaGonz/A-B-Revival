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
    /// 2048x2048 texture composed from the exported part PNGs and the matching UV table.
    /// The rects come from <see cref="SpriteUvTableBuilder"/>, so the texture and the table
    /// can never disagree. Original game IP: Vargames (2011).
    /// </summary>
    public static class PartsAtlasBuilder
    {
        private const string PartBin = "../src/assets/binaryData/8_all.AllData_defPart.bin";
        private const string FireBin = "../src/assets/binaryData/2_all.AllData_defFire.bin";
        private const string BulletBin = "../src/assets/binaryData/11_all.AllData_defBullet.bin";

        public const string PartsFolder = "Assets/Art/Sprites/Parts";
        public const string AtlasPath = "Assets/Art/Atlases/PartsAtlas.png";
        public const string UvTablePath = "Assets/Art/Atlases/PartsAtlasUv.txt";

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

                var mappings = PartSpriteMap.Build(records);
                var sizes = PngSize.ReadFolder(Resolve(projectRoot, PartsFolder));

                string uvText = SpriteUvTableBuilder.Build(mappings, sizes);
                string uvAbsolute = Resolve(projectRoot, UvTablePath);
                Directory.CreateDirectory(Path.GetDirectoryName(uvAbsolute));
                File.WriteAllText(uvAbsolute, uvText, new UTF8Encoding(false));

                WriteAtlasTexture(projectRoot, mappings, sizes);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureAtlasImporter();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    $"[PartsAtlasBuilder] Wrote {AtlasPath} and {UvTablePath} " +
                    $"({mappings.Count} part ids, {sizes.Count} symbols).");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[PartsAtlasBuilder] " + exception);
                return false;
            }
        }

        private static void WriteAtlasTexture(
            string projectRoot,
            IReadOnlyList<PartSpriteMapping> mappings,
            IReadOnlyDictionary<string, SpritePixelSize> sizes)
        {
            const int width = SpriteAtlasLayout.DefaultWidth;
            const int height = SpriteAtlasLayout.DefaultHeight;
            List<SpriteSymbolRect> layout = SpriteUvTableBuilder.BuildLayout(mappings, sizes);
            string partsFolder = Resolve(projectRoot, PartsFolder);

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
                        : ReadSprite(scratch, Path.Combine(partsFolder, rect.Symbol), rect);

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
            if (!File.Exists(path)) throw new FileNotFoundException("Exported part sprite missing.", path);
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
