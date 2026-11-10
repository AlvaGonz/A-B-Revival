using System.Collections;
using System.Reflection;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Shared Boot-scene driver for PlayMode tests. The frozen flow boots into UIMain; the
    /// menu tickets (#181) keep that order, so every screen test starts with PLAY.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class PlayModeBoot
    {
        public const int WaitFrames = 600;

        public static IEnumerator LoadMainMenu()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            yield return WaitForMainMenu();
        }

        public static IEnumerator LoadCenter()
        {
            yield return LoadMainMenu();
            StartGame();
            yield return WaitForCenter();
            yield return null;
            yield return null;
        }

        public static IEnumerator WaitForMainMenu()
        {
            for (int i = 0; i < WaitFrames; i++)
            {
                MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
                if (menu != null)
                {
                    ScreenPanel panel = menu.GetComponent<ScreenPanel>();
                    if (panel != null && panel.Root != null && panel.Root.Q("main-root") != null)
                    {
                        yield return null;
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail("Boot did not reach the main menu.");
        }

        public static IEnumerator WaitForCenter()
        {
            for (int i = 0; i < WaitFrames; i++)
            {
                CenterController center = Object.FindAnyObjectByType<CenterController>();
                GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
                if (center != null && center.FleetCount > 0 && flow != null && flow.State == GameFlowState.Center)
                {
                    var document = center.GetComponent<UIDocument>();
                    if (document != null && document.rootVisualElement != null &&
                        document.rootVisualElement.Q("center-root") != null)
                    {
                        yield return null;
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail("Boot did not reach the Center screen after PLAY.");
        }

        public static void StartGame()
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "Boot must host the main menu.");
            VisualElement root = menu.GetComponent<ScreenPanel>().Root;
            Assert.IsNotNull(root, "The main menu has no root.");
            Click(root.Q<Button>("play-button"));
        }

        public static void Click(Button button)
        {
            Assert.IsNotNull(button);
            MethodInfo simulate = typeof(Clickable).GetMethod(
                "SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(simulate, "UI Toolkit Clickable.SimulateSingleClick moved.");
            simulate.Invoke(button.clickable, new object[] { null, 100 });
        }
    }
}
