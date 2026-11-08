using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ABRevival.Editor
{
    /// <summary>
    /// Regenerates the nine original cinematic clips from the frozen SWF.
    /// FFDec renders each movie clip at the 640x480 stage, then the editor encodes
    /// the letterboxed 640x360 picture to a 1920x1080 H.264 MP4 via Unity's
    /// MediaEncoder. One menu item or one batchmode call regenerates all clips.
    /// Original movie content: Vargames (2011).
    /// </summary>
    public static class CinematicExporter
    {
        private const string OutputDirectory = "Assets/Resources/Cinematics";
        private const string TempDirectory = "Library/CinematicsExport";
        private const string MovieBinSuffix = "12_all.AllData_movie.bin";
        private const string UiBinSuffix = "9_all.AllData_ui.bin";
        private const int SourceWidth = 640;
        private const int SourceHeight = 480;
        private const int SourceContentY = 60;
        private const int SourceContentHeight = 360;
        private const int OutputWidth = 1920;
        private const int OutputHeight = 1080;
        private const int FrameRate = 60;

        [MenuItem("Tools/A&B Revival/Export Cinematics")]
        public static void ExportAll()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string repoRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
            string ffdec = ResolveFfdec();
            string swf = ResolveSourceSwf(repoRoot);
            string tempRoot = Path.Combine(projectRoot, TempDirectory);
            string binDir = Path.Combine(tempRoot, "bin");
            string uiXml = Path.Combine(tempRoot, "ui.xml");

            if (Directory.Exists(binDir))
            {
                Directory.Delete(binDir, true);
            }

            Directory.CreateDirectory(binDir);
            Directory.CreateDirectory(Path.Combine(projectRoot, OutputDirectory));

            Debug.Log($"[CinematicExporter] Source SWF: {swf}");
            Debug.Log($"[CinematicExporter] FFDec CLI: {ffdec}");

            RunFfdec(ffdec, "-export", "binaryData", binDir, swf);

            string uiBin = FindFileBySuffix(binDir, UiBinSuffix);
            string movieBin = FindFileBySuffix(binDir, MovieBinSuffix);
            if (uiBin == null || movieBin == null)
            {
                throw new FileNotFoundException(
                    $"[CinematicExporter] Missing extracted binary data. Expected '{UiBinSuffix}' and '{MovieBinSuffix}' under {binDir}.");
            }

            RunFfdec(ffdec, "-swf2xml", uiBin, uiXml);

            string sourceXml = File.ReadAllText(uiXml);
            Dictionary<string, ClipSymbol> symbols = ParseSymbols(uiXml);
            List<string> clipNames = ParseMovieOrder(movieBin);

            foreach (string clipName in clipNames)
            {
                if (!symbols.TryGetValue(clipName, out ClipSymbol symbol))
                {
                    throw new InvalidOperationException(
                        $"[CinematicExporter] '{clipName}' is listed in the movie index but has no SymbolClass entry in the UI SWF.");
                }

                string wrapperXml = Path.Combine(tempRoot, $"{clipName}.xml");
                string wrapperSwf = Path.Combine(tempRoot, $"{clipName}.swf");
                string frameDir = Path.Combine(tempRoot, "frames", clipName);
                string outputPath = Path.Combine(projectRoot, OutputDirectory, $"{clipName}.mp4");

                if (Directory.Exists(frameDir))
                {
                    Directory.Delete(frameDir, true);
                }

                File.WriteAllText(wrapperXml, BuildWrapperXml(sourceXml, symbol.CharacterId, symbol.FrameCount));
                RunFfdec(ffdec, "-xml2swf", wrapperXml, wrapperSwf);
                RunFfdec(ffdec, "-config", "export.zoom=1.0", "-export", "frame", frameDir, wrapperSwf);
                EncodeClip(frameDir, symbol.FrameCount, outputPath);

                Debug.Log($"[CinematicExporter] {clipName}: {symbol.FrameCount} frames -> {outputPath}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"[CinematicExporter] Exported {clipNames.Count} cinematics to {OutputDirectory}.");
        }

        private readonly struct ClipSymbol
        {
            public readonly int CharacterId;
            public readonly int FrameCount;

            public ClipSymbol(int characterId, int frameCount)
            {
                CharacterId = characterId;
                FrameCount = frameCount;
            }
        }

        private static string ResolveFfdec()
        {
            string fromEnvironment = Environment.GetEnvironmentVariable("FFDEC_CLI");
            if (!string.IsNullOrWhiteSpace(fromEnvironment) && File.Exists(fromEnvironment))
            {
                return fromEnvironment;
            }

            string[] candidates =
            {
                @"C:\Program Files (x86)\FFDec\ffdec-cli.exe",
                @"C:\Program Files\FFDec\ffdec-cli.exe"
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return "ffdec-cli";
        }

        private static string ResolveSourceSwf(string repoRoot)
        {
            string[] candidates =
            {
                Path.Combine(repoRoot, "game", "a&b_space_mod.swf"),
                Path.Combine(repoRoot, "game", "a&b_space.swf")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException(
                "[CinematicExporter] Source SWF not found. Expected game/a&b_space_mod.swf or game/a&b_space.swf.");
        }

        private static string FindFileBySuffix(string directory, string suffix)
        {
            return Directory.GetFiles(directory)
                .FirstOrDefault(path => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, ClipSymbol> ParseSymbols(string uiXmlPath)
        {
            XDocument document = XDocument.Load(uiXmlPath);
            List<XElement> tags = document.Root.Element("tags").Elements("item").ToList();

            XElement symbolClass = tags.FirstOrDefault(tag => (string)tag.Attribute("type") == "SymbolClassTag");
            if (symbolClass == null)
            {
                throw new InvalidOperationException("[CinematicExporter] UI SWF has no SymbolClass tag.");
            }

            int[] ids = symbolClass.Element("tags").Elements("item").Select(item => int.Parse(item.Value)).ToArray();
            string[] names = symbolClass.Element("names").Elements("item").Select(item => item.Value).ToArray();
            Dictionary<int, int> frameCounts = tags
                .Where(tag => (string)tag.Attribute("type") == "DefineSpriteTag")
                .ToDictionary(
                    tag => int.Parse((string)tag.Attribute("spriteId")),
                    tag => int.Parse((string)tag.Attribute("frameCount")));

            var symbols = new Dictionary<string, ClipSymbol>(StringComparer.Ordinal);
            for (int i = 0; i < Math.Min(ids.Length, names.Length); i++)
            {
                if (frameCounts.TryGetValue(ids[i], out int frameCount))
                {
                    symbols[names[i]] = new ClipSymbol(ids[i], frameCount);
                }
            }

            return symbols;
        }

        private static List<string> ParseMovieOrder(string movieBinPath)
        {
            XDocument document = XDocument.Load(movieBinPath);
            List<string> names = document.Root.Elements("movieOne")
                .OrderBy(movie => int.Parse((string)movie.Element("index")))
                .Select(movie => (string)movie.Element("mcName"))
                .ToList();

            if (names.Count == 0)
            {
                throw new InvalidOperationException("[CinematicExporter] Movie index contains no clips.");
            }

            return names;
        }

        private static string BuildWrapperXml(string sourceXml, int characterId, int frameCount)
        {
            string xml = new Regex("frameCount=\"\\d+\"").Replace(
                sourceXml, $"frameCount=\"{frameCount}\"", 1);
            xml = new Regex("frameRate=\"[^\"]+\"").Replace(
                xml, $"frameRate=\"{FrameRate}.0\"", 1);

            const string showFrame = "<item type=\"ShowFrameTag\" forceWriteAsLong=\"false\"/>";
            int markerIndex = xml.LastIndexOf(showFrame, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                throw new InvalidOperationException("[CinematicExporter] UI XML has no root ShowFrame tag.");
            }

            var body = new StringBuilder();
            body.Append($"<item type=\"PlaceObject2Tag\" characterId=\"{characterId}\" depth=\"1\" ")
                .Append("forceWriteAsLong=\"true\" placeFlagHasCharacter=\"true\" placeFlagHasClipActions=\"false\" ")
                .Append("placeFlagHasClipDepth=\"false\" placeFlagHasColorTransform=\"false\" placeFlagHasMatrix=\"true\" ")
                .Append("placeFlagHasName=\"false\" placeFlagHasRatio=\"false\" placeFlagMove=\"false\">\n")
                .Append("<matrix type=\"MATRIX\" hasRotate=\"false\" hasScale=\"false\" nRotateBits=\"0\" ")
                .Append("nScaleBits=\"0\" nTranslateBits=\"0\" translateX=\"0\" translateY=\"0\"/>\n")
                .Append("</item>\n");

            for (int i = 0; i < frameCount; i++)
            {
                body.Append(showFrame).Append('\n');
            }

            return xml.Substring(0, markerIndex) + body + xml.Substring(markerIndex + showFrame.Length);
        }

        private static void EncodeClip(string frameDirectory, int frameCount, string outputPath)
        {
            string[] frames = Directory.GetFiles(frameDirectory, "*.png");
            if (frames.Length != frameCount)
            {
                throw new InvalidOperationException(
                    $"[CinematicExporter] Expected {frameCount} PNG frames in {frameDirectory}, found {frames.Length}.");
            }

            File.Delete(outputPath);

            var h264 = new H264EncoderAttributes
            {
                gopSize = FrameRate,
                numConsecutiveBFrames = 0,
                profile = VideoEncodingProfile.H264High
            };
            var videoAttributes = new VideoTrackEncoderAttributes(h264)
            {
                frameRate = new MediaRational(FrameRate),
                width = OutputWidth,
                height = OutputHeight,
                targetBitRate = 8_000_000
            };

            var source = new Texture2D(SourceWidth, SourceHeight, TextureFormat.RGBA32, false);
            var target = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
            RenderTexture renderTarget = RenderTexture.GetTemporary(
                OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try
            {
                using (var encoder = new MediaEncoder(outputPath, videoAttributes))
                {
                    for (int frame = 1; frame <= frameCount; frame++)
                    {
                        string framePath = Path.Combine(frameDirectory, $"{frame}.png");
                        if (!ImageConversion.LoadImage(source, File.ReadAllBytes(framePath), true))
                        {
                            throw new InvalidOperationException($"[CinematicExporter] Could not decode {framePath}.");
                        }

                        Graphics.Blit(
                            source,
                            renderTarget,
                            new Vector2(1f, SourceContentHeight / (float)SourceHeight),
                            new Vector2(0f, SourceContentY / (float)SourceHeight));

                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = renderTarget;
                        target.ReadPixels(new Rect(0, 0, OutputWidth, OutputHeight), 0, 0, false);
                        target.Apply(false);
                        RenderTexture.active = previous;

                        encoder.AddFrame(target);
                    }
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTarget);
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void RunFfdec(string ffdec, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo(ffdec)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = string.Join(" ", arguments.Select(QuoteArgument))
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                string output = outputTask.GetAwaiter().GetResult();
                string error = errorTask.GetAwaiter().GetResult();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"[CinematicExporter] FFDec exited with code {process.ExitCode}.\n{output}\n{error}");
                }
            }
        }

        private static string QuoteArgument(string argument)
        {
            return $"\"{argument.Replace("\"", "\\\"")}\"";
        }
    }
}
