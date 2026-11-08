namespace ABRevival.Presentation
{
    /// <summary>
    /// Injected click-sound seam. Ticket #158 supplies the sound-bank implementation;
    /// the default is silent so dialogue playback works without the audio pipeline.
    /// </summary>
    public interface ITalkAudio
    {
        void PlayAdvanceClick();
    }
}
