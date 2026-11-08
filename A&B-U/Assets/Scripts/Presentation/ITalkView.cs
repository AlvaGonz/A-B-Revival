using ABRevival.Core.Data;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Narrow view seam for talk playback so sequencing is testable without a panel.
    /// </summary>
    public interface ITalkView
    {
        void ShowLine(TalkLine line);
        void ShowComplete();
    }
}
