using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ABRevival.Core.Data;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ABRevival.Editor
{
    /// <summary>
    /// Exports the original hangar and HUD sprites from the frozen flash UI library,
    /// applies the PPU 1 import contract, writes the symbol-to-PNG manifest and builds
    /// a contact sheet for eyeball verification. One menu item, one batchmode method.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class UiSpriteExportPipeline
    {
        private const string TempFolder = "ab-revival-ui-sprite-export";
        private static readonly Regex SymbolClassLine = new Regex("^(\\d+);\"(.*)\"$", RegexOptions.Compiled);
        private static readonly Regex SpriteFolder = new Regex("^DefineSprite_(\\d+)(?:_|$)", RegexOptions.Compiled);
        private static readonly Dictionary<string, string> PageReferenceFiles =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "UI_MakePlayer", "factory-page.png" },
                { "UI_ChooseFlyAI", "center-page.png" },
                { "UI_Shop", "shop-page.png" },
                { "UI_Game", "hud-page.png" }
            };

        [MenuItem("Tools/A&B Revival/Export UI Sprites")]
        public static void ExportFromMenu()
        {
            Run();
        }

        /// <summary>
        /// Batchmode entry point:
        /// Unity.exe -batchmode -quit -projectPath "A&amp;B-U" -executeMethod ABRevival.Editor.UiSpriteExportPipeline.ExportBatch
        /// </summary>
        public static void ExportBatch()
        {
            if (!Run()) EditorApplication.Exit(1);
        }

        private static bool Run()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string repoRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
            string tempRoot = Path.Combine(Path.GetTempPath(), TempFolder);

            try
            {
                string uiLibrary = Resolve(repoRoot, UiSpriteCatalog.UiLibrary);
                if (!File.Exists(uiLibrary))
                {
                    throw new FileNotFoundException("Frozen UI library is missing.", uiLibrary);
                }

                string ffdec = SpriteExportPipeline.ResolveFfdec();
                Debug.Log($"[UiSpriteExportPipeline] FFDec: {ffdec}");
                Debug.Log($"[UiSpriteExportPipeline] UI library: {uiLibrary}");

                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
                Directory.CreateDirectory(tempRoot);

                string dump = RunFfdec(ffdec, "-dumpSWF " + Quote(uiLibrary));
                List<UiSpriteSpec> generated = UiSpriteCatalog.Sprites
                    .Where(UiSpriteCatalog.IsGenerated)
                    .ToList();
                ValidateSymbols(dump, generated);

                string symbolDir = Path.Combine(tempRoot, "symbols");
                Directory.CreateDirectory(symbolDir);
                RunFfdec(ffdec, "-export symbolClass " + Quote(symbolDir) + " " + Quote(uiLibrary));
                Dictionary<int, string> classNames = ReadSymbolClasses(Path.Combine(symbolDir, "symbols.csv"));

                string spriteDir = Path.Combine(tempRoot, "sprites");
                string shapeDir = Path.Combine(tempRoot, "shapes");
                Directory.CreateDirectory(spriteDir);
                Directory.CreateDirectory(shapeDir);

                int[] spriteIds = generated
                    .Where(spec => spec.kind == UiSpriteKind.Sprite)
                    .Select(spec => spec.symbolId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();
                int[] shapeIds = generated
                    .Where(spec => spec.kind == UiSpriteKind.Shape)
                    .Select(spec => spec.symbolId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();

                if (spriteIds.Length > 0)
                {
                    RunFfdec(
                        ffdec,
                        "-selectid " + string.Join(",", spriteIds.Select(id => id.ToString(CultureInfo.InvariantCulture))) +
                        " -format sprite:png -zoom " + SpriteNaming.ExportZoom +
                        " -ignorebackground -export sprite " + Quote(spriteDir) + " " + Quote(uiLibrary));
                }

                if (shapeIds.Length > 0)
                {
                    RunFfdec(
                        ffdec,
                        "-selectid " + string.Join(",", shapeIds.Select(id => id.ToString(CultureInfo.InvariantCulture))) +
                        " -format shape:png -export shape " + Quote(shapeDir) + " " + Quote(uiLibrary));
                }

                var expected = new HashSet<string>(StringComparer.Ordinal);
                foreach (UiSpriteSpec spec in generated)
                {
                    string relative = UiSpriteCatalog.FileName(spec);
                    expected.Add(Path.GetFileName(relative));
                    string destination = Resolve(repoRoot, relative);
                    string source = spec.kind == UiSpriteKind.Sprite
                        ? FindSpriteFrame(spriteDir, spec.symbolId, spec.frame)
                        : FindShapeFrame(shapeDir, spec.symbolId);
                    SpriteExportPipeline.CopyIfChanged(source, destination);
                }

                CopyPageReferences(repoRoot, classNames);

                foreach (UiSpriteSpec spec in UiSpriteCatalog.Sprites.Where(spec => !UiSpriteCatalog.IsGenerated(spec)))
                {
                    if (!File.Exists(Resolve(repoRoot, UiSpriteCatalog.FileName(spec))))
                    {
                        throw new FileNotFoundException(
                            $"Required {spec.kind} sprite for '{spec.logicalName}' is missing.",
                            spec.externalFile);
                    }
                }

                RemoveStaleGeneratedPngs(Resolve(repoRoot, UiSpriteCatalog.OutputFolder), expected);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                SpriteExportPipeline.ApplyImportSettings(UiSpriteCatalog.ProjectOutputFolder);
                SpriteExportPipeline.RebuildAtlas(UiSpriteCatalog.AtlasPath, UiSpriteCatalog.ProjectOutputFolder);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Dictionary<string, PngSize> sizes = WriteContactSheet(repoRoot);
                WriteManifest(repoRoot, classNames, sizes);

                Debug.Log(
                    $"[UiSpriteExportPipeline] Exported {generated.Count} UI symbols " +
                    $"({spriteIds.Length} sprites, {shapeIds.Length} shapes) and {UiSpriteCatalog.Sprites.Count} manifest entries. " +
                    $"Manifest: {UiSpriteCatalog.ManifestFile}; contact sheet: {UiSpriteCatalog.ContactSheetFile}.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[UiSpriteExportPipeline] " + exception);
                return false;
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }

        private static void ValidateSymbols(string dump, IReadOnlyList<UiSpriteSpec> specs)
        {
            string[] lines = dump.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (UiSpriteSpec spec in specs)
            {
                string token = "(chid: " + spec.symbolId.ToString(CultureInfo.InvariantCulture) + ")";
                string expectedTag = spec.kind == UiSpriteKind.Sprite ? "DefineSprite" : "DefineShape";
                bool found = lines.Any(line => line.Contains(expectedTag) && line.Contains(token));
                if (!found)
                {
                    throw new InvalidOperationException(
                        $"UI library has no {expectedTag} with character id {spec.symbolId} for '{spec.logicalName}'.");
                }
            }
        }

        private static Dictionary<int, string> ReadSymbolClasses(string csvPath)
        {
            var result = new Dictionary<int, string>();
            if (!File.Exists(csvPath)) return result;

            foreach (string line in File.ReadAllLines(csvPath))
            {
                Match match = SymbolClassLine.Match(line.Trim());
                if (!match.Success) continue;
                int id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                result[id] = match.Groups[2].Value;
            }

            return result;
        }

        private static string FindSpriteFrame(string root, int characterId, int frame)
        {
            string[] folders = Directory.GetDirectories(root, "DefineSprite_" + characterId + "*");
            string folder = folders.FirstOrDefault(path =>
                SpriteFolder.IsMatch(Path.GetFileName(path)) &&
                int.Parse(SpriteFolder.Match(Path.GetFileName(path)).Groups[1].Value, CultureInfo.InvariantCulture) == characterId);
            if (folder == null)
            {
                throw new InvalidOperationException(
                    $"FFDec did not export sprite {characterId} into {root}.");
            }

            string png = Path.Combine(folder, frame.ToString(CultureInfo.InvariantCulture) + ".png");
            if (!File.Exists(png))
            {
                throw new InvalidOperationException(
                    $"FFDec did not render frame {frame} for sprite {characterId} ({png}).");
            }

            return png;
        }

        private static string FindShapeFrame(string root, int characterId)
        {
            string png = Path.Combine(root, characterId.ToString(CultureInfo.InvariantCulture) + ".png");
            if (!File.Exists(png))
            {
                throw new InvalidOperationException(
                    $"FFDec did not export shape {characterId} ({png}).");
            }

            return png;
        }

        private static void CopyPageReferences(
            string repoRoot, IReadOnlyDictionary<int, string> classNames)
        {
            foreach (UiSpriteSpec spec in UiSpriteCatalog.Sprites)
            {
                if (!spec.logicalName.StartsWith("reference_", StringComparison.Ordinal)) continue;
                string symbolClass = ResolveSymbolClass(spec, classNames);
                if (!PageReferenceFiles.TryGetValue(symbolClass, out string file)) continue;

                string source = Resolve(repoRoot, UiSpriteCatalog.FileName(spec));
                if (!File.Exists(source)) continue;
                SpriteExportPipeline.CopyIfChanged(
                    source, Resolve(repoRoot, "docs/assets/reference/" + file));
            }
        }

        private static void RemoveStaleGeneratedPngs(string folder, HashSet<string> expected)
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder, "ui_????_*.png"))
            {
                if (expected.Contains(Path.GetFileName(file))) continue;
                File.Delete(file);
                string meta = file + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }
        }

        private static void WriteManifest(
            string repoRoot,
            IReadOnlyDictionary<int, string> classNames,
            IReadOnlyDictionary<string, PngSize> sizes)
        {
            var text = new StringBuilder();
            text.AppendLine("# UI Sprite Manifest");
            text.AppendLine();
            text.AppendLine(
                "Generated by `Tools/A&B Revival/Export UI Sprites` " +
                "(`ABRevival.Editor.UiSpriteExportPipeline.ExportBatch`) from " +
                "`src/assets/binaryData/9_all.AllData_ui.bin`. Do not edit by hand.");
            text.AppendLine();
            text.AppendLine($"- Entries: {UiSpriteCatalog.Sprites.Count}");
            text.AppendLine($"- Atlas: `{UiSpriteCatalog.AtlasPath}`");
            text.AppendLine($"- Contact sheet: `{UiSpriteCatalog.ContactSheetFile}`");
            text.AppendLine("- Import: Sprite (Single), PPU 1, uncompressed, bilinear, clamp, no mipmaps");
            text.AppendLine("- Rect: exported sprite texture rect (0,0,w,h); the SpriteAtlas packs it at import");
            text.AppendLine();
            text.AppendLine("## Regenerate");
            text.AppendLine();
            text.AppendLine("Editor menu: **Tools -> A&B Revival -> Export UI Sprites**");
            text.AppendLine();
            text.AppendLine("Batchmode:");
            text.AppendLine();
            text.AppendLine("```powershell");
            text.AppendLine("& \"C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.8f1\\Editor\\Unity.exe\" `");
            text.AppendLine("  -batchmode -quit -projectPath \"A&B-U\" `");
            text.AppendLine("  -executeMethod ABRevival.Editor.UiSpriteExportPipeline.ExportBatch `");
            text.AppendLine("  -logFile \"$env:TEMP\\ab-ui-sprite-export.log\"");
            text.AppendLine("```");
            text.AppendLine();
            text.AppendLine("Or run `scripts\\export-ui-sprites.bat`.");
            text.AppendLine();
            text.AppendLine("## Symbols");
            text.AppendLine();
            text.AppendLine("| # | Logical name | Symbol id | Symbol class | Kind | Frame | PNG | Rect | Note |");
            text.AppendLine("|---:|---|---:|---|---|---:|---|---|---|");

            int index = 1;
            foreach (UiSpriteSpec spec in UiSpriteCatalog.Sprites)
            {
                string file = UiSpriteCatalog.FileName(spec);
                PngSize size = sizes[file];
                string symbolClass = ResolveSymbolClass(spec, classNames);
                string note = string.IsNullOrEmpty(spec.note) ? "-" : spec.note.Replace("|", "/");
                text.AppendLine(
                    $"| {index} | `{spec.logicalName}` | {spec.symbolId} | {symbolClass} | " +
                    $"{KindLabel(spec.kind)} | {spec.frame} | `{file}` | 0,0,{size.Width},{size.Height} | {note} |");
                index++;
            }

            text.AppendLine();
            text.AppendLine("## Coverage notes");
            text.AppendLine();
            text.AppendLine("- The frozen HUD has no single full-screen frame; `hud_frame` maps to the edge-anchored top-left plate (477).");
            text.AppendLine("- Money is a dynamic TextField; `hud_money_panel` uses its 477 backing plate.");
            text.AppendLine("- Tactical-mode and camera-distance badges were added in the revival; both reuse the blank AI-count plate (493).");
            text.AppendLine("- Battle-damage telemetry is a dynamic TextField; `hud_damage_telemetry_row` uses the closest bar frame (481).");
            text.AppendLine("- Category filters use text labels in the frozen release; the filter button sprites are the closest category art.");
            text.AppendLine("- The frozen catalog has only Body/Wing/Eng/Gun; `hangar_shop_thumbnail_module` uses Body_2 as the closest generic part.");
            text.AppendLine("- `hangar_button_launch` uses the READY button because no LAUNCH label exists in the frozen hangar.");
            text.AppendLine("- Pilot and captain portrait frames reuse the existing dialogue portraits and are not re-exported.");

            string path = Resolve(repoRoot, UiSpriteCatalog.ManifestFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static string ResolveSymbolClass(UiSpriteSpec spec, IReadOnlyDictionary<int, string> classNames)
        {
            if (classNames.TryGetValue(spec.symbolId, out string name) && !string.IsNullOrEmpty(name))
            {
                return name;
            }

            return string.IsNullOrEmpty(spec.symbolClass) ? "-" : spec.symbolClass;
        }

        private static string KindLabel(UiSpriteKind kind)
        {
            switch (kind)
            {
                case UiSpriteKind.Sprite: return "sprite";
                case UiSpriteKind.Shape: return "shape";
                case UiSpriteKind.ExistingPart: return "part";
                case UiSpriteKind.ExistingPortrait: return "portrait";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static Dictionary<string, PngSize> WriteContactSheet(string repoRoot)
        {
            const int columns = 6;
            const int cellWidth = 280;
            const int cellHeight = 220;
            int rows = (UiSpriteCatalog.Sprites.Count + columns - 1) / columns;
            int width = columns * cellWidth;
            int height = rows * cellHeight;

            var pixels = new Color32[width * height];
            Fill(pixels, new Color32(24, 26, 30, 255));
            var sizes = new Dictionary<string, PngSize>(StringComparer.Ordinal);
            var scratch = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                for (int index = 0; index < UiSpriteCatalog.Sprites.Count; index++)
                {
                    UiSpriteSpec spec = UiSpriteCatalog.Sprites[index];
                    string relative = UiSpriteCatalog.FileName(spec);
                    string absolute = Resolve(repoRoot, relative);
                    if (!ImageConversion.LoadImage(scratch, File.ReadAllBytes(absolute), false))
                    {
                        throw new InvalidDataException("Could not decode " + absolute);
                    }

                    int sourceWidth = scratch.width;
                    int sourceHeight = scratch.height;
                    sizes[relative] = new PngSize(sourceWidth, sourceHeight);
                    Color32[] source = scratch.GetPixels32();

                    int cellX = (index % columns) * cellWidth;
                    int cellY = (index / columns) * cellHeight;
                    FillRect(pixels, width, height, cellX + 4, cellY + 4, cellWidth - 8, cellHeight - 8,
                        new Color32(36, 39, 45, 255));
                    DrawChecker(pixels, width, height, cellX + 4, cellY + 4, cellWidth - 8, cellHeight - 8);

                    float scale = Mathf.Min(
                        (cellWidth - 16f) / sourceWidth,
                        (cellHeight - 42f) / sourceHeight);
                    if (scale > 1f) scale = Mathf.Floor(scale);
                    scale = Mathf.Min(scale, 3f);
                    int drawWidth = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
                    int drawHeight = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));
                    int drawX = cellX + (cellWidth - drawWidth) / 2;
                    int drawY = cellY + 24 + (cellHeight - 24 - drawHeight) / 2;
                    DrawScaled(pixels, width, height, source, sourceWidth, sourceHeight,
                        drawX, drawY, drawWidth, drawHeight);

                    DrawNumber(pixels, width, height, cellX + 8, cellY + 7, index + 1);
                    DrawBorder(pixels, width, height, cellX + 2, cellY + 2, cellWidth - 4, cellHeight - 4,
                        new Color32(96, 102, 112, 255));
                }

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                try
                {
                    texture.SetPixels32(pixels);
                    texture.Apply(false, false);
                    string path = Resolve(repoRoot, UiSpriteCatalog.ContactSheetFile);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scratch);
            }

            return sizes;
        }

        private static void DrawChecker(Color32[] pixels, int width, int height, int x, int y, int w, int h)
        {
            const int tile = 8;
            var light = new Color32(70, 74, 80, 255);
            var dark = new Color32(46, 49, 55, 255);
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    bool isLight = ((px / tile) + (py / tile)) % 2 == 0;
                    SetPixel(pixels, width, height, x + px, y + py, isLight ? light : dark);
                }
            }
        }

        private static void DrawScaled(
            Color32[] target, int targetWidth, int targetHeight,
            Color32[] source, int sourceWidth, int sourceHeight,
            int x, int y, int width, int height)
        {
            for (int py = 0; py < height; py++)
            {
                int sourceY = sourceHeight - 1 - Mathf.Min(sourceHeight - 1, py * sourceHeight / height);
                for (int px = 0; px < width; px++)
                {
                    int sourceX = Mathf.Min(sourceWidth - 1, px * sourceWidth / width);
                    Color32 src = source[sourceY * sourceWidth + sourceX];
                    if (src.a == 0) continue;

                    int targetX = x + px;
                    int targetY = y + py;
                    if (targetX < 0 || targetX >= targetWidth || targetY < 0 || targetY >= targetHeight) continue;

                    int offset = (targetHeight - 1 - targetY) * targetWidth + targetX;
                    Color32 dst = target[offset];
                    byte alpha = src.a;
                    target[offset] = new Color32(
                        (byte)((src.r * alpha + dst.r * (255 - alpha)) / 255),
                        (byte)((src.g * alpha + dst.g * (255 - alpha)) / 255),
                        (byte)((src.b * alpha + dst.b * (255 - alpha)) / 255),
                        255);
                }
            }
        }

        private static void DrawNumber(Color32[] pixels, int width, int height, int x, int y, int value)
        {
            string digits = value.ToString(CultureInfo.InvariantCulture);
            const int scale = 3;
            for (int digitIndex = 0; digitIndex < digits.Length; digitIndex++)
            {
                byte[] glyph = DigitGlyph(digits[digitIndex] - '0');
                int originX = x + digitIndex * 4 * scale;
                for (int row = 0; row < 5; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        if ((glyph[row] & (1 << (2 - column))) == 0) continue;
                        for (int sy = 0; sy < scale; sy++)
                        {
                            for (int sx = 0; sx < scale; sx++)
                            {
                                SetPixel(pixels, width, height, originX + column * scale + sx,
                                    y + row * scale + sy, new Color32(255, 220, 90, 255));
                            }
                        }
                    }
                }
            }
        }

        private static byte[] DigitGlyph(int digit)
        {
            switch (digit)
            {
                case 0: return new byte[] { 7, 5, 5, 5, 7 };
                case 1: return new byte[] { 2, 6, 2, 2, 7 };
                case 2: return new byte[] { 7, 1, 7, 4, 7 };
                case 3: return new byte[] { 7, 1, 7, 1, 7 };
                case 4: return new byte[] { 5, 5, 7, 1, 1 };
                case 5: return new byte[] { 7, 4, 7, 1, 7 };
                case 6: return new byte[] { 7, 4, 7, 5, 7 };
                case 7: return new byte[] { 7, 1, 1, 1, 1 };
                case 8: return new byte[] { 7, 5, 7, 5, 7 };
                case 9: return new byte[] { 7, 5, 7, 1, 7 };
                default: return new byte[] { 7, 1, 1, 1, 7 };
            }
        }

        private static void Fill(Color32[] pixels, Color32 color)
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        }

        private static void FillRect(
            Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
        {
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    SetPixel(pixels, width, height, x + px, y + py, color);
                }
            }
        }

        private static void DrawBorder(
            Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
        {
            for (int px = 0; px < w; px++)
            {
                SetPixel(pixels, width, height, x + px, y, color);
                SetPixel(pixels, width, height, x + px, y + h - 1, color);
            }

            for (int py = 0; py < h; py++)
            {
                SetPixel(pixels, width, height, x, y + py, color);
                SetPixel(pixels, width, height, x + w - 1, y + py, color);
            }
        }

        private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            pixels[(height - 1 - y) * width + x] = color;
        }

        private static string RunFfdec(string executable, string arguments)
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
                return stdout;
            }
        }

        private static string Resolve(string root, string relative)
        {
            return Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }

        private readonly struct PngSize
        {
            public readonly int Width;
            public readonly int Height;

            public PngSize(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }
    }
}
