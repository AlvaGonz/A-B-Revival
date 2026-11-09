using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ABRevival.Editor
{
    /// <summary>
    /// Applies the extraction manifest's per-symbol pivots, nine-slice borders and PPU 1
    /// import contract, imports the frozen fonts, and builds the category sprite atlases.
    /// Driven by <c>scripts/export_all_assets.py</c>; the manifest is the single source.
    /// Original game IP and art: Vargames (2011). Font policy: ADR 0020.
    /// </summary>
    public static class FullAssetExtractionImporter
    {
        private const string ManifestRelative = "docs/assets/extraction-manifest.json";

        [MenuItem("Tools/A&B Revival/Import Extracted Assets")]
        public static void ImportFromMenu()
        {
            if (!Run())
            {
                Debug.LogError("[FullAssetExtractionImporter] Import failed; see the console.");
            }
        }

        /// <summary>
        /// Batchmode entry point:
        /// Unity.exe -batchmode -quit -projectPath "A&amp;B-U" -executeMethod
        /// ABRevival.Editor.FullAssetExtractionImporter.ImportBatch
        /// </summary>
        public static void ImportBatch()
        {
            if (!Run()) EditorApplication.Exit(1);
        }

        private static bool Run()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string repoRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
                string manifestPath = Path.Combine(repoRoot, ManifestRelative);
                if (!File.Exists(manifestPath))
                {
                    throw new FileNotFoundException(
                        "Extraction manifest missing. Run python scripts/export_all_assets.py first.",
                        manifestPath);
                }

                ExtractionManifest manifest = JsonUtility.FromJson<ExtractionManifest>(
                    File.ReadAllText(manifestPath));
                if (manifest?.unityImport?.sprites == null || manifest.unityImport.sprites.Length == 0)
                {
                    throw new InvalidDataException("Extraction manifest has no unityImport.sprites.");
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ImportSprites(manifest.unityImport.sprites);
                ImportFonts(manifest.unityImport.fonts);
                ImportAudio(manifest.unityImport.audio);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                RemoveObsoleteAtlases(manifest.unityImport.removeAtlases);
                foreach (ExtractionAtlas atlas in manifest.unityImport.atlases)
                {
                    RebuildAtlas(atlas);
                }

                // The runtime BRG atlas consumes the regenerated mount/bullet PNGs.
                if (!PartsAtlasBuilder.Build())
                {
                    throw new InvalidOperationException("The runtime part atlas could not be rebuilt.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(
                    $"[FullAssetExtractionImporter] Imported {manifest.unityImport.sprites.Length} sprites, " +
                    $"{manifest.unityImport.fonts.Length} fonts, {manifest.unityImport.audio.Length} UI sounds and " +
                    $"{manifest.unityImport.atlases.Length} category atlases.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[FullAssetExtractionImporter] FAILED: " + exception);
                return false;
            }
        }

        private static void ImportSprites(ExtractionSprite[] sprites)
        {
            int configured = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (ExtractionSprite sprite in sprites)
                {
                    var importer = AssetImporter.GetAtPath(sprite.path) as TextureImporter;
                    if (importer == null)
                    {
                        throw new InvalidOperationException(
                            $"TextureImporter missing for '{sprite.path}'. Was the PNG regenerated?");
                    }

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
                    dirty |= Set(importer.crunchedCompression, () => importer.crunchedCompression = false);
                    dirty |= Set(importer.maxTextureSize != sprite.maxSize,
                        () => importer.maxTextureSize = sprite.maxSize);
                    dirty |= Set(!importer.sRGBTexture, () => importer.sRGBTexture = true);
                    dirty |= Set(importer.isReadable, () => importer.isReadable = false);
                    dirty |= Set(importer.npotScale != TextureImporterNPOTScale.None,
                        () => importer.npotScale = TextureImporterNPOTScale.None);

                    if (sprite.maxSize > 2048)
                    {
                        // Starfield tiles exceed the default 2048 page; keep native pixels.
                        foreach (string platform in new[] { "Standalone", "Android", "iPhone" })
                        {
                            TextureImporterPlatformSettings platformSettings =
                                importer.GetPlatformTextureSettings(platform);
                            if (platformSettings.maxTextureSize == sprite.maxSize &&
                                platformSettings.overridden)
                            {
                                continue;
                            }
                            platformSettings.overridden = true;
                            platformSettings.maxTextureSize = sprite.maxSize;
                            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.SetPlatformTextureSettings(platformSettings);
                            dirty = true;
                        }
                    }

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Vector2 pivot = new Vector2(sprite.pivotX, sprite.pivotY);
                    Vector4 border = Border(sprite.border);
                    bool settingsDirty =
                        settings.spriteAlignment != (int)SpriteAlignment.Custom ||
                        settings.spritePivot != pivot ||
                        settings.spriteBorder != border ||
                        settings.spriteMeshType != SpriteMeshType.FullRect ||
                        settings.spriteExtrude != 0 ||
                        settings.spriteGenerateFallbackPhysicsShape;
                    if (settingsDirty)
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.Custom;
                        settings.spritePivot = pivot;
                        settings.spriteBorder = border;
                        settings.spriteMeshType = SpriteMeshType.FullRect;
                        settings.spriteExtrude = 0;
                        settings.spriteGenerateFallbackPhysicsShape = false;
                        importer.SetTextureSettings(settings);
                        dirty = true;
                    }

                    if (dirty) importer.SaveAndReimport();
                    configured++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[FullAssetExtractionImporter] Configured {configured} sprite importers.");
        }

        private static Vector4 Border(int[] border)
        {
            if (border == null || border.Length != 4) return Vector4.zero;
            return new Vector4(border[0], border[1], border[2], border[3]);
        }

        private static void ImportFonts(ExtractionFont[] fonts)
        {
            if (fonts == null) return;
            foreach (ExtractionFont font in fonts)
            {
                var importer = AssetImporter.GetAtPath(font.path) as TrueTypeFontImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"TrueTypeFontImporter missing for '{font.path}'.");
                }

                bool dirty = false;
                dirty |= Set(!importer.includeFontData, () => importer.includeFontData = true);
                dirty |= Set(importer.fontTextureCase != FontTextureCase.Dynamic,
                    () => importer.fontTextureCase = FontTextureCase.Dynamic);
                dirty |= Set(importer.fontSize != 16, () => importer.fontSize = 16);
                if (dirty) importer.SaveAndReimport();
            }

            Debug.Log($"[FullAssetExtractionImporter] Embedded {fonts.Length} original fonts.");
        }

        private static void ImportAudio(ExtractionAudio[] clips)
        {
            if (clips == null) return;
            foreach (ExtractionAudio clip in clips)
            {
                var importer = AssetImporter.GetAtPath(clip.path) as AudioImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"AudioImporter missing for '{clip.path}'.");
                }

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                bool dirty =
                    settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                    !settings.preloadAudioData;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                if (dirty)
                {
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log($"[FullAssetExtractionImporter] Configured {clips.Length} UI sound clips.");
        }

        private static void RemoveObsoleteAtlases(string[] paths)
        {
            if (paths == null) return;
            foreach (string path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"[FullAssetExtractionImporter] Removed obsolete atlas {path}.");
                }
            }
        }

        private static void RebuildAtlas(ExtractionAtlas spec)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(spec.path) != null)
            {
                AssetDatabase.DeleteAsset(spec.path);
            }

            var atlas = new SpriteAtlasAsset();
            SpriteAtlasAsset.Save(atlas, spec.path);
            AssetDatabase.ImportAsset(spec.path, ImportAssetOptions.ForceSynchronousImport);

            atlas = SpriteAtlasAsset.Load(spec.path);
            var textures = new List<UnityEngine.Object>(spec.assets.Length);
            foreach (string path in spec.assets)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    throw new InvalidOperationException($"Atlas '{spec.name}' is missing texture '{path}'.");
                }
                textures.Add(texture);
            }

            atlas.Add(textures.ToArray());
            SpriteAtlasAsset.Save(atlas, spec.path);
            AssetDatabase.ImportAsset(spec.path, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(spec.path) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("SpriteAtlasImporter missing for " + spec.path);
            }

            SpriteAtlasTextureSettings textureSettings = importer.textureSettings;
            textureSettings.filterMode = FilterMode.Bilinear;
            textureSettings.generateMipMaps = false;
            textureSettings.readable = false;
            importer.textureSettings = textureSettings;

            SpriteAtlasPackingSettings packing = importer.packingSettings;
            packing.padding = 4;
            packing.blockOffset = 1;
            packing.enableRotation = false;
            packing.enableTightPacking = true;
            packing.enableAlphaDilation = true;
            importer.packingSettings = packing;

            importer.includeInBuild = true;
            importer.SetPlatformSettings(SpriteExportPipeline.PlatformSettings("Standalone", TextureImporterFormat.BC7));
            importer.SetPlatformSettings(SpriteExportPipeline.PlatformSettings("Android", TextureImporterFormat.ASTC_6x6));
            importer.SetPlatformSettings(SpriteExportPipeline.PlatformSettings("iPhone", TextureImporterFormat.ASTC_6x6));
            importer.SaveAndReimport();

            Debug.Log($"[FullAssetExtractionImporter] Atlas {spec.name}: {spec.assets.Length} sprites -> {spec.path}.");
        }

        private static bool Set(bool changed, Action apply)
        {
            if (!changed) return false;
            apply();
            return true;
        }

        [Serializable]
        private sealed class ExtractionManifest
        {
            public ExtractionUnityImport unityImport;
        }

        [Serializable]
        private sealed class ExtractionUnityImport
        {
            public ExtractionSprite[] sprites;
            public ExtractionAtlas[] atlases;
            public ExtractionFont[] fonts;
            public ExtractionAudio[] audio;
            public string[] removeAtlases;
        }

        [Serializable]
        private sealed class ExtractionSprite
        {
            public string path;
            public float pivotX;
            public float pivotY;
            public int[] border;
            public int maxSize;
        }

        [Serializable]
        private sealed class ExtractionAtlas
        {
            public string name;
            public string path;
            public string[] assets;
        }

        [Serializable]
        private sealed class ExtractionFont
        {
            public string path;
        }

        [Serializable]
        private sealed class ExtractionAudio
        {
            public string path;
        }
    }
}
