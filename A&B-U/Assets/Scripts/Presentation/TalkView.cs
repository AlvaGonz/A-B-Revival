using System;
using ABRevival.Core.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit talk panel: portrait plus speaker name and line text. Elements are built once
    /// so each advance only assigns existing strings and a sprite reference.
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

            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);

            ClickArea = new VisualElement { name = "talk-click-area", pickingMode = PickingMode.Position };
            ClickArea.style.position = Position.Absolute;
            ClickArea.style.left = 0;
            ClickArea.style.right = 0;
            ClickArea.style.top = 0;
            ClickArea.style.bottom = 0;
            root.Add(ClickArea);

            _panel = new VisualElement { name = "talk-panel", pickingMode = PickingMode.Ignore };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 48;
            _panel.style.right = 48;
            _panel.style.bottom = 48;
            _panel.style.height = 220;
            _panel.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            root.Add(_panel);

            _portrait = new Image { name = "talk-portrait", pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            _portrait.style.position = Position.Absolute;
            _portrait.style.left = 16;
            _portrait.style.top = 16;
            _portrait.style.width = 160;
            _portrait.style.height = 160;
            _panel.Add(_portrait);

            _speaker = new Label { name = "talk-speaker", pickingMode = PickingMode.Ignore };
            _speaker.style.position = Position.Absolute;
            _speaker.style.left = 196;
            _speaker.style.top = 14;
            _speaker.style.fontSize = 26;
            _speaker.style.color = Color.white;
            _speaker.style.unityFontStyleAndWeight = FontStyle.Bold;
            _panel.Add(_speaker);

            _text = new Label { name = "talk-text", pickingMode = PickingMode.Ignore };
            _text.style.position = Position.Absolute;
            _text.style.left = 196;
            _text.style.right = 20;
            _text.style.top = 60;
            _text.style.bottom = 16;
            _text.style.fontSize = 22;
            _text.style.color = Color.white;
            _text.style.whiteSpace = WhiteSpace.Normal;
            _text.style.unityTextAlign = TextAnchor.UpperLeft;
            _panel.Add(_text);
        }

        public VisualElement ClickArea { get; }
        public Image Portrait => _portrait;
        public Label SpeakerName => _speaker;
        public Label Text => _text;

        public void ShowLine(TalkLine line)
        {
            _panel.style.display = DisplayStyle.Flex;
            ClickArea.pickingMode = PickingMode.Position;
            _speaker.text = line.Speaker;
            _text.text = line.Text;
            _portrait.sprite = _portraits != null ? _portraits.GetPortrait(line.SpeakerId) : null;
        }

        public void ShowComplete()
        {
            _panel.style.display = DisplayStyle.None;
            ClickArea.pickingMode = PickingMode.Ignore;
            _speaker.text = string.Empty;
            _text.text = string.Empty;
            _portrait.sprite = null;
        }
    }
}
