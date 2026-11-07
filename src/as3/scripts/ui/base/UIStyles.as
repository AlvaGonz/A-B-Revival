package ui.base
{
   import flash.filters.GlowFilter;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;

   /**
    * Centralized typography and styling helper for Assembly & Battle.
    * Standardizes font tokens, size hierarchy, palette roles, and CRT/holographic glows.
    */
   public class UIStyles
   {
      // Font tokens
      public static const FONT_TITLE:String = "SF TransRobotics";
      public static const FONT_HEADER:String = "SF Zero Gravity";
      public static const FONT_BODY:String = "Arial";
      public static const FONT_SANS:String = "_sans";

      // Size hierarchy tokens
      public static const SIZE_SMALL:int = 10;
      public static const SIZE_HUD:int = 11;
      public static const SIZE_ALERT:int = 13;
      public static const SIZE_HEADER:int = 16;
      public static const SIZE_TITLE:int = 20;

      // Color tokens
      public static const COLOR_SYSTEM_CYAN:uint = 0x00CCFF;
      public static const COLOR_SYSTEM_LIGHT:uint = 0x66CCFF;
      public static const COLOR_DAMAGE_CRIMSON:uint = 0xFF5555;
      public static const COLOR_DAMAGE_RED:uint = 0xFF3333;
      public static const COLOR_CAPTAIN_AMBER:uint = 0xFFCC00;
      public static const COLOR_CAPTAIN_GOLD:uint = 0xFFAA00;
      public static const COLOR_SUCCESS_MINT:uint = 0x44FF88;
      public static const COLOR_WHITE:uint = 0xFFFFFF;
      public static const COLOR_SILVER:uint = 0xBFBFBF;

      public function UIStyles()
      {
         super();
      }

      public static function createFormat(param1:String = "_sans", param2:int = 11, param3:uint = 0x00CCFF, param4:Boolean = true, param5:String = "center") : TextFormat
      {
         var _loc6_:TextFormat = new TextFormat(param1, param2, param3, param4);
         _loc6_.align = param5;
         return _loc6_;
      }

      public static function applyFormat(param1:TextField, param2:String = "_sans", param3:int = 11, param4:uint = 0x00CCFF, param5:Boolean = true, param6:String = "center") : TextFormat
      {
         var _loc7_:TextFormat = createFormat(param2, param3, param4, param5, param6);
         param1.defaultTextFormat = _loc7_;
         param1.setTextFormat(_loc7_);
         return _loc7_;
      }

      public static function applyGlow(param1:TextField, param2:uint = 0x000000, param3:Number = 0.9, param4:Number = 3, param5:Number = 3) : void
      {
         param1.filters = [new GlowFilter(param2, param3, param4, param4, param5, 1)];
      }

      public static function applyHoloGlow(param1:TextField, param2:uint, param3:Number = 3) : void
      {
         var _loc4_:GlowFilter = new GlowFilter(0x000000, 0.9, param3, param3, 3, 1);
         var _loc5_:GlowFilter = new GlowFilter(param2, 0.45, param3 * 2, param3 * 2, 1.5, 1);
         param1.filters = [_loc4_, _loc5_];
      }
   }
}
