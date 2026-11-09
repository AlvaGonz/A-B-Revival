using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Shared fitter for the frozen full-page menu renders (Main, Options, Ready, Results,
    /// Repair). Keeps page-local pixel coordinates while scaling the page into the output:
    /// cover for full-bleed sheets, contain for pages that sit on a starfield backdrop.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class MenuPageFitter
    {
        /// <summary>Fits the page and returns false when the root size did not change.</summary>
        public static bool Fit(
            VisualElement page,
            float width,
            float height,
            float pageWidth,
            float pageHeight,
            bool cover,
            ref Vector2 lastRootSize)
        {
            if (page == null || width <= 1f || height <= 1f) return false;
            if (Mathf.Approximately(width, lastRootSize.x) &&
                Mathf.Approximately(height, lastRootSize.y))
            {
                return false;
            }

            lastRootSize = new Vector2(width, height);
            float scale = cover
                ? Mathf.Max(width / pageWidth, height / pageHeight)
                : Mathf.Min(width / pageWidth, height / pageHeight);

            page.style.left = (width - pageWidth) * 0.5f;
            page.style.top = (height - pageHeight) * 0.5f;
            page.style.transformOrigin = new StyleTransformOrigin(
                new TransformOrigin(Length.Percent(50f), Length.Percent(50f), 0f));
            page.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
            return true;
        }
    }
}
