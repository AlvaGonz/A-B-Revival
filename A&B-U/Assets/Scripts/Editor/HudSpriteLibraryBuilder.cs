using System;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using UnityEditor;
using UnityEngine;

namespace ABRevival.Editor
{
    /// <summary>
    /// Regenerates the <see cref="HudSpriteLibrary"/> asset from the sprite manifest that
    /// ticket #166 committed, so the HUD never hard-codes a PNG path. One editor command and
    /// one batchmode entry point produce the same asset.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class HudSpriteLibraryBuilder
    {
        public const string AssetPath = "Assets/Resources/UI/HudSpriteLibrary.asset";
        private const string ProjectPrefix = "A&B-U/";

        [MenuItem("Tools/A&B Revival/Build HUD Sprite Library")]
        public static void Build()
        {
            HudSpriteLibrary library = AssetDatabase.LoadAssetAtPath<HudSpriteLibrary>(AssetPath);
            if (library == null)
            {
                string folder = Path.GetDirectoryName(AssetPath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(folder)?.Replace('\\', '/'), Path.GetFileName(folder));

                library = ScriptableObject.CreateInstance<HudSpriteLibrary>();
                AssetDatabase.CreateAsset(library, AssetPath);
            }

            var serialized = new SerializedObject(library);
            Assign(serialized, "_moneyPanel", Load("hud_money_panel"));
            Assign(serialized, "_pauseButton", Load("hud_button_pause"));
            Assign(serialized, "_helpButton", Load("hud_button_help"));
            Assign(serialized, "_radar", Load("hud_radar_ring"));
            Assign(serialized, "_radarArrow", Load("hud_radar_arrow"));
            // The extracted AI plate has "AI x 10" baked in; the live count needs a blank plate.
            Assign(serialized, "_aiCount", Load("hud_mode_badge"));
            Assign(serialized, "_minimap", Load("hud_minimap"));
            Assign(serialized, "_pilotPortrait", Load("hud_pilot_portrait_frame"));
            Assign(serialized, "_captainPortrait", Load("hud_captain_portrait_frame"));
            Assign(serialized, "_portraitBacking", Load("hud_portrait_backing"));
            Assign(serialized, "_healthBar", Load("hud_health_bar"));
            Assign(serialized, "_energyBar", Load("hud_energy_bar"));
            Assign(serialized, "_shieldBar", Load("hud_shield_bar"));
            Assign(serialized, "_barFrame", Load("hud_bar_frame"));
            Assign(serialized, "_reticle", Load("hud_reticle"));
            Assign(serialized, "_modeBadge", Load("hud_mode_badge"));
            Assign(serialized, "_cameraBadge", Load("hud_camera_badge"));
            Assign(serialized, "_damageTelemetryRow", Load("hud_damage_telemetry_row"));
            Assign(serialized, "_portraitHealthPip", Load("hud_portrait_health_pip"));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HudSpriteLibraryBuilder] Wrote {AssetPath}.");
        }

        /// <summary>Batchmode entry point (-executeMethod ABRevival.Editor.HudSpriteLibraryBuilder.BuildBatch).</summary>
        public static void BuildBatch()
        {
            Build();
        }

        private static Sprite Load(string logicalName)
        {
            for (int i = 0; i < UiSpriteCatalog.Sprites.Count; i++)
            {
                UiSpriteSpec spec = UiSpriteCatalog.Sprites[i];
                if (!string.Equals(spec.logicalName, logicalName, StringComparison.Ordinal)) continue;

                string path = UiSpriteCatalog.FileName(spec);
                if (path.StartsWith(ProjectPrefix, StringComparison.Ordinal))
                    path = path.Substring(ProjectPrefix.Length);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    throw new InvalidOperationException($"[HudSpriteLibraryBuilder] Missing sprite at {path}.");
                return sprite;
            }

            throw new InvalidOperationException(
                $"[HudSpriteLibraryBuilder] '{logicalName}' is not in the UI sprite catalog.");
        }

        private static void Assign(SerializedObject serialized, string field, Sprite sprite)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"[HudSpriteLibraryBuilder] Missing field {field}.");
            property.objectReferenceValue = sprite;
        }
    }
}
