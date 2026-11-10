using System;
using ABRevival.Core.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Binds the imported talk script to a UI Toolkit document (ADR 0014) and advances on click.
    /// The click sound is injected through <see cref="ITalkAudio"/>; ticket #158 supplies the
    /// sound-bank component, which is picked up automatically when present on the same object.
    /// </summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class TalkScreen : MonoBehaviour
    {
        [SerializeField] private ScreenPanel _screen;
        [SerializeField] private TalkPortraitCatalog _portraits;
        [SerializeField] private TextAsset _talkData;
        [SerializeField] private int _firstLineIndex = 0;
        [SerializeField] private int _lastLineIndex = -1;
        [SerializeField] private bool _autoStart = true;

        private TalkPresenter _presenter;
        private TalkView _view;
        private bool _started;

        public ITalkAudio Audio { get; set; }
        public TalkPresenter Presenter => _presenter;
        public bool IsComplete => _presenter != null && _presenter.IsComplete;
        public event Action Completed;

        private void Awake()
        {
            _screen = GetComponent<ScreenPanel>();
            if (_screen != null) _screen.SetVisible(false);
            if (Audio == null) Audio = GetComponent<ITalkAudio>();
            if (Audio == null) Audio = NoOpTalkAudio.Instance;
        }

        private void Start()
        {
            if (!_autoStart) return;
            if (_started) return;

            if (_talkData == null) _talkData = Resources.Load<TextAsset>("Talk/5_all.AllData_talk");
            if (_portraits == null) _portraits = Resources.Load<TalkPortraitCatalog>("Talk/TalkPortraitCatalog");

            if (_talkData == null)
            {
                Debug.LogError("[TalkScreen] Talk data is missing from Resources/Talk.");
                return;
            }

            TalkScript script = TalkScriptParser.ParseXml(_talkData.text);
            int lastLineIndex = _lastLineIndex >= 0 ? _lastLineIndex : script.LastIndex;
            Begin(script.Slice(_firstLineIndex, lastLineIndex));
        }

        /// <summary>
        /// Starts a pre-loaded sequence. Story progression (#156) resolves talkBefore/talkAfter
        /// cues and hands the sliced script here; no story text is held by this component.
        /// </summary>
        public void Begin(TalkScript sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            _screen = GetComponent<ScreenPanel>();
            if (_screen == null)
            {
                Debug.LogError("[TalkScreen] ScreenPanel with a PanelRenderer is required.");
                return;
            }

            _started = true;
            SetVisible(true);
            _screen.WhenReady(root =>
            {
                // Build the panel once; repeated missions must not stack subtrees or callbacks.
                if (_view == null)
                {
                    _view = new TalkView(root, _portraits);
                    _view.ClickArea.RegisterCallback<ClickEvent>(OnClick);
                }

                _presenter = new TalkPresenter(sequence, _view, Audio, OnCompleted);
                _presenter.Advance();
            });
        }

        public void Advance()
        {
            if (_presenter != null) _presenter.Advance();
        }

        /// <summary>Shows or hides the talk overlay without starting a sequence.</summary>
        public void SetVisible(bool visible)
        {
            if (_screen == null) _screen = GetComponent<ScreenPanel>();
            _screen?.SetVisible(visible);
        }

        private void OnClick(ClickEvent clickEvent)
        {
            Advance();
        }

        private void OnCompleted()
        {
            Completed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_view != null) _view.ClickArea.UnregisterCallback<ClickEvent>(OnClick);
        }
    }
}
