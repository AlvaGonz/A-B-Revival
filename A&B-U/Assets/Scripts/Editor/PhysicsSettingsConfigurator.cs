using System;
using Unity.U2D.Physics;
using UnityEditor;
using UnityEngine;

namespace ABRevival.Editor
{
    /// <summary>
    /// Creates and assigns the PhysicsCore2D settings asset for the pixel-based world standard
    /// (ADR 0017): 1 Unity unit = 1 stage pixel, lengthUnitsPerMeter 100, thresholds scaled by 100.
    /// Run before any physics object exists; lengthUnitsPerMeter is one-shot.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class PhysicsSettingsConfigurator
    {
        private const string SettingsAssetPath = "Assets/Settings/PhysicsCoreSettings2D.asset";
        private const string ProjectSettingsAssetPath = "ProjectSettings/PhysicsCoreProjectSettings2D.asset";

        private const float LengthUnitsPerMeter = 100f;
        private const float GravityY = -981f;
        private const float MaximumLinearSpeed = 40000f;
        private const float BounceThreshold = 100f;
        private const float ContactHitEventThreshold = 100f;
        private const float ContactSpeed = 300f;
        private const float ContactRecycleDistance = 5f;
        private const float SleepThreshold = 5f;

        [MenuItem("Tools/A&B Revival/Apply Physics Core 2D World-Unit Baseline")]
        public static void ApplyWorldUnitPhysicsSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PhysicsCoreSettings2D>(SettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PhysicsCoreSettings2D>();
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            }
            else if (!Mathf.Approximately(settings.lengthUnitsPerMeter, LengthUnitsPerMeter))
            {
                Debug.LogWarning(
                    $"[PhysicsSettingsConfigurator] Existing asset had lengthUnitsPerMeter {settings.lengthUnitsPerMeter}; " +
                    "changing it after any physics object exists is a migration, not a setting.");
            }

            settings.lengthUnitsPerMeter = LengthUnitsPerMeter;

            var world = settings.physicsWorldDefinition;
            world.gravity = new Vector2(0f, GravityY);
            world.maximumLinearSpeed = MaximumLinearSpeed;
            world.bounceThreshold = BounceThreshold;
            world.contactHitEventThreshold = ContactHitEventThreshold;
            world.contactSpeed = ContactSpeed;
            world.contactRecycleDistance = ContactRecycleDistance;
            settings.physicsWorldDefinition = world;

            var body = settings.physicsBodyDefinition;
            body.sleepThreshold = SleepThreshold;
            settings.physicsBodyDefinition = body;

            EditorUtility.SetDirty(settings);
            AssignProjectSettings(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[PhysicsSettingsConfigurator] Applied world-unit baseline to {SettingsAssetPath} and assigned it in " +
                $"{ProjectSettingsAssetPath}: lengthUnitsPerMeter={LengthUnitsPerMeter}, gravityY={GravityY}, " +
                $"maximumLinearSpeed={MaximumLinearSpeed}.");
        }

        private static void AssignProjectSettings(PhysicsCoreSettings2D settings)
        {
            UnityEngine.Object[] projectSettings = AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsAssetPath);
            if (projectSettings == null || projectSettings.Length == 0)
            {
                throw new InvalidOperationException($"Could not load {ProjectSettingsAssetPath}.");
            }

            var serializedSettings = new SerializedObject(projectSettings[0]);
            SerializedProperty settingsReference = serializedSettings.FindProperty("m_PhysicsCoreSettings");
            if (settingsReference == null)
            {
                throw new InvalidOperationException(
                    $"{ProjectSettingsAssetPath} has no m_PhysicsCoreSettings property.");
            }

            settingsReference.objectReferenceValue = settings;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
