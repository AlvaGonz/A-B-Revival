using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Applies a frozen sprite to a UI Toolkit element through an <see cref="Image"/> child.
    /// UI Toolkit's background-image path samples an atlased sprite at its local rect on the
    /// atlas page, so the original button/tab/icon art renders as an unrelated atlas fragment.
    /// The Image element consumes the sprite's textureRect and keeps the extracted art intact.
    /// The inline background image is preserved for existing capture contracts and tests.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class UiSpriteSkin
    {
        private const string ArtElement = "sprite-art";
        private const string TextElement = "sprite-text";

        /// <summary>Adds or updates the rendered art and, for text buttons, the text overlay.</summary>
        public static void Apply(VisualElement element, Sprite sprite, ScaleMode scaleMode,
            TextAnchor textAlign = TextAnchor.MiddleCenter, bool keepBackground = true)
        {
            if (element == null || sprite == null) return;

            // Existing sections keep the inline assignment as their serialized contract.
            // New surfaces pass keepBackground: false so the atlased sprite is never sampled
            // through UI Toolkit's broken background-image path.
            element.style.backgroundImage = keepBackground
                ? new StyleBackground(sprite)
                : StyleKeyword.None;

            Image art = FindChild<Image>(element, ArtElement);
            if (art == null)
            {
                art = new Image { name = ArtElement, pickingMode = PickingMode.Ignore };
                art.style.position = Position.Absolute;
                art.style.left = 0;
                art.style.right = 0;
                art.style.top = 0;
                art.style.bottom = 0;
                element.Insert(0, art);
            }

            art.sprite = sprite;
            art.scaleMode = scaleMode;

            if (element is TextElement text && !string.IsNullOrEmpty(text.text))
                SyncText(element, textAlign);
        }

        /// <summary>Copies the button's live text to the overlay label after a text change.</summary>
        public static void SyncText(VisualElement element, TextAnchor textAlign = TextAnchor.MiddleCenter)
        {
            if (!(element is TextElement text) || string.IsNullOrEmpty(text.text)) return;

            Label label = FindChild<Label>(element, TextElement);
            if (label == null)
            {
                label = new Label { name = TextElement, pickingMode = PickingMode.Ignore };
                label.style.position = Position.Absolute;
                label.style.left = 0;
                label.style.right = 0;
                label.style.top = 0;
                label.style.bottom = 0;
                label.style.unityTextAlign = textAlign;
                element.Add(label);
            }

            label.text = text.text;
        }

        private static T FindChild<T>(VisualElement parent, string name) where T : VisualElement
        {
            foreach (VisualElement child in parent.Children())
            {
                if (child.name == name && child is T match) return match;
            }

            return null;
        }
    }
}
