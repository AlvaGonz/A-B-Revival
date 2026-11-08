namespace ABRevival.Presentation
{
    /// <summary>
    /// Silent default used until the sound-bank implementation from ticket #158 is wired.
    /// </summary>
    public sealed class NoOpTalkAudio : ITalkAudio
    {
        public static readonly NoOpTalkAudio Instance = new NoOpTalkAudio();

        private NoOpTalkAudio()
        {
        }

        public void PlayAdvanceClick()
        {
        }
    }
}
