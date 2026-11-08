using System;
using ABRevival.Core.Data;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Drives one talk sequence: one line and one click sound per advance, then one completion.
    /// The presenter caches no per-line objects so advancing does not allocate managed memory.
    /// </summary>
    public sealed class TalkPresenter
    {
        private readonly TalkScript _script;
        private readonly ITalkView _view;
        private readonly ITalkAudio _audio;
        private readonly Action _onCompleted;
        private int _next;

        public TalkPresenter(TalkScript script, ITalkView view, ITalkAudio audio, Action onCompleted = null)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _audio = audio ?? NoOpTalkAudio.Instance;
            _onCompleted = onCompleted;
        }

        public bool IsComplete { get; private set; }
        public int DisplayedLineCount => _next;
        public int RemainingLineCount => _script.Count - _next;

        public void Advance()
        {
            if (IsComplete) return;

            if (_next < _script.Count)
            {
                TalkLine line = _script.Lines[_next];
                _next++;
                _view.ShowLine(line);
                _audio.PlayAdvanceClick();
                return;
            }

            IsComplete = true;
            _view.ShowComplete();
            _onCompleted?.Invoke();
        }
    }
}
