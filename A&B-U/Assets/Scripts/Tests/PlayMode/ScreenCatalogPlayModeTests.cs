using System.Collections;
using System.Collections.Generic;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// ADR 0025 acceptance: the ScreenCatalog is the single direct-reference list, it has no
    /// nulls or duplicates, and Boot instantiates exactly the 11 section prefabs.
    /// </summary>
    public sealed class ScreenCatalogPlayModeTests
    {
        [UnityTest]
        public IEnumerator Boot_InstantiatesTheElevenSectionPrefabsFromTheCatalog()
        {
            yield return PlayModeBoot.LoadBoot();
            yield return null;

            ScreenHost host = Object.FindAnyObjectByType<ScreenHost>();
            Assert.IsNotNull(host, "Boot must host the ScreenHost.");
            ScreenCatalog catalog = host.Catalog;
            Assert.IsNotNull(catalog, "ScreenHost must reference the ScreenCatalog.");
            Assert.AreEqual(11, catalog.Sections.Count, "The catalog must list 11 sections.");

            var seen = new HashSet<GameObject>();
            foreach (GameObject prefab in catalog.Sections)
            {
                Assert.IsNotNull(prefab, "The catalog must not contain null sections.");
                Assert.IsTrue(seen.Add(prefab), "The catalog must not contain duplicate sections.");
            }

            GameObject screens = GameObject.Find("Screens");
            Assert.IsNotNull(screens, "Boot must have a Screens root.");
            ScreenPanel[] panels = screens.GetComponentsInChildren<ScreenPanel>(true);
            Assert.AreEqual(11, panels.Length, "The Screens root must instantiate all 11 sections.");

            var orders = new HashSet<int>();
            var trees = new HashSet<VisualTreeAsset>();
            foreach (ScreenPanel panel in panels)
            {
                PanelRenderer renderer = panel.GetComponent<PanelRenderer>();
                Assert.IsNotNull(renderer, panel.name + " must have a PanelRenderer.");
                Assert.IsTrue(orders.Add(panel.SortingOrder),
                    panel.name + " reuses sortingOrder " + panel.SortingOrder + ".");
                VisualTreeAsset tree = renderer.visualTreeAsset;
                Assert.IsNotNull(tree, panel.name + " has no visualTreeAsset.");
                Assert.IsTrue(trees.Add(tree),
                    "Two sections share " + tree.name + "; check the prefab sourceAsset guid.");
            }
        }
    }
}
