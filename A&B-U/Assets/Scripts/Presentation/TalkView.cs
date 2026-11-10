using System;
using ABRevival.Core.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit talk panel: binds the static frame authored in <c>Talk.uxml</c> (ADR 0025)
    /// so each advance only assigns the portrait sprite, speaker name and line text.
    /// </summary>
    public sealed class TalkView : ITalkView
    {
        private readonly TalkPortraitCatalog _portraits;
        private readonly VisualElement _panel;
        private readonly Image _portrait;
        private readonly Label _speaker;
        private readonly Label _text;

        public TalkView(VisualElement root, TalkPortraitCatalog portraits)
        {
            _portraits = portraits;
            if (root == null) throw new ArgumentNullException(nameof(root));

            ClickArea = root.Q("talk-click-area");
            _panel = root.Q("talk-panel");
            _portrait = root.Q<Image>("talk-portrait");
            _speaker = root.Q<Label>("talk-speaker");
            _text = root.Q<Label>("talk-text");

            if (_panel != null)
            {
                _portrait.scaleMode = ScaleMode.StretchToFill;
                return;
            }

            // Bare roots (TalkPlaybackTests) carry no UXML, so create only the live nodes the
            // presenter touches. The frame itself is authored in Talk.uxml.
            _portrait = new Image { name = "talk-portrait", pickingMode = PickingMode.Ignore };
            root.Add(_portrait);
            _speaker = new Label { name = "talk-speaker", pickingMode = PickingMode.Ignore };
            root.Add(_speaker);
            _text = new Label { name = "talk-text", pickingMode = PickingMode.Ignore };
            root.Add(_text);
        }

        public VisualElement ClickArea { get; }
        public Image Portrait => _portrait;
        public Label SpeakerName => _speaker;
        public Label Text => _text;

        public void ShowLine(TalkLine line)
        {
            if (_panel != null) _panel.style.display = DisplayStyle.Flex;
            if (ClickArea != null) ClickArea.pickingMode = PickingMode.Position;
            _speaker.text = line.Speaker;
            _text.text = line.Text;
            _portrait.sprite = _portraits != null ? _portraits.GetPortrait(line.SpeakerId) : null;
        }

        public void ShowComplete()
        {
            if (_panel != null) _panel.style.display = DisplayStyle.None;
            if (ClickArea != null) ClickArea.pickingMode = PickingMode.Ignore;
            _speaker.text = string.Empty;
            _text.text = string.Empty;
            _portrait.sprite = null;
        }
    }
}
