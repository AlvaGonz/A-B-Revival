using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Direct references to the 11 section prefabs. Boot's <see cref="ScreenHost"/> instantiates
    /// this list at startup; keeping the references here is what pulls the prefabs into the build
    /// without Resources or Addressables (ADR 0025).
    /// Original game IP: Vargames (2011).
    /// </summary>
    [CreateAssetMenu(menuName = "A&B Revival/Screen Catalog", fileName = "ScreenCatalog")]
    public sealed class ScreenCatalog : ScriptableObject
    {
        [SerializeField] private GameObject[] sections = Array.Empty<GameObject>();

        public IReadOnlyList<GameObject> Sections => sections;
    }
}
