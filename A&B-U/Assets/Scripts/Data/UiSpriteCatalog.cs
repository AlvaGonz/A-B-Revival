using System;
using System.Collections.Generic;

namespace ABRevival.Core.Data
{
    public enum UiSpriteKind
    {
        Sprite,
        Shape,
        ExistingPart,
        ExistingPortrait
    }

    /// <summary>
    /// One original UI/HUD symbol selected from the frozen flash UI library.
    /// Logical names are stable and drive deterministic output file names.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class UiSpriteSpec
    {
        public readonly string logicalName;
        public readonly int symbolId;
        public readonly string symbolClass;
        public readonly UiSpriteKind kind;
        public readonly int frame;
        public readonly string externalFile;
        public readonly bool required;
        public readonly string note;

        public UiSpriteSpec(
            string logicalName,
            int symbolId,
            string symbolClass,
            UiSpriteKind kind,
            int frame,
            string externalFile,
            bool required,
            string note)
        {
            this.logicalName = logicalName;
            this.symbolId = symbolId;
            this.symbolClass = symbolClass;
            this.kind = kind;
            this.frame = frame;
            this.externalFile = externalFile;
            this.required = required;
            this.note = note;
        }
    }

    /// <summary>
    /// Manifest of the hangar and HUD sprites extracted from the frozen UI library.
    /// The ordered list is the single source for the editor exporter, the generated
    /// Markdown manifest and the Tier 1 coverage test.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class UiSpriteCatalog
    {
        public const string UiLibrary = "src/assets/binaryData/9_all.AllData_ui.bin";
        public const string DataLibrary = "src/assets/binaryData/4_all.AllData_data.bin";
        public const string ProjectOutputFolder = "Assets/Art/Sprites/UI";
        public const string OutputFolder = "A&B-U/" + ProjectOutputFolder;
        public const string ManifestFile = "docs/assets/ui-sprites.md";
        public const string ContactSheetFile = "docs/assets/ui-sprites-contact-sheet.png";
        public const string AtlasPath = "Assets/Art/Atlases/UiAtlas.spriteatlasv2";

        private static readonly UiSpriteSpec[] Specs =
        {
            // Hangar frame, panels and table.
            new UiSpriteSpec("hangar_background", 312, "", UiSpriteKind.Shape, 1, null, true,
                "Full 640x480 hangar backdrop shape; the frozen hangar has no separate page frame."),
            new UiSpriteSpec("hangar_frame", 315, "", UiSpriteKind.Sprite, 1, null, true,
                "showFly craft preview frame and grid; used as the central hangar frame."),
            new UiSpriteSpec("hangar_header", 107, "", UiSpriteKind.Shape, 1, null, true,
                "Closest header plate; the frozen hangar header is plain new/edit text with no dedicated bar symbol."),
            new UiSpriteSpec("hangar_panel", 325, "", UiSpriteKind.Shape, 1, null, true,
                "640x240 metallic parts-table panel."),
            new UiSpriteSpec("hangar_table_header", 325, "", UiSpriteKind.Shape, 1, null, true,
                "Header strip is embedded in the 640x240 table panel; no standalone header symbol exists."),
            new UiSpriteSpec("hangar_table_row_even", 335, "", UiSpriteKind.Sprite, 1, null, true,
                "608x17 table row background with selection marker."),
            new UiSpriteSpec("hangar_table_row_odd", 345, "", UiSpriteKind.Sprite, 1, null, true,
                "608x17 table row background with selection marker."),
            new UiSpriteSpec("hangar_parts_table", 368, "", UiSpriteKind.Sprite, 1, null, false,
                "Full 660x280 parts table including header and filter buttons."),
            new UiSpriteSpec("hangar_stats_card", 384, "", UiSpriteKind.Sprite, 1, null, true,
                "dtTemp stat card with Hp, Steering, Speed, Fire pow and Parts rows."),
            new UiSpriteSpec("hangar_detail_card", 425, "", UiSpriteKind.Sprite, 1, null, false,
                "showDetail stat card used by the part inspector."),
            new UiSpriteSpec("hangar_shop_form", 163, "", UiSpriteKind.Sprite, 1, null, true,
                "Full 686x165 shop form with five rows."),
            new UiSpriteSpec("hangar_shop_row_even", 93, "", UiSpriteKind.Sprite, 1, null, true,
                "608x17 shop row background with selection marker."),
            new UiSpriteSpec("hangar_shop_row_odd", 106, "", UiSpriteKind.Sprite, 1, null, true,
                "608x17 shop row background with selection marker."),

            // Hangar buttons and category filters.
            new UiSpriteSpec("hangar_button_merge", 364, "", UiSpriteKind.Sprite, 1, null, true,
                "Three-frame MERGE button, frame 1."),
            new UiSpriteSpec("hangar_button_new", 361, "", UiSpriteKind.Sprite, 1, null, true,
                "Three-frame NEW button, frame 1."),
            new UiSpriteSpec("hangar_button_back_to_center", 324, "", UiSpriteKind.Sprite, 1, null, true,
                "Three-frame BACK TO CENTER button, frame 1."),
            new UiSpriteSpec("hangar_button_parts_crafts", 404, "", UiSpriteKind.Sprite, 1, null, true,
                "Three-frame PARTS/CRAFTS toggle, frame 1."),
            new UiSpriteSpec("hangar_button_launch", 585, "", UiSpriteKind.Sprite, 1, null, true,
                "No LAUNCH label exists in the frozen hangar; READY is the closest mission-start button, frame 1."),
            new UiSpriteSpec("hangar_button_accept", 170, "", UiSpriteKind.Sprite, 1, null, false,
                "Three-frame ACCEPT button, frame 1."),
            new UiSpriteSpec("hangar_button_reset", 167, "", UiSpriteKind.Sprite, 1, null, false,
                "Three-frame RESET button, frame 1."),
            new UiSpriteSpec("hangar_button_add", 356, "", UiSpriteKind.Sprite, 1, null, false,
                "Three-frame ADD button, frame 1."),
            new UiSpriteSpec("hangar_category_all", 124, "", UiSpriteKind.Sprite, 1, null, true,
                "Text-label category filter button; the frozen release uses labels, not pictograms."),
            new UiSpriteSpec("hangar_category_body", 136, "", UiSpriteKind.Sprite, 1, null, true,
                "Text-label category filter button; the frozen release uses labels, not pictograms."),
            new UiSpriteSpec("hangar_category_wing", 133, "", UiSpriteKind.Sprite, 1, null, true,
                "Text-label category filter button; the frozen release uses labels, not pictograms."),
            new UiSpriteSpec("hangar_category_engine", 130, "", UiSpriteKind.Sprite, 1, null, true,
                "Text-label category filter button; the frozen release uses labels, not pictograms."),
            new UiSpriteSpec("hangar_category_gun", 127, "", UiSpriteKind.Sprite, 1, null, true,
                "Text-label category filter button; the frozen release uses labels, not pictograms."),

            // Shop thumbnails are the existing part sprites, not separate art.
            new UiSpriteSpec("hangar_shop_thumbnail_body", 680, "Body_1", UiSpriteKind.ExistingPart, 1,
                "A&B-U/Assets/Art/Sprites/Parts/part_000_Body_1.png", true,
                "Existing part export; shop thumbnails are the part sprites themselves."),
            new UiSpriteSpec("hangar_shop_thumbnail_wing", 150, "Wing_1", UiSpriteKind.ExistingPart, 1,
                "A&B-U/Assets/Art/Sprites/Parts/part_173_Wing_1.png", true,
                "Existing part export; shop thumbnails are the part sprites themselves."),
            new UiSpriteSpec("hangar_shop_thumbnail_engine", 270, "Pow_1", UiSpriteKind.ExistingPart, 1,
                "A&B-U/Assets/Art/Sprites/Parts/part_133_Pow_1.png", true,
                "Existing part export; shop thumbnails are the part sprites themselves."),
            new UiSpriteSpec("hangar_shop_thumbnail_gun", 455, "Gun_1", UiSpriteKind.ExistingPart, 1,
                "A&B-U/Assets/Art/Sprites/Parts/part_071_Gun_1.png", true,
                "Existing part export; shop thumbnails are the part sprites themselves."),
            new UiSpriteSpec("hangar_shop_thumbnail_module", 677, "Body_2", UiSpriteKind.ExistingPart, 1,
                "A&B-U/Assets/Art/Sprites/Parts/part_001_Body_2.png", true,
                "The frozen catalog has no module type (Body/Wing/Eng/Gun only); Body_2 is the closest generic thumbnail."),

            // HUD frame, panels and buttons.
            new UiSpriteSpec("hud_frame", 477, "", UiSpriteKind.Shape, 1, null, true,
                "No single full-screen HUD frame exists; 477 is the edge-anchored top-left plate."),
            new UiSpriteSpec("hud_money_panel", 477, "", UiSpriteKind.Shape, 1, null, true,
                "Money is a dynamic TextField in the frozen HUD; this plate is its backing."),
            new UiSpriteSpec("hud_button_pause", 505, "", UiSpriteKind.Sprite, 1, null, true,
                "Three-frame pause button, frame 1."),
            new UiSpriteSpec("hud_button_help", 73, "", UiSpriteKind.Sprite, 1, null, true,
                "Three-frame help button, frame 1."),
            new UiSpriteSpec("hud_radar_ring", 470, "", UiSpriteKind.Shape, 1, null, true,
                "96x119 radar ring shape with range bands."),
            new UiSpriteSpec("hud_radar", 476, "", UiSpriteKind.Sprite, 1, null, false,
                "Radar cluster with arrow and distance/time fields."),
            new UiSpriteSpec("hud_radar_arrow", 474, "", UiSpriteKind.Sprite, 1, null, false,
                "70-frame rotating radar arrow, frame 1."),
            new UiSpriteSpec("hud_minimap_frame", 468, "", UiSpriteKind.Shape, 1, null, true,
                "170x176 minimap bezel shape."),
            new UiSpriteSpec("hud_minimap", 469, "", UiSpriteKind.Sprite, 1, null, false,
                "Minimap cluster with grid and map slot."),
            new UiSpriteSpec("hud_portrait_backing", 61, "", UiSpriteKind.Sprite, 1, null, false,
                "49x51 dark portrait backing used by the HUD portrait tile."),
            new UiSpriteSpec("hud_portrait_health_pip", 491, "", UiSpriteKind.Sprite, 1, null, false,
                "10x10 portrait health pip."),

            // Portrait frames are the existing dialogue portraits; do not re-export them.
            new UiSpriteSpec("hud_pilot_portrait_frame", 56, "face", UiSpriteKind.ExistingPortrait, 1,
                "A&B-U/Assets/Art/Talk/Portraits/Michael.png", true,
                "Existing dialogue portrait frame 1; not re-exported by this pipeline."),
            new UiSpriteSpec("hud_captain_portrait_frame", 56, "face", UiSpriteKind.ExistingPortrait, 4,
                "A&B-U/Assets/Art/Talk/Portraits/Captain.png", true,
                "Existing dialogue portrait frame 4; not re-exported by this pipeline."),

            // HUD bars, reticle and badges.
            new UiSpriteSpec("hud_health_bar", 483, "", UiSpriteKind.Sprite, 1, null, true,
                "Red health fill bar; 180px source width."),
            new UiSpriteSpec("hud_energy_bar", 487, "", UiSpriteKind.Sprite, 1, null, true,
                "Blue energy fill bar; 180px source width."),
            new UiSpriteSpec("hud_shield_bar", 485, "", UiSpriteKind.Sprite, 1, null, true,
                "Green powb boost/shield fill bar; 180px source width."),
            new UiSpriteSpec("hud_bar_frame", 480, "", UiSpriteKind.Shape, 1, null, true,
                "Double-outlined 185x37 bar frame."),
            new UiSpriteSpec("hud_bar_frame_alt", 481, "", UiSpriteKind.Shape, 1, null, false,
                "Single-outlined 181x33 bar frame."),
            new UiSpriteSpec("hud_reticle", 917, "", UiSpriteKind.Sprite, 1, null, true,
                "MouseOut reticle with ring and pointer."),
            new UiSpriteSpec("hud_mode_badge", 493, "", UiSpriteKind.Shape, 1, null, true,
                "No tactical-mode art exists because modes were added in the revival; the AI-count plate is reused as a generic badge."),
            new UiSpriteSpec("hud_camera_badge", 493, "", UiSpriteKind.Shape, 1, null, true,
                "No camera-badge art exists because the badge was added in the revival; the AI-count plate is reused as a generic badge."),
            new UiSpriteSpec("hud_damage_telemetry_row", 481, "", UiSpriteKind.Shape, 1, null, true,
                "The original is a dynamic TextField above the life bar; this bar frame is the closest row backing."),
            new UiSpriteSpec("hud_ai_count", 495, "", UiSpriteKind.Sprite, 1, null, false,
                "AI x 10 counter plate."),
            new UiSpriteSpec("hud_standby_badge", 509, "", UiSpriteKind.Sprite, 1, null, false,
                "161-frame Stand By status badge, frame 1."),

            // Full-page reference renders for side-by-side parity work.
            new UiSpriteSpec("reference_hangar_page", 440, "UI_MakePlayer", UiSpriteKind.Sprite, 1, null, false,
                "Full-page reference render, not a reusable sprite."),
            new UiSpriteSpec("reference_shop_page", 205, "UI_Shop", UiSpriteKind.Sprite, 1, null, false,
                "Full-page reference render, not a reusable sprite."),
            new UiSpriteSpec("reference_hud_page", 510, "UI_Game", UiSpriteKind.Sprite, 1, null, false,
                "Full-page reference render, not a reusable sprite."),
            new UiSpriteSpec("reference_mission_select_page", 608, "UI_ChooseFlyAI", UiSpriteKind.Sprite, 1, null, false,
                "Full-page reference render, not a reusable sprite.")
        };

        public static readonly IReadOnlyList<UiSpriteSpec> Sprites = SortByLogicalName(Specs);

        public static string FileName(UiSpriteSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.externalFile)) return spec.externalFile;

            return OutputFolder + "/ui_" + spec.symbolId.ToString("D4") + "_" +
                   SpriteNaming.Sanitize(spec.logicalName) + ".png";
        }

        public static bool IsGenerated(UiSpriteSpec spec)
        {
            return spec.kind == UiSpriteKind.Sprite || spec.kind == UiSpriteKind.Shape;
        }

        private static IReadOnlyList<UiSpriteSpec> SortByLogicalName(UiSpriteSpec[] specs)
        {
            var sorted = (UiSpriteSpec[])specs.Clone();
            Array.Sort(sorted, (left, right) => string.CompareOrdinal(left.logicalName, right.logicalName));
            return sorted;
        }
    }
}
