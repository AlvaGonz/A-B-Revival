using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Static sprite surface for UXML/UI Builder authoring. Renders the frozen art through an
    /// <see cref="Image"/> child because UI Toolkit's background-image path samples atlased
    /// sprites at their atlas rect (ADR 0025). Positions and sizes live in USS.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [UxmlElement]
    public partial class SpriteElement : VisualElement
    {
        private const string ArtElement = "sprite-art";

        private readonly Image _image = new Image
        {
            name = ArtElement,
            pickingMode = PickingMode.Ignore
        };

        [UxmlAttribute]
        public Sprite sprite
        {
            get => _image.sprite;
            set => _image.sprite = value;
        }

        [UxmlAttribute]
        public ScaleMode scaleMode
        {
            get => _image.scaleMode;
            set => _image.scaleMode = value;
        }

        [UxmlAttribute]
        public Color tint
        {
            get => _image.tintColor;
            set => _image.tintColor = value;
        }

        public SpriteElement()
        {
            pickingMode = PickingMode.Ignore;
            // Art layers are overlays; never let the sprite drive the parent's content size.
            style.position = Position.Absolute;
            _image.style.position = Position.Absolute;
            _image.style.left = 0;
            _image.style.right = 0;
            _image.style.top = 0;
            _image.style.bottom = 0;
            Add(_image);
        }
    }
}
