using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Instantiates every section prefab from the <see cref="ScreenCatalog"/> at boot.
    /// Runs before the default execution order so GameFlow can resolve the controllers in Start.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class ScreenHost : MonoBehaviour
    {
        [SerializeField] private ScreenCatalog catalog;
        [SerializeField] private Transform screensRoot;

        private void Awake()
        {
            if (catalog == null)
            {
                Debug.LogError("[ScreenHost] A ScreenCatalog is required.");
                return;
            }

            Transform parent = screensRoot != null ? screensRoot : transform;
            foreach (GameObject prefab in catalog.Sections)
            {
                if (prefab != null)
                    Instantiate(prefab, parent);
            }
        }
    }
}
