package mochi.as3
{
   import flash.display.MovieClip;
   
   public class MochiAd
   {
      public function MochiAd()
      {
         super();
      }
      
      public static function getVersion() : String
      {
         return "3.0.4";
      }
      
      public static function doOnEnterFrame(param1:MovieClip) : void
      {
      }
      
      public static function createEmptyMovieClip(param1:Object, param2:String, param3:Number) : MovieClip
      {
         var _loc4_:MovieClip = new MovieClip();
         if(param1)
         {
            param1.addChild(_loc4_);
         }
         return _loc4_;
      }
      
      public static function showPreGameAd(param1:Object) : void
      {
         if(param1 && param1.ad_finished is Function)
         {
            param1.ad_finished();
         }
      }
      
      public static function showClickAwayAd(param1:Object) : void
      {
         if(param1 && param1.ad_finished is Function)
         {
            param1.ad_finished();
         }
      }
      
      public static function showInterLevelAd(param1:Object) : void
      {
         if(param1 && param1.ad_finished is Function)
         {
            param1.ad_finished();
         }
      }
      
      public static function showPreloaderAd(param1:Object) : void
      {
         if(param1 && param1.ad_finished is Function)
         {
            param1.ad_finished();
         }
      }
      
      public static function showTimedAd(param1:Object) : void
      {
         if(param1 && param1.ad_finished is Function)
         {
            param1.ad_finished();
         }
      }
      
      public static function _allowDomains(param1:String) : String
      {
         return param1;
      }
      
      public static function load(param1:Object) : MovieClip
      {
         if(param1 && param1.ad_finished is Function)
         {
            param1.ad_finished();
         }
         return new MovieClip();
      }
      
      public static function unload(param1:Object) : Boolean
      {
         return true;
      }
      
      public static function _cleanup(param1:Object) : void
      {
      }
      
      public static function _isNetworkAvailable() : Boolean
      {
         return false;
      }
      
      public static function _getRes(param1:Object, param2:Object) : Array
      {
         return [640, 480];
      }
      
      public static function _parseOptions(param1:Object, param2:Object) : Object
      {
         return param1;
      }
      
      public static function rpc(param1:Object, param2:Number, param3:Object) : void
      {
      }
      
      public static function setValue(param1:Object, param2:String, param3:Object) : void
      {
      }
      
      public static function getValue(param1:Object, param2:String) : Object
      {
         return null;
      }
      
      public static function runMethod(param1:Object, param2:String, param3:Array) : Object
      {
         return null;
      }
      
      public static function adShowing(param1:Object) : void
      {
      }
   }
}
