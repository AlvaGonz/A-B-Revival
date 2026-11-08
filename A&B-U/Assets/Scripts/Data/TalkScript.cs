using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// One imported dialogue line from 5_all.AllData_talk.bin.
    /// SpeakerId is assigned in first-appearance order and is stable across reloads.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public readonly struct TalkLine
    {
        public readonly int Index;
        public readonly int SpeakerId;
        public readonly string Speaker;
        public readonly string Text;

        public TalkLine(int index, int speakerId, string speaker, string text)
        {
            Index = index;
            SpeakerId = speakerId;
            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>
    /// Immutable, deterministically ordered talk script imported from the frozen XML data.
    /// Lines keep their source index; story progression slices the script by index range.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class TalkScript
    {
        private readonly TalkLine[] _lines;
        private readonly string[] _speakers;

        public TalkScript(TalkLine[] lines, string[] speakers)
        {
            _lines = lines ?? throw new ArgumentNullException(nameof(lines));
            _speakers = speakers ?? throw new ArgumentNullException(nameof(speakers));
        }

        public IReadOnlyList<TalkLine> Lines => _lines;
        public IReadOnlyList<string> Speakers => _speakers;
        public int Count => _lines.Length;
        public int SpeakerCount => _speakers.Length;
        public int FirstIndex => _lines.Length == 0 ? -1 : _lines[0].Index;
        public int LastIndex => _lines.Length == 0 ? -1 : _lines[_lines.Length - 1].Index;

        /// <summary>
        /// Finds the array offset of a source line index. Returns -1 when absent.
        /// </summary>
        public int IndexOfLine(int sourceIndex)
        {
            int lo = 0;
            int hi = _lines.Length - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int value = _lines[mid].Index;
                if (value == sourceIndex) return mid;
                if (value < sourceIndex) lo = mid + 1;
                else hi = mid - 1;
            }
            return -1;
        }

        public bool TryGetLine(int sourceIndex, out TalkLine line)
        {
            int offset = IndexOfLine(sourceIndex);
            if (offset < 0)
            {
                line = default;
                return false;
            }
            line = _lines[offset];
            return true;
        }

        /// <summary>
        /// Returns the inclusive source-index range used by a talkBefore/talkAfter cue.
        /// Speaker ids stay global so portrait lookup never changes between cues.
        /// </summary>
        public TalkScript Slice(int firstSourceIndex, int lastSourceIndex)
        {
            int start = IndexOfLine(firstSourceIndex);
            int end = IndexOfLine(lastSourceIndex);
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstSourceIndex), firstSourceIndex, "Talk line index is not present in this script.");
            }
            if (end < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lastSourceIndex), lastSourceIndex, "Talk line index is not present in this script.");
            }
            if (end < start)
            {
                throw new ArgumentException("Last talk line index must not precede the first.", nameof(lastSourceIndex));
            }

            int count = end - start + 1;
            var lines = new TalkLine[count];
            Array.Copy(_lines, start, lines, 0, count);
            return new TalkScript(lines, _speakers);
        }
    }

    /// <summary>
    /// Pure C# importer for the frozen XML dialogue data. No Unity dependencies, so the
    /// full script is validated inside the Tier 1 dotnet test gate.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class TalkScriptParser
    {
        public static TalkScript ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("Talk data file not found.", path);
            return ParseXml(File.ReadAllText(path));
        }

        public static TalkScript ParseXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) throw new ArgumentException("Talk XML is empty.", nameof(xml));

            XDocument document;
            try
            {
                document = XDocument.Parse(xml);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("Talk XML could not be parsed.", exception);
            }

            XElement root = document.Root;
            if (root == null || root.Name != "talk")
            {
                throw new InvalidDataException("Talk XML root element must be <talk>.");
            }

            var raw = new List<TalkLine>();
            var sourceIndices = new HashSet<int>();
            foreach (XElement element in root.Elements("talkOne"))
            {
                int index = ParseIndex(element);
                if (!sourceIndices.Add(index))
                {
                    throw new InvalidDataException($"Duplicate talk line index {index}.");
                }

                string speaker = RequiredValue(element, "who", index);
                string text = RequiredValue(element, "word", index);
                raw.Add(new TalkLine(index, 0, speaker, text));
            }

            if (raw.Count == 0)
            {
                throw new InvalidDataException("Talk XML contains no <talkOne> lines.");
            }

            raw.Sort((left, right) => left.Index.CompareTo(right.Index));

            var speakerIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var speakerNames = new List<string>();
            var lines = new TalkLine[raw.Count];
            for (int i = 0; i < raw.Count; i++)
            {
                TalkLine source = raw[i];
                int speakerId;
                if (!speakerIds.TryGetValue(source.Speaker, out speakerId))
                {
                    speakerId = speakerNames.Count;
                    speakerIds.Add(source.Speaker, speakerId);
                    speakerNames.Add(source.Speaker);
                }
                lines[i] = new TalkLine(source.Index, speakerId, source.Speaker, source.Text);
            }

            return new TalkScript(lines, speakerNames.ToArray());
        }

        private static int ParseIndex(XElement element)
        {
            string value = element.Element("index")?.Value;
            int index;
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out index))
            {
                throw new InvalidDataException("Talk line is missing a valid <index>.");
            }
            return index;
        }

        private static string RequiredValue(XElement element, string name, int index)
        {
            string value = element.Element(name)?.Value;
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidDataException($"Talk line {index} is missing <{name}>.");
            }
            return value;
        }
    }
}
