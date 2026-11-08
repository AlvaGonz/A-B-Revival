using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Guards the boot scene's UI plumbing: UI Toolkit buttons only receive clicks when an
    /// EventSystem with an input module exists in the scene.
    /// </summary>
    public class BootSceneUiTests
    {
        [UnityTest]
        public IEnumerator BootScene_HasEventSystemWithInputActions()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "Boot scene needs an EventSystem for UI Toolkit clicks.");

            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(module, "Boot scene needs an InputSystemUIInputModule.");
            Assert.IsNotNull(module.point, "Input module has no point action; default actions did not assign.");
            Assert.IsNotNull(module.point.action, "Input module point action is null.");
        }
    }
}
