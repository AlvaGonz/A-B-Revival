using System;
using System.Collections.Generic;
using System.IO;
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
        private const string MainMenuUxmlPath = "Assets/UI/Main.uxml";
        private const string OptionsUxmlPath = "Assets/UI/Options.uxml";
        private const string CenterUxmlPath = "Assets/UI/Center.uxml";
        private const string FactoryUxmlPath = "Assets/UI/Factory.uxml";
        private const string ShopUxmlPath = "Assets/UI/Shop.uxml";
        private const string InfoUxmlPath = "Assets/UI/Info.uxml";
        private const string ReadyUxmlPath = "Assets/UI/Ready.uxml";
        private const string ResultsUxmlPath = "Assets/UI/Results.uxml";
        private const string RepairUxmlPath = "Assets/UI/Repair.uxml";
        private const string HudUxmlPath = "Assets/UI/CombatHud.uxml";
        private const string PanelSettingsPath = "Assets/UI/HangarPanelSettings.asset";
        private const string PortraitPath = "Assets/Resources/Talk/TalkPortraitCatalog.asset";
        private const string TalkDataPath = "Assets/Resources/Talk/5_all.AllData_talk.xml";
        private const string OpeningClipPath = "Assets/Resources/Cinematics/Mov1.mp4";
        private const string CatalogFolder = "Assets/Data/Parts";
        private const string UiSpriteFolder = "Assets/Art/Sprites/UI";
        private const string MountSpriteFolder = "Assets/Art/Sprites/PartMounts";
        private const string PresentationAssembly = ", ABRevival.Presentation";

        [MenuItem("Tools/A&B Revival/Build Boot Scene")]
        public static void Build()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PanelSettings panelSettings = LoadRequired<PanelSettings>(PanelSettingsPath);
            VisualTreeAsset mainMenuUxml = LoadRequired<VisualTreeAsset>(MainMenuUxmlPath);
            VisualTreeAsset optionsUxml = LoadRequired<VisualTreeAsset>(OptionsUxmlPath);
            VisualTreeAsset centerUxml = LoadRequired<VisualTreeAsset>(CenterUxmlPath);
            VisualTreeAsset factoryUxml = LoadRequired<VisualTreeAsset>(FactoryUxmlPath);
            VisualTreeAsset shopUxml = LoadRequired<VisualTreeAsset>(ShopUxmlPath);
            VisualTreeAsset infoUxml = LoadRequired<VisualTreeAsset>(InfoUxmlPath);
            VisualTreeAsset readyUxml = LoadRequired<VisualTreeAsset>(ReadyUxmlPath);
            VisualTreeAsset resultsUxml = LoadRequired<VisualTreeAsset>(ResultsUxmlPath);
            VisualTreeAsset repairUxml = LoadRequired<VisualTreeAsset>(RepairUxmlPath);
            VisualTreeAsset hudUxml = LoadRequired<VisualTreeAsset>(HudUxmlPath);
            UnityEngine.Object portraits = LoadRequired<UnityEngine.Object>(PortraitPath);
            TextAsset talkData = LoadRequired<TextAsset>(TalkDataPath);
            VideoClip openingClip = LoadRequired<VideoClip>(OpeningClipPath);

            var audioObject = new GameObject("Audio");
            Component audio = AddPresentationComponent(audioObject, "ABRevival.Presentation.Audio.AudioService");

            // UI Toolkit buttons need an EventSystem with an input module; without this,
            // Center's navigation buttons never receive a click.
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var mainMenuObject = new GameObject("MainMenu");
            UIDocument mainMenuDocument = mainMenuObject.AddComponent<UIDocument>();
            SetReference(mainMenuDocument, "m_PanelSettings", panelSettings);
            SetReference(mainMenuDocument, "sourceAsset", mainMenuUxml);
            mainMenuDocument.sortingOrder = 2;
            Component mainMenu = AddPresentationComponent(mainMenuObject, "ABRevival.Presentation.MainMenuController");

            var optionsObject = new GameObject("Options");
            UIDocument optionsDocument = optionsObject.AddComponent<UIDocument>();
            SetReference(optionsDocument, "m_PanelSettings", panelSettings);
            SetReference(optionsDocument, "sourceAsset", optionsUxml);
            optionsDocument.sortingOrder = 3;
            Component options = AddPresentationComponent(optionsObject, "ABRevival.Presentation.OptionsController");

            var centerObject = new GameObject("Center");
            UIDocument centerDocument = centerObject.AddComponent<UIDocument>();
            SetReference(centerDocument, "m_PanelSettings", panelSettings);
            SetReference(centerDocument, "sourceAsset", centerUxml);
            centerDocument.sortingOrder = 0;
            Component center = AddPresentationComponent(centerObject, "ABRevival.Presentation.CenterController");

            var factoryObject = new GameObject("Factory");
            UIDocument factoryDocument = factoryObject.AddComponent<UIDocument>();
            SetReference(factoryDocument, "m_PanelSettings", panelSettings);
            SetReference(factoryDocument, "sourceAsset", factoryUxml);
            factoryDocument.sortingOrder = 1;
            Component factory = AddPresentationComponent(factoryObject, "ABRevival.Presentation.FactoryController");

            var shopObject = new GameObject("Shop");
            UIDocument shopDocument = shopObject.AddComponent<UIDocument>();
            SetReference(shopDocument, "m_PanelSettings", panelSettings);
            SetReference(shopDocument, "sourceAsset", shopUxml);
            shopDocument.sortingOrder = 1;
            Component shop = AddPresentationComponent(shopObject, "ABRevival.Presentation.ShopController");

            var infoObject = new GameObject("Info");
            UIDocument infoDocument = infoObject.AddComponent<UIDocument>();
            SetReference(infoDocument, "m_PanelSettings", panelSettings);
            SetReference(infoDocument, "sourceAsset", infoUxml);
            infoDocument.sortingOrder = 1;
            Component info = AddPresentationComponent(infoObject, "ABRevival.Presentation.InfoController");

            var readyObject = new GameObject("Ready");
            UIDocument readyDocument = readyObject.AddComponent<UIDocument>();
            SetReference(readyDocument, "m_PanelSettings", panelSettings);
            SetReference(readyDocument, "sourceAsset", readyUxml);
            readyDocument.sortingOrder = 1;
            Component ready = AddPresentationComponent(readyObject, "ABRevival.Presentation.ReadyController");

            var resultsObject = new GameObject("Results");
            UIDocument resultsDocument = resultsObject.AddComponent<UIDocument>();
            SetReference(resultsDocument, "m_PanelSettings", panelSettings);
            SetReference(resultsDocument, "sourceAsset", resultsUxml);
            resultsDocument.sortingOrder = 6;
            Component results = AddPresentationComponent(resultsObject, "ABRevival.Presentation.ResultsController");

            var repairObject = new GameObject("Repair");
            UIDocument repairDocument = repairObject.AddComponent<UIDocument>();
            SetReference(repairDocument, "m_PanelSettings", panelSettings);
            SetReference(repairDocument, "sourceAsset", repairUxml);
            repairDocument.sortingOrder = 6;
            Component repair = AddPresentationComponent(repairObject, "ABRevival.Presentation.RepairController");

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
            SetMainMenuSprites(mainMenu);
            SetOptionsSprites(options);
            SetCatalog(center);
            SetCenterSprites(center);
            SetCatalog(factory);
            SetFactorySprites(factory);
            SetCatalog(shop);
            SetShopSprites(shop);
            SetCatalog(info);
            SetInfoSprites(info);
            SetReadySprites(ready);
            SetResultsSprites(results);
            SetCatalog(repair);
            SetRepairSprites(repair);

            Texture2D partsAtlas = LoadRequired<Texture2D>(PartsAtlasBuilder.AtlasPath);
            TextAsset partsUvTable = LoadRequired<TextAsset>(PartsAtlasBuilder.UvTablePath);
            Shader spriteShader = LoadRequired<Shader>("Assets/Scripts/Presentation/Shaders/InstancedSprite.shader");
            SetReference(batchRenderer, "_spriteShader", spriteShader);
            SetReference(batchRenderer, "_atlas", partsAtlas);
            SetReference(spriteView, "_renderer", batchRenderer);
            SetReference(spriteView, "_atlas", partsAtlas);
            SetReference(spriteView, "_uvTableData", partsUvTable);

            var flowSerialized = new SerializedObject(flow);
            SetReference(flowSerialized, "_mainMenu", mainMenu);
            SetReference(flowSerialized, "_options", options);
            SetReference(flowSerialized, "_center", center);
            SetReference(flowSerialized, "_factory", factory);
            SetReference(flowSerialized, "_shop", shop);
            SetReference(flowSerialized, "_info", info);
            SetReference(flowSerialized, "_ready", ready);
            SetReference(flowSerialized, "_results", results);
            SetReference(flowSerialized, "_repairScreen", repair);
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
                new EditorBuildSettingsScene(ScenePath, true)
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
        /// Assigns every generated PartDefinitionSO to a screen, sorted by legacy part id so
        /// the scene is deterministic across import runs.
        /// </summary>
        private static void SetCatalog(Component screen)
        {
            string[] guids = AssetDatabase.FindAssets("t:PartDefinitionSO", new[] { CatalogFolder });
            var parts = new List<PartDefinitionSO>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                var part = AssetDatabase.LoadAssetAtPath<PartDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (part != null) parts.Add(part);
            }
            parts.Sort((left, right) => left.partId.CompareTo(right.partId));

            var serialized = new SerializedObject(screen);
            SerializedProperty catalog = serialized.FindProperty("catalogParts");
            if (catalog == null)
            {
                throw new InvalidOperationException("[BootSceneBuilder] " + screen.GetType().Name + " is missing catalogParts.");
            }

            catalog.arraySize = parts.Count;
            for (int i = 0; i < parts.Count; i++)
                catalog.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[BootSceneBuilder] Assigned {parts.Count} PartDefinitionSO assets to {screen.GetType().Name}.");
        }

        /// <summary>
        /// Wires the original 2011 UI sprites into Center's serialized fields. The runtime never
        /// guesses an asset path; missing art fails the scene build.
        /// </summary>
        private static void SetCenterSprites(Component center)
        {
            var serialized = new SerializedObject(center);
            SetSprite(serialized, "centerBackground", UiSpriteFolder, "ui_0312_hangar_background.png");
            SetSprite(serialized, "centerHeader", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "previewFrame", UiSpriteFolder, "ui_0315_hangar_frame.png");
            SetSprite(serialized, "panel", UiSpriteFolder, "ui_0325_hangar_panel.png");
            SetSprite(serialized, "buttonPlate", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "buttonReady", UiSpriteFolder, "ui_0585_hangar_button_launch.png");
            SetSprite(serialized, "gradePlate", UiSpriteFolder, "ui_0061_hud_portrait_backing.png");
            SetSprite(serialized, "helpButton", UiSpriteFolder, "ui_0073_hud_button_help.png");
            SetMountArt(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the frozen 2011 UI_HelpGame/UI_Data sprites into Info's serialized fields.
        /// The runtime never guesses an asset path; missing art fails the scene build.
        /// </summary>
        private static void SetInfoSprites(Component info)
        {
            var serialized = new SerializedObject(info);
            SetSprite(serialized, "infoBackground", UiSpriteFolder, "ui_0312_hangar_background.png");
            SetSprite(serialized, "infoHeader", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "buttonPlate", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "backButton", UiSpriteFolder, "ui_0324_hangar_button_back_to_center.png");
            SetSprite(serialized, "helpPageOne", UiSpriteFolder, "Help/UI_HelpGame.png");
            SetSprite(serialized, "helpPageTwo", UiSpriteFolder, "Help/UI_HelpGame2.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Wires the frozen UIMain layers and its six live button widgets.</summary>
        private static void SetMainMenuSprites(Component mainMenu)
        {
            var serialized = new SerializedObject(mainMenu);
            SetSprite(serialized, "backdrop", UiSpriteFolder, "ui_0005.png");
            SetSprite(serialized, "titleArt", UiSpriteFolder, "Screens/Title/UI_Main_TitleFull.png");
            SetSprite(serialized, "shipArt", UiSpriteFolder, "Screens/UI_Main_Ship.png");
            SetSprite(serialized, "playNormal", UiSpriteFolder, "UI_Main/447_widget_normal.png");
            SetSprite(serialized, "playHover", UiSpriteFolder, "UI_Main/447_widget_hover.png");
            SetSprite(serialized, "dataNormal", UiSpriteFolder, "UI_Main/450_widget_normal.png");
            SetSprite(serialized, "dataHover", UiSpriteFolder, "UI_Main/450_widget_hover.png");
            SetSprite(serialized, "optionsNormal", UiSpriteFolder, "Widgets/242_widget_normal.png");
            SetSprite(serialized, "optionsHover", UiSpriteFolder, "Widgets/242_widget_hover.png");
            SetSprite(serialized, "creditsNormal", UiSpriteFolder, "UI_Main/453_widget_normal.png");
            SetSprite(serialized, "creditsHover", UiSpriteFolder, "UI_Main/453_widget_hover.png");
            SetSprite(serialized, "linkNormal", UiSpriteFolder, "Widgets/20_widget_normal.png");
            SetSprite(serialized, "linkHover", UiSpriteFolder, "Widgets/20_widget_normal.png");
            SetSprite(serialized, "playerNormal", UiSpriteFolder, "UI_Main/458_widget_normal.png");
            SetSprite(serialized, "playerHover", UiSpriteFolder, "UI_Main/458_widget_hover.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Wires the frozen UI_Options page and its OK/CANCEL plates.</summary>
        private static void SetOptionsSprites(Component options)
        {
            var serialized = new SerializedObject(options);
            SetSprite(serialized, "pageArt", UiSpriteFolder, "Screens/UI_Options.png");
            SetSprite(serialized, "okNormal", UiSpriteFolder, "UI_Options/276_widget_normal.png");
            SetSprite(serialized, "okHover", UiSpriteFolder, "UI_Options/276_widget_hover.png");
            SetSprite(serialized, "cancelNormal", UiSpriteFolder, "UI_Options/273_widget_normal.png");
            SetSprite(serialized, "cancelHover", UiSpriteFolder, "UI_Options/273_widget_hover.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the frozen UI_MapBuilding page and OK plate plus the shared title plate
        /// into Ready. The runtime never guesses an asset path; missing art fails the scene build.
        /// </summary>
        private static void SetReadySprites(Component ready)
        {
            var serialized = new SerializedObject(ready);
            SetSprite(serialized, "pageArt", UiSpriteFolder, "Screens/UI_MapBuilding.png");
            SetSprite(serialized, "titlePlate", UiSpriteFolder, "UI_MapBuilding/309_widget_normal.png");
            SetSprite(serialized, "launchButton", UiSpriteFolder, "UI_MapBuilding/310_widget_normal.png");
            SetSprite(serialized, "backButton", UiSpriteFolder, "ui_0324_hangar_button_back_to_center.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the frozen UIWin/UIFail/UIAllWin backdrops and button plates into Results.
        /// The 13/16/20 widgets are the original UIWin children; 299 is UIFail's BACK plate.
        /// The runtime never guesses an asset path; missing art fails the scene build.
        /// </summary>
        private static void SetResultsSprites(Component results)
        {
            var serialized = new SerializedObject(results);
            SetSprite(serialized, "winBackground", UiSpriteFolder, "ui_0005.png");
            SetSprite(serialized, "failBackground", UiSpriteFolder, "ui_0511.png");
            SetSprite(serialized, "nextButton", UiSpriteFolder, "UI_Win/13_widget_normal.png");
            SetSprite(serialized, "nextButtonHover", UiSpriteFolder, "UI_Win/13_widget_hover.png");
            SetSprite(serialized, "nextButtonPressed", UiSpriteFolder, "UI_Win/13_widget_pressed.png");
            SetSprite(serialized, "restartButton", UiSpriteFolder, "UI_Win/28_widget_normal.png");
            SetSprite(serialized, "restartButtonHover", UiSpriteFolder, "UI_Win/28_widget_hover.png");
            SetSprite(serialized, "restartButtonPressed", UiSpriteFolder, "UI_Win/28_widget_pressed.png");
            SetSprite(serialized, "repairButton", UiSpriteFolder, "UI_Win/16_widget_normal.png");
            SetSprite(serialized, "repairButtonHover", UiSpriteFolder, "UI_Win/16_widget_hover.png");
            SetSprite(serialized, "repairButtonPressed", UiSpriteFolder, "UI_Win/16_widget_pressed.png");
            SetSprite(serialized, "retryButton", UiSpriteFolder, "UI_Fail/517_widget_normal.png");
            SetSprite(serialized, "retryButtonHover", UiSpriteFolder, "UI_Fail/517_widget_hover.png");
            SetSprite(serialized, "retryButtonPressed", UiSpriteFolder, "UI_Fail/517_widget_pressed.png");
            SetSprite(serialized, "backButton", UiSpriteFolder, "UI_Fail/299_widget_normal.png");
            SetSprite(serialized, "backButtonHover", UiSpriteFolder, "UI_Fail/299_widget_hover.png");
            SetSprite(serialized, "backButtonPressed", UiSpriteFolder, "UI_Fail/299_widget_pressed.png");
            SetSprite(serialized, "linkButton", UiSpriteFolder, "UI_Win/20_widget_normal.png");
            SetSprite(serialized, "linkButtonHover", UiSpriteFolder, "UI_Win/20_widget_hover.png");
            SetSprite(serialized, "endingButton", UiSpriteFolder, "UI_AllWin/614_widget_normal.png");
            SetSprite(serialized, "endingButtonHover", UiSpriteFolder, "UI_AllWin/614_widget_hover.png");
            SetSprite(serialized, "endingButtonPressed", UiSpriteFolder, "UI_AllWin/614_widget_pressed.png");
            SetSprite(serialized, "allWinRestartButton", UiSpriteFolder, "UI_AllWin/615_widget_normal.png");
            SetSprite(serialized, "allWinRestartButtonHover", UiSpriteFolder, "UI_AllWin/615_widget_hover.png");
            SetSprite(serialized, "allWinRestartButtonPressed", UiSpriteFolder, "UI_AllWin/615_widget_pressed.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the frozen UIRepair plates into Repair: the shared 2011 gradient/header,
        /// EXIT/RESET/ACCEPT/SELECT ALL, the help icon and the original help page.
        /// </summary>
        private static void SetRepairSprites(Component repair)
        {
            var serialized = new SerializedObject(repair);
            SetSprite(serialized, "repairBackground", UiSpriteFolder, "ui_0312_hangar_background.png");
            SetSprite(serialized, "repairHeader", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "helpButton", UiSpriteFolder, "UI_Repair/73_widget_normal.png");
            SetSprite(serialized, "exitButton", UiSpriteFolder, "UI_Repair/215_widget_normal.png");
            SetSprite(serialized, "exitButtonHover", UiSpriteFolder, "UI_Repair/215_widget_hover.png");
            SetSprite(serialized, "exitButtonPressed", UiSpriteFolder, "UI_Repair/215_widget_pressed.png");
            SetSprite(serialized, "resetButton", UiSpriteFolder, "UI_Repair/167_widget_normal.png");
            SetSprite(serialized, "resetButtonHover", UiSpriteFolder, "UI_Repair/167_widget_hover.png");
            SetSprite(serialized, "resetButtonPressed", UiSpriteFolder, "UI_Repair/167_widget_pressed.png");
            SetSprite(serialized, "acceptButton", UiSpriteFolder, "UI_Repair/216_widget_normal.png");
            SetSprite(serialized, "acceptButtonHover", UiSpriteFolder, "UI_Repair/216_widget_hover.png");
            SetSprite(serialized, "acceptButtonPressed", UiSpriteFolder, "UI_Repair/216_widget_pressed.png");
            SetSprite(serialized, "selectAllButton", UiSpriteFolder, "UI_Repair/225_widget_normal.png");
            SetSprite(serialized, "selectAllButtonHover", UiSpriteFolder, "UI_Repair/225_widget_hover.png");
            SetSprite(serialized, "selectAllButtonPressed", UiSpriteFolder, "UI_Repair/225_widget_pressed.png");
            SetSprite(serialized, "helpPage", UiSpriteFolder, "Help/UI_HelpRepair.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the frozen 2011 UIMakePlayer art into Factory's serialized fields.
        /// The runtime never guesses an asset path; missing art fails the scene build.
        /// </summary>
        private static void SetFactorySprites(Component factory)
        {
            var serialized = new SerializedObject(factory);
            SetSprite(serialized, "factoryBackground", UiSpriteFolder, "ui_0312_hangar_background.png");
            SetSprite(serialized, "factoryHeader", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "previewFrame", UiSpriteFolder, "ui_0315_hangar_frame.png");
            SetSprite(serialized, "buttonPlate", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "buttonMerge", UiSpriteFolder, "ui_0364_hangar_button_merge.png");
            SetSprite(serialized, "buttonNew", UiSpriteFolder, "ui_0361_hangar_button_new.png");
            SetSprite(serialized, "buttonAdd", UiSpriteFolder, "ui_0356_hangar_button_add.png");
            SetSprite(serialized, "buttonBackToCenter", UiSpriteFolder, "ui_0324_hangar_button_back_to_center.png");
            SetSprite(serialized, "buttonPartsCrafts", UiSpriteFolder, "ui_0404_hangar_button_parts_crafts.png");
            SetSprite(serialized, "helpButton", UiSpriteFolder, "ui_0073_hud_button_help.png");
            SetSprite(serialized, "categoryAll", UiSpriteFolder, "ui_0124_hangar_category_all.png");
            SetSprite(serialized, "categoryBody", UiSpriteFolder, "ui_0136_hangar_category_body.png");
            SetSprite(serialized, "categoryWing", UiSpriteFolder, "ui_0133_hangar_category_wing.png");
            SetSprite(serialized, "categoryEngine", UiSpriteFolder, "ui_0130_hangar_category_engine.png");
            SetSprite(serialized, "categoryGun", UiSpriteFolder, "ui_0127_hangar_category_gun.png");
            SetMountArt(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the frozen 2011 UIShop art into Shop's serialized fields.
        /// The runtime never guesses an asset path; missing art fails the scene build.
        /// </summary>
        private static void SetShopSprites(Component shop)
        {
            var serialized = new SerializedObject(shop);
            SetSprite(serialized, "shopBackground", UiSpriteFolder, "ui_0312_hangar_background.png");
            SetSprite(serialized, "shopHeader", UiSpriteFolder, "ui_0107_hangar_header.png");
            SetSprite(serialized, "panel", UiSpriteFolder, "ui_0325_hangar_panel.png");
            SetSprite(serialized, "buttonAccept", UiSpriteFolder, "ui_0170_hangar_button_accept.png");
            SetSprite(serialized, "buttonReset", UiSpriteFolder, "ui_0167_hangar_button_reset.png");
            SetSprite(serialized, "buttonCenter", UiSpriteFolder, "ui_0173_hangar_button_center.png");
            SetSprite(serialized, "buttonInvert", UiSpriteFolder, "ui_0204_hangar_button_invert.png");
            SetSprite(serialized, "newBadge", UiSpriteFolder, "ui_0162_hangar_shop_new_badge.png");
            SetSprite(serialized, "helpOverlay", UiSpriteFolder, "ui_0744_hangar_help_shop.png");
            SetSprite(serialized, "helpButton", UiSpriteFolder, "ui_0073_hud_button_help.png");
            SetSprite(serialized, "categoryAll", UiSpriteFolder, "ui_0124_hangar_category_all.png");
            SetSprite(serialized, "categoryBody", UiSpriteFolder, "ui_0136_hangar_category_body.png");
            SetSprite(serialized, "categoryWing", UiSpriteFolder, "ui_0133_hangar_category_wing.png");
            SetSprite(serialized, "categoryEngine", UiSpriteFolder, "ui_0130_hangar_category_engine.png");
            SetSprite(serialized, "categoryGun", UiSpriteFolder, "ui_0127_hangar_category_gun.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wires the 2x in-game mount sprites and their measured opaque rects. The rect is
        /// decoded from the committed PNG alpha so Center can fit the visible craft, not the
        /// transparent canvas around it.
        /// </summary>
        private static void SetMountArt(SerializedObject serialized)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { MountSpriteFolder });
            var entries = new List<(Sprite sprite, Vector4 content)>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                entries.Add((sprite, MeasureOpaqueRect(path, sprite)));
            }
            entries.Sort((left, right) => string.CompareOrdinal(left.sprite.name, right.sprite.name));

            SerializedProperty mountArt = serialized.FindProperty("mountArt");
            if (mountArt == null)
            {
                throw new InvalidOperationException("[BootSceneBuilder] CenterController is missing mountArt.");
            }

            mountArt.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty element = mountArt.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("sprite").objectReferenceValue = entries[i].sprite;
                element.FindPropertyRelative("contentRect").vector4Value = entries[i].content;
            }
        }

        /// <summary>Opaque bounds in bottom-left texture pixels, matching Unity sprite space.</summary>
        private static Vector4 MeasureOpaqueRect(string texturePath, Sprite sprite)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(texturePath)))
                    return new Vector4(0f, 0f, sprite.rect.width, sprite.rect.height);

                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a <= 8) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < 0) return new Vector4(0f, 0f, sprite.rect.width, sprite.rect.height);
                return new Vector4(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void SetSprite(SerializedObject serialized, string property, string folder, string file)
        {
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
            {
                throw new InvalidOperationException("[BootSceneBuilder] Missing serialized sprite field " + property);
            }

            field.objectReferenceValue = LoadRequired<Sprite>(folder + "/" + file);
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
