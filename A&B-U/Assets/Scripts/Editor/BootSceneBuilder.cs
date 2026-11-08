using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace ABRevival.Editor
{
    /// <summary>
    /// Deterministically rebuilds the boot scene that hosts the story-0 game flow.
    /// Run from Tools/A&amp;B Revival/Build Boot Scene or in batchmode with
    /// -executeMethod ABRevival.Editor.BootSceneBuilder.BuildBatch.
    /// Presentation types are resolved by name because this editor assembly intentionally
    /// does not reference the runtime presentation assembly.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class BootSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string HangarScenePath = "Assets/Scenes/Hangar.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string HangarUxmlPath = "Assets/UI/Hangar.uxml";
        private const string HudUxmlPath = "Assets/UI/CombatHud.uxml";
        private const string PanelSettingsPath = "Assets/UI/HangarPanelSettings.asset";
        private const string PortraitPath = "Assets/Resources/Talk/TalkPortraitCatalog.asset";
        private const string TalkDataPath = "Assets/Resources/Talk/5_all.AllData_talk.xml";
        private const string OpeningClipPath = "Assets/Resources/Cinematics/Mov1.mp4";
        private const string CatalogFolder = "Assets/Data/Parts";
        private const string PresentationAssembly = ", ABRevival.Presentation";

        [MenuItem("Tools/A&B Revival/Build Boot Scene")]
        public static void Build()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PanelSettings panelSettings = LoadRequired<PanelSettings>(PanelSettingsPath);
            VisualTreeAsset hangarUxml = LoadRequired<VisualTreeAsset>(HangarUxmlPath);
            VisualTreeAsset hudUxml = LoadRequired<VisualTreeAsset>(HudUxmlPath);
            UnityEngine.Object portraits = LoadRequired<UnityEngine.Object>(PortraitPath);
            TextAsset talkData = LoadRequired<TextAsset>(TalkDataPath);
            VideoClip openingClip = LoadRequired<VideoClip>(OpeningClipPath);

            var audioObject = new GameObject("Audio");
            Component audio = AddPresentationComponent(audioObject, "ABRevival.Presentation.Audio.AudioService");

            // UI Toolkit buttons need an EventSystem with an input module; without this,
            // the hangar's Launch Mission button never receives a click.
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var hangarObject = new GameObject("Hangar");
            UIDocument hangarDocument = hangarObject.AddComponent<UIDocument>();
            SetReference(hangarDocument, "m_PanelSettings", panelSettings);
            SetReference(hangarDocument, "sourceAsset", hangarUxml);
            hangarDocument.sortingOrder = 0;
            Component hangar = AddPresentationComponent(hangarObject, "ABRevival.Presentation.HangarController");

            var talkObject = new GameObject("Talk");
            UIDocument talkDocument = talkObject.AddComponent<UIDocument>();
            SetReference(talkDocument, "m_PanelSettings", panelSettings);
            talkDocument.sortingOrder = 10;
            Component talk = AddPresentationComponent(talkObject, "ABRevival.Presentation.TalkScreen");

            var combatObject = new GameObject("Combat");
            combatObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = combatObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            camera.tag = "MainCamera";
            combatObject.AddComponent<AudioListener>();
            Component cameraRig = AddPresentationComponent(combatObject, "ABRevival.Presentation.CombatCameraRig");
            Component batchRenderer = AddPresentationComponent(combatObject, "ABRevival.Presentation.BatchSpriteRenderer");
            Component spriteView = AddPresentationComponent(combatObject, "ABRevival.Presentation.MissionSpriteView");
            UIDocument hudDocument = combatObject.AddComponent<UIDocument>();
            SetReference(hudDocument, "m_PanelSettings", panelSettings);
            SetReference(hudDocument, "sourceAsset", hudUxml);
            hudDocument.sortingOrder = 5;
            Component hud = AddPresentationComponent(combatObject, "ABRevival.Presentation.CombatHudBehaviour");

            var cinematicObject = new GameObject("Cinematics");
            cinematicObject.AddComponent<VideoPlayer>().playOnAwake = false;
            Component cinematic = AddPresentationComponent(
                cinematicObject, "ABRevival.Presentation.Cinematics.CinematicPlayer");

            var flowObject = new GameObject("GameFlow");
            Component flow = AddPresentationComponent(flowObject, "ABRevival.Presentation.GameFlowController");

            var flightObject = new GameObject("Flight");
            Component flight = AddPresentationComponent(
                flightObject, "ABRevival.Presentation.PlayerFlightController");

            SetReference(talk, "_document", talkDocument);
            SetReference(talk, "_portraits", portraits);
            SetReference(talk, "_talkData", talkData);
            SetBool(talk, "_autoStart", false);

            SetReference(hud, "_document", hudDocument);
            SetReference(hud, "_cameraRig", cameraRig);
            SetCatalog(hangar);

            Texture2D partsAtlas = LoadRequired<Texture2D>(PartsAtlasBuilder.AtlasPath);
            TextAsset partsUvTable = LoadRequired<TextAsset>(PartsAtlasBuilder.UvTablePath);
            Shader spriteShader = LoadRequired<Shader>("Assets/Scripts/Presentation/Shaders/InstancedSprite.shader");
            SetReference(batchRenderer, "_spriteShader", spriteShader);
            SetReference(batchRenderer, "_atlas", partsAtlas);
            SetReference(spriteView, "_renderer", batchRenderer);
            SetReference(spriteView, "_atlas", partsAtlas);
            SetReference(spriteView, "_uvTableData", partsUvTable);

            var flowSerialized = new SerializedObject(flow);
            SetReference(flowSerialized, "_hangar", hangar);
            SetReference(flowSerialized, "_talkScreen", talk);
            SetReference(flowSerialized, "_cinematicPlayer", cinematic);
            SetReference(flowSerialized, "_hud", hud);
            SetReference(flowSerialized, "_cameraRig", cameraRig);
            SetReference(flowSerialized, "_spriteView", spriteView);
            SetReference(flowSerialized, "_flight", flight);
            SetReference(flowSerialized, "_audio", audio);
            SetReference(flowSerialized, "_flowDocument", hudDocument);

            SerializedProperty clips = flowSerialized.FindProperty("_briefingClips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = openingClip;

            flowSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(HangarScenePath, true),
                new EditorBuildSettingsScene(SampleScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BootSceneBuilder] Wrote {ScenePath} and registered it first in build settings.");
        }

        /// <summary>Batchmode entry point (-executeMethod ABRevival.Editor.BootSceneBuilder.BuildBatch).</summary>
        public static void BuildBatch()
        {
            Build();
        }

        private static Component AddPresentationComponent(GameObject host, string fullTypeName)
        {
            Type type = Type.GetType(fullTypeName + PresentationAssembly);
            if (type == null)
            {
                throw new InvalidOperationException(
                    $"[BootSceneBuilder] Presentation type '{fullTypeName}' was not loaded.");
            }

            return host.AddComponent(type);
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"[BootSceneBuilder] Missing asset at {path}.");
            }

            return asset;
        }

        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SetReference(serialized, property, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Assigns every generated PartDefinitionSO to the hangar, sorted by legacy part id so
        /// the scene is deterministic across import runs.
        /// </summary>
        private static void SetCatalog(Component hangar)
        {
            string[] guids = AssetDatabase.FindAssets("t:PartDefinitionSO", new[] { CatalogFolder });
            var parts = new List<PartDefinitionSO>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                var part = AssetDatabase.LoadAssetAtPath<PartDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (part != null) parts.Add(part);
            }
            parts.Sort((left, right) => left.partId.CompareTo(right.partId));

            var serialized = new SerializedObject(hangar);
            SerializedProperty catalog = serialized.FindProperty("catalogParts");
            if (catalog == null)
            {
                throw new InvalidOperationException("[BootSceneBuilder] HangarController is missing catalogParts.");
            }

            catalog.arraySize = parts.Count;
            for (int i = 0; i < parts.Count; i++)
                catalog.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[BootSceneBuilder] Assigned {parts.Count} PartDefinitionSO assets to the hangar.");
        }

        private static void SetReference(SerializedObject serialized, string property, UnityEngine.Object value)
        {
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
            {
                throw new InvalidOperationException($"[BootSceneBuilder] Missing serialized field {property}.");
            }

            field.objectReferenceValue = value;
        }

        private static void SetBool(UnityEngine.Object target, string property, bool value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
            {
                throw new InvalidOperationException($"[BootSceneBuilder] Missing serialized field {property}.");
            }

            field.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
