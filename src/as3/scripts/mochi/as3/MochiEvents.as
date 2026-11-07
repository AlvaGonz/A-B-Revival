package mochi.as3
{
   public class MochiEvents
   {
      public static const ACHIEVEMENTS_OWNED:String = "AchievementOwned";
      public static const ACHIEVEMENT_NEW:String = "AchievementReceived";
      public static const GAME_ACHIEVEMENTS:String = "GameAchievements";
      public static const ERROR:String = "Error";
      public static const IO_ERROR:String = "IOError";
      public static const IO_PENDING:String = "IOPending";
      public static const ALIGN_TOP_LEFT:String = "ALIGN_TL";
      public static const ALIGN_TOP:String = "ALIGN_T";
      public static const ALIGN_TOP_RIGHT:String = "ALIGN_TR";
      public static const ALIGN_LEFT:String = "ALIGN_L";
      public static const ALIGN_CENTER:String = "ALIGN_C";
      public static const ALIGN_RIGHT:String = "ALIGN_R";
      public static const ALIGN_BOTTOM_LEFT:String = "ALIGN_BL";
      public static const ALIGN_BOTTOM:String = "ALIGN_B";
      public static const ALIGN_BOTTOM_RIGHT:String = "ALIGN_BR";
      public static const FORMAT_SHORT:String = "ShortForm";
      public static const FORMAT_LONG:String = "LongForm";
      public static const FORMAT_NONE:String = "NoForm";
      
      public function MochiEvents()
      {
         super();
      }
      
      public static function getVersion() : String
      {
         return "3.0.4";
      }
      
      public static function getAchievements(param1:Object = null) : void
      {
      }
      
      public static function unlockAchievement(param1:Object) : void
      {
      }
      
      public static function startSession(param1:String) : void
      {
      }
      
      public static function showAwards(param1:Object = null) : void
      {
      }
      
      public static function setNotifications(param1:Object) : void
      {
      }
      
      public static function addEventListener(param1:String, param2:Function) : void
      {
      }
      
      public static function triggerEvent(param1:String, param2:Object) : void
      {
      }
      
      public static function removeEventListener(param1:String, param2:Function) : void
      {
      }
      
      public static function startPlay(param1:String = "gameplay") : void
      {
      }
      
      public static function endPlay() : void
      {
      }
      
      public static function trackEvent(param1:String, param2:* = null) : void
      {
      }
   }
}
